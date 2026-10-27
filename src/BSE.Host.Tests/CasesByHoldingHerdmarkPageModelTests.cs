using BSE.Host.Pages.Search;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Tests;

public sealed class CasesByHoldingHerdmarkPageModelTests
{
    private readonly ICaseSearchService _search = Substitute.For<ICaseSearchService>();

    private CasesByHoldingHerdmarkModel CreateModel()
    {
        var model = new CasesByHoldingHerdmarkModel(_search);
        PageTestContext.Attach(model);
        return model;
    }

    private static List<CaseDetailSearchResult> Rows(int count) =>
        Enumerable.Range(1, count).Select(i => new CaseDetailSearchResult
        {
            Rbse = $"00260{i:0000}",
            Cphh = $"26/001/{i:0000}",
            Eartag = $"UK{i}",
            BirthDate = new DateTime(2020, 1, 1),
            PurchaseAgeInMonths = 3,
            DaysElapsed = i
        }).ToList();

    private void Returns(List<CaseDetailSearchResult> rows) =>
        _search.GetCasesByCphhAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(rows);

    [Fact]
    public async Task OnGet_InvalidModelState_DoesNotSearch()
    {
        var model = CreateModel();
        model.Cphh = "26/001";
        model.ModelState.AddModelError("Cphh", "bad");

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().GetCasesByCphhAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task OnGet_NoCriteria_NoQuery_NotFlagged()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.NoCriteria.Should().BeFalse();
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_NoCriteria_Searched_IsFlagged()
    {
        var model = CreateModel();
        model.Searched = true;

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
        await _search.DidNotReceiveWithAnyArgs().GetCasesByCphhAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task OnGet_NoCriteria_QueryStringPresent_IsFlagged()
    {
        var model = CreateModel();
        model.HttpContext.Request.QueryString = new QueryString("?x=1");

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_WithCriteria_NormalizesAndSearches()
    {
        Returns(Rows(25));
        var model = CreateModel();
        model.Cphh = "26/001/0001";
        model.Herdmark = " AB12 ";
        model.NumericHerdmark = "123456";
        model.IncludeNonGb = true;
        model.PageNumber = 99;

        await model.OnGetAsync();

        await _search.Received(1).GetCasesByCphhAsync("260010001", "AB12", "123456", true, Arg.Any<CancellationToken>());
        model.HasSearched.Should().BeTrue();
        model.TotalCount.Should().Be(25);
        model.TotalPages.Should().Be(3);
        model.PageNumber.Should().Be(3);
        model.GetPagedResults().Should().HaveCount(5);
    }

    [Fact]
    public async Task OnGet_PageNumberBelowOne_IsClamped()
    {
        Returns(Rows(3));
        var model = CreateModel();
        model.Herdmark = "AB";
        model.PageNumber = 0;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_NoResults_HasSearchedWithSinglePage()
    {
        Returns([]);
        var model = CreateModel();
        model.NumericHerdmark = "123456";

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.TotalPages.Should().Be(1);
    }

    [Theory]
    [InlineData("rbse", false)]
    [InlineData("rbse", true)]
    [InlineData("cphh", true)]
    [InlineData("eartag", false)]
    [InlineData("birthdate", true)]
    [InlineData("timeelapsed", false)]
    [InlineData("unknown", false)]
    public async Task GetPagedResults_Sorts(string column, bool desc)
    {
        Returns(Rows(3));
        var model = CreateModel();
        model.Cphh = "26";
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        var paged = model.GetPagedResults();
        paged.Should().HaveCount(3);
        if (column == "rbse" || column == "cphh" || column == "eartag")
        {
            paged[0].Rbse.Should().Be(desc ? "002600003" : "002600001");
        }
    }

    [Fact]
    public async Task Export_NoFilter_RedirectsToPage()
    {
        var result = await CreateModel().OnGetExportAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _search.DidNotReceiveWithAnyArgs().GetCasesByCphhAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Export_WithFilter_ReturnsWorkbookWithAllRows()
    {
        Returns(Rows(12));
        var model = CreateModel();
        model.Cphh = "26";

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("casebycphhsearchresults.xlsx");
        using var wb = new XLWorkbook(new MemoryStream(file.FileContents));
        var ws = wb.Worksheet("Results");
        ws.Cell(1, 1).GetString().Should().Be("RBSE");
        ws.LastRowUsed()!.RowNumber().Should().Be(13);
        ws.Cell(2, 5).GetString().Should().Be("01/01/2020 00:00:00");
    }
}
