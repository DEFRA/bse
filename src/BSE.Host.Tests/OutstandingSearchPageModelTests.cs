using BSE.Host.Pages.Search;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Tests;

public sealed class OutstandingSearchPageModelTests
{
    private readonly IOutstandingDataSearchService _search = Substitute.For<IOutstandingDataSearchService>();

    private OutstandingModel CreateModel(string type = "")
    {
        var model = new OutstandingModel(_search);
        model.Filter.SearchType = type;
        PageTestContext.Attach(model);
        return model;
    }

    private static List<OutstandingCaseResult> Rows(int count) =>
        Enumerable.Range(1, count).Select(i =>
            new OutstandingCaseResult($"00260{i:0000}", "26/001/0001", $"UK{i}", new DateTime(2024, 5, 1), new DateTime(2020, 1, 1), "Fate", "Res")).ToList();

    public OutstandingSearchPageModelTests()
    {
        _search.GetOutstandingBse1sAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(2));
        _search.GetOutstandingFatesAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(3));
        _search.GetOutstandingResultsAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(4));
    }

    [Fact]
    public async Task OnGet_Nothing_NoFlagNoSearch()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.NoOptionSelected.Should().BeFalse();
        model.Filter.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_SearchedWithoutType_FlagsNoOption()
    {
        var model = CreateModel();
        model.Searched = true;

        await model.OnGetAsync();

        model.NoOptionSelected.Should().BeTrue();
        await _search.DidNotReceiveWithAnyArgs().GetOutstandingBse1sAsync(default!);
    }

    [Fact]
    public async Task OnGet_QueryStringWithUnknownType_FlagsNoOption()
    {
        var model = CreateModel("Other");
        model.HttpContext.Request.QueryString = new QueryString("?a=b");

        await model.OnGetAsync();

        model.NoOptionSelected.Should().BeTrue();
    }

    [Theory]
    [InlineData("BSE1", 2)]
    [InlineData("Fates", 3)]
    [InlineData("Results", 4)]
    public async Task OnGet_Type_CallsMatchingService(string type, int expected)
    {
        var model = CreateModel(type);
        model.Searched = true;

        await model.OnGetAsync();

        model.NoOptionSelected.Should().BeFalse();
        model.Filter.HasSearched.Should().BeTrue();
        model.Filter.Results.Should().HaveCount(expected);
    }

    [Fact]
    public async Task OnGet_InvalidDate_DoesNotSearch()
    {
        var model = CreateModel("BSE1");
        model.Filter.EarliestFormADate = "nope";

        await model.OnGetAsync();

        model.Filter.EarliestFormADateError.Should().NotBeNull();
        model.Filter.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_EarliestAfterLatest_DoesNotSearch()
    {
        var model = CreateModel("Fates");
        model.Filter.EarliestFormADate = "2024-06-01";
        model.Filter.LatestFormADate = "2024-01-01";

        await model.OnGetAsync();

        model.Filter.LatestFormADateError.Should().NotBeNull();
        await _search.DidNotReceiveWithAnyArgs().GetOutstandingFatesAsync(default!);
    }

    [Fact]
    public async Task OnGet_PassesParsedDatesAndClampsPage()
    {
        var model = CreateModel("Results");
        model.Filter.EarliestFormADate = "01/02/2024";
        model.Filter.LatestFormADate = "2024-03-01";
        model.Filter.IncludeNonGb = true;
        model.Filter.PageNumber = 0;

        await model.OnGetAsync();

        await _search.Received(1).GetOutstandingResultsAsync(
            Arg.Is<OutstandingSearchQuery>(q => q.EarliestFormADate == new DateTime(2024, 2, 1)
                && q.LatestFormADate == new DateTime(2024, 3, 1) && q.IncludeNonGbCases),
            Arg.Any<CancellationToken>());
        model.Filter.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_PageBeyondEnd_IsClamped()
    {
        _search.GetOutstandingBse1sAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(15));
        var model = CreateModel("BSE1");
        model.Filter.PageNumber = 10;

        await model.OnGetAsync();

        model.Filter.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task Export_UnknownType_Redirects()
    {
        (await CreateModel("x").OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Export_InvalidDate_Redirects()
    {
        var model = CreateModel("BSE1");
        model.Filter.LatestFormADate = "bad";

        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Theory]
    [InlineData("BSE1", 2)]
    [InlineData("Fates", 3)]
    [InlineData("Results", 4)]
    public async Task Export_ReturnsWorkbook(string type, int rows)
    {
        var model = CreateModel(type);
        model.Filter.SortColumn = "eartag";
        model.Filter.SortDesc = true;

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("outstandingdatasearchresults.xlsx");
        using var wb = new XLWorkbook(new MemoryStream(file.FileContents));
        var ws = wb.Worksheet("Results");
        ws.LastRowUsed()!.RowNumber().Should().Be(rows + 1);
        ws.Cell(2, 3).GetString().Should().Be($"UK{rows}");
    }
}
