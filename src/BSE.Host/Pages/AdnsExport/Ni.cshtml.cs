using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.AdnsExport;

[Authorize(Policy = "DEFRAMaintenance")]
public class NiModel(IAdnsExportService adnsExportService) : PageModel
{
    private const int PageSize = 10;
    private const string DraftTempDataKey = "AdnsNiDraftCases";
    private const string PreviewTempDataKey = "AdnsNiPreview";
    private const string ContextTempDataKey = "AdnsNiContext";

    [BindProperty]
    [Required(ErrorMessage = "Enter an email reference.")]
    public string EmailReference { get; set; } = string.Empty;

    [BindProperty]
    [Range(2000, 2100, ErrorMessage = "ADNS year must be between 2000 and 2100.")]
    public int InputAdnsYear { get; set; } = DateTime.Today.Year;

    [BindProperty]
    [Range(1, 99999, ErrorMessage = "Enter an ADNS number between 1 and 99999.")]
    public int InputAdnsNumber { get; set; }

    [BindProperty]
    [Range(1, 99999, ErrorMessage = "Enter a valid region ID.")]
    public int InputAdnsRegionId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Enter a confirmation date.")]
    public DateTime? InputConfirmationDate { get; set; }

    [BindProperty]
    public string UserEmailAddress { get; set; } = string.Empty;

    [BindProperty]
    public bool SaveAdnsData { get; set; } = true;

    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = "AdnsReference";
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public AdnsExportPreview? Preview { get; private set; }
    public List<NiCaseInput> DraftCases { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    // No SMTP options dependency for NI page
    public string FromAddress => "tse.queries@apha.gov.uk";

    public IReadOnlyList<NiGridRow> CurrentRows =>
        Preview is null
            ? DraftCases.Select(ToGridRow).ToList()
            : Preview.Cases.Select(c => new NiGridRow(
                c.AdnsYear,
                c.AdnsNumber,
                c.AdnsRegionId,
                c.AdnsRegionName ?? $"{c.AdnsRegionId:00000}",
                c.ConfirmationDate,
                $"{c.AdnsYear}|{c.AdnsNumber}|{c.AdnsRegionId}|{c.ConfirmationDate:O}")).ToList();

    public int TotalCount => CurrentRows.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<NiGridRow> PagedRows =>
        ApplySorting(CurrentRows).Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public IActionResult OnGet()
    {
        RestoreContext();
        LoadDraftCases();
        LoadPreview();
        return Page();
    }

    public IActionResult OnPostAddToGrid()
    {
        RestoreContext();
        LoadDraftCases();

        if (!ModelState.IsValid)
            return Page();

        if (InputConfirmationDate is null)
        {
            ModelState.AddModelError(nameof(InputConfirmationDate), "Enter a confirmation date.");
            return Page();
        }

        var newCase = new NiCaseInput(
            Rbse: string.Empty,
            AdnsYear: InputAdnsYear,
            AdnsNumber: InputAdnsNumber,
            AdnsRegionId: InputAdnsRegionId,
            AdnsRegionName: $"{InputAdnsRegionId:00000}",
            ConfirmationDate: InputConfirmationDate.Value);

        DraftCases.Add(newCase);
        SaveDraftCases();
        PersistContext();

        // Grid changed -> stale preview invalid
        TempData.Remove(PreviewTempDataKey);
        Preview = null;

        // reset current entry fields
        InputAdnsNumber = 0;
        InputAdnsRegionId = 0;
        InputConfirmationDate = null;

        return Page();
    }

    public IActionResult OnPostDeleteFromGrid(string rowKey)
    {
        RestoreContext();
        LoadDraftCases();

        RemoveModelStateFor(nameof(EmailReference));
        RemoveModelStateFor(nameof(InputAdnsYear));
        RemoveModelStateFor(nameof(InputAdnsNumber));
        RemoveModelStateFor(nameof(InputAdnsRegionId));
        RemoveModelStateFor(nameof(InputConfirmationDate));
        RemoveModelStateFor(nameof(UserEmailAddress));

        if (string.IsNullOrWhiteSpace(rowKey))
        {
            ModelState.AddModelError(string.Empty, "Selected record could not be found.");
            return Page();
        }

        var removed = DraftCases.RemoveAll(c => BuildRowKey(c) == rowKey) > 0;
        if (!removed)
        {
            ModelState.AddModelError(string.Empty, "Selected record could not be found.");
            return Page();
        }

        SaveDraftCases();
        PersistContext();

        // Grid changed -> stale preview invalid
        TempData.Remove(PreviewTempDataKey);
        Preview = null;

        return Page();
    }

    public IActionResult OnPostGenerateReport()
    {
        RestoreContext();
        LoadDraftCases();

        // Generate Report should not validate add-row/dispatch-only fields.
        RemoveModelStateFor(nameof(InputAdnsYear));
        RemoveModelStateFor(nameof(InputAdnsNumber));
        RemoveModelStateFor(nameof(InputAdnsRegionId));
        RemoveModelStateFor(nameof(InputConfirmationDate));
        RemoveModelStateFor(nameof(UserEmailAddress));

        if (string.IsNullOrWhiteSpace(EmailReference))
            ModelState.AddModelError(nameof(EmailReference), "Enter an email reference.");

        if (!ModelState.IsValid)
            return Page();

        if (DraftCases.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Add at least one case to the grid before generating the report.");
            return Page();
        }

        Preview = adnsExportService.PreviewNiExport(EmailReference, DraftCases);
        TempData[PreviewTempDataKey] = JsonSerializer.Serialize(Preview);
        PersistContext();

        return Page();
    }

    public async Task<IActionResult> OnPostDispatchAsync()
    {
        RestoreContext();
        LoadDraftCases();

        RemoveModelStateFor(nameof(InputAdnsYear));
        RemoveModelStateFor(nameof(InputAdnsNumber));
        RemoveModelStateFor(nameof(InputAdnsRegionId));
        RemoveModelStateFor(nameof(InputConfirmationDate));

        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Enter your email address.");
            LoadPreview();
            return Page();
        }

        var previewJson = TempData.Peek(PreviewTempDataKey)?.ToString();
        if (string.IsNullOrEmpty(previewJson))
        {
            ErrorMessage = "Session expired — please generate the report again.";
            return Page();
        }

        Preview = JsonSerializer.Deserialize<AdnsExportPreview>(previewJson);
        var cases = Preview?.Cases.ToList() ?? [];

        var command = new DispatchAdnsCommand("NI", EmailReference, cases, UserEmailAddress, SaveAdnsData);

        try
        {
            await adnsExportService.DispatchAsync(command);
            TempData.Remove(PreviewTempDataKey);
            TempData.Remove(DraftTempDataKey);
            TempData.Remove(ContextTempDataKey);
            TempData["Success"] = "NI ADNS export dispatched successfully.";
            return RedirectToPage("/AdnsExport/Menu");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Dispatch failed: {ex.Message}";
            return Page();
        }
    }

