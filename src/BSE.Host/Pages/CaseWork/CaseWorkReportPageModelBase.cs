using BSE.Modules.CaseWork.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

/// <summary>Sorting, paging and selected-row handling shared by the open and closed casework reports.</summary>
public abstract class CaseWorkReportPageModelBase : PageModel
{
    private const int PageSize = 10;
    private static readonly StringComparer TextComparer = StringComparer.OrdinalIgnoreCase;

    private delegate IOrderedEnumerable<CaseWorkEntryRecord> SortKey(IEnumerable<CaseWorkEntryRecord> source, bool descending);

    private static SortKey Text(Func<CaseWorkEntryRecord, string?> selector) =>
        (source, desc) => desc
            ? source.OrderByDescending(x => selector(x) ?? string.Empty, TextComparer)
            : source.OrderBy(x => selector(x) ?? string.Empty, TextComparer);

    private static SortKey Date(Func<CaseWorkEntryRecord, DateTime?> selector) =>
        (source, desc) => desc ? source.OrderByDescending(selector) : source.OrderBy(selector);

    private static readonly Dictionary<string, SortKey> CommonKeys = new()
    {
        ["Rbse"] = Text(x => x.Rbse),
        ["Survey"] = Text(x => x.Survey),
        ["Barcode"] = Text(x => x.Barcode),
        ["AhfReference"] = Text(x => x.AhfReference),
        ["FormADate"] = Date(x => x.FormADate),
        ["RbseDate"] = Date(x => x.RbseDate),
        ["SlaughterDate"] = Date(x => x.SlaughterDate),
        ["Fate"] = Text(x => x.Fate),
        ["ActiveMemoDate"] = Date(x => x.ActiveMemoDate),
        ["AnnexADate"] = Date(x => x.AnnexADate),
        ["AnnexBDate"] = Date(x => x.AnnexBDate),
        ["PaperworkCompleteDate"] = Date(x => x.PaperworkCompleteDate),
        ["AnnexCDate"] = Date(x => x.AnnexCDate),
        ["AnnexDDate"] = Date(x => x.AnnexDDate),
        ["RegionalLab"] = Text(x => x.RegionalLab),
        ["ReceivedByRegionalLabDate"] = Date(x => x.ReceivedByRegionalLabDate),
        ["InitialReceivedDate"] = Date(x => x.InitialReceivedDate),
        ["FinalReceivedDate"] = Date(x => x.FinalReceivedDate),
        ["FinalSentDate"] = Date(x => x.FinalSentDate),
        ["LabChasedDate"] = Date(x => x.LabChasedDate),
        ["FinalResult"] = Text(x => x.FinalResult),
        ["FinalResultDate"] = Date(x => x.FinalResultDate),
        ["BirthDate"] = Date(x => x.BirthDate),
        ["Post2000SentDate"] = Date(x => x.Post2000SentDate),
        ["BarbMinuteSentDate"] = Date(x => x.BarbMinuteSentDate),
        ["DataCompleteDate"] = Date(x => x.DataCompleteDate),
        ["CaseWorkNotes"] = Text(x => x.CaseWorkNotes),
    };

    // Due-date columns exist only on the open-cases report.
    private static readonly Dictionary<string, SortKey> DueDateKeys = new()
    {
        ["ActiveMemoDueDate"] = Date(x => x.ActiveMemoDueDate),
        ["AnnexADueDate"] = Date(x => x.AnnexADueDate),
        ["AnnexBDueDate"] = Date(x => x.AnnexBDueDate),
        ["AnnexCDueDate"] = Date(x => x.AnnexCDueDate),
        ["AnnexDDueDate"] = Date(x => x.AnnexDDueDate),
        ["LabChaseDueDate"] = Date(x => x.LabChaseDueDate),
        ["BarbMemoDue"] = Text(x => x.BarbMemoDue),
    };

    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = "Rbse";
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public string? Rbse { get; set; }

    public IEnumerable<CaseWorkEntryRecord> Cases { get; private set; } = [];

    public int TotalCount => Cases.Count();
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<CaseWorkEntryRecord> PagedCases() =>
        Cases.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    protected virtual bool HasDueDateColumns => false;

    protected void LoadCases(IEnumerable<CaseWorkEntryRecord> source)
    {
        Cases = ApplySort(source).ToList();

        // Legacy parity: the Pager.SelectGridRowForDataRow of the legacy report jumps to the
        // page containing ?rbse= (e.g. after Save redirects back here) instead of always page 1.
        // Pagination links carry ?rbse= forward to keep the row highlighted, so an explicit page
        // number always wins — otherwise every page link snaps straight back to the selected row.
        if (HttpContext?.Request.Query.ContainsKey(nameof(PageNumber)) != true && !string.IsNullOrWhiteSpace(Rbse))
        {
            var index = Cases.ToList().FindIndex(c => string.Equals(c.Rbse, Rbse, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) PageNumber = index / PageSize + 1;
        }

        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages) PageNumber = TotalPages;
    }

    private IEnumerable<CaseWorkEntryRecord> ApplySort(IEnumerable<CaseWorkEntryRecord> source)
    {
        var col = string.IsNullOrWhiteSpace(SortColumn) ? "Rbse" : SortColumn;

        if (!CommonKeys.TryGetValue(col, out var key)
            && !(HasDueDateColumns && DueDateKeys.TryGetValue(col, out key)))
        {
            key = CommonKeys["Rbse"];
        }

        return key(source, SortDesc);
    }
}
