using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs;
using PriceTracker.Application.Services;

namespace PriceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Fiyat Kontrol")]
public class ScraperController : ControllerBase
{
    private readonly PriceCheckService _priceCheckService;

    public ScraperController(PriceCheckService priceCheckService)
    {
        _priceCheckService = priceCheckService;
    }

    /// <summary>Tek bir takip ürününün fiyatını kazır ve kaydeder.</summary>
    [HttpPost("check/{trackedItemId:guid}")]
    public async Task<ActionResult<PriceCheckResultDto>> CheckOne(Guid trackedItemId, CancellationToken ct)
    {
        var result = await _priceCheckService.CheckItemAsync(trackedItemId, ct);
        if (!result.Success && result.ErrorMessage == "Ürün bulunamadı.")
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Tüm aktif takip ürünlerini tarar.</summary>
    [HttpPost("check-all")]
    public async Task<ActionResult<IReadOnlyList<PriceCheckResultDto>>> CheckAll(CancellationToken ct)
        => Ok(await _priceCheckService.CheckAllActiveAsync(ct));
}
