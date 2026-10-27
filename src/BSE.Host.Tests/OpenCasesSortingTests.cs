using BSE.Host.Pages.CaseWork;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;

namespace BSE.Host.Tests;

public sealed class OpenCasesSortingTests
{
    private static OpenCasesModel CreateModel(IEnumerable<CaseWorkEntryRecord> rows)
    {
        var service = Substitute.For<ICaseWorkService>();
        service.GetOpenCasesAsync().Returns(Task.FromResult(rows));
        return new OpenCasesModel(service);
    }

    private static readonly DateTime D1 = new(2024, 1, 1);
    private static readonly DateTime D2 = new(2024, 6, 1);

    private static CaseWorkEntryRecord Low() => new()
    {
        Rbse = "A", Survey = "A", Barcode = "A", AhfReference = "A", FormADate = D1, RbseDate = D1, SlaughterDate = D1, Fate = "A",
        ActiveMemoDueDate = D1, ActiveMemoDate = D1, AnnexADueDate = D1, AnnexADate = D1, AnnexBDueDate = D1, AnnexBDate = D1,
        PaperworkCompleteDate = D1, AnnexCDueDate = D1, AnnexCDate = D1, AnnexDDueDate = D1, AnnexDDate = D1, RegionalLab = "A",
        ReceivedByRegionalLabDate = D1, InitialReceivedDate = D1, FinalReceivedDate = D1, FinalSentDate = D1, LabChaseDueDate = D1,
        LabChasedDate = D1, FinalResult = "A", FinalResultDate = D1, BirthDate = D1, Post2000SentDate = D1, BarbMemoDue = "A",
        BarbMinuteSentDate = D1, DataCompleteDate = D1, CaseWorkNotes = "A"
    };

    private static CaseWorkEntryRecord High() => new()
    {
        Rbse = "B", Survey = "B", Barcode = "B", AhfReference = "B", FormADate = D2, RbseDate = D2, SlaughterDate = D2, Fate = "B",
        ActiveMemoDueDate = D2, ActiveMemoDate = D2, AnnexADueDate = D2, AnnexADate = D2, AnnexBDueDate = D2, AnnexBDate = D2,
        PaperworkCompleteDate = D2, AnnexCDueDate = D2, AnnexCDate = D2, AnnexDDueDate = D2, AnnexDDate = D2, RegionalLab = "B",
        ReceivedByRegionalLabDate = D2, InitialReceivedDate = D2, FinalReceivedDate = D2, FinalSentDate = D2, LabChaseDueDate = D2,
        LabChasedDate = D2, FinalResult = "B", FinalResultDate = D2, BirthDate = D2, Post2000SentDate = D2, BarbMemoDue = "B",
        BarbMinuteSentDate = D2, DataCompleteDate = D2, CaseWorkNotes = "B"
    };

    // High is listed first so an ordering that ignores the column cannot pass by accident.
    private static readonly string[] Columns =
    [
        "Rbse", "Survey", "Barcode", "AhfReference", "FormADate", "RbseDate", "SlaughterDate", "Fate", "ActiveMemoDueDate",
        "ActiveMemoDate", "AnnexADueDate", "AnnexADate", "AnnexBDueDate", "AnnexBDate", "PaperworkCompleteDate", "AnnexCDueDate",
        "AnnexCDate", "AnnexDDueDate", "AnnexDDate", "RegionalLab", "ReceivedByRegionalLabDate", "InitialReceivedDate",
        "FinalReceivedDate", "FinalSentDate", "LabChaseDueDate", "LabChasedDate", "FinalResult", "FinalResultDate", "BirthDate",
        "Post2000SentDate", "BarbMemoDue", "BarbMinuteSentDate", "DataCompleteDate", "CaseWorkNotes"
    ];

    public static IEnumerable<object[]> AllColumns() => Columns.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(AllColumns))]
    public async Task OnGet_SortsAscendingByColumn(string column)
    {
        var model = CreateModel([High(), Low()]);
        model.SortColumn = column;

        await model.OnGetAsync();

        model.GetPagedCases().Select(c => c.Rbse).Should().Equal("A", "B");
    }

    [Theory]
    [MemberData(nameof(AllColumns))]
    public async Task OnGet_SortsDescendingByColumn(string column)
    {
        var model = CreateModel([Low(), High()]);
        model.SortColumn = column;
        model.SortDesc = true;

        await model.OnGetAsync();

        model.GetPagedCases().Select(c => c.Rbse).Should().Equal("B", "A");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnGet_UnknownOrBlankColumn_FallsBackToRbse(bool desc)
    {
        var model = CreateModel([High(), Low()]);
        model.SortColumn = "Nope";
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.GetPagedCases().Select(c => c.Rbse).Should().Equal(desc ? ["B", "A"] : ["A", "B"]);

        var blank = CreateModel([High(), Low()]);
        blank.SortColumn = " ";
        await blank.OnGetAsync();
        blank.GetPagedCases()[0].Rbse.Should().Be("A");
    }

    [Fact]
    public async Task OnGet_NullTextValuesSortWithoutError()
    {
        var model = CreateModel([new CaseWorkEntryRecord { Rbse = "B" }, new CaseWorkEntryRecord { Rbse = "A", Survey = "S" }]);
        model.SortColumn = "Survey";

        await model.OnGetAsync();

        model.GetPagedCases().Select(c => c.Rbse).Should().Equal("B", "A");
    }

    [Fact]
    public async Task OnGet_ExplicitPageQuery_WinsOverRbseJump()
    {
        var rows = Enumerable.Range(1, 25).Select(i => new CaseWorkEntryRecord { Rbse = $"{i:000000000}" }).ToList();
        var model = CreateModel(rows);
        model.Rbse = "000000015";
        model.PageNumber = 1;
        PageTestContext.Attach(model);
        model.HttpContext.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?PageNumber=1");

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_UnknownRbse_StaysOnFirstPage()
    {
        var rows = Enumerable.Range(1, 25).Select(i => new CaseWorkEntryRecord { Rbse = $"{i:000000000}" }).ToList();
        var model = CreateModel(rows);
        model.Rbse = "999999999";

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_Empty_HasSinglePage()
    {
        var model = CreateModel([]);

        await model.OnGetAsync();

        model.TotalPages.Should().Be(1);
        model.GetPagedCases().Should().BeEmpty();
    }
}
