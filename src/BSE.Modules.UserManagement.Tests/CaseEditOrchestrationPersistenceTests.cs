using System.Data;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Repositories;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

#pragma warning disable xUnit1051 // NSubstitute's Arg.Any<CancellationToken>() is intentional here for default-token matching.

/// <summary>Covers the persistence paths of <see cref="CaseEditOrchestrationService.CommitAllAsync"/>:
/// the early exit, the existing-case save, each staged grid, batch assignment and rollback.</summary>
public sealed class CaseEditOrchestrationPersistenceTests
{
    private const string Rbse = "002600001";
    private const string Cphh = "01001000101";

    private readonly ICaseEditSessionStateService _sessionState = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseWizardStateService _wizardState = Substitute.For<ICaseWizardStateService>();
    private readonly ICaseRepository _caseRepository = Substitute.For<ICaseRepository>();
    private readonly IFarmRepository _farmRepository = Substitute.For<IFarmRepository>();
    private readonly IBabRepository _babRepository = Substitute.For<IBabRepository>();
    private readonly IClinicalRepository _clinicalRepository = Substitute.For<IClinicalRepository>();
    private readonly IFeedRepository _feedRepository = Substitute.For<IFeedRepository>();
    private readonly IAnimalRelationsRepository _relationsRepository = Substitute.For<IAnimalRelationsRepository>();
    private readonly IPedigreeRepository _pedigreeRepository = Substitute.For<IPedigreeRepository>();
    private readonly ICaseWorkRepository _caseWorkRepository = Substitute.For<ICaseWorkRepository>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly ITestRepository _testRepository = Substitute.For<ITestRepository>();
    private readonly IOtherOwnerRepository _otherOwnerRepository = Substitute.For<IOtherOwnerRepository>();
    private readonly IHerdSizeRepository _herdSizeRepository = Substitute.For<IHerdSizeRepository>();
    private readonly IFarmRelationRepository _farmRelationRepository = Substitute.For<IFarmRelationRepository>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();

    public CaseEditOrchestrationPersistenceTests()
    {
        _connection.BeginTransaction().Returns(_transaction);
        _connectionFactory.CreateConnection().Returns(_connection);
        _wizardState.GetAsync(Arg.Any<CancellationToken>()).Returns((CaseWizardState?)null);
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(ExistingCase());
        _farmRepository.GetByCphhAsync(Cphh).Returns(ExistingFarm());
    }

    private CaseEditOrchestrationService CreateService() => new(
        _sessionState, _wizardState, _caseRepository, _farmRepository, _babRepository, _clinicalRepository,
        _feedRepository, _relationsRepository, _pedigreeRepository, _caseWorkRepository, _batchRepository,
        _testRepository, _otherOwnerRepository, _herdSizeRepository, _farmRelationRepository, _connectionFactory,
        NullLogger<CaseEditOrchestrationService>.Instance);

    private static CaseRecord ExistingCase() => new()
    {
        Rbse = Rbse, Cphh = Cphh, EartagCountry = "UK", EartagHerdmark = "12345", Eartag = "1",
        FormADate = DateTime.Today.AddDays(-10), RowStamp = [1, 2, 3]
    };

    private static FarmRecord ExistingFarm() => new()
    {
        CPHH = Cphh, IsNonGBFarm = false, OwnerName = "Owner", Address1 = "1 Lane", Parish = "P", County = "C",
        AHO = "A", ADNSRegionID = 5, RowStamp = [1]
    };

