using BSE.Host.Pages.CaseWork;
using BSE.Host.Services;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NSubstitute;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BSE.Modules.UserManagement.Tests;

public sealed class CaseWorkEntryModelTests
{
    private const string Rbse = "002600001";
    private readonly ICaseWorkService _service = Substitute.For<ICaseWorkService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILookupRepository _lookups = Substitute.For<ILookupRepository>();
    private readonly ITestRepository _tests = Substitute.For<ITestRepository>();

    public CaseWorkEntryModelTests()
    {
        _currentUser.GetUserIdAsync().Returns(9);
        _lookups.GetRegionalLabsAsync().Returns(new[] { new LookupItem(1, "RL1 ", "Lab one") });
        _lookups.GetAHROAsync().Returns(new[] { new LuAhro { Id = 4, Name = "Ahro four" } });
        _lookups.GetTSETestingSitesAsync().Returns(new[] { new LuTseTestingSite { Name = "Site A" } });
        _lookups.GetCaseFatesAsync().Returns(new[] { new LuCaseFate { Code = "SL", Description = "Slaughter" } });
        _lookups.GetAnimalOriginsAsync().Returns(new[] { new LuAnimalOrigin { Code = "P", Description = "Purchased" } });
        _lookups.GetSurveysAsync().Returns(new[] { new LookupItem(1, "FS", "Fallen Stock"), new LookupItem(2, "CL", "Clinical") });
        _lookups.GetTestResultsAsync().Returns(new[] { new LuTestResult { Code = "Neg", Description = "Negative" } });
        _tests.GetByRbseAsync(Rbse).Returns(Array.Empty<CaseTestRecord>());
        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry());
    }

    private static CaseWorkEntryRecord Entry(Func<CaseWorkEntryRecord, CaseWorkEntryRecord>? tweak = null)
    {
        var e = new CaseWorkEntryRecord
        {
            Rbse = Rbse, Survey = "CL", Fate = "SL", Origin = "P", FinalResult = "Neg",
            RbseDate = new DateTime(2024, 1, 1), BirthDate = new DateTime(2003, 1, 1),
            Barcode = "BC", RegionalLab = "RL1", IsCaseClosed = false, TseTestingSite = "Site A", Ahro = 4,
            SamplingDate = new DateTime(2024, 1, 5)
        };
        return tweak is null ? e : tweak(e);
    }

    private CaseWorkEntryModel Create()
    {
        var model = new CaseWorkEntryModel(_service, _currentUser, _lookups, _tests) { Rbse = Rbse };
        PageTestHelpers.Attach(model, "VLAMaintenance");
        return model;
    }

    [Fact]
    public async Task OnGetAsync_WithoutRbse_RedirectsToSessionError()
    {
        var model = Create();
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGetAsync_WhenEntryMissing_ReturnsTheEmptyPage()
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns((CaseWorkEntryRecord?)null);
        var model = Create();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();
        model.Entry.Should().BeNull();
    }

    [Fact]
    public async Task OnGetAsync_PopulatesTheFormAndDescriptions()
    {
        var model = Create();

        await model.OnGetAsync();

        model.Barcode.Should().Be("BC");
        model.RegionalLab.Should().Be("RL1");
        model.AhroId.Should().Be(4);
        model.FateDescription.Should().Be("Slaughter");
        model.OriginDescription.Should().Be("Purchased");
        model.SurveyDescription.Should().Be("Clinical");
        model.FinalResultDescription.Should().Be("Negative");
        model.ShowTseFields.Should().BeFalse();
        model.RegionalLabOptions.Should().ContainSingle();
        model.AhroOptions.Should().ContainSingle();
        model.TseTestingSiteOptions.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGetAsync_ShowsTseFieldsForFallenStock()
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry(e => e with { Survey = "FS" }));
        var model = Create();

        await model.OnGetAsync();

        model.ShowTseFields.Should().BeTrue();
    }

    [Theory]
    [InlineData("DIED", "Pos", true)]
    [InlineData("SL", "Neg", false)]
    [InlineData("ALIVE", "Pos", false)]
    public async Task OnGetAsync_RaisesThePost2000WarningOnlyForNonNegativeResults(string fate, string result, bool expected)
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry(e => e with { Fate = fate }));
        _tests.GetByRbseAsync(Rbse).Returns(new[] { new CaseTestRecord(1, Rbse, "T", "Type", result, result, [1]) });
        var model = Create();

        await model.OnGetAsync();

        model.ShowPost2000Warning.Should().Be(expected);
    }

    [Fact]
    public async Task OnGetAsync_DoesNotWarnForOlderAnimals()
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry(e => e with { BirthDate = new DateTime(1999, 1, 1), Fate = "DIED" }));
        var model = Create();

        await model.OnGetAsync();

        model.ShowPost2000Warning.Should().BeFalse();
    }

    [Fact]
    public async Task OnGetAsync_ExplainsWhyAnnexButtonsAreDisabled()
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry(e => e with { RbseDate = DateTime.Today }));
        var model = Create();

        await model.OnGetAsync();

        model.AnnexADisabledReason.Should().Contain("after the RBSE Date");
        model.AnnexBDisabledReason.Should().NotBeNull();
        model.AnnexDDisabledReason.Should().NotBeNull();

        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry());
        var later = Create();
        await later.OnGetAsync();
        later.AnnexADisabledReason.Should().BeNull();
        later.AnnexBDisabledReason.Should().Be("Annex B cannot be sent before Annex A");
        later.AnnexDDisabledReason.Should().Be("Annex D cannot be sent before Annex C");
    }

    [Fact]
    public async Task OnPostSaveAsync_WhenEntryMissing_IsNotFound()
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns((CaseWorkEntryRecord?)null);

        (await Create().OnPostSaveAsync()).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostSaveAsync_ValidatesDateRanges()
    {
        var model = Create();
        model.PurchaserBse1ReceivedDate = new DateTime(2023, 1, 1);
        model.BreederBse1ReceivedDate = DateTime.Today.AddDays(2);
        model.FinalReceivedDate = new DateTime(2024, 3, 1);
        model.FinalSentDate = new DateTime(2024, 2, 1);

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain(["PurchaserBse1ReceivedDate", "BreederBse1ReceivedDate", "FinalSentDate"]);
        await _service.DidNotReceive().EditCaseWorkEntryAsync(Arg.Any<EditCaseWorkEntryCommand>());
    }

    [Fact]
    public async Task OnPostSaveAsync_RequiresTseDetailsForFallenStock()
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry(e => e with { Survey = "FS" }));
        var model = Create();
        model.TseTestingSite = "Unknown";
        model.SamplingDate = null;

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain(["TseTestingSite", "SamplingDate"]);
    }

    [Theory]
    [InlineData(false, false, "/CaseWork/OpenCases")]
    [InlineData(false, true, "/CaseWork/ClosedCases")]
    [InlineData(true, true, "/CaseWork/OpenCases")]
    [InlineData(true, false, "/CaseWork/ClosedCases")]
    public async Task OnPostSaveAsync_TogglesTheClosedStateRelativeToTheLoadedState(bool closedOnLoad, bool toggle, string expectedPage)
    {
        var model = Create();
        model.ClosedOnLoad = closedOnLoad;
        model.ToggleCaseStatus = toggle;
        model.RegionalLab = " ";

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be(expectedPage);
        await _service.Received(1).EditCaseWorkEntryAsync(Arg.Is<EditCaseWorkEntryCommand>(c => c.UserId == 9 && c.RegionalLab == null));
    }

    [Fact]
    public async Task OnPostCancelAsync_ReturnsToTheListMatchingTheCaseState()
    {
        (await Create().OnPostCancelAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/OpenCases");

        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry(e => e with { IsCaseClosed = true }));
        (await Create().OnPostCancelAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/ClosedCases");
    }

    [Fact]
    public async Task OnPostSendMinuteAsync_RejectsUnknownMinuteTypes()
    {
        (await Create().OnPostSendMinuteAsync("Bogus")).Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnPostSendMinuteAsync_WhenEntryMissing_IsNotFound()
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns((CaseWorkEntryRecord?)null);

        (await Create().OnPostSendMinuteAsync("AnnexA")).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostSendMinuteAsync_WithInvalidDates_RedisplaysThePage()
    {
        var model = Create();
        model.PurchaserBse1ReceivedDate = new DateTime(2023, 1, 1);

        (await model.OnPostSendMinuteAsync("AnnexA")).Should().BeOfType<PageResult>();
    }

    [Theory]
    [InlineData("ActiveMemo", "Clinical", "ActiveMemo")]
    [InlineData("AnnexA", "Clinical", "AnnexA")]
    public async Task OnPostSendMinuteAsync_SavesStampsTheSentDateAndOpensTheMinute(string minute, string survey, string expectedType)
    {
        var model = Create();

        var result = await model.OnPostSendMinuteAsync(minute);

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/CaseWork/Minute");
        redirect.RouteValues!["type"].Should().Be(expectedType);
        await _service.Received(1).SetMinuteSentDateAsync(Rbse, minute);
    }

    [Fact]
    public async Task OnPostSendMinuteAsync_UsesTheFallenStockMemoAndSkipsRestamping()
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry(e => e with { Survey = "FS", ActiveMemoDate = new DateTime(2024, 2, 2) }));
        var model = Create();
        model.TseTestingSite = "Site A";
        model.SamplingDate = new DateTime(2024, 1, 5);

        var result = await model.OnPostSendMinuteAsync("ActiveMemo");

        result.Should().BeOfType<RedirectToPageResult>().Which.RouteValues!["type"].Should().Be("AMFS");
        await _service.DidNotReceive().SetMinuteSentDateAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData("AnnexB")]
    [InlineData("AnnexC")]
    [InlineData("AnnexD")]
    public async Task OnPostSendMinuteAsync_ReadsTheExistingSentDateForEachAnnex(string minute)
    {
        _service.GetCaseWorkEntryAsync(Rbse).Returns(Entry(e => e with
        {
            AnnexADate = new DateTime(2024, 2, 1), AnnexBDate = new DateTime(2024, 2, 2),
            AnnexCDate = new DateTime(2024, 2, 3), AnnexDDate = new DateTime(2024, 2, 4)
        }));

        var result = await Create().OnPostSendMinuteAsync(minute);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/Minute");
        await _service.DidNotReceive().SetMinuteSentDateAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task OnPostSendMinuteAsync_WhenTheAnnexConstraintFires_ShowsAFriendlyError()
    {
        var model = Create();
        _service.SetMinuteSentDateAsync(Rbse, "AnnexB").Returns(Task.FromException(CreateSqlException("CK_CaseWork_AnnexB violated")));

        var result = await model.OnPostSendMinuteAsync("AnnexB");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/Entry");
        model.TempData["ErrorMessage"]!.ToString().Should().Contain("Annex B cannot be sent on the same day");
    }

    private static SqlException CreateSqlException(string message)
    {
        var ex = (SqlException)RuntimeHelpers.GetUninitializedObject(typeof(SqlException));
        typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ex, message);
        return ex;
    }
}

