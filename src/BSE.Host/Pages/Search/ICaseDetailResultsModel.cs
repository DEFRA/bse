using BSE.Modules.Search.Models;

namespace BSE.Host.Pages.Search;

/// <summary>What the shared case-detail results partial needs from a search page model.</summary>
public interface ICaseDetailResultsModel
{
    int PageNumber { get; }
    string SortColumn { get; }
    bool SortDesc { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    IReadOnlyList<CaseDetailSearchResult> PagedResults();
}
