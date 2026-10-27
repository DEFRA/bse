using Microsoft.AspNetCore.Authentication.Cookies;

namespace BSE.Host.Authentication;

public static class AuthCookieConfiguration
{
    public static void Apply(
        CookieAuthenticationOptions options, string cookieName, TimeSpan idleTimeout, CookieSecurePolicy securePolicy)
    {
        options.ExpireTimeSpan = idleTimeout;
        options.SlidingExpiration = true;
        options.Cookie.Name = cookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = securePolicy;
        options.Events.OnSigningIn = SingleSessionCookieEvents.OnSigningIn;
        options.Events.OnValidatePrincipal = SingleSessionCookieEvents.OnValidatePrincipal;
    }
}
