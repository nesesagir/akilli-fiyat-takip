namespace PriceTracker.Application.DTOs;

public record ScrapeResultDto(
    bool Success,
    decimal? Price,
    string? Title,
    string? ImageUrl,
    string? StoreName,
    string Currency,
    bool IsInStock,
    string? ErrorMessage);

public record PriceCheckResultDto(
    Guid TrackedItemId,
    bool Success,
    decimal? Price,
    bool TargetReached,
    bool IsInStock,
    string? Title,
    string? ErrorMessage);
