using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.CaseWork;
using BSE.Host.Pages.Search;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
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

#pragma warning disable xUnit1051 // NSubstitute's Arg.Any<CancellationToken>() is intentional here for default-token matching.

public sealed class MinuteAndSearchCasesModelTests
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

    private static MinuteModel CreateMinuteModel(ICaseWorkService service)
    {
        var session = new TestSession();
        var httpContext = new DefaultHttpContext
        {
            User = PageTestHelpers.Principal("VLAMaintenance"),
            Session = session
        };
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        var model = new MinuteModel(service)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>())
        };

        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.Url = Substitute.For<IUrlHelper>();
        model.Url.ActionContext.Returns(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()));
        return model;
    }

    private static CasesModel CreateCasesModel(ICaseSearchService search, ILookupDataService lookups)
    {
        var httpContext = new DefaultHttpContext
        {
            User = PageTestHelpers.Principal("VLAMaintenance")
        };
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        var model = new CasesModel(search, lookups)
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
    public async Task Minute_OnGetAsync_NormalizesMinuteTypeAndLoadsDate()
    {
        var service = Substitute.For<ICaseWorkService>();
        service.GetMinuteDetailsAsync("002600001", "AMFS").Returns(new MinuteDetailsRecord
        {
            Rbse = "002600001",
            ActiveMemoDate = new DateTime(2024, 02, 15)
        });

        var model = CreateMinuteModel(service);
        model.Rbse = "002600001";
        model.Type = "ActiveMemoFS";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Type.Should().Be("AMFS");
        model.MinuteTypeLabel.Should().Be("Active Memo");
        model.Details.Should().NotBeNull();
        model.MinuteDate.Should().Be(new DateTime(2024, 02, 15));
    }

    [Fact]
    public async Task Minute_OnPostDownloadAsync_RequiresOutstandingFormsForAnnexC()
    {
        var service = Substitute.For<ICaseWorkService>();
        service.GetMinuteDetailsAsync("002600001", "AnnexC").Returns(new MinuteDetailsRecord
        {
            Rbse = "002600001",
            AnnexCDate = new DateTime(2024, 05, 10)
        });

        var model = CreateMinuteModel(service);
        model.Rbse = "002600001";
        model.Type = "Annex C";

        var result = await model.OnPostDownloadAsync();

        result.Should().BeOfType<PageResult>();
        var formState = model.ModelState["OutstandingForms"];
        formState.Should().NotBeNull();
        formState!.Errors.Should().ContainSingle(e => e.ErrorMessage == "Select at least one outstanding form.");
    }

    [Fact]
    public async Task Cases_OnGetAsync_WithFilter_FormatsRbseAndExecutesSearch()
    {
        var search = Substitute.For<ICaseSearchService>();
        var lookups = Substitute.For<ILookupDataService>();
        lookups.GetSexesAsync().Returns(new[] { new LuSex { Id = 1, Code = "F", Description = "Female" } });
        lookups.GetLookupAsync(LookupTableId.Survey).Returns(new[] { new LookupItem(1, "FS", "Fallen Stock") });
        lookups.GetCaseFatesAsync().Returns(new[] { new LuCaseFate { Id = 1, Code = "SL", Description = "Slaughter" } });
        lookups.GetTestResultsAsync().Returns(new[] { new LuTestResult { Id = 1, Code = "Neg", Description = "Negative" } });
        search.SearchCasesAsync(Arg.Any<CaseSearchQuery>()).Returns(new[]
        {
            new CaseSearchResult(
                "002600001",
                "01/001/0001/01",
                "F",
                "FS",
                "E123",
                null,
                null,
                null,
                "SL",
                "Neg",
                null,
                null,
                "n",
                null,
                null,
                null)
        });

        var model = CreateCasesModel(search, lookups);
        model.Filter = new CaseSearchViewModel { Rbse = "26/1", Survey = "FS" };

        await model.OnGetAsync();

        model.Filter.Results.Should().HaveCount(1);
        model.Filter.HasSearched.Should().BeTrue();
        model.Filter.Rbse.Should().Be(RbseHelper.Format("002600001"));
    }

    [Fact]
    public async Task Cases_OnGetAsync_WhenNoCriteriaProvided_SetsNoCriteriaFlag()
    {
        var search = Substitute.For<ICaseSearchService>();
        var lookups = Substitute.For<ILookupDataService>();
        lookups.GetSexesAsync().Returns(Array.Empty<LuSex>());
        lookups.GetLookupAsync(LookupTableId.Survey).Returns(Array.Empty<LookupItem>());
        lookups.GetCaseFatesAsync().Returns(Array.Empty<LuCaseFate>());
        lookups.GetTestResultsAsync().Returns(Array.Empty<LuTestResult>());

        var model = CreateCasesModel(search, lookups);
        model.Searched = true;
        model.Filter = new CaseSearchViewModel();

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }
}
