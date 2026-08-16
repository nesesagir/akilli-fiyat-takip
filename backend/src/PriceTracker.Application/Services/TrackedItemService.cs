using PriceTracker.Application.DTOs;
using PriceTracker.Application.Interfaces;
using PriceTracker.Domain.Entities;

namespace PriceTracker.Application.Services;

public class TrackedItemService
{
    private readonly IUnitOfWork _unitOfWork;

    public TrackedItemService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TrackedItemDto> CreateAsync(CreateTrackedItemRequest request, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, ct)
                   ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        var item = new TrackedItem
        {
            UserId = user.Id,
            ProductUrl = request.ProductUrl.Trim(),
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Takip edilen ürün" : request.Title.Trim(),
            TargetPrice = request.TargetPrice,
            StoreName = StoreNameResolver.FromUrl(request.ProductUrl)
        };

        await _unitOfWork.TrackedItems.AddAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<IReadOnlyList<TrackedItemDto>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _unitOfWork.TrackedItems.GetByUserIdAsync(userId, ct);
        return items.Select(Map).ToList();
    }

    public async Task<TrackedItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _unitOfWork.TrackedItems.GetByIdAsync(id, ct);
        return item is null ? null : Map(item);
    }

    public async Task<IReadOnlyList<PriceHistoryPointDto>> GetPriceHistoryAsync(
        Guid itemId,
        int days = 30,
        CancellationToken ct = default)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var history = await _unitOfWork.PriceHistories.GetByItemIdAsync(itemId, from, ct);
        return history
            .Select(h => new PriceHistoryPointDto(h.Id, h.Price, h.IsInStock, h.RecordedAtUtc))
            .ToList();
    }

    public async Task<TrackedItemDto?> UpdateTargetPriceAsync(
        Guid id,
        decimal targetPrice,
        CancellationToken ct = default)
    {
        var item = await _unitOfWork.TrackedItems.GetByIdAsync(id, ct);
        if (item is null) return null;

        item.TargetPrice = targetPrice;
        item.NotificationSentForCurrentTarget = false;
        item.UpdatedAtUtc = DateTime.UtcNow;
        _unitOfWork.TrackedItems.Update(item);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _unitOfWork.TrackedItems.GetByIdAsync(id, ct);
        if (item is null) return false;

        item.IsActive = false;
        item.UpdatedAtUtc = DateTime.UtcNow;
        _unitOfWork.TrackedItems.Update(item);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _unitOfWork.TrackedItems.GetByUserIdAsync(userId, ct);
        var dtos = items.Select(Map).ToList();

        var potentialSavings = dtos
            .Where(i => i.CurrentPrice.HasValue && i.CurrentPrice > i.TargetPrice)
            .Sum(i => i.CurrentPrice!.Value - i.TargetPrice);

        var deal = dtos
            .Where(i => i.CurrentPrice.HasValue)
            .OrderByDescending(i =>
            {
                // En büyük yüzde düşüş potansiyeli (hedefe göre)
                if (!i.CurrentPrice.HasValue || i.CurrentPrice <= 0) return 0m;
                return (i.CurrentPrice.Value - i.TargetPrice) / i.CurrentPrice.Value;
            })
            .FirstOrDefault();

        return new DashboardSummaryDto(potentialSavings, deal, dtos);
    }

    private static TrackedItemDto Map(TrackedItem item)
    {
        decimal? progress = null;
        if (item.CurrentPrice is > 0)
        {
            // Hedefe ne kadar yaklaşıldı: current == target => 100%, current >> target => düşük
            var ratio = item.TargetPrice / item.CurrentPrice.Value;
            progress = Math.Clamp(ratio * 100m, 0m, 100m);
        }

        return new TrackedItemDto(
            item.Id,
            item.UserId,
            item.ProductUrl,
            item.Title,
            item.ImageUrl,
            StoreNameResolver.FromUrl(item.ProductUrl),
            item.Currency,
            item.CurrentPrice,
            item.TargetPrice,
            item.IsInStock,
            item.IsActive,
            item.LastCheckedAtUtc,
            progress);
    }
}
