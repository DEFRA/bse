namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luADNSRegion via GetluADNSRegion / GetluADNSRegionByAuthority.</summary>
public record LuADNSRegion
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
