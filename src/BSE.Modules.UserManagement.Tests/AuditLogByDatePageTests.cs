using BSE.Host.Pages.AuditLog;
using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public sealed class AuditLogByDatePageTests
{
    private readonly IAuditLogService _service = Substitute.For<IAuditLogService>();

    private ByDateModel Create(string? query = null)
    {
        var http = new DefaultHttpContext();
        if (query is not null) http.Request.QueryString = new QueryString(query);
        return new ByDateModel(_service) { PageContext = new PageContext { HttpContext = http } };
    }

    [Fact]
    public async Task OnGetAsync_WhenDateMissingFromQuery_DoesNothing()
    {
        var model = Create();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.HasSearched.Should().BeFalse();
        model.LogDateError.Should().BeNull();
        await _service.DidNotReceiveWithAnyArgs().GetByDateAsync(default);
    }

    [Fact]
    public async Task OnGetAsync_WhenDateIsBlank_ReportsValidationError()
    {
        var model = Create("?LogDate=");

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.LogDateError.Should().Be("Enter a date");
        model.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task OnGetAsync_WhenDateIsValid_LoadsAndSortsEntries()
    {
        var model = Create("?LogDate=2024-05-10");
        model.LogDate = new DateTime(2024, 5, 10);
        model.SortColumn = "User";
        model.SortDesc = true;
        _service.GetByDateAsync(new DateTime(2024, 5, 10)).Returns(new[]
        {
            new AuditLogEntry { UserName = "A", TableName = "t1", FieldName = "f1", Key = "k1", BeforeValue = "1", AfterValue = "2", Reason = "r1", DateTime = new DateTime(2024, 5, 10, 9, 0, 0) },
            new AuditLogEntry { UserName = "B", TableName = "t2", FieldName = "f2", Key = "k2", BeforeValue = "3", AfterValue = "4", Reason = "r2", DateTime = new DateTime(2024, 5, 10, 10, 0, 0) }
        });

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.HasSearched.Should().BeTrue();
        model.PagedEntries().Should().HaveCount(2);
        model.PagedEntries().First().UserName.Should().Be("B");
    }

    [Fact]
    public async Task OnGetExportAsync_WithoutDate_RedirectsToPage()
    {
        var model = Create();

        var result = await model.OnGetExportAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnGetExportAsync_WithDate_ReturnsWorkbook()
    {
        var model = Create("?LogDate=2024-05-10");
        model.LogDate = new DateTime(2024, 5, 10);
        _service.GetByDateAsync(new DateTime(2024, 5, 10)).Returns(new[]
        {
            new AuditLogEntry { UserName = "A", TableName = "t1", FieldName = "f1", Key = "k1", BeforeValue = "1", AfterValue = "2", Reason = "r1", DateTime = new DateTime(2024, 5, 10, 9, 0, 0) }
        });

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("DailyAuditLog_20240510.xlsx");
        file.FileContents.Should().NotBeEmpty();
    }
}
