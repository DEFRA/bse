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
public class FarmsModel : PageModel
{
    private readonly IFarmSearchService _search;
    private readonly ILookupDataService _lookups;

    public FarmsModel(IFarmSearchService search, ILookupDataService lookups)
    {
        _search = search;
        _lookups = lookups;
    }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public FarmSearchViewModel Filter { get; set; } = new();

    public IReadOnlyList<LookupItem> CountyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> AhoOptions { get; private set; } = [];

    public const string NoCriteriaMessage = "Please provide one or more search criteria";

    public bool NoCriteria { get; private set; }

    public async Task OnGetAsync()
    {
        CountyOptions = (await _lookups.GetLookupAsync(LookupTableId.BSECounty)).ToList();
        AhoOptions = (await _lookups.GetLookupAsync(LookupTableId.AHO)).ToList();

        if (!ModelState.IsValid) return;

        if (HasAnyFilter())
        {
            var results = await _search.SearchFarmsAsync(Filter.ToQuery());
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
        var rows = await _search.SearchFarmsAsync(Filter.ToQuery());
        return BuildExcel(rows, $"Farms_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    private static FileContentResult BuildExcel(IEnumerable<FarmSearchResult> rows, string filename)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");
        // Legacy exported the raw result-set column names, not the on-screen captions.
        string[] headers = ["CPHH", "OwnerName", "Address", "CorrespondenceAddress", "County", "Herdmark",
            "NumericHerdmark", "MapReference", "AHO", "HerdType",
            "CasesCount", "ConfirmedCasesCount"];
        for (var c = 1; c <= headers.Length; c++) { ws.Cell(1, c).Value = headers[c - 1]; ws.Cell(1, c).Style.Font.Bold = true; }
        var row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Cphh;
            ws.Cell(row, 2).Value = r.OwnerName;
            ws.Cell(row, 3).Value = r.Address;
            ws.Cell(row, 4).Value = r.CorrespondenceAddress;
            ws.Cell(row, 5).Value = r.County;
            ws.Cell(row, 6).Value = r.Herdmark;
            ws.Cell(row, 7).Value = r.NumericHerdmark;
            ws.Cell(row, 8).Value = r.MapReference;
            ws.Cell(row, 9).Value = r.Aho;
            ws.Cell(row, 10).Value = r.HerdType;
            ws.Cell(row, 11).Value = r.CasesCount;
            ws.Cell(row, 12).Value = r.ConfirmedCasesCount;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new FileContentResult(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = filename };
    }

    // Legacy ignored the Include Non-GB Farms checkbox when checking for search criteria.
    private bool HasAnyFilter() =>
        !string.IsNullOrWhiteSpace(Filter.Cphh) ||
        !string.IsNullOrWhiteSpace(Filter.OwnerName) ||
        !string.IsNullOrWhiteSpace(Filter.Address) ||
        !string.IsNullOrWhiteSpace(Filter.County) ||
        !string.IsNullOrWhiteSpace(Filter.Herdmark) ||
        !string.IsNullOrWhiteSpace(Filter.NumericHerdmark) ||
        Filter.IsDealer.HasValue ||
        !string.IsNullOrWhiteSpace(Filter.Aho);
}
