using BSE.Modules.CaseManagement.Services;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.FarmManagement.Repositories;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class FarmLinkedFarmAddModel(
    ICaseService caseService,
    IFarmService farmService,
    ICaseFarmDraftStateService farmDraftState) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty] public string? RelatedCphh { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var @case = await caseService.GetCaseAsync(Rbse);
        if (@case is null)
            return RedirectToPage("/Home");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var @case = await caseService.GetCaseAsync(Rbse);
        if (@case?.Cphh is not { } cphh)
            return RedirectToPage("/Case/Farm", new { rbse = Rbse });

        var normalisedCphh = CphhNormalizer.Normalize(RelatedCphh);

        if (string.IsNullOrWhiteSpace(normalisedCphh))
            ModelState.AddModelError(nameof(RelatedCphh), "Enter a CPHH.");

        if (!string.IsNullOrWhiteSpace(normalisedCphh) && normalisedCphh.Length != 11)
            ModelState.AddModelError(nameof(RelatedCphh), "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");

        if (string.Equals(CphhNormalizer.Normalize(cphh), normalisedCphh, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(RelatedCphh), "Cannot link a farm to itself.");

        var draft = await farmDraftState.GetAsync(Rbse);
        var existing = draft?.LinkedFarms.Select(f => f.RelatedCphh).ToList() ??
                       (await farmService.GetRelatedFarmsAsync(cphh)).Select(x => x.RelatedCPHH).ToList();
        if (existing.Any(f => string.Equals(CphhNormalizer.Normalize(f), normalisedCphh, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(RelatedCphh), $"CPHH {normalisedCphh} is already in the Linked Farms list.");

        if (!ModelState.IsValid)
            return Page();

        draft ??= new CaseFarmDraftState { Rbse = Rbse, Cphh = cphh };
        draft.LinkedFarms.Add(new CaseFarmDraftLinkedFarmItem
        {
            Id = 0,
            RelatedCphh = normalisedCphh,
            RowStampBase64 = string.Empty,
            Status = string.Empty
        });

        await farmDraftState.SetAsync(draft);
        TempData["Success"] = $"Linked farm {normalisedCphh} added to pending changes.";
        return RedirectToPage("/Case/Farm", new { rbse = Rbse });
    }
}
