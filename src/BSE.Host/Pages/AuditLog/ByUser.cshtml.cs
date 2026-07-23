using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AuditLog;

public class ByUserModel(IAuditLogService auditLogService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Today;
    [BindProperty(SupportsGet = true)]
    public int UserId { get; set; }

    public IEnumerable<AuditLogEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(UserId)))
        {
            HasSearched = true;
            Entries = await auditLogService.GetByUserAsync(StartDate, EndDate, UserId);
        }
        return Page();
    }
}
