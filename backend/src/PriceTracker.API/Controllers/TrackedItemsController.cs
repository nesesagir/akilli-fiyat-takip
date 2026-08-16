using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs;
using PriceTracker.Application.Services;
using PriceTracker.Application.Validators;

namespace PriceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Takip Ürünleri")]
public class TrackedItemsController : ControllerBase
{
    private readonly TrackedItemService _service;
    private readonly PriceCheckService _priceCheckService;
    private readonly CreateTrackedItemRequestValidator _validator = new();

    public TrackedItemsController(TrackedItemService service, PriceCheckService priceCheckService)
    {
        _service = service;
        _priceCheckService = priceCheckService;
    }

    [HttpPost]
    public async Task<ActionResult<TrackedItemDto>> Create([FromBody] CreateTrackedItemRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var item = await _service.CreateAsync(request, ct);
            // Ekler eklemez fiyat + görsel çek (istemci ikinci isteğe bağlı kalmasın)
            await _priceCheckService.CheckItemAsync(item.Id, ct);
            item = await _service.GetByIdAsync(item.Id, ct) ?? item;
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TrackedItemDto>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<TrackedItemDto>>> GetByUser(Guid userId, CancellationToken ct)
        => Ok(await _service.GetByUserAsync(userId, ct));

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<PriceHistoryPointDto>>> GetHistory(
        Guid id,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
        => Ok(await _service.GetPriceHistoryAsync(id, days, ct));

    [HttpPatch("{id:guid}/target-price")]
    public async Task<ActionResult<TrackedItemDto>> UpdateTargetPrice(
        Guid id,
        [FromBody] UpdateTargetPriceRequest request,
        CancellationToken ct)
    {
        if (request.TargetPrice <= 0)
            return BadRequest(new { message = "Hedef fiyat 0'dan büyük olmalıdır." });

        var item = await _service.UpdateTargetPriceAsync(id, request.TargetPrice, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var ok = await _service.DeactivateAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Bu ürün için anlık fiyat kazıma (scrape) çalıştırır.</summary>
    [HttpPost("{id:guid}/check")]
    public async Task<ActionResult<PriceCheckResultDto>> CheckPrice(Guid id, CancellationToken ct)
    {
        var result = await _priceCheckService.CheckItemAsync(id, ct);
        if (!result.Success && result.ErrorMessage == "Ürün bulunamadı.")
            return NotFound(result);
        return Ok(result);
    }
}
