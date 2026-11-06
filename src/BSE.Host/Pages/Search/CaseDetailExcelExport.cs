using BSE.Modules.Search.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Pages.Search;

/// <summary>Excel export shared by the case detail search result pages.</summary>
public static class CaseDetailExcelExport
{
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm:ss";

    public static FileContentResult Build(IEnumerable<CaseDetailSearchResult> rows, string filename)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");
        // Legacy's HTML export had no gridlines outside the bordered table; match that here.
        ws.ShowGridLines = false;
        // Legacy exported the raw result-set column names, not the on-screen captions.
        // Legacy's DataGrid export bound to the whole result set, so it also included the
        // hidden DaysElapsed sort value alongside the on-screen TimeElapsed column.
        string[] headers = ["RBSE", "CPHH", "Sex", "Eartag", "BirthDate", "Origin",
            "PurchaseDate", "PurchaseAgeInMonths", "OnsetDate", "FormADate",
            "SlaughterDate", "FinalResultDate", "OnsetAgeInMonths",
            "Fate", "FinalResult", "Survey", "CaseStatus", "TimeElapsed", "DaysElapsed"];
        // Legacy's exported header row was plain text, not bold.
        for (var c = 1; c <= headers.Length; c++) { ws.Cell(1, c).Value = headers[c - 1]; }
        var row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Rbse;
            ws.Cell(row, 2).Value = r.Cphh;
            ws.Cell(row, 3).Value = r.Sex;
            ws.Cell(row, 4).Value = r.Eartag;
            ws.Cell(row, 5).Value = r.BirthDate?.ToString(DateTimeFormat);
            ws.Cell(row, 6).Value = r.Origin;
            ws.Cell(row, 7).Value = r.PurchaseDate?.ToString(DateTimeFormat);
            ws.Cell(row, 8).Value = r.PurchaseAgeInMonths?.ToString();
            ws.Cell(row, 9).Value = r.OnsetDate?.ToString(DateTimeFormat);
            ws.Cell(row, 10).Value = r.FormADate?.ToString(DateTimeFormat);
            ws.Cell(row, 11).Value = r.SlaughterDate?.ToString(DateTimeFormat);
            ws.Cell(row, 12).Value = r.FinalResultDate?.ToString(DateTimeFormat);
            ws.Cell(row, 13).Value = r.OnsetAgeInMonths?.ToString();
            ws.Cell(row, 14).Value = r.Fate;
            ws.Cell(row, 15).Value = r.FinalResult;
            ws.Cell(row, 16).Value = r.Survey;
            ws.Cell(row, 17).Value = r.CaseStatus;
            ws.Cell(row, 18).Value = r.TimeElapsed;
            ws.Cell(row, 19).Value = r.DaysElapsed?.ToString();
            row++;
        }
        // Legacy rendered the exported grid with all borders around the record area only.
        var recordRange = ws.Range(1, 1, row - 1, headers.Length);
        recordRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        recordRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new FileContentResult(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = filename };
    }
}
