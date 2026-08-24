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
public class VlaEditModel(
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

        // Mirrors legacy EmptyPurchaseFields: clear purchase data when Origin is not Purchased
        if (Case.Origin != "P")
        {
            Case.PurchaseDate        = null;
            Case.PurchaseAgeInMonths = null;
            Case.PurchasedCounty     = null;
        }

        ValidateVlaDomainRules();
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

    // Mirrors legacy UpdateSessionWithCaseDetails validation methods
    private void ValidateVlaDomainRules()
    {
        var today     = DateTime.Today;
        var formADate = Case.FormADate?.Date;

        // BirthDate: after 1970-01-01, before FormADate or today
        if (Case.BirthDate.HasValue)
        {
            if (Case.BirthDate.Value.Date < new DateTime(1970, 1, 1))
                ModelState.AddModelError("Case.BirthDate", "Birth date must be after 31/12/1969.");
            else
            {
                var limit = formADate ?? today;
                if (Case.BirthDate.Value.Date >= limit)
                    ModelState.AddModelError("Case.BirthDate",
                        formADate.HasValue ? "Birth date must be before the Form A date." : "Birth date must be a past date.");
            }
        }

        // PurchaseDate: after BirthDate, before FormADate or today
        if (Case.PurchaseDate.HasValue)
        {
            if (Case.BirthDate.HasValue && Case.PurchaseDate.Value.Date <= Case.BirthDate.Value.Date)
                ModelState.AddModelError("Case.PurchaseDate", "Purchase date must be after the birth date and before the Form A date.");
            else
            {
                var limit = formADate ?? today;
                if (Case.PurchaseDate.Value.Date >= limit)
                    ModelState.AddModelError("Case.PurchaseDate",
                        formADate.HasValue ? "Purchase date must be before the Form A date." : "Purchase date must be a past date.");
            }
        }

        // HerdEntryDate: before FormADate or today
        if (Case.HerdEntryDate.HasValue)
        {
            var limit = formADate ?? today;
            if (Case.HerdEntryDate.Value.Date > limit)
                ModelState.AddModelError("Case.HerdEntryDate",
                    formADate.HasValue ? "Herd entry date must be before the Form A date." : "Herd entry date must be a past date.");
        }

        // OnsetDate: after BirthDate (if set), before FormADate or today
        if (Case.OnsetDate.HasValue)
        {
            if (Case.BirthDate.HasValue && Case.OnsetDate.Value.Date <= Case.BirthDate.Value.Date)
                ModelState.AddModelError("Case.OnsetDate", "Onset date must be after the date of birth and before the Form A date.");
            else
            {
                var limit = formADate ?? today;
                if (Case.OnsetDate.Value.Date > limit)
                    ModelState.AddModelError("Case.OnsetDate",
                        formADate.HasValue ? "Onset date must be before the Form A date." : "Onset date must be a past date.");
            }
        }

        // MonthsPregnant and MonthsPostCalving: cannot both have values
        if (Case.MonthsPregnant.HasValue && Case.MonthsPostCalving.HasValue)
            ModelState.AddModelError("Case.MonthsPostCalving",
                "You cannot enter values for both months pregnant and months post calving.");

        // MonthsPregnant: 1–9; MonthsPostCalving: 1–3
        if (Case.MonthsPregnant.HasValue && (Case.MonthsPregnant.Value < 1 || Case.MonthsPregnant.Value > 9))
            ModelState.AddModelError("Case.MonthsPregnant", "Months pregnant must be between 1 and 9.");
        if (Case.MonthsPostCalving.HasValue && (Case.MonthsPostCalving.Value < 1 || Case.MonthsPostCalving.Value > 3))
            ModelState.AddModelError("Case.MonthsPostCalving", "Months post calving must be between 1 and 3.");

        // SlaughterDate: after FormADate (or BirthDate) and not in the future
        if (Case.SlaughterDate.HasValue)
        {
            if (Case.SlaughterDate.Value.Date > today)
                ModelState.AddModelError("Case.SlaughterDate", "Slaughter date must not be in the future.");
            if (formADate.HasValue && Case.SlaughterDate.Value.Date < formADate.Value)
                ModelState.AddModelError("Case.SlaughterDate", "Slaughter date must be after the Form A date.");
            else if (!formADate.HasValue && Case.BirthDate.HasValue && Case.SlaughterDate.Value.Date < Case.BirthDate.Value.Date)
                ModelState.AddModelError("Case.SlaughterDate", "Slaughter date must be after the birth date.");
        }
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
