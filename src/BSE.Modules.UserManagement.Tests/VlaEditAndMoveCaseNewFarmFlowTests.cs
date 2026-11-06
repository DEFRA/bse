using System.Security.Claims;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages;
using BSE.Host.Pages.Case;
using BSE.Host.Pages.Farm;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;

namespace BSE.Modules.UserManagement.Tests;

public sealed class VlaEditPageFlowTests
{
    private const string Rbse = "002600001";
    private static readonly string RowStampKey = $"VlaEdit_RowStamp_{Rbse}";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();

    public VlaEditPageFlowTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns(Array.Empty<BatchNumberEntry>());
        _user.GetUserIdAsync().Returns(2);
    }

    private VlaEditModel Create(bool dataEntry = true, bool withRowStamp = true)
    {
        var model = new VlaEditModel(_cases, _user, _lookups, _batches,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "https://s" }).Build())
        { Rbse = Rbse };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(dataEntry ? [new Claim(ClaimTypes.Role, "DataEntry")] : [], "TestAuth"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        if (withRowStamp)
            model.TempData[RowStampKey] = Convert.ToBase64String([1]);
        return model;
    }

    [Fact]
    public async Task OnGet_CaseMissing_WarnsAndRedirectsHome()
    {
        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        var model = Create();

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["Warning"].Should().NotBeNull();
    }

    [Fact]
    public async Task OnGet_CaseFound_LoadsEverything()
    {
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, RowStamp = [5] });
        var model = Create();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Case.Rbse.Should().Be(Rbse);
        model.SpolSiteUrl.Should().Be("https://s");
        model.TempData[RowStampKey].Should().Be(Convert.ToBase64String([5]));
    }

    [Fact]
    public async Task OnPost_WithoutDataEntry_Forbidden()
    {
        (await Create(dataEntry: false).OnPostAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPost_NormalisesPurchaseAndSlaughterFields_ThenSaves()
    {
        _cases.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 2).Returns(EditCaseResult.Success);
        var model = Create();
        model.Case.Origin = "N";
        model.Case.PurchasedCounty = "Devon";
        model.Case.FormBDate = DateTime.Today.AddDays(-2);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.Case.PurchasedCounty.Should().BeNull();
        model.Case.SlaughterDate.Should().Be(model.Case.FormBDate);
        model.TempData["Success"].Should().Be($"Case {Rbse} has been updated.");
    }

    [Theory]
    [InlineData("birth-epoch")]
    [InlineData("birth-after-forma")]
    [InlineData("birth-future")]
    [InlineData("purchase-before-birth")]
    [InlineData("purchase-after-forma")]
    [InlineData("herd-entry-future")]
    [InlineData("onset-before-birth")]
    [InlineData("onset-after-forma")]
    [InlineData("both-months")]
    [InlineData("pregnant-range")]
    [InlineData("postcalving-range")]
    [InlineData("slaughter-future")]
    [InlineData("slaughter-before-forma")]
    [InlineData("slaughter-before-birth")]
    public async Task OnPost_DomainRuleViolations_ReturnPage(string scenario)
    {
        var model = Create();
        var today = DateTime.Today;
        model.Case.Origin = "P";
        switch (scenario)
        {
            case "birth-epoch": model.Case.BirthDate = new DateTime(1960, 1, 1); break;
            case "birth-after-forma": model.Case.FormADate = today.AddDays(-10); model.Case.BirthDate = today.AddDays(-5); break;
            case "birth-future": model.Case.BirthDate = today.AddDays(3); break;
            case "purchase-before-birth": model.Case.BirthDate = today.AddYears(-3); model.Case.PurchaseDate = today.AddYears(-4); break;
            case "purchase-after-forma": model.Case.FormADate = today.AddDays(-10); model.Case.PurchaseDate = today.AddDays(-2); break;
            case "herd-entry-future": model.Case.HerdEntryDate = today.AddDays(2); break;
            case "onset-before-birth": model.Case.BirthDate = today.AddYears(-3); model.Case.OnsetDate = today.AddYears(-4); break;
            case "onset-after-forma": model.Case.FormADate = today.AddDays(-10); model.Case.OnsetDate = today.AddDays(-2); break;
            case "both-months": model.Case.MonthsPregnant = 3; model.Case.MonthsPostCalving = 2; break;
            case "pregnant-range": model.Case.MonthsPregnant = 12; break;
            case "postcalving-range": model.Case.MonthsPostCalving = 9; break;
            case "slaughter-future": model.Case.SlaughterDate = today.AddDays(2); break;
            case "slaughter-before-forma": model.Case.FormADate = today.AddDays(-10); model.Case.SlaughterDate = today.AddDays(-20); break;
            case "slaughter-before-birth": model.Case.BirthDate = today.AddDays(-10); model.Case.SlaughterDate = today.AddDays(-20); break;
        }

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState.IsValid.Should().BeFalse();
        await _cases.DidNotReceiveWithAnyArgs().EditCaseAsync(default!, default);
    }

    [Fact]
    public async Task OnPost_MissingRowStamp_ReportsSessionExpired()
    {
        var model = Create(withRowStamp: false);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Contain("Session expired");
    }

    [Fact]
    public async Task OnPost_ConcurrencyConflict_RefreshesRowStamp()
    {
        _cases.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 2).Returns(EditCaseResult.ConcurrencyConflict);
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, RowStamp = [9] });
        var model = Create();

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ConcurrencyError.Should().Contain("Another user");
        model.TempData[RowStampKey].Should().Be(Convert.ToBase64String([9]));
    }

    [Fact]
    public async Task OnPost_ConcurrencyConflict_CaseGone_KeepsGoing()
    {
        _cases.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 2).Returns(EditCaseResult.ConcurrencyConflict);
        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        (await Create().OnPostAsync()).Should().BeOfType<PageResult>();
    }

    [Theory]
    [InlineData(EditCaseResult.RbseNotFound, "not found")]
    [InlineData(EditCaseResult.AuditLogError, "Audit log error")]
    [InlineData(EditCaseResult.PostUpdateError, "Database error")]
    [InlineData((EditCaseResult)99, "Update failed")]
    public async Task OnPost_OtherFailures_AddModelError(EditCaseResult result, string expected)
    {
        _cases.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 2).Returns(result);
        var model = Create();

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Contain(expected);
    }
}

