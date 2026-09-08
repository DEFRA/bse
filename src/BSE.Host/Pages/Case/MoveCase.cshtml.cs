using BSE.Host.Helpers;
using BSE.Host.Services;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DEFRAMaintenance")]
public class MoveCaseModel(
    ICaseService caseService,
    IFarmService farmService,
    ICurrentUserService currentUserService,
    ILogger<MoveCaseModel> logger) : PageModel
{
    public const string CaseNotFoundMessage = "A case with this RBSE was not found";
    public const string FarmNotFoundMessage = "A farm with this CPHH was not found";
    public const string SameCphhMessage = "The CPHH you have entered is the same as the existing one.";
    public const string GbCaseNonGbFarmMessage = "You have entered a GB Case and a Non-GB Farm";
    public const string NonGbCaseGbFarmMessage = "You have entered a Non-GB Case and a GB Farm";
    public const string MoveFailedMessage = "Error moving Case";
    public const string LoadFailedMessage = "Failed to load case details.";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty]
    public string NewCphh { get; set; } = string.Empty;

    public CaseRecord? CaseRecord { get; private set; }
    public FarmRecord? CurrentFarm { get; private set; }
    public FarmRecord? NewFarm { get; private set; }

    public string? RbseError { get; private set; }
    public string? CphhError { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>Legacy revealed "Create New Farm" only when the new CPH matched the existing one.</summary>
    public bool ShowNewFarmPrompt { get; private set; }

    public bool CaseFound => CaseRecord is not null;

    /// <summary>Legacy enabled OK only after Check confirmed a valid, different destination farm.</summary>
    public bool CanMove => NewFarm is not null && CphhError is null;

    public string EartagDisplay =>
        string.Join(" ", new[] { CaseRecord?.EartagHerdmark, CaseRecord?.Eartag }
            .Where(v => !string.IsNullOrWhiteSpace(v)));

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Rbse))
        {
            await LookUpCaseAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostLookUpAsync()
    {
        await LookUpCaseAsync();
        return Page();
    }

    /// <summary>Legacy btnCheck: validates the destination farm before the move is offered.</summary>
    public async Task<IActionResult> OnPostCheckAsync()
    {
        await LookUpCaseAsync();
        if (!CaseFound) return Page();

        await CheckNewFarmAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMoveAsync()
    {
        await LookUpCaseAsync();
        if (!CaseFound) return Page();

        await CheckNewFarmAsync();
        if (!CanMove) return Page();

        var rbse = RbseHelper.ParseToRaw(Rbse);
        var newCphh = CphhNormalizer.Normalize(NewCphh);

        MoveCaseResult result;
        try
        {
            var userId = await currentUserService.GetUserIdAsync();
            result = await caseService.MoveCaseAsync(rbse, newCphh, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MoveCase stored procedure threw an exception");
            ErrorMessage = MoveFailedMessage;
            return Page();
        }

        if (result != MoveCaseResult.Success)
        {
            logger.LogWarning("MoveCase returned {Result}", result);
            ErrorMessage = result switch
            {
                MoveCaseResult.RbseNotFound => CaseNotFoundMessage,
                MoveCaseResult.NewFarmNotFound => FarmNotFoundMessage,
                _ => MoveFailedMessage
            };
            return Page();
        }

        TempData[MaintenanceConfirmationModel.TitleKey] = "Case move complete";
        TempData[MaintenanceConfirmationModel.SummaryKey] =
            $"Case {RbseHelper.Format(rbse)} was moved to {BseFormat.FormatCphh(newCphh)}";
        TempData[MaintenanceConfirmationModel.MessageKey] =
            "If no other case remained on the original farm, that farm record has been removed.";

        return RedirectToPage("/MaintenanceConfirmation");
    }

    private async Task LookUpCaseAsync()
    {
        var rbse = RbseHelper.ParseToRaw(Rbse);

        if (rbse.Length == 0)
        {
            RbseError = CaseNotFoundMessage;
            return;
        }

        try
        {
            CaseRecord = await caseService.GetCaseAsync(rbse);
            if (CaseRecord is null)
            {
                logger.LogInformation("No case found for move lookup");
                RbseError = CaseNotFoundMessage;
                return;
            }

            if (!string.IsNullOrWhiteSpace(CaseRecord.Cphh))
            {
                CurrentFarm = await farmService.GetByCphhAsync(CaseRecord.Cphh);
            }
        }
        catch (Exception ex)
        {
            CaseRecord = null;
            logger.LogError(ex, "Failed to load case details for move");
            ErrorMessage = LoadFailedMessage;
        }
    }

    private async Task CheckNewFarmAsync()
    {
        var newCphh = CphhNormalizer.Normalize(NewCphh);
        var currentCphh = CphhNormalizer.Normalize(CaseRecord?.Cphh);

        if (newCphh.Length == 0)
        {
            CphhError = FarmNotFoundMessage;
            return;
        }

        if (string.Equals(newCphh, currentCphh, StringComparison.Ordinal))
        {
            CphhError = SameCphhMessage;
            return;
        }

        try
        {
            NewFarm = await farmService.GetByCphhAsync(newCphh);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load destination farm for move");
            ErrorMessage = LoadFailedMessage;
            return;
        }

        if (NewFarm is null)
        {
            CphhError = FarmNotFoundMessage;

            // Legacy offered farm creation only when the CPH (first 9 digits) was unchanged.
            ShowNewFarmPrompt = newCphh.Length >= 9 && currentCphh.Length >= 9
                && newCphh[..9] == currentCphh[..9];
            return;
        }

        // Legacy blocked mixing GB and Non-GB records in either direction.
        if (NewFarm.IsNonGBFarm && !CaseRecord!.IsNonGbCase)
        {
            CphhError = GbCaseNonGbFarmMessage;
            NewFarm = null;
        }
        else if (!NewFarm.IsNonGBFarm && CaseRecord!.IsNonGbCase)
        {
            CphhError = NonGbCaseGbFarmMessage;
            NewFarm = null;
        }
    }
}
