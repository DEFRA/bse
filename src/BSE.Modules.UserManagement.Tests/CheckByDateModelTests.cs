using BSE.Host.Pages.Bsess;
using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class CheckByDateModelTests
{
    private readonly IBsessCheckService _service = Substitute.For<IBsessCheckService>();

    private CheckByDateModel Create(bool withQuery)
    {
        var model = new CheckByDateModel(_service);
        var http = new DefaultHttpContext();
        if (withQuery)
            http.Request.QueryString = new QueryString("?StartDate=x");
        model.PageContext = new PageContext { HttpContext = http };
        return model;
    }

    private static BsessDiscrepancyRecord R(string rbse, int day, string bsess = "  B  ", string bse = "A") =>
        new(rbse, new DateTime(2020, 1, day), new DateTime(2021, 1, day), bsess, bse, bsess, bse);

    [Fact]
    public async Task OnGet_NoQuery_DoesNotSearch()
    {
        var model = Create(false);

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.HasSearched.Should().BeFalse();
        await _service.DidNotReceiveWithAnyArgs().GetCheckByDateAsync(default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnGet_MissingDates_AddsErrors()
    {
        var model = Create(true);

        await model.OnGetAsync();

        model.ModelState.Should().ContainKeys("StartDate", "EndDate");
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_EndBeforeStart_AddsError()
    {
        var model = Create(true);
        model.StartDate = new DateTime(2020, 2, 1);
        model.EndDate = new DateTime(2020, 1, 1);

        await model.OnGetAsync();

        model.ModelState["EndDate"]!.Errors.Should().ContainSingle();
        model.HasSearched.Should().BeFalse();
    }

    [Theory]
    [InlineData("BsessBirthDate", false, "R1")]
    [InlineData("BseBirthDate", true, "R3")]
    [InlineData("BsessEartag", false, "R1")]
    [InlineData("BseEartag", false, "R1")]
    [InlineData("BsessTestGroup", false, "R1")]
    [InlineData("BseTestGroup", false, "R1")]
    [InlineData("Rbse", true, "R3")]
    [InlineData("unknown", false, "R1")]
    public async Task OnGet_ValidRange_SortsAndPages(string column, bool desc, string expectedFirst)
    {
        _service.GetCheckByDateAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { R("R1", 1), R("R2", 2), R("R3", 3) });
        var model = Create(true);
        model.StartDate = new DateTime(2020, 1, 1);
        model.EndDate = new DateTime(2020, 2, 1);
        model.SortColumn = column;
        model.SortDesc = desc;
        model.PageNumber = 50;

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.PageNumber.Should().Be(1);
        model.TotalPages.Should().Be(1);
        model.PagedDiscrepancies[0].Rbse.Should().Be(expectedFirst);
    }

    [Fact]
    public async Task OnGet_LowPageNumber_ClampsToOne()
    {
        _service.GetCheckByDateAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 12).Select(i => R($"R{i:00}", 1)).ToList());
        var model = Create(true);
        model.StartDate = new DateTime(2020, 1, 1);
        model.EndDate = new DateTime(2020, 2, 1);
        model.PageNumber = 0;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
        model.TotalPages.Should().Be(2);
        model.PagedDiscrepancies.Should().HaveCount(10);
    }

    [Fact]
    public async Task Export_MissingParameters_Redirects()
    {
        (await Create(false).OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
        (await Create(true).OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Export_ValidRange_ReturnsWorkbook()
    {
        _service.GetCheckByDateAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { R("R1", 1), new BsessDiscrepancyRecord("R2", null, null, null, null, null, null) });
        var model = Create(true);
        model.StartDate = new DateTime(2020, 1, 1);
        model.EndDate = new DateTime(2020, 2, 1);

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("tsess-check-by-date.xlsx");
        file.FileContents.Should().NotBeEmpty();
    }
}
