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
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>Covers the request-handling behaviour of <see cref="FeedsModel"/> (0% new-code coverage):
/// OnGetAsync, the staged add/edit/delete row handlers, OnPostSaveFeedsAsync, and
/// OnPostCancelFeedsEditAsync — none of which had any prior test. OnGetValidateSupplierAsync is
/// excluded: it calls <c>Url.Page</c>, whose extension-method implementation cannot be driven
/// through a plain <see cref="Microsoft.AspNetCore.Mvc.IUrlHelper"/> substitute without a full
/// <see cref="Microsoft.AspNetCore.Mvc.ActionContext"/>/routing setup (see refactor suggestion below).</summary>
public sealed class FeedsModelHandlerTests
{
    private const string Rbse = "002600001";
    private static readonly string[] Roles = ["DataEntry", "VLAAccess"];

    private readonly IFeedRepository _feedRepository = Substitute.For<IFeedRepository>();
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ILookupRepository _lookupRepository = Substitute.For<ILookupRepository>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly ICaseFeedsDraftStateService _feedsDraftState = Substitute.For<ICaseFeedsDraftStateService>();
    private readonly ICaseScalarDraftStateService _caseScalarDraftState = Substitute.For<ICaseScalarDraftStateService>();
    private readonly ICaseEditOrchestrationService _caseEditOrchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public FeedsModelHandlerTests()
    {
        _feedRepository.GetByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<CaseFeedRecord>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _lookups.GetLookupAsync(LookupTableId.RationType).Returns(Array.Empty<LookupItem>());
        _caseScalarDraftState.GetAsync(Arg.Any<string>()).Returns((CaseScalarDraftState?)null);
        _feedsDraftState.GetAsync(Arg.Any<string>()).Returns((CaseFeedsDraftState?)null);
    }

