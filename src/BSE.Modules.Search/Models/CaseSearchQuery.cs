namespace BSE.Modules.Search.Models;

/// <summary>
/// Input parameters for GetSearchCase SP. Nullable dates use the SP default
/// (1 Jan 1900 for earliest, GETDATE() for latest) when not specified.
/// </summary>
public record CaseSearchQuery(
    string Rbse = "",
    string Eartag = "",
    string Dbse = "",
    string Fate = "",
    string FinalResult = "",
    string Sex = "",
    string Survey = "",
    string Notes = "",
    DateTime? EarliestFormADate = null,
    DateTime? LatestFormADate = null,
    DateTime? EarliestFinalResultDate = null,
    DateTime? LatestFinalResultDate = null,
    DateTime? EarliestBirthDate = null,
    DateTime? LatestBirthDate = null,
    bool IncludeNonGbCases = false,
    string PassiveActive = "",
    bool IsImportedCase = false);
