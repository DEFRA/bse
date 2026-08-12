using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class OutstandingSearchViewModel
{
    public DateTime? EarliestFormADate { get; set; }
    public DateTime? LatestFormADate { get; set; }
    public bool IncludeNonGb { get; set; }

    public string SearchType { get; set; } = "BSE1"; // BSE1 | Fates | Results

    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<OutstandingCaseResult> Results { get; set; } = [];
    public bool HasSearched { get; set; }

    public const int PageSize = 50;
    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<OutstandingCaseResult> PagedResults =>
        Results.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public OutstandingSearchQuery ToQuery() => new(
        EarliestFormADate: EarliestFormADate,
        LatestFormADate: LatestFormADate,
        IncludeNonGbCases: IncludeNonGb);
}
