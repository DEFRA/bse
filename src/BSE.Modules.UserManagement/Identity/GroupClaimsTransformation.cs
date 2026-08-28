using System.Security.Claims;
using BSE.Modules.UserManagement.Repositories;
using Microsoft.AspNetCore.Authentication;

namespace BSE.Modules.UserManagement.Identity;

/// <summary>
/// Runs on every authenticated request. Resolves the user's group name from the database
/// and adds claims to the principal:
/// <list type="bullet">
///   <item><c>bse:group</c> — the <c>luUserGroup.Name</c> display string from the database.</item>
///   <item><see cref="ClaimTypes.Role"/> — one claim per policy name the group satisfies, enabling ASP.NET Core policy checks.</item>
/// </list>
/// </summary>
/// <remarks>
/// Lookup strategy (email-first with NTLogin fallback):
/// <list type="number">
///   <item>Read email from <c>emailaddress</c> claim (added by AcsCommandResultCreated from Entra SAML assertion),
///         falling back to <c>ClaimTypes.Email</c>, <c>preferred_username</c>, then <c>ClaimTypes.Upn</c>.</item>
///   <item>Call <see cref="IUserRepository.GetByUpnAsync"/>. Returns a match once the UPN column
///         is populated (after <c>AddUserUpnColumn.sql</c> + SP update).</item>
///   <item>If not found, derive NTLogin from the email local part (before '@') and call
///         <see cref="IUserRepository.GetByNtLoginAsync"/>. This is the transition-period heuristic
///         that works when database NTLogin equals the email local part.</item>
/// </list>
/// </remarks>
public sealed class GroupClaimsTransformation : IClaimsTransformation
{
    private readonly IUserRepository _userRepository;

    public GroupClaimsTransformation(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only transform authenticated principals.
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        // Avoid double-transformation: guard on bse:groupId — a claim we exclusively own
        // and emit. Unlike bse:group (a display string), bse:groupId will never appear in
        // an upstream Azure AD token, so this guard cannot be spoofed.
        if (principal.HasClaim(c => c.Type == ClaimsUserContext.BseGroupIdClaimType))
            return principal;

        var upn = principal.FindFirstValue(ClaimsUserContext.EmailClaimType)
                  ?? principal.FindFirstValue(ClaimTypes.Email)
                  ?? principal.FindFirstValue("preferred_username")
                  ?? principal.FindFirstValue(ClaimTypes.Upn);

        if (string.IsNullOrWhiteSpace(upn))
            return principal;

        var user = await _userRepository.GetByUpnAsync(upn)
                   ?? await _userRepository.GetByNtLoginAsync(DeriveNtLoginFromUpn(upn));

        if (user is null)
            return principal;

        var clone = principal.Clone();
        var identity = (ClaimsIdentity)clone.Identity!;

        // ── Purge upstream claims before emitting the authoritative DB set ────────────
        // The authentication provider (Azure AD / OIDC) may emit ClaimTypes.Role claims
        // (Azure AD app roles) or bse:* claims whose values coincide with our policy names.
        // Leaving them in place would let a misconfigured or forged upstream token bypass
        // the DB-driven access model. Stripping them here makes the database the sole
        // authority for authorisation.
        foreach (var c in identity.FindAll(ClaimTypes.Role).ToList())
            identity.RemoveClaim(c);
        foreach (var c in identity.FindAll(ClaimsUserContext.BseGroupClaimType).ToList())
            identity.RemoveClaim(c);
        foreach (var c in identity.FindAll(ClaimsUserContext.BseGroupIdClaimType).ToList())
            identity.RemoveClaim(c);
        // ─────────────────────────────────────────────────────────────────────────────

        // Replace the Name claim with the display name from the [User] table.
        // DevelopmentAuthHandler (and OIDC) may emit NTLogin or UPN as ClaimTypes.Name;
        // overwrite it so ClaimsUserContext.DisplayName shows the friendly name from the DB.
        var existingName = identity.FindFirst(ClaimTypes.Name);
        if (existingName is not null)
            identity.RemoveClaim(existingName);
        identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));

        // Emit the luUserGroup display name as the bse:group claim (used for display / audit).
        identity.AddClaim(new Claim(ClaimsUserContext.BseGroupClaimType, user.GroupName ?? string.Empty));

        // Emit the luUserGroup integer ID as bse:groupId — the authoritative claim for UserGroup
        // enum resolution in ClaimsUserContext.Group. Using the integer avoids fragile display-name
        // parsing (e.g. "DEFRA Viewer" cannot be Enum.TryParsed into UserGroup).
        identity.AddClaim(new Claim(ClaimsUserContext.BseGroupIdClaimType, user.UserGroupId.ToString()));

        // Emit the policy names this DB group satisfies as role claims.
        // To change a user's access: update [User].UserGroup in the database — no code changes needed.
        foreach (var policy in GetPoliciesForGroup(user.GroupName))
            identity.AddClaim(new Claim(ClaimTypes.Role, policy));

        return clone;
    }

    /// <summary>
    /// Maps a <c>luUserGroup.Name</c> value to the set of policy names it satisfies.
    /// Each policy name matches exactly one <c>AddPolicy</c> entry in Program.cs.
    /// Source of truth: docs/Legacy-Page-Level-Access-Control.md (Section 5 access matrix).
    /// </summary>
    private static IEnumerable<string> GetPoliciesForGroup(string? groupName) =>
        groupName switch
        {
            "DEFRA Viewer"              => ["ReadOnly", "DEFRAAccess"],
            "DEFRA Data Entry"          => ["ReadOnly", "DataEntry", "FarmCreation", "DEFRAAccess"],
            "DEFRA Maintenance"         => ["ReadOnly", "DataEntry", "DEFRAMaintenance", "PickListAccess", "FarmCreation", "DEFRAAccess"],
            "VLA Data Entry"            => ["ReadOnly", "DataEntry", "VLAAccess", "PickListAccess"],
            "VLA Maintenance"           => ["ReadOnly", "DataEntry", "DEFRAMaintenance", "VLAAccess", "VLAMaintenance", "PickListAccess", "FarmCreation"],
            "DEFRA AI Wales Scotland"   => ["ReadOnly"],
            "DEFRA AHO User"            => ["ReadOnly"],
            _                           => []
        };

    /// <summary>
    /// During the OIDC transition period, attempts to map a UPN to an NT login by
    /// extracting the local part before '@'.
    /// Example: "john.smith@defra.gov.uk" → "john.smith"
    /// </summary>
    private static string DeriveNtLoginFromUpn(string upn)
    {
        var at = upn.IndexOf('@');
        return at > 0 ? upn[..at] : upn;
    }
}
