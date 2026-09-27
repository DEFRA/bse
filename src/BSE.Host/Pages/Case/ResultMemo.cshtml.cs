using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

/// <summary>
/// Migrated equivalent of legacy ResultMemo.aspx. The same markup serves both actions:
/// Print renders it for the browser print dialog, Download re-serves it with a Word
/// content type — exactly as the legacy page did via Response.ContentType.
/// </summary>
[Authorize(Policy = "DEFRAMaintenance")]
public class ResultMemoModel(ICaseService cases, ILogger<ResultMemoModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;

    /// <summary>When true the page auto-opens the print dialog; otherwise it downloads as a .doc.</summary>
    [BindProperty(SupportsGet = true)] public bool Print { get; set; }

    public FinalResultRecord? Result { get; private set; }

    public bool IsPositive =>
        string.Equals(Result?.FinalResult, "Pos", StringComparison.OrdinalIgnoreCase);

    /// <summary>Legacy showed the last 8 characters of the formatted RBSE, i.e. YY/NNNNN.</summary>
    public string CaseRef
    {
        get
        {
            var formatted = RbseHelper.Format(Result?.Rbse) ?? string.Empty;
            return formatted.Length >= 8 ? formatted[^8..] : formatted;
        }
    }

    public bool HasAlternateDiagnosis => !string.IsNullOrWhiteSpace(Result?.AlternateDiagnosis);

    public async Task<IActionResult> OnGetAsync()
    {
        var rbse = RbseHelper.ParseToRaw(Rbse);
        if (rbse.Length == 0)
        {
            return RedirectToPage("/Case/FinalResultEntry");
        }

        try
        {
            Result = await cases.GetFinalResultAsync(rbse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load case details for the result memo");
            return RedirectToPage("/Case/FinalResultEntry", new { rbse });
        }

        if (Result is null)
        {
            return RedirectToPage("/Case/FinalResultEntry", new { rbse });
        }

        if (!Print)
        {
            // rbse is digit-only after normalisation, so it is safe in the header value.
            Response.ContentType = "application/vnd.ms-word";
            Response.Headers.ContentDisposition = $"attachment; filename=\"memo{rbse}.doc\"";
        }

        return Page();
    }
}
