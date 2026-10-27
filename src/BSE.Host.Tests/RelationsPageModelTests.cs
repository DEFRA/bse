using System.Security.Claims;
using System.Text.Json;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using DamSireRecord = BSE.Modules.AnimalRelations.Models.DamSireDetailRecord;
using RelationRecord = BSE.Modules.AnimalRelations.Models.CaseRelationRecord;

namespace BSE.Host.Tests;

public sealed class RelationsPageModelTests
{
    private const string Rbse = "002600001";
    private static readonly string[] AllRoles = ["DataEntry", "VLAAccess"];

    private readonly IAnimalRelationsRepository _relations = Substitute.For<IAnimalRelationsRepository>();
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batch = Substitute.For<IBatchRepository>();
    private readonly ICaseEditSessionStateService _sessionState = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private CaseEditSessionState? _draft;
    private RelationDetailsRecord _details = new(null, null, []);

    public RelationsPageModelTests()
    {
        _lookups.GetLookupAsync(LookupTableId.RelationType).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "S", "Sibling")]);
        _lookups.GetLookupAsync(LookupTableId.RelationFate).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "D", "Died")]);
        _lookups.GetSexesAsync().Returns((IEnumerable<LuSex>)[new LuSex { Id = 1, Code = "M", Description = "Male" }]);
        _lookups.GetAnimalStatusesAsync().Returns((IEnumerable<LuAnimalStatus>)[new LuAnimalStatus { Id = 1, Code = "A", Description = "Alive" }]);
        _batch.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns((IReadOnlyList<BatchNumberEntry>)[new BatchNumberEntry(1, "B1", Rbse, "BSE1")]);
        _relations.GetRelationsDetailsByRbseAsync(Arg.Any<string>()).Returns(_ => _details);
        _caseService.GetCaseAsync(Arg.Any<string>()).Returns(new CaseRecord
        {
            Rbse = Rbse, Cphh = "01001000101", DamStatus = "A", Herdbook = "HB", RowStamp = [1], PedigreeRowStamp = [2]
        });
        _sessionState.GetAsync(Arg.Any<string>()).Returns(_ => _draft);
        _sessionState.SetAsync(Arg.Any<CaseEditSessionState>()).Returns(ci =>
        {
            _draft = ci.Arg<CaseEditSessionState>();
            return Task.CompletedTask;
        });
        _currentUser.GetUserIdAsync().Returns(5);
    }

    private RelationsModel CreateModel(string[]? roles = null, string rbse = Rbse, string? spol = "http://spol")
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            spol is null ? [] : new Dictionary<string, string?> { ["SpolSiteUrl"] = spol }).Build();
        var model = new RelationsModel(_relations, _caseService, _lookups, _batch, _sessionState, _discard,
            _orchestration, _connectionFactory, _currentUser, NullLogger<RelationsModel>.Instance, config)
        { Rbse = rbse };

        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity((roles ?? []).Select(r => new Claim(ClaimTypes.Role, r)), "Test"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static RelationRecord Rel(int id, string rbse, string? type = "S") => new()
    {
        Id = id, RelationType = type, RelationRbse = rbse, Sex = "M", BirthDay = 1, BirthMonth = 2, BirthYear = 2000, RowStamp = [1]
    };

    private static DamSireRecord Parent(int id, string rbse) => new()
    {
        Id = id, Rbse = rbse, Eartag = "E" + id, Name = "N" + id, Herdbook = "H", BirthDay = 1, BirthMonth = 2, BirthYear = 2000,
        Fate = "F", FinalResult = "R", ChildCount = 2, RowStamp = [9]
    };

    private static CaseEditSessionState DraftWith(params CaseRelationsDraftItem[] items) =>
        new() { Rbse = Rbse, Relations = [.. items], HasRelationsChanges = true };

    // ── GET ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var result = await CreateModel(rbse: " ").OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_LoadsDetailsInitialisesDraftAndPopulatesParents()
    {
        _details = new RelationDetailsRecord(Parent(10, "002700010"), Parent(11, "002700011"), [Rel(1, "002700012")]);
        var model = CreateModel();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.SpolSiteUrl.Should().Be("http://spol");
        model.BatchNumbers.Should().ContainSingle();
        model.TotalRelationsCount.Should().Be(1);
        model.PagedRelations.Single().RelationRbse.Should().Be("002700012");
        model.DamSire.HasDam.Should().BeTrue();
        model.DamSire.DamId.Should().Be(10);
        model.DamSire.DamRowStamp.Should().Be(Convert.ToBase64String(new byte[] { 9 }));
        model.DamSire.HasSire.Should().BeTrue();
        model.DamSire.SireName.Should().Be("N11");
        model.CaseHerdbook.Should().Be("HB");
        model.DamStatusDescription.Should().Be("Alive");
        model.DamSire.DamStatus.Should().Be("A");
        model.DamStatusOptions.Should().ContainSingle();
        _draft!.Relations.Should().ContainSingle().Which.RowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 1 }));
        model.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_MissingSpolSetting_UsesEmptyUrl()
    {
        var model = CreateModel(spol: null);

        await model.OnGetAsync();

        model.SpolSiteUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_DamStatusNotInLookup_FallsBackToCode()
    {
        _caseService.GetCaseAsync(Arg.Any<string>()).Returns(new CaseRecord { Rbse = Rbse, DamStatus = "ZZ" });
        var model = CreateModel();

        await model.OnGetAsync();

        model.DamStatusDescription.Should().Be("ZZ");
    }

    [Fact]
    public async Task OnGet_NoCaseRecord_LeavesDamStatusEmpty()
    {
        _caseService.GetCaseAsync(Arg.Any<string>()).Returns((CaseRecord?)null);
        var model = CreateModel();

        await model.OnGetAsync();

        model.DamStatusDescription.Should().BeNull();
        model.CaseHerdbook.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_StagedCaseAndHerdbookOverlayPersistedValues()
    {
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse,
            Case = BSE.Host.Models.ViewModels.CaseEditViewModel.FromRecord(new CaseRecord { Rbse = Rbse }).ToEditCommand([1]) with { DamStatus = "STAGED" },
            DamSire = Blank<BSE.Modules.CaseManagement.Commands.AddEditDamSireCommand>() with { CaseHerdbook = "STAGED-HB" },
            HasRelationsChanges = true
        };
        var model = CreateModel();

        await model.OnGetAsync();

        model.DamSire.DamStatus.Should().Be("STAGED");
        model.CaseHerdbook.Should().Be("STAGED-HB");
        model.HasUnsavedChanges.Should().BeTrue();
    }

    private static T Blank<T>() where T : class => (T)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(T));

    [Fact]
    public async Task OnGetEditCaseHerdbook_SetsFlag()
    {
        var model = CreateModel();

        await model.OnGetEditCaseHerdbookAsync();

        model.EditCaseHerdbook.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_RemovePendingFlags_ClearDamAndSire()
    {
        _details = new RelationDetailsRecord(Parent(10, "002700010"), Parent(11, "002700011"), []);
        _draft = new CaseEditSessionState { Rbse = Rbse, RemoveDamPending = true, RemoveSirePending = true, HasRelationsChanges = true };
        var model = CreateModel();

        await model.OnGetAsync();

        model.DamSire.HasDam.Should().BeFalse();
        model.DamSire.DamId.Should().Be(0);
        model.DamSire.HasSire.Should().BeFalse();
        model.DamSire.SireRbse.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_PendingParentsInDraft_OverrideDetails()
    {
        _details = new RelationDetailsRecord(Parent(10, "002700010"), null, []);
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse,
            HasRelationsChanges = true,
            PendingDam = new PendingParentDraft { Id = 50, Rbse = "002700050", Name = "PD", RowStampBase64 = "AQ==" },
            PendingSire = new PendingParentDraft { Id = 60, Rbse = "002700060", Name = "PS", ChildCount = 4 }
        };
        var model = CreateModel();

        await model.OnGetAsync();

        model.DamSire.DamId.Should().Be(50);
        model.DamSire.DamName.Should().Be("PD");
        model.DamSire.DamRowStamp.Should().Be("AQ==");
        model.DamSire.SireId.Should().Be(60);
        model.DamSire.SireChildCount.Should().Be(4);
    }

    [Fact]
    public async Task OnGet_PendingDamFromTempData_IsAppliedAndClearsRemoveFlag()
    {
        _draft = new CaseEditSessionState { Rbse = Rbse, RemoveDamPending = true, RemoveSirePending = true, HasRelationsChanges = true };
        var model = CreateModel();
        model.TempData[PendingDamSireKeys.Dam] = JsonSerializer.Serialize(
            new PendingDamSire(77, "002700077", "E", "Dam", "H", 1, 2, 2000, "AQ==", "F", "R", 3));
        model.TempData[PendingDamSireKeys.Sire] = JsonSerializer.Serialize(
            new PendingDamSire(78, "002700078", "E", "Sire", "H", 1, 2, 2000, "Ag==", "F", null, 1));

        await model.OnGetAsync();

        model.DamSire.DamId.Should().Be(77);
        model.DamSire.DamFinalResult.Should().Be("R");
        model.DamSire.SireId.Should().Be(78);
        _draft!.PendingDam!.Id.Should().Be(77);
        _draft.PendingSire!.Id.Should().Be(78);
        _draft.RemoveDamPending.Should().BeFalse();
        _draft.RemoveSirePending.Should().BeFalse();
        _draft.HasRelationsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_PendingTempDataSameAsCase_SetsErrorAndSkips()
    {
        var model = CreateModel();
        model.TempData[PendingDamSireKeys.Dam] = JsonSerializer.Serialize(
            new PendingDamSire(77, Rbse, null, null, null, null, null, null, null, null, null, null));

        await model.OnGetAsync();

        model.DamError.Should().Be(RelationsModel.SameAsCaseRbse);
        model.DamSire.DamId.Should().Be(0);
    }

    [Fact]
    public async Task OnGet_InvalidPendingTempDataJson_IsIgnored()
    {
        var model = CreateModel();
        model.TempData[PendingDamSireKeys.Sire] = "{not json";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.DamSire.HasSire.Should().BeFalse();
    }

    [Theory]
    [InlineData("RelationType", false)]
    [InlineData("RelationRbse", true)]
    [InlineData("Sex", false)]
    [InlineData("BirthDate", true)]
    [InlineData("RelationFate", false)]
    [InlineData("LeftDate", true)]
    [InlineData("Eartag", false)]
    [InlineData("Sire", true)]
    [InlineData("Unknown", false)]
    public async Task OnGet_SortsByRequestedColumn(string column, bool desc)
    {
        _details = new RelationDetailsRecord(null, null,
        [
            new RelationRecord { Id = 1, RelationType = "A", RelationRbse = "1", Sex = "F", BirthYear = 2001, LeftDate = new DateTime(2020, 1, 1), Eartag = "B", Sire = "Z", RelationFate = "X" },
            new RelationRecord { Id = 2, RelationType = "B", RelationRbse = "2", Sex = "M", BirthYear = 2002, LeftDate = new DateTime(2021, 1, 1), Eartag = "A", Sire = "Y", RelationFate = "W" },
        ]);
        var model = CreateModel();
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.PagedRelations.Should().HaveCount(2);
        if (column == "Unknown")
            model.PagedRelations.Select(r => r.Id).Should().Equal(1, 2);
        else if (column is "RelationType" or "RelationRbse" or "BirthDate" or "LeftDate" or "Sex")
            model.PagedRelations.Select(r => r.Id).Should().Equal(desc ? [2, 1] : [1, 2]);
        else
            model.PagedRelations.Select(r => r.Id).Should().Equal(desc ? [1, 2] : [2, 1]);
    }

    [Fact]
    public async Task OnGet_PagesRelationsAndClampsPageNumber()
    {
        _details = new RelationDetailsRecord(null, null, Enumerable.Range(1, 25).Select(i => Rel(i, "R" + i)).ToList());
        var model = CreateModel();
        model.PageNumber = 99;

        await model.OnGetAsync();

        model.TotalPages.Should().Be(3);
        model.PageNumber.Should().Be(3);
        model.PagedRelations.Should().HaveCount(5);
    }

    [Fact]
    public async Task OnGetCancel_DiscardsDraftAndRedirectsHome()
    {
        var result = await CreateModel().OnGetCancelRelationsEditAsync();

        await _discard.Received(1).DiscardAllAsync(Rbse);
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnPostCancel_DiscardsDraftAndRedirectsHome()
    {
        var result = await CreateModel().OnPostCancelRelationsEditAsync();

        await _discard.Received(1).DiscardAllAsync(Rbse);
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public void OnPostDeleteRelation_Superseded_RedirectsBack()
    {
        var result = CreateModel().OnPostDeleteRelationAsync(1, "x");

        result.Should().BeOfType<RedirectToPageResult>().Which.RouteValues!["rbse"].Should().Be(Rbse);
    }

    // ── Look Up ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LookUpDam_SearchRbseIsAlreadyOtherParent_SetsAlreadyRelationError()
    {
        _details = new RelationDetailsRecord(null, Parent(11, "002700011"), []);
        var model = CreateModel();
        model.DamSire.DamSearchRbse = "002700011";

        var result = await model.OnPostLookUpDamAsync();

        result.Should().BeOfType<PageResult>();
        model.DamError.Should().Be(RelationsModel.AlreadyARelation);
        model.DamSire.HasSire.Should().BeTrue();
    }

    [Fact]
    public async Task LookUpDam_SearchRbseIsStagedRelation_SetsAlreadyRelationError()
    {
        _details = new RelationDetailsRecord(null, null, [Rel(1, "002700009")]);
        var model = CreateModel();
        model.DamSire.DamSearchRbse = "002700009";

        await model.OnPostLookUpDamAsync();

        model.DamError.Should().Be(RelationsModel.AlreadyARelation);
    }

    [Fact]
    public async Task LookUpDam_SingleMatch_StagesPendingDamInDraftAndRedirects()
    {
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700002", null, "F")
            .Returns((IReadOnlyList<DamSireRecord>)[Parent(42, "002700002")]);
        var model = CreateModel();
        model.DamSire.DamSearchRbse = "002700002";

        var result = await model.OnPostLookUpDamAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().BeNull();
        _draft!.PendingDam!.Id.Should().Be(42);
        _draft.PendingDam.FinalResult.Should().Be("R");
        _draft.RemoveDamPending.Should().BeFalse();
    }

    [Fact]
    public async Task LookUpSire_SingleMatch_StagesPendingSireWithoutFinalResult()
    {
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700003", null, "M")
            .Returns((IReadOnlyList<DamSireRecord>)[Parent(43, "002700003")]);
        var model = CreateModel(AllRoles);
        model.DamSire.SireSearchRbse = "002700003";

        await model.OnPostLookUpSireAsync();

        _draft!.PendingSire!.Id.Should().Be(43);
        _draft.PendingSire.FinalResult.Should().BeNull();
    }

    [Fact]
    public async Task LookUpSire_NoMatch_SetsSireNotFoundAndRehydratesDam()
    {
        _details = new RelationDetailsRecord(Parent(10, "002700010"), null, []);
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700004", null, "M")
            .Returns((IReadOnlyList<DamSireRecord>)[]);
        var model = CreateModel(AllRoles);
        model.DamSire.SireSearchRbse = "002700004";

        var result = await model.OnPostLookUpSireAsync();

        result.Should().BeOfType<PageResult>();
        model.SireError.Should().Be(RelationsModel.SireNotFound);
        model.DamSire.DamId.Should().Be(10);
        model.DamSire.HasSire.Should().BeFalse();
    }

    [Fact]
    public async Task LookUpDam_NoMatch_RehydratesSireFromPendingDraft()
    {
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse, HasRelationsChanges = true, RemoveSirePending = true,
        };
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700004", null, "F")
            .Returns((IReadOnlyList<DamSireRecord>)[]);
        _details = new RelationDetailsRecord(null, Parent(11, "002700011"), []);
        var model = CreateModel();
        model.DamSire.DamSearchRbse = "002700004";

        await model.OnPostLookUpDamAsync();

        model.DamError.Should().Be(RelationsModel.DamNotFound);
        model.DamSire.HasSire.Should().BeFalse();
    }

    [Fact]
    public async Task LookUpSire_BlankRbse_RedirectsToPickerWithoutRbseRoute()
    {
        var model = CreateModel(AllRoles);

        var result = await model.OnPostLookUpSireAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/PickSireDam");
        redirect.RouteValues!["sex"].Should().Be("M");
        model.TempData[PickSireDamContextKeys.ReturnTo].Should().Be("Relations");
    }

    // ── Dam/Sire save + remove ───────────────────────────────────────────

    [Theory]
    [InlineData(nameof(RelationsModel.OnPostSaveDamSireAsync))]
    [InlineData(nameof(RelationsModel.OnPostSaveCaseHerdbookAsync))]
    public async Task SaveDamSireOrHerdbook_WithoutDataEntry_Forbids(string handler)
    {
        var model = CreateModel();

        var result = await (Task<IActionResult>)typeof(RelationsModel).GetMethod(handler)!.Invoke(model, null)!;

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SaveDamSire_StagesHerdbookDamStatusAndMarksDraftPending()
    {
        var model = CreateModel(["DataEntry"]);
        model.CaseHerdbook = "NEW-HB";
        model.DamSire.DamStatus = "A";
        model.DamSire.HasDam = true;
        model.DamSire.DamName = "Bess";
        model.TempData[SuccessKeyName] = "x";

        var result = await model.OnPostSaveDamSireAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _draft!.HasRelationsChanges.Should().BeTrue();
        _draft.HasPendingChanges.Should().BeTrue();
        _draft.Case!.DamStatus.Should().Be("A");
        _draft.DamSire!.CaseHerdbook.Should().Be("NEW-HB");
        _draft.DamSire.DamName.Should().Be("Bess");
        model.TempData.ContainsKey(SuccessKeyName).Should().BeFalse();
    }

    private const string SuccessKeyName = "Success";

    [Fact]
    public async Task StageAndGoto_WithoutDataEntry_RedirectsWithoutStaging()
    {
        var model = CreateModel();

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _sessionState.DidNotReceive().SetAsync(Arg.Any<CaseEditSessionState>());
    }

    [Fact]
    public async Task StageAndGoto_UnchangedValues_DoesNotStage()
    {
        var model = CreateModel(["DataEntry"]);
        model.CaseHerdbook = "HB";
        model.DamSire.DamStatus = "A";

        await model.OnPostStageAndGotoAsync("/Case/Farm");

        await _sessionState.DidNotReceive().SetAsync(Arg.Any<CaseEditSessionState>());
    }

    [Fact]
    public async Task StageAndGoto_ChangedValues_StagesDamStatusAndHerdbook()
    {
        var model = CreateModel(["DataEntry"]);
        model.CaseHerdbook = "CHANGED";
        model.DamSire.DamStatus = "A";

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _draft!.DamSire!.CaseHerdbook.Should().Be("CHANGED");
    }

    [Fact]
    public async Task ResortRelations_WithoutDataEntry_RedirectsToSamePage()
    {
        var result = await CreateModel().OnPostResortRelationsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().BeNull();
    }

    [Fact]
    public async Task ResortRelations_WithDataEntry_StagesChangesAndRendersPage()
    {
        _details = new RelationDetailsRecord(null, null, [Rel(1, "R1")]);
        var model = CreateModel(["DataEntry"]);
        model.CaseHerdbook = "CHANGED";
        model.DamSire.DamStatus = "A";

        var result = await model.OnPostResortRelationsAsync();

        result.Should().BeOfType<PageResult>();
        _draft!.DamSire!.CaseHerdbook.Should().Be("CHANGED");
        model.PagedRelations.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveDam_WithoutRole_Forbids()
    {
        (await CreateModel().OnPostRemoveDamAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RemoveDam_FlagsDraftClearsDamStatusAndTempData()
    {
        var model = CreateModel(["DataEntry"]);
        model.DamSire.DamStatus = "A";
        model.TempData[PendingDamSireKeys.Dam] = "x";

        var result = await model.OnPostRemoveDamAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _draft!.RemoveDamPending.Should().BeTrue();
        _draft.PendingDam.Should().BeNull();
        _draft.HasRelationsChanges.Should().BeTrue();
        _draft.Case!.DamStatus.Should().BeNull();
        model.TempData.ContainsKey(PendingDamSireKeys.Dam).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveSire_RequiresBothRoles()
    {
        (await CreateModel(["DataEntry"]).OnPostRemoveSireAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RemoveSire_FlagsDraft()
    {
        var model = CreateModel(AllRoles);
        model.TempData[PendingDamSireKeys.Sire] = "x";

        var result = await model.OnPostRemoveSireAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _draft!.RemoveSirePending.Should().BeTrue();
        _draft.PendingSire.Should().BeNull();
        model.TempData.ContainsKey(PendingDamSireKeys.Sire).Should().BeFalse();
    }

    // ── Relation rows ───────────────────────────────────────────────────

    [Theory]
    [InlineData(nameof(RelationsModel.OnPostAddRelationRowAsync), null)]
    [InlineData(nameof(RelationsModel.OnPostUpdateRelationRowAsync), null)]
    [InlineData(nameof(RelationsModel.OnPostBeginEditRelationRowAsync), "k")]
    [InlineData(nameof(RelationsModel.OnPostDeleteRelationRowAsync), "k")]
    public async Task RelationRowHandlers_RequireBothRoles(string handler, string? arg)
    {
        var model = CreateModel(["DataEntry"]);
        var method = typeof(RelationsModel).GetMethod(handler)!;

        var result = await (Task<IActionResult>)method.Invoke(model, arg is null ? null : [arg])!;

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddRelationRow_MissingRelationTypeAndSex_ShowsErrorsAndDoesNotAdd()
    {
        var model = CreateModel(AllRoles);

        var result = await model.OnPostAddRelationRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ShowAddRelationRow.Should().BeTrue();
        model.RelationFieldErrors.Should().ContainKeys("RelationType", "Sex");
        _draft!.Relations.Should().BeEmpty();
    }

    [Fact]
    public async Task AddRelationRow_Valid_StagesNewRowWithDescriptions()
    {
        var model = CreateModel(AllRoles);
        model.RelationType = "S";
        model.Sex = "M";
        model.RelationFate = "D";
        model.BirthDate = new DateTime(2001, 5, 17);
        model.TempData[SuccessKeyName] = "x";

        var result = await model.OnPostAddRelationRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var row = _draft!.Relations.Should().ContainSingle().Subject;
        row.Id.Should().BeNull();
        row.RelationTypeDesc.Should().Be("Sibling");
        row.SexDesc.Should().Be("Male");
        row.RelationFateDesc.Should().Be("Died");
        (row.BirthDay, row.BirthMonth, row.BirthYear).Should().Be((17, 5, 2001));
        row.RelationRbse.Should().BeNull();
        _draft.HasRelationsChanges.Should().BeTrue();
        model.TempData.ContainsKey(SuccessKeyName).Should().BeFalse();
    }

    [Fact]
    public async Task AddRelationRow_WithRelationRbse_TakesFieldsFromRelatedCase()
    {
        _relations.GetRelationDetailsOfRelatedCaseAsync("002700020").Returns(new RelatedCaseDetailsRecord
        {
            RelationRbse = "002700020", Sex = "M", Fate = "D", FateDesc = "Died", EartagCountry = "UK", EartagHerdmark = "123",
            Eartag = "9", BirthDay = 3, BirthMonth = 4, BirthYear = 1999, LeftDate = "01/02/2021", Name = "SireName"
        });
        var model = CreateModel(AllRoles);
        model.RelationType = "S";
        model.RelationRbse = "002700020";

        var result = await model.OnPostAddRelationRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var row = _draft!.Relations.Should().ContainSingle().Subject;
        row.RelationRbse.Should().Be("002700020");
        row.Sex.Should().Be("M");
        row.RelationFateDesc.Should().Be("Died");
        row.Eartag.Should().Be("9");
        row.BirthYear.Should().Be(1999);
        row.LeftDate.Should().Be(new DateTime(2021, 1, 2));
        row.Sire.Should().Be("SireName");
    }

    [Fact]
    public async Task AddRelationRow_RelatedCaseNotFound_ReportsRbseNotFound()
    {
        _relations.GetRelationDetailsOfRelatedCaseAsync("002700021").Returns((RelatedCaseDetailsRecord?)null);
        var model = CreateModel(AllRoles);
        model.RelationType = "S";
        model.RelationRbse = "002700021";

        var result = await model.OnPostAddRelationRowAsync();

        result.Should().BeOfType<PageResult>();
        model.RelationFieldErrors["RelationRbse"].Should().Be("RBSE number not found");
    }

    [Fact]
    public async Task AddRelationRow_DuplicateOfStagedRelation_ReportsAlreadyARelation()
    {
        _details = new RelationDetailsRecord(null, null, [Rel(1, "002700022")]);
        var model = CreateModel(AllRoles);
        model.RelationType = "S";
        model.RelationRbse = "002700022";

        await model.OnPostAddRelationRowAsync();

        model.RelationFieldErrors["RelationRbse"].Should().Contain("already a relation");
    }

    [Fact]
    public async Task BeginEditRelationRow_PopulatesPanelFromStagedRow()
    {
        _draft = DraftWith(new CaseRelationsDraftItem
        {
            ClientKey = "k1", RelationType = "S", RelationRbse = "R1", Sex = "M", BirthDay = 5, BirthMonth = 6, BirthYear = 2000,
            RelationFate = "D", LeftDate = new DateTime(2020, 1, 1), Eartag = "E", Sire = "Sr"
        });
        var model = CreateModel(AllRoles);

        var result = await model.OnPostBeginEditRelationRowAsync("k1");

        result.Should().BeOfType<PageResult>();
        model.EditingClientKey.Should().Be("k1");
        model.RelationRbse.Should().Be("R1");
        model.BirthDate.Should().Be(new DateTime(2000, 6, 5));
        model.LeftDate.Should().Be(new DateTime(2020, 1, 1));
        model.Sire.Should().Be("Sr");
    }

    [Fact]
    public async Task BeginEditRelationRow_PartialBirthDate_LeavesBirthDateNull()
    {
        _draft = DraftWith(new CaseRelationsDraftItem { ClientKey = "k1", RelationType = "S", BirthYear = 2000 });
        var model = CreateModel(AllRoles);

        await model.OnPostBeginEditRelationRowAsync("k1");

        model.BirthDate.Should().BeNull();
    }

    [Fact]
    public async Task BeginEditRelationRow_UnknownKey_JustRendersPage()
    {
        var model = CreateModel(AllRoles);

        var result = await model.OnPostBeginEditRelationRowAsync("nope");

        result.Should().BeOfType<PageResult>();
        model.EditingClientKey.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRelationRow_UnknownKey_RedirectsWithError()
    {
        var model = CreateModel(AllRoles);
        model.EditingClientKey = "missing";

        var result = await model.OnPostUpdateRelationRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["ErrorMessage"].Should().Be("The relation record being edited no longer exists.");
    }

    [Fact]
    public async Task UpdateRelationRow_ValidationFailure_ReopensEditor()
    {
        _draft = DraftWith(new CaseRelationsDraftItem { ClientKey = "k1", RelationType = "S", Sex = "M" });
        var model = CreateModel(AllRoles);
        model.EditingClientKey = "k1";

        var result = await model.OnPostUpdateRelationRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ReopenEditClientKey.Should().Be("k1");
        model.RelationFieldErrors.Should().ContainKey("RelationType");
    }

    [Fact]
    public async Task UpdateRelationRow_Valid_UpdatesStagedRow()
    {
        _draft = DraftWith(new CaseRelationsDraftItem { ClientKey = "k1", RelationType = "S", Sex = "F", Id = 4 });
        var model = CreateModel(AllRoles);
        model.EditingClientKey = "k1";
        model.RelationType = "S";
        model.Sex = "M";
        model.Eartag = "NEWTAG";

        var result = await model.OnPostUpdateRelationRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var row = _draft!.Relations.Single();
        row.Sex.Should().Be("M");
        row.SexDesc.Should().Be("Male");
        row.Eartag.Should().Be("NEWTAG");
        row.Id.Should().Be(4);
        _draft.HasRelationsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteRelationRow_RemovesRowOnlyWhenFound()
    {
        _draft = DraftWith(new CaseRelationsDraftItem { ClientKey = "k1", RelationType = "S" });

        await CreateModel(AllRoles).OnPostDeleteRelationRowAsync("other");
        _draft.Relations.Should().HaveCount(1);

        var result = await CreateModel(AllRoles).OnPostDeleteRelationRowAsync("k1");

        result.Should().BeOfType<RedirectToPageResult>();
        _draft.Relations.Should().BeEmpty();
        _draft.HasRelationsChanges.Should().BeTrue();
    }

    // ── Save ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveRelations_WithoutDataEntry_Forbids()
    {
        (await CreateModel().OnPostSaveRelationsAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SaveRelations_Success_StagesAndCommitsThenRedirectsHome()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(["DataEntry"]);
        model.CaseHerdbook = "HB2";
        model.DamSire.DamStatus = "A";
        model.DamSire.HasDam = true;
        model.DamSire.DamName = "Bess";
        model.DamSire.DamBirthYear = 2000;

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _orchestration.Received(1).CommitAllAsync(Rbse, 5);
        _draft!.DamSire!.DamName.Should().Be("Bess");
        _draft.DamSire.CaseHerdbook.Should().Be("HB2");
        _draft.DamSire.CaseRowStamp.Should().Equal(2);
    }

    [Fact]
    public async Task SaveRelations_PendingRemovalWithNoInput_ClearsDamAndSire()
    {
        _draft = new CaseEditSessionState { Rbse = Rbse, HasRelationsChanges = true, RemoveDamPending = true, RemoveSirePending = true };
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(["DataEntry"]);

        await model.OnPostSaveRelationsAsync();

        _draft.DamSire!.DamId.Should().BeNull();
        _draft.DamSire.SireId.Should().BeNull();
    }

    [Fact]
    public async Task SaveRelations_UnlinkedIdsInferHasFlags()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(["DataEntry"]);
        model.DamSire.SireName = "S";
        model.DamSire.SireId = 0;
        model.DamSire.SireRbse = "  ";

        await model.OnPostSaveRelationsAsync();

        _draft!.DamSire!.SireId.Should().BeNull();
    }

    [Fact]
    public async Task SaveRelations_DamSameAsCase_ReturnsPageWithError()
    {
        var model = CreateModel(["DataEntry"]);
        model.DamSire.DamRbse = Rbse;

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<PageResult>();
        model.DamError.Should().Be(RelationsModel.SameAsCaseRbse);
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task SaveRelations_SireSameAsCase_ReturnsPageWithError()
    {
        var model = CreateModel(["DataEntry"]);
        model.DamSire.SireRbse = Rbse;

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<PageResult>();
        model.SireError.Should().Be(RelationsModel.SameAsCaseRbse);
    }

    [Theory]
    [InlineData(40, 1, 2000, "valid day")]
    [InlineData(1, 13, 2000, "month between 1 and 12")]
    [InlineData(1, 1, 99, "four digit year")]
    [InlineData(null, null, 9999, "must not be in future")]
    [InlineData(5, null, 2000, "enter a month")]
    [InlineData(31, 2, 2000, "valid date")]
    public async Task SaveRelations_InvalidDamBirthDate_ReturnsPageWithDamError(int? day, int? month, int? year, string expected)
    {
        var model = CreateModel(["DataEntry"]);
        model.DamSire.DamBirthDay = day;
        model.DamSire.DamBirthMonth = month;
        model.DamSire.DamBirthYear = year;

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<PageResult>();
        model.DamError.Should().Contain(expected);
    }

    [Fact]
    public async Task SaveRelations_InvalidSireBirthDate_ReturnsPageWithSireError()
    {
        var model = CreateModel(["DataEntry"]);
        model.DamSire.SireBirthMonth = 0;

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<PageResult>();
        model.SireError.Should().Contain("month between 1 and 12");
    }

    [Fact]
    public async Task SaveRelations_BlankYearWithDayAndMonth_Passes()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(["DataEntry"]);
        model.DamSire.DamBirthDay = 5;
        model.DamSire.DamBirthMonth = 5;

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task SaveRelations_ParentRbseWithMultipleMatches_ReturnsPageWithError()
    {
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700030", null, "F")
            .Returns((IReadOnlyList<DamSireRecord>)[Parent(1, "002700030"), Parent(2, "002700030")]);
        var model = CreateModel(["DataEntry"]);
        model.DamSire.DamRbse = "002700030";

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<PageResult>();
        model.DamError.Should().Contain("Multiple dam matches");
    }

    [Fact]
    public async Task SaveRelations_SireMultipleMatches_ReturnsPageWithError()
    {
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700031", null, "M")
            .Returns((IReadOnlyList<DamSireRecord>)[Parent(1, "002700031"), Parent(2, "002700031")]);
        var model = CreateModel(["DataEntry"]);
        model.DamSire.SireRbse = "002700031";

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<PageResult>();
        model.SireError.Should().Contain("Multiple sire matches");
    }

    [Fact]
    public async Task SaveRelations_LinkedParentWithoutRowStamp_ResolvesMatchThenRefreshesPedigreeFromDb()
    {
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700032", null, "F")
            .Returns((IReadOnlyList<DamSireRecord>)[Parent(8, "002700032")]);
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(["DataEntry"]);
        model.DamSire.DamRbse = "002700032";
        model.DamSire.DamId = 8;
        model.DamSire.HasDam = true;
        model.DamSire.DamRowStamp = null;
        _connectionFactory.CreateConnection().Returns(_ => throw new InvalidOperationException("pedigree lookup"));

        var act = () => model.OnPostSaveRelationsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("pedigree lookup");
        model.DamSire.DamRowStamp.Should().Be(Convert.ToBase64String(new byte[] { 9 }));
    }

    [Fact]
    public async Task SaveRelations_ParentWithIdButNoRowStampAndNoMatches_ReturnsRowStampError()
    {
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700033", null, "M")
            .Returns((IReadOnlyList<DamSireRecord>)[]);
        var model = CreateModel(["DataEntry"]);
        model.DamSire.SireRbse = "002700033";
        model.DamSire.SireId = 5;
        model.DamSire.HasSire = true;

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<PageResult>();
        model.SireError.Should().Contain("Sire details could not be refreshed");
    }

    [Fact]
    public async Task SaveRelations_DamWithIdButNoRowStampAndNoRbse_ReturnsRowStampError()
    {
        var model = CreateModel(["DataEntry"]);
        model.DamSire.DamId = 5;
        model.DamSire.HasDam = true;

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<PageResult>();
        model.DamError.Should().Contain("Dam details could not be refreshed");
    }

    [Fact]
    public async Task SaveRelations_MandatoryFieldsMissing_StagesMissingFieldsResult()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns<Task<CaseCommitOutcome>>(_ => throw new MandatoryCaseFieldsMissingException(["Need owner"]));
        var model = CreateModel(["DataEntry"]);

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
        model.TempData[SaveResultModel.ModeKey].Should().Be(nameof(SaveResultMode.MissingMandatoryFields));
        ((string)model.TempData[SaveResultModel.MessagesKey]!).Should().Contain("Need owner");
    }

    [Fact]
    public async Task SaveRelations_UnexpectedException_StagesGenericFailure()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns<Task<CaseCommitOutcome>>(_ => throw new InvalidOperationException("secret sql detail"));
        var model = CreateModel(["DataEntry"]);

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
        model.TempData[SaveResultModel.ModeKey].Should().Be(nameof(SaveResultMode.Failure));
        ((string)model.TempData[SaveResultModel.MessagesKey]!).Should().NotContain("secret");
    }

    [Fact]
    public async Task SaveRelations_NonSuccessOutcome_SetsErrorAndRedirectsHome()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Failure(EditCaseResult.PostUpdateError));
        var model = CreateModel(["DataEntry"]);

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        ((string)model.TempData["ErrorMessage"]!).Should().Contain("PostUpdateError");
    }

    [Fact]
    public async Task SaveRelations_Warnings_StagesPartialSuccess()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Success(["stale row"]));
        var model = CreateModel(["DataEntry"]);

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
        model.TempData[SaveResultModel.ModeKey].Should().Be(nameof(SaveResultMode.PartialSuccess));
    }

    // ── Relation details JSON ────────────────────────────────────────────

    [Fact]
    public async Task GetRelationDetails_WithoutBirthParts_ReturnsNullBirthDate()
    {
        _relations.GetRelationDetailsOfRelatedCaseAsync("002700040").Returns(new RelatedCaseDetailsRecord { RelationRbse = "002700040", BirthYear = 2000 });

        var result = await CreateModel().OnGetRelationDetailsAsync("002700040");

        var json = JsonSerializer.Serialize(result.Should().BeOfType<JsonResult>().Subject.Value);
        json.Should().Contain("\"found\":true").And.Contain("\"birthDate\":null");
    }
}
