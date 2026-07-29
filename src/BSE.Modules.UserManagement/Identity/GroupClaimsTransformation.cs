using System.Security.Claims;
using BSE.Modules.UserManagement.Repositories;
using Microsoft.AspNetCore.Authentication;

namespace BSE.Modules.UserManagement.Identity;

/// <summary>
/// Runs on every authenticated request. Resolves the user's <see cref="BSE.SharedKernel.UserGroup"/>
/// from the database and adds two claims to the principal:
/// <list type="bullet">
///   <item><c>bse:group</c> — the <see cref="BSE.SharedKernel.UserGroup"/> enum name.</item>
///   <item><see cref="ClaimTypes.Role"/> — same value, enabling ASP.NET Core policy role checks.</item>
/// </list>
/// </summary>
/// <remarks>
/// Lookup strategy (UPN-first with NTLogin fallback):
/// <list type="number">
///   <item>Read UPN from <c>preferred_username</c> claim (Azure AD format), falling back to
///         <see cref="ClaimTypes.Upn"/>.</item>
///   <item>Call <see cref="IUserRepository.GetByUpnAsync"/>. Returns a match once the UPN column
///         is populated (after <c>AddUserUpnColumn.sql</c> + SP update).</item>
///   <item>If not found, derive NTLogin from the UPN local part (before '@') and call
///         <see cref="IUserRepository.GetByNtLoginAsync"/>. This is the transition-period heuristic
///         that works when database NTLogin equals the UPN local part.</item>
/// </list>
/// When real Azure AD is wired (env vars OIDC__Authority / OIDC__ClientId / OIDC__ClientSecret
/// set), this transformation runs unchanged — no code changes required.
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

        // Avoid double-transformation if claims already present.
        if (principal.HasClaim(c => c.Type == ClaimsUserContext.BseGroupClaimType))
            return principal;

        var upn = principal.FindFirstValue(ClaimsUserContext.PreferredUsernameClaim)
                  ?? principal.FindFirstValue(ClaimTypes.Upn);

        if (string.IsNullOrWhiteSpace(upn))
            return principal;

        var user = await _userRepository.GetByUpnAsync(upn)
                   ?? await _userRepository.GetByNtLoginAsync(DeriveNtLoginFromUpn(upn));

        if (user is null)
            return principal;

        var clone = principal.Clone();
        var identity = (ClaimsIdentity)clone.Identity!;

        var groupName = user.UserGroup.ToString();
        identity.AddClaim(new Claim(ClaimsUserContext.BseGroupClaimType, groupName));
        identity.AddClaim(new Claim(ClaimTypes.Role, groupName));

        return clone;
    }

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
