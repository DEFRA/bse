namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luRelationFate via GetluRelationFate.</summary>
public record LuRelationFate
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
