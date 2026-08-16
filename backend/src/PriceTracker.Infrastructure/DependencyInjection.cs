using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceTracker.Application.Interfaces;
using PriceTracker.Infrastructure.Email;
using PriceTracker.Infrastructure.Jobs;
using PriceTracker.Infrastructure.Persistence;
using PriceTracker.Infrastructure.Repositories;
using PriceTracker.Infrastructure.Scraping;

namespace PriceTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = PostgresConnection.Normalize(
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' bulunamadı."));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITrackedItemRepository, TrackedItemRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<PuppeteerPriceScraper>();
        services.AddSingleton<IPriceScraper>(sp => sp.GetRequiredService<PuppeteerPriceScraper>());

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailNotifier, MailKitEmailNotifier>();
        services.AddScoped<Admin.AdminAuthService>();

        // Otomatik fiyat: BackgroundService (Hangfire Neon/Render'da açılışı düşürüyordu).
        if (configuration.GetValue("PriceCheck:Enabled", true))
            services.AddHostedService<PriceCheckBackgroundService>();

        // İsteğe bağlı yerel Hangfire (varsayılan kapalı).
        if (configuration.GetValue("Hangfire:Enabled", false))
            services.AddHangfireJobs(configuration);

        return services;
    }
}
