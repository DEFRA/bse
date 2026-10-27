using BSE.Host.Pages.Farm;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RefGeo = BSE.Modules.ReferenceData.Services.IGeoLookupService;
using HostGeo = BSE.Host.Services.IGeoLookupService;

namespace BSE.Host.Tests;

public class FarmNewModelTests
{
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly RefGeo _geo = Substitute.For<RefGeo>();
    private readonly HostGeo _mapGeo = Substitute.For<HostGeo>();

    private NewModel CreateModel()
    {
        _lookups.GetLookupAsync(Arg.Any<BSE.SharedKernel.LookupTableId>())
            .Returns(Task.FromResult<IEnumerable<LookupItem>>([]));
        _lookups.GetHerdTypesAsync().Returns(Task.FromResult<IEnumerable<LuHerdType>>([]));
        _lookups.GetAuthoritiesByCountyAsync(Arg.Any<int>())
            .Returns(Task.FromResult<IEnumerable<LuAuthority>>([new LuAuthority { Id = 5, Name = "Auth" }]));
        _lookups.GetADNSRegionsByAuthorityAsync(Arg.Any<int>())
            .Returns(Task.FromResult<IEnumerable<LuADNSRegion>>([new LuADNSRegion { Id = 9, Name = "Reg" }]));
        _farms.GetVetnetDetailsAsync(Arg.Any<string>()).Returns(Task.FromResult<IEnumerable<VetnetRecord>>([]));
        _farms.GetByCphhAsync(Arg.Any<string>()).Returns(Task.FromResult<FarmRecord?>(null));
        _user.GetUserIdAsync().Returns(7);

        var http = new DefaultHttpContext();
        return new NewModel(_farms, _user, _lookups, _geo, _mapGeo)
        {
            PageContext = new PageContext
            {
                HttpContext = http,
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                    new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                    new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
            },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task OnGet_NormalizesCphhAndLoadsLookups()
    {
        var model = CreateModel();

        var result = await model.OnGetAsync("01/234/5678/90");

        result.Should().BeOfType<PageResult>();
        model.Farm.CPHH.Should().Be("01234567890");
        model.ViewData["AuthorityOptions"].Should().BeAssignableTo<IEnumerable<LuAuthority>>().Which.Should().BeEmpty();
        model.ViewData["AdnsOptions"].Should().BeAssignableTo<IEnumerable<LuADNSRegion>>().Which.Should().BeEmpty();
        await _farms.DidNotReceive().GetVetnetDetailsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task OnGet_ReturnToCaseFarm_VetnetFoundAndParishDerived()
    {
        var model = CreateModel();
        model.ReturnToCaseFarm = true;
        model.ReturnCphh = "01/234/5678/90";
        _farms.GetVetnetDetailsAsync("01234567890").Returns(Task.FromResult<IEnumerable<VetnetRecord>>(
            [new VetnetRecord { CPHH = "01234567890" }, new VetnetRecord { Herdmark = "AB", NumericHerdmark = "12" }]));
        _geo.GetParishAsync("01", "234").Returns(Task.FromResult<ParishLookup?>(
            new ParishLookup { Name = "P", BSECounty = "C", ADNSRegionID = 3 }));

        await model.OnGetAsync();

        model.VetnetFound.Should().BeTrue();
        model.VetnetHerdmark.Should().Be("AB");
        model.ShowBrusselsWarning.Should().BeFalse();
        model.VetnetMessage.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_ReturnToCaseFarm_NothingFound_SetsMessageAndWarning()
    {
        var model = CreateModel();
        model.ReturnToCaseFarm = true;

        await model.OnGetAsync("01234567890");

        model.VetnetMessage.Should().Contain("not Found on Vetnet");
        model.VetnetFound.Should().BeFalse();
        model.ShowBrusselsWarning.Should().BeTrue();
        model.BrusselsWarningText.Should().Contain("Brussels");
    }

    [Fact]
    public async Task OnGet_ReturnToCaseFarm_BlankCphh_SetsNotFoundMessage()
    {
        var model = CreateModel();
        model.ReturnToCaseFarm = true;

        await model.OnGetAsync(null);

        model.VetnetMessage.Should().NotBeNull();
        await _farms.DidNotReceive().GetVetnetDetailsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task OnPost_ShortCphh_AddsModelError()
    {
        var model = CreateModel();
        model.Farm.CPHH = "123";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState["Farm.CPHH"]!.Errors.Should().ContainSingle();
        await _farms.DidNotReceive().AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>());
    }

    [Fact]
    public async Task OnPost_ExistingFarm_AddsAlreadyExistsError()
    {
        var model = CreateModel();
        model.Farm.CPHH = "01/234/5678/90";
        _farms.GetByCphhAsync("01234567890").Returns(Task.FromResult<FarmRecord?>(new FarmRecord { CPHH = "01234567890" }));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState["Farm.CPHH"]!.Errors[0].ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task OnPost_SaveThrows_AddsGenericError()
    {
        var model = CreateModel();
        model.Farm.CPHH = "01234567890";
        _farms.AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>()).Returns<Task>(_ => throw new InvalidOperationException());

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors.Should().ContainSingle();
        model.TempData.ContainsKey("Success").Should().BeFalse();
    }

    [Fact]
    public async Task OnPost_Success_RedirectsToDetails()
    {
        var model = CreateModel();
        model.Farm.CPHH = "01234567890";

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Farm/Details");
        redirect.RouteValues!["cphh"].Should().Be("01234567890");
        model.TempData["Success"].Should().Be("Farm 01234567890 has been created.");
        await _farms.Received(1).AddAsync(Arg.Is<AddFarmCommand>(c => c.CPHH == "01234567890"), 7);
    }

    [Fact]
    public async Task OnPost_SuccessWithReturnRbse_RedirectsToCaseFarmWithNewCphh()
    {
        var model = CreateModel();
        model.Farm.CPHH = "01234567890";
        model.ReturnRbse = "R1";
        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues!["newCphh"].Should().Be("01234567890");
        redirect.RouteValues["forceNewFarmDetails"].Should().Be(true);
        await _farms.DidNotReceive().AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>());
    }

    [Fact]
    public async Task OnPost_PickFarmJourney_ExistingFarm_RedirectsWithSelectedCphh()
    {
        var model = CreateModel();
        model.ReturnToCaseFarm = true;
        model.ReturnRbse = "R1";
        model.ReturnCphh = "01234567890";
        _farms.GetByCphhAsync("01234567890").Returns(Task.FromResult<FarmRecord?>(new FarmRecord { CPHH = "01234567890" }));

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues!["selectedCphh"].Should().Be("01234567890");
    }

    [Fact]
    public async Task OnPost_PickFarmJourney_AcceptVetnet_SeedsHerdmarks()
    {
        var model = CreateModel();
        model.ReturnToCaseFarm = true;
        model.ReturnRbse = "R1";
        model.ReturnCphh = "01234567890";
        model.AcceptVetnetDetails = true;
        _farms.GetVetnetDetailsAsync("01234567890").Returns(Task.FromResult<IEnumerable<VetnetRecord>>(
            [new VetnetRecord { Herdmark = "AB", NumericHerdmark = "12" }]));
        _geo.GetParishAsync("01", "234").Returns(Task.FromResult<ParishLookup?>(
            new ParishLookup { Name = "P", BSECounty = "C", ADNSRegionID = 3, AuthorityID = 4, AuthorityCountyID = 5 }));

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.RouteValues!["seedHerdmark1"].Should().Be("AB");
        redirect.RouteValues["seedNumericHerdmark1"].Should().Be("12");
        redirect.RouteValues["seedParish"].Should().Be("P");
        redirect.RouteValues["seedAdnsRegionId"].Should().Be(3);
    }

    [Fact]
    public async Task OnPost_PickFarmJourneyWithoutRbse_RedirectsHome()
    {
        var model = CreateModel();
        model.ReturnToCaseFarm = true;
        model.Farm.CPHH = "01234567890";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnPost_WithAuthorityIds_LoadsDependentLookups()
    {
        var model = CreateModel();
        model.Farm.CPHH = "12";
        model.Farm.AuthorityCountyID = 2;
        model.Farm.AuthorityID = 5;

        await model.OnPostAsync();

        model.ViewData["AuthorityOptions"].Should().BeAssignableTo<IEnumerable<LuAuthority>>().Which.Should().ContainSingle();
        model.ViewData["AdnsOptions"].Should().BeAssignableTo<IEnumerable<LuADNSRegion>>().Which.Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task OnGetAuthorities_NoCounty_ReturnsEmpty(int? id)
    {
        var model = CreateModel();

        var result = await model.OnGetAuthoritiesAsync(id);

        ((Array)result.Should().BeOfType<JsonResult>().Subject.Value!).Length.Should().Be(0);
    }

    [Fact]
    public async Task OnGetAuthorities_ReturnsMappedItems()
    {
        var model = CreateModel();

        var result = await model.OnGetAuthoritiesAsync(2);

        var items = result.Should().BeOfType<JsonResult>().Subject.Value.Should().BeAssignableTo<System.Collections.IEnumerable>().Subject;
        items.Cast<object>().Should().ContainSingle();
        await _lookups.Received(1).GetAuthoritiesByCountyAsync(2);
    }

    [Fact]
    public async Task OnGetAdnsRegions_Branches()
    {
        var model = CreateModel();

        var empty = await model.OnGetAdnsRegionsAsync(0);
        var full = await model.OnGetAdnsRegionsAsync(4);

        ((Array)empty.Should().BeOfType<JsonResult>().Subject.Value!).Length.Should().Be(0);
        ((System.Collections.IEnumerable)((JsonResult)full).Value!).Cast<object>().Should().ContainSingle();
    }

    [Fact]
    public async Task OnGetEstimateMapReference_ShortCphh_ReturnsError()
    {
        var model = CreateModel();

        var result = await model.OnGetEstimateMapReferenceAsync("12");

        result.Should().BeOfType<JsonResult>().Which.Value!.ToString().Should().Contain("at least 5");
        await _mapGeo.DidNotReceive().EstimateMapReferenceAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task OnGetEstimateMapReference_ServiceError_ReturnsError()
    {
        var model = CreateModel();
        _mapGeo.EstimateMapReferenceAsync("01234").Returns(new MapReferenceEstimate(null, null, null, "boom"));

        var result = await model.OnGetEstimateMapReferenceAsync("01/234");

        result.Should().BeOfType<JsonResult>().Which.Value!.ToString().Should().Contain("boom");
    }

    [Fact]
    public async Task OnGetEstimateMapReference_Success_ReturnsRefs()
    {
        var model = CreateModel();
        _mapGeo.EstimateMapReferenceAsync("01234").Returns(new MapReferenceEstimate("SU", "123", "456", null));

        var result = await model.OnGetEstimateMapReferenceAsync("01234");

        var text = result.Should().BeOfType<JsonResult>().Subject.Value!.ToString();
        text.Should().Contain("SU").And.Contain("123").And.Contain("456");
    }

    [Fact]
    public async Task OnGetValidateMapReference_ShortInputs_AreValidWithoutLookup()
    {
        var model = CreateModel();

        var result = await model.OnGetValidateMapReferenceAsync("01", null);

        result.Should().BeOfType<JsonResult>().Which.Value!.ToString().Should().Contain("True");
        await _mapGeo.DidNotReceive().IsMapReferenceWithinParishAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnGetValidateMapReference_UsesLookup(bool within)
    {
        var model = CreateModel();
        _mapGeo.IsMapReferenceWithinParishAsync("01234", "SU123456").Returns(within);

        var result = await model.OnGetValidateMapReferenceAsync("01234", " su123456 ");

        var text = result.Should().BeOfType<JsonResult>().Subject.Value!.ToString();
        if (within) text.Should().NotContain("message");
        else text.Should().Contain("parish boundaries");
    }
}
