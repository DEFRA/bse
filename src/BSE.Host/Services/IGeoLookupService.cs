namespace BSE.Host.Services;

public record ParishMapReferenceRow
{
    public string XReference1 { get; init; } = string.Empty;
    public string YReference1 { get; init; } = string.Empty;
    public string XReference2 { get; init; } = string.Empty;
    public string YReference2 { get; init; } = string.Empty;
}

public record MapPrefixXY
{
    public string Code         { get; init; } = string.Empty;
    public string XCoordPrefix { get; init; } = string.Empty;
    public string YCoordPrefix { get; init; } = string.Empty;
}

/// <summary>Result of <see cref="IGeoLookupService.EstimateMapReferenceAsync"/>: either the 3-part
/// reference (2-letter OS grid code + 3-digit easting + 3-digit northing) or an error message.</summary>
public sealed record MapReferenceEstimate(string? MapRef1, string? MapRef2, string? MapRef3, string? Error);

public interface IGeoLookupService
{
    Task<IReadOnlyList<ParishMapReferenceRow>> GetAllParishMapReferencesAsync(string county, string parish);
    Task<string?> GetPrefixCodeAsync(string xCoordPrefix, string yCoordPrefix);
    Task<MapPrefixXY?> GetXYCoordsByPrefixCodeAsync(string code);

    /// <summary>Mirrors legacy BSELib.clsCase.EstimateMapReference: picks the parish's middle
    /// coordinate row and converts it to an OS grid reference. Single implementation shared by
    /// every page with a Map Reference "Estimate" button, so the algorithm can't drift between them.</summary>
    Task<MapReferenceEstimate> EstimateMapReferenceAsync(string cphh);

    /// <summary>Mirrors legacy BSELib.clsCase.ValidateMapReference: true if the given map reference
    /// falls within any of the parish's coordinate rows (or if there's no boundary data to check
    /// against at all). Single implementation shared by every page with live map-reference validation.</summary>
    Task<bool> IsMapReferenceWithinParishAsync(string cphh, string mapRef);
}
