using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BSE.Modules.ReferenceData.Tests;

public sealed class GeoLookupServiceTests
{
    private readonly ILookupRepository _repo = Substitute.For<ILookupRepository>();
    private readonly GeoLookupService _sut;

    public GeoLookupServiceTests() => _sut = new GeoLookupService(_repo);

    [Fact]
    public async Task GetMapReferenceAsync_DelegatesToRepository_WithCorrectParams()
    {
        var expected = new GeoMapReference
        {
            XReference1 = "012", YReference1 = "034",
            XReference2 = "056", YReference2 = "078"
        };
        _repo.GetMapReferenceByCountyParishAsync("AB", "001").Returns(expected);

        var result = await _sut.GetMapReferenceAsync("AB", "001");

        result.Should().BeEquivalentTo(expected);
        await _repo.Received(1).GetMapReferenceByCountyParishAsync("AB", "001");
    }

    [Fact]
    public async Task GetMapReferenceAsync_ReturnsNull_WhenCountyParishNotFound()
    {
        _repo.GetMapReferenceByCountyParishAsync(Arg.Any<string>(), Arg.Any<string>())
             .Returns((GeoMapReference?)null);

        var result = await _sut.GetMapReferenceAsync("ZZ", "999");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetParishAsync_DelegatesToRepository_WithCorrectParams()
    {
        var expected = new ParishLookup
        {
            County = "AB", Parish = "001", Name = "Aboyne", ADNSRegionID = 5
        };
        _repo.GetParishByCountyParishAsync("AB", "001").Returns(expected);

        var result = await _sut.GetParishAsync("AB", "001");

        result.Should().BeEquivalentTo(expected);
        await _repo.Received(1).GetParishByCountyParishAsync("AB", "001");
    }

    [Fact]
    public async Task GetNonGBCountyAsync_DelegatesToRepository()
    {
        var counties = new[] { new LuBSECounty { Id = 1, Code = "NL", Description = "Netherlands" } };
        _repo.GetNonGBCountiesAsync().Returns(counties);

        var result = await _sut.GetNonGBCountyAsync();

        result.Should().BeEquivalentTo(counties);
        await _repo.Received(1).GetNonGBCountiesAsync();
    }
}
