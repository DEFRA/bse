using BSE.Host.Services;
using BSE.Modules.Batch.Services;
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
public class DeleteModel(
    ICaseService caseService,
    IFarmService farmService,
    IBatchService batchService,
    ICurrentUserService currentUserService,
    ILogger<DeleteModel> logger) : PageModel
{
    public const string CaseNotFoundMessage = "Cannot find a case with this RBSE";
    public const string VlaDataMessage = "Cannot delete — VLA have entered data on this case";
    public const string FarmDeleteMessage = "Farm will be deleted along with case";
    public const string DeleteFailedMessage = "Error deleting Case";
    public const string LoadFailedMessage = "Failed to load case details.";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public CaseRecord? CaseRecord { get; private set; }
    public FarmRecord? Farm { get; private set; }
    public int NumberOfCasesOnFarm { get; private set; }
    public string? RbseError { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>Legacy VLATablesEmpty: the OK button stayed disabled when VLA data existed.</summary>
    public bool HasVlaData { get; private set; }

    public bool CaseFound => CaseRecord is not null;

    public bool CanDelete => CaseFound && !HasVlaData;

    /// <summary>Legacy showed "Farm will be deleted along with case" when this was the farm's only case.</summary>
    public bool FarmWillBeDeleted => NumberOfCasesOnFarm == 1;

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

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await LookUpCaseAsync();

        if (!CaseFound)
        {
            return Page();
        }

        // Legacy kept the OK button disabled while VLA data existed; re-checked here
        // because a disabled control is not a server-side guarantee.
        if (HasVlaData)
        {
            ErrorMessage = VlaDataMessage;
            return Page();
        }

        var rbse = RbseHelper.ParseToRaw(Rbse);
        var farmWillBeDeleted = FarmWillBeDeleted;

        DeleteCaseResult result;
        try
        {
            var userId = await currentUserService.GetUserIdAsync();
            result = await caseService.DeleteCaseAsync(rbse, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeleteCase stored procedure threw an exception");
            ErrorMessage = DeleteFailedMessage;
            return Page();
        }

        if (result != DeleteCaseResult.Success)
        {
            logger.LogWarning("DeleteCase returned {Result}", result);
            ErrorMessage = result switch
            {
                DeleteCaseResult.RbseNotFound => CaseNotFoundMessage,
                DeleteCaseResult.HasLinkedRecords => VlaDataMessage,
                _ => DeleteFailedMessage
            };
            return Page();
        }

        TempData[MaintenanceConfirmationModel.TitleKey] = "Case deleted";
        TempData[MaintenanceConfirmationModel.SummaryKey] = $"Case {RbseHelper.Format(rbse)} was deleted";
        TempData[MaintenanceConfirmationModel.MessageKey] = farmWillBeDeleted
            ? "This was the only case on the farm, so the farm record was deleted as well."
            : "Other cases remain on the farm, so the farm record has been kept.";

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
            var details = await caseService.GetCaseDetailsAsync(rbse);
            CaseRecord = details?.Case;

            if (CaseRecord is null)
            {
                logger.LogInformation("No case found for delete lookup");
                RbseError = CaseNotFoundMessage;
                return;
            }

            if (!string.IsNullOrWhiteSpace(CaseRecord.Cphh))
            {
                Farm = await farmService.GetByCphhAsync(CaseRecord.Cphh);
                NumberOfCasesOnFarm = await farmService.GetCaseCountByCphhAsync(CaseRecord.Cphh);
            }

            var batches = await batchService.GetBatchNumbersByRbseAsync(rbse);

            // Mirrors legacy VLATablesEmpty across feeds, clinical, relations,
            // other owners, clinical visits and batch links.
            HasVlaData =
                details!.Feeds.Count > 0
                || details.Clinical is not null
                || details.Relations.Count > 0
                || details.OtherOwners.Count > 0
                || details.ClinicalVisits.Count > 0
                || batches.Count > 0;
        }
        catch (Exception ex)
        {
            CaseRecord = null;
            logger.LogError(ex, "Failed to load case details for delete");
            ErrorMessage = LoadFailedMessage;
        }
    }
}
