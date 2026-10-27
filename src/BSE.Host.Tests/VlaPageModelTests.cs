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
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Tests;

public sealed class VlaPageModelTests
{
    private const string Rbse = "002600001";
    private const string RowStampKey = "VlaEdit_RowStamp_" + Rbse;
    private static readonly DateTime Today = DateTime.Today;
    private static readonly string[] Vla = ["DataEntry", "VLAAccess"];

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IBabRepository _bab = Substitute.For<IBabRepository>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ICaseWizardStateService _wizard = Substitute.For<ICaseWizardStateService>();
    private readonly ICaseEditSessionStateService _session = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batch = Substitute.For<IBatchRepository>();
    private readonly IOtherOwnerRepository _owners = Substitute.For<IOtherOwnerRepository>();
    private readonly ITestRepository _tests = Substitute.For<ITestRepository>();
    private CaseEditSessionState? _draft;
    private CaseRecord? _record = new() { Rbse = Rbse, Cphh = "01001000101", Origin = "P", FormADate = Today.AddDays(-50), RowStamp = [1, 2] };
    private IReadOnlyList<BatchNumberEntry> _batches = [new BatchNumberEntry(1, "B1", Rbse, "BSE1")];

    public VlaPageModelTests()
    {
        _cases.GetCaseAsync(Rbse).Returns(_ => _record);
        _user.GetUserIdAsync().Returns(5);
        _batch.GetBatchNumbersByRbseAsync(Rbse).Returns(_ => Task.FromResult(_batches));
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Task.FromResult<IEnumerable<LookupItem>>([new LookupItem(1, "CUR", "Current Owner")]));
        _lookups.GetLookupAsync(LookupTableId.OwnerType).Returns(Task.FromResult<IEnumerable<LookupItem>>(
            [new LookupItem(1, "PRV", "Previous Owner"), new LookupItem(2, "CUR", "Current Owner")]));
        _owners.GetByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<OtherOwnerRecord>>(
            [new OtherOwnerRecord(10, Rbse, "CUR", "Bob", null, [1]), new OtherOwnerRecord(11, Rbse, "PRV", "Old", "01001000101", null)]));
        _tests.GetByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<CaseTestRecord>>([new CaseTestRecord(1, Rbse, "T1", "Type", "P", "Pos", [1])]));
        _session.GetAsync(Arg.Any<string>()).Returns(_ => _draft);
        _session.SetAsync(Arg.Any<CaseEditSessionState>()).Returns(ci => { _draft = ci.Arg<CaseEditSessionState>(); return Task.CompletedTask; });
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Success([]));
    }

    private VlaModel CreateModel(string[]? roles = null, bool rowStamp = true)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build();
        var model = new VlaModel(_cases, _bab, _user, _wizard, _session, _discard, _orchestration, _lookups, _batch, _owners, _tests, config)
        { Rbse = Rbse };
        PageTestContext.Attach(model, roles ?? Vla);
        if (rowStamp) model.TempData[RowStampKey] = Convert.ToBase64String(new byte[] { 1, 2 });
        return model;
    }

    private static void MakeValid(VlaModel model)
    {
        model.Case.Origin = "P";
        model.Case.FormADate = Today.AddDays(-50);
        model.Case.BirthDate = Today.AddDays(-1000);
    }

    private static string FirstError(VlaModel model, string key) => model.ModelState[key]!.Errors[0].ErrorMessage;

    // ---------- GET ----------

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var model = CreateModel();
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_ExistingCase_LoadsEverything()
    {
        _bab.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, TracedName = "T" });
        var model = CreateModel();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Case.Rbse.Should().Be(Rbse);
        model.SpolSiteUrl.Should().Be("http://spol");
        model.HasTracedBabData.Should().BeTrue();
        model.CanEditMainCase.Should().BeTrue();
        model.BatchNumbers.Should().HaveCount(1);
        model.OtherOwnersTotalCount.Should().Be(2);
        model.OtherOwners.Should().HaveCount(2);
        model.SexOptions.Should().NotBeEmpty();
        model.TempData[RowStampKey].Should().Be(Convert.ToBase64String(new byte[] { 1, 2 }));
        _draft!.Tests.Should().HaveCount(1);
    }

    [Fact]
    public async Task OnGet_NewCase_StoresEmptyRowStamp()
    {
        _record = null;
        _batches = [];
        var model = CreateModel(rowStamp: false);

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.TempData[RowStampKey].Should().Be("");
        model.CanEditMainCase.Should().BeFalse();
        model.HasTracedBabData.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_WithoutVlaRole_CannotEdit()
    {
        var model = CreateModel(["DataEntry"]);

        await model.OnGetAsync();

        model.CanEditMainCase.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_PendingBatchForCase_AllowsEditWithoutHistory()
    {
        _batches = [];
        _wizard.GetAsync().Returns(new CaseWizardState("00/26/00001", "B9", 9));
        var model = CreateModel();

        await model.OnGetAsync();

        model.PendingBatch.Should().NotBeNull();
        model.CanEditMainCase.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_PendingBatchForOtherCase_DoesNotAllowEdit()
    {
        _batches = [];
        _wizard.GetAsync().Returns(new CaseWizardState("002600099", "B9", 9));
        var model = CreateModel();

        await model.OnGetAsync();

        model.CanEditMainCase.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_StagedCaseOverlay_IsApplied()
    {
        var staged = new VlaEditViewModel { Rbse = Rbse, Sex = "M", Origin = "P" }.ToEditCommand([1, 2]);
        _draft = new CaseEditSessionState { Rbse = Rbse, Case = staged, HasCaseEditChanges = true, OtherOwners = [new CaseEditDraftOtherOwnerItem { Id = 1, Type = "CUR", Name = "N" }] };
        var model = CreateModel();

        await model.OnGetAsync();

        model.Case.Sex.Should().Be("M");
        model.HasUnsavedOwnerChanges.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_OwnersPagedAndSorted()
    {
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse,
            HasCaseEditChanges = true,
            OtherOwners = Enumerable.Range(1, 12).Select(i => new CaseEditDraftOtherOwnerItem { Id = i, Type = "CUR", Name = $"N{i:00}", Cphh = $"C{i:00}" }).ToList()
        };
        var model = CreateModel();
        model.OPage = 9;
        model.OSort = "name";
        model.ODir = "desc";

        await model.OnGetAsync();

        model.OtherOwnersTotalPages.Should().Be(2);
        model.OPage.Should().Be(2);
        model.OtherOwners.Select(o => o.Name).Should().Equal("N02", "N01");
    }

    [Theory]
    [InlineData("cphh")]
    [InlineData("type")]
    public async Task OnGet_OwnerSortKeys(string sort)
    {
        var model = CreateModel();
        model.OSort = sort;

        await model.OnGetAsync();

        model.OtherOwners.Should().HaveCount(2);
    }

    // ---------- POST (save) ----------

    [Fact]
    public async Task OnPost_WithoutDataEntry_Forbids()
    {
        (await CreateModel(["VLAAccess"]).OnPostAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPost_NoEditPermission_OnlyCommitsStagedChanges()
    {
        _batches = [];
        var model = CreateModel();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _orchestration.Received(1).CommitAllAsync(Rbse, 5);
        _draft.Should().BeNull();
    }

    [Fact]
    public async Task OnPost_Valid_StagesScalarsAndCommits()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.Sex = "F";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _draft!.Case!.Sex.Should().Be("F");
        _draft.HasPendingChanges.Should().BeTrue();
        await _orchestration.Received(1).CommitAllAsync(Rbse, 5);
    }

    [Fact]
    public async Task OnPost_NewCaseWithoutPersistedRecord_StillStages()
    {
        _record = null;
        var model = CreateModel();
        MakeValid(model);

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();

        _draft!.Case.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPost_InvalidBirthDate_ReturnsPageAndDoesNotCommit()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.BirthDate = new DateTime(1960, 1, 1);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        FirstError(model, "Case.BirthDate").Should().Contain("Birth Date must be after");
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task OnPost_MissingRowStamp_ReportsSessionExpired()
    {
        var model = CreateModel(rowStamp: false);
        MakeValid(model);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ConcurrencyError.Should().Contain("Session expired");
    }

    [Fact]
    public async Task OnPost_NonPurchasedOrigin_ClearsPurchaseFields_AndTracedBabFields()
    {
        _bab.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, NatalCphh = "N", TracedName = "T", RowStamp = [3] });
        var model = CreateModel();
        MakeValid(model);
        model.Case.Origin = "H";
        model.Case.PurchaseDate = Today.AddDays(-100);
        model.Case.PurchaseAgeInMonths = 5;
        model.Case.PurchasedCounty = "X";

        await model.OnPostAsync();

        model.Case.PurchaseDate.Should().BeNull();
        model.Case.PurchaseAgeInMonths.Should().BeNull();
        model.Case.PurchasedCounty.Should().BeNull();
        _draft!.Bab!.TracedName.Should().BeNull();
        _draft.Bab.NatalCphh.Should().BeNull();
    }

    [Fact]
    public async Task OnPost_NonPurchasedOrigin_BabWithoutTracedData_StagesNothingExtra()
    {
        _bab.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse });
        var model = CreateModel();
        MakeValid(model);
        model.Case.Origin = "H";

        await model.OnPostAsync();

        _draft!.Bab.Should().BeNull();
    }

    [Fact]
    public async Task OnPost_NonPurchasedOrigin_UsesAlreadyStagedBab()
    {
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse,
            Bab = new EditCaseBabCommand(Rbse, "N", null, "Traced", null, null, null, null, null, null, null, [1])
        };
        var model = CreateModel();
        MakeValid(model);
        model.Case.Origin = "H";

        await model.OnPostAsync();

        _draft!.Bab!.TracedName.Should().BeNull();
        _draft.Bab.NatalCphh.Should().BeNull();
    }

    [Fact]
    public async Task OnPost_NormalisesDependentFields()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormBDate = Today.AddDays(-10);
        model.Case.BirthDate = null;
        model.Case.BirthDateSource = "S";
        model.Case.IsBirthDateEst = true;
        model.Case.OnsetDate = null;
        model.Case.IsOnsetDateEst = true;

        await model.OnPostAsync();

        model.Case.SlaughterDate.Should().Be(Today.AddDays(-10));
        model.Case.BirthDateSource.Should().BeNull();
        model.Case.IsBirthDateEst.Should().BeFalse();
        model.Case.IsOnsetDateEst.Should().BeFalse();
    }

    [Fact]
    public async Task OnPost_CommitFails_RedirectsHomeWithMessage()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Failure(EditCaseResult.AuditLogError));
        var model = CreateModel();
        MakeValid(model);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().Be("Audit log error during update.");
    }

    [Theory]
    [InlineData(EditCaseResult.RbseNotFound, "not found")]
    [InlineData(EditCaseResult.PostUpdateError, "Database error after update.")]
    [InlineData(EditCaseResult.ConcurrencyConflict, "Update failed: ConcurrencyConflict")]
    public async Task OnPost_CommitFailureMessages(EditCaseResult failure, string expected)
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Failure(failure));
        var model = CreateModel();
        MakeValid(model);

        await model.OnPostAsync();

        model.TempData["ErrorMessage"]!.ToString().Should().Contain(expected);
    }

    [Fact]
    public async Task OnPost_MandatoryFieldsMissing_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns<CaseCommitOutcome>(_ => throw new MandatoryCaseFieldsMissingException(["Missing X"]));
        var model = CreateModel();
        MakeValid(model);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task OnPost_CommitWarnings_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 5).Returns(CaseCommitOutcome.Success(["Row changed by another user"]));
        var model = CreateModel();
        MakeValid(model);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    // ---------- Domain rules ----------

    [Fact]
    public async Task Rules_PurchaseDate_BeforeBirth_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.PurchaseDate = Today.AddDays(-2000);

        await model.OnPostAsync();

        FirstError(model, "Case.PurchaseDate").Should().Contain("Purchase Date must be after the birth date");
    }

    [Fact]
    public async Task Rules_HerdEntryAfterFormA_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.HerdEntryDate = Today.AddDays(-10);

        await model.OnPostAsync();

        FirstError(model, "Case.HerdEntryDate").Should().Contain("Herd Entry Date");
    }

    [Fact]
    public async Task Rules_OnsetBeforeBirth_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.OnsetDate = Today.AddDays(-2000);

        await model.OnPostAsync();

        FirstError(model, "Case.OnsetDate").Should().Contain("after the Date Of Birth");
    }

    [Fact]
    public async Task Rules_OnsetAfterFormAWithoutBirth_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.BirthDate = null;
        model.Case.OnsetDate = Today.AddDays(-5);

        await model.OnPostAsync();

        FirstError(model, "Case.OnsetDate").Should().Be("Onset Date must be before the Form A Date");
    }

    [Fact]
    public async Task Rules_MonthsPregnantAndPostCalving_Conflict()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.MonthsPregnant = 12;
        model.Case.MonthsPostCalving = 5;

        await model.OnPostAsync();

        model.ModelState["Case.MonthsPostCalving"]!.Errors.Select(e => e.ErrorMessage)
            .Should().Contain(["You cannot enter a value for Month's Post Calving and Months Pregnant", "Months post calving must be between 1 and 3."]);
        FirstError(model, "Case.MonthsPregnant").Should().Contain("between 1 and 9");
    }

    [Fact]
    public async Task Rules_SlaughterOutsideFormAToToday_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.SlaughterDate = Today.AddDays(-80);

        await model.OnPostAsync();

        FirstError(model, "Case.SlaughterDate").Should().Contain("Form A Date and todays date");
    }

    [Fact]
    public async Task Rules_SlaughterWithoutFormA_UsesBirthBound()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormADate = null;
        model.Case.SlaughterDate = Today.AddDays(-2000);

        await model.OnPostAsync();

        FirstError(model, "Case.SlaughterDate").Should().Contain("Birth Date and todays date");
    }

    [Fact]
    public async Task Rules_FutureSlaughterWithoutAnchors_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormADate = null;
        model.Case.BirthDate = null;
        model.Case.SlaughterDate = Today.AddDays(5);

        await model.OnPostAsync();

        FirstError(model, "Case.SlaughterDate").Should().Contain("future date");
    }

    // ---------- Stage and goto ----------

    [Fact]
    public async Task StageAndGoto_WithoutDataEntry_JustRedirects()
    {
        var result = await CreateModel(["ReadOnly"]).OnPostStageAndGotoAsync("/Case/Farm");

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        _draft.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_NoEditPermission_RedirectsWithoutStaging()
    {
        _batches = [];
        var model = CreateModel();

        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<RedirectToPageResult>();

        _draft.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_Valid_StagesAndRedirects()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.Sex = "M";

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<RedirectToPageResult>().Which.RouteValues!["rbse"].Should().Be(Rbse);
        _draft!.Case!.Sex.Should().Be("M");
    }

    [Fact]
    public async Task StageAndGoto_Invalid_ReturnsPage()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.HerdEntryDate = Today.AddDays(-10);

        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task StageAndGoto_MissingRowStamp_ReportsSessionExpired()
    {
        var model = CreateModel(rowStamp: false);
        MakeValid(model);

        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<PageResult>();

        model.ConcurrencyError.Should().NotBeNull();
    }

    [Fact]
    public async Task StageAndGoToAuditLog_Valid_RedirectsToAuditLog()
    {
        var model = CreateModel();
        MakeValid(model);

        var result = await model.OnPostStageAndGoToAuditLogAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/AuditLog/ByCase");
        redirect.RouteValues!["returnTo"].Should().Be("/Case/Vla");
    }

    [Fact]
    public async Task ResortOwners_Valid_ReturnsPageWithOwners()
    {
        var model = CreateModel();
        MakeValid(model);

        (await model.OnPostResortOwnersAsync()).Should().BeOfType<PageResult>();

        model.OtherOwners.Should().HaveCount(2);
    }

    [Fact]
    public async Task ResortOwners_Invalid_ReturnsPage()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.HerdEntryDate = Today.AddDays(-10);

        (await model.OnPostResortOwnersAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task Cancel_DiscardsDraftAndRedirectsHome()
    {
        var result = await CreateModel().OnGetCancelVlaEditAsync();

        await _discard.Received(1).DiscardAllAsync(Rbse);
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public void OwnersSortUrl_TogglesDirection()
    {
        var model = CreateModel();
        model.OSort = "name";
        model.ODir = "asc";

        model.OwnersSortUrl("name").Should().Contain("ODir=desc");
        model.OwnersSortUrl("cphh").Should().Contain("ODir=asc").And.Contain("OSort=cphh");
    }

    [Theory]
    [InlineData("P", "p", "selected")]
    [InlineData("P", "H", null)]
    public void SelectedIfMatches_IgnoresCase(string current, string option, string? expected)
    {
        VlaModel.SelectedIfMatches(current, option).Should().Be(expected);
    }

    // ---------- Owner rows ----------

    [Fact]
    public async Task BeginEditOwner_WithoutDataEntry_Forbids()
    {
        (await CreateModel(["VLAAccess"]).OnPostBeginEditOwnerRowAsync(10)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task BeginEditOwner_NoEditPermission_RedirectsToSelf()
    {
        _batches = [];

        (await CreateModel().OnPostBeginEditOwnerRowAsync(10)).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task BeginEditOwner_UnknownOwner_RedirectsToSelf()
    {
        (await CreateModel().OnPostBeginEditOwnerRowAsync(999)).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task BeginEditOwner_Known_PopulatesEditFields()
    {
        var model = CreateModel();

        (await model.OnPostBeginEditOwnerRowAsync(10)).Should().BeOfType<PageResult>();

        model.ReopenEditOwnerId.Should().Be(10);
        model.EditOwnerType.Should().Be("CUR");
        model.EditOwnerName.Should().Be("Bob");
        model.EditOwnerRowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 1 }));
    }

    [Fact]
    public async Task AddOwner_WithoutDataEntry_Forbids()
    {
        (await CreateModel(["VLAAccess"]).OnPostAddOwnerRowAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddOwner_NoEditPermission_Redirects()
    {
        _batches = [];

        (await CreateModel().OnPostAddOwnerRowAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task AddOwner_Empty_ShowsRowWithErrors()
    {
        var model = CreateModel();

        (await model.OnPostAddOwnerRowAsync()).Should().BeOfType<PageResult>();

        model.ShowAddOwnerRow.Should().BeTrue();
        model.ModelState.Keys.Should().BeEquivalentTo("NewOwnerType", "NewOwnerName");
    }

    [Fact]
    public async Task AddOwner_BadCphh_IsRejected()
    {
        var model = CreateModel();
        model.NewOwnerType = "CUR";
        model.NewOwnerCphh = "12";

        await model.OnPostAddOwnerRowAsync();

        FirstError(model, "NewOwnerCphh").Should().Be("CPHH must be 11 digits.");
    }

    [Fact]
    public async Task AddOwner_DuplicatePrevious_IsRejected()
    {
        var model = CreateModel();
        model.NewOwnerType = "PRV";
        model.NewOwnerName = "Another";

        await model.OnPostAddOwnerRowAsync();

        FirstError(model, "NewOwnerType").Should().Contain("only have one owner of type Previous");
    }

    [Fact]
    public async Task AddOwner_Valid_StagesWithTemporaryNegativeId()
    {
        var model = CreateModel();
        model.NewOwnerType = "CUR";
        model.NewOwnerCphh = "01/001/0001/01";

        var result = await model.OnPostAddOwnerRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var added = _draft!.OtherOwners.Single(o => o.Id < 0);
        added.Id.Should().Be(-1);
        added.Cphh.Should().Be("01001000101");
        added.Name.Should().BeNull();
        _draft.HasCaseEditChanges.Should().BeTrue();
        _draft.Case.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateOwner_WithoutDataEntry_Forbids()
    {
        (await CreateModel(["VLAAccess"]).OnPostUpdateOwnerRowAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateOwner_NoEditPermission_Redirects()
    {
        _batches = [];

        (await CreateModel().OnPostUpdateOwnerRowAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task UpdateOwner_Invalid_ReopensEditRow()
    {
        var model = CreateModel();
        model.EditingOwnerId = 10;

        (await model.OnPostUpdateOwnerRowAsync()).Should().BeOfType<PageResult>();

        model.ReopenEditOwnerId.Should().Be(10);
    }

    [Fact]
    public async Task UpdateOwner_PreviousOwnerOnSameRow_IsAllowed()
    {
        var model = CreateModel();
        model.EditingOwnerId = 11;
        model.EditOwnerType = "PRV";
        model.EditOwnerName = "Renamed";

        (await model.OnPostUpdateOwnerRowAsync()).Should().BeOfType<RedirectToPageResult>();

        _draft!.OtherOwners.Single(o => o.Id == 11).Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task UpdateOwner_UnknownOwner_WarnsAndRedirects()
    {
        var model = CreateModel();
        model.EditingOwnerId = 999;
        model.EditOwnerType = "CUR";
        model.EditOwnerName = "X";

        (await model.OnPostUpdateOwnerRowAsync()).Should().BeOfType<RedirectToPageResult>();

        model.TempData["Warning"].Should().Be("Owner record not found.");
    }

    [Fact]
    public async Task DeleteOwner_WithoutDataEntry_Forbids()
    {
        (await CreateModel(["VLAAccess"]).OnPostDeleteOwnerAsync(10, "")).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteOwner_NoEditPermission_Redirects()
    {
        _batches = [];

        (await CreateModel().OnPostDeleteOwnerAsync(10, "")).Should().BeOfType<RedirectToPageResult>();
        _draft.Should().BeNull();
    }

    [Fact]
    public async Task DeleteOwner_Unknown_Warns()
    {
        var model = CreateModel();

        await model.OnPostDeleteOwnerAsync(999, "");

        model.TempData["Warning"].Should().Be("Owner record not found.");
    }

    [Fact]
    public async Task DeleteOwner_Known_RemovesFromDraft()
    {
        var model = CreateModel();

        (await model.OnPostDeleteOwnerAsync(10, "")).Should().BeOfType<RedirectToPageResult>();

        _draft!.OtherOwners.Should().NotContain(o => o.Id == 10);
        _draft.HasCaseEditChanges.Should().BeTrue();
    }
}
