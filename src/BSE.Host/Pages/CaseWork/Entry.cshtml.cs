using BSE.Host.Services;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using BSE.Modules.ReferenceData.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class CaseWorkEntryModel(
    ICaseWorkService caseWorkService,
    ICurrentUserService currentUserService,
    ILookupRepository lookupRepository,
    ITestRepository testRepository) : PageModel
{
    private const string SurveyFallenStock = "fallen stock";
    private const string SurveySurveillanceCohort = "surveillance cohort";

    /// <summary>Legacy sentinel: a SamplingDate of DateTime.MaxValue means "unknown".</summary>
    private static readonly DateTime SamplingDateUnknown = DateTime.MaxValue;

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public CaseWorkEntryRecord? Entry { get; private set; }

    // ── Read-only display values ──────────────────────────────────────────────
    public string? FateDescription { get; private set; }
    public string? OriginDescription { get; private set; }
    public string? SurveyDescription { get; private set; }
    public string? FinalResultDescription { get; private set; }

    // ── Lookup lists ──────────────────────────────────────────────────────────
    public IReadOnlyList<SelectListItem> RegionalLabOptions { get; private set; } = [];
    public IReadOnlyList<SelectListItem> AhroOptions { get; private set; } = [];
    public IReadOnlyList<SelectListItem> TseTestingSiteOptions { get; private set; } = [];

    /// <summary>TSE site, sampling date and AHRO only apply to Fallen Stock and Surveillance Cohort surveys.</summary>
    public bool ShowTseFields { get; private set; }

    /// <summary>Born after Dec 2000, died or slaughtered, and a non-negative test result — DEFRA must be informed.</summary>
    public bool ShowPost2000Warning { get; private set; }

    // ── Minute send-button availability ───────────────────────────────────────
    public string? AnnexADisabledReason { get; private set; }
    public string? AnnexBDisabledReason { get; private set; }
    public string? AnnexCDisabledReason { get; private set; }
    public string? AnnexDDisabledReason { get; private set; }

    // ── Editable form fields ──────────────────────────────────────────────────
    [BindProperty] public string? Barcode { get; set; }
    [BindProperty] public string? AhfReference { get; set; }
    [BindProperty] public string? RegionalLab { get; set; }
    [BindProperty] public DateTime? ReceivedByRegionalLabDate { get; set; }
    [BindProperty] public DateTime? InitialReceivedDate { get; set; }
    [BindProperty] public DateTime? FinalReceivedDate { get; set; }
    [BindProperty] public DateTime? FinalSentDate { get; set; }
    [BindProperty] public DateTime? PurchaserBse1ReceivedDate { get; set; }
    [BindProperty] public DateTime? BreederBse1ReceivedDate { get; set; }
    [BindProperty] public DateTime? Vendor1Bse1ReceivedDate { get; set; }
    [BindProperty] public DateTime? HomebredBse1ReceivedDate { get; set; }
    [BindProperty] public DateTime? SummarySheetReceivedDate { get; set; }
    [BindProperty] public DateTime? PaperworkCompleteDate { get; set; }
    [BindProperty] public DateTime? DataCompleteDate { get; set; }
    [BindProperty] public string? TseTestingSite { get; set; }
    [BindProperty] public DateTime? SamplingDate { get; set; }
    [BindProperty] public bool SamplingDateIsUnknown { get; set; }
    [BindProperty] public DateTime? LabChasedDate { get; set; }
    [BindProperty] public DateTime? BarbMinuteSentDate { get; set; }
    [BindProperty] public DateTime? Post2000SentDate { get; set; }
    [BindProperty] public string? CaseWorkNotes { get; set; }
    [BindProperty] public int? AhroId { get; set; }

    /// <summary>Legacy checkbox: always renders unchecked, and toggles the case between open and closed.</summary>
    [BindProperty] public bool ToggleCaseStatus { get; set; }

    /// <summary>Round-trips the on-load closed state so the toggle can be interpreted on post.</summary>
    [BindProperty] public bool ClosedOnLoad { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Entry is null) return Page();

        Barcode = Entry.Barcode;
        AhfReference = Entry.AhfReference;
        RegionalLab = Entry.RegionalLab?.Trim();
        ReceivedByRegionalLabDate = Entry.ReceivedByRegionalLabDate;
        InitialReceivedDate = Entry.InitialReceivedDate;
        FinalReceivedDate = Entry.FinalReceivedDate;
        FinalSentDate = Entry.FinalSentDate;
        PurchaserBse1ReceivedDate = Entry.PurchaserBse1ReceivedDate;
        BreederBse1ReceivedDate = Entry.BreederBse1ReceivedDate;
        Vendor1Bse1ReceivedDate = Entry.Vendor1Bse1ReceivedDate;
        HomebredBse1ReceivedDate = Entry.HomebredBse1ReceivedDate;
        SummarySheetReceivedDate = Entry.SummarySheetReceivedDate;
        PaperworkCompleteDate = Entry.PaperworkCompleteDate;
        DataCompleteDate = Entry.DataCompleteDate;
        TseTestingSite = Entry.TseTestingSite;
        AhroId = Entry.Ahro;
        LabChasedDate = Entry.LabChasedDate;
        BarbMinuteSentDate = Entry.BarbMinuteSentDate;
        Post2000SentDate = Entry.Post2000SentDate;
        CaseWorkNotes = Entry.CaseWorkNotes;
        ClosedOnLoad = Entry.IsCaseClosed == true;

        if (Entry.SamplingDate == SamplingDateUnknown)
            SamplingDateIsUnknown = true;
        else
            SamplingDate = Entry.SamplingDate;

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        await LoadAsync();
        if (Entry is null) return NotFound();

        Validate();
        if (!ModelState.IsValid) return Page();

        var closedAfterEdit = await SaveAsync();

        TempData["Success"] = $"Case work entry for {Rbse} has been updated.";
        return RedirectToPage(closedAfterEdit ? "/CaseWork/ClosedCases" : "/CaseWork/OpenCases");
    }

    public async Task<IActionResult> OnPostSendMinuteAsync(string minuteType)
    {
        if (!IsSendableMinuteType(minuteType)) return BadRequest();

        await LoadAsync();
        if (Entry is null) return NotFound();

        Validate();
        if (!ModelState.IsValid) return Page();

        var alreadySent = SentDateFor(minuteType) is not null;

        await SaveAsync();

        if (!alreadySent)
            await caseWorkService.SetMinuteSentDateAsync(Rbse, minuteType);

        // Fallen Stock and Surveillance Cohort surveys use the FS variant of the active memo.
        var routedType = minuteType == "ActiveMemo" && ShowTseFields ? "AMFS" : minuteType;

        return RedirectToPage("/CaseWork/Minute", new { rbse = Rbse, type = routedType });
    }

    /// <summary>Legacy rule: a supplied date must fall between the day after the RBSE Date and today.</summary>
    private void Validate()
    {
        const string afterRbse = "You must enter a date in the past but after the RBSE Date.";
        var today = DateTime.Today;

        if (Entry?.RbseDate is DateTime rbseDate)
        {
            var earliest = rbseDate.Date.AddDays(1);

            RequireRange(nameof(PurchaserBse1ReceivedDate), PurchaserBse1ReceivedDate, earliest, today, afterRbse);
            RequireRange(nameof(BreederBse1ReceivedDate), BreederBse1ReceivedDate, earliest, today, afterRbse);
            RequireRange(nameof(Vendor1Bse1ReceivedDate), Vendor1Bse1ReceivedDate, earliest, today, afterRbse);
            RequireRange(nameof(HomebredBse1ReceivedDate), HomebredBse1ReceivedDate, earliest, today, afterRbse);
            RequireRange(nameof(SummarySheetReceivedDate), SummarySheetReceivedDate, earliest, today, afterRbse);
            RequireRange(nameof(PaperworkCompleteDate), PaperworkCompleteDate, earliest, today, afterRbse);
            RequireRange(nameof(FinalReceivedDate), FinalReceivedDate, earliest, today, afterRbse);
            RequireRange(nameof(LabChasedDate), LabChasedDate, earliest, today, afterRbse);
            RequireRange(nameof(BarbMinuteSentDate), BarbMinuteSentDate, earliest, today, afterRbse);
            RequireRange(nameof(Post2000SentDate), Post2000SentDate, earliest, today, afterRbse);
            RequireRange(nameof(DataCompleteDate), DataCompleteDate, earliest, today, afterRbse);
        }

        if (FinalReceivedDate is DateTime finalReceived)
        {
            RequireRange(nameof(FinalSentDate), FinalSentDate, finalReceived.Date, today,
                "You must enter a date in the past but after (or equal to) the Final Received Date.");
        }

        if (!ShowTseFields) return;

        if (string.IsNullOrWhiteSpace(TseTestingSite))
            ModelState.AddModelError(nameof(TseTestingSite), "You must select a TSE testing site.");

        if (!SamplingDateIsUnknown && SamplingDate is null)
            ModelState.AddModelError(nameof(SamplingDate), "You must enter a sampling date, or tick Unknown.");
    }

    private void RequireRange(string key, DateTime? value, DateTime earliest, DateTime latest, string message)
    {
        if (value is null) return;
        if (value.Value.Date < earliest || value.Value.Date > latest)
            ModelState.AddModelError(key, message);
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        var record = await caseWorkService.GetCaseWorkEntryAsync(Rbse);
        return RedirectToPage(record?.IsCaseClosed == true ? "/CaseWork/ClosedCases" : "/CaseWork/OpenCases");
    }

    private async Task<bool> SaveAsync()
    {
        var userId = await currentUserService.GetUserIdAsync();

        // Legacy interprets the checkbox relative to the state the page was loaded in.
        var isCaseClosed = ClosedOnLoad ? !ToggleCaseStatus : ToggleCaseStatus;

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
            // Minute dates are set by the Send buttons, never edited directly.
            ActiveMemoDate: Entry?.ActiveMemoDate,
            AnnexADate: Entry?.AnnexADate,
            AnnexBDate: Entry?.AnnexBDate,
            AnnexCDate: Entry?.AnnexCDate,
            AnnexDDate: Entry?.AnnexDDate,
            RegionalLab: string.IsNullOrWhiteSpace(RegionalLab) ? null : RegionalLab,
            ReceivedByRegionalLabDate: ReceivedByRegionalLabDate,
            InitialReceivedDate: InitialReceivedDate,
            FinalReceivedDate: FinalReceivedDate,
            FinalSentDate: FinalSentDate,
            LabChasedDate: LabChasedDate,
            BarbMinuteSentDate: BarbMinuteSentDate,
            Post2000SentDate: Post2000SentDate,
            CaseWorkNotes: CaseWorkNotes,
            DataCompleteDate: DataCompleteDate,
            IsCaseClosed: isCaseClosed,
            UserId: userId,
            TseTestingSite: string.IsNullOrWhiteSpace(TseTestingSite) ? null : TseTestingSite,
            SamplingDate: SamplingDateIsUnknown ? SamplingDateUnknown : SamplingDate,
            AhroId: AhroId);

        await caseWorkService.EditCaseWorkEntryAsync(command);
        return isCaseClosed;
    }

    private DateTime? SentDateFor(string minuteType) => minuteType switch
    {
        "ActiveMemo" => Entry?.ActiveMemoDate,
        "AnnexA" => Entry?.AnnexADate,
        "AnnexB" => Entry?.AnnexBDate,
        "AnnexC" => Entry?.AnnexCDate,
        "AnnexD" => Entry?.AnnexDDate,
        _ => null,
    };

    private static bool IsSendableMinuteType(string minuteType) =>
        minuteType is "ActiveMemo" or "AnnexA" or "AnnexB" or "AnnexC" or "AnnexD";

    private async Task LoadAsync()
    {
        Entry = await caseWorkService.GetCaseWorkEntryAsync(Rbse);
        if (Entry is null) return;

        var regionalLabs = (await lookupRepository.GetRegionalLabsAsync()).ToList();

        RegionalLabOptions = regionalLabs
            .Select(l => new SelectListItem(l.Description, l.Code?.Trim()))
            .ToList();

        AhroOptions = (await lookupRepository.GetAHROAsync())
            .Select(a => new SelectListItem(a.Name, a.Id.ToString()))
            .ToList();

        TseTestingSiteOptions = (await lookupRepository.GetTSETestingSitesAsync())
            .Select(s => new SelectListItem(s.Name, s.Name))
            .ToList();

        FateDescription = (await lookupRepository.GetCaseFatesAsync())
            .FirstOrDefault(f => Matches(f.Code, Entry.Fate))?.Description;

        OriginDescription = (await lookupRepository.GetAnimalOriginsAsync())
            .FirstOrDefault(o => Matches(o.Code, Entry.Origin))?.Description;

        SurveyDescription = (await lookupRepository.GetSurveysAsync())
            .FirstOrDefault(s => Matches(s.Code, Entry.Survey))?.Description;

        FinalResultDescription = (await lookupRepository.GetTestResultsAsync())
            .FirstOrDefault(r => Matches(r.Code, Entry.FinalResult))?.Description;

        ShowTseFields = string.Equals(SurveyDescription, SurveyFallenStock, StringComparison.OrdinalIgnoreCase)
            || string.Equals(SurveyDescription, SurveySurveillanceCohort, StringComparison.OrdinalIgnoreCase);

        SetMinuteSendRules();
        await SetPost2000WarningAsync();
    }

    private static bool Matches(string? lookupCode, string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && string.Equals(lookupCode?.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase);

    private void SetMinuteSendRules()
    {
        if (Entry is null) return;

        if (Entry.RbseDate is not null && Entry.RbseDate >= DateTime.Today)
        {
            AnnexADisabledReason = "Annex A cannot be sent until after the RBSE Date";
            AnnexBDisabledReason = "Annex B cannot be sent until after the RBSE Date";
            AnnexCDisabledReason = "Annex C cannot be sent until after the RBSE Date";
            AnnexDDisabledReason = "Annex D cannot be sent until after the RBSE Date";
            return;
        }

        if (Entry.AnnexADate is null)
            AnnexBDisabledReason = "Annex B cannot be sent before Annex A";

        if (Entry.AnnexCDate is null)
            AnnexDDisabledReason = "Annex D cannot be sent before Annex C";
    }

    private async Task SetPost2000WarningAsync()
    {
        if (Entry?.BirthDate is null || Entry.BirthDate <= new DateTime(2000, 12, 31)) return;
        if (Entry.Fate?.Trim() is not ("DIED" or "SL")) return;

        var tests = await testRepository.GetByRbseAsync(Rbse);
        ShowPost2000Warning = tests.Any(t => !string.Equals(t.TestResult?.Trim(), "Neg", StringComparison.OrdinalIgnoreCase));
    }
}
