namespace BSE.Modules.AuditLog.Tests;

/// <summary>
/// Integration tests for AuditLog module.
/// These require a running SQL Server instance with the BSE schema applied.
/// All tests are skipped unless Testcontainers SQL Server is configured.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AuditLogIntegrationTests
{
    [Fact(Skip = "Requires Testcontainers SQL Server — wire up in CI integration stage")]
    public Task GetByCaseAsync_ReturnsEntriesFromDatabase()
        => Task.CompletedTask;

    [Fact(Skip = "Requires Testcontainers SQL Server — wire up in CI integration stage")]
    public Task GetByDateAsync_ReturnsEntriesFromDatabase()
        => Task.CompletedTask;

    [Fact(Skip = "Requires Testcontainers SQL Server — wire up in CI integration stage")]
    public Task GetByFarmAsync_ReturnsEntriesFromDatabase()
        => Task.CompletedTask;

    [Fact(Skip = "Requires Testcontainers SQL Server — wire up in CI integration stage")]
    public Task GetByUserAsync_ReturnsEntriesFromDatabase()
        => Task.CompletedTask;

    [Fact(Skip = "Requires Testcontainers SQL Server — wire up in CI integration stage")]
    public Task GetCaseMovesAsync_ReturnsEntriesFromDatabase()
        => Task.CompletedTask;

    [Fact(Skip = "Requires Testcontainers SQL Server — wire up in CI integration stage")]
    public Task GetCphhChangesAsync_ReturnsEntriesFromDatabase()
        => Task.CompletedTask;

    [Fact(Skip = "Requires Testcontainers SQL Server — wire up in CI integration stage")]
    public Task GetNewFarmsAsync_ReturnsEntriesFromDatabase()
        => Task.CompletedTask;

    [Fact(Skip = "Requires Testcontainers SQL Server — wire up in CI integration stage")]
    public Task GetRbseChangesAsync_ReturnsEntriesFromDatabase()
        => Task.CompletedTask;
}
