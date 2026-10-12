using BSE.Host.Services;
using BSE.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests.Services;

/// <summary>Reported bug: a non-GB CPHH (county/parish "00"/"999", with no real OS grid boundary
/// data) made IsMapReferenceWithinParishAsync return true ("valid", no warning), while legacy's
/// ValidateMapReference falls through to False in the exact same no-data case — legacy always shows
/// the green "isn't within the county" warning star once a non-GB farm's Map Reference is entered.</summary>
public sealed class GeoLookupServiceTests
{
    private readonly IDbRepository _db = Substitute.For<IDbRepository>();
    private GeoLookupService CreateService() => new(_db);

    [Fact]
    public async Task IsMapReferenceWithinParishAsync_NoParishBoundaryData_ReturnsFalse()
    {
        _db.QuerySingleOrDefaultAsync<MapPrefixXY>("GetXYReferenceByPrefixCode", Arg.Any<object>())
            .Returns(new MapPrefixXY { Code = "NX", XCoordPrefix = "08", YCoordPrefix = "44" });
        _db.QueryAsync<ParishMapReferenceRow>("GetMapReferenceByCountyParish", Arg.Any<object>())
            .Returns(Array.Empty<ParishMapReferenceRow>());

        var sut = CreateService();

        var result = await sut.IsMapReferenceWithinParishAsync("00999900103", "NX084441");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsMapReferenceWithinParishAsync_WithinParishBounds_ReturnsTrue()
    {
        _db.QuerySingleOrDefaultAsync<MapPrefixXY>("GetXYReferenceByPrefixCode", Arg.Any<object>())
            .Returns(new MapPrefixXY { Code = "SO", XCoordPrefix = "12", YCoordPrefix = "34" });
        _db.QueryAsync<ParishMapReferenceRow>("GetMapReferenceByCountyParish", Arg.Any<object>())
            .Returns([new ParishMapReferenceRow { XReference1 = "1234", YReference1 = "3400", YReference2 = "3499" }]);

        var sut = CreateService();

        var result = await sut.IsMapReferenceWithinParishAsync("01001000101", "SO345450");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsMapReferenceWithinParishAsync_PrefixCodeNotFound_ReturnsFalse()
    {
        _db.QuerySingleOrDefaultAsync<MapPrefixXY>("GetXYReferenceByPrefixCode", Arg.Any<object>())
            .Returns((MapPrefixXY?)null);

        var sut = CreateService();

        var result = await sut.IsMapReferenceWithinParishAsync("01001000101", "ZZ123450");

        result.Should().BeFalse();
    }
}
