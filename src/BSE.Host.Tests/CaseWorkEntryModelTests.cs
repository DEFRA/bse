using BSE.Host.Pages.CaseWork;
using BSE.Host.Services;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;

namespace BSE.Host.Tests;

public class CaseWorkEntryModelTests
{
    private const string Rbse = "002600001";

    private readonly ICaseWorkService _service = Substitute.For<ICaseWorkService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupRepository _lookups = Substitute.For<ILookupRepository>();
    private readonly ITestRepository _tests = Substitute.For<ITestRepository>();

    private static CaseWorkEntryRecord Record(Func<CaseWorkEntryRecord, CaseWorkEntryRecord>? tweak = null)
    {
        var r = new CaseWorkEntryRecord
        {
            Rbse = Rbse,
            RbseDate = new DateTime(2020, 1, 1),
            Survey = "FS",
            Fate = "DIED",
            Origin = "HB",
            FinalResult = "Neg",
            RegionalLab = " LAB1 ",
            Barcode = "BC",
            AhfReference = "AHF",
            CaseWorkNotes = "notes",
            IsCaseClosed = false,
            TseTestingSite = "Site A",
            SamplingDate = new DateTime(2021, 1, 1),
            Ahro = 3
        };
        return tweak is null ? r : tweak(r);
    }

