using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Services;
using BSE.SharedKernel;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.FarmManagement.Tests;

public sealed class FarmServiceTests
{
    private readonly IFarmRepository _farmRepo = Substitute.For<IFarmRepository>();
    private readonly IFarmRelationRepository _relationRepo = Substitute.For<IFarmRelationRepository>();
    private readonly IHerdSizeRepository _herdSizeRepo = Substitute.For<IHerdSizeRepository>();
    private readonly IVetnetRepository _vetnetRepo = Substitute.For<IVetnetRepository>();
    private readonly FarmService _sut;

    public FarmServiceTests()
    {
        _sut = new FarmService(_farmRepo, _relationRepo, _herdSizeRepo, _vetnetRepo);
    }

    [Fact]
    public async Task GetByCphhAsync_WhenFarmExists_ReturnsFarmRecord()
    {
        var expected = new FarmRecord { CPHH = "01001000101", OwnerName = "Test Farm" };
        _farmRepo.GetByCphhAsync("01001000101").Returns(expected);

        var result = await _sut.GetByCphhAsync("01001000101");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetByCphhAsync_WhenFarmNotFound_ReturnsNull()
    {
        _farmRepo.GetByCphhAsync("99999999999").Returns((FarmRecord?)null);

        var result = await _sut.GetByCphhAsync("99999999999");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDetailsByCphhAsync_WhenFarmExists_ReturnsCompositeRecord()
    {
        var farm = new FarmRecord { CPHH = "01001000101" };
        var relations = new List<FarmRelationRecord>
        {
            new() { ID = 1, CPHH = "01001000101", RelatedCPHH = "01001000202" }
        };
        var herdSizes = new List<HerdSizeRecord>
        {
            new() { ID = 1, CPHH = "01001000101", HerdYear = 2024, TotalSize = 120 }
        };
        var detail = new FarmDetailRecord(farm, relations, herdSizes);
        _farmRepo.GetDetailsByCphhAsync("01001000101").Returns(detail);

        var result = await _sut.GetDetailsByCphhAsync("01001000101");

        result.Farm.Should().Be(farm);
        result.RelatedFarms.Should().HaveCount(1);
        result.HerdSizes.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByCphAsync_WhenMatches_ReturnsSummaryList()
    {
        var summaries = new List<FarmSummaryRecord>
        {
            new() { CPHH = "01/001/0001/01", OwnerName = "Farm A", Address1 = "Lane 1" },
            new() { CPHH = "01/001/0001/02", OwnerName = "Farm B", Address1 = "Lane 2" }
        };
        _farmRepo.GetByCphAsync("01001").Returns(summaries);

        var result = await _sut.GetByCphAsync("01001");

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddAsync_DelegatesToFarmRepository()
    {
        var command = new AddFarmCommand(
            "01001000101", "Owner", "Addr1", null, null, "SW1A 1AA",
            "Parish", "District", "Glos", null, null, null, null,
            null, null, null, null, null, null, "GL", "D", "PB", false, 1);

        await _sut.AddAsync(command, userId: 42);

        await _farmRepo.Received(1).AddAsync(command, 42);
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToFarmRepository()
    {
        var command = new UpdateFarmCommand(
            "01001000101", "Owner Updated", "Addr1", null, null, "SW1A 1AA",
            "Parish", "District", "Glos", null, null, null, null,
            null, null, null, null, null, null, "GL", "D", "PB", false, 1,
            RowStamp: new byte[] { 0x01, 0x02 });

        await _sut.UpdateAsync(command, userId: 42);

        await _farmRepo.Received(1).UpdateAsync(command, 42);
    }

    [Fact]
    public async Task ChangeCphhAsync_WhenSuccess_ReturnsSuccess()
    {
        _farmRepo.ChangeCphhAsync("01001000101", "02002000202", 42)
            .Returns(ChangeCphhResult.Success);

        var result = await _sut.ChangeCphhAsync("01001000101", "02002000202", 42);

        result.Should().Be(ChangeCphhResult.Success);
    }

    [Fact]
    public async Task ChangeCphhAsync_WhenOldCphhNotFound_ReturnsOldCphhNotFoundOrNewCphhAlreadyExists()
    {
        _farmRepo.ChangeCphhAsync("99999999999", "02002000202", 42)
            .Returns(ChangeCphhResult.OldCphhNotFoundOrNewCphhAlreadyExists);

        var result = await _sut.ChangeCphhAsync("99999999999", "02002000202", 42);

        result.Should().Be(ChangeCphhResult.OldCphhNotFoundOrNewCphhAlreadyExists);
    }

    [Fact]
    public async Task GetConfirmedCaseCountAsync_ReturnsCountFromRepository()
    {
        _farmRepo.GetConfirmedCaseCountAsync("01001000101").Returns(3);

        var result = await _sut.GetConfirmedCaseCountAsync("01001000101");

        result.Should().Be(3);
    }

    [Fact]
    public async Task GetCaseCountByCphhAsync_ReturnsCountFromRepository()
    {
        _farmRepo.GetCaseCountByCphhAsync("01001000101").Returns(7);

        var result = await _sut.GetCaseCountByCphhAsync("01001000101");

        result.Should().Be(7);
    }

    [Fact]
    public async Task GetRelatedFarmsAsync_DelegatesToFarmRelationRepository()
    {
        var relations = new List<FarmRelationRecord>
        {
            new() { ID = 5, CPHH = "01001000101", RelatedCPHH = "01001000303", Status = "Jones Farm, Chapel Road" }
        };
        _relationRepo.GetRelatedFarmAsync("01001000101").Returns(relations);

        var result = await _sut.GetRelatedFarmsAsync("01001000101");

        result.Should().HaveCount(1);
        await _farmRepo.DidNotReceive().GetByCphhAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetHerdSizesAsync_DelegatesToHerdSizeRepository()
    {
        var sizes = new List<HerdSizeRecord>
        {
            new() { ID = 3, CPHH = "01001000101", HerdYear = 2023, TotalSize = 85 }
        };
        _herdSizeRepo.GetByCphhAsync("01001000101").Returns(sizes);

        var result = await _sut.GetHerdSizesAsync("01001000101");

        result.Should().HaveCount(1);
        result.First().HerdYear.Should().Be(2023);
    }

    [Fact]
    public async Task GetVetnetDetailsAsync_DelegatesToVetnetRepository()
    {
        var vetnet = new List<VetnetRecord>
        {
            new() { CPHH = "01001000101", Herdmark = "AB1234", NumericHerdmark = "001234" }
        };
        _vetnetRepo.GetByCphhAsync("01001000101").Returns(vetnet);

        var result = await _sut.GetVetnetDetailsAsync("01001000101");

        result.Should().ContainSingle(v => v.Herdmark == "AB1234");
    }

    [Fact]
    public async Task ChangeCphhAsync_WhenErrorUpdatingCase_ReturnsErrorUpdatingCase()
    {
        _farmRepo.ChangeCphhAsync("01001000101", "02002000202", 99)
            .Returns(ChangeCphhResult.ErrorUpdatingCase);

        var result = await _sut.ChangeCphhAsync("01001000101", "02002000202", 99);

        result.Should().Be(ChangeCphhResult.ErrorUpdatingCase);
    }
}
