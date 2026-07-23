namespace BSE.Modules.CaseWork.Commands;

/// <summary>Maps to <c>AddCaseWork</c> (10 parameters). Called as part of the case-create transaction.</summary>
public sealed record AddCaseWorkCommand(
    string Rbse,
    DateTime? RbseDate,
    string? Barcode,
    string? AhfReference,
    DateTime? PurchaserBse1ReceivedDate,
    DateTime? BreederBse1ReceivedDate,
    DateTime? Vendor1Bse1ReceivedDate,
    DateTime? HomebredBse1ReceivedDate,
    DateTime? SummarySheetReceivedDate,
    DateTime? PaperworkCompleteDate);

/// <summary>Maps to <c>EditCaseWork</c> (10 parameters). Basic casework field update.</summary>
public sealed record EditCaseWorkCommand(
    string Rbse,
    DateTime? RbseDate,
    string? Barcode,
    string? AhfReference,
    DateTime? PurchaserBse1ReceivedDate,
    DateTime? BreederBse1ReceivedDate,
    DateTime? Vendor1Bse1ReceivedDate,
    DateTime? HomebredBse1ReceivedDate,
    DateTime? SummarySheetReceivedDate,
    DateTime? PaperworkCompleteDate);

/// <summary>
/// Maps to <c>EditCaseWorkEntry</c> (29 parameters including UserID for audit logging).
/// Full casework form update — includes all date fields, notes, closure flag, and TSE fields.
/// </summary>
public sealed record EditCaseWorkEntryCommand(
    string Rbse,
    string? Barcode,
    string? AhfReference,
    DateTime? PurchaserBse1ReceivedDate,
    DateTime? BreederBse1ReceivedDate,
    DateTime? Vendor1Bse1ReceivedDate,
    DateTime? HomebredBse1ReceivedDate,
    DateTime? SummarySheetReceivedDate,
    DateTime? PaperworkCompleteDate,
    DateTime? ActiveMemoDate,
    DateTime? AnnexADate,
    DateTime? AnnexBDate,
    DateTime? AnnexCDate,
    DateTime? AnnexDDate,
    string? RegionalLab,
    DateTime? ReceivedByRegionalLabDate,
    DateTime? InitialReceivedDate,
    DateTime? FinalReceivedDate,
    DateTime? FinalSentDate,
    DateTime? LabChasedDate,
    DateTime? BarbMinuteSentDate,
    DateTime? Post2000SentDate,
    string? CaseWorkNotes,
    DateTime? DataCompleteDate,
    bool? IsCaseClosed,
    int UserId,
    string? TseTestingSite,
    DateTime? SamplingDate,
    int? AhroId);
