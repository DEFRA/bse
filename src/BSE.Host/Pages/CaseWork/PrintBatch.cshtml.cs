using System.Text;
using System.Text.Encodings.Web;
using BSE.Modules.Batch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAAccess")]
public class PrintBatchModel(IBatchService batchService) : PageModel
{
    public enum ReportType
    {
        None = 0,
        Clinical = 1,
        FarmAndCase = 2,
        Feeds = 3,
        Offspring = 4,
        Pedigree = 5
    }

    [BindProperty] public short? BatchYear { get; set; }
    [BindProperty] public int? BatchNumber { get; set; }
    [BindProperty] public ReportType SelectedReportType { get; set; } = ReportType.None;

    public async Task<IActionResult> OnPostDownloadAsync()
    {
        if (BatchYear is null) ModelState.AddModelError(nameof(BatchYear), "Enter batch year.");
        if (BatchNumber is null) ModelState.AddModelError(nameof(BatchNumber), "Enter batch number.");
        if (SelectedReportType == ReportType.None) ModelState.AddModelError(nameof(SelectedReportType), "Select report type.");
        if (!ModelState.IsValid) return Page();

        var batchId = await batchService.GetBatchIdAsync(BatchYear.Value, BatchNumber.Value);
        if (batchId is null)
        {
            ModelState.AddModelError(nameof(BatchNumber), $"Batch {BatchYear}/{BatchNumber} was not found.");
            return Page();
        }

        var batchLabel = $"{BatchYear}/{BatchNumber}";
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
        sb.AppendLine("<style>body{font-family:Arial,Helvetica,sans-serif;color:black;} table{border-collapse:collapse;} th,td{padding:4px;text-align:left;vertical-align:top;}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>{HtmlEncoder.Default.Encode(title)}</h1>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Batch No.</th><th>Date Run</th><th>Number In Batch</th></tr>");
        sb.AppendLine($"<tr><td>{HtmlEncoder.Default.Encode(batchLabel)}</td><td>{DateTime.Now:dd/MM/yyyy HH:mm}</td><td>{rows.Count}</td></tr>");
        sb.AppendLine("</table><hr/>");

        if (rows.Count == 0)
        {
            sb.AppendLine("<p>No records found for this batch/report type.</p>");
        }
        else
        {
            // Generic tabular renderer using returned SP columns.
            var columns = rows[0].Keys.ToList();
            sb.AppendLine("<table border=\"1\">");
            sb.AppendLine("<tr>" + string.Join("", columns.Select(c => $"<th>{HtmlEncoder.Default.Encode(c)}</th>")) + "</tr>");
            foreach (var row in rows)
            {
                sb.AppendLine("<tr>");
                foreach (var c in columns)
                {
                    row.TryGetValue(c, out var value);
                    sb.Append($"<td>{HtmlEncoder.Default.Encode(value?.ToString() ?? string.Empty)}</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}