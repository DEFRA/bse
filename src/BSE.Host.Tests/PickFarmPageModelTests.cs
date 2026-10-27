using BSE.Host.Pages.Case;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class PickFarmPageModelTests
{
    private const string Rbse = "002600001";

    private readonly IFarmService _farms = Substitute.For<IFarmService>();

    private PickFarmModel CreateModel(string[]? roles = null, string rbse = Rbse, string cphh = "01/001/0001/01")
    {
        var model = new PickFarmModel(_farms) { Rbse = rbse, Cphh = cphh };
        PageTestContext.Attach(model, roles ?? ["DataEntry"]);
        return model;
    }

    private static List<FarmSummaryRecord> Farms(int n) =>
        Enumerable.Range(1, n).Select(i => new FarmSummaryRecord { CPHH = $"C{i:00}", OwnerName = $"O{n - i:00}", Address1 = $"A{i:00}" }).ToList();

    private static void AssertHome(IActionResult r) =>
        r.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");

    [Theory]
    [InlineData("DEFRAAccess")]
    [InlineData("VLAAccess")]
    public async Task AllHandlers_RestrictedRole_RedirectHome(string role)
    {
        AssertHome(await CreateModel([role]).OnGetAsync());
        AssertHome(await CreateModel([role]).OnPostUseSelectedAsync("x"));
        AssertHome(CreateModel([role]).OnPostNewAsync());
        AssertHome(CreateModel([role]).OnPostCancelAsync());
    }

    [Fact]
    public async Task Access_DefraViewerWithDataEntry_IsAllowed()
    {
        _farms.GetByCphAsync(Arg.Any<string>()).Returns(Farms(1));

        (await CreateModel(["DEFRAAccess", "DataEntry"]).OnGetAsync()).Should().BeOfType<PageResult>();
        (await CreateModel(["VLAAccess", "VLAMaintenance"]).OnGetAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task AllHandlers_BlankRbse_RedirectHome()
    {
        AssertHome(await CreateModel(rbse: "").OnGetAsync());
        AssertHome(await CreateModel(rbse: "").OnPostUseSelectedAsync("x"));
        AssertHome(CreateModel(rbse: "").OnPostNewAsync());
        AssertHome(CreateModel(rbse: "").OnPostCancelAsync());
    }

    [Fact]
    public async Task OnGet_LongCphh_SearchesNineCharPrefix()
    {
        _farms.GetByCphAsync("010010001").Returns(Farms(2));
        var model = CreateModel();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Cphh.Should().Be("01001000101");
        model.FormattedRequestedCphh.Should().Be("01/001/0001/01");
        model.TotalFarmCount.Should().Be(2);
    }

    [Fact]
    public async Task OnGet_ShortCphh_UsesWholeValueAsPrefix()
    {
        _farms.GetByCphAsync("01001").Returns(Farms(1));
        var model = CreateModel(cphh: "01/001");

        await model.OnGetAsync();

        model.Farms.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGet_Paging_ClampsAndSlices()
    {
        _farms.GetByCphAsync(Arg.Any<string>()).Returns(Farms(23));
        var model = CreateModel();
        model.PageNumber = 50;

        await model.OnGetAsync();

        model.TotalPages.Should().Be(3);
        model.PageNumber.Should().Be(3);
        model.Farms.Should().HaveCount(3);
        model.Farms[0].CPHH.Should().Be("C21");
    }

    [Fact]
    public async Task OnGet_NoResults_SingleEmptyPage()
    {
        _farms.GetByCphAsync(Arg.Any<string>()).Returns(new List<FarmSummaryRecord>());
        var model = CreateModel();
        model.PageNumber = 0;

        await model.OnGetAsync();

        model.TotalPages.Should().Be(1);
        model.PageNumber.Should().Be(1);
        model.Farms.Should().BeEmpty();
    }

    [Theory]
    [InlineData("CPHH", false, "C01")]
    [InlineData("CPHH", true, "C03")]
    [InlineData("OwnerName", false, "C03")]
    [InlineData("OwnerName", true, "C01")]
    [InlineData("Address1", false, "C01")]
    [InlineData("Address1", true, "C03")]
    [InlineData("Unknown", true, "C01")]
    public async Task OnGet_Sorting_OrdersRows(string column, bool desc, string first)
    {
        _farms.GetByCphAsync(Arg.Any<string>()).Returns(Farms(3));
        var model = CreateModel();
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.Farms[0].CPHH.Should().Be(first);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public async Task UseSelected_NoUsableSelection_ReRendersList(string? selected)
    {
        _farms.GetByCphAsync(Arg.Any<string>()).Returns(Farms(2));
        var model = CreateModel();

        (await model.OnPostUseSelectedAsync(selected)).Should().BeOfType<PageResult>();

        model.Farms.Should().HaveCount(2);
        model.FormattedRequestedCphh.Should().Be("01/001/0001/01");
    }

    [Fact]
    public async Task UseSelected_Valid_RedirectsToCaseFarm()
    {
        var result = await CreateModel().OnPostUseSelectedAsync("01/001/0002/02");

        var r = result.Should().BeOfType<RedirectToPageResult>().Subject;
        r.PageName.Should().Be("/Case/Farm");
        r.RouteValues!["rbse"].Should().Be(Rbse);
        r.RouteValues["selectedCphh"].Should().Be("01001000202");
    }

    [Fact]
    public void New_RedirectsToFarmNewWithReturnContext()
    {
        var r = CreateModel().OnPostNewAsync().Should().BeOfType<RedirectToPageResult>().Subject;

        r.PageName.Should().Be("/Farm/New");
        r.RouteValues!["returnRbse"].Should().Be(Rbse);
        r.RouteValues["returnCphh"].Should().Be("01001000101");
        r.RouteValues["returnToCaseFarm"].Should().Be(true);
    }

    [Fact]
    public void Cancel_RedirectsToCaseFarmWithNewCphh()
    {
        var r = CreateModel().OnPostCancelAsync().Should().BeOfType<RedirectToPageResult>().Subject;

        r.PageName.Should().Be("/Case/Farm");
        r.RouteValues!["newCphh"].Should().Be("01001000101");
    }
}
