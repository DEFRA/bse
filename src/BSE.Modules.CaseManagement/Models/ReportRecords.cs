namespace BSE.Modules.CaseManagement.Models;

/// <summary>Summary result for GetCaseFarmByBatchID — RBSE/CPHH pair for batch report context.</summary>
public record CaseFarmSummaryRecord(string Rbse, string? Cphh);

/// <summary>Result for GetFinalResultByRBSE.</summary>
public record FinalResultRecord(string Rbse, string? FinalResult, DateTime? FinalResultDate);

/// <summary>Row returned by GetLatestRBSEForYear and GetLatestDBSEForYear.</summary>
public record LatestReferenceRecord(string LatestReference);
