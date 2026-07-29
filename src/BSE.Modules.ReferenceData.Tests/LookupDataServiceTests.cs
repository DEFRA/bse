using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BSE.Modules.ReferenceData.Tests;

public sealed class LookupDataServiceTests
{
    private readonly ILookupRepository _repo = Substitute.For<ILookupRepository>();
    private readonly LookupDataService _sut;

    public LookupDataServiceTests() => _sut = new LookupDataService(_repo);

    // ── GetBreedsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBreedsAsync_CallsRepository_AndReturnsResults()
    {
        var breeds = new[] { new LuBreed { Id = 1, Code = "B", FullName = "Black", AmalgamatedName = "Black" } };
        _repo.GetBreedsAsync().Returns(breeds);

        var result = await _sut.GetBreedsAsync();

        result.Should().BeEquivalentTo(breeds);
        await _repo.Received(1).GetBreedsAsync();
    }

    // ── GetCaseTypesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetCaseTypesAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuCaseType { Id = 1, Code = "BS", Description = "BSE Suspect" } };
        _repo.GetCaseTypesAsync().Returns(items);

        var result = await _sut.GetCaseTypesAsync();

        result.Should().BeEquivalentTo(items);
        await _repo.Received(1).GetCaseTypesAsync();
    }

    // ── GetCaseFatesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetCaseFatesAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuCaseFate { Id = 1, Code = "SL", Description = "Slaughtered" } };
        _repo.GetCaseFatesAsync().Returns(items);

        var result = await _sut.GetCaseFatesAsync();

        result.Should().BeEquivalentTo(items);
    }

    // ── GetAnimalStatusesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAnimalStatusesAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuAnimalStatus { Id = 1, Code = "ALIVE", Description = "Alive" } };
        _repo.GetAnimalStatusesAsync().Returns(items);

        var result = await _sut.GetAnimalStatusesAsync();

        result.Should().BeEquivalentTo(items);
    }

    // ── GetSexesAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSexesAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuSex { Id = 1, Code = "M", Description = "Male" } };
        _repo.GetSexesAsync().Returns(items);

        var result = await _sut.GetSexesAsync();

        result.Should().BeEquivalentTo(items);
    }

    // ── GetHerdTypesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetHerdTypesAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuHerdType { Id = 1, Code = "D", Description = "Dairy" } };
        _repo.GetHerdTypesAsync().Returns(items);

        var result = await _sut.GetHerdTypesAsync();

        result.Should().BeEquivalentTo(items);
    }

    // ── GetADNSRegionsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetADNSRegionsAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuADNSRegion { Id = 1, Name = "North" } };
        _repo.GetADNSRegionsAsync().Returns(items);

        var result = await _sut.GetADNSRegionsAsync();

        result.Should().BeEquivalentTo(items);
    }

    // ── GetCountiesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetCountiesAsync_CallsBSECountiesOnRepository()
    {
        var items = new[] { new LuBSECounty { Id = 1, Code = "AB", Description = "Aberdeen" } };
        _repo.GetBSECountiesAsync().Returns(items);

        var result = await _sut.GetCountiesAsync();

        result.Should().BeEquivalentTo(items);
        await _repo.Received(1).GetBSECountiesAsync();
    }

    // ── GetBSERegionsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetBSERegionsAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuBSERegion { Id = 1, SortOrder = 1, Name = "Scotland" } };
        _repo.GetBSERegionsAsync().Returns(items);

        var result = await _sut.GetBSERegionsAsync();

        result.Should().BeEquivalentTo(items);
    }

    // ── GetTestTypesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetTestTypesAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuTestType { Id = 1, Code = "IHC", Description = "IHC", IsActive = true } };
        _repo.GetTestTypesAsync().Returns(items);

        var result = await _sut.GetTestTypesAsync();

        result.Should().BeEquivalentTo(items);
    }

    // ── GetTestResultsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTestResultsAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuTestResult { Id = 1, Code = "POS", Description = "Positive" } };
        _repo.GetTestResultsAsync().Returns(items);

        var result = await _sut.GetTestResultsAsync();

        result.Should().BeEquivalentTo(items);
    }

    // ── GetUserGroupsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserGroupsAsync_CallsRepository_AndReturnsResults()
    {
        var items = new[] { new LuUserGroup { Id = 1, Name = "Vet" } };
        _repo.GetUserGroupsAsync().Returns(items);

        var result = await _sut.GetUserGroupsAsync();

        result.Should().BeEquivalentTo(items);
    }

    // ── GetLookupAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLookupAsync_Breed_ProjectsFullNameAsDescription()
    {
        _repo.GetBreedsAsync().Returns(new[]
        {
            new LuBreed { Id = 5, Code = "HF", FullName = "Hereford", AmalgamatedName = "Hereford" }
        });

        var result = (await _sut.GetLookupAsync(LookupTableId.Breed)).ToList();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(5);
        result[0].Description.Should().Be("Hereford");
    }

    [Fact]
    public async Task GetLookupAsync_ADNSRegion_ProjectsNameAsDescription()
    {
        _repo.GetADNSRegionsAsync().Returns(new[]
        {
            new LuADNSRegion { Id = 3, Name = "Wales" }
        });

        var result = (await _sut.GetLookupAsync(LookupTableId.ADNSRegion)).ToList();

        result[0].Id.Should().Be(3);
        result[0].Description.Should().Be("Wales");
    }

    [Fact]
    public async Task GetLookupAsync_CaseType_ProjectsDescriptionField()
    {
        _repo.GetCaseTypesAsync().Returns(new[]
        {
            new LuCaseType { Id = 2, Code = "BS", Description = "BSE Suspect" }
        });

        var result = (await _sut.GetLookupAsync(LookupTableId.CaseType)).ToList();

        result[0].Description.Should().Be("BSE Suspect");
    }

    [Fact]
    public async Task GetLookupAsync_UnknownTableId_ThrowsArgumentOutOfRange()
    {
        var act = () => _sut.GetLookupAsync((LookupTableId)999);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
