using System.Security.Claims;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Tests;

public sealed class BabPageModelTests
{
    private const string Rbse = "002600001";
    private const string Natal = "01001000202";
    private static readonly string[] DataEntry = ["DataEntry"];
    private static readonly string[] VlaDataEntry = ["DataEntry", "VLAAccess"];
    private static readonly string[] VlaMaintenance = ["DataEntry", "VLAAccess", "VLAMaintenance"];

    private readonly IBabRepository _bab = Substitute.For<IBabRepository>();
    private readonly ICaseRepository _cases = Substitute.For<ICaseRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly ICaseEditSessionStateService _session = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private CaseEditSessionState? _draft;

    public BabPageModelTests()
    {
        _user.GetUserIdAsync().Returns(4);
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "01001000101", BirthDate = new DateTime(2000, 1, 1), Origin = "HB", RowStamp = [1] });
        _bab.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns((IReadOnlyList<BatchNumberEntry>)[new BatchNumberEntry(1, "B1", Rbse, "BSE1")]);
        _lookups.GetAnimalOriginsAsync().Returns(Task.FromResult<IEnumerable<LuAnimalOrigin>>([new LuAnimalOrigin { Code = "P", Description = "Purchased" }]));
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "X", "Ex")]);
        _session.GetAsync(Arg.Any<string>()).Returns(_ => _draft);
        _session.SetAsync(Arg.Any<CaseEditSessionState>()).Returns(ci =>
        {
            _draft = ci.Arg<CaseEditSessionState>();
            return Task.CompletedTask;
        });
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Success([]));
    }

    private BabModel CreateModel(string[]? roles = null, string rbse = Rbse)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build();
        var model = new BabModel(_bab, _cases, _lookups, _batches, _session, _discard, _orchestration, _user, config) { Rbse = rbse };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity((roles ?? []).Select(r => new Claim(ClaimTypes.Role, r)), "Test"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static CaseBabRecord ExistingBab() => new() { Rbse = Rbse, NatalCphh = Natal, Notes = "n", FeedRisk = "F", RowStamp = [5, 6] };

    // ── GET ──

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var result = await CreateModel(DataEntry, "").OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_LoadsRecordLookupsAndRowStamp()
    {
        _bab.GetByRbseAsync(Rbse).Returns(ExistingBab());
        var model = CreateModel(DataEntry);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Bab.NatalCphh.Should().Be(Natal);
        model.RowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 5, 6 }));
        model.SpolSiteUrl.Should().Be("http://spol");
        model.BatchNumbers.Should().ContainSingle();
        model.AnimalOrigins.Should().ContainSingle().Which.Code.Should().Be("P");
        model.FeedRisks.Should().NotBeEmpty();
        model.HorizontalRisks.Should().NotBeEmpty();
        model.MaternalRisks.Should().NotBeEmpty();
        model.Origin.Should().Be("HB");
        model.HasPurchaseData.Should().BeFalse();
        model.CanEditBabControls.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_NoBabRow_HasNullRowStamp()
    {
        var model = CreateModel(DataEntry);

        await model.OnGetAsync();

        model.RowStampBase64.Should().BeNull();
        model.Bab.NatalCphh.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_StagedBabOverlaysPersisted()
    {
        _bab.GetByRbseAsync(Rbse).Returns(ExistingBab());
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse,
            Bab = new EditCaseBabCommand(Rbse, null, "staged", null, null, null, null, null, "SF", null, null, [])
        };
        var model = CreateModel(DataEntry);

        await model.OnGetAsync();

        model.Bab.Notes.Should().Be("staged");
        model.Bab.FeedRisk.Should().Be("SF");
        model.Bab.NatalCphh.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_StagedCaseScalars_DriveOriginPurchaseAndEditability()
    {
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse,
            Case = BuildEditCommand(origin: "P", birthDate: null, purchaseCounty: "Kent")
        };
        var model = CreateModel(DataEntry);

        await model.OnGetAsync();

        model.Origin.Should().Be("P");
        model.HasPurchaseData.Should().BeTrue();
        model.CanEditBabControls.Should().BeFalse();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("DataEntry", true)]
    [InlineData("DataEntry,VLAAccess", false)]
    [InlineData("DataEntry,VLAAccess,VLAMaintenance", true)]
    public async Task OnGet_EditPermissionByRole(string roles, bool expected)
    {
        var model = CreateModel(roles.Length == 0 ? [] : roles.Split(','));

        await model.OnGetAsync();

        model.CanEditBabControls.Should().Be(expected);
    }

    [Fact]
    public async Task OnGet_OldBirthDateWithoutBabRow_ReadOnly_ButEditableWhenBabExists()
    {
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, BirthDate = new DateTime(1985, 1, 1) });
        var model = CreateModel(DataEntry);
        await model.OnGetAsync();
        model.CanEditBabControls.Should().BeFalse();

        _bab.GetByRbseAsync(Rbse).Returns(ExistingBab());
        model = CreateModel(DataEntry);
        await model.OnGetAsync();
        model.CanEditBabControls.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_NoBirthDate_ReadOnly()
    {
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse });
        var model = CreateModel(DataEntry);

        await model.OnGetAsync();

        model.CanEditBabControls.Should().BeFalse();
    }

    // ── Save ──

    [Fact]
    public async Task Save_WithoutDataEntry_Forbids()
    {
        var result = await CreateModel().OnPostSaveBabAsync(null);

        result.Should().BeOfType<ForbidResult>();
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task Save_ReadOnlyTab_CommitsOtherStagedChangesWithoutStagingBab()
    {
        var model = CreateModel(VlaDataEntry);

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _orchestration.Received(1).CommitAllAsync(Rbse, 4);
        await _session.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Fact]
    public async Task Save_ReadOnlyTab_MandatoryMissing_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 4).Returns<Task<CaseCommitOutcome>>(_ => throw new MandatoryCaseFieldsMissingException(["Owner"]));

        var result = await CreateModel(VlaDataEntry).OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task Save_ReadOnlyTab_Failure_RedirectsHomeWithError()
    {
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Failure(EditCaseResult.PostUpdateError));
        var model = CreateModel(VlaDataEntry);

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"]!.ToString().Should().StartWith("Unable to save BAB changes");
    }

    [Fact]
    public async Task Save_ReadOnlyTab_Warnings_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Success(["w"]));

        var result = await CreateModel(VlaDataEntry).OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task Save_InvalidNatalCphh_ReturnsPageWithError()
    {
        var model = CreateModel(DataEntry);
        model.Origin = "P";
        model.Bab.NatalCphh = "01/001";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<PageResult>();
        model.ModelState["Bab.NatalCphh"]!.Errors.Should().NotBeEmpty();
        model.Bab.NatalCphh.Should().Be("01001");
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task Save_PurchasedOrigin_StagesBabAndCaseOriginThenCommitsAndRedirectsHome()
    {
        var model = CreateModel(DataEntry);
        model.Origin = "P";
        model.Bab.NatalCphh = "01/001/0002/02";
        model.Bab.TracedName = "Trader";
        model.Bab.Notes = "notes";

        var result = await model.OnPostSaveBabAsync(Convert.ToBase64String(new byte[] { 9 }));

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _draft.Should().NotBeNull();
        _draft!.Bab!.NatalCphh.Should().Be(Natal);
        _draft.Bab.TracedName.Should().Be("Trader");
        _draft.Bab.RowStamp.Should().Equal(9);
        _draft.BabOrigin.Should().Be("P");
        _draft.Case!.Origin.Should().Be("P");
        _draft.HasPendingChanges.Should().BeTrue();
        await _orchestration.Received(1).CommitAllAsync(Rbse, 4);
    }

    [Fact]
    public async Task Save_NonPurchasedOrigin_ClearsTracedFieldsAndPurchaseData()
    {
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord
        {
            Rbse = Rbse, Cphh = "01001000101", BirthDate = new DateTime(2000, 1, 1), Origin = "P",
            PurchaseDate = new DateTime(2010, 1, 1), PurchasedCounty = "Kent", PurchaseAgeInMonths = 4, RowStamp = [1]
        });
        var model = CreateModel(DataEntry);
        model.Origin = "HB";
        model.Bab.NatalCphh = Natal;
        model.Bab.TracedName = "Trader";
        model.Bab.TracedPostcode = "AB1";
        model.Bab.Notes = "keep";

        await model.OnPostSaveBabAsync(null);

        _draft!.Bab!.NatalCphh.Should().BeNull();
        _draft.Bab.TracedName.Should().BeNull();
        _draft.Bab.TracedPostcode.Should().BeNull();
        _draft.Bab.Notes.Should().Be("keep");
        _draft.Case!.PurchaseDate.Should().BeNull();
        _draft.Case.PurchasedCounty.Should().BeNull();
        _draft.Case.PurchaseAgeInMonths.Should().BeNull();
        _draft.Case.Origin.Should().Be("HB");
    }

    [Fact]
    public async Task Save_NonPurchasedOrigin_StagedCaseWithPurchaseData_IsCleared()
    {
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse,
            Case = BuildEditCommand(origin: "P", birthDate: new DateTime(2000, 1, 1), purchaseCounty: "Kent"),
            CaseBaseRowStampBase64 = Convert.ToBase64String(new byte[] { 1 })
        };
        var model = CreateModel(DataEntry);
        model.Origin = "HB";

        await model.OnPostSaveBabAsync(null);

        _draft!.Case!.PurchasedCounty.Should().BeNull();
        _draft.Case.Origin.Should().Be("HB");
    }

    [Fact]
    public async Task Save_NonPurchasedOrigin_NothingToClear_DoesNotTouchPurchaseFields()
    {
        var model = CreateModel(DataEntry);
        model.Origin = "HB";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        _draft!.Case.Should().BeNull();
    }

    [Fact]
    public async Task Save_UnknownCaseCannotEdit_DoesNotStage()
    {
        _cases.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        var model = CreateModel(DataEntry);

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _session.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Fact]
    public async Task Save_CommitFailure_RedirectsHomeWithError()
    {
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Failure(EditCaseResult.ConcurrencyConflict));
        var model = CreateModel(DataEntry);
        model.Origin = "HB";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Save_CommitWarnings_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 4).Returns(CaseCommitOutcome.Success(["w"]));
        var model = CreateModel(DataEntry);
        model.Origin = "HB";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    // ── Stage and goto ──

    [Fact]
    public async Task StageAndGoto_WithoutDataEntry_RedirectsWithoutLoading()
    {
        var result = await CreateModel().OnPostStageAndGotoAsync("/Case/Vla", null);

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Vla");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
        await _bab.DidNotReceiveWithAnyArgs().GetByRbseAsync(default!);
    }

    [Fact]
    public async Task StageAndGoto_ReadOnly_RedirectsWithoutStaging()
    {
        var result = await CreateModel(VlaDataEntry).OnPostStageAndGotoAsync("/Case/Vla", null);

        result.Should().BeOfType<RedirectToPageResult>();
        await _session.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Fact]
    public async Task StageAndGoto_InvalidNatalCphh_ReturnsPage()
    {
        var model = CreateModel(VlaMaintenance);
        model.Origin = "P";
        model.Bab.NatalCphh = "123";

        var result = await model.OnPostStageAndGotoAsync("/Case/Vla", null);

        result.Should().BeOfType<PageResult>();
        model.ModelState["Bab.NatalCphh"]!.Errors.Should().NotBeEmpty();
        model.SpolSiteUrl.Should().Be("http://spol");
    }

    [Fact]
    public async Task StageAndGoto_UntouchedForm_StagesWithoutMarkingPending()
    {
        var model = CreateModel(DataEntry);
        model.Origin = "HB";

        var result = await model.OnPostStageAndGotoAsync("/Case/Vla", null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Vla");
        _draft.Should().NotBeNull();
        _draft!.Bab.Should().NotBeNull();
        _draft.HasPendingChanges.Should().BeFalse();
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task StageAndGoto_EditedField_MarksPending()
    {
        var model = CreateModel(DataEntry);
        model.Origin = "HB";
        model.Bab.Notes = "typed";

        await model.OnPostStageAndGotoAsync("/Case/Vla", null);

        _draft!.Bab!.Notes.Should().Be("typed");
        _draft.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task StageAndGoto_OriginChanged_MarksPendingAndStagesCaseOrigin()
    {
        var model = CreateModel(DataEntry);
        model.Origin = "P";

        await model.OnPostStageAndGotoAsync("/Case/Vla", null);

        _draft!.HasPendingChanges.Should().BeTrue();
        _draft.Case!.Origin.Should().Be("P");
        _draft.BabOrigin.Should().Be("P");
    }

    [Fact]
    public async Task StageAndGoto_ExistingBabUnchanged_NotPending()
    {
        var existing = ExistingBab();
        _bab.GetByRbseAsync(Rbse).Returns(existing);
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, BirthDate = new DateTime(2000, 1, 1), Origin = "P" });
        var model = CreateModel(DataEntry);
        model.Origin = "P";
        model.Bab = BabModel.BabFormViewModel.FromRecord(existing);

        await model.OnPostStageAndGotoAsync("/Case/Vla", Convert.ToBase64String(new byte[] { 5, 6 }));

        _draft!.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task StageAndGoto_NonPurchasedOrigin_ClearsTracedFields()
    {
        var model = CreateModel(DataEntry);
        model.Origin = "HB";
        model.Bab.TracedName = "Trader";
        model.Bab.NatalCphh = Natal;

        await model.OnPostStageAndGotoAsync("/Case/Vla", null);

        _draft!.Bab!.TracedName.Should().BeNull();
        _draft.Bab.NatalCphh.Should().BeNull();
    }

    // ── Cancel ──

    [Fact]
    public async Task CancelBabEdit_DiscardsDraftAndRedirectsHome()
    {
        var result = await CreateModel(DataEntry).OnGetCancelBabEdit();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _discard.Received(1).DiscardAllAsync(Rbse);
    }

    // ── helpers ──

    private static BSE.Modules.CaseManagement.Commands.EditCaseCommand BuildEditCommand(string? origin, DateTime? birthDate, string? purchaseCounty) =>
        new BSE.Host.Models.ViewModels.CaseEditViewModel { Rbse = Rbse, Origin = origin, BirthDate = birthDate, PurchasedCounty = purchaseCounty }
            .ToEditCommand([1]);
}
