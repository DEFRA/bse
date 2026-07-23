using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AuditLog;

public class ByDateModel(IAuditLogService auditLogService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime LogDate { get; set; } = DateTime.Today;

    public IEnumerable<AuditLogEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(LogDate)))
        {
            HasSearched = true;
            Entries = await auditLogService.GetByDateAsync(LogDate);
        }
        return Page();
    }
}
