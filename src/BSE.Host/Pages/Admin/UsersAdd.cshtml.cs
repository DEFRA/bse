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
    // Column limits from [dbo].[User]: Name VARCHAR(35), Email VARCHAR(60).
    private const int UserNameMaxLength = 35;
    private const int EmailMaxLength = 60;

    // NT Login is a legacy Windows-auth identifier; the business wants it hidden from the UI
    // wherever possible now that Entra ID/email is the primary identity. Derived automatically
    // from Email below rather than collected from the user.
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

        var email = Email?.Trim();
        var userName = UserName?.Trim() ?? string.Empty;
        UserName = userName;

        if (string.IsNullOrWhiteSpace(email))
            ModelState.AddModelError(nameof(Email), "Enter an email address");
        else if (email.Length > EmailMaxLength)
            ModelState.AddModelError(nameof(Email), $"Email must be {EmailMaxLength} characters or fewer");
        else if (!ValidationHelpers.IsValidEmail(email))
            ModelState.AddModelError(nameof(Email), "Enter an email address in the correct format, like name@example.com");

        if (string.IsNullOrWhiteSpace(userName))
            ModelState.AddModelError(nameof(UserName), "Enter a display name");
        else if (userName.Length > UserNameMaxLength)
            ModelState.AddModelError(nameof(UserName), $"Display name must be {UserNameMaxLength} characters or fewer");

        if (UserGroupId <= 0)
            ModelState.AddModelError(nameof(UserGroupId), "Select a user group");

        var users = await userManagementService.GetAllUsersAsync();
        UserGroups = await lookupDataService.GetUserGroupsAsync();

        if (ModelState.IsValid)
        {
            if (users.Any(u => !string.IsNullOrWhiteSpace(u.Email) && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(Email), "Unable to add the selected user");
        }

        if (!ModelState.IsValid)
            return Page();

        var user = new User(
            UserId: 0,
            NTLogin: DeriveNtLogin(email!, users),
            Upn: Upn,
            UserName: UserName,
            Email: email,
            IsActive: IsActive,
            UserGroupId: UserGroupId,
            UserGroup: (UserGroup)UserGroupId);

        await userManagementService.AddUserAsync(user);
        TempData["Success"] = $"User '{UserName}' added.";
        return RedirectToPage("/Admin/Users");
    }

    // NTLogin is VARCHAR(25) NOT NULL with a unique constraint; derive a value from the email
    // local part so the column stays populated without asking the user for it.
    private static string DeriveNtLogin(string email, IEnumerable<User> existingUsers)
    {
        var localPart = email.Split('@')[0];
        var sanitised = new string(localPart.Where(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-').ToArray());
        if (string.IsNullOrEmpty(sanitised))
            sanitised = "user";

        var baseValue = sanitised.Length > 21 ? sanitised[..21] : sanitised;
        var existingLogins = new HashSet<string>(
            existingUsers.Select(u => u.NTLogin), StringComparer.OrdinalIgnoreCase);

        var candidate = baseValue;
        var suffix = 1;
        while (existingLogins.Contains(candidate))
        {
            candidate = $"{baseValue}{suffix++}";
            if (candidate.Length > 25) candidate = candidate[..25];
        }

        return candidate;
    }
}
