using System.Data;
using BSE.Infrastructure;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Tests.Fakes;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.FarmManagement.Tests;

/// <summary>Covers <see cref="HerdSizeRepository"/> (0% new-code coverage) — every method is a thin
/// Dapper pass-through, so each test asserts the correct stored procedure name is used for both the
/// plain and transactional overloads.</summary>
public sealed class HerdSizeRepositoryTests
{
    private const string Cphh = "01001000101";

    private static (HerdSizeRepository Sut, FakeDbConnection Connection) CreateSut(params DataTable[] resultSets)
    {
        var connection = new FakeDbConnection(resultSets);
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        return (new HerdSizeRepository(factory), connection);
    }

    private static AddHerdSizeCommand ValidAddCommand() => new(
        CPHH: Cphh, HerdYear: 2020, TotalSize: 50,
        Lactation1Size: 10, Lactation2Size: null, Lactation3Size: null, Lactation4Size: null,
        Lactation5Size: null, Lactation6Size: null, Lactation7Size: null, Lactation8Size: null,
        Lactation9Size: null, Lactation10Size: null, Lactation10PlusSize: null);

    private static UpdateHerdSizeCommand ValidUpdateCommand() => new(
        ID: 1, HerdYear: 2020, TotalSize: 50,
        Lactation1Size: 10, Lactation2Size: null, Lactation3Size: null, Lactation4Size: null,
        Lactation5Size: null, Lactation6Size: null, Lactation7Size: null, Lactation8Size: null,
        Lactation9Size: null, Lactation10Size: null, Lactation10PlusSize: null, RowStamp: [1, 2, 3]);

    [Fact]
    public async Task GetByCphhAsync_QueriesGetHerdSizeByCphh()
    {
        var table = new DataTable();
        table.Columns.Add("ID", typeof(int));
        table.Columns.Add("CPHH", typeof(string));
        table.Columns.Add("HerdYear", typeof(short));
        table.Columns.Add("TotalSize", typeof(short));
        table.Rows.Add(1, Cphh, (short)2020, (short)50);
        var (sut, connection) = CreateSut(table);

        var result = (await sut.GetByCphhAsync(Cphh)).ToList();

        result.Should().ContainSingle().Which.TotalSize.Should().Be(50);
        connection.CommandTexts.Should().Contain("GetHerdSizeByCPHH");
    }

    [Fact]
    public async Task GetByBatchIdAsync_QueriesGetHerdDetailByBatchId()
    {
        var table = new DataTable();
        table.Columns.Add("RBSE", typeof(string));
        table.Columns.Add("CPHH", typeof(string));
        table.Rows.Add("002600001", Cphh);
        var (sut, connection) = CreateSut(table);

        var result = (await sut.GetByBatchIdAsync(42)).ToList();

        result.Should().ContainSingle().Which.CPHH.Should().Be(Cphh);
        connection.CommandTexts.Should().Contain("GetHerdDetailByBatchID");
    }

    [Fact]
    public async Task AddAsync_WithoutTransaction_ExecutesAddHerdSize()
    {
        var (sut, connection) = CreateSut();

        await sut.AddAsync(ValidAddCommand());

        connection.CommandTexts.Should().Contain("AddHerdSize");
    }

    [Fact]
    public async Task AddAsync_WithTransaction_ExecutesAddHerdSizeOnTheSuppliedConnection()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.Open();

        await sut.AddAsync(ValidAddCommand(), txConnection, null!);

        txConnection.CommandTexts.Should().Contain("AddHerdSize");
    }

    [Fact]
    public async Task UpdateAsync_WithoutTransaction_ExecutesEditHerdSize()
    {
        var (sut, connection) = CreateSut();

        await sut.UpdateAsync(ValidUpdateCommand());

        connection.CommandTexts.Should().Contain("EditHerdSize");
    }

    [Fact]
    public async Task UpdateAsync_WithTransaction_ExecutesEditHerdSizeOnTheSuppliedConnection()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.Open();

        await sut.UpdateAsync(ValidUpdateCommand(), txConnection, null!);

        txConnection.CommandTexts.Should().Contain("EditHerdSize");
    }

    [Fact]
    public async Task DeleteAsync_WithoutTransaction_ExecutesDeleteHerdSize()
    {
        var (sut, connection) = CreateSut();

        await sut.DeleteAsync(1, [1, 2, 3]);

        connection.CommandTexts.Should().Contain("DeleteHerdSize");
    }

    [Fact]
    public async Task DeleteAsync_WithTransaction_ExecutesDeleteHerdSizeOnTheSuppliedConnection()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.Open();

        await sut.DeleteAsync(1, [1, 2, 3], txConnection, null!);

        txConnection.CommandTexts.Should().Contain("DeleteHerdSize");
    }
}
