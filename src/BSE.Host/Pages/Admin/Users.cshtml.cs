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
    private const int PageSize = 10;

    // Column limits from [dbo].[User]: Name VARCHAR(35), Email VARCHAR(60).
    private const int UserNameMaxLength = 35;
    private const int EmailMaxLength = 60;

    public IEnumerable<User> Users { get; private set; } = [];
    public IEnumerable<LuUserGroup> UserGroups { get; private set; } = [];

    public int TotalCount => Users.Count();
    public int TotalPages => PageCountFor(TotalCount);
    public IReadOnlyList<User> PagedUsers => Users.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    private static int PageCountFor(int totalCount) =>
        totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);

    // Row highlighted via the row-select arrow. Distinct from EditUserId — selecting a row
    // only enables the New/Edit buttons below the grid, it doesn't start editing by itself.
    [BindProperty(SupportsGet = true)] public int SelectedUserId { get; set; }

    // True while the blank "add a new user" row is rendered (EditUserId stays 0 in this state).
    [BindProperty(SupportsGet = true)] public bool IsAddingNew { get; set; }

    // Edit form fields — EditUserId also supports GET so the inline row-select link can
    // activate edit mode for a given row without a full postback.
    [BindProperty(SupportsGet = true)] public int EditUserId { get; set; }
    [BindProperty] public string EditNTLogin { get; set; } = string.Empty;
    [BindProperty] public string? EditUpn { get; set; }
    [BindProperty] public string EditUserName { get; set; } = string.Empty;
    [BindProperty] public string? EditEmail { get; set; }
    [BindProperty] public bool EditIsActive { get; set; }
    [BindProperty] public int EditUserGroupId { get; set; }

    public bool IsEditing(int userId) => EditUserId != 0 && EditUserId == userId;
    public bool IsEditingOrAdding => EditUserId != 0 || IsAddingNew;

    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        UserGroups = await lookupDataService.GetUserGroupsAsync();
        Users = ApplySorting(await userManagementService.GetAllUsersAsync());

        if (EditUserId != 0)
        {
            var editUser = Users.FirstOrDefault(u => u.UserId == EditUserId);
            if (editUser is not null)
            {
                EditNTLogin    = editUser.NTLogin;
                EditUpn        = editUser.Upn;
                EditUserName   = editUser.UserName;
                EditEmail      = editUser.Email;
                EditIsActive   = editUser.IsActive;
                EditUserGroupId = editUser.UserGroupId;
            }
            else
            {
                EditUserId = 0;
            }
        }
        else if (IsAddingNew)
        {
            EditUserName = string.Empty;
            EditEmail = string.Empty;
            EditIsActive = true;
            EditUserGroupId = 0;
        }

        return Page();
    }

    // "New" button below the grid — available regardless of whether a row is selected. The blank
    // row is always added at the end of the last page, where the saved record will land.
    public async Task<IActionResult> OnPostStartNewAsync()
    {
        var lastPage = PageCountFor((await userManagementService.GetAllUsersAsync()).Count());
        return RedirectToPage(new { IsAddingNew = true, SortColumn, SortDesc, PageNumber = lastPage });
    }

    // "Edit" button below the grid — turns the selected row into the existing inline edit row.
    public IActionResult OnPostStartEdit()
    {
        if (SelectedUserId == 0)
            return RedirectToPage(new { SortColumn, SortDesc, PageNumber });

        return RedirectToPage(new { EditUserId = SelectedUserId, SortColumn, SortDesc, PageNumber });
    }

    public IActionResult OnPostCancel()
    {
        return RedirectToPage(new { SortColumn, SortDesc, PageNumber });
    }

    private IEnumerable<User> ApplySorting(IEnumerable<User> users)
    {
        Func<User, object?> keySelector = SortColumn switch
        {
            "NTLogin" => u => u.NTLogin,
            "UserName" => u => u.UserName,
            "Email" => u => u.Email,
            "Group" => u => UserGroups.FirstOrDefault(g => g.Id == u.UserGroupId)?.Name,
            "IsActive" => u => u.IsActive,
            _ => u => u.UserId,
        };

        return SortDesc
            ? users.OrderByDescending(keySelector)
            : users.OrderBy(keySelector);
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        // Resolve checkbox value from posted form values (handles true/false dual inputs reliably).
        EditIsActive = Request.Form[nameof(EditIsActive)]
            .Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));

        var isAdding = EditUserId == 0;

        EditEmail = EditEmail?.Trim();
        EditUserName = EditUserName?.Trim() ?? string.Empty;

        ValidateEditFields(isAdding);

        Users = await userManagementService.GetAllUsersAsync();
        UserGroups = await lookupDataService.GetUserGroupsAsync();

        if (ModelState.IsValid)
            ValidateUniqueness(isAdding);

        if (!ModelState.IsValid)
        {
            IsAddingNew = isAdding;
            Users = ApplySorting(Users);
            return Page();
        }

        if (isAdding)
        {
            var newUser = new User(
                UserId: 0,
                NTLogin: DeriveNtLogin(EditEmail!, Users),
                Upn: EditUpn,
                UserName: EditUserName,
                Email: EditEmail,
                IsActive: EditIsActive,
                UserGroupId: EditUserGroupId,
                UserGroup: (UserGroup)EditUserGroupId);

            await userManagementService.AddUserAsync(newUser);
            TempData["Success"] = $"User '{EditUserName}' added.";

            // The new record sorts to the end, so show the page it landed on.
            return RedirectToPage(new { SortColumn, SortDesc, PageNumber = PageCountFor(Users.Count() + 1) });
        }
        else
        {
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
        }

        return RedirectToPage(new { SortColumn, SortDesc, PageNumber });
    }

    private void ValidateEditFields(bool isAdding)
    {
        if (string.IsNullOrWhiteSpace(EditUserName))
            ModelState.AddModelError(nameof(EditUserName), "Enter a display name");
        else if (EditUserName.Length > UserNameMaxLength)
            ModelState.AddModelError(nameof(EditUserName), $"Display name must be {UserNameMaxLength} characters or fewer");

        if (isAdding)
        {
            if (string.IsNullOrWhiteSpace(EditEmail))
                ModelState.AddModelError(nameof(EditEmail), "Enter an email address");
            else if (EditEmail.Length > EmailMaxLength)
                ModelState.AddModelError(nameof(EditEmail), $"Email must be {EmailMaxLength} characters or fewer");
            else if (!ValidationHelpers.IsValidEmail(EditEmail))
                ModelState.AddModelError(nameof(EditEmail), "Enter an email address in the correct format, like name@example.com");
        }
        else if (string.IsNullOrWhiteSpace(EditNTLogin))
        {
            ModelState.AddModelError(nameof(EditNTLogin), "Enter NT login");
        }

        if (EditUserGroupId <= 0)
            ModelState.AddModelError(nameof(EditUserGroupId), "Select a user group");
    }

    private void ValidateUniqueness(bool isAdding)
    {
        if (!isAdding && Users.Any(u => u.UserId != EditUserId && u.NTLogin.Equals(EditNTLogin, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(EditNTLogin), "Unable to add the selected user");
        if (!string.IsNullOrWhiteSpace(EditEmail) &&
            Users.Any(u => u.UserId != EditUserId && !string.IsNullOrWhiteSpace(u.Email) && u.Email.Equals(EditEmail, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(EditEmail), "Unable to add the selected user");
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
