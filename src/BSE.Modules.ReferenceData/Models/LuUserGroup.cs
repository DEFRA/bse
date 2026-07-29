namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luUserGroup via GetluUserGroup (SP owned by UserManagement domain).</summary>
public record LuUserGroup
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
