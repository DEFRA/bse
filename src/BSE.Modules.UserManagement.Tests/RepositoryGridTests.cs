using BSE.Infrastructure;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Repositories;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.UserManagement.Tests.Db;
using FluentAssertions;
using NSubstitute;
using AnimalDamSire = BSE.Modules.AnimalRelations.Models.DamSireDetailRecord;
using AnimalRelation = BSE.Modules.AnimalRelations.Models.CaseRelationRecord;

namespace BSE.Modules.UserManagement.Tests;

public sealed class RepositoryGridTests
{
    private static (FakeDbConnection Connection, IDbConnectionFactory Factory) Create()
    {
        var connection = new FakeDbConnection();
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        return (connection, factory);
    }

    private static void EnqueueCaseDetails(FakeDbConnection connection, bool includeCase)
    {
        connection.Enqueue(
            includeCase ? FakeDbConnection.Table(new CaseRecord { Rbse = "002600001", Cphh = "01001000101" }) : FakeDbConnection.Table<CaseRecord>(),
            FakeDbConnection.Table(new CaseClinicalRecord { Rbse = "002600001", Tremor = true }),
            FakeDbConnection.Table(new CaseBabRecord { Rbse = "002600001", Notes = "n" }),
            FakeDbConnection.Table(new OtherOwnerRecord(1, "002600001", "T", "Name", null, null)),
            FakeDbConnection.Table(new CaseFeedRecord { Id = 2, Rbse = "002600001", YearFrom = 2000 }),
            FakeDbConnection.Table(new ClinicalVisitRecord(3, "002600001", new DateTime(2020, 1, 1), null)),
            FakeDbConnection.Table(new DamSireDetailRecord { Id = 4, Rbse = "002700001" }),
            FakeDbConnection.Table(new DamSireDetailRecord { Id = 5, Rbse = "002700002" }),
            FakeDbConnection.Table(new CaseRelationRecord { Id = 6, Rbse = "002600001", RelationType = "Offspring" }),
            FakeDbConnection.Table<CaseTestRecord>(),
            FakeDbConnection.Table(new CaseWorkRecord { Rbse = "002600001" }));
    }

    [Fact]
    public async Task GetCaseDetails_MapsAllGrids()
    {
        var (connection, factory) = Create();
        EnqueueCaseDetails(connection, includeCase: true);

        var result = await new CaseRepository(factory).GetCaseDetailsByRbseAsync("002600001");

        result.Should().NotBeNull();
        result!.OtherOwners.Should().ContainSingle();
        result.Feeds.Should().ContainSingle();
        result.ClinicalVisits.Should().ContainSingle();
        result.Relations.Should().ContainSingle();
        result.Tests.Should().BeEmpty();
        connection.Commands.Should().Contain(c => c.Text == "GetCaseDetailsByRBSE");
    }

    [Fact]
    public async Task GetCaseDetails_WithoutCase_ReturnsNull()
    {
        var (connection, factory) = Create();
        EnqueueCaseDetails(connection, includeCase: false);

        (await new CaseRepository(factory).GetCaseDetailsByRbseAsync("002600001")).Should().BeNull();
    }

    [Fact]
    public async Task GetRelationsDetails_MapsDamSireAndRelations()
    {
        var (connection, factory) = Create();
        connection.Enqueue(
            FakeDbConnection.Table(new AnimalDamSire { Id = 1, Rbse = "002700001" }),
            FakeDbConnection.Table<AnimalDamSire>(),
            FakeDbConnection.Table(new AnimalRelation { Id = 2, Rbse = "002600001", RelationType = "Offspring" }));

        var result = await new AnimalRelationsRepository(factory).GetRelationsDetailsByRbseAsync("002600001");

        result.Dam.Should().NotBeNull();
        result.Sire.Should().BeNull();
        result.Relations.Should().ContainSingle();
    }

    [Fact]
    public async Task GetCasesForGb_ReturnsBothGrids()
    {
        var (connection, factory) = Create();
        connection.Enqueue(
            FakeDbConnection.Table(new AdnsCaseRecord()),
            FakeDbConnection.Table<MissingAdnsCaseRecord>());

        var (cases, missing) = await new AdnsRepository(factory).GetCasesForGbAsync(2024, 1);

        cases.Should().HaveCount(1);
        missing.Should().BeEmpty();
    }
}
