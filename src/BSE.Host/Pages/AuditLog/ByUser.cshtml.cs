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
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Today;
    [BindProperty(SupportsGet = true)]
    public int UserId { get; set; }
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IEnumerable<SelectListItem> Users { get; private set; } = [];
    public IEnumerable<AuditLogEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }
    public string? ValidationError { get; private set; }
    public int TotalCount => Entries.Count();
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<AuditLogEntry> PagedEntries => Entries.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

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
            Entries = ApplySorting(await auditLogService.GetByUserAsync(StartDate, EndDate, UserId));
        }
        return Page();
    }

    private IEnumerable<AuditLogEntry> ApplySorting(IEnumerable<AuditLogEntry> entries)
    {
        Func<AuditLogEntry, object?> keySelector = SortColumn switch
        {
            "User" => e => e.UserName,
            "Table" => e => e.TableName,
            "Field" => e => e.FieldName,
            "Key" => e => e.Key,
            "Before" => e => e.BeforeValue,
            "After" => e => e.AfterValue,
            "Reason" => e => e.Reason,
            _ => e => e.DateTime,
        };

        return SortDesc ? entries.OrderByDescending(keySelector) : entries.OrderBy(keySelector);
    }
}
