using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "Admin")]
public class UsersModel(IUserManagementService userManagementService) : PageModel
{
    public IEnumerable<User> Users { get; private set; } = [];

    // Add form fields
    [BindProperty] public string NTLogin { get; set; } = string.Empty;
    [BindProperty] public string? Upn { get; set; }
    [BindProperty] public string UserName { get; set; } = string.Empty;
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;
    [BindProperty] public int UserGroupId { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        Users = await userManagementService.GetAllUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid) return await OnGetAsync().ContinueWith(_ => Page());

        var user = new User(
            UserId: 0,
            NTLogin: NTLogin,
            Upn: Upn,
            UserName: UserName,
            Email: Email,
            IsActive: IsActive,
            UserGroupId: UserGroupId,
            UserGroup: (UserGroup)UserGroupId);

        await userManagementService.AddUserAsync(user);
        TempData["Success"] = $"User '{UserName}' added.";
        return RedirectToPage();
    }
}
