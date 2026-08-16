using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriceTracker.Application.Services;

namespace PriceTracker.Infrastructure.Jobs;

/// <summary>
/// Neon + Render'da Hangfire/PostgreSQL storage açılışta süreci düşürüyordu.
/// Otomatik fiyat kontrolü bu hosted service ile yapılır (Hangfire gerekmez).
/// </summary>
public sealed class PriceCheckBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PriceCheckBackgroundService> _logger;

    public PriceCheckBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PriceCheckBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = _configuration.GetValue("PriceCheck:IntervalMinutes", 30);
        if (minutes < 5) minutes = 5;

        _logger.LogInformation(
            "Otomatik fiyat kontrolü açık. Aralık={Minutes} dakika",
            minutes);

        // İlk tarama: kısa gecikme (migration / warm-up)
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));

        await RunOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Aktif ürün taraması başladı.");

            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<PriceCheckService>();
            var results = await service.CheckAllActiveAsync(ct);

            _logger.LogInformation(
                "Tarama bitti. Total={Total} Ok={Ok} Deals={Deals}",
                results.Count,
                results.Count(r => r.Success),
                results.Count(r => r.TargetReached));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            // Host düşmesin; bir sonraki turda tekrar dener.
            _logger.LogError(ex, "Fiyat taraması başarısız.");
        }
    }
}
