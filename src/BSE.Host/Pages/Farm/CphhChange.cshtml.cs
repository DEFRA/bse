using BSE.Host.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize(Policy = "DataEntry")]
public class CphhChangeModel : PageModel
{
    private readonly IFarmService _farm;
    private readonly ICurrentUserService _currentUser;

    public CphhChangeModel(IFarmService farm, ICurrentUserService currentUser)
    {
        _farm = farm;
        _currentUser = currentUser;
    }

    public string OldCphh { get; private set; } = "";
    [BindProperty] public string NewCphh { get; set; } = "";
    public string? ErrorMessage { get; private set; }

    public void OnGet(string cphh) => OldCphh = cphh;

    public async Task<IActionResult> OnPostAsync(string cphh)
    {
        OldCphh = cphh;
        if (!ModelState.IsValid) return Page();

        var userId = await _currentUser.GetUserIdAsync();
        var result = await _farm.ChangeCphhAsync(cphh, NewCphh.Trim(), userId);

        if (result != ChangeCphhResult.Success)
        {
            ErrorMessage = $"CPHH change failed: {result}";
            return Page();
        }

        TempData["SuccessMessage"] = $"CPHH changed from {cphh} to {NewCphh} successfully.";
        return RedirectToPage("/Farm/Details", new { cphh = NewCphh.Trim() });
    }
}
