using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PriceTracker.Application.DTOs;
using PriceTracker.Application.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Infrastructure.Persistence;

namespace PriceTracker.Infrastructure.Admin;

public class AdminAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AdminAuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task EnsureSeedAdminAsync(CancellationToken ct = default)
    {
        var email = (_config["Admin:Email"] ?? "").Trim().ToLowerInvariant();
        var password = _config["Admin:Password"] ?? "";
        var name = _config["Admin:DisplayName"] ?? "Yönetici";
        var reset = _config.GetValue("Admin:ResetPasswordOnStartup", false);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "Admin:Email ve Admin:Password ayarlanmalı (.env / appsettings).");

        var existing = await _db.AdminAccounts
            .FirstOrDefaultAsync(a => a.Email.ToLower() == email, ct);

        if (existing is null)
        {
            _db.AdminAccounts.Add(new AdminAccount
            {
                Email = email,
                DisplayName = name,
                PasswordHash = UserService.HashPassword(password),
                IsActive = true
            });
            await _db.SaveChangesAsync(ct);
            return;
        }

        if (reset)
        {
            existing.Email = email;
            existing.DisplayName = name;
            existing.PasswordHash = UserService.HashPassword(password);
            existing.IsActive = true;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<AdminLoginResponse?> LoginAsync(AdminLoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await _db.AdminAccounts.FirstOrDefaultAsync(
            a => a.Email.ToLower() == email && a.IsActive, ct);
        if (admin is null) return null;

        if (!UserService.VerifyPassword(request.Password.Trim(), admin.PasswordHash))
            return null;

        // Legacy hash upgrade
        if (!admin.PasswordHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
        {
            admin.PasswordHash = UserService.HashPassword(request.Password.Trim());
            admin.UpdatedAtUtc = DateTime.UtcNow;
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expires = DateTime.UtcNow.AddDays(7);

        _db.AdminSessions.Add(new AdminSession
        {
            AdminAccountId = admin.Id,
            Token = token,
            ExpiresAtUtc = expires
        });
        await _db.SaveChangesAsync(ct);

        return new AdminLoginResponse(token, expires, admin.Email, admin.DisplayName);
    }

    public async Task<AdminAccount?> ValidateTokenAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var session = await _db.AdminSessions
            .Include(s => s.AdminAccount)
            .FirstOrDefaultAsync(s => s.Token == token, ct);

        if (session is null || session.ExpiresAtUtc < DateTime.UtcNow)
            return null;

        if (!session.AdminAccount.IsActive)
            return null;

        return session.AdminAccount;
    }

    public async Task LogoutAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var session = await _db.AdminSessions.FirstOrDefaultAsync(s => s.Token == token, ct);
        if (session is null) return;
        _db.AdminSessions.Remove(session);
        await _db.SaveChangesAsync(ct);
    }
}
