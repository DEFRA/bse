using System.Text;
using System.Text.Json;
using BSE.Host.Pages.OssExport;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Services;
using BSE.Modules.OssExport.Models;
using BSE.Modules.OssExport.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class OssExportBse1ModelTests
{
    private readonly IBatchService _batches = Substitute.For<IBatchService>();

    private OssExportBse1Model Create()
    {
        var model = new OssExportBse1Model(_batches);
        PageTestHelpers.Attach(model, "VLAAccess");
        return model;
    }

    [Fact]
    public void OnGet_DoesNothing()
    {
        Create().OnGet();
    }

    [Fact]
    public async Task Download_MissingBatchNumber_ReturnsPageWithError()
    {
        var model = Create();

        (await model.OnPostDownloadAsync()).Should().BeOfType<PageResult>();
        model.ModelState.ContainsKey("BatchNumber").Should().BeTrue();
    }

    [Fact]
    public async Task Download_InvalidModelState_ReturnsPage()
    {
        var model = Create();
        model.BatchNumber = 5;
        model.ModelState.AddModelError("BatchYear", "bad");

        (await model.OnPostDownloadAsync()).Should().BeOfType<PageResult>();
        await _batches.DidNotReceiveWithAnyArgs().GetBatchIdAsync(default, default);
    }

    [Theory]
    [InlineData(null, "Batch 7 was not found.")]
    [InlineData((short)2024, "Batch 2024/7 was not found.")]
    public async Task Download_BatchNotFound_SetsMessage(short? year, string expected)
    {
        _batches.GetBatchIdAsync(year, 7).Returns((int?)null);
        var model = Create();
        model.BatchYear = year;
        model.BatchNumber = 7;

        (await model.OnPostDownloadAsync()).Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be(expected);
    }

    [Fact]
    public async Task Download_NoCases_SetsMessage()
    {
        _batches.GetBatchIdAsync((short)2024, 7).Returns(11);
        _batches.GetCasesByBatchIdAsync(11).Returns(Array.Empty<BatchCaseSummaryRecord>());
        var model = Create();
        model.BatchYear = 2024;
        model.BatchNumber = 7;

        await model.OnPostDownloadAsync();

        model.ErrorMessage.Should().Be("Batch 2024/7 contains no cases.");
    }

    [Fact]
    public async Task Download_WithCases_ReturnsLegacyFile()
    {
        _batches.GetBatchIdAsync((short)2024, 1007).Returns(11);
        _batches.GetCasesByBatchIdAsync(11).Returns(new[] { new BatchCaseSummaryRecord("002600001", "01001000101"), new BatchCaseSummaryRecord("002600002", null) });
        var model = Create();
        model.BatchYear = 2024;
        model.BatchNumber = 1007;

        var file = (await model.OnPostDownloadAsync()).Should().BeOfType<FileContentResult>().Subject;

        file.FileDownloadName.Should().Be("EPBSE_24.007");
        Encoding.UTF8.GetString(file.FileContents).Should().Contain("|24007|002600001|01001000101|").And.Contain("|24007|002600002||");
    }

    [Fact]
    public async Task Download_BlankYear_UsesCurrentYearInFileName()
    {
        _batches.GetBatchIdAsync(null, 3).Returns(1);
        _batches.GetCasesByBatchIdAsync(1).Returns(new[] { new BatchCaseSummaryRecord("002600001", "x") });
        var model = Create();
        model.BatchNumber = 3;

        var file = (await model.OnPostDownloadAsync()).Should().BeOfType<FileContentResult>().Subject;

        file.FileDownloadName.Should().Be($"EPBSE_{DateTime.Today.Year % 100:00}.003");
    }

    [Fact]
    public async Task Download_ServiceThrows_SetsExportFailed()
    {
        _batches.GetBatchIdAsync(Arg.Any<short?>(), Arg.Any<int>()).Returns<int?>(_ => throw new InvalidOperationException("boom"));
        var model = Create();
        model.BatchNumber = 3;

        (await model.OnPostDownloadAsync()).Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be("Export failed: boom");
    }
}

public sealed class OssExportBse1bGridTests
{
    private sealed class Session : ISession
    {
        private readonly Dictionary<string, byte[]> _store = [];
        public IEnumerable<string> Keys => _store.Keys;
        public string Id => "t";
        public bool IsAvailable => true;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    private readonly IOssExportService _service = Substitute.For<IOssExportService>();

