using BSE.Host.Pages.Bsess;
using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class CheckByDatePageModelTests
{
    private static readonly DateTime Start = new(2026, 1, 1);
    private static readonly DateTime End = new(2026, 1, 31);

    private readonly IBsessCheckService _service = Substitute.For<IBsessCheckService>();

    private CheckByDateModel CreateModel(bool withQuery = true)
    {
        var model = new CheckByDateModel(_service);
        PageTestContext.Attach(model);
        if (withQuery)
            model.PageContext.HttpContext.Request.QueryString = new QueryString("?StartDate=x");
        return model;
    }

    private static BsessDiscrepancyRecord Row(string rbse, DateTime? b1 = null, DateTime? b2 = null, string? e1 = null, string? e2 = null, string? g1 = null, string? g2 = null) =>
        new(rbse, b1, b2, e1, e2, g1, g2);

    private void Returns(params BsessDiscrepancyRecord[] rows) =>
        _service.GetCheckByDateAsync(Start, End, Arg.Any<CancellationToken>()).Returns(rows);

    [Fact]
    public async Task OnGet_NoStartDateKey_RendersWithoutSearch()
    {
        var model = CreateModel(withQuery: false);

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.HasSearched.Should().BeFalse();
        await _service.DidNotReceiveWithAnyArgs().GetCheckByDateAsync(default, default);
    }

    [Fact]
    public async Task OnGet_MissingDates_AddsBothErrors()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.ModelState[nameof(CheckByDateModel.StartDate)]!.Errors.Should().ContainSingle();
        model.ModelState[nameof(CheckByDateModel.EndDate)]!.Errors.Should().ContainSingle();
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_EndBeforeStart_AddsError()
    {
        var model = CreateModel();
        model.StartDate = End;
        model.EndDate = Start;

        await model.OnGetAsync();

        model.ModelState[nameof(CheckByDateModel.EndDate)]!.Errors.Single().ErrorMessage.Should().Contain("on or after");
        await _service.DidNotReceiveWithAnyArgs().GetCheckByDateAsync(default, default);
    }

    [Fact]
    public async Task OnGet_ValidRange_SearchesAndPages()
    {
        Returns(Enumerable.Range(1, 25).Select(i => Row($"R{i:000}")).ToArray());
        var model = CreateModel();
        model.StartDate = Start;
        model.EndDate = End;
        model.PageNumber = 99;

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.Discrepancies.Should().HaveCount(25);
        model.TotalPages.Should().Be(3);
        model.PageNumber.Should().Be(3);
        model.PagedDiscrepancies.Select(d => d.Rbse).Should().Equal("R021", "R022", "R023", "R024", "R025");
    }

    [Fact]
    public async Task OnGet_PageNumberBelowOne_ResetsToOne()
    {
        Returns(Row("A"));
        var model = CreateModel();
        model.StartDate = Start;
        model.EndDate = End;
        model.PageNumber = 0;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
        model.TotalPages.Should().Be(1);
    }

    [Theory]
    [InlineData("rbse", false, "A")]
    [InlineData("rbse", true, "B")]
    [InlineData("BsessBirthDate", false, "A")]
    [InlineData("BsessBirthDate", true, "B")]
    [InlineData("BseBirthDate", false, "A")]
    [InlineData("BseBirthDate", true, "B")]
    [InlineData("BsessEartag", false, "A")]
    [InlineData("BsessEartag", true, "B")]
    [InlineData("BseEartag", false, "A")]
    [InlineData("BseEartag", true, "B")]
    [InlineData("BsessTestGroup", false, "A")]
    [InlineData("BsessTestGroup", true, "B")]
    [InlineData("BseTestGroup", false, "A")]
    [InlineData("BseTestGroup", true, "B")]
    [InlineData("bogus", false, "A")]
    public async Task OnGet_Sorting_OrdersRows(string column, bool desc, string first)
    {
        var d1 = new DateTime(2025, 1, 1);
        var d2 = new DateTime(2025, 2, 1);
        Returns(
            Row("B", d2, d2, "  z", "  z", "  z", "  z"),
            Row("A", d1, d1, "a  ", "a  ", "a  ", "a  "));
        var model = CreateModel();
        model.StartDate = Start;
        model.EndDate = End;
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.PagedDiscrepancies[0].Rbse.Should().Be(first);
    }

    [Fact]
    public async Task OnGet_NullDatesSortFirstAscending()
    {
        Returns(Row("B", new DateTime(2025, 1, 1)), Row("A"));
        var model = CreateModel();
        model.StartDate = Start;
        model.EndDate = End;
        model.SortColumn = "BsessBirthDate";

        await model.OnGetAsync();

        model.PagedDiscrepancies[0].Rbse.Should().Be("A");
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public async Task Export_MissingQueryOrDates_RedirectsToPage(bool hasQuery, bool hasStart, bool hasEnd)
    {
        var model = CreateModel(hasQuery);
        model.StartDate = hasStart ? Start : null;
        model.EndDate = hasEnd ? End : null;

        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();

        await _service.DidNotReceiveWithAnyArgs().GetCheckByDateAsync(default, default);
    }

    [Fact]
    public async Task Export_ValidRange_ReturnsSortedWorkbook()
    {
        Returns(
            Row("B", new DateTime(2025, 3, 4), null, " E1 ", "E2", "G1", " G2"),
            Row("A"));
        var model = CreateModel();
        model.StartDate = Start;
        model.EndDate = End;

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("tsess-check-by-date.xlsx");
        using var wb = new XLWorkbook(new MemoryStream(file.FileContents));
        var ws = wb.Worksheet("TSESS Check Results");
        ws.Cell(1, 1).GetString().Should().Be("RBSE");
        ws.Cell(2, 1).GetString().Should().Be("A");
        ws.Cell(2, 2).GetString().Should().BeEmpty();
        ws.Cell(3, 1).GetString().Should().Be("B");
        ws.Cell(3, 2).GetString().Should().Be("04/03/2025");
        ws.Cell(3, 3).GetString().Should().BeEmpty();
        ws.Cell(3, 4).GetString().Should().Be("E1");
        ws.Cell(3, 7).GetString().Should().Be("G2");
    }
}
