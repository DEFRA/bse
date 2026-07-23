using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Repositories;
using BSE.Modules.BsessIntegration.Services;

namespace BSE.Modules.BsessIntegration.Tests;

public class BsessCheckServiceTests
{
    private readonly IBsessRepository _repository = Substitute.For<IBsessRepository>();
    private readonly BsessCheckService _sut;

    public BsessCheckServiceTests()
    {
        _sut = new BsessCheckService(_repository);
    }

    [Fact]
    public async Task GetCheckByRbseAsync_DelegatesToRepository()
    {
        const string rbse = "001234567";
        var expected = new BsessCheckByRbseResult(
            "01/01/2020", "UK123456789", "01/01/2018", "OTM3", "NEGATIVE",
            "BC001", "02/01/2020", "UK987654321", "02/01/2018", "OTM3", "NEGATIVE");

        _repository.GetCheckByRbseAsync(rbse, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetCheckByRbseAsync(rbse);

        result.Should().Be(expected);
        await _repository.Received(1).GetCheckByRbseAsync(rbse, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCheckByRbseAsync_WhenNoRecord_ReturnsNull()
    {
        _repository.GetCheckByRbseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((BsessCheckByRbseResult?)null);

        var result = await _sut.GetCheckByRbseAsync("000000000");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCheckByDateAsync_DelegatesToRepository()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        IReadOnlyList<BsessDiscrepancyRecord> expected = new List<BsessDiscrepancyRecord>
        {
            new("001234567", new DateTime(2018, 1, 1), new DateTime(2018, 2, 1),
                "UK111111111", "UK222222222", "OTM3", "FS")
        }.AsReadOnly();

        _repository.GetCheckByDateAsync(start, end, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetCheckByDateAsync(start, end);

        result.Should().BeEquivalentTo(expected);
        await _repository.Received(1).GetCheckByDateAsync(start, end, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCheckByDateAsync_WhenNoDiscrepancies_ReturnsEmptyList()
    {
        IReadOnlyList<BsessDiscrepancyRecord> empty = Array.Empty<BsessDiscrepancyRecord>();
        _repository.GetCheckByDateAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(empty);

        var result = await _sut.GetCheckByDateAsync(DateTime.Today, DateTime.Today);

        result.Should().BeEmpty();
    }
}
