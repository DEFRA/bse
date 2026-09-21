using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class OpenCasesModel(ICaseWorkService caseWorkService) : PageModel
{
    private const int PageSize = 10;
    private static readonly StringComparer TextComparer = StringComparer.OrdinalIgnoreCase;

    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = "Rbse";
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }

    public IEnumerable<CaseWorkEntryRecord> Cases { get; private set; } = [];

    public int TotalCount => Cases.Count();
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<CaseWorkEntryRecord> PagedCases =>
        Cases.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public async Task OnGetAsync()
    {
        var openCases = await caseWorkService.GetOpenCasesAsync();
        Cases = ApplySort(openCases).ToList();

        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages) PageNumber = TotalPages;
    }

    private IEnumerable<CaseWorkEntryRecord> ApplySort(IEnumerable<CaseWorkEntryRecord> source)
    {
        var col = string.IsNullOrWhiteSpace(SortColumn) ? "Rbse" : SortColumn;

        if (SortDesc)
        {
            return col switch
            {
                "Rbse" => source.OrderByDescending(x => x.Rbse, TextComparer),
                "Survey" => source.OrderByDescending(x => x.Survey ?? string.Empty, TextComparer),
                "Barcode" => source.OrderByDescending(x => x.Barcode ?? string.Empty, TextComparer),
                "AhfReference" => source.OrderByDescending(x => x.AhfReference ?? string.Empty, TextComparer),
                "FormADate" => source.OrderByDescending(x => x.FormADate),
                "RbseDate" => source.OrderByDescending(x => x.RbseDate),
                "SlaughterDate" => source.OrderByDescending(x => x.SlaughterDate),
                "Fate" => source.OrderByDescending(x => x.Fate ?? string.Empty, TextComparer),
                "ActiveMemoDueDate" => source.OrderByDescending(x => x.ActiveMemoDueDate),
                "ActiveMemoDate" => source.OrderByDescending(x => x.ActiveMemoDate),
                "AnnexADueDate" => source.OrderByDescending(x => x.AnnexADueDate),
                "AnnexADate" => source.OrderByDescending(x => x.AnnexADate),
                "AnnexBDueDate" => source.OrderByDescending(x => x.AnnexBDueDate),
                "AnnexBDate" => source.OrderByDescending(x => x.AnnexBDate),
                "PaperworkCompleteDate" => source.OrderByDescending(x => x.PaperworkCompleteDate),
                "AnnexCDueDate" => source.OrderByDescending(x => x.AnnexCDueDate),
                "AnnexCDate" => source.OrderByDescending(x => x.AnnexCDate),
                "AnnexDDueDate" => source.OrderByDescending(x => x.AnnexDDueDate),
                "AnnexDDate" => source.OrderByDescending(x => x.AnnexDDate),
                "RegionalLab" => source.OrderByDescending(x => x.RegionalLab ?? string.Empty, TextComparer),
                "ReceivedByRegionalLabDate" => source.OrderByDescending(x => x.ReceivedByRegionalLabDate),
                "InitialReceivedDate" => source.OrderByDescending(x => x.InitialReceivedDate),
                "FinalReceivedDate" => source.OrderByDescending(x => x.FinalReceivedDate),
                "FinalSentDate" => source.OrderByDescending(x => x.FinalSentDate),
                "LabChaseDueDate" => source.OrderByDescending(x => x.LabChaseDueDate),
                "LabChasedDate" => source.OrderByDescending(x => x.LabChasedDate),
                "FinalResult" => source.OrderByDescending(x => x.FinalResult ?? string.Empty, TextComparer),
                "FinalResultDate" => source.OrderByDescending(x => x.FinalResultDate),
                "BirthDate" => source.OrderByDescending(x => x.BirthDate),
                "Post2000SentDate" => source.OrderByDescending(x => x.Post2000SentDate),
                "BarbMemoDue" => source.OrderByDescending(x => x.BarbMemoDue ?? string.Empty, TextComparer),
                "BarbMinuteSentDate" => source.OrderByDescending(x => x.BarbMinuteSentDate),
                "DataCompleteDate" => source.OrderByDescending(x => x.DataCompleteDate),
                "CaseWorkNotes" => source.OrderByDescending(x => x.CaseWorkNotes ?? string.Empty, TextComparer),
                _ => source.OrderByDescending(x => x.Rbse, TextComparer)
            };
        }

        return col switch
        {
            "Rbse" => source.OrderBy(x => x.Rbse, TextComparer),
            "Survey" => source.OrderBy(x => x.Survey ?? string.Empty, TextComparer),
            "Barcode" => source.OrderBy(x => x.Barcode ?? string.Empty, TextComparer),
            "AhfReference" => source.OrderBy(x => x.AhfReference ?? string.Empty, TextComparer),
            "FormADate" => source.OrderBy(x => x.FormADate),
            "RbseDate" => source.OrderBy(x => x.RbseDate),
            "SlaughterDate" => source.OrderBy(x => x.SlaughterDate),
            "Fate" => source.OrderBy(x => x.Fate ?? string.Empty, TextComparer),
            "ActiveMemoDueDate" => source.OrderBy(x => x.ActiveMemoDueDate),
            "ActiveMemoDate" => source.OrderBy(x => x.ActiveMemoDate),
            "AnnexADueDate" => source.OrderBy(x => x.AnnexADueDate),
            "AnnexADate" => source.OrderBy(x => x.AnnexADate),
            "AnnexBDueDate" => source.OrderBy(x => x.AnnexBDueDate),
            "AnnexBDate" => source.OrderBy(x => x.AnnexBDate),
            "PaperworkCompleteDate" => source.OrderBy(x => x.PaperworkCompleteDate),
            "AnnexCDueDate" => source.OrderBy(x => x.AnnexCDueDate),
            "AnnexCDate" => source.OrderBy(x => x.AnnexCDate),
            "AnnexDDueDate" => source.OrderBy(x => x.AnnexDDueDate),
            "AnnexDDate" => source.OrderBy(x => x.AnnexDDate),
            "RegionalLab" => source.OrderBy(x => x.RegionalLab ?? string.Empty, TextComparer),
            "ReceivedByRegionalLabDate" => source.OrderBy(x => x.ReceivedByRegionalLabDate),
            "InitialReceivedDate" => source.OrderBy(x => x.InitialReceivedDate),
            "FinalReceivedDate" => source.OrderBy(x => x.FinalReceivedDate),
            "FinalSentDate" => source.OrderBy(x => x.FinalSentDate),
            "LabChaseDueDate" => source.OrderBy(x => x.LabChaseDueDate),
            "LabChasedDate" => source.OrderBy(x => x.LabChasedDate),
            "FinalResult" => source.OrderBy(x => x.FinalResult ?? string.Empty, TextComparer),
            "FinalResultDate" => source.OrderBy(x => x.FinalResultDate),
            "BirthDate" => source.OrderBy(x => x.BirthDate),
            "Post2000SentDate" => source.OrderBy(x => x.Post2000SentDate),
            "BarbMemoDue" => source.OrderBy(x => x.BarbMemoDue ?? string.Empty, TextComparer),
            "BarbMinuteSentDate" => source.OrderBy(x => x.BarbMinuteSentDate),
            "DataCompleteDate" => source.OrderBy(x => x.DataCompleteDate),
            "CaseWorkNotes" => source.OrderBy(x => x.CaseWorkNotes ?? string.Empty, TextComparer),
            _ => source.OrderBy(x => x.Rbse, TextComparer)
        };
    }
}
