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
public class OtherOwnerAddModel(
    IOtherOwnerRepository ownerRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;

    [BindProperty] public string? OwnerType { get; set; }
    [BindProperty] public string? OwnerName { get; set; }
    [BindProperty] public string? OwnerCphh { get; set; }

    public IEnumerable<LookupItem> OwnerTypes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadOwnerTypesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadOwnerTypesAsync();
        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var normalizedCphh = string.IsNullOrWhiteSpace(OwnerCphh) ? null : CphhNormalizer.Normalize(OwnerCphh);

        if (string.IsNullOrWhiteSpace(OwnerType))
            ModelState.AddModelError(nameof(OwnerType), "Owner type is required.");

        if (string.IsNullOrWhiteSpace(OwnerName) && string.IsNullOrWhiteSpace(normalizedCphh))
            ModelState.AddModelError(nameof(OwnerName), "You must enter either an owner name or a CPHH.");

        if (!string.IsNullOrWhiteSpace(normalizedCphh) && normalizedCphh.Length != 11)
            ModelState.AddModelError(nameof(OwnerCphh), "CPHH must be 11 digits.");

        if (!string.IsNullOrWhiteSpace(OwnerType))
        {
            var allOwners = await ownerRepository.GetByRbseAsync(caseRbse);
            var typeDesc = OwnerTypes.FirstOrDefault(t => t.Code == OwnerType)?.Description ?? string.Empty;
            if (typeDesc.Contains("Previous", StringComparison.OrdinalIgnoreCase)
                && allOwners.Any(o => o.Type == OwnerType))
            {
                ModelState.AddModelError(nameof(OwnerType), "You can only have one owner of type Previous.");
            }
        }

        if (!ModelState.IsValid)
            return Page();

        var command = new AddOtherOwnerCommand(
            caseRbse,
            OwnerType!,
            string.IsNullOrWhiteSpace(OwnerName) ? null : OwnerName,
            string.IsNullOrWhiteSpace(normalizedCphh) ? null : normalizedCphh);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await ownerRepository.AddAsync(command, conn, tx);
        tx.Commit();

        TempData["Success"] = "Owner record added.";
        return RedirectToPage("/Case/Vla", new { rbse = Rbse });
    }

    private async Task LoadOwnerTypesAsync()
    {
        OwnerTypes = await lookups.GetLookupAsync(LookupTableId.OwnerType);
    }
}
