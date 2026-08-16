using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PriceTracker.Application;
using PriceTracker.Application.DTOs;
using PriceTracker.Application.Interfaces;
using PuppeteerSharp;

namespace PriceTracker.Infrastructure.Scraping;

public sealed class PuppeteerPriceScraper : IPriceScraper, IAsyncDisposable
{
    private readonly ILogger<PuppeteerPriceScraper> _logger;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private IBrowser? _browser;
    private bool _chromeReady;

    public PuppeteerPriceScraper(ILogger<PuppeteerPriceScraper> logger)
    {
        _logger = logger;
    }

    public async Task<ScrapeResultDto> ScrapeAsync(string productUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(productUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Fail("Geçersiz ürün URL'i.");
        }

        var storeName = StoreNameResolver.FromUrl(productUrl);
        // Temu vb. linklerde görsel query parametresinde gelir; takip çöpü navigasyonu bozar
        var urlImage = TryExtractImageFromQuery(uri);
        var browseUrl = SanitizeBrowseUrl(uri);
        var originalUrl = uri.GetLeftPart(UriPartial.Query).TrimEnd('?');

        MetaFetch meta = new(null, urlImage, null, null);

        try
        {
            // 1) HTTP: hem orijinal hem temiz URL — her siteden görsel/fiyat yakala
            var metaBrowse = await TryFetchMetaAsync(browseUrl, cancellationToken);
            MetaFetch metaOriginal = metaBrowse;
            if (!string.Equals(browseUrl, originalUrl, StringComparison.OrdinalIgnoreCase))
                metaOriginal = await TryFetchMetaAsync(originalUrl, cancellationToken);

            meta = MergeMeta(metaBrowse, metaOriginal, urlImage);

            // Fiyat + görsel birlikte geldiyse Puppeteer'a girme
            if (meta.Price is not null && !string.IsNullOrWhiteSpace(meta.ImageUrl))
            {
                _logger.LogInformation(
                    "Meta scrape başarılı. Url={Url} Price={Price} Image={HasImage}",
                    browseUrl,
                    meta.Price,
                    true);
                return new ScrapeResultDto(
                    true,
                    meta.Price,
                    meta.Title,
                    meta.ImageUrl,
                    storeName,
                    meta.Currency ?? "TRY",
                    true,
                    null);
            }

            // Görsel URL'de varsa ama fiyat yoksa: görseli kaybetme; fiyat için devam
            if (meta.Price is not null && string.IsNullOrWhiteSpace(meta.ImageUrl))
            {
                _logger.LogInformation(
                    "Meta fiyat bulundu, görsel için tarayıcı deneniyor. Url={Url}",
                    browseUrl);
            }
            else if (!string.IsNullOrWhiteSpace(meta.ImageUrl) && meta.Price is null)
            {
                _logger.LogInformation(
                    "Meta görsel bulundu, fiyat için tarayıcı deneniyor. Url={Url}",
                    browseUrl);
            }

            await EnsureBrowserAsync(cancellationToken);

            await using var page = await _browser!.NewPageAsync();
            await page.SetUserAgentAsync(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            await page.SetViewportAsync(new ViewPortOptions { Width = 1366, Height = 900 });

            _logger.LogInformation("Sayfa açılıyor: {Url}", browseUrl);

            IResponse? response = null;
            try
            {
                response = await page.GoToAsync(
                    browseUrl,
                    new NavigationOptions
                    {
                        WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
                        Timeout = 45000
                    });
            }
            catch (NavigationException navEx)
            {
                _logger.LogWarning(navEx, "Tarayıcı navigasyonu başarısız, meta/URL görseli kullanılacak. Url={Url}", browseUrl);
                return PartialFromMeta(meta, storeName, urlImage,
                    "Sayfa tam açılamadı; mevcut görsel/fiyat kaydedildi.");
            }

            if (response is null || !response.Ok && response.Status is not (System.Net.HttpStatusCode.NotModified))
            {
                var status = response?.Status.ToString() ?? "null";
                _logger.LogWarning("HTTP yanıtı başarısız. Url={Url} Status={Status}", browseUrl, status);
            }

            // JS ile yüklenen fiyatlar için kısa bekleme
            await Task.Delay(1500, cancellationToken);

            var payloadJson = await page.EvaluateFunctionAsync<string>(ExtractionScript);
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            var title = GetString(root, "title") ?? meta.Title;
            var imageUrl = GetString(root, "imageUrl") ?? meta.ImageUrl ?? urlImage;
            var priceText = GetString(root, "priceText");
            var currency = GetString(root, "currency") ?? meta.Currency ?? "TRY";
            var bodyText = GetString(root, "bodySample") ?? string.Empty;

            // JS seçicileri kaçırsa sayfa HTML'inden genel görsel tarama
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                var pageHtml = await page.GetContentAsync();
                imageUrl = ExtractBestImageUrl(pageHtml, new Uri(browseUrl)) ?? meta.ImageUrl ?? urlImage;
            }

            var price = ParsePrice(priceText) ?? meta.Price;
            if (price is null)
            {
                _logger.LogWarning("Fiyat parse edilemedi. Url={Url} Raw={Raw}", browseUrl, priceText);
                return new ScrapeResultDto(
                    false,
                    null,
                    title,
                    imageUrl,
                    storeName,
                    currency,
                    DetectInStock(bodyText),
                    "Sayfadan fiyat çıkarılamadı. Site bot engeli uygulamış olabilir. Karttan tekrar dene.");
            }

            var inStock = DetectInStock(bodyText);

            return new ScrapeResultDto(
                true,
                price,
                title,
                imageUrl,
                storeName,
                currency,
                inStock,
                null);
        }
        catch (NavigationException ex)
        {
            _logger.LogError(ex, "Navigasyon hatası. Url={Url}", browseUrl);
            return PartialFromMeta(meta, storeName, urlImage, $"Sayfa açılamadı: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scrape hatası. Url={Url}", browseUrl);
            return PartialFromMeta(meta, storeName, urlImage, $"Scrape hatası: {ex.Message}");
        }
    }

