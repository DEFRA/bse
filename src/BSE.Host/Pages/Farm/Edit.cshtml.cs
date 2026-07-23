using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize(Policy = "DataEntry")]
public class EditModel : PageModel
{
    private readonly IFarmService _farm;
    private readonly ICurrentUserService _currentUser;

    public EditModel(IFarmService farm, ICurrentUserService currentUser)
    {
        _farm = farm;
        _currentUser = currentUser;
    }

    [BindProperty] public FarmEditViewModel? Farm { get; set; }
    private byte[]? _rowStamp;

    public async Task<IActionResult> OnGetAsync(string cphh)
    {
        var record = await _farm.GetByCphhAsync(cphh);
        if (record is null) return NotFound();

        _rowStamp = record.RowStamp;
        Farm = FarmEditViewModel.FromRecord(record);
        TempData["FarmRowStamp"] = record.RowStamp != null ? Convert.ToBase64String(record.RowStamp) : null;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || Farm is null) return Page();

        var rowStampBase64 = TempData["FarmRowStamp"] as string;
        var rowStamp = rowStampBase64 != null ? Convert.FromBase64String(rowStampBase64) : null;

        var userId = await _currentUser.GetUserIdAsync();
        await _farm.UpdateAsync(Farm.ToUpdateCommand(rowStamp), userId);

        TempData["SuccessMessage"] = "Farm updated successfully.";
        return RedirectToPage("/Farm/Details", new { cphh = Farm.CPHH });
    }
}
