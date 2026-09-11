using BSE.Modules.AuditLog.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Helpers;

/// <summary>Builds the audit log Excel exports offered by every legacy AuditLog page.</summary>
internal static class AuditLogExcel
{
    // Legacy exported the raw result-set column names, not the on-screen captions.
    private static readonly string[] BaseHeaders =
        ["TableName", "FieldName", "DateTime", "UserName", "BeforeValue", "AfterValue", "Reason", "Key"];

    public static FileContentResult Build<T>(
        IEnumerable<T> entries,
        string sheetName,
        string fileName,
        IReadOnlyList<(string Header, Func<T, object?> Value)>? extraColumns = null,
        IReadOnlyList<(string Header, Func<T, object?> Value)>? leadingColumns = null)
        where T : AuditLogEntry
    {
        extraColumns ??= [];
        leadingColumns ??= [];

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sheetName);
        // Legacy's HTML export had no gridlines outside the bordered table; match that here.
        ws.ShowGridLines = false;

        var headers = leadingColumns.Select(c => c.Header)
            .Concat(BaseHeaders)
            .Concat(extraColumns.Select(c => c.Header))
            .ToArray();
        for (var col = 1; col <= headers.Length; col++)
        {
            ws.Cell(1, col).Value = headers[col - 1];
            ws.Cell(1, col).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var entry in entries)
        {
            var col = 1;
            foreach (var leading in leadingColumns)
            {
                ws.Cell(row, col).Value = leading.Value(entry)?.ToString();
                col++;
            }

            ws.Cell(row, col++).Value = entry.TableName;
            ws.Cell(row, col++).Value = entry.FieldName;
            ws.Cell(row, col++).Value = entry.DateTime.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, col++).Value = entry.UserName;
            ws.Cell(row, col++).Value = entry.BeforeValue;
            ws.Cell(row, col++).Value = entry.AfterValue;
            ws.Cell(row, col++).Value = entry.Reason;
            ws.Cell(row, col++).Value = entry.Key;

            foreach (var extra in extraColumns)
            {
                ws.Cell(row, col).Value = extra.Value(entry)?.ToString();
                col++;
            }
            row++;
        }

        // Legacy rendered the exported grid with all borders around the record area only.
        var recordRange = ws.Range(1, 1, row - 1, headers.Length);
        recordRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        recordRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new FileContentResult(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        { FileDownloadName = fileName };
    }
}
