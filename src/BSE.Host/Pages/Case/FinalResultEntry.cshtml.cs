using BSE.Host.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DEFRAMaintenance")]
public class FinalResultEntryModel(
    ICaseService cases,
    ITestRepository testRepository,
    ILookupDataService lookupService,
    ICurrentUserService currentUser,
    ILogger<FinalResultEntryModel> logger) : PageModel
{
    public const string RbseRequiredMessage = "Please enter an RBSE";
    public const string CaseNotFoundMessage = "This RBSE could not be found in the database";
    public const string FinalResultRequiredMessage = "You must select a final result";
    public const string SaveFailedMessage = "Failed to Save Final Result Details.";
    public const string LoadFailedMessage = "Failed to Load Case Details.";
    public const string ConcurrencyMessage = "This case was changed by another user. Look the case up again and re-enter your changes.";

    private const string PositiveCode = "Pos";
    private const string NegativeCode = "Neg";

    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;

    [BindProperty] public string? FinalResult { get; set; }
    [BindProperty] public string? RetrospectiveTestType { get; set; }
    [BindProperty] public string? RetrospectiveResult { get; set; }
    [BindProperty] public DateTime? RetrospectiveResultDate { get; set; }
    [BindProperty] public string? RetrospectiveComment { get; set; }
    [BindProperty] public string? LabComment { get; set; }

    public FinalResultRecord? Result { get; private set; }
    public IReadOnlyList<CaseTestRecord> Tests { get; private set; } = [];
    public IReadOnlyList<LuTestType> TestTypes { get; private set; } = [];
    public IReadOnlyList<LuTestResult> TestResults { get; private set; } = [];

    public string? RbseError { get; private set; }
    public string? FinalResultError { get; private set; }
    public string? ErrorMessage { get; private set; }

    public bool CaseFound => Result is not null;

    /// <summary>Legacy allocated the final result date automatically; it is never keyed in.</summary>
    public DateTime? DisplayFinalResultDate =>
        Result?.FinalResult is null ? DateTime.Today : Result?.FinalResultDate;

    /// <summary>Legacy showed "TBC" until the stored procedure allocated a DBSE for a positive.</summary>
    public string DisplayDbse =>
        Result?.FinalResult is null ? "TBC" : RbseHelper.FormatDbse(Result?.Dbse) ?? "TBC";

    public bool HasFinalResult => !string.IsNullOrWhiteSpace(Result?.FinalResult);

    public bool TestsContainPositive =>
        Tests.Any(t => string.Equals(t.TestResult, PositiveCode, StringComparison.OrdinalIgnoreCase));

    // ── Legacy ddlFinalResult_SelectedIndexChanged warnings ──────────────────

    public bool ShowPositiveWithoutPositiveTest =>
        string.Equals(FinalResult, PositiveCode, StringComparison.OrdinalIgnoreCase) && !TestsContainPositive;

    public bool ShowNegativeWithPositiveTest =>
        string.Equals(FinalResult, NegativeCode, StringComparison.OrdinalIgnoreCase) && TestsContainPositive;

    public bool ShowPaperworkIncomplete =>
        string.Equals(FinalResult, PositiveCode, StringComparison.OrdinalIgnoreCase)
        && Result is not null && !Result.IsPaperworkComplete;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();

        if (!string.IsNullOrWhiteSpace(Rbse))
        {
            await LookUpCaseAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostLookUpAsync()
    {
        await LoadLookupsAsync();

        if (string.IsNullOrWhiteSpace(Rbse))
        {
            RbseError = RbseRequiredMessage;
            return Page();
        }

        await LookUpCaseAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        await LoadLookupsAsync();
        await LookUpCaseAsync(preserveInput: true);

        if (!CaseFound)
        {
            return Page();
        }

        if (string.IsNullOrWhiteSpace(FinalResult))
        {
            FinalResultError = FinalResultRequiredMessage;
            return Page();
        }

        var command = new EditFinalResultCommand(
            Rbse: RbseHelper.ParseToRaw(Rbse),
            FinalResult: FinalResult,
            // Legacy stamped today's date when the result was chosen; it is not user-entered.
            FinalResultDate: Result!.FinalResultDate ?? DateTime.Today,
            RetrospectiveTestType: RetrospectiveTestType,
            RetrospectiveResult: RetrospectiveResult,
            RetrospectiveResultDate: RetrospectiveResultDate,
            RetrospectiveComment: RetrospectiveComment,
            LabComment: LabComment,
            RowStamp: Result.RowStamp ?? []);

        EditCaseResult result;
        try
        {
            var userId = await currentUser.GetUserIdAsync();
            result = await cases.SaveFinalResultAsync(command, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EditCaseFinalResult stored procedure threw an exception");
            ErrorMessage = SaveFailedMessage;
            return Page();
        }

        if (result != EditCaseResult.Success)
        {
            logger.LogWarning("EditCaseFinalResult returned {Result}", result);
            ErrorMessage = result switch
            {
                EditCaseResult.ConcurrencyConflict => ConcurrencyMessage,
                EditCaseResult.RbseNotFound => CaseNotFoundMessage,
                _ => SaveFailedMessage
            };
            return Page();
        }

        // Re-read so the confirmation can show the DBSE the stored procedure allocated.
        var saved = await cases.GetFinalResultAsync(command.Rbse);

        TempData[MaintenanceConfirmationModel.TitleKey] = "Final result saved";
        TempData[MaintenanceConfirmationModel.SummaryKey] =
            $"Final result recorded for {RbseHelper.Format(command.Rbse)}";
        TempData[MaintenanceConfirmationModel.MessageKey] =
            string.Equals(FinalResult, PositiveCode, StringComparison.OrdinalIgnoreCase)
                ? $"DBSE {RbseHelper.FormatDbse(saved?.Dbse)} has been allocated to this case."
                : "No DBSE is allocated because the final result is not positive.";

        return RedirectToPage("/MaintenanceConfirmation");
    }

    private async Task LookUpCaseAsync(bool preserveInput = false)
    {
        var rbse = RbseHelper.ParseToRaw(Rbse);

        if (rbse.Length == 0)
        {
            RbseError = RbseRequiredMessage;
            return;
        }

        try
        {
            Result = await cases.GetFinalResultAsync(rbse);

            if (Result is null)
            {
                logger.LogInformation("No case found for final result entry lookup");
                RbseError = CaseNotFoundMessage;
                return;
            }

            Tests = await testRepository.GetByRbseAsync(rbse);

            if (!preserveInput)
            {
                FinalResult             = Result.FinalResult;
                RetrospectiveTestType   = Result.RetrospectiveTestType;
                RetrospectiveResult     = Result.RetrospectiveResult;
                RetrospectiveResultDate = Result.RetrospectiveResultDate;
                RetrospectiveComment    = Result.RetrospectiveComment;
                LabComment              = Result.LabComment;
            }
        }
        catch (Exception ex)
        {
            Result = null;
            logger.LogError(ex, "Failed to load case details for final result entry");
            ErrorMessage = LoadFailedMessage;
        }
    }

    private async Task LoadLookupsAsync()
    {
        var types   = await lookupService.GetTestTypesAsync();
        var results = await lookupService.GetTestResultsAsync();
        TestTypes   = types.ToList();
        TestResults = results.ToList();
    }

    public string DescribeTestType(string? code) =>
        TestTypes.FirstOrDefault(t => t.Code == code)?.Description ?? code ?? string.Empty;

    public string DescribeTestResult(string? code) =>
        TestResults.FirstOrDefault(t => t.Code == code)?.Description ?? code ?? string.Empty;
}
