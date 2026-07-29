using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class CaseSearchViewModel
{
    // --- Filter inputs ---
    public string Rbse { get; set; } = "";
    public string Eartag { get; set; } = "";
    public string Dbse { get; set; } = "";
    public string Fate { get; set; } = "";
    public string FinalResult { get; set; } = "";
    public string Sex { get; set; } = "";
    public string Survey { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool IncludeNonGb { get; set; }
    public DateTime? EarliestFormADate { get; set; }
    public DateTime? LatestFormADate { get; set; }
    public DateTime? EarliestFinalResultDate { get; set; }
    public DateTime? LatestFinalResultDate { get; set; }
    public DateTime? EarliestBirthDate { get; set; }
    public DateTime? LatestBirthDate { get; set; }

    // --- Results ---
    public IReadOnlyList<CaseSearchResult> Results { get; set; } = [];
    public bool HasSearched { get; set; }

    public CaseSearchQuery ToQuery() => new(
        Rbse: Rbse,
        Eartag: Eartag,
        Dbse: Dbse,
        Fate: Fate,
        FinalResult: FinalResult,
        Sex: Sex,
        Survey: Survey,
        Notes: Notes,
        EarliestFormADate: EarliestFormADate,
        LatestFormADate: LatestFormADate,
        EarliestFinalResultDate: EarliestFinalResultDate,
        LatestFinalResultDate: LatestFinalResultDate,
        EarliestBirthDate: EarliestBirthDate,
        LatestBirthDate: LatestBirthDate,
        IncludeNonGbCases: IncludeNonGb);
}
