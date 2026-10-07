using System.Collections.Generic;
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
    /// Email/UPN used for database user lookup in local bypass mode.
    /// </summary>
    public string Email { get; set; } = "dev-user@defra.gov.uk";

    /// <summary>
    /// When true (default) the handler automatically reads the current Windows
    /// session identity (DOMAIN\username) and strips the domain prefix to derive
    /// the NT login — no need to set NtLogin in config manually.
    /// Falls back to NtLogin if the Windows identity is unavailable.
    /// </summary>
    public bool UseWindowsIdentity { get; set; } = true;

    /// <summary>
    /// When true, emits the bse:groupId / bse:group / role claims directly so
    /// GroupClaimsTransformation's DB lookup guard short-circuits and no SQL
    /// connection is required at all for local/offline development.
    /// </summary>
    public bool SkipDbRoleLookup { get; set; }

    /// <summary>
    /// UserGroup enum id (BSE.SharedKernel.UserGroup) to emit as bse:groupId when
    /// SkipDbRoleLookup is true. Defaults to 1 (Admin) for full local access.
    /// </summary>
    public int DevUserGroupId { get; set; } = 1;

    /// <summary>
    /// Display group name (matches GroupClaimsTransformation.GetPoliciesForGroup) to
    /// emit as bse:group when SkipDbRoleLookup is true.
    /// </summary>
    public string DevUserGroupName { get; set; } = "VLA Maintenance";
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
        var email = ResolveEmail(Options.Email);

        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("DevBypass: signing in as NT login '{NtLogin}' with email '{Email}'", ntLogin, email);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, email),
            new(ClaimTypes.Name,           email),
            new(ClaimTypes.Email,          email),
            new("preferred_username",      email),
        };

        if (Options.SkipDbRoleLookup)
        {
            // Pre-seed the authoritative DB-driven claims so GroupClaimsTransformation's
            // guard (HasClaim bse:groupId) short-circuits and skips its SQL lookup entirely.
            claims.Add(new Claim("bse:groupId", Options.DevUserGroupId.ToString()));
            claims.Add(new Claim("bse:group",   Options.DevUserGroupName));
            foreach (var policy in GetPoliciesForGroup(Options.DevUserGroupName))
                claims.Add(new Claim(ClaimTypes.Role, policy));
        }

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Mirrors GroupClaimsTransformation.GetPoliciesForGroup so the dev bypass grants
    /// the same policy set a real DB-resolved group would, without any DB dependency.
    /// </summary>
    private static IEnumerable<string> GetPoliciesForGroup(string? groupName) =>
        groupName switch
        {
            "DEFRA Viewer"            => ["ReadOnly", "DEFRAAccess"],
            "DEFRA Data Entry"        => ["ReadOnly", "DataEntry", "FarmCreation", "DEFRAAccess"],
            "DEFRA Maintenance"       => ["ReadOnly", "DataEntry", "DEFRAMaintenance", "PickListAccess", "FarmCreation", "DEFRAAccess"],
            "VLA Data Entry"          => ["ReadOnly", "DataEntry", "VLAAccess", "PickListAccess"],
            "VLA Maintenance"         => ["ReadOnly", "DataEntry", "DEFRAMaintenance", "VLAAccess", "VLAMaintenance", "PickListAccess", "FarmCreation"],
            "DEFRA AI Wales Scotland" => ["ReadOnly"],
            "DEFRA AHO User"          => ["ReadOnly"],
            _                         => []
        };

    /// <summary>
    /// Priority:
    ///   1. Current Windows identity (DOMAIN\user → user) when UseWindowsIdentity=true
    ///   2. NtLogin from config — explicit override / fallback
    /// </summary>
    private string ResolveNtLogin()
    {
        if (Options.UseWindowsIdentity && OperatingSystem.IsWindows())
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

    private string ResolveEmail(string ntLogin)
    {
        if (!string.IsNullOrWhiteSpace(Options.Email))
            return Options.Email;

        if (ntLogin.Contains('@'))
            return ntLogin;

        return $"{ntLogin}@defra.gov.uk";
    }
}
