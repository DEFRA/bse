namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps the calculated OS map-reference columns returned by GetMapReferenceByCountyParish.</summary>
public record GeoMapReference
{
    public string XReference1 { get; init; } = string.Empty;
    public string YReference1 { get; init; } = string.Empty;
    public string XReference2 { get; init; } = string.Empty;
    public string YReference2 { get; init; } = string.Empty;
}
