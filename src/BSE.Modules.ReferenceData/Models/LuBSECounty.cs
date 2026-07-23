namespace BSE.Modules.ReferenceData.Models;

/// <summary>
/// Maps to luBSECounty via GetluBSECounty / GetNonGBCounty.
/// IDColumn is the source table's char(2) ID (not the sequential identity returned by the SP).
/// </summary>
public record LuBSECounty
{
    public int Id { get; init; }
    /// <summary>Source table [ID] column (char 2) aliased as IDColumn in the SP result set.</summary>
    public string IDColumn { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int? BSERegionID { get; init; }
}
