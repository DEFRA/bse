namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to the joined luParish / luADNSRegion / luAuthority result from GetParishByCountyParish.</summary>
public record ParishLookup
{
    public string County { get; init; } = string.Empty;
    public string Parish { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? ADNSRegionID { get; init; }
    public int? AuthorityID { get; init; }
    public int? AuthorityCountyID { get; init; }
    public string? BSECounty { get; init; }
}
