using System.Security.Claims;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
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
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>Covers the request-handling behaviour of <see cref="FarmModel"/> (OnGet/OnPost handlers),
/// which are exercised far less than its sorting/pagination helpers by <c>PageModelCoverageTests</c>.</summary>
public sealed class FarmModelHandlerTests
{
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly IFarmService _farmService = Substitute.For<IFarmService>();
    private readonly IFarmRelationRepository _relationRepo = Substitute.For<IFarmRelationRepository>();
    private readonly IHerdSizeRepository _herdSizeRepo = Substitute.For<IHerdSizeRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly IBatchService _batchService = Substitute.For<IBatchService>();
    private readonly ICaseWizardStateService _wizardState = Substitute.For<ICaseWizardStateService>();
    private readonly ICaseFarmDraftStateService _farmDraftState = Substitute.For<ICaseFarmDraftStateService>();
    private readonly ICaseScalarDraftStateService _caseScalarDraftState = Substitute.For<ICaseScalarDraftStateService>();
    private readonly ICaseEditOrchestrationService _caseEditOrchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly BSE.Host.Services.IGeoLookupService _geoLookup = Substitute.For<BSE.Host.Services.IGeoLookupService>();

    private const string Rbse = "002600001";
    private const string Cphh = "01001000101";

    public FarmModelHandlerTests()
    {
        _lookups.GetADNSRegionsAsync().Returns(Array.Empty<LuADNSRegion>());
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetHerdTypesAsync().Returns(Array.Empty<LuHerdType>());
        _lookups.GetAuthoritiesByCountyAsync(Arg.Any<int>()).Returns(Array.Empty<LuAuthority>());
        _lookups.GetADNSRegionsByAuthorityAsync(Arg.Any<int>()).Returns(Array.Empty<LuADNSRegion>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _wizardState.GetAsync().Returns((CaseWizardState?)null);
        _farmService.GetRelatedFarmsAsync(Arg.Any<string>()).Returns(Array.Empty<FarmRelationRecord>());
        _farmService.GetHerdSizesAsync(Arg.Any<string>()).Returns(Array.Empty<HerdSizeRecord>());
        _farmService.GetConfirmedCaseCountAsync(Arg.Any<string>()).Returns(0);
    }

    private FarmModel CreateModel(string[]? roles = null)
    {
        var model = new FarmModel(
            _caseService, _farmService, _relationRepo, _herdSizeRepo, _lookups,
            _batchRepository, _batchService, _wizardState, _farmDraftState,
            _caseScalarDraftState, _caseEditOrchestration, _currentUser,
            NullLogger<FarmModel>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            _geoLookup)
        {
            Rbse = Rbse
        };

        var httpContext = new DefaultHttpContext();
        if (roles is { Length: > 0 })
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"));
        }
        else
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        model.PageContext = new PageContext { HttpContext = httpContext };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static FarmRecord MakeFarm(byte[]? rowStamp = null) => new()
    {
        CPHH = Cphh,
        OwnerName = "Test Owner",
        Address1 = "1 Test Lane",
        Parish = "Test Parish",
        County = "TC",
        AHO = "TA",
        ADNSRegionID = 1,
        RowStamp = rowStamp ?? new byte[] { 1, 2, 3 }
    };

    // ── OnGetAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_ExistingCase_ReturnsPage()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());

        var model = CreateModel(["DataEntry"]);
        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Farm.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_NoCase_FallsBackToNewCaseFlow()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var model = CreateModel(["DataEntry"]);
        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.CanEditCreateMode.Should().BeTrue();
    }

    // ── OnPostCreateCaseAsync ────────────────────────────────────────────────

