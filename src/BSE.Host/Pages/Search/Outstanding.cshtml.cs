using BSE.Host.Models.ViewModels;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class OutstandingModel : PageModel
{
    private readonly IOutstandingDataSearchService _search;

    public OutstandingModel(IOutstandingDataSearchService search) => _search = search;

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public OutstandingSearchViewModel Filter { get; set; } = new();

    public const string NoOptionMessage = "Please select one of these three options";

    public bool NoOptionSelected { get; private set; }

    public async Task OnGetAsync()
    {
        // Legacy required one of the three options; the date range is optional.
        if (!IsKnownSearchType())
        {
            NoOptionSelected = Request.Query.Count > 0;
            return;
        }

        var query = Filter.ToQuery();
        var results = Filter.SearchType switch
        {
            "Fates" => await _search.GetOutstandingFatesAsync(query),
            "Results" => await _search.GetOutstandingResultsAsync(query),
            _ => await _search.GetOutstandingBse1sAsync(query)
        };
        Filter.Results = results.ToList().AsReadOnly();
        Filter.HasSearched = true;
        if (Filter.PageNumber < 1) Filter.PageNumber = 1;
        if (Filter.PageNumber > Filter.TotalPages) Filter.PageNumber = Filter.TotalPages;
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!IsKnownSearchType()) return RedirectToPage();
        var query = Filter.ToQuery();
        var rows = Filter.SearchType switch
        {
            "Fates" => await _search.GetOutstandingFatesAsync(query),
            "Results" => await _search.GetOutstandingResultsAsync(query),
            _ => await _search.GetOutstandingBse1sAsync(query)
        };
        // Legacy exported all three outstanding-data variants under the same fixed file name.
        return BuildExcel(rows, "outstandingdatasearchresults.xlsx");
    }

    private bool IsKnownSearchType() =>
        Filter.SearchType is "BSE1" or "Fates" or "Results";

    private static FileContentResult BuildExcel(IEnumerable<OutstandingCaseResult> rows, string filename)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");
        // Legacy's HTML export had no gridlines outside the bordered table; match that here.
        ws.ShowGridLines = false;
        // Legacy exported the raw result-set column names, not the on-screen captions.
        string[] headers = ["RBSE", "CPHH", "Eartag", "FormADate", "BirthDate", "Fate", "FinalResult"];
        for (var c = 1; c <= headers.Length; c++) { ws.Cell(1, c).Value = headers[c - 1]; ws.Cell(1, c).Style.Font.Bold = true; }
        var row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Rbse;
            ws.Cell(row, 2).Value = r.Cphh;
            ws.Cell(row, 3).Value = r.Eartag;
            ws.Cell(row, 4).Value = r.FormADate?.ToString("dd/MM/yyyy HH:mm:ss");
            ws.Cell(row, 5).Value = r.BirthDate?.ToString("dd/MM/yyyy HH:mm:ss");
            ws.Cell(row, 6).Value = r.Fate;
            ws.Cell(row, 7).Value = r.FinalResult;
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
