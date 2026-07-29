using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize(Policy = "DataEntry")]
public class NewModel(IFarmService farmService, ICurrentUserService currentUserService) : PageModel
{
    [BindProperty]
    public FarmEditViewModel Farm { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var userId = await currentUserService.GetUserIdAsync();
        var command = Farm.ToAddCommand();
        await farmService.AddAsync(command, userId);

        TempData["Success"] = $"Farm {Farm.CPHH} has been created.";
        return RedirectToPage("/Farm/Details", new { cphh = Farm.CPHH });
    }
}
