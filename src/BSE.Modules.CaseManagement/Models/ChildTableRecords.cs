namespace BSE.Modules.CaseManagement.Models;

/// <summary>Result record for GetOtherOwnerByRBSE.</summary>
public record OtherOwnerRecord(int Id, string Rbse, string? Type, string? Name, string? Cphh, byte[]? RowStamp);

/// <summary>Result record for GetFeedByRBSE — feed history with ration type and supplier lookups.</summary>
public sealed record CaseFeedRecord
{
    public int Id { get; init; }
    public string Rbse { get; init; } = string.Empty;
    public short? YearFrom { get; init; }
    public short? YearTo { get; init; }
    public string? RationType { get; init; }
    public string? RationDescription { get; init; }
    public int? SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string? RationName { get; init; }
    public bool IsPrePurchase { get; init; }
    public byte[]? RowStamp { get; init; }
}

/// <summary>Result record for GetClinicalVisitByRBSE.</summary>
public record ClinicalVisitRecord(int Id, string Rbse, DateTime? VisitDate, byte[]? RowStamp);

/// <summary>Result record for GetTestByRBSE — test results with lookup descriptions.</summary>
public record CaseTestRecord(
    int Id,
    string Rbse,
    string TestType,
    string? TestTypeDescription,
    string? TestResult,
    string? TestResultDescription,
    byte[]? RowStamp);

/// <summary>Result record for GetCaseWorkByRBSE — casework admin dates and status.</summary>
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
