using BSE.Infrastructure;
using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Email;
using BSE.Modules.AdnsExport.Exceptions;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Repositories;
using BSE.Modules.AdnsExport.Services;
using Microsoft.Extensions.Options;
using System.Data;

namespace BSE.Modules.AdnsExport.Tests;

public sealed class AdnsExportServiceTests
{
    private readonly IAdnsRepository _repo = Substitute.For<IAdnsRepository>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();
    private readonly ISmtpClient _smtp = Substitute.For<ISmtpClient>();
    private readonly AdnsExportService _sut;

    private static readonly AdnsSmtpOptions SmtpOpts = new()
    {
        Host = "smtp.test", Port = 25,
        FromAddress = "from@test.com",
        ToAddress = "brussels@adns.int"
    };

    public AdnsExportServiceTests()
    {
        _connectionFactory.CreateConnection().Returns(_connection);
        _connection.BeginTransaction().Returns(_transaction);
        _sut = new AdnsExportService(
            _repo, _connectionFactory, _smtp,
            Options.Create(SmtpOpts));
    }

    // ── PreviewGbExportAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task PreviewGbExportAsync_NoCases_ReturnsEmptyPreview()
    {
        _repo.GetCasesForGbAsync(2024, 1)
             .Returns((Array.Empty<AdnsCaseRecord>(), Array.Empty<MissingAdnsCaseRecord>()));

        var result = await _sut.PreviewGbExportAsync("DBSE2024/00001", 2024, 1);

        result.Cases.Should().BeEmpty();
        result.EmailBody.Should().BeEmpty();
        result.StartAdnsReference.Should().BeNull();
        result.EndAdnsReference.Should().BeNull();
    }

    [Fact]
    public async Task PreviewGbExportAsync_WithCases_PopulatesEmailBodyAndReferences()
    {
        var cases = new[] { MakeCase(1, "2024/00042") };
        _repo.GetCasesForGbAsync(2024, 42)
             .Returns((cases, Array.Empty<MissingAdnsCaseRecord>()));

        var result = await _sut.PreviewGbExportAsync("DBSE2024/00042", 2024, 42);

        result.Cases.Should().HaveCount(1);
        result.EmailBody.Should().Contain("<I>CVETUNK1");
        result.StartAdnsReference.Should().Be("2024/00042");
        result.EndAdnsReference.Should().Be("2024/00042");
    }

    [Fact]
    public async Task PreviewGbExportAsync_SubjectFormatIsCorrect()
    {
        _repo.GetCasesForGbAsync(Arg.Any<int>(), Arg.Any<int>())
             .Returns((Array.Empty<AdnsCaseRecord>(), Array.Empty<MissingAdnsCaseRecord>()));

        var result = await _sut.PreviewGbExportAsync("DBSE2024/00001", 2024, 1);

        result.EmailSubject.Should().Be("t=DATA;r=DBSE2024/00001");
    }

    // ── PreviewCiExport ───────────────────────────────────────────────────────

    [Fact]
    public void PreviewCiExport_JerseyOnly_CreatesCasesWithJerseyRegion()
    {
        var result = _sut.PreviewCiExport("DBSE2024/00010", 2024, 10,
            jerseyCases: 2, guernseyCases: 0, isleOfManCases: 0,
            confirmationDate: new DateTime(2024, 5, 1));

        result.Cases.Should().HaveCount(2);
        result.Cases.Should().AllSatisfy(c => c.AdnsRegionId.Should().Be(6200));
        result.Cases.Should().AllSatisfy(c => c.AdnsRegionName.Should().Be("Jersey"));
    }

    [Fact]
    public void PreviewCiExport_MixedIslands_AssignsSequentialNumbers()
    {
        var result = _sut.PreviewCiExport("DBSE2024/00010", 2024, 10,
            jerseyCases: 1, guernseyCases: 1, isleOfManCases: 1,
            confirmationDate: new DateTime(2024, 5, 1));

        result.Cases.Should().HaveCount(3);
        result.Cases[0].AdnsNumber.Should().Be(10);  // Jersey
        result.Cases[1].AdnsNumber.Should().Be(11);  // Guernsey
        result.Cases[2].AdnsNumber.Should().Be(12);  // Isle of Man
    }

    // ── PreviewNiExport ───────────────────────────────────────────────────────

    [Fact]
    public void PreviewNiExport_WithCases_BuildsCorrectSummary()
    {
        var niCases = new[]
        {
            new NiCaseInput("010000001", 2024, 20, 9001, "NI Region A", new DateTime(2024,4,1)),
            new NiCaseInput("010000002", 2024, 21, 9001, "NI Region A", new DateTime(2024,4,2)),
        };

        var result = _sut.PreviewNiExport("DBSE2024/00020", niCases);

        result.Cases.Should().HaveCount(2);
        result.Summary.Should().HaveCount(1);
        result.Summary[0].CasesCount.Should().Be(2);
    }

    // ── GetLastReferenceAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetLastReferenceAsync_DelegatesToRepository()
    {
        var expected = new LastAdnsReferenceRecord { LastAdnsReferenceYear = 2024, LastAdnsReferenceNumber = 99 };
        _repo.GetLastReferenceByAreaAsync("GB").Returns(expected);

        var result = await _sut.GetLastReferenceAsync("GB");

        result.Should().Be(expected);
    }

    // ── DispatchAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_SaveAdnsData_CallsEditCaseAdnsPerCase()
    {
        var cases = new[] { MakeCase(1, "2024/00001"), MakeCase(2, "2024/00002") };
        _repo.EditCaseAdnsAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<int>(),
            Arg.Any<short>(), Arg.Any<int>(), Arg.Any<byte[]>(),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(0);

        var cmd = new DispatchAdnsCommand("GB", "DBSE2024/00001", cases, "user@test.com", SaveAdnsData: true);
        await _sut.DispatchAsync(cmd);

        await _repo.Received(2).EditCaseAdnsAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
            Arg.Any<int>(), Arg.Any<short>(), Arg.Any<int>(), Arg.Any<byte[]>(),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
    }

    [Fact]
    public async Task DispatchAsync_SaveAdnsDataFalse_DoesNotCallEditCaseAdns()
    {
        var cases = new[] { MakeCase(1, "2024/00001") };
        var cmd = new DispatchAdnsCommand("GB", "DBSE2024/00001", cases, "user@test.com", SaveAdnsData: false);

        await _sut.DispatchAsync(cmd);

        await _repo.DidNotReceive().EditCaseAdnsAsync(Arg.Any<string>(), Arg.Any<DateTime>(),
            Arg.Any<int>(), Arg.Any<short>(), Arg.Any<int>(), Arg.Any<byte[]>(),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
    }

    [Fact]
    public async Task DispatchAsync_Success_SendsTwoEmails()
    {
        var cases = new[] { MakeCase(1, "2024/00001") };
        _repo.EditCaseAdnsAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<int>(),
            Arg.Any<short>(), Arg.Any<int>(), Arg.Any<byte[]>(),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(0);

        var cmd = new DispatchAdnsCommand("GB", "DBSE2024/00001", cases, "user@test.com", SaveAdnsData: true);
        await _sut.DispatchAsync(cmd);

        await _smtp.Received(1).SendAsync("from@test.com", "user@test.com", Arg.Any<string>(), Arg.Any<string>());
        await _smtp.Received(1).SendAsync("from@test.com", "brussels@adns.int", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DispatchAsync_Success_CommitsTransaction()
    {
        var cases = new[] { MakeCase(1, "2024/00001") };
        _repo.EditCaseAdnsAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<int>(),
            Arg.Any<short>(), Arg.Any<int>(), Arg.Any<byte[]>(),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(0);

        var cmd = new DispatchAdnsCommand("GB", "DBSE2024/00001", cases, "user@test.com", SaveAdnsData: true);
        await _sut.DispatchAsync(cmd);

        _transaction.Received(1).Commit();
        _transaction.DidNotReceive().Rollback();
    }

    [Fact]
    public async Task DispatchAsync_EditCaseAdnsReturnsConcurrency_ThrowsAndRollsBack()
    {
        var cases = new[] { MakeCase(1, "2024/00001") };
        _repo.EditCaseAdnsAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<int>(),
            Arg.Any<short>(), Arg.Any<int>(), Arg.Any<byte[]>(),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(1); // ConcurrencyConflict

        var cmd = new DispatchAdnsCommand("GB", "DBSE2024/00001", cases, "user@test.com", SaveAdnsData: true);
        var act = async () => await _sut.DispatchAsync(cmd);

        await act.Should().ThrowAsync<AdnsCaseUpdateException>()
            .Where(ex => ex.ReturnCode == 1);

        _transaction.Received(1).Rollback();
        _transaction.DidNotReceive().Commit();
    }

    [Fact]
    public async Task DispatchAsync_SmtpThrows_RollsBackTransaction()
    {
        var cases = new[] { MakeCase(1, "2024/00001") };
        _repo.EditCaseAdnsAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<int>(),
            Arg.Any<short>(), Arg.Any<int>(), Arg.Any<byte[]>(),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(0);
        _smtp.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
             .Returns<Task>(_ => throw new InvalidOperationException("SMTP failure"));

        var cmd = new DispatchAdnsCommand("GB", "DBSE2024/00001", cases, "user@test.com", SaveAdnsData: true);
        var act = async () => await _sut.DispatchAsync(cmd);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _transaction.Received(1).Rollback();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AdnsCaseRecord MakeCase(int id, string adnsReference) => new()
    {
        Id = id,
        Rbse = $"01000000{id}",
        AdnsYear = 2024,
        AdnsNumber = id,
        AdnsRegionId = 1001,
        AdnsRegionName = "SW",
        ConfirmationDate = new DateTime(2024, 1, id),
        AdnsReference = adnsReference,
        RowStamp = new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)id }
    };
}
