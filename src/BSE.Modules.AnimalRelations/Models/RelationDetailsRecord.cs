namespace BSE.Modules.AnimalRelations.Models;

/// <summary>
/// Aggregated result from <c>GetRelationsDetailsByRBSE</c>, which executes three
/// child stored procedures returning three result sets:
/// <list type="number">
///   <item><c>GetDamDetailsByRBSE</c> — 0 or 1 row</item>
///   <item><c>GetSireDetailsByRBSE</c> — 0 or 1 row</item>
///   <item><c>GetRelationsByRBSE</c> — 0-N rows</item>
/// </list>
/// </summary>
public sealed record RelationDetailsRecord(
    DamSireDetailRecord? Dam,
    DamSireDetailRecord? Sire,
    IReadOnlyList<CaseRelationRecord> Relations);
