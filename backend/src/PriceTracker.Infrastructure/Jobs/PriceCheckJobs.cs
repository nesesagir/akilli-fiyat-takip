using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PriceTracker.Application.Services;

namespace PriceTracker.Infrastructure.Jobs;

/// <summary>Hangfire tarafından çağrılan arka plan işleri.</summary>
public class PriceCheckJobs
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PriceCheckJobs> _logger;

    public PriceCheckJobs(IServiceScopeFactory scopeFactory, ILogger<PriceCheckJobs> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task CheckAllActiveProductsAsync()
    {
        _logger.LogInformation("Hangfire: aktif ürün taraması başladı.");

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PriceCheckService>();
        var results = await service.CheckAllActiveAsync();

        var ok = results.Count(r => r.Success);
        var deals = results.Count(r => r.TargetReached);

        _logger.LogInformation(
            "Hangfire: tarama bitti. Total={Total} Ok={Ok} Deals={Deals}",
            results.Count,
            ok,
            deals);
    }
}
