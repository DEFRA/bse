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
            Entries = all.Skip((PageNumber - 1) * PageSize).Take(PageSize);
        }
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage();

        var all = await auditLogService.GetByCaseAsync(Rbse.Trim().ToUpperInvariant());

        var sb = new StringBuilder();
        sb.AppendLine("Date/Time,User,Table,Field,Key,Before,After,Reason");
        foreach (var e in all)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(e.DateTime.ToString("dd/MM/yyyy HH:mm")),
                CsvEscape(e.UserName),
                CsvEscape(e.TableName),
                CsvEscape(e.FieldName),
                CsvEscape(e.Key),
                CsvEscape(e.BeforeValue),
                CsvEscape(e.AfterValue),
                CsvEscape(e.Reason)));
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
}
