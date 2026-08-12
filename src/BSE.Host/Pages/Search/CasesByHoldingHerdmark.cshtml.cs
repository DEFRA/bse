using System.ComponentModel.DataAnnotations;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class CasesByHoldingHerdmarkModel : PageModel
{
    private readonly ICaseSearchService _search;
    private const int PageSize = 50;

    public CasesByHoldingHerdmarkModel(ICaseSearchService search) => _search = search;

    [BindProperty(SupportsGet = true)]
    [RegularExpression("^(?:\\d{2}(/)?\\d{3}(/)?\\d{4}(/)?\\d{2})?$", ErrorMessage = "Enter CPHH in the format NN/NNN/NNNN/NN or digits only.")]
    public string? Cphh { get; set; }

    [BindProperty(SupportsGet = true)]
    [RegularExpression("^[A-Za-z]{0,4}[0-9]{0,4}$", ErrorMessage = "Enter a valid herdmark.")]
    public string? Herdmark { get; set; }

    [BindProperty(SupportsGet = true)]
    [RegularExpression("^(?:\\d{6})?$", ErrorMessage = "Numeric herdmark must be 6 digits.")]
    public string? NumericHerdmark { get; set; }
    [BindProperty(SupportsGet = true)] public bool IncludeNonGb { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IReadOnlyList<CaseDetailSearchResult> Results { get; private set; } = [];
    public bool HasSearched { get; private set; }
    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<CaseDetailSearchResult> PagedResults =>
        Results.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public async Task OnGetAsync()
    {
        if (!ModelState.IsValid) return;

        if (!string.IsNullOrWhiteSpace(Cphh) || !string.IsNullOrWhiteSpace(Herdmark) || !string.IsNullOrWhiteSpace(NumericHerdmark))
        {
            var results = await _search.GetCasesByCphhAsync(
                (Cphh ?? "").Trim(),
                (Herdmark ?? "").Trim(),
                (NumericHerdmark ?? "").Trim(),
                IncludeNonGb);
            Results = results.ToList().AsReadOnly();
            HasSearched = true;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;
        }
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!HasAnyFilter()) return RedirectToPage();
        var results = await _search.GetCasesByCphhAsync(
            (Cphh ?? "").Trim(), (Herdmark ?? "").Trim(), (NumericHerdmark ?? "").Trim(), IncludeNonGb);
        return BuildExcel(results, $"CasesByHoldingHerdmark_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    private bool HasAnyFilter() =>
        !string.IsNullOrWhiteSpace(Cphh) || !string.IsNullOrWhiteSpace(Herdmark) || !string.IsNullOrWhiteSpace(NumericHerdmark);

    private static FileContentResult BuildExcel(IEnumerable<CaseDetailSearchResult> rows, string filename)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");
        string[] headers = ["RBSE", "CPHH", "Sex", "Eartag", "Birth Date", "Origin",
            "Date Purchased", "Age at Purchase", "Date Onset", "Form A Date",
            "Slaughter Date", "Final Result Date", "Age at Onset",
            "Fate", "Final Result", "Survey", "Case Status", "Time Elapsed"];
        for (var c = 1; c <= headers.Length; c++) { ws.Cell(1, c).Value = headers[c - 1]; ws.Cell(1, c).Style.Font.Bold = true; }
        var row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Rbse;
            ws.Cell(row, 2).Value = r.Cphh;
            ws.Cell(row, 3).Value = r.Sex;
            ws.Cell(row, 4).Value = r.Eartag;
            ws.Cell(row, 5).Value = r.BirthDate?.ToString("dd/MM/yyyy");
            ws.Cell(row, 6).Value = r.Origin;
            ws.Cell(row, 7).Value = r.PurchaseDate?.ToString("dd/MM/yyyy");
            ws.Cell(row, 8).Value = r.PurchaseAgeInMonths?.ToString();
            ws.Cell(row, 9).Value = r.OnsetDate?.ToString("dd/MM/yyyy");
            ws.Cell(row, 10).Value = r.FormADate?.ToString("dd/MM/yyyy");
            ws.Cell(row, 11).Value = r.SlaughterDate?.ToString("dd/MM/yyyy");
            ws.Cell(row, 12).Value = r.FinalResultDate?.ToString("dd/MM/yyyy");
            ws.Cell(row, 13).Value = r.OnsetAgeInMonths?.ToString();
            ws.Cell(row, 14).Value = r.Fate;
            ws.Cell(row, 15).Value = r.FinalResult;
            ws.Cell(row, 16).Value = r.Survey;
            ws.Cell(row, 17).Value = r.CaseStatus;
            ws.Cell(row, 18).Value = r.TimeElapsed;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new FileContentResult(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = filename };
    }
}