    private OssExportBse1bModel Create(string? query = null)
    {
        var http = new DefaultHttpContext { User = PageTestHelpers.Principal("VLAAccess"), Session = new Session() };
        if (query is not null) http.Request.QueryString = new QueryString(query);
        var model = new OssExportBse1bModel(_service)
        {
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.Url = Substitute.For<IUrlHelper>();
        model.Url.ActionContext.Returns(new ActionContext(http, new RouteData(), new ActionDescriptor()));
        return model;
    }

    private static void Seed(OssExportBse1bModel model, int count, BatchNumber1989Result? batch = null)
    {
        var entries = Enumerable.Range(1, count).Select(i => new OssExportBatchEntryRecord
        {
            Id = i, Rbse = $"00260{i:0000}", Cphh = $"C{count - i:00}", OwnerName = $"O{i:00}", Address1 = $"A{count - i:00}", BatchId = 9
        }).ToList();
        model.PageContext.HttpContext.Session.SetString("OssExportBSE1bGridEntries", JsonSerializer.Serialize(entries));
        if (batch is not null)
            model.PageContext.HttpContext.Session.SetString("OssExportBSE1bBatchResult", JsonSerializer.Serialize(batch));
    }

    [Theory]
    [InlineData("Cphh", false)] [InlineData("OwnerName", true)] [InlineData("Address1", false)] [InlineData("Rbse", true)]
    public async Task OnGet_WithPageNumberQuery_LoadsGridAndClampsPage(string column, bool desc)
    {
        var model = Create("?PageNumber=9");
        Seed(model, 12);
        model.PageNumber = 9;
        model.SortColumn = column;
        model.SortDesc = desc;

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.PageNumber.Should().Be(2);
        model.TotalPages.Should().Be(2);
        model.PagedGridEntries.Should().HaveCount(2);
        model.IsGridEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_PageNumberBelowOne_ClampsToOne()
    {
        var model = Create("?pagenumber=0");
        Seed(model, 3);
        model.PageNumber = 0;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnPostPage_LoadsAndClamps()
    {
        var model = Create();
        Seed(model, 12);
        model.PageNumber = 7;

        await model.OnPostPageAsync();
        model.PageNumber.Should().Be(2);

        model.PageNumber = -1;
        await model.OnPostPageAsync();
        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task AddToGrid_BlankInput_ShowsMessage()
    {
        var model = Create();

        await model.OnPostAddToGridAsync();

        model.ValidationMessage.Should().Be("Please enter an RBSE number.");
    }

    [Fact]
    public async Task AddToGrid_Duplicate_ShowsMessage()
    {
        var model = Create();
        Seed(model, 1);
        model.RbseInput = "002600001";

        await model.OnPostAddToGridAsync();

        model.ValidationMessage.Should().Contain("already in the table");
    }

    [Fact]
    public async Task AddToGrid_NotFound_ShowsMessage()
    {
        _service.ValidateAndGetRbseDetailsAsync(Arg.Any<string>()).Returns((OssExportRecord?)null);
        var model = Create();
        model.RbseInput = "002600009";

        await model.OnPostAddToGridAsync();

        model.ValidationMessage.Should().Contain("was not found in the database");
    }

    [Fact]
    public async Task AddToGrid_BatchCreationFails_ShowsMessage()
    {
        _service.ValidateAndGetRbseDetailsAsync(Arg.Any<string>()).Returns(new OssExportRecord { Rbse = "002600009", Cphh = "01001000101" });
        _service.CreateBatchNumber1989Async().Returns((BatchNumber1989Result?)null);
        var model = Create();
        model.RbseInput = "002600009";

        await model.OnPostAddToGridAsync();

        model.ValidationMessage.Should().Be("Failed to create batch number.");
    }

    [Fact]
    public async Task AddToGrid_WithExistingBatch_DoesNotCreateAnother()
    {
        _service.ValidateAndGetRbseDetailsAsync(Arg.Any<string>()).Returns(new OssExportRecord { Rbse = "002600009", Cphh = "01001000101" });
        var model = Create();
        Seed(model, 1, new BatchNumber1989Result(9, 2024, 5));
        model.RbseInput = "002600009";

        await model.OnPostAddToGridAsync();

        model.GridEntries.Should().HaveCount(2);
        await _service.DidNotReceive().CreateBatchNumber1989Async();
    }

    [Fact]
    public async Task Delete_UnknownId_LeavesGridAndClampsPage()
    {
        var model = Create();
        Seed(model, 12, new BatchNumber1989Result(9, 2024, 5));
        model.PageNumber = 5;

        await model.OnPostDeleteAsync(999);

        model.GridEntries.Should().HaveCount(12);
        model.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task Export_Success_ReturnsNamedFile()
    {
        _service.GenerateOssExportFileAsync(9, 2024, 5, Arg.Any<IEnumerable<OssExportBatchEntryRecord>>()).Returns("content");
        var model = Create();
        Seed(model, 2, new BatchNumber1989Result(9, 2024, 5));

        var file = (await model.OnPostExportAsync()).Should().BeOfType<FileContentResult>().Subject;

        file.FileDownloadName.Should().Be("EPBSE_24.005");
        Encoding.ASCII.GetString(file.FileContents).Should().Be("content");
    }

    [Fact]
    public async Task Export_ServiceThrows_ShowsMessageAndKeepsGrid()
    {
        _service.GenerateOssExportFileAsync(Arg.Any<int>(), Arg.Any<short>(), Arg.Any<int>(), Arg.Any<IEnumerable<OssExportBatchEntryRecord>>())
            .Returns<string>(_ => throw new InvalidOperationException("nope"));
        var model = Create();
        Seed(model, 2, new BatchNumber1989Result(9, 2024, 5));

        (await model.OnPostExportAsync()).Should().BeOfType<PageResult>();

        model.ValidationMessage.Should().Be("Export failed: nope");
    }
}
