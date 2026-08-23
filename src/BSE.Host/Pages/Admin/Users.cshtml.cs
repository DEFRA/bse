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
public class UsersModel(IUserManagementService userManagementService, ILookupDataService lookupDataService) : PageModel
{
    public IEnumerable<User> Users { get; private set; } = [];
    public IEnumerable<LuUserGroup> UserGroups { get; private set; } = [];

    // Add form fields
    [BindProperty] public string NTLogin { get; set; } = string.Empty;
    [BindProperty] public string? Upn { get; set; }
    [BindProperty] public string UserName { get; set; } = string.Empty;
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;
    [BindProperty] public int UserGroupId { get; set; } = 0;

    // Edit form fields
    [BindProperty] public int EditUserId { get; set; }
    [BindProperty] public string EditNTLogin { get; set; } = string.Empty;
    [BindProperty] public string? EditUpn { get; set; }
    [BindProperty] public string EditUserName { get; set; } = string.Empty;
    [BindProperty] public string? EditEmail { get; set; }
    [BindProperty] public bool EditIsActive { get; set; }
    [BindProperty] public int EditUserGroupId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Users = await userManagementService.GetAllUsersAsync();
        UserGroups = await lookupDataService.GetUserGroupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (string.IsNullOrWhiteSpace(NTLogin))
            ModelState.AddModelError(nameof(NTLogin), "Enter NT login");
        if (string.IsNullOrWhiteSpace(UserName))
            ModelState.AddModelError(nameof(UserName), "Enter a display name");
        if (UserGroupId <= 0)
            ModelState.AddModelError(nameof(UserGroupId), "Select a user group");

        Users = await userManagementService.GetAllUsersAsync();

        if (ModelState.IsValid)
        {
            if (Users.Any(u => u.NTLogin.Equals(NTLogin, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(NTLogin), "Unable to add the selected user");
            if (!string.IsNullOrWhiteSpace(Email) &&
                Users.Any(u => !string.IsNullOrWhiteSpace(u.Email) && u.Email.Equals(Email, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(Email), "Unable to add the selected user");
        }

        if (!ModelState.IsValid)
        {
            UserGroups = await lookupDataService.GetUserGroupsAsync();
            return Page();
        }

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

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (string.IsNullOrWhiteSpace(EditNTLogin))
            ModelState.AddModelError(nameof(EditNTLogin), "Enter NT login");
        if (string.IsNullOrWhiteSpace(EditUserName))
            ModelState.AddModelError(nameof(EditUserName), "Enter a display name");
        if (EditUserGroupId <= 0)
            ModelState.AddModelError(nameof(EditUserGroupId), "Select a user group");

        Users = await userManagementService.GetAllUsersAsync();

        if (ModelState.IsValid)
        {
            if (Users.Any(u => u.UserId != EditUserId && u.NTLogin.Equals(EditNTLogin, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(EditNTLogin), "Unable to add the selected user");
            if (!string.IsNullOrWhiteSpace(EditEmail) &&
                Users.Any(u => u.UserId != EditUserId && !string.IsNullOrWhiteSpace(u.Email) && u.Email.Equals(EditEmail, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(EditEmail), "Unable to add the selected user");
        }

        if (!ModelState.IsValid)
        {
            UserGroups = await lookupDataService.GetUserGroupsAsync();
            return Page();
        }

        var user = new User(
            UserId: EditUserId,
            NTLogin: EditNTLogin,
            Upn: EditUpn,
            UserName: EditUserName,
            Email: EditEmail,
            IsActive: EditIsActive,
            UserGroupId: EditUserGroupId,
            UserGroup: (UserGroup)EditUserGroupId);

        await userManagementService.UpdateUserAsync(user);
        TempData["Success"] = $"User '{EditUserName}' updated.";
        return RedirectToPage();
    }
}
