namespace BSE.Host.Authentication;

/// <summary>
/// Strongly-typed options for OpenID Connect / Entra ID integration via Microsoft.Identity.Web.
/// Bound from the <c>AzureAd</c> section in <c>appsettings.json</c>.
/// All values are required for production use; placeholders are supplied in
/// <c>appsettings.json</c> and overridden per environment.
/// </summary>
public sealed class AzureAdOidcConfiguration
{
    public const string SectionName = "AzureAd";

    /// <summary>
    /// The Azure AD / Entra ID cloud instance. Default: <c>https://login.microsoftonline.com/</c>
    /// </summary>
    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// The Entra ID tenant (directory) ID.
    /// Example: <c>770a2450-0227-4c62-90c7-4e38537f1102</c>
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// The Application (client) ID of the Entra ID app registration used as the OIDC relying party.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// The client secret for the Entra ID app registration.
    /// Leave empty in Development (bypass path used instead).
    /// In production, reference an Azure Key Vault secret — never commit a real secret.
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// The path on this application where Entra ID posts the OIDC authorization code response.
    /// Must match a Redirect URI registered in Entra ID. Default: <c>/signin-oidc</c>
    /// </summary>
    public string CallbackPath { get; init; } = "/signin-oidc";

    /// <summary>
    /// The path used after sign-out completes at the identity provider. Default: <c>/signout-callback-oidc</c>
    /// </summary>
    public string SignedOutCallbackPath { get; init; } = "/signout-callback-oidc";

    /// <summary>
    /// Overrides the public-facing origin that the OIDC handler uses when building the
    /// redirect URI sent to Entra ID.
    /// Required when the app sits behind a reverse proxy, or when the request origin
    /// differs from the registered Redirect URI.
    /// Example: <c>https://localhost:53642</c> for local development.
    /// Leave null to let the handler infer from the incoming request.
    /// </summary>
    public string? PublicOrigin { get; init; }

    /// <summary>
    /// When true, every authorization request sets <c>prompt=login</c> so Entra ID prompts for
    /// credentials instead of reusing its SSO session (needed for the idle timeout to force re-login).
    /// </summary>
    public bool ForceAuthn { get; init; }
}
