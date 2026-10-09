using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Tests.Fakes;

namespace BSE.Modules.CaseManagement.Tests;

/// <summary>Covers the three repositories in ChildRepositories.cs (0% new-code coverage):
/// <see cref="TestRepository"/>, <see cref="OtherOwnerRepository"/>, and <see cref="PedigreeRepository"/>
/// — in particular <see cref="PedigreeRepository.AddEditDamSireAsync"/>'s return-code switch, which has
/// 5 branches and was entirely untested at the repository level (only exercised end-to-end via
/// <c>CaseEditOrchestrationServiceTests.CommitAllAsync_WhenDamSireIsStaged_SurfacesThePedigreeWarning</c>,
/// which only covers the warning-string pass-through, not this method's own code paths).</summary>
public sealed class ChildRepositoriesTests
{
    private const string Rbse = "002600001";

    [Fact]
    public async Task TestRepository_GetByRbseAsync_QueriesGetTestByRbse()
    {
        var connection = new FakeDbConnection(new DataTable());
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new TestRepository(factory);

        var result = await sut.GetByRbseAsync(Rbse);

        result.Should().BeEmpty();
        connection.CommandTexts.Should().Contain("GetTestByRBSE");
    }

    [Fact]
    public async Task TestRepository_AddAsync_StandaloneOverload_ExecutesAddTest()
    {
        var connection = new FakeDbConnection();
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new TestRepository(factory);

        await sut.AddAsync(new AddTestCommand(Rbse, "E", "Negative"));

        connection.CommandTexts.Should().Contain("AddTest");
    }

    [Fact]
    public async Task TestRepository_EditAsync_TransactionalOverload_ExecutesEditTest()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var factory = Substitute.For<IDbConnectionFactory>();
        var sut = new TestRepository(factory);

        await sut.EditAsync(new EditTestCommand(1, Rbse, "E", "Positive", [1, 2, 3]), txConnection, null!);

        txConnection.CommandTexts.Should().Contain("EditTest");
    }

    [Fact]
    public async Task TestRepository_DeleteAsync_StandaloneOverload_ExecutesDeleteTest()
    {
        var connection = new FakeDbConnection();
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new TestRepository(factory);

        await sut.DeleteAsync(1, [1, 2, 3]);

        connection.CommandTexts.Should().Contain("DeleteTest");
    }

    [Fact]
    public async Task OtherOwnerRepository_GetByRbseAsync_QueriesGetOtherOwnerByRbse()
    {
        var connection = new FakeDbConnection(new DataTable());
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new OtherOwnerRepository(factory);

        var result = await sut.GetByRbseAsync(Rbse);

        result.Should().BeEmpty();
        connection.CommandTexts.Should().Contain("GetOtherOwnerByRBSE");
    }

    [Fact]
    public async Task OtherOwnerRepository_AddEditDelete_ExecuteTheirRespectiveStoredProcedures()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var factory = Substitute.For<IDbConnectionFactory>();
        var sut = new OtherOwnerRepository(factory);

        await sut.AddAsync(new AddOtherOwnerCommand(Rbse, "O", "Name", "01001000101"), txConnection, null!);
        await sut.EditAsync(new EditOtherOwnerCommand(1, "O", "Name", "01001000101", [1, 2, 3]), txConnection, null!);
        await sut.DeleteAsync(1, [1, 2, 3], txConnection, null!);

        txConnection.CommandTexts.Should().ContainInOrder("AddOtherOwner", "EditOtherOwner", "DeleteOtherOwner");
    }

    [Fact]
    public async Task PedigreeRepository_GetDamByRbseAsync_QueriesGetDamDetailsByRbse()
    {
        var connection = new FakeDbConnection(new DataTable());
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new PedigreeRepository(factory);

        await sut.GetDamByRbseAsync(Rbse);

        connection.CommandTexts.Should().Contain("GetDamDetailsByRBSE");
    }

    [Fact]
    public async Task PedigreeRepository_GetSireByRbseAsync_QueriesGetSireDetailsByRbse()
    {
        var connection = new FakeDbConnection(new DataTable());
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        var sut = new PedigreeRepository(factory);

        await sut.GetSireByRbseAsync(Rbse);

        connection.CommandTexts.Should().Contain("GetSireDetailsByRBSE");
    }

    private static AddEditDamSireCommand ValidDamSireCommand() => new(
        Rbse,
        DamId: null, DamRbse: null, DamEartag: null, DamName: "Dam", DamHerdbook: null,
        DamBirthDay: null, DamBirthMonth: null, DamBirthYear: null, DamRowStamp: null,
        SireId: null, SireRbse: null, SireEartag: null, SireName: "Sire", SireHerdbook: null,
        SireBirthDay: null, SireBirthMonth: null, SireBirthYear: null, SireRowStamp: null,
        CaseHerdbook: null, CaseRowStamp: null);

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "Failed to create or update a dam record.  The record may have been changed by another user")]
    [InlineData(2, "Failed to create or update a sire record.  The record may have been changed by another user")]
    [InlineData(3, "Failed to create a pedigree record for the case.")]
    [InlineData(4, "Failed to update the case's pedigree record with pointers to the dam and sire information.  The record may have been changed by another user.")]
    [InlineData(5, "AddEditDamSireDetails returned unexpected code 5.")]
    public async Task PedigreeRepository_AddEditDamSireAsync_MapsEveryReturnCodeToItsWarning(int code, string? expectedWarning)
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["AddEditDamSireDetails"] = code;
        txConnection.Open();
        var factory = Substitute.For<IDbConnectionFactory>();
        var sut = new PedigreeRepository(factory);

        var result = await sut.AddEditDamSireAsync(ValidDamSireCommand(), txConnection, null!);

        result.Should().Be(expectedWarning);
        txConnection.CommandTexts.Should().Contain("AddEditDamSireDetails");
    }
}
