using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace BSE.Host.Authentication;

/// <summary>
/// Redirects authenticated users who lack the required role to the Home page
/// instead of returning a bare 403 Forbidden response.
/// Registered as the <see cref="IAuthorizationMiddlewareResultHandler"/> in Program.cs
/// so it applies regardless of whether the bypass or SAML auth path is active.
/// </summary>
public sealed class HomeRedirectAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // Authenticated but forbidden (wrong role) → redirect to Home.
        if (authorizeResult.Forbidden && context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.Redirect("/Home");
            return;
        }

        // All other cases (unauthenticated challenge, success, etc.) → default behaviour.
        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
