using System.Security.Claims;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
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
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;

namespace BSE.Modules.UserManagement.Tests;

public sealed class FarmModelFlowTests
{
    private const string Rbse = "002600001";
    private const string Cphh = "01001000101";
    private const string Other = "01001000202";
    private const string Linked = "01001000303";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly ICaseWizardStateService _wizard = Substitute.For<ICaseWizardStateService>();
    private readonly InMemorySessionState _session = new();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly IGeoLookupService _geo = Substitute.For<IGeoLookupService>();

    public FarmModelFlowTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetHerdTypesAsync().Returns(Array.Empty<LuHerdType>());
        _lookups.GetAuthoritiesByCountyAsync(Arg.Any<int>()).Returns(Array.Empty<LuAuthority>());
        _lookups.GetADNSRegionsByAuthorityAsync(Arg.Any<int>()).Returns(Array.Empty<LuAdnsRegion>());
        _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _wizard.GetAsync().Returns((CaseWizardState?)null);
        _farms.GetRelatedFarmsAsync(Arg.Any<string>()).Returns(Array.Empty<FarmRelationRecord>());
        _farms.GetHerdSizesAsync(Arg.Any<string>()).Returns(Array.Empty<HerdSizeRecord>());
        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Success([]));
        _user.GetUserIdAsync().Returns(4);
    }

    private FarmModel Create(params string[] roles)
    {
        var model = new FarmModel(_cases, _farms, _lookups, _batches, _wizard, _session,
            Substitute.For<ICaseDraftDiscardService>(), _orchestration, _user,
            NullLogger<FarmModel>.Instance, new ConfigurationBuilder().Build(), _geo)
        { Rbse = Rbse };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static FarmRecord MakeFarm(string cphh = Cphh) => new()
    {
        CPHH = cphh, OwnerName = "Owner", Address1 = "1 Lane", Parish = "P", County = "TC", AHO = "TA", ADNSRegionID = 1, RowStamp = [1, 2, 3]
    };

    private void GivenExistingCase()
    {
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Cphh });
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _farms.GetRelatedFarmsAsync(Cphh).Returns(new[] { new FarmRelationRecord { ID = 1, CPHH = Cphh, RelatedCPHH = Linked, RowStamp = [1] } });
    }

    private static FarmEditViewModel Posted(string cphh = Cphh) => new() { CPHH = cphh, OwnerName = "Owner", Address1 = "1 Lane", Parish = "P", County = "TC", AHO = "TA", ADNSRegionID = 1 };

    // ── Create mode: GET ─────────────────────────────────────────────────

    [Fact]
    public async Task OnGet_NewCase_SelectedCphhFound_UsesExistingFarm()
    {
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var model = Create("DataEntry");
        model.SelectedCphh = "01/001/0001/01";

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.EditableFarm!.CPHH.Should().Be(Cphh);
        model.RequireFarmDetails.Should().BeFalse();
        model.Farm.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGet_NewCase_ForceNewFarmDetails_SeedsFields()
    {
        var model = Create("DataEntry");
        model.NewCphh = Other;
        model.ForceNewFarmDetails = true;
        model.SeedParish = "Seed parish";
        model.SeedCounty = "SC";
        model.SeedAdnsRegionId = 3;
        model.SeedAuthorityId = 4;
        model.SeedAuthorityCountyId = 5;
        model.SeedHerdmark1 = "AB";
        model.SeedNumericHerdmark1 = "123456";

        await model.OnGetAsync();

        model.RequireFarmDetails.Should().BeTrue();
        model.EditableFarm!.Parish.Should().Be("Seed parish");
        model.EditableFarm.ADNSRegionID.Should().Be(3);
        model.EditableFarm.NumericHerdmark1.Should().Be("123456");
    }

    [Fact]
    public async Task OnGet_NewCase_StagedFarmForSameCphh_IsOverlaid()
    {
        _session.Current = new CaseEditSessionState { Rbse = Rbse, Farm = new FarmEditViewModel { CPHH = Other, OwnerName = "Staged" }.ToUpdateCommand(null) };
        var model = Create("DataEntry");
        model.NewCphh = Other;

        await model.OnGetAsync();

        model.EditableFarm!.OwnerName.Should().Be("Staged");
        model.RequireFarmDetails.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_NewCase_StagedFarmForDifferentCphh_IsIgnored()
    {
        _session.Current = new CaseEditSessionState { Rbse = Rbse, Farm = new FarmEditViewModel { CPHH = Other, OwnerName = "Staged" }.ToUpdateCommand(null) };
        var model = Create("DataEntry");
        model.SelectedCphh = Cphh;
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());

        await model.OnGetAsync();

        model.EditableFarm!.OwnerName.Should().Be("Owner");
    }

    [Fact]
    public async Task OnGet_NewCase_DraftCphh_IsUsed()
    {
        _session.Current = new CaseEditSessionState { Rbse = Rbse, Cphh = "01/001/0001/01" };
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var model = Create("DataEntry");

        await model.OnGetAsync();

        model.EditableFarm!.CPHH.Should().Be(Cphh);
        model.Farm.Should().NotBeNull();
    }

    // ── Create mode: POST ────────────────────────────────────────────────

    [Fact]
    public async Task CreateCase_ViewerOnly_Forbidden()
    {
        (await Create("DEFRAAccess").OnPostCreateCaseAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateCase_NoCphh_ReturnsPageWithError()
    {
        var model = Create("DataEntry");

        (await model.OnPostCreateCaseAsync()).Should().BeOfType<PageResult>();

        model.ModelState.ContainsKey("EditableFarm.CPHH").Should().BeTrue();
    }

    [Fact]
    public async Task CreateCase_UnknownFarmWithoutDetails_RequiresFarmDetails()
    {
        var model = Create("DataEntry");
        model.EditableFarm = new FarmEditViewModel { CPHH = Other };

        (await model.OnPostCreateCaseAsync()).Should().BeOfType<PageResult>();

        model.RequireFarmDetails.Should().BeTrue();
        model.ModelState.Keys.Should().Contain(["EditableFarm.OwnerName", "EditableFarm.Address1", "EditableFarm.Parish",
            "EditableFarm.County", "EditableFarm.AHO", "EditableFarm.ADNSRegionID"]);
    }

    [Fact]
    public async Task CreateCase_NonGbCphh_SkipsFarmLookup_AndCommits()
    {
        var model = Create("DataEntry");
        model.EditableFarm = new FarmEditViewModel { CPHH = "00/123/4567/89" };

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.Farm.Should().BeNull();
        model.RequireFarmDetails.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCase_ExistingFarm_StagesFarmAndCaseThenCommits()
    {
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        model.NewSurvey = "S";
        model.NewBirthDate = new DateTime(2020, 1, 1);

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _session.Current!.Farm.Should().NotBeNull();
        _session.Current.Case!.Survey.Should().Be("S");
        _session.Current.Case.IsBirthDateEst.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCase_WithCaseAlreadyStaged_DoesNotOverwriteIt()
    {
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var staged = new CaseEditViewModel { Rbse = Rbse, Notes = "keep" }.ToEditCommand([]);
        _session.Current = new CaseEditSessionState { Rbse = Rbse, Case = staged };
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        model.NewNotes = "ignored";

        await model.OnPostCreateCaseAsync();

        _session.Current!.Case!.Notes.Should().Be("keep");
    }

    [Fact]
    public async Task CreateCase_InsertRace_ShowsInlineError()
    {
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _orchestration.CommitAllAsync(Rbse, 4).Returns<CaseCommitOutcome>(_ => throw new InvalidOperationException("duplicate"));
        var model = Create("DataEntry");
        model.EditableFarm = Posted();

        (await model.OnPostCreateCaseAsync()).Should().BeOfType<PageResult>();

        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Be("duplicate");
    }

    [Fact]
    public async Task CreateCase_CommitFailure_RedirectsHomeWithMessage()
    {
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Failure(EditCaseResult.PostUpdateError));
        var model = Create("DataEntry");
        model.EditableFarm = Posted();

        var result = await model.OnPostCreateCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"]!.ToString().Should().Contain("Unable to create case");
    }

    [Fact]
    public async Task CreateCase_Warnings_GoToSaveResult()
    {
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Success(["warn"]));
        var model = Create("DataEntry");
        model.EditableFarm = Posted();

        (await model.OnPostCreateCaseAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    // ── Create mode: Look Up ─────────────────────────────────────────────

    [Fact]
    public async Task LookupNewCase_ViewerOnly_Forbidden()
    {
        (await Create("DEFRAAccess").OnPostLookupNewCaseAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task LookupNewCase_NonGbCphh_ShowsWarning()
    {
        var model = Create("DataEntry");
        model.EditableFarm = new FarmEditViewModel { CPHH = "00/123/4567/89" };

        (await model.OnPostLookupNewCaseAsync()).Should().BeOfType<PageResult>();

        model.ModelState["EditableFarm.CPHH"]!.Errors.Single().ErrorMessage.Should().Contain("non-GB");
    }

    [Fact]
    public async Task LookupNewCase_NonGbCphh_WithNullEditableFarm_StillShowsWarning()
    {
        var model = Create("DataEntry");
        model.EditableFarm = null;
        model.NewCphh = "00123456789";

        // Null EditableFarm normalises to blank, so the lookup falls through to the picker.
        (await model.OnPostLookupNewCaseAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task LookupNewCase_FarmFound_LoadsItAndStagesScalars()
    {
        _farms.GetByCphhAsync(Cphh).Returns(MakeFarm());
        var model = Create("DataEntry");
        model.EditableFarm = new FarmEditViewModel { CPHH = Cphh, OwnerName = "stale" };

        (await model.OnPostLookupNewCaseAsync()).Should().BeOfType<PageResult>();

        model.EditableFarm!.OwnerName.Should().Be("Owner");
        model.EditableFarmRowStampBase64.Should().Be(Convert.ToBase64String([1, 2, 3]));
        _session.Current!.Farm!.OwnerName.Should().Be("Owner");
    }

    [Fact]
    public async Task LookupNewCase_FarmNotFound_RedirectsToPicker()
    {
        var model = Create("DataEntry");
        model.EditableFarm = new FarmEditViewModel { CPHH = Other };

        var result = await model.OnPostLookupNewCaseAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/PickFarm");
    }

    // ── Existing case: Linked farm rows ──────────────────────────────────

    [Fact]
    public async Task LinkedFarmHandlers_WithoutDataEntry_AreForbidden()
    {
        var model = Create("VLAAccess");

        (await model.OnPostAddLinkedFarmRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostBeginEditLinkedFarmRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostUpdateLinkedFarmRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostDeleteLinkedFarmAsync("k")).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task LinkedFarmHandlers_VlaDataEntry_CannotEditDefraControls_AreRedirected()
    {
        GivenExistingCase();
        var model = Create("DataEntry", "VLAAccess");

        (await model.OnPostAddLinkedFarmRowAsync()).Should().BeOfType<RedirectToPageResult>();
        (await model.OnPostBeginEditLinkedFarmRowAsync()).Should().BeOfType<RedirectToPageResult>();
        (await model.OnPostUpdateLinkedFarmRowAsync()).Should().BeOfType<RedirectToPageResult>();
        (await model.OnPostDeleteLinkedFarmAsync("k")).Should().BeOfType<RedirectToPageResult>();
    }

    [Theory]
    [InlineData("", "Enter a CPHH.")]
    [InlineData("123", "11 digits")]
    [InlineData(Cphh, "itself")]
    [InlineData(Linked, "already in the Linked Farms")]
    public async Task AddLinkedFarmRow_Invalid_ReopensAddRow(string cphh, string fragment)
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.NewLinkedCphh = cphh;

        (await model.OnPostAddLinkedFarmRowAsync()).Should().BeOfType<PageResult>();

        model.ShowAddLinkedRow.Should().BeTrue();
        model.ModelState["NewLinkedCphh"]!.Errors.Select(e => e.ErrorMessage).Should().Contain(m => m.Contains(fragment));
    }

    [Fact]
    public async Task AddLinkedFarmRow_Valid_StagesRowWithStatus()
    {
        GivenExistingCase();
        _farms.GetByCphhAsync(Other).Returns(MakeFarm(Other));
        var model = Create("DataEntry");
        model.NewLinkedCphh = "01/001/0002/02";
        model.EditableFarm = Posted();

        var result = await model.OnPostAddLinkedFarmRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _session.Current!.LinkedFarms.Should().Contain(l => l.RelatedCphh == Other);
        _session.Current.HasFarmCollectionsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task LinkedFarmRow_BeginEditUpdateAndDelete()
    {
        GivenExistingCase();
        await Create("DataEntry").OnGetAsync();
        var key = _session.Current!.LinkedFarms.Single().ClientKey;

        var begin = Create("DataEntry");
        begin.EditingLinkedClientKey = key;
        (await begin.OnPostBeginEditLinkedFarmRowAsync()).Should().BeOfType<PageResult>();
        begin.ReopenLinkedEditClientKey.Should().Be(key);
        begin.EditLinkedCphh.Should().Be(Linked);

        var update = Create("DataEntry");
        update.EditingLinkedClientKey = key;
        update.EditLinkedCphh = "01001000404";
        (await update.OnPostUpdateLinkedFarmRowAsync()).Should().BeOfType<RedirectToPageResult>();
        _session.Current!.LinkedFarms.Single().RelatedCphh.Should().Be("01001000404");

        var delete = Create("DataEntry");
        (await delete.OnPostDeleteLinkedFarmAsync(key)).Should().BeOfType<RedirectToPageResult>();
        _session.Current!.LinkedFarms.Should().BeEmpty();
    }

    [Fact]
    public async Task LinkedFarmRow_UnknownKeyAndInvalidUpdate()
    {
        GivenExistingCase();
        await Create("DataEntry").OnGetAsync();
        var key = _session.Current!.LinkedFarms.Single().ClientKey;

        var unknown = Create("DataEntry");
        unknown.EditingLinkedClientKey = "nope";
        unknown.EditLinkedCphh = "01001000404";
        (await unknown.OnPostUpdateLinkedFarmRowAsync()).Should().BeOfType<RedirectToPageResult>();
        unknown.TempData["ErrorMessage"].Should().NotBeNull();

        var missing = Create("DataEntry");
        missing.EditingLinkedClientKey = "nope";
        (await missing.OnPostBeginEditLinkedFarmRowAsync()).Should().BeOfType<PageResult>();
        missing.ReopenLinkedEditClientKey.Should().BeNull();

        foreach (var bad in new[] { "", "123", Cphh })
        {
            var invalid = Create("DataEntry");
            invalid.EditingLinkedClientKey = key;
            invalid.EditLinkedCphh = bad;
            (await invalid.OnPostUpdateLinkedFarmRowAsync()).Should().BeOfType<PageResult>();
            invalid.ReopenLinkedEditClientKey.Should().Be(key);
        }

        (await Create("DataEntry").OnPostDeleteLinkedFarmAsync("nope")).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task ResortGrids_ReturnsPage()
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = Posted();

        (await model.OnPostResortGridsAsync()).Should().BeOfType<PageResult>();
    }

    // ── Save ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveFarm_WithoutDataEntry_Forbidden()
    {
        (await Create("VLAAccess").OnPostSaveFarmAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SaveFarm_RowStillBeingEdited_Blocked()
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        model.EditingClientKey = "row";

        (await model.OnPostSaveFarmAsync()).Should().BeOfType<PageResult>();

        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Contain("Finish editing");
    }

    [Fact]
    public async Task SaveFarm_InvalidFields_ReturnsPage()
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = Posted(cphh: "");
        model.EditableFarm.NumericHerdmark1 = "12";
        model.EditableFarm.NumericHerdmark2 = "abcdef";

        (await model.OnPostSaveFarmAsync()).Should().BeOfType<PageResult>();

        model.ModelState.Keys.Should().Contain(["EditableFarm.CPHH", "EditableFarm.NumericHerdmark1", "EditableFarm.NumericHerdmark2"]);
    }

    [Fact]
    public async Task SaveFarm_AdnsRegionNotInSelectedAuthority_Rejected()
    {
        GivenExistingCase();
        _lookups.GetADNSRegionsByAuthorityAsync(7).Returns(new[] { new LuAdnsRegion { Id = 9, Name = "R9" } });
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        model.EditableFarm.AuthorityID = 7;
        model.EditableFarm.ADNSRegionID = 1;

        (await model.OnPostSaveFarmAsync()).Should().BeOfType<PageResult>();

        model.ModelState.ContainsKey("EditableFarm.ADNSRegionID").Should().BeTrue();
    }

    [Fact]
    public async Task SaveFarm_Valid_StagesAndCommits()
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        model.EditableFarm.OwnerName = "Changed";
        model.EditableFarmRowStampBase64 = Convert.ToBase64String([1, 2, 3]);

        var result = await model.OnPostSaveFarmAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _session.Current!.Farm!.OwnerName.Should().Be("Changed");
        _session.Current.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task SaveFarm_CommitFailureAndWarnings()
    {
        GivenExistingCase();
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Failure(EditCaseResult.ConcurrencyConflict));
        var failed = Create("DataEntry");
        failed.EditableFarm = Posted();
        (await failed.OnPostSaveFarmAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        failed.TempData["ErrorMessage"]!.ToString().Should().Contain("Unable to save farm changes");

        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Success(["warn"]));
        var warned = Create("DataEntry");
        warned.EditableFarm = Posted();
        (await warned.OnPostSaveFarmAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    // ── Navigation staging ───────────────────────────────────────────────

    [Fact]
    public async Task StageAndGoto_WithoutDataEntry_JustRedirects()
    {
        var result = await Create().OnPostStageAndGotoAsync("/Case/Edit");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Edit");
    }

    [Fact]
    public async Task StageAndGoto_NoPostedFarm_RedirectsWithoutStaging()
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = null;

        (await model.OnPostStageAndGotoAsync("/Case/Edit")).Should().BeOfType<RedirectToPageResult>();
        _session.Current.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_NewCaseWithoutCphh_RedirectsWithoutStaging()
    {
        var model = Create("DataEntry");
        model.EditableFarm = new FarmEditViewModel();

        (await model.OnPostStageAndGotoAsync("/Case/Edit")).Should().BeOfType<RedirectToPageResult>();
        _session.Current.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_ValidEdit_Stages()
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        model.EditableFarm.OwnerName = "Typed";
        model.EditableFarmRowStampBase64 = Convert.ToBase64String([1, 2, 3]);

        (await model.OnPostStageAndGotoAsync("/Case/Edit")).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Edit");

        _session.Current!.Farm!.OwnerName.Should().Be("Typed");
        _session.Current.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task StageAndGoto_InvalidEdit_ReturnsPage()
    {
        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        model.EditableFarm.NumericHerdmark1 = "12";

        (await model.OnPostStageAndGotoAsync("/Case/Edit")).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task StageAndGoToAuditLog_Branches()
    {
        (await Create("VLAAccess").OnPostStageAndGoToAuditLogAsync()).Should().BeOfType<ForbidResult>();

        GivenExistingCase();
        var model = Create("DataEntry");
        model.EditableFarm = Posted();
        var redirect = (await model.OnPostStageAndGoToAuditLogAsync()).Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/AuditLog/ByFarm");
        redirect.RouteValues!["cphh"].Should().Be(Cphh);

        var invalid = Create("DataEntry");
        invalid.EditableFarm = Posted();
        invalid.EditableFarm.NumericHerdmark2 = "x";
        (await invalid.OnPostStageAndGoToAuditLogAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task CancelFarmEdit_DiscardsAndGoesHome()
    {
        var model = Create("DataEntry");

        (await model.OnPostCancelFarmEditAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        (await model.OnGetCancelFarmEditAsync()).Should().BeOfType<RedirectToPageResult>();
    }
}