    private static EditCaseCommand CaseCommand(byte[]? rowStamp = null) => new(
        Rbse: Rbse, EartagCountry: "UK", EartagHerdmark: "12345", Eartag: "1", PreviousEartag: null,
        Bse1ReceivedDate: null, FormADate: DateTime.Today.AddDays(-10), FormAResubmittedDate: null, FormBDate: null,
        Fate: null, FormCDate: null, IsPurchaserBse1Received: false, IsBreederBse1Received: false,
        IsVendor1Bse1Received: false, IsHomebredBse1Received: false, IsSummarySheetReceived: false,
        IsPaperworkComplete: false, ReportedLocation: null, Survey: null, Notes: null, BirthDate: null,
        IsBirthDateEst: null, DamStatus: null, BirthDateSource: null, ValuationAge: null, Sex: null,
        Breed: null, Origin: null, PurchaseDate: null, PurchaseAgeInMonths: null, PurchasedCounty: null,
        HerdEntryDate: null, OnsetDate: null, IsOnsetDateEst: null, MonthsPregnant: null,
        MonthsPostCalving: null, OnsetAgeInMonths: null, SlaughterDate: null, RowStamp: rowStamp ?? [1, 2, 3],
        AlternateDiagnosis: null, LabComment: null, CaseType: null);

    private static UpdateFarmCommand FarmCommand() => new(
        CPHH: Cphh, OwnerName: "Owner", Address1: "1 Lane", Address2: null, Address3: null, Postcode: null,
        Parish: "P", District: null, County: "C", CorrespondenceAddress1: null, CorrespondenceAddress2: null,
        CorrespondenceAddress3: null, CorrespondencePostcode: null, MapReference: null, Herdmark1: null,
        Herdmark2: null, Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "A",
        HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: 5, RowStamp: [1]);

    private static EditCaseBabCommand BabCommand() =>
        new(Rbse, null, "note", null, null, null, null, null, null, null, null, [9]);

    private static EditCaseClinicalCommand ClinicalCommand() => new(
        Rbse, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
        false, [9]);

    private void Stage(CaseEditSessionState state)
    {
        state.Rbse = Rbse;
        _sessionState.GetAsync(Rbse, Arg.Any<CancellationToken>()).Returns(state);
    }

    [Fact]
    public async Task NothingStaged_ReturnsSuccessWithoutOpeningAConnection()
    {
        _sessionState.GetAsync(Rbse, Arg.Any<CancellationToken>()).Returns((CaseEditSessionState?)null);

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        _connectionFactory.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task StagedCaseEdit_IsSavedCommittedAndTheDraftCleared()
    {
        Stage(new CaseEditSessionState { HasPendingChanges = true, Case = CaseCommand() });
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, _connection, _transaction).Returns(EditCaseResult.Success);

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().BeEmpty();
        _transaction.Received(1).Commit();
        await _sessionState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task ConcurrencyConflictOnTheCaseRow_IsASoftWarning()
    {
        Stage(new CaseEditSessionState { HasPendingChanges = true, Case = CaseCommand() });
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, _connection, _transaction).Returns(EditCaseResult.ConcurrencyConflict);

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().ContainSingle(w => w.Contains("modified by another user"));
        _transaction.Received(1).Commit();
    }

