using System.Security.Claims;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
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
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class VlaModelFlowTests
{
    private const string Rbse = "002600001";
    private static readonly string RowStampKey = $"VlaEdit_RowStamp_{Rbse}";

    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly IBabRepository _bab = Substitute.For<IBabRepository>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ICaseWizardStateService _wizard = Substitute.For<ICaseWizardStateService>();
    private readonly ICaseEditSessionStateService _session = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly IOtherOwnerRepository _owners = Substitute.For<IOtherOwnerRepository>();
    private readonly ITestRepository _tests = Substitute.For<ITestRepository>();
    private CaseEditSessionState? _stored;

    public VlaModelFlowTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(new[] { new BatchNumberEntry(1, "B1", Rbse, null) });
        _wizard.GetAsync().Returns((CaseWizardState?)null);
        _bab.GetByRbseAsync(Arg.Any<string>()).Returns((CaseBabRecord?)null);
        _owners.GetByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<OtherOwnerRecord>());
        _user.GetUserIdAsync().Returns(1);
        _session.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => _stored);
        _session.SetAsync(Arg.Do<CaseEditSessionState>(d => _stored = d), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _orchestration.CommitAllAsync(Rbse, 1).Returns(CaseCommitOutcome.Success([]));
    }

    private VlaModel Create(bool editable = true, bool withRowStamp = true)
    {
        if (!editable)
            _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());

        var model = new VlaModel(_caseService, _bab, _user, _wizard, _session, Substitute.For<ICaseDraftDiscardService>(),
            _orchestration, _lookups, _batches, _owners, _tests, new ConfigurationBuilder().Build())
        {
            Rbse = Rbse
        };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, "DataEntry"), new Claim(ClaimTypes.Role, "VLAAccess")], "TestAuth"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        if (withRowStamp)
            model.TempData[RowStampKey] = Convert.ToBase64String([1]);
        return model;
    }

    private static CaseBabRecord TracedBab() => new() { Rbse = Rbse, TracedName = "Trader", RowStamp = [1] };

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var model = Create();
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Theory]
    [InlineData("cphh", "desc")]
    [InlineData("name", "asc")]
    [InlineData("type", "asc")]
    public async Task OnGet_SortsStagedOwners(string sort, string dir)
    {
        _stored = new CaseEditSessionState
        {
            Rbse = Rbse,
            HasCaseEditChanges = true,
            OtherOwners =
            [
                new CaseEditDraftOtherOwnerItem { Id = 1, Type = "A", Name = "x", Cphh = "1", RowStampBase64 = "" },
                new CaseEditDraftOtherOwnerItem { Id = 2, Type = "B", Name = "y", Cphh = "2", RowStampBase64 = "" }
            ]
        };
        _bab.GetByRbseAsync(Rbse).Returns(TracedBab());
        var model = Create();
        model.OSort = sort;
        model.ODir = dir;

        await model.OnGetAsync();

        model.OtherOwnersTotalCount.Should().Be(2);
        model.HasTracedBabData.Should().BeTrue();
        model.HasUnsavedOwnerChanges.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_PendingBatchForCase_AllowsEdit()
    {
        _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _wizard.GetAsync().Returns(new CaseWizardState(Rbse, "B9", 9));
        var model = Create();

        await model.OnGetAsync();

        model.CanEditMainCase.Should().BeTrue();
    }

    [Fact]
    public async Task Post_MissingRowStamp_ReportsSessionExpired()
    {
        var model = Create(withRowStamp: false);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Contain("Session expired");
    }

    [Fact]
    public async Task Post_InvalidDomainRules_ReturnsPage()
    {
        var model = Create();
        model.Case.OnsetDate = DateTime.Today.AddDays(5);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.ContainsKey("Case.OnsetDate").Should().BeTrue();
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task Post_Valid_NormalisesStagesAndCommits()
    {
        var model = Create();
        model.Case.Origin = "N";
        model.Case.PurchasedCounty = "Devon";
        model.Case.FormBDate = DateTime.Today.AddDays(-3);
        model.Case.BirthDateSource = "X";
        model.Case.IsBirthDateEst = true;
        model.Case.IsOnsetDateEst = true;

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.Case.PurchasedCounty.Should().BeNull();
        model.Case.SlaughterDate.Should().Be(model.Case.FormBDate);
        model.Case.BirthDateSource.Should().BeNull();
        model.Case.IsBirthDateEst.Should().BeFalse();
        model.Case.IsOnsetDateEst.Should().BeFalse();
        _stored!.Case.Should().NotBeNull();
        _stored.CaseBaseRowStampBase64.Should().Be(Convert.ToBase64String([1]));
    }

    [Theory]
    [InlineData(EditCaseResult.RbseNotFound, "not found")]
    [InlineData(EditCaseResult.AuditLogError, "Audit log error")]
    [InlineData(EditCaseResult.PostUpdateError, "Database error")]
    [InlineData(EditCaseResult.ConcurrencyConflict, "Update failed")]
    public async Task Post_CommitFailure_SetsMessage(EditCaseResult failure, string expected)
    {
        _orchestration.CommitAllAsync(Rbse, 1).Returns(CaseCommitOutcome.Failure(failure));
        var model = Create();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"]!.ToString().Should().Contain(expected);
    }

    [Fact]
    public async Task Post_NotPurchased_ClearsPersistedTracedBabFields()
    {
        _bab.GetByRbseAsync(Rbse).Returns(TracedBab());
        var model = Create();
        model.Case.Origin = "N";

        await model.OnPostAsync();

        _stored!.Bab.Should().NotBeNull();
        _stored.Bab!.TracedName.Should().BeNull();
        _stored.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task Post_NotPurchased_ClearsAlreadyStagedTracedBabFields()
    {
        _stored = new CaseEditSessionState
        {
            Rbse = Rbse,
            Bab = new EditCaseBabCommand(Rbse, "12345678901", null, "T", "a1", "a2", "a3", "pc", null, null, null, [])
        };
        var model = Create();
        model.Case.Origin = "N";

        await model.OnPostAsync();

        _stored!.Bab!.NatalCphh.Should().BeNull();
        _stored.Bab.TracedPostcode.Should().BeNull();
    }

    [Fact]
    public async Task Post_NotPurchased_NothingTracedToClear_LeavesBabUntouched()
    {
        _bab.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, Notes = "only notes" });
        var model = Create();
        model.Case.Origin = "N";

        await model.OnPostAsync();

        _stored!.Bab.Should().BeNull();
    }

    [Fact]
    public async Task Post_NotPurchased_StagedBabAlreadyBlank_LeavesBabUntouched()
    {
        var blank = new EditCaseBabCommand(Rbse, null, "n", null, null, null, null, null, null, null, null, []);
        _stored = new CaseEditSessionState { Rbse = Rbse, Bab = blank };
        var model = Create();
        model.Case.Origin = "N";

        await model.OnPostAsync();

        _stored!.Bab.Should().Be(blank);
    }

    [Fact]
    public async Task StageAndGoto_NotEditable_JustRedirects()
    {
        var result = await Create(editable: false).OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _stored.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_Valid_StagesAndRedirects()
    {
        var model = Create();
        model.Case.Origin = "N";
        model.Case.Sex = "F";

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _stored!.Case.Should().NotBeNull();
        _stored.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task StageAndGoto_MissingRowStamp_ReturnsPageWithError()
    {
        var model = Create(withRowStamp: false);

        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().NotBeNull();
    }

    [Fact]
    public async Task StageAndGoto_Invalid_ReturnsPage()
    {
        var model = Create();
        model.Case.MonthsPregnant = 12;

        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task StageAndGoToAuditLog_Valid_RedirectsToAuditLog()
    {
        var model = Create();
        model.Case.Origin = "N";

        var result = await model.OnPostStageAndGoToAuditLogAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/AuditLog/ByCase");
        redirect.RouteValues!["returnTo"].Should().Be("/Case/Vla");
    }

    [Fact]
    public async Task StageAndGoToAuditLog_Invalid_ReturnsPage()
    {
        var model = Create();
        model.Case.MonthsPostCalving = 9;

        (await model.OnPostStageAndGoToAuditLogAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task ResortOwners_Valid_ReturnsPageWithOwners()
    {
        var model = Create();
        model.Case.Origin = "N";

        (await model.OnPostResortOwnersAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task ResortOwners_MissingRowStamp_ReturnsPage()
    {
        var model = Create(withRowStamp: false);

        (await model.OnPostResortOwnersAsync()).Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateOwnerRow_Invalid_ReopensEditRow()
    {
        _stored = new CaseEditSessionState { Rbse = Rbse, HasCaseEditChanges = true };
        var model = Create();
        model.EditingOwnerId = 5;
        model.EditOwnerType = null;

        (await model.OnPostUpdateOwnerRowAsync()).Should().BeOfType<PageResult>();
        model.ReopenEditOwnerId.Should().Be(5);
    }

    [Fact]
    public async Task UpdateOwnerRow_UnknownOwner_SetsWarning()
    {
        _stored = new CaseEditSessionState { Rbse = Rbse, HasCaseEditChanges = true };
        var model = Create();
        model.EditingOwnerId = 99;
        model.EditOwnerType = "T";
        model.EditOwnerName = "n";

        var result = await model.OnPostUpdateOwnerRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["Warning"].Should().Be("Owner record not found.");
    }

    [Fact]
    public async Task DeleteOwner_UnknownOwner_SetsWarning()
    {
        _stored = new CaseEditSessionState { Rbse = Rbse, HasCaseEditChanges = true };
        var model = Create();

        var result = await model.OnPostDeleteOwnerAsync(42, "");

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["Warning"].Should().Be("Owner record not found.");
    }

    [Fact]
    public async Task AddOwnerRow_Valid_StagesPostedCaseScalarsToo()
    {
        _stored = new CaseEditSessionState { Rbse = Rbse, HasCaseEditChanges = true };
        var model = Create();
        model.NewOwnerType = "T";
        model.NewOwnerName = "Name";
        model.NewOwnerCphh = "12/345/6789/01";

        await model.OnPostAddOwnerRowAsync();

        _stored!.OtherOwners.Should().ContainSingle().Which.Cphh.Should().Be("12345678901");
        _stored.Case.Should().NotBeNull();
    }

    [Fact]
    public async Task AddOwnerRow_InvalidCphh_ShowsAddRow()
    {
        _stored = new CaseEditSessionState { Rbse = Rbse, HasCaseEditChanges = true };
        var model = Create();
        model.NewOwnerType = "T";
        model.NewOwnerCphh = "123";

        (await model.OnPostAddOwnerRowAsync()).Should().BeOfType<PageResult>();
        model.ShowAddOwnerRow.Should().BeTrue();
    }
}
