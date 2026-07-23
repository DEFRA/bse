using FluentAssertions;
using Xunit;

namespace BSE.Modules.ReferenceData.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class LookupIntegrationTests : ReferenceDataIntegrationTestBase
{
    [Fact]
    public async Task GetBreedsAsync_ReturnsExpectedShape()
    {
        var breeds = (await LookupDataService.GetBreedsAsync()).ToList();

        breeds.Should().NotBeEmpty();
        breeds.Should().AllSatisfy(b =>
        {
            b.Id.Should().BeGreaterThan(0);
            b.Code.Should().NotBeNullOrWhiteSpace();
            b.FullName.Should().NotBeNullOrWhiteSpace();
            b.AmalgamatedName.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task GetBreedsAsync_OrderedByFullName()
    {
        var breeds = (await LookupDataService.GetBreedsAsync()).ToList();

        // GetluBreed orders by FullName — Aberdeen Angus < Hereford
        breeds.Should().HaveCountGreaterThanOrEqualTo(2);
        breeds[0].FullName.Should().Be("Aberdeen Angus");
        breeds[1].FullName.Should().Be("Hereford");
    }

    [Fact]
    public async Task GetMapReferenceAsync_ReturnsExpectedShape()
    {
        var mapRef = await GeoLookupService.GetMapReferenceAsync("AB", "001");

        mapRef.Should().NotBeNull();
        mapRef!.XReference1.Should().NotBeNullOrWhiteSpace();
        mapRef.YReference1.Should().NotBeNullOrWhiteSpace();
        mapRef.XReference2.Should().NotBeNullOrWhiteSpace();
        mapRef.YReference2.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetMapReferenceAsync_UnknownCountyParish_ReturnsNull()
    {
        var mapRef = await GeoLookupService.GetMapReferenceAsync("ZZ", "999");

        mapRef.Should().BeNull();
    }
}
