using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AuditLog;

public class RbseChangesModel(IAuditLogService auditLogService) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
    [BindProperty(SupportsGet = true)] public DateTime EndDate { get; set; } = DateTime.Today;

    public IEnumerable<AuditLogRBSEChangeEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(StartDate)))
        {
            HasSearched = true;
            Entries = (await auditLogService.GetRbseChangesAsync(StartDate, EndDate)).Cast<AuditLogRBSEChangeEntry>();
        }
        return Page();
    }
}
