using BSE.Host.Pages;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class FinalResultEntryModelTests
{
    private const string Rbse = "002600001";
    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly ITestRepository _tests = Substitute.For<ITestRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public FinalResultEntryModelTests()
    {
        _lookups.GetTestTypesAsync().Returns(new[] { new LuTestType { Code = "T1", Description = "Type one" }, new LuTestType { Code = "T2", Description = "Type two" } });
        _lookups.GetTestResultsAsync().Returns(new[] { new LuTestResult { Code = "Pos", Description = "Positive" }, new LuTestResult { Code = "Neg", Description = "Negative" } });
        _tests.GetByRbseAsync(Rbse).Returns(new[]
        {
            new CaseTestRecord(1, Rbse, "T1", "Type one", "Neg", "Negative", [1]),
            new CaseTestRecord(2, Rbse, "T2", "Type two", "Pos", "Positive", [2]),
        });
        _cases.GetFinalResultAsync(Rbse).Returns(Record());
        _currentUser.GetUserIdAsync().Returns(5);
    }

    private static FinalResultRecord Record(Func<FinalResultRecord, FinalResultRecord>? tweak = null)
    {
        var r = new FinalResultRecord { Rbse = Rbse, IsPaperworkComplete = true, RowStamp = [1] };
        return tweak is null ? r : tweak(r);
    }

    private FinalResultEntryModel Create(string rbse = Rbse)
    {
        var model = new FinalResultEntryModel(_cases, _tests, _lookups, _currentUser, NullLogger<FinalResultEntryModel>.Instance) { Rbse = rbse };
        PageTestHelpers.Attach(model, "DEFRAMaintenance");
        return model;
    }

    [Fact]
    public async Task OnGetAsync_WithoutRbse_OnlyLoadsLookups()
    {
        var model = Create("");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.CaseFound.Should().BeFalse();
        model.TestTypes.Should().HaveCount(2);
    }

    [Fact]
    public async Task OnGetAsync_WithAnRbse_LoadsTheCaseAndTests()
    {
        _cases.GetFinalResultAsync(Rbse).Returns(Record(r => r with { FinalResult = "Pos", Dbse = "002600099", FinalResultDate = new DateTime(2024, 1, 1), LabComment = "note" }));
        var model = Create("26/00001");

        await model.OnGetAsync();

        model.CaseFound.Should().BeTrue();
        model.FinalResult.Should().Be("Pos");
        model.LabComment.Should().Be("note");
        model.HasFinalResult.Should().BeTrue();
        model.DbseWasAllocated.Should().BeTrue();
        model.TestsContainPositive.Should().BeTrue();
        model.TestsTotalCount.Should().Be(2);
        model.DescribeTestType("T1").Should().Be("Type one");
        model.DescribeTestType("zz").Should().Be("zz");
        model.DescribeTestResult(null).Should().BeEmpty();
    }

    [Fact]
    public async Task Display_ForANewResult_ShowsTodayAndTbc()
    {
        var model = Create();

        await model.OnGetAsync();

        model.DisplayFinalResultDate.Should().Be(DateTime.Today);
        model.DisplayDbse.Should().Be("TBC");
        model.HasFinalResult.Should().BeFalse();
    }

    [Fact]
    public async Task OnPostLookUpAsync_ValidatesTheRbse()
    {
        var blank = Create("");
        await blank.OnPostLookUpAsync();
        blank.RbseError.Should().Be(FinalResultEntryModel.RbseRequiredMessage);

        _cases.GetFinalResultAsync(Rbse).Returns((FinalResultRecord?)null);
        var missing = Create();
        await missing.OnPostLookUpAsync();
        missing.RbseError.Should().Be(FinalResultEntryModel.CaseNotFoundMessage);
    }

    [Fact]
    public async Task LookUp_WhenTheServiceThrows_ShowsALoadError()
    {
        _cases.GetFinalResultAsync(Rbse).Returns<FinalResultRecord?>(_ => throw new InvalidOperationException("db"));
        var model = Create();

        await model.OnPostLookUpAsync();

        model.ErrorMessage.Should().Be(FinalResultEntryModel.LoadFailedMessage);
        model.CaseFound.Should().BeFalse();
    }

    [Theory]
    [InlineData("TestType", true)]
    [InlineData("TestType", false)]
    [InlineData("TestResult", true)]
    [InlineData("TestResult", false)]
    [InlineData(null, false)]
    public async Task LookUp_SortsAndPagesTheTests(string? column, bool desc)
    {
        var model = Create();
        model.TestSortColumn = column;
        model.TestSortDesc = desc;
        model.TestPage = 9;

        await model.OnPostLookUpAsync();

        model.Tests.Should().HaveCount(2);
        model.TestPage.Should().Be(1);
        model.TestsTotalPages.Should().Be(1);
    }

    [Fact]
    public void TestsPageUrl_IncludesSortState()
    {
        var model = Create();
        model.TestSortColumn = "TestType";
        model.TestSortDesc = true;

        model.TestsPageUrl(2).Should().Contain("TestPage=2").And.Contain("TestSortColumn=TestType").And.Contain("TestSortDesc=true");
        model.TestsPageUrl(1, "TestResult", false).Should().Contain("TestResult").And.NotContain("TestSortDesc");
    }

    [Theory]
    [InlineData("Pos", false, true, false, true)]
    [InlineData("Neg", true, false, true, false)]
    public async Task Warnings_ReflectTheSelectedResult(string finalResult, bool hasPositive, bool expectNoPositiveWarning, bool expectNegativeWarning, bool expectPaperwork)
    {
        _tests.GetByRbseAsync(Rbse).Returns(hasPositive
            ? new[] { new CaseTestRecord(1, Rbse, "T1", "t", "Pos", "p", [1]) }
            : new[] { new CaseTestRecord(1, Rbse, "T1", "t", "Neg", "n", [1]) });
        _cases.GetFinalResultAsync(Rbse).Returns(Record(r => r with { IsPaperworkComplete = false }));
        var model = Create();
        model.FinalResult = finalResult;

        await model.OnPostSaveAsync();
        model.FinalResult = finalResult;

        model.ShowPositiveWithoutPositiveTest.Should().Be(expectNoPositiveWarning);
        model.ShowNegativeWithPositiveTest.Should().Be(expectNegativeWarning);
        model.ShowPaperworkIncomplete.Should().Be(expectPaperwork);
    }

    [Fact]
    public async Task OnPostSaveAsync_WhenTheCaseIsMissing_JustRedisplays()
    {
        _cases.GetFinalResultAsync(Rbse).Returns((FinalResultRecord?)null);

        (await Create().OnPostSaveAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostSaveAsync_RequiresAFinalResult()
    {
        var model = Create();
        model.FinalResult = " ";

        (await model.OnPostSaveAsync()).Should().BeOfType<PageResult>();

        model.FinalResultError.Should().Be(FinalResultEntryModel.FinalResultRequiredMessage);
    }

    [Theory]
    [InlineData("Pos", "has been allocated")]
    [InlineData("Neg", "No DBSE is allocated")]
    public async Task OnPostSaveAsync_SavesAndShowsTheConfirmation(string finalResult, string expectedMessage)
    {
        _cases.SaveFinalResultAsync(Arg.Any<EditFinalResultCommand>(), 5).Returns(EditCaseResult.Success);
        var model = Create();
        model.FinalResult = finalResult;

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        model.TempData[MaintenanceConfirmationModel.MessageKey]!.ToString().Should().Contain(expectedMessage);
        await _cases.Received(1).SaveFinalResultAsync(Arg.Is<EditFinalResultCommand>(c => c.FinalResult == finalResult && c.FinalResultDate == DateTime.Today), 5);
    }

    [Fact]
    public async Task OnPostSaveAsync_KeepsTheSavedDateWhenTheResultIsUnchanged()
    {
        var saved = new DateTime(2023, 3, 3);
        _cases.GetFinalResultAsync(Rbse).Returns(Record(r => r with { FinalResult = "Neg", FinalResultDate = saved }));
        _cases.SaveFinalResultAsync(Arg.Any<EditFinalResultCommand>(), 5).Returns(EditCaseResult.Success);
        var model = Create();
        model.FinalResult = "Neg";

        await model.OnPostSaveAsync();

        await _cases.Received(1).SaveFinalResultAsync(Arg.Is<EditFinalResultCommand>(c => c.FinalResultDate == saved), 5);
    }

    [Theory]
    [InlineData(EditCaseResult.ConcurrencyConflict, FinalResultEntryModel.ConcurrencyMessage)]
    [InlineData(EditCaseResult.RbseNotFound, FinalResultEntryModel.CaseNotFoundMessage)]
    [InlineData(EditCaseResult.AuditLogError, FinalResultEntryModel.SaveFailedMessage)]
    public async Task OnPostSaveAsync_MapsFailureResultsToMessages(EditCaseResult outcome, string expected)
    {
        _cases.SaveFinalResultAsync(Arg.Any<EditFinalResultCommand>(), 5).Returns(outcome);
        var model = Create();
        model.FinalResult = "Pos";

        (await model.OnPostSaveAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be(expected);
    }

    [Fact]
    public async Task OnPostSaveAsync_WhenSavingThrows_ShowsAGenericError()
    {
        _cases.SaveFinalResultAsync(Arg.Any<EditFinalResultCommand>(), 5).Returns<EditCaseResult>(_ => throw new InvalidOperationException("db"));
        var model = Create();
        model.FinalResult = "Pos";

        (await model.OnPostSaveAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be(FinalResultEntryModel.SaveFailedMessage);
    }
}
