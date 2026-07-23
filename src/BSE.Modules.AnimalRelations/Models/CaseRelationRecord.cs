namespace BSE.Modules.AnimalRelations.Models;

/// <summary>
/// One row from <c>GetRelationsByRBSE</c> — a relation of a case (dam, sire, offspring, etc.).
/// ISNULL expressions in the SP mean most fields are coalesced from the linked Case row
/// when RelationRBSE points at a known case.
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