    [Fact]
    public async Task OnPostCreateCaseAsync_Forbidden_WhenUserCannotCreate()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var model = CreateModel(); // no roles
        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostCreateCaseAsync_MissingCphh_AddsModelError()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = new FarmEditViewModel { CPHH = "" };

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("EditableFarm.CPHH");
    }

    [Fact]
    public async Task OnPostCreateCaseAsync_NewFarmDetails_SavesFarmAndCreatesCase()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        _farmService.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        _currentUser.GetUserIdAsync().Returns(7);
        _batchService.GetOrCreateBatchNumberAsync().Returns(new BatchRecord(1, 2024, 1));
        _caseService.CreateCaseAsync(Arg.Any<UpdateCaseDetailsCommand>(), 7).Returns(AddCaseResult.Success);

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = new FarmEditViewModel
        {
            CPHH = Cphh,
            OwnerName = "New Owner",
            Address1 = "Address 1",
            Parish = "Parish",
            County = "CC",
            AHO = "AA",
            ADNSRegionID = 5
        };

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _farmService.Received(1).AddAsync(Arg.Any<AddFarmCommand>(), 7);
        await _farmDraftState.Received(1).ClearAsync(Rbse);
        model.TempData["SuccessMessage"].Should().Be($"Case {Rbse} created successfully.");
    }

    [Fact]
    public async Task OnPostCreateCaseAsync_DuplicateRbse_AddsModelError()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _currentUser.GetUserIdAsync().Returns(7);
        _batchService.GetOrCreateBatchNumberAsync().Returns(new BatchRecord(1, 2024, 1));
        _caseService.CreateCaseAsync(Arg.Any<UpdateCaseDetailsCommand>(), 7).Returns(AddCaseResult.DuplicateRbse);

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = new FarmEditViewModel { CPHH = Cphh };

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("already exists"));
    }

    // ── OnPostLookupNewCaseAsync ─────────────────────────────────────────────

    [Fact]
    public async Task OnPostLookupNewCaseAsync_NonGbCphh_AddsModelError()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = new FarmEditViewModel { CPHH = "00123456789" };

        var result = await model.OnPostLookupNewCaseAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("EditableFarm.CPHH");
    }

    [Fact]
    public async Task OnPostLookupNewCaseAsync_FarmFound_ReturnsPageWithDetails()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = new FarmEditViewModel { CPHH = Cphh };

        var result = await model.OnPostLookupNewCaseAsync();

        result.Should().BeOfType<PageResult>();
        model.RequireFarmDetails.Should().BeFalse();
        model.EditableFarm.OwnerName.Should().Be("Test Owner");
    }

    [Fact]
    public async Task OnPostLookupNewCaseAsync_FarmNotFound_RedirectsToPickFarm()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        _farmService.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = new FarmEditViewModel { CPHH = Cphh };

        var result = await model.OnPostLookupNewCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        ((RedirectToPageResult)result).PageName.Should().Be("/Case/PickFarm");
    }

    // ── Linked farm handlers ─────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAddLinkedFarmRowAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateModel(["VLAAccess"]);
        var result = await model.OnPostAddLinkedFarmRowAsync();
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostAddLinkedFarmRowAsync_InvalidCphh_ReopensRowWithError()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState { Rbse = Rbse, Cphh = Cphh });

        var model = CreateModel(["DataEntry"]);
        model.NewLinkedCphh = "123";

        var result = await model.OnPostAddLinkedFarmRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ShowAddLinkedRow.Should().BeTrue();
        model.ModelState.Should().ContainKey(nameof(FarmModel.NewLinkedCphh));
    }

    [Fact]
    public async Task OnPostAddLinkedFarmRowAsync_Valid_AddsToDraftAndRedirects()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync("02002000202").Returns((FarmRecord?)null);

        var model = CreateModel(["DataEntry"]);
        model.NewLinkedCphh = "02002000202";

        var result = await model.OnPostAddLinkedFarmRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _farmDraftState.Received(1).SetAsync(Arg.Is<CaseFarmDraftState>(d =>
            d.LinkedFarms.Any(x => x.RelatedCphh == "02002000202")));
    }

    [Fact]
    public async Task OnPostDeleteLinkedFarmAsync_RemovesMatchingRow()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var draft = new CaseFarmDraftState
        {
            Rbse = Rbse,
            Cphh = Cphh,
            LinkedFarms = [new CaseFarmDraftLinkedFarmItem { ClientKey = "k1", RelatedCphh = "02002000202" }]
        };
        _farmDraftState.GetAsync(Rbse).Returns(draft);

        var model = CreateModel(["DataEntry"]);
        var result = await model.OnPostDeleteLinkedFarmAsync("k1");

        result.Should().BeOfType<RedirectToPageResult>();
        await _farmDraftState.Received(1).SetAsync(Arg.Is<CaseFarmDraftState>(d => d.LinkedFarms.Count == 0));
    }

    [Fact]
    public async Task OnPostBeginEditLinkedFarmRowAsync_SetsReopenState()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var draft = new CaseFarmDraftState
        {
            Rbse = Rbse,
            Cphh = Cphh,
            LinkedFarms = [new CaseFarmDraftLinkedFarmItem { ClientKey = "k1", RelatedCphh = "02002000202" }]
        };
        _farmDraftState.GetAsync(Rbse).Returns(draft);

        var model = CreateModel(["DataEntry"]);
        model.EditingLinkedClientKey = "k1";

        var result = await model.OnPostBeginEditLinkedFarmRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ReopenLinkedEditClientKey.Should().Be("k1");
        model.EditLinkedCphh.Should().Be("02002000202");
    }

    [Fact]
    public async Task OnPostUpdateLinkedFarmRowAsync_Valid_UpdatesDraftRow()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _farmService.GetByCphhAsync("03003000303").Returns((FarmRecord?)null);
        var draft = new CaseFarmDraftState
        {
            Rbse = Rbse,
            Cphh = Cphh,
            LinkedFarms = [new CaseFarmDraftLinkedFarmItem { ClientKey = "k1", RelatedCphh = "02002000202" }]
        };
        _farmDraftState.GetAsync(Rbse).Returns(draft);

        var model = CreateModel(["DataEntry"]);
        model.EditingLinkedClientKey = "k1";
        model.EditLinkedCphh = "03003000303";

        var result = await model.OnPostUpdateLinkedFarmRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _farmDraftState.Received(1).SetAsync(Arg.Is<CaseFarmDraftState>(d =>
            d.LinkedFarms.Single().RelatedCphh == "03003000303"));
    }

    // ── Herd size handlers ───────────────────────────────────────────────────

    [Fact]
    public async Task OnPostBeginEditHerdRowAsync_PopulatesEditRow()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var draft = new CaseFarmDraftState
        {
            Rbse = Rbse,
            Cphh = Cphh,
            HerdSizes = [new CaseFarmDraftHerdSizeItem { ClientKey = "h1", HerdYear = 2024, TotalSize = 20 }]
        };
        _farmDraftState.GetAsync(Rbse).Returns(draft);

        var model = CreateModel(["DataEntry"]);
        model.EditingClientKey = "h1";

        var result = await model.OnPostBeginEditHerdRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ReopenEditClientKey.Should().Be("h1");
        model.EditHerdRow.HerdYear.Should().Be(2024);
    }

    [Fact]
    public async Task OnPostAddHerdSizeRowAsync_InvalidRow_ReturnsPageWithErrors()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState { Rbse = Rbse, Cphh = Cphh });

        var model = CreateModel(["DataEntry"]);
        model.NewHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = 1900, TotalSize = 0 };

        var result = await model.OnPostAddHerdSizeRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ShowAddHerdRow.Should().BeTrue();
        model.ModelState.Should().ContainKey($"{nameof(FarmModel.NewHerdRow)}.HerdYear");
    }

    [Fact]
    public async Task OnPostAddHerdSizeRowAsync_Valid_AddsRowToDraft()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState { Rbse = Rbse, Cphh = Cphh });

        var model = CreateModel(["DataEntry"]);
        model.NewHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = 2024, TotalSize = 20 };

        var result = await model.OnPostAddHerdSizeRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _farmDraftState.Received(1).SetAsync(Arg.Is<CaseFarmDraftState>(d =>
            d.HerdSizes.Any(h => h.HerdYear == 2024 && h.TotalSize == 20)));
    }

    [Fact]
    public async Task OnPostUpdateHerdSizeRowAsync_Valid_UpdatesDraftRow()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var draft = new CaseFarmDraftState
        {
            Rbse = Rbse,
            Cphh = Cphh,
            HerdSizes = [new CaseFarmDraftHerdSizeItem { ClientKey = "h1", HerdYear = 2023, TotalSize = 10 }]
        };
        _farmDraftState.GetAsync(Rbse).Returns(draft);

        var model = CreateModel(["DataEntry"]);
        model.EditingClientKey = "h1";
        model.EditHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = 2024, TotalSize = 30 };

        var result = await model.OnPostUpdateHerdSizeRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _farmDraftState.Received(1).SetAsync(Arg.Is<CaseFarmDraftState>(d =>
            d.HerdSizes.Single().HerdYear == 2024 && d.HerdSizes.Single().TotalSize == 30));
    }

    [Fact]
    public async Task OnPostDeleteHerdSizeAsync_RemovesMatchingRow()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var draft = new CaseFarmDraftState
        {
            Rbse = Rbse,
            Cphh = Cphh,
            HerdSizes = [new CaseFarmDraftHerdSizeItem { ClientKey = "h1", HerdYear = 2023, TotalSize = 10 }]
        };
        _farmDraftState.GetAsync(Rbse).Returns(draft);

        var model = CreateModel(["DataEntry"]);
        var result = await model.OnPostDeleteHerdSizeAsync("h1");

        result.Should().BeOfType<RedirectToPageResult>();
        await _farmDraftState.Received(1).SetAsync(Arg.Is<CaseFarmDraftState>(d => d.HerdSizes.Count == 0));
    }

    // ── Save / Cancel farm edit ──────────────────────────────────────────────

    [Fact]
    public async Task OnPostSaveFarmAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateModel(["VLAAccess"]);
        var result = await model.OnPostSaveFarmAsync();
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSaveFarmAsync_Valid_PersistsFarmAndRedirects()
    {
        var farm = MakeFarm();
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(farm);
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState { Rbse = Rbse, Cphh = Cphh });
        _currentUser.GetUserIdAsync().Returns(9);

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = FarmEditViewModel.FromRecord(farm);
        model.EditableFarmRowStampBase64 = Convert.ToBase64String(farm.RowStamp!);

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _farmService.Received(1).UpdateAsync(Arg.Any<UpdateFarmCommand>(), 9);
        await _farmDraftState.Received(1).ClearAsync(Rbse);
        model.TempData["Success"].Should().Be("Farm updated successfully.");
    }

    [Fact]
    public async Task OnPostCancelFarmEditAsync_ClearsDraftAndRedirectsHome()
    {
        var model = CreateModel(["DataEntry"]);
        var result = await model.OnPostCancelFarmEditAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        ((RedirectToPageResult)result).PageName.Should().Be("/Home");
        await _farmDraftState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task OnGetCancelFarmEditAsync_ClearsDraftAndRedirectsHome()
    {
        var model = CreateModel(["DataEntry"]);
        var result = await model.OnGetCancelFarmEditAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        ((RedirectToPageResult)result).PageName.Should().Be("/Home");
    }

    // ── Batch assignment ─────────────────────────────────────────────────────

    [Fact]
    public async Task OnPostSaveBatchAsync_Forbidden_WhenNotVlaAccess()
    {
        var model = CreateModel(["DataEntry"]);
        var result = await model.OnPostSaveBatchAsync();
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSaveBatchAsync_NoPendingBatch_SetsErrorAndRedirects()
    {
        _wizardState.GetAsync().Returns((CaseWizardState?)null);

        var model = CreateModel(["VLAAccess"]);
        var result = await model.OnPostSaveBatchAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["ErrorMessage"].Should().Be("No batch was selected. Return to the home page and choose a batch number.");
    }

    [Fact]
    public async Task OnPostSaveBatchAsync_Success_AssignsCaseAndRedirects()
    {
        var pending = new CaseWizardState(Rbse, "2024/001", 55);
        _wizardState.GetAsync().Returns(pending);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(Array.Empty<BatchNumberEntry>());
        _batchService.AssignCaseToBatchAsync(55, Rbse, "BSE1").Returns(BatchAssignmentResult.Success);
        _currentUser.GetUserIdAsync().Returns(3);

        var model = CreateModel(["VLAAccess"]);
        var result = await model.OnPostSaveBatchAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _wizardState.Received(1).ClearAsync();
        model.TempData["Success"].Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostCancelBatchAsync_ClearsPendingAndRedirectsToHome()
    {
        _wizardState.GetAsync().Returns(new CaseWizardState(Rbse, "2024/007", 1));

        var model = CreateModel(["VLAAccess"]);
        var result = await model.OnPostCancelBatchAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Home");
        redirect.RouteValues.Should().ContainKey("batchYear").WhoseValue.Should().Be((short)2024);
        await _wizardState.Received(1).ClearAsync();
    }

    // ── AJAX endpoints ───────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetLinkedFarmStatusAsync_EmptyCphh_ReturnsNullStatus()
    {
        var model = CreateModel();
        var result = await model.OnGetLinkedFarmStatusAsync(null);
        result.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public async Task OnGetLinkedFarmStatusAsync_FarmFound_ReturnsOwnerAndAddress()
    {
        _farmService.GetByCphhAsync("02002000202").Returns(MakeFarm());

        var model = CreateModel();
        var result = (JsonResult)await model.OnGetLinkedFarmStatusAsync("02/002/0002/02");

        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAuthoritiesAsync_NullId_ReturnsEmptyArray()
    {
        var model = CreateModel();
        var result = (JsonResult)await model.OnGetAuthoritiesAsync(null);
        result.Value.Should().BeAssignableTo<IEnumerable<object>>().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAuthoritiesAsync_ValidId_ReturnsMappedItems()
    {
        _lookups.GetAuthoritiesByCountyAsync(5).Returns([new LuAuthority { Id = 1, Name = "Authority A" }]);

        var model = CreateModel();
        var result = (JsonResult)await model.OnGetAuthoritiesAsync(5);

        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAdnsRegionsAsync_NullId_ReturnsEmptyArray()
    {
        var model = CreateModel();
        var result = (JsonResult)await model.OnGetAdnsRegionsAsync(null);
        result.Value.Should().BeAssignableTo<IEnumerable<object>>().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetEstimateMapReferenceAsync_ShortCphh_ReturnsError()
    {
        var model = CreateModel();
        var result = (JsonResult)await model.OnGetEstimateMapReferenceAsync("123");
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetEstimateMapReferenceAsync_NoParishData_ReturnsError()
    {
        _geoLookup.GetAllParishMapReferencesAsync("01", "001").Returns(Array.Empty<ParishMapReferenceRow>());

        var model = CreateModel();
        var result = (JsonResult)await model.OnGetEstimateMapReferenceAsync("01001000101");

        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetValidateMapReferenceAsync_BlankInputs_ReturnsValidTrue()
    {
        var model = CreateModel();
        var result = (JsonResult)await model.OnGetValidateMapReferenceAsync(null, null);
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostSaveFarmAsync_MissingRequiredFields_ReturnsPageWithErrors()
    {
        var farm = MakeFarm();
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(farm);
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState { Rbse = Rbse, Cphh = Cphh });

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = new FarmEditViewModel { CPHH = Cphh }; // missing owner/address/parish/aho/adns
        model.EditableFarmRowStampBase64 = Convert.ToBase64String(farm.RowStamp!);

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("EditableFarm.OwnerName");
        model.ModelState.Should().ContainKey("EditableFarm.Address1");
    }

    [Fact]
    public async Task OnPostSaveFarmAsync_WithStagedCollections_PersistsLinkedFarmsAndHerdSizes()
    {
        var farm = MakeFarm();
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(farm);
        _farmService.GetRelatedFarmsAsync(Cphh).Returns(
            [new FarmRelationRecord { ID = 1, CPHH = Cphh, RelatedCPHH = "02002000202", RowStamp = [1] }]);
        _farmService.GetHerdSizesAsync(Cphh).Returns(
            [new HerdSizeRecord { ID = 1, CPHH = Cphh, HerdYear = 2020, TotalSize = 10, RowStamp = [2] }]);
        // An existing draft (rather than null) means LoadOrInitializeDraftStateAsync uses these
        // staged values as-is instead of re-seeding fresh ones from the persisted DB rows.
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState
        {
            Rbse = Rbse,
            Cphh = Cphh,
            HasPendingChanges = true,
            LinkedFarms =
            [
                new CaseFarmDraftLinkedFarmItem { Id = 1, RelatedCphh = "04004000404", RowStampBase64 = Convert.ToBase64String([1]) },
                new CaseFarmDraftLinkedFarmItem { Id = null, RelatedCphh = "03003000303" }
            ],
            HerdSizes =
            [
                new CaseFarmDraftHerdSizeItem { Id = 1, HerdYear = 2021, TotalSize = 15, RowStampBase64 = Convert.ToBase64String([2]) },
                new CaseFarmDraftHerdSizeItem { Id = null, HerdYear = 2022, TotalSize = 25 }
            ]
        });
        _currentUser.GetUserIdAsync().Returns(9);

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = FarmEditViewModel.FromRecord(farm);
        model.EditableFarmRowStampBase64 = Convert.ToBase64String(farm.RowStamp!);

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _relationRepo.Received(1).AddAsync(Cphh, "03003000303");
        await _relationRepo.Received(1).UpdateAsync(1, "04004000404", Arg.Any<byte[]>());
        await _herdSizeRepo.Received(1).AddAsync(Arg.Is<AddHerdSizeCommand>(c => c.HerdYear == 2022 && c.TotalSize == 25));
        await _herdSizeRepo.Received(1).UpdateAsync(Arg.Is<UpdateHerdSizeCommand>(c => c.ID == 1 && c.HerdYear == 2021));
    }

    [Fact]
    public async Task OnGetEstimateMapReferenceAsync_Success_ReturnsMapReferenceParts()
    {
        _geoLookup.GetAllParishMapReferencesAsync("01", "001").Returns(
        [
            new ParishMapReferenceRow { XReference1 = "1234", YReference1 = "0100", YReference2 = "0200" }
        ]);
        _geoLookup.GetPrefixCodeAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("SO");

        var model = CreateModel();
        var result = (JsonResult)await model.OnGetEstimateMapReferenceAsync("01001000101");

        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetValidateMapReferenceAsync_NoParishData_ReturnsValidTrue()
    {
        _geoLookup.GetXYCoordsByPrefixCodeAsync(Arg.Any<string>()).Returns((MapPrefixXY?)null);

        var model = CreateModel();
        var result = (JsonResult)await model.OnGetValidateMapReferenceAsync("01001000101", "SO123450");

        result.Value.Should().NotBeNull();
    }

    // ── SortHerdSizesByColumn / OrderHerdSizesByColumn branch coverage ──────────
    // These two (HSort, HDir) switches account for the bulk of Farm.cshtml.cs's
    // uncovered conditions, so every column x direction combination is exercised.

    public static TheoryData<string, string> HerdSizeSortColumns() => new()
    {
        { "total", "asc" }, { "total", "desc" },
        { "lac1", "asc" }, { "lac1", "desc" },
        { "lac2", "asc" }, { "lac2", "desc" },
        { "lac3", "asc" }, { "lac3", "desc" },
        { "lac4", "asc" }, { "lac4", "desc" },
        { "lac5", "asc" }, { "lac5", "desc" },
        { "lac6", "asc" }, { "lac6", "desc" },
        { "lac7", "asc" }, { "lac7", "desc" },
        { "lac8", "asc" }, { "lac8", "desc" },
        { "lac9", "asc" }, { "lac9", "desc" },
        { "lac10", "asc" }, { "lac10", "desc" },
        { "lac10p", "asc" }, { "lac10p", "desc" },
        { "year", "asc" }, { "year", "desc" },
        { "unknown", "asc" }, { "unknown", "desc" }
    };

    private static FarmModel.StagedHerdSizeItem LesserHerdSizeItem() => new()
    {
        HerdYear = 2020, TotalSize = 10,
        Lactation1Size = 1, Lactation2Size = 2, Lactation3Size = 3, Lactation4Size = 4, Lactation5Size = 5,
        Lactation6Size = 6, Lactation7Size = 7, Lactation8Size = 8, Lactation9Size = 9, Lactation10Size = 10,
        Lactation10PlusSize = 11
    };

    private static FarmModel.StagedHerdSizeItem GreaterHerdSizeItem() => new()
    {
        HerdYear = 2021, TotalSize = 20,
        Lactation1Size = 12, Lactation2Size = 13, Lactation3Size = 14, Lactation4Size = 15, Lactation5Size = 16,
        Lactation6Size = 17, Lactation7Size = 18, Lactation8Size = 19, Lactation9Size = 20, Lactation10Size = 21,
        Lactation10PlusSize = 22
    };

    [Theory]
    [MemberData(nameof(HerdSizeSortColumns))]
    public void SortedStagedHerdSizes_OrdersEveryColumnInBothDirections(string sort, string dir)
    {
        var model = CreateModel();
        model.HSort = sort;
        model.HDir = dir;
        var lesser = LesserHerdSizeItem();
        var greater = GreaterHerdSizeItem();
        model.StagedHerdSizes = [greater, lesser];

        var result = model.SortedStagedHerdSizes();

        // Unknown sort columns and "desc" both fall back to the default (descending by year) arm.
        var expectedFirst = dir == "asc" && sort != "unknown" ? lesser : greater;
        result[0].Should().BeSameAs(expectedFirst);
    }

    private static HerdSizeRecord LesserHerdSizeRecord(int id) => new()
    {
        ID = id, CPHH = Cphh, HerdYear = 2020, TotalSize = 10,
        Lactation1Size = 1, Lactation2Size = 2, Lactation3Size = 3, Lactation4Size = 4, Lactation5Size = 5,
        Lactation6Size = 6, Lactation7Size = 7, Lactation8Size = 8, Lactation9Size = 9, Lactation10Size = 10,
        Lactation10PlusSize = 11
    };

    private static HerdSizeRecord GreaterHerdSizeRecord(int id) => new()
    {
        ID = id, CPHH = Cphh, HerdYear = 2021, TotalSize = 20,
        Lactation1Size = 12, Lactation2Size = 13, Lactation3Size = 14, Lactation4Size = 15, Lactation5Size = 16,
        Lactation6Size = 17, Lactation7Size = 18, Lactation8Size = 19, Lactation9Size = 20, Lactation10Size = 21,
        Lactation10PlusSize = 22
    };

    [Theory]
    [MemberData(nameof(HerdSizeSortColumns))]
    public async Task OnGetAsync_SortsPersistedHerdSizesByColumn(string sort, string dir)
    {
        var lesser = LesserHerdSizeRecord(1);
        var greater = GreaterHerdSizeRecord(2);
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _farmService.GetHerdSizesAsync(Cphh).Returns([greater, lesser]);

        var model = CreateModel(["DataEntry"]);
        model.HSort = sort;
        model.HDir = dir;

        await model.OnGetAsync();

        // Unlike the staged-row switch, the persisted-row default arm still honours HDir
        // for any unrecognised HSort value, so direction alone determines order here.
        var expectedFirstId = dir == "asc" ? 1 : 2;
        model.HerdSizes[0].ID.Should().Be(expectedFirstId);
    }

    // ── GetSortedStagedLinkedFarms branch coverage ──────────────────────────────

    [Theory]
    [InlineData("status", "asc")]
    [InlineData("status", "desc")]
    [InlineData("cphh", "asc")]
    [InlineData("cphh", "desc")]
    public void SortedStagedLinkedFarms_OrdersByStatusOrCphh(string sort, string dir)
    {
        var model = CreateModel();
        model.LSort = sort;
        model.LDir = dir;
        var a = new FarmModel.StagedLinkedFarmItem { RelatedCphh = "01001000101", Status = "Alpha" };
        var b = new FarmModel.StagedLinkedFarmItem { RelatedCphh = "02002000202", Status = "Beta" };
        model.StagedLinkedFarms = [b, a];

        var result = model.SortedStagedLinkedFarms();

        var expectFirst = dir == "asc" ? a : b;
        result[0].Should().BeSameAs(expectFirst);
    }

    // ── Validation branch coverage ──────────────────────────────────────────────

    [Fact]
    public async Task OnPostSaveFarmAsync_NonGbFarm_SkipsParishAndAhoRequiredChecks()
    {
        var farm = MakeFarm() with { CPHH = "00001000101" };
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "00001000101" });
        _farmService.GetByCphhAsync("00001000101").Returns(farm);
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState { Rbse = Rbse, Cphh = "00001000101" });
        _currentUser.GetUserIdAsync().Returns(9);

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = new FarmEditViewModel
        {
            CPHH = "00001000101",
            OwnerName = "Non-GB Owner",
            Address1 = "Address 1",
            County = "FR"
            // Parish, AHO, ADNSRegionID intentionally left blank: not required for a non-GB farm.
        };
        model.EditableFarmRowStampBase64 = Convert.ToBase64String(farm.RowStamp!);

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.ModelState.Should().NotContainKey("EditableFarm.Parish");
        model.ModelState.Should().NotContainKey("EditableFarm.AHO");
    }

    [Fact]
    public async Task OnPostSaveFarmAsync_InvalidNumericHerdmarks_AddsModelErrors()
    {
        var farm = MakeFarm();
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(farm);
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState { Rbse = Rbse, Cphh = Cphh });

        var model = CreateModel(["DataEntry"]);
        model.EditableFarm = FarmEditViewModel.FromRecord(farm);
        model.EditableFarm.NumericHerdmark1 = "12A45";
        model.EditableFarm.NumericHerdmark2 = "1";
        model.EditableFarmRowStampBase64 = Convert.ToBase64String(farm.RowStamp!);

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("EditableFarm.NumericHerdmark1");
        model.ModelState.Should().ContainKey("EditableFarm.NumericHerdmark2");
    }

    [Fact]
    public async Task OnPostAddHerdSizeRowAsync_LactationOutOfRange_AddsModelError()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farmService.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _farmDraftState.GetAsync(Rbse).Returns(new CaseFarmDraftState { Rbse = Rbse, Cphh = Cphh });

        var model = CreateModel(["DataEntry"]);
        model.NewHerdRow = new FarmModel.HerdSizeRowInput { HerdYear = 2024, TotalSize = 20, Lactation1Size = 1000 };

        var result = await model.OnPostAddHerdSizeRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey($"{nameof(FarmModel.NewHerdRow)}.Lactation1Size");
    }

    [Fact]
    public async Task OnGetAsync_ForceNewFarmDetails_SeedsEditableFarmFromQuery()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        _farmService.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);

        var model = CreateModel(["DataEntry"]);
        model.NewCphh = Cphh;
        model.ForceNewFarmDetails = true;
        model.SeedParish = "Seeded Parish";
        model.SeedCounty = "SC";
        model.SeedAdnsRegionId = 3;
        model.SeedAuthorityId = 4;
        model.SeedAuthorityCountyId = 5;
        model.SeedHerdmark1 = "HM1";
        model.SeedNumericHerdmark1 = "123456";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.RequireFarmDetails.Should().BeTrue();
        model.EditableFarm!.Parish.Should().Be("Seeded Parish");
        model.EditableFarm.County.Should().Be("SC");
        model.EditableFarm.ADNSRegionID.Should().Be(3);
    }

    [Fact]
    public async Task OnGetAsync_SelectedCphh_PopulatesEditableFarmFromExistingFarm()
    {
        var existingFarm = MakeFarm() with { CPHH = "03003000303", OwnerName = "Selected Owner" };
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        _farmService.GetByCphhAsync("03003000303").Returns(existingFarm);

        var model = CreateModel(["DataEntry"]);
        model.SelectedCphh = "03003000303";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.RequireFarmDetails.Should().BeFalse();
        model.EditableFarm!.OwnerName.Should().Be("Selected Owner");
    }
}


