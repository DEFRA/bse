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
public class UsersEditModel(IUserManagementService userManagementService, ILookupDataService lookupDataService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty] public string EditNTLogin { get; set; } = string.Empty;
    [BindProperty] public string EditUserName { get; set; } = string.Empty;
    [BindProperty] public string? EditEmail { get; set; }
    [BindProperty] public bool EditIsActive { get; set; }
    [BindProperty] public int EditUserGroupId { get; set; }

    public IEnumerable<LuUserGroup> UserGroups { get; private set; } = [];
    public User? ExistingUser { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var users = (await userManagementService.GetAllUsersAsync()).ToList();
        ExistingUser = users.FirstOrDefault(u => u.UserId == Id);
        if (ExistingUser is null)
            return RedirectToPage("/Admin/Users");

        UserGroups = await lookupDataService.GetUserGroupsAsync();
        EditNTLogin = ExistingUser.NTLogin;
        EditUserName = ExistingUser.UserName;
        EditEmail = ExistingUser.Email;
        EditIsActive = ExistingUser.IsActive;
        EditUserGroupId = ExistingUser.UserGroupId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        EditIsActive = Request.Form[nameof(EditIsActive)]
            .Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(EditUserName))
            ModelState.AddModelError(nameof(EditUserName), "Enter a display name");
        if (EditUserGroupId <= 0)
            ModelState.AddModelError(nameof(EditUserGroupId), "Select a user group");

        var users = (await userManagementService.GetAllUsersAsync()).ToList();
        ExistingUser = users.FirstOrDefault(u => u.UserId == Id);
        UserGroups = await lookupDataService.GetUserGroupsAsync();

        if (ExistingUser is null)
            return RedirectToPage("/Admin/Users");

        if (ModelState.IsValid)
        {
            if (users.Any(u => u.UserId != Id && u.NTLogin.Equals(EditNTLogin, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(EditNTLogin), "Unable to add the selected user");
            if (!string.IsNullOrWhiteSpace(EditEmail) &&
                users.Any(u => u.UserId != Id && !string.IsNullOrWhiteSpace(u.Email) && u.Email.Equals(EditEmail, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(EditEmail), "Unable to add the selected user");
        }

        if (!ModelState.IsValid)
            return Page();

        var user = new User(
            UserId: Id,
            NTLogin: EditNTLogin,
            Upn: ExistingUser.Upn,
            UserName: EditUserName,
            Email: EditEmail,
            IsActive: EditIsActive,
            UserGroupId: EditUserGroupId,
            UserGroup: (UserGroup)EditUserGroupId);

        await userManagementService.UpdateUserAsync(user);
        TempData["Success"] = $"User '{EditUserName}' updated.";
        return RedirectToPage("/Admin/Users");
    }
}