namespace BSE.Modules.Batch.Models;

/// <summary>
/// Result record for GetBatchNumberByRBSE — one row per document linked to the batch.
/// BatchNumber is formatted as "YYYY/NNN" by the SP.
/// </summary>
public record BatchNumberEntry(int BatchId, string BatchNumber, string Rbse, string? Document);
