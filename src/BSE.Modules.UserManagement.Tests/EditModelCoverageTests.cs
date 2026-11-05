using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using CaseWorkRecord = BSE.Modules.CaseWork.Models.CaseWorkRecord;

namespace BSE.Modules.UserManagement.Tests;

public sealed class EditModelCoverageTests
{
    private const string Rbse = "002600001";
    private static readonly string RowStampKey = $"CaseEdit_RowStamp_{Rbse}";
    private static readonly string[] Defra = ["DataEntry"];
    private static readonly string[] VlaDataEntry = ["DataEntry", "VLAAccess"];

    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ICaseWorkRepository _caseWorkRepository = Substitute.For<ICaseWorkRepository>();
    private readonly ITestRepository _testRepository = Substitute.For<ITestRepository>();
    private readonly InMemorySessionState _sessionState = new();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();

    public EditModelCoverageTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(new[] { new LookupItem(1, "T1", "Type one"), new LookupItem(2, "P", "Positive") });
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _caseWorkRepository.GetByRbseAsync(Arg.Any<string>()).Returns((CaseWorkRecord?)null);
        _caseWorkRepository.GetEntryByRbseAsync(Arg.Any<string>()).Returns((CaseWorkEntryRecord?)null);
        _testRepository.GetByRbseAsync(Rbse).Returns(new[]
        {
            new CaseTestRecord(1, Rbse, "T1", "Type one", "P", "Positive", [1]),
            new CaseTestRecord(2, Rbse, "T2", "Type two", "N", "Negative", [2]),
        });
        _caseService.GetCaseAsync(Rbse).Returns(ExistingCase());
    }

    private static CaseRecord ExistingCase(bool nonGb = false) => new()
    {
        Rbse = Rbse, Cphh = "01001000701", FormADate = new DateTime(2024, 1, 1), BirthDate = new DateTime(2000, 1, 1),
        EartagCountry = "UK", EartagHerdmark = "123456", Eartag = "789012", RowStamp = [1, 2, 3], IsNonGbCase = nonGb
    };

    private EditModel Create(params string[] roles)
    {
        var model = new EditModel(_caseService, _currentUser, _lookups, _caseWorkRepository, _testRepository,
            _sessionState, _discard, _orchestration, _batchRepository,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build())
        {
            Rbse = Rbse
        };
        PageTestHelpers.Attach(model, roles);
        model.TempData[RowStampKey] = Convert.ToBase64String([1, 2, 3]);
        return model;
    }

    private static void FillValidCase(EditModel model)
    {
        var c = model.Case;
        c.Rbse = Rbse;
        c.EartagCountry = "UK";
        c.EartagHerdmark = "123456";
        c.Eartag = "789012";
        c.FormADate = new DateTime(2024, 1, 1);
        c.BirthDate = new DateTime(2000, 1, 1);
    }

    // ── OnGetAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_WithoutRbse_RedirectsToSessionError()
    {
        var model = Create(Defra);
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGetAsync_ForANewCase_StartsWithACaseWorkRow()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        var model = Create(Defra);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.HasCaseWorkLink.Should().BeTrue();
        model.Case.HasCaseWork.Should().BeTrue();
        model.Case.RbseDate.Should().Be(DateTime.Today);
        model.CanEditCaseFields.Should().BeTrue();
        model.CanEditDefraNotes.Should().BeTrue();
    }

    [Fact]
    public async Task OnGetAsync_ForAnExistingCase_AppliesStagedOverlaysAndPaginatesTests()
    {
        _caseWorkRepository.GetByRbseAsync(Rbse).Returns(new CaseWorkRecord { Rbse = Rbse, Barcode = "BC", IsCaseClosed = false });
        _sessionState.Current = new CaseEditSessionState
        {
            Rbse = Rbse,
            CaseWork = new BSE.Modules.CaseWork.Commands.EditCaseWorkCommand(Rbse, new DateTime(2025, 1, 1), "STAGED", "AHF", null, null, null, null, null, null)
        };
        var model = Create(Defra);
        model.TSort = "type";
        model.TDir = "desc";

        await model.OnGetAsync();

        model.Case.Barcode.Should().Be("STAGED");
        model.CanEditPaperworkCompleteDate.Should().BeTrue();
        model.Tests.Should().HaveCount(2);
        model.Tests[0].TestTypeDescription.Should().Be("Type two");
        model.TestsTotalCount.Should().Be(2);
    }

    [Theory]
    [InlineData("result", "asc")]
    [InlineData("result", "desc")]
    [InlineData("type", "asc")]
    [InlineData("", "asc")]
    public async Task OnGetAsync_SortsTheTestsGrid(string sort, string dir)
    {
        var model = Create(Defra);
        model.TSort = sort;
        model.TDir = dir;

        await model.OnGetAsync();

        model.Tests.Should().HaveCount(2);
    }

    [Fact]
    public void SortAndPageUrls_CarryTheCurrentState()
    {
        var model = Create(Defra);
        model.TSort = "type";
        model.TDir = "asc";

        model.TestsSortUrl("type").Should().Contain("TDir=desc").And.Contain("#test-records");
        model.TestsSortUrl("result").Should().Contain("TDir=asc");
        model.TestsPageUrl(3).Should().Contain("TPage=3").And.Contain("TSort=type");
    }

    [Fact]
    public async Task OnGetCancelEditAsync_DiscardsTheDraftAndGoesHome()
    {
        var result = await Create(Defra).OnGetCancelEditAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _discard.Received(1).DiscardAllAsync(Rbse);
    }

    [Fact]
    public void OnGetValidateEartag_ReturnsJson() =>
        Create(Defra).OnGetValidateEartag("UK", "123456", "789012").Should().BeOfType<JsonResult>();

    // ── Tests grid ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task TestGridHandlers_AreForbiddenWithoutWriteAccess()
    {
        var viewer = Create();
        (await viewer.OnPostAddTestRowAsync()).Should().BeOfType<ForbidResult>();
        (await viewer.OnPostBeginEditTestRowAsync(1)).Should().BeOfType<ForbidResult>();
        (await viewer.OnPostUpdateTestRowAsync()).Should().BeOfType<ForbidResult>();
        (await viewer.OnPostDeleteTestAsync(1, null)).Should().BeOfType<ForbidResult>();

        var vlaDataEntry = Create(VlaDataEntry);
        (await vlaDataEntry.OnPostAddTestRowAsync()).Should().BeOfType<ForbidResult>();
        (await vlaDataEntry.OnPostBeginEditTestRowAsync(1)).Should().BeOfType<ForbidResult>();
        (await vlaDataEntry.OnPostUpdateTestRowAsync()).Should().BeOfType<ForbidResult>();
        (await vlaDataEntry.OnPostDeleteTestAsync(1, null)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostAddTestRowAsync_RequiresATypeAndResult()
    {
        var model = Create(Defra);
        FillValidCase(model);

        var result = await model.OnPostAddTestRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ShowAddTestRow.Should().BeTrue();
        PageTestHelpers.ErrorMessages(model, "NewTestType").Should().NotBeEmpty();
        PageTestHelpers.ErrorMessages(model, "NewTestResult").Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnPostAddTestRowAsync_StagesANewRowWithATemporaryId()
    {
        var model = Create(Defra);
        FillValidCase(model);
        model.NewTestType = "T1";
        model.NewTestResult = "P";

        var result = await model.OnPostAddTestRowAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.Fragment.Should().Be("test-records");
        _sessionState.Current!.Tests.Should().HaveCount(3);
        _sessionState.Current.Tests.Last().Id.Should().BeNegative();
        _sessionState.Current.HasCaseEditChanges.Should().BeTrue();
        _sessionState.Current.Case.Should().NotBeNull();
    }

    [Fact]
    public async Task BeginEditTestRow_ReopensTheRowOrFallsBackToTheAnchor()
    {
        var found = Create(Defra);
        FillValidCase(found);
        (await found.OnPostBeginEditTestRowAsync(1)).Should().BeOfType<PageResult>();
        found.ReopenEditTestId.Should().Be(1);
        found.EditTestType.Should().Be("T1");

        var missing = Create(Defra);
        FillValidCase(missing);
        (await missing.OnPostBeginEditTestRowAsync(999)).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnPostUpdateTestRowAsync_ValidatesThenUpdatesTheRow()
    {
        var invalid = Create(Defra);
        FillValidCase(invalid);
        invalid.EditingTestId = 1;
        (await invalid.OnPostUpdateTestRowAsync()).Should().BeOfType<PageResult>();
        invalid.ReopenEditTestId.Should().Be(1);

        var ok = Create(Defra);
        FillValidCase(ok);
        ok.EditingTestId = 1;
        ok.EditTestType = "T1";
        ok.EditTestResult = "N";
        (await ok.OnPostUpdateTestRowAsync()).Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.Tests.First(t => t.Id == 1).TestResult.Should().Be("N");

        var missing = Create(Defra);
        FillValidCase(missing);
        missing.EditingTestId = 999;
        missing.EditTestType = "T1";
        missing.EditTestResult = "N";
        (await missing.OnPostUpdateTestRowAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnPostDeleteTestAsync_RemovesTheStagedRow()
    {
        var model = Create(Defra);
        FillValidCase(model);

        var result = await model.OnPostDeleteTestAsync(1, null);

        result.Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.Tests.Should().ContainSingle(t => t.Id == 2);
    }

    [Fact]
    public async Task TestGridHandlers_WorkForANewCaseToo()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        var model = Create(Defra);
        FillValidCase(model);
        model.NewTestType = "T1";
        model.NewTestResult = "P";

        (await model.OnPostAddTestRowAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    // ── Staging on navigation ───────────────────────────────────────────────────

    [Fact]
    public async Task StageAndGoto_StagesTheEditAndRedirects()
    {
        var model = Create(Defra);
        FillValidCase(model);
        model.Case.EartagCountry = "UK";

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _sessionState.Current!.Case.Should().NotBeNull();
    }

    [Fact]
    public async Task StageAndGoToAuditLog_StagesTheEditAndRedirectsToTheAuditLog()
    {
        var model = Create(Defra);
        FillValidCase(model);

        var result = await model.OnPostStageAndGoToAuditLogAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/AuditLog/ByCase");
    }

    [Fact]
    public async Task StageAndGoto_SkipsStagingForReadOnlyUsers()
    {
        var viewer = Create();
        (await viewer.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<RedirectToPageResult>();

        var vlaDataEntry = Create(VlaDataEntry);
        (await vlaDataEntry.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<RedirectToPageResult>();

        _sessionState.Current.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_WithoutARowStamp_ReportsASessionError()
    {
        var model = Create(Defra);
        FillValidCase(model);
        model.TempData.Remove(RowStampKey);

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm");

        result.Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Contain("Session expired");
    }

    [Fact]
    public async Task StageAndGoto_ForANonGbCase_KeepsThePersistedFormADate()
    {
        var record = ExistingCase(true);
        _caseService.GetCaseAsync(Rbse).Returns(record);
        var model = Create(Defra);
        FillValidCase(model);
        model.Case.FormADate = null;

        await model.OnPostStageAndGotoAsync("/Case/Farm");

        model.Case.FormADate.Should().Be(record.FormADate);
        model.IsNonGbCase.Should().BeTrue();
    }

    [Fact]
    public async Task ResortTests_StagesAndReturnsThePage()
    {
        var model = Create(Defra);
        FillValidCase(model);

        (await model.OnPostResortTestsAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task StageAndGoto_AppliesTheLegacyNormalisations()
    {
        var model = Create(Defra);
        FillValidCase(model);
        model.Case.BirthDate = null;
        model.Case.BirthDateSource = "X";
        model.Case.IsBirthDateEst = true;
        model.Case.FormBDate = new DateTime(2024, 2, 1);
        model.Case.Fate = "SL";
        model.Case.PurchaserBse1ReceivedDate = new DateTime(2024, 3, 1);
        model.Case.BreederBse1ReceivedDate = new DateTime(2024, 3, 1);
        model.Case.Vendor1Bse1ReceivedDate = new DateTime(2024, 3, 1);
        model.Case.HomebredBse1ReceivedDate = new DateTime(2024, 3, 1);
        model.Case.SummarySheetReceivedDate = new DateTime(2024, 3, 1);
        model.Case.PaperworkCompleteDate = new DateTime(2024, 3, 1);

        await model.OnPostStageAndGotoAsync("/Case/Farm");

        model.Case.BirthDateSource.Should().BeNull();
        model.Case.IsBirthDateEst.Should().BeFalse();
        model.Case.SlaughterDate.Should().Be(new DateTime(2024, 2, 1));
        model.Case.IsPurchaserBse1Received.Should().BeTrue();
        model.Case.IsPaperworkComplete.Should().BeTrue();
    }

    // ── Case work staging ───────────────────────────────────────────────────────

    [Fact]
    public async Task StageCaseWork_NoRoleCanChangeBarcodeAndAhfReference()
    {
        _caseWorkRepository.GetByRbseAsync(Rbse).Returns(new CaseWorkRecord { Rbse = Rbse, Barcode = "OLD", AhfReference = "OLDAHF", RbseDate = new DateTime(2023, 1, 1), IsCaseClosed = false });

        var defra = Create(Defra);
        FillValidCase(defra);
        defra.Case.HasCaseWork = true;
        defra.Case.Barcode = "FORGED";
        await defra.OnPostStageAndGotoAsync("/Case/Farm");
        _sessionState.Current!.CaseWork!.Barcode.Should().Be("OLD");
    }

    // ── Domain rules (through Save) ─────────────────────────────────────────────

    private static IEnumerable<object[]> RuleCases()
    {
        var a = new DateTime(2024, 1, 1);
        yield return [(Action<EditModel>)(m => { m.Case.EartagCountry = "ZZ"; m.Case.EartagHerdmark = "1"; m.Case.Eartag = "2"; }), "Case.EartagCountry"];
        yield return [(Action<EditModel>)(m => m.Case.Bse1ReceivedDate = DateTime.Today.AddDays(3)), "Case.Bse1ReceivedDate"];
        yield return [(Action<EditModel>)(m => m.Case.FormADate = DateTime.Today.AddDays(3)), "Case.FormADate"];
        yield return [(Action<EditModel>)(m => { m.Case.FormADate = a; m.Case.SlaughterDate = a.AddDays(-5); }), "Case.FormADate"];
        yield return [(Action<EditModel>)(m => { m.Case.FormADate = null; m.Case.FormAResubmittedDate = a; }), "Case.FormAResubmittedDate"];
        yield return [(Action<EditModel>)(m => { m.Case.FormAResubmittedDate = a.AddDays(-9); }), "Case.FormAResubmittedDate"];
        yield return [(Action<EditModel>)(m => { m.Case.FormADate = null; m.Case.FormBDate = a; }), "Case.FormBDate"];
        yield return [(Action<EditModel>)(m => { m.Case.FormBDate = a.AddDays(-9); }), "Case.FormBDate"];
        yield return [(Action<EditModel>)(m => { m.Case.FormBDate = a.AddDays(9); m.Case.Fate = ""; }), "Case.Fate"];
        yield return [(Action<EditModel>)(m => { m.Case.FormBDate = null; m.Case.FormCDate = a.AddDays(9); }), "Case.FormCDate"];
        yield return [(Action<EditModel>)(m => m.Case.BirthDate = new DateTime(1960, 1, 1)), "Case.BirthDate"];
        yield return [(Action<EditModel>)(m => m.Case.BirthDate = a.AddDays(30)), "Case.BirthDate"];
        yield return [(Action<EditModel>)(m => { m.Case.BirthDate = a.AddDays(-1); m.Case.PurchaseDate = a.AddDays(-9); }), "Case.BirthDate"];
        yield return [(Action<EditModel>)(m => { m.Case.BirthDate = a.AddDays(-1); m.Case.OnsetDate = a.AddDays(-9); }), "Case.BirthDate"];
        yield return [(Action<EditModel>)(m => { m.Case.HasCaseWork = true; m.Case.RbseDate = a; m.Case.PurchaserBse1ReceivedDate = a; }), "Case.PurchaserBse1ReceivedDate"];
        yield return [(Action<EditModel>)(m => { m.Case.HasCaseWork = true; m.Case.RbseDate = a; m.Case.PaperworkCompleteDate = DateTime.Today.AddDays(4); }), "Case.PaperworkCompleteDate"];
    }

    private static readonly object[][] RuleRows = RuleCases().ToArray();

    public static TheoryData<int> Rules() => [.. Enumerable.Range(0, RuleRows.Length)];

    [Theory]
    [MemberData(nameof(Rules))]
    public async Task Save_ReportsLegacyRuleViolations(int caseIndex)
    {
        var arrange = (Action<EditModel>)RuleRows[caseIndex][0];
        var key = (string)RuleRows[caseIndex][1];
        var model = Create(Defra);
        FillValidCase(model);
        arrange(model);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(key);
    }

    [Fact]
    public async Task Save_AsVlaDataEntry_RedirectsWithoutCommitting()
    {
        var model = Create(VlaDataEntry);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _orchestration.DidNotReceive().CommitAllAsync(Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task SaveAndGotoCasework_ForAViewer_RedirectsToTheCaseworkPage()
    {
        var result = await Create().OnPostSaveAndGotoCaseworkAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/Entry");
    }

    [Fact]
    public async Task Save_ForANonGbCase_KeepsThePersistedFormADate()
    {
        var record = ExistingCase(true);
        _caseService.GetCaseAsync(Rbse).Returns(record);
        var model = Create(Defra);
        FillValidCase(model);
        model.Case.FormADate = null;
        model.Case.BirthDate = new DateTime(1960, 1, 1);

        await model.OnPostAsync();

        model.Case.FormADate.Should().Be(record.FormADate);
    }

    [Theory]
    [InlineData(BSE.Modules.CaseManagement.Enums.EditCaseResult.AuditLogError, "Audit log error")]
    [InlineData(BSE.Modules.CaseManagement.Enums.EditCaseResult.PostUpdateError, "Database error")]
    [InlineData(BSE.Modules.CaseManagement.Enums.EditCaseResult.ConcurrencyConflict, "Update failed")]
    public async Task Save_CommitFailure_SetsErrorMessage(BSE.Modules.CaseManagement.Enums.EditCaseResult failure, string expected)
    {
        _currentUser.GetUserIdAsync().Returns(7);
        _orchestration.CommitAllAsync(Rbse, 7).Returns(CaseCommitOutcome.Failure(failure));
        var model = Create(Defra);
        FillValidCase(model);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"]!.ToString().Should().Contain(expected);
    }

    [Fact]
    public async Task StageAndGoToAuditLog_InvalidCase_ReturnsPage()
    {
        var model = Create(Defra);
        FillValidCase(model);
        model.Case.BirthDate = new DateTime(1960, 1, 1);

        (await model.OnPostStageAndGoToAuditLogAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task StageAndGoto_InvalidCase_ReturnsPage()
    {
        var model = Create(Defra);
        FillValidCase(model);
        model.Case.BirthDate = new DateTime(1960, 1, 1);

        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("Case.BirthDate");
    }

    [Fact]
    public async Task TestGridHandlers_WithPersistedCaseWork_StillReopenTheRow()
    {
        _caseWorkRepository.GetByRbseAsync(Rbse).Returns(new CaseWorkRecord { Rbse = Rbse, Barcode = "BC" });
        var model = Create(Defra);
        FillValidCase(model);

        (await model.OnPostBeginEditTestRowAsync(1)).Should().BeOfType<PageResult>();

        model.ReopenEditTestId.Should().Be(1);
    }
}
