using System.Text.RegularExpressions;

namespace PriceTracker.Infrastructure.Persistence;

/// <summary>
/// Neon / bulut bağlantı dizesini Npgsql anahtar=değer formatına çevirir.
/// URI içindeki ?sslmode=require gibi parçalar bazı panellerde kırpılabiliyor.
/// </summary>
public static class PostgresConnection
{
    public static string Normalize(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        var cs = connectionString.Trim().Trim('"').Trim('\'');

        // ep-xxx-pooler.region → ep-xxx.region (Hangfire / uzun oturum için)
        cs = cs.Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);

        if (cs.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            cs.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return FromUri(cs);
        }

        // Zaten Host=... formatı
        if (!cs.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) &&
            !cs.Contains("Ssl Mode", StringComparison.OrdinalIgnoreCase))
        {
            cs = cs.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=true";
        }

        return cs;
    }

    private static string FromUri(string uriText)
    {
        // Kırpılmış ?sslmode (equals yutulmuş) → düzelt
        if (Regex.IsMatch(uriText, @"[?&]sslmode(?:=)?$", RegexOptions.IgnoreCase))
            uriText = Regex.Replace(uriText, @"[?&]sslmode(?:=)?$", "", RegexOptions.IgnoreCase);
        if (!uriText.Contains("sslmode=", StringComparison.OrdinalIgnoreCase))
        {
            uriText += uriText.Contains('?') ? "&sslmode=require" : "?sslmode=require";
        }

        if (!Uri.TryCreate(uriText, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Geçersiz PostgreSQL bağlantı dizesi (URI).");

        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? "");
        var pass = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? "");
        var db = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrEmpty(db)) db = "neondb";

        // Password'daki özel karakterler için Escape yok — Npgsql keyword formda ; ayırır
        pass = pass.Replace(";", "%3B");

        return
            $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={db};" +
            $"Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
    }
}
