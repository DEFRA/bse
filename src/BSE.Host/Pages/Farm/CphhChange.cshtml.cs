using BSE.Host.Helpers;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize(Policy = "DEFRAMaintenance")]
public class CphhChangeModel(
    IFarmService farm,
    ICurrentUserService currentUser,
    ILogger<CphhChangeModel> logger) : PageModel
{
    public const string FarmNotFoundMessage = "A farm with this CPHH was not found";
    public const string NewCphhRequiredMessage = "You must enter a new CPHH";
    public const string SameCphhMessage = "The Old and New CPHHs are the same";
    public const string NewCphhExistsMessage = "This Farm already exists in the database.";
    public const string UpdateFailedMessage = "Error updating the CPHH";
    public const string LoadFailedMessage = "Failed to load farm details.";
    public const string OldCphhRequiredMessage = "You must enter the old CPHH";

    [BindProperty(SupportsGet = true)] public string OldCphh { get; set; } = string.Empty;
    [BindProperty] public string NewCphh { get; set; } = string.Empty;

    public FarmRecord? Farm { get; private set; }
    public int ConfirmedCases { get; private set; }
    public string? OldCphhError { get; private set; }
    public string? NewCphhError { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>Legacy enabled the New CPHH field and OK button only once the old CPHH was confirmed to exist.</summary>
    public bool FarmFound => Farm is not null;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(OldCphh))
        {
            await LookUpFarmAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostLookUpAsync()
    {
        await LookUpFarmAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostChangeAsync()
    {
        await LookUpFarmAsync();
        if (!FarmFound)
        {
            return Page();
        }

        var oldCphh = CphhNormalizer.Normalize(OldCphh);
        var newCphh = CphhNormalizer.Normalize(NewCphh);

        if (newCphh.Length == 0)
        {
            NewCphhError = NewCphhRequiredMessage;
            return Page();
        }

        if (string.Equals(oldCphh, newCphh, StringComparison.Ordinal))
        {
            NewCphhError = SameCphhMessage;
            return Page();
        }

        if (await FarmExistsAsync(newCphh))
        {
            NewCphhError = NewCphhExistsMessage;
            return Page();
        }

        ChangeCphhResult result;
        try
        {
            var userId = await currentUser.GetUserIdAsync();
            result = await farm.ChangeCphhAsync(oldCphh, newCphh, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChangeCPHH stored procedure threw an exception");
            ErrorMessage = UpdateFailedMessage;
            return Page();
        }

        if (result != ChangeCphhResult.Success)
        {
            logger.LogWarning("ChangeCPHH returned {Result}", result);
            ErrorMessage = result == ChangeCphhResult.OldCphhNotFoundOrNewCphhAlreadyExists
                ? NewCphhExistsMessage
                : UpdateFailedMessage;
            return Page();
        }

        TempData[MaintenanceConfirmationModel.TitleKey] = "CPHH change complete";
        TempData[MaintenanceConfirmationModel.SummaryKey] =
            $"{BseFormat.FormatCphh(oldCphh)} changed to {BseFormat.FormatCphh(newCphh)}";
        TempData[MaintenanceConfirmationModel.MessageKey] =
            "Location information for the farm, including County, AHO and ADNS Region, has not been changed. " +
            "Check these details on the farm record and amend them if necessary.";

        return RedirectToPage("/MaintenanceConfirmation");
    }

    private async Task LookUpFarmAsync()
    {
        var cphh = CphhNormalizer.Normalize(OldCphh);

        if (cphh.Length == 0)
        {
            OldCphhError = OldCphhRequiredMessage;
            return;
        }

        try
        {
            Farm = await farm.GetByCphhAsync(cphh);
            if (Farm is null)
            {
                logger.LogInformation(
                    "No farm found for CPHH lookup ({DigitCount} digits supplied)", cphh.Length);
                OldCphhError = FarmNotFoundMessage;
                return;
            }

            ConfirmedCases = await farm.GetConfirmedCaseCountAsync(cphh);
        }
        catch (Exception ex)
        {
            Farm = null;
            logger.LogError(ex, "Failed to load farm details for CPHH change");
            ErrorMessage = LoadFailedMessage;
        }
    }

    private async Task<bool> FarmExistsAsync(string cphh) =>
        await farm.GetByCphhAsync(cphh) is not null;
}
