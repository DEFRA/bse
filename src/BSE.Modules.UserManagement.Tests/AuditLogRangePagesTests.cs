using BSE.Host.Pages.AuditLog;
using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class AuditLogRangePagesTests
{
    private static readonly DateTime Start = new(2024, 1, 1);
    private static readonly DateTime End = new(2024, 2, 1);

    private readonly IAuditLogService _audit = Substitute.For<IAuditLogService>();

    private static T WithContext<T>(T model, string? query) where T : PageModel
    {
        var http = new DefaultHttpContext();
        if (query is not null) http.Request.QueryString = new QueryString(query);
        model.PageContext = new PageContext { HttpContext = http };
        return model;
    }

    private static T E<T>(string table, int minute) where T : AuditLogEntry, new() => new()
    {
        TableName = table, FieldName = "F" + table, UserName = "U" + table, BeforeValue = "b", AfterValue = "a",
        Reason = "r", Key = "k" + table, DateTime = new DateTime(2024, 1, 1, 0, minute, 0)
    };

    // ── NewFarms ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Table")] [InlineData("Field")] [InlineData("User")] [InlineData("Before")]
    [InlineData("After")] [InlineData("Reason")] [InlineData("Key")] [InlineData("Owner")]
    [InlineData("Address")] [InlineData("County")] [InlineData("")]
    public async Task NewFarms_OnGet_SortsByEveryColumn(string column)
    {
        _audit.GetNewFarmsAsync(Start, End).Returns(new AuditLogEntry[] { E<AuditLogNewFarmEntry>("A", 1), E<AuditLogNewFarmEntry>("B", 2) });
        var model = WithContext(new NewFarmsModel(_audit), "?StartDate=x&EndDate=y");
        model.StartDate = Start; model.EndDate = End; model.SortColumn = column; model.SortDesc = true;

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.HasSearched.Should().BeTrue();
        model.TotalCount.Should().Be(2);
        model.TotalPages.Should().Be(1);
        model.PagedEntries().Should().HaveCount(2);
    }

    [Fact]
    public async Task NewFarms_OnGet_InvalidRange_SetsErrors()
    {
        var model = WithContext(new NewFarmsModel(_audit), "?StartDate=");

        await model.OnGetAsync();

        model.StartDateError.Should().NotBeNull();
        model.EndDateError.Should().NotBeNull();
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task NewFarms_OnGet_NoQuery_DoesNothing()
    {
        var model = WithContext(new NewFarmsModel(_audit), null);

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task NewFarms_Export()
    {
        var invalid = WithContext(new NewFarmsModel(_audit), null);
        (await invalid.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();

        _audit.GetNewFarmsAsync(Start, End).Returns(new AuditLogEntry[] { E<AuditLogNewFarmEntry>("A", 1) });
        var model = WithContext(new NewFarmsModel(_audit), null);
        model.StartDate = Start; model.EndDate = End;

        var file = (await model.OnGetExportAsync()).Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().StartWith("NewFarms_").And.EndWith(".xlsx");
        file.FileContents.Should().NotBeEmpty();
    }

    // ── CaseMoves ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Table")] [InlineData("Field")] [InlineData("User")] [InlineData("Before")]
    [InlineData("After")] [InlineData("Reason")] [InlineData("Key")] [InlineData("HasBatches")] [InlineData("")]
    public async Task CaseMoves_OnGet_SortsByEveryColumn(string column)
    {
        _audit.GetCaseMovesAsync(Start, End).Returns(new AuditLogEntry[] { E<AuditLogCaseMoveEntry>("A", 1), E<AuditLogCaseMoveEntry>("B", 2) });
        var model = WithContext(new CaseMovesModel(_audit), "?StartDate=x");
        model.StartDate = Start; model.EndDate = End; model.SortColumn = column;

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.PagedEntries().Should().HaveCount(2);
    }

    [Fact]
    public async Task CaseMoves_InvalidRangeAndExport()
    {
        var model = WithContext(new CaseMovesModel(_audit), "?EndDate=");
        await model.OnGetAsync();
        model.StartDateError.Should().NotBeNull();
        (await WithContext(new CaseMovesModel(_audit), null).OnGetAsync()).Should().BeOfType<PageResult>();
        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();

        _audit.GetCaseMovesAsync(Start, End).Returns(new AuditLogEntry[] { E<AuditLogCaseMoveEntry>("A", 1) });
        model.StartDate = Start; model.EndDate = End;
        (await model.OnGetExportAsync()).Should().BeOfType<FileContentResult>()
            .Which.FileDownloadName.Should().StartWith("CaseMoves_");
    }

    // ── CphhChanges ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("Table")] [InlineData("Field")] [InlineData("User")] [InlineData("Before")]
    [InlineData("After")] [InlineData("Reason")] [InlineData("Key")] [InlineData("Cases")] [InlineData("")]
    public async Task CphhChanges_OnGet_SortsByEveryColumn(string column)
    {
        _audit.GetCphhChangesAsync(Start, End).Returns(new AuditLogEntry[] { E<AuditLogCphhChangeEntry>("A", 1), E<AuditLogCphhChangeEntry>("B", 2) });
        var model = WithContext(new CphhChangesModel(_audit), "?StartDate=x");
        model.StartDate = Start; model.EndDate = End; model.SortColumn = column;

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.PagedEntries().Should().HaveCount(2);
    }

    [Fact]
    public async Task CphhChanges_InvalidRangeAndExport()
    {
        var model = WithContext(new CphhChangesModel(_audit), "?EndDate=");
        await model.OnGetAsync();
        model.EndDateError.Should().NotBeNull();
        (await WithContext(new CphhChangesModel(_audit), null).OnGetAsync()).Should().BeOfType<PageResult>();
        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();

        _audit.GetCphhChangesAsync(Start, End).Returns(new AuditLogEntry[] { E<AuditLogCphhChangeEntry>("A", 1) });
        model.StartDate = Start; model.EndDate = End;
        (await model.OnGetExportAsync()).Should().BeOfType<FileContentResult>()
            .Which.FileDownloadName.Should().StartWith("CphhChanges_");
    }

    // ── RbseChanges ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("Table")] [InlineData("Field")] [InlineData("User")] [InlineData("Before")]
    [InlineData("After")] [InlineData("Reason")] [InlineData("Key")] [InlineData("HasBatches")] [InlineData("")]
    public async Task RbseChanges_OnGet_SortsByEveryColumn(string column)
    {
        _audit.GetRbseChangesAsync(Start, End).Returns(new AuditLogEntry[] { E<AuditLogRbseChangeEntry>("A", 1), E<AuditLogRbseChangeEntry>("B", 2) });
        var model = WithContext(new RbseChangesModel(_audit), "?StartDate=x");
        model.StartDate = Start; model.EndDate = End; model.SortColumn = column;

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.PagedEntries().Should().HaveCount(2);
    }

    [Fact]
    public async Task RbseChanges_InvalidRangeAndExport()
    {
        var model = WithContext(new RbseChangesModel(_audit), "?EndDate=");
        await model.OnGetAsync();
        model.EndDateError.Should().NotBeNull();
        (await WithContext(new RbseChangesModel(_audit), null).OnGetAsync()).Should().BeOfType<PageResult>();
        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();

        _audit.GetRbseChangesAsync(Start, End).Returns(new AuditLogEntry[] { E<AuditLogRbseChangeEntry>("A", 1) });
        model.StartDate = Start; model.EndDate = End;
        (await model.OnGetExportAsync()).Should().BeOfType<FileContentResult>()
            .Which.FileDownloadName.Should().StartWith("RbseChanges_");
    }

    // ── ByFarm ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("User")] [InlineData("Table")] [InlineData("Field")] [InlineData("Key")]
    [InlineData("Before")] [InlineData("After")] [InlineData("Reason")] [InlineData("")]
    public async Task ByFarm_OnGet_NormalisesCphhAndSorts(string column)
    {
        _audit.GetByFarmAsync("12345678901").Returns(new[] { E<AuditLogEntry>("A", 1), E<AuditLogEntry>("B", 2) });
        var model = WithContext(new ByFarmModel(_audit), null);
        model.Cphh = " 12/345/6789/01 "; model.SortColumn = column; model.SortDesc = true;

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.PagedEntries().Should().HaveCount(2);
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task ByFarm_OnGet_BlankCphh_DoesNotSearch()
    {
        var model = WithContext(new ByFarmModel(_audit), null);

        await model.OnGetAsync();

        model.HasSearched.Should().BeFalse();
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task ByFarm_Export()
    {
        (await WithContext(new ByFarmModel(_audit), null).OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();

        _audit.GetByFarmAsync("12345678901").Returns(new[] { E<AuditLogEntry>("A", 1), E<AuditLogEntry>("B", 2) });
        var model = WithContext(new ByFarmModel(_audit), null);
        model.Cphh = "12/345/6789/01";

        var file = (await model.OnGetExportAsync()).Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().StartWith("FarmAuditLog_12345678901_");
    }

    // ── ByUser ───────────────────────────────────────────────────────────

    private static User U(int id, string name, bool active) =>
        new(id, name, null, name, null, active, 1, UserGroup.DataEntry);

    private ByUserModel CreateByUser(string? query)
    {
        var users = Substitute.For<IUserManagementService>();
        users.GetAllUsersAsync().Returns(new[] { U(2, "Zed", true), U(1, "Amy", true), U(3, "Off", false) });
        return WithContext(new ByUserModel(_audit, users), query);
    }

    [Fact]
    public async Task ByUser_OnGet_NoQuery_ListsActiveUsersOnly()
    {
        var model = CreateByUser(null);

        await model.OnGetAsync();

        model.Users.Select(u => u.Text).Should().Equal("Amy", "Zed");
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task ByUser_OnGet_NoUserAndNoDates_SetsAllErrors()
    {
        var model = CreateByUser("?UserId=0");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.ValidationError.Should().Be("Select a user.");
        model.StartDateError.Should().NotBeNull();
        model.EndDateError.Should().NotBeNull();
        model.HasSearched.Should().BeFalse();
    }

    [Theory]
    [InlineData("User")] [InlineData("Table")] [InlineData("Field")] [InlineData("Key")]
    [InlineData("Before")] [InlineData("After")] [InlineData("Reason")] [InlineData("")]
    public async Task ByUser_OnGet_Valid_SortsEntries(string column)
    {
        _audit.GetByUserAsync(Start, End, 2).Returns(new[] { E<AuditLogEntry>("A", 1), E<AuditLogEntry>("B", 2) });
        var model = CreateByUser("?UserId=2");
        model.UserId = 2; model.StartDate = Start; model.EndDate = End; model.SortColumn = column;

        await model.OnGetAsync();

        model.HasSearched.Should().BeTrue();
        model.PagedEntries().Should().HaveCount(2);
    }

    [Fact]
    public async Task ByUser_Export()
    {
        var model = CreateByUser(null);
        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();

        model.UserId = 2;
        (await model.OnGetExportAsync()).Should().BeOfType<RedirectToPageResult>();

        _audit.GetByUserAsync(Start, End, 2).Returns(new[] { E<AuditLogEntry>("A", 1) });
        model.StartDate = Start; model.EndDate = End;
        (await model.OnGetExportAsync()).Should().BeOfType<FileContentResult>()
            .Which.FileDownloadName.Should().StartWith("AuditLogByUser_");
    }
}