    private static ScrapeResultDto PartialFromMeta(
        MetaFetch meta,
        string? storeName,
        string? urlImage,
        string error)
    {
        var image = !string.IsNullOrWhiteSpace(meta.ImageUrl) ? meta.ImageUrl : urlImage;
        var hasPrice = meta.Price is not null;
        // Görsel veya fiyat varsa kısmi başarı — PriceCheckService görseli kaydeder
        if (hasPrice || !string.IsNullOrWhiteSpace(image) || !string.IsNullOrWhiteSpace(meta.Title))
        {
            return new ScrapeResultDto(
                hasPrice,
                meta.Price,
                meta.Title,
                image,
                storeName,
                meta.Currency ?? "TRY",
                true,
                hasPrice ? null : error);
        }

        return Fail(error, storeName);
    }

    /// <summary>Temu top_gallery_url vb. query görsellerini alır.</summary>
    internal static string? TryExtractImageFromQuery(Uri uri)
    {
        if (string.IsNullOrEmpty(uri.Query)) return null;

        string[] keys =
        [
            "top_gallery_url", "gallery_url", "image", "img", "image_url", "imageUrl",
            "thumbnail", "thumb", "photo", "pic"
        ];

        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            var key = Uri.UnescapeDataString(part[..eq]);
            if (!keys.Contains(key, StringComparer.OrdinalIgnoreCase)) continue;
            var value = Uri.UnescapeDataString(part[(eq + 1)..]);
            if (value.StartsWith("//", StringComparison.Ordinal))
                value = "https:" + value;
            if (Uri.TryCreate(value, UriKind.Absolute, out var imgUri) &&
                (imgUri.Scheme == Uri.UriSchemeHttp || imgUri.Scheme == Uri.UriSchemeHttps))
                return imgUri.ToString();
        }

