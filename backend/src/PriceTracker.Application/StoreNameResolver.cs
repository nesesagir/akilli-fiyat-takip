namespace PriceTracker.Application;

public static class StoreNameResolver
{
    public static string FromUrl(string productUrl)
    {
        if (!Uri.TryCreate(productUrl, UriKind.Absolute, out var uri))
            return "Mağaza";

        var host = uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();

        if (host.Contains("trendyol")) return "Trendyol";
        if (host.Contains("hepsiburada")) return "Hepsiburada";
        if (host.Contains("amazon")) return "Amazon";
        if (host.Contains("n11.")) return "N11";
        if (host.Contains("temu")) return "Temu";
        if (host.Contains("teknosa")) return "Teknosa";
        if (host.Contains("mediamarkt")) return "MediaMarkt";
        if (host.Contains("boyner")) return "Boyner";
        if (host.Contains("lcwaikiki") || host.Contains("lcw.")) return "LC Waikiki";
        if (host.Contains("dr.com.tr")) return "D&R";

        return host;
    }
}
