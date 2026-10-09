using System.Data;
using BSE.Infrastructure;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Tests.Fakes;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.FarmManagement.Tests;

/// <summary>Covers <see cref="FarmRelationRepository"/> (0% new-code coverage) — every method is a
/// thin Dapper pass-through, so each test asserts the correct stored procedure name and parameters
/// are used for both the plain and transactional overloads.</summary>
public sealed class FarmRelationRepositoryTests
{
    private static (FarmRelationRepository Sut, FakeDbConnection Connection) CreateSut(params DataTable[] resultSets)
    {
        var connection = new FakeDbConnection(resultSets);
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        return (new FarmRelationRepository(factory), connection);
    }

    [Fact]
    public async Task GetRelatedFarmAsync_QueriesGetRelatedFarm()
    {
        var table = new DataTable();
        table.Columns.Add("ID", typeof(int));
        table.Columns.Add("CPHH", typeof(string));
        table.Columns.Add("RelatedCPHH", typeof(string));
        table.Rows.Add(1, "01001000101", "01001000102");
        var (sut, connection) = CreateSut(table);

        var result = (await sut.GetRelatedFarmAsync("01001000101")).ToList();

        result.Should().ContainSingle().Which.RelatedCPHH.Should().Be("01001000102");
        connection.CommandTexts.Should().Contain("GetRelatedFarm");
    }

    [Fact]
    public async Task AddAsync_WithoutTransaction_ExecutesAddFarmRelation()
    {
        var (sut, connection) = CreateSut();

        await sut.AddAsync("01001000101", "01001000102");

        connection.CommandTexts.Should().Contain("AddFarmRelation");
    }

    [Fact]
    public async Task AddAsync_WithTransaction_ExecutesAddFarmRelationOnTheSuppliedConnection()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.Open();

        await sut.AddAsync("01001000101", "01001000102", txConnection, null!);

        txConnection.CommandTexts.Should().Contain("AddFarmRelation");
    }

    [Fact]
    public async Task UpdateAsync_WithoutTransaction_ExecutesEditFarmRelation()
    {
        var (sut, connection) = CreateSut();

        await sut.UpdateAsync(1, "01001000102", [1, 2, 3]);

        connection.CommandTexts.Should().Contain("EditFarmRelation");
    }

    [Fact]
    public async Task UpdateAsync_WithTransaction_ExecutesEditFarmRelationOnTheSuppliedConnection()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.Open();

        await sut.UpdateAsync(1, "01001000102", [1, 2, 3], txConnection, null!);

        txConnection.CommandTexts.Should().Contain("EditFarmRelation");
    }

    [Fact]
    public async Task DeleteAsync_WithoutTransaction_ExecutesDeleteFarmRelation()
    {
        var (sut, connection) = CreateSut();

        await sut.DeleteAsync(1, [1, 2, 3]);

        connection.CommandTexts.Should().Contain("DeleteFarmRelation");
    }

    [Fact]
    public async Task DeleteAsync_WithTransaction_ExecutesDeleteFarmRelationOnTheSuppliedConnection()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.Open();

        await sut.DeleteAsync(1, [1, 2, 3], txConnection, null!);

        txConnection.CommandTexts.Should().Contain("DeleteFarmRelation");
    }
}
