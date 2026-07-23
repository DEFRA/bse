using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class OutstandingSearchViewModel
{
    public DateTime? EarliestFormADate { get; set; }
    public DateTime? LatestFormADate { get; set; }
    public bool IncludeNonGb { get; set; }

    public string SearchType { get; set; } = "BSE1"; // BSE1 | Fates | Results

    public IReadOnlyList<OutstandingCaseResult> Results { get; set; } = [];
    public bool HasSearched { get; set; }

    public OutstandingSearchQuery ToQuery() => new(
        EarliestFormADate: EarliestFormADate,
        LatestFormADate: LatestFormADate,
        IncludeNonGbCases: IncludeNonGb);
}
