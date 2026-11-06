using BSE.Host.Models.ViewModels;
using BSE.Host.Pages;
using BSE.Host.Pages.Case;
using BSE.Host.Pages.Farm;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

public sealed class MoveCaseNewFarmModelTests
{
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly IFarmService _farmService = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly BSE.Host.Services.IGeoLookupService _geo = Substitute.For<BSE.Host.Services.IGeoLookupService>();

    private MoveCaseNewFarmModel Create()
    {
        _currentUser.GetUserIdAsync().Returns(4);
        _lookups.GetLookupAsync(LookupTableId.BSECounty).Returns(Array.Empty<LookupItem>());
        _lookups.GetLookupAsync(LookupTableId.AHO).Returns(Array.Empty<LookupItem>());
        _lookups.GetHerdTypesAsync().Returns(Array.Empty<LuHerdType>());
        _lookups.GetLookupAsync(LookupTableId.PedigreeType).Returns(Array.Empty<LookupItem>());
        _lookups.GetLookupAsync(LookupTableId.AuthorityCounty).Returns(Array.Empty<LookupItem>());

        var model = new MoveCaseNewFarmModel(_caseService, _farmService, _currentUser, _lookups, _geo);
        PageTestHelpers.Attach(model, "DataEntry");
        return model;
    }

    [Fact]
    public async Task OnGetAsync_WhenCaseHasExistingFarm_PopulatesTheForm()
    {
        var model = Create();
        model.ReturnRbse = "000500001";
        model.ReturnCphh = "01/001/0007/01";
        _caseService.GetCaseAsync("000500001").Returns(new CaseRecord { Cphh = "01001000701" });
        _farmService.GetByCphhAsync("01001000701").Returns(new FarmRecord
        {
            OwnerName = "Owner",
            Address1 = "1 The Lane",
            Parish = "Parish",
            County = "County",
            AHO = "AHO",
            AuthorityCountyID = 11,
            AuthorityID = 12,
            ADNSRegionID = 13
        });

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Farm.CPHH.Should().Be("01001000701");
        model.Farm.OwnerName.Should().Be("Owner");
        model.Farm.Address1.Should().Be("1 The Lane");
    }

    [Fact]
    public async Task OnPostAsync_WithInvalidCphh_ShowsValidationError()
    {
        var model = Create();
        model.Farm.CPHH = "123";
        model.Farm.OwnerName = "Owner";
        model.Farm.Address1 = "1 The Lane";
        model.Farm.AHO = "AHO";
        model.Farm.Parish = "Parish";
        model.Farm.County = "County";
        model.Farm.ADNSRegionID = 6;

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        PageTestHelpers.ErrorMessages(model, "Farm.CPHH").Should().ContainSingle(m => m.StartsWith("Enter CPHH as 11 digits"));
    }

    [Fact]
    public async Task OnPostAsync_WhenFarmAlreadyExists_ShowsErrorAndDoesNotSave()
    {
        var model = Create();
        model.Farm.CPHH = "01001000701";
        model.Farm.OwnerName = "Owner";
        model.Farm.Address1 = "1 The Lane";
        model.Farm.AHO = "AHO";
        model.Farm.Parish = "Parish";
        model.Farm.County = "County";
        model.Farm.ADNSRegionID = 6;
        _farmService.GetByCphhAsync("01001000701").Returns(new FarmRecord { CPHH = "01001000701" });

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        PageTestHelpers.ErrorMessages(model, "Farm.CPHH").Should().Contain("A farm with this CPHH already exists.");
        await _farmService.DidNotReceive().AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>());
    }

    [Fact]
    public async Task OnPostAsync_WhenValid_RedirectsToMoveCaseWithNewCphh()
    {
        var model = Create();
        model.ReturnRbse = "000500001";
        model.Farm.CPHH = "01001000701";
        model.Farm.OwnerName = "Owner";
        model.Farm.Address1 = "1 The Lane";
        model.Farm.AHO = "AHO";
        model.Farm.Parish = "Parish";
        model.Farm.County = "County";
        model.Farm.ADNSRegionID = 6;

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/MoveCase");
        redirect.RouteValues.Should().ContainKey("newCphh").WhoseValue.Should().Be("01001000701");
        await _farmService.Received(1).AddAsync(Arg.Is<AddFarmCommand>(c => c.CPHH == "01001000701" && c.HerdType == null && c.PedigreeType == null && c.IsDealer == false), 4);
    }
}

public sealed class MoveCaseModelTests
{
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly IFarmService _farmService = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILogger<MoveCaseModel> _logger = Substitute.For<ILogger<MoveCaseModel>>();

