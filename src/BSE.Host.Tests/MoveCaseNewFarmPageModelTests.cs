using System.Text.Json;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages;
using BSE.Host.Pages.Farm;
using BSE.Host.Services;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;

namespace BSE.Host.Tests;

public sealed class MoveCaseNewFarmPageModelTests
{
    private const string Rbse = "002600001";
    private const string OldCphh = "01001000101";
    private const string NewCphh = "01001000202";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IGeoLookupService _geo = Substitute.For<IGeoLookupService>();

    public MoveCaseNewFarmPageModelTests()
    {
        _user.GetUserIdAsync().Returns(7);
        _lookups.GetLookupAsync(default).ReturnsForAnyArgs(new List<LookupItem>());
        _lookups.GetHerdTypesAsync().Returns(new List<LuHerdType>());
        _lookups.GetAuthoritiesByCountyAsync(default).ReturnsForAnyArgs(new List<LuAuthority> { new() { Id = 3, Name = "Auth" } });
        _lookups.GetADNSRegionsByAuthorityAsync(default).ReturnsForAnyArgs(new List<LuADNSRegion> { new() { Id = 5, Name = "Reg" } });
    }

    private MoveCaseNewFarmModel CreateModel()
    {
        var http = new DefaultHttpContext();
        var model = new MoveCaseNewFarmModel(_cases, _farms, _user, _lookups, _geo)
        {
            PageContext = new PageContext
            {
                HttpContext = http,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
        return model;
    }

    private static void FillValid(MoveCaseNewFarmModel model)
    {
        model.Farm = new FarmEditViewModel
        {
            CPHH = NewCphh,
            OwnerName = "Owner",
            Address1 = "1 Road",
            AHO = "AHO1",
            Parish = "Parish",
            County = "County",
            AuthorityCountyID = 2,
            AuthorityID = 3,
            ADNSRegionID = 5,
            HerdType = "H",
            PedigreeType = "P",
            IsDealer = true
        };
    }

    private static string Json(IActionResult r) => JsonSerializer.Serialize(((JsonResult)r).Value);

    [Fact]
    public async Task OnGet_PrepopulatesFromCaseFarm()
    {
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = OldCphh });
        _farms.GetByCphhAsync(OldCphh).Returns(new FarmRecord
        {
            CPHH = OldCphh, OwnerName = "Old Owner", Address1 = "A1", Parish = "P", County = "C", AHO = "AH",
            AuthorityCountyID = 2, AuthorityID = 3, ADNSRegionID = 5
        });
        var model = CreateModel();
        model.ReturnRbse = "00/26/00001";

        var result = await model.OnGetAsync("01/001/0002/02");

        result.Should().BeOfType<PageResult>();
        model.Farm.OwnerName.Should().Be("Old Owner");
        model.Farm.ADNSRegionID.Should().Be(5);
        model.Farm.CPHH.Should().Be(NewCphh);
        model.ViewData["AuthorityOptions"].Should().BeAssignableTo<IEnumerable<LuAuthority>>().Which.Should().ContainSingle();
        model.ViewData["AdnsOptions"].Should().BeAssignableTo<IEnumerable<LuADNSRegion>>().Which.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGet_ReturnCphhTakesPrecedenceOverQueryCphh()
    {
        var model = CreateModel();
        model.ReturnCphh = NewCphh;

        await model.OnGetAsync("99999999999");

        model.Farm.CPHH.Should().Be(NewCphh);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("123")]
    public async Task OnGet_BadReturnRbse_SkipsPrepopulate(string? rbse)
    {
        var model = CreateModel();
        model.ReturnRbse = rbse;

        await model.OnGetAsync();

        await _cases.DidNotReceiveWithAnyArgs().GetCaseAsync(default!);
        model.ViewData["AuthorityOptions"].Should().BeAssignableTo<IEnumerable<LuAuthority>>().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_CaseWithoutCphhOrFarm_SkipsPrepopulate()
    {
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "" });
        var model = CreateModel();
        model.ReturnRbse = Rbse;

        await model.OnGetAsync();
        await _farms.DidNotReceiveWithAnyArgs().GetByCphhAsync(default!);

        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = OldCphh });
        _farms.GetByCphhAsync(OldCphh).Returns((FarmRecord?)null);
        await model.OnGetAsync();

        model.Farm.OwnerName.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_UnknownCase_SkipsPrepopulate()
    {
        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        var model = CreateModel();
        model.ReturnRbse = Rbse;

        await model.OnGetAsync();

        await _farms.DidNotReceiveWithAnyArgs().GetByCphhAsync(default!);
    }

    [Fact]
    public async Task OnPost_EmptyForm_ReportsAllMandatoryErrors()
    {
        var model = CreateModel();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain([
            "Farm.CPHH", "Farm.OwnerName", "Farm.Address1", "Farm.AHO", "Farm.Parish", "Farm.County", "Farm.ADNSRegionID"]);
        await _farms.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task OnPost_AdnsRegionNotInAuthorityList_Rejected()
    {
        var model = CreateModel();
        FillValid(model);
        model.Farm.ADNSRegionID = 99;

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState["Farm.ADNSRegionID"]!.Errors.Should().NotBeEmpty();
        await _farms.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task OnPost_ExistingFarm_RejectedAsDuplicate()
    {
        _farms.GetByCphhAsync(NewCphh).Returns(new FarmRecord { CPHH = NewCphh });
        var model = CreateModel();
        FillValid(model);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState["Farm.CPHH"]!.Errors.Single().ErrorMessage.Should().Be("A farm with this CPHH already exists.");
        await _farms.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task OnPost_SaveThrows_AddsGeneralError()
    {
        _farms.AddAsync(default!, default).ReturnsForAnyArgs(_ => throw new InvalidOperationException("db"));
        var model = CreateModel();
        FillValid(model);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Contain("Unable to save farm details");
    }

    [Fact]
    public async Task OnPost_WithReturnRbse_AddsFarmAndReturnsToMoveCase()
    {
        AddFarmCommand? captured = null;
        await _farms.AddAsync(Arg.Do<AddFarmCommand>(c => captured = c), 7);
        var model = CreateModel();
        FillValid(model);
        model.ReturnRbse = Rbse;

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/MoveCase");
        redirect.RouteValues!["newCphh"].Should().Be(NewCphh);
        captured.Should().NotBeNull();
        captured!.HerdType.Should().BeNull();
        captured.PedigreeType.Should().BeNull();
        captured.IsDealer.Should().BeFalse();
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("New farm created");
        model.TempData[MaintenanceConfirmationModel.SummaryKey]!.ToString().Should().Contain("was created");
    }

    [Fact]
    public async Task OnPost_WithoutReturnRbse_RedirectsToFarmDetails()
    {
        var model = CreateModel();
        FillValid(model);

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Farm/Details");
        redirect.RouteValues!["cphh"].Should().Be(NewCphh);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task OnGetAuthorities_NoCounty_ReturnsEmptyArray(int? id)
    {
        var result = await CreateModel().OnGetAuthoritiesAsync(id);

        Json(result).Should().Be("[]");
        await _lookups.DidNotReceiveWithAnyArgs().GetAuthoritiesByCountyAsync(default);
    }

    [Fact]
    public async Task OnGetAuthorities_ReturnsIdAndName()
    {
        var result = await CreateModel().OnGetAuthoritiesAsync(2);

        Json(result).Should().Be("[{\"id\":3,\"name\":\"Auth\"}]");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task OnGetAdnsRegions_NoAuthority_ReturnsEmptyArray(int? id)
    {
        var result = await CreateModel().OnGetAdnsRegionsAsync(id);

        Json(result).Should().Be("[]");
    }

    [Fact]
    public async Task OnGetAdnsRegions_ReturnsIdAndName()
    {
        var result = await CreateModel().OnGetAdnsRegionsAsync(3);

        Json(result).Should().Be("[{\"id\":5,\"name\":\"Reg\"}]");
    }

    [Fact]
    public async Task OnGetEstimateMapReference_ShortCphh_ReturnsError()
    {
        var result = await CreateModel().OnGetEstimateMapReferenceAsync("01");

        Json(result).Should().Contain("at least 5 characters");
        await _geo.DidNotReceiveWithAnyArgs().EstimateMapReferenceAsync(default!);
    }

    [Fact]
    public async Task OnGetEstimateMapReference_ServiceError_ReturnsError()
    {
        _geo.EstimateMapReferenceAsync(NewCphh).Returns(new MapReferenceEstimate(null, null, null, "no data"));

        var result = await CreateModel().OnGetEstimateMapReferenceAsync(NewCphh);

        Json(result).Should().Contain("no data");
    }

    [Fact]
    public async Task OnGetEstimateMapReference_Success_ReturnsParts()
    {
        _geo.EstimateMapReferenceAsync(NewCphh).Returns(new MapReferenceEstimate("SU", "123", "456", null));

        var result = await CreateModel().OnGetEstimateMapReferenceAsync(NewCphh);

        Json(result).Should().Be("{\"mapRef1\":\"SU\",\"mapRef2\":\"123\",\"mapRef3\":\"456\"}");
    }

    [Theory]
    [InlineData("01", "SU123456")]
    [InlineData(NewCphh, "SU12")]
    [InlineData(NewCphh, null)]
    public async Task OnGetValidateMapReference_TooShort_IsValidWithoutLookup(string cphh, string? mapRef)
    {
        var result = await CreateModel().OnGetValidateMapReferenceAsync(cphh, mapRef);

        Json(result).Should().Be("{\"valid\":true}");
        await _geo.DidNotReceiveWithAnyArgs().IsMapReferenceWithinParishAsync(default!, default!);
    }

    [Fact]
    public async Task OnGetValidateMapReference_OutsideParish_ReturnsMessage()
    {
        _geo.IsMapReferenceWithinParishAsync(NewCphh, "SU123456").Returns(false);

        var result = await CreateModel().OnGetValidateMapReferenceAsync(NewCphh, " su123456 ");

        Json(result).Should().Contain("\"valid\":false").And.Contain("parish boundaries");
    }

    [Fact]
    public async Task OnGetValidateMapReference_InsideParish_IsValid()
    {
        _geo.IsMapReferenceWithinParishAsync(NewCphh, "SU123456").Returns(true);

        var result = await CreateModel().OnGetValidateMapReferenceAsync(NewCphh, "SU123456");

        Json(result).Should().Be("{\"valid\":true}");
    }

    [Fact]
    public void DisplayRbse_FormatsReturnRbse()
    {
        var model = CreateModel();
        model.ReturnRbse = "26/1";

        model.DisplayRbse.Should().Be("00/26/00001");
    }
}
