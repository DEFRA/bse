namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luTestType via GetluTestType.</summary>
public record LuTestType
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
