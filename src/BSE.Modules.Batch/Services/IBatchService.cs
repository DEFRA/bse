using BSE.Modules.Batch.Models;

namespace BSE.Modules.Batch.Services;

public interface IBatchService
{
    /// <summary>
    /// Returns the existing unfilled batch if the current batch has no linked cases,
    /// otherwise creates and returns a new batch for the current year.
    /// </summary>
    Task<BatchRecord> GetOrCreateBatchNumberAsync();

    /// <summary>Creates a new batch entry for year 1989 (pre-1990 historic data).</summary>
    Task<BatchRecord> CreateBatchNumber1989Async();

    /// <summary>Links a case document to a batch. Standalone (own transaction).</summary>
    Task AddBatchNumberLinkAsync(int batchId, string rbse, string document);

    /// <summary>Resolves BatchId from batch year and number. Returns null if not found.</summary>
    Task<int?> GetBatchIdAsync(short batchYear, int batchNumber);

    /// <summary>Returns all batch links for a given RBSE number.</summary>
    Task<IReadOnlyList<BatchNumberEntry>> GetBatchNumbersByRbseAsync(string rbse);

    /// <summary>Returns the top-3 most recent batches with BSE1 case counts.</summary>
    Task<IReadOnlyList<LatestBatchRecord>> GetLatestBatchNumbersAsync();

    /// <summary>Returns the minimal RBSE/CPHH list for a given batch ID.</summary>
    Task<IReadOnlyList<BatchCaseSummaryRecord>> GetCasesByBatchIdAsync(int batchId);

    /// <summary>Returns the minimal RBSE/CPHH list for a batch identified by year and number.</summary>
    Task<IReadOnlyList<BatchCaseSummaryRecord>> GetCasesByBatchAsync(short batchYear, int batchNumber);

    /// <summary>Returns the display-formatted RBSE/CPHH list for a given batch ID.</summary>
    Task<IReadOnlyList<BatchCaseRecord>> GetCaseDetailsByBatchIdAsync(int batchId);
}
