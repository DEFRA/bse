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

public interface IGeoLookupService
{
    Task<IReadOnlyList<ParishMapReferenceRow>> GetAllParishMapReferencesAsync(string county, string parish);
    Task<string?> GetPrefixCodeAsync(string xCoordPrefix, string yCoordPrefix);
    Task<MapPrefixXY?> GetXYCoordsByPrefixCodeAsync(string code);
}