public sealed class MoveCaseNewFarmFlowTests
{
    private const string Rbse = "002600001";
    private const string Cphh = "01001000101";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IGeoLookupService _geo = Substitute.For<IGeoLookupService>();

    public MoveCaseNewFarmFlowTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetHerdTypesAsync().Returns(Array.Empty<LuHerdType>());
        _user.GetUserIdAsync().Returns(8);
    }

    private MoveCaseNewFarmModel Create(string? returnRbse = Rbse)
    {
        var model = new MoveCaseNewFarmModel(_cases, _farms, _user, _lookups, _geo) { ReturnRbse = returnRbse };
        var http = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static FarmEditViewModel ValidFarm(string cphh = Cphh) => new()
    {
        CPHH = cphh, OwnerName = "O", Address1 = "A", AHO = "H", Parish = "P", County = "C", ADNSRegionID = 1
    };

    [Fact]
    public void DisplayRbse_FormatsReturnRbse()
    {
        Create().DisplayRbse.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OnGet_PrepopulatesFromCurrentFarm()
    {
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farms.GetByCphhAsync(Cphh).Returns(new FarmRecord { CPHH = Cphh, OwnerName = "Prev", Address1 = "Road", County = "K", AHO = "X", ADNSRegionID = 2 });
        var model = Create();

        (await model.OnGetAsync("01/001/0002/02")).Should().BeOfType<PageResult>();

        model.Farm.OwnerName.Should().Be("Prev");
        model.Farm.CPHH.Should().Be("01001000202");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("12345")]
    public async Task OnGet_InvalidReturnRbse_SkipsPrepopulation(string rbse)
    {
        var model = Create(rbse);

        await model.OnGetAsync(Cphh);

        model.Farm.OwnerName.Should().BeNull();
        await _cases.DidNotReceiveWithAnyArgs().GetCaseAsync(default!);
    }

    [Fact]
    public async Task OnGet_CaseOrFarmMissing_SkipsPrepopulation()
    {
        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        (await Create().OnGetAsync(Cphh)).Should().BeOfType<PageResult>();

        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "" });
        (await Create().OnGetAsync(Cphh)).Should().BeOfType<PageResult>();

        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farms.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        (await Create().OnGetAsync(Cphh)).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPost_MissingEverything_ReportsAllErrors()
    {
        var model = Create();
        model.Farm = new FarmEditViewModel { CPHH = "12" };

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState.Keys.Should().Contain(["Farm.CPHH", "Farm.OwnerName", "Farm.Address1", "Farm.AHO", "Farm.Parish", "Farm.County", "Farm.ADNSRegionID"]);
    }

    [Fact]
    public async Task OnPost_AdnsRegionNotValidForAuthority_Rejected()
    {
        _lookups.GetADNSRegionsByAuthorityAsync(4).Returns(new[] { new LuAdnsRegion { Id = 9, Name = "R9" } });
        var model = Create();
        model.Farm = ValidFarm();
        model.Farm.AuthorityID = 4;
        model.Farm.ADNSRegionID = 1;

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState.ContainsKey("Farm.ADNSRegionID").Should().BeTrue();
    }

    [Fact]
    public async Task OnPost_FarmAlreadyExists_Rejected()
    {
        _farms.GetByCphhAsync(Cphh).Returns(new FarmRecord { CPHH = Cphh });
        var model = Create();
        model.Farm = ValidFarm("01/001/0001/01");

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState["Farm.CPHH"]!.Errors.Single().ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task OnPost_SaveThrows_ShowsGenericError()
    {
        _farms.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        _farms.AddAsync(Arg.Any<AddFarmCommand>(), 8).Returns<Task>(_ => throw new InvalidOperationException());
        var model = Create();
        model.Farm = ValidFarm();

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Contain("Unable to save farm details");
    }

    [Fact]
    public async Task OnPost_Valid_CreatesFarmWithoutVlaOnlyFieldsAndReturnsToMoveCase()
    {
        _farms.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        var model = Create();
        model.Farm = ValidFarm();
        model.Farm.HerdType = "X";
        model.Farm.PedigreeType = "Y";
        model.Farm.IsDealer = true;

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/MoveCase");
        redirect.RouteValues!["newCphh"].Should().Be(Cphh);
        await _farms.Received(1).AddAsync(Arg.Is<AddFarmCommand>(c => c.HerdType == null && c.PedigreeType == null && !c.IsDealer), 8);
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("New farm created");
    }

    [Fact]
    public async Task OnPost_Valid_WithoutReturnRbse_GoesToFarmDetails()
    {
        _farms.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        var model = Create(returnRbse: null);
        model.Farm = ValidFarm();

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Farm/Details");
    }

    [Fact]
    public async Task Ajax_Lookups()
    {
        _lookups.GetAuthoritiesByCountyAsync(1).Returns(new[] { new LuAuthority { Id = 1, Name = "A" } });
        _lookups.GetADNSRegionsByAuthorityAsync(2).Returns(new[] { new LuAdnsRegion { Id = 2, Name = "R" } });
        _geo.EstimateMapReferenceAsync("12345").Returns(new MapReferenceEstimate(null, null, null, "bad"));
        _geo.EstimateMapReferenceAsync("54321").Returns(new MapReferenceEstimate("TQ", "1", "2", null));
        _geo.IsMapReferenceWithinParishAsync("12345", "TQ123456").Returns(true);
        _geo.IsMapReferenceWithinParishAsync("54321", "TQ123456").Returns(false);
        var model = Create();

        (await model.OnGetAuthoritiesAsync(1)).Should().BeOfType<JsonResult>();
        (await model.OnGetAdnsRegionsAsync(2)).Should().BeOfType<JsonResult>();
        ((JsonResult)await model.OnGetEstimateMapReferenceAsync("12")).Value.Should().BeEquivalentTo(new { error = "CPHH must be at least 5 characters to estimate a map reference." });
        ((JsonResult)await model.OnGetEstimateMapReferenceAsync("12345")).Value.Should().BeEquivalentTo(new { error = "bad" });
        ((JsonResult)await model.OnGetEstimateMapReferenceAsync("54321")).Value.Should().BeEquivalentTo(new { mapRef1 = "TQ", mapRef2 = "1", mapRef3 = "2" });
        ((JsonResult)await model.OnGetValidateMapReferenceAsync("12", "tq123456")).Value.Should().BeEquivalentTo(new { valid = true });
        ((JsonResult)await model.OnGetValidateMapReferenceAsync("12345", "tq123456")).Value.Should().BeEquivalentTo(new { valid = true });
        ((JsonResult)await model.OnGetValidateMapReferenceAsync("54321", "tq123456")).Value.Should().BeEquivalentTo(
            new { valid = false, message = "Map reference does not lie within the parish boundaries for this CPHH." });
    }
}
