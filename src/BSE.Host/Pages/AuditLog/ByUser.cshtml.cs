using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using BSE.Modules.UserManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BSE.Host.Pages.AuditLog;

[Authorize(Policy = "AuditAccess")]
public class ByUserModel(IAuditLogService auditLogService, IUserManagementService userManagementService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Today;
    [BindProperty(SupportsGet = true)]
    public int UserId { get; set; }

    public IEnumerable<SelectListItem> Users { get; private set; } = [];
    public IEnumerable<AuditLogEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }
    public string? ValidationError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var allUsers = await userManagementService.GetAllUsersAsync();
        Users = allUsers
            .Where(u => u.IsActive)
            .OrderBy(u => u.UserName)
            .Select(u => new SelectListItem(u.UserName, u.UserId.ToString()));

        if (Request.Query.ContainsKey(nameof(UserId)))
        {
            if (UserId == 0)
            {
                ValidationError = "Select a user.";
                return Page();
            }
            HasSearched = true;
            Entries = await auditLogService.GetByUserAsync(StartDate, EndDate, UserId);
        }
        return Page();
    }
}
