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
        IReadOnlyList<(string Header, Func<T, object?> Value)>? extraColumns = null)
        where T : AuditLogEntry
    {
        extraColumns ??= [];

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sheetName);

        var headers = BaseHeaders.Concat(extraColumns.Select(c => c.Header)).ToArray();
        for (var col = 1; col <= headers.Length; col++)
        {
            ws.Cell(1, col).Value = headers[col - 1];
            ws.Cell(1, col).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var entry in entries)
        {
            ws.Cell(row, 1).Value = entry.TableName;
            ws.Cell(row, 2).Value = entry.FieldName;
            ws.Cell(row, 3).Value = entry.DateTime.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 4).Value = entry.UserName;
            ws.Cell(row, 5).Value = entry.BeforeValue;
            ws.Cell(row, 6).Value = entry.AfterValue;
            ws.Cell(row, 7).Value = entry.Reason;
            ws.Cell(row, 8).Value = entry.Key;

            var col = BaseHeaders.Length + 1;
            foreach (var extra in extraColumns)
            {
                ws.Cell(row, col).Value = extra.Value(entry)?.ToString();
                col++;
            }
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new FileContentResult(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        { FileDownloadName = fileName };
    }
}
