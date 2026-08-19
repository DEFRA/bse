namespace BSE.Modules.CaseManagement.Models;

/// <summary>Summary result for GetCaseFarmByBatchID — RBSE/CPHH pair for batch report context.</summary>
public record CaseFarmSummaryRecord(string Rbse, string? Cphh);

/// <summary>Full result for GetFinalResultByRBSE — used by the Final Result Entry page.</summary>
public record FinalResultRecord
{
    public string Rbse { get; init; } = string.Empty;
    public string? Cphh { get; init; }
    public string? Eartag { get; init; }
    public DateTime? BirthDate { get; init; }
    public bool IsPurchaserBse1Received { get; init; }
    public bool IsBreederBse1Received { get; init; }
    public bool IsVendor1Bse1Received { get; init; }
    public bool IsHomebredBse1Received { get; init; }
    public bool IsSummarySheetReceived { get; init; }
    public bool IsPaperworkComplete { get; init; }
    public string? FinalResult { get; init; }
    public DateTime? FinalResultDate { get; init; }
    public string? Dbse { get; init; }
    public string? RetrospectiveTestType { get; init; }
    public string? RetrospectiveResult { get; init; }
    public DateTime? RetrospectiveResultDate { get; init; }
    public string? RetrospectiveComment { get; init; }
    public string? LabComment { get; init; }
    public string? AlternateDiagnosis { get; init; }
    public string? CaseType { get; init; }
    public string? OwnerName { get; init; }
    public string? Address1 { get; init; }
    public DateTime? PurchaserBse1ReceivedDate { get; init; }
    public DateTime? BreederBse1ReceivedDate { get; init; }
    public DateTime? Vendor1Bse1ReceivedDate { get; init; }
    public DateTime? HomebredBse1ReceivedDate { get; init; }
    public DateTime? SummarySheetReceivedDate { get; init; }
    public DateTime? PaperworkCompleteDate { get; init; }
    public byte[]? RowStamp { get; init; }
}
