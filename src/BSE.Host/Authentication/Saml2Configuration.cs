namespace BSE.Host.Authentication;

/// <summary>
/// Strongly-typed options for SAML 2.0 / Entra ID integration.
/// Bound from the <c>Saml2</c> section in <c>appsettings.json</c>.
/// All values are required for production use; placeholders are supplied in
/// <c>appsettings.json</c> and overridden per environment.
/// </summary>
public sealed class Saml2Configuration
{
    public const string SectionName = "Saml2";

    /// <summary>
    /// The Entity ID (URI) that uniquely identifies this application as the SAML Service Provider.
    /// Must match the Identifier (Entity ID) registered in the Entra ID Enterprise Application.
    /// Example: <c>https://bse.example.gov.uk/</c>
    /// </summary>
    public string SPEntityId { get; init; } = string.Empty;

    /// <summary>
    /// The Entity ID (URI) of the Entra ID Identity Provider.
    /// Typically: <c>https://sts.windows.net/{tenant-id}/</c>
    /// </summary>
    public string IdPEntityId { get; init; } = string.Empty;

    /// <summary>
    /// The federation metadata URL published by Entra ID.
    /// Sustainsys fetches this at startup to obtain the IdP signing certificate and SSO endpoints.
    /// Example: <c>https://login.microsoftonline.com/{tenant-id}/federationmetadata/2007-06/federationmetadata.xml?appid={app-id}</c>
    /// </summary>
    public string IdPMetadataUrl { get; init; } = string.Empty;

    /// <summary>
    /// The path on this application where Entra ID posts the SAML assertion (Assertion Consumer Service).
    /// Must match the Reply URL registered in Entra ID. Default: <c>/Saml2/Acs</c>
    /// </summary>
    public string AcsPath { get; init; } = "/Saml2/Acs";

    /// <summary>
    /// The path used for Single Logout (SLO). Default: <c>/Saml2/Logout</c>
    /// </summary>
    public string SloPath { get; init; } = "/Saml2/Logout";

    /// <summary>
    /// Certificate thumbprint for the SP signing/decryption certificate.
    /// Leave empty in Development (unsigned assertions accepted).
    /// In production, reference an Azure Key Vault certificate — never commit a real thumbprint.
    /// </summary>
    public string? SPCertificateThumbprint { get; init; }

    /// <summary>
    /// Overrides the public-facing origin that Sustainsys.Saml2 uses when building the
    /// ACS (Reply) URL included in the SAML AuthnRequest sent to Entra ID.
    /// Required when the app sits behind a reverse proxy, or when the request origin
    /// (e.g. http://localhost:53643) differs from the registered Reply URL.
    /// Example: <c>https://localhost:53642</c> for local development.
    /// Leave null to let Sustainsys infer from the incoming request.
    /// </summary>
    public string? PublicOrigin { get; init; }
}
