using System.Data;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Models;

namespace BSE.Modules.AnimalRelations.Repositories;

public interface IAnimalRelationsRepository
{
    // ── Reads ──────────────────────────────────────────────────────────────────

    /// <summary>Returns all relations for the given RBSE (GetRelationsByRBSE).</summary>
    Task<IReadOnlyList<CaseRelationRecord>> GetRelationsByRbseAsync(string rbse);

    /// <summary>
    /// Returns the full relation context for a case: dam detail, sire detail, and all relations
    /// (GetRelationsDetailsByRBSE — three result sets via QueryMultiple).
    /// </summary>
    Task<RelationDetailsRecord> GetRelationsDetailsByRbseAsync(string rbse);

    /// <summary>Returns live case data for a single related animal (GetRelationDetailsOfRelatedCase).</summary>
    Task<RelatedCaseDetailsRecord?> GetRelationDetailsOfRelatedCaseAsync(string rbse);

    /// <summary>Returns relation summaries for all cases in a batch (GetRelationsByBatchID).</summary>
    Task<IReadOnlyList<BatchRelationRecord>> GetRelationsByBatchIdAsync(int batchId);

    /// <summary>Returns dam/sire pedigree for all cases in a batch (GetDamSireDetailsByBatchID).</summary>
    Task<IReadOnlyList<BatchDamSireRecord>> GetDamSireDetailsByBatchIdAsync(int batchId);

    /// <summary>
    /// Returns pedigree animals matching the search criteria for the dam/sire picker
    /// (GetDamSireDetailsMatches). Sex drives the dam (F) vs sire (M) branch in the SP.
    /// </summary>
    Task<IReadOnlyList<DamSireDetailRecord>> GetDamSireDetailsMatchesAsync(
        string? eartag, string? name, string? rbse, string? herdbook, string sex);

    // ── Writes (enlisted in caller's transaction) ──────────────────────────────

    Task AddRelationAsync(AddCaseRelationCommand command, IDbConnection connection, IDbTransaction transaction);
    Task EditRelationAsync(EditCaseRelationCommand command, IDbConnection connection, IDbTransaction transaction);
    Task DeleteRelationAsync(DeleteCaseRelationCommand command, IDbConnection connection, IDbTransaction transaction);
}
