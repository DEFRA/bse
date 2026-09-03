using System.Security.Claims;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace BSE.Modules.UserManagement.Identity;

/// <summary>
/// Resolves <see cref="IUserContext"/> from the current HTTP request's <see cref="ClaimsPrincipal"/>.
/// Registered as scoped so it is recreated per request.
/// </summary>
/// <remarks>
/// Email claim resolution order (matches Entra ID SAML/OIDC assertion output):
/// 1. <c>emailaddress</c> — canonical internal claim added by AcsCommandResultCreated
/// 2. <c>ClaimTypes.Email</c> — standard .NET email claim type
/// 3. <c>preferred_username</c> — Azure AD OIDC fallback
/// 4. <c>ClaimTypes.Upn</c> — legacy Windows/Kerberos UPN fallback
/// </remarks>
public sealed class ClaimsUserContext : IUserContext
{
    /// <summary>The <c>bse:group</c> claim holds the <c>luUserGroup.Name</c> display string from the database.</summary>
    public const string BseGroupClaimType   = "bse:group";

    /// <summary>
    /// The <c>bse:groupId</c> claim holds the <c>luUserGroup.ID</c> integer as a string.
    /// This is the authoritative source for <see cref="Group"/> — avoids fragile display-name parsing.
    /// </summary>
    public const string BseGroupIdClaimType = "bse:groupId";

    // Canonical internal claim for the user's email / UPN — added by AcsCommandResultCreated
    // from the Entra ID SAML assertion. Mirrors the reference ExtractEmail priority order.
    public const string EmailClaimType = "emailaddress";

    private readonly ClaimsPrincipal _principal;

    public ClaimsUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _principal = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    }

    public string Upn =>
        _principal.FindFirstValue(EmailClaimType)
        ?? _principal.FindFirstValue(ClaimTypes.Email)
        ?? _principal.FindFirstValue("preferred_username")
        ?? _principal.FindFirstValue(ClaimTypes.Upn)
        ?? string.Empty;

    public string DisplayName =>
        _principal.FindFirstValue(ClaimTypes.Name)
        ?? _principal.FindFirstValue("name")
        ?? Upn;

    /// <summary>The <c>luUserGroup.Name</c> display string (e.g. "DEFRA Viewer").</summary>
    public string GroupName =>
        _principal.FindFirstValue(BseGroupClaimType) ?? string.Empty;

    /// <summary>
    /// Resolves the <see cref="UserGroup"/> enum by parsing the integer <c>bse:groupId</c> claim
    /// emitted by <see cref="GroupClaimsTransformation"/>. Falls back to <see cref="UserGroup.None"/>
    /// if the claim is absent or the value is unrecognised.
    /// </summary>
    public UserGroup Group
    {
        get
        {
            var raw = _principal.FindFirstValue(BseGroupIdClaimType);
            return int.TryParse(raw, out var id) && Enum.IsDefined(typeof(UserGroup), id)
                ? (UserGroup)id
                : UserGroup.None;
        }
    }

    public bool IsInGroup(UserGroup group) => Group == group;
}