    private FeedsModel CreateModel(string[]? roles = null)
    {
        var model = new FeedsModel(
            _feedRepository, _caseService, _lookups, _lookupRepository, _batchRepository,
            _feedsDraftState, _caseScalarDraftState, _caseEditOrchestration, _currentUser,
            new ConfigurationBuilder().AddInMemoryCollection().Build())
        {
            Rbse = Rbse
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            (roles ?? []).Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"));
        model.PageContext = new PageContext { HttpContext = httpContext };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        return model;
    }

    // ── OnGetAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_MissingRbse_RedirectsToSessionError()
    {
        var model = CreateModel();
        model.Rbse = string.Empty;

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGetAsync_WhenDraftIsEmpty_InitializesFromPersistedFeeds()
    {
        _feedRepository.GetByRbseAsync(Rbse).Returns([
            new CaseFeedRecord { Id = 1, Rbse = Rbse, RationName = "Ration A", YearFrom = 1995, YearTo = 1996 }
        ]);

        var model = CreateModel(Roles);
        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Feeds.Should().ContainSingle().Which.RationName.Should().Be("Ration A");
        await _feedsDraftState.Received(1).SetAsync(Arg.Any<CaseFeedsDraftState>());
    }

    [Fact]
    public async Task OnGetAsync_WhenPickedSupplierProvided_PopulatesSupplierFields()
    {
        var model = CreateModel(Roles);
        model.PickedSupplierId = 5;
        model.PickedSupplierName = "ACME Feeds";

        await model.OnGetAsync();

        model.SupplierId.Should().Be(5);
        model.SupplierName.Should().Be("ACME Feeds");
    }

    [Fact]
    public async Task OnGetAsync_WhenResetSupplierIsSet_ClearsSupplierFields()
    {
        var model = CreateModel(Roles);
        model.ResetSupplier = true;
        model.SupplierId = 5;
        model.SupplierName = "ACME Feeds";

        await model.OnGetAsync();

        model.SupplierId.Should().BeNull();
        model.SupplierName.Should().BeEmpty();
    }

    // ── OnPostAddFeedRowAsync ────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAddFeedRowAsync_Forbidden_WhenMissingRequiredRole()
    {
        var model = CreateModel(["DataEntry"]); // missing VLAAccess

        var result = await model.OnPostAddFeedRowAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostAddFeedRowAsync_WithMissingYearFrom_ReturnsPageWithFieldError()
    {
        // Seed an already-initialized draft so LoadOrInitializeDraftStateAsync's own one-time
        // SetAsync (persisting the freshly-initialized draft) doesn't masquerade as a staged add.
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState { Rbse = Rbse, Feeds = [], HasPendingChanges = false });
        var model = CreateModel(Roles);
        model.YearTo = 1996;
        model.RationType = "C";
        model.SupplierId = 1;

        var result = await model.OnPostAddFeedRowAsync();

        result.Should().BeOfType<PageResult>();
        model.FieldErrors.Should().ContainKey("YearFrom");
        await _feedsDraftState.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Fact]
    public async Task OnPostAddFeedRowAsync_WithValidFields_StagesANewFeedAndRedirects()
    {
        var model = CreateModel(Roles);
        model.YearFrom = 1995;
        model.YearTo = 1996;
        model.RationType = "C";
        model.RationName = "New Ration";
        model.SupplierId = 1;

        var result = await model.OnPostAddFeedRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        // Received() rather than Received(1): LoadOrInitializeDraftStateAsync also persists an
        // initial empty draft before the add runs, and NSubstitute's argument matcher evaluates
        // against the mutable CaseFeedsDraftState's *current* state, so both recorded calls match
        // the predicate once the row has been added to the shared list reference.
        await _feedsDraftState.Received().SetAsync(Arg.Is<CaseFeedsDraftState>(
            d => d.HasPendingChanges && d.Feeds.Any(f => f.RationName == "New Ration")));
    }

    // ── OnPostBeginEditFeedRowAsync ──────────────────────────────────────────

    [Fact]
    public async Task OnPostBeginEditFeedRowAsync_Forbidden_WhenMissingRequiredRole()
    {
        var model = CreateModel();

        var result = await model.OnPostBeginEditFeedRowAsync("key-1");

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostBeginEditFeedRowAsync_WhenRowExists_PopulatesThePanel()
    {
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState
        {
            Rbse = Rbse,
            Feeds = [new CaseFeedsDraftItem { ClientKey = "key-1", Id = 7, RationName = "Ration A", YearFrom = 1995, YearTo = 1996 }]
        });

        var model = CreateModel(Roles);
        var result = await model.OnPostBeginEditFeedRowAsync("key-1");

        result.Should().BeOfType<PageResult>();
        model.EditingFeedId.Should().Be(7);
        model.RationName.Should().Be("Ration A");
    }

    [Fact]
    public async Task OnPostBeginEditFeedRowAsync_WhenRowDoesNotExist_LeavesPanelUnset()
    {
        var model = CreateModel(Roles);

        var result = await model.OnPostBeginEditFeedRowAsync("missing-key");

        result.Should().BeOfType<PageResult>();
        model.EditingFeedId.Should().BeNull();
    }

    // ── OnPostUpdateFeedRowAsync ─────────────────────────────────────────────

    [Fact]
    public async Task OnPostUpdateFeedRowAsync_Forbidden_WhenMissingRequiredRole()
    {
        var model = CreateModel();

        var result = await model.OnPostUpdateFeedRowAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostUpdateFeedRowAsync_WhenRowNoLongerExists_SetsErrorAndRedirects()
    {
        var model = CreateModel(Roles);
        model.EditingClientKey = "missing-key";

        var result = await model.OnPostUpdateFeedRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["ErrorMessage"].Should().Be("The feed record being edited no longer exists.");
    }

    [Fact]
    public async Task OnPostUpdateFeedRowAsync_WithInvalidFields_ReturnsPageAndKeepsEditingKey()
    {
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState
        {
            Rbse = Rbse,
            Feeds = [new CaseFeedsDraftItem { ClientKey = "key-1", Id = 7, RationName = "Ration A", YearFrom = 1995, YearTo = 1996 }]
        });

        var model = CreateModel(Roles);
        model.EditingClientKey = "key-1";
        model.YearFrom = null; // invalid

        var result = await model.OnPostUpdateFeedRowAsync();

        result.Should().BeOfType<PageResult>();
        model.EditingClientKey.Should().Be("key-1");
        model.FieldErrors.Should().ContainKey("YearFrom");
    }

    [Fact]
    public async Task OnPostUpdateFeedRowAsync_WithValidFields_UpdatesTheStagedRowAndRedirects()
    {
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState
        {
            Rbse = Rbse,
            Feeds = [new CaseFeedsDraftItem { ClientKey = "key-1", Id = 7, RationName = "Ration A", YearFrom = 1995, YearTo = 1996, RationType = "C" }]
        });

        var model = CreateModel(Roles);
        model.EditingClientKey = "key-1";
        model.YearFrom = 1995;
        model.YearTo = 1997;
        model.RationType = "C";
        model.RationName = "Ration A (updated)";
        model.SupplierId = 1;

        var result = await model.OnPostUpdateFeedRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _feedsDraftState.Received(1).SetAsync(Arg.Is<CaseFeedsDraftState>(
            d => d.HasPendingChanges && d.Feeds.Single().RationName == "Ration A (updated)"));
    }

    // ── OnPostDeleteFeedRowAsync ─────────────────────────────────────────────

    [Fact]
    public async Task OnPostDeleteFeedRowAsync_Forbidden_WhenMissingRequiredRole()
    {
        var model = CreateModel();

        var result = await model.OnPostDeleteFeedRowAsync("key-1");

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostDeleteFeedRowAsync_WhenRowExists_RemovesItAndRedirects()
    {
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState
        {
            Rbse = Rbse,
            Feeds = [new CaseFeedsDraftItem { ClientKey = "key-1", Id = 7, RationName = "Ration A" }]
        });

        var model = CreateModel(Roles);
        var result = await model.OnPostDeleteFeedRowAsync("key-1");

        result.Should().BeOfType<RedirectToPageResult>();
        await _feedsDraftState.Received(1).SetAsync(Arg.Is<CaseFeedsDraftState>(
            d => d.HasPendingChanges && d.Feeds.Count == 0));
    }

    [Fact]
    public async Task OnPostDeleteFeedRowAsync_WhenRowDoesNotExist_StillRedirectsWithoutChanges()
    {
        var model = CreateModel(Roles);

        var result = await model.OnPostDeleteFeedRowAsync("missing-key");

        result.Should().BeOfType<RedirectToPageResult>();
        await _feedsDraftState.DidNotReceive().SetAsync(Arg.Is<CaseFeedsDraftState>(d => d.HasPendingChanges));
    }

    // ── OnPostSaveFeedsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task OnPostSaveFeedsAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateModel(["VLAAccess"]);

        var result = await model.OnPostSaveFeedsAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSaveFeedsAsync_WhenCommitSucceeds_RedirectsToHome()
    {
        _currentUser.GetUserIdAsync().Returns(7);
        _caseEditOrchestration.CommitAllAsync(Rbse, 7).Returns(CaseCommitOutcome.Success([]));

        var model = CreateModel(Roles);
        var result = await model.OnPostSaveFeedsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnPostSaveFeedsAsync_WhenCommitHasWarnings_RedirectsToSaveResult()
    {
        _currentUser.GetUserIdAsync().Returns(7);
        _caseEditOrchestration.CommitAllAsync(Rbse, 7).Returns(CaseCommitOutcome.Success(["some warning"]));

        var model = CreateModel(Roles);
        var result = await model.OnPostSaveFeedsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task OnPostSaveFeedsAsync_WhenCommitFailsHard_RedirectsToHomeWithErrorMessage()
    {
        _currentUser.GetUserIdAsync().Returns(7);
        _caseEditOrchestration.CommitAllAsync(Rbse, 7)
            .Returns(CaseCommitOutcome.Failure(EditCaseResult.PostUpdateError));

        var model = CreateModel(Roles);
        var result = await model.OnPostSaveFeedsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().Be("Unable to save feed records: PostUpdateError.");
    }

    // ── OnPostCancelFeedsEditAsync ───────────────────────────────────────────

    [Fact]
    public async Task OnPostCancelFeedsEditAsync_ClearsBothDraftsAndRedirectsToHome()
    {
        var model = CreateModel(Roles);

        var result = await model.OnPostCancelFeedsEditAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _feedsDraftState.Received(1).ClearAsync(Rbse);
        await _caseScalarDraftState.Received(1).ClearAsync(Rbse);
    }

    // ── OnPostValidateSupplierNavigateAsync ──────────────────────────────────

    [Fact]
    public async Task OnPostValidateSupplierNavigateAsync_Forbidden_WhenMissingRequiredRole()
    {
        var model = CreateModel();

        var result = await model.OnPostValidateSupplierNavigateAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostValidateSupplierNavigateAsync_RedirectsToPickSupplierWithTrimmedName()
    {
        var model = CreateModel(Roles);
        model.SupplierLookupName = "  ACME Feeds  ";
        model.HttpContext.Request.Form = new Microsoft.AspNetCore.Http.FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        var result = await model.OnPostValidateSupplierNavigateAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Which;
        redirect.PageName.Should().Be("/Case/PickSupplier");
        redirect.RouteValues.Should().ContainValue("ACME Feeds");
    }
}
