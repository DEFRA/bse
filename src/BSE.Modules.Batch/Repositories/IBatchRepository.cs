using System.Data;
using BSE.Modules.Batch.Models;

namespace BSE.Modules.Batch.Repositories;

public interface IBatchRepository
{
    /// <summary>
    /// Calls AddBatchNumber. Returns the existing unfilled batch if no cases are linked yet,
    /// otherwise inserts and returns a new batch for the current year.
    /// </summary>
    Task<BatchRecord> GetOrCreateBatchNumberAsync();

    /// <summary>
    /// Calls AddBatchNumber1989 — always inserts a new batch for year 1989 (pre-1990 historic data).
    /// </summary>
    Task<BatchRecord> CreateBatchNumber1989Async();

    /// <summary>Calls AddBatchNumberLink — links a case document to a batch (standalone).</summary>
    Task AddBatchNumberLinkAsync(int batchId, string rbse, string document);

    /// <summary>
    /// Calls AddBatchNumberLink on a caller-supplied connection/transaction.
    /// Used by Case Management to enlist the link operation in the case-save transaction.
    /// </summary>
    Task AddBatchNumberLinkAsync(int batchId, string rbse, string document, IDbConnection connection, IDbTransaction? transaction);

    /// <summary>Calls GetBatchIDForBatch — resolves BatchId from year + number. Returns null if not found.</summary>
    Task<int?> GetBatchIdAsync(short batchYear, int batchNumber);

    /// <summary>Calls GetBatchNumberByRBSE — all batch links for a given RBSE.</summary>
    Task<IReadOnlyList<BatchNumberEntry>> GetBatchNumbersByRbseAsync(string rbse);

    /// <summary>Calls GetLatestBatchNumbers — top-3 most recent batches with BSE1 case counts.</summary>
    Task<IReadOnlyList<LatestBatchRecord>> GetLatestBatchNumbersAsync();

    /// <summary>Calls GetCaseByBatchID — minimal RBSE/CPHH list for a batch.</summary>
    Task<IReadOnlyList<BatchCaseSummaryRecord>> GetCasesByBatchIdAsync(int batchId);

    /// <summary>Calls GetCPHHRBSEForBatch — minimal RBSE/CPHH list by year and number.</summary>
    Task<IReadOnlyList<BatchCaseSummaryRecord>> GetCasesByBatchAsync(short batchYear, int batchNumber);

    /// <summary>Calls GetCPHHRBSEForBatchID — full display-formatted RBSE/CPHH list for a batch.</summary>
    Task<IReadOnlyList<BatchCaseRecord>> GetCaseDetailsByBatchIdAsync(int batchId);
}
