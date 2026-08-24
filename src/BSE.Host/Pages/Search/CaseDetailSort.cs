using BSE.Modules.Search.Models;

namespace BSE.Host.Pages.Search;

internal static class CaseDetailSort
{
    internal static IEnumerable<CaseDetailSearchResult> Apply(
        IReadOnlyList<CaseDetailSearchResult> source,
        string? sortColumn,
        bool sortDesc) =>
        (sortColumn?.ToLowerInvariant(), sortDesc) switch
        {
            ("cphh",                false) => source.OrderBy(r => r.Cphh),
            ("cphh",                true)  => source.OrderByDescending(r => r.Cphh),
            ("sex",                 false) => source.OrderBy(r => r.Sex),
            ("sex",                 true)  => source.OrderByDescending(r => r.Sex),
            ("eartag",              false) => source.OrderBy(r => r.Eartag),
            ("eartag",              true)  => source.OrderByDescending(r => r.Eartag),
            ("birthdate",           false) => source.OrderBy(r => r.BirthDate),
            ("birthdate",           true)  => source.OrderByDescending(r => r.BirthDate),
            ("origin",              false) => source.OrderBy(r => r.Origin),
            ("origin",              true)  => source.OrderByDescending(r => r.Origin),
            ("purchasedate",        false) => source.OrderBy(r => r.PurchaseDate),
            ("purchasedate",        true)  => source.OrderByDescending(r => r.PurchaseDate),
            ("purchaseageinmonths", false) => source.OrderBy(r => r.PurchaseAgeInMonths),
            ("purchaseageinmonths", true)  => source.OrderByDescending(r => r.PurchaseAgeInMonths),
            ("onsetdate",           false) => source.OrderBy(r => r.OnsetDate),
            ("onsetdate",           true)  => source.OrderByDescending(r => r.OnsetDate),
            ("formadate",           false) => source.OrderBy(r => r.FormADate),
            ("formadate",           true)  => source.OrderByDescending(r => r.FormADate),
            ("slaughterdate",       false) => source.OrderBy(r => r.SlaughterDate),
            ("slaughterdate",       true)  => source.OrderByDescending(r => r.SlaughterDate),
            ("finalresultdate",     false) => source.OrderBy(r => r.FinalResultDate),
            ("finalresultdate",     true)  => source.OrderByDescending(r => r.FinalResultDate),
            ("onsetageinmonths",    false) => source.OrderBy(r => r.OnsetAgeInMonths),
            ("onsetageinmonths",    true)  => source.OrderByDescending(r => r.OnsetAgeInMonths),
            ("fate",                false) => source.OrderBy(r => r.Fate),
            ("fate",                true)  => source.OrderByDescending(r => r.Fate),
            ("finalresult",         false) => source.OrderBy(r => r.FinalResult),
            ("finalresult",         true)  => source.OrderByDescending(r => r.FinalResult),
            ("survey",              false) => source.OrderBy(r => r.Survey),
            ("survey",              true)  => source.OrderByDescending(r => r.Survey),
            ("casestatus",          false) => source.OrderBy(r => r.CaseStatus),
            ("casestatus",          true)  => source.OrderByDescending(r => r.CaseStatus),
            ("timeelapsed",         false) => source.OrderBy(r => r.TimeElapsed),
            ("timeelapsed",         true)  => source.OrderByDescending(r => r.TimeElapsed),
            _                              => source.OrderBy(r => r.Rbse),
        };
}
