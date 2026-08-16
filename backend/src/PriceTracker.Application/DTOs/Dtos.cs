namespace PriceTracker.Application.DTOs;

public record CreateTrackedItemRequest(
    Guid UserId,
    string ProductUrl,
    decimal TargetPrice,
    string? Title = null);

public record UpdateTargetPriceRequest(decimal TargetPrice);

public record TrackedItemDto(
    Guid Id,
    Guid UserId,
    string ProductUrl,
    string Title,
    string? ImageUrl,
    string? StoreName,
    string Currency,
    decimal? CurrentPrice,
    decimal TargetPrice,
    bool IsInStock,
    bool IsActive,
    DateTime? LastCheckedAtUtc,
    decimal? ProgressToTargetPercent);

public record PriceHistoryPointDto(
    Guid Id,
    decimal Price,
    bool IsInStock,
    DateTime RecordedAtUtc);

public record DashboardSummaryDto(
    decimal PotentialMonthlySavings,
    TrackedItemDto? DealOfTheDay,
    IReadOnlyList<TrackedItemDto> Items);

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName);

public class LoginUserRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public record UpdateUserProfileRequest(
    string FirstName,
    string LastName,
    string Email,
    string PreferredCurrency,
    string PreferredLanguage,
    bool EmailNotificationsEnabled);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive,
    string PreferredCurrency,
    string PreferredLanguage,
    bool EmailNotificationsEnabled,
    DateTime CreatedAtUtc,
    int TrackedItemCount = 0);

public record RegisteredUserListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive,
    string PreferredCurrency,
    string PreferredLanguage,
    bool EmailNotificationsEnabled,
    DateTime CreatedAtUtc,
    int TrackedItemCount);

public class AdminLoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public record AdminLoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string Email,
    string DisplayName);
