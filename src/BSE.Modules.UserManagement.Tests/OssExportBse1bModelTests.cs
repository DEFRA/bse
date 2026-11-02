using System.Text.Json;
using BSE.Host.Pages.OssExport;
using BSE.Modules.OssExport.Models;
using BSE.Modules.OssExport.Services;
using BSE.SharedKernel;
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

public sealed class OssExportBse1bModelTests
{
    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

        public IEnumerable<string> Keys => _store.Keys;
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public bool IsAvailable => true;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    private static OssExportBse1bModel Create(IOssExportService service)
    {
        var session = new TestSession();
        var httpContext = new DefaultHttpContext
        {
            User = PageTestHelpers.Principal("VLAAccess"),
            Session = session
        };
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        var model = new OssExportBse1bModel(service)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>())
        };

        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.Url = Substitute.For<IUrlHelper>();
        model.Url.ActionContext.Returns(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()));
        return model;
    }

    [Fact]
    public async Task OnGetAsync_ResetsGridState_ForFreshRequest()
    {
        var service = Substitute.For<IOssExportService>();
        var model = Create(service);
        model.PageContext.HttpContext.Session.SetString("OssExportBSE1bGridEntries", JsonSerializer.Serialize(new[]
        {
            new OssExportBatchEntryRecord { Id = 1, Rbse = "0100100001", Cphh = "01/001/0001/01", OwnerName = "Owner" }
        }));
        model.PageContext.HttpContext.Session.SetString("OssExportBSE1bBatchResult", JsonSerializer.Serialize(new BatchNumber1989Result(99, 2026, 7)));

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.GridEntries.Should().BeEmpty();
        model.BatchResult.Should().BeNull();
    }

    [Fact]
    public async Task OnPostAddToGridAsync_WhenRbseInvalid_ShowsValidationMessage()
    {
        var service = Substitute.For<IOssExportService>();
        var model = Create(service);
        model.RbseInput = "bad";

        var result = await model.OnPostAddToGridAsync();

        result.Should().BeOfType<PageResult>();
        model.ValidationMessage.Should().Be("Please enter a valid RBSE number (9 digits).");
        await service.DidNotReceive().CreateBatchNumber1989Async();
    }

    [Fact]
    public async Task OnPostAddToGridAsync_WhenValid_AddsEntryAndCreatesBatch()
    {
        var service = Substitute.For<IOssExportService>();
        service.ValidateAndGetRbseDetailsAsync(Arg.Any<string>()).Returns(new OssExportRecord
        {
            Rbse = "01/001/0001",
            Cphh = "01/001/0001/01",
            OwnerName = "Owner",
            Address1 = "1 Main Street"
        });
        service.CreateBatchNumber1989Async().Returns(new BatchNumber1989Result(45, 2026, 5));

        var model = Create(service);
        model.RbseInput = "01/001/0001";

        var result = await model.OnPostAddToGridAsync();

        result.Should().BeOfType<PageResult>();
        model.GridEntries.Should().ContainSingle();
        model.BatchResult.Should().BeEquivalentTo(new BatchNumber1989Result(45, 2026, 5));
        model.GridEntries[0].Rbse.Should().Be(RbseHelper.ParseToRaw(model.RbseInput));
        model.RbseInput.Should().Be(RbseHelper.Format(model.GridEntries[0].Rbse));
        model.PageContext.HttpContext.Session.GetString("OssExportBSE1bGridEntries").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenGridBecomesEmpty_ClearsBatchState()
    {
        var service = Substitute.For<IOssExportService>();
        service.ValidateAndGetRbseDetailsAsync(Arg.Any<string>()).Returns(new OssExportRecord { Rbse = "01/001/0001", Cphh = "01/001/0001/01", OwnerName = "Owner", Address1 = "1 Main Street" });
        service.CreateBatchNumber1989Async().Returns(new BatchNumber1989Result(45, 2026, 5));

        var model = Create(service);
        model.RbseInput = "01/001/0001";
        await model.OnPostAddToGridAsync();

        var entryId = model.GridEntries.Single().Id;
        var result = await model.OnPostDeleteAsync(entryId);

        result.Should().BeOfType<PageResult>();
        model.GridEntries.Should().BeEmpty();
        model.BatchResult.Should().BeNull();
        model.PageContext.HttpContext.Session.GetString("OssExportBSE1bBatchResult").Should().BeNull();
    }

    [Fact]
    public async Task OnPostExportAsync_WhenGridEmpty_ShowsValidationMessage()
    {
        var service = Substitute.For<IOssExportService>();
        var model = Create(service);

        var result = await model.OnPostExportAsync();

        result.Should().BeOfType<PageResult>();
        model.ValidationMessage.Should().Be("The grid is empty. Please add RBSE records before exporting.");
    }
}
