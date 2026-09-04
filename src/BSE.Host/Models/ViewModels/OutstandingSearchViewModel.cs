using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class OutstandingSearchViewModel : SearchViewModelBase<OutstandingCaseResult>
{
    public DateTime? EarliestFormADate { get; set; }
    public DateTime? LatestFormADate { get; set; }
    public bool IncludeNonGb { get; set; }

    public string SearchType { get; set; } = ""; // BSE1 | Fates | Results

    protected override int PageSize => 10;

    protected override IEnumerable<OutstandingCaseResult> ApplySorting(IReadOnlyList<OutstandingCaseResult> source) =>
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
