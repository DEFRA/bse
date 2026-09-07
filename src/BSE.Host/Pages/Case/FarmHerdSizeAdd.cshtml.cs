using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class FarmHerdSizeAddModel(
    ICaseService caseService,
    IHerdSizeRepository herdSizeRepo) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty] public FarmModel.HerdSizeFormViewModel HerdSize { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var @case = await caseService.GetCaseAsync(Rbse);
        if (@case?.Cphh is null)
            return RedirectToPage("/Case/Farm", new { rbse = Rbse });

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var @case = await caseService.GetCaseAsync(Rbse);
        if (@case?.Cphh is not { } cphh)
            return RedirectToPage("/Case/Farm", new { rbse = Rbse });

        if (HerdSize.HerdYear < 1980 || HerdSize.HerdYear > 2100)
            ModelState.AddModelError("HerdSize.HerdYear", "Year is required and must be a valid year (1980–2100).");

        if (HerdSize.TotalSize <= 0)
            ModelState.AddModelError("HerdSize.TotalSize", "Total size is required and must be greater than zero.");

        if (!ModelState.IsValid)
            return Page();

        var cmd = new AddHerdSizeCommand(
            cphh,
            (short)HerdSize.HerdYear,
            (short)HerdSize.TotalSize,
            (short)HerdSize.Lactation1Size,
            (short)HerdSize.Lactation2Size,
            (short)HerdSize.Lactation3Size,
            (short)HerdSize.Lactation4Size,
            (short)HerdSize.Lactation5Size,
            (short)HerdSize.Lactation6Size,
            (short)HerdSize.Lactation7Size,
            (short)HerdSize.Lactation8Size,
            (short)HerdSize.Lactation9Size,
            (short)HerdSize.Lactation10Size,
            (short)HerdSize.Lactation10PlusSize);

        await herdSizeRepo.AddAsync(cmd);

        var lacTotal = HerdSize.Lactation1Size + HerdSize.Lactation2Size + HerdSize.Lactation3Size
                     + HerdSize.Lactation4Size + HerdSize.Lactation5Size + HerdSize.Lactation6Size
                     + HerdSize.Lactation7Size + HerdSize.Lactation8Size + HerdSize.Lactation9Size
                     + HerdSize.Lactation10Size + HerdSize.Lactation10PlusSize;

        if (lacTotal > 0 && lacTotal != HerdSize.TotalSize)
            TempData["Warning"] = $"Herd size for {HerdSize.HerdYear} added, but the lactation total ({lacTotal}) does not equal the total herd size ({HerdSize.TotalSize}).";
        else
            TempData["Success"] = $"Herd size for {HerdSize.HerdYear} added.";

        return RedirectToPage("/Case/Farm", new { rbse = Rbse });
    }
}
