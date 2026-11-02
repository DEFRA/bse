using System.Data;
using BSE.Host.Services;
using HostGeoLookupService = BSE.Host.Services.GeoLookupService;
using BSE.Infrastructure;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Repositories;
using BSE.Modules.UserManagement.Tests.Db;
using BSE.SharedKernel;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class GeoLookupServiceTests
{
    private readonly IDbRepository _db = Substitute.For<IDbRepository>();
    private readonly HostGeoLookupService _sut;

    public GeoLookupServiceTests() => _sut = new HostGeoLookupService(_db);

    private void GivenRows(params ParishMapReferenceRow[] rows) =>
        _db.QueryAsync<ParishMapReferenceRow>("GetMapReferenceByCountyParish", Arg.Any<object?>()).Returns(rows);

    private static ParishMapReferenceRow Row(string x, string y1, string y2) =>
        new() { XReference1 = x, YReference1 = y1, XReference2 = x, YReference2 = y2 };

    [Fact]
    public async Task GetAllParishMapReferences_ReturnsRows()
    {
        GivenRows(Row("1234", "0300", "0310"));

        (await _sut.GetAllParishMapReferencesAsync("01", "001")).Should().ContainSingle();
    }


    private static DataTable Grid(params string[][] rows)
    {
        var table = new DataTable();
        foreach (var column in new[] { "XReference1", "YReference1", "XReference2", "YReference2" })
            table.Columns.Add(column, typeof(string));
        foreach (var row in rows) table.Rows.Add(row);
        return table;
    }

    private static DataTable PrefixCode(string? code)
    {
        var table = new DataTable();
        table.Columns.Add("Code", typeof(string));
        if (code is not null) table.Rows.Add(code);
        return table;
    }

    private static (FakeDbConnection Connection, HostGeoLookupService Service) CreateReal()
    {
        var connection = new FakeDbConnection();
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        return (connection, new HostGeoLookupService(new DapperRepository(factory)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Estimate_WithKnownGridSquare_BuildsTheReference(int rowCount)
    {
        var (connection, service) = CreateReal();
        connection.Enqueue(Grid(Enumerable.Range(0, rowCount).Select(_ => new[] { "1234", "0300", "1234", "0310" }).ToArray()));
        connection.Enqueue(PrefixCode("TQ"));

        var result = await service.EstimateMapReferenceAsync("01001000101");

        result.Error.Should().BeNull();
        result.MapRef1.Should().Be("TQ");
        result.MapRef2.Should().Be("345");
        result.MapRef3.Should().Be("055");
    }

    [Fact]
    public async Task Estimate_NonNumericNorthings_UsesTheStartValue()
    {
        var (connection, service) = CreateReal();
        connection.Enqueue(Grid(["1234", "0700", "1234", "xxxx"]));
        connection.Enqueue(PrefixCode("SU"));

        var result = await service.EstimateMapReferenceAsync("01001000101");

        result.MapRef1.Should().Be("SU");
        result.MapRef3.Should().Be("005");
    }

    [Fact]
    public async Task GetPrefixCode_ReturnsTheCodeFromTheRow()
    {
        var (connection, service) = CreateReal();
        connection.Enqueue(PrefixCode("TQ"));

        (await service.GetPrefixCodeAsync("2", "3")).Should().Be("TQ");
    }

    [Fact]
    public async Task GetXYCoordsByPrefixCode_DelegatesToRepository()
    {
        var expected = new MapPrefixXY { Code = "TQ", XCoordPrefix = "5", YCoordPrefix = "1" };
        _db.QuerySingleOrDefaultAsync<MapPrefixXY>("GetXYReferenceByPrefixCode", Arg.Any<object?>()).Returns(expected);

        (await _sut.GetXYCoordsByPrefixCodeAsync("TQ")).Should().BeSameAs(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0100")]
    public async Task Estimate_ShortCphh_ReturnsError(string cphh)
    {
        (await _sut.EstimateMapReferenceAsync(cphh)).Error.Should().Contain("at least 5 characters");
    }

    [Fact]
    public async Task Estimate_NoParishData_ReturnsError()
    {
        GivenRows();

        (await _sut.EstimateMapReferenceAsync("01001000101")).Error.Should().Contain("No map reference data");
    }

    [Fact]
    public async Task Estimate_UnknownGridSquare_ReturnsError()
    {
        GivenRows(Row("1234", "0300", "0310"));

        (await _sut.EstimateMapReferenceAsync("01001000101")).Error.Should().Contain("Could not determine OS grid square");
    }

    [Theory]
    [InlineData("0100", "TQ123456")]
    [InlineData("01001", "TQ12")]
    public async Task IsWithinParish_ShortInputs_AreTreatedAsValid(string cphh, string mapRef)
    {
        (await _sut.IsMapReferenceWithinParishAsync(cphh, mapRef)).Should().BeTrue();
    }

    [Fact]
    public async Task IsWithinParish_UnknownPrefix_IsInvalid()
    {
        _db.QuerySingleOrDefaultAsync<MapPrefixXY>("GetXYReferenceByPrefixCode", Arg.Any<object?>()).Returns((MapPrefixXY?)null);

        (await _sut.IsMapReferenceWithinParishAsync("01001000101", "tq123456")).Should().BeFalse();
    }

    [Theory]
    [InlineData("TQ125305", true)]
    [InlineData("TQ125390", false)]
    [InlineData("TQ995305", false)]
    public async Task IsWithinParish_ComparesAgainstParishBoundingRows(string mapRef, bool expected)
    {
        _db.QuerySingleOrDefaultAsync<MapPrefixXY>("GetXYReferenceByPrefixCode", Arg.Any<object?>())
            .Returns(new MapPrefixXY { Code = "TQ", XCoordPrefix = "5", YCoordPrefix = "1" });
        GivenRows(Row("512", "125", "135"));
        var rows = new[] { Row("512", "125", "135") };
        _db.QueryAsync<ParishMapReferenceRow>("GetMapReferenceByCountyParish", Arg.Any<object?>()).Returns(rows);

        var result = await _sut.IsMapReferenceWithinParishAsync("01001000101", mapRef);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task IsWithinParish_NoRows_IsInvalid()
    {
        _db.QuerySingleOrDefaultAsync<MapPrefixXY>("GetXYReferenceByPrefixCode", Arg.Any<object?>())
            .Returns(new MapPrefixXY { Code = "TQ", XCoordPrefix = "5", YCoordPrefix = "1" });
        GivenRows();

        (await _sut.IsMapReferenceWithinParishAsync("01001000101", "tq123456")).Should().BeFalse();
    }
}

public sealed class LookupDataServiceCoverageTests
{
    private readonly ILookupRepository _repo = Substitute.For<ILookupRepository>();

    [Theory]
    [MemberData(nameof(AllTables))]
    public async Task GetLookupAsync_ResolvesEveryTable(LookupTableId id)
    {
        var sut = new LookupDataService(_repo);

        var result = await sut.GetLookupAsync(id);

        result.Should().NotBeNull();
    }

    public static TheoryData<LookupTableId> AllTables() => [.. Enum.GetValues<LookupTableId>().Where(v => v != LookupTableId.LocalAuthority)];

    [Fact]
    public async Task GetLookupAsync_UnknownTable_Throws()
    {
        var sut = new LookupDataService(_repo);

        var act = () => sut.GetLookupAsync((LookupTableId)9999);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task TypedGetters_DelegateToRepository()
    {
        var sut = new LookupDataService(_repo);

        await sut.GetBreedsAsync();
        await sut.GetCaseTypesAsync();
        await sut.GetCaseFatesAsync();
        await sut.GetAnimalStatusesAsync();
        await sut.GetAnimalOriginsAsync();
        await sut.GetSexesAsync();
        await sut.GetHerdTypesAsync();
        await sut.GetADNSRegionsAsync();
        await sut.GetCountiesAsync();
        await sut.GetBSERegionsAsync();
        await sut.GetTestTypesAsync();
        await sut.GetTestResultsAsync();
        await sut.GetUserGroupsAsync();
        await sut.GetAuthoritiesByCountyAsync(1);
        await sut.GetADNSRegionsByAuthorityAsync(2);

        await _repo.Received(1).GetBreedsAsync();
        await _repo.Received(1).GetBSECountiesAsync();
        await _repo.Received(1).GetAuthoritiesByCountyAsync(1);
        await _repo.Received(1).GetADNSRegionsByAuthorityAsync(2);
    }
}

public sealed class UserRepositoryMappingTests
{
    private static DataTable UsersTable(params object[][] rows)
    {
        var table = new DataTable();
        table.Columns.Add("ID", typeof(int));
        table.Columns.Add("NTLogin", typeof(string));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("UserGroup", typeof(int));
        table.Columns.Add("IsActive", typeof(bool));
        foreach (var row in rows) table.Rows.Add(row);
        return table;
    }

    private static (FakeDbConnection Connection, UserRepository Repository) Create()
    {
        var connection = new FakeDbConnection();
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        return (connection, new UserRepository(factory));
    }

    [Fact]
    public async Task GetAll_MapsRowsAndFallsBackToNoneForUnknownGroups()
    {
        var (connection, repository) = Create();
        connection.Enqueue(UsersTable(
            [1, "a1", "Alice", "alice@example.test", (int)UserGroup.DataEntry, true],
            [2, "b2", "Bob", DBNull.Value, 9999, false]));

        var users = (await repository.GetAllAsync()).ToList();

        users.Should().HaveCount(2);
        users[0].UserName.Should().Be("Alice");
        users[0].UserGroup.Should().Be(UserGroup.DataEntry);
        users[1].UserGroup.Should().Be(UserGroup.None);
        users[1].IsActive.Should().BeFalse();
        users[1].Email.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmail_MapsTheRow()
    {
        var (connection, repository) = Create();
        var table = new DataTable();
        foreach (var (name, type) in new (string, Type)[] { ("ID", typeof(int)), ("NTLogin", typeof(string)), ("Name", typeof(string)), ("UserGroup", typeof(int)), ("GroupName", typeof(string)), ("Email", typeof(string)) })
            table.Columns.Add(name, type);
        table.Rows.Add(7, "nt7", "Seven", (int)UserGroup.DataEntry, "Data Entry", "seven@example.test");
        connection.Enqueue(table);

        var user = await repository.GetByEmailAsync("seven@example.test");

        user.Should().NotBeNull();
        user!.UserId.Should().Be(7);
        user.NTLogin.Should().Be("nt7");
        user.GroupName.Should().Be("Data Entry");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByNtLogin_MapsTheRow()
    {
        var (connection, repository) = Create();
        var table = new DataTable();
        foreach (var (name, type) in new (string, Type)[] { ("ID", typeof(int)), ("Name", typeof(string)), ("UserGroup", typeof(int)), ("GroupName", typeof(string)), ("Email", typeof(string)) })
            table.Columns.Add(name, type);
        table.Rows.Add(8, "Eight", (int)UserGroup.DataEntry, "Data Entry", "eight@example.test");
        connection.Enqueue(table);

        var user = await repository.GetByNtLoginAsync("nt8");

        user!.NTLogin.Should().Be("nt8");
        user.Email.Should().Be("eight@example.test");
        user.UserName.Should().Be("Eight");
    }

    [Fact]
    public async Task GetByEmailAndNtLogin_WithNoRow_ReturnNull()
    {
        var (_, repository) = Create();

        (await repository.GetByEmailAsync("none@example.test")).Should().BeNull();
        (await repository.GetByNtLoginAsync("none")).Should().BeNull();
    }

    [Fact]
    public async Task AddAndUpdate_SendTheUserFieldsToTheStoredProcedures()
    {
        var (connection, repository) = Create();
        var user = new User(0, "nt9", null, "Nine", "nine@example.test", true, (int)UserGroup.DataEntry, UserGroup.DataEntry);

        await repository.AddAsync(user);
        await repository.UpdateAsync(user with { UserId = 9 });

        connection.Commands.Should().Contain(c => c.Text == "AddUser" && (string?)c.Parameters["NTLogin"] == "nt9");
        connection.Commands.Should().Contain(c => c.Text == "EditUser" && (int?)c.Parameters["ID"] == 9);
    }
}
