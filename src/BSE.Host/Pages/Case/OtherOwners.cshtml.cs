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

    [BindProperty]
    public NewOwnerViewModel NewOwner { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddOwnerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewOwner.Type))
        {
            ModelState.AddModelError(string.Empty, "Owner type is required.");
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
        OtherOwners = await ownerRepository.GetByRbseAsync(Rbse);
        OwnerTypes = await lookups.GetLookupAsync(LookupTableId.OwnerType);
    }

    public class NewOwnerViewModel
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? Cphh { get; set; }
    }
}
