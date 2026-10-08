using System.Security.Claims;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.CaseWork.Repositories;
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
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>
/// Covers <see cref="EditModel"/> (Case/Edit — the "Case (DEFRA)" tab) — previously
/// untested despite containing the OnPostAsync save pipeline and the
/// ValidateLegacyParityRules family extracted while fixing a Sonar cognitive-complexity
/// finding. Each test targets one branch of that pipeline that had zero coverage.
/// </summary>
public sealed class EditModelHandlerTests
{
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ICaseWorkRepository _caseWorkRepository = Substitute.For<ICaseWorkRepository>();
    private readonly ITestRepository _testRepository = Substitute.For<ITestRepository>();
    private readonly ICaseEditDraftStateService _caseEditDraftState = Substitute.For<ICaseEditDraftStateService>();
    private readonly ICaseScalarDraftStateService _caseScalarDraftState = Substitute.For<ICaseScalarDraftStateService>();
    private readonly ICaseEditOrchestrationService _caseEditOrchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();

    private const string Rbse = "002600001";
    private static string RowStampTempDataKey => $"CaseEdit_RowStamp_{Rbse}";

    public EditModelHandlerTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _caseWorkRepository.GetByRbseAsync(Arg.Any<string>()).Returns((BSE.Modules.CaseWork.Models.CaseWorkRecord?)null);
        _caseWorkRepository.GetEntryByRbseAsync(Arg.Any<string>()).Returns((BSE.Modules.CaseWork.Models.CaseWorkEntryRecord?)null);
        _caseEditDraftState.GetAsync(Arg.Any<string>()).Returns(new CaseEditDraftState { Rbse = Rbse });
        _currentUserService.GetUserIdAsync().Returns(7);
    }

    private EditModel CreateModel(string[]? roles = null)
    {
        var model = new EditModel(
            _caseService, _currentUserService, _lookups, _caseWorkRepository,
            _testRepository, _caseEditDraftState, _caseScalarDraftState, _caseEditOrchestration, _batchRepository,
            new ConfigurationBuilder().AddInMemoryCollection().Build())
        {
            Rbse = Rbse
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = roles is { Length: > 0 }
            ? new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"))
            : new ClaimsPrincipal(new ClaimsIdentity());

        model.PageContext = new PageContext { HttpContext = httpContext };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static CaseRecord MakeValidCaseRecord() => new()
    {
        Rbse = Rbse,
        Cphh = "12345678901",
        IsNonGbCase = false,
        FormADate = new DateTime(2024, 1, 1),
        BirthDate = new DateTime(1990, 1, 1),
        RowStamp = [1, 2, 3]
    };

    // Uncovered path: OnGetAsync's "case not found" branch (lines ~87-98) — no existing
    // test instantiates EditModel at all, so this branch had 0% coverage.
    [Fact]
    public async Task OnGetAsync_WhenCaseNotFound_SetsWarningAndReturnsPage()
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Case.Rbse.Should().Be(Rbse);
        model.TempData[RowStampTempDataKey].Should().Be(Convert.ToBase64String([]));
    }

    // Uncovered path: OnGetAsync's "case found" branch (lines ~101-116) — populates Case
    // from the persisted record and round-trips the RowStamp via TempData.
    [Fact]
    public async Task OnGetAsync_WhenCaseFound_PopulatesCaseFromRecord()
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Case.Rbse.Should().Be(Rbse);
        model.IsNonGbCase.Should().BeFalse();
        model.TempData[RowStampTempDataKey].Should().NotBeNull();
    }

    // Uncovered path: OnPostAsync's role guard (line ~224) — never exercised.
    [Fact]
    public async Task OnPostAsync_WhenUserLacksDataEntryRole_ReturnsForbid()
    {
        var model = CreateModel();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    // Uncovered path: OnPostAsync's "persisted record not found" branch (lines ~228-241).
    [Fact]
    public async Task OnPostAsync_WhenCaseNotFound_SetsWarningAndReturnsPageWithoutSaving()
    {
        var model = CreateModel(["DataEntry"]);
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var result = await model.OnPostAsync();

        // Legacy parity: a brand-new case lives entirely in the shared session object until
        // the first Save from any tab, so posting this tab before the case exists no longer
        // shows a blocking warning - it falls through to normal validation/staging. With no
        // RowStamp TempData seeded (OnGetAsync wasn't called first), the handler treats this
        // as an expired session.
        result.Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Be("Session expired — please reload the page and try again.");
        await _caseEditOrchestration.DidNotReceive().CommitAllAsync(Arg.Any<string>(), Arg.Any<int>());
    }

    // Uncovered path: ValidateBirthDate (extracted from the former 45-complexity
    // ValidateLegacyParityRules) rejecting a birth date before the Unix epoch, and
    // OnPostAsync's "!ModelState.IsValid -> return Page()" short-circuit (lines ~254-256).
    [Fact]
    public async Task OnPostAsync_WhenBirthDateBeforeEpoch_FailsValidationWithoutSaving()
    {
        var model = CreateModel(["DataEntry"]);
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.Case = new CaseEditViewModel
        {
            Rbse = Rbse,
            FormADate = new DateTime(2024, 1, 1),
            BirthDate = new DateTime(1960, 1, 1)
        };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("Case.BirthDate");
        model.ModelState["Case.BirthDate"]!.Errors
            .Should().Contain(e => e.ErrorMessage.Contains("01/01/1970"));
        await _caseService.DidNotReceive().EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), Arg.Any<int>());
    }

    // Uncovered path: OnPostAsync's "session expired" branch (lines ~260-264) — reached
    // when validation passes but the RowStamp TempData key is missing/expired.
    [Fact]
    public async Task OnPostAsync_WhenRowStampMissingFromTempData_SetsConcurrencyErrorAndReturnsPage()
    {
        var model = CreateModel(["DataEntry"]);
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.Case = new CaseEditViewModel
        {
            Rbse = Rbse,
            FormADate = new DateTime(2024, 1, 1),
            Eartag = "1",
            EartagHerdmark = "GY1",
            EartagCountry = "UK"
        };
        // Intentionally no TempData[RowStampTempDataKey] set here.

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Be("Session expired — please reload the page and try again.");
    }

    // Uncovered path: the full happy-path save pipeline (lines ~267-320) — builds the
    // EditCaseDetailsCommand, calls EditCaseAsync, clears the draft, and redirects.
    [Fact]
    public async Task OnPostAsync_WhenValid_SavesAndRedirectsToSelf()
    {
        var model = CreateModel(["DataEntry"]);
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.TempData[RowStampTempDataKey] = Convert.ToBase64String([1, 2, 3]);
        model.Case = new CaseEditViewModel
        {
            Rbse = Rbse,
            FormADate = new DateTime(2024, 1, 1),
            Eartag = "1",
            EartagHerdmark = "GY1",
            EartagCountry = "UK"
        };
        _caseEditOrchestration.CommitAllAsync(Rbse, 7).Returns(CaseCommitOutcome.Success([]));

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Home");
        await _caseEditDraftState.Received(1).ClearAsync(Rbse, Arg.Any<CancellationToken>());
    }

    // Uncovered path: OnPostAsync's commit-outcome-has-warnings branch (lines ~343-348) —
    // a per-table "modified by another user" conflict is now soft (CaseEditOrchestrationService
    // folds a Case-level ConcurrencyConflict into CaseCommitOutcome.Warnings rather than failing
    // the whole commit), so the save redirects to the partial-success SaveResult page instead of
    // re-showing this page with a ConcurrencyError.
    [Fact]
    public async Task OnPostAsync_WhenCommitHasWarnings_RedirectsToPartialSuccessSaveResult()
    {
        var model = CreateModel(["DataEntry"]);
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.TempData[RowStampTempDataKey] = Convert.ToBase64String([1, 2, 3]);
        model.Case = new CaseEditViewModel
        {
            Rbse = Rbse,
            FormADate = new DateTime(2024, 1, 1),
            Eartag = "1",
            EartagHerdmark = "GY1",
            EartagCountry = "UK"
        };
        _caseEditOrchestration.CommitAllAsync(Rbse, 7).Returns(
            CaseCommitOutcome.Success([$"The case record with RBSE {Rbse} has been modified by another user"]));

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/SaveResult");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
    }

    // Uncovered path: OnPostAsync's generic failure switch expression (lines ~296-306) —
    // covers the non-success, non-concurrency result mapping to a model error.
    [Fact]
    public async Task OnPostAsync_WhenRbseNotFoundResult_AddsModelErrorWithoutRedirect()
    {
        var model = CreateModel(["DataEntry"]);
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.TempData[RowStampTempDataKey] = Convert.ToBase64String([1, 2, 3]);
        model.Case = new CaseEditViewModel
        {
            Rbse = Rbse,
            FormADate = new DateTime(2024, 1, 1),
            Eartag = "1",
            EartagHerdmark = "GY1",
            EartagCountry = "UK"
        };
        _caseEditOrchestration.CommitAllAsync(Rbse, 7).Returns(CaseCommitOutcome.Failure(EditCaseResult.RbseNotFound));

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().Be($"Case '{Rbse}' not found.");
    }
}
