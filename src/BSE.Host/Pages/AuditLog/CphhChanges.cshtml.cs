using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AuditLog;

[Authorize(Policy = "AuditAccess")]
public class CphhChangesModel(IAuditLogService auditLogService) : PageModel
{
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)] public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
    [BindProperty(SupportsGet = true)] public DateTime EndDate { get; set; } = DateTime.Today;
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IEnumerable<AuditLogCPHHChangeEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }
    public int TotalCount => Entries.Count();
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<AuditLogCPHHChangeEntry> PagedEntries => Entries.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(StartDate)))
        {
            HasSearched = true;
            Entries = ApplySorting((await auditLogService.GetCphhChangesAsync(StartDate, EndDate)).Cast<AuditLogCPHHChangeEntry>());
        }
        return Page();
    }

    private IEnumerable<AuditLogCPHHChangeEntry> ApplySorting(IEnumerable<AuditLogCPHHChangeEntry> entries)
    {
        Func<AuditLogCPHHChangeEntry, object?> keySelector = SortColumn switch
        {
            "User" => e => e.UserName,
            "Key" => e => e.Key,
            "Before" => e => e.BeforeValue,
            "After" => e => e.AfterValue,
            "Cases" => e => e.CaseCount,
            _ => e => e.DateTime,
        };

        return SortDesc ? entries.OrderByDescending(keySelector) : entries.OrderBy(keySelector);
    }
}