    [Fact]
    public async Task HardFailureOnTheCaseRow_RollsBackAndReturnsTheResult()
    {
        Stage(new CaseEditSessionState { HasPendingChanges = true, Case = CaseCommand() });
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, _connection, _transaction).Returns(EditCaseResult.RbseNotFound);

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.RbseNotFound);
        _transaction.Received(1).Rollback();
        _transaction.DidNotReceive().Commit();
        await _sessionState.DidNotReceive().ClearAsync(Rbse);
    }

    [Fact]
    public async Task StagedFarmBabClinicalCaseWorkAndDamSire_AreSavedAndWarningsCollected()
    {
        Stage(new CaseEditSessionState
        {
            HasPendingChanges = true,
            Case = CaseCommand(),
            Farm = FarmCommand(),
            Bab = BabCommand(),
            Clinical = ClinicalCommand(),
            CaseWork = new EditCaseWorkCommand(Rbse, null, "BC", null, null, null, null, null, null, null),
            DamSire = new AddEditDamSireCommand(Rbse, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null, null)
        });
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, _connection, _transaction).Returns(EditCaseResult.Success);
        _farmRepository.UpdateAsync(Arg.Any<UpdateFarmCommand>(), 1, _connection, _transaction).Returns("farm warning");
        _babRepository.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse });
        _babRepository.EditAsync(Arg.Any<EditCaseBabCommand>(), Arg.Any<string?>(), _connection, _transaction).Returns("bab warning");
        _clinicalRepository.GetByRbseAsync(Rbse).Returns(new CaseClinicalRecord { Rbse = Rbse });
        _clinicalRepository.EditAsync(Arg.Any<EditCaseClinicalCommand>(), _connection, _transaction).Returns("clinical warning");
        _pedigreeRepository.AddEditDamSireAsync(Arg.Any<AddEditDamSireCommand>(), _connection, _transaction).Returns("damsire warning");

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        outcome.Warnings.Should().BeEquivalentTo("farm warning", "bab warning", "clinical warning", "damsire warning");
        await _caseWorkRepository.Received(1).EditAsync(Arg.Any<EditCaseWorkCommand>(), _connection, _transaction);
    }

    [Fact]
    public async Task BabAndClinicalWithNoExistingRows_AreAddedInsteadOfEdited()
    {
        Stage(new CaseEditSessionState { HasPendingChanges = true, Bab = BabCommand(), Clinical = ClinicalCommand() });
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, _connection, _transaction).Returns(EditCaseResult.Success);
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);
        _clinicalRepository.GetByRbseAsync(Rbse).Returns((CaseClinicalRecord?)null);

        await CreateService().CommitAllAsync(Rbse, 1);

        await _babRepository.Received(1).AddAsync(Arg.Any<AddCaseBabCommand>(), Arg.Any<string?>(), _connection, _transaction);
        await _clinicalRepository.Received(1).AddAsync(Arg.Any<AddCaseClinicalCommand>(), _connection, _transaction);
        await _babRepository.DidNotReceive().EditAsync(Arg.Any<EditCaseBabCommand>(), Arg.Any<string?>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
    }

    [Fact]
    public async Task NewCase_AlsoCreatesCaseWorkBabAndClinicalRows()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _farmRepository.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        _caseRepository.AddCaseAsync(Arg.Any<AddCaseCommand>(), 1, _connection, _transaction).Returns(AddCaseResult.Success);
        Stage(new CaseEditSessionState
        {
            HasPendingChanges = true,
            Farm = FarmCommand(),
            Case = CaseCommand(),
            Bab = BabCommand(),
            Clinical = ClinicalCommand(),
            CaseWork = new EditCaseWorkCommand(Rbse, new DateTime(2026, 1, 1), "BC", "AHF", null, null, null, null, null, null)
        });

        await CreateService().CommitAllAsync(Rbse, 1);

        await _caseWorkRepository.Received(1).AddAsync(Arg.Is<AddCaseWorkCommand>(c => c.Barcode == "BC" && c.AhfReference == "AHF"), _connection, _transaction);
        await _babRepository.Received(1).AddAsync(Arg.Any<AddCaseBabCommand>(), Arg.Any<string?>(), _connection, _transaction);
        await _clinicalRepository.Received(1).AddAsync(Arg.Any<AddCaseClinicalCommand>(), _connection, _transaction);
    }

    [Fact]
    public async Task NewCase_WhenTheCaseInsertFails_RollsBackAndRethrows()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _farmRepository.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        _caseRepository.AddCaseAsync(Arg.Any<AddCaseCommand>(), 1, _connection, _transaction).Returns(AddCaseResult.DuplicateRbse);
        Stage(new CaseEditSessionState { HasPendingChanges = true, Farm = FarmCommand(), Case = CaseCommand() });

        var act = () => CreateService().CommitAllAsync(Rbse, 1);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _transaction.Received(1).Rollback();
    }

    [Fact]
    public async Task NewCaseWithoutAStagedFarm_IsRejectedByTheMandatoryCheck()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        Stage(new CaseEditSessionState { HasFeedsChanges = true });

        var act = () => CreateService().CommitAllAsync(Rbse, 1);

        await act.Should().ThrowAsync<MandatoryCaseFieldsMissingException>();
    }

    [Fact]
    public async Task Feeds_AddEditDeleteAndUnchangedRowsAreHandledIndividually()
    {
        _feedRepository.GetByRbseAsync(Rbse).Returns(new[]
        {
            new CaseFeedRecord { Id = 1, YearFrom = 2000, YearTo = 2001, RationType = "A", RationName = "one", RowStamp = [1] },
            new CaseFeedRecord { Id = 2, YearFrom = 2002, YearTo = 2003, RationType = "B", RationName = "two", RowStamp = [2] },
            new CaseFeedRecord { Id = 3, YearFrom = 2004, YearTo = 2005, RationType = "C", RationName = "three", RowStamp = [3] },
            new CaseFeedRecord { Id = 4, YearFrom = 2006, YearTo = 2007, RationType = "D", RationName = "four", RowStamp = null },
        });
        _feedRepository.EditAsync(Arg.Any<EditFeedCommand>(), _connection, _transaction).Returns(0);
        _feedRepository.DeleteAsync(2, Arg.Any<byte[]>(), _connection, _transaction).Returns(0);
        Stage(new CaseEditSessionState
        {
            HasFeedsChanges = true,
            Feeds =
            [
                new() { Id = 1, YearFrom = 2000, YearTo = 2010, RationType = "A", RationName = "one", RowStampBase64 = "AQ==" },
                new() { Id = 3, YearFrom = 2004, YearTo = 2005, RationType = "C", RationName = "three" },
                new() { Id = null, YearFrom = 2020, YearTo = 2021, RationType = "E", RationName = "new" },
                new() { Id = 99, YearFrom = 1, YearTo = 2, RationType = "Z" },
            ]
        });

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        await _feedRepository.Received(1).AddAsync(Arg.Is<AddFeedCommand>(c => c.RationType == "E"), _connection, _transaction);
        await _feedRepository.Received(1).EditAsync(Arg.Is<EditFeedCommand>(c => c.Id == 1 && c.YearTo == 2010), _connection, _transaction);
        await _feedRepository.Received(1).DeleteAsync(2, Arg.Any<byte[]>(), _connection, _transaction);
        await _feedRepository.DidNotReceive().DeleteAsync(4, Arg.Any<byte[]>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        outcome.Warnings.Should().Contain(w => w.StartsWith("Failed to delete feed")).And.Contain(w => w.StartsWith("Failed to update feed"));
    }

    [Fact]
    public async Task Relations_AddEditDeleteAndUnchangedRowsAreHandledIndividually()
    {
        _relationsRepository.GetRelationsByRbseAsync(Rbse).Returns((IReadOnlyList<BSE.Modules.AnimalRelations.Models.CaseRelationRecord>)new[]
        {
            new BSE.Modules.AnimalRelations.Models.CaseRelationRecord { Id = 1, RelationType = "D", RelationRbse = "A", Sex = "F", RowStamp = [1] },
            new BSE.Modules.AnimalRelations.Models.CaseRelationRecord { Id = 2, RelationType = "S", RelationRbse = "B", RowStamp = [2] },
            new BSE.Modules.AnimalRelations.Models.CaseRelationRecord { Id = 3, RelationType = "S", RelationRbse = "C", RowStamp = [3] },
        });
        Stage(new CaseEditSessionState
        {
            HasRelationsChanges = true,
            Relations =
            [
                new() { Id = 1, RelationType = "D", RelationRbse = "CHANGED", Sex = "F", RowStampBase64 = "AQ==" },
                new() { Id = 3, RelationType = "S", RelationRbse = "C" },
                new() { Id = null, RelationType = "O", RelationRbse = "N", BirthDay = 5, BirthMonth = 6, BirthYear = 2000 },
            ]
        });

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        await _relationsRepository.Received(1).AddRelationAsync(Arg.Is<AddCaseRelationCommand>(c => c.RelationType == "O"), _connection, _transaction);
        await _relationsRepository.Received(1).EditRelationAsync(Arg.Is<EditCaseRelationCommand>(c => c.Id == 1), _connection, _transaction);
        await _relationsRepository.Received(1).DeleteRelationAsync(Arg.Is<DeleteCaseRelationCommand>(c => c.Id == 2), _connection, _transaction);
        outcome.Warnings.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestsAndOtherOwners_AreAddedEditedAndDeleted()
    {
        _testRepository.GetByRbseAsync(Rbse).Returns(new[]
        {
            new CaseTestRecord(1, Rbse, "T1", "Type1", "P", "Pos", [1]),
            new CaseTestRecord(2, Rbse, "T2", "Type2", "N", "Neg", [2]),
        });
        _otherOwnerRepository.GetByRbseAsync(Rbse).Returns(new[]
        {
            new OtherOwnerRecord(1, Rbse, "B", "Old", "01001000701", [1]),
            new OtherOwnerRecord(2, Rbse, "P", "Gone", null, [2]),
            new OtherOwnerRecord(3, Rbse, "V", "Same", null, [3]),
        });
        _testRepository.EditAsync(Arg.Any<EditTestCommand>(), _connection, _transaction).Returns(1);
        _otherOwnerRepository.EditAsync(Arg.Any<EditOtherOwnerCommand>(), _connection, _transaction).Returns(1);
        Stage(new CaseEditSessionState
        {
            HasCaseEditChanges = true,
            Tests =
            [
                new() { Id = 1, TestType = "T1", TestResult = "N", RowStampBase64 = "AQ==" },
                new() { Id = null, TestType = "T3", TestResult = "P" },
            ],
            OtherOwners =
            [
                new() { Id = 1, Type = "B", Name = "New", Cphh = "01001000701", RowStampBase64 = "AQ==" },
                new() { Id = 3, Type = "V", Name = "Same", Cphh = "" },
                new() { Id = null, Type = "P", Name = " ", Cphh = "" },
            ]
        });

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        await _testRepository.Received(1).AddAsync(Arg.Is<AddTestCommand>(c => c.TestType == "T3"), _connection, _transaction);
        await _testRepository.Received(1).EditAsync(Arg.Is<EditTestCommand>(c => c.Id == 1), _connection, _transaction);
        await _testRepository.Received(1).DeleteAsync(2, Arg.Any<byte[]>(), _connection, _transaction);
        await _otherOwnerRepository.Received(1).EditAsync(Arg.Is<EditOtherOwnerCommand>(c => c.Id == 1 && c.Name == "New"), _connection, _transaction);
        await _otherOwnerRepository.Received(1).AddAsync(Arg.Is<AddOtherOwnerCommand>(c => c.Name == null), _connection, _transaction);
        await _otherOwnerRepository.Received(1).DeleteAsync(2, Arg.Any<byte[]>(), _connection, _transaction);
        outcome.Warnings.Should().HaveCount(2);
    }

    [Fact]
    public async Task ClinicalVisits_AreAddedEditedAndDeleted()
    {
        _clinicalRepository.GetVisitsByRbseAsync(Rbse).Returns(new[]
        {
            new ClinicalVisitRecord(1, Rbse, new DateTime(2024, 1, 1), [1]),
            new ClinicalVisitRecord(2, Rbse, new DateTime(2024, 2, 1), [2]),
            new ClinicalVisitRecord(3, Rbse, new DateTime(2024, 3, 1), [3]),
        });
        Stage(new CaseEditSessionState
        {
            HasClinicalVisitsChanges = true,
            Visits =
            [
                new() { Id = 1, VisitDate = new DateTime(2024, 1, 9), RowStampBase64 = "AQ==" },
                new() { Id = 3, VisitDate = new DateTime(2024, 3, 1) },
                new() { Id = null, VisitDate = new DateTime(2025, 5, 5) },
            ]
        });

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        await _clinicalRepository.Received(1).AddVisitAsync(Arg.Any<AddClinicalVisitCommand>(), _connection, _transaction);
        await _clinicalRepository.Received(1).EditVisitAsync(Arg.Is<EditClinicalVisitCommand>(c => c.Id == 1), _connection, _transaction);
        await _clinicalRepository.Received(1).DeleteVisitAsync(2, Arg.Any<byte[]>(), _connection, _transaction);
        outcome.Warnings.Should().HaveCount(2);
    }

    [Fact]
    public async Task LinkedFarmsAndHerdSizes_AreAddedEditedAndDeleted()
    {
        _farmRelationRepository.GetRelatedFarmAsync(Cphh).Returns(new[]
        {
            new FarmRelationRecord { ID = 1, CPHH = Cphh, RelatedCPHH = "01001000701", RowStamp = [1] },
            new FarmRelationRecord { ID = 2, CPHH = Cphh, RelatedCPHH = "01001009201", RowStamp = [2] },
        });
        _herdSizeRepository.GetByCphhAsync(Cphh).Returns(new[]
        {
            new HerdSizeRecord { ID = 1, CPHH = Cphh, HerdYear = 2020, TotalSize = 10, RowStamp = [1] },
            new HerdSizeRecord { ID = 2, CPHH = Cphh, HerdYear = 2021, TotalSize = 20, RowStamp = [2] },
        });
        Stage(new CaseEditSessionState
        {
            HasFarmCollectionsChanges = true,
            LinkedFarms =
            [
                new() { Id = 1, RelatedCphh = "01001000999", RowStampBase64 = "AQ==" },
                new() { Id = null, RelatedCphh = "01001000555" },
            ],
            HerdSizes =
            [
                new() { Id = 1, HerdYear = 2020, TotalSize = 99, RowStampBase64 = "AQ==" },
                new() { Id = null, HerdYear = 2022, TotalSize = 5 },
            ]
        });

        var outcome = await CreateService().CommitAllAsync(Rbse, 1);

        await _farmRelationRepository.Received(1).AddAsync(Cphh, "01001000555", _connection, _transaction);
        await _farmRelationRepository.Received(1).UpdateAsync(1, "01001000999", Arg.Any<byte[]>(), _connection, _transaction);
        await _farmRelationRepository.Received(1).DeleteAsync(2, Arg.Any<byte[]>(), _connection, _transaction);
        await _herdSizeRepository.Received(1).AddAsync(Arg.Is<AddHerdSizeCommand>(c => c.HerdYear == 2022), _connection, _transaction);
        await _herdSizeRepository.Received(1).UpdateAsync(Arg.Is<UpdateHerdSizeCommand>(c => c.ID == 1 && c.TotalSize == 99), _connection, _transaction);
        await _herdSizeRepository.Received(1).DeleteAsync(2, Arg.Any<byte[]>(), _connection, _transaction);
        outcome.Warnings.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(BatchAssignmentResult.Success, false)]
    [InlineData(BatchAssignmentResult.AlreadyAssigned, false)]
    [InlineData(BatchAssignmentResult.BatchNotFound, true)]
    [InlineData(BatchAssignmentResult.InsertError, true)]
    public async Task PendingBatch_IsAssignedAndAnyOtherResultAbortsTheTransaction(BatchAssignmentResult result, bool shouldThrow)
    {
        _wizardState.GetAsync(Arg.Any<CancellationToken>()).Returns(new CaseWizardState(Rbse, "2026/1", 7));
        _batchRepository.AssignCaseToBatchAsync(7, Rbse, "BSE1", _connection, _transaction).Returns(result);
        _sessionState.GetAsync(Rbse, Arg.Any<CancellationToken>()).Returns((CaseEditSessionState?)null);

        var act = () => CreateService().CommitAllAsync(Rbse, 1);

        if (shouldThrow)
        {
            await act.Should().ThrowAsync<InvalidOperationException>();
            _transaction.Received(1).Rollback();
        }
        else
        {
            (await act()).Result.Should().Be(EditCaseResult.Success);
            await _wizardState.Received(1).ClearAsync();
        }
    }

    [Fact]
    public async Task PendingBatchForAnotherRbse_IsIgnored()
    {
        _wizardState.GetAsync(Arg.Any<CancellationToken>()).Returns(new CaseWizardState("009900099", "2026/1", 7));
        _sessionState.GetAsync(Rbse, Arg.Any<CancellationToken>()).Returns((CaseEditSessionState?)null);

        await CreateService().CommitAllAsync(Rbse, 1);

        await _batchRepository.DidNotReceive().AssignCaseToBatchAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
    }
}
#pragma warning restore xUnit1051
