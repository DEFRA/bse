namespace BSE.Modules.AnimalRelations.Models;

/// <summary>
/// Result from <c>GetRelationDetailsOfRelatedCase</c> — live case data for a related animal.
/// Used to populate an edit form with the current state of a related case.
/// Note: <c>LeftDate</c> is returned as a <c>varchar(30)</c> by the SP (DD/MM/YYYY format).
/// </summary>
public sealed record RelatedCaseDetailsRecord
{
    public string RelationRbse { get; init; } = string.Empty;
    public string? Sex { get; init; }
    public string? SexDesc { get; init; }
    public int? BirthDay { get; init; }
    public int? BirthMonth { get; init; }
    public int? BirthYear { get; init; }
    public string? Fate { get; init; }
    public string? FateDesc { get; init; }
    /// <summary>Formatted as DD/MM/YYYY by the SP (CONVERT(varchar(30), ..., 103)).</summary>
    public string? LeftDate { get; init; }
    public string? EartagCountry { get; init; }
    public string? EartagHerdmark { get; init; }
    public string? Eartag { get; init; }
    /// <summary>Sire name from Pedigree.</summary>
    public string? Name { get; init; }
}
