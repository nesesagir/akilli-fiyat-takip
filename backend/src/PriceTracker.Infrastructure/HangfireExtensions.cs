using System.Net.Http.Headers;
using System.Text;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceTracker.Infrastructure.Jobs;
using PriceTracker.Infrastructure.Persistence;

namespace PriceTracker.Infrastructure;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireJobs(this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Hangfire:Enabled", true))
            return services;

        var connectionString = PostgresConnection.Normalize(
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Hangfire için DefaultConnection gerekli."));

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1;
            options.Queues = ["default"];
            options.ServerTimeout = TimeSpan.FromMinutes(5);
            options.SchedulePollingInterval = TimeSpan.FromSeconds(30);
        });

        services.AddTransient<PriceCheckJobs>();
        return services;
    }

    public static WebApplication UseHangfireJobs(this WebApplication app, IConfiguration configuration)
    {
        if (!configuration.GetValue("Hangfire:Enabled", true))
            return app;

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAdminBasicAuthFilter(configuration)]
        });

        var cron = configuration["Hangfire:Cron"] ?? "*/30 * * * *";

        RecurringJob.AddOrUpdate<PriceCheckJobs>(
            "check-all-active-prices",
            job => job.CheckAllActiveProductsAsync(),
            cron,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        return app;
    }
}

/// <summary>Hangfire dashboard — Admin e-posta/şifre ile HTTP Basic Auth.</summary>
internal sealed class HangfireAdminBasicAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string _email;
    private readonly string _password;

    public HangfireAdminBasicAuthFilter(IConfiguration configuration)
    {
        _email = (configuration["Admin:Email"] ?? "").Trim().ToLowerInvariant();
        _password = configuration["Admin:Password"] ?? "";
    }

    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var header = http.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(header, out var auth) ||
            !string.Equals(auth.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(auth.Parameter))
        {
            Challenge(http);
            return false;
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter));
        }
        catch
        {
            Challenge(http);
            return false;
        }

        var sep = decoded.IndexOf(':');
        if (sep < 0)
        {
            Challenge(http);
            return false;
        }

        var user = decoded[..sep].Trim().ToLowerInvariant();
        var pass = decoded[(sep + 1)..];
        var ok = string.Equals(user, _email, StringComparison.Ordinal) &&
                 string.Equals(pass, _password, StringComparison.Ordinal);
        if (!ok) Challenge(http);
        return ok;
    }

    private static void Challenge(Microsoft.AspNetCore.Http.HttpContext http)
    {
        http.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire\"";
        http.Response.StatusCode = 401;
    }
}
