namespace BSE.SharedKernel;

/// <summary>
/// Represents the BSE application user group, mapping directly to the <c>luUserGroup</c>
/// table in the database. Values are derived from <c>GetluUserGroup</c> SP and legacy
/// <c>clsUser.vb</c> usage.
/// </summary>
public enum UserGroup
{
    None = 0,
    Admin = 1,
    DataEntry = 2,
    ReadOnly = 3,
    DEFRAMaintenance = 4,
    Supervisor = 5
}
