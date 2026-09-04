using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class FarmLinkedFarmEditModel(
    ICaseService caseService,
    IFarmService farmService,
    IFarmRelationRepository relationRepo) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    [BindProperty] public string RelatedCphh { get; set; } = string.Empty;
    [BindProperty] public string RowStampBase64 { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var @case = await caseService.GetCaseAsync(Rbse);
        if (@case?.Cphh is not { } cphh)
            return RedirectToPage("/Case/Farm", new { rbse = Rbse });

        var linked = (await farmService.GetRelatedFarmsAsync(cphh)).FirstOrDefault(f => f.ID == Id);
        if (linked is null)
            return RedirectToPage("/Case/Farm", new { rbse = Rbse });

        RelatedCphh = linked.RelatedCPHH;
        RowStampBase64 = linked.RowStamp is not null ? Convert.ToBase64String(linked.RowStamp) : string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var @case = await caseService.GetCaseAsync(Rbse);
        if (@case?.Cphh is not { } cphh)
            return RedirectToPage("/Case/Farm", new { rbse = Rbse });

        if (string.IsNullOrWhiteSpace(RelatedCphh))
            ModelState.AddModelError(nameof(RelatedCphh), "Enter a CPHH.");

        var normalisedCphh = CphhNormalizer.Normalize(RelatedCphh);

        if (!string.IsNullOrWhiteSpace(normalisedCphh) && normalisedCphh.Length > 11)
            ModelState.AddModelError(nameof(RelatedCphh), "CPHH must be 11 characters or fewer.");

        if (!string.IsNullOrWhiteSpace(normalisedCphh) &&
            string.Equals(CphhNormalizer.Normalize(cphh), normalisedCphh, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(RelatedCphh), "Cannot link a farm to itself.");

        if (!string.IsNullOrWhiteSpace(normalisedCphh))
        {
            var existing = await farmService.GetRelatedFarmsAsync(cphh);
            if (existing.Any(f => string.Equals(f.RelatedCPHH, normalisedCphh, StringComparison.OrdinalIgnoreCase) && f.ID != Id))
                ModelState.AddModelError(nameof(RelatedCphh), "CPHH already exists in the Linked Farms list.");
        }

        if (!ModelState.IsValid)
            return Page();

        var rowStamp = Convert.FromBase64String(RowStampBase64);
        await relationRepo.UpdateAsync(Id, normalisedCphh, rowStamp);

        TempData["Success"] = "Linked farm updated.";
        return RedirectToPage("/Case/Farm", new { rbse = Rbse });
    }
}