using BSE.Host.Pages.Search;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using BSE.SharedKernel;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Tests;

public sealed class FarmsSearchPageModelTests
{
    private readonly IFarmSearchService _search = Substitute.For<IFarmSearchService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private FarmsModel CreateModel()
    {
        _lookups.GetLookupAsync(LookupTableId.BSECounty).Returns(new[] { new LookupItem(1, "Devon") }.AsEnumerable());
        _lookups.GetLookupAsync(LookupTableId.AHO).Returns(new[] { new LookupItem(2, "North"), new LookupItem(3, "South") }.AsEnumerable());
        var model = new FarmsModel(_search, _lookups);
        PageTestContext.Attach(model);
        return model;
    }

    private static List<FarmSearchResult> Rows(int count) =>
        Enumerable.Range(1, count).Select(i =>
            new FarmSearchResult($"26/001/{i:0000}", $"Owner{i}", "Addr", "Devon", "AB", "123456", "MR", "North", "Dairy", "Corr", i, 0)).ToList();

    private void Returns(List<FarmSearchResult> rows) =>
        _search.SearchFarmsAsync(Arg.Any<FarmSearchQuery>(), Arg.Any<CancellationToken>()).Returns(rows);

    [Fact]
    public async Task OnGet_LoadsLookups()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.CountyOptions.Should().HaveCount(1);
        model.AhoOptions.Should().HaveCount(2);
    }

    [Fact]
    public async Task OnGet_InvalidModelState_LoadsLookupsButDoesNotSearch()
    {
        var model = CreateModel();
        model.Filter.Cphh = "26";
        model.ModelState.AddModelError("Filter.Cphh", "bad");

        await model.OnGetAsync();

        model.CountyOptions.Should().NotBeEmpty();
        await _search.DidNotReceiveWithAnyArgs().SearchFarmsAsync(default!);
    }

    [Fact]
    public async Task OnGet_NoFilter_Searched_FlagsNoCriteria()
    {
        var model = CreateModel();
        model.Searched = true;

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
        model.Filter.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_NoFilter_NothingSubmitted_NotFlagged()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.NoCriteria.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_NoFilter_QueryPresent_Flagged()
    {
        var model = CreateModel();
        model.HttpContext.Request.QueryString = new QueryString("?a=b");

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_IncludeNonGbAloneIsNotACriterion()
    {
        var model = CreateModel();
        model.Searched = true;
        model.Filter.IncludeNonGb = true;

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
        await _search.DidNotReceiveWithAnyArgs().SearchFarmsAsync(default!);
    }

    [Theory]
    [InlineData("Cphh")]
    [InlineData("OwnerName")]
    [InlineData("Address")]
    [InlineData("County")]
    [InlineData("Herdmark")]
    [InlineData("NumericHerdmark")]
    [InlineData("IsDealer")]
    [InlineData("Aho")]
    public async Task OnGet_AnyFilter_Searches(string field)
    {
        Returns(Rows(2));
        var model = CreateModel();
        switch (field)
        {
            case "Cphh": model.Filter.Cphh = "26/001"; break;
            case "OwnerName": model.Filter.OwnerName = "x"; break;
            case "Address": model.Filter.Address = "x"; break;
            case "County": model.Filter.County = "x"; break;
            case "Herdmark": model.Filter.Herdmark = "x"; break;
            case "NumericHerdmark": model.Filter.NumericHerdmark = "123456"; break;
            case "IsDealer": model.Filter.IsDealer = false; break;
            case "Aho": model.Filter.Aho = "x"; break;
        }

        await model.OnGetAsync();

        model.Filter.HasSearched.Should().BeTrue();
        model.Filter.Results.Should().HaveCount(2);
        model.NoCriteria.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_BuildsQueryFromFilter_AndClampsPage()
    {
        Returns(Rows(11));
        var model = CreateModel();
        model.Filter.Cphh = "26/001/0001";
        model.Filter.OwnerName = "Smith";
        model.Filter.PageNumber = 9;

        await model.OnGetAsync();

        await _search.Received(1).SearchFarmsAsync(
            Arg.Is<FarmSearchQuery>(q => q.Cphh == "260010001" && q.OwnerName == "Smith" && q.Address == ""),
            Arg.Any<CancellationToken>());
        model.Filter.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task OnGet_PageNumberBelowOne_IsClamped()
    {
        Returns(Rows(1));
        var model = CreateModel();
        model.Filter.Cphh = "26";
        model.Filter.PageNumber = 0;

        await model.OnGetAsync();

        model.Filter.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Export_NoFilter_Redirects()
    {
        var result = await CreateModel().OnGetExportAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Export_WithFilter_ReturnsSortedWorkbook()
    {
        Returns(Rows(3));
        var model = CreateModel();
        model.Filter.OwnerName = "Owner";
        model.Filter.SortColumn = "casescount";
        model.Filter.SortDesc = true;

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("farmsearchresults.xlsx");
        using var wb = new XLWorkbook(new MemoryStream(file.FileContents));
        var ws = wb.Worksheet("Results");
        ws.Cell(1, 1).GetString().Should().Be("CPHH");
        ws.Cell(2, 1).GetString().Should().Be("26/001/0003");
        ws.LastRowUsed()!.RowNumber().Should().Be(4);
    }
}
