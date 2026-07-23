using System.Data;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;

namespace BSE.Modules.CaseManagement.Tests;

public sealed class CaseServiceTests
{
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();

    private readonly ICaseRepository _caseRepo = Substitute.For<ICaseRepository>();
    private readonly IClinicalRepository _clinicalRepo = Substitute.For<IClinicalRepository>();
    private readonly IBabRepository _babRepo = Substitute.For<IBabRepository>();
    private readonly IFeedRepository _feedRepo = Substitute.For<IFeedRepository>();
    private readonly ITestRepository _testRepo = Substitute.For<ITestRepository>();
    private readonly IOtherOwnerRepository _otherOwnerRepo = Substitute.For<IOtherOwnerRepository>();
    private readonly IPedigreeRepository _pedigreeRepo = Substitute.For<IPedigreeRepository>();
    private readonly CaseService _sut;

    public CaseServiceTests()
    {
        _connectionFactory.CreateConnection().Returns(_connection);
        _connection.BeginTransaction().Returns(_transaction);
        _sut = new CaseService(
            _connectionFactory, _caseRepo, _clinicalRepo, _babRepo,
            _feedRepo, _testRepo, _otherOwnerRepo, _pedigreeRepo);
    }

    private static AddCaseCommand MakeCase(string rbse = "010000001") => new(
        rbse, "12/345/6789/01", null, null, null, null, null, null, null, null,
        null, null, false, false, false, false, false, false, null, null, null,
        null, false, null, null, null, null, null, null, null, null, null, null,
        null, false, null, null, null, null, null, null, null);

    // ── GetCaseDetailsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCaseDetailsAsync_DelegatesToRepository()
    {
        var expected = new CaseDetailRecord(
            null, null, null,
            Array.Empty<OtherOwnerRecord>(),
            Array.Empty<CaseFeedRecord>(),
            Array.Empty<ClinicalVisitRecord>(),
            null, null,
            Array.Empty<CaseRelationRecord>(),
            Array.Empty<CaseTestRecord>(),
            null);
        _caseRepo.GetCaseDetailsByRbseAsync("010000001").Returns(expected);

        var result = await _sut.GetCaseDetailsAsync("010000001");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetCaseDetailsAsync_NotFound_ReturnsNull()
    {
        _caseRepo.GetCaseDetailsByRbseAsync("NOTEXIST").Returns((CaseDetailRecord?)null);

        var result = await _sut.GetCaseDetailsAsync("NOTEXIST");

        result.Should().BeNull();
    }

    // ── CreateCaseAsync — success path ───────────────────────────────────────

    [Fact]
    public async Task CreateCaseAsync_Success_CommitsTransaction()
    {
        var cmd = new UpdateCaseDetailsCommand(MakeCase(), null, null, null,
            Array.Empty<AddFeedCommand>(), Array.Empty<AddTestCommand>(),
            Array.Empty<AddOtherOwnerCommand>(), null,
            Array.Empty<AddClinicalVisitCommand>());
        _caseRepo.AddCaseAsync(cmd.Case, 1, _connection, _transaction)
                 .Returns(AddCaseResult.Success);

        var result = await _sut.CreateCaseAsync(cmd, 1);

        result.Should().Be(AddCaseResult.Success);
        _transaction.Received(1).Commit();
        _transaction.DidNotReceive().Rollback();
    }

    [Fact]
    public async Task CreateCaseAsync_DuplicateRbse_ReturnsWithoutChildCalls()
    {
        var cmd = new UpdateCaseDetailsCommand(MakeCase(), null, null, null,
            Array.Empty<AddFeedCommand>(), Array.Empty<AddTestCommand>(),
            Array.Empty<AddOtherOwnerCommand>(), null,
            Array.Empty<AddClinicalVisitCommand>());
        _caseRepo.AddCaseAsync(cmd.Case, 1, _connection, _transaction)
                 .Returns(AddCaseResult.DuplicateRbse);

        var result = await _sut.CreateCaseAsync(cmd, 1);

        result.Should().Be(AddCaseResult.DuplicateRbse);
        await _clinicalRepo.DidNotReceive().AddAsync(Arg.Any<AddCaseClinicalCommand>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
    }

    /// <summary>
    /// ACCEPTANCE CRITERION: Simulates a SQL failure after AddCase succeeds (before child SP calls).
    /// The exception must cause the transaction to be rolled back — no partial-state possible.
    /// </summary>
    [Fact]
    public async Task CreateCaseAsync_ChildThrows_RollsBackTransaction()
    {
        var clinical = new AddCaseClinicalCommand(
            "010000001", false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false);

        var cmd = new UpdateCaseDetailsCommand(MakeCase(), null, clinical, null,
            Array.Empty<AddFeedCommand>(), Array.Empty<AddTestCommand>(),
            Array.Empty<AddOtherOwnerCommand>(), null,
            Array.Empty<AddClinicalVisitCommand>());

        _caseRepo.AddCaseAsync(cmd.Case, 1, _connection, _transaction)
                 .Returns(AddCaseResult.Success);
        _clinicalRepo.AddAsync(clinical, _connection, _transaction)
                     .Returns(Task.FromException(new InvalidOperationException("simulated SQL failure")));

        var act = async () => await _sut.CreateCaseAsync(cmd, 1);

        await act.Should().ThrowAsync<InvalidOperationException>("the SQL failure propagates to the caller");
        _transaction.Received(1).Rollback();
        _transaction.DidNotReceive().Commit();
    }

    // ── CreateCaseAsync — child records ──────────────────────────────────────

    [Fact]
    public async Task CreateCaseAsync_WithFeeds_CallsFeedRepositoryForEach()
    {
        var feed1 = new AddFeedCommand("010000001", 2015, 2016, "M", null, null, false);
        var feed2 = new AddFeedCommand("010000001", 2016, 2017, "M", null, null, false);
        var cmd = new UpdateCaseDetailsCommand(MakeCase(), null, null, null,
            new[] { feed1, feed2 }, Array.Empty<AddTestCommand>(),
            Array.Empty<AddOtherOwnerCommand>(), null,
            Array.Empty<AddClinicalVisitCommand>());
        _caseRepo.AddCaseAsync(cmd.Case, 1, _connection, _transaction)
                 .Returns(AddCaseResult.Success);

        await _sut.CreateCaseAsync(cmd, 1);

        await _feedRepo.Received(1).AddAsync(feed1, _connection, _transaction);
        await _feedRepo.Received(1).AddAsync(feed2, _connection, _transaction);
    }

    // ── DeleteCaseAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCaseAsync_DelegatesToRepository()
    {
        _caseRepo.DeleteCaseAsync("010000001", 1).Returns(DeleteCaseResult.Success);

        var result = await _sut.DeleteCaseAsync("010000001", 1);

        result.Should().Be(DeleteCaseResult.Success);
    }

    [Fact]
    public async Task DeleteCaseAsync_HasLinkedRecords_ReturnsHasLinkedRecords()
    {
        _caseRepo.DeleteCaseAsync("010000001", 1).Returns(DeleteCaseResult.HasLinkedRecords);

        var result = await _sut.DeleteCaseAsync("010000001", 1);

        result.Should().Be(DeleteCaseResult.HasLinkedRecords);
    }

    // ── MoveCaseAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveCaseAsync_DelegatesToRepository()
    {
        _caseRepo.MoveCaseAsync("010000001", "12/345/9999/01", 1).Returns(MoveCaseResult.Success);

        var result = await _sut.MoveCaseAsync("010000001", "12/345/9999/01", 1);

        result.Should().Be(MoveCaseResult.Success);
    }

    [Fact]
    public async Task MoveCaseAsync_NewFarmNotFound_ReturnsNewFarmNotFound()
    {
        _caseRepo.MoveCaseAsync("010000001", "00/000/0000/00", 1).Returns(MoveCaseResult.NewFarmNotFound);

        var result = await _sut.MoveCaseAsync("010000001", "00/000/0000/00", 1);

        result.Should().Be(MoveCaseResult.NewFarmNotFound);
    }

    // ── ChangeRbseAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeRbseAsync_DelegatesToRepository()
    {
        _caseRepo.ChangeRbseAsync("010000001", "010000002", 1).Returns(ChangeRbseResult.Success);

        var result = await _sut.ChangeRbseAsync("010000001", "010000002", 1);

        result.Should().Be(ChangeRbseResult.Success);
    }

    [Fact]
    public async Task ChangeRbseAsync_NewRbseAlreadyExists_ReturnsConflict()
    {
        _caseRepo.ChangeRbseAsync("010000001", "010000003", 1)
                 .Returns(ChangeRbseResult.NewRbseAlreadyExists);

        var result = await _sut.ChangeRbseAsync("010000001", "010000003", 1);

        result.Should().Be(ChangeRbseResult.NewRbseAlreadyExists);
    }

    // ── EditCaseAsync ────────────────────────────────────────────────────────

    private static EditCaseCommand MakeEditCase(string rbse = "010000001") => new(
        rbse, null, null, null, null, null, null, null, null,
        null, null, false, false, false, false, false, false, null, null, null,
        null, false, null, null, null, null, null, null, null, null, null, null,
        null, false, null, null, null, null, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, null, null, null);

    [Fact]
    public async Task EditCaseAsync_Success_CommitsTransaction()
    {
        var editCmd = MakeEditCase();
        var cmd = new EditCaseDetailsCommand(editCmd, Clinical: null, Bab: null, DamSire: null);
        _caseRepo.EditCaseAsync(editCmd, 1, _connection, _transaction)
                 .Returns(EditCaseResult.Success);

        var result = await _sut.EditCaseAsync(cmd, 1);

        result.Should().Be(EditCaseResult.Success);
        _transaction.Received(1).Commit();
        _transaction.DidNotReceive().Rollback();
    }

    [Fact]
    public async Task EditCaseAsync_ConcurrencyConflict_ReturnsWithoutChildCalls()
    {
        var editCmd = MakeEditCase();
        var cmd = new EditCaseDetailsCommand(editCmd, Clinical: null, Bab: null, DamSire: null);
        _caseRepo.EditCaseAsync(editCmd, 1, _connection, _transaction)
                 .Returns(EditCaseResult.ConcurrencyConflict);

        var result = await _sut.EditCaseAsync(cmd, 1);

        result.Should().Be(EditCaseResult.ConcurrencyConflict);
        await _clinicalRepo.DidNotReceive().EditAsync(Arg.Any<EditCaseClinicalCommand>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
    }

    [Fact]
    public async Task EditCaseAsync_WithClinical_CallsClinicalRepositoryEdit()
    {
        var editCmd = MakeEditCase();
        var clinical = new EditCaseClinicalCommand(
            "010000001", false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var cmd = new EditCaseDetailsCommand(editCmd, clinical, Bab: null, DamSire: null);
        _caseRepo.EditCaseAsync(editCmd, 1, _connection, _transaction)
                 .Returns(EditCaseResult.Success);

        await _sut.EditCaseAsync(cmd, 1);

        await _clinicalRepo.Received(1).EditAsync(clinical, _connection, _transaction);
    }

    [Fact]
    public async Task EditCaseAsync_ChildThrows_RollsBackTransaction()
    {
        var editCmd = MakeEditCase();
        var clinical = new EditCaseClinicalCommand(
            "010000001", false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var cmd = new EditCaseDetailsCommand(editCmd, clinical, Bab: null, DamSire: null);
        _caseRepo.EditCaseAsync(editCmd, 1, _connection, _transaction)
                 .Returns(EditCaseResult.Success);
        _clinicalRepo.EditAsync(clinical, _connection, _transaction)
                     .Returns(Task.FromException(new InvalidOperationException("simulated failure")));

        var act = async () => await _sut.EditCaseAsync(cmd, 1);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _transaction.Received(1).Rollback();
        _transaction.DidNotReceive().Commit();
    }
}