public sealed class CaseWorkReportModelTests
{
    private readonly ICaseWorkService _service = Substitute.For<ICaseWorkService>();

    private static CaseWorkEntryRecord Row(string rbse, string barcode, string? survey = "CL") => new()
    {
        Rbse = rbse,
        Barcode = barcode,
        Survey = survey,
        Fate = "SL",
        ActiveMemoDate = new DateTime(2024, 1, 1),
        RbseDate = new DateTime(2024, 1, 2)
    };

    [Fact]
    public async Task OpenCases_OnGetAsync_PaginatesAndSortsTheCaseList()
    {
        _service.GetOpenCasesAsync().Returns(Enumerable.Range(1, 25).Select(i => Row($"00260000{i:00}", $"B{i:00}")).ToArray());
        var model = new OpenCasesModel(_service)
        {
            PageNumber = 2,
            SortColumn = "Barcode",
            SortDesc = true
        };

        await model.OnGetAsync();

        model.TotalPages.Should().Be(3);
        model.PagedCases().Should().HaveCount(10);
        model.PagedCases().First().Rbse.Should().Be("0026000015");
        model.PagedCases().Last().Rbse.Should().Be("0026000006");
    }

    [Fact]
    public async Task ClosedCases_OnGetAsync_JumpsToTheSelectedRbsePage()
    {
        _service.GetClosedCasesAsync().Returns(Enumerable.Range(1, 25).Select(i => Row($"00260000{i:00}", $"C{i:00}")).ToArray());
        var model = new ClosedCasesModel(_service)
        {
            Rbse = "0026000015",
            PageNumber = 1
        };

        await model.OnGetAsync();

        model.PageNumber.Should().Be(2);
        model.PagedCases().Should().Contain(c => c.Rbse == "0026000015");
    }
}
