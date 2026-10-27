using System.Security.Claims;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.CaseWork.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using CaseWorkRow = BSE.Modules.CaseWork.Models.CaseWorkRecord;
using CaseWorkEntryRow = BSE.Modules.CaseWork.Models.CaseWorkEntryRecord;

namespace BSE.Host.Tests;

public sealed class CaseEditPageModelTests
{
    private const string Rbse = "002600001";
    private const string RowStampKey = "CaseEdit_RowStamp_" + Rbse;
    private static readonly string[] Defra = ["DataEntry"];
    private static readonly string[] VlaDataEntry = ["DataEntry", "VLAAccess"];
    private static readonly string[] VlaMaintenance = ["DataEntry", "VLAAccess", "VLAMaintenance"];

    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ICaseWorkRepository _caseWork = Substitute.For<ICaseWorkRepository>();
    private readonly ITestRepository _tests = Substitute.For<ITestRepository>();
    private readonly ICaseEditSessionStateService _sessionState = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly IBatchRepository _batch = Substitute.For<IBatchRepository>();
    private CaseEditSessionState? _draft;
    private CaseRecord? _record = Persisted();

    public CaseEditPageModelTests()
    {
        _caseService.GetCaseAsync(Rbse).Returns(_ => _record);
        _user.GetUserIdAsync().Returns(9);
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "X", "Ex")]);
        _lookups.GetLookupAsync(LookupTableId.TestType).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "T1", "Type One")]);
        _lookups.GetLookupAsync(LookupTableId.TestResult).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "P", "Positive")]);
        _batch.GetBatchNumbersByRbseAsync(Rbse).Returns((IReadOnlyList<BatchNumberEntry>)[new BatchNumberEntry(1, "B1", Rbse, "BSE1")]);
        _tests.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseTestRecord>)[]);
        _sessionState.GetAsync(Arg.Any<string>()).Returns(_ => _draft);
        _sessionState.SetAsync(Arg.Any<CaseEditSessionState>()).Returns(ci =>
        {
            _draft = ci.Arg<CaseEditSessionState>();
            return Task.CompletedTask;
        });
    }

    private static CaseRecord Persisted() => new()
    {
        Rbse = Rbse, Cphh = "01001000101", FormADate = new DateTime(2020, 1, 1), RowStamp = [1, 2], IsNonGbCase = false
    };

    private static CaseWorkRow OpenCaseWork(bool closed = false) => new()
    {
        Rbse = Rbse, RbseDate = new DateTime(2019, 1, 1), Barcode = "BC-DB", AhfReference = "AHF-DB", IsCaseClosed = closed
    };

    private EditModel CreateModel(string[]? roles = null, bool withRowStamp = true)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build();
        var model = new EditModel(_caseService, _user, _lookups, _caseWork, _tests, _sessionState, _discard, _orchestration, _batch, config)
        { Rbse = Rbse };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity((roles ?? []).Select(r => new Claim(ClaimTypes.Role, r)), "Test"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        if (withRowStamp)
            model.TempData[RowStampKey] = Convert.ToBase64String(new byte[] { 1, 2 });
        model.Case.Rbse = Rbse;
        return model;
    }

    // ── GET ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var model = CreateModel();
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_NewCase_ForcesCaseWorkAndDefaultsRbseDate()
    {
        _record = null;
        var model = CreateModel(Defra, withRowStamp: false);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Case.HasCaseWork.Should().BeTrue();
        model.Case.RbseDate.Should().Be(DateTime.Today);
        model.HasCaseWorkLink.Should().BeTrue();
        model.BatchNumbers.Should().ContainSingle();
        model.SpolSiteUrl.Should().Be("http://spol");
        model.TempData[RowStampKey].Should().Be("");
        model.CanEditCaseFields.Should().BeTrue();
        model.CanEditDefraNotes.Should().BeTrue();
        model.CanEditVlaMaintenanceCaseworkFields.Should().BeFalse();
        model.FateOptions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnGet_ExistingCaseWithCaseWork_LoadsRecordAndStagedOverlay()
    {
        _caseWork.GetByRbseAsync(Rbse).Returns(OpenCaseWork());
        _draft = new CaseEditSessionState
        {
            Rbse = Rbse,
            Case = BSE.Host.Models.ViewModels.CaseEditViewModel.FromRecord(Persisted()).ToEditCommand([1]) with { Notes = "staged note" },
            CaseWork = new BSE.Modules.CaseWork.Commands.EditCaseWorkCommand(Rbse, new DateTime(2019, 1, 1), "BC-STAGED", "AHF", null, null, null, null, null, null)
        };
        var model = CreateModel(VlaMaintenance, withRowStamp: false);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Case.FormADate.Should().Be(new DateTime(2020, 1, 1));
        model.Case.Notes.Should().Be("staged note");
        model.Case.Barcode.Should().Be("BC-STAGED");
        model.HasCaseWorkLink.Should().BeTrue();
        model.CanEditVlaMaintenanceCaseworkFields.Should().BeTrue();
        model.CanEditPaperworkCompleteDate.Should().BeTrue();
        model.CanEditDefraNotes.Should().BeFalse();
        model.TempData[RowStampKey].Should().Be(Convert.ToBase64String(new byte[] { 1, 2 }));
    }

    [Fact]
    public async Task OnGet_ClosedCaseWork_DisablesVlaMaintenanceCaseworkFields()
    {
        _caseWork.GetByRbseAsync(Rbse).Returns(OpenCaseWork(closed: true));
        var model = CreateModel(VlaMaintenance);

        await model.OnGetAsync();

        model.CanEditVlaMaintenanceCaseworkFields.Should().BeFalse();
        model.CanEditCaseFields.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_VlaDataEntry_IsReadOnly()
    {
        var model = CreateModel(VlaDataEntry);

        await model.OnGetAsync();

        model.CanEditCaseFields.Should().BeFalse();
        model.CanEditDefraNotes.Should().BeFalse();
        model.CanEditPaperworkCompleteDate.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public async Task OnGet_HasCaseWorkLink_FallsBackToEntryRecord(bool hasCaseWork, bool hasEntry, bool expected)
    {
        _caseWork.GetByRbseAsync(Rbse).Returns(hasCaseWork ? OpenCaseWork() : null);
        _caseWork.GetEntryByRbseAsync(Rbse).Returns(hasEntry ? new CaseWorkEntryRow { Rbse = Rbse } : null);
        var model = CreateModel(Defra);

        await model.OnGetAsync();

        model.HasCaseWorkLink.Should().Be(expected);
    }

    [Fact]
    public async Task OnGet_NonGbCase_SetsFlagAndInitialisesTestsDraft()
    {
        _record = Persisted() with { IsNonGbCase = true };
        _tests.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseTestRecord>)
        [
            new CaseTestRecord(2, Rbse, "T1", "Type One", "P", "Positive", [1]),
            new CaseTestRecord(1, Rbse, "T1", "Type One", "P", "Positive", null),
        ]);
        var model = CreateModel(Defra);

        await model.OnGetAsync();

        model.IsNonGbCase.Should().BeTrue();
        model.StagedTests.Select(t => t.Id).Should().Equal(1, 2);
        model.Tests.Should().HaveCount(2);
        model.TestsTotalCount.Should().Be(2);
        model.HasUnsavedChanges.Should().BeFalse();
        _draft!.Tests.Should().HaveCount(2);
    }

    [Fact]
    public async Task OnGetCancel_DiscardsDraftAndRedirectsHome()
    {
        var result = await CreateModel().OnGetCancelEditAsync();

        await _discard.Received(1).DiscardAllAsync(Rbse);
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public void OnGetValidateEartag_ReturnsJsonWithErrorProperty()
    {
        var result = CreateModel().OnGetValidateEartag("UK", "12", "x");

        result.Should().BeOfType<JsonResult>().Which.Value.Should().NotBeNull();
    }

    [Fact]
    public void SortAndPageUrls_ToggleDirectionAndEscapeRbse()
    {
        var model = CreateModel();
        model.TSort = "type";
        model.TDir = "asc";

        model.TestsSortUrl("type").Should().Contain("TDir=desc").And.Contain("TSort=type").And.EndWith("#test-records");
        model.TestsSortUrl("result").Should().Contain("TDir=asc");
        model.TestsPageUrl(3).Should().Contain("TPage=3").And.Contain("TSort=type");
    }

    // ── Save ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Save_WithoutDataEntry_Forbids()
    {
        (await CreateModel().OnPostAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SaveAndGotoCasework_WithoutDataEntry_RedirectsToCaseworkEntry()
    {
        var result = await CreateModel().OnPostSaveAndGotoCaseworkAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/CaseWork/Entry");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
    }

    [Fact]
    public async Task Save_VlaDataEntry_IsNoOpRedirectHome()
    {
        var result = await CreateModel(VlaDataEntry).OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task SaveAndGotoCasework_VlaDataEntry_RedirectsToCasework()
    {
        var result = await CreateModel(VlaDataEntry).OnPostSaveAndGotoCaseworkAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/Entry");
    }

    [Fact]
    public async Task Save_Valid_StagesCaseAndCaseWorkThenCommitsAndRedirectsHome()
    {
        _caseWork.GetByRbseAsync(Rbse).Returns(OpenCaseWork());
        _orchestration.CommitAllAsync(Rbse, 9).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(Defra);
        model.Case.HasCaseWork = true;
        model.Case.RbseDate = new DateTime(2019, 1, 1);
        model.Case.FormADate = new DateTime(2020, 1, 1);
        model.Case.FormBDate = new DateTime(2020, 6, 1);
        model.Case.Fate = "F";
        model.Case.PurchaserBse1ReceivedDate = new DateTime(2020, 2, 1);
        model.Case.Barcode = "FORGED";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _orchestration.Received(1).CommitAllAsync(Rbse, 9);
        _draft!.HasPendingChanges.Should().BeTrue();
        _draft.Case!.RowStamp.Should().Equal(1, 2);
        _draft.Case.SlaughterDate.Should().Be(new DateTime(2020, 6, 1));
        _draft.Case.IsPurchaserBse1Received.Should().BeTrue();
        _draft.CaseWork!.Barcode.Should().Be("BC-DB");
        _draft.CaseWork.AhfReference.Should().Be("AHF-DB");
    }

    [Fact]
    public async Task Save_VlaMaintenanceOnOpenCaseWork_KeepsPostedBarcode()
    {
        _caseWork.GetByRbseAsync(Rbse).Returns(OpenCaseWork());
        _orchestration.CommitAllAsync(Rbse, 9).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(VlaMaintenance);
        model.Case.HasCaseWork = true;
        model.Case.RbseDate = new DateTime(2019, 1, 1);
        model.Case.FormADate = new DateTime(2020, 1, 1);
        model.Case.Barcode = "NEW-BC";

        await model.OnPostAsync();

        _draft!.CaseWork!.Barcode.Should().Be("NEW-BC");
    }

    [Fact]
    public async Task Save_NewCaseWithoutCaseWorkRow_DefaultsRbseDateAndStagesWork()
    {
        _record = null;
        _orchestration.CommitAllAsync(Rbse, 9).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(Defra);
        model.Case.HasCaseWork = true;

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _draft!.CaseWork!.RbseDate.Should().Be(DateTime.Today);
        _draft.CaseWork.Barcode.Should().BeNull();
    }

    [Fact]
    public async Task Save_NonGbCase_KeepsPersistedFormADate()
    {
        _record = Persisted() with { IsNonGbCase = true };
        _orchestration.CommitAllAsync(Rbse, 9).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(Defra);
        model.Case.FormADate = new DateTime(2019, 5, 5);

        await model.OnPostAsync();

        _draft!.Case!.FormADate.Should().Be(new DateTime(2020, 1, 1));
    }

    [Fact]
    public async Task Save_CaseWorkVariant_RedirectsToCaseworkOnSuccess()
    {
        _orchestration.CommitAllAsync(Rbse, 9).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(Defra);
        model.Case.FormADate = new DateTime(2020, 1, 1);

        var result = await model.OnPostSaveAndGotoCaseworkAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/CaseWork/Entry");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
    }

    [Fact]
    public async Task Save_MissingRowStampInTempData_ReportsConcurrencyError()
    {
        var model = CreateModel(Defra, withRowStamp: false);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Contain("Session expired");
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task Save_MandatoryFieldsMissing_RedirectsToSaveResult()
    {
        _orchestration.CommitAllAsync(Rbse, 9).Returns<Task<CaseCommitOutcome>>(_ => throw new MandatoryCaseFieldsMissingException(["Owner"]));
        var model = CreateModel(Defra);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
        model.TempData[SaveResultModel.ModeKey].Should().Be(nameof(SaveResultMode.MissingMandatoryFields));
    }

    [Theory]
    [InlineData(EditCaseResult.RbseNotFound, "not found")]
    [InlineData(EditCaseResult.AuditLogError, "Audit log error")]
    [InlineData(EditCaseResult.PostUpdateError, "Database error")]
    [InlineData(EditCaseResult.ConcurrencyConflict, "Update failed")]
    public async Task Save_FailedOutcome_RedirectsHomeWithMappedMessage(EditCaseResult failure, string expected)
    {
        _orchestration.CommitAllAsync(Rbse, 9).Returns(CaseCommitOutcome.Failure(failure));
        var model = CreateModel(Defra);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        ((string)model.TempData["ErrorMessage"]!).Should().Contain(expected);
    }

    [Fact]
    public async Task Save_Warnings_RedirectsToSaveResultPartialSuccess()
    {
        _orchestration.CommitAllAsync(Rbse, 9).Returns(CaseCommitOutcome.Success(["w"]));
        var model = CreateModel(Defra);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
        model.TempData[SaveResultModel.ModeKey].Should().Be(nameof(SaveResultMode.PartialSuccess));
    }

    [Fact]
    public async Task Save_InvalidEartag_ReturnsPageWithEartagError()
    {
        var model = CreateModel(Defra);
        model.Case.EartagCountry = "UK";
        model.Case.EartagHerdmark = "1";
        model.Case.Eartag = "bad";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("Case.EartagCountry");
    }

    private static readonly DateTime Today = DateTime.Today;

    public static TheoryData<string, string> InvalidCases => new()
    {
        { "FutureBse1", "Case.Bse1ReceivedDate" },
        { "FutureFormA", "Case.FormADate" },
        { "FormAAfterSlaughter", "Case.FormADate" },
        { "ResubmittedWithoutFormA", "Case.FormAResubmittedDate" },
        { "ResubmittedBeforeFormA", "Case.FormAResubmittedDate" },
        { "FormBWithoutFormA", "Case.FormBDate" },
        { "FormBBeforeFormA", "Case.FormBDate" },
        { "FormCWithoutFormB", "Case.FormCDate" },
        { "FormBWithoutFate", "Case.Fate" },
        { "BirthBeforeEpoch", "Case.BirthDate" },
        { "BirthAfterFormA", "Case.BirthDate" },
        { "BirthAfterPurchase", "Case.BirthDate" },
        { "BirthAfterOnset", "Case.BirthDate" },
        { "CaseWorkDateTooEarly", "Case.PurchaserBse1ReceivedDate" },
        { "CaseWorkDateFuture", "Case.PaperworkCompleteDate" },
    };

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public async Task Save_InvalidDates_ReturnPageWithModelError(string scenario, string key)
    {
        var model = CreateModel(Defra);
        var c = model.Case;
        c.FormADate = new DateTime(2020, 1, 1);
        switch (scenario)
        {
            case "FutureBse1": c.Bse1ReceivedDate = Today.AddDays(2); break;
            case "FutureFormA": c.FormADate = Today.AddDays(2); break;
            case "FormAAfterSlaughter": c.SlaughterDate = new DateTime(2019, 1, 1); break;
            case "ResubmittedWithoutFormA": c.FormADate = null; c.FormAResubmittedDate = new DateTime(2020, 1, 1); break;
            case "ResubmittedBeforeFormA": c.FormAResubmittedDate = new DateTime(2019, 1, 1); break;
            case "FormBWithoutFormA": c.FormADate = null; c.FormBDate = new DateTime(2020, 1, 1); c.Fate = "F"; break;
            case "FormBBeforeFormA": c.FormBDate = new DateTime(2019, 1, 1); c.Fate = "F"; break;
            case "FormCWithoutFormB": c.FormCDate = new DateTime(2020, 3, 1); break;
            case "FormBWithoutFate": c.FormBDate = new DateTime(2020, 3, 1); break;
            case "BirthBeforeEpoch": c.BirthDate = new DateTime(1960, 1, 1); break;
            case "BirthAfterFormA": c.BirthDate = new DateTime(2021, 1, 1); break;
            case "BirthAfterPurchase": c.BirthDate = new DateTime(2015, 1, 1); c.PurchaseDate = new DateTime(2014, 1, 1); break;
            case "BirthAfterOnset": c.BirthDate = new DateTime(2015, 1, 1); c.OnsetDate = new DateTime(2014, 1, 1); break;
            case "CaseWorkDateTooEarly": c.HasCaseWork = true; c.RbseDate = new DateTime(2019, 1, 1); c.PurchaserBse1ReceivedDate = new DateTime(2019, 1, 1); break;
            case "CaseWorkDateFuture": c.HasCaseWork = true; c.RbseDate = new DateTime(2019, 1, 1); c.PaperworkCompleteDate = Today.AddDays(3); break;
        }

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(key);
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task Save_BirthDateCleared_ResetsSourceAndEstimate()
    {
        _orchestration.CommitAllAsync(Rbse, 9).Returns(CaseCommitOutcome.Success([]));
        var model = CreateModel(Defra);
        model.Case.BirthDateSource = "X";
        model.Case.IsBirthDateEst = true;

        await model.OnPostAsync();

        _draft!.Case!.BirthDateSource.Should().BeNull();
        _draft.Case.IsBirthDateEst.Should().NotBe(true);
    }

    // ── Tab navigation ───────────────────────────────────────────────────

    [Fact]
    public async Task StageAndGoto_ViewerWithoutDataEntry_RedirectsWithoutStaging()
    {
        var result = await CreateModel().OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _sessionState.DidNotReceive().SetAsync(Arg.Any<CaseEditSessionState>());
    }

    [Fact]
    public async Task StageAndGoto_VlaDataEntry_RedirectsWithoutStaging()
    {
        var result = await CreateModel(VlaDataEntry).OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _sessionState.DidNotReceive().SetAsync(Arg.Any<CaseEditSessionState>());
    }

    [Fact]
    public async Task StageAndGoto_UnchangedValues_StagesWithoutMarkingPending()
    {
        var model = CreateModel(Defra);
        model.Case.FormADate = new DateTime(2020, 1, 1);

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _draft!.Case.Should().NotBeNull();
        _draft.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task StageAndGoto_ChangedValues_MarksPending()
    {
        var model = CreateModel(Defra);
        model.Case.FormADate = new DateTime(2020, 1, 1);
        model.Case.Notes = "changed";

        await model.OnPostStageAndGotoAsync("/Case/Farm");

        _draft!.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task StageAndGoto_InvalidDates_ReturnsPage()
    {
        var model = CreateModel(Defra);
        model.Case.FormADate = Today.AddDays(5);

        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task StageAndGoto_EartagNotValidatedOnTabSwitch()
    {
        var model = CreateModel(Defra);
        model.Case.FormADate = new DateTime(2020, 1, 1);
        model.Case.EartagCountry = "UK";
        model.Case.Eartag = "bad";

        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task StageAndGoto_MissingRowStamp_ReportsConcurrencyError()
    {
        var model = CreateModel(Defra, withRowStamp: false);

        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().NotBeNull();
    }

    [Fact]
    public async Task StageAndGoToAuditLog_RedirectsWithReturnTo()
    {
        var model = CreateModel(Defra);
        model.Case.FormADate = new DateTime(2020, 1, 1);

        var result = await model.OnPostStageAndGoToAuditLogAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/AuditLog/ByCase");
        redirect.RouteValues!["returnTo"].Should().Be("/Case/Edit");
    }

    [Fact]
    public async Task StageAndGoToAuditLog_InvalidDates_ReturnsPage()
    {
        var model = CreateModel(Defra);
        model.Case.FormADate = Today.AddDays(5);

        (await model.OnPostStageAndGoToAuditLogAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task ResortTests_ValidStaging_RendersPage()
    {
        var model = CreateModel(Defra);
        model.Case.FormADate = new DateTime(2020, 1, 1);

        (await model.OnPostResortTestsAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task ResortTests_WithoutRole_RendersPage()
    {
        (await CreateModel().OnPostResortTestsAsync()).Should().BeOfType<PageResult>();
    }

    // ── Tests grid ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(nameof(EditModel.OnPostAddTestRowAsync), null)]
    [InlineData(nameof(EditModel.OnPostUpdateTestRowAsync), null)]
    [InlineData(nameof(EditModel.OnPostBeginEditTestRowAsync), 1)]
    public async Task TestGridHandlers_WithoutDataEntry_Forbid(string handler, int? arg)
    {
        var model = CreateModel();
        var method = typeof(EditModel).GetMethod(handler)!;

        var result = await (Task<IActionResult>)method.Invoke(model, arg is null ? null : [arg.Value])!;

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteTest_WithoutDataEntry_Forbids()
    {
        (await CreateModel().OnPostDeleteTestAsync(1, null)).Should().BeOfType<ForbidResult>();
    }

    [Theory]
    [InlineData(nameof(EditModel.OnPostAddTestRowAsync), null)]
    [InlineData(nameof(EditModel.OnPostUpdateTestRowAsync), null)]
    [InlineData(nameof(EditModel.OnPostBeginEditTestRowAsync), 1)]
    public async Task TestGridHandlers_ForVlaDataEntry_Forbid(string handler, int? arg)
    {
        var model = CreateModel(VlaDataEntry);
        var method = typeof(EditModel).GetMethod(handler)!;

        var result = await (Task<IActionResult>)method.Invoke(model, arg is null ? null : [arg.Value])!;

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteTest_ForVlaDataEntry_Forbids()
    {
        (await CreateModel(VlaDataEntry).OnPostDeleteTestAsync(1, null)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddTestRow_MissingTypeAndResult_ShowsRowWithErrors()
    {
        var model = CreateModel(Defra);

        var result = await model.OnPostAddTestRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ShowAddTestRow.Should().BeTrue();
        model.ModelState.Should().ContainKeys(nameof(EditModel.NewTestType), nameof(EditModel.NewTestResult));
    }

    [Fact]
    public async Task AddTestRow_Valid_StagesRowsWithDescendingTemporaryIds()
    {
        var model = CreateModel(Defra);
        model.NewTestType = "T1";
        model.NewTestResult = "P";

        var first = await model.OnPostAddTestRowAsync();
        var second = await CreateModel(Defra).Tap(m => { m.NewTestType = "T1"; m.NewTestResult = "P"; }).OnPostAddTestRowAsync();

        first.Should().BeOfType<RedirectToPageResult>().Which.Fragment.Should().Be("test-records");
        second.Should().BeOfType<RedirectToPageResult>();
        _draft!.Tests.Select(t => t.Id).Should().Equal(-1, -2);
        _draft.Tests[0].TestTypeDescription.Should().Be("Type One");
        _draft.Tests[0].TestResultDescription.Should().Be("Positive");
        _draft.HasCaseEditChanges.Should().BeTrue();
    }

    [Fact]
    public async Task BeginEditTestRow_PopulatesEditFieldsOrRedirectsWhenMissing()
    {
        _tests.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseTestRecord>)[new CaseTestRecord(5, Rbse, "T1", "Type One", "P", "Positive", [4])]);

        var found = CreateModel(Defra);
        var foundResult = await found.OnPostBeginEditTestRowAsync(5);
        var missingResult = await CreateModel(Defra).OnPostBeginEditTestRowAsync(77);

        foundResult.Should().BeOfType<PageResult>();
        found.ReopenEditTestId.Should().Be(5);
        found.EditTestType.Should().Be("T1");
        found.EditTestResult.Should().Be("P");
        found.EditTestRowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 4 }));
        missingResult.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task UpdateTestRow_MissingFields_ReopensEditor()
    {
        var model = CreateModel(Defra);
        model.EditingTestId = 5;

        var result = await model.OnPostUpdateTestRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ReopenEditTestId.Should().Be(5);
        model.ModelState.Should().ContainKeys(nameof(EditModel.EditTestType), nameof(EditModel.EditTestResult));
    }

    [Fact]
    public async Task UpdateTestRow_ExistingAndUnknownIds()
    {
        _tests.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseTestRecord>)[new CaseTestRecord(5, Rbse, "T1", "Type One", "P", "Positive", [4])]);

        var model = CreateModel(Defra);
        model.EditingTestId = 5;
        model.EditTestType = "T1";
        model.EditTestResult = "Q";
        var ok = await model.OnPostUpdateTestRowAsync();

        ok.Should().BeOfType<RedirectToPageResult>();
        _draft!.Tests.Single().TestResult.Should().Be("Q");
        _draft.HasCaseEditChanges.Should().BeTrue();

        var unknown = CreateModel(Defra);
        unknown.EditingTestId = 99;
        unknown.EditTestType = "T1";
        unknown.EditTestResult = "Q";
        (await unknown.OnPostUpdateTestRowAsync()).Should().BeOfType<RedirectToPageResult>();
        _draft.Tests.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteTest_RemovesStagedRowOnlyWhenPresent()
    {
        _tests.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseTestRecord>)[new CaseTestRecord(5, Rbse, "T1", "Type One", "P", "Positive", [4])]);

        var none = await CreateModel(Defra).OnPostDeleteTestAsync(99, null);
        none.Should().BeOfType<RedirectToPageResult>();
        _draft!.HasCaseEditChanges.Should().BeFalse();

        var removed = await CreateModel(Defra).OnPostDeleteTestAsync(5, null);

        removed.Should().BeOfType<RedirectToPageResult>();
        _draft.Tests.Should().BeEmpty();
        _draft.HasCaseEditChanges.Should().BeTrue();
    }

    [Theory]
    [InlineData("type", "asc", "Type A", "Type B")]
    [InlineData("type", "desc", "Type B", "Type A")]
    [InlineData("result", "asc", "Type A", "Type B")]
    [InlineData("result", "desc", "Type B", "Type A")]
    [InlineData("", "asc", "Type B", "Type A")]
    public async Task OnGet_SortsTestsGrid(string sort, string dir, string first, string second)
    {
        _tests.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseTestRecord>)
        [
            new CaseTestRecord(1, Rbse, "T2", "Type B", "P", "Result B", [1]),
            new CaseTestRecord(2, Rbse, "T1", "Type A", "P", "Result A", [1]),
        ]);
        var model = CreateModel(Defra);
        model.TSort = sort;
        model.TDir = dir;

        await model.OnGetAsync();

        model.Tests.Select(t => t.TestTypeDescription).Should().Equal(first, second);
    }

    [Fact]
    public async Task OnGet_ManyTests_PagesAndClampsPage()
    {
        _tests.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseTestRecord>)
            Enumerable.Range(1, 23).Select(i => new CaseTestRecord(i, Rbse, "T1", "Type One", "P", "Positive", [1])).ToList());
        var model = CreateModel(Defra);
        model.TPage = 50;

        await model.OnGetAsync();

        model.TestsTotalPages.Should().Be(3);
        model.TPage.Should().Be(3);
        model.Tests.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddTestRow_ForNewCase_StillLoadsLookupsAndStages()
    {
        _record = null;
        var model = CreateModel(Defra);
        model.NewTestType = "T1";
        model.NewTestResult = "P";

        var result = await model.OnPostAddTestRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.Case.HasCaseWork.Should().BeTrue();
        _draft!.Tests.Should().ContainSingle();
    }
}

internal static class ModelTapExtensions
{
    public static T Tap<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}
