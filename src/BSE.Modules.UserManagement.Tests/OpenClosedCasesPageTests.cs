using BSE.Host.Pages.CaseWork;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

internal static class CaseWorkRecordFixtures
{
    public static readonly string[] CommonColumns =
    [
        "Rbse", "Survey", "Barcode", "AhfReference", "FormADate", "RbseDate", "SlaughterDate", "Fate",
        "ActiveMemoDate", "AnnexADate", "AnnexBDate", "PaperworkCompleteDate", "AnnexCDate", "AnnexDDate",
        "RegionalLab", "ReceivedByRegionalLabDate", "InitialReceivedDate", "FinalReceivedDate", "FinalSentDate",
        "LabChasedDate", "FinalResult", "FinalResultDate", "BirthDate", "Post2000SentDate",
        "BarbMinuteSentDate", "DataCompleteDate", "CaseWorkNotes", "SomethingUnknown", ""
    ];

    public static readonly string[] OpenOnlyColumns =
    [
        "ActiveMemoDueDate", "AnnexADueDate", "AnnexBDueDate", "AnnexCDueDate", "AnnexDDueDate",
        "LabChaseDueDate", "BarbMemoDue"
    ];

    public static CaseWorkEntryRecord Record(int i) => new()
    {
        Rbse = $"00/01/{i:00000}",
        Survey = $"Survey{i}",
        Barcode = $"BC{i}",
        AhfReference = $"AHF{i}",
        FormADate = new DateTime(2022, 1, 1).AddDays(i),
        RbseDate = new DateTime(2022, 2, 1).AddDays(i),
        SlaughterDate = new DateTime(2022, 3, 1).AddDays(i),
        Fate = $"Fate{i}",
        ActiveMemoDate = new DateTime(2022, 4, 1).AddDays(i),
        AnnexADate = new DateTime(2022, 5, 1).AddDays(i),
        AnnexBDate = new DateTime(2022, 6, 1).AddDays(i),
        PaperworkCompleteDate = new DateTime(2022, 7, 1).AddDays(i),
        AnnexCDate = new DateTime(2022, 8, 1).AddDays(i),
        AnnexDDate = new DateTime(2022, 9, 1).AddDays(i),
        RegionalLab = $"Lab{i}",
        ReceivedByRegionalLabDate = new DateTime(2022, 10, 1).AddDays(i),
        InitialReceivedDate = new DateTime(2022, 11, 1).AddDays(i),
        FinalReceivedDate = new DateTime(2022, 12, 1).AddDays(i),
        FinalSentDate = new DateTime(2023, 1, 1).AddDays(i),
        LabChasedDate = new DateTime(2023, 2, 1).AddDays(i),
        FinalResult = $"Result{i}",
        FinalResultDate = new DateTime(2023, 3, 1).AddDays(i),
        BirthDate = new DateTime(2019, 1, 1).AddDays(i),
        Post2000SentDate = new DateTime(2023, 4, 1).AddDays(i),
        BarbMinuteSentDate = new DateTime(2023, 5, 1).AddDays(i),
        DataCompleteDate = new DateTime(2023, 6, 1).AddDays(i),
        CaseWorkNotes = $"Notes{i}",
        ActiveMemoDueDate = new DateTime(2023, 7, 1).AddDays(i),
        AnnexADueDate = new DateTime(2023, 8, 1).AddDays(i),
        AnnexBDueDate = new DateTime(2023, 9, 1).AddDays(i),
        AnnexCDueDate = new DateTime(2023, 10, 1).AddDays(i),
        AnnexDDueDate = new DateTime(2023, 11, 1).AddDays(i),
        LabChaseDueDate = new DateTime(2023, 12, 1).AddDays(i),
        BarbMemoDue = i % 2 == 0 ? "Yes" : null
    };

    public static List<CaseWorkEntryRecord> Records(int count) =>
        Enumerable.Range(1, count).Select(Record).ToList();
}

public class OpenCasesModelTests
{
    private readonly ICaseWorkService _service = Substitute.For<ICaseWorkService>();

    private OpenCasesModel Create(string? query = null)
    {
        var http = new DefaultHttpContext();
        if (query is not null) http.Request.QueryString = new QueryString(query);
        return new OpenCasesModel(_service) { PageContext = new PageContext { HttpContext = http } };
    }

