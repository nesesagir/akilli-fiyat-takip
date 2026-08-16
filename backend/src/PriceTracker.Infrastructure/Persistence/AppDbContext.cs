using Microsoft.EntityFrameworkCore;
using PriceTracker.Domain.Entities;

namespace PriceTracker.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<TrackedItem> TrackedItems => Set<TrackedItem>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PreferredCurrency).HasMaxLength(8).HasDefaultValue("TRY");
            entity.Property(x => x.PreferredLanguage).HasMaxLength(8).HasDefaultValue("tr");
            entity.Property(x => x.EmailNotificationsEnabled).HasDefaultValue(true);

            entity.HasMany(x => x.TrackedItems)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrackedItem>(entity =>
        {
            entity.ToTable("tracked_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductUrl).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(2048);
            entity.Property(x => x.StoreName).HasMaxLength(200);
            entity.Property(x => x.Currency).HasMaxLength(8).HasDefaultValue("TRY");
            entity.Property(x => x.CurrentPrice).HasPrecision(18, 2);
            entity.Property(x => x.TargetPrice).HasPrecision(18, 2);
            entity.Property(x => x.LastScrapeError).HasMaxLength(2000);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.IsActive, x.LastCheckedAtUtc });

            entity.HasMany(x => x.PriceHistories)
                .WithOne(x => x.TrackedItem)
                .HasForeignKey(x => x.TrackedItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.ToTable("price_histories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.TrackedItemId, x.RecordedAtUtc });
        });

        modelBuilder.Entity<AdminAccount>(entity =>
        {
            entity.ToTable("admin_accounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(128).IsRequired();

            entity.HasMany(x => x.Sessions)
                .WithOne(x => x.AdminAccount)
                .HasForeignKey(x => x.AdminAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdminSession>(entity =>
        {
            entity.ToTable("admin_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Token).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasIndex(x => x.ExpiresAtUtc);
        });
    }
}
