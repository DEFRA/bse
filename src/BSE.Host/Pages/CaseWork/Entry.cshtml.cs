using BSE.Host.Services;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "DataEntry")]
public class CaseWorkEntryModel(ICaseWorkService caseWorkService, ICurrentUserService currentUserService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public CaseWorkEntryRecord? Entry { get; private set; }

    // Form fields
    [BindProperty] public string? Barcode { get; set; }
    [BindProperty] public string? AhfReference { get; set; }
    [BindProperty] public DateTime? RbseDate { get; set; }
    [BindProperty] public DateTime? PurchaserBse1ReceivedDate { get; set; }
    [BindProperty] public DateTime? BreederBse1ReceivedDate { get; set; }
    [BindProperty] public DateTime? Vendor1Bse1ReceivedDate { get; set; }
    [BindProperty] public DateTime? HomebredBse1ReceivedDate { get; set; }
    [BindProperty] public DateTime? SummarySheetReceivedDate { get; set; }
    [BindProperty] public DateTime? PaperworkCompleteDate { get; set; }
    [BindProperty] public DateTime? ActiveMemoDate { get; set; }
    [BindProperty] public DateTime? AnnexADate { get; set; }
    [BindProperty] public DateTime? AnnexBDate { get; set; }
    [BindProperty] public DateTime? AnnexCDate { get; set; }
    [BindProperty] public DateTime? AnnexDDate { get; set; }
    [BindProperty] public string? RegionalLab { get; set; }
    [BindProperty] public DateTime? ReceivedByRegionalLabDate { get; set; }
    [BindProperty] public DateTime? InitialReceivedDate { get; set; }
    [BindProperty] public DateTime? FinalReceivedDate { get; set; }
    [BindProperty] public DateTime? FinalSentDate { get; set; }
    [BindProperty] public DateTime? LabChasedDate { get; set; }
    [BindProperty] public DateTime? BarbMinuteSentDate { get; set; }
    [BindProperty] public DateTime? Post2000SentDate { get; set; }
    [BindProperty] public string? CaseWorkNotes { get; set; }
    [BindProperty] public DateTime? DataCompleteDate { get; set; }
    [BindProperty] public bool IsCaseClosed { get; set; }
    [BindProperty] public string? TseTestingSite { get; set; }
    [BindProperty] public DateTime? SamplingDate { get; set; }
    [BindProperty] public int? AhroId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Entry = await caseWorkService.GetCaseWorkEntryAsync(Rbse);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = await currentUserService.GetUserIdAsync();

        var command = new EditCaseWorkEntryCommand(
            Rbse: Rbse,
            Barcode: Barcode,
            AhfReference: AhfReference,
            PurchaserBse1ReceivedDate: PurchaserBse1ReceivedDate,
            BreederBse1ReceivedDate: BreederBse1ReceivedDate,
            Vendor1Bse1ReceivedDate: Vendor1Bse1ReceivedDate,
            HomebredBse1ReceivedDate: HomebredBse1ReceivedDate,
            SummarySheetReceivedDate: SummarySheetReceivedDate,
            PaperworkCompleteDate: PaperworkCompleteDate,
            ActiveMemoDate: ActiveMemoDate,
            AnnexADate: AnnexADate,
            AnnexBDate: AnnexBDate,
            AnnexCDate: AnnexCDate,
            AnnexDDate: AnnexDDate,
            RegionalLab: RegionalLab,
            ReceivedByRegionalLabDate: ReceivedByRegionalLabDate,
            InitialReceivedDate: InitialReceivedDate,
            FinalReceivedDate: FinalReceivedDate,
            FinalSentDate: FinalSentDate,
            LabChasedDate: LabChasedDate,
            BarbMinuteSentDate: BarbMinuteSentDate,
            Post2000SentDate: Post2000SentDate,
            CaseWorkNotes: CaseWorkNotes,
            DataCompleteDate: DataCompleteDate,
            IsCaseClosed: IsCaseClosed,
            UserId: userId,
            TseTestingSite: TseTestingSite,
            SamplingDate: SamplingDate,
            AhroId: AhroId);

        await caseWorkService.EditCaseWorkEntryAsync(command);

        TempData["Success"] = $"Case work entry for {Rbse} has been updated.";
        return RedirectToPage("/CaseWork/Menu", new { rbse = Rbse });
    }
}