    [Fact]
    public async Task OnGet_NoCases_ReportsSinglePage()
    {
        _service.GetOpenCasesAsync().Returns(Array.Empty<CaseWorkEntryRecord>());
        var model = Create();

        await model.OnGetAsync();

        model.TotalCount.Should().Be(0);
        model.TotalPages.Should().Be(1);
        model.PagedCases.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_PagesTenAtATime()
    {
        _service.GetOpenCasesAsync().Returns(CaseWorkRecordFixtures.Records(25));
        var model = Create("?PageNumber=2");
        model.PageNumber = 2;

        await model.OnGetAsync();

        model.TotalPages.Should().Be(3);
        model.PagedCases.Should().HaveCount(10);
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(99, 3)]
    public async Task OnGet_PageNumber_IsClamped(int requested, int expected)
    {
        _service.GetOpenCasesAsync().Returns(CaseWorkRecordFixtures.Records(25));
        var model = Create("?PageNumber=1");
        model.PageNumber = requested;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(expected);
    }

    [Fact]
    public async Task OnGet_WithRbseAndNoPageInQuery_JumpsToPageContainingRow()
    {
        _service.GetOpenCasesAsync().Returns(CaseWorkRecordFixtures.Records(25));
        var model = Create();
        model.Rbse = "00/01/00015";

        await model.OnGetAsync();

        model.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task OnGet_WithRbseAndExplicitPage_KeepsRequestedPage()
    {
        _service.GetOpenCasesAsync().Returns(CaseWorkRecordFixtures.Records(25));
        var model = Create("?PageNumber=1");
        model.Rbse = "00/01/00015";

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_UnknownRbse_StaysOnFirstPage()
    {
        _service.GetOpenCasesAsync().Returns(CaseWorkRecordFixtures.Records(25));
        var model = Create();
        model.Rbse = "99/99/99999";

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_SortsByEveryColumnInBothDirections()
    {
        _service.GetOpenCasesAsync().Returns(CaseWorkRecordFixtures.Records(4));
        var columns = CaseWorkRecordFixtures.CommonColumns.Concat(CaseWorkRecordFixtures.OpenOnlyColumns);

        foreach (var column in columns)
        {
            foreach (var desc in new[] { false, true })
            {
                var model = Create("?PageNumber=1");
                model.SortColumn = column;
                model.SortDesc = desc;

                await model.OnGetAsync();

                model.Cases.Should().HaveCount(4, $"{column} desc={desc}");
            }
        }
    }

    [Fact]
    public async Task OnGet_SortByRbseDescending_PutsHighestFirst()
    {
        _service.GetOpenCasesAsync().Returns(CaseWorkRecordFixtures.Records(3));
        var model = Create("?PageNumber=1");
        model.SortDesc = true;

        await model.OnGetAsync();

        model.Cases.First().Rbse.Should().Be("00/01/00003");
    }
}

public class ClosedCasesModelTests
{
    private readonly ICaseWorkService _service = Substitute.For<ICaseWorkService>();

    private ClosedCasesModel Create(string? query = null)
    {
        var http = new DefaultHttpContext();
        if (query is not null) http.Request.QueryString = new QueryString(query);
        return new ClosedCasesModel(_service) { PageContext = new PageContext { HttpContext = http } };
    }

    [Fact]
    public async Task OnGet_NoCases_ReportsSinglePage()
    {
        _service.GetClosedCasesAsync().Returns(Array.Empty<CaseWorkEntryRecord>());
        var model = Create();

        await model.OnGetAsync();

        model.TotalPages.Should().Be(1);
        model.PagedCases.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 2)]
    public async Task OnGet_PageNumber_IsClamped(int requested, int expected)
    {
        _service.GetClosedCasesAsync().Returns(CaseWorkRecordFixtures.Records(14));
        var model = Create("?PageNumber=1");
        model.PageNumber = requested;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(expected);
    }

    [Fact]
    public async Task OnGet_WithRbseAndNoPageInQuery_JumpsToPageContainingRow()
    {
        _service.GetClosedCasesAsync().Returns(CaseWorkRecordFixtures.Records(25));
        var model = Create();
        model.Rbse = "00/01/00021";

        await model.OnGetAsync();

        model.PageNumber.Should().Be(3);
    }

    [Fact]
    public async Task OnGet_SortsByEveryColumnInBothDirections()
    {
        _service.GetClosedCasesAsync().Returns(CaseWorkRecordFixtures.Records(4));

        foreach (var column in CaseWorkRecordFixtures.CommonColumns)
        {
            foreach (var desc in new[] { false, true })
            {
                var model = Create("?PageNumber=1");
                model.SortColumn = column;
                model.SortDesc = desc;

                await model.OnGetAsync();

                model.Cases.Should().HaveCount(4, $"{column} desc={desc}");
            }
        }
    }

    [Fact]
    public async Task OnGet_PagedCases_ReturnsRequestedSlice()
    {
        _service.GetClosedCasesAsync().Returns(CaseWorkRecordFixtures.Records(13));
        var model = Create("?PageNumber=2");
        model.PageNumber = 2;

        await model.OnGetAsync();

        model.PagedCases.Should().HaveCount(3);
    }
}
