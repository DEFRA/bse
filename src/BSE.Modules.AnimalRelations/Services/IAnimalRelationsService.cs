using BSE.Modules.AnimalRelations.Models;

namespace BSE.Modules.AnimalRelations.Services;

public interface IAnimalRelationsService
{
    /// <summary>Returns all relations for a case.</summary>
    Task<IReadOnlyList<CaseRelationRecord>> GetRelationsByRbseAsync(string rbse);

    /// <summary>Returns dam detail, sire detail, and all relations for a case.</summary>
    Task<RelationDetailsRecord> GetRelationsDetailsByRbseAsync(string rbse);

    /// <summary>Returns live case data for a related animal (used to pre-populate an edit form).</summary>
    Task<RelatedCaseDetailsRecord?> GetRelationDetailsOfRelatedCaseAsync(string rbse);

    /// <summary>Returns relation summaries for all cases in a batch.</summary>
    Task<IReadOnlyList<BatchRelationRecord>> GetRelationsByBatchIdAsync(int batchId);

    /// <summary>Returns dam/sire pedigree for all cases in a batch.</summary>
    Task<IReadOnlyList<BatchDamSireRecord>> GetDamSireDetailsByBatchIdAsync(int batchId);

    /// <summary>
    /// Returns pedigree animals matching the dam/sire picker search criteria.
    /// <paramref name="sex"/> must be "F" (dam) or "M" (sire).
    /// </summary>
    Task<IReadOnlyList<DamSireDetailRecord>> GetDamSireDetailsMatchesAsync(
        string? eartag, string? name, string? rbse, string? herdbook, string sex);
}
