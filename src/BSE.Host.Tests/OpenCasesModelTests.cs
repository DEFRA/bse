using BSE.Host.Pages.CaseWork;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;

namespace BSE.Host.Tests;

public class OpenCasesModelTests
{
    private static CaseWorkEntryRecord Case(string rbse) => new() { Rbse = rbse };

    private static OpenCasesModel CreateModel(int count)
    {
        var service = Substitute.For<ICaseWorkService>();
        IEnumerable<CaseWorkEntryRecord> cases = Enumerable.Range(1, count)
            .Select(i => Case($"{i:000000000}"))
            .ToList();
        service.GetOpenCasesAsync().Returns(Task.FromResult(cases));
        return new OpenCasesModel(service);
    }

    [Fact]
    public async Task GetPagedCases_FirstPage_ReturnsTenRows()
    {
        var model = CreateModel(25);

        await model.OnGetAsync();

        model.TotalCount.Should().Be(25);
        model.TotalPages.Should().Be(3);
        model.GetPagedCases().Should().HaveCount(10);
    }

    [Fact]
    public async Task GetPagedCases_LastPage_ReturnsRemainder()
    {
        var model = CreateModel(25);
        model.PageNumber = 3;

        await model.OnGetAsync();

        model.GetPagedCases().Should().HaveCount(5);
    }

    [Fact]
    public async Task OnGetAsync_PageNumberAboveTotal_ClampsToLastPage()
    {
        var model = CreateModel(25);
        model.PageNumber = 99;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(3);
    }

    [Fact]
    public async Task OnGetAsync_PageNumberBelowOne_ClampsToFirstPage()
    {
        var model = CreateModel(25);
        model.PageNumber = 0;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGetAsync_WithRbse_JumpsToPageContainingRow()
    {
        var model = CreateModel(25);
        model.Rbse = "000000015";

        await model.OnGetAsync();

        model.PageNumber.Should().Be(2);
        model.GetPagedCases().Should().Contain(c => c.Rbse == "000000015");
    }

    [Fact]
    public async Task OnGetAsync_SortDescending_ReturnsHighestRbseFirst()
    {
        var model = CreateModel(25);
        model.SortDesc = true;

        await model.OnGetAsync();

        model.GetPagedCases()[0].Rbse.Should().Be("000000025");
    }

    [Fact]
    public async Task OnGetAsync_NoCases_ReportsSinglePageAndNoRows()
    {
        var model = CreateModel(0);

        await model.OnGetAsync();

        model.TotalPages.Should().Be(1);
        model.GetPagedCases().Should().BeEmpty();
    }
}
