using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs;
using PriceTracker.Application.Services;

namespace PriceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Panel")]
public class DashboardController : ControllerBase
{
    private readonly TrackedItemService _service;

    public DashboardController(TrackedItemService service)
    {
        _service = service;
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<DashboardSummaryDto>> Get(Guid userId, CancellationToken ct)
        => Ok(await _service.GetDashboardAsync(userId, ct));
}
