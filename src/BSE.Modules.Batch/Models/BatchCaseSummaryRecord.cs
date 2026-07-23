namespace BSE.Modules.Batch.Models;

/// <summary>
/// Minimal RBSE/CPHH pair returned by GetCaseByBatchID and GetCPHHRBSEForBatch.
/// </summary>
public record BatchCaseSummaryRecord(string Rbse, string? Cphh);
