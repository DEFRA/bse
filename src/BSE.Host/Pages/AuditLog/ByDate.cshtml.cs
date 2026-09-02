using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AuditLog;

[Authorize(Policy = "AuditAccess")]
public class ByDateModel(IAuditLogService auditLogService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime LogDate { get; set; } = DateTime.Today;
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }

    public IEnumerable<AuditLogEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(LogDate)))
        {
            HasSearched = true;
            Entries = ApplySorting(await auditLogService.GetByDateAsync(LogDate));
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
