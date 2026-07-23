using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.AnimalRelations.Services;

namespace BSE.Modules.AnimalRelations.Tests;

public sealed class AnimalRelationsServiceTests
{
    private readonly IAnimalRelationsRepository _repo = Substitute.For<IAnimalRelationsRepository>();
    private readonly AnimalRelationsService _sut;

    public AnimalRelationsServiceTests()
        => _sut = new AnimalRelationsService(_repo);

    // ── GetRelationsByRbseAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetRelationsByRbseAsync_DelegatesToRepository()
    {
        var expected = new List<CaseRelationRecord>
        {
            new() { Id = 1, Rbse = "010000001", RelationType = "OFFSPRING" }
        };
        _repo.GetRelationsByRbseAsync("010000001").Returns(expected);

        var result = await _sut.GetRelationsByRbseAsync("010000001");

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetRelationsByRbseAsync_NoRelations_ReturnsEmpty()
    {
        _repo.GetRelationsByRbseAsync("010000002").Returns(Array.Empty<CaseRelationRecord>());

        var result = await _sut.GetRelationsByRbseAsync("010000002");

        result.Should().BeEmpty();
    }

    // ── GetRelationsDetailsByRbseAsync ───────────────────────────────────────

    [Fact]
    public async Task GetRelationsDetailsByRbseAsync_WithDamAndSire_ReturnsBothAndRelations()
    {
        var dam = new DamSireDetailRecord { Id = 10, Name = "Dolly" };
        var sire = new DamSireDetailRecord { Id = 20, Name = "Bull" };
        var relations = new List<CaseRelationRecord> { new() { Id = 5, Rbse = "010000001" } };
        var expected = new RelationDetailsRecord(dam, sire, relations);
        _repo.GetRelationsDetailsByRbseAsync("010000001").Returns(expected);

        var result = await _sut.GetRelationsDetailsByRbseAsync("010000001");

        result.Dam.Should().Be(dam);
        result.Sire.Should().Be(sire);
        result.Relations.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRelationsDetailsByRbseAsync_NoPedigree_ReturnsNullDamAndSire()
    {
        var expected = new RelationDetailsRecord(null, null, Array.Empty<CaseRelationRecord>());
        _repo.GetRelationsDetailsByRbseAsync("010000099").Returns(expected);

        var result = await _sut.GetRelationsDetailsByRbseAsync("010000099");

        result.Dam.Should().BeNull();
        result.Sire.Should().BeNull();
        result.Relations.Should().BeEmpty();
    }

    // ── GetRelationDetailsOfRelatedCaseAsync ────────────────────────────────

    [Fact]
    public async Task GetRelationDetailsOfRelatedCaseAsync_Found_ReturnsRecord()
    {
        var expected = new RelatedCaseDetailsRecord { RelationRbse = "010000003", Sex = "F" };
        _repo.GetRelationDetailsOfRelatedCaseAsync("010000003").Returns(expected);

        var result = await _sut.GetRelationDetailsOfRelatedCaseAsync("010000003");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetRelationDetailsOfRelatedCaseAsync_NotFound_ReturnsNull()
    {
        _repo.GetRelationDetailsOfRelatedCaseAsync("000000000").Returns((RelatedCaseDetailsRecord?)null);

        var result = await _sut.GetRelationDetailsOfRelatedCaseAsync("000000000");

        result.Should().BeNull();
    }

    // ── GetRelationsByBatchIdAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetRelationsByBatchIdAsync_DelegatesToRepository()
    {
        var expected = new List<BatchRelationRecord> { new() { Rbse = "010000001" } };
        _repo.GetRelationsByBatchIdAsync(42).Returns(expected);

        var result = await _sut.GetRelationsByBatchIdAsync(42);

        result.Should().BeSameAs(expected);
    }

    // ── GetDamSireDetailsByBatchIdAsync ──────────────────────────────────────

    [Fact]
    public async Task GetDamSireDetailsByBatchIdAsync_DelegatesToRepository()
    {
        var expected = new List<BatchDamSireRecord> { new() { Rbse = "010000001", SireName = "Bull" } };
        _repo.GetDamSireDetailsByBatchIdAsync(7).Returns(expected);

        var result = await _sut.GetDamSireDetailsByBatchIdAsync(7);

        result.Should().BeSameAs(expected);
    }

    // ── GetDamSireDetailsMatchesAsync ────────────────────────────────────────

    [Fact]
    public async Task GetDamSireDetailsMatchesAsync_DamSearch_DelegatesToRepository()
    {
        var expected = new List<DamSireDetailRecord>
        {
            new() { Id = 1, Name = "Dolly", Rbse = "010000001" }
        };
        _repo.GetDamSireDetailsMatchesAsync("UK123456", "Dolly", null, null, "F").Returns(expected);

        var result = await _sut.GetDamSireDetailsMatchesAsync("UK123456", "Dolly", null, null, "F");

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetDamSireDetailsMatchesAsync_SireSearch_DelegatesToRepository()
    {
        var expected = new List<DamSireDetailRecord>
        {
            new() { Id = 5, Name = "Bull", Rbse = "010000002" }
        };
        _repo.GetDamSireDetailsMatchesAsync(null, "Bull", null, null, "M").Returns(expected);

        var result = await _sut.GetDamSireDetailsMatchesAsync(null, "Bull", null, null, "M");

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetDamSireDetailsMatchesAsync_ByRbse_DelegatesToRepository()
    {
        var expected = new List<DamSireDetailRecord> { new() { Id = 3, Rbse = "010000005" } };
        _repo.GetDamSireDetailsMatchesAsync(null, null, "010000005", null, "F").Returns(expected);

        var result = await _sut.GetDamSireDetailsMatchesAsync(null, null, "010000005", null, "F");

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetDamSireDetailsMatchesAsync_NoMatches_ReturnsEmpty()
    {
        _repo.GetDamSireDetailsMatchesAsync("XYZNOTFOUND", null, null, null, "M")
             .Returns(Array.Empty<DamSireDetailRecord>());

        var result = await _sut.GetDamSireDetailsMatchesAsync("XYZNOTFOUND", null, null, null, "M");

        result.Should().BeEmpty();
    }
}
