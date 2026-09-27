using System.Globalization;
using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Bsess;

[Authorize(Policy = "AuditAccess")]
public class CheckByDateModel(IBsessCheckService bsessCheckService) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = "Rbse";
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IReadOnlyList<BsessDiscrepancyRecord> Discrepancies { get; private set; } = [];
    public IReadOnlyList<BsessDiscrepancyRecord> PagedDiscrepancies { get; private set; } = [];
    public int PageSize { get; } = 10;
    public int TotalPages { get; private set; } = 1;
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(StartDate)))
        {
            if (!StartDate.HasValue)
                ModelState.AddModelError(nameof(StartDate), "Enter a start date");
            if (!EndDate.HasValue)
                ModelState.AddModelError(nameof(EndDate), "Enter an end date");
            if (StartDate.HasValue && EndDate.HasValue && StartDate.Value > EndDate.Value)
                ModelState.AddModelError(nameof(EndDate), "The end date must be on or after the start date");

            if (ModelState.IsValid)
            {
                HasSearched = true;
                Discrepancies = await bsessCheckService.GetCheckByDateAsync(StartDate!.Value, EndDate!.Value);
                ApplySortAndPaging();
            }
        }
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!Request.Query.ContainsKey(nameof(StartDate)) || !StartDate.HasValue || !EndDate.HasValue)
            return RedirectToPage();

        var discrepancies = await bsessCheckService.GetCheckByDateAsync(StartDate.Value, EndDate.Value);
        var ordered = ApplySort(discrepancies);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("TSES Check Results");
        ws.ShowGridLines = false;

        string[] headers = ["RBSE", "TSES Birth Date", "BSE Birth Date", "TSES Eartag", "BSE Eartag", "TSES Test Group", "BSE Test Group"];
        for (var col = 1; col <= headers.Length; col++)
            ws.Cell(1, col).Value = headers[col - 1];

        var row = 2;
        foreach (var d in ordered)
        {
            ws.Cell(row, 1).Value = d.Rbse;
            ws.Cell(row, 2).Value = d.BsessBirthDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
            ws.Cell(row, 3).Value = d.BseBirthDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
            ws.Cell(row, 4).Value = d.BsessEartag ?? string.Empty;
            ws.Cell(row, 5).Value = d.BseEartag ?? string.Empty;
            ws.Cell(row, 6).Value = d.BsessTestGroup ?? string.Empty;
            ws.Cell(row, 7).Value = d.BseTestGroup ?? string.Empty;
            row++;
        }

        var range = ws.Range(1, 1, row - 1, headers.Length);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "tses-check-by-date.xlsx");
    }

    private void ApplySortAndPaging()
    {
        var ordered = ApplySort(Discrepancies);
        TotalPages = Math.Max(1, (int)Math.Ceiling(ordered.Count / (double)PageSize));
        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages) PageNumber = TotalPages;
        PagedDiscrepancies = ordered.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList().AsReadOnly();
    }

    private IReadOnlyList<BsessDiscrepancyRecord> ApplySort(IReadOnlyList<BsessDiscrepancyRecord> source)
    {
        var ordered = SortColumn?.ToLowerInvariant() switch
        {
            "rbse" => SortDesc ? source.OrderByDescending(x => x.Rbse).ToList() : source.OrderBy(x => x.Rbse).ToList(),
            "bsessbirthdate" => SortDesc ? source.OrderByDescending(x => x.BsessBirthDate ?? DateTime.MinValue).ToList() : source.OrderBy(x => x.BsessBirthDate ?? DateTime.MinValue).ToList(),
            "bsebirthdate" => SortDesc ? source.OrderByDescending(x => x.BseBirthDate ?? DateTime.MinValue).ToList() : source.OrderBy(x => x.BseBirthDate ?? DateTime.MinValue).ToList(),
            "bsesseartag" => SortDesc ? source.OrderByDescending(x => x.BsessEartag ?? string.Empty).ToList() : source.OrderBy(x => x.BsessEartag ?? string.Empty).ToList(),
            "bseeartag" => SortDesc ? source.OrderByDescending(x => x.BseEartag ?? string.Empty).ToList() : source.OrderBy(x => x.BseEartag ?? string.Empty).ToList(),
            "bsesstestgroup" => SortDesc ? source.OrderByDescending(x => x.BsessTestGroup ?? string.Empty).ToList() : source.OrderBy(x => x.BsessTestGroup ?? string.Empty).ToList(),
            "bsetestgroup" => SortDesc ? source.OrderByDescending(x => x.BseTestGroup ?? string.Empty).ToList() : source.OrderBy(x => x.BseTestGroup ?? string.Empty).ToList(),
            _ => SortDesc ? source.OrderByDescending(x => x.Rbse).ToList() : source.OrderBy(x => x.Rbse).ToList(),
        };

        return ordered;
    }
}
