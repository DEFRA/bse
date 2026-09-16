using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class OtherOwnerEditModel(
    IOtherOwnerRepository ownerRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    [BindProperty] public string? OwnerType { get; set; }
    [BindProperty] public string? OwnerName { get; set; }
    [BindProperty] public string? OwnerCphh { get; set; }
    [BindProperty] public string RowStampBase64 { get; set; } = string.Empty;

    public IEnumerable<LookupItem> OwnerTypes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;
        await LoadOwnerTypesAsync();

        var owner = (await ownerRepository.GetByRbseAsync(caseRbse)).FirstOrDefault(o => o.Id == Id);
        if (owner is null)
            return RedirectToPage("/Case/Vla", new { rbse = caseRbse });

        OwnerType = owner.Type;
        OwnerName = owner.Name;
        OwnerCphh = owner.Cphh;
        RowStampBase64 = Convert.ToBase64String(owner.RowStamp ?? []);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadOwnerTypesAsync();
        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;
        var normalizedCphh = string.IsNullOrWhiteSpace(OwnerCphh) ? null : CphhNormalizer.Normalize(OwnerCphh);

        if (string.IsNullOrWhiteSpace(OwnerType))
            ModelState.AddModelError(nameof(OwnerType), "Owner type is required.");

        if (string.IsNullOrWhiteSpace(OwnerName) && string.IsNullOrWhiteSpace(normalizedCphh))
            ModelState.AddModelError(nameof(OwnerName), "You must enter either an owner name or a CPHH.");

        if (!string.IsNullOrWhiteSpace(normalizedCphh) && normalizedCphh.Length != 11)
            ModelState.AddModelError(nameof(OwnerCphh), "CPHH must be 11 digits.");

        var allOwners = await ownerRepository.GetByRbseAsync(caseRbse);
        var typeDesc = OwnerTypes.FirstOrDefault(t => t.Code == OwnerType)?.Description ?? string.Empty;
        if (typeDesc.Contains("Previous", StringComparison.OrdinalIgnoreCase)
            && allOwners.Any(o => o.Type == OwnerType && o.Id != Id))
        {
            ModelState.AddModelError(nameof(OwnerType), "You can only have one owner of type Previous.");
        }

        if (!ModelState.IsValid)
            return Page();

        var rowStamp = string.IsNullOrEmpty(RowStampBase64) ? [] : Convert.FromBase64String(RowStampBase64);
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await ownerRepository.EditAsync(new EditOtherOwnerCommand(
            Id,
            caseRbse,
            OwnerType!,
            string.IsNullOrWhiteSpace(OwnerName) ? null : OwnerName,
            string.IsNullOrWhiteSpace(normalizedCphh) ? null : normalizedCphh,
            rowStamp), conn, tx);
        tx.Commit();

        TempData["Success"] = "Owner record updated.";
        return RedirectToPage("/Case/Vla", new { rbse = caseRbse });
    }

    private async Task LoadOwnerTypesAsync()
    {
        OwnerTypes = await lookups.GetLookupAsync(BSE.SharedKernel.LookupTableId.OwnerType);
    }
}
