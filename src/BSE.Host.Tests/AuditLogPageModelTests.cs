using System.Text;
using BSE.Host.Pages.AuditLog;
using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Services;
using BSE.SharedKernel;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class AuditLogPageModelTests
{
    private readonly IAuditLogService _audit = Substitute.For<IAuditLogService>();
    private readonly IUserManagementService _users = Substitute.For<IUserManagementService>();

    private static List<AuditLogEntry> Entries(int count) =>
        Enumerable.Range(1, count).Select(i => new AuditLogEntry
        {
            Id = i,
            TableName = $"T{i:00}",
            FieldName = $"F{i:00}",
            DateTime = new DateTime(2024, 1, 1).AddDays(i),
            UserName = $"U{i:00}",
            BeforeValue = i == 1 ? "a,b" : $"B{i:00}",
            AfterValue = $"A{i:00}",
            Reason = $"R{i:00}",
            Key = $"K{i:00}"
        }).ToList();

    // ---------- ByCase ----------

    private ByCaseModel CreateByCase(string rbse = "00/26/00001")
    {
        var model = new ByCaseModel(_audit) { Rbse = rbse };
        PageTestContext.Attach(model);
        return model;
    }

    [Fact]
    public async Task ByCase_OnGet_BlankRbse_RedirectsHome()
    {
        var result = await CreateByCase("").OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task ByCase_OnGet_PagesAndClamps()
    {
        _audit.GetByCaseAsync("00/26/00001").Returns(Entries(45));
        var model = CreateByCase();
        model.PageNumber = 99;

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.HasSearched.Should().BeTrue();
        model.TotalCount.Should().Be(45);
        model.TotalPages.Should().Be(3);
        model.PageNumber.Should().Be(3);
        model.Entries.Should().HaveCount(5);
    }

    [Fact]
    public async Task ByCase_OnGet_PageNumberBelowOne_IsClamped()
    {
        _audit.GetByCaseAsync(Arg.Any<string>()).Returns(Entries(3));
        var model = CreateByCase();
        model.PageNumber = -1;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task ByCase_OnGet_NoEntries_HasZeroPages()
    {
        _audit.GetByCaseAsync(Arg.Any<string>()).Returns(new List<AuditLogEntry>());
        var model = CreateByCase();

        await model.OnGetAsync();

        model.TotalPages.Should().Be(0);
        model.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData("table", "asc", "T01")]
    [InlineData("table", "desc", "T03")]
    [InlineData("field", "desc", "T03")]
    [InlineData("user", "asc", "T01")]
    [InlineData("before", "desc", "T03")]
    [InlineData("after", "asc", "T01")]
    [InlineData("reason", "desc", "T03")]
    [InlineData("key", "asc", "T01")]
    [InlineData("datetime", "desc", "T03")]
    [InlineData("other", "asc", "T01")]
    public async Task ByCase_OnGet_Sorts(string sortBy, string dir, string firstTable)
    {
        _audit.GetByCaseAsync(Arg.Any<string>()).Returns(Entries(3));
        var model = CreateByCase();
        model.SortBy = sortBy;
        model.SortDir = dir;

        await model.OnGetAsync();

        model.Entries.First().TableName.Should().Be(firstTable);
    }

    [Fact]
    public async Task ByCase_Export_BlankRbse_Redirects()
    {
        (await CreateByCase("").OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task ByCase_Export_ReturnsCsvWithEscaping()
    {
        _audit.GetByCaseAsync("00/26/00001").Returns(Entries(2));
        var model = CreateByCase(" 00/26/00001 ");
        model.SortDir = "asc";

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("text/csv");
        file.FileDownloadName.Should().StartWith("AuditLog_00/26/00001_").And.EndWith(".csv");
        var text = Encoding.UTF8.GetString(file.FileContents).TrimStart('\uFEFF');
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        lines[0].Should().Be("Table,Field,Date/Time,User,Before,After,Reason,Key");
        lines[1].Should().Contain("\"a,b\"");
        lines.Should().HaveCount(3);
    }

    [Fact]
    public void ByCase_SortUrl_TogglesDirectionAndIncludesReturnTo()
    {
        var model = CreateByCase("00/26/00001");
        model.SortBy = "table";
        model.SortDir = "asc";
        model.ReturnTo = "/Case/Defra?x=1";

        model.SortUrl("table").Should().Contain("sortDir=desc").And.Contain("returnTo=%2FCase%2FDefra%3Fx%3D1").And.Contain("rbse=00%2F26%2F00001");
        model.SortUrl("field").Should().Contain("sortDir=asc");
    }

    [Fact]
    public void ByCase_SortUrl_WithoutReturnTo_OmitsIt()
    {
        CreateByCase().SortUrl("user").Should().NotContain("returnTo");
    }

    // ---------- ByFarm ----------

    private ByFarmModel CreateByFarm(string cphh = "01/001/0001/01")
    {
        var model = new ByFarmModel(_audit) { Cphh = cphh };
        PageTestContext.Attach(model);
        return model;
    }

    [Fact]
    public async Task ByFarm_OnGet_BlankCphh_ReturnsPageWithoutSearch()
    {
        var model = CreateByFarm("");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.HasSearched.Should().BeFalse();
        await _audit.DidNotReceiveWithAnyArgs().GetByFarmAsync(default!);
    }

    [Fact]
    public async Task ByFarm_OnGet_NormalisesCphhAndPages()
    {
        _audit.GetByFarmAsync("01001000101").Returns(Entries(12));
        var model = CreateByFarm();
        model.PageNumber = 2;

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.TotalCount.Should().Be(12);
        model.TotalPages.Should().Be(2);
        model.GetPagedEntries().Should().HaveCount(2);
    }

    [Fact]
    public async Task ByFarm_OnGet_NoEntries_HasOnePage()
    {
        _audit.GetByFarmAsync(Arg.Any<string>()).Returns(new List<AuditLogEntry>());
        var model = CreateByFarm();

        await model.OnGetAsync();

        model.TotalPages.Should().Be(1);
    }

    [Theory]
    [InlineData("User", false, "T01")]
    [InlineData("Table", true, "T03")]
    [InlineData("Field", true, "T03")]
    [InlineData("Key", false, "T01")]
    [InlineData("Before", false, "T01")]
    [InlineData("After", true, "T03")]
    [InlineData("Reason", false, "T01")]
    [InlineData("", true, "T03")]
    public async Task ByFarm_OnGet_Sorts(string column, bool desc, string first)
    {
        _audit.GetByFarmAsync(Arg.Any<string>()).Returns(Entries(3));
        var model = CreateByFarm();
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.GetPagedEntries()[0].TableName.Should().Be(first);
    }

    [Fact]
    public async Task ByFarm_Export_BlankCphh_Redirects()
    {
        (await CreateByFarm(" ").OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task ByFarm_Export_ReturnsWorkbook()
    {
        _audit.GetByFarmAsync("01001000101").Returns(Entries(3));
        var model = CreateByFarm();
        model.SortColumn = "Table";
        model.SortDesc = true;

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().StartWith("FarmAuditLog_01001000101_").And.EndWith(".xlsx");
        using var wb = new XLWorkbook(new MemoryStream(file.FileContents));
        var ws = wb.Worksheet("Farm Audit Log");
        ws.Cell(1, 1).GetString().Should().Be("TableName");
        ws.Cell(2, 1).GetString().Should().Be("T03");
        ws.LastRowUsed()!.RowNumber().Should().Be(4);
    }

    // ---------- ByUser ----------

    private ByUserModel CreateByUser(string? query = null)
    {
        _users.GetAllUsersAsync().Returns(new[]
        {
            new User(2, "b", null, "Zed", null, true, 2, UserGroup.DataEntry),
            new User(1, "a", null, "Amy", null, true, 2, UserGroup.DataEntry),
            new User(3, "c", null, "Gone", null, false, 2, UserGroup.DataEntry)
        }.AsEnumerable());
        var model = new ByUserModel(_audit, _users);
        PageTestContext.Attach(model);
        if (query is not null) model.HttpContext.Request.QueryString = new QueryString(query);
        return model;
    }

    [Fact]
    public async Task ByUser_OnGet_NoQuery_ListsActiveUsersSorted_NoSearch()
    {
        var model = CreateByUser();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Users.Select(u => u.Text).Should().Equal("Amy", "Zed");
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task ByUser_OnGet_MissingEverything_SetsAllErrors()
    {
        var model = CreateByUser("?UserId=0");

        await model.OnGetAsync();

        model.ValidationError.Should().Be("Select a user.");
        model.StartDateError.Should().NotBeNull();
        model.EndDateError.Should().NotBeNull();
        model.HasSearched.Should().BeFalse();
        await _audit.DidNotReceiveWithAnyArgs().GetByUserAsync(default, default, default);
    }

    [Fact]
    public async Task ByUser_OnGet_StartAfterEnd_SetsDateErrors()
    {
        var model = CreateByUser("?UserId=1");
        model.UserId = 1;
        model.StartDate = new DateTime(2024, 2, 1);
        model.EndDate = new DateTime(2024, 1, 1);

        await model.OnGetAsync();

        model.ValidationError.Should().BeNull();
        model.StartDateError.Should().Contain("start date");
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task ByUser_OnGet_Valid_SearchesAndPages()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 2, 1);
        _audit.GetByUserAsync(start, end, 1).Returns(Entries(11));
        var model = CreateByUser("?UserId=1");
        model.UserId = 1;
        model.StartDate = start;
        model.EndDate = end;
        model.SortColumn = "Table";
        model.SortDesc = true;
        model.PageNumber = 2;

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.TotalCount.Should().Be(11);
        model.TotalPages.Should().Be(2);
        model.GetPagedEntries().Should().ContainSingle().Which.TableName.Should().Be("T01");
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Field")]
    [InlineData("Key")]
    [InlineData("Before")]
    [InlineData("After")]
    [InlineData("Reason")]
    [InlineData("")]
    public async Task ByUser_OnGet_AllSortColumns(string column)
    {
        _audit.GetByUserAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 1).Returns(Entries(3));
        var model = CreateByUser("?UserId=1");
        model.UserId = 1;
        model.StartDate = new DateTime(2024, 1, 1);
        model.EndDate = new DateTime(2024, 2, 1);
        model.SortColumn = column;

        await model.OnGetAsync();

        model.GetPagedEntries().Should().HaveCount(3);
    }

    [Fact]
    public async Task ByUser_Export_NoUser_Redirects()
    {
        var model = CreateByUser();
        model.StartDate = new DateTime(2024, 1, 1);
        model.EndDate = new DateTime(2024, 2, 1);

        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task ByUser_Export_InvalidDates_Redirects()
    {
        var model = CreateByUser();
        model.UserId = 1;

        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task ByUser_Export_Valid_ReturnsWorkbook()
    {
        _audit.GetByUserAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 1).Returns(Entries(2));
        var model = CreateByUser();
        model.UserId = 1;
        model.StartDate = new DateTime(2024, 1, 1);
        model.EndDate = new DateTime(2024, 2, 1);

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().StartWith("AuditLogByUser_").And.EndWith(".xlsx");
        using var wb = new XLWorkbook(new MemoryStream(file.FileContents));
        wb.Worksheet("Audit Log By User").LastRowUsed()!.RowNumber().Should().Be(3);
    }
}
