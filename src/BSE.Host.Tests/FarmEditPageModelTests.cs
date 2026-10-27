using System.Text.Json;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Farm;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;

namespace BSE.Host.Tests;

public sealed class FarmEditPageModelTests
{
    private const string Cphh = "01001000101";

    private readonly IFarmService _farm = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IGeoLookupService _geo = Substitute.For<IGeoLookupService>();

    public FarmEditPageModelTests()
    {
        _user.GetUserIdAsync().Returns(3);
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns([new LookupItem(1, "x")]);
        _lookups.GetHerdTypesAsync().Returns([new LuHerdType { Id = 1, Code = "D", Description = "Dairy" }]);
        _lookups.GetAuthoritiesByCountyAsync(5).Returns([new LuAuthority { Id = 7, Name = "Auth" }]);
        _lookups.GetADNSRegionsByAuthorityAsync(7).Returns([new LuADNSRegion { Id = 8, Name = "Region" }]);
    }

    private EditModel CreateModel()
    {
        var model = new EditModel(_farm, _user, _lookups, _geo);
        PageTestContext.Attach(model);
        return model;
    }

    private static string Json(IActionResult result) =>
        JsonSerializer.Serialize(result.Should().BeOfType<JsonResult>().Subject.Value);

    [Fact]
    public async Task OnGet_UnknownFarm_ReturnsNotFound()
    {
        _farm.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);

