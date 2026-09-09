using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class MinuteDocumentModel(ICaseWorkService caseWorkService) : PageModel
{
    private const string SessionHomebred = "CaseWork.OutstandingForms.Homebred";
    private const string SessionBreeder = "CaseWork.OutstandingForms.Breeder";
    private const string SessionPurchaser = "CaseWork.OutstandingForms.Purchaser";
    private const string SessionVendor = "CaseWork.OutstandingForms.Vendor";
    private const string SessionSummarySheet = "CaseWork.OutstandingForms.SummarySheet";
    private const string SessionAllPaperwork = "CaseWork.OutstandingForms.AllPaperwork";

    public string Rbse { get; private set; } = string.Empty;
    public string MinuteType { get; private set; } = string.Empty;
    public bool IsPrintMode { get; private set; }
    public MinuteDetailsRecord Details { get; private set; } = default!;
    public DateTime? MinuteDate { get; private set; }
    public IReadOnlyList<string> OutstandingForms { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string rbse, string type, string? print)
    {
        var minuteType = NormalizeMinuteType(type);
        if (!IsSupportedMinuteType(minuteType)) return BadRequest();

        var details = await caseWorkService.GetMinuteDetailsAsync(rbse, minuteType);
        if (details is null) return NotFound();

        Rbse = rbse;
        MinuteType = minuteType;
        IsPrintMode = string.Equals(print, "Print", StringComparison.OrdinalIgnoreCase);
        Details = details;
        MinuteDate = GetMinuteDate(details, minuteType);

        if (minuteType is "AnnexC" or "AnnexD")
        {
            OutstandingForms = ReadOutstandingForms();
            if (OutstandingForms.Count == 0)
                return BadRequest("At least one outstanding form must be selected for Annex C or Annex D.");
        }

        if (!IsPrintMode)
        {
            Response.ContentType = "application/vnd.ms-word";
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{GetLegacyFileName(minuteType, rbse)}\"";
        }

        if (minuteType is "AnnexC" or "AnnexD")
            ClearOutstandingForms();

        return Page();
    }

    private IReadOnlyList<string> ReadOutstandingForms()
    {
        bool B(string key) => bool.TryParse(HttpContext.Session.GetString(key), out var v) && v;
        var result = new List<string>();
        if (B(SessionHomebred)) result.Add("BSE 1 - Homebred");
        if (B(SessionBreeder)) result.Add("BSE 1 - Breeder");
        if (B(SessionPurchaser)) result.Add("BSE 1 - Purchaser");
        if (B(SessionVendor)) result.Add("BSE 1 - Vendor");
        if (B(SessionSummarySheet)) result.Add("Summary Sheet");
        if (B(SessionAllPaperwork)) result.Add("All Paperwork");
        return result;
    }

    private void ClearOutstandingForms()
    {
        HttpContext.Session.Remove(SessionHomebred);
        HttpContext.Session.Remove(SessionBreeder);
        HttpContext.Session.Remove(SessionPurchaser);
        HttpContext.Session.Remove(SessionVendor);
        HttpContext.Session.Remove(SessionSummarySheet);
        HttpContext.Session.Remove(SessionAllPaperwork);
    }

    private static string NormalizeMinuteType(string t) => t switch
    {
        "ActiveMemoFS" => "AMFS",
        "Annex C" => "AnnexC",
        "Annex D" => "AnnexD",
        _ => t
    };

    private static bool IsSupportedMinuteType(string t) =>
        t is "ActiveMemo" or "AMFS" or "AnnexA" or "AnnexB" or "AnnexC" or "AnnexD";

    private static string GetLegacyFileName(string minuteType, string rbse)
    {
        var n = rbse.Replace("/", "", StringComparison.Ordinal);
        return minuteType switch
        {
            "ActiveMemo" or "AMFS" => $"activememo{n}.doc",
            "AnnexA" => $"annexa{n}.doc",
            "AnnexB" => $"annexb{n}.doc",
            "AnnexC" => $"annexc{n}.doc",
            "AnnexD" => $"annexd{n}.doc",
            _ => $"minute{n}.doc"
        };
    }

    private static DateTime? GetMinuteDate(MinuteDetailsRecord d, string type) => type switch
    {
        "ActiveMemo" or "AMFS" => d.ActiveMemoDate,
        "AnnexA" => d.AnnexADate,
        "AnnexB" => d.AnnexBDate,
        "AnnexC" => d.AnnexCDate,
        "AnnexD" => d.AnnexDDate,
        _ => null
    };
}
