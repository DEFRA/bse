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
    [BindProperty] public int UserGroupId { get; set; } = 1;

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
        if (!ModelState.IsValid)
        {
            Users = await userManagementService.GetAllUsersAsync();
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
        if (!ModelState.IsValid)
        {
            Users = await userManagementService.GetAllUsersAsync();
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
