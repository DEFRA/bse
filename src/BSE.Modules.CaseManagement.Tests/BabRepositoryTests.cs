using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Tests.Fakes;

namespace BSE.Modules.CaseManagement.Tests;

/// <summary>Covers <see cref="BabRepository"/> (0% new-code coverage), including the
/// <see cref="BabRepository.EditAsync"/> return-code switch, its soft-fail exception handler, and
/// the Origin/purchase-field clearing side effect on every Add/Edit — all previously untested.</summary>
public sealed class BabRepositoryTests
{
    private const string Rbse = "002600001";

    private static (BabRepository Sut, FakeDbConnection Connection) CreateSut(params DataTable[] resultSets)
    {
        var connection = new FakeDbConnection(resultSets);
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        return (new BabRepository(factory), connection);
    }

    private static AddCaseBabCommand ValidAddCommand() => new(
        Rbse, NatalCphh: null, Notes: "notes", TracedName: null, TracedAddress1: null,
        TracedAddress2: null, TracedAddress3: null, TracedPostcode: null, FeedRisk: null,
        HorizontalRisk: null, MaternalRisk: null);

    private static EditCaseBabCommand ValidEditCommand() => new(
        Rbse, NatalCphh: null, Notes: "notes", TracedName: null, TracedAddress1: null,
        TracedAddress2: null, TracedAddress3: null, TracedPostcode: null, FeedRisk: null,
        HorizontalRisk: null, MaternalRisk: null, RowStamp: [1, 2, 3]);

    [Fact]
    public async Task GetByRbseAsync_QueriesGetBabByRbse()
    {
        var (sut, connection) = CreateSut(new DataTable());

        await sut.GetByRbseAsync(Rbse);

        connection.CommandTexts.Should().Contain("GetBABByRBSE");
    }

    [Fact]
    public async Task AddAsync_ExecutesAddCaseBabThenUpdatesOrigin()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var (sut, _) = CreateSut();

        await sut.AddAsync(ValidAddCommand(), origin: "P", txConnection, null!);

        txConnection.CommandTexts.Should().Contain("AddCaseBAB");
        txConnection.CommandTexts.Should().ContainSingle(c => c.Contains("UPDATE [Case]"));
    }

    [Fact]
    public async Task EditAsync_WhenReturnCodeIsZero_ReturnsNull()
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditCaseBAB"] = 0;
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), origin: null, txConnection, null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EditAsync_WhenReturnCodeIsOne_ReturnsModifiedByAnotherUserWarning()
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditCaseBAB"] = 1;
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), origin: null, txConnection, null!);

        result.Should().Be("Failed to update the BAB table.  The data may have been changed by another user.");
    }

    [Fact]
    public async Task EditAsync_WhenReturnCodeIsTwo_ReturnsGenericFailureWarning()
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditCaseBAB"] = 2;
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), origin: null, txConnection, null!);

        result.Should().Be("Failed to update the BAB table.");
    }

    [Fact]
    public async Task EditAsync_WhenReturnCodeIsUnexpected_ReturnsAWarningContainingTheCode()
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditCaseBAB"] = 7;
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), origin: null, txConnection, null!);

        result.Should().Be("Failed to update the BAB table (code 7).");
    }

    [Fact]
    public async Task EditAsync_WhenTheSpThrows_ReturnsTheExceptionMessageAndStillUpdatesOrigin()
    {
        var txConnection = new FakeDbConnection();
        txConnection.ThrowFor.Add("EditCaseBAB");
        txConnection.Open();
        var (sut, _) = CreateSut();

        var result = await sut.EditAsync(ValidEditCommand(), origin: "P", txConnection, null!);

        result.Should().Be("Simulated failure executing EditCaseBAB");
        txConnection.CommandTexts.Should().ContainSingle(c => c.Contains("UPDATE [Case]"));
    }

    [Theory]
    [InlineData("P", true)]
    [InlineData("H", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public async Task EditAsync_UpdateOrigin_OnlyTreatsExactlyPAsPurchased(string? origin, bool expectedIsPurchased)
    {
        var txConnection = new FakeDbConnection();
        txConnection.ReturnValues["EditCaseBAB"] = 0;
        txConnection.Open();
        var (sut, _) = CreateSut();

        await sut.EditAsync(ValidEditCommand(), origin, txConnection, null!);

        txConnection.LastCommand!.CommandText.Should().Contain("UPDATE [Case]");
        var isPurchasedParam = txConnection.LastCommand.Parameters
            .Cast<FakeDbParameter>()
            .First(p => p.ParameterName == "IsPurchased");
        isPurchasedParam.Value.Should().Be(expectedIsPurchased ? 1 : 0);
    }
}
