using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.Interfaces;
using PriceTracker.Domain.Common;
using PriceTracker.Domain.Entities;
using PriceTracker.Infrastructure.Persistence;

namespace PriceTracker.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Remove(T entity) => DbSet.Remove(entity);
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
}

public class TrackedItemRepository : Repository<TrackedItem>, ITrackedItemRepository
{
    public TrackedItemRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<TrackedItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TrackedItem>> GetActiveItemsAsync(CancellationToken cancellationToken = default)
        => await DbSet
            .Where(x => x.IsActive)
            .OrderBy(x => x.LastCheckedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<TrackedItem?> GetWithHistoryAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.PriceHistories.OrderByDescending(h => h.RecordedAtUtc).Take(100))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}

public class PriceHistoryRepository : Repository<PriceHistory>, IPriceHistoryRepository
{
    public PriceHistoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<PriceHistory>> GetByItemIdAsync(
        Guid trackedItemId,
        DateTime? fromUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(x => x.TrackedItemId == trackedItemId);
        if (fromUtc.HasValue)
            query = query.Where(x => x.RecordedAtUtc >= fromUtc.Value);

        return await query
            .OrderBy(x => x.RecordedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(
        AppDbContext context,
        IUserRepository users,
        ITrackedItemRepository trackedItems,
        IPriceHistoryRepository priceHistories)
    {
        _context = context;
        Users = users;
        TrackedItems = trackedItems;
        PriceHistories = priceHistories;
    }

    public IUserRepository Users { get; }
    public ITrackedItemRepository TrackedItems { get; }
    public IPriceHistoryRepository PriceHistories { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
