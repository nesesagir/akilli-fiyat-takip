namespace PriceTracker.Infrastructure.Persistence;

/// <summary>
/// Neon / bulut için bağlantı dizesini normalize eder.
/// Hangfire PgBouncer (pooler) ile güvenilir çalışmaz; doğrudan endpoint kullanılır.
/// </summary>
public static class PostgresConnection
{
    public static string Normalize(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        var cs = connectionString.Trim();

        // ep-xxx-pooler.region → ep-xxx.region
        cs = cs.Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);

        cs = cs.Replace("&channel_binding=require", "", StringComparison.OrdinalIgnoreCase)
            .Replace("?channel_binding=require&", "?", StringComparison.OrdinalIgnoreCase)
            .Replace("?channel_binding=require", "", StringComparison.OrdinalIgnoreCase);

        return cs;
    }
}
