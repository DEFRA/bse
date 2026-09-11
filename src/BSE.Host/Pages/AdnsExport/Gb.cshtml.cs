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
    IOptions<AdnsSmtpOptions> smtpOptions) : PageModel
{
    private const int PageSize = 10;
    private const string PreviewTempDataKey = "AdnsGbPreview";
    private const string ContextTempDataKey = "AdnsGbContext";
    private readonly AdnsSmtpOptions _smtpOptions = smtpOptions.Value;

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

    [BindProperty]
    public string UserEmailAddress { get; set; } = string.Empty;

    [BindProperty] public bool SaveAdnsData { get; set; } = true;

    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public AdnsExportPreview? Preview { get; private set; }
    public LastAdnsReferenceRecord? LastReference { get; private set; }
    public string? ErrorMessage { get; private set; }

    public string FromEmailAddress => _smtpOptions.FromAddress;
    public string DefaultToEmailAddress => _smtpOptions.ToAddress;

    public int TotalCount => Preview?.Cases.Count ?? 0;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<AdnsCaseRecord> PagedCases =>
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
        if (TryLoadPreview(out var preview))
        {
            Preview = preview;
            RestoreContext();
            if (string.IsNullOrWhiteSpace(UserEmailAddress))
            {
                UserEmailAddress = DefaultToEmailAddress;
            }
            return Page();
        }

        LastReference = await adnsExportService.GetLastReferenceAsync("GB");
        if (LastReference is not null)
        {
            AdnsYear = LastReference.LastAdnsReferenceYear ?? DateTime.Today.Year;
            StartAdnsNumber = (LastReference.LastAdnsReferenceNumber ?? 0) + 1;
        }
        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            UserEmailAddress = DefaultToEmailAddress;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateReportAsync()
    {
        ModelState.Remove(nameof(UserEmailAddress));

        if (!ModelState.IsValid)
        {
            if (string.IsNullOrWhiteSpace(UserEmailAddress))
            {
                UserEmailAddress = DefaultToEmailAddress;
            }
            return Page();
        }
            

        try
        {
            Preview = await adnsExportService.PreviewGbExportAsync(EmailReference, AdnsYear, StartAdnsNumber);
            PersistPreview(Preview);
            PersistContext();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Report generation failed: {ex.Message}";
        }

        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            UserEmailAddress = DefaultToEmailAddress;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostDispatchAsync()
    {
        if (!TryLoadPreview(out var preview))
        {
            ErrorMessage = "Session expired — please generate the report again.";
            return Page();
        }

        Preview = preview;
        RestoreContext();

        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Enter your email address.");
            return Page();
        }

        if (!new EmailAddressAttribute().IsValid(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Enter an email address in the correct format, like name@example.com.");
            return Page();
        }

        var command = new DispatchAdnsCommand(
            Area: "GB",
            EmailReference: EmailReference,
            Cases: preview!.Cases.ToList(),
            UserEmailAddress: UserEmailAddress,
            SaveAdnsData: SaveAdnsData);

        try
        {
            await adnsExportService.DispatchAsync(command);
            TempData.Remove(PreviewTempDataKey);
            TempData.Remove(ContextTempDataKey);
            TempData["Success"] = "GB ADNS export dispatched successfully.";
            return RedirectToPage("/AdnsExport/Menu");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Dispatch failed: {ex.Message}";
            return Page();
        }
    }

    private void PersistPreview(AdnsExportPreview preview) =>
        TempData[PreviewTempDataKey] = JsonSerializer.Serialize(preview);

    private bool TryLoadPreview(out AdnsExportPreview? preview)
    {
        var previewJson = TempData.Peek(PreviewTempDataKey)?.ToString();
        preview = string.IsNullOrWhiteSpace(previewJson)
            ? null
            : JsonSerializer.Deserialize<AdnsExportPreview>(previewJson);
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