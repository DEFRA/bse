using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Tests.Fakes;

namespace BSE.Modules.CaseManagement.Tests;

/// <summary>Covers <see cref="FeedRepository"/> (0% new-code coverage) — including the
/// <see cref="FeedRepository.EditAsync"/>/<see cref="FeedRepository.DeleteAsync"/> row-count returns
/// that <see cref="BSE.Host.Services.CaseEditOrchestrationService"/> relies on to detect a stale
/// RowStamp, which had no direct repository-level test.</summary>
public sealed class FeedRepositoryTests
{
    private const string Rbse = "002600001";

    private static (FeedRepository Sut, FakeDbConnection Connection) CreateSut(params DataTable[] resultSets)
    {
        var connection = new FakeDbConnection(resultSets);
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.CreateConnection().Returns(connection);
        return (new FeedRepository(factory), connection);
    }

    [Fact]
    public async Task GetByRbseAsync_QueriesGetFeedByRbse()
    {
        var (sut, connection) = CreateSut(new DataTable());

        var result = await sut.GetByRbseAsync(Rbse);

        result.Should().BeEmpty();
        connection.CommandTexts.Should().Contain("GetFeedByRBSE");
    }

    [Fact]
    public async Task AddAsync_ExecutesAddCaseFeed()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var (sut, _) = CreateSut();

        await sut.AddAsync(new AddFeedCommand(Rbse, 1995, 1996, "C", null, "Ration A", false), txConnection, null!);

        txConnection.CommandTexts.Should().Contain("AddCaseFeed");
    }

    [Fact]
    public async Task EditAsync_WhenRowStampMatches_ReturnsOneRowAffected()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var (sut, _) = CreateSut();

        var rows = await sut.EditAsync(new EditFeedCommand(7, 1995, 1996, "C", null, "Ration A", false, [1, 2, 3]), txConnection, null!);

        rows.Should().Be(1);
        txConnection.CommandTexts.Should().Contain("EditCaseFeed");
    }

    [Fact]
    public async Task DeleteAsync_ExecutesDeleteCaseFeed()
    {
        var txConnection = new FakeDbConnection();
        txConnection.Open();
        var (sut, _) = CreateSut();

        var rows = await sut.DeleteAsync(7, [1, 2, 3], txConnection, null!);

        rows.Should().Be(1);
        txConnection.CommandTexts.Should().Contain("DeleteCaseFeed");
    }
}
