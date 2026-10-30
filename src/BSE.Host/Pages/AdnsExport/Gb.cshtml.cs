using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BSE.Host.Pages.AdnsExport;

[Authorize(Policy = "DEFRAMaintenance")]
public class GbModel(
    IAdnsExportService adnsExportService,
    IOptions<AdnsMsGraphOptions> msGraphOptions) : AdnsExportPageModelBase(adnsExportService, msGraphOptions)
{
    private const int PageSize = 10;
    private const string PreviewTempDataKey = "AdnsGbPreview";
    private const string ContextTempDataKey = "AdnsGbContext";

    [BindProperty]
    [Required(ErrorMessage = "Enter an email reference.")]
    [StringLength(50, ErrorMessage = "Email reference must be 50 characters or fewer.")]
    public string EmailReference { get; set; } = string.Empty;

    [BindProperty]
    [Range(2000, 2100, ErrorMessage = "ADNS year must be between 2000 and 2100.")]
    public int AdnsYear { get; set; } = DateTime.Today.Year;

    [BindProperty]
    [Range(1, 99999, ErrorMessage = "Start ADNS number must be between 1 and 99999.")]
    public int StartAdnsNumber { get; set; } = 1;

    [BindProperty] public bool SaveAdnsData { get; set; } = true;

    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public LastAdnsReferenceRecord? LastReference { get; private set; }

    public int TotalCount => Preview?.Cases.Count ?? 0;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<AdnsCaseRecord> PagedCases() =>
        ApplySorting(Preview?.Cases ?? []).Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    private IEnumerable<AdnsCaseRecord> ApplySorting(IEnumerable<AdnsCaseRecord> cases) => SortColumn switch
    {
        "Region" => SortDesc ? cases.OrderByDescending(c => c.AdnsRegionName) : cases.OrderBy(c => c.AdnsRegionName),
        "AdnsReference" => SortDesc ? cases.OrderByDescending(c => c.AdnsReference) : cases.OrderBy(c => c.AdnsReference),
        "ConfirmationDate" => SortDesc ? cases.OrderByDescending(c => c.ConfirmationDate) : cases.OrderBy(c => c.ConfirmationDate),
        "Rbse" => SortDesc ? cases.OrderByDescending(c => c.Rbse) : cases.OrderBy(c => c.Rbse),
        _ => cases.OrderBy(c => c.Rbse),
    };

    public async Task<IActionResult> OnGetAsync()
    {
        // A fresh visit to this page (e.g. via the breadcrumb or menu) always starts clean —
        // a previously generated report must not resurface just because TempData hadn't expired yet.
        TempData.Remove(PreviewTempDataKey);
        TempData.Remove(ContextTempDataKey);

        LastReference = await ExportService.GetLastReferenceAsync("GB");
        if (LastReference is not null)
        {
            AdnsYear = LastReference.LastAdnsReferenceYear ?? DateTime.Today.Year;
            StartAdnsNumber = (LastReference.LastAdnsReferenceNumber ?? 0) + 1;
            EmailReference = $"DBSE{StartAdnsNumber:00000}";
        }
        ApplyDefaultRecipientIfBlank();

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateReportAsync()
    {
        ModelState.Remove(nameof(UserEmailAddress));

        if (!ModelState.IsValid)
        {
            ApplyDefaultRecipientIfBlank();
            return Page();
        }

        try
        {
            Preview = await ExportService.PreviewGbExportAsync(EmailReference, AdnsYear, StartAdnsNumber);
            Message = Preview.EmailBody;
            PersistPreview(Preview);
            PersistContext();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Report generation failed: {ex.Message}";
        }

        ApplyDefaultRecipientIfBlank();
        return Page();
    }

    public async Task<IActionResult> OnPostDispatchAsync()
    {
        if (!TryLoadPreview(out var preview))
        {
            SetSessionExpired();
            return Page();
        }

        // Keep the preview available in case validation below fails and we re-render this same form.
        TempData.Keep(PreviewTempDataKey);
        TempData.Keep(ContextTempDataKey);

        Preview = preview;
        RestoreContext();

        if (!HasValidRecipient())
            return Page();

        var command = new DispatchAdnsCommand(
            Area: "GB",
            EmailReference: EmailReference,
            Cases: preview!.Cases.ToList(),
            UserEmailAddress: UserEmailAddress,
            SaveAdnsData: SaveAdnsData,
            Message: Message.Trim());

        if (!await TryDispatchAsync(command))
            return Page();

        TempData.Remove(PreviewTempDataKey);
        TempData.Remove(ContextTempDataKey);
        return RedirectToPage();
    }

    private void PersistPreview(AdnsExportPreview preview) =>
        TempData[PreviewTempDataKey] = JsonSerializer.Serialize(preview);

    private bool TryLoadPreview(out AdnsExportPreview? preview)
    {
        preview = DeserializePreview(TempData[PreviewTempDataKey]?.ToString());
        return preview is not null;
    }

    private void PersistContext()
    {
        var context = new GbPreviewContext(EmailReference, AdnsYear, StartAdnsNumber);
        TempData[ContextTempDataKey] = JsonSerializer.Serialize(context);
    }

    private void RestoreContext()
    {
        var ctxJson = TempData.Peek(ContextTempDataKey)?.ToString();
        if (string.IsNullOrWhiteSpace(ctxJson)) return;

        var ctx = JsonSerializer.Deserialize<GbPreviewContext>(ctxJson);
        if (ctx is null) return;

        EmailReference = ctx.EmailReference;
        AdnsYear = ctx.AdnsYear;
        StartAdnsNumber = ctx.StartAdnsNumber;
    }

    private sealed record GbPreviewContext(string EmailReference, int AdnsYear, int StartAdnsNumber);
}