using System.Data;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.Batch.Services;

namespace BSE.Modules.Batch.Tests;

public sealed class BatchServiceTests
{
    private readonly IBatchRepository _repo = Substitute.For<IBatchRepository>();
    private readonly BatchService _sut;

    public BatchServiceTests()
    {
        _sut = new BatchService(_repo);
    }

    [Fact]
    public async Task GetOrCreateBatchNumberAsync_DelegatesToRepository()
    {
        var expected = new BatchRecord(42, 2026, 7);
        _repo.GetOrCreateBatchNumberAsync().Returns(expected);

        var result = await _sut.GetOrCreateBatchNumberAsync();

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CreateBatchNumber1989Async_DelegatesToRepository()
    {
        var expected = new BatchRecord(10, 1989, 3);
        _repo.CreateBatchNumber1989Async().Returns(expected);

        var result = await _sut.CreateBatchNumber1989Async();

        result.Should().Be(expected);
        result.BatchYear.Should().Be(1989);
    }

    [Fact]
    public async Task AddBatchNumberLinkAsync_DelegatesToRepository()
    {
        await _sut.AddBatchNumberLinkAsync(42, "010000001", "BSE1");

        await _repo.Received(1).AddBatchNumberLinkAsync(42, "010000001", "BSE1");
    }

    [Fact]
    public async Task GetBatchIdAsync_DelegatesToRepository()
    {
        _repo.GetBatchIdAsync((short)2026, 7).Returns(42);

        var result = await _sut.GetBatchIdAsync(2026, 7);

        result.Should().Be(42);
    }

    [Fact]
    public async Task GetBatchIdAsync_NotFound_ReturnsNull()
    {
        _repo.GetBatchIdAsync((short)1999, 99).Returns((int?)null);

        var result = await _sut.GetBatchIdAsync(1999, 99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBatchNumbersByRbseAsync_DelegatesToRepository()
    {
        var expected = new List<BatchNumberEntry>
        {
            new(42, "2026/7", "010000001", "BSE1"),
            new(42, "2026/7", "010000001", "ANNEX_A")
        };
        _repo.GetBatchNumbersByRbseAsync("010000001").Returns(expected);

        var result = await _sut.GetBatchNumbersByRbseAsync("010000001");

        result.Should().HaveCount(2);
        result[0].BatchNumber.Should().Be("2026/7");
    }

    [Fact]
    public async Task GetLatestBatchNumbersAsync_DelegatesToRepository()
    {
        var expected = new List<LatestBatchRecord>
        {
            new("2026/7", 12),
            new("2026/6", 8),
            new("2026/5", 15)
        };
        _repo.GetLatestBatchNumbersAsync().Returns(expected);

        var result = await _sut.GetLatestBatchNumbersAsync();

        result.Should().HaveCount(3);
        result[0].Batch.Should().Be("2026/7");
    }

    [Fact]
    public async Task GetCasesByBatchIdAsync_DelegatesToRepository()
    {
        var expected = new List<BatchCaseSummaryRecord>
        {
            new("010000001", "12/345/6789/01"),
            new("010000002", "12/345/6789/02")
        };
        _repo.GetCasesByBatchIdAsync(42).Returns(expected);

        var result = await _sut.GetCasesByBatchIdAsync(42);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCasesByBatchAsync_DelegatesToRepository()
    {
        var expected = new List<BatchCaseSummaryRecord>
        {
            new("010000001", "12/345/6789/01")
        };
        _repo.GetCasesByBatchAsync((short)2026, 7).Returns(expected);

        var result = await _sut.GetCasesByBatchAsync(2026, 7);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCaseDetailsByBatchIdAsync_DelegatesToRepository()
    {
        var expected = new List<BatchCaseRecord>
        {
            new("010000001", "01/00/00001", "123456789012", "12/345/6789/01")
        };
        _repo.GetCaseDetailsByBatchIdAsync(42).Returns(expected);

        var result = await _sut.GetCaseDetailsByBatchIdAsync(42);

        result.Should().HaveCount(1);
        result[0].DisplayRbse.Should().Be("01/00/00001");
    }

    [Fact]
    public async Task AddBatchNumberLinkAsync_OnlyCallsStandaloneOverload()
    {
        // Verifies the service layer does NOT call the transactional overload
        await _sut.AddBatchNumberLinkAsync(1, "010000001", "BSE1");

        await _repo.DidNotReceive().AddBatchNumberLinkAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction?>());
    }
}
