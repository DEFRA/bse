namespace BSE.Modules.CaseWork.Models;

/// <summary>
/// Full casework entry record from <c>GetCaseWorkEntryByRBSE</c>.
/// Joins [Case] and [CaseWork] and includes server-computed due date expressions.
/// <c>BarbMemoDue</c> is a computed varchar column ('Yes' or NULL), not a date.
/// </summary>
public sealed record CaseWorkEntryRecord
{
    // ── Case columns ──────────────────────────────────────────────────────────
    public string Rbse { get; init; } = string.Empty;
    public string? Survey { get; init; }
    public DateTime? FormADate { get; init; }
    public DateTime? FormBDate { get; init; }
    public DateTime? SlaughterDate { get; init; }
    public string? Fate { get; init; }
    public bool? IsPaperworkComplete { get; init; }
    public string? FinalResult { get; init; }
    public DateTime? FinalResultDate { get; init; }
    public DateTime? BirthDate { get; init; }
    public string? Origin { get; init; }
    public string? Dbse { get; init; }
    public string? AlternateDiagnosis { get; init; }

    // ── CaseWork columns ──────────────────────────────────────────────────────
    public string? Barcode { get; init; }
    public string? AhfReference { get; init; }
    public DateTime? RbseDate { get; init; }
    public DateTime? PurchaserBse1ReceivedDate { get; init; }
    public DateTime? BreederBse1ReceivedDate { get; init; }
    public DateTime? Vendor1Bse1ReceivedDate { get; init; }
    public DateTime? HomebredBse1ReceivedDate { get; init; }
    public DateTime? SummarySheetReceivedDate { get; init; }
    public DateTime? PaperworkCompleteDate { get; init; }
    public DateTime? ActiveMemoDate { get; init; }
    public DateTime? AnnexADate { get; init; }
    public DateTime? AnnexBDate { get; init; }
    public DateTime? AnnexCDate { get; init; }
    public DateTime? AnnexDDate { get; init; }
    public string? RegionalLab { get; init; }
    public DateTime? ReceivedByRegionalLabDate { get; init; }
    public DateTime? InitialReceivedDate { get; init; }
    public DateTime? FinalReceivedDate { get; init; }
    public DateTime? FinalSentDate { get; init; }
    public DateTime? LabChasedDate { get; init; }
    public DateTime? Post2000SentDate { get; init; }
    public DateTime? BarbMinuteSentDate { get; init; }
    public DateTime? DataCompleteDate { get; init; }
    public string? CaseWorkNotes { get; init; }
    public bool? IsCaseClosed { get; init; }
    public string? TseTestingSite { get; init; }
    public DateTime? SamplingDate { get; init; }
    public int? Ahro { get; init; }

    // ── Server-computed due dates ─────────────────────────────────────────────
    public DateTime? ActiveMemoDueDate { get; init; }
    public DateTime? AnnexADueDate { get; init; }
    public DateTime? AnnexBDueDate { get; init; }
    public DateTime? AnnexCDueDate { get; init; }
    public DateTime? AnnexDDueDate { get; init; }
    public DateTime? LabChaseDueDate { get; init; }
    /// <summary>'Yes' if BARB minute is due, NULL otherwise (varchar column from SP).</summary>
    public string? BarbMemoDue { get; init; }
}
