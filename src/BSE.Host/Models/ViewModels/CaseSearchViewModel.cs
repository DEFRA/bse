using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class CaseSearchViewModel
{
    public const int PageSize = 50;

    // --- Filter inputs ---
    [RegularExpression("^(?:\\d{9})?$", ErrorMessage = "Enter RBSE as 9 digits (for example 000000001).")]
    public string Rbse { get; set; } = "";

    public string Eartag { get; set; } = "";

    [RegularExpression("^(?:\\d{2}(/)?\\d{5})?$", ErrorMessage = "Enter DBSE in the form YY/NNNNN or YYNNNNN.")]
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

    // --- Pagination ---
    public int PageNumber { get; set; } = 1;

    // --- Results ---
    public IReadOnlyList<CaseSearchResult> Results { get; set; } = [];
    public bool HasSearched { get; set; }

    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<CaseSearchResult> PagedResults =>
        Results.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public CaseSearchQuery ToQuery() => new(
        Rbse: Rbse ?? "",
        Eartag: Eartag ?? "",
        Dbse: Dbse ?? "",
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
