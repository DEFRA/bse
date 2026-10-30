using System.Security.Claims;

namespace BSE.Host.Authentication;

/// <summary>
/// Pure helper functions extracted from the OIDC authentication wiring in <c>Program.cs</c>
/// so they can be unit tested directly without booting a full <see cref="WebApplicationFactory{TEntryPoint}"/> host.
/// </summary>
public static class OidcHelpers
{
    /// <summary>
    /// Resolves the user's email from an Entra ID OIDC token's claims, trying the standard
    /// claim types in priority order: <see cref="ClaimTypes.Email"/>, then <c>preferred_username</c>,
    /// then <see cref="ClaimTypes.Upn"/>.
    /// </summary>
    /// <returns>The resolved email, or <see langword="null"/> when none of the claims are present.</returns>
    public static string? ResolveEmailClaim(ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return identity.FindFirst(ClaimTypes.Email)?.Value
            ?? identity.FindFirst("preferred_username")?.Value
            ?? identity.FindFirst(ClaimTypes.Upn)?.Value;
    }

    /// <summary>
    /// Rewrites the scheme/host/port of <paramref name="redirectUri"/> to match <paramref name="publicOrigin"/>
    /// when they differ, so the redirect_uri sent to Entra ID never leaks the raw App Service hostname.
    /// </summary>
    /// <param name="redirectUri">The absolute redirect URI built by the OIDC handler.</param>
    /// <param name="publicOrigin">
    /// The configured public origin, or <see langword="null"/> when no override is configured
    /// (in which case <paramref name="redirectUri"/> is returned unchanged).
    /// </param>
    public static string RewriteRedirectUriHost(string redirectUri, Uri? publicOrigin)
    {
        ArgumentNullException.ThrowIfNull(redirectUri);

        if (publicOrigin is null
            || !Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedRedirectUri)
            || string.Equals(parsedRedirectUri.Host, publicOrigin.Host, StringComparison.OrdinalIgnoreCase))
        {
            return redirectUri;
        }

        return new UriBuilder(parsedRedirectUri)
        {
            Scheme = publicOrigin.Scheme,
            Host   = publicOrigin.Host,
            Port   = publicOrigin.IsDefaultPort ? -1 : publicOrigin.Port
        }.Uri.ToString();
    }
}
