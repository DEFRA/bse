namespace BSE.Modules.CaseWork.Models;

/// <summary>
/// Result from <c>GetCaseWorkByRBSE</c> — the lightweight casework admin record used during
/// the core case detail view (12 columns). This is the same shape returned by the SP called
/// within the CaseManagement GetCaseDetailsByRBSE multi-result-set query.
/// </summary>
public sealed record CaseWorkRecord
{
    public string Rbse { get; init; } = string.Empty;
    public DateTime? RbseDate { get; init; }
    public string? Barcode { get; init; }
    public string? AhfReference { get; init; }
    public DateTime? PurchaserBse1ReceivedDate { get; init; }
    public DateTime? BreederBse1ReceivedDate { get; init; }
    public DateTime? Vendor1Bse1ReceivedDate { get; init; }
    public DateTime? HomebredBse1ReceivedDate { get; init; }
    public DateTime? SummarySheetReceivedDate { get; init; }
    public DateTime? PaperworkCompleteDate { get; init; }
    public byte[]? RowStamp { get; init; }
    public bool IsCaseClosed { get; init; }
}
