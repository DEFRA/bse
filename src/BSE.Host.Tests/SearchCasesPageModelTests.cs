using BSE.Host.Pages.Search;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using BSE.SharedKernel;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class SearchCasesPageModelTests
{
    private readonly ICaseSearchService _search = Substitute.For<ICaseSearchService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    public SearchCasesPageModelTests()
    {
        _lookups.GetSexesAsync().Returns([new LuSex { Id = 1, Code = "M", Description = "Male" }]);
        _lookups.GetCaseFatesAsync().Returns([new LuCaseFate { Id = 2, Code = "S", Description = "Slaughter" }]);
        _lookups.GetTestResultsAsync().Returns([new LuTestResult { Id = 3, Code = "N", Description = "Negative" }]);
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns([new LookupItem(4, "Survey one")]);
        _search.SearchCasesAsync(Arg.Any<CaseSearchQuery>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private CasesModel CreateModel(string query = "")
    {
        var model = new CasesModel(_search, _lookups);
        PageTestContext.Attach(model);
        model.PageContext.HttpContext.Request.QueryString = new QueryString(query);
        return model;
    }

    private static CaseSearchResult Row(string rbse, string cphh = "C", DateTime? birth = null) =>
        new(rbse, cphh, "M", "Sv", "ET", birth, "N", birth, "Fate", "Res", birth, "2600001", "n", "bn", "UK", "12");

    private static List<CaseSearchResult> Rows(int n) =>
        Enumerable.Range(1, n).Select(i => Row($"00260{i:0000}")).ToList();

    [Fact]
    public async Task OnGet_LoadsLookupOptions()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.SexOptions.Should().ContainSingle().Which.Code.Should().Be("M");
        model.FateOptions.Should().ContainSingle().Which.Code.Should().Be("S");
        model.FinalResultOptions.Should().ContainSingle().Which.Code.Should().Be("N");
        model.SurveyOptions.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGet_NoFilterNoQuery_NoCriteriaFalse()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.NoCriteria.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().SearchCasesAsync(default!);
    }

    [Fact]
    public async Task OnGet_SearchedWithoutFilter_FlagsNoCriteria()
    {
        var model = CreateModel();
        model.Searched = true;

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_QueryStringWithoutFilter_FlagsNoCriteria()
    {
        var model = CreateModel("?Filter.IncludeNonGb=true");

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_InvalidDate_SkipsSearch()
    {
        var model = CreateModel();
        model.Filter.EarliestBirthDate = "not-a-date";

        await model.OnGetAsync();

        model.Filter.EarliestBirthDateError.Should().NotBeNull();
        model.Filter.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().SearchCasesAsync(default!);
    }

    [Fact]
    public async Task OnGet_WithRbse_PadsRbseAndSearches()
    {
        _search.SearchCasesAsync(Arg.Any<CaseSearchQuery>(), Arg.Any<CancellationToken>()).Returns(Rows(25));
        var model = CreateModel();
        model.Filter.Rbse = "26/1";
        model.Filter.PageNumber = 99;

        await model.OnGetAsync();

        await _search.Received(1).SearchCasesAsync(Arg.Is<CaseSearchQuery>(q => q.Rbse == "002600001"), Arg.Any<CancellationToken>());
        model.Filter.Rbse.Should().Be("00/26/00001");
        model.Filter.HasSearched.Should().BeTrue();
        model.Filter.TotalCount.Should().Be(25);
        model.Filter.PageNumber.Should().Be(3);
    }

    [Fact]
    public async Task OnGet_PageNumberBelowOne_ResetsToOne()
    {
        var model = CreateModel();
        model.Filter.Eartag = "123";
        model.Filter.PageNumber = 0;

        await model.OnGetAsync();

        model.Filter.PageNumber.Should().Be(1);
        model.NoCriteria.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_ImportedCaseAlone_CountsAsCriterion()
    {
        var model = CreateModel();
        model.Filter.IsImportedCase = true;

        await model.OnGetAsync();

        await _search.Received(1).SearchCasesAsync(Arg.Is<CaseSearchQuery>(q => q.IsImportedCase), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnGet_IncludeNonGbAlone_IsNotACriterion()
    {
        var model = CreateModel();
        model.Filter.IncludeNonGb = true;

        await model.OnGetAsync();

        await _search.DidNotReceiveWithAnyArgs().SearchCasesAsync(default!);
    }

    [Fact]
    public async Task OnGet_DbseWithSlash_IsStrippedInQuery()
    {
        var model = CreateModel();
        model.Filter.Dbse = "26/00001";

        await model.OnGetAsync();

        await _search.Received(1).SearchCasesAsync(Arg.Is<CaseSearchQuery>(q => q.Dbse == "2600001"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_NoFilter_RedirectsToPage()
    {
        var result = await CreateModel().OnGetExportAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _search.DidNotReceiveWithAnyArgs().SearchCasesAsync(default!);
    }

    [Fact]
    public async Task Export_InvalidDate_RedirectsToPage()
    {
        var model = CreateModel();
        model.Filter.Eartag = "1";
        model.Filter.EarliestFormADate = "2026-02-01";
        model.Filter.LatestFormADate = "2026-01-01";

        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Export_WithResults_ReturnsSortedWorkbook()
    {
        _search.SearchCasesAsync(Arg.Any<CaseSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns([Row("002600002", "B", new DateTime(2026, 1, 2, 3, 4, 5)), Row("002600001", "A")]);
        var model = CreateModel();
        model.Filter.Eartag = "1";

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("casesearchresults.xlsx");
        using var wb = new XLWorkbook(new MemoryStream(file.FileContents));
        var ws = wb.Worksheet("Case Search Results");
        ws.Cell(1, 1).GetString().Should().Be("RBSE");
        ws.Cell(2, 1).GetString().Should().Be("002600001");
        ws.Cell(2, 6).GetString().Should().BeEmpty();
        ws.Cell(3, 1).GetString().Should().Be("002600002");
        ws.Cell(3, 6).GetString().Should().Be("02/01/2026 03:04:05");
        ws.Cell(3, 12).GetString().Should().Be("2600001");
    }

    [Theory]
    [InlineData("rbse")]
    [InlineData("cphh")]
    [InlineData("sex")]
    [InlineData("survey")]
    [InlineData("eartag")]
    [InlineData("birthdate")]
    [InlineData("isbirthdateest")]
    [InlineData("origin")]
    [InlineData("formadate")]
    [InlineData("fate")]
    [InlineData("finalresult")]
    [InlineData("finalresultdate")]
    [InlineData("dbse")]
    [InlineData("valuationage")]
    [InlineData("notes")]
    [InlineData("babnotes")]
    [InlineData("unknown")]
    public void Sorting_AnyColumnBothDirections_ReturnsAllRows(string column)
    {
        var model = CreateModel();
        model.Filter.Results = Rows(3);
        model.Filter.SortColumn = column;

        model.Filter.GetSortedResults().Should().HaveCount(3);
        model.Filter.SortDesc = true;
        model.Filter.GetSortedResults().Should().HaveCount(3);
    }
}
