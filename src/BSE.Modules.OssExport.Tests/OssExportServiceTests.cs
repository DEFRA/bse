using BSE.Modules.OssExport.Models;
using BSE.Modules.OssExport.Repositories;
using BSE.Modules.OssExport.Services;

namespace BSE.Modules.OssExport.Tests;

public sealed class OssExportServiceTests
{
    private readonly IOssExportRepository _repo = Substitute.For<IOssExportRepository>();
    private readonly OssExportService _sut;

    public OssExportServiceTests()
        => _sut = new OssExportService(_repo);

    // ── PopulateStagingTablesAsync ────────────────────────────────────────────

    [Fact]
    public async Task PopulateStagingTablesAsync_DelegatesToRepository()
    {
        await _sut.PopulateStagingTablesAsync();

        await _repo.Received(1).PopulateStagingTablesAsync();
    }

    [Fact]
    public async Task PopulateStagingTablesAsync_RepositoryThrows_PropagatesException()
    {
        _repo.PopulateStagingTablesAsync()
             .Returns<Task>(_ => throw new InvalidOperationException("SP failed"));

        var act = async () => await _sut.PopulateStagingTablesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SP failed");
    }

    // ── GetExportDetailsByRbseAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetExportDetailsByRbseAsync_Found_ReturnsRecord()
    {
        var expected = new OssExportRecord
        {
            Rbse = "01/23/45678",
            Cphh = "12/345/6789/01",
            OwnerName = "Test Farm Ltd",
            Address1 = "1 Farm Lane"
        };
        _repo.GetExportDetailsByRbseAsync("010234567").Returns(expected);

        var result = await _sut.GetExportDetailsByRbseAsync("010234567");

        result.Should().Be(expected);
        result!.Rbse.Should().Be("01/23/45678");
        result.OwnerName.Should().Be("Test Farm Ltd");
    }

    [Fact]
    public async Task GetExportDetailsByRbseAsync_NotFound_ReturnsNull()
    {
        _repo.GetExportDetailsByRbseAsync("000000000").Returns((OssExportRecord?)null);

        var result = await _sut.GetExportDetailsByRbseAsync("000000000");

        result.Should().BeNull();
    }

    // ── CreateBatchNumber1989Async ────────────────────────────────────────────

    [Fact]
    public async Task CreateBatchNumber1989Async_Success_ReturnsBatchResult()
    {
        var expected = new BatchNumber1989Result(BatchId: 42, BatchYear: 1989, BatchNumber: 7);
        _repo.CreateBatchNumber1989Async().Returns(expected);

        var result = await _sut.CreateBatchNumber1989Async();

        result.Should().Be(expected);
        result!.BatchYear.Should().Be(1989);
        result.BatchNumber.Should().Be(7);
    }

    [Fact]
    public async Task CreateBatchNumber1989Async_SpFails_ReturnsNull()
    {
        _repo.CreateBatchNumber1989Async().Returns((BatchNumber1989Result?)null);

        var result = await _sut.CreateBatchNumber1989Async();

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateBatchNumber1989Async_DelegatesToRepository()
    {
        _repo.CreateBatchNumber1989Async()
             .Returns(new BatchNumber1989Result(1, 1989, 1));

        await _sut.CreateBatchNumber1989Async();

        await _repo.Received(1).CreateBatchNumber1989Async();
    }

    // ── Service does not call wrong methods ───────────────────────────────────

    [Fact]
    public async Task GetExportDetails_DoesNotCallPopulateOrCreate()
    {
        _repo.GetExportDetailsByRbseAsync(Arg.Any<string>())
             .Returns((OssExportRecord?)null);

        await _sut.GetExportDetailsByRbseAsync("010000001");

        await _repo.DidNotReceive().PopulateStagingTablesAsync();
        await _repo.DidNotReceive().CreateBatchNumber1989Async();
    }

    [Fact]
    public async Task PopulateStaging_DoesNotCallGetOrCreate()
    {
        await _sut.PopulateStagingTablesAsync();

        await _repo.DidNotReceive().GetExportDetailsByRbseAsync(Arg.Any<string>());
        await _repo.DidNotReceive().CreateBatchNumber1989Async();
    }
}
