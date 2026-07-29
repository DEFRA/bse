using System.Security.Claims;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace BSE.Modules.UserManagement.Identity;

/// <summary>
/// Resolves <see cref="IUserContext"/> from the current HTTP request's <see cref="ClaimsPrincipal"/>.
/// Registered as scoped so it is recreated per request.
/// </summary>
/// <remarks>
/// UPN claim resolution order (Azure AD format):
/// 1. <c>preferred_username</c> — standard Azure AD OIDC claim
/// 2. <c>ClaimTypes.Upn</c> — legacy Windows/Kerberos UPN fallback
/// </remarks>
public sealed class ClaimsUserContext : IUserContext
{
    // "bse:group" claim is populated by GroupClaimsTransformation before this is resolved.
    public const string BseGroupClaimType = "bse:group";

    // Primary Azure AD claim name for UPN.
    public const string PreferredUsernameClaim = "preferred_username";

    private readonly ClaimsPrincipal _principal;

    public ClaimsUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _principal = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    }

    public string Upn =>
        _principal.FindFirstValue(PreferredUsernameClaim)
        ?? _principal.FindFirstValue(ClaimTypes.Upn)
        ?? string.Empty;

    public string DisplayName =>
        _principal.FindFirstValue(ClaimTypes.Name)
        ?? _principal.FindFirstValue("name")
        ?? Upn;

    public UserGroup Group
    {
        get
        {
            var raw = _principal.FindFirstValue(BseGroupClaimType);
            return Enum.TryParse<UserGroup>(raw, ignoreCase: true, out var group)
                ? group
                : UserGroup.None;
        }
    }

    public bool IsInGroup(UserGroup group) => Group == group;
}
