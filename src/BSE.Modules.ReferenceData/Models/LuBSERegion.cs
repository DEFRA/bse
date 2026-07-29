namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luBSERegion via GetluBSERegion.</summary>
public record LuBSERegion
{
    public int Id { get; init; }
    public int SortOrder { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? CountryID { get; init; }
}
