using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "CaseEntryAccess")]
[Authorize(Policy = "DataEntry")]
public class DefraEditModel(
    ICaseService caseService,
    ICurrentUserService currentUserService,
    ILookupDataService lookups,
    ICaseWorkRepository caseWorkRepository,
    IBatchRepository batchRepository,
    IConfiguration configuration) : PageModel, ICaseEditFormPage
{
    private const string BirthDateKey = "Case.BirthDate";

    private const string RowStampKey = "DefraEdit_RowStamp_{0}";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public bool Embed { get; set; }

    [BindProperty]
    public CaseEditViewModel Case { get; set; } = new();

    public string? ConcurrencyError { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public bool IsNonGbCase { get; private set; }

    public IEnumerable<ILookupItem> FateOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> SurveyOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> ReportedLocationOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> BirthDateSourceOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> ValuationAgeOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> CaseTypeOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        var record = await caseService.GetCaseAsync(Rbse);
        if (record is null)
        {
            TempData["Warning"] = $"Case '{Rbse}' not found.";
            return RedirectToPage("/Home");
        }

        TempData[string.Format(RowStampKey, Rbse)] = Convert.ToBase64String(record.RowStamp ?? []);
        Case = CaseEditViewModel.FromRecord(record);
        IsNonGbCase = record.IsNonGbCase;

        var caseWork = await caseWorkRepository.GetByRbseAsync(Rbse);
        if (caseWork is not null)
            Case.ApplyCaseWork(caseWork);

        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        await Task.WhenAll(LoadLookupsAsync(), batchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        var currentRecord = await caseService.GetCaseAsync(Rbse);
        if (currentRecord is null)
        {
            TempData["Warning"] = $"Case '{Rbse}' not found.";
            return RedirectToPage("/Home");
        }

        IsNonGbCase = currentRecord.IsNonGbCase;

        // Legacy CaseEntryDEFRA: Form A date is read-only for non-GB cases.
        if (IsNonGbCase)
            Case.FormADate = currentRecord.FormADate;

        await LoadLookupsAsync();

        // Mirrors legacy CaseEntryDEFRA: clear dependent dates when anchor date is removed
        if (!Case.FormADate.HasValue)
        {
            Case.FormAResubmittedDate = null;
            Case.FormBDate            = null;
            Case.FormCDate            = null;
            Case.Fate                 = null;
        }
        else if (!Case.FormBDate.HasValue)
        {
            Case.FormCDate = null;
            Case.Fate      = null;
        }

        // Mirrors legacy CaseEntryDEFRA: default SlaughterDate to FormBDate when not yet set by VLA
        if (Case.FormBDate.HasValue && !Case.SlaughterDate.HasValue)
            Case.SlaughterDate = Case.FormBDate;

        ValidateDomainRules();

        if (!ModelState.IsValid)
            return Page();

        var rowStampBase64 = TempData[string.Format(RowStampKey, Rbse)]?.ToString();
        if (string.IsNullOrEmpty(rowStampBase64))
        {
            ConcurrencyError = "Session expired — please reload the page and try again.";
            return Page();
        }

        var rowStamp = Convert.FromBase64String(rowStampBase64);
        var editCommand = Case.ToEditCommand(rowStamp);
        var command = new EditCaseDetailsCommand(editCommand, Clinical: null, Bab: null, DamSire: null);

        var userId = await currentUserService.GetUserIdAsync();
        var result = await caseService.EditCaseAsync(command, userId);

        if (result == EditCaseResult.ConcurrencyConflict)
        {
            ConcurrencyError = "Another user has modified this case since you loaded it. " +
                               "Please reload to get the latest version and apply your changes again.";
            var current = await caseService.GetCaseAsync(Rbse);
            if (current is not null)
                TempData[string.Format(RowStampKey, Rbse)] = Convert.ToBase64String(current.RowStamp ?? []);
            return Page();
        }

        if (result != EditCaseResult.Success)
        {
            var message = result switch
            {
                EditCaseResult.RbseNotFound    => $"Case '{Rbse}' not found.",
                EditCaseResult.AuditLogError   => "Audit log error during update.",
                EditCaseResult.PostUpdateError => "Database error after update.",
                _                              => $"Update failed: {result}"
            };
            ModelState.AddModelError("", message);
            return Page();
        }

        if (Case.HasCaseWork)
        {
            var cwCommand = new EditCaseWorkCommand(
                Rbse:                       Rbse,
                RbseDate:                   Case.RbseDate,
                Barcode:                    Case.Barcode,
                AhfReference:               Case.AhfReference,
                PurchaserBse1ReceivedDate:  Case.PurchaserBse1ReceivedDate,
                BreederBse1ReceivedDate:    Case.BreederBse1ReceivedDate,
                Vendor1Bse1ReceivedDate:    Case.Vendor1Bse1ReceivedDate,
                HomebredBse1ReceivedDate:   Case.HomebredBse1ReceivedDate,
                SummarySheetReceivedDate:   Case.SummarySheetReceivedDate,
                PaperworkCompleteDate:      Case.PaperworkCompleteDate);

            await caseWorkRepository.EditAsync(cwCommand);
        }

        TempData["Success"] = $"Case {Rbse} has been updated.";
        return RedirectToPage("/Case/Edit", new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var fateTask             = lookups.GetLookupAsync(LookupTableId.CaseFate);
        var surveyTask           = lookups.GetLookupAsync(LookupTableId.Survey);
        var reportedLocationTask = lookups.GetLookupAsync(LookupTableId.ReportedLocation);
        var birthDateSourceTask  = lookups.GetLookupAsync(LookupTableId.BirthDateSource);
        var valuationAgeTask     = lookups.GetLookupAsync(LookupTableId.ValuationAge);
        var caseTypeTask         = lookups.GetLookupAsync(LookupTableId.CaseType);

        await Task.WhenAll(fateTask, surveyTask, reportedLocationTask,
                           birthDateSourceTask, valuationAgeTask, caseTypeTask);

        FateOptions             = await fateTask;
        SurveyOptions           = await surveyTask;
        ReportedLocationOptions = await reportedLocationTask;
        BirthDateSourceOptions  = await birthDateSourceTask;
        ValuationAgeOptions     = await valuationAgeTask;
        CaseTypeOptions         = await caseTypeTask;
    }

    // Mirrors legacy UpdateSessionWithCaseDetails validation (FormXDateValid, DateOfBirthValid, BSE1ReceivedDateValid)
    private void ValidateDomainRules()
    {
        var today = DateTime.Today;

        ValidateEartag();
        ValidateFormADates(today);
        ValidateFormBAndCDates(today);
        ValidateBirthDate(today);
        ValidateBse1ReceiptDates(today);
    }

    private void ValidateEartag()
    {
        if (string.IsNullOrWhiteSpace(Case.EartagCountry)
            && string.IsNullOrWhiteSpace(Case.EartagHerdmark)
            && string.IsNullOrWhiteSpace(Case.Eartag))
        {
            ModelState.AddModelError("Case.EartagCountry", "Enter an eartag.");
            return;
        }

        // Mirrors BSELib.Eartag.GetEartag + .ErrorCode check from ThreePartEartag.Validate()
        var eartagError = EartagValidator.Validate(Case.EartagCountry, Case.EartagHerdmark, Case.Eartag);
        if (eartagError is not null)
            ModelState.AddModelError("Case.EartagCountry", eartagError);
    }

    private void ValidateFormADates(DateTime today)
    {
        if (!IsNonGbCase && !Case.FormADate.HasValue)
            ModelState.AddModelError("Case.FormADate", "Enter a Form A date.");

        if (Case.Bse1ReceivedDate.HasValue && Case.Bse1ReceivedDate.Value.Date > today)
            ModelState.AddModelError("Case.Bse1ReceivedDate", "Invalid Date");

        // Form A Date: must be ≤ SlaughterDate (if set) else ≤ today
        if (Case.FormADate.HasValue)
        {
            var limit = Case.SlaughterDate.HasValue ? Case.SlaughterDate.Value.Date : today;
            if (Case.FormADate.Value.Date > limit)
                ModelState.AddModelError("Case.FormADate",
                    Case.SlaughterDate.HasValue
                        ? $"Form A date must be before the slaughter date ({Case.SlaughterDate.Value:dd/MM/yyyy})."
                        : "Form A date must be a past date.");
        }

        // Form A Resubmitted Date: requires Form A, must be after Form A and ≤ today
        ValidateDateAfterFormA(Case.FormAResubmittedDate, "Case.FormAResubmittedDate", "Form A resubmitted date", today);
    }

    private void ValidateFormBAndCDates(DateTime today)
    {
        // Form B Date: requires Form A, must be after Form A and ≤ today
        ValidateDateAfterFormA(Case.FormBDate, "Case.FormBDate", "Form B date", today);

        // Fate required when Form B Date is filled (legacy treats "-1" as unselected)
        if (Case.FormBDate.HasValue && (string.IsNullOrWhiteSpace(Case.Fate) || Case.Fate == "-1"))
            ModelState.AddModelError("Case.Fate",
                "Fate (Form B reason) is required when a Form B date is entered.");

        // Form C date: ≤ today (FormC ≠ FormB is a soft warning — not a hard block)
        if (Case.FormCDate.HasValue && Case.FormCDate.Value.Date > today)
            ModelState.AddModelError("Case.FormCDate", "Invalid Date");
    }

    private void ValidateDateAfterFormA(DateTime? date, string field, string label, DateTime today)
    {
        // OnPostAsync already clears dates that depend on a missing Form A, so only the range checks remain.
        if (!date.HasValue || !Case.FormADate.HasValue)
            return;

        if (date.Value.Date <= Case.FormADate.Value.Date)
            ModelState.AddModelError(field, $"{label} must be after the Form A date.");
        else if (date.Value.Date > today)
            ModelState.AddModelError(field, $"{label} must be a past date.");
    }

    // Date of Birth: after 1 Jan 1970, before Form A date (or today), PurchaseDate, OnsetDate
    private void ValidateBirthDate(DateTime today)
    {
        if (!Case.BirthDate.HasValue)
            return;

        var birthDate = Case.BirthDate.Value.Date;
        if (birthDate < DateTime.UnixEpoch)
        {
            ModelState.AddModelError(BirthDateKey, "Date of birth must be after 1 January 1970.");
            return;
        }

        var formALimit = Case.FormADate.HasValue ? Case.FormADate.Value.Date : today;
        if (birthDate >= formALimit)
            ModelState.AddModelError(BirthDateKey,
                Case.FormADate.HasValue
                    ? "Date of birth must be before the Form A date."
                    : "Date of birth must be a past date.");

        if (Case.PurchaseDate.HasValue && birthDate >= Case.PurchaseDate.Value.Date)
            ModelState.AddModelError(BirthDateKey, "Date of birth must be before the purchase date.");

        if (Case.OnsetDate.HasValue && birthDate >= Case.OnsetDate.Value.Date)
            ModelState.AddModelError(BirthDateKey, "Date of birth must be before the onset date.");
    }

    // BSE-1 receipt dates: if filled, must be after RBSEDate + 1 day and ≤ today
    private void ValidateBse1ReceiptDates(DateTime today)
    {
        if (!Case.HasCaseWork || !Case.RbseDate.HasValue)
            return;

        var rbseDate = Case.RbseDate.Value;
        CheckBse1Date(Case.PurchaserBse1ReceivedDate, "Case.PurchaserBse1ReceivedDate", "Purchaser BSE-1 received date", rbseDate, today);
        CheckBse1Date(Case.BreederBse1ReceivedDate,   "Case.BreederBse1ReceivedDate",   "Breeder BSE-1 received date", rbseDate, today);
        CheckBse1Date(Case.Vendor1Bse1ReceivedDate,   "Case.Vendor1Bse1ReceivedDate",   "Vendor 1 BSE-1 received date", rbseDate, today);
        CheckBse1Date(Case.HomebredBse1ReceivedDate,  "Case.HomebredBse1ReceivedDate",  "Homebred BSE-1 received date", rbseDate, today);
        CheckBse1Date(Case.SummarySheetReceivedDate,  "Case.SummarySheetReceivedDate",  "Summary sheet received date", rbseDate, today);
        CheckBse1Date(Case.PaperworkCompleteDate,     "Case.PaperworkCompleteDate",     "Paperwork complete date", rbseDate, today);
    }

    private void CheckBse1Date(DateTime? date, string field, string label, DateTime rbseDate, DateTime today)
    {
        if (!date.HasValue)
            return;

        if (date.Value.Date < rbseDate.Date.AddDays(1))
            ModelState.AddModelError(field, $"{label} must be after the RBSE date ({rbseDate:dd/MM/yyyy}).");
        else if (date.Value.Date > today)
            ModelState.AddModelError(field, $"{label} must be a past date.");
    }

}
