using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.FarmManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class FarmLinkedFarmAddModel(
    ICaseService caseService,
    IFarmService farmService,
    IFarmRelationRepository relationRepo) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty] public string? RelatedCphh { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var @case = await caseService.GetCaseAsync(Rbse);
        if (@case is null)
            return RedirectToPage("/Case/Lookup");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var @case = await caseService.GetCaseAsync(Rbse);
        if (@case?.Cphh is not { } cphh)
            return RedirectToPage("/Case/Farm", new { rbse = Rbse });

        if (string.IsNullOrWhiteSpace(RelatedCphh))
            ModelState.AddModelError(nameof(RelatedCphh), "Enter a CPHH to link.");

        var normalisedCphh = (RelatedCphh ?? string.Empty).Trim().ToUpperInvariant();
        if (normalisedCphh.Length > 11)
            ModelState.AddModelError(nameof(RelatedCphh), "CPHH must be 11 characters or fewer.");

        if (ModelState.IsValid)
        {
            var existing = await farmService.GetRelatedFarmsAsync(cphh);
            if (existing.Any(f => string.Equals(f.RelatedCPHH, normalisedCphh, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(RelatedCphh), $"CPHH {normalisedCphh} is already in the Linked Farms list.");
        }

        if (!ModelState.IsValid)
            return Page();

        await relationRepo.AddAsync(cphh, normalisedCphh);
        TempData["Success"] = $"Linked farm {normalisedCphh} added.";
        return RedirectToPage("/Case/Farm", new { rbse = Rbse });
    }
}
