using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class OtherOwnersModel(
    IOtherOwnerRepository ownerRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public IReadOnlyList<OtherOwnerRecord> OtherOwners { get; private set; } = [];
    public IEnumerable<LookupItem> OwnerTypes { get; private set; } = [];

    // ── Sort/pagination ───────────────────────────────────────────────────────────
    private const int PageSize = 10;
    [BindProperty(SupportsGet = true)] public int    OPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string OSort { get; set; } = "type";
    [BindProperty(SupportsGet = true)] public string ODir  { get; set; } = "asc";
    public int OtherOwnersTotalPages { get; private set; } = 1;
    public int OtherOwnersTotalCount { get; private set; }

    [BindProperty] public NewOwnerViewModel NewOwner { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddOwnerAsync()
    {
        // Must have either Name or CPHH (mirrors legacy lblOwnerError)
        if (string.IsNullOrWhiteSpace(NewOwner.Type))
            ModelState.AddModelError(string.Empty, "Owner type is required.");
        if (string.IsNullOrWhiteSpace(NewOwner.Name) && string.IsNullOrWhiteSpace(NewOwner.Cphh))
            ModelState.AddModelError(string.Empty, "You must enter either an owner name or a CPHH.");

        // Only one Previous-type owner allowed (mirrors legacy lblPreviousError)
        if (!string.IsNullOrWhiteSpace(NewOwner.Type))
        {
            var allOwners = await ownerRepository.GetByRbseAsync(Rbse);
            var typeDesc  = OwnerTypes.FirstOrDefault(t => t.Code == NewOwner.Type)?.Description ?? "";
            if (typeDesc.Contains("Previous", StringComparison.OrdinalIgnoreCase) &&
                allOwners.Any(o => o.Type == NewOwner.Type))
                ModelState.AddModelError(string.Empty, "You can only have one owner of type Previous.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var command = new AddOtherOwnerCommand(
            Rbse: Rbse,
            Type: NewOwner.Type,
            Name: string.IsNullOrWhiteSpace(NewOwner.Name) ? null : NewOwner.Name,
            Cphh: string.IsNullOrWhiteSpace(NewOwner.Cphh) ? null : NewOwner.Cphh);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await ownerRepository.AddAsync(command, conn, tx);
        tx.Commit();

        TempData["Success"] = "Owner record added.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteOwnerAsync(int ownerId, string rowStampBase64)
    {
        var rowStamp = Convert.FromBase64String(rowStampBase64);
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await ownerRepository.DeleteAsync(ownerId, conn, tx);
        tx.Commit();

        TempData["Success"] = "Owner record deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    private async Task LoadAsync()
    {
        var allOwners = (await ownerRepository.GetByRbseAsync(Rbse)).ToList();
        OtherOwnersTotalCount = allOwners.Count;
        OtherOwnersTotalPages = Math.Max(1, (int)Math.Ceiling(allOwners.Count / (double)PageSize));
        OPage = Math.Clamp(OPage, 1, OtherOwnersTotalPages);
        IEnumerable<OtherOwnerRecord> sorted = OSort == "cphh"
            ? (ODir == "desc" ? allOwners.OrderByDescending(o => o.Cphh) : allOwners.OrderBy(o => o.Cphh))
            : (ODir == "desc" ? allOwners.OrderByDescending(o => o.Type) : allOwners.OrderBy(o => o.Type));
        OtherOwners = sorted.Skip((OPage - 1) * PageSize).Take(PageSize).ToList().AsReadOnly();
        OwnerTypes  = await lookups.GetLookupAsync(LookupTableId.OwnerType);
    }

    public string OtherOwnersSortUrl(string col)
    {
        var dir = string.Equals(OSort, col, StringComparison.OrdinalIgnoreCase) && ODir == "asc" ? "desc" : "asc";
        return $"?rbse={Uri.EscapeDataString(Rbse)}&OSort={col}&ODir={dir}&OPage=1";
    }

    public string OtherOwnersPageUrl(int page) =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&OPage={page}&OSort={OSort}&ODir={ODir}";

    public class NewOwnerViewModel
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? Cphh { get; set; }
    }
}
