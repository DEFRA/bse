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

    public async Task OnGetAsync()
    {
        if (Filter.EarliestFormADate.HasValue || Filter.LatestFormADate.HasValue)
        {
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
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!Filter.EarliestFormADate.HasValue && !Filter.LatestFormADate.HasValue) return RedirectToPage();
        var query = Filter.ToQuery();
        var rows = Filter.SearchType switch
        {
            "Fates" => await _search.GetOutstandingFatesAsync(query),
            "Results" => await _search.GetOutstandingResultsAsync(query),
            _ => await _search.GetOutstandingBse1sAsync(query)
        };
        var label = Filter.SearchType switch { "Fates" => "Fates", "Results" => "Results", _ => "BSE1s" };
        return BuildExcel(rows, $"Outstanding{label}_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    private static FileContentResult BuildExcel(IEnumerable<OutstandingCaseResult> rows, string filename)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");
        string[] headers = ["RBSE", "CPHH", "Eartag", "Form A Date", "Birth Date", "Fate", "Final Result"];
        for (var c = 1; c <= headers.Length; c++) { ws.Cell(1, c).Value = headers[c - 1]; ws.Cell(1, c).Style.Font.Bold = true; }
        var row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Rbse;
            ws.Cell(row, 2).Value = r.Cphh;
            ws.Cell(row, 3).Value = r.Eartag;
            ws.Cell(row, 4).Value = r.FormADate?.ToString("dd/MM/yyyy");
            ws.Cell(row, 5).Value = r.BirthDate?.ToString("dd/MM/yyyy");
            ws.Cell(row, 6).Value = r.Fate;
            ws.Cell(row, 7).Value = r.FinalResult;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new FileContentResult(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = filename };
    }
}