        (await CreateModel().OnGetAsync("01/001/0001/01")).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGet_KnownFarm_LoadsFormLookupsAndRowStamp()
    {
        _farm.GetByCphhAsync(Cphh).Returns(new FarmRecord { CPHH = Cphh, OwnerName = "Owner", RowStamp = [1, 2, 3], MapReference = "SU123456" });
        _farm.GetConfirmedCaseCountAsync(Cphh).Returns(4);
        var model = CreateModel();

        var result = await model.OnGetAsync("01/001/0001/01", "002600001");

        result.Should().BeOfType<PageResult>();
        model.Rbse.Should().Be("002600001");
        model.Farm!.OwnerName.Should().Be("Owner");
        model.Farm.MapRef1.Should().Be("SU");
        model.ConfirmedCaseCount.Should().Be(4);
        model.TempData["FarmRowStamp"].Should().Be(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        model.ViewData["CountyOptions"].Should().NotBeNull();
        model.ViewData["HerdTypeOptions"].Should().NotBeNull();
        ((IEnumerable<LuAuthority>)model.ViewData["AuthorityOptions"]!).Should().BeEmpty();
        ((IEnumerable<LuADNSRegion>)model.ViewData["AdnsOptions"]!).Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_FarmWithoutRowStampOrRbse_UsesDefaults()
    {
        _farm.GetByCphhAsync(Cphh).Returns(new FarmRecord { CPHH = Cphh });
        var model = CreateModel();

        await model.OnGetAsync(Cphh);

        model.Rbse.Should().BeEmpty();
        model.TempData["FarmRowStamp"].Should().BeNull();
    }

    [Fact]
    public async Task Post_InvalidModelState_ReturnsPageWithoutUpdating()
    {
        var model = CreateModel();
        model.Farm = new FarmEditViewModel { CPHH = "01/001/0001/01" };
        model.ModelState.AddModelError("Farm.OwnerName", "required");

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.Farm.CPHH.Should().Be(Cphh);
        await _farm.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Post_NullFarm_ReturnsPage()
    {
        (await CreateModel().OnPostAsync()).Should().BeOfType<PageResult>();

        await _farm.DidNotReceiveWithAnyArgs().GetConfirmedCaseCountAsync(default!);
    }

    [Fact]
    public async Task Post_MapReferenceOutsideParish_AddsModelError()
    {
        _geo.IsMapReferenceWithinParishAsync(Cphh, "SU123456").Returns(false);
        var model = CreateModel();
        model.Farm = new FarmEditViewModel { CPHH = Cphh, MapRef1 = "SU", MapRef2 = "123", MapRef3 = "456" };

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState["Farm.MapRef1"]!.Errors.Should().ContainSingle();
        await _farm.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Post_ShortMapReference_SkipsParishCheck()
    {
        var model = CreateModel();
        model.Farm = new FarmEditViewModel { CPHH = Cphh, MapRef1 = "SU" };

        await model.OnPostAsync();

        await _geo.DidNotReceiveWithAnyArgs().IsMapReferenceWithinParishAsync(default!, default!);
    }

    [Fact]
    public async Task Post_Valid_UpdatesWithRowStampAndRedirects()
    {
        _geo.IsMapReferenceWithinParishAsync(Cphh, "SU123456").Returns(true);
        _farm.GetConfirmedCaseCountAsync(Cphh).Returns(2);
        var model = CreateModel();
        model.Rbse = "002600001";
        model.Farm = new FarmEditViewModel { CPHH = "01/001/0001/01", OwnerName = "O", MapRef1 = "SU", MapRef2 = "123", MapRef3 = "456" };
        model.TempData["FarmRowStamp"] = Convert.ToBase64String(new byte[] { 4, 5 });

        var result = await model.OnPostAsync();

        var r = result.Should().BeOfType<RedirectToPageResult>().Subject;
        r.PageName.Should().Be("/Case/Farm");
        r.RouteValues!["rbse"].Should().Be("002600001");
        model.ConfirmedCaseCount.Should().Be(2);
        await _farm.Received(1).UpdateAsync(
            Arg.Is<UpdateFarmCommand>(c => c.CPHH == Cphh && c.OwnerName == "O" && c.RowStamp!.SequenceEqual(new byte[] { 4, 5 })), 3);
        model.TempData["Success"].Should().Be("Farm updated successfully.");
    }

    [Fact]
    public async Task Post_WithoutRowStamp_UpdatesWithNullStamp()
    {
        var model = CreateModel();
        model.Farm = new FarmEditViewModel { CPHH = Cphh, AuthorityCountyID = 5, AuthorityID = 7 };

        await model.OnPostAsync();

        await _farm.Received(1).UpdateAsync(Arg.Is<UpdateFarmCommand>(c => c.RowStamp == null), 3);
        ((IEnumerable<LuAuthority>)model.ViewData["AuthorityOptions"]!).Should().ContainSingle();
        ((IEnumerable<LuADNSRegion>)model.ViewData["AdnsOptions"]!).Should().ContainSingle();
    }

    [Fact]
    public async Task Authorities_NullOrZero_ReturnsEmptyList()
    {
        var model = CreateModel();

        Json(await model.OnGetAuthoritiesAsync(null)).Should().Be("[]");
        Json(await model.OnGetAuthoritiesAsync(0)).Should().Be("[]");
        await _lookups.DidNotReceiveWithAnyArgs().GetAuthoritiesByCountyAsync(default);
    }

    [Fact]
    public async Task Authorities_ReturnsIdAndName()
    {
        Json(await CreateModel().OnGetAuthoritiesAsync(5)).Should().Be("[{\"id\":7,\"name\":\"Auth\"}]");
    }

    [Fact]
    public async Task AdnsRegions_NullOrZero_ReturnsEmptyList()
    {
        Json(await CreateModel().OnGetAdnsRegionsAsync(null)).Should().Be("[]");
    }

    [Fact]
    public async Task AdnsRegions_ReturnsIdAndName()
    {
        Json(await CreateModel().OnGetAdnsRegionsAsync(7)).Should().Be("[{\"id\":8,\"name\":\"Region\"}]");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0100")]
    public async Task Estimate_ShortCphh_ReturnsError(string? cphh)
    {
        Json(await CreateModel().OnGetEstimateMapReferenceAsync(cphh)).Should().Contain("at least 5 characters");
        await _geo.DidNotReceiveWithAnyArgs().EstimateMapReferenceAsync(default!);
    }

    [Fact]
    public async Task Estimate_ServiceError_IsReturned()
    {
        _geo.EstimateMapReferenceAsync(Cphh).Returns(new MapReferenceEstimate(null, null, null, "nope"));

        Json(await CreateModel().OnGetEstimateMapReferenceAsync(Cphh)).Should().Be("{\"error\":\"nope\"}");
    }

    [Fact]
    public async Task Estimate_Success_ReturnsParts()
    {
        _geo.EstimateMapReferenceAsync(Cphh).Returns(new MapReferenceEstimate("SU", "125", "355", null));

        Json(await CreateModel().OnGetEstimateMapReferenceAsync(Cphh)).Should().Be("{\"mapRef1\":\"SU\",\"mapRef2\":\"125\",\"mapRef3\":\"355\"}");
    }

    [Theory]
    [InlineData(null, "SU123456")]
    [InlineData("0100", "SU123456")]
    [InlineData("01001000101", null)]
    [InlineData("01001000101", "SU12")]
    public async Task Validate_IncompleteInput_IsValidWithoutLookup(string? cphh, string? mapRef)
    {
        Json(await CreateModel().OnGetValidateMapReferenceAsync(cphh, mapRef)).Should().Be("{\"valid\":true}");
        await _geo.DidNotReceiveWithAnyArgs().IsMapReferenceWithinParishAsync(default!, default!);
    }

    [Theory]
    [InlineData(true, "\"valid\":true")]
    [InlineData(false, "\"valid\":false")]
    public async Task Validate_CompleteInput_UsesGeoLookup(bool within, string expected)
    {
        _geo.IsMapReferenceWithinParishAsync(Cphh, "SU123456").Returns(within);

        var json = Json(await CreateModel().OnGetValidateMapReferenceAsync(Cphh, "SU123456"));

        json.Should().Contain(expected);
        if (!within) json.Should().Contain("parish boundaries");
    }
}
