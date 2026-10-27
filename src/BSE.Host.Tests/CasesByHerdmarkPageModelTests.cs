using BSE.Host.Pages.Search;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Tests;

public sealed class CasesByHerdmarkPageModelTests
{
    private readonly ICaseSearchService _search = Substitute.For<ICaseSearchService>();

    private CasesByHerdmarkModel CreateModel()
    {
        var model = new CasesByHerdmarkModel(_search);
        PageTestContext.Attach(model);
        return model;
    }

    private static List<CaseDetailSearchResult> Rows(int count) =>
        Enumerable.Range(1, count).Select(i => new CaseDetailSearchResult
        {
            Rbse = $"00260{i:0000}",
            Cphh = "26/001/0001",
            Eartag = $"UK{i}"
        }).ToList();

    private void Returns(List<CaseDetailSearchResult> rows) =>
        _search.GetCasesByEartagHerdmarkAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(rows);

    [Fact]
    public async Task OnGet_InvalidModelState_DoesNotSearch()
    {
        var model = CreateModel();
        model.Searched = true;
        model.ModelState.AddModelError("Herdmark", "bad");

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().GetCasesByEartagHerdmarkAsync(default!, default);
    }

    [Fact]
    public async Task OnGet_NotSearched_DoesNotSearch()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().GetCasesByEartagHerdmarkAsync(default!, default);
    }

    [Fact]
    public async Task OnGet_Searched_TrimsHerdmarkAndPages()
    {
        Returns(Rows(21));
        var model = CreateModel();
        model.Searched = true;
        model.Herdmark = "  UK12 ";
        model.IncludeNonGb = true;
        model.PageNumber = 50;

        await model.OnGetAsync();

        await _search.Received(1).GetCasesByEartagHerdmarkAsync("UK12", true, Arg.Any<CancellationToken>());
        model.HasSearched.Should().BeTrue();
        model.TotalPages.Should().Be(3);
        model.PageNumber.Should().Be(3);
        model.GetPagedResults().Should().HaveCount(1);
    }

    [Fact]
    public async Task OnGet_BlankHerdmark_SearchesWithEmptyString()
    {
        Returns([]);
        var model = CreateModel();
        model.Searched = true;
        model.PageNumber = -2;

        await model.OnGetAsync();

        await _search.Received(1).GetCasesByEartagHerdmarkAsync("", false, Arg.Any<CancellationToken>());
        model.PageNumber.Should().Be(1);
        model.TotalPages.Should().Be(1);
    }

    [Theory]
    [InlineData("eartag", true, "002600003")]
    [InlineData("eartag", false, "002600001")]
    [InlineData("rbse", true, "002600003")]
    [InlineData("", false, "002600001")]
    public async Task GetPagedResults_Sorts(string column, bool desc, string first)
    {
        Returns(Rows(3));
        var model = CreateModel();
        model.Searched = true;
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.GetPagedResults()[0].Rbse.Should().Be(first);
    }

    [Fact]
    public async Task Export_ReturnsWorkbook()
    {
        Returns(Rows(2));
        var model = CreateModel();
        model.Herdmark = "UK";

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("casebyherdmarksearchresults.xlsx");
        using var wb = new XLWorkbook(new MemoryStream(file.FileContents));
        var ws = wb.Worksheet("Results");
        ws.Cell(2, 1).GetString().Should().Be("002600001");
        ws.Cell(3, 4).GetString().Should().Be("UK2");
    }
}
