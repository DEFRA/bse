using BSE.Modules.Search.Models;
using BSE.Modules.Search.Repositories;
using BSE.Modules.Search.Services;

namespace BSE.Modules.Search.Tests;

public sealed class OutstandingDataSearchServiceTests
{
    private readonly ISearchRepository _repo = Substitute.For<ISearchRepository>();
    private readonly OutstandingDataSearchService _sut;

    public OutstandingDataSearchServiceTests()
    {
        _sut = new OutstandingDataSearchService(_repo);
    }

    // OutstandingCaseResult: Rbse, Cphh, Eartag, FormADate, BirthDate, Fate, FinalResult

    private static OutstandingCaseResult SampleResult() =>
        new("01/23/12345", "12/345/6789/01", "UK 123456 12345",
            new DateTime(2020, 5, 1), new DateTime(2018, 1, 1), null, null);

    [Fact]
    public async Task GetOutstandingBse1sAsync_DelegatesToRepository()
    {
        var query = new OutstandingSearchQuery(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31));
        var expected = new List<OutstandingCaseResult> { SampleResult() };
        _repo.GetOutstandingBse1sAsync(query, TestContext.Current.CancellationToken).Returns(expected);

        var result = await _sut.GetOutstandingBse1sAsync(query, TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetOutstandingFatesAsync_DelegatesToRepository()
    {
        var query = new OutstandingSearchQuery(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31));
        var expected = new List<OutstandingCaseResult> { SampleResult() };
        _repo.GetOutstandingFatesAsync(query, TestContext.Current.CancellationToken).Returns(expected);

        var result = await _sut.GetOutstandingFatesAsync(query, TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetOutstandingResultsAsync_DelegatesToRepository()
    {
        var query = new OutstandingSearchQuery(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31));
        var expected = new List<OutstandingCaseResult> { SampleResult() };
        _repo.GetOutstandingResultsAsync(query, TestContext.Current.CancellationToken).Returns(expected);

        var result = await _sut.GetOutstandingResultsAsync(query, TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetOutstandingBse1sAsync_EmptyQuery_ReturnsEmpty()
    {
        var query = new OutstandingSearchQuery();
        _repo.GetOutstandingBse1sAsync(query, TestContext.Current.CancellationToken).Returns(Array.Empty<OutstandingCaseResult>());

        var result = await _sut.GetOutstandingBse1sAsync(query, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOutstandingFatesAsync_IncludeNonGb_PassesFlagToRepository()
    {
        var query = new OutstandingSearchQuery(IncludeNonGbCases: true);
        _repo.GetOutstandingFatesAsync(query, TestContext.Current.CancellationToken).Returns(Array.Empty<OutstandingCaseResult>());

        await _sut.GetOutstandingFatesAsync(query, TestContext.Current.CancellationToken);

        await _repo.Received(1).GetOutstandingFatesAsync(
            Arg.Is<OutstandingSearchQuery>(q => q.IncludeNonGbCases == true), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetOutstandingResultsAsync_IncludeNonGb_PassesFlagToRepository()
    {
        var query = new OutstandingSearchQuery(IncludeNonGbCases: true);
        _repo.GetOutstandingResultsAsync(query, TestContext.Current.CancellationToken).Returns(Array.Empty<OutstandingCaseResult>());

        await _sut.GetOutstandingResultsAsync(query, TestContext.Current.CancellationToken);

        await _repo.Received(1).GetOutstandingResultsAsync(
            Arg.Is<OutstandingSearchQuery>(q => q.IncludeNonGbCases == true), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ThreeOutstandingMethods_AreIndependent()
    {
        // Verifies that calling BSE1s does not accidentally call Fates or Results
        var query = new OutstandingSearchQuery();
        _repo.GetOutstandingBse1sAsync(query, TestContext.Current.CancellationToken).Returns(Array.Empty<OutstandingCaseResult>());

        await _sut.GetOutstandingBse1sAsync(query, TestContext.Current.CancellationToken);

        await _repo.DidNotReceive().GetOutstandingFatesAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().GetOutstandingResultsAsync(Arg.Any<OutstandingSearchQuery>(), Arg.Any<CancellationToken>());
    }
}
