using System.Security.Cryptography;
using System.Text;
using PriceTracker.Application.DTOs;
using PriceTracker.Application.Interfaces;
using PriceTracker.Domain.Entities;

namespace PriceTracker.Application.Services;

public class UserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _unitOfWork.Users.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new InvalidOperationException("Bu e-posta adresi zaten kayıtlı.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new InvalidOperationException("Şifre en az 8 karakter olmalı.");

        var first = request.FirstName.Trim();
        var last = request.LastName.Trim();
        var user = new User
        {
            Email = email,
            FirstName = first,
            LastName = last,
            DisplayName = $"{first} {last}".Trim(),
            PasswordHash = HashPassword(request.Password),
            PreferredCurrency = "TRY",
            PreferredLanguage = "tr",
            EmailNotificationsEnabled = true
        };

        await _unitOfWork.Users.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<UserDto?> LoginAsync(LoginUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.GetByEmailAsync(email, ct);
        if (user is null || !user.IsActive)
            return null;

        if (!VerifyPassword(request.Password, user.PasswordHash))
            return null;

        // Eski SHA256 hash'i PBKDF2'ye yükselt
        if (!user.PasswordHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
        {
            user.PasswordHash = HashPassword(request.Password);
            user.UpdatedAtUtc = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var count = (await _unitOfWork.TrackedItems.GetByUserIdAsync(user.Id, ct)).Count;
        return Map(user, count);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct);
        if (user is null) return null;
        var count = (await _unitOfWork.TrackedItems.GetByUserIdAsync(id, ct)).Count;
        return Map(user, count);
    }

    public async Task<UserDto?> UpdateProfileAsync(
        Guid id,
        UpdateUserProfileRequest request,
        CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct);
        if (user is null) return null;

        var email = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var taken = await _unitOfWork.Users.GetByEmailAsync(email, ct);
            if (taken is not null && taken.Id != id)
                throw new InvalidOperationException("Bu e-posta adresi zaten kayıtlı.");
            user.Email = email;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DisplayName = $"{user.FirstName} {user.LastName}".Trim();
        user.PreferredCurrency = NormalizeCurrency(request.PreferredCurrency);
        user.PreferredLanguage = NormalizeLanguage(request.PreferredLanguage);
        user.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        user.UpdatedAtUtc = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        var count = (await _unitOfWork.TrackedItems.GetByUserIdAsync(id, ct)).Count;
        return Map(user, count);
    }

    public async Task<IReadOnlyList<RegisteredUserListItemDto>> ListRegisteredUsersAsync(
        CancellationToken ct = default)
    {
        var users = await _unitOfWork.Users.GetAllAsync(ct);
        var result = new List<RegisteredUserListItemDto>();

        foreach (var u in users.OrderByDescending(x => x.CreatedAtUtc))
        {
            var count = (await _unitOfWork.TrackedItems.GetByUserIdAsync(u.Id, ct)).Count;
            result.Add(new RegisteredUserListItemDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.DisplayName,
                u.IsActive,
                u.PreferredCurrency,
                u.PreferredLanguage,
                u.EmailNotificationsEnabled,
                u.CreatedAtUtc,
                count));
        }

        return result;
    }

    private static UserDto Map(User user, int trackedCount = 0)
        => new(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.IsActive,
            user.PreferredCurrency,
            user.PreferredLanguage,
            user.EmailNotificationsEnabled,
            user.CreatedAtUtc,
            trackedCount);

    private static string NormalizeCurrency(string? raw)
    {
        var c = (raw ?? "TRY").Trim().ToUpperInvariant();
        return c is "TRY" or "USD" or "EUR" or "GBP" ? c : "TRY";
    }

    private static string NormalizeLanguage(string? raw)
    {
        var l = (raw ?? "tr").Trim().ToLowerInvariant();
        return l is "tr" or "en" ? l : "tr";
    }

    /// <summary>PBKDF2 hash (yeni). Eski SHA256 hex hâlâ VerifyPassword ile çalışır.</summary>
    public static string HashPassword(string password)
    {
        const int iterations = 100_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);
        return $"PBKDF2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        if (storedHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 4) return false;
            if (!int.TryParse(parts[1], out var iterations)) return false;
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        // Eski SHA256 (admin/user legacy)
        var legacy = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        return string.Equals(legacy, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