    private MoveCaseModel Create()
    {
        _currentUser.GetUserIdAsync().Returns(42);
        var model = new MoveCaseModel(_caseService, _farmService, _currentUser, _logger);
        PageTestHelpers.Attach(model, "DataEntry");
        return model;
    }

    [Fact]
    public async Task OnGetAsync_WhenDestinationFarmIsValid_SetsCanMoveAndLoadsFarmDetails()
    {
        var model = Create();
        model.Rbse = "000500001";
        model.NewCphh = "01001000202";
        _caseService.GetCaseAsync("000500001").Returns(new CaseRecord { Rbse = "000500001", Cphh = "01001000101", IsNonGbCase = false });
        _farmService.GetByCphhAsync("01001000101").Returns(new FarmRecord { CPHH = "01001000101", IsNonGBFarm = false });
        _farmService.GetCaseCountByCphhAsync("01001000101").Returns(2);
        _farmService.GetByCphhAsync("01001000202").Returns(new FarmRecord { CPHH = "01001000202", IsNonGBFarm = false });

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.CaseFound.Should().BeTrue();
        model.NewFarm.Should().NotBeNull();
        model.CanMove.Should().BeTrue();
    }

    [Fact]
    public async Task OnPostCheckAsync_WhenSameFarmSelected_SetsSameCphhError()
    {
        var model = Create();
        model.Rbse = "000500001";
        model.NewCphh = "01001000101";
        _caseService.GetCaseAsync("000500001").Returns(new CaseRecord { Rbse = "000500001", Cphh = "01001000101", IsNonGbCase = false });
        _farmService.GetByCphhAsync("01001000101").Returns(new FarmRecord { CPHH = "01001000101", IsNonGBFarm = false });

        var result = await model.OnPostCheckAsync();

        result.Should().BeOfType<PageResult>();
        model.CphhError.Should().Be(MoveCaseModel.SameCphhMessage);
        model.CanMove.Should().BeFalse();
    }

    [Fact]
    public async Task OnPostMoveAsync_WhenServiceSucceeds_RedirectsToMaintenanceConfirmation()
    {
        var model = Create();
        model.Rbse = "000500001";
        model.NewCphh = "01001000202";
        _caseService.GetCaseAsync("000500001").Returns(new CaseRecord { Rbse = "000500001", Cphh = "01001000101", IsNonGbCase = false });
        _farmService.GetByCphhAsync("01001000101").Returns(new FarmRecord { CPHH = "01001000101", IsNonGBFarm = false });
        _farmService.GetCaseCountByCphhAsync("01001000101").Returns(2);
        _farmService.GetByCphhAsync("01001000202").Returns(new FarmRecord { CPHH = "01001000202", IsNonGBFarm = false });
        _caseService.MoveCaseAsync("000500001", "01001000202", 42).Returns(MoveCaseResult.Success);

        var result = await model.OnPostMoveAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("Case move complete");
    }
}

public sealed class DeleteModelTests
{
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly IFarmService _farmService = Substitute.For<IFarmService>();
    private readonly IBatchService _batchService = Substitute.For<IBatchService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILogger<DeleteModel> _logger = Substitute.For<ILogger<DeleteModel>>();

    private DeleteModel Create()
    {
        _currentUser.GetUserIdAsync().Returns(42);
        var model = new DeleteModel(_caseService, _farmService, _batchService, _currentUser, _logger);
        PageTestHelpers.Attach(model, "DataEntry");
        return model;
    }

