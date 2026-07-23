using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Repositories;
using BSE.Modules.CaseWork.Services;

namespace BSE.Modules.CaseWork.Tests;

public sealed class CaseWorkServiceTests
{
    private readonly ICaseWorkRepository _repo = Substitute.For<ICaseWorkRepository>();
    private readonly CaseWorkService _sut;

    public CaseWorkServiceTests()
        => _sut = new CaseWorkService(_repo);

    // ── GetCaseWorkAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetCaseWorkAsync_Found_DelegatesToRepository()
    {
        var expected = new CaseWorkRecord { Rbse = "010000001", IsCaseClosed = false };
        _repo.GetByRbseAsync("010000001").Returns(expected);

        var result = await _sut.GetCaseWorkAsync("010000001");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetCaseWorkAsync_NotFound_ReturnsNull()
    {
        _repo.GetByRbseAsync("000000000").Returns((CaseWorkRecord?)null);

        var result = await _sut.GetCaseWorkAsync("000000000");

        result.Should().BeNull();
    }

    // ── GetCaseWorkEntryAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetCaseWorkEntryAsync_Found_ReturnsFullEntry()
    {
        var expected = new CaseWorkEntryRecord
        {
            Rbse = "010000001",
            Survey = "PS",
            IsCaseClosed = false,
            ActiveMemoDueDate = new DateTime(2026, 1, 10)
        };
        _repo.GetEntryByRbseAsync("010000001").Returns(expected);

        var result = await _sut.GetCaseWorkEntryAsync("010000001");

        result.Should().Be(expected);
        result!.ActiveMemoDueDate.Should().Be(new DateTime(2026, 1, 10));
    }

    [Fact]
    public async Task GetCaseWorkEntryAsync_NotFound_ReturnsNull()
    {
        _repo.GetEntryByRbseAsync("000000000").Returns((CaseWorkEntryRecord?)null);

        var result = await _sut.GetCaseWorkEntryAsync("000000000");

        result.Should().BeNull();
    }

    // ── GetMinuteDetailsAsync ────────────────────────────────────────────────

    [Theory]
    [InlineData("ActiveMemo")]
    [InlineData("AnnexA")]
    [InlineData("AnnexB")]
    [InlineData("AnnexC")]
    [InlineData("AnnexD")]
    [InlineData("AMFS")]
    public async Task GetMinuteDetailsAsync_ValidType_DelegatesToRepository(string minuteType)
    {
        var expected = new MinuteDetailsRecord { Rbse = "010000001" };
        _repo.GetMinuteDetailsAsync("010000001", minuteType).Returns(expected);

        var result = await _sut.GetMinuteDetailsAsync("010000001", minuteType);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetMinuteDetailsAsync_NotFound_ReturnsNull()
    {
        _repo.GetMinuteDetailsAsync("000000000", "ActiveMemo").Returns((MinuteDetailsRecord?)null);

        var result = await _sut.GetMinuteDetailsAsync("000000000", "ActiveMemo");

        result.Should().BeNull();
    }

    // ── SetMinuteSentDateAsync ───────────────────────────────────────────────

    [Theory]
    [InlineData("ActiveMemo")]
    [InlineData("AnnexA")]
    [InlineData("AnnexB")]
    [InlineData("AnnexC")]
    [InlineData("AnnexD")]
    public async Task SetMinuteSentDateAsync_DelegatesToRepository(string minuteType)
    {
        await _sut.SetMinuteSentDateAsync("010000001", minuteType);

        await _repo.Received(1).SetMinuteSentDateAsync("010000001", minuteType);
    }

    // ── EditCaseWorkEntryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EditCaseWorkEntryAsync_DelegatesToRepository()
    {
        var command = new EditCaseWorkEntryCommand(
            Rbse: "010000001",
            Barcode: "BC001",
            AhfReference: null,
            PurchaserBse1ReceivedDate: null,
            BreederBse1ReceivedDate: null,
            Vendor1Bse1ReceivedDate: null,
            HomebredBse1ReceivedDate: null,
            SummarySheetReceivedDate: null,
            PaperworkCompleteDate: null,
            ActiveMemoDate: null,
            AnnexADate: null,
            AnnexBDate: null,
            AnnexCDate: null,
            AnnexDDate: null,
            RegionalLab: null,
            ReceivedByRegionalLabDate: null,
            InitialReceivedDate: null,
            FinalReceivedDate: null,
            FinalSentDate: null,
            LabChasedDate: null,
            BarbMinuteSentDate: null,
            Post2000SentDate: null,
            CaseWorkNotes: "Test note",
            DataCompleteDate: null,
            IsCaseClosed: false,
            UserId: 99,
            TseTestingSite: null,
            SamplingDate: null,
            AhroId: null);

        await _sut.EditCaseWorkEntryAsync(command);

        await _repo.Received(1).EditEntryAsync(command);
    }

    [Fact]
    public async Task EditCaseWorkEntryAsync_ClosedCase_PassesClosedFlagToRepository()
    {
        var command = new EditCaseWorkEntryCommand(
            Rbse: "010000002",
            Barcode: null, AhfReference: null,
            PurchaserBse1ReceivedDate: null, BreederBse1ReceivedDate: null,
            Vendor1Bse1ReceivedDate: null, HomebredBse1ReceivedDate: null,
            SummarySheetReceivedDate: null, PaperworkCompleteDate: null,
            ActiveMemoDate: null, AnnexADate: null, AnnexBDate: null,
            AnnexCDate: null, AnnexDDate: null, RegionalLab: null,
            ReceivedByRegionalLabDate: null, InitialReceivedDate: null,
            FinalReceivedDate: null, FinalSentDate: null, LabChasedDate: null,
            BarbMinuteSentDate: null, Post2000SentDate: null, CaseWorkNotes: null,
            DataCompleteDate: null, IsCaseClosed: true, UserId: 42,
            TseTestingSite: null, SamplingDate: null, AhroId: null);

        await _sut.EditCaseWorkEntryAsync(command);

        await _repo.Received(1).EditEntryAsync(Arg.Is<EditCaseWorkEntryCommand>(c =>
            c.IsCaseClosed == true && c.UserId == 42));
    }
}
