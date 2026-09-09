using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class MinuteModel(ICaseWorkService caseWorkService) : PageModel
{
    private const string SessionHomebred = "CaseWork.OutstandingForms.Homebred";
    private const string SessionBreeder = "CaseWork.OutstandingForms.Breeder";
    private const string SessionPurchaser = "CaseWork.OutstandingForms.Purchaser";
    private const string SessionVendor = "CaseWork.OutstandingForms.Vendor";
    private const string SessionSummarySheet = "CaseWork.OutstandingForms.SummarySheet";
    private const string SessionAllPaperwork = "CaseWork.OutstandingForms.AllPaperwork";

    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ActiveMemo"] = "Active Memo",
        ["AMFS"] = "Active Memo (Fallen Stock)",
        ["AnnexA"] = "Annex A",
        ["AnnexB"] = "Annex B",
        ["AnnexC"] = "Annex C",
        ["AnnexD"] = "Annex D"
    };

    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string Type { get; set; } = string.Empty;

    [BindProperty] public bool OutstandingHomebred { get; set; }
    [BindProperty] public bool OutstandingBreeder { get; set; }
    [BindProperty] public bool OutstandingPurchaser { get; set; }
    [BindProperty] public bool OutstandingVendor { get; set; }
    [BindProperty] public bool OutstandingSummarySheet { get; set; }
    [BindProperty] public bool OutstandingAllPaperwork { get; set; }

    public string MinuteTypeLabel => Labels.TryGetValue(Type, out var label) ? label : Type;
    public MinuteDetailsRecord? Details { get; private set; }
    public DateTime? MinuteDate { get; private set; }

    public bool ShowOutstandingForms => IsAnnexCorD(NormalizeMinuteType(EffectiveMinuteType));
    public bool TriggerPrintPopup { get; private set; }
    public string? PrintPopupUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var minuteType = NormalizeMinuteType(EffectiveMinuteType);
        if (!IsSupportedMinuteType(minuteType)) return BadRequest();

        Type = minuteType;
        Details = await caseWorkService.GetMinuteDetailsAsync(Rbse, minuteType);
        MinuteDate = Details is null ? null : GetMinuteDate(Details, minuteType);

        if (ShowOutstandingForms)
            LoadOutstandingSelectionFromSession();

        return Page();
    }

    public async Task<IActionResult> OnPostDownloadAsync()
    {
        var minuteType = NormalizeMinuteType(EffectiveMinuteType);
        if (!IsSupportedMinuteType(minuteType)) return BadRequest();

        Type = minuteType;
        Details = await caseWorkService.GetMinuteDetailsAsync(Rbse, minuteType);
        if (Details is null) return NotFound();

        MinuteDate = GetMinuteDate(Details, minuteType);

        if (!ValidateOutstandingFormsIfRequired(minuteType))
            return Page();

        SaveOutstandingSelectionToSession();

        // Legacy parity: redirect to minute template endpoint for Word output.
        return RedirectToPage("/CaseWork/MinuteDocument", new { rbse = Rbse, type = minuteType });
    }

    public async Task<IActionResult> OnPostPrintMemoAsync()
    {
        var minuteType = NormalizeMinuteType(EffectiveMinuteType);
        if (!IsSupportedMinuteType(minuteType)) return BadRequest();

        Type = minuteType;
        Details = await caseWorkService.GetMinuteDetailsAsync(Rbse, minuteType);
        if (Details is null) return NotFound();

        MinuteDate = GetMinuteDate(Details, minuteType);

        if (!ValidateOutstandingFormsIfRequired(minuteType))
            return Page();

        SaveOutstandingSelectionToSession();

        // Legacy parity: popup window to document page with print mode.
        TriggerPrintPopup = true;
        PrintPopupUrl = Url.Page("/CaseWork/MinuteDocument", new { rbse = Rbse, type = minuteType, print = "Print" });

        return Page();
    }

    private bool ValidateOutstandingFormsIfRequired(string minuteType)
    {
        if (!IsAnnexCorD(minuteType)) return true;

        var anySelected =
            OutstandingHomebred || OutstandingBreeder || OutstandingPurchaser ||
            OutstandingVendor || OutstandingSummarySheet || OutstandingAllPaperwork;

        if (anySelected) return true;

        ModelState.AddModelError("OutstandingForms", "Select at least one outstanding form.");
        return false;
    }

    private void SaveOutstandingSelectionToSession()
    {
        HttpContext.Session.SetString(SessionHomebred, OutstandingHomebred.ToString());
        HttpContext.Session.SetString(SessionBreeder, OutstandingBreeder.ToString());
        HttpContext.Session.SetString(SessionPurchaser, OutstandingPurchaser.ToString());
        HttpContext.Session.SetString(SessionVendor, OutstandingVendor.ToString());
        HttpContext.Session.SetString(SessionSummarySheet, OutstandingSummarySheet.ToString());
        HttpContext.Session.SetString(SessionAllPaperwork, OutstandingAllPaperwork.ToString());
    }

    private void LoadOutstandingSelectionFromSession()
    {
        OutstandingHomebred = bool.TryParse(HttpContext.Session.GetString(SessionHomebred), out var hb) && hb;
        OutstandingBreeder = bool.TryParse(HttpContext.Session.GetString(SessionBreeder), out var br) && br;
        OutstandingPurchaser = bool.TryParse(HttpContext.Session.GetString(SessionPurchaser), out var pu) && pu;
        OutstandingVendor = bool.TryParse(HttpContext.Session.GetString(SessionVendor), out var ve) && ve;
        OutstandingSummarySheet = bool.TryParse(HttpContext.Session.GetString(SessionSummarySheet), out var ss) && ss;
        OutstandingAllPaperwork = bool.TryParse(HttpContext.Session.GetString(SessionAllPaperwork), out var ap) && ap;
    }

    private string EffectiveMinuteType
    {
        get
        {
            var fromRoute = Type?.Trim();
            if (!string.IsNullOrWhiteSpace(fromRoute))
                return fromRoute;

            return Request.Query["minute"].ToString().Trim(); // legacy query-string compatibility
        }
    }

    private static string NormalizeMinuteType(string minuteType)
    {
        var m = (minuteType ?? string.Empty).Trim();
        return m switch
        {
            "ActiveMemoFS" => "AMFS",
            "Annex C" => "AnnexC",
            "Annex D" => "AnnexD",
            _ => m
        };
    }

    private static bool IsSupportedMinuteType(string minuteType) =>
        minuteType is "ActiveMemo" or "AMFS" or "AnnexA" or "AnnexB" or "AnnexC" or "AnnexD";

    private static bool IsAnnexCorD(string minuteType) =>
        string.Equals(minuteType, "AnnexC", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(minuteType, "AnnexD", StringComparison.OrdinalIgnoreCase);

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
