using BSE.SharedKernel;

namespace BSE.Modules.UserManagement.Models;

/// <summary>
/// Represents a BSE application user mapped from the <c>[User]</c> table.
/// </summary>
public sealed record User(
    int UserId,
    string NTLogin,
    string? Upn,
    string UserName,
    string? Email,
    bool IsActive,
    int UserGroupId,
    UserGroup UserGroup);
