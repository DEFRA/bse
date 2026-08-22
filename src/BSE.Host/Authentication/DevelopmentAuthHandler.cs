using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BSE.Host.Authentication;

/// <summary>
/// Options for the development auth bypass handler.
/// Configure under the Authentication section in appsettings.Development.json.
/// </summary>
public sealed class DevelopmentAuthOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// NT login of the local dev user (e.g. "DS000104").
    /// Used only when <see cref="UseWindowsIdentity"/> is false, or as a fallback.
    /// Must exist as an active row in the [User] table with a valid UserGroup value.
    /// </summary>
    public string NtLogin { get; set; } = "dev-user";

    /// <summary>
    /// When true (default) the handler automatically reads the current Windows
    /// session identity (DOMAIN\username) and strips the domain prefix to derive
    /// the NT login — no need to set NtLogin in config manually.
    /// Falls back to NtLogin if the Windows identity is unavailable.
    /// </summary>
    public bool UseWindowsIdentity { get; set; } = true;
}

/// <summary>
/// Development-only authentication handler. Bypasses Entra ID / SAML.
/// When UseWindowsIdentity=true reads WindowsIdentity.GetCurrent() (e.g. DEFRA\DS000104 → DS000104)
/// so the correct [User] row is found automatically without any config change per developer.
/// Does NOT hardcode roles — emits preferred_username so GroupClaimsTransformation
/// resolves the role from the [User] table, identical to the production SAML flow.
/// </summary>
public sealed class DevelopmentAuthHandler : AuthenticationHandler<DevelopmentAuthOptions>
{
    public const string SchemeName = "DevBypass";

    public DevelopmentAuthHandler(
        IOptionsMonitor<DevelopmentAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var ntLogin = ResolveNtLogin();

        Logger.LogDebug("DevBypass: signing in as NT login '{NtLogin}'", ntLogin);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ntLogin),
            new Claim(ClaimTypes.Name,           ntLogin),
            new Claim("preferred_username",      ntLogin + "@dev.local"),
        };

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Priority:
    ///   1. Current Windows identity (DOMAIN\user → user) when UseWindowsIdentity=true
    ///   2. NtLogin from config — explicit override / fallback
    /// </summary>
    private string ResolveNtLogin()
    {
        if (Options.UseWindowsIdentity)
        {
            try
            {
                var windowsName = WindowsIdentity.GetCurrent().Name; // "DEFRA\DS000104"
                if (!string.IsNullOrWhiteSpace(windowsName))
                {
                    var slash = windowsName.LastIndexOf('\\');
                    return slash >= 0
                        ? windowsName[(slash + 1)..]   // → "DS000104"
                        : windowsName;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "DevBypass: could not read Windows identity; " +
                    "falling back to NtLogin config value '{NtLogin}'",
                    Options.NtLogin);
            }
        }

        return Options.NtLogin;
    }
}
