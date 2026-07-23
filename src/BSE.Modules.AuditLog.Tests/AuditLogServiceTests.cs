using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Repositories;
using BSE.Modules.AuditLog.Services;
using FluentAssertions;
using NSubstitute;

namespace BSE.Modules.AuditLog.Tests;

public sealed class AuditLogServiceTests
{
    private readonly IAuditLogRepository _repository = Substitute.For<IAuditLogRepository>();
    private readonly IAuditLogService _sut;

    public AuditLogServiceTests() => _sut = new AuditLogService(_repository);

    // ── GetByCaseAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCaseAsync_PassesRbseToRepository_AndReturnsResult()
    {
        var entries = new[] { new AuditLogEntry { Id = 1, TableName = "Case", UserName = "alice" } };
        _repository.GetByCaseAsync("AB12345AB").Returns(entries);

        var result = await _sut.GetByCaseAsync("AB12345AB");

        result.Should().BeSameAs(entries);
        await _repository.Received(1).GetByCaseAsync("AB12345AB");
    }

    // ── GetByDateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByDateAsync_PassesLogDateToRepository_AndReturnsResult()
    {
        var logDate = new DateTime(2026, 1, 15);
        var entries = new[] { new AuditLogEntry { Id = 2, TableName = "Farm", UserName = "bob" } };
        _repository.GetByDateAsync(logDate).Returns(entries);

        var result = await _sut.GetByDateAsync(logDate);

        result.Should().BeSameAs(entries);
        await _repository.Received(1).GetByDateAsync(logDate);
    }

    // ── GetByFarmAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByFarmAsync_PassesCphhToRepository_AndReturnsResult()
    {
        var entries = new[] { new AuditLogEntry { Id = 3, TableName = "Farm", UserName = "carol" } };
        _repository.GetByFarmAsync("12/345/6789/01").Returns(entries);

        var result = await _sut.GetByFarmAsync("12/345/6789/01");

        result.Should().BeSameAs(entries);
        await _repository.Received(1).GetByFarmAsync("12/345/6789/01");
    }

    // ── GetByUserAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_PassesAllParametersToRepository_AndReturnsResult()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);
        const int userId = 42;
        var entries = new[] { new AuditLogEntry { Id = 4, TableName = "Case", UserName = "dave" } };
        _repository.GetByUserAsync(start, end, userId).Returns(entries);

        var result = await _sut.GetByUserAsync(start, end, userId);

        result.Should().BeSameAs(entries);
        await _repository.Received(1).GetByUserAsync(start, end, userId);
    }

    // ── GetCaseMovesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetCaseMovesAsync_PassesDateRangeToRepository_AndReturnsResult()
    {
        var start = new DateTime(2026, 2, 1);
        var end = new DateTime(2026, 2, 28);
        var entries = new[]
        {
            new AuditLogCaseMoveEntry { Id = 5, TableName = "Case", UserName = "eve", HasBatches = "Y" }
        };
        _repository.GetCaseMovesAsync(start, end).Returns(entries);

        var result = await _sut.GetCaseMovesAsync(start, end);

        result.Should().ContainSingle(e => e.Id == 5);
        await _repository.Received(1).GetCaseMovesAsync(start, end);
    }

    // ── GetCphhChangesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetCphhChangesAsync_PassesDateRangeToRepository_AndReturnsResult()
    {
        var start = new DateTime(2026, 3, 1);
        var end = new DateTime(2026, 3, 31);
        var entries = new[]
        {
            new AuditLogCPHHChangeEntry { Id = 6, TableName = "Farm", UserName = "frank", CaseCount = 3 }
        };
        _repository.GetCphhChangesAsync(start, end).Returns(entries);

        var result = await _sut.GetCphhChangesAsync(start, end);

        result.Should().ContainSingle(e => e.Id == 6);
        await _repository.Received(1).GetCphhChangesAsync(start, end);
    }

    // ── GetNewFarmsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetNewFarmsAsync_PassesDateRangeToRepository_AndReturnsResult()
    {
        var start = new DateTime(2026, 4, 1);
        var end = new DateTime(2026, 4, 30);
        var entries = new[]
        {
            new AuditLogNewFarmEntry
            {
                Id = 7, TableName = "Farm", UserName = "grace",
                OwnerName = "J Smith", Address = "1 High St", County = "Devon"
            }
        };
        _repository.GetNewFarmsAsync(start, end).Returns(entries);

        var result = await _sut.GetNewFarmsAsync(start, end);

        result.Should().ContainSingle(e => e.Id == 7);
        await _repository.Received(1).GetNewFarmsAsync(start, end);
    }

    // ── GetRbseChangesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetRbseChangesAsync_PassesDateRangeToRepository_AndReturnsResult()
    {
        var start = new DateTime(2026, 5, 1);
        var end = new DateTime(2026, 5, 31);
        var entries = new[]
        {
            new AuditLogRBSEChangeEntry { Id = 8, TableName = "Case", UserName = "henry", HasBatches = "N" }
        };
        _repository.GetRbseChangesAsync(start, end).Returns(entries);

        var result = await _sut.GetRbseChangesAsync(start, end);

        result.Should().ContainSingle(e => e.Id == 8);
        await _repository.Received(1).GetRbseChangesAsync(start, end);
    }

    // ── Service is read-only — no write methods ───────────────────────────────

    [Fact]
    public void IAuditLogService_HasNoWriteMethods()
    {
        var methods = typeof(IAuditLogService).GetMethods();
        var writeMethods = methods.Where(m =>
            m.Name.StartsWith("Add", StringComparison.OrdinalIgnoreCase) ||
            m.Name.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ||
            m.Name.StartsWith("Delete", StringComparison.OrdinalIgnoreCase) ||
            m.Name.StartsWith("Save", StringComparison.OrdinalIgnoreCase) ||
            m.Name.StartsWith("Create", StringComparison.OrdinalIgnoreCase));

        writeMethods.Should().BeEmpty("AuditLog is read-only from the application layer");
    }
}
