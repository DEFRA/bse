using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BSE.Host.Helpers;
using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class CaseSearchViewModel : SearchViewModelBase<CaseSearchResult>
{
    // --- Filter inputs ---
    // Legacy RBSE.ascx format is NN/NN/NNNNN; the search proc does a prefix match
    // (LIKE @RBSE + '%'), so business wants a partial prefix such as the first 2 digits to work.
    [RegularExpression(@"^(\d{2}(/)?(\d{0,2}(/)?\d{0,5})?)?$", ErrorMessage = "Enter RBSE as digits in the format NN/NN/NNNNN, or a shorter prefix such as the first 2 digits.")]
    public string Rbse { get; set; } = "";

    public string Eartag { get; set; } = "";

    // Legacy DBSE format is YY/NNNNN; same prefix-search rule as RBSE above.
    [RegularExpression(@"^(\d{2}(/)?\d{0,5})?$", ErrorMessage = "Enter DBSE as digits in the format YY/NNNNN, or a shorter prefix such as the first 2 digits.")]
    public string Dbse { get; set; } = "";
    public string Fate { get; set; } = "";
    public string FinalResult { get; set; } = "";
    public string Sex { get; set; } = "";
    public string Survey { get; set; } = "";
    public string Notes { get; set; } = "";
    public string PassiveActive { get; set; } = "";
    public bool IsImportedCase { get; set; }
    public bool IncludeNonGb { get; set; }

    // Stored as strings to avoid __Invariant GET-form binding issues with DateTime?.
    // <input type="date"> always submits yyyy-MM-dd (ISO 8601).
    public string? EarliestFormADate { get; set; }
    public string? LatestFormADate { get; set; }
    public string? EarliestFinalResultDate { get; set; }
    public string? LatestFinalResultDate { get; set; }
    public string? EarliestBirthDate { get; set; }
    public string? LatestBirthDate { get; set; }

    // Populated by ValidateDates(); null when the corresponding field is blank or a valid date.
    public string? EarliestFormADateError { get; private set; }
    public string? LatestFormADateError { get; private set; }
    public string? EarliestFinalResultDateError { get; private set; }
    public string? LatestFinalResultDateError { get; private set; }
    public string? EarliestBirthDateError { get; private set; }
    public string? LatestBirthDateError { get; private set; }

    /// <summary>Business rule: date-range fields are optional, but a non-blank value must be a real date,
    /// and when both ends of a range are given the earliest date must not be after the latest date.</summary>
    public bool ValidateDates()
    {
        var ok = SearchDateField.TryParse(EarliestFormADate, out var formAFrom, out var e1);
        EarliestFormADateError = e1;
        ok &= SearchDateField.TryParse(LatestFormADate, out var formATo, out var e2);
        LatestFormADateError = e2;
        ok &= SearchDateField.TryParse(EarliestFinalResultDate, out var finalFrom, out var e3);
        EarliestFinalResultDateError = e3;
        ok &= SearchDateField.TryParse(LatestFinalResultDate, out var finalTo, out var e4);
        LatestFinalResultDateError = e4;
        ok &= SearchDateField.TryParse(EarliestBirthDate, out var birthFrom, out var e5);
        EarliestBirthDateError = e5;
        ok &= SearchDateField.TryParse(LatestBirthDate, out var birthTo, out var e6);
        LatestBirthDateError = e6;

        if (ok)
        {
            ok &= CheckOrder(formAFrom, formATo, e => EarliestFormADateError = e, e => LatestFormADateError = e);
            ok &= CheckOrder(finalFrom, finalTo, e => EarliestFinalResultDateError = e, e => LatestFinalResultDateError = e);
            ok &= CheckOrder(birthFrom, birthTo, e => EarliestBirthDateError = e, e => LatestBirthDateError = e);
        }

        return ok;
    }

    private static bool CheckOrder(DateTime? from, DateTime? to, Action<string> setFromError, Action<string> setToError)
    {
        if (from is null || to is null || from <= to) return true;

        setFromError("Must be earlier than the latest date");
        setToError("Must be later than the earliest date");
        return false;
    }

    protected override int PageSize => 10;

    protected override IEnumerable<CaseSearchResult> ApplySorting(IReadOnlyList<CaseSearchResult> source) =>
        (SortColumn?.ToLowerInvariant(), SortDesc) switch
        {
            ("rbse",            false) => source.OrderBy(r => r.Rbse),
            ("rbse",            true)  => source.OrderByDescending(r => r.Rbse),
            ("cphh",            false) => source.OrderBy(r => r.Cphh),
            ("cphh",            true)  => source.OrderByDescending(r => r.Cphh),
            ("sex",             false) => source.OrderBy(r => r.Sex),
            ("sex",             true)  => source.OrderByDescending(r => r.Sex),
            ("survey",          false) => source.OrderBy(r => r.Survey),
            ("survey",          true)  => source.OrderByDescending(r => r.Survey),
            ("eartag",          false) => source.OrderBy(r => r.Eartag),
            ("eartag",          true)  => source.OrderByDescending(r => r.Eartag),
            ("birthdate",       false) => source.OrderBy(r => r.BirthDate),
            ("birthdate",       true)  => source.OrderByDescending(r => r.BirthDate),
            ("isbirthdateest",  false) => source.OrderBy(r => r.IsBirthDateEst),
            ("isbirthdateest",  true)  => source.OrderByDescending(r => r.IsBirthDateEst),
            ("origin",          false) => source.OrderBy(r => r.Origin),
            ("origin",          true)  => source.OrderByDescending(r => r.Origin),
            ("formadate",       false) => source.OrderBy(r => r.FormADate),
            ("formadate",       true)  => source.OrderByDescending(r => r.FormADate),
            ("fate",            false) => source.OrderBy(r => r.Fate),
            ("fate",            true)  => source.OrderByDescending(r => r.Fate),
            ("finalresult",     false) => source.OrderBy(r => r.FinalResult),
            ("finalresult",     true)  => source.OrderByDescending(r => r.FinalResult),
            ("finalresultdate", false) => source.OrderBy(r => r.FinalResultDate),
            ("finalresultdate", true)  => source.OrderByDescending(r => r.FinalResultDate),
            ("dbse",            false) => source.OrderBy(r => r.Dbse),
            ("dbse",            true)  => source.OrderByDescending(r => r.Dbse),
            ("valuationage",    false) => source.OrderBy(r => r.ValuationAge),
            ("valuationage",    true)  => source.OrderByDescending(r => r.ValuationAge),
            ("notes",           false) => source.OrderBy(r => r.Notes),
            ("notes",           true)  => source.OrderByDescending(r => r.Notes),
            ("babnotes",        false) => source.OrderBy(r => r.BabNotes),
            ("babnotes",        true)  => source.OrderByDescending(r => r.BabNotes),
            _                          => source.OrderBy(r => r.Rbse),
        };

    public CaseSearchQuery ToQuery() => new(
        Rbse: (Rbse ?? "").Replace("/", ""),
        Eartag: Eartag ?? "",
        // DBSE is stored without a slash (YYNNNNN); legacy stripped "/" before searching (SearchCase.aspx.vb).
        Dbse: (Dbse ?? "").Replace("/", ""),
        Fate: Fate ?? "",
        FinalResult: FinalResult ?? "",
        Sex: Sex ?? "",
        Survey: Survey ?? "",
        Notes: Notes ?? "",
        EarliestFormADate: ParseDate(EarliestFormADate),
        LatestFormADate: ParseDate(LatestFormADate),
        EarliestFinalResultDate: ParseDate(EarliestFinalResultDate),
        LatestFinalResultDate: ParseDate(LatestFinalResultDate),
        EarliestBirthDate: ParseDate(EarliestBirthDate),
        LatestBirthDate: ParseDate(LatestBirthDate),
        IncludeNonGbCases: IncludeNonGb,
        PassiveActive: PassiveActive ?? "",
        IsImportedCase: IsImportedCase);

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
}
