using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Case;
using BSE.Host.Pages.Farm;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Data;
using IReferenceGeoLookupService = BSE.Modules.ReferenceData.Services.IGeoLookupService;

namespace BSE.Modules.UserManagement.Tests;

public sealed class FarmNewModelTests
{
    private readonly IFarmService _farmService = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IReferenceGeoLookupService _geo = Substitute.For<IReferenceGeoLookupService>();
    private readonly BSE.Host.Services.IGeoLookupService _mapGeo = Substitute.For<BSE.Host.Services.IGeoLookupService>();

    private BSE.Host.Pages.Farm.NewModel Create()
    {
        _currentUser.GetUserIdAsync().Returns(4);
        _farmService.GetVetnetDetailsAsync(Arg.Any<string>()).Returns(Array.Empty<VetnetRecord>());
        var model = new BSE.Host.Pages.Farm.NewModel(_farmService, _currentUser, _lookups, _geo, _mapGeo);
        PageTestHelpers.Attach(model, "FarmCreation");
        return model;
    }

    [Fact]
    public async Task OnGetAsync_NormalisesTheSuppliedCphh()
    {
        var model = Create();

        var result = await model.OnGetAsync("01/001/0007/01");

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>();
        model.Farm.CPHH.Should().Be("01001000701");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    public async Task OnPostAsync_RejectsAnInvalidCphh(string cphh)
    {
        var model = Create();
        model.Farm.CPHH = cphh;

        var result = await model.OnPostAsync();

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>();
        PageTestHelpers.ErrorMessages(model, "Farm.CPHH").Should().ContainSingle(m => m.StartsWith("Enter CPHH as 11 digits"));
    }

    [Fact]
    public async Task OnPostAsync_WhenTheFarmAlreadyExists_ShowsAnError()
    {
        var model = Create();
        model.Farm.CPHH = "01001000701";
        _farmService.GetByCphhAsync("01001000701").Returns(new FarmRecord { CPHH = "01001000701" });

        await model.OnPostAsync();

        PageTestHelpers.ErrorMessages(model, "Farm.CPHH").Should().Contain("A farm with this CPHH already exists.");
        await _farmService.DidNotReceive().AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>());
    }

    [Fact]
    public async Task OnPostAsync_PickFarmJourneyWithoutARbse_RedirectsHome()
    {
        var model = Create();
        model.ReturnToCaseFarm = true;
        model.Farm.CPHH = "01001000701";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnPostAsync_PickFarmJourneyForAnExistingFarm_SelectsIt()
    {
        var model = Create();
        model.ReturnToCaseFarm = true;
        model.ReturnRbse = "000500001";
        model.Farm.CPHH = "01001000701";
        _farmService.GetByCphhAsync("01001000701").Returns(new FarmRecord { CPHH = "01001000701" });

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues.Should().ContainKey("selectedCphh").WhoseValue.Should().Be("01001000701");
    }

    [Fact]
    public async Task OnPostAsync_PickFarmJourneyForANewFarm_SeedsTheCaseFarmPage()
    {
        var model = Create();
        model.ReturnToCaseFarm = true;
        model.AcceptVetnetDetails = true;
        model.ReturnRbse = "000500001";
        model.Farm.CPHH = "01001000701";
        _farmService.GetByCphhAsync("01001000701").Returns((FarmRecord?)null);
        _farmService.GetVetnetDetailsAsync("01001000701").Returns(new[] { new VetnetRecord { Herdmark = "AB1234", NumericHerdmark = "123456" } });
        _geo.GetParishAsync("01", "001").Returns(new ParishLookup { Name = "Parish", BSECounty = "BEDS", ADNSRegionID = 137, AuthorityID = 1, AuthorityCountyID = 3 });

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.RouteValues.Should().ContainKey("newCphh");
        redirect.RouteValues!["seedParish"].Should().Be("Parish");
        redirect.RouteValues["seedHerdmark1"].Should().Be("AB1234");
        redirect.RouteValues["seedAdnsRegionId"].Should().Be(137);
    }

    [Fact]
    public async Task OnPostAsync_PickFarmJourneyWithoutVetnetOrParish_ShowsTheWarnings()
    {
        var model = Create();
        model.ReturnToCaseFarm = true;
        model.Farm.CPHH = "";

        await model.OnPostAsync();

        model.VetnetMessage.Should().NotBeNull();

        model.Farm.CPHH = "01001000701";
        _geo.GetParishAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((ParishLookup?)null);
        model.ReturnRbse = "000500001";
        await model.OnPostAsync();

        model.ShowBrusselsWarning.Should().BeTrue();
        model.VetnetMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostAsync_WhenValid_CreatesTheFarmAndRedirectsToDetails()
    {
        var model = Create();
        model.Farm.CPHH = "01001000701";
        model.Farm.OwnerName = "Owner";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Farm/Details");
        await _farmService.Received(1).AddAsync(Arg.Any<AddFarmCommand>(), 4);
        model.TempData["Success"].Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostAsync_WhenSavingThrows_ShowsAGenericError()
    {
        var model = Create();
        model.Farm.CPHH = "01001000701";
        _farmService.AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>()).Returns(Task.FromException(new InvalidOperationException("db")));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>();
        PageTestHelpers.ErrorMessages(model, string.Empty).Should().ContainSingle(m => m.StartsWith("Unable to save farm details"));
    }

    [Theory]
    [InlineData(true, "/Case/Farm")]
    [InlineData(false, "/Case/Farm")]
    public async Task OnPostAsync_AfterCreating_ReturnsToTheCaseFarmPageWhenAskedTo(bool toCaseFarm, string page)
    {
        var model = Create();
        model.Farm.CPHH = "01001000701";
        model.ReturnRbse = "000500001";
        model.ReturnCphh = "01001000701";
        model.ReturnToCaseFarm = toCaseFarm;
        _farmService.GetByCphhAsync("01001000701").Returns((FarmRecord?)null);

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be(page);
    }
}

public sealed class BabEditModelTests
{
    private const string Rbse = "002600001";
    private readonly IBabRepository _babRepository = Substitute.For<IBabRepository>();
    private readonly ICaseRepository _caseRepository = Substitute.For<ICaseRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();

    private BabEditModel Create()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetAnimalOriginsAsync().Returns(Array.Empty<LuAnimalOrigin>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Origin = "P" });
        _connection.BeginTransaction().Returns(_transaction);
        _connectionFactory.CreateConnection().Returns(_connection);
        var model = new BabEditModel(_babRepository, _caseRepository, _lookups, _batchRepository, _connectionFactory,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build())
        {
            Rbse = Rbse
        };
        PageTestHelpers.Attach(model, "DataEntry");
        return model;
    }

