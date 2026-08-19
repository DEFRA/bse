using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class OutstandingSearchViewModel
{
    public DateTime? EarliestFormADate { get; set; }
    public DateTime? LatestFormADate { get; set; }
    public bool IncludeNonGb { get; set; }

    public string SearchType { get; set; } = "BSE1"; // BSE1 | Fates | Results

    public int PageNumber { get; set; } = 1;
    public string SortColumn { get; set; } = "";
    public bool SortDesc { get; set; }

    public IReadOnlyList<OutstandingCaseResult> Results { get; set; } = [];
    public bool HasSearched { get; set; }

    public const int PageSize = 50;
    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<OutstandingCaseResult> PagedResults =>
        ApplySorting(Results)
            .Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    private IEnumerable<OutstandingCaseResult> ApplySorting(IReadOnlyList<OutstandingCaseResult> source) =>
        (SortColumn?.ToLowerInvariant(), SortDesc) switch
        {
            ("cphh",        false) => source.OrderBy(r => r.Cphh),
            ("cphh",        true)  => source.OrderByDescending(r => r.Cphh),
            ("eartag",      false) => source.OrderBy(r => r.Eartag),
            ("eartag",      true)  => source.OrderByDescending(r => r.Eartag),
            ("formadate",   false) => source.OrderBy(r => r.FormADate),
            ("formadate",   true)  => source.OrderByDescending(r => r.FormADate),
            ("birthdate",   false) => source.OrderBy(r => r.BirthDate),
            ("birthdate",   true)  => source.OrderByDescending(r => r.BirthDate),
            ("fate",        false) => source.OrderBy(r => r.Fate),
            ("fate",        true)  => source.OrderByDescending(r => r.Fate),
            ("finalresult", false) => source.OrderBy(r => r.FinalResult),
            ("finalresult", true)  => source.OrderByDescending(r => r.FinalResult),
            _                      => source.OrderBy(r => r.Rbse),
        };

    public OutstandingSearchQuery ToQuery() => new(
        EarliestFormADate: EarliestFormADate,
        LatestFormADate: LatestFormADate,
        IncludeNonGbCases: IncludeNonGb);
}
