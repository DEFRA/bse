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
public class RbseChangeModel(
    ICaseService caseService,
    IFarmService farmService,
    ICurrentUserService currentUserService,
    ILogger<RbseChangeModel> logger) : PageModel
{
    public const string CaseNotFoundMessage = "A case with this RBSE was not found";
    public const string NewRbseRequiredMessage = "You must enter a new RBSE";
    public const string SameRbseMessage = "The Old and New RBSEs are the same";
    public const string NewRbseExistsMessage = "This Case already exists in the Database.";
    public const string ChangeFailedMessage = "Error changing the RBSE";
    public const string LoadFailedMessage = "Failed to load case details.";

    [BindProperty(SupportsGet = true)]
    public string OldRbse { get; set; } = string.Empty;

    [BindProperty]
    public string NewRbse { get; set; } = string.Empty;

    public CaseRecord? Case { get; private set; }
    public FarmRecord? Farm { get; private set; }
    public string? OldRbseError { get; private set; }
    public string? NewRbseError { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>Legacy enabled the New RBSE field and OK button only once the case was found.</summary>
    public bool CaseFound => Case is not null;

    /// <summary>Legacy joined the herdmark and eartag with a single space.</summary>
    public string EartagDisplay =>
        string.Join(" ", new[] { Case?.EartagHerdmark, Case?.Eartag }
            .Where(v => !string.IsNullOrWhiteSpace(v)));

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(OldRbse))
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

    public async Task<IActionResult> OnPostChangeAsync()
    {
        await LookUpCaseAsync();
        if (!CaseFound)
        {
            return Page();
        }

        var oldRbse = RbseHelper.ParseToRaw(OldRbse);
        var newRbse = RbseHelper.ParseToRaw(NewRbse);

        if (newRbse.Length == 0)
        {
            NewRbseError = NewRbseRequiredMessage;
            return Page();
        }

        if (string.Equals(oldRbse, newRbse, StringComparison.OrdinalIgnoreCase))
        {
            NewRbseError = SameRbseMessage;
            return Page();
        }

        ChangeRbseResult result;
        try
        {
            var userId = await currentUserService.GetUserIdAsync();
            result = await caseService.ChangeRbseAsync(oldRbse, newRbse, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChangeRBSE stored procedure threw an exception");
            ErrorMessage = ChangeFailedMessage;
            return Page();
        }

        switch (result)
        {
            case ChangeRbseResult.Success:
                break;

            case ChangeRbseResult.NewRbseAlreadyExists:
                NewRbseError = NewRbseExistsMessage;
                return Page();

            case ChangeRbseResult.OldRbseNotFound:
                OldRbseError = CaseNotFoundMessage;
                return Page();

            default:
                logger.LogWarning("ChangeRBSE returned {Result}", result);
                ErrorMessage = ChangeFailedMessage;
                return Page();
        }

        TempData[MaintenanceConfirmationModel.TitleKey] = "RBSE change complete";
        TempData[MaintenanceConfirmationModel.SummaryKey] =
            $"{RbseHelper.Format(oldRbse)} changed to {RbseHelper.Format(newRbse)}";
        TempData[MaintenanceConfirmationModel.MessageKey] =
            "All records linked to the case, including relations, tests, feeds and batch assignments, "
            + "now use the new RBSE.";

        return RedirectToPage("/MaintenanceConfirmation");
    }

    private async Task LookUpCaseAsync()
    {
        var rbse = RbseHelper.ParseToRaw(OldRbse);

        if (rbse.Length == 0)
        {
            OldRbseError = CaseNotFoundMessage;
            return;
        }

        try
        {
            Case = await caseService.GetCaseAsync(rbse);
            if (Case is null)
            {
                logger.LogInformation(
                    "No case found for RBSE change lookup ({Length} characters supplied)", rbse.Length);
                OldRbseError = CaseNotFoundMessage;
                return;
            }

            if (!string.IsNullOrWhiteSpace(Case.Cphh))
            {
                Farm = await farmService.GetByCphhAsync(Case.Cphh);
            }
        }
        catch (Exception ex)
        {
            Case = null;
            logger.LogError(ex, "Failed to load case details for RBSE change");
            ErrorMessage = LoadFailedMessage;
        }
    }
}
