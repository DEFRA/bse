namespace BSE.Modules.Search.Models;

/// <summary>
/// Result record for the three outstanding-data SPs:
/// GetSearchOutstandingBSE1s, GetSearchOutstandingFates, GetSearchOutstandingResults.
/// All three return the same shape.
/// </summary>
public record OutstandingCaseResult(
    string Rbse,
    string Cphh,
    string? Eartag,
    DateTime? FormADate,
    DateTime? BirthDate,
    string? Fate,
    string? FinalResult);
