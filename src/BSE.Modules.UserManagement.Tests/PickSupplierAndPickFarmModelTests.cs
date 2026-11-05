using BSE.Host.Pages.Case;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using System.Security.Claims;

namespace BSE.Modules.UserManagement.Tests;

public sealed class PickSupplierModelTests
{
    private readonly ILookupRepository _lookups = Substitute.For<ILookupRepository>();
    private readonly IEditableLookupAdminService _admin = Substitute.For<IEditableLookupAdminService>();

    private PickSupplierModel Create(params string[] roles)
    {
        var model = new PickSupplierModel(_lookups, _admin) { Rbse = "16/01" };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        return model;
    }

    private static LuSupplier S(int id, string name, string? details = null) => new() { Id = id, Name = name, Details = details };

    private PickSupplierModel Authorised() => Create("DataEntry", "VLAAccess");

    [Fact]
    public async Task OnGet_WithoutAccess_RedirectsHome()
    {
        var result = await Create("DataEntry").OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToFeeds()
    {
        var model = Authorised();
        model.Rbse = "";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Feeds");
    }

    [Fact]
    public async Task OnGet_NoSearch_ListsAllSortedAndPaged()
    {
        _lookups.GetSuppliersAsync().Returns(Enumerable.Range(1, 12).Select(i => S(i, $"S{i:00}")).ToList());
        var model = Authorised();
        model.PageNumber = 99;

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.TotalCount.Should().Be(12);
        model.TotalPages.Should().Be(2);
        model.PageNumber.Should().Be(2);
        model.Suppliers.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("Name", true, "B")]
    [InlineData("Name", false, "A")]
    [InlineData("Details", true, "B")]
    [InlineData("Details", false, "A")]
    [InlineData(null, false, "A")]
    public async Task OnGet_Search_SortsByColumn(string? column, bool desc, string expectedFirst)
    {
        _lookups.GetPossibleSuppliersAsync("AB").Returns(new[] { S(1, "A", "x"), S(2, "B", "y") });
        var model = Authorised();
        model.Name = " ab ";
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.Name.Should().Be("ab");
        model.CanCreateNew.Should().BeTrue();
        model.Suppliers[0].Name.Should().Be(expectedFirst);
    }

    [Fact]
    public async Task UseSelected_WithoutAccess_RedirectsHome()
    {
        var result = await Create().OnPostUseSelectedAsync(1, null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task UseSelected_FoundInAll_RedirectsToFeedsWithSupplier()
    {
        _lookups.GetSuppliersAsync().Returns(new[] { S(5, "Acme") });

        var result = await Authorised().OnPostUseSelectedAsync(5, null);

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Feeds");
        redirect.RouteValues!["pickedSupplierId"].Should().Be(5);
        redirect.RouteValues["pickedSupplierName"].Should().Be("Acme");
    }

    [Fact]
    public async Task UseSelected_FoundInSearch_UsesPostedName()
    {
        _lookups.GetPossibleSuppliersAsync("AC").Returns(new[] { S(5, "Acme") });
        var model = Authorised();
        model.Name = "ac";

        var result = await model.OnPostUseSelectedAsync(5, "Custom");

        result.Should().BeOfType<RedirectToPageResult>()
            .Which.RouteValues!["pickedSupplierName"].Should().Be("Custom");
    }

    [Fact]
    public async Task UseSelected_NotFound_ReturnsPageWithError()
    {
        _lookups.GetSuppliersAsync().Returns(new[] { S(1, "Acme") });
        var model = Authorised();

        var result = await model.OnPostUseSelectedAsync(99, null);

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task New_WithoutAccess_RedirectsHome()
    {
        var result = await Create().OnPostNewAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task New_BlankName_ReturnsPage()
    {
        _lookups.GetSuppliersAsync().Returns(Array.Empty<LuSupplier>());
        var model = Authorised();
        model.Name = "  ";

        var result = await model.OnPostNewAsync();

        result.Should().BeOfType<PageResult>();
        await _admin.DidNotReceiveWithAnyArgs().AddSupplierAsync(default!, default);
    }

    [Fact]
    public async Task New_BlankDetails_AddsModelError()
    {
        _lookups.GetPossibleSuppliersAsync("ACME").Returns(Array.Empty<LuSupplier>());
        var model = Authorised();
        model.Name = "Acme";
        model.NewSupplierDetails = " ";

        var result = await model.OnPostNewAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(PickSupplierModel.NewSupplierDetails));
    }

    [Fact]
    public async Task New_CreatedWithMatchingDetails_RedirectsWithNewestMatch()
    {
        _lookups.GetPossibleSuppliersAsync("ACME").Returns(new[]
        {
            S(1, "Acme", "Leeds"), S(7, "ACME", "leeds"), S(9, "Acme", "Other")
        });
        var model = Authorised();
        model.Name = "Acme";
        model.NewSupplierDetails = "Leeds";

        var result = await model.OnPostNewAsync();

        await _admin.Received(1).AddSupplierAsync("Acme", "Leeds");
        result.Should().BeOfType<RedirectToPageResult>().Which.RouteValues!["pickedSupplierId"].Should().Be(7);
    }

    [Fact]
    public async Task New_NoDetailsMatch_FallsBackToNameMatch()
    {
        _lookups.GetPossibleSuppliersAsync("ACME").Returns(new[] { S(3, "Acme", "Elsewhere") });
        var model = Authorised();
        model.Name = "Acme";
        model.NewSupplierDetails = "Leeds";

        var result = await model.OnPostNewAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.RouteValues!["pickedSupplierId"].Should().Be(3);
    }

    [Fact]
    public async Task New_CreatedButNotFound_ReturnsPageWithError()
    {
        _lookups.GetPossibleSuppliersAsync(Arg.Any<string>()).Returns(Array.Empty<LuSupplier>());
        var model = Authorised();
        model.Name = "Acme";
        model.NewSupplierDetails = "Leeds";

        var result = await model.OnPostNewAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Cancel_WithoutAccess_RedirectsHome()
    {
        Create().OnPostCancel().Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public void Cancel_RedirectsToFeedsWithReset()
    {
        var redirect = Authorised().OnPostCancel().Should().BeOfType<RedirectToPageResult>().Subject;

        redirect.PageName.Should().Be("/Case/Feeds");
        redirect.RouteValues!["resetSupplier"].Should().Be(true);
    }
}

public sealed class PickFarmModelTests
{
    private readonly IFarmService _farms = Substitute.For<IFarmService>();

    private PickFarmModel Create(params string[] roles)
    {
        var model = new PickFarmModel(_farms) { Rbse = "16/01" };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        return model;
    }

    private static FarmSummaryRecord F(string cphh, string owner = "O", string addr = "A") =>
        new() { CPHH = cphh, OwnerName = owner, Address1 = addr };

    [Theory]
    [InlineData("DEFRAAccess")]
    [InlineData("VLAAccess")]
    public async Task OnGet_RestrictedRoles_RedirectHome(string role)
    {
        var result = await Create(role).OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsHome()
    {
        var model = Create("DataEntry");
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Theory]
    [InlineData("CPHH", true, "B")]
    [InlineData("CPHH", false, "A")]
    [InlineData("OwnerName", true, "B")]
    [InlineData("OwnerName", false, "A")]
    [InlineData("Address1", true, "B")]
    [InlineData("Address1", false, "A")]
    [InlineData(null, false, "A")]
    public async Task OnGet_SortsAndTrimsToNinePrefix(string? column, bool desc, string expectedOwner)
    {
        _farms.GetByCphAsync("123456789").Returns(new[]
        {
            F("11", "A", "A"), F("22", "B", "B")
        });
        var model = Create("DataEntry");
        model.Cphh = "12/345/6789/01";
        model.SortColumn = column;
        model.SortDesc = desc;

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Cphh.Should().Be("1234567890" + "1");
        model.Farms[0].OwnerName.Should().Be(expectedOwner);
        model.TotalFarmCount.Should().Be(2);
        model.FormattedRequestedCphh.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGet_ShortCphh_UsesWholeValueAndClampsPage()
    {
        _farms.GetByCphAsync("1234").Returns(new[] { F("1") });
        var model = Create("DataEntry");
        model.Cphh = "1234";
        model.PageNumber = 5;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task UseSelected_WithoutAccess_RedirectsHome()
    {
        (await Create("VLAAccess").OnPostUseSelectedAsync("1")).Should()
            .BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task UseSelected_BlankRbse_RedirectsHome()
    {
        var model = Create("DataEntry");
        model.Rbse = "";

        (await model.OnPostUseSelectedAsync("1")).Should()
            .BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public async Task UseSelected_NothingUsable_ReloadsPage(string? selected)
    {
        _farms.GetByCphAsync("1234").Returns(new[] { F("1") });
        var model = Create("DataEntry");
        model.Cphh = "1234";

        var result = await model.OnPostUseSelectedAsync(selected);

        result.Should().BeOfType<PageResult>();
        model.Farms.Should().HaveCount(1);
        model.FormattedRequestedCphh.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UseSelected_Valid_RedirectsToFarm()
    {
        var result = await Create("DataEntry").OnPostUseSelectedAsync("12/345/6789");

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues!["selectedCphh"].Should().Be("123456789");
    }

    [Fact]
    public void New_Guards()
    {
        Create("VLAAccess").OnPostNewAsync().Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");

        var blank = Create("DataEntry");
        blank.Rbse = "";
        blank.OnPostNewAsync().Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public void New_RedirectsToFarmNew()
    {
        var model = Create("DataEntry");
        model.Cphh = "12/345";

        var redirect = model.OnPostNewAsync().Should().BeOfType<RedirectToPageResult>().Subject;

        redirect.PageName.Should().Be("/Farm/New");
        redirect.RouteValues!["returnCphh"].Should().Be("12345");
    }

    [Fact]
    public void Cancel_Guards()
    {
        Create("VLAAccess").OnPostCancelAsync().Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");

        var blank = Create("DataEntry");
        blank.Rbse = "";
        blank.OnPostCancelAsync().Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public void Cancel_RedirectsToCaseFarm()
    {
        var model = Create("DataEntry");
        model.Cphh = "12/345";

        var redirect = model.OnPostCancelAsync().Should().BeOfType<RedirectToPageResult>().Subject;

        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues!["newCphh"].Should().Be("12345");
    }
}