    [Fact]
    public async Task OnGetAsync_WhenCaseExists_LoadsCaseAndFarmDetails()
    {
        var model = Create();
        model.Rbse = "000500001";
        _caseService.GetCaseDetailsAsync("000500001").Returns(new CaseDetailRecord(
            new CaseRecord { Rbse = "000500001", Cphh = "01001000101", EartagHerdmark = "AB", Eartag = "123" },
            null,
            null,
            Array.Empty<OtherOwnerRecord>(),
            Array.Empty<CaseFeedRecord>(),
            Array.Empty<ClinicalVisitRecord>(),
            null,
            null,
            Array.Empty<CaseRelationRecord>(),
            Array.Empty<CaseTestRecord>(),
            null));
        _farmService.GetByCphhAsync("01001000101").Returns(new FarmRecord { CPHH = "01001000101", IsNonGBFarm = false });
        _farmService.GetCaseCountByCphhAsync("01001000101").Returns(2);
        _batchService.GetBatchNumbersByRbseAsync("000500001").Returns(Array.Empty<BatchNumberEntry>());

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.CaseFound.Should().BeTrue();
        model.Farm.Should().NotBeNull();
        model.HasVlaData.Should().BeFalse();
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenVlaDataExists_ReturnsPageWithError()
    {
        var model = Create();
        model.Rbse = "000500001";
        _caseService.GetCaseDetailsAsync("000500001").Returns(new CaseDetailRecord(
            new CaseRecord { Rbse = "000500001", Cphh = "01001000101" },
            null,
            null,
            Array.Empty<OtherOwnerRecord>(),
            new[] { new CaseFeedRecord() },
            Array.Empty<ClinicalVisitRecord>(),
            null,
            null,
            Array.Empty<CaseRelationRecord>(),
            Array.Empty<CaseTestRecord>(),
            null));
        _farmService.GetByCphhAsync("01001000101").Returns(new FarmRecord { CPHH = "01001000101", IsNonGBFarm = false });
        _farmService.GetCaseCountByCphhAsync("01001000101").Returns(1);
        _batchService.GetBatchNumbersByRbseAsync("000500001").Returns(Array.Empty<BatchNumberEntry>());

        var result = await model.OnPostDeleteAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be(DeleteModel.VlaDataMessage);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenDeleteSucceeds_RedirectsToMaintenanceConfirmation()
    {
        var model = Create();
        model.Rbse = "000500001";
        _caseService.GetCaseDetailsAsync("000500001").Returns(new CaseDetailRecord(
            new CaseRecord { Rbse = "000500001", Cphh = "01001000101" },
            null,
            null,
            Array.Empty<OtherOwnerRecord>(),
            Array.Empty<CaseFeedRecord>(),
            Array.Empty<ClinicalVisitRecord>(),
            null,
            null,
            Array.Empty<CaseRelationRecord>(),
            Array.Empty<CaseTestRecord>(),
            null));
        _farmService.GetByCphhAsync("01001000101").Returns(new FarmRecord { CPHH = "01001000101", IsNonGBFarm = false });
        _farmService.GetCaseCountByCphhAsync("01001000101").Returns(2);
        _batchService.GetBatchNumbersByRbseAsync("000500001").Returns(Array.Empty<BatchNumberEntry>());
        _caseService.DeleteCaseAsync("000500001", 42).Returns(DeleteCaseResult.Success);

        var result = await model.OnPostDeleteAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/MaintenanceConfirmation");
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("Case deleted");
    }
}

public sealed class FarmDetailsModelTests
{
    private readonly IFarmService _farmService = Substitute.For<IFarmService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private DetailsModel Create() => new(_farmService, _lookups);

    [Fact]
    public async Task OnGetAsync_WhenFarmDoesNotExist_ReturnsPageWithoutData()
    {
        var model = Create();
        _farmService.GetByCphhAsync("01001000101").Returns((FarmRecord?)null);

        var result = await model.OnGetAsync("01001000101");

        result.Should().BeOfType<PageResult>();
        model.Farm.Should().BeNull();
        model.RelatedFarms.Should().BeEmpty();
        model.HerdSizes.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_WhenFarmExists_LoadsRelatedDataAndAdnsRegionName()
    {
        var model = Create();
        _farmService.GetByCphhAsync("01001000101").Returns(new FarmRecord { CPHH = "01001000101", ADNSRegionID = 42 });
        _farmService.GetRelatedFarmsAsync("01001000101").Returns(new[]
        {
            new FarmRelationRecord { ID = 1, CPHH = "01001000101", RelatedCPHH = "01001000102", Status = "BSE Free" }
        });
        _farmService.GetHerdSizesAsync("01001000101").Returns(new[]
        {
            new HerdSizeRecord { ID = 1, CPHH = "01001000101", HerdYear = 2025, TotalSize = 12 },
            new HerdSizeRecord { ID = 2, CPHH = "01001000101", HerdYear = 2024, TotalSize = 10 }
        });
        _farmService.GetConfirmedCaseCountAsync("01001000101").Returns(3);
        _lookups.GetADNSRegionsAsync().Returns(new[] { new LuAdnsRegion { Id = 42, Name = "North Region" } });

        var result = await model.OnGetAsync("01001000101");

        result.Should().BeOfType<PageResult>();
        model.Farm.Should().NotBeNull();
        model.RelatedFarms.Should().ContainSingle();
        model.HerdSizes.Should().HaveCount(2);
        model.HerdSizes.Select(h => h.HerdYear).Should().BeInDescendingOrder();
        model.ConfirmedCaseCount.Should().Be(3);
        model.ADNSRegionName.Should().Be("North Region");
    }
}
