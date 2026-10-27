using System.Security.Claims;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Tests;

public class FeedsModelTests
{
    private const string Rbse = "002600001";

    private readonly IFeedRepository _feeds = Substitute.For<IFeedRepository>();
    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ILookupRepository _lookupRepo = Substitute.For<ILookupRepository>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly ICaseEditSessionStateService _session = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private IReadOnlyList<CaseFeedRecord> _persisted = [];
    private CaseEditSessionState? _state;

    private FeedsModel CreateModel(bool dataEntry = true, bool vla = true, string? spolUrl = "http://spol")
    {
        var roles = new List<Claim>();
        if (dataEntry) roles.Add(new Claim(ClaimTypes.Role, "DataEntry"));
        if (vla) roles.Add(new Claim(ClaimTypes.Role, "VLAAccess"));
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(roles, "test")) };

        _feeds.GetByRbseAsync(Arg.Any<string>()).Returns(_ => Task.FromResult(_persisted));
        _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>())
            .Returns(Task.FromResult<IReadOnlyList<BatchNumberEntry>>([new BatchNumberEntry(1, "2026/1", Rbse, "BSE1")]));
        _lookups.GetLookupAsync(LookupTableId.RationType)
            .Returns(Task.FromResult<IEnumerable<LookupItem>>([new LookupItem(1, "HAY", "Hay")]));
        _cases.GetCaseAsync(Arg.Any<string>()).Returns(Task.FromResult<CaseRecord?>(null));
        _session.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(_state));
        _currentUser.GetUserIdAsync().Returns(Task.FromResult(5));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(spolUrl is null ? [] : new Dictionary<string, string?> { ["SpolSiteUrl"] = spolUrl })
            .Build();

        http.Request.ContentType = "application/x-www-form-urlencoded";
        http.Request.Form = new Microsoft.AspNetCore.Http.FormCollection([]);
        var url = Substitute.For<IUrlHelper>();
        url.ActionContext.Returns(new Microsoft.AspNetCore.Mvc.ActionContext(
            http, new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));

        return new FeedsModel(_feeds, _cases, _lookups, _lookupRepo, _batches, _session, _discard,
            _orchestration, _currentUser, config)
        {
            Rbse = Rbse,
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>()),
            Url = url
        };
    }

    private static CaseFeedRecord Persisted(int id, short from = 2020, short to = 2021, int supplier = 9) => new()
    {
        Id = id, Rbse = Rbse, YearFrom = from, YearTo = to, RationType = "HAY", RationDescription = "Hay",
        SupplierId = supplier, SupplierName = "Sup", RationName = "R", RowStamp = [1, 2]
    };

    private static CaseFeedsDraftItem Item(string key, int? id = null, short from = 2020, short to = 2021) => new()
    {
        ClientKey = key, Id = id, YearFrom = from, YearTo = to, RationType = "HAY", SupplierId = 9, SupplierName = "Sup"
    };

    private static void FillValid(FeedsModel m)
    {
        m.YearFrom = 2020; m.YearTo = 2021; m.RationType = "HAY"; m.SupplierId = 9; m.SupplierName = "Sup";
    }

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var model = CreateModel();
        model.Rbse = " ";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_InitializesDraftFromPersistedFeeds()
    {
        _persisted = [Persisted(1), Persisted(2, 2018, 2019)];
        var model = CreateModel();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.SpolSiteUrl.Should().Be("http://spol");
        model.Feeds.Should().HaveCount(2);
        model.Feeds[0].YearFrom.Should().Be(2018);
        model.BatchNumbers.Should().HaveCount(1);
        model.RationTypes.Should().HaveCount(1);
        model.HasUnsavedChanges.Should().BeFalse();
        await _session.Received(1).SetAsync(Arg.Is<CaseEditSessionState>(s => s.Feeds.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnGet_MissingSpolConfig_UsesEmptyString()
    {
        var model = CreateModel(spolUrl: null);

        await model.OnGetAsync();

        model.SpolSiteUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_ExistingDraft_IsNotReinitialized()
    {
        _persisted = [Persisted(1)];
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true };
        var model = CreateModel();

        await model.OnGetAsync();

        model.Feeds.Should().BeEmpty();
        model.HasUnsavedChanges.Should().BeTrue();
        await _session.DidNotReceive().SetAsync(Arg.Any<CaseEditSessionState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnGet_PickedSupplierAndReset_AppliesPick()
    {
        var model = CreateModel();
        model.ResetSupplier = true;
        model.SupplierId = 3;
        model.PickedSupplierId = 12;
        model.PickedSupplierName = "Picked";

        await model.OnGetAsync();

        model.SupplierId.Should().Be(12);
        model.SupplierName.Should().Be("Picked");
    }

    [Fact]
    public async Task OnGet_ResetSupplier_ClearsSupplier()
    {
        var model = CreateModel();
        model.ResetSupplier = true;
        model.SupplierId = 3;

        await model.OnGetAsync();

        model.SupplierId.Should().BeNull();
        model.SupplierName.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_RestoresStashedPanelState()
    {
        _persisted = [Persisted(1), Persisted(2)];
        var model = CreateModel();
        FillValid(model);
        model.RationName = "Nm";
        _state = new CaseEditSessionState { Rbse = Rbse, Feeds = [Item("k1", 1), Item("k2", 2)], HasFeedsChanges = true };

        await model.OnPostDeleteFeedRowAsync("k1");
        model.YearFrom = null; model.SupplierId = null; model.RationName = null;
        await model.OnGetAsync();

        model.YearFrom.Should().Be(2020);
        model.SupplierId.Should().Be(9);
        model.RationName.Should().Be("Nm");
    }

    [Theory]
    [InlineData("YearFrom", false)]
    [InlineData("YearFrom", true)]
    [InlineData("YearTo", false)]
    [InlineData("YearTo", true)]
    [InlineData("RationDescription", false)]
    [InlineData("RationDescription", true)]
    [InlineData("SupplierName", false)]
    [InlineData("SupplierName", true)]
    [InlineData("RationName", false)]
    [InlineData("RationName", true)]
    [InlineData("IsPrePurchase", false)]
    [InlineData("IsPrePurchase", true)]
    [InlineData(null, false)]
    public async Task OnGet_Sorting_OrdersRows(string? column, bool desc)
    {
        _state = new CaseEditSessionState
        {
            Rbse = Rbse,
            HasFeedsChanges = true,
            Feeds =
            [
                new CaseFeedsDraftItem { ClientKey = "a", YearFrom = 2010, YearTo = 2012, RationDescription = "A", SupplierName = "A", RationName = "A", IsPrePurchase = false },
                new CaseFeedsDraftItem { ClientKey = "b", YearFrom = 2015, YearTo = 2016, RationDescription = "B", SupplierName = "B", RationName = "B", IsPrePurchase = true }
            ]
        };
        var model = CreateModel();
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        var expectedFirst = desc ? "b" : "a";
        model.Feeds[0].ClientKey.Should().Be(expectedFirst);
    }

    [Fact]
    public async Task OnGet_Paging_ClampsPageAndReturnsSlice()
    {
        _state = new CaseEditSessionState
        {
            Rbse = Rbse,
            HasFeedsChanges = true,
            Feeds = Enumerable.Range(0, 25).Select(i => Item($"k{i}", i + 1, (short)(2000 + i), (short)(2030))).ToList()
        };
        var model = CreateModel();
        model.PageNumber = 99;

        await model.OnGetAsync();

        model.TotalPages.Should().Be(3);
        model.CurrentPage.Should().Be(3);
        model.GetPagedFeeds().Should().HaveCount(5);
    }

    [Fact]
    public void StagedFeedItem_IsUnsaved_ReflectsId()
    {
        new FeedsModel.StagedFeedItem { Id = null }.IsUnsaved.Should().BeTrue();
        new FeedsModel.StagedFeedItem { Id = 0 }.IsUnsaved.Should().BeTrue();
        new FeedsModel.StagedFeedItem { Id = 4 }.IsUnsaved.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSupplier_NotAuthorised_ReturnsNotFound()
    {
        var model = CreateModel(vla: false);

        var result = await model.OnGetValidateSupplierAsync("x");

        result.Should().BeOfType<JsonResult>().Which.Value!.ToString().Should().Contain("found = False");
        await _lookupRepo.DidNotReceive().GetSupplierByNameAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ValidateSupplier_BlankName_ReturnsNotFound()
    {
        var model = CreateModel();

        var result = await model.OnGetValidateSupplierAsync("   ");

        result.Should().BeOfType<JsonResult>().Which.Value!.ToString().Should().Contain("found = False");
        await _lookupRepo.DidNotReceive().GetSupplierByNameAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ValidateSupplier_ExactMatch_ReturnsSupplier()
    {
        _lookupRepo.GetSupplierByNameAsync("ACME").Returns(Task.FromResult<LuSupplier?>(new LuSupplier { Id = 4, Name = "ACME" }));
        var model = CreateModel();

        var result = await model.OnGetValidateSupplierAsync(" acme ");

        var text = result.Should().BeOfType<JsonResult>().Which.Value!.ToString();
        text.Should().Contain("found = True").And.Contain("id = 4");
    }

    [Fact]
    public async Task ValidateSupplier_NoMatch_ReturnsPickUrl()
    {
        _lookupRepo.GetSupplierByNameAsync(Arg.Any<string>()).Returns(Task.FromResult<LuSupplier?>(null));
        var model = CreateModel();

        var result = await model.OnGetValidateSupplierAsync("zzz");

        result.Should().BeOfType<JsonResult>().Which.Value!.ToString().Should().Contain("found = False").And.Contain("pick = True");
    }

    [Fact]
    public async Task AddFeedRow_NotAuthorised_Forbids()
    {
        var model = CreateModel(dataEntry: false);

        (await model.OnPostAddFeedRowAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddFeedRow_Invalid_ReturnsPageWithFieldErrors()
    {
        var model = CreateModel();

        var result = await model.OnPostAddFeedRowAsync();

        result.Should().BeOfType<PageResult>();
        model.FieldErrors.Should().ContainKeys("YearFrom", "YearTo", "RationType", "SupplierId");
        await _session.DidNotReceive().SetAsync(Arg.Is<CaseEditSessionState>(s => s.Feeds.Count > 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddFeedRow_YearBeforeStagedBirthDate_IsInvalid()
    {
        _state = new CaseEditSessionState
        {
            Rbse = Rbse,
            HasFeedsChanges = true,
            Case = ((BSE.Modules.CaseManagement.Commands.EditCaseCommand)System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(BSE.Modules.CaseManagement.Commands.EditCaseCommand))) with { BirthDate = new DateTime(2021, 1, 1) }
        };
        var model = CreateModel();
        FillValid(model);

        var result = await model.OnPostAddFeedRowAsync();

        result.Should().BeOfType<PageResult>();
        model.FieldErrors.Should().ContainKey("YearFrom");
    }

    [Fact]
    public async Task AddFeedRow_Valid_StagesRowAndRedirects()
    {
        var model = CreateModel();
        FillValid(model);
        model.RationName = " ";
        model.IsPrePurchase = true;
        CaseEditSessionState? saved = null;
        await _session.SetAsync(Arg.Do<CaseEditSessionState>(s => saved = s), Arg.Any<CancellationToken>());

        var result = await model.OnPostAddFeedRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        saved.Should().NotBeNull();
        var item = saved!.Feeds.Should().ContainSingle().Subject;
        item.RationDescription.Should().Be("Hay");
        item.RationName.Should().BeNull();
        item.IsPrePurchase.Should().BeTrue();
        item.Id.Should().BeNull();
        saved.HasFeedsChanges.Should().BeTrue();
        model.YearFrom.Should().BeNull();
        model.SupplierId.Should().BeNull();
    }

    [Fact]
    public async Task BeginEdit_PopulatesPanel()
    {
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true, Feeds = [Item("k1", 7)] };
        var model = CreateModel();

        var result = await model.OnPostBeginEditFeedRowAsync("k1");

        result.Should().BeOfType<PageResult>();
        model.EditingClientKey.Should().Be("k1");
        model.EditingFeedId.Should().Be(7);
        model.SupplierId.Should().Be(9);
        model.RationType.Should().Be("HAY");
    }

    [Fact]
    public async Task BeginEdit_UnknownKey_LeavesPanelUntouched()
    {
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true, Feeds = [Item("k1", 7)] };
        var model = CreateModel();

        var result = await model.OnPostBeginEditFeedRowAsync("nope");

        result.Should().BeOfType<PageResult>();
        model.EditingClientKey.Should().BeNull();
    }

    [Fact]
    public async Task BeginEdit_NotAuthorised_Forbids()
    {
        var model = CreateModel(vla: false);

        (await model.OnPostBeginEditFeedRowAsync("k")).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateFeedRow_NotAuthorised_Forbids()
    {
        var model = CreateModel(dataEntry: false);

        (await model.OnPostUpdateFeedRowAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateFeedRow_RowMissing_SetsErrorAndRedirects()
    {
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true };
        var model = CreateModel();
        model.EditingClientKey = "gone";

        var result = await model.OnPostUpdateFeedRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["ErrorMessage"].Should().Be("The feed record being edited no longer exists.");
    }

    [Fact]
    public async Task UpdateFeedRow_FallsBackToFeedId()
    {
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true, Feeds = [Item("k1", 7)] };
        var model = CreateModel();
        model.EditingClientKey = "stale";
        model.EditingFeedId = 7;
        FillValid(model);
        model.YearTo = 2025;

        var result = await model.OnPostUpdateFeedRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _state.Feeds[0].YearTo.Should().Be(2025);
    }

    [Fact]
    public async Task UpdateFeedRow_Invalid_ReturnsPageKeepingEditingKey()
    {
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true, Feeds = [Item("k1", 7)] };
        var model = CreateModel();
        model.EditingClientKey = "k1";

        var result = await model.OnPostUpdateFeedRowAsync();

        result.Should().BeOfType<PageResult>();
        model.FieldErrors.Should().NotBeEmpty();
        model.EditingClientKey.Should().Be("k1");
    }

    [Fact]
    public async Task UpdateFeedRow_Valid_UpdatesDraftAndMarksDirty()
    {
        _persisted = [Persisted(7)];
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true, Feeds = [Item("k1", 7)] };
        var model = CreateModel();
        model.EditingClientKey = "k1";
        FillValid(model);
        model.RationName = "New";

        var result = await model.OnPostUpdateFeedRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _state.Feeds[0].RationName.Should().Be("New");
        _state.Feeds[0].RationDescription.Should().Be("Hay");
        _state.HasFeedsChanges.Should().BeTrue();
        await _session.Received().SetAsync(_state, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateFeedRow_NoNetChange_ClearsDirtyFlag()
    {
        _persisted = [Persisted(7, 2020, 2021)];
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true, Feeds = [Item("k1", 7)] };
        var model = CreateModel();
        model.EditingClientKey = "k1";
        FillValid(model);
        model.RationName = "R";

        await model.OnPostUpdateFeedRowAsync();

        _state.HasFeedsChanges.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFeedRow_NotAuthorised_Forbids()
    {
        var model = CreateModel(dataEntry: false);

        (await model.OnPostDeleteFeedRowAsync("k")).Should().BeOfType<Microsoft.AspNetCore.Mvc.ForbidResult>();
    }

    [Fact]
    public async Task DeleteFeedRow_RemovesRowAndMarksDirty()
    {
        _persisted = [Persisted(1), Persisted(2)];
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true, Feeds = [Item("k1", 1), Item("k2", 2)] };
        var model = CreateModel();

        var result = await model.OnPostDeleteFeedRowAsync("k1");

        result.Should().BeOfType<RedirectToPageResult>();
        _state.Feeds.Should().ContainSingle(f => f.ClientKey == "k2");
        _state.HasFeedsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFeedRow_UnknownKey_DoesNotSave()
    {
        _state = new CaseEditSessionState { Rbse = Rbse, HasFeedsChanges = true, Feeds = [Item("k1", 1)] };
        var model = CreateModel();

        await model.OnPostDeleteFeedRowAsync("zzz");

        await _session.DidNotReceive().SetAsync(Arg.Any<CaseEditSessionState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveFeeds_NotDataEntry_Forbids()
    {
        var model = CreateModel(dataEntry: false);

        (await model.OnPostSaveFeedsAsync()).Should().BeOfType<ForbidResult>();
        await _orchestration.DidNotReceive().CommitAllAsync(Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task SaveFeeds_Success_RedirectsHome()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(Task.FromResult(CaseCommitOutcome.Success([])));
        var model = CreateModel();

        var result = await model.OnPostSaveFeedsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task SaveFeeds_Warnings_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(Task.FromResult(CaseCommitOutcome.Success(["warn"])));
        var model = CreateModel();

        var result = await model.OnPostSaveFeedsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
        model.TempData[SaveResultModel.ModeKey].Should().Be("PartialSuccess");
    }

    [Fact]
    public async Task SaveFeeds_MissingMandatory_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 5)
            .Returns<CaseCommitOutcome>(_ => throw new MandatoryCaseFieldsMissingException(["Missing X"]));
        var model = CreateModel();

        var result = await model.OnPostSaveFeedsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
        model.TempData[SaveResultModel.ModeKey].Should().Be("MissingMandatoryFields");
    }

    [Fact]
    public async Task SaveFeeds_Failure_SetsErrorAndRedirectsHome()
    {
        _orchestration.CommitAllAsync(Rbse, 5)
            .Returns(Task.FromResult(CaseCommitOutcome.Failure(EditCaseResult.ConcurrencyConflict)));
        var model = CreateModel();

        var result = await model.OnPostSaveFeedsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().Be("Unable to save feed records: ConcurrencyConflict.");
    }

    [Fact]
    public async Task CancelFeedsEdit_PostAndGet_DiscardAndRedirectHome()
    {
        var model = CreateModel();

        var post = await model.OnPostCancelFeedsEditAsync();
        var get = await model.OnGetCancelFeedsEditAsync();

        post.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        get.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _discard.Received(2).DiscardAllAsync(Rbse);
    }

    [Fact]
    public async Task ValidateSupplierNavigate_NotAuthorised_Forbids()
    {
        var model = CreateModel(vla: false);

        (await model.OnPostValidateSupplierNavigateAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ValidateSupplierNavigate_ExactMatch_FillsSupplier()
    {
        _lookupRepo.GetSupplierByNameAsync("ACME").Returns(Task.FromResult<LuSupplier?>(new LuSupplier { Id = 4, Name = "ACME Ltd" }));
        var model = CreateModel();
        model.SupplierLookupName = " acme ";

        var result = await model.OnPostValidateSupplierNavigateAsync();

        result.Should().BeOfType<PageResult>();
        model.SupplierId.Should().Be(4);
        model.SupplierName.Should().Be("ACME Ltd");
    }

    [Fact]
    public async Task ValidateSupplierNavigate_NoMatch_RedirectsToPickSupplier()
    {
        _lookupRepo.GetSupplierByNameAsync(Arg.Any<string>()).Returns(Task.FromResult<LuSupplier?>(null));
        var model = CreateModel();
        model.SupplierLookupName = "zzz";

        var result = await model.OnPostValidateSupplierNavigateAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/PickSupplier");
        redirect.RouteValues!["name"].Should().Be("zzz");
    }

    [Fact]
    public async Task ValidateSupplierNavigate_BlankName_RedirectsWithoutLookup()
    {
        var model = CreateModel();

        var result = await model.OnPostValidateSupplierNavigateAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/PickSupplier");
        await _lookupRepo.DidNotReceive().GetSupplierByNameAsync(Arg.Any<string>());
    }
}
