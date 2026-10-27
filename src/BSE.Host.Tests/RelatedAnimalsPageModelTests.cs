using BSE.Host.Pages.Search;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class RelatedAnimalsPageModelTests
{
    private readonly ICaseSearchService _search = Substitute.For<ICaseSearchService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    public RelatedAnimalsPageModelTests()
    {
        _lookups.GetLookupAsync(default).ReturnsForAnyArgs(new List<LookupItem> { new() { Code = "TWIN", Description = "Twin" } });
        _search.GetRelatedAnimalsAsync(default!, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(new List<RelatedAnimalResult>());
    }

    private RelatedAnimalsModel CreateModel(string query = "")
    {
        var model = new RelatedAnimalsModel(_search, _lookups);
        var http = new DefaultHttpContext();
        if (query.Length > 0) http.Request.QueryString = new QueryString(query);
        model.PageContext = new PageContext { HttpContext = http };
        return model;
    }

    private static RelatedAnimalResult Row(string rbse, string cphh = "C", string? type = "DAM", DateTime? left = null) =>
        new(rbse, cphh, type, "F", "ET", "01/01/2020", "Alive", left, "Name", "REt", "000000001");

    private void SetRows(params RelatedAnimalResult[] rows) =>
        _search.GetRelatedAnimalsAsync(default!, default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(rows.ToList());

    [Fact]
    public async Task OnGet_LoadsRelationTypeOptionsWithDamAndSireFirst()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.RelationTypeOptions.Select(o => o.Code).Should().Equal("DAM", "SIRE", "TWIN");
    }

    [Fact]
    public async Task OnGet_NoFilterNoQuery_NoCriteriaFalse()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.NoCriteria.Should().BeFalse();
        model.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().GetRelatedAnimalsAsync(default!, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task OnGet_SearchedWithoutFilter_FlagsNoCriteria()
    {
        var model = CreateModel();
        model.Searched = true;

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_QueryStringWithoutFilter_FlagsNoCriteria()
    {
        var model = CreateModel("?PageNumber=1");

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_InvalidModelState_SkipsSearch()
    {
        var model = CreateModel();
        model.Rbse = "bad";
        model.ModelState.AddModelError(nameof(model.Rbse), "bad");

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().GetRelatedAnimalsAsync(default!, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task OnGet_WithFilters_PadsRbseAndSearches()
    {
        SetRows(Row("002600001"));
        var model = CreateModel();
        model.Rbse = "26/1";
        model.RelationRbse = "27/2";
        model.Name = "N";
        model.Eartag = "E";
        model.RelationType = "DAM";

        await model.OnGetAsync();

        model.Rbse.Should().Be("00/26/00001");
        model.RelationRbse.Should().Be("00/27/00002");
        model.HasSearched.Should().BeTrue();
        model.Results.Should().HaveCount(1);
        await _search.Received(1).GetRelatedAnimalsAsync("002600001", "N", "E", "002700002", "DAM", default);
    }

    [Fact]
    public async Task OnGet_NullTextFilters_PassedAsEmptyStrings()
    {
        var model = CreateModel();
        model.RelationType = "SIRE";

        await model.OnGetAsync();

        await _search.Received(1).GetRelatedAnimalsAsync("", "", "", "", "SIRE", default);
    }

    [Fact]
    public async Task OnGet_PageNumberBelowOne_ClampedToOne()
    {
        var model = CreateModel();
        model.Name = "x";
        model.PageNumber = -3;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_PageNumberBeyondLast_ClampedToTotalPages()
    {
        SetRows(Enumerable.Range(1, 25).Select(i => Row(i.ToString("D9"))).ToArray());
        var model = CreateModel();
        model.Name = "x";
        model.PageNumber = 99;

        await model.OnGetAsync();

        model.TotalCount.Should().Be(25);
        model.TotalPages.Should().Be(3);
        model.PageNumber.Should().Be(3);
        model.GetPagedResults().Should().HaveCount(5);
    }

    [Fact]
    public void TotalPages_NoResults_IsOne()
    {
        CreateModel().TotalPages.Should().Be(1);
    }

    [Theory]
    [InlineData("rbse", false, "000000001")]
    [InlineData("rbse", true, "000000003")]
    [InlineData("cphh", false, "000000002")]
    [InlineData("cphh", true, "000000003")]
    [InlineData("relationtype", false, "000000003")]
    [InlineData("relationtype", true, "000000001")]
    [InlineData("relsex", false, "000000001")]
    [InlineData("relsex", true, "000000002")]
    [InlineData("eartag", false, "000000002")]
    [InlineData("eartag", true, "000000003")]
    [InlineData("relbirthdate", false, "000000003")]
    [InlineData("relbirthdate", true, "000000002")]
    [InlineData("relfate", false, "000000001")]
    [InlineData("relfate", true, "000000003")]
    [InlineData("leftdate", false, "000000002")]
    [InlineData("leftdate", true, "000000003")]
    [InlineData("relname", false, "000000003")]
    [InlineData("relname", true, "000000001")]
    [InlineData("releartag", false, "000000002")]
    [InlineData("releartag", true, "000000003")]
    [InlineData("relationrbse", false, "000000001")]
    [InlineData("relationrbse", true, "000000002")]
    [InlineData("unknown", false, "000000001")]
    [InlineData("RBSE", true, "000000003")]
    public async Task GetPagedResults_SortsByColumn(string column, bool desc, string expectedFirst)
    {
        SetRows(
            new RelatedAnimalResult("000000001", "B", "Z", "A", "B", "2", "A", new DateTime(2020, 2, 1), "Z", "B", "A"),
            new RelatedAnimalResult("000000002", "A", "Y", "Z", "A", "3", "B", new DateTime(2020, 1, 1), "Y", "A", "Z"),
            new RelatedAnimalResult("000000003", "C", "X", "B", "C", "1", "C", new DateTime(2020, 3, 1), "X", "C", "B"));
        var model = CreateModel();
        model.Name = "x";
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.GetPagedResults().First().Rbse.Should().Be(expectedFirst);
    }

    [Fact]
    public async Task OnGetExport_NoFilter_RedirectsToPage()
    {
        var model = CreateModel();

        var result = await model.OnGetExportAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _search.DidNotReceiveWithAnyArgs().GetRelatedAnimalsAsync(default!, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task OnGetExport_WithFilter_ReturnsXlsxWithRows()
    {
        SetRows(Row("000000002", left: new DateTime(2021, 5, 4, 3, 2, 1)), Row("000000001"));
        var model = CreateModel();
        model.Rbse = "000000001";

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("relatedanimalsearchresults.xlsx");
        file.ContentType.Should().Contain("spreadsheetml");
        using var wb = new ClosedXML.Excel.XLWorkbook(new MemoryStream(file.FileContents));
        var ws = wb.Worksheet("Results");
        ws.Cell(1, 1).GetString().Should().Be("RBSE");
        ws.Cell(2, 1).GetString().Should().Be("000000001");
        ws.Cell(3, 8).GetString().Should().Be("04/05/2021 03:02:01");
    }
}
