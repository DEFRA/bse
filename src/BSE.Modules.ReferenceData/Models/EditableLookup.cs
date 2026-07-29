namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to the EditableLookup table via GetEditableLookups.</summary>
public record EditableLookup
{
    public int Id { get; init; }
    public string TableName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
