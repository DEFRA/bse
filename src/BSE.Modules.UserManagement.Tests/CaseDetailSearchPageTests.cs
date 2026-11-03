using BSE.Host.Pages.Search;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

internal static class CaseDetailFixtures
{
    public static CaseDetailSearchResult Row(string rbse, int day) => new()
    {
        Rbse = rbse,
        Cphh = $"12/345/00{day:00}",
        Sex = day % 2 == 0 ? "F" : "M",
        Eartag = $"UK{day:000000}",
        BirthDate = new DateTime(2020, 1, day),
        PurchaseDate = new DateTime(2021, 1, day),
        PurchaseAgeInMonths = (short)day,
        OnsetDate = new DateTime(2022, 1, day),
        FormADate = new DateTime(2022, 2, day),
        SlaughterDate = new DateTime(2022, 3, day),
        FinalResultDate = new DateTime(2022, 4, day),
        OnsetAgeInMonths = (short)(day + 1),
        Fate = $"Fate{day}",
        FinalResult = $"Result{day}",
        Survey = $"Survey{day}",
        CaseStatus = $"Status{day}",
        TimeElapsed = $"{day} days",
        DaysElapsed = day,
        Origin = $"Origin{day}"
    };

    public static List<CaseDetailSearchResult> ThreeRows() =>
        [Row("00/01/00003", 3), Row("00/01/00001", 1), Row("00/01/00002", 2)];

    public static readonly string[] SortColumns =
    [
        "rbse", "cphh", "sex", "eartag", "birthdate", "origin", "purchasedate", "purchaseageinmonths",
        "onsetdate", "formadate", "slaughterdate", "finalresultdate", "onsetageinmonths", "fate",
        "finalresult", "survey", "casestatus", "timeelapsed", "unknown-column"
    ];

    public static PageContext Context(string? query = null)
    {
        var http = new DefaultHttpContext();
        if (query is not null) http.Request.QueryString = new QueryString(query);
        return new PageContext { HttpContext = http };
    }
}

public class CasesByHerdmarkModelTests
{
    private readonly ICaseSearchService _search = Substitute.For<ICaseSearchService>();

    private CasesByHerdmarkModel Create() =>
        new(_search) { PageContext = CaseDetailFixtures.Context() };

