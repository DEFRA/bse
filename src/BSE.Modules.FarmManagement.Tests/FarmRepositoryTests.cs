using System.Data;
using BSE.Infrastructure;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Tests.Fakes;
using BSE.SharedKernel;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.FarmManagement.Tests;

/// <summary>Covers <see cref="FarmRepository"/>, previously untested (0% new-code coverage) — both the
/// plain pass-through query/command methods and the <see cref="FarmRepository.UpdateAsync"/> return-code
/// switch, which has hard-failure branches no other test exercised.</summary>
public sealed class FarmRepositoryTests
{
    private const string Cphh = "01001000101";

    private static DataTable SingleRow(params (string Column, object Value)[] columns)
    {
        var table = new DataTable();
        foreach (var (column, value) in columns)
            table.Columns.Add(column, value.GetType());
        var row = table.NewRow();
        foreach (var (column, value) in columns)
            row[column] = value;
        table.Rows.Add(row);
        return table;
    }

    private static (FarmRepository Sut, FakeDbConnection Connection) CreateSut(params DataTable[] resultSets)
    {
        var connection = new FakeDbConnection(resultSets);
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        return (new FarmRepository(factory), connection);
    }

    [Fact]
    public async Task GetByCphhAsync_WhenFarmExists_QueriesGetFarmByCphhAndMapsResult()
    {
        var (sut, connection) = CreateSut(SingleRow(("CPHH", Cphh), ("OwnerName", "Alice")));

        var result = await sut.GetByCphhAsync(Cphh);

        result.Should().NotBeNull();
        result!.CPHH.Should().Be(Cphh);
        result.OwnerName.Should().Be("Alice");
        connection.CommandTexts.Should().Contain("GetFarmByCPHH");
    }

    [Fact]
    public async Task GetByCphhAsync_WhenFarmDoesNotExist_ReturnsNull()
    {
        var (sut, _) = CreateSut(new DataTable());

        var result = await sut.GetByCphhAsync(Cphh);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDetailsByCphhAsync_CombinesFarmRelationsAndHerdSizesFromThreeQueries()
    {
        var (sut, connection) = CreateSut(
            SingleRow(("CPHH", Cphh), ("OwnerName", "Alice")),
            SingleRow(("ID", 1), ("CPHH", Cphh), ("RelatedCPHH", "01001000102")),
            SingleRow(("ID", 2), ("CPHH", Cphh), ("HerdYear", (short)2020), ("TotalSize", (short)50)));

        var result = await sut.GetDetailsByCphhAsync(Cphh);

        result.Farm.Should().NotBeNull();
        result.RelatedFarms.Should().ContainSingle().Which.RelatedCPHH.Should().Be("01001000102");
        result.HerdSizes.Should().ContainSingle().Which.TotalSize.Should().Be(50);
        connection.CommandTexts.Should().ContainInOrder("GetFarmByCPHH", "GetRelatedFarm", "GetHerdSizeByCPHH");
    }

    [Fact]
    public async Task GetByCphAsync_QueriesGetFarmsByCph()
    {
        var (sut, connection) = CreateSut(new DataTable());

        await sut.GetByCphAsync("0100100");

        connection.CommandTexts.Should().Contain("GetFarmsByCPH");
    }

    private static UpdateFarmCommand ValidUpdateFarmCommand() => new(
        CPHH: Cphh, OwnerName: "Owner", Address1: "1 Farm Lane", Address2: null, Address3: null,
        Postcode: null, Parish: "Parish", District: null, County: "County",
        CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
        CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
        Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "AHO",
        HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: 5, RowStamp: [1, 2, 3]);

    [Fact]
    public async Task AddAsync_WithoutTransaction_ExecutesAddFarm()
    {
        var (sut, connection) = CreateSut();

        await sut.AddAsync(new AddFarmCommand(
            CPHH: Cphh, OwnerName: "Owner", Address1: "1 Farm Lane", Address2: null, Address3: null,
            Postcode: null, Parish: "Parish", District: null, County: "County",
            CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
            CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
            Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "AHO",
            HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: 5), userId: 1);

        connection.CommandTexts.Should().Contain("AddFarm");
    }

    [Fact]
    public async Task AddAsync_WithTransaction_UsesSuppliedConnectionAndTransaction()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var command = new AddFarmCommand(
            CPHH: Cphh, OwnerName: "Owner", Address1: "1 Farm Lane", Address2: null, Address3: null,
            Postcode: null, Parish: "Parish", District: null, County: "County",
            CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
            CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
            Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "AHO",
            HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: 5);

        await sut.AddAsync(command, userId: 1, txConnection, null!);

        txConnection.CommandTexts.Should().Contain("AddFarm");
    }

    [Fact]
    public async Task UpdateAsync_WhenEditFarmReturnsZero_ReturnsNull()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditFarm"] = 0;
        txConnection.Open();

        var result = await sut.UpdateAsync(ValidUpdateFarmCommand(), userId: 1, txConnection, null!);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task UpdateAsync_WhenEditFarmReturnsAHardFailureCode_Throws(int code)
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditFarm"] = code;
        txConnection.Open();

        var act = () => sut.UpdateAsync(ValidUpdateFarmCommand(), userId: 1, txConnection, null!);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenEditFarmReturnsThree_ReturnsModifiedByAnotherUserWarning()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditFarm"] = 3;
        txConnection.Open();

        var result = await sut.UpdateAsync(ValidUpdateFarmCommand(), userId: 1, txConnection, null!);

        result.Should().Be($"The farm record with CPHH {Cphh} has been modified by another user");
    }

    [Fact]
    public async Task UpdateAsync_WhenEditFarmReturnsAnUnexpectedCode_ThrowsWithTheCodeInTheMessage()
    {
        var (sut, _) = CreateSut();
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditFarm"] = 99;
        txConnection.Open();

        var act = () => sut.UpdateAsync(ValidUpdateFarmCommand(), userId: 1, txConnection, null!);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*99*");
    }

    [Theory]
    [InlineData(0, ChangeCphhResult.Success)]
    [InlineData(1, ChangeCphhResult.OldCphhNotFoundOrNewCphhAlreadyExists)]
    public async Task ChangeCphhAsync_MapsTheReturnCodeToTheResultEnum(int code, ChangeCphhResult expected)
    {
        var (sut, connection) = CreateSut();
        connection.ReturnValues["ChangeCPHH"] = code;

        var result = await sut.ChangeCphhAsync("01001000101", "01001000102", userId: 1);

        result.Should().Be(expected);
        connection.CommandTexts.Should().Contain("ChangeCPHH");
    }

    [Fact]
    public async Task GetConfirmedCaseCountAsync_WhenNoRowsReturned_ReturnsZero()
    {
        var (sut, _) = CreateSut(new DataTable());

        var result = await sut.GetConfirmedCaseCountAsync(Cphh);

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetCaseCountByCphhAsync_WhenRowsReturned_ReturnsTheFirstValue()
    {
        var table = new DataTable();
        table.Columns.Add("", typeof(int));
        table.Rows.Add(7);
        var (sut, connection) = CreateSut(table);

        var result = await sut.GetCaseCountByCphhAsync(Cphh);

        result.Should().Be(7);
        connection.CommandTexts.Should().Contain("GetNumberOfCasesByCPHH");
    }
}
