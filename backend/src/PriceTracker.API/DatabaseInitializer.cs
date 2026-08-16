using Microsoft.EntityFrameworkCore;
using PriceTracker.Infrastructure.Admin;
using PriceTracker.Infrastructure.Persistence;

namespace PriceTracker.API;

public static class DatabaseInitializer
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        // Eski IsAdmin sütunu kaldırılamadan önce migration ile düşer;
        // FirstName/LastName boş olanları DisplayName'den doldur
        var users = await db.Users.ToListAsync();
        var changed = false;
        foreach (var u in users)
        {
            if (string.IsNullOrWhiteSpace(u.FirstName) && !string.IsNullOrWhiteSpace(u.DisplayName))
            {
                var parts = u.DisplayName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                u.FirstName = parts.ElementAtOrDefault(0) ?? u.DisplayName;
                u.LastName = parts.ElementAtOrDefault(1) ?? "-";
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(u.LastName))
            {
                u.LastName = "-";
                changed = true;
            }
        }
        if (changed)
            await db.SaveChangesAsync();

        var adminAuth = scope.ServiceProvider.GetRequiredService<AdminAuthService>();
        await adminAuth.EnsureSeedAdminAsync();
    }
}
