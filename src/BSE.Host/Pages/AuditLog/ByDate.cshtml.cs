using BSE.Host.Helpers;
using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AuditLog;

[Authorize(Policy = "AuditAccess")]
public class ByDateModel(IAuditLogService auditLogService) : PageModel
{
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)]
    public DateTime? LogDate { get; set; }
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IEnumerable<AuditLogEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }
    public string? LogDateError { get; private set; }
    public int TotalCount => Entries.Count();
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<AuditLogEntry> PagedEntries => Entries.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(LogDate)))
        {
            if (LogDate is null)
            {
                LogDateError = AuditDateRange.MissingDateMessage;
                return Page();
            }

            HasSearched = true;
            Entries = ApplySorting(await auditLogService.GetByDateAsync(LogDate.Value));
        }
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (LogDate is null) return RedirectToPage();

        var entries = await auditLogService.GetByDateAsync(LogDate.Value);
        return AuditLogExcel.Build(entries, "Daily Audit Log", $"DailyAuditLog_{LogDate.Value:yyyyMMdd}.xlsx");
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
