using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

/// <summary>
/// Farm tab for a case — mirrors the legacy CaseEntryFarm.aspx Farm tab.
/// Shows: confirmed case count, full farm details, linked farms (add/delete),
/// herd size history (add/delete). Farm field editing is delegated to /Farm/Edit.
/// </summary>
[Authorize]
public class FarmModel(
    ICaseService caseService,
    IFarmService farmService,
    IFarmRelationRepository relationRepo,
    IHerdSizeRepository herdSizeRepo,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IConfiguration configuration) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public CaseRecord? Case { get; private set; }
    public FarmRecord? Farm { get; private set; }
    public int ConfirmedCaseCount { get; private set; }
    public IReadOnlyList<FarmRelationRecord> LinkedFarms { get; private set; } = [];
    public IReadOnlyList<HerdSizeRecord> HerdSizes { get; private set; } = [];
    public string? ADNSRegionName { get; private set; }
    public string? CountyName { get; private set; }
    public string? AHOName { get; private set; }
    public string? HerdTypeName { get; private set; }
    public string? PedigreeTypeName { get; private set; }
    public string? AuthorityCountyName { get; private set; }
    public string? LocalAuthorityName { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    public string SpolSiteUrl { get; private set; } = string.Empty;

    [BindProperty]
    public string? NewLinkedCphh { get; set; }

    [BindProperty]
    public HerdSizeFormViewModel NewHerdSize { get; set; } = new();

    // ── GET ────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        return Page();
    }

    // ── POST: Linked farms ─────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostAddLinkedFarmAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        Case = await caseService.GetCaseAsync(Rbse);
        if (string.IsNullOrWhiteSpace(NewLinkedCphh))
        {
            ModelState.AddModelError(nameof(NewLinkedCphh), "Enter a CPHH to link.");
            await LoadFromCase();
            return Page();
        }

        var normalisedCphh = NewLinkedCphh.Trim().ToUpperInvariant();

        if (normalisedCphh.Length > 11)
        {
            ModelState.AddModelError(nameof(NewLinkedCphh), "CPHH must be 11 characters or fewer.");
            await LoadFromCase();
            return Page();
        }

        if (Case?.Cphh is { } cphh)
        {
            var existing = await farmService.GetRelatedFarmsAsync(cphh);
            if (existing.Any(f => string.Equals(f.RelatedCPHH, normalisedCphh, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(NewLinkedCphh), $"CPHH {normalisedCphh} is already in the Linked Farms list.");
                await LoadFromCase();
                return Page();
            }
            await relationRepo.AddAsync(cphh, normalisedCphh);
        }

        TempData["Success"] = $"Linked farm {normalisedCphh} added.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteLinkedFarmAsync(int id, string rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        var rowStamp = Convert.FromBase64String(rowStampBase64);
        await relationRepo.DeleteAsync(id, rowStamp);
        TempData["Success"] = "Linked farm removed.";
        return RedirectToPage(new { rbse = Rbse });
    }

    // ── POST: Herd sizes ───────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostAddHerdSizeAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        Case = await caseService.GetCaseAsync(Rbse);
        if (Case?.Cphh is not { } cphh)
            return RedirectToPage(new { rbse = Rbse });

        if (NewHerdSize.HerdYear < 1980 || NewHerdSize.HerdYear > 2100)
        {
            ModelState.AddModelError("NewHerdSize.HerdYear", "Year is required and must be a valid year (1980–2100).");
            await LoadFromCase();
            return Page();
        }

        if (NewHerdSize.TotalSize <= 0)
        {
            ModelState.AddModelError("NewHerdSize.TotalSize", "Total size is required and must be greater than zero.");
            await LoadFromCase();
            return Page();
        }

        var cmd = new AddHerdSizeCommand(
            cphh,
            (short)NewHerdSize.HerdYear,
            (short)NewHerdSize.TotalSize,
            (short)NewHerdSize.Lactation1Size,
            (short)NewHerdSize.Lactation2Size,
            (short)NewHerdSize.Lactation3Size,
            (short)NewHerdSize.Lactation4Size,
            (short)NewHerdSize.Lactation5Size,
            (short)NewHerdSize.Lactation6Size,
            (short)NewHerdSize.Lactation7Size,
            (short)NewHerdSize.Lactation8Size,
            (short)NewHerdSize.Lactation9Size,
            (short)NewHerdSize.Lactation10Size,
            (short)NewHerdSize.Lactation10PlusSize);

        await herdSizeRepo.AddAsync(cmd);

        var lacTotal = NewHerdSize.Lactation1Size + NewHerdSize.Lactation2Size + NewHerdSize.Lactation3Size
                     + NewHerdSize.Lactation4Size + NewHerdSize.Lactation5Size + NewHerdSize.Lactation6Size
                     + NewHerdSize.Lactation7Size + NewHerdSize.Lactation8Size + NewHerdSize.Lactation9Size
                     + NewHerdSize.Lactation10Size + NewHerdSize.Lactation10PlusSize;

        if (lacTotal > 0 && lacTotal != NewHerdSize.TotalSize)
            TempData["Warning"] = $"Herd size for {NewHerdSize.HerdYear} added, but the lactation total ({lacTotal}) does not equal the total herd size ({NewHerdSize.TotalSize}).";
        else
            TempData["Success"] = $"Herd size for {NewHerdSize.HerdYear} added.";

        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteHerdSizeAsync(int id, string rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        var rowStamp = Convert.FromBase64String(rowStampBase64);
        await herdSizeRepo.DeleteAsync(id, rowStamp);
        TempData["Success"] = "Herd size record deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        Case = await caseService.GetCaseAsync(Rbse);
        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        await Task.WhenAll(LoadFromCase(), batchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
    }

    private async Task LoadFromCase()
    {
        if (Case?.Cphh is not { } cphh) return;

        var farmTask        = farmService.GetByCphhAsync(cphh);
        var confirmedTask   = farmService.GetConfirmedCaseCountAsync(cphh);
        var linkedTask      = farmService.GetRelatedFarmsAsync(cphh);
        var herdTask        = farmService.GetHerdSizesAsync(cphh);
        var adnsTask        = lookups.GetADNSRegionsAsync();
        var countyTask      = lookups.GetLookupAsync(LookupTableId.BSECounty);
        var ahoTask         = lookups.GetLookupAsync(LookupTableId.AHO);
        var herdTypeTask    = lookups.GetHerdTypesAsync();
        var pedigreeTask    = lookups.GetLookupAsync(LookupTableId.PedigreeType);
        var authCountyTask  = lookups.GetLookupAsync(LookupTableId.AuthorityCounty);

        await Task.WhenAll(farmTask, confirmedTask, linkedTask, herdTask, adnsTask,
                           countyTask, ahoTask, herdTypeTask, pedigreeTask, authCountyTask);

        Farm              = await farmTask;
        ConfirmedCaseCount = await confirmedTask;
        LinkedFarms       = (await linkedTask).ToList().AsReadOnly();
        HerdSizes         = (await herdTask).OrderByDescending(h => h.HerdYear).ToList().AsReadOnly();

        if (Farm is null) return;

        // Resolve ADNS region name
        if (Farm.ADNSRegionID.HasValue)
            ADNSRegionName = (await adnsTask).FirstOrDefault(r => r.Id == Farm.ADNSRegionID.Value)?.Name;

        // Resolve County code → description  (Farm.County FK → luBSECounty.Code)
        if (!string.IsNullOrWhiteSpace(Farm.County))
            CountyName = (await countyTask).FirstOrDefault(c => c.Code == Farm.County.Trim())?.Description;

        // Resolve AHO code → name  (Farm.AHO FK → luAHO.Code)
        if (!string.IsNullOrWhiteSpace(Farm.AHO))
            AHOName = (await ahoTask).FirstOrDefault(a => a.Code == Farm.AHO.Trim())?.Description;

        // Resolve HerdType code → description  (Farm.HerdType FK → luHerdType.Code)
        if (!string.IsNullOrWhiteSpace(Farm.HerdType))
            HerdTypeName = (await herdTypeTask).FirstOrDefault(h => h.Code == Farm.HerdType.Trim())?.Description;

        // Resolve PedigreeType code → description  (Farm.PedigreeType FK → luPedigreeType.Code)
        if (!string.IsNullOrWhiteSpace(Farm.PedigreeType))
            PedigreeTypeName = (await pedigreeTask).FirstOrDefault(p => p.Code == Farm.PedigreeType.Trim())?.Description;

        // Resolve AuthorityCounty ID → county name
        if (Farm.AuthorityCountyID.HasValue)
            AuthorityCountyName = (await authCountyTask).FirstOrDefault(a => a.Id == Farm.AuthorityCountyID.Value)?.Description;

        // Resolve LocalAuthority ID → name (filter by county so the SP is called with the right county)
        if (Farm.AuthorityID.HasValue && Farm.AuthorityCountyID.HasValue)
        {
            var authorities = await lookups.GetAuthoritiesByCountyAsync(Farm.AuthorityCountyID.Value);
            LocalAuthorityName = authorities.FirstOrDefault(a => a.Id == Farm.AuthorityID.Value)?.Name;
        }
    }

    // ── View models ────────────────────────────────────────────────────────────

    public class HerdSizeFormViewModel
    {
        public int HerdYear { get; set; }
        public int TotalSize { get; set; }
        public int Lactation1Size { get; set; }
        public int Lactation2Size { get; set; }
        public int Lactation3Size { get; set; }
        public int Lactation4Size { get; set; }
        public int Lactation5Size { get; set; }
        public int Lactation6Size { get; set; }
        public int Lactation7Size { get; set; }
        public int Lactation8Size { get; set; }
        public int Lactation9Size { get; set; }
        public int Lactation10Size { get; set; }
        public int Lactation10PlusSize { get; set; }
    }
}
