using BSE.Modules.Search.Models;
using BSE.Modules.Search.Repositories;
using BSE.Modules.Search.Services;

namespace BSE.Modules.Search.Tests;

public sealed class FarmSearchServiceTests
{
    private readonly ISearchRepository _repo = Substitute.For<ISearchRepository>();
    private readonly FarmSearchService _sut;

    public FarmSearchServiceTests()
    {
        _sut = new FarmSearchService(_repo);
    }

    // FarmSearchResult: Cphh, OwnerName, Address, County, Herdmark, NumericHerdmark,
    // MapReference, Aho, HerdType, CorrespondenceAddress, CasesCount, ConfirmedCasesCount

    [Fact]
    public async Task SearchFarmsAsync_DelegatesToRepository()
    {
        var query = new FarmSearchQuery(OwnerName: "Smith");
        var expected = new List<FarmSearchResult>
        {
            new("12/345/6789/01", "John Smith", "Main Farm, Ottery", "Devon",
                "123456", "123456", null, "SW", "Beef", null, 5, 2)
        };
        _repo.SearchFarmsAsync(query, default).Returns(expected);

        var result = await _sut.SearchFarmsAsync(query);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SearchFarmsAsync_EmptyQuery_ReturnsEmpty()
    {
        var query = new FarmSearchQuery();
        _repo.SearchFarmsAsync(query, default).Returns(Array.Empty<FarmSearchResult>());

        var result = await _sut.SearchFarmsAsync(query);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchFarmsAsync_WithCountyFilter_PassesCountyToRepository()
    {
        var query = new FarmSearchQuery(County: "Devon");
        _repo.SearchFarmsAsync(query, default).Returns(Array.Empty<FarmSearchResult>());

        await _sut.SearchFarmsAsync(query);

        await _repo.Received(1).SearchFarmsAsync(
            Arg.Is<FarmSearchQuery>(q => q.County == "Devon"), default);
    }

    [Fact]
    public async Task SearchFarmsAsync_WithHerdmarkFilter_PassesHerdmarkToRepository()
    {
        var query = new FarmSearchQuery(Herdmark: "123456");
        _repo.SearchFarmsAsync(query, default).Returns(Array.Empty<FarmSearchResult>());

        await _sut.SearchFarmsAsync(query);

        await _repo.Received(1).SearchFarmsAsync(
            Arg.Is<FarmSearchQuery>(q => q.Herdmark == "123456"), default);
    }

    [Fact]
    public async Task SearchFarmsAsync_IncludeNonGbFarms_PassesFlagToRepository()
    {
        var query = new FarmSearchQuery(IncludeNonGbFarms: true);
        _repo.SearchFarmsAsync(query, default).Returns(Array.Empty<FarmSearchResult>());

        await _sut.SearchFarmsAsync(query);

        await _repo.Received(1).SearchFarmsAsync(
            Arg.Is<FarmSearchQuery>(q => q.IncludeNonGbFarms == true), default);
    }

    [Fact]
    public async Task SearchFarmsAsync_DealerFilter_PassesFlagToRepository()
    {
        var query = new FarmSearchQuery(IsDealer: true);
        _repo.SearchFarmsAsync(query, default).Returns(Array.Empty<FarmSearchResult>());

        await _sut.SearchFarmsAsync(query);

        await _repo.Received(1).SearchFarmsAsync(
            Arg.Is<FarmSearchQuery>(q => q.IsDealer == true), default);
    }

    [Fact]
    public async Task SearchFarmsAsync_MultipleResults_ReturnsAll()
    {
        var query = new FarmSearchQuery(County: "Devon");
        var results = new List<FarmSearchResult>
        {
            new("12/345/6789/01", "John Smith", "Main Farm", "Devon", "111111", "111111", null, "SW", "Beef", null, 2, 1),
            new("12/345/6789/02", "Jane Doe", "Hill Farm", "Devon", "222222", "222222", null, "SW", "Dairy", null, 0, 0)
        };
        _repo.SearchFarmsAsync(query, default).Returns(results);

        var result = await _sut.SearchFarmsAsync(query);

        result.Should().HaveCount(2);
    }
}
