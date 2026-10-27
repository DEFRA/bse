using System.Text;
using BSE.Host.Pages.CaseWork;
using BSE.Modules.Batch.Services;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Tests;

public class PrintBatchModelTests
{
    private readonly IBatchService _batches = Substitute.For<IBatchService>();

    private PrintBatchModel CreateModel(string? year, string? number, PrintBatchModel.ReportType type) =>
        new(_batches) { BatchYear = year, BatchNumber = number, SelectedReportType = type };

    private void Rows(string sp, params Dictionary<string, object?>[] rows)
    {
        _batches.GetBatchIdAsync(Arg.Any<short?>(), Arg.Any<int>()).Returns(Task.FromResult<int?>(9));
        _batches.GetReportRowsAsync(sp, 9)
            .Returns(Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(rows.Cast<IDictionary<string, object?>>().ToList()));
    }

    private static string Html(IActionResult result) =>
        Encoding.UTF8.GetString(result.Should().BeOfType<FileContentResult>().Subject.FileContents);

    [Fact]
    public async Task Download_EmptyNumber_AddsError()
    {
        var model = CreateModel("2026", " ", PrintBatchModel.ReportType.Clinical);

        (await model.OnPostDownloadAsync()).Should().BeOfType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>();
        model.ModelState["BatchYear"]!.Errors.Should().ContainSingle(e => e.ErrorMessage == "Enter batch number.");
    }

    [Fact]
    public async Task Download_BadYearAndNumber_AddsErrors()
    {
        var model = CreateModel("20x", "1234567", PrintBatchModel.ReportType.Clinical);

        await model.OnPostDownloadAsync();

        model.ModelState["BatchYear"]!.Errors[0].ErrorMessage.Should().Be("Enter a four digit year");
        model.ModelState["BatchNumber"]!.Errors[0].ErrorMessage.Should().Be("Enter a valid batch number");
        await _batches.DidNotReceive().GetBatchIdAsync(Arg.Any<short?>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Download_NoReportType_AddsError()
    {
        var model = CreateModel("2026", "5", PrintBatchModel.ReportType.None);

        await model.OnPostDownloadAsync();

        model.ModelState.Should().ContainKey("SelectedReportType");
    }

    [Theory]
    [InlineData("2026", "2026/5")]
    [InlineData("", "5")]
    public async Task Download_BatchNotFound_AddsError(string year, string label)
    {
        _batches.GetBatchIdAsync(Arg.Any<short?>(), Arg.Any<int>()).Returns(Task.FromResult<int?>(null));
        var model = CreateModel(year, "5", PrintBatchModel.ReportType.Feeds);

        var result = await model.OnPostDownloadAsync();

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>();
        model.ModelState["BatchNumber"]!.Errors[0].ErrorMessage.Should().Be($"Batch {label} was not found.");
        await _batches.Received(1).GetBatchIdAsync(year == "" ? null : (short)2026, 5);
    }

    [Fact]
    public async Task Download_NoRows_ReportsNoRecords()
    {
        Rows("GetClinicalByBatchID");
        var model = CreateModel("2026", "5", PrintBatchModel.ReportType.Clinical);

        var result = await model.OnPostDownloadAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("ClinicalReport.doc");
        file.ContentType.Should().Be("application/vnd.ms-word");
        Html(result).Should().Contain("Clinical Report").And.Contain("No records found").And.Contain("2026/5");
    }

    [Fact]
    public async Task Download_Clinical_RendersEncodedSymptoms()
    {
        Rows("GetClinicalByBatchID", new Dictionary<string, object?> { ["DisplayRBSE"] = "00/26/00001", ["DisplayCPHH"] = "<x>", ["Apprehension"] = "Yes" });
        var model = CreateModel("2026", "5", PrintBatchModel.ReportType.Clinical);

        var html = Html(await model.OnPostDownloadAsync());

        html.Should().Contain("Case - 00/26/00001").And.Contain("&lt;x&gt;").And.Contain("APPR").And.Contain("Yes");
    }

    [Fact]
    public async Task Download_FarmAndCase_RendersCaseBlock()
    {
        Rows("GetCaseFarmByBatchID", new Dictionary<string, object?> { ["DisplayRBSE"] = "R1", ["Ownername"] = "Bob & Co", ["HerdType"] = "Dairy", ["PedigreeType"] = "P" });
        var model = CreateModel("2026", "5", PrintBatchModel.ReportType.FarmAndCase);

        var result = await model.OnPostDownloadAsync();

        result.Should().BeOfType<FileContentResult>().Which.FileDownloadName.Should().Be("FarmCaseReport.doc");
        Html(result).Should().Contain("Case And Farm Report").And.Contain("Bob &amp; Co").And.Contain("Dairy P");
    }

    [Fact]
    public async Task Download_Feeds_GroupsByRbseAndFormatsCphh()
    {
        Rows("GetFeedsByBatchID",
            new Dictionary<string, object?> { ["RBSE"] = "002600001", ["CPHH"] = "01234567890", ["YearFrom"] = 2020, ["Name"] = "S1" },
            new Dictionary<string, object?> { ["RBSE"] = "002600001", ["CPHH"] = "01234567890", ["YearFrom"] = 2021, ["Name"] = "S2" },
            new Dictionary<string, object?> { ["RBSE"] = "002600002", ["CPHH"] = "short", ["YearFrom"] = 2022, ["Name"] = "S3" });
        var model = CreateModel("2026", "5", PrintBatchModel.ReportType.Feeds);

        var result = await model.OnPostDownloadAsync();
        var html = Html(result);

        result.Should().BeOfType<FileContentResult>().Which.FileDownloadName.Should().Be("FeedsReport.doc");
        html.Should().Contain("Case - 00/26/00001").And.Contain("CPHH - 01/234/5678/90").And.Contain("CPHH - short");
        html.Split("Case - ").Length.Should().Be(3);
    }

    [Fact]
    public async Task Download_Offspring_RendersRelations()
    {
        Rows("GetRelationsByBatchID",
            new Dictionary<string, object?> { ["RBSE"] = "002600001", ["SexDesc"] = "Female", ["RelationRBSE"] = "002600009", ["Sire"] = "Bull" });
        var model = CreateModel("2026", "5", PrintBatchModel.ReportType.Offspring);

        var result = await model.OnPostDownloadAsync();

        result.Should().BeOfType<FileContentResult>().Which.FileDownloadName.Should().Be("OffspringReport.doc");
        Html(result).Should().Contain("Case - 00/26/00001").And.Contain("00/26/00009").And.Contain("Female").And.Contain("Bull");
    }

    [Fact]
    public async Task Download_Pedigree_RendersSireAndDam()
    {
        Rows("GetDamSireDetailsByBatchID", new Dictionary<string, object?> { ["DisplayRBSE"] = "R1", ["SireName"] = "Sir", ["DamName"] = "Dame", ["DamStatus"] = "Alive" });
        var model = CreateModel("2026", "5", PrintBatchModel.ReportType.Pedigree);

        var result = await model.OnPostDownloadAsync();

        result.Should().BeOfType<FileContentResult>().Which.FileDownloadName.Should().Be("PedigreeReport.doc");
        Html(result).Should().Contain("Sire Detail:").And.Contain("Sir").And.Contain("Dame").And.Contain("Alive");
    }
}
