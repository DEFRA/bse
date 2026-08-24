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
using System.Text.RegularExpressions;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class DefraEditModel(
    ICaseService caseService,
    ICurrentUserService currentUserService,
    ILookupDataService lookups,
    ICaseWorkRepository caseWorkRepository,
    IBatchRepository batchRepository) : PageModel
{
    private const string RowStampKey = "DefraEdit_RowStamp_{0}";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty]
    public CaseEditViewModel Case { get; set; } = new();

    public string? ConcurrencyError { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    public IEnumerable<ILookupItem> FateOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> SurveyOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> ReportedLocationOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> BirthDateSourceOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> ValuationAgeOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> CaseTypeOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var record = await caseService.GetCaseAsync(Rbse);
        if (record is null)
        {
            TempData["Warning"] = $"Case '{Rbse}' not found.";
            return RedirectToPage("/Case/Lookup");
        }

        TempData[string.Format(RowStampKey, Rbse)] = Convert.ToBase64String(record.RowStamp ?? []);
        Case = CaseEditViewModel.FromRecord(record);

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
        await LoadLookupsAsync();

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
        // Eartag: mirrors BSELib.Eartag.GetEartag + .ErrorCode check from ThreePartEartag.Validate()
        var eartagError = ValidateEartagFormat(Case.EartagCountry, Case.EartagHerdmark, Case.Eartag);
        if (eartagError is not null)
            ModelState.AddModelError("Case.EartagCountry", eartagError);

        var today = DateTime.Today;

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
        if (Case.FormAResubmittedDate.HasValue)
        {
            if (!Case.FormADate.HasValue)
                ModelState.AddModelError("Case.FormAResubmittedDate",
                    "Form A resubmitted date requires a Form A date to be set.");
            else if (Case.FormAResubmittedDate.Value.Date <= Case.FormADate.Value.Date)
                ModelState.AddModelError("Case.FormAResubmittedDate",
                    "Form A resubmitted date must be after the Form A date.");
            else if (Case.FormAResubmittedDate.Value.Date > today)
                ModelState.AddModelError("Case.FormAResubmittedDate",
                    "Form A resubmitted date must be a past date.");
        }

        // Form B Date: requires Form A, must be after Form A and ≤ today
        if (Case.FormBDate.HasValue)
        {
            if (!Case.FormADate.HasValue)
                ModelState.AddModelError("Case.FormBDate",
                    "Form B date requires a Form A date to be set.");
            else if (Case.FormBDate.Value.Date <= Case.FormADate.Value.Date)
                ModelState.AddModelError("Case.FormBDate",
                    "Form B date must be after the Form A date.");
            else if (Case.FormBDate.Value.Date > today)
                ModelState.AddModelError("Case.FormBDate",
                    "Form B date must be a past date.");
        }

        // Fate required when Form B Date is filled
        if (Case.FormBDate.HasValue && string.IsNullOrWhiteSpace(Case.Fate))
            ModelState.AddModelError("Case.Fate",
                "Fate (Form B reason) is required when a Form B date is entered.");

        // Form C Date: requires Form B Date (FormC ≠ FormB is a soft warning — not a hard block)
        if (Case.FormCDate.HasValue && !Case.FormBDate.HasValue)
            ModelState.AddModelError("Case.FormCDate",
                "Form C date requires a Form B date to be set.");

        // Date of Birth: after 1 Jan 1970, before Form A date (or today), PurchaseDate, OnsetDate
        if (Case.BirthDate.HasValue)
        {
            if (Case.BirthDate.Value.Date < new DateTime(1970, 1, 1))
                ModelState.AddModelError("Case.BirthDate",
                    "Date of birth must be after 1 January 1970.");
            else
            {
                var formALimit = Case.FormADate.HasValue ? Case.FormADate.Value.Date : today;
                if (Case.BirthDate.Value.Date >= formALimit)
                    ModelState.AddModelError("Case.BirthDate",
                        Case.FormADate.HasValue
                            ? "Date of birth must be before the Form A date."
                            : "Date of birth must be a past date.");

                if (Case.PurchaseDate.HasValue && Case.BirthDate.Value.Date >= Case.PurchaseDate.Value.Date)
                    ModelState.AddModelError("Case.BirthDate",
                        "Date of birth must be before the purchase date.");

                if (Case.OnsetDate.HasValue && Case.BirthDate.Value.Date >= Case.OnsetDate.Value.Date)
                    ModelState.AddModelError("Case.BirthDate",
                        "Date of birth must be before the onset date.");
            }
        }

        // BSE-1 receipt dates: if filled, must be after RBSEDate + 1 day and ≤ today
        if (Case.HasCaseWork && Case.RbseDate.HasValue)
        {
            var minDate = Case.RbseDate.Value.Date.AddDays(1);
            void CheckBse1Date(DateTime? date, string field, string label)
            {
                if (!date.HasValue) return;
                if (date.Value.Date < minDate)
                    ModelState.AddModelError(field,
                        $"{label} must be after the RBSE date ({Case.RbseDate.Value:dd/MM/yyyy}).");
                else if (date.Value.Date > today)
                    ModelState.AddModelError(field, $"{label} must be a past date.");
            }
            CheckBse1Date(Case.PurchaserBse1ReceivedDate, "Case.PurchaserBse1ReceivedDate", "Purchaser BSE-1 received date");
            CheckBse1Date(Case.BreederBse1ReceivedDate,   "Case.BreederBse1ReceivedDate",   "Breeder BSE-1 received date");
            CheckBse1Date(Case.Vendor1Bse1ReceivedDate,   "Case.Vendor1Bse1ReceivedDate",   "Vendor 1 BSE-1 received date");
            CheckBse1Date(Case.HomebredBse1ReceivedDate,  "Case.HomebredBse1ReceivedDate",  "Homebred BSE-1 received date");
            CheckBse1Date(Case.SummarySheetReceivedDate,  "Case.SummarySheetReceivedDate",  "Summary sheet received date");
            CheckBse1Date(Case.PaperworkCompleteDate,     "Case.PaperworkCompleteDate",     "Paperwork complete date");
        }
    }

    // Mirrors BSELib.Eartag.GetEartag routing and each format's Validate() method.
    private static string? ValidateEartagFormat(string? country, string? herdmark, string? animal)
    {
        var c = (country  ?? "").Trim().ToUpperInvariant();
        var h = (herdmark ?? "").Trim().ToUpperInvariant();
        var a = (animal   ?? "").Trim().ToUpperInvariant();

        if (c == "" && h == "" && a == "") return null; // eartag is optional

        // ISO EID format: country code longer than 2 chars (IsoNumericCountryEartagFormat / IsoAlphaNumericCountryEartagFormat)
        if (c.Length > 2)
        {
            if (c.Length > 0 && char.IsDigit(c[0]))
            {
                if (!Regex.IsMatch(c, @"^\d{3}[012_ ]?$"))
                    return "Country component is invalid: It should contain 3 numerical digits followed by 0, 1, 2, _ or space character.";
            }
            if (!Regex.IsMatch(h, @"^\d{6}$"))
                return "Herd component is invalid: It should contain 6 numerical digits.";
            if (!Regex.IsMatch(a, @"^\d{5}$"))
                return "Animal component is invalid: It should contain 5 numerical digits.";
            return null;
        }

        // UK eartag: UKEartag formats inherit EartagFormatBase.Validate which returns empty — no blocking error
        if (c == "UK") return null;

        // EC non-UK country codes: ECEartagFormat.Validate checks animal is 1–12 uppercase alphanumeric chars
        string[] ecCodes = ["AT", "BE", "DE", "DK", "EL", "ES", "FI", "FR", "IE", "IT", "LU", "NL", "PT", "SE"];
        if (ecCodes.Contains(c))
        {
            if (!Regex.IsMatch(a, @"^[0-9A-Z]{1,12}$"))
                return "Animal component is invalid: It should contain 1 to 12 numerical or uppercase alphabetical characters.";
            return null;
        }

        // Unknown / no country (NoCountryEartag): FreeEartagFormat / PreBarimoEartagFormat return empty — no blocking error
        return null;
    }
}
