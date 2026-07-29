namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luMapReference via GetXYReferenceByPrefixCode.</summary>
public record MapReference
{
    public string Code { get; init; } = string.Empty;
    public string XCoordPrefix { get; init; } = string.Empty;
    public string YCoordPrefix { get; init; } = string.Empty;
}
