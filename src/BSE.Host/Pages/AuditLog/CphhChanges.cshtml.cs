using BSE.Host.Helpers;
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

    [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IEnumerable<AuditLogCPHHChangeEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }
    public int TotalCount => Entries.Count();
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<AuditLogCPHHChangeEntry> PagedEntries => Entries.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public string? StartDateError { get; private set; }
    public string? EndDateError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(StartDate)) || Request.Query.ContainsKey(nameof(EndDate)))
        {
            if (!AuditDateRange.Validate(StartDate, EndDate, out var startError, out var endError))
            {
                StartDateError = startError;
                EndDateError = endError;
                return Page();
            }

            HasSearched = true;
            Entries = ApplySorting((await auditLogService.GetCphhChangesAsync(StartDate!.Value, EndDate!.Value)).Cast<AuditLogCPHHChangeEntry>());
        }
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!AuditDateRange.Validate(StartDate, EndDate, out _, out _)) return RedirectToPage();

        var entries = (await auditLogService.GetCphhChangesAsync(StartDate!.Value, EndDate!.Value)).Cast<AuditLogCPHHChangeEntry>();
        return AuditLogExcel.Build(entries, "CPHH Changes", $"CphhChanges_{DateTime.Today:yyyyMMdd}.xlsx",
            [("CaseCount", e => e.CaseCount)]);
    }

    private IEnumerable<AuditLogCPHHChangeEntry> ApplySorting(IEnumerable<AuditLogCPHHChangeEntry> entries)
    {
        Func<AuditLogCPHHChangeEntry, object?> keySelector = SortColumn switch
        {
            "Table" => e => e.TableName,
            "Field" => e => e.FieldName,
            "User" => e => e.UserName,
            "Before" => e => e.BeforeValue,
            "After" => e => e.AfterValue,
            "Reason" => e => e.Reason,
            "Key" => e => e.Key,
            "Cases" => e => e.CaseCount,
            _ => e => e.DateTime,
        };

        return SortDesc ? entries.OrderByDescending(keySelector) : entries.OrderBy(keySelector);
    }
}
