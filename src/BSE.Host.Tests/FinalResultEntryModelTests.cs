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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace BSE.Host.Tests;

public class FinalResultEntryModelTests
{
    private const string Rbse = "002600001";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly ITestRepository _tests = Substitute.For<ITestRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILogger<FinalResultEntryModel> _logger = Substitute.For<ILogger<FinalResultEntryModel>>();

    private FinalResultEntryModel CreateModel(FinalResultRecord? record, IReadOnlyList<CaseTestRecord>? tests = null)
    {
        _cases.GetFinalResultAsync(Arg.Any<string>()).Returns(Task.FromResult(record));
        _tests.GetByRbseAsync(Arg.Any<string>()).Returns(Task.FromResult(tests ?? []));
        _lookups.GetTestTypesAsync().Returns(Task.FromResult<IEnumerable<LuTestType>>(
            [new LuTestType { Code = "ELISA", Description = "Elisa Test" }]));
        _lookups.GetTestResultsAsync().Returns(Task.FromResult<IEnumerable<LuTestResult>>(
            [new LuTestResult { Code = "Pos", Description = "Positive" }, new LuTestResult { Code = "Neg", Description = "Negative" }]));
        _cases.SaveFinalResultAsync(Arg.Any<EditFinalResultCommand>(), Arg.Any<int>()).Returns(Task.FromResult(EditCaseResult.Success));
        _user.GetUserIdAsync().Returns(Task.FromResult(8));

        var http = new DefaultHttpContext();
        return new FinalResultEntryModel(_cases, _tests, _lookups, _user, _logger)
        {
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    private static FinalResultRecord Rec(string? finalResult = null, string? dbse = null, bool paperwork = true) => new()
    {
        Rbse = Rbse,
        FinalResult = finalResult,
        FinalResultDate = finalResult is null ? null : new DateTime(2025, 6, 1),
        Dbse = dbse,
        IsPaperworkComplete = paperwork,
        RetrospectiveTestType = "ELISA",
        RetrospectiveResult = "Neg",
        RetrospectiveResultDate = new DateTime(2025, 5, 5),
        RetrospectiveComment = "retro",
        LabComment = "lab",
        RowStamp = [1, 2, 3]
    };

    private static CaseTestRecord Test(int id, string type = "ELISA", string result = "Neg") =>
        new(id, Rbse, type, null, result, null, null);

    [Fact]
    public async Task OnGet_NoRbse_ReturnsPageWithoutLookup()
    {
        var model = CreateModel(Rec());

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.CaseFound.Should().BeFalse();
        model.TestTypes.Should().HaveCount(1);
        model.TestResults.Should().HaveCount(2);
        await _cases.DidNotReceive().GetFinalResultAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task OnGet_WithRbse_PadsAndPopulatesInput()
    {
        var model = CreateModel(Rec("Neg", "2600005"));
        model.Rbse = "26/1";

        await model.OnGetAsync();

        model.Rbse.Should().Be(Rbse);
        model.CaseFound.Should().BeTrue();
        model.FinalResult.Should().Be("Neg");
        model.RetrospectiveTestType.Should().Be("ELISA");
        model.RetrospectiveResultDate.Should().Be(new DateTime(2025, 5, 5));
        model.LabComment.Should().Be("lab");
        model.HasFinalResult.Should().BeTrue();
        model.DisplayDbse.Should().Be("26/00005");
        model.DbseWasAllocated.Should().BeTrue();
        model.DisplayFinalResultDate.Should().Be(new DateTime(2025, 6, 1));
    }

    [Fact]
    public async Task OnGet_CaseWithoutFinalResult_ShowsTbcAndToday()
    {
        var model = CreateModel(Rec());
        model.Rbse = Rbse;

        await model.OnGetAsync();

        model.HasFinalResult.Should().BeFalse();
        model.DisplayDbse.Should().Be("TBC");
        model.DbseWasAllocated.Should().BeFalse();
        model.DisplayFinalResultDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task OnGet_CaseNotFound_SetsRbseError()
    {
        var model = CreateModel(null);
        model.Rbse = Rbse;

        await model.OnGetAsync();

        model.RbseError.Should().Be(FinalResultEntryModel.CaseNotFoundMessage);
        model.CaseFound.Should().BeFalse();
        await _tests.DidNotReceive().GetByRbseAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task OnGet_LoadThrows_SetsLoadFailedMessage()
    {
        var model = CreateModel(Rec());
        _cases.GetFinalResultAsync(Arg.Any<string>()).Returns<Task<FinalResultRecord?>>(_ => throw new InvalidOperationException("x"));
        model.Rbse = Rbse;

        await model.OnGetAsync();

        model.ErrorMessage.Should().Be(FinalResultEntryModel.LoadFailedMessage);
        model.CaseFound.Should().BeFalse();
    }

    [Fact]
    public async Task LookUp_BlankRbse_SetsRbseRequired()
    {
        var model = CreateModel(Rec());

        var result = await model.OnPostLookUpAsync();

        result.Should().BeOfType<PageResult>();
        model.RbseError.Should().Be(FinalResultEntryModel.RbseRequiredMessage);
    }

    [Fact]
    public async Task LookUp_Found_LoadsCase()
    {
        var model = CreateModel(Rec("Pos"));
        model.Rbse = Rbse;

        await model.OnPostLookUpAsync();

        model.CaseFound.Should().BeTrue();
        model.FinalResult.Should().Be("Pos");
    }

    [Fact]
    public async Task Save_CaseNotFound_ReturnsPageWithoutSaving()
    {
        var model = CreateModel(null);
        model.Rbse = Rbse;
        model.FinalResult = "Pos";

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<PageResult>();
        await _cases.DidNotReceive().SaveFinalResultAsync(Arg.Any<EditFinalResultCommand>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Save_NoFinalResult_SetsFieldError()
    {
        var model = CreateModel(Rec());
        model.Rbse = Rbse;
        model.FinalResult = " ";

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<PageResult>();
        model.FinalResultError.Should().Be(FinalResultEntryModel.FinalResultRequiredMessage);
        await _cases.DidNotReceive().SaveFinalResultAsync(Arg.Any<EditFinalResultCommand>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Save_ChangedResult_UsesTodayAndRedirectsWithPositiveMessage()
    {
        var model = CreateModel(Rec("Neg", "2600005"));
        _cases.GetFinalResultAsync(Rbse).Returns(
            Task.FromResult<FinalResultRecord?>(Rec("Neg", "2600005")),
            Task.FromResult<FinalResultRecord?>(Rec("Pos", "2600007")));
        model.Rbse = Rbse;
        model.FinalResult = "Pos";
        model.LabComment = "new lab";

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        await _cases.Received(1).SaveFinalResultAsync(Arg.Is<EditFinalResultCommand>(c =>
            c.Rbse == Rbse && c.FinalResult == "Pos" && c.FinalResultDate == DateTime.Today
            && c.LabComment == "new lab" && c.RowStamp.Length == 3), 8);
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("Final result saved");
        model.TempData[MaintenanceConfirmationModel.SummaryKey].Should().Be("Final result recorded for 00/26/00001");
        model.TempData[MaintenanceConfirmationModel.MessageKey].Should().Be("DBSE 26/00007 has been allocated to this case.");
        model.TempData[MaintenanceConfirmationModel.AddAnotherResultKey].Should().Be("/Case/FinalResultEntry");
        model.TempData[MaintenanceConfirmationModel.MemoRbseKey].Should().Be(Rbse);
    }

    [Fact]
    public async Task Save_UnchangedResult_KeepsOriginalDate_AndNonPositiveMessage()
    {
        var model = CreateModel(Rec("Neg", "2600005"));
        model.Rbse = Rbse;
        model.FinalResult = "neg";

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _cases.Received(1).SaveFinalResultAsync(
            Arg.Is<EditFinalResultCommand>(c => c.FinalResultDate == new DateTime(2025, 6, 1)), Arg.Any<int>());
        model.TempData[MaintenanceConfirmationModel.MessageKey].Should().Be("No DBSE is allocated because the final result is not positive.");
    }

    [Fact]
    public async Task Save_ServiceThrows_SetsSaveFailed()
    {
        var model = CreateModel(Rec());
        _cases.SaveFinalResultAsync(Arg.Any<EditFinalResultCommand>(), Arg.Any<int>())
            .Returns<Task<EditCaseResult>>(_ => throw new InvalidOperationException("db"));
        model.Rbse = Rbse;
        model.FinalResult = "Pos";

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be(FinalResultEntryModel.SaveFailedMessage);
    }

    [Theory]
    [InlineData(EditCaseResult.ConcurrencyConflict, FinalResultEntryModel.ConcurrencyMessage)]
    [InlineData(EditCaseResult.RbseNotFound, FinalResultEntryModel.CaseNotFoundMessage)]
    [InlineData((EditCaseResult)99, FinalResultEntryModel.SaveFailedMessage)]
    public async Task Save_FailureResult_SetsMappedMessage(EditCaseResult code, string message)
    {
        var model = CreateModel(Rec());
        _cases.SaveFinalResultAsync(Arg.Any<EditFinalResultCommand>(), Arg.Any<int>()).Returns(Task.FromResult(code));
        model.Rbse = Rbse;
        model.FinalResult = "Pos";

        var result = await model.OnPostSaveAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be(message);
    }

    [Fact]
    public async Task Save_PreservesPostedInputRatherThanReloading()
    {
        var model = CreateModel(Rec("Neg"));
        model.Rbse = Rbse;
        model.FinalResult = " ";
        model.RetrospectiveComment = "typed";

        await model.OnPostSaveAsync();

        model.RetrospectiveComment.Should().Be("typed");
    }

    [Fact]
    public async Task Warnings_PositiveWithoutPositiveTest_AndPaperworkIncomplete()
    {
        var model = CreateModel(Rec(paperwork: false), [Test(1)]);
        model.Rbse = Rbse;
        await model.OnGetAsync();
        model.FinalResult = "Pos";

        model.ShowPositiveWithoutPositiveTest.Should().BeTrue();
        model.ShowPaperworkIncomplete.Should().BeTrue();
        model.ShowNegativeWithPositiveTest.Should().BeFalse();
    }

    [Fact]
    public async Task Warnings_NegativeWithPositiveTest()
    {
        var model = CreateModel(Rec(), [Test(1, result: "Pos")]);
        model.Rbse = Rbse;
        await model.OnGetAsync();
        model.FinalResult = "Neg";

        model.TestsContainPositive.Should().BeTrue();
        model.ShowNegativeWithPositiveTest.Should().BeTrue();
        model.ShowPositiveWithoutPositiveTest.Should().BeFalse();
        model.ShowPaperworkIncomplete.Should().BeFalse();
    }

    [Fact]
    public async Task Tests_PagedAndClamped()
    {
        var tests = Enumerable.Range(1, 23).Select(i => Test(i, type: $"T{i:00}")).ToList();
        var model = CreateModel(Rec(), tests);
        model.Rbse = Rbse;
        model.TestPage = 50;

        await model.OnGetAsync();

        model.TestsTotalCount.Should().Be(23);
        model.TestsTotalPages.Should().Be(3);
        model.TestPage.Should().Be(3);
        model.Tests.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("TestType", false, 2)]
    [InlineData("TestType", true, 1)]
    [InlineData("TestResult", false, 2)]
    [InlineData("TestResult", true, 1)]
    [InlineData(null, false, 2)]
    public async Task Tests_Sorting(string? column, bool desc, int expectedFirstId)
    {
        // id1: type ELISA(Elisa Test) / Pos(Positive); id2: type "AAA" / Neg(Negative)
        var tests = new List<CaseTestRecord> { Test(1, "ELISA", "Pos"), Test(2, "AAA", "Neg") };
        var model = CreateModel(Rec(), tests);
        model.Rbse = Rbse;
        model.TestSortColumn = column;
        model.TestSortDesc = desc;

        await model.OnGetAsync();

        model.Tests[0].Id.Should().Be(expectedFirstId);
    }

    [Fact]
    public async Task Describe_FallsBackToCode()
    {
        var model = CreateModel(Rec());
        await model.OnGetAsync();

        model.DescribeTestType("ELISA").Should().Be("Elisa Test");
        model.DescribeTestType("X").Should().Be("X");
        model.DescribeTestType(null).Should().BeEmpty();
        model.DescribeTestResult("Pos").Should().Be("Positive");
        model.DescribeTestResult("Y").Should().Be("Y");
    }

    [Fact]
    public void TestsPageUrl_BuildsQuery()
    {
        var model = CreateModel(null);
        model.Rbse = "00/26/00001";
        model.TestSortColumn = "TestType";
        model.TestSortDesc = true;

        model.TestsPageUrl(2).Should().Be("?rbse=00%2F26%2F00001&TestPage=2&TestSortColumn=TestType&TestSortDesc=true");
        model.TestsPageUrl(1, "TestResult", false).Should().Be("?rbse=00%2F26%2F00001&TestPage=1&TestSortColumn=TestResult");
    }
}
