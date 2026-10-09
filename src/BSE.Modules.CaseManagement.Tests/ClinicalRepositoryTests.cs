using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Tests.Fakes;

namespace BSE.Modules.CaseManagement.Tests;

/// <summary>Covers <see cref="ClinicalRepository"/> (0% new-code coverage), including the
/// <see cref="ClinicalRepository.EditAsync"/> return-code switch and its soft-fail exception
/// handler — both previously untested.</summary>
public sealed class ClinicalRepositoryTests
{
    private const string Rbse = "002600001";

    private static (ClinicalRepository Sut, FakeDbConnection Connection) CreateSut(params DataTable[] resultSets)
    {
        var connection = new FakeDbConnection(resultSets);
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        return (new ClinicalRepository(factory), connection);
    }

    private static AddCaseClinicalCommand ValidAddCommand() => new(
        Rbse, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false);

    private static EditCaseClinicalCommand ValidEditCommand() => new(
        Rbse, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false, [1, 2, 3]);

    [Fact]
    public async Task GetByRbseAsync_QueriesGetClinicalByRbse()
    {
        var (sut, connection) = CreateSut(new DataTable());

        await sut.GetByRbseAsync(Rbse);

        connection.CommandTexts.Should().Contain("GetClinicalByRBSE");
    }

    [Fact]
    public async Task GetVisitsByRbseAsync_QueriesGetClinicalVisitByRbse()
    {
        var (sut, connection) = CreateSut(new DataTable());

        var result = await sut.GetVisitsByRbseAsync(Rbse);

        result.Should().BeEmpty();
        connection.CommandTexts.Should().Contain("GetClinicalVisitByRBSE");
    }

    [Fact]
    public async Task AddAsync_ExecutesAddCaseClinicalOnTheSuppliedConnection()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var (sut, _) = CreateSut();

        await sut.AddAsync(ValidAddCommand(), txConnection, null!);

        txConnection.CommandTexts.Should().Contain("AddCaseClinical");
    }

    [Fact]
    public async Task EditAsync_WhenReturnCodeIsZero_ReturnsNull()
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditCaseClinical"] = 0;
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), txConnection, null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EditAsync_WhenReturnCodeIsOne_ReturnsModifiedByAnotherUserWarning()
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditCaseClinical"] = 1;
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), txConnection, null!);

        result.Should().Be("Failed to update the Clinical table.  The data may have been changed by another user.");
    }

    [Fact]
    public async Task EditAsync_WhenReturnCodeIsTwo_ReturnsGenericFailureWarning()
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditCaseClinical"] = 2;
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), txConnection, null!);

        result.Should().Be("Failed to update the Clinical table.");
    }

    [Fact]
    public async Task EditAsync_WhenReturnCodeIsUnexpected_ReturnsAWarningContainingTheCode()
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditCaseClinical"] = 99;
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), txConnection, null!);

        result.Should().Be("Failed to update the Clinical table (code 99).");
    }

    [Fact]
    public async Task EditAsync_WhenTheSpThrows_ReturnsTheExceptionMessageInsteadOfThrowing()
    {
        // Legacy parity: clsCase.UpdateClinicalRecord treats any failure here as soft/non-fatal.
        var txConnection = new FakeDbConnection();
        txConnection.ThrowFor.Add("EditCaseClinical");
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), txConnection, null!);

        result.Should().Be("Simulated failure executing EditCaseClinical");
    }

    [Fact]
    public async Task AddVisitAsync_ExecutesAddClinicalVisit()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var (sut, _) = CreateSut();

        await sut.AddVisitAsync(new AddClinicalVisitCommand(Rbse, DateTime.Today), txConnection, null!);

        txConnection.CommandTexts.Should().Contain("AddClinicalVisit");
    }

    [Fact]
    public async Task EditVisitAsync_ExecutesEditClinicalVisit()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var (sut, _) = CreateSut();

        await sut.EditVisitAsync(new EditClinicalVisitCommand(1, DateTime.Today, [1, 2, 3]), txConnection, null!);

        txConnection.CommandTexts.Should().Contain("EditClinicalVisit");
    }

    [Fact]
    public async Task DeleteVisitAsync_ExecutesDeleteClinicalVisit()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var (sut, _) = CreateSut();

        await sut.DeleteVisitAsync(1, [1, 2, 3], txConnection, null!);

        txConnection.CommandTexts.Should().Contain("DeleteClinicalVisit");
    }
}
