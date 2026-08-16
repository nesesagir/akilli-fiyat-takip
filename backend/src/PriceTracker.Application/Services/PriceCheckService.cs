using Microsoft.Extensions.Logging;
using PriceTracker.Application.DTOs;
using PriceTracker.Application.Interfaces;
using PriceTracker.Domain.Entities;

namespace PriceTracker.Application.Services;

public class PriceCheckService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceScraper _scraper;
    private readonly IEmailNotifier _emailNotifier;
    private readonly ILogger<PriceCheckService> _logger;

    public PriceCheckService(
        IUnitOfWork unitOfWork,
        IPriceScraper scraper,
        IEmailNotifier emailNotifier,
        ILogger<PriceCheckService> logger)
    {
        _unitOfWork = unitOfWork;
        _scraper = scraper;
        _emailNotifier = emailNotifier;
        _logger = logger;
    }

    public async Task<PriceCheckResultDto> CheckItemAsync(Guid trackedItemId, CancellationToken ct = default)
    {
        var item = await _unitOfWork.TrackedItems.GetByIdAsync(trackedItemId, ct);
        if (item is null)
            return new PriceCheckResultDto(trackedItemId, false, null, false, false, null, "Ürün bulunamadı.");

        if (!item.IsActive)
            return new PriceCheckResultDto(trackedItemId, false, null, false, false, item.Title, "Takip pasif.");

        _logger.LogInformation("Fiyat kontrolü başlıyor. ItemId={ItemId} Url={Url}", item.Id, item.ProductUrl);

        ScrapeResultDto scrape;
        try
        {
            scrape = await _scraper.ScrapeAsync(item.ProductUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scraper beklenmeyen hata. ItemId={ItemId}", item.Id);
            item.LastCheckedAtUtc = DateTime.UtcNow;
            item.LastScrapeError = Truncate(ex.Message, 2000);
            item.UpdatedAtUtc = DateTime.UtcNow;
            _unitOfWork.TrackedItems.Update(item);
            await _unitOfWork.SaveChangesAsync(ct);

            return new PriceCheckResultDto(item.Id, false, null, false, item.IsInStock, item.Title, ex.Message);
        }

        item.LastCheckedAtUtc = DateTime.UtcNow;
        item.UpdatedAtUtc = DateTime.UtcNow;

        if (!scrape.Success || scrape.Price is null)
        {
            item.LastScrapeError = Truncate(scrape.ErrorMessage ?? "Fiyat okunamadı.", 2000);
            ApplyScrapeMedia(item, scrape);
            item.StoreName = StoreNameResolver.FromUrl(item.ProductUrl);
            _unitOfWork.TrackedItems.Update(item);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Scrape başarısız. ItemId={ItemId} Error={Error} HasImage={HasImage}",
                item.Id,
                scrape.ErrorMessage,
                !string.IsNullOrWhiteSpace(item.ImageUrl));

            return new PriceCheckResultDto(
                item.Id,
                false,
                null,
                false,
                scrape.IsInStock,
                scrape.Title ?? item.Title,
                scrape.ErrorMessage);
        }

        item.CurrentPrice = scrape.Price;
        item.IsInStock = scrape.IsInStock;
        item.LastScrapeError = null;
        item.Currency = string.IsNullOrWhiteSpace(scrape.Currency) ? item.Currency : scrape.Currency;

        ApplyScrapeMedia(item, scrape);

        // Mağaza adı her zaman URL'den (ürün başlığıyla karışmasın)
        item.StoreName = StoreNameResolver.FromUrl(item.ProductUrl);

        await _unitOfWork.PriceHistories.AddAsync(new PriceHistory
        {
            TrackedItemId = item.Id,
            Price = scrape.Price.Value,
            IsInStock = scrape.IsInStock,
            RecordedAtUtc = DateTime.UtcNow
        }, ct);

        var targetReached = scrape.IsInStock && scrape.Price.Value <= item.TargetPrice;
        if (targetReached && !item.NotificationSentForCurrentTarget)
        {
            _logger.LogInformation(
                "HEDEF FİYAT ULAŞILDI! ItemId={ItemId} Price={Price} Target={Target}",
                item.Id,
                scrape.Price,
                item.TargetPrice);

            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(item.UserId, ct);
                if (user is not null &&
                    user.EmailNotificationsEnabled &&
                    !string.IsNullOrWhiteSpace(user.Email))
                {
                    var sent = await _emailNotifier.SendDealAlertAsync(
                        user.Email,
                        item.Title,
                        item.ProductUrl,
                        scrape.Price.Value,
                        item.TargetPrice,
                        item.Currency,
                        ct);

                    // Sadece gerçekten mail gittiyse işaretle; yoksa sonra tekrar dener
                    if (sent)
                        item.NotificationSentForCurrentTarget = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fırsat e-postası gönderilemedi. ItemId={ItemId}", item.Id);
            }
        }

        _unitOfWork.TrackedItems.Update(item);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Fiyat güncellendi. ItemId={ItemId} Price={Price} InStock={InStock}",
            item.Id,
            scrape.Price,
            scrape.IsInStock);

        return new PriceCheckResultDto(
            item.Id,
            true,
            scrape.Price,
            targetReached,
            scrape.IsInStock,
            item.Title,
            null);
    }

    public async Task<IReadOnlyList<PriceCheckResultDto>> CheckAllActiveAsync(CancellationToken ct = default)
    {
        var items = await _unitOfWork.TrackedItems.GetActiveItemsAsync(ct);
        var results = new List<PriceCheckResultDto>();

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                results.Add(await CheckItemAsync(item.Id, ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu kontrolde ürün atlandı. ItemId={ItemId}", item.Id);
                results.Add(new PriceCheckResultDto(item.Id, false, null, false, false, item.Title, ex.Message));
            }
        }

        return results;
    }

    private static void ApplyScrapeMedia(TrackedItem item, ScrapeResultDto scrape)
    {
        // Görsel her zaman güncellenir (eklenen her ürün için)
        if (!string.IsNullOrWhiteSpace(scrape.ImageUrl))
            item.ImageUrl = Truncate(scrape.ImageUrl!, 2048);

        if (string.IsNullOrWhiteSpace(scrape.Title)) return;

        var isPlaceholder =
            string.IsNullOrWhiteSpace(item.Title) ||
            item.Title == "Takip edilen ürün" ||
            item.Title.Length < 4;

        if (isPlaceholder || scrape.Title.Length > item.Title.Length + 8)
            item.Title = Truncate(scrape.Title!, 500);
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
