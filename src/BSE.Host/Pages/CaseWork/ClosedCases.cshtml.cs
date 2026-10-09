using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class ClosedCasesModel(ICaseWorkService caseWorkService) : PageModel
{
    private const int PageSize = 10;
    private static readonly StringComparer TextComparer = StringComparer.OrdinalIgnoreCase;

    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = "Rbse";
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public string? Rbse { get; set; }

    public IEnumerable<CaseWorkEntryRecord> Cases { get; private set; } = [];

    public int TotalCount => Cases.Count();
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<CaseWorkEntryRecord> GetPagedCases() =>
        Cases.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public async Task OnGetAsync()
    {
        var closedCases = await caseWorkService.GetClosedCasesAsync();
        Cases = ApplySort(closedCases).ToList();

        // Legacy parity: CaseWorkClosedReport.aspx's Pager.SelectGridRowForDataRow jumps to the
        // page containing ?rbse= (e.g. after Save redirects back here) instead of always page 1.
        if (!string.IsNullOrWhiteSpace(Rbse))
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

        return col switch
        {
            "Rbse" => SortDesc ? source.OrderByDescending(x => x.Rbse, TextComparer) : source.OrderBy(x => x.Rbse, TextComparer),
            "Survey" => SortDesc ? source.OrderByDescending(x => x.Survey ?? string.Empty, TextComparer) : source.OrderBy(x => x.Survey ?? string.Empty, TextComparer),
            "Barcode" => SortDesc ? source.OrderByDescending(x => x.Barcode ?? string.Empty, TextComparer) : source.OrderBy(x => x.Barcode ?? string.Empty, TextComparer),
            "AhfReference" => SortDesc ? source.OrderByDescending(x => x.AhfReference ?? string.Empty, TextComparer) : source.OrderBy(x => x.AhfReference ?? string.Empty, TextComparer),
            "FormADate" => SortDesc ? source.OrderByDescending(x => x.FormADate) : source.OrderBy(x => x.FormADate),
            "RbseDate" => SortDesc ? source.OrderByDescending(x => x.RbseDate) : source.OrderBy(x => x.RbseDate),
            "SlaughterDate" => SortDesc ? source.OrderByDescending(x => x.SlaughterDate) : source.OrderBy(x => x.SlaughterDate),
            "Fate" => SortDesc ? source.OrderByDescending(x => x.Fate ?? string.Empty, TextComparer) : source.OrderBy(x => x.Fate ?? string.Empty, TextComparer),
            "ActiveMemoDate" => SortDesc ? source.OrderByDescending(x => x.ActiveMemoDate) : source.OrderBy(x => x.ActiveMemoDate),
            "AnnexADate" => SortDesc ? source.OrderByDescending(x => x.AnnexADate) : source.OrderBy(x => x.AnnexADate),
            "AnnexBDate" => SortDesc ? source.OrderByDescending(x => x.AnnexBDate) : source.OrderBy(x => x.AnnexBDate),
            "PaperworkCompleteDate" => SortDesc ? source.OrderByDescending(x => x.PaperworkCompleteDate) : source.OrderBy(x => x.PaperworkCompleteDate),
            "AnnexCDate" => SortDesc ? source.OrderByDescending(x => x.AnnexCDate) : source.OrderBy(x => x.AnnexCDate),
            "AnnexDDate" => SortDesc ? source.OrderByDescending(x => x.AnnexDDate) : source.OrderBy(x => x.AnnexDDate),
            "RegionalLab" => SortDesc ? source.OrderByDescending(x => x.RegionalLab ?? string.Empty, TextComparer) : source.OrderBy(x => x.RegionalLab ?? string.Empty, TextComparer),
            "ReceivedByRegionalLabDate" => SortDesc ? source.OrderByDescending(x => x.ReceivedByRegionalLabDate) : source.OrderBy(x => x.ReceivedByRegionalLabDate),
            "InitialReceivedDate" => SortDesc ? source.OrderByDescending(x => x.InitialReceivedDate) : source.OrderBy(x => x.InitialReceivedDate),
            "FinalReceivedDate" => SortDesc ? source.OrderByDescending(x => x.FinalReceivedDate) : source.OrderBy(x => x.FinalReceivedDate),
            "FinalSentDate" => SortDesc ? source.OrderByDescending(x => x.FinalSentDate) : source.OrderBy(x => x.FinalSentDate),
            "LabChasedDate" => SortDesc ? source.OrderByDescending(x => x.LabChasedDate) : source.OrderBy(x => x.LabChasedDate),
            "FinalResult" => SortDesc ? source.OrderByDescending(x => x.FinalResult ?? string.Empty, TextComparer) : source.OrderBy(x => x.FinalResult ?? string.Empty, TextComparer),
            "FinalResultDate" => SortDesc ? source.OrderByDescending(x => x.FinalResultDate) : source.OrderBy(x => x.FinalResultDate),
            "BirthDate" => SortDesc ? source.OrderByDescending(x => x.BirthDate) : source.OrderBy(x => x.BirthDate),
            "Post2000SentDate" => SortDesc ? source.OrderByDescending(x => x.Post2000SentDate) : source.OrderBy(x => x.Post2000SentDate),
            "BarbMinuteSentDate" => SortDesc ? source.OrderByDescending(x => x.BarbMinuteSentDate) : source.OrderBy(x => x.BarbMinuteSentDate),
            "DataCompleteDate" => SortDesc ? source.OrderByDescending(x => x.DataCompleteDate) : source.OrderBy(x => x.DataCompleteDate),
            "CaseWorkNotes" => SortDesc ? source.OrderByDescending(x => x.CaseWorkNotes ?? string.Empty, TextComparer) : source.OrderBy(x => x.CaseWorkNotes ?? string.Empty, TextComparer),
            _ => SortDesc ? source.OrderByDescending(x => x.Rbse, TextComparer) : source.OrderBy(x => x.Rbse, TextComparer)
        };
    }
}
