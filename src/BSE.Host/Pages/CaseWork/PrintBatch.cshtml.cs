using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using BSE.Modules.Batch.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAAccess")]
public class PrintBatchModel(IBatchService batchService) : PageModel
{
    // Matches legacy BatchNumber.ascx: 4-digit year, up to 6-digit numeric serial.
    private static readonly Regex YearPattern = new(@"^\d{4}$", RegexOptions.Compiled);
    private static readonly Regex NumberPattern = new(@"^\d{1,6}$", RegexOptions.Compiled);

    // DateTime.Now follows the host OS's local timezone, which defaults to UTC on typical
    // Linux/container deployments and would show "Date Run" an hour behind the real UK
    // wall-clock time during BST. Convert explicitly instead of relying on host configuration.
    private static readonly TimeZoneInfo UkTimeZone = ResolveUkTimeZone();

    private static TimeZoneInfo ResolveUkTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); } // Windows ID
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); // Linux/IANA ID
        }
    }

    public enum ReportType
    {
        None = 0,
        Clinical = 1,
        FarmAndCase = 2,
        Feeds = 3,
        Offspring = 4,
        Pedigree = 5
    }

    [BindProperty] public string? BatchYear { get; set; }
    [BindProperty] public string? BatchNumber { get; set; }
    [BindProperty] public ReportType SelectedReportType { get; set; } = ReportType.None;

    public async Task<IActionResult> OnPostDownloadAsync()
    {
        var yearText = BatchYear?.Trim();
        var numberText = BatchNumber?.Trim();

        if (string.IsNullOrEmpty(numberText))
        {
            ModelState.AddModelError(nameof(BatchYear), "Enter batch number.");
        }
        else
        {
            // Legacy allowed the year to be left blank to look up batches recorded
            // before the year field existed (stored with a NULL BatchYear).
            if (!string.IsNullOrEmpty(yearText) && !YearPattern.IsMatch(yearText))
                ModelState.AddModelError(nameof(BatchYear), "Enter a four digit year");
            if (!NumberPattern.IsMatch(numberText))
                ModelState.AddModelError(nameof(BatchNumber), "Enter a valid batch number");
        }

        if (SelectedReportType == ReportType.None) ModelState.AddModelError(nameof(SelectedReportType), "Select report type.");
        if (!ModelState.IsValid) return Page();

        short? batchYear = string.IsNullOrEmpty(yearText) ? null : short.Parse(yearText);
        var batchNumber = int.Parse(numberText!);

        var batchId = await batchService.GetBatchIdAsync(batchYear, batchNumber);
        if (batchId is null)
        {
            ModelState.AddModelError(nameof(BatchNumber), $"Batch {FormatBatchLabel(batchYear, batchNumber)} was not found.");
            return Page();
        }

        var batchLabel = FormatBatchLabel(batchYear, batchNumber);
        var html = await BuildLegacyStyleHtmlAsync(batchId.Value, batchLabel, SelectedReportType);
        var fileName = SelectedReportType switch
        {
            ReportType.Clinical => "ClinicalReport.doc",
            ReportType.FarmAndCase => "FarmCaseReport.doc",
            ReportType.Feeds => "FeedsReport.doc",
            ReportType.Offspring => "OffspringReport.doc",
            ReportType.Pedigree => "PedigreeReport.doc",
            _ => "BatchReport.doc"
        };

        return File(
            Encoding.UTF8.GetBytes(html),
            "application/vnd.ms-word",
            fileName);
    }

    private static string FormatBatchLabel(short? batchYear, int batchNumber) =>
        batchYear.HasValue ? $"{batchYear}/{batchNumber}" : batchNumber.ToString();

    private async Task<string> BuildLegacyStyleHtmlAsync(int batchId, string batchLabel, ReportType reportType)
    {
        var title = reportType switch
        {
            ReportType.Clinical => "Clinical Report",
            ReportType.FarmAndCase => "Case And Farm Report",
            ReportType.Feeds => "Feeds Report",
            ReportType.Offspring => "Offspring Report",
            ReportType.Pedigree => "Pedigree Report",
            _ => "Batch Report"
        };

        var mainSp = reportType switch
        {
            ReportType.Clinical => "GetClinicalByBatchID",
            ReportType.FarmAndCase => "GetCaseFarmByBatchID",
            ReportType.Feeds => "GetFeedsByBatchID",
            ReportType.Offspring => "GetRelationsByBatchID",
            ReportType.Pedigree => "GetDamSireDetailsByBatchID",
            _ => "GetCPHHRBSEForBatchID"
        };

        var rows = await batchService.GetReportRowsAsync(mainSp, batchId);

        var sb = new StringBuilder();
        sb.AppendLine("<html><head><meta charset=\"utf-8\" />");
        // Word's HTML renderer needs the mso-page-orientation/Section pattern (not plain
        // @page{size:landscape}) to keep a wide report within the printable page frame
        // instead of running off the edge; word-wrap keeps long cell content wrapping
        // inside its column rather than overflowing.
        sb.AppendLine("<style>");
        sb.AppendLine("@page Section1 { size: 29.7cm 21cm; mso-page-orientation: landscape; margin: 1.5cm; }");
        sb.AppendLine("div.Section1 { page: Section1; }");
        sb.AppendLine("body { font-family: Arial, Helvetica, sans-serif; color: black; }");
        sb.AppendLine("table { border-collapse: collapse; }");
        sb.AppendLine(".report-table, .report-table-small { table-layout: fixed; width: 100%; margin-bottom: 6px; }");
        sb.AppendLine(".report-table th, .report-table td { padding: 4px 8px; text-align: left; vertical-align: top; font-size: 90%; word-wrap: break-word; overflow-wrap: break-word; }");
        sb.AppendLine(".report-table-small th, .report-table-small td { padding: 2px 4px; text-align: left; vertical-align: top; font-size: 70%; word-wrap: break-word; overflow-wrap: break-word; }");
        sb.AppendLine("th { font-weight: bold; }");
        sb.AppendLine(".case-block { margin-bottom: 10px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body><div class=\"Section1\">");
        sb.AppendLine($"<h1>{HtmlEncoder.Default.Encode(title)}</h1>");
        sb.AppendLine("<table class=\"report-table\">");
        sb.AppendLine("<tr><th>Batch No.</th><th>Date Run</th><th>Number In Batch</th></tr>");
        sb.AppendLine($"<tr><td>{HtmlEncoder.Default.Encode(batchLabel)}</td><td>{TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UkTimeZone):dd/MM/yyyy HH:mm}</td><td>{rows.Count}</td></tr>");
        sb.AppendLine("</table><hr/>");

        if (rows.Count == 0)
        {
            sb.AppendLine("<p>No records found for this batch/report type.</p>");
        }
        else
        {
            switch (reportType)
            {
                case ReportType.Clinical: AppendClinicalReport(sb, rows); break;
                case ReportType.FarmAndCase: AppendCaseFarmReport(sb, rows); break;
                case ReportType.Feeds: AppendFeedsReport(sb, rows); break;
                case ReportType.Offspring: AppendOffspringReport(sb, rows); break;
                case ReportType.Pedigree: AppendPedigreeReport(sb, rows); break;
                default: AppendGenericReport(sb, rows); break;
            }
        }

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static string E(object? value) => HtmlEncoder.Default.Encode(value?.ToString() ?? string.Empty);

    private static string V(IDictionary<string, object?> row, string column) =>
        row.TryGetValue(column, out var value) ? E(value) : string.Empty;

    /// <summary>Legacy's CPHH formula: LEFT(x,2)/SUBSTRING(x,3,3)/SUBSTRING(x,6,4)/RIGHT(x,2).</summary>
    private static string FormatCphh(IDictionary<string, object?> row, string column)
    {
        var raw = row.TryGetValue(column, out var value) ? value?.ToString() : null;
        if (string.IsNullOrEmpty(raw) || raw.Length < 11) return E(raw);
        return E($"{raw[..2]}/{raw.Substring(2, 3)}/{raw.Substring(5, 4)}/{raw[^2..]}");
    }

    // Legacy's Clinical report: one grouped symptom grid per case, matching ReportClinical.aspx.
    private static void AppendClinicalReport(StringBuilder sb, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        foreach (var row in rows)
        {
            sb.AppendLine("<div class=\"case-block\">");
            sb.AppendLine("<table class=\"report-table\"><tr>" +
                $"<th>Case - {V(row, "DisplayRBSE")}</th><th>CPHH - {V(row, "DisplayCPHH")}</th></tr></table>");
            sb.AppendLine("<table class=\"report-table-small\">");
            AppendSymptomRow(sb, row, "APPR", "Apprehension", "ABNO", "AbnormalBehaviour");
            AppendSymptomRow(sb, row, "TOUCH", "HypersensitiveTouch", "SHY", "HeadShyness");
            AppendSymptomRow(sb, row, "SOUND", "HypersensitiveSound", "FLANK", "LickingFlank");
            AppendSymptomRow(sb, row, "MANIC", "Maniacal", "NOSE", "LickingNose");
            AppendSymptomRow(sb, row, "PANIC", "PanicStricken", "KICK", "Kicking");
            AppendSymptomRow(sb, row, "TEMP", "TemperamentChange", "RELUC", "ReluctantDoorways");
            AppendSymptomRow(sb, row, "ABNOR", "AbnormalHeadCarriage", "PRESS", "HeadPressing");
            AppendSymptomRow(sb, row, "TWITCH", "EarTwitching", "RUB", "HeadRubbing");
            AppendSymptomRow(sb, row, "ANGLE", "EarsOddAngle", "TEETH", "TeethGrinding");
            AppendSymptomRow(sb, row, "BLIND", "Blindness", "FALL", "Falling", "REC", "Recumbent");
            AppendSymptomRow(sb, row, "CIRCLE", "Circling", "PAR", "Paresis", "TREM", "Tremor");
            AppendSymptomRow(sb, row, "H ATAX", "HindAtaxia", "F ATX", "ForeAtaxia", "KNUCK", "KnucklingFetlock");
            AppendSymptomRow(sb, row, "WT", "WeightLoss", "COND", "ConditionLoss", "MILK", "MilkYield");
            sb.AppendLine("</table></div><hr/>");
        }
    }

    private static void AppendSymptomRow(StringBuilder sb, IDictionary<string, object?> row, params string[] labelColumnPairs)
    {
        sb.Append("<tr>");
        for (var i = 0; i < labelColumnPairs.Length; i += 2)
        {
            sb.Append($"<td>{E(labelColumnPairs[i])}</td><td>{V(row, labelColumnPairs[i + 1])}</td>");
        }
        sb.AppendLine("</tr>");
    }

    // Legacy's Case And Farm report: case/farm summary + onset details for each case.
    // The legacy report also showed nested Previous Owner history and Herd lactation
    // distribution child tables; those come from separate data relations that GetCaseFarmByBatchID
    // does not currently return, so they're omitted here rather than faked.
    private static void AppendCaseFarmReport(StringBuilder sb, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        foreach (var row in rows)
        {
            sb.AppendLine("<div class=\"case-block\">");
            sb.AppendLine("<table class=\"report-table\">");
            sb.AppendLine("<tr><th>RBSE</th><th>EARTAG</th><th>CPHH</th><th>OWNER</th><th>Address</th></tr>");
            sb.AppendLine("<tr>" +
                $"<td>{V(row, "DisplayRBSE")}</td>" +
                $"<td>{V(row, "EartagHerdMark")} {V(row, "EarTag")}</td>" +
                $"<td>{V(row, "DisplayCPHH")}</td>" +
                $"<td>{V(row, "Ownername")}</td>" +
                $"<td>{V(row, "Address1")}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("<table class=\"report-table-small\">");
            sb.AppendLine("<tr><th>BIRTH</th><th>BAB</th><th>AGE</th><th>SEX</th><th>BREED</th><th>HB/P</th><th>DATE P</th><th>AGE P</th></tr>");
            sb.AppendLine("<tr>" +
                $"<td>{V(row, "BirthDate")}</td><td>{V(row, "IsBAB")}</td><td>{V(row, "OnsetAgeInMonths")}</td>" +
                $"<td>{V(row, "Sex")}</td><td>{V(row, "Breed")}</td><td>{V(row, "Origin")}</td>" +
                $"<td>{V(row, "PurchaseDate")}</td><td>{V(row, "PurchaseAgeInMonths")}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("<table class=\"report-table-small\">");
            sb.AppendLine("<tr><th>ENTRY</th><th>ONSET</th><th>PREG</th><th>MONTHS POST CALVING</th></tr>");
            sb.AppendLine("<tr>" +
                $"<td>{V(row, "HerdEntryDate")}</td><td>{V(row, "OnsetDate")}</td>" +
                $"<td>{V(row, "MonthsPregnant")}</td><td>{V(row, "MonthsPostCalving")}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("<table class=\"report-table-small\">");
            sb.AppendLine("<tr><th>H TYPE</th></tr>");
            sb.AppendLine($"<tr><td>{V(row, "HerdType")} {V(row, "PedigreeType")}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("</div><hr/>");
        }
    }

    // Legacy's Feeds report: cases grouped with a Ration history table per case.
    private static void AppendFeedsReport(StringBuilder sb, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        foreach (var group in rows.GroupBy(r => r.TryGetValue("RBSE", out var v) ? v?.ToString() ?? "" : ""))
        {
            var first = group.First();
            sb.AppendLine("<div class=\"case-block\">");
            sb.AppendLine("<table class=\"report-table\"><tr>" +
                $"<th>Case - {E(RbseHelper.Format(group.Key))}</th><th>CPHH - {FormatCphh(first, "CPHH")}</th></tr></table>");
            sb.AppendLine("<table class=\"report-table-small\">");
            sb.AppendLine("<tr><th>Year From</th><th>Year To</th><th>Ration Type</th><th>Supplier Name</th><th>Ration Name</th><th>Is PrePurchase</th></tr>");
            foreach (var row in group)
            {
                sb.AppendLine("<tr>" +
                    $"<td>{V(row, "YearFrom")}</td><td>{V(row, "YearTo")}</td><td>{V(row, "RationType")}</td>" +
                    $"<td>{V(row, "Name")}</td><td>{V(row, "RationName")}</td><td>{V(row, "IsPrePurchase")}</td></tr>");
            }
            sb.AppendLine("</table></div><hr/>");
        }
    }

    // Legacy's Offspring report: cases grouped with a relation history table per case.
    // GetRelationsByBatchID does not return CPHH, so only the RBSE header is shown.
    private static void AppendOffspringReport(StringBuilder sb, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        foreach (var group in rows.GroupBy(r => r.TryGetValue("RBSE", out var v) ? v?.ToString() ?? "" : ""))
        {
            sb.AppendLine("<div class=\"case-block\">");
            sb.AppendLine($"<table class=\"report-table\"><tr><th>Case - {E(RbseHelper.Format(group.Key))}</th></tr></table>");
            sb.AppendLine("<table class=\"report-table-small\">");
            sb.AppendLine("<tr><th>BIRTH</th><th>SEX</th><th>RELATION TYPE</th><th>EARTAG</th><th>FATE</th><th>DATE LEFT</th><th>RBSE OF RELATION</th><th>SIRE</th></tr>");
            foreach (var row in group)
            {
                sb.AppendLine("<tr>" +
                    $"<td>{V(row, "BirthDate")}</td><td>{V(row, "SexDesc")}</td><td>{V(row, "RelationTypeDesc")}</td>" +
                    $"<td>{V(row, "EartagHerdmark")} {V(row, "Eartag")}</td><td>{V(row, "RelationFateDesc")}</td>" +
                    $"<td>{V(row, "LeftDate")}</td><td>{E(RbseHelper.Format(row.TryGetValue("RelationRBSE", out var rv) ? rv?.ToString() : null))}</td><td>{V(row, "Sire")}</td></tr>");
            }
            sb.AppendLine("</table></div><hr/>");
        }
    }

    // Legacy's Pedigree report: one Sire/Dam detail block per case.
    private static void AppendPedigreeReport(StringBuilder sb, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        foreach (var row in rows)
        {
            sb.AppendLine("<div class=\"case-block\">");
            sb.AppendLine("<table class=\"report-table\"><tr>" +
                $"<th>Case - {V(row, "DisplayRBSE")}</th><th>CPHH - {V(row, "DisplayCPHH")}</th></tr></table>");
            sb.AppendLine("<table class=\"report-table-small\">");
            sb.AppendLine("<tr><th colspan=\"8\">Sire Detail:</th></tr>");
            sb.AppendLine("<tr><th>ID</th><th>RBSE</th><th>EARTAG</th><th>NAME</th><th>DATE OF BIRTH</th><th>FATE</th><th>HERD BOOK</th><th>ALT NAME</th></tr>");
            sb.AppendLine("<tr>" +
                $"<td>{V(row, "SireID")}</td><td>{V(row, "SireDisplayRBSE")}</td><td>{V(row, "SireEartag")}</td>" +
                $"<td>{V(row, "SireName")}</td><td>{V(row, "SireBirthDate")}</td><td>{V(row, "SireFate")}</td>" +
                $"<td>{V(row, "SireHerdbook")}</td><td>{V(row, "SireAlternativeName")}</td></tr>");
            sb.AppendLine("<tr><th colspan=\"9\">Dam Detail:</th></tr>");
            sb.AppendLine("<tr><th>ID</th><th>RBSE</th><th>EARTAG</th><th>NAME</th><th>DATE OF BIRTH</th><th>FATE</th><th>HERD BOOK</th><th>ALT NAME</th><th>STATUS</th></tr>");
            sb.AppendLine("<tr>" +
                $"<td>{V(row, "DamID")}</td><td>{V(row, "DamDisplayRBSE")}</td><td>{V(row, "DamEartag")}</td>" +
                $"<td>{V(row, "DamName")}</td><td>{V(row, "DamBirthDate")}</td><td>{V(row, "DamFate")}</td>" +
                $"<td>{V(row, "DamHerdbook")}</td><td>{V(row, "DamAlternativeName")}</td><td>{V(row, "DamStatus")}</td></tr>");
            sb.AppendLine("</table></div><hr/>");
        }
    }

    // Fallback for any batch report type without a bespoke layout (e.g. the default CPHH/RBSE list).
    private static void AppendGenericReport(StringBuilder sb, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        var columns = rows[0].Keys.ToList();
        sb.AppendLine("<table class=\"report-table\" style=\"table-layout:fixed\">");
        sb.AppendLine("<tr>" + string.Join("", columns.Select(c => $"<th>{E(c)}</th>")) + "</tr>");
        foreach (var row in rows)
        {
            sb.AppendLine("<tr>" + string.Join("", columns.Select(c => $"<td>{V(row, c)}</td>")) + "</tr>");
        }
        sb.AppendLine("</table>");
    }
}