using PriceTracker.Domain.Common;

namespace PriceTracker.Domain.Entities;

public class TrackedItem : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Varyant odaklı ürün URL'i (ör. beden/renk spesifik link).</summary>
    public string ProductUrl { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? StoreName { get; set; }
    public string Currency { get; set; } = "TRY";

    public decimal? CurrentPrice { get; set; }
    public decimal TargetPrice { get; set; }
    public bool IsInStock { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool NotificationSentForCurrentTarget { get; set; }

    public DateTime? LastCheckedAtUtc { get; set; }
    public string? LastScrapeError { get; set; }

    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}
