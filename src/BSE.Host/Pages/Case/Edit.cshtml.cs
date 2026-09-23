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
    ICaseEditDraftStateService caseEditDraftState,
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
    public bool HasCaseWorkLink { get; private set; }

    // Lookup options for dropdowns
    public IEnumerable<BSE.SharedKernel.ILookupItem> FateOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> SurveyOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> ReportedLocationOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> BirthDateSourceOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> ValuationAgeOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> CaseTypeOptions { get; private set; } = [];

    // Tests grid
    public IReadOnlyList<CaseTestRecord> Tests { get; private set; } = [];
    [BindProperty] public List<StagedTestItem> StagedTests { get; set; } = [];
    public bool HasUnsavedChanges { get; private set; }

    // View Docs — SharePoint URL (RBSE appended by view, slashes stripped)
    public string SpolSiteUrl { get; private set; } = string.Empty;

    public IEnumerable<ILookupItem> TestTypeOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> TestResultOptions { get; private set; } = [];

    [BindProperty] public string NewTestType { get; set; } = string.Empty;
    [BindProperty] public string? NewTestResult { get; set; }
    [BindProperty] public int EditingTestId { get; set; }
    [BindProperty] public string EditTestType { get; set; } = string.Empty;
    [BindProperty] public string? EditTestResult { get; set; }
    [BindProperty] public string EditTestRowStampBase64 { get; set; } = string.Empty;
    public bool ShowAddTestRow { get; private set; }
    public int? ReopenEditTestId { get; private set; }

    private const int TestsPageSize = 10;
    [BindProperty(SupportsGet = true)] public int    TPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string TSort { get; set; } = "type";
    [BindProperty(SupportsGet = true)] public string TDir  { get; set; } = "asc";
    public int TestsTotalPages { get; private set; } = 1;
    public int TestsTotalCount { get; private set; }


    public async Task<IActionResult> OnGetAsync()
    {
        var record = await caseService.GetCaseAsync(Rbse);
        if (record is null)
        {
            TempData["Warning"] = $"Case '{Rbse}' not found.";
            return RedirectToPage("/Home");
        }

        TempData[string.Format(RowStampKey, Rbse)] = Convert.ToBase64String(record.RowStamp ?? []);
        Case = CaseEditViewModel.FromRecord(record);

        var caseWork = await caseWorkRepository.GetByRbseAsync(Rbse);
        if (caseWork is not null)
            Case.ApplyCaseWork(caseWork);

        HasCaseWorkLink = caseWork is not null
                          || await caseWorkRepository.GetEntryByRbseAsync(Rbse) is not null;

        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        await Task.WhenAll(LoadLookupsAsync(), batchTask);
        await LoadOrInitializeDraftStateAsync();
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        return Page();
    }

    public async Task<IActionResult> OnPostBeginEditTestRowAsync(int id)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadReadonlyPageAsync();
        await LoadOrInitializeDraftStateAsync();

        var test = StagedTests.FirstOrDefault(t => t.Id == id);
        if (test is null)
            return RedirectToPage(new { rbse = Rbse });

        ReopenEditTestId = id;
        EditTestType = test.TestType;
        EditTestResult = test.TestResult;
        EditTestRowStampBase64 = test.RowStampBase64;
        return Page();
    }

    public async Task<IActionResult> OnPostAddTestRowAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadReadonlyPageAsync();
        await LoadOrInitializeDraftStateAsync();

        if (string.IsNullOrWhiteSpace(NewTestType))
            ModelState.AddModelError(nameof(NewTestType), "Select a test type.");

        if (!ModelState.IsValid)
        {
            ShowAddTestRow = true;
            return Page();
        }

        StagedTests.Add(new StagedTestItem
        {
            Id = NextTemporaryTestId(),
            TestType = NewTestType,
            TestResult = NewTestResult,
            RowStampBase64 = string.Empty
        });

        await SaveDraftStateAsync();
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadReadonlyPageAsync();
        await LoadOrInitializeDraftStateAsync();

        await PersistStagedTestsAsync();
        await caseEditDraftState.ClearAsync(Rbse);
        TempData["Success"] = "Case test changes saved.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnGetCancelEditAsync()
    {
        await caseEditDraftState.ClearAsync(Rbse);
        return RedirectToPage("/Home");
    }

    public async Task<IActionResult> OnPostUpdateTestRowAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadReadonlyPageAsync();
        await LoadOrInitializeDraftStateAsync();

        if (string.IsNullOrWhiteSpace(EditTestType))
            ModelState.AddModelError(nameof(EditTestType), "Select a test type.");

        if (!ModelState.IsValid)
        {
            ReopenEditTestId = EditingTestId;
            return Page();
        }

        var test = StagedTests.FirstOrDefault(t => t.Id == EditingTestId);
        if (test is null)
            return RedirectToPage(new { rbse = Rbse });

        test.TestType = EditTestType;
        test.TestResult = EditTestResult;
        await SaveDraftStateAsync();
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadLookupsAsync();
        await LoadOrInitializeDraftStateAsync();

        ApplyLegacyPreSaveNormalizations();
        ValidateLegacyParityRules();

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

        await PersistStagedTestsAsync();
        await caseEditDraftState.ClearAsync(Rbse);

        TempData["Success"] = $"Case {Rbse} has been updated.";
        return RedirectToPage(new { rbse = Rbse });
    }

    private void ApplyLegacyPreSaveNormalizations()
    {
        if (!Case.BirthDate.HasValue)
        {
            Case.BirthDateSource = null;
            Case.IsBirthDateEst = false;
        }

        // Legacy behavior: when Form B is entered and Slaughter Date is empty,
        // Slaughter Date is set to Form B Date during save mapping.
        if (!Case.SlaughterDate.HasValue && Case.FormBDate.HasValue)
            Case.SlaughterDate = Case.FormBDate;
    }

    private void ValidateLegacyParityRules()
    {
        var today = DateTime.Today;

        if (Case.Bse1ReceivedDate.HasValue && Case.Bse1ReceivedDate.Value.Date > today)
            ModelState.AddModelError("Case.Bse1ReceivedDate", "You must enter a past date.");

        if (Case.FormADate.HasValue)
        {
            var latest = Case.SlaughterDate?.Date ?? today;
            var formA = Case.FormADate.Value.Date;
            if (formA > latest)
            {
                var message = Case.SlaughterDate.HasValue
                    ? "You must enter a date before the Slaughter Date."
                    : "You must enter a past date.";
                ModelState.AddModelError("Case.FormADate", message);
            }
        }

        if (Case.FormAResubmittedDate.HasValue)
        {
            if (!Case.FormADate.HasValue)
            {
                ModelState.AddModelError("Case.FormAResubmittedDate", "You must enter a Form A Date first.");
            }
            else
            {
                var value = Case.FormAResubmittedDate.Value.Date;
                var min = Case.FormADate.Value.Date;
                if (value < min || value > today)
                    ModelState.AddModelError("Case.FormAResubmittedDate", "You must enter a date in the past but after the Form A Date.");
            }
        }

        if (Case.FormBDate.HasValue)
        {
            if (!Case.FormADate.HasValue)
            {
                ModelState.AddModelError("Case.FormBDate", "You must enter a Form A Date first.");
            }
            else
            {
                var value = Case.FormBDate.Value.Date;
                var min = Case.FormADate.Value.Date;
                if (value < min || value > today)
                    ModelState.AddModelError("Case.FormBDate", "You must enter a date in the past but after the Form A Date.");
            }
        }

        if (Case.FormCDate.HasValue && !Case.FormBDate.HasValue)
            ModelState.AddModelError("Case.FormCDate", "You must enter a Form B Date first.");

        if (Case.FormBDate.HasValue && string.IsNullOrWhiteSpace(Case.Fate))
            ModelState.AddModelError("Case.Fate", "Select a fate.");

        if (Case.BirthDate.HasValue)
        {
            var birthDate = Case.BirthDate.Value.Date;
            if (birthDate < new DateTime(1970, 1, 1))
                ModelState.AddModelError("Case.BirthDate", "Date of Birth must be on or after 01/01/1970.");

            var latestForFormA = Case.FormADate?.Date ?? today;
            if (birthDate > latestForFormA)
                ModelState.AddModelError("Case.BirthDate", "Date of Birth must be before the Form A Date");

            if (Case.OnsetDate.HasValue && birthDate > Case.OnsetDate.Value.Date)
                ModelState.AddModelError("Case.BirthDate", "Date of Birth must be before the Onset Date");
        }

        if (Case.HasCaseWork && Case.RbseDate.HasValue)
        {
            var min = Case.RbseDate.Value.Date.AddDays(1);
            var max = today;
            var message = $"You must enter a date in the past but after the RBSE Date ({Case.RbseDate.Value:dd/MM/yyyy})";

            ValidateOptionalRange(Case.PurchaserBse1ReceivedDate, "Case.PurchaserBse1ReceivedDate", min, max, message);
            ValidateOptionalRange(Case.BreederBse1ReceivedDate, "Case.BreederBse1ReceivedDate", min, max, message);
            ValidateOptionalRange(Case.Vendor1Bse1ReceivedDate, "Case.Vendor1Bse1ReceivedDate", min, max, message);
            ValidateOptionalRange(Case.HomebredBse1ReceivedDate, "Case.HomebredBse1ReceivedDate", min, max, message);
            ValidateOptionalRange(Case.SummarySheetReceivedDate, "Case.SummarySheetReceivedDate", min, max, message);
            ValidateOptionalRange(Case.PaperworkCompleteDate, "Case.PaperworkCompleteDate", min, max, message);
        }
    }

    private void ValidateOptionalRange(DateTime? value, string modelKey, DateTime min, DateTime max, string message)
    {
        if (!value.HasValue)
            return;

        var date = value.Value.Date;
        if (date < min || date > max)
            ModelState.AddModelError(modelKey, message);
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
        var all = StagedTests.Select(t => new CaseTestRecord(
            Id: t.Id ?? 0,
            Rbse: Rbse,
            TestType: t.TestType,
            TestTypeDescription: t.TestTypeDescription,
            TestResult: t.TestResult,
            TestResultDescription: t.TestResultDescription,
            RowStamp: string.IsNullOrWhiteSpace(t.RowStampBase64) ? [] : Convert.FromBase64String(t.RowStampBase64)))
            .ToList();

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

    private async Task LoadReadonlyPageAsync()
    {
        var record = await caseService.GetCaseAsync(Rbse);
        if (record is null)
            return;

        Case = CaseEditViewModel.FromRecord(record);
        var caseWork = await caseWorkRepository.GetByRbseAsync(Rbse);
        if (caseWork is not null)
            Case.ApplyCaseWork(caseWork);

        HasCaseWorkLink = caseWork is not null
                          || await caseWorkRepository.GetEntryByRbseAsync(Rbse) is not null;

        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        await Task.WhenAll(LoadLookupsAsync(), batchTask, LoadTestsAsync());
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
    }

    private async Task<CaseEditDraftState> LoadOrInitializeDraftStateAsync()
    {
        var draft = await caseEditDraftState.GetAsync(Rbse);
        if (draft is null)
        {
            var persistedTests = (await testRepository.GetByRbseAsync(Rbse)).ToList();
            draft = new CaseEditDraftState
            {
                Rbse = Rbse,
                Tests = persistedTests.Select(t => new CaseEditDraftTestItem
                {
                    Id = t.Id,
                    TestType = t.TestType,
                    TestTypeDescription = t.TestTypeDescription,
                    TestResult = t.TestResult,
                    TestResultDescription = t.TestResultDescription,
                    RowStampBase64 = t.RowStamp is null ? string.Empty : Convert.ToBase64String(t.RowStamp)
                }).ToList(),
                HasPendingChanges = false
            };

            await caseEditDraftState.SetAsync(draft);
        }

        StagedTests = draft.Tests.Select(t => new StagedTestItem
        {
            ClientKey = t.ClientKey,
            Id = t.Id,
            TestType = t.TestType,
            TestTypeDescription = t.TestTypeDescription,
            TestResult = t.TestResult,
            TestResultDescription = t.TestResultDescription,
            RowStampBase64 = t.RowStampBase64
        }).ToList();

        HasUnsavedChanges = draft.HasPendingChanges;
        await LoadTestsAsync();
        return draft;
    }

    private async Task SaveDraftStateAsync(bool hasPendingChanges = true)
    {
        var testTypeByCode = (await lookups.GetLookupAsync(LookupTableId.TestType)).ToDictionary(x => x.Code, x => x.Description, StringComparer.OrdinalIgnoreCase);
        var testResultByCode = (await lookups.GetLookupAsync(LookupTableId.TestResult)).ToDictionary(x => x.Code, x => x.Description, StringComparer.OrdinalIgnoreCase);

        var draft = new CaseEditDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = hasPendingChanges,
            Tests = StagedTests.Select(t => new CaseEditDraftTestItem
            {
                ClientKey = t.ClientKey,
                Id = t.Id,
                TestType = t.TestType,
                TestTypeDescription = testTypeByCode.GetValueOrDefault(t.TestType),
                TestResult = t.TestResult,
                TestResultDescription = string.IsNullOrWhiteSpace(t.TestResult) ? null : testResultByCode.GetValueOrDefault(t.TestResult),
                RowStampBase64 = t.RowStampBase64
            }).ToList()
        };

        await caseEditDraftState.SetAsync(draft);
        HasUnsavedChanges = hasPendingChanges;
    }

    private async Task PersistStagedTestsAsync()
    {
        var persisted = (await testRepository.GetByRbseAsync(Rbse)).ToList();
        var persistedById = persisted.ToDictionary(t => t.Id);
        var stagedByExistingId = StagedTests.Where(t => t.Id is > 0).ToDictionary(t => t.Id!.Value);

        foreach (var removed in persisted.Where(p => !stagedByExistingId.ContainsKey(p.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            await testRepository.DeleteAsync(removed.Id, removed.RowStamp);
        }

        foreach (var staged in StagedTests)
        {
            if (staged.Id is null || staged.Id <= 0)
            {
                await testRepository.AddAsync(new AddTestCommand(Rbse.Replace("/", ""), staged.TestType, staged.TestResult));
                continue;
            }

            if (!persistedById.TryGetValue(staged.Id.Value, out var current))
                continue;

            var changed = !string.Equals(current.TestType, staged.TestType, StringComparison.OrdinalIgnoreCase)
                          || !string.Equals(current.TestResult ?? string.Empty, staged.TestResult ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            if (!changed)
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? current.RowStamp ?? []
                : Convert.FromBase64String(staged.RowStampBase64);

            await testRepository.EditAsync(new EditTestCommand(staged.Id.Value, Rbse.Replace("/", ""), staged.TestType, staged.TestResult, rowStamp));
        }
    }

    public async Task<IActionResult> OnPostDeleteTestAsync(int id, string? rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadReadonlyPageAsync();
        await LoadOrInitializeDraftStateAsync();

        var item = StagedTests.FirstOrDefault(t => t.Id == id);
        if (item is not null)
        {
            StagedTests.Remove(item);
            await SaveDraftStateAsync();
        }

        return RedirectToPage(new { rbse = Rbse });
    }

    public string TestsSortUrl(string col)
    {
        var dir = string.Equals(TSort, col, StringComparison.OrdinalIgnoreCase) && TDir == "asc" ? "desc" : "asc";
        return $"?rbse={Uri.EscapeDataString(Rbse)}&TSort={col}&TDir={dir}&TPage=1";
    }

    public string TestsPageUrl(int page) =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&TPage={page}&TSort={TSort}&TDir={TDir}";

    public sealed class StagedTestItem
    {
        public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
        public int? Id { get; set; }
        public string TestType { get; set; } = string.Empty;
        public string? TestTypeDescription { get; set; }
        public string? TestResult { get; set; }
        public string? TestResultDescription { get; set; }
        public string RowStampBase64 { get; set; } = string.Empty;
    }

    private int NextTemporaryTestId()
    {
        var minExistingId = StagedTests.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).DefaultIfEmpty(0).Min();
        return minExistingId <= 0 ? minExistingId - 1 : -1;
    }
}
