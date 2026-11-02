using System.Text;
using BSE.Host.Pages.CaseWork;
using BSE.Modules.Batch.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class PrintBatchModelTests
{
    private readonly IBatchService _batchService = Substitute.For<IBatchService>();

    private PrintBatchModel Create(string? year, string? number, PrintBatchModel.ReportType type)
    {
        var model = new PrintBatchModel(_batchService) { BatchYear = year, BatchNumber = number, SelectedReportType = type };
        PageTestHelpers.Attach(model, "VLAAccess");
        return model;
    }

    private static IReadOnlyList<IDictionary<string, object?>> Rows(params Dictionary<string, object?>[] rows) =>
        rows.Cast<IDictionary<string, object?>>().ToList();

    private static string Html(IActionResult result) =>
        Encoding.UTF8.GetString(result.Should().BeOfType<FileContentResult>().Subject.FileContents);

    [Fact]
    public async Task OnPostDownloadAsync_RequiresABatchNumberAndReportType()
    {
        var model = Create(null, " ", PrintBatchModel.ReportType.None);

        var result = await model.OnPostDownloadAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain(["BatchYear", "SelectedReportType"]);
    }

    [Theory]
    [InlineData("20", "5", "BatchYear")]
    [InlineData("2026", "abc", "BatchNumber")]
    [InlineData("2026", "1234567", "BatchNumber")]
    public async Task OnPostDownloadAsync_RejectsMalformedBatchReferences(string year, string number, string key)
    {
        var model = Create(year, number, PrintBatchModel.ReportType.Clinical);

        (await model.OnPostDownloadAsync()).Should().BeOfType<PageResult>();

        model.ModelState.Keys.Should().Contain(key);
    }

    [Theory]
    [InlineData("2026", "5", "2026/5")]
    [InlineData("", "5", "was not found")]
    public async Task OnPostDownloadAsync_WhenTheBatchDoesNotExist_ShowsAnError(string year, string number, string expected)
    {
        _batchService.GetBatchIdAsync(Arg.Any<short?>(), Arg.Any<int>()).Returns((int?)null);
        var model = Create(year, number, PrintBatchModel.ReportType.Clinical);

        (await model.OnPostDownloadAsync()).Should().BeOfType<PageResult>();

        model.ModelState["BatchNumber"]!.Errors.Single().ErrorMessage.Should().Contain(expected);
    }

    [Theory]
    [InlineData(PrintBatchModel.ReportType.Clinical, "ClinicalReport.doc", "GetClinicalByBatchID")]
    [InlineData(PrintBatchModel.ReportType.FarmAndCase, "FarmCaseReport.doc", "GetCaseFarmByBatchID")]
    [InlineData(PrintBatchModel.ReportType.Feeds, "FeedsReport.doc", "GetFeedsByBatchID")]
    [InlineData(PrintBatchModel.ReportType.Offspring, "OffspringReport.doc", "GetRelationsByBatchID")]
    [InlineData(PrintBatchModel.ReportType.Pedigree, "PedigreeReport.doc", "GetDamSireDetailsByBatchID")]
    public async Task OnPostDownloadAsync_NamesTheFileAndRunsTheMatchingProcedure(PrintBatchModel.ReportType type, string fileName, string procedure)
    {
        _batchService.GetBatchIdAsync(Arg.Any<short?>(), 5).Returns(11);
        _batchService.GetReportRowsAsync(procedure, 11).Returns(Rows());
        var model = Create("2026", "5", type);

        var result = await model.OnPostDownloadAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be(fileName);
        file.ContentType.Should().Be("application/vnd.ms-word");
        Encoding.UTF8.GetString(file.FileContents).Should().Contain("No records found");
    }

    [Fact]
    public async Task ClinicalReport_ListsSymptomsPerCase()
    {
        _batchService.GetBatchIdAsync(Arg.Any<short?>(), 5).Returns(11);
        _batchService.GetReportRowsAsync("GetClinicalByBatchID", 11)
            .Returns(Rows(new Dictionary<string, object?> { ["DisplayRBSE"] = "00/26/00001", ["DisplayCPHH"] = "01/001/0007/01", ["Apprehension"] = true }));

        var html = Html(await Create("2026", "5", PrintBatchModel.ReportType.Clinical).OnPostDownloadAsync());

        html.Should().Contain("Case - 00/26/00001").And.Contain("APPR").And.Contain("<h1>Clinical Report</h1>");
    }

    [Fact]
    public async Task CaseAndFarmReport_ShowsOwnerAndOnsetDetails()
    {
        _batchService.GetBatchIdAsync(Arg.Any<short?>(), 5).Returns(11);
        _batchService.GetReportRowsAsync("GetCaseFarmByBatchID", 11)
            .Returns(Rows(new Dictionary<string, object?> { ["DisplayRBSE"] = "R", ["Ownername"] = "<Owner>", ["Address1"] = "1 Lane", ["HerdType"] = "Dairy" }));

        var html = Html(await Create("2026", "5", PrintBatchModel.ReportType.FarmAndCase).OnPostDownloadAsync());

        html.Should().Contain("&lt;Owner&gt;").And.Contain("H TYPE").And.Contain("Dairy");
    }

    [Fact]
    public async Task FeedsReport_GroupsRowsByCaseAndFormatsTheCphh()
    {
        _batchService.GetBatchIdAsync(Arg.Any<short?>(), 5).Returns(11);
        _batchService.GetReportRowsAsync("GetFeedsByBatchID", 11).Returns(Rows(
            new Dictionary<string, object?> { ["RBSE"] = "002600001", ["CPHH"] = "01001000701", ["YearFrom"] = 2000, ["RationName"] = "one" },
            new Dictionary<string, object?> { ["RBSE"] = "002600001", ["CPHH"] = "01001000701", ["YearFrom"] = 2001, ["RationName"] = "two" },
            new Dictionary<string, object?> { ["RBSE"] = "002600002", ["CPHH"] = "short", ["YearFrom"] = 2002 }));

        var html = Html(await Create("2026", "5", PrintBatchModel.ReportType.Feeds).OnPostDownloadAsync());

        html.Should().Contain("01/001/0007/01").And.Contain("one").And.Contain("two").And.Contain("CPHH - short");
        html.Split("case-block").Length.Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task OffspringReport_GroupsRelationsPerCase()
    {
        _batchService.GetBatchIdAsync(Arg.Any<short?>(), 5).Returns(11);
        _batchService.GetReportRowsAsync("GetRelationsByBatchID", 11).Returns(Rows(
            new Dictionary<string, object?> { ["RBSE"] = "002600001", ["SexDesc"] = "Female", ["RelationRBSE"] = "002700011", ["Sire"] = "Bull" }));

        var html = Html(await Create("2026", "5", PrintBatchModel.ReportType.Offspring).OnPostDownloadAsync());

        html.Should().Contain("RELATION TYPE").And.Contain("Female").And.Contain("Bull");
    }

    [Fact]
    public async Task PedigreeReport_ShowsSireAndDamDetail()
    {
        _batchService.GetBatchIdAsync(Arg.Any<short?>(), 5).Returns(11);
        _batchService.GetReportRowsAsync("GetDamSireDetailsByBatchID", 11)
            .Returns(Rows(new Dictionary<string, object?> { ["DisplayRBSE"] = "R", ["SireName"] = "Sire One", ["DamName"] = "Dam One", ["DamStatus"] = "Alive" }));

        var html = Html(await Create("2026", "5", PrintBatchModel.ReportType.Pedigree).OnPostDownloadAsync());

        html.Should().Contain("Sire Detail:").And.Contain("Dam Detail:").And.Contain("Sire One").And.Contain("Dam One");
    }
}
