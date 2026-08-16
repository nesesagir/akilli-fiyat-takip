using PriceTracker.Domain.Common;

namespace PriceTracker.Domain.Entities;

public class PriceHistory : BaseEntity
{
    public Guid TrackedItemId { get; set; }
    public TrackedItem TrackedItem { get; set; } = null!;

    public decimal Price { get; set; }
    public bool IsInStock { get; set; } = true;
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
