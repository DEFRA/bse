using System.Text;
using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AuditLog;

[Authorize(Policy = "AuditAccess")]
public class ByCaseModel(IAuditLogService auditLogService) : PageModel
{
    private const int PageSize = 20;

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "datetime";

    [BindProperty(SupportsGet = true)]
    public string SortDir { get; set; } = "desc";

    [BindProperty(SupportsGet = true)]
    public string? ReturnTo { get; set; }

    public IEnumerable<AuditLogEntry> Entries { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Legacy read the case RBSE from session, not a search box — this page is only
        // reachable via the "Case Audit log" link on the Case (DEFRA) tab.
        if (string.IsNullOrWhiteSpace(Rbse))
        {
            return RedirectToPage("/Home");
        }

        HasSearched = true;
        var all = (await auditLogService.GetByCaseAsync(Rbse.Trim().ToUpperInvariant())).ToList();
        TotalCount = all.Count;
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

        IEnumerable<AuditLogEntry> sorted = ApplySorting(all);
        Entries = sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize);
        return Page();
    }

    private IEnumerable<AuditLogEntry> ApplySorting(IEnumerable<AuditLogEntry> entries)
    {
        Func<AuditLogEntry, object?> sortKey = SortBy switch
        {
            "table"  => e => e.TableName,
            "field"  => e => e.FieldName,
            "user"   => e => e.UserName,
            "before" => e => e.BeforeValue,
            "after"  => e => e.AfterValue,
            "reason" => e => e.Reason,
            "key"    => e => e.Key,
            _        => e => e.DateTime,
        };

        return SortDir == "desc" ? entries.OrderByDescending(sortKey) : entries.OrderBy(sortKey);
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage();

        var all = ApplySorting(await auditLogService.GetByCaseAsync(Rbse.Trim().ToUpperInvariant()));

        var sb = new StringBuilder();
        sb.AppendLine("Table,Field,Date/Time,User,Before,After,Reason,Key");
        foreach (var e in all)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(e.TableName),
                CsvEscape(e.FieldName),
                CsvEscape(e.DateTime.ToString("dd/MM/yyyy HH:mm")),
                CsvEscape(e.UserName),
                CsvEscape(e.BeforeValue),
                CsvEscape(e.AfterValue),
                CsvEscape(e.Reason),
                CsvEscape(e.Key)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"AuditLog_{Rbse.Trim().ToUpperInvariant()}_{BSE.SharedKernel.UkClock.Today:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    public string SortUrl(string col)
    {
        var dir = string.Equals(SortBy, col, StringComparison.OrdinalIgnoreCase) && SortDir == "asc" ? "desc" : "asc";
        var q = $"?rbse={Uri.EscapeDataString(Rbse)}&sortBy={col}&sortDir={dir}&pageNumber={PageNumber}";
        if (!string.IsNullOrWhiteSpace(ReturnTo))
            q += $"&returnTo={Uri.EscapeDataString(ReturnTo)}";
        return q;
    }
}