    private static NiGridRow ToGridRow(NiCaseInput c) =>
        new(
            c.AdnsYear,
            c.AdnsNumber,
            c.AdnsRegionId,
            string.IsNullOrWhiteSpace(c.AdnsRegionName) ? $"{c.AdnsRegionId:00000}" : c.AdnsRegionName,
            c.ConfirmationDate,
            BuildRowKey(c));

    private static string BuildRowKey(NiCaseInput c) =>
        $"{c.AdnsYear}|{c.AdnsNumber}|{c.AdnsRegionId}|{c.ConfirmationDate:O}";

    private IEnumerable<NiGridRow> ApplySorting(IEnumerable<NiGridRow> rows) => SortColumn switch
    {
        "Region" => SortDesc ? rows.OrderByDescending(r => r.RegionName) : rows.OrderBy(r => r.RegionName),
        "ConfirmationDate" => SortDesc ? rows.OrderByDescending(r => r.ConfirmationDate) : rows.OrderBy(r => r.ConfirmationDate),
        "AdnsReference" => SortDesc ? rows.OrderByDescending(r => r.AdnsReference) : rows.OrderBy(r => r.AdnsReference),
        _ => rows.OrderBy(r => r.AdnsReference),
    };

    private void LoadDraftCases()
    {
        var json = TempData.Peek(DraftTempDataKey)?.ToString();
        DraftCases = string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<NiCaseInput>>(json) ?? [];
    }

    private void SaveDraftCases() =>
        TempData[DraftTempDataKey] = JsonSerializer.Serialize(DraftCases);

    private void LoadPreview()
    {
        var json = TempData.Peek(PreviewTempDataKey)?.ToString();
        Preview = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<AdnsExportPreview>(json);
    }

    private void PersistContext()
    {
        var ctx = new NiContext(EmailReference, InputAdnsYear);
        TempData[ContextTempDataKey] = JsonSerializer.Serialize(ctx);
    }

    private void RestoreContext()
    {
        var json = TempData.Peek(ContextTempDataKey)?.ToString();
        if (string.IsNullOrWhiteSpace(json)) return;

        var ctx = JsonSerializer.Deserialize<NiContext>(json);
        if (ctx is null) return;

        EmailReference = ctx.EmailReference;
        InputAdnsYear = ctx.AdnsYear;
    }

    private void RemoveModelStateFor(string propertyName)
    {
        var keys = ModelState.Keys
            .Where(k => k.Equals(propertyName, StringComparison.OrdinalIgnoreCase)
                     || k.StartsWith(propertyName + ".", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keys)
            ModelState.Remove(key);
    }

    public sealed record NiGridRow(
        int AdnsYear,
        int AdnsNumber,
        int AdnsRegionId,
        string RegionName,
        DateTime ConfirmationDate,
        string RowKey)
    {
        public string AdnsReference => $"{AdnsYear}/{AdnsNumber:00000}";
    }

    private sealed record NiContext(string EmailReference, int AdnsYear);
}