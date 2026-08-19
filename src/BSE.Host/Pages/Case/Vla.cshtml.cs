using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class VlaModel(
    ICaseService caseService,
    ICurrentUserService currentUserService,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IConfiguration configuration) : PageModel
{
    private const string RowStampKey = "VlaEdit_RowStamp_{0}";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty]
    public VlaEditViewModel Case { get; set; } = new();

    public string? ConcurrencyError { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    public IEnumerable<ILookupItem> BirthDateSourceOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> SexOptions            { get; private set; } = [];
    public IEnumerable<ILookupItem> BreedOptions          { get; private set; } = [];
    public IEnumerable<ILookupItem> OriginOptions         { get; private set; } = [];
    public IEnumerable<ILookupItem> PurchasedCountyOptions { get; private set; } = [];

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
        Case = VlaEditViewModel.FromRecord(record);
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        await Task.WhenAll(LoadLookupsAsync(), batchTask);
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
        {
            BatchNumbers = (await batchRepository.GetBatchNumbersByRbseAsync(Rbse)).ToList().AsReadOnly();
            return Page();
        }

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

        TempData["Success"] = $"Case {Rbse} has been updated.";
        return RedirectToPage(new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var t1 = lookups.GetLookupAsync(LookupTableId.BirthDateSource);
        var t2 = lookups.GetLookupAsync(LookupTableId.Sex);
        var t3 = lookups.GetLookupAsync(LookupTableId.Breed);
        var t4 = lookups.GetLookupAsync(LookupTableId.AnimalOrigin);
        var t5 = lookups.GetLookupAsync(LookupTableId.BSECounty);

        await Task.WhenAll(t1, t2, t3, t4, t5);

        BirthDateSourceOptions  = await t1;
        SexOptions              = await t2;
        BreedOptions            = await t3;
        OriginOptions           = await t4;
        PurchasedCountyOptions  = await t5;
    }
}
