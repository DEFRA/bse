namespace BSE.Modules.Batch.Models;

/// <summary>
/// Result record for GetLatestBatchNumbers — top-3 most recent batches with BSE1 case counts.
/// Batch is formatted as "YYYY/N" by the SP.
/// </summary>
public record LatestBatchRecord(string Batch, int CaseCount);
