using PriceTracker.Domain.Common;

namespace PriceTracker.Domain.Entities;

/// <summary>Ayrı yönetici hesabı — normal kullanıcılarla karışmaz.</summary>
public class AdminAccount : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<AdminSession> Sessions { get; set; } = new List<AdminSession>();
}

public class AdminSession : BaseEntity
{
    public Guid AdminAccountId { get; set; }
    public AdminAccount AdminAccount { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
