namespace BSE.Modules.CaseManagement.Models;

/// <summary>
/// Result record for GetRelationsByRBSE — a case's relations (dam/sire/offspring linkages).
/// </summary>
public sealed record CaseRelationRecord
{
    public int Id { get; init; }
    public string Rbse { get; init; } = string.Empty;
    public string? RelationType { get; init; }
    public string? RelationTypeDesc { get; init; }
    public string? RelationRbse { get; init; }
    public string? Sex { get; init; }
    public string? SexDesc { get; init; }
    public int? BirthDay { get; init; }
    public int? BirthMonth { get; init; }
    public int? BirthYear { get; init; }
    public string? RelationFate { get; init; }
    public string? RelationFateDesc { get; init; }
    public DateTime? LeftDate { get; init; }
    public string? EartagCountry { get; init; }
    public string? EartagHerdmark { get; init; }
    public string? Eartag { get; init; }
    public string? Sire { get; init; }
    public byte[]? RowStamp { get; init; }
}

/// <summary>
/// Result record for GetDamDetailsByRBSE and GetSireDetailsByRBSE.
/// Both SPs return the same shape (pedigree animal + child count).
/// </summary>
public sealed record DamSireDetailRecord
{
    public int Id { get; init; }
    public string? Rbse { get; init; }
    public string? Eartag { get; init; }
    public string? Name { get; init; }
    public string? Herdbook { get; init; }
    public int? BirthDay { get; init; }
    public int? BirthMonth { get; init; }
    public int? BirthYear { get; init; }
    public string? Fate { get; init; }
    public string? FinalResult { get; init; }
    public int? ChildCount { get; init; }
}
