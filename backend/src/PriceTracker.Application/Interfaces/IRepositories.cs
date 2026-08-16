using PriceTracker.Domain.Common;

namespace PriceTracker.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUserRepository : IRepository<Domain.Entities.User>
{
    Task<Domain.Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface ITrackedItemRepository : IRepository<Domain.Entities.TrackedItem>
{
    Task<IReadOnlyList<Domain.Entities.TrackedItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.TrackedItem>> GetActiveItemsAsync(CancellationToken cancellationToken = default);
    Task<Domain.Entities.TrackedItem?> GetWithHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPriceHistoryRepository : IRepository<Domain.Entities.PriceHistory>
{
    Task<IReadOnlyList<Domain.Entities.PriceHistory>> GetByItemIdAsync(
        Guid trackedItemId,
        DateTime? fromUtc = null,
        CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ITrackedItemRepository TrackedItems { get; }
    IPriceHistoryRepository PriceHistories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
