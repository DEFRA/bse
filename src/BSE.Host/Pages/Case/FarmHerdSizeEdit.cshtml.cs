using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class FarmHerdSizeEditModel(
    ICaseService caseService,
    IFarmService farmService,
    IHerdSizeRepository herdSizeRepo) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    [BindProperty] public FarmModel.HerdSizeFormViewModel HerdSize { get; set; } = new();
    [BindProperty] public string RowStampBase64 { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var @case = await caseService.GetCaseAsync(Rbse);
        if (@case?.Cphh is not { } cphh)
            return RedirectToPage("/Case/Farm", new { rbse = Rbse });

        var record = (await farmService.GetHerdSizesAsync(cphh)).FirstOrDefault(h => h.ID == Id);
        if (record is null)
            return RedirectToPage("/Case/Farm", new { rbse = Rbse });

        HerdSize = new FarmModel.HerdSizeFormViewModel
        {
            HerdYear = record.HerdYear,
            TotalSize = record.TotalSize,
            Lactation1Size = record.Lactation1Size,
            Lactation2Size = record.Lactation2Size,
            Lactation3Size = record.Lactation3Size,
            Lactation4Size = record.Lactation4Size,
            Lactation5Size = record.Lactation5Size,
            Lactation6Size = record.Lactation6Size,
            Lactation7Size = record.Lactation7Size,
            Lactation8Size = record.Lactation8Size,
            Lactation9Size = record.Lactation9Size,
            Lactation10Size = record.Lactation10Size,
            Lactation10PlusSize = record.Lactation10PlusSize
        };

        RowStampBase64 = record.RowStamp is not null ? Convert.ToBase64String(record.RowStamp) : string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (HerdSize.HerdYear < 1980 || HerdSize.HerdYear > 2100)
            ModelState.AddModelError("HerdSize.HerdYear", "Year must be a valid year (1980–2100).");

        if (HerdSize.TotalSize <= 0)
            ModelState.AddModelError("HerdSize.TotalSize", "Total size must be greater than zero.");

        if (!ModelState.IsValid)
            return Page();

        var rowStamp = string.IsNullOrWhiteSpace(RowStampBase64)
            ? null
            : Convert.FromBase64String(RowStampBase64);

        await herdSizeRepo.UpdateAsync(new UpdateHerdSizeCommand(
            Id,
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
            (short)HerdSize.Lactation10PlusSize,
            rowStamp));

        var lacTotal = HerdSize.Lactation1Size + HerdSize.Lactation2Size + HerdSize.Lactation3Size
                     + HerdSize.Lactation4Size + HerdSize.Lactation5Size + HerdSize.Lactation6Size
                     + HerdSize.Lactation7Size + HerdSize.Lactation8Size + HerdSize.Lactation9Size
                     + HerdSize.Lactation10Size + HerdSize.Lactation10PlusSize;

        if (lacTotal > 0 && lacTotal != HerdSize.TotalSize)
            TempData["Warning"] = $"Herd size for {HerdSize.HerdYear} updated, but the lactation total ({lacTotal}) does not equal the total herd size ({HerdSize.TotalSize}).";
        else
            TempData["Success"] = $"Herd size for {HerdSize.HerdYear} updated.";

        return RedirectToPage("/Case/Farm", new { rbse = Rbse });
    }
}