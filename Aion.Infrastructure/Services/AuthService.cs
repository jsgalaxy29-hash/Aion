using System.Security.Claims;
using Aion.DataEngine.Entities;
using Aion.Domain.Contracts;
using Aion.Security;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aion.Infrastructure.Services;

/// <summary>
/// Cookie-based authentication service that validates credentials against the security database.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IDbContextFactory<SecurityDbContext> _dbFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IDbContextFactory<SecurityDbContext> dbFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _dbFactory = dbFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string username, string password, int tenantId, bool rememberMe = false)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _logger.LogWarning("HttpContext unavailable during login for {Username}", username);
            return false;
        }

        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning("HTTP response already started before login for {Username}", username);
            return false;
        }

        try
        {
            var normalizedUserName = username.Trim().ToUpperInvariant();

            await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);

            var user = await db.SUser
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.TenantId == tenantId &&
                    !u.Deleted &&
                    u.IsActive &&
                    u.NormalizedUserName == normalizedUserName)
                .ConfigureAwait(false);

            if (user is null)
            {
                _logger.LogInformation("Authentication failed for {Username}: user not found", username);
                return false;
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Account {Username} locked until {LockoutEnd}", username, user.LockoutEnd);
                return false;
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogInformation("Authentication failed for {Username}: invalid password", username);
                await IncrementFailedLoginAsync(user.Id).ConfigureAwait(false);
                return false;
            }

            var claims = BuildClaims(user);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties).ConfigureAwait(false);

            await ResetLockoutAsync(user.Id).ConfigureAwait(false);

            _logger.LogInformation("User {Username} authenticated successfully", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Username}", username);
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning("Cannot logout because response has already started");
            return;
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
    }

    public async Task<SUser?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var subjectClaim = httpContext.User.FindFirst("sub") ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (subjectClaim is null || !int.TryParse(subjectClaim.Value, out var userId))
        {
            return null;
        }

        await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await db.SUser
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.Deleted)
            .ConfigureAwait(false);
    }

    private async Task IncrementFailedLoginAsync(int userId)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
            var now = DateTime.UtcNow;
            var lockoutEnd = now.AddMinutes(30);

            FormattableString sql = $@"
UPDATE [SUser]
SET [AccessFailedCount] = [AccessFailedCount] + 1,
    [LockoutEnd] = CASE WHEN [AccessFailedCount] + 1 >= 4 THEN {lockoutEnd} ELSE [LockoutEnd] END,
    [DtModification] = {now}
WHERE [Id] = {userId};";

            await db.Database.ExecuteSqlInterpolatedAsync(sql).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment lockout counter for user {UserId}", userId);
        }
    }

    private async Task ResetLockoutAsync(int userId)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync().ConfigureAwait(false);
            var entity = await db.SUser.FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);
            if (entity is null)
            {
                return;
            }

            entity.AccessFailedCount = 0;
            entity.LockoutEnd = null;
            entity.LastLoginDate = DateTime.UtcNow;
            entity.DtModification = DateTime.UtcNow;
            entity.UsrModificationId = userId;

            await db.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset lockout information for user {UserId}", userId);
        }
    }

    private static bool VerifyPassword(string password, string? hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (SaltParseException)
        {
            return false;
        }
    }

    private static IEnumerable<Claim> BuildClaims(SUser user)
    {
        yield return new Claim("sub", user.Id.ToString());
        yield return new Claim("tenant", user.TenantId.ToString());
        yield return new Claim(ClaimTypes.NameIdentifier, user.Id.ToString());
        yield return new Claim(ClaimTypes.Name, user.UserName);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            yield return new Claim(ClaimTypes.Email, user.Email);
        }

        var fullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
        yield return new Claim("fullname", fullName ?? string.Empty);
    }
}
