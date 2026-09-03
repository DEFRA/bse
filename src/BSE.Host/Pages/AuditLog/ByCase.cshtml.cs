using System.Text;
using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AuditLog;

[Authorize(Policy = "Authenticated")]
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

    public IEnumerable<AuditLogEntry> Entries { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Rbse))
        {
            HasSearched = true;
            var all = (await auditLogService.GetByCaseAsync(Rbse.Trim().ToUpperInvariant())).ToList();
            TotalCount = all.Count;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            IEnumerable<AuditLogEntry> sorted = SortBy switch
            {
                "table"   => SortDir == "desc" ? all.OrderByDescending(e => e.TableName)   : all.OrderBy(e => e.TableName),
                "field"   => SortDir == "desc" ? all.OrderByDescending(e => e.FieldName)   : all.OrderBy(e => e.FieldName),
                "user"    => SortDir == "desc" ? all.OrderByDescending(e => e.UserName)    : all.OrderBy(e => e.UserName),
                "before"  => SortDir == "desc" ? all.OrderByDescending(e => e.BeforeValue) : all.OrderBy(e => e.BeforeValue),
                "after"   => SortDir == "desc" ? all.OrderByDescending(e => e.AfterValue)  : all.OrderBy(e => e.AfterValue),
                "reason"  => SortDir == "desc" ? all.OrderByDescending(e => e.Reason)      : all.OrderBy(e => e.Reason),
                "key"     => SortDir == "desc" ? all.OrderByDescending(e => e.Key)         : all.OrderBy(e => e.Key),
                _         => SortDir == "desc" ? all.OrderByDescending(e => e.DateTime)    : all.OrderBy(e => e.DateTime),
            };
            Entries = sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize);
        }
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage();

        var all = await auditLogService.GetByCaseAsync(Rbse.Trim().ToUpperInvariant());

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
        var fileName = $"AuditLog_{Rbse.Trim().ToUpperInvariant()}_{DateTime.Now:yyyyMMdd}.csv";
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
        return $"?rbse={Uri.EscapeDataString(Rbse)}&sortBy={col}&sortDir={dir}&pageNumber=1";
    }

    public string PageUrl(int page) =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&pageNumber={page}&sortBy={SortBy}&sortDir={SortDir}";
}
