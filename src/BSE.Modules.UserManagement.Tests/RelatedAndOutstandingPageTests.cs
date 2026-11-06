using BSE.Host.Pages.Search;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public class RelatedAnimalsModelTests
{
    private readonly ICaseSearchService _search = Substitute.For<ICaseSearchService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private RelatedAnimalsModel Create(string? query = null)
    {
        _lookups.GetLookupAsync(LookupTableId.RelationType)
            .Returns(new[] { new LookupItem { Code = "HALF", Description = "Half sibling" } });
        return new RelatedAnimalsModel(_search, _lookups) { PageContext = CaseDetailFixtures.Context(query) };
    }

    private static RelatedAnimalResult Related(int i) =>
        new($"00/01/{i:00000}", $"12/345/{i:0000}", i % 2 == 0 ? "DAM" : "SIRE", "F", $"UK{i:000000}",
            $"0{i}/01/2020", $"Fate{i}", new DateTime(2022, 1, i), $"Name{i}", $"RelTag{i}", $"00/02/{i:00000}");

    private void ReturnRows(int count) =>
        _search.GetRelatedAnimalsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, count).Select(Related).ToList());

    [Fact]
    public async Task OnGet_AddsDamAndSireBeforeLookupRows()
    {
        var model = Create();

        await model.OnGetAsync();

        model.RelationTypeOptions.Select(o => o.Code).Should().ContainInOrder("DAM", "SIRE", "HALF");
    }

    [Fact]
    public async Task OnGet_NoFilter_NotSearched_LeavesFlagsClear()
    {
        var model = Create();

        await model.OnGetAsync();

        model.NoCriteria.Should().BeFalse();
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_SearchedWithoutFilter_SetsNoCriteria()
    {
        var model = Create();
        model.Searched = true;

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
        RelatedAnimalsModel.NoCriteriaMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OnGet_QueryStringWithoutFilter_SetsNoCriteria()
    {
        var model = Create("?Searched=true");

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_InvalidModelState_SkipsSearch()
    {
        var model = Create();
        model.Rbse = "x";
        model.ModelState.AddModelError("Rbse", "bad");

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_WithRbse_FormatsValuesAndSearches()
    {
        var model = Create();
        model.Rbse = "16/01/00001";
        model.RelationRbse = "16/02/00002";
        model.Name = "Bess";
        model.Eartag = "UK1";
        model.RelationType = "DAM";
        ReturnRows(3);

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.TotalCount.Should().Be(3);
        model.PagedResults().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(99, 2)]
    public async Task OnGet_PageNumber_IsClamped(int requested, int expected)
    {
        var model = Create();
        model.Name = "x";
        model.PageNumber = requested;
        ReturnRows(14);

        await model.OnGetAsync();

        model.PageNumber.Should().Be(expected);
    }

    [Fact]
    public async Task PagedResults_SortsByEveryColumnInBothDirections()
    {
        var model = Create();
        model.Eartag = "UK";
        ReturnRows(4);
        await model.OnGetAsync();

        string[] columns =
        [
            "rbse", "cphh", "relationtype", "relsex", "eartag", "relbirthdate", "relfate",
            "leftdate", "relname", "releartag", "relationrbse", "something-else"
        ];
        foreach (var column in columns)
        {
            foreach (var desc in new[] { false, true })
            {
                model.SortColumn = column;
                model.SortDesc = desc;
                model.PagedResults().Should().HaveCount(4, $"{column} desc={desc}");
            }
        }
    }

    [Fact]
    public async Task OnGetExport_WithoutFilter_Redirects()
    {
        var model = Create();

        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnGetExport_WithFilter_ReturnsWorkbook()
    {
        var model = Create();
        model.RelationType = "SIRE";
        model.SortColumn = "relname";
        ReturnRows(3);

        var result = await model.OnGetExportAsync();

        result.Should().BeOfType<FileContentResult>().Which.FileContents.Should().NotBeEmpty();
    }
}

public class OutstandingModelTests
{
    private readonly IOutstandingDataSearchService _search = Substitute.For<IOutstandingDataSearchService>();

    private OutstandingModel Create(string? query = null) =>
        new(_search) { PageContext = CaseDetailFixtures.Context(query) };

    private static List<OutstandingCaseResult> Rows(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new OutstandingCaseResult($"00/01/{i:00000}", $"12/345/{i:0000}", $"UK{i:000000}",
                new DateTime(2022, 1, i), new DateTime(2020, 1, i), $"Fate{i}", $"Result{i}"))
            .ToList();

    [Fact]
    public async Task OnGet_NothingSelected_NotSearched_NoWarning()
    {
        var model = Create();

        await model.OnGetAsync();

        model.NoOptionSelected.Should().BeFalse();
        model.Filter.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_SearchedWithoutType_FlagsNoOption()
    {
        var model = Create();
        model.Searched = true;

        await model.OnGetAsync();

        model.NoOptionSelected.Should().BeTrue();
        OutstandingModel.NoOptionMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OnGet_QueryStringWithoutType_FlagsNoOption()
    {
        var model = Create("?x=1");

        await model.OnGetAsync();

        model.NoOptionSelected.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_InvalidDate_SkipsSearch()
    {
        var model = Create();
        model.Filter.SearchType = "Fates";
        model.Filter.EarliestFormADate = "garbage";

        await model.OnGetAsync();

        model.Filter.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().GetOutstandingFatesAsync(default!, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Fates")]
    [InlineData("Results")]
    [InlineData("BSE1")]
    public async Task OnGet_EachSearchType_CallsMatchingService(string type)
    {
        var model = Create();
        model.Filter.SearchType = type;
        _search.GetOutstandingFatesAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(2));
        _search.GetOutstandingResultsAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(3));
        _search.GetOutstandingBse1sAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(4));

        await model.OnGetAsync();

        model.Filter.HasSearched.Should().BeTrue();
        model.Filter.TotalCount.Should().Be(type switch { "Fates" => 2, "Results" => 3, _ => 4 });
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 2)]
    public async Task OnGet_PageNumber_IsClamped(int requested, int expected)
    {
        var model = Create();
        model.Filter.SearchType = "BSE1";
        model.Filter.PageNumber = requested;
        _search.GetOutstandingBse1sAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(12));

        await model.OnGetAsync();

        model.Filter.PageNumber.Should().Be(expected);
    }

    [Fact]
    public async Task OnGet_DateRangeOutOfOrder_SetsBothErrors()
    {
        var model = Create();
        model.Filter.SearchType = "BSE1";
        model.Filter.EarliestFormADate = "2024-05-01";
        model.Filter.LatestFormADate = "2024-01-01";

        await model.OnGetAsync();

        model.Filter.EarliestFormADateError.Should().Contain("earlier");
        model.Filter.LatestFormADateError.Should().Contain("later");
    }

    [Fact]
    public async Task Filter_SortsByEveryColumn()
    {
        var model = Create();
        model.Filter.SearchType = "BSE1";
        _search.GetOutstandingBse1sAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(3));
        await model.OnGetAsync();

        foreach (var column in new[] { "rbse", "cphh", "eartag", "formadate", "birthdate", "fate", "finalresult", "other" })
        {
            foreach (var desc in new[] { false, true })
            {
                model.Filter.SortColumn = column;
                model.Filter.SortDesc = desc;
                model.Filter.PagedResults().Should().HaveCount(3);
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("nonsense")]
    public async Task OnGetExport_UnknownType_Redirects(string type)
    {
        var model = Create();
        model.Filter.SearchType = type;

        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnGetExport_InvalidDate_Redirects()
    {
        var model = Create();
        model.Filter.SearchType = "Fates";
        model.Filter.LatestFormADate = "nope";

        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Theory]
    [InlineData("Fates")]
    [InlineData("Results")]
    [InlineData("BSE1")]
    public async Task OnGetExport_ValidType_ReturnsWorkbook(string type)
    {
        var model = Create();
        model.Filter.SearchType = type;
        _search.GetOutstandingFatesAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(2));
        _search.GetOutstandingResultsAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(2));
        _search.GetOutstandingBse1sAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(2));

        var result = await model.OnGetExportAsync();

        result.Should().BeOfType<FileContentResult>()
            .Which.FileDownloadName.Should().Be("outstandingdatasearchresults.xlsx");
    }
}
