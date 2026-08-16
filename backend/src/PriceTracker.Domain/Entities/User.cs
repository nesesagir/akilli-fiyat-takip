using PriceTracker.Domain.Common;

namespace PriceTracker.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string PreferredCurrency { get; set; } = "TRY";
    public string PreferredLanguage { get; set; } = "tr";
    public bool EmailNotificationsEnabled { get; set; } = true;

    public ICollection<TrackedItem> TrackedItems { get; set; } = new List<TrackedItem>();
}
