namespace BSE.Modules.Batch.Models;

/// <summary>
/// Result record for AddBatchNumber and AddBatchNumber1989 — populated from OUTPUT params.
/// Also used when reading a batch by year/number.
/// </summary>
public record BatchRecord(int BatchId, short BatchYear, int BatchNumber);
