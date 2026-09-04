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

[Authorize]
public class EditModel(
    ICaseService caseService,
    ICurrentUserService currentUserService,
    ILookupDataService lookups,
    ICaseWorkRepository caseWorkRepository,
    ITestRepository testRepository,
    IBatchRepository batchRepository,
    IConfiguration configuration) : PageModel
{
    private const string RowStampKey = "CaseEdit_RowStamp_{0}";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty]
    public CaseEditViewModel Case { get; set; } = new();

    public string? ConcurrencyError { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    // Lookup options for dropdowns
    public IEnumerable<BSE.SharedKernel.ILookupItem> FateOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> SurveyOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> ReportedLocationOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> BirthDateSourceOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> ValuationAgeOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> CaseTypeOptions { get; private set; } = [];

    // Tests grid
    public IReadOnlyList<CaseTestRecord> Tests { get; private set; } = [];

    // View Docs — SharePoint URL (RBSE appended by view, slashes stripped)
    public string SpolSiteUrl { get; private set; } = string.Empty;

    public IEnumerable<ILookupItem> TestTypeOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> TestResultOptions { get; private set; } = [];

    private const int TestsPageSize = 10;
    [BindProperty(SupportsGet = true)] public int    TPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string TSort { get; set; } = "type";
    [BindProperty(SupportsGet = true)] public string TDir  { get; set; } = "asc";
    public int TestsTotalPages { get; private set; } = 1;
    public int TestsTotalCount { get; private set; }

    [BindProperty] public string  NewTestType          { get; set; } = string.Empty;
    [BindProperty] public string? NewTestResult        { get; set; }

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
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        await Task.WhenAll(LoadLookupsAsync(), batchTask, LoadTestsAsync());
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
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
                EditCaseResult.RbseNotFound     => $"Case '{Rbse}' not found.",
                EditCaseResult.AuditLogError    => "Audit log error during update.",
                EditCaseResult.PostUpdateError  => "Database error after update.",
                _                               => $"Update failed: {result}"
            };
            ModelState.AddModelError("", message);
            return Page();
        }

        // Save casework fields if the case has a CaseWork row
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
        return RedirectToPage(new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var fateTask             = lookups.GetLookupAsync(LookupTableId.CaseFate);
        var surveyTask           = lookups.GetLookupAsync(LookupTableId.Survey);
        var reportedLocationTask = lookups.GetLookupAsync(LookupTableId.ReportedLocation);
        var birthDateSourceTask  = lookups.GetLookupAsync(LookupTableId.BirthDateSource);
        var valuationAgeTask     = lookups.GetLookupAsync(LookupTableId.ValuationAge);
        var caseTypeTask         = lookups.GetLookupAsync(LookupTableId.CaseType);
        var testTypeTask         = lookups.GetLookupAsync(LookupTableId.TestType);
        var testResultTask       = lookups.GetLookupAsync(LookupTableId.TestResult);

        await Task.WhenAll(fateTask, surveyTask, reportedLocationTask, birthDateSourceTask,
                           valuationAgeTask, caseTypeTask, testTypeTask, testResultTask);

        FateOptions             = await fateTask;
        SurveyOptions           = await surveyTask;
        ReportedLocationOptions = await reportedLocationTask;
        BirthDateSourceOptions  = await birthDateSourceTask;
        ValuationAgeOptions     = await valuationAgeTask;
        CaseTypeOptions         = await caseTypeTask;
        TestTypeOptions         = await testTypeTask;
        TestResultOptions       = await testResultTask;
    }

    private async Task LoadTestsAsync()
    {
        var all = (await testRepository.GetByRbseAsync(Rbse)).ToList();
        TestsTotalCount = all.Count;
        TestsTotalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)TestsPageSize));
        TPage = Math.Clamp(TPage, 1, TestsTotalPages);
        IEnumerable<CaseTestRecord> sorted = TSort switch
        {
            "result" => TDir == "desc" ? all.OrderByDescending(t => t.TestResultDescription) : all.OrderBy(t => t.TestResultDescription),
            _        => TDir == "desc" ? all.OrderByDescending(t => t.TestTypeDescription)   : all.OrderBy(t => t.TestTypeDescription),
        };
        Tests = sorted.Skip((TPage - 1) * TestsPageSize).Take(TestsPageSize).ToList().AsReadOnly();
    }

    public async Task<IActionResult> OnPostAddTestAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        if (string.IsNullOrWhiteSpace(NewTestType))
        {
            ModelState.AddModelError(nameof(NewTestType), "Select a test type.");
            var record = await caseService.GetCaseAsync(Rbse);
            if (record is not null) { Case = CaseEditViewModel.FromRecord(record); var cw = await caseWorkRepository.GetByRbseAsync(Rbse); if (cw is not null) Case.ApplyCaseWork(cw); }
            SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
            var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
            await Task.WhenAll(LoadLookupsAsync(), LoadTestsAsync(), batchTask);
            BatchNumbers = (await batchTask).ToList().AsReadOnly();
            return Page();
        }
        await testRepository.AddAsync(new AddTestCommand(Rbse.Replace("/", ""), NewTestType, NewTestResult));
        TempData["Success"] = "Test record added.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteTestAsync(int id, string rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        await testRepository.DeleteAsync(id, Convert.FromBase64String(rowStampBase64));
        TempData["Success"] = "Test record deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public string TestsSortUrl(string col)
    {
        var dir = string.Equals(TSort, col, StringComparison.OrdinalIgnoreCase) && TDir == "asc" ? "desc" : "asc";
        return $"?rbse={Uri.EscapeDataString(Rbse)}&TSort={col}&TDir={dir}&TPage=1";
    }

    public string TestsPageUrl(int page) =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&TPage={page}&TSort={TSort}&TDir={TDir}";
}
