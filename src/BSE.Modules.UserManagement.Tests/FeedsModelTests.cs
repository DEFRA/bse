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
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

internal sealed class InMemorySessionState : ICaseEditSessionStateService
{
    public CaseEditSessionState? Current { get; set; }

    public Task<CaseEditSessionState?> GetAsync(string rbse, CancellationToken ct = default) => Task.FromResult(Current);

    public Task SetAsync(CaseEditSessionState state, CancellationToken ct = default)
    {
        Current = state;
        return Task.CompletedTask;
    }

    public Task ClearAsync(string rbse, CancellationToken ct = default)
    {
        Current = null;
        return Task.CompletedTask;
    }
}

public sealed class FeedsModelTests
{
    private const string Rbse = "002600001";
    private readonly IFeedRepository _feedRepository = Substitute.For<IFeedRepository>();
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ILookupRepository _lookupRepository = Substitute.For<ILookupRepository>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly InMemorySessionState _sessionState = new();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public FeedsModelTests()
    {
        _feedRepository.GetByRbseAsync(Arg.Any<string>()).Returns(new[]
        {
            new CaseFeedRecord { Id = 1, YearFrom = 2000, YearTo = 2001, RationType = "R1", RationName = "one", SupplierId = 5, RowStamp = [1] },
            new CaseFeedRecord { Id = 2, YearFrom = 2003, YearTo = 2004, RationType = "R2", RationName = "two", SupplierId = 5, RowStamp = null },
        });
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _lookups.GetLookupAsync(LookupTableId.RationType).Returns(new[] { new LookupItem(1, "R1", "Ration one") });
        _currentUser.GetUserIdAsync().Returns(2);
    }

    private FeedsModel Create(params string[] roles)
    {
        var model = new FeedsModel(_feedRepository, _caseService, _lookups, _lookupRepository, _batchRepository,
            _sessionState, _discard, _orchestration, _currentUser,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build())
        {
            Rbse = Rbse
        };
        PageTestHelpers.Attach(model, roles);
        return model;
    }

    private static readonly string[] Editor = ["DataEntry", "VLAAccess"];

