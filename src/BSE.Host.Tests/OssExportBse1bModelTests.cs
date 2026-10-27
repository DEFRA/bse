using BSE.Host.Pages.OssExport;
using BSE.Modules.OssExport.Models;
using BSE.Modules.OssExport.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public class OssExportBse1bModelTests
{
    private readonly IOssExportService _service = Substitute.For<IOssExportService>();
    private readonly TestSession _session = new();

    private OssExportBse1bModel CreateModel(string query = "")
    {
        var http = new DefaultHttpContext();
        http.Features.Set<ISessionFeature>(new TestSessionFeature(_session));
        http.Request.QueryString = new QueryString(query);
        return new OssExportBse1bModel(_service) { PageContext = new PageContext { HttpContext = http } };
    }

    private sealed class TestSessionFeature(ISession session) : ISessionFeature
    {
        public ISession Session { get; set; } = session;
    }

    private void ServiceKnowsRbse(string cphh = "01/234/5678/90")
    {
        _service.ValidateAndGetRbseDetailsAsync(Arg.Any<string>())
            .Returns(Task.FromResult<OssExportRecord?>(new OssExportRecord { Cphh = cphh, OwnerName = "Owner", Address1 = "Addr" }));
        _service.CreateBatchNumber1989Async()
            .Returns(Task.FromResult<BatchNumber1989Result?>(new BatchNumber1989Result(5, 2026, 7)));
    }

    private async Task AddAsync(OssExportBse1bModel model, string rbse)
    {
        model.RbseInput = rbse;
        await model.OnPostAddToGridAsync();
    }

    [Fact]
    public async Task OnGet_Fresh_ClearsGrid()
    {
        var model = CreateModel();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.IsGridEmpty.Should().BeTrue();
        model.BatchResult.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_WithPageNumber_ClampsToTotalPages()
    {
        ServiceKnowsRbse();
        var model = CreateModel();
        await AddAsync(model, "002600001");

        var next = CreateModel("?PageNumber=99");
        next.PageNumber = 99;
        await next.OnGetAsync();

        next.GridEntries.Should().ContainSingle();
        next.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnPostPage_BelowOne_ResetsToOne()
    {
        var model = CreateModel();
        model.PageNumber = -3;

        await model.OnPostPageAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task AddToGrid_Blank_ShowsMessage()
    {
        var model = CreateModel();

        await AddAsync(model, " ");

        model.ValidationMessage.Should().Contain("enter an RBSE");
    }

    [Fact]
    public async Task AddToGrid_InvalidFormat_ShowsMessage()
    {
        var model = CreateModel();

        await AddAsync(model, "12");

        model.ValidationMessage.Should().Contain("valid RBSE");
    }

    [Fact]
    public async Task AddToGrid_NotFound_ShowsMessage()
    {
        _service.ValidateAndGetRbseDetailsAsync(Arg.Any<string>()).Returns(Task.FromResult<OssExportRecord?>(null));
        var model = CreateModel();

        await AddAsync(model, "002600001");

        model.ValidationMessage.Should().Contain("not found");
        model.GridEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToGrid_BatchCreationFails_ShowsMessage()
    {
        ServiceKnowsRbse();
        _service.CreateBatchNumber1989Async().Returns(Task.FromResult<BatchNumber1989Result?>(null));
        var model = CreateModel();

        await AddAsync(model, "002600001");

        model.ValidationMessage.Should().Be("Failed to create batch number.");
    }

    [Fact]
    public async Task AddToGrid_Valid_AddsAndPersistsRow()
    {
        ServiceKnowsRbse();
        var model = CreateModel();

        await AddAsync(model, "002600001");

        model.GridEntries.Should().ContainSingle(e => e.Rbse == "002600001" && e.BatchId == 5);
        model.ValidationMessage.Should().BeNull();
        model.TotalPages.Should().Be(1);
        model.PagedGridEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddToGrid_Duplicate_ShowsMessage()
    {
        ServiceKnowsRbse();
        var model = CreateModel();
        await AddAsync(model, "002600001");

        var again = CreateModel();
        await AddAsync(again, "002600001");

        again.ValidationMessage.Should().Contain("already in the table");
    }

    [Fact]
    public async Task Delete_LastRow_ClearsBatch()
    {
        ServiceKnowsRbse();
        var model = CreateModel();
        await AddAsync(model, "002600001");

        var deleter = CreateModel();
        await deleter.OnPostDeleteAsync(1);

        deleter.GridEntries.Should().BeEmpty();
        deleter.BatchResult.Should().BeNull();
    }

    [Fact]
    public async Task Delete_UnknownId_LeavesGrid()
    {
        ServiceKnowsRbse();
        var model = CreateModel();
        await AddAsync(model, "002600001");

        var deleter = CreateModel();
        await deleter.OnPostDeleteAsync(99);

        deleter.GridEntries.Should().ContainSingle();
    }

    [Fact]
    public async Task Export_EmptyGrid_ShowsMessage()
    {
        var model = CreateModel();

        var result = await model.OnPostExportAsync();

        result.Should().BeOfType<PageResult>();
        model.ValidationMessage.Should().Contain("grid is empty");
    }

    [Fact]
    public async Task Export_WithRows_ReturnsNamedFile()
    {
        ServiceKnowsRbse();
        _service.GenerateOssExportFileAsync(5, 2026, 7, Arg.Any<IEnumerable<OssExportBatchEntryRecord>>())
            .Returns(Task.FromResult("content"));
        var model = CreateModel();
        await AddAsync(model, "002600001");

        var result = await CreateModel().OnPostExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("EPBSE_26.007");
        System.Text.Encoding.ASCII.GetString(file.FileContents).Should().Be("content");
    }

    [Fact]
    public async Task Export_ServiceThrows_ShowsMessage()
    {
        ServiceKnowsRbse();
        _service.GenerateOssExportFileAsync(Arg.Any<int>(), Arg.Any<short>(), Arg.Any<int>(), Arg.Any<IEnumerable<OssExportBatchEntryRecord>>())
            .Returns(Task.FromException<string>(new InvalidOperationException("bad")));
        await AddAsync(CreateModel(), "002600001");
        var model = CreateModel();

        var result = await model.OnPostExportAsync();

        result.Should().BeOfType<PageResult>();
        model.ValidationMessage.Should().Be("Export failed: bad");
    }

    [Theory]
    [InlineData("Cphh", false)]
    [InlineData("OwnerName", true)]
    [InlineData("Address1", false)]
    [InlineData("Rbse", true)]
    public async Task PagedGridEntries_SortsByColumn(string column, bool desc)
    {
        ServiceKnowsRbse();
        await AddAsync(CreateModel(), "002600001");
        await AddAsync(CreateModel(), "002600002");
        var model = CreateModel();
        await model.OnPostPageAsync();
        model.SortColumn = column;
        model.SortDesc = desc;

        model.PagedGridEntries.Should().HaveCount(2);
    }
}
