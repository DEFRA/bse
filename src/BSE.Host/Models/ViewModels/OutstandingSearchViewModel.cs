using BSE.Host.Helpers;
using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class OutstandingSearchViewModel : SearchViewModelBase<OutstandingCaseResult>
{
    // Stored as strings to avoid __Invariant GET-form binding issues with DateTime?.
    // <input type="date"> always submits yyyy-MM-dd (ISO 8601).
    public string? EarliestFormADate { get; set; }
    public string? LatestFormADate { get; set; }
    public bool IncludeNonGb { get; set; }

    public string SearchType { get; set; } = ""; // BSE1 | Fates | Results

    // Populated by ValidateDates(); null when the corresponding field is blank or a valid date.
    public string? EarliestFormADateError { get; private set; }
    public string? LatestFormADateError { get; private set; }

    /// <summary>Business rule: date-range fields are optional, but a non-blank value must be a real date.</summary>
    public bool ValidateDates()
    {
        var ok = SearchDateField.TryParse(EarliestFormADate, out _, out var e1);
        EarliestFormADateError = e1;
        ok &= SearchDateField.TryParse(LatestFormADate, out _, out var e2);
        LatestFormADateError = e2;
        return ok;
    }

    protected override int PageSize => 10;

    protected override IEnumerable<OutstandingCaseResult> ApplySorting(IReadOnlyList<OutstandingCaseResult> source) =>
        (SortColumn?.ToLowerInvariant(), SortDesc) switch
        {
            ("rbse",        false) => source.OrderBy(r => r.Rbse),
            ("rbse",        true)  => source.OrderByDescending(r => r.Rbse),
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

    public OutstandingSearchQuery ToQuery()
    {
        SearchDateField.TryParse(EarliestFormADate, out var earliest, out _);
        SearchDateField.TryParse(LatestFormADate, out var latest, out _);
        return new(
            EarliestFormADate: earliest,
            LatestFormADate: latest,
            IncludeNonGbCases: IncludeNonGb);
    }
}
