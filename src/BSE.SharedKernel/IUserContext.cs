namespace BSE.SharedKernel;

/// <summary>
/// Provides read-only access to the resolved identity of the current HTTP request's user.
/// Populated by <c>GroupClaimsTransformation</c> in the UserManagement module.
/// </summary>
public interface IUserContext
{
    /// <summary>User Principal Name (Azure AD format: user@domain).</summary>
    string Upn { get; }

    /// <summary>Display name from claims.</summary>
    string DisplayName { get; }

    /// <summary>Resolved database user group.</summary>
    UserGroup Group { get; }

    /// <summary>Returns <c>true</c> if the user belongs to the specified group.</summary>
    bool IsInGroup(UserGroup group);
}
