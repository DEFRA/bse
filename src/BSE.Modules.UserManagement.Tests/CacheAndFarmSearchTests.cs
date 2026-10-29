using BSE.Host.Cache;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Search;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public class ResilientDistributedCacheTests
{
    private readonly IDistributedCache _redis = Substitute.For<IDistributedCache>();
    private readonly IDistributedCache _memory = Substitute.For<IDistributedCache>();
    private readonly ResilientDistributedCache _sut;
    private static readonly DistributedCacheEntryOptions Options = new();

    public ResilientDistributedCacheTests()
    {
        _sut = new ResilientDistributedCache(_redis, _memory, NullLogger<ResilientDistributedCache>.Instance);
    }

    [Fact]
    public void Get_WhenRedisHealthy_ReturnsRedisValueAndStaysDistributed()
    {
        _redis.Get("k").Returns(new byte[] { 1 });

        _sut.Get("k").Should().Equal(1);
        _sut.IsUsingFallback.Should().BeFalse();
        _memory.DidNotReceiveWithAnyArgs().Get(default!);
    }

    [Fact]
    public void Get_WhenRedisFails_FallsBackToMemoryAndMarksDegraded()
    {
        _redis.When(c => c.Get("k")).Do(_ => throw new InvalidOperationException("down"));
        _memory.Get("k").Returns(new byte[] { 9 });

        _sut.Get("k").Should().Equal(9);
        _sut.IsUsingFallback.Should().BeTrue();
    }

    [Fact]
    public void Get_AfterRedisRecovers_ClearsFallbackFlag()
    {
        _redis.When(c => c.Get("bad")).Do(_ => throw new TimeoutException());
        _sut.Get("bad");
        _sut.IsUsingFallback.Should().BeTrue();

        _redis.Get("good").Returns(new byte[] { 2 });
        _sut.Get("good");

        _sut.IsUsingFallback.Should().BeFalse();
    }

    [Fact]
    public void Set_WhenRedisFails_WritesToMemory()
    {
        _redis.When(c => c.Set("k", Arg.Any<byte[]>(), Options)).Do(_ => throw new InvalidOperationException());

        _sut.Set("k", [1, 2], Options);

        _memory.Received(1).Set("k", Arg.Any<byte[]>(), Options);
        _sut.IsUsingFallback.Should().BeTrue();
    }

    [Fact]
    public void Set_WhenRedisHealthy_DoesNotTouchMemory()
    {
        _sut.Set("k", [1], Options);

        _redis.Received(1).Set("k", Arg.Any<byte[]>(), Options);
        _memory.DidNotReceiveWithAnyArgs().Set(default!, default!, default!);
    }

    [Fact]
    public void Refresh_WhenRedisFails_RefreshesMemory()
    {
        _redis.When(c => c.Refresh("k")).Do(_ => throw new InvalidOperationException());

        _sut.Refresh("k");

        _memory.Received(1).Refresh("k");
    }

    [Fact]
    public void Remove_WhenRedisFails_RemovesFromMemory()
    {
        _redis.When(c => c.Remove("k")).Do(_ => throw new InvalidOperationException());

        _sut.Remove("k");

        _memory.Received(1).Remove("k");
    }

    [Fact]
    public void Remove_WhenRedisHealthy_OnlyRemovesFromRedis()
    {
        _sut.Remove("k");

        _redis.Received(1).Remove("k");
        _memory.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task GetAsync_WhenRedisHealthy_ReturnsValue()
    {
        _redis.GetAsync("k", Arg.Any<CancellationToken>()).Returns(new byte[] { 5 });

        (await _sut.GetAsync("k", TestContext.Current.CancellationToken)).Should().Equal(5);
        _sut.IsUsingFallback.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_WhenRedisFails_FallsBackToMemory()
    {
        _redis.GetAsync("k", Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());
        _memory.GetAsync("k", Arg.Any<CancellationToken>()).Returns(new byte[] { 7 });

        (await _sut.GetAsync("k", TestContext.Current.CancellationToken)).Should().Equal(7);
        _sut.IsUsingFallback.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsync_WhenRedisFails_WritesToMemory()
    {
        _redis.SetAsync("k", Arg.Any<byte[]>(), Options, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        await _sut.SetAsync("k", [1], Options, TestContext.Current.CancellationToken);

        await _memory.Received(1).SetAsync("k", Arg.Any<byte[]>(), Options, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_WhenRedisHealthy_DoesNotTouchMemory()
    {
        await _sut.SetAsync("k", [1], Options, TestContext.Current.CancellationToken);

        await _memory.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task RefreshAsync_WhenRedisFails_RefreshesMemory()
    {
        _redis.RefreshAsync("k", Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());

        await _sut.RefreshAsync("k", TestContext.Current.CancellationToken);

        await _memory.Received(1).RefreshAsync("k", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_WhenRedisFails_RemovesFromMemory()
    {
        _redis.RemoveAsync("k", Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());

        await _sut.RemoveAsync("k", TestContext.Current.CancellationToken);

        await _memory.Received(1).RemoveAsync("k", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_AndRemoveAsync_WhenHealthy_UseRedisOnly()
    {
        await _sut.RefreshAsync("k", TestContext.Current.CancellationToken);
        await _sut.RemoveAsync("k", TestContext.Current.CancellationToken);

        await _redis.Received(1).RefreshAsync("k", Arg.Any<CancellationToken>());
        await _redis.Received(1).RemoveAsync("k", Arg.Any<CancellationToken>());
        await _memory.DidNotReceiveWithAnyArgs().RefreshAsync(default!, default);
    }

    [Fact]
    public async Task GetAsync_WhenCancelled_Rethrows()
    {
        _redis.GetAsync("k", Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());

        var act = () => _sut.GetAsync("k", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _sut.IsUsingFallback.Should().BeFalse();
    }

    [Fact]
    public void Get_WhenDisposed_Rethrows()
    {
        _redis.When(c => c.Get("k")).Do(_ => throw new ObjectDisposedException("redis"));

        var act = () => _sut.Get("k");

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void SecondFailure_WhileDegraded_StaysDegraded()
    {
        _redis.When(c => c.Get(Arg.Any<string>())).Do(_ => throw new InvalidOperationException());

        _sut.Get("a");
        _sut.Get("b");

        _sut.IsUsingFallback.Should().BeTrue();
    }
}

public class FarmsModelTests
{
    private readonly IFarmSearchService _search = Substitute.For<IFarmSearchService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private FarmsModel CreateModel(string? query = null)
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        var http = new DefaultHttpContext();
        if (query is not null) http.Request.QueryString = new QueryString(query);

        return new FarmsModel(_search, _lookups)
        {
            PageContext = new PageContext { HttpContext = http }
        };
    }

    private static FarmSearchResult Farm(string cphh) =>
        new(cphh, "Owner", "1 Road", "Devon", "AB1", "123", "SX1234", "EXETER", "Dairy", "2 Lane", 3, 1);

    [Fact]
    public async Task OnGet_WithoutFilterOrQuery_DoesNotSearchAndFlagsNothing()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        model.NoCriteria.Should().BeFalse();
        model.Filter.HasSearched.Should().BeFalse();
        await _search.DidNotReceiveWithAnyArgs().SearchFarmsAsync(default!);
    }

    [Fact]
    public async Task OnGet_SearchedWithoutFilter_FlagsNoCriteria()
    {
        var model = CreateModel();
        model.Searched = true;

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
        FarmsModel.NoCriteriaMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OnGet_QueryStringWithoutFilter_FlagsNoCriteria()
    {
        var model = CreateModel("?IncludeNonGb=true");

        await model.OnGetAsync();

        model.NoCriteria.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_WithFilter_RunsSearchAndPopulatesResults()
    {
        var model = CreateModel();
        model.Filter.Cphh = "12/345/6789";
        _search.SearchFarmsAsync(Arg.Any<FarmSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[] { Farm("12/345/6789") });

        await model.OnGetAsync();

        model.Filter.HasSearched.Should().BeTrue();
        model.Filter.Results.Should().ContainSingle();
        model.Filter.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_PageNumberBeyondLastPage_IsClamped()
    {
        var model = CreateModel();
        model.Filter.OwnerName = "x";
        model.Filter.PageNumber = 99;
        _search.SearchFarmsAsync(Arg.Any<FarmSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 12).Select(i => Farm($"c{i}")).ToArray());

        await model.OnGetAsync();

        model.Filter.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task OnGet_PageNumberBelowOne_IsRaisedToOne()
    {
        var model = CreateModel();
        model.Filter.Aho = "EXETER";
        model.Filter.PageNumber = 0;
        _search.SearchFarmsAsync(Arg.Any<FarmSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[] { Farm("c1") });

        await model.OnGetAsync();

        model.Filter.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_InvalidModelState_SkipsSearch()
    {
        var model = CreateModel();
        model.Filter.Cphh = "x";
        model.ModelState.AddModelError("Filter.Cphh", "bad");

        await model.OnGetAsync();

        await _search.DidNotReceiveWithAnyArgs().SearchFarmsAsync(default!);
    }

    [Theory]
    [InlineData("Address")]
    [InlineData("County")]
    [InlineData("Herdmark")]
    [InlineData("NumericHerdmark")]
    [InlineData("Dealer")]
    public async Task OnGet_EachSingleCriterion_TriggersSearch(string field)
    {
        var model = CreateModel();
        switch (field)
        {
            case "Address": model.Filter.Address = "a"; break;
            case "County": model.Filter.County = "Devon"; break;
            case "Herdmark": model.Filter.Herdmark = "AB"; break;
            case "NumericHerdmark": model.Filter.NumericHerdmark = "1"; break;
            case "Dealer": model.Filter.IsDealer = true; break;
        }
        _search.SearchFarmsAsync(Arg.Any<FarmSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<FarmSearchResult>());

        await model.OnGetAsync();

        model.Filter.HasSearched.Should().BeTrue();
    }

    [Fact]
    public async Task OnGetExport_WithoutFilter_RedirectsToPage()
    {
        var model = CreateModel();

        var result = await model.OnGetExportAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnGetExport_WithFilter_ReturnsWorkbook()
    {
        var model = CreateModel();
        model.Filter.Cphh = "12/345/6789";
        _search.SearchFarmsAsync(Arg.Any<FarmSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[] { Farm("12/345/6789"), Farm("98/765/4321") });

        var result = await model.OnGetExportAsync();

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("farmsearchresults.xlsx");
        file.FileContents.Should().NotBeEmpty();
    }
}