    private CaseWorkEntryModel CreateModel(CaseWorkEntryRecord? record, string surveyDescription = "Fallen Stock")
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns(Task.FromResult(record));
        _user.GetUserIdAsync().Returns(Task.FromResult(11));
        _lookups.GetRegionalLabsAsync().Returns(Task.FromResult<IEnumerable<LookupItem>>([new LookupItem(1, "LAB1 ", "Lab One")]));
        _lookups.GetAHROAsync().Returns(Task.FromResult<IEnumerable<LuAHRO>>([new LuAHRO { Id = 3, Name = "Ahro" }]));
        _lookups.GetTSETestingSitesAsync().Returns(Task.FromResult<IEnumerable<LuTSETestingSite>>([new LuTSETestingSite { Id = 1, Name = "Site A" }]));
        _lookups.GetCaseFatesAsync().Returns(Task.FromResult<IEnumerable<LuCaseFate>>([new LuCaseFate { Code = "DIED", Description = "Died" }]));
        _lookups.GetAnimalOriginsAsync().Returns(Task.FromResult<IEnumerable<LuAnimalOrigin>>([new LuAnimalOrigin { Code = "HB", Description = "Homebred" }]));
        _lookups.GetSurveysAsync().Returns(Task.FromResult<IEnumerable<LookupItem>>([new LookupItem(1, "FS", surveyDescription)]));
        _lookups.GetTestResultsAsync().Returns(Task.FromResult<IEnumerable<LuTestResult>>([new LuTestResult { Code = "Neg", Description = "Negative" }]));
        _tests.GetByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<CaseTestRecord>>([]));

        var http = new DefaultHttpContext();
        return new CaseWorkEntryModel(_service, _user, _lookups, _tests)
        {
            Rbse = Rbse,
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    private static SqlException CreateSqlException(string message)
    {
        var ex = (SqlException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SqlException));
        typeof(Exception).GetField("_message", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(ex, message);
        return ex;
    }

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var model = CreateModel(Record());
        model.Rbse = "";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_NoEntry_ReturnsPageWithoutPopulating()
    {
        var model = CreateModel(null);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Entry.Should().BeNull();
        model.Barcode.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_Found_PopulatesFieldsAndDescriptions()
    {
        var model = CreateModel(Record(r => r with { IsCaseClosed = true }));

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Barcode.Should().Be("BC");
        model.RegionalLab.Should().Be("LAB1");
        model.AhroId.Should().Be(3);
        model.ClosedOnLoad.Should().BeTrue();
        model.FateDescription.Should().Be("Died");
        model.OriginDescription.Should().Be("Homebred");
        model.SurveyDescription.Should().Be("Fallen Stock");
        model.FinalResultDescription.Should().Be("Negative");
        model.ShowTseFields.Should().BeTrue();
        model.RegionalLabOptions.Should().ContainSingle(o => o.Value == "LAB1");
        model.AhroOptions.Should().ContainSingle(o => o.Value == "3");
        model.TseTestingSiteOptions.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGet_OtherSurvey_HidesTseFields()
    {
        var model = CreateModel(Record(), surveyDescription: "Clinical");

        await model.OnGetAsync();

        model.ShowTseFields.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_SurveillanceCohort_ShowsTseFields()
    {
        var model = CreateModel(Record(), surveyDescription: "Surveillance Cohort");

        await model.OnGetAsync();

        model.ShowTseFields.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_Post2000DiedWithNonNegativeTest_ShowsWarning()
    {
        var model = CreateModel(Record(r => r with { BirthDate = new DateTime(2002, 1, 1) }));
        _tests.GetByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<CaseTestRecord>>(
            [new CaseTestRecord(1, Rbse, "T", null, "Pos", null, null)]));

        await model.OnGetAsync();

        model.ShowPost2000Warning.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_Post2000AllNegative_NoWarning()
    {
        var model = CreateModel(Record(r => r with { BirthDate = new DateTime(2002, 1, 1), Fate = "SL" }));
        _tests.GetByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<CaseTestRecord>>(
            [new CaseTestRecord(1, Rbse, "T", null, " neg ", null, null)]));

        await model.OnGetAsync();

        model.ShowPost2000Warning.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_Post2000OtherFate_SkipsTestLookup()
    {
        var model = CreateModel(Record(r => r with { BirthDate = new DateTime(2002, 1, 1), Fate = "OTHER" }));

        await model.OnGetAsync();

        model.ShowPost2000Warning.Should().BeFalse();
        await _tests.DidNotReceive().GetByRbseAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task OnGet_BornBefore2001_SkipsTestLookup()
    {
        var model = CreateModel(Record(r => r with { BirthDate = new DateTime(1999, 1, 1) }));

        await model.OnGetAsync();

        await _tests.DidNotReceive().GetByRbseAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task OnGet_MinuteRules_DisabledReasons()
    {
        var model = CreateModel(Record());

        await model.OnGetAsync();

        model.AnnexADisabledReason.Should().BeNull();
        model.AnnexBDisabledReason.Should().Be("Annex B cannot be sent before Annex A");
        model.AnnexCDisabledReason.Should().BeNull();
        model.AnnexDDisabledReason.Should().Be("Annex D cannot be sent before Annex C");
    }

    [Fact]
    public async Task OnGet_RbseDateInFuture_DisablesAnnexes()
    {
        var model = CreateModel(Record(r => r with { RbseDate = DateTime.Today.AddDays(5), AnnexADate = DateTime.Today, AnnexCDate = DateTime.Today }));

        await model.OnGetAsync();

        model.AnnexADisabledReason.Should().Be("Annex A cannot be sent until after the RBSE Date");
        model.AnnexDDisabledReason.Should().Be("Annex D cannot be sent until after the RBSE Date");
    }

    [Fact]
    public async Task Save_NoEntry_ReturnsNotFound()
    {
        var model = CreateModel(null);

        (await model.OnPostSaveAsync()).Should().BeOfType<NotFoundResult>();
        await _service.DidNotReceive().EditCaseWorkEntryAsync(Arg.Any<EditCaseWorkEntryCommand>());
    }

    [Fact]
    public async Task Save_DateBeforeRbseDate_AddsModelError()
    {
        var model = CreateModel(Record());
        model.BreederBse1ReceivedDate = new DateTime(2019, 1, 1);

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("BreederBse1ReceivedDate");
        await _service.DidNotReceive().EditCaseWorkEntryAsync(Arg.Any<EditCaseWorkEntryCommand>());
    }

    [Fact]
    public async Task Save_DateInFuture_AddsModelError()
    {
        var model = CreateModel(Record());
        model.DataCompleteDate = DateTime.Today.AddDays(1);

        (await model.OnPostSaveAsync()).Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("DataCompleteDate");
    }

    [Fact]
    public async Task Save_FinalSentBeforeFinalReceived_AddsModelError()
    {
        var model = CreateModel(Record());
        model.FinalReceivedDate = new DateTime(2021, 5, 1);
        model.FinalSentDate = new DateTime(2021, 4, 1);

        (await model.OnPostSaveAsync()).Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("FinalSentDate");
    }

    [Fact]
    public async Task Save_TseSiteInvalidAndNoSamplingDate_AddsErrors()
    {
        var model = CreateModel(Record());
        model.TseTestingSite = "Unknown";
        model.SamplingDate = null;

        (await model.OnPostSaveAsync()).Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKeys("TseTestingSite", "SamplingDate");
    }

    [Fact]
    public async Task Save_Valid_OpenCase_SavesAndRedirectsToOpenCases()
    {
        var model = CreateModel(Record(r => r with { AnnexADate = new DateTime(2021, 2, 2) }));
        model.TseTestingSite = " site a ";
        model.SamplingDate = new DateTime(2021, 1, 1);
        model.Barcode = "B1";
        model.RegionalLab = " ";
        model.ClosedOnLoad = false;
        model.ToggleCaseStatus = false;

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/OpenCases");
        model.TempData["Success"].Should().Be($"Case work entry for {Rbse} has been updated.");
        await _service.Received(1).EditCaseWorkEntryAsync(Arg.Is<EditCaseWorkEntryCommand>(c =>
            c.Rbse == Rbse && c.Barcode == "B1" && c.RegionalLab == null && c.UserId == 11
            && c.IsCaseClosed == false && c.AnnexADate == new DateTime(2021, 2, 2)));
    }

    [Theory]
    [InlineData(false, true, "/CaseWork/ClosedCases")]
    [InlineData(true, false, "/CaseWork/ClosedCases")]
    [InlineData(true, true, "/CaseWork/OpenCases")]
    public async Task Save_ToggleInterpretedRelativeToLoadedState(bool closedOnLoad, bool toggle, string page)
    {
        var model = CreateModel(Record(), surveyDescription: "Clinical");
        model.ClosedOnLoad = closedOnLoad;
        model.ToggleCaseStatus = toggle;
        var expectedClosed = closedOnLoad ? !toggle : toggle;

        var result = await model.OnPostSaveAsync();

        expectedClosed.Should().Be(page == "/CaseWork/ClosedCases");
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be(page);
        await _service.Received(1).EditCaseWorkEntryAsync(Arg.Is<EditCaseWorkEntryCommand>(c => c.IsCaseClosed == expectedClosed));
    }

    [Fact]
    public async Task SendMinute_InvalidType_ReturnsBadRequest()
    {
        var model = CreateModel(Record());

        (await model.OnPostSendMinuteAsync("Bogus")).Should().BeOfType<BadRequestResult>();
        await _service.DidNotReceive().GetCaseWorkEntryAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SendMinute_NoEntry_ReturnsNotFound()
    {
        var model = CreateModel(null);

        (await model.OnPostSendMinuteAsync("AnnexA")).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SendMinute_ValidationFails_ReturnsPage()
    {
        var model = CreateModel(Record());
        model.PaperworkCompleteDate = DateTime.Today.AddDays(2);

        (await model.OnPostSendMinuteAsync("AnnexA")).Should().BeOfType<PageResult>();
        await _service.DidNotReceive().SetMinuteSentDateAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SendMinute_NotYetSent_SetsDateAndRedirectsToMinute()
    {
        var model = CreateModel(Record(), surveyDescription: "Clinical");

        var result = await model.OnPostSendMinuteAsync("AnnexA");

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/CaseWork/Minute");
        redirect.RouteValues!["type"].Should().Be("AnnexA");
        await _service.Received(1).SetMinuteSentDateAsync(Rbse, "AnnexA");
        await _service.Received(1).EditCaseWorkEntryAsync(Arg.Any<EditCaseWorkEntryCommand>());
    }

    [Fact]
    public async Task SendMinute_AlreadySent_DoesNotResetDate()
    {
        var model = CreateModel(Record(r => r with { AnnexADate = new DateTime(2021, 1, 1) }), surveyDescription: "Clinical");

        await model.OnPostSendMinuteAsync("AnnexA");

        await _service.DidNotReceive().SetMinuteSentDateAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData("Fallen Stock", "AMFS")]
    [InlineData("Clinical", "ActiveMemo")]
    public async Task SendMinute_ActiveMemo_RoutesByTseSurvey(string survey, string expectedType)
    {
        var model = CreateModel(Record(r => r with { SamplingDate = new DateTime(2021, 1, 1) }), survey);
        model.TseTestingSite = "Site A";
        model.SamplingDate = new DateTime(2021, 1, 1);

        var result = await model.OnPostSendMinuteAsync("ActiveMemo");

        result.Should().BeOfType<RedirectToPageResult>().Which.RouteValues!["type"].Should().Be(expectedType);
    }

    [Fact]
    public async Task SendMinute_SameDayAnnexConstraint_ShowsFriendlyError()
    {
        var model = CreateModel(Record(), surveyDescription: "Clinical");
        _service.SetMinuteSentDateAsync(Rbse, "AnnexB")
            .Returns<Task>(_ => throw CreateSqlException("violates CK_CaseWork_AnnexB check"));

        var result = await model.OnPostSendMinuteAsync("AnnexB");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/Entry");
        model.TempData["ErrorMessage"].Should().Be(
            "Annex B cannot be sent on the same day as the preceding annex. Please try again tomorrow.");
    }

    [Fact]
    public async Task SendMinute_OtherSqlError_Propagates()
    {
        var model = CreateModel(Record(), surveyDescription: "Clinical");
        _service.SetMinuteSentDateAsync(Rbse, "AnnexA")
            .Returns<Task>(_ => throw CreateSqlException("deadlock"));

        var act = () => model.OnPostSendMinuteAsync("AnnexA");

        await act.Should().ThrowAsync<SqlException>();
    }

    [Theory]
    [InlineData(true, "/CaseWork/ClosedCases")]
    [InlineData(false, "/CaseWork/OpenCases")]
    [InlineData(null, "/CaseWork/OpenCases")]
    public async Task Cancel_RedirectsByClosedState(bool? closed, string page)
    {
        var model = CreateModel(Record(r => r with { IsCaseClosed = closed }));

        var result = await model.OnPostCancelAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be(page);
    }

    [Fact]
    public async Task Cancel_NoRecord_RedirectsToOpenCases()
    {
        var model = CreateModel(null);

        (await model.OnPostCancelAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/OpenCases");
    }
}
