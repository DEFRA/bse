using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace BSE.Host.Tests;

public sealed class ClinicalPageModelTests
{
    private const string Rbse = "002600001";
    private static readonly DateTime Today = DateTime.Today;
    private static readonly string[] Vla = ["DataEntry", "VLAAccess"];

    private readonly IClinicalRepository _clinical = Substitute.For<IClinicalRepository>();
    private readonly ICaseRepository _caseRepo = Substitute.For<ICaseRepository>();
    private readonly IBatchRepository _batch = Substitute.For<IBatchRepository>();
    private readonly ICaseWizardStateService _wizard = Substitute.For<ICaseWizardStateService>();
    private readonly ICaseEditSessionStateService _session = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private CaseEditSessionState? _draft;
    private CaseClinicalRecord? _record = new() { Rbse = Rbse, Apprehension = true, Tremor = true, RowStamp = [1, 2] };

    public ClinicalPageModelTests()
    {
        _clinical.GetByRbseAsync(Rbse).Returns(_ => _record);
        _clinical.GetVisitsByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<ClinicalVisitRecord>>(
            [new ClinicalVisitRecord(1, Rbse, new DateTime(2022, 3, 3), [7]), new ClinicalVisitRecord(2, Rbse, new DateTime(2022, 1, 1), null)]));
        _batch.GetBatchNumbersByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<BatchNumberEntry>>([new BatchNumberEntry(4, "B4", Rbse, "BSE1")]));
        _caseRepo.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, BirthDate = new DateTime(2015, 1, 1) });
        _user.GetUserIdAsync().Returns(8);
        _session.GetAsync(Arg.Any<string>()).Returns(_ => _draft);
        _session.SetAsync(Arg.Any<CaseEditSessionState>()).Returns(ci => { _draft = ci.Arg<CaseEditSessionState>(); return Task.CompletedTask; });
        _orchestration.CommitAllAsync(Rbse, 8).Returns(CaseCommitOutcome.Success([]));
    }

    private ClinicalModel CreateModel(string[]? roles = null, params string[] checkedSigns)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build();
        var model = new ClinicalModel(_clinical, _caseRepo, _batch, _wizard, _session, _discard, _orchestration, _user, config) { Rbse = Rbse };
        PageTestContext.Attach(model, roles ?? Vla);
        model.HttpContext.Request.Form = new FormCollection(
            checkedSigns.ToDictionary(s => $"Signs.{s}", _ => new StringValues("true")));
        return model;
    }

    // ---------- GET ----------

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var model = CreateModel();
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_LoadsSignsVisitsAndContext()
    {
        var model = CreateModel();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Signs.Apprehension.Should().BeTrue();
        model.Signs.Tremor.Should().BeTrue();
        model.Signs.Kicking.Should().BeFalse();
        model.ClinicalRowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 1, 2 }));
        model.BirthDate.Should().Be(new DateTime(2015, 1, 1));
        model.SpolSiteUrl.Should().Be("http://spol");
        model.BatchNumbers.Should().HaveCount(1);
        model.VisitsTotalCount.Should().Be(2);
        model.Visits.Select(v => v.Id).Should().Equal(2, 1);
        model.Visits[1].IsUnsaved.Should().BeFalse();
        model.IsVlaAllowedAdditionalCaseEdit.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_NoClinicalRecord_LeavesDefaults()
    {
        _record = null;
        var model = CreateModel();

        await model.OnGetAsync();

        model.ClinicalRowStampBase64.Should().BeNull();
        model.Signs.Apprehension.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_NullRowStamp_GivesNullBase64()
    {
        _record = new CaseClinicalRecord { Rbse = Rbse, RowStamp = null };
        var model = CreateModel();

        await model.OnGetAsync();

        model.ClinicalRowStampBase64.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_VisitsDescendingAndPaged()
    {
        _clinical.GetVisitsByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<ClinicalVisitRecord>>(
            Enumerable.Range(1, 12).Select(i => new ClinicalVisitRecord(i, Rbse, new DateTime(2022, 1, 1).AddDays(i), null)).ToList()));
        var model = CreateModel();
        model.VDir = "desc";
        model.VPage = 5;

        await model.OnGetAsync();

        model.VisitsTotalPages.Should().Be(2);
        model.VPage.Should().Be(2);
        model.Visits.Select(v => v.Id).Should().Equal(2, 1);
        model.VisitsSortUrl().Should().Contain("VDir=asc");
    }

    [Fact]
    public async Task OnGet_StagedSignsAndBirthDate_OverridePersisted()
    {
        var staged = new ClinicalModel.ClinicalSignsViewModel { Kicking = true }.ToEditCommand(Rbse, [1]);
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse,
            Clinical = staged,
            Case = new ClinicalCaseStub().Command(new DateTime(2018, 5, 5)),
            HasClinicalVisitsChanges = true
        };
        var model = CreateModel();

        await model.OnGetAsync();

        model.Signs.Kicking.Should().BeTrue();
        model.Signs.Apprehension.Should().BeFalse();
        model.BirthDate.Should().Be(new DateTime(2018, 5, 5));
        model.HasUnsavedChanges.Should().BeTrue();
        model.Visits.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_PendingBatchAlreadyLinked_LocksVlaEdit()
    {
        _wizard.GetAsync().Returns(new CaseWizardState(Rbse, "B4", 4));
        var model = CreateModel();

        await model.OnGetAsync();

        model.IsVlaAllowedAdditionalCaseEdit.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_PendingBatchNotLinked_AllowsEdit()
    {
        _wizard.GetAsync().Returns(new CaseWizardState(Rbse, "B9", 9));
        var model = CreateModel();

        await model.OnGetAsync();

        model.IsVlaAllowedAdditionalCaseEdit.Should().BeTrue();
    }

    // ---------- Save ----------

    [Theory]
    [InlineData("DataEntry")]
    [InlineData("VLAAccess")]
    public async Task SaveSigns_MissingRole_Forbids(string role)
    {
        (await CreateModel([role]).OnPostSaveSignsAsync(null)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SaveSigns_StagesPostedSignsAndCommits()
    {
        var model = CreateModel(null, "Kicking", "Tremor");

        var result = await model.OnPostSaveSignsAsync(Convert.ToBase64String(new byte[] { 5, 6 }));

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _draft!.Clinical!.Kicking.Should().BeTrue();
        _draft.Clinical.Apprehension.Should().BeFalse();
        _draft.Clinical.RowStamp.Should().Equal(5, 6);
        _draft.ClinicalBaseRowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 5, 6 }));
        _draft.HasPendingChanges.Should().BeTrue();
        await _orchestration.Received(1).CommitAllAsync(Rbse, 8);
    }

    [Fact]
    public async Task SaveSigns_BlankRowStamp_UsesEmptyArray()
    {
        var model = CreateModel();

        await model.OnPostSaveSignsAsync(null);

        _draft!.Clinical!.RowStamp.Should().BeEmpty();
        _draft.ClinicalBaseRowStampBase64.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveSigns_CommitFails_RedirectsHomeWithMessage()
    {
        _orchestration.CommitAllAsync(Rbse, 8).Returns(CaseCommitOutcome.Failure(EditCaseResult.PostUpdateError));
        var model = CreateModel();

        var result = await model.OnPostSaveSignsAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().Be("Unable to save clinical signs: PostUpdateError.");
    }

    [Fact]
    public async Task SaveSigns_MandatoryFieldsMissing_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 8).Returns<CaseCommitOutcome>(_ => throw new MandatoryCaseFieldsMissingException(["x"]));

        var result = await CreateModel().OnPostSaveSignsAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task SaveSigns_Warnings_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 8).Returns(CaseCommitOutcome.Success(["warn"]));

        var result = await CreateModel().OnPostSaveSignsAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    // ---------- Navigation ----------

    [Fact]
    public async Task StageAndGoto_DataEntry_StagesChangedSignsAndRedirects()
    {
        var model = CreateModel(["DataEntry"], "Kicking");

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm", Convert.ToBase64String(new byte[] { 1, 2 }));

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
        _draft!.Clinical!.Kicking.Should().BeTrue();
        _draft.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task StageAndGoto_UnchangedSigns_DoNotFlagPending()
    {
        var model = CreateModel(["DataEntry"], "Apprehension", "Tremor");

        await model.OnPostStageAndGotoAsync("/Case/Farm", Convert.ToBase64String(new byte[] { 1, 2 }));

        _draft!.HasPendingChanges.Should().BeFalse();
        _draft.Clinical.Should().NotBeNull();
    }

    [Fact]
    public async Task StageAndGoto_NewCaseWithoutClinicalRecord_ComparesAgainstBlankBaseline()
    {
        _record = null;
        var model = CreateModel(["DataEntry"]);

        await model.OnPostStageAndGotoAsync("/Case/Farm", null);

        _draft!.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task StageAndGoto_WithoutDataEntry_OnlyRedirects()
    {
        var result = await CreateModel(["ReadOnly"]).OnPostStageAndGotoAsync("/Case/Farm", null);

        result.Should().BeOfType<RedirectToPageResult>();
        _draft.Should().BeNull();
    }

    [Fact]
    public async Task ResortVisits_DataEntry_StagesAndReturnsPage()
    {
        var model = CreateModel(["DataEntry"], "Kicking");

        (await model.OnPostResortVisitsAsync(Convert.ToBase64String(new byte[] { 1, 2 }))).Should().BeOfType<PageResult>();

        _draft!.Clinical!.Kicking.Should().BeTrue();
        model.Visits.Should().HaveCount(2);
    }

    [Fact]
    public async Task ResortVisits_WithoutDataEntry_DoesNotStageSigns()
    {
        var model = CreateModel(["ReadOnly"]);

        (await model.OnPostResortVisitsAsync(null)).Should().BeOfType<PageResult>();

        _draft!.Clinical.Should().BeNull();
    }

    [Fact]
    public async Task Cancel_Post_And_Get_DiscardDraft()
    {
        var model = CreateModel();

        (await model.OnPostCancelClinicalEditAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        (await model.OnGetCancelClinicalEditAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");

        await _discard.Received(2).DiscardAllAsync(Rbse);
    }

    // ---------- Visit rows ----------

    [Fact]
    public async Task AddVisit_MissingRole_Forbids()
    {
        (await CreateModel(["DataEntry"]).OnPostAddVisitRowAsync(null)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddVisit_BlankDate_ShowsRowWithError()
    {
        var model = CreateModel();

        (await model.OnPostAddVisitRowAsync(null)).Should().BeOfType<PageResult>();

        model.ShowAddVisitRow.Should().BeTrue();
        model.ModelState["NewVisitDate"]!.Errors[0].ErrorMessage.Should().Be("Enter a visit date.");
    }

    [Fact]
    public async Task AddVisit_BinderErrorIsNotStackedWithBlankMessage()
    {
        var model = CreateModel();
        model.ModelState.AddModelError("NewVisitDate", "Please enter a valid date");

        await model.OnPostAddVisitRowAsync(null);

        model.ModelState["NewVisitDate"]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task AddVisit_BeforeBirth_IsRejected()
    {
        var model = CreateModel();
        model.NewVisitDate = new DateTime(2014, 1, 1);

        await model.OnPostAddVisitRowAsync(null);

        model.ModelState["NewVisitDate"]!.Errors[0].ErrorMessage.Should().Be("The visit date must be on or after the birth date.");
        model.NewVisitDate.Should().Be(new DateTime(2014, 1, 1));
    }

    [Fact]
    public async Task AddVisit_BeforeEpochWithoutBirthDate_IsRejected()
    {
        _caseRepo.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse });
        var model = CreateModel();
        model.NewVisitDate = new DateTime(1960, 1, 1);

        await model.OnPostAddVisitRowAsync(null);

        model.ModelState["NewVisitDate"]!.Errors[0].ErrorMessage.Should().Contain("1 January 1970");
    }

    [Fact]
    public async Task AddVisit_InFuture_IsRejected()
    {
        var model = CreateModel();
        model.NewVisitDate = Today.AddDays(3);

        await model.OnPostAddVisitRowAsync(null);

        model.ModelState["NewVisitDate"]!.Errors[0].ErrorMessage.Should().Be("The visit date must not be in the future.");
    }

    [Fact]
    public async Task AddVisit_DuplicateDate_IsRejected()
    {
        var model = CreateModel();
        model.NewVisitDate = new DateTime(2022, 3, 3);

        await model.OnPostAddVisitRowAsync(null);

        model.ModelState["NewVisitDate"]!.Errors[0].ErrorMessage.Should().Contain("must be unique");
    }

    [Fact]
    public async Task AddVisit_Valid_StagesVisitAndRedirects()
    {
        var model = CreateModel(null, "Kicking");
        model.NewVisitDate = new DateTime(2023, 4, 4);

        var result = await model.OnPostAddVisitRowAsync(Convert.ToBase64String(new byte[] { 1, 2 }));

        result.Should().BeOfType<RedirectToPageResult>();
        _draft!.Visits.Should().HaveCount(3);
        _draft.Visits.Should().Contain(v => v.Id == null && v.VisitDate == new DateTime(2023, 4, 4));
        _draft.HasClinicalVisitsChanges.Should().BeTrue();
        _draft.Clinical!.Kicking.Should().BeTrue();
    }

    [Fact]
    public async Task BeginEditVisit_MissingRole_Forbids()
    {
        (await CreateModel(["VLAAccess"]).OnPostBeginEditVisitRowAsync("k", null)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task BeginEditVisit_KnownKey_PopulatesEditState()
    {
        var model = CreateModel();
        await model.OnGetAsync();
        var key = _draft!.Visits.First(v => v.Id == 1).ClientKey;
        var editing = CreateModel();

        (await editing.OnPostBeginEditVisitRowAsync(key, null)).Should().BeOfType<PageResult>();

        editing.ReopenEditClientKey.Should().Be(key);
        editing.EditVisitDate.Should().Be(new DateTime(2022, 3, 3));
    }

    [Fact]
    public async Task BeginEditVisit_UnknownKey_ReturnsPageWithoutEditState()
    {
        var model = CreateModel();

        (await model.OnPostBeginEditVisitRowAsync("nope", null)).Should().BeOfType<PageResult>();

        model.ReopenEditClientKey.Should().BeNull();
    }

    [Fact]
    public async Task UpdateVisit_MissingRole_Forbids()
    {
        (await CreateModel(["DataEntry"]).OnPostUpdateVisitRowAsync(null)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateVisit_UnknownRow_SetsErrorAndRedirects()
    {
        var model = CreateModel();
        model.EditingClientKey = "nope";

        var result = await model.OnPostUpdateVisitRowAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["ErrorMessage"].Should().Be("The clinical visit row being edited no longer exists.");
    }

    private async Task<string> SeedAndGetKey(int visitId)
    {
        await CreateModel().OnGetAsync();
        return _draft!.Visits.First(v => v.Id == visitId).ClientKey;
    }

    [Fact]
    public async Task UpdateVisit_BlankDate_ReopensEditRow()
    {
        var key = await SeedAndGetKey(1);
        var model = CreateModel();
        model.EditingClientKey = key;

        (await model.OnPostUpdateVisitRowAsync(null)).Should().BeOfType<PageResult>();

        model.ReopenEditClientKey.Should().Be(key);
        model.ModelState["EditVisitDate"]!.Errors[0].ErrorMessage.Should().Be("Enter a visit date.");
    }

    [Fact]
    public async Task UpdateVisit_DuplicateOfAnotherRow_IsRejected()
    {
        var key = await SeedAndGetKey(1);
        var model = CreateModel();
        model.EditingClientKey = key;
        model.EditVisitDate = new DateTime(2022, 1, 1);

        await model.OnPostUpdateVisitRowAsync(null);

        model.ModelState["EditVisitDate"]!.Errors[0].ErrorMessage.Should().Contain("must be unique");
    }

    [Fact]
    public async Task UpdateVisit_Valid_UpdatesDraft()
    {
        var key = await SeedAndGetKey(1);
        var model = CreateModel();
        model.EditingClientKey = key;
        model.EditVisitDate = new DateTime(2022, 3, 3);

        (await model.OnPostUpdateVisitRowAsync(null)).Should().BeOfType<RedirectToPageResult>();

        _draft!.Visits.Single(v => v.ClientKey == key).VisitDate.Should().Be(new DateTime(2022, 3, 3));
        _draft.HasClinicalVisitsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteVisit_MissingRole_Forbids()
    {
        (await CreateModel(["DataEntry"]).OnPostDeleteVisitAsync("k", null)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteVisit_Known_RemovesFromDraft()
    {
        var key = await SeedAndGetKey(1);
        var model = CreateModel();

        (await model.OnPostDeleteVisitAsync(key, null)).Should().BeOfType<RedirectToPageResult>();

        _draft!.Visits.Should().NotContain(v => v.ClientKey == key);
        _draft.HasClinicalVisitsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteVisit_Unknown_LeavesDraftUntouched()
    {
        var model = CreateModel();

        (await model.OnPostDeleteVisitAsync("nope", null)).Should().BeOfType<RedirectToPageResult>();

        _draft!.Visits.Should().HaveCount(2);
        _draft.HasClinicalVisitsChanges.Should().BeFalse();
    }

    // Builds a staged EditCaseCommand carrying only a birth date.
    private sealed class ClinicalCaseStub
    {
        public EditCaseCommand Command(DateTime birthDate) =>
            new BSE.Host.Models.ViewModels.VlaEditViewModel { Rbse = Rbse, BirthDate = birthDate }.ToEditCommand([1]);
    }
}