        return null;
    }

    /// <summary>Affiliate / tracking query'sini temizler; Chromium navigasyon hatalarını azaltır.</summary>
    internal static string SanitizeBrowseUrl(Uri uri)
    {
        // Temu ve benzeri: path yeterli; query referrerPolicy / tracking patlatabiliyor
        var host = uri.Host.ToLowerInvariant();
        if (host.Contains("temu.com", StringComparison.Ordinal) ||
            host.Contains("kwcdn.com", StringComparison.Ordinal))
            return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";

        // Diğer sitelerde path + kısa query (ürün kimliği) tut; uzun tracking kes
        if (uri.Query.Length > 180)
            return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";

        return uri.GetLeftPart(UriPartial.Query).TrimEnd('?');
    }

    private static MetaFetch MergeMeta(MetaFetch a, MetaFetch b, string? urlImage)
    {
        var image = FirstNonEmpty(a.ImageUrl, b.ImageUrl, urlImage);
        return new MetaFetch(
            FirstNonEmpty(a.Title, b.Title),
            image,
            a.Price ?? b.Price,
            FirstNonEmpty(a.Currency, b.Currency) ?? "TRY");
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private sealed record MetaFetch(string? Title, string? ImageUrl, decimal? Price, string? Currency);

    private async Task<MetaFetch> TryFetchMetaAsync(string productUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var http = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            })
            {
                Timeout = TimeSpan.FromSeconds(25)
            };
            http.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8");
            http.DefaultRequestHeaders.TryAddWithoutValidation(
                "Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");

            using var response = await http.GetAsync(productUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Meta HTTP {Status} Url={Url}", (int)response.StatusCode, productUrl);
                return new MetaFetch(null, null, null, null);
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            Uri.TryCreate(productUrl, UriKind.Absolute, out var baseUri);

            string? Meta(string property)
            {
                var m = Regex.Match(
                    html,
                    $@"<meta[^>]+(?:property|name)=[""']{Regex.Escape(property)}[""'][^>]+content=[""']([^""']*)[""']",
                    RegexOptions.IgnoreCase);
                if (!m.Success)
                {
                    m = Regex.Match(
                        html,
                        $@"<meta[^>]+content=[""']([^""']*)[""'][^>]+(?:property|name)=[""']{Regex.Escape(property)}[""']",
                        RegexOptions.IgnoreCase);
                }
                if (!m.Success) return null;
                var value = System.Net.WebUtility.HtmlDecode(m.Groups[1].Value)?.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }

            var title = Meta("og:title") ?? Meta("twitter:title") ?? Meta("title");
            if (title is null)
            {
                var t = Regex.Match(html, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
                if (t.Success) title = System.Net.WebUtility.HtmlDecode(t.Groups[1].Value).Trim();
            }

            var image = Meta("og:image")
                        ?? Meta("og:image:url")
                        ?? Meta("twitter:image")
                        ?? Meta("og:image:secure_url")
                        ?? Meta("twitter:image:src");
            image = ResolveUrl(image, baseUri);
            image ??= ExtractBestImageUrl(html, baseUri);

            var priceRaw = Meta("product:price:amount") ?? Meta("og:price:amount") ?? Meta("twitter:data1");
            var currency = Meta("product:price:currency") ?? Meta("og:price:currency") ?? "TRY";

            if (priceRaw is null)
            {
                var ld = Regex.Match(html, @"""price""\s*:\s*""?([\d.,]+)""?", RegexOptions.IgnoreCase);
                if (ld.Success) priceRaw = ld.Groups[1].Value;
            }

            return new MetaFetch(title, image, ParsePrice(priceRaw), currency);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Meta HTTP scrape başarısız. Url={Url}", productUrl);
            return new MetaFetch(null, null, null, null);
        }
    }

    private static string? ResolveUrl(string? raw, Uri? baseUri)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var url = System.Net.WebUtility.HtmlDecode(raw).Trim();
        if (url.StartsWith("//", StringComparison.Ordinal))
            url = "https:" + url;
        if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
            return abs.ToString();
        if (baseUri is not null && Uri.TryCreate(baseUri, url, out var rel))
            return rel.ToString();
        return null;
    }

    private async Task EnsureBrowserAsync(CancellationToken cancellationToken)
    {
        await _browserLock.WaitAsync(cancellationToken);
        try
        {
            var executablePath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");

            if (!_chromeReady)
            {
                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    _logger.LogInformation("Chromium indiriliyor/hazırlanıyor (ilk çalıştırmada biraz sürebilir)...");
                    var fetcher = new BrowserFetcher();
                    await fetcher.DownloadAsync();
                }
                else
                {
                    _logger.LogInformation("Sistem Chromium kullanılıyor: {Path}", executablePath);
                }

                _chromeReady = true;
            }

            if (_browser is null || _browser.IsClosed)
            {
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = string.IsNullOrWhiteSpace(executablePath) ? null : executablePath,
                    Args =
                    [
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu"
                    ]
                });
            }
        }
        finally
        {
            _browserLock.Release();
        }
    }

    /// <summary>
    /// og:image boş olan siteler için HTML içinden en iyi ürün görselini seçer (tüm mağazalar).
    /// </summary>
    internal static string? ExtractBestImageUrl(string html, Uri? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;

        var candidates = new List<(string Url, int Score)>();

        void Consider(string? raw, int bonus = 0)
        {
            var resolved = ResolveUrl(raw, baseUri);
            if (string.IsNullOrWhiteSpace(resolved)) return;
            if (!Uri.TryCreate(resolved, UriKind.Absolute, out var uri)) return;
            if (uri.Scheme is not ("http" or "https")) return;

            var path = uri.AbsolutePath.ToLowerInvariant();
            // Banner / ikon / font gürültüsünü ele
            if (path.Contains("banner", StringComparison.Ordinal) ||
                path.Contains("logo", StringComparison.Ordinal) ||
                path.Contains("icon", StringComparison.Ordinal) ||
                path.Contains("sprite", StringComparison.Ordinal) ||
                path.Contains("/themes/", StringComparison.Ordinal) ||
                path.EndsWith(".svg", StringComparison.Ordinal) ||
                path.EndsWith(".woff2", StringComparison.Ordinal))
                return;

            var host = uri.Host.ToLowerInvariant();
            var score = bonus;
            if (path.Contains("/originals/", StringComparison.Ordinal)) score += 50;
            if (path.Contains("/cache/", StringComparison.Ordinal)) score += 30;
            if (path.Contains("/product", StringComparison.Ordinal) ||
                path.Contains("/catalog", StringComparison.Ordinal) ||
                path.Contains("/media/", StringComparison.Ordinal) ||
                path.Contains("/local-image/", StringComparison.Ordinal))
                score += 15;

            // Yaygın e-ticaret CDN'leri
            string[] productCdns =
            [
                "dsmcdn", "dr.com.tr", "hbcdn", "productimages", "n11scdn", "n11static",
                "media-amazon", "m.media-amazon", "images-na.ssl-images-amazon",
                "lcwaikiki", "static.lcw", "img-trendyol", "mncdn", "akinon",
                "kwcdn", "img.kwcdn", "img-eu.kwcdn"
            ];
            if (productCdns.Any(c => host.Contains(c, StringComparison.Ordinal) ||
                                     path.Contains(c, StringComparison.Ordinal)))
                score += 40;

            if (path.Contains("600x600", StringComparison.Ordinal) ||
                path.Contains("800x800", StringComparison.Ordinal) ||
                path.Contains("1000x", StringComparison.Ordinal))
                score += 25;
            if (path.Contains("500x400", StringComparison.Ordinal)) score += 15;
            if (path.Contains("1200", StringComparison.Ordinal) ||
                path.Contains("org_zoom", StringComparison.Ordinal) ||
                path.Contains("_large", StringComparison.Ordinal))
                score += 20;
            if (path.Contains("64x64", StringComparison.Ordinal) ||
                path.Contains("69x69", StringComparison.Ordinal) ||
                path.Contains("40x40", StringComparison.Ordinal) ||
                path.Contains("thumbnail", StringComparison.Ordinal))
                score -= 40;
            if (path.EndsWith(".jpg", StringComparison.Ordinal) ||
                path.EndsWith(".jpeg", StringComparison.Ordinal) ||
                path.EndsWith(".webp", StringComparison.Ordinal) ||
                path.EndsWith(".png", StringComparison.Ordinal))
                score += 5;

            candidates.Add((uri.ToString(), score));
        }

        // JSON-LD "image": "..." veya "image": ["..."]
        foreach (Match m in Regex.Matches(
                     html,
                     @"""image""\s*:\s*(?:\[\s*)?""(https?://[^""]+|/[^""]+)""",
                     RegexOptions.IgnoreCase))
            Consider(m.Groups[1].Value, 60);

        // link rel="image_src"
        foreach (Match m in Regex.Matches(
                     html,
                     @"<link[^>]+rel=[""']image_src[""'][^>]+href=[""']([^""']+)[""']",
                     RegexOptions.IgnoreCase))
            Consider(m.Groups[1].Value, 55);

        // img src / data-src / data-zoom-image / srcset ilk aday
        foreach (Match m in Regex.Matches(
                     html,
                     @"<(?:img|source)[^>]+(?:src|data-src|data-original|data-zoom-image|data-lazy|data-image)=[""']([^""']+)[""']",
                     RegexOptions.IgnoreCase))
            Consider(m.Groups[1].Value, 20);

        foreach (Match m in Regex.Matches(
                     html,
                     @"srcset=[""'](https?://[^""'\s,]+|/[^\s,""']+)",
                     RegexOptions.IgnoreCase))
            Consider(m.Groups[1].Value, 15);

        // Bilinen ürün CDN URL'leri (meta boş olsa bile)
        foreach (Match m in Regex.Matches(
                     html,
                     @"https?://(?:cdn\.dsmcdn\.com|i\.dr\.com\.tr|productimages\.hepsiburada\.net|images\.hepsiburada\.net|n11scdn\.akamaized\.net|m\.media-amazon\.com|img[^/]*\.kwcdn\.com)[^""'\s<>]+",
                     RegexOptions.IgnoreCase))
            Consider(m.Value, 35);

        return candidates
            .OrderByDescending(c => c.Score)
            .Select(c => c.Url)
            .FirstOrDefault();
    }

    private static ScrapeResultDto Fail(string message, string? storeName = null)
        => new(false, null, null, null, storeName, "TRY", true, message);

    private static string? GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()
            : null;

    internal static decimal? ParsePrice(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var cleaned = raw
            .Replace("TL", "", StringComparison.OrdinalIgnoreCase)
            .Replace("TRY", "", StringComparison.OrdinalIgnoreCase)
            .Replace("₺", "", StringComparison.Ordinal)
            .Replace("\u00A0", " ")
            .Trim();

        // "1.299,90" (TR) veya "1299.90" / "1,299.90"
        var match = Regex.Match(cleaned, @"(\d{1,3}(?:[.\s]\d{3})*(?:,\d{1,2})|\d+(?:[.,]\d{1,2})?)");
        if (!match.Success) return null;

        var num = match.Groups[1].Value.Replace(" ", "");

        if (num.Contains(',') && num.Contains('.'))
        {
            // 1.299,90 → binlik nokta, ondalık virgül
            if (num.LastIndexOf(',') > num.LastIndexOf('.'))
                num = num.Replace(".", "").Replace(',', '.');
            else
                num = num.Replace(",", "");
        }
        else if (num.Contains(','))
        {
            // 1299,90 veya 1.299 (nadiren)
            var parts = num.Split(',');
            num = parts.Length == 2 && parts[1].Length <= 2
                ? num.Replace(".", "").Replace(',', '.')
                : num.Replace(",", "");
        }

        return decimal.TryParse(num, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static bool DetectInStock(string bodySample)
    {
        var text = bodySample.ToLowerInvariant();
        string[] outOfStock =
        [
            "tükendi", "stokta yok", "stokta değil", "out of stock", "sold out",
            "şu an mevcut değil", "geçici olarak temin edilemiyor", "ürün mevcut değil"
        ];
        return !outOfStock.Any(text.Contains);
    }

    /// <summary>
    /// JSON-LD Product, Open Graph ve yaygın fiyat seçicilerini tarar.
    /// </summary>
    private const string ExtractionScript = """
        () => {
          const pick = (...vals) => {
            for (const v of vals) {
              if (v && String(v).trim()) return String(v).trim();
            }
            return null;
          };

          let title = null, imageUrl = null, priceText = null, currency = null;

          // JSON-LD Product
          const scripts = [...document.querySelectorAll('script[type="application/ld+json"]')];
          for (const s of scripts) {
            try {
              let data = JSON.parse(s.textContent || 'null');
              const list = Array.isArray(data) ? data : [data];
              for (const item of list) {
                const nodes = item && item['@graph'] ? item['@graph'] : [item];
                for (const node of nodes) {
                  if (!node) continue;
                  const type = node['@type'];
                  const isProduct = type === 'Product' || (Array.isArray(type) && type.includes('Product'));
                  if (!isProduct) continue;
                  title = pick(title, node.name);
                  imageUrl = pick(imageUrl, Array.isArray(node.image) ? node.image[0] : node.image);
                  const offers = node.offers;
                  const offer = Array.isArray(offers) ? offers[0] : offers;
                  if (offer) {
                    priceText = pick(priceText, offer.price, offer.lowPrice);
                    currency = pick(currency, offer.priceCurrency);
                  }
                }
              }
            } catch (_) {}
          }

          // Open Graph / meta (boş content'i yok say)
          const meta = (sel) => {
            const v = document.querySelector(sel)?.getAttribute('content');
            return v && v.trim() ? v.trim() : null;
          };
          title = pick(title, meta('meta[property="og:title"]'), document.title);
          imageUrl = pick(imageUrl, meta('meta[property="og:image"]'), meta('meta[property="twitter:image"]'));
          priceText = pick(
            priceText,
            meta('meta[property="product:price:amount"]'),
            meta('meta[itemprop="price"]'),
            document.querySelector('[itemprop="price"]')?.getAttribute('content'),
            document.querySelector('[itemprop="price"]')?.textContent
          );
          currency = pick(currency, meta('meta[property="product:price:currency"]'), 'TRY');

          // Görsel: ürün galerisi / lazy img / genel aday skorlama
          if (!imageUrl) {
            const imgSelectors = [
              '.js-prd-first-image img', '.product-img img', '#productImage',
              '[itemprop="image"]', 'img[data-zoom-image]',
              '.gallery-container img', '.product-image img',
              'img[src*="/originals/"]', 'img[src*="dsmcdn"]', 'img[src*="/cache/"]',
              'img[src*="productimages"]', 'img[src*="hbcdn"]', 'img[src*="media-amazon"]'
            ];
            for (const sel of imgSelectors) {
              const el = document.querySelector(sel);
              if (!el) continue;
              const src = pick(
                el.getAttribute('data-zoom-image'),
                el.getAttribute('data-src'),
                el.getAttribute('data-original'),
                el.getAttribute('src')
              );
              if (src && !src.startsWith('data:')) { imageUrl = src; break; }
            }
          }
          if (!imageUrl) {
            const imgs = [...document.querySelectorAll('img')];
            let best = null, bestScore = -1;
            for (const el of imgs) {
              const src = pick(el.getAttribute('data-zoom-image'), el.getAttribute('data-src'), el.src);
              if (!src || src.startsWith('data:') || src.includes('logo') || src.includes('banner')) continue;
              let s = (el.naturalWidth || el.width || 0) + (el.naturalHeight || el.height || 0);
              if (/dsmcdn|originals|productimages|hbcdn|org_zoom|600x600/i.test(src)) s += 500;
              if (s > bestScore) { bestScore = s; best = src; }
            }
            if (best) imageUrl = best;
          }

          // Yaygın CSS seçicileri
          if (!priceText) {
            const selectors = [
              '[data-price]', '[data-testid*="price" i]', '.product-price', '.price',
              '#price', '.a-price .a-offscreen', '.price-value', '.prc-dsc',
              '.product-price-new', '.new-price', '[class*="currentPrice" i]'
            ];
            for (const sel of selectors) {
              const el = document.querySelector(sel);
              if (!el) continue;
              priceText = pick(priceText, el.getAttribute('data-price'), el.getAttribute('content'), el.textContent);
              if (priceText) break;
            }
          }

          const bodySample = (document.body?.innerText || '').slice(0, 4000);

          return JSON.stringify({ title, imageUrl, priceText, currency, bodySample });
        }
        """;

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser.Dispose();
            _browser = null;
        }

        _browserLock.Dispose();
    }
}
