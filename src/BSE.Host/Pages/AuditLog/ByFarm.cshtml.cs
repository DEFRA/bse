using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AuditLog;

[Authorize(Policy = "Authenticated")]
public class ByFarmModel(IAuditLogService auditLogService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Cphh { get; set; } = string.Empty;

    /// <summary>
    /// Optional RBSE number — supplied when navigating from /Case/{rbse} (Farm tab).
    /// Used to render a contextual breadcrumb and back link.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Rbse { get; set; }
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }

    public IEnumerable<AuditLogEntry> Entries { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Cphh))
        {
            HasSearched = true;
            Entries = ApplySorting(await auditLogService.GetByFarmAsync(NormaliseCphh(Cphh)));
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

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (string.IsNullOrWhiteSpace(Cphh)) return RedirectToPage();

        var entries = await auditLogService.GetByFarmAsync(NormaliseCphh(Cphh));

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Farm Audit Log");

        string[] headers = ["Table", "Field", "Date/Time", "User", "Before", "After", "Reason", "Key"];
        for (var col = 1; col <= headers.Length; col++)
        {
            ws.Cell(1, col).Value = headers[col - 1];
            ws.Cell(1, col).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var e in entries)
        {
            ws.Cell(row, 1).Value = e.TableName;
            ws.Cell(row, 2).Value = e.FieldName;
            ws.Cell(row, 3).Value = e.DateTime.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 4).Value = e.UserName;
            ws.Cell(row, 5).Value = e.BeforeValue ?? "";
            ws.Cell(row, 6).Value = e.AfterValue ?? "";
            ws.Cell(row, 7).Value = e.Reason ?? "";
            ws.Cell(row, 8).Value = e.Key ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"FarmAuditLog_{NormaliseCphh(Cphh)}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>Strips display slashes and normalises to uppercase — matches the legacy Replace(sCPHH, "/", "") pattern.</summary>
    private static string NormaliseCphh(string cphh) => cphh.Replace("/", "").Trim().ToUpperInvariant();
}
