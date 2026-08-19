using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

/// <summary>
/// Migrated equivalent of legacy FinalResultEntry.aspx.
/// Accessible to DEFRA Maintenance and VLA Maintenance only (DEFRAMaintenance policy).
/// The user enters an RBSE to load case details, then sets the final result and
/// optional retrospective test information.
/// </summary>
[Authorize(Policy = "DEFRAMaintenance")]
public class FinalResultEntryModel(
    ICaseRepository caseRepository,
    ITestRepository testRepository,
    ILookupDataService lookups,
    ICurrentUserService currentUserService,
    IDbConnectionFactory connectionFactory) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Rbse { get; set; }

    // ── Case summary (read-only display fields) ───────────────────────────────
    public FinalResultRecord? FinalResultData { get; private set; }
    public IReadOnlyList<CaseTestRecord> Tests { get; private set; } = [];

    // ── Lookup options ────────────────────────────────────────────────────────
    public IEnumerable<LuTestType> TestTypeOptions { get; private set; } = [];
    public IEnumerable<LuTestResult> TestResultOptions { get; private set; } = [];

    // ── Editable form fields ──────────────────────────────────────────────────
    [BindProperty] public string? FinalResult { get; set; }
    [BindProperty] public string? RetrospectiveTestType { get; set; }
    [BindProperty] public string? RetrospectiveResult { get; set; }
    [BindProperty] public DateTime? RetrospectiveResultDate { get; set; }
    [BindProperty] public string? RetrospectiveComment { get; set; }
    [BindProperty] public string? LabComment { get; set; }

    // ── Concurrency token (hidden in form) ────────────────────────────────────
    [BindProperty] public string? RowStampBase64 { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        if (!string.IsNullOrWhiteSpace(Rbse))
            await LoadCaseAsync(Rbse.Replace("/", "").ToUpper());
        return Page();
    }

    public async Task<IActionResult> OnPostLookupAsync()
    {
        await LoadLookupsAsync();
        if (string.IsNullOrWhiteSpace(Rbse))
        {
            ModelState.AddModelError(nameof(Rbse), "Enter an RBSE number.");
            return Page();
        }
        await LoadCaseAsync(Rbse.Replace("/", "").ToUpper());
        if (FinalResultData is null)
            ModelState.AddModelError(nameof(Rbse), "This RBSE could not be found in the database.");
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        await LoadLookupsAsync();

        if (string.IsNullOrWhiteSpace(Rbse))
        {
            ModelState.AddModelError(nameof(Rbse), "Enter an RBSE number.");
            return Page();
        }
        if (string.IsNullOrWhiteSpace(FinalResult))
        {
            ModelState.AddModelError(nameof(FinalResult), "Select a final result.");
            await LoadCaseAsync(Rbse.Replace("/", "").ToUpper());
            return Page();
        }
        if (string.IsNullOrWhiteSpace(RowStampBase64))
        {
            ModelState.AddModelError(string.Empty, "Session expired — please look up the case again.");
            await LoadCaseAsync(Rbse.Replace("/", "").ToUpper());
            return Page();
        }

        var rowStamp = Convert.FromBase64String(RowStampBase64);
        var command = new EditFinalResultCommand(
            Rbse:                    Rbse.Replace("/", "").ToUpper(),
            FinalResult:             FinalResult,
            FinalResultDate:         DateTime.Today,
            RetrospectiveTestType:   RetrospectiveTestType,
            RetrospectiveResult:     RetrospectiveResult,
            RetrospectiveResultDate: RetrospectiveResultDate,
            RetrospectiveComment:    RetrospectiveComment,
            LabComment:              LabComment,
            RowStamp:                rowStamp);

        var userId = await currentUserService.GetUserIdAsync();

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        var result = await caseRepository.EditFinalResultAsync(command, userId, conn, tx);
        tx.Commit();

        if (result == EditCaseResult.ConcurrencyConflict)
        {
            ModelState.AddModelError(string.Empty,
                "Another user modified this case. Look up the case again to get the latest version.");
            await LoadCaseAsync(Rbse.Replace("/", "").ToUpper());
            return Page();
        }

        TempData["Success"] = $"Final result saved for {Rbse}.";
        return RedirectToPage(new { rbse = Rbse });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task LoadCaseAsync(string rbse)
    {
        var dataTask  = caseRepository.GetFinalResultByRbseAsync(rbse);
        var testsTask = testRepository.GetByRbseAsync(rbse);
        await Task.WhenAll(dataTask, testsTask);

        FinalResultData = await dataTask;
        Tests           = await testsTask;

        if (FinalResultData is not null)
        {
            // Pre-populate form fields from existing saved values
            FinalResult             = FinalResultData.FinalResult;
            RetrospectiveTestType   = FinalResultData.RetrospectiveTestType;
            RetrospectiveResult     = FinalResultData.RetrospectiveResult;
            RetrospectiveResultDate = FinalResultData.RetrospectiveResultDate;
            RetrospectiveComment    = FinalResultData.RetrospectiveComment;
            LabComment              = FinalResultData.LabComment;
            RowStampBase64          = FinalResultData.RowStamp is not null
                                        ? Convert.ToBase64String(FinalResultData.RowStamp)
                                        : null;
        }
    }

    private async Task LoadLookupsAsync()
    {
        var t1 = lookups.GetTestTypesAsync();
        var t2 = lookups.GetTestResultsAsync();
        await Task.WhenAll(t1, t2);
        TestTypeOptions   = await t1;
        TestResultOptions = await t2;
    }
}
