namespace BSE.Modules.Search.Models;

/// <summary>
/// Result record for GetSearchCaseByCPHH and GetSearchCaseByEartagHerdmark —
/// extended case result including purchase/onset dates and status fields.
/// </summary>
public record CaseDetailSearchResult(
    string Rbse,
    string Cphh,
    string? Sex,
    string? Eartag,
    DateTime? BirthDate,
    DateTime? PurchaseDate,
    int? PurchaseAgeInMonths,
    DateTime? OnsetDate,
    DateTime? FormADate,
    DateTime? SlaughterDate,
    DateTime? FinalResultDate,
    int? OnsetAgeInMonths,
    string? Fate,
    string? FinalResult,
    string? Survey,
    string? CaseStatus,
    string? TimeElapsed,
    int? DaysElapsed,
    string? Origin);
