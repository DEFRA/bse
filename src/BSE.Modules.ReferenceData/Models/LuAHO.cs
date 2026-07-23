namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luAHO via GetluAHO / GetluAHOCode.</summary>
public record LuAHO
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? BSERegionID { get; init; }
}
