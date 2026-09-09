using System.Text;
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

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Type { get; set; } = string.Empty;

    // Legacy-only requirement for AnnexC/AnnexD:
    [BindProperty] public bool OutstandingHomebred { get; set; }
    [BindProperty] public bool OutstandingBreeder { get; set; }
    [BindProperty] public bool OutstandingPurchaser { get; set; }
    [BindProperty] public bool OutstandingVendor { get; set; }
    [BindProperty] public bool OutstandingSummarySheet { get; set; }
    [BindProperty] public bool OutstandingAllPaperwork { get; set; }

    public string MinuteTypeLabel => Labels.TryGetValue(Type, out var label) ? label : Type;
    public MinuteDetailsRecord? Details { get; private set; }
    public DateTime? MinuteDate { get; private set; }
    public bool TriggerPrint { get; private set; }
    public bool ShowOutstandingForms => IsAnnexCorD(EffectiveMinuteType);

    private static bool IsAnnexCorD(string minuteType) =>
        string.Equals(minuteType, "AnnexC", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(minuteType, "AnnexD", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(minuteType, "Annex C", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(minuteType, "Annex D", StringComparison.OrdinalIgnoreCase);

    // Update OnGetAsync / post handlers to use effective type
    public async Task<IActionResult> OnGetAsync()
    {
        var minuteType = EffectiveMinuteType;
        if (!IsSupportedMinuteType(minuteType)) return BadRequest();

        Type = minuteType; // keep bound value consistent for postback
        Details = await caseWorkService.GetMinuteDetailsAsync(Rbse, minuteType);
        MinuteDate = Details is null ? null : GetMinuteDate(Details, minuteType);

        if (ShowOutstandingForms)
            LoadOutstandingSelectionFromSession();

        return Page();
    }

    public async Task<IActionResult> OnPostDownloadAsync()
    {
        if (!IsSupportedMinuteType(Type)) return BadRequest();

        Details = await caseWorkService.GetMinuteDetailsAsync(Rbse, Type);
        if (Details is null) return NotFound();

        MinuteDate = GetMinuteDate(Details, Type);

        if (!ValidateOutstandingFormsIfRequired())
            return Page();

        SaveOutstandingSelectionToSession();

        var date = MinuteDate?.ToString("dd/MM/yyyy") ?? "-";
        var content = BuildDownloadContent(date);

        var fileName = $"{Type}-{Rbse}-{DateTime.UtcNow:yyyyMMdd}.txt";
        return File(Encoding.UTF8.GetBytes(content), "text/plain", fileName);
    }

    public async Task<IActionResult> OnPostPrintMemoAsync()
    {
        if (!IsSupportedMinuteType(Type)) return BadRequest();

        Details = await caseWorkService.GetMinuteDetailsAsync(Rbse, Type);
        if (Details is null) return NotFound();

        MinuteDate = GetMinuteDate(Details, Type);

        if (!ValidateOutstandingFormsIfRequired())
            return Page();

        SaveOutstandingSelectionToSession();

        TriggerPrint = true;
        return Page();
    }

    private string BuildDownloadContent(string date)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BSE Database System");
        sb.AppendLine("Casework Minute Confirmation");
        sb.AppendLine();
        sb.AppendLine($"RBSE: {Rbse}");
        sb.AppendLine($"Minute: {MinuteTypeLabel}");
        sb.AppendLine($"Date: {date}");

        if (ShowOutstandingForms)
        {
            sb.AppendLine();
            sb.AppendLine("Outstanding Forms:");
            if (OutstandingHomebred) sb.AppendLine("- Homebred");
            if (OutstandingBreeder) sb.AppendLine("- Breeder");
            if (OutstandingPurchaser) sb.AppendLine("- Purchaser");
            if (OutstandingVendor) sb.AppendLine("- Vendor");
            if (OutstandingSummarySheet) sb.AppendLine("- Summary Sheet");
            if (OutstandingAllPaperwork) sb.AppendLine("- All Paperwork");
        }

        return sb.ToString();
    }

    private bool ValidateOutstandingFormsIfRequired()
    {
        if (!ShowOutstandingForms) return true;

        var anySelected =
            OutstandingHomebred ||
            OutstandingBreeder ||
            OutstandingPurchaser ||
            OutstandingVendor ||
            OutstandingSummarySheet ||
            OutstandingAllPaperwork;

        if (anySelected) return true;

        ModelState.AddModelError("OutstandingForms", "For Annex C or Annex D, select at least one outstanding form.");
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

    private static bool IsSupportedMinuteType(string minuteType) =>
        minuteType is "ActiveMemo" or "AMFS" or "AnnexA" or "AnnexB" or "AnnexC" or "AnnexD";

    private static DateTime? GetMinuteDate(MinuteDetailsRecord d, string type) => type switch
    {
        "ActiveMemo" or "AMFS" => d.ActiveMemoDate,
        "AnnexA" => d.AnnexADate,
        "AnnexB" => d.AnnexBDate,
        "AnnexC" => d.AnnexCDate,
        "AnnexD" => d.AnnexDDate,
        _ => null
    };
    // Add inside MinuteModel
    private string EffectiveMinuteType
    {
        get
        {
            var fromRoute = Type?.Trim();
            if (!string.IsNullOrWhiteSpace(fromRoute))
                return fromRoute;

            // Legacy-style fallback: ?minute=AnnexC
            return Request.Query["minute"].ToString().Trim();
        }
    }

}