    [Fact]
    public async Task OnGetAsync_LoadsLookupsAndTheCaseOrigin()
    {
        var model = Create();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>();
        model.Origin.Should().Be("P");
        model.SpolSiteUrl.Should().Be("http://spol");
    }

    [Fact]
    public async Task OnPostSaveBabAsync_WithAnInvalidCphh_RedisplaysThePage()
    {
        var model = Create();
        model.Bab.NatalCphh = "12/345";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>();
        PageTestHelpers.ErrorMessages(model, "Bab.NatalCphh").Should().ContainSingle(m => m.StartsWith("Enter CPHH as 11 digits"));
    }

    [Fact]
    public async Task OnPostSaveBabAsync_WithoutARowStamp_AddsTheBabRow()
    {
        var model = Create();
        model.Origin = "P";
        model.Bab.NatalCphh = "01/001/0007/01";
        model.Bab.TracedName = "Seller";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Bab");
        await _babRepository.Received(1).AddAsync(Arg.Is<AddCaseBabCommand>(c => c.NatalCphh == "01001000701" && c.TracedName == "Seller"), "P", _connection, _transaction);
        _transaction.Received(1).Commit();
    }

    [Fact]
    public async Task OnPostSaveBabAsync_WithARowStamp_EditsTheBabRow()
    {
        var model = Create();
        model.Origin = "P";
        model.Bab.TracedName = "Seller";

        await model.OnPostSaveBabAsync(Convert.ToBase64String([1, 2]));

        await _babRepository.Received(1).EditAsync(Arg.Is<EditCaseBabCommand>(c => c.RowStamp.Length == 2), "P", _connection, _transaction);
    }

    [Fact]
    public async Task OnPostSaveBabAsync_WhenOriginIsNotPurchased_ClearsTheTracedFields()
    {
        var model = Create();
        model.Origin = "N";
        model.Bab.NatalCphh = "01001000701";
        model.Bab.TracedName = "Seller";
        model.Bab.TracedPostcode = "AB1";

        await model.OnPostSaveBabAsync(null);

        await _babRepository.Received(1).AddAsync(
            Arg.Is<AddCaseBabCommand>(c => c.NatalCphh == null && c.TracedName == null && c.TracedPostcode == null),
            "N", _connection, _transaction);
    }
}
