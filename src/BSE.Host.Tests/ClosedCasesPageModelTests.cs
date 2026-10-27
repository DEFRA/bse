using BSE.Host.Pages.CaseWork;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Http;

namespace BSE.Host.Tests;

public sealed class ClosedCasesPageModelTests
{
    private readonly ICaseWorkService _service = Substitute.For<ICaseWorkService>();

    private ClosedCasesModel CreateModel(string query = "")
    {
        var model = new ClosedCasesModel(_service);
        PageTestContext.Attach(model);
        model.PageContext.HttpContext.Request.QueryString = new QueryString(query);
        return model;
    }

    private void Returns(IEnumerable<CaseWorkEntryRecord> rows) => _service.GetClosedCasesAsync().Returns(rows);

    private static List<CaseWorkEntryRecord> Rows(int n) =>
        Enumerable.Range(1, n).Select(i => new CaseWorkEntryRecord { Rbse = $"R{i:000}" }).ToList();

    [Fact]
    public async Task OnGet_NoCases_SinglePage()
    {
        Returns([]);
        var model = CreateModel();

        await model.OnGetAsync();

        model.TotalCount.Should().Be(0);
        model.TotalPages.Should().Be(1);
        model.GetPagedCases().Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_DefaultSort_OrdersByRbseAndPages()
    {
        Returns(Rows(25).AsEnumerable().Reverse());
        var model = CreateModel();
        model.PageNumber = 3;

        await model.OnGetAsync();

        model.TotalPages.Should().Be(3);
        model.GetPagedCases().Select(c => c.Rbse).Should().Equal("R021", "R022", "R023", "R024", "R025");
    }

    [Fact]
    public async Task OnGet_PageNumberOutOfRange_IsClamped()
    {
        Returns(Rows(12));
        var high = CreateModel("?PageNumber=99");
        high.PageNumber = 99;
        var low = CreateModel("?PageNumber=-4");
        low.PageNumber = -4;

        await high.OnGetAsync();
        await low.OnGetAsync();

        high.PageNumber.Should().Be(2);
        low.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_RbseWithoutPageNumber_JumpsToItsPage()
    {
        Returns(Rows(25));
        var model = CreateModel();
        model.Rbse = "r013";

        await model.OnGetAsync();

        model.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task OnGet_RbseWithExplicitPageNumber_KeepsPageNumber()
    {
        Returns(Rows(25));
        var model = CreateModel("?PageNumber=1");
        model.Rbse = "R013";

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_UnknownRbse_StaysOnFirstPage()
    {
        Returns(Rows(25));
        var model = CreateModel();
        model.Rbse = "NOPE";

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_BlankSortColumn_FallsBackToRbse()
    {
        Returns(Rows(3).AsEnumerable().Reverse());
        var model = CreateModel();
        model.SortColumn = " ";

        await model.OnGetAsync();

        model.Cases.First().Rbse.Should().Be("R001");
    }

    [Fact]
    public async Task OnGet_UnknownSortColumn_DescendingByRbse()
    {
        Returns(Rows(3));
        var model = CreateModel();
        model.SortColumn = "Nope";
        model.SortDesc = true;

        await model.OnGetAsync();

        model.Cases.First().Rbse.Should().Be("R003");
    }

    public static IEnumerable<object[]> SortColumns() =>
    [
        .. new[]
        {
            "Survey", "Barcode", "AhfReference", "Fate", "RegionalLab", "FinalResult", "CaseWorkNotes",
            "FormADate", "RbseDate", "SlaughterDate", "ActiveMemoDate", "AnnexADate", "AnnexBDate",
            "PaperworkCompleteDate", "AnnexCDate", "AnnexDDate", "ReceivedByRegionalLabDate",
            "InitialReceivedDate", "FinalReceivedDate", "FinalSentDate", "LabChasedDate", "FinalResultDate",
            "BirthDate", "Post2000SentDate", "BarbMinuteSentDate", "DataCompleteDate"
        }.Select(c => new object[] { c })
    ];

    [Theory]
    [MemberData(nameof(SortColumns))]
    public async Task OnGet_SortByColumn_OrdersBothDirections(string column)
    {
        var prop = typeof(CaseWorkEntryRecord).GetProperty(column)!;
        CaseWorkEntryRecord Make(string rbse, int rank)
        {
            var r = new CaseWorkEntryRecord { Rbse = rbse };
            object value = prop.PropertyType == typeof(string) ? $"v{rank}" : new DateTime(2026, 1, rank);
            prop.SetValue(r, value);
            return r;
        }

        Returns([Make("B", 2), Make("A", 1)]);

        var asc = CreateModel();
        asc.SortColumn = column;
        await asc.OnGetAsync();
        asc.Cases.First().Rbse.Should().Be("A");

        var desc = CreateModel();
        desc.SortColumn = column;
        desc.SortDesc = true;
        await desc.OnGetAsync();
        desc.Cases.First().Rbse.Should().Be("B");
    }

    [Fact]
    public async Task OnGet_RbseColumnDescending_ReversesOrder()
    {
        Returns(Rows(3));
        var model = CreateModel();
        model.SortColumn = "Rbse";
        model.SortDesc = true;

        await model.OnGetAsync();

        model.Cases.First().Rbse.Should().Be("R003");
    }
}
