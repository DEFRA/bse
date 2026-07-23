namespace BSE.Modules.AnimalRelations.Models;

/// <summary>
/// Result record for <c>GetDamDetailsByRBSE</c> and <c>GetSireDetailsByRBSE</c>.
/// Both SPs return the same 12-column shape; the sire SP omits <c>FinalResult</c>,
/// which Dapper maps as <c>null</c>.
/// Also returned by <c>GetDamSireDetailsMatches</c> (all 12 cols present).
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
    public byte[]? RowStamp { get; init; }
}
