using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BSE.Modules.UserManagement.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace BSE.Host.Authentication;

/// <summary>
/// Tracks the newest sign-in per user so an earlier session stops working once the same user signs in again.
/// </summary>
public interface ISignInSessionRegistry
{
    /// <summary>Records a new sign-in for the user and returns its session id.</summary>
    Task<string> StartAsync(string userKey, CancellationToken cancellationToken = default);

    /// <summary>True when <paramref name="sessionId"/> is the user's latest sign-in.</summary>
    Task<bool> IsCurrentAsync(string userKey, string sessionId, CancellationToken cancellationToken = default);
}

public sealed class SignInSessionRegistry(IDistributedCache cache) : ISignInSessionRegistry
{
    private static readonly DistributedCacheEntryOptions EntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(12)
    };

    public async Task<string> StartAsync(string userKey, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        await cache.SetStringAsync(CacheKey(userKey), sessionId, EntryOptions, cancellationToken);
        return sessionId;
    }

    public async Task<bool> IsCurrentAsync(string userKey, string sessionId, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(userKey);
        var current = await cache.GetStringAsync(key, cancellationToken);

        // A missing entry (cache eviction or restart) must not sign every user out; the caller's session becomes current.
        if (current is null)
        {
            await cache.SetStringAsync(key, sessionId, EntryOptions, cancellationToken);
            return true;
        }

        return string.Equals(current, sessionId, StringComparison.Ordinal);
    }

    private static string CacheKey(string userKey) =>
        "signin:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userKey.Trim().ToLowerInvariant())));
}

/// <summary>Cookie events that enforce one active session per user.</summary>
public static class SingleSessionCookieEvents
{
    public const string SessionIdClaimType = "bse:sessionId";

    public static async Task OnSigningIn(CookieSigningInContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity
            || identity.HasClaim(c => c.Type == SessionIdClaimType))
            return;

        var userKey = GetUserKey(context.Principal);
        if (userKey is null)
            return;

        var registry = context.HttpContext.RequestServices.GetRequiredService<ISignInSessionRegistry>();
        var sessionId = await registry.StartAsync(userKey, context.HttpContext.RequestAborted);
        identity.AddClaim(new Claim(SessionIdClaimType, sessionId));
    }

    public static async Task OnValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var sessionId = principal?.FindFirstValue(SessionIdClaimType);
        var userKey = principal is null ? null : GetUserKey(principal);

        // Cookies issued without a session id (before this control, or with no user key) are not trusted.
        var isCurrent = sessionId is not null && userKey is not null
            && await context.HttpContext.RequestServices.GetRequiredService<ISignInSessionRegistry>()
                .IsCurrentAsync(userKey, sessionId, context.HttpContext.RequestAborted);

        if (!isCurrent)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    }

    private static string? GetUserKey(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimsUserContext.EmailClaimType)
        ?? principal.FindFirstValue(ClaimTypes.Email)
        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
}
