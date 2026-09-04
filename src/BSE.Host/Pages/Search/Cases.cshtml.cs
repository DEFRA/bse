using BSE.Host.Models.ViewModels;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using BSE.SharedKernel;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class CasesModel : PageModel
{
    private readonly ICaseSearchService _search;
    private readonly ILookupDataService _lookups;

    public CasesModel(ICaseSearchService search, ILookupDataService lookups)
    {
        _search = search;
        _lookups = lookups;
    }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public CaseSearchViewModel Filter { get; set; } = new();

    public IReadOnlyList<LookupItem> SexOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> SurveyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> FateOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> FinalResultOptions { get; private set; } = [];

    public const string NoCriteriaMessage = "Please provide one or more search criteria";

    public bool NoCriteria { get; private set; }

    public async Task OnGetAsync()
    {
        SexOptions = (await _lookups.GetSexesAsync())
            .Select(x => new LookupItem(x.Id, x.Description))
            .ToList();
        SurveyOptions = (await _lookups.GetLookupAsync(LookupTableId.Survey)).ToList();
        FateOptions = (await _lookups.GetCaseFatesAsync())
            .Select(x => new LookupItem(x.Id, x.Description))
            .ToList();
        FinalResultOptions = (await _lookups.GetTestResultsAsync())
            .Select(x => new LookupItem(x.Id, x.Description))
            .ToList();

        if (HasAnyFilter())
        {
            var query = Filter.ToQuery();
            var results = await _search.SearchCasesAsync(query);
            Filter.Results = results.ToList().AsReadOnly();
            Filter.HasSearched = true;
            if (Filter.PageNumber < 1) Filter.PageNumber = 1;
            if (Filter.PageNumber > Filter.TotalPages) Filter.PageNumber = Filter.TotalPages;
        }
        else
        {
            NoCriteria = Request.Query.Count > 0;
        }
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!HasAnyFilter()) return RedirectToPage();

        var results = await _search.SearchCasesAsync(Filter.ToQuery());

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Case Search Results");

        // Legacy exported the raw result-set column names, not the on-screen captions.
        string[] headers =
        [
            "RBSE", "CPHH", "Sex", "Survey", "Eartag", "BirthDate", "IsBirthDateEst",
            "FormADate", "Fate", "FinalResult", "FinalResultDate",
            "DBSE", "Notes", "BabNotes", "Origin", "ValuationAge"
        ];

        for (var col = 1; col <= headers.Length; col++)
        {
            ws.Cell(1, col).Value = headers[col - 1];
            ws.Cell(1, col).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var r in results)
        {
            ws.Cell(row, 1).Value = r.Rbse;
            ws.Cell(row, 2).Value = r.Cphh;
            ws.Cell(row, 3).Value = r.Sex;
            ws.Cell(row, 4).Value = r.Survey;
            ws.Cell(row, 5).Value = r.Eartag;
            ws.Cell(row, 6).Value = r.BirthDate.HasValue ? r.BirthDate.Value.ToString("dd/MM/yyyy") : "";
            ws.Cell(row, 7).Value = r.IsBirthDateEst;
            ws.Cell(row, 8).Value = r.FormADate.HasValue ? r.FormADate.Value.ToString("dd/MM/yyyy") : "";
            ws.Cell(row, 9).Value = r.Fate;
            ws.Cell(row, 10).Value = r.FinalResult;
            ws.Cell(row, 11).Value = r.FinalResultDate.HasValue ? r.FinalResultDate.Value.ToString("dd/MM/yyyy") : "";
            ws.Cell(row, 12).Value = r.Dbse;
            ws.Cell(row, 13).Value = r.Notes;
            ws.Cell(row, 14).Value = r.BabNotes;
            ws.Cell(row, 15).Value = r.Origin;
            ws.Cell(row, 16).Value = r.ValuationAge;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"CaseSearch_{DateTime.Today:yyyyMMdd}.xlsx");
    }
    private bool HasAnyFilter() =>
        !string.IsNullOrWhiteSpace(Filter.Rbse) ||
        !string.IsNullOrWhiteSpace(Filter.Eartag) ||
        !string.IsNullOrWhiteSpace(Filter.Dbse) ||
        !string.IsNullOrWhiteSpace(Filter.Fate) ||
        !string.IsNullOrWhiteSpace(Filter.FinalResult) ||
        !string.IsNullOrWhiteSpace(Filter.Sex) ||
        !string.IsNullOrWhiteSpace(Filter.Survey) ||
        !string.IsNullOrWhiteSpace(Filter.Notes) ||
        !string.IsNullOrWhiteSpace(Filter.EarliestFormADate) ||
        !string.IsNullOrWhiteSpace(Filter.LatestFormADate) ||
        !string.IsNullOrWhiteSpace(Filter.EarliestFinalResultDate) ||
        !string.IsNullOrWhiteSpace(Filter.LatestFinalResultDate) ||
        !string.IsNullOrWhiteSpace(Filter.EarliestBirthDate) ||
        !string.IsNullOrWhiteSpace(Filter.LatestBirthDate) ||
        !string.IsNullOrWhiteSpace(Filter.PassiveActive) ||
        Filter.IsImportedCase ||
        Filter.IncludeNonGb;
}
