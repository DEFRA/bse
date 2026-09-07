using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "VLAMaintenance")]
public class UsersAddModel(
    IUserManagementService userManagementService,
    ILookupDataService lookupDataService) : PageModel
{
    [BindProperty] public string? NTLogin { get; set; } = string.Empty;
    [BindProperty] public string? Upn { get; set; }
    [BindProperty] public string UserName { get; set; } = string.Empty;
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;
    [BindProperty] public int UserGroupId { get; set; } = 0;

    public IEnumerable<LuUserGroup> UserGroups { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        UserGroups = await lookupDataService.GetUserGroupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        IsActive = Request.Form[nameof(IsActive)]
            .Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(UserName))
            ModelState.AddModelError(nameof(UserName), "Enter a display name");
        if (UserGroupId <= 0)
            ModelState.AddModelError(nameof(UserGroupId), "Select a user group");

        var users = await userManagementService.GetAllUsersAsync();
        UserGroups = await lookupDataService.GetUserGroupsAsync();

        if (ModelState.IsValid)
        {
            if (!string.IsNullOrWhiteSpace(Email) &&
                users.Any(u => !string.IsNullOrWhiteSpace(u.Email) && u.Email.Equals(Email, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(Email), "Unable to add the selected user");
        }

        if (!ModelState.IsValid)
            return Page();

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
        return RedirectToPage("/Admin/Users");
    }
}
