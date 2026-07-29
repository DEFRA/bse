namespace BSE.Modules.Search.Models;

/// <summary>
/// Input parameters shared by the three outstanding-data SPs:
/// GetSearchOutstandingBSE1s, GetSearchOutstandingFates, GetSearchOutstandingResults.
/// </summary>
public record OutstandingSearchQuery(
    DateTime? EarliestFormADate = null,
    DateTime? LatestFormADate = null,
    bool IncludeNonGbCases = false);
