namespace BSE.Modules.CaseManagement.Commands;

/// <summary>Maps to the AddNonGBCase stored procedure parameters.</summary>
public sealed record AddNonGbCaseCommand(
    string Rbse,
    string Cphh,
    string? EartagCountry,
    string? EartagHerdmark,
    string? Eartag,
    string? Fate,
    string? FinalResult,
    DateTime? FinalResultDate,
    DateTime? SlaughterDate,
    string? OwnerName,
    string? Address1,
    string? Address2,
    string? Address3,
    string? Postcode,
    string? County,
    string? Herdmark1,
    string? NumericHerdmark1,
    DateTime? RbseDate,
    string? Barcode,
    string? AhfReference);