    [Fact]
    public async Task OnGetAsync_WithoutRbse_RedirectsToSessionError()
    {
        var model = Create(Editor);
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGetAsync_SeedsTheDraftFromPersistedFeeds()
    {
        var model = Create(Editor);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Feeds.Should().HaveCount(2);
        model.SpolSiteUrl.Should().Be("http://spol");
        model.HasUnsavedChanges.Should().BeFalse();
        _sessionState.Current!.Feeds.Should().HaveCount(2);
    }

    [Fact]
    public async Task OnGetAsync_AppliesResetAndPickedSupplier()
    {
        var model = Create(Editor);
        model.ResetSupplier = true;
        model.SupplierId = 9;
        await model.OnGetAsync();
        model.SupplierId.Should().BeNull();

        var picked = Create(Editor);
        picked.PickedSupplierId = 7;
        picked.PickedSupplierName = "Acme";
        await picked.OnGetAsync();
        picked.SupplierId.Should().Be(7);
        picked.SupplierName.Should().Be("Acme");
    }

    [Theory]
    [InlineData("YearFrom", true)]
    [InlineData("YearTo", false)]
    [InlineData("RationType", true)]
    [InlineData("RationName", false)]
    [InlineData("Supplier", true)]
    [InlineData("Other", false)]
    [InlineData("RationDescription", true)]
    [InlineData("SupplierName", false)]
    [InlineData("IsPrePurchase", true)]
    public async Task OnGetAsync_SortsTheGrid(string column, bool desc)
    {
        var model = Create(Editor);
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.Feeds.Should().HaveCount(2);
        model.GetPagedFeeds().Should().HaveCount(2);
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task OnGetValidateSupplierAsync_ReturnsExactAndMissingMatches()
    {
        var model = Create(Editor);
        _lookupRepository.GetSupplierByNameAsync("ACME").Returns(new LuSupplier { Id = 3, Name = "Acme" });

        var exact = (JsonResult)await model.OnGetValidateSupplierAsync(" acme ");
        var missing = (JsonResult)await model.OnGetValidateSupplierAsync("zzz");
        var blank = (JsonResult)await model.OnGetValidateSupplierAsync(" ");
        var forbidden = (JsonResult)await Create("DataEntry").OnGetValidateSupplierAsync("acme");

        exact.Value!.ToString().Should().Contain("found = True");
        missing.Value!.ToString().Should().Contain("found = False");
        blank.Value!.ToString().Should().Contain("found = False");
        forbidden.Value!.ToString().Should().Contain("matches");
    }

    [Fact]
    public async Task EditorHandlers_AreForbiddenWithoutTheRequiredRoles()
    {
        var model = Create("ReadOnly");

        (await model.OnPostAddFeedRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostBeginEditFeedRowAsync("k")).Should().BeOfType<ForbidResult>();
        (await model.OnPostUpdateFeedRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostDeleteFeedRowAsync("k")).Should().BeOfType<ForbidResult>();
        (await model.OnPostSaveFeedsAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostValidateSupplierNavigateAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostAddFeedRowAsync_WithInvalidFields_RedisplaysThePage()
    {
        var model = Create(Editor);

        var result = await model.OnPostAddFeedRowAsync();

        result.Should().BeOfType<PageResult>();
        model.FieldErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnPostAddFeedRowAsync_StagesTheRowAndMarksTheDraftDirty()
    {
        var model = Create(Editor);
        model.YearFrom = 2010;
        model.YearTo = 2011;
        model.RationType = "R1";
        model.RationName = "new ration";
        model.SupplierId = 5;
        model.SupplierName = "Acme";

        var result = await model.OnPostAddFeedRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.Feeds.Should().HaveCount(3);
        _sessionState.Current.HasFeedsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task BeginEditThenUpdate_ChangesTheStagedRow()
    {
        await Create(Editor).OnGetAsync();
        var key = _sessionState.Current!.Feeds.First(f => f.Id == 1).ClientKey;

        var begin = Create(Editor);
        await begin.OnPostBeginEditFeedRowAsync(key);
        begin.EditingFeedId.Should().Be(1);
        begin.YearFrom.Should().Be(2000);

        var update = Create(Editor);
        update.EditingClientKey = key;
        update.YearFrom = 2000;
        update.YearTo = 2002;
        update.RationType = "R1";
        update.SupplierId = 5;
        var result = await update.OnPostUpdateFeedRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.Feeds.First(f => f.Id == 1).YearTo.Should().Be(2002);
        _sessionState.Current.HasFeedsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task OnPostUpdateFeedRowAsync_FindsTheRowByIdAndReportsAMissingOne()
    {
        await Create(Editor).OnGetAsync();

        var byId = Create(Editor);
        byId.EditingClientKey = "stale";
        byId.EditingFeedId = 1;
        byId.YearFrom = 2000;
        byId.YearTo = 2001;
        byId.RationType = "R1";
        byId.SupplierId = 5;
        (await byId.OnPostUpdateFeedRowAsync()).Should().BeOfType<RedirectToPageResult>();

        var missing = Create(Editor);
        missing.EditingClientKey = "nope";
        (await missing.OnPostUpdateFeedRowAsync()).Should().BeOfType<RedirectToPageResult>();
        missing.TempData["ErrorMessage"].Should().NotBeNull();

        var invalid = Create(Editor);
        invalid.EditingClientKey = _sessionState.Current!.Feeds[0].ClientKey;
        (await invalid.OnPostUpdateFeedRowAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task UpdatingARowWithoutChangingAnyField_LeavesTheDraftClean()
    {
        await Create(Editor).OnGetAsync();
        var key = _sessionState.Current!.Feeds.First(f => f.Id == 1).ClientKey;
        var update = Create(Editor);
        update.EditingClientKey = key;
        update.YearFrom = 2000;
        update.YearTo = 2001;
        update.RationType = "R1";
        update.RationName = "one";
        update.SupplierId = 5;

        (await update.OnPostUpdateFeedRowAsync()).Should().BeOfType<RedirectToPageResult>();

        _sessionState.Current!.HasFeedsChanges.Should().BeFalse();
    }

    [Fact]
    public async Task AddingThenDeletingARow_StillCountsAsAnUnsavedChange()
    {
        var add = Create(Editor);
        add.YearFrom = 2010;
        add.YearTo = 2011;
        add.RationType = "R1";
        add.RationName = "new ration";
        add.SupplierId = 5;
        await add.OnPostAddFeedRowAsync();
        var key = _sessionState.Current!.Feeds.First(f => f.Id == 1).ClientKey;

        await Create(Editor).OnPostDeleteFeedRowAsync(key);

        _sessionState.Current!.Feeds.Should().HaveCount(2);
        _sessionState.Current.HasFeedsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task DeletingARow_WhenAStagedRowIsNotPersisted_CountsAsAnUnsavedChange()
    {
        _sessionState.Current = new CaseEditSessionState
        {
            Rbse = Rbse,
            HasFeedsChanges = true,
            Feeds =
            [
                new() { ClientKey = "a", Id = 1, YearFrom = 2000, YearTo = 2001, RationType = "R1", RationName = "one", SupplierId = 5 },
                new() { ClientKey = "b", Id = 99, YearFrom = 2005, YearTo = 2006, RationType = "R1" },
                new() { ClientKey = "c", Id = 2, YearFrom = 2003, YearTo = 2004, RationType = "R2", RationName = "two", SupplierId = 5 }
            ]
        };

        await Create(Editor).OnPostDeleteFeedRowAsync("c");

        _sessionState.Current!.HasFeedsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task PanelValuesStashedByADelete_AreRestoredOnTheNextGet()
    {
        var model = Create(Editor);
        await model.OnGetAsync();
        var key = _sessionState.Current!.Feeds[0].ClientKey;
        model.EditingClientKey = "k";
        model.EditingFeedId = 3;
        model.YearFrom = 2015;
        model.YearTo = 2016;
        model.RationType = "R1";
        model.RationName = "typed";
        model.IsPrePurchase = true;
        model.SupplierId = 5;
        model.SupplierName = "Acme";
        await model.OnPostDeleteFeedRowAsync(key);
        model.YearFrom = null;
        model.RationName = null;

        await model.OnGetAsync();

        model.YearFrom.Should().Be(2015);
        model.RationName.Should().Be("typed");
        model.SupplierName.Should().Be("Acme");
        model.IsPrePurchase.Should().BeTrue();
        model.Feeds.Should().OnlyContain(f => !f.IsUnsaved);
    }

    [Fact]
    public async Task AnUnreadableStashedPanel_IsIgnored()
    {
        var model = Create(Editor);
        model.TempData[$"FeedsPanelState_{Rbse}"] = "null";
        model.YearFrom = 1999;

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.YearFrom.Should().Be(1999);
    }

    [Fact]
    public async Task OnPostDeleteFeedRowAsync_RemovesTheStagedRow()
    {
        await Create(Editor).OnGetAsync();
        var key = _sessionState.Current!.Feeds[0].ClientKey;

        var result = await Create(Editor).OnPostDeleteFeedRowAsync(key);

        result.Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.Feeds.Should().HaveCount(1);
        _sessionState.Current.HasFeedsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task OnPostSaveFeedsAsync_CommitsAndRedirectsHome()
    {
        _orchestration.CommitAllAsync(Rbse, 2).Returns(CaseCommitOutcome.Success([]));

        var result = await Create(Editor).OnPostSaveFeedsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnPostSaveFeedsAsync_RoutesFailuresAndWarnings()
    {
        _orchestration.CommitAllAsync(Rbse, 2).Returns(CaseCommitOutcome.Failure(EditCaseResult.RbseNotFound));
        (await Create(Editor).OnPostSaveFeedsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");

        _orchestration.CommitAllAsync(Rbse, 2).Returns(CaseCommitOutcome.Success(["warn"]));
        (await Create(Editor).OnPostSaveFeedsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");

        _orchestration.CommitAllAsync(Rbse, 2).Returns<CaseCommitOutcome>(_ => throw new MandatoryCaseFieldsMissingException(["missing"]));
        (await Create(Editor).OnPostSaveFeedsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task Cancel_DiscardsTheDraftAndRedirectsHome()
    {
        var post = await Create(Editor).OnPostCancelFeedsEditAsync();
        var get = await Create(Editor).OnGetCancelFeedsEditAsync();

        post.Should().BeOfType<RedirectToPageResult>();
        get.Should().BeOfType<RedirectToPageResult>();
        await _discard.Received(2).DiscardAllAsync(Rbse);
    }

    [Fact]
    public async Task OnPostValidateSupplierNavigateAsync_UsesAnExactMatchOrPicksFromCloseMatches()
    {
        _lookupRepository.GetSupplierByNameAsync("ACME").Returns(new LuSupplier { Id = 3, Name = "Acme" });
        var exact = Create(Editor);
        exact.SupplierLookupName = "acme";

        (await exact.OnPostValidateSupplierNavigateAsync()).Should().BeOfType<PageResult>();
        exact.SupplierId.Should().Be(3);

        var pick = Create(Editor);
        pick.SupplierLookupName = "unknown";
        (await pick.OnPostValidateSupplierNavigateAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/PickSupplier");
    }
}