    [Fact]
    public async Task OnGet_NotSearched_DoesNothing()
    {
        var model = Create();

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().GetCasesByEartagHerdmarkAsync(default!, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnGet_InvalidModelState_DoesNothing()
    {
        var model = Create();
        model.Searched = true;
        model.ModelState.AddModelError("Herdmark", "too long");

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_Searched_TrimsHerdmarkAndPopulatesResults()
    {
        var model = Create();
        model.Searched = true;
        model.Herdmark = "  AB12  ";
        model.IncludeNonGb = true;
        _search.GetCasesByEartagHerdmarkAsync("AB12", true, Arg.Any<CancellationToken>())
            .Returns(CaseDetailFixtures.ThreeRows());

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.TotalCount.Should().Be(3);
        model.TotalPages.Should().Be(1);
        model.PagedResults().Should().HaveCount(3);
    }

    [Fact]
    public async Task OnGet_BlankHerdmark_SearchesWithEmptyString()
    {
        var model = Create();
        model.Searched = true;
        _search.GetCasesByEartagHerdmarkAsync("", false, Arg.Any<CancellationToken>())
            .Returns(new List<CaseDetailSearchResult>());

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.TotalPages.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 2)]
    public async Task OnGet_PageNumber_IsClampedIntoRange(int requested, int expected)
    {
        var model = Create();
        model.Searched = true;
        model.PageNumber = requested;
        _search.GetCasesByEartagHerdmarkAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 15).Select(i => CaseDetailFixtures.Row($"00/01/{i:00000}", (i % 27) + 1)).ToList());

        await model.OnGetAsync();

        model.PageNumber.Should().Be(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PagedResults_SortsByEveryColumn(bool desc)
    {
        var model = Create();
        model.Searched = true;
        _search.GetCasesByEartagHerdmarkAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(CaseDetailFixtures.ThreeRows());
        await model.OnGetAsync();

        foreach (var column in CaseDetailFixtures.SortColumns)
        {
            model.SortColumn = column;
            model.SortDesc = desc;
            model.PagedResults().Should().HaveCount(3, $"sorting by {column}");
        }
    }

    [Fact]
    public async Task PagedResults_SortedByRbseDescending_PutsHighestFirst()
    {
        var model = Create();
        model.Searched = true;
        model.SortColumn = "RBSE";
        model.SortDesc = true;
        _search.GetCasesByEartagHerdmarkAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(CaseDetailFixtures.ThreeRows());
        await model.OnGetAsync();

        model.PagedResults().Select(r => r.Rbse).Should().ContainInOrder("00/01/00003", "00/01/00002", "00/01/00001");
    }

    [Fact]
    public async Task OnGetExport_ReturnsWorkbook()
    {
        var model = Create();
        model.Herdmark = "AB12";
        model.SortColumn = "cphh";
        var rows = CaseDetailFixtures.ThreeRows();
        rows[0].BirthDate = null;
        rows[0].PurchaseAgeInMonths = null;
        _search.GetCasesByEartagHerdmarkAsync("AB12", false, Arg.Any<CancellationToken>()).Returns(rows);

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("casebyherdmarksearchresults.xlsx");
        file.FileContents.Should().NotBeEmpty();
    }
}

public class CasesByHoldingHerdmarkModelTests
{
    private readonly ICaseSearchService _search = Substitute.For<ICaseSearchService>();

    private CasesByHoldingHerdmarkModel Create(string? query = null) =>
        new(_search) { PageContext = CaseDetailFixtures.Context(query) };

    [Fact]
    public async Task OnGet_NoCriteria_NotSearched_LeavesFlagsClear()
    {
        var model = Create();

        await model.OnGetAsync();

        model.NoCriteria.Should().BeFalse();
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_SearchedWithoutCriteria_SetsNoCriteria()
    {
        var model = Create();
        model.Searched = true;

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
        CasesByHoldingHerdmarkModel.NoCriteriaMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OnGet_QueryStringWithoutCriteria_SetsNoCriteria()
    {
        var model = Create("?IncludeNonGb=true");

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_InvalidModelState_DoesNothing()
    {
        var model = Create();
        model.Herdmark = "bad!";
        model.ModelState.AddModelError("Herdmark", "Enter a valid herdmark.");

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
    }

    [Theory]
    [InlineData("12", null, null)]
    [InlineData(null, "AB12", null)]
    [InlineData(null, null, "123456")]
    public async Task OnGet_WithAnyCriterion_RunsSearch(string? cphh, string? herd, string? numeric)
    {
        var model = Create();
        model.Cphh = cphh;
        model.Herdmark = herd;
        model.NumericHerdmark = numeric;
        _search.GetCasesByCphhAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(CaseDetailFixtures.ThreeRows());

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.PagedResults().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(-3, 1)]
    [InlineData(40, 2)]
    public async Task OnGet_PageNumber_IsClamped(int requested, int expected)
    {
        var model = Create();
        model.Herdmark = "AB12";
        model.PageNumber = requested;
        _search.GetCasesByCphhAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 11).Select(i => CaseDetailFixtures.Row($"00/01/{i:00000}", i)).ToList());

        await model.OnGetAsync();

        model.PageNumber.Should().Be(expected);
    }

    [Fact]
    public async Task OnGetExport_WithoutFilter_Redirects()
    {
        var model = Create();

        var result = await model.OnGetExportAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnGetExport_WithFilter_ReturnsWorkbook()
    {
        var model = Create();
        model.NumericHerdmark = "123456";
        _search.GetCasesByCphhAsync(Arg.Any<string>(), Arg.Any<string>(), "123456", Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(CaseDetailFixtures.ThreeRows());

        var result = await model.OnGetExportAsync();

        result.Should().BeOfType<FileContentResult>()
            .Which.FileDownloadName.Should().Be("casebycphhsearchresults.xlsx");
    }

    [Fact]
    public async Task PagedResults_SortsWithoutError_ForEveryColumn()
    {
        var model = Create();
        model.Herdmark = "AB12";
        _search.GetCasesByCphhAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(CaseDetailFixtures.ThreeRows());
        await model.OnGetAsync();

        foreach (var column in CaseDetailFixtures.SortColumns)
        {
            foreach (var desc in new[] { false, true })
            {
                model.SortColumn = column;
                model.SortDesc = desc;
                model.PagedResults().Should().HaveCount(3);
            }
        }
    }
}
