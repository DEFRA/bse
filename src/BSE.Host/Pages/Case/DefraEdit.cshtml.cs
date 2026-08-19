using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
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

[Authorize(Policy = "DataEntry")]
public class DefraEditModel(
    ICaseService caseService,
    ICurrentUserService currentUserService,
    ILookupDataService lookups,
    ICaseWorkRepository caseWorkRepository,
    ITestRepository testRepository,
    IBatchRepository batchRepository,
    IConfiguration configuration) : PageModel
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

    public IReadOnlyList<CaseTestRecord> Tests { get; private set; } = [];
    public string SpolSiteUrl { get; private set; } = string.Empty;

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
        Tests = (await testRepository.GetByRbseAsync(Rbse)).ToList().AsReadOnly();
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        await Task.WhenAll(LoadLookupsAsync(), batchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadLookupsAsync();
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
}
