namespace BSE.Modules.AnimalRelations.Models;

/// <summary>
/// One row from <c>GetRelationsByBatchID</c> — relation summary for all cases in a batch.
/// BirthDate and LeftDate are pre-formatted strings (DD/MM/YYYY) by the SP.
/// EartagHerdmark is the combined country+herdmark concatenation.
/// </summary>
public sealed record BatchRelationRecord
{
    public string Rbse { get; init; } = string.Empty;
    public string? RelationTypeDesc { get; init; }
    public string? RelationRbse { get; init; }
    public string? Sex { get; init; }
    public string? SexDesc { get; init; }
    public int? BirthDay { get; init; }
    public int? BirthMonth { get; init; }
    public int? BirthYear { get; init; }
    /// <summary>Pre-formatted birth date string produced by the SP (e.g. "12/03/2020").</summary>
    public string? BirthDate { get; init; }
    public string? RelationFate { get; init; }
    public string? RelationFateDesc { get; init; }
    /// <summary>Pre-formatted left date string (DD/MM/YYYY) produced by the SP.</summary>
    public string? LeftDate { get; init; }
    /// <summary>Combined EartagCountry + EartagHerdmark (concatenated by SP).</summary>
    public string? EartagHerdmark { get; init; }
    public string? Eartag { get; init; }
    public string? Sire { get; init; }
}
