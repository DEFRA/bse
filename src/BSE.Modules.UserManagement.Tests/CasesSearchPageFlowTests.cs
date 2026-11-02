using BSE.Host.Pages.Search;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class CasesSearchPageFlowTests
{
    private readonly ICaseSearchService _search = Substitute.For<ICaseSearchService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    public CasesSearchPageFlowTests()
    {
        _lookups.GetSexesAsync().Returns(new[] { new LuSex { Id = 1, Code = "F", Description = "Female" } });
        _lookups.GetLookupAsync(LookupTableId.Survey).Returns(new[] { new LookupItem(1, "S1", "Survey 1") });
        _lookups.GetCaseFatesAsync().Returns(new[] { new LuCaseFate { Id = 1, Code = "SL", Description = "Slaughter" } });
        _lookups.GetTestResultsAsync().Returns(new[] { new LuTestResult { Id = 1, Code = "Neg", Description = "Negative" } });
    }

    private CasesModel Create(string? query = null)
    {
        var http = new DefaultHttpContext();
        if (query is not null) http.Request.QueryString = new QueryString(query);
        return new CasesModel(_search, _lookups) { PageContext = new PageContext { HttpContext = http } };
    }

    private static CaseSearchResult R(string rbse, string? cphh = "C", DateTime? date = null, string? text = "x") =>
        new(rbse, cphh!, text, text, text, date, "Y", date, text, text, date, text, text, text, text, text);

    [Fact]
    public async Task OnGet_LoadsLookupOptions()
    {
        var model = Create();

        await model.OnGetAsync();

        model.SexOptions.Should().ContainSingle(o => o.Code == "F");
        model.SurveyOptions.Should().HaveCount(1);
        model.FateOptions.Should().ContainSingle(o => o.Code == "SL");
        model.FinalResultOptions.Should().ContainSingle(o => o.Code == "Neg");
        model.NoCriteria.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_QueryWithoutCriteria_FlagsNoCriteria()
    {
        var model = Create("?Searched=true");
        model.Searched = true;

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_InvalidDates_SkipsSearch()
    {
        var model = Create("?x=1");
        model.Filter.Eartag = "123";
        model.Filter.EarliestFormADate = "not a date";

        await model.OnGetAsync();

        model.Filter.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().SearchCasesAsync(default!);
    }

    [Fact]
    public async Task OnGet_Criteria_RunsSearchAndClampsPage()
    {
        _search.SearchCasesAsync(Arg.Any<CaseSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 12).Select(i => R($"0026{i:00000}")).ToList());
        var model = Create("?x=1");
        model.Filter.Rbse = "16/01";
        model.Filter.PageNumber = 9;

        await model.OnGetAsync();

        model.Filter.HasSearched.Should().BeTrue();
        model.Filter.Results.Should().HaveCount(12);
        model.Filter.PageNumber.Should().Be(2);

        var low = Create("?x=1");
        low.Filter.Notes = "n";
        low.Filter.PageNumber = 0;
        await low.OnGetAsync();
        low.Filter.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Export_WithoutCriteriaOrWithBadDates_Redirects()
    {
        (await Create().OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();

        var bad = Create();
        bad.Filter.Eartag = "1";
        bad.Filter.LatestBirthDate = "nope";
        (await bad.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Export_WithResults_ReturnsWorkbook()
    {
        _search.SearchCasesAsync(Arg.Any<CaseSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[] { R("002600002", date: new DateTime(2020, 1, 2)), R("002600001", date: null) });
        var model = Create();
        model.Filter.Eartag = "1";

        var file = (await model.OnGetExportAsync()).Should().BeOfType<FileContentResult>().Subject;

        file.FileDownloadName.Should().Be("casesearchresults.xlsx");
        file.FileContents.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Fate")] [InlineData("FinalResult")] [InlineData("Sex")] [InlineData("Survey")]
    [InlineData("Dbse")] [InlineData("PassiveActive")] [InlineData("EarliestFormADate")]
    [InlineData("LatestFormADate")] [InlineData("EarliestFinalResultDate")] [InlineData("LatestFinalResultDate")]
    [InlineData("EarliestBirthDate")] [InlineData("LatestBirthDate")]
    public async Task EachFilterFieldAloneCountsAsCriteria(string field)
    {
        _search.SearchCasesAsync(Arg.Any<CaseSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<CaseSearchResult>());
        var model = Create("?x=1");
        var value = field.Contains("Date") ? "2024-01-01" : field == "Dbse" ? "24/1" : "1";
        typeof(BSE.Host.Models.ViewModels.CaseSearchViewModel).GetProperty(field)!.SetValue(model.Filter, value);

        await model.OnGetAsync();

        model.Filter.HasSearched.Should().BeTrue();
    }

    [Fact]
    public async Task ImportedCaseFlagAloneCountsAsCriteria()
    {
        _search.SearchCasesAsync(Arg.Any<CaseSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<CaseSearchResult>());
        var model = Create("?x=1");
        model.Filter.IsImportedCase = true;

        await model.OnGetAsync();

        model.Filter.HasSearched.Should().BeTrue();
    }
}
