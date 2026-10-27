using System.Data;
using System.Runtime.CompilerServices;
using BSE.Host.Models.ViewModels;
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
using Microsoft.Extensions.Logging.Abstractions;
using RelationRecord = BSE.Modules.AnimalRelations.Models.CaseRelationRecord;

namespace BSE.Host.Tests;

public sealed class CaseEditOrchestrationCommitTests
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
    private CaseEditSessionState _session = new() { Rbse = Rbse };

    public CaseEditOrchestrationCommitTests()
    {
        _connection.BeginTransaction().Returns(_transaction);
        _connectionFactory.CreateConnection().Returns(_connection);
        _sessionState.GetAsync(Rbse).Returns(_ => _session);
        _wizardState.GetAsync().Returns((CaseWizardState?)null);
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(ValidCase());
        _farmRepository.GetByCphhAsync(Cphh).Returns(ValidFarm());

        _farmRepository.UpdateAsync(Arg.Any<UpdateFarmCommand>(), Arg.Any<int>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(Task.FromResult<string?>(null));
        _babRepository.EditAsync(Arg.Any<EditCaseBabCommand>(), Arg.Any<string?>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(Task.FromResult<string?>(null));
        _clinicalRepository.EditAsync(Arg.Any<EditCaseClinicalCommand>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(Task.FromResult<string?>(null));
        _pedigreeRepository.AddEditDamSireAsync(Arg.Any<AddEditDamSireCommand>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(Task.FromResult<string?>(null));

        _feedRepository.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseFeedRecord>)[]);
        _relationsRepository.GetRelationsByRbseAsync(Rbse).Returns((IReadOnlyList<RelationRecord>)[]);
        _testRepository.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseTestRecord>)[]);
        _otherOwnerRepository.GetByRbseAsync(Rbse).Returns((IReadOnlyList<OtherOwnerRecord>)[]);
        _clinicalRepository.GetVisitsByRbseAsync(Rbse).Returns((IReadOnlyList<ClinicalVisitRecord>)[]);
        _farmRelationRepository.GetRelatedFarmAsync(Cphh).Returns((IEnumerable<FarmRelationRecord>)[]);
        _herdSizeRepository.GetByCphhAsync(Cphh).Returns((IEnumerable<HerdSizeRecord>)[]);
    }

    private CaseEditOrchestrationService Sut() => new(
        _sessionState, _wizardState, _caseRepository, _farmRepository, _babRepository, _clinicalRepository,
        _feedRepository, _relationsRepository, _pedigreeRepository, _caseWorkRepository, _batchRepository,
        _testRepository, _otherOwnerRepository, _herdSizeRepository, _farmRelationRepository, _connectionFactory,
        NullLogger<CaseEditOrchestrationService>.Instance);

    private static CaseRecord ValidCase() => new()
    {
        Rbse = Rbse,
        Cphh = Cphh,
        EartagCountry = "UK",
        EartagHerdmark = "12345",
        Eartag = "1",
        FormADate = DateTime.Today.AddDays(-10),
        RowStamp = [1, 2, 3]
    };

    private static FarmRecord ValidFarm() => new()
    {
        CPHH = Cphh,
        IsNonGBFarm = false,
        OwnerName = "Owner",
        Address1 = "1 Lane",
        Parish = "Parish",
        County = "County",
        AHO = "AHO",
        ADNSRegionID = 5,
        RowStamp = [1]
    };

    private static UpdateFarmCommand FarmCommand(string cphh = Cphh, string? owner = "Owner") => new(
        CPHH: cphh, OwnerName: owner, Address1: "1 Lane", Address2: null, Address3: null,
        Postcode: null, Parish: "Parish", District: null, County: "County",
        CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
        CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
        Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "AHO",
        HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: 5, RowStamp: [1]);

    private static EditCaseCommand CaseCommand(string? fate = null, DateTime? formB = null, string? eartag = "1", DateTime? formA = null) => new(
        Rbse: Rbse, EartagCountry: "UK", EartagHerdmark: "12345", Eartag: eartag, PreviousEartag: null,
        Bse1ReceivedDate: null, FormADate: formA ?? DateTime.Today, FormAResubmittedDate: null, FormBDate: formB,
        Fate: fate, FormCDate: null, IsPurchaserBse1Received: false, IsBreederBse1Received: false,
        IsVendor1Bse1Received: false, IsHomebredBse1Received: false, IsSummarySheetReceived: false,
        IsPaperworkComplete: false, ReportedLocation: null, Survey: null, Notes: null, BirthDate: null,
        IsBirthDateEst: null, DamStatus: null, BirthDateSource: null, ValuationAge: null, Sex: null,
        Breed: null, Origin: null, PurchaseDate: null, PurchaseAgeInMonths: null, PurchasedCounty: null,
        HerdEntryDate: null, OnsetDate: null, IsOnsetDateEst: null, MonthsPregnant: null,
        MonthsPostCalving: null, OnsetAgeInMonths: null, SlaughterDate: null, RowStamp: [9],
        AlternateDiagnosis: null, LabComment: null, CaseType: null);

    private static T Blank<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    [Fact]
    public async Task NothingStaged_ReturnsSuccessWithoutOpeningConnection()
    {
        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().BeEmpty();
        _connectionFactory.DidNotReceive().CreateConnection();
        await _sessionState.DidNotReceive().ClearAsync(Rbse);
    }

    [Fact]
    public async Task NullSession_NothingPending_ReturnsSuccess()
    {
        _session = null!;

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
    }

    [Fact]
    public async Task ExistingCase_CaseEdit_CommitsAndClearsSession()
    {
        _session.HasPendingChanges = true;
        _session.Case = CaseCommand();
        _caseRepository.EditCaseAsync(_session.Case, 7, _connection, _transaction).Returns(EditCaseResult.Success);

        var outcome = await Sut().CommitAllAsync(Rbse, 7);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().BeEmpty();
        _connection.Received(1).Open();
        _transaction.Received(1).Commit();
        await _sessionState.Received(1).ClearAsync(Rbse);
        await _wizardState.DidNotReceive().ClearAsync();
    }

    [Fact]
    public async Task ExistingCase_ConcurrencyConflict_IsSoftWarning()
    {
        _session.HasPendingChanges = true;
        _session.Case = CaseCommand();
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, _connection, _transaction).Returns(EditCaseResult.ConcurrencyConflict);

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().ContainSingle().Which.Should().Contain("modified by another user");
        _transaction.Received(1).Commit();
    }

    [Fact]
    public async Task ExistingCase_HardEditFailure_RollsBackAndReturnsFailure()
    {
        _session.HasPendingChanges = true;
        _session.Case = CaseCommand();
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, _connection, _transaction).Returns(EditCaseResult.PostUpdateError);

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.PostUpdateError);
        _transaction.Received(1).Rollback();
        _transaction.DidNotReceive().Commit();
        await _sessionState.DidNotReceive().ClearAsync(Rbse);
    }

    [Fact]
    public async Task ExistingCase_FarmWarningAndDamSireWarning_AreCollected()
    {
        _session.HasPendingChanges = true;
        _session.Farm = FarmCommand();
        _session.DamSire = Blank<AddEditDamSireCommand>();
        _farmRepository.UpdateAsync(_session.Farm, 1, _connection, _transaction).Returns(Task.FromResult<string?>("farm warn"));
        _pedigreeRepository.AddEditDamSireAsync(_session.DamSire, _connection, _transaction).Returns(Task.FromResult<string?>("pedigree warn"));

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        outcome.Warnings.Should().Equal("farm warn", "pedigree warn");
        _transaction.Received(1).Commit();
    }

    [Fact]
    public async Task ExistingCase_BabOnly_PassesExistingCaseThroughAndEditsExistingBab()
    {
        _session.HasPendingChanges = true;
        _session.Bab = Blank<EditCaseBabCommand>();
        _session.BabOrigin = "X";
        _babRepository.GetByRbseAsync(Rbse).Returns(Blank<CaseBabRecord>());
        _babRepository.EditAsync(_session.Bab, "X", _connection, _transaction).Returns(Task.FromResult<string?>("bab warn"));

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        await _caseRepository.Received(1).EditCaseAsync(
            Arg.Is<EditCaseCommand>(c => c.Rbse == Rbse && c.RowStamp.SequenceEqual(new byte[] { 1, 2, 3 })), 1, _connection, _transaction);
        await _babRepository.DidNotReceive().AddAsync(Arg.Any<AddCaseBabCommand>(), Arg.Any<string?>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        outcome.Warnings.Should().ContainSingle().Which.Should().Be("bab warn");
    }

    [Fact]
    public async Task ExistingCase_BabAndClinicalWithoutPersistedRows_AreAdded()
    {
        _session.HasPendingChanges = true;
        _session.Bab = Blank<EditCaseBabCommand>();
        _session.Clinical = Blank<EditCaseClinicalCommand>();
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);
        _clinicalRepository.GetByRbseAsync(Rbse).Returns((CaseClinicalRecord?)null);

        await Sut().CommitAllAsync(Rbse, 1);

        await _babRepository.Received(1).AddAsync(Arg.Any<AddCaseBabCommand>(), Arg.Any<string?>(), _connection, _transaction);
        await _clinicalRepository.Received(1).AddAsync(Arg.Any<AddCaseClinicalCommand>(), _connection, _transaction);
        await _babRepository.DidNotReceive().EditAsync(Arg.Any<EditCaseBabCommand>(), Arg.Any<string?>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        await _clinicalRepository.DidNotReceive().EditAsync(Arg.Any<EditCaseClinicalCommand>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
    }

    [Fact]
    public async Task ExistingCase_ClinicalAndCaseWork_EditExistingRows()
    {
        _session.HasPendingChanges = true;
        _session.Clinical = Blank<EditCaseClinicalCommand>();
        _session.CaseWork = Blank<EditCaseWorkCommand>();
        _clinicalRepository.GetByRbseAsync(Rbse).Returns(Blank<CaseClinicalRecord>());
        _clinicalRepository.EditAsync(_session.Clinical, _connection, _transaction).Returns(Task.FromResult<string?>("clinical warn"));

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        await _caseWorkRepository.Received(1).EditAsync(_session.CaseWork, _connection, _transaction);
        outcome.Warnings.Should().ContainSingle().Which.Should().Be("clinical warn");
    }

    [Fact]
    public async Task NewCase_CreatesFarmCaseCaseWorkBabAndClinical()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _farmRepository.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        _session.HasPendingChanges = true;
        _session.Farm = FarmCommand();
        _session.Case = CaseCommand();
        _session.Bab = Blank<EditCaseBabCommand>();
        _session.Clinical = Blank<EditCaseClinicalCommand>();
        _session.CaseWork = Blank<EditCaseWorkCommand>();
        _caseRepository.AddCaseAsync(Arg.Any<AddCaseCommand>(), 3, _connection, _transaction).Returns(AddCaseResult.Success);

        var outcome = await Sut().CommitAllAsync(Rbse, 3);

        outcome.Result.Should().Be(EditCaseResult.Success);
        await _farmRepository.Received(1).AddAsync(Arg.Is<AddFarmCommand>(f => f.CPHH == Cphh), 3, _connection, _transaction);
        await _caseWorkRepository.Received(1).AddAsync(Arg.Is<AddCaseWorkCommand>(c => c.Rbse == Rbse), _connection, _transaction);
        await _babRepository.Received(1).AddAsync(Arg.Any<AddCaseBabCommand>(), Arg.Any<string?>(), _connection, _transaction);
        await _clinicalRepository.Received(1).AddAsync(Arg.Any<AddCaseClinicalCommand>(), _connection, _transaction);
        _transaction.Received(1).Commit();
    }

    [Fact]
    public async Task NewCase_ExistingFarm_DoesNotAddFarm()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _session.HasPendingChanges = true;
        _session.Farm = FarmCommand();
        _session.Case = CaseCommand();

        await Sut().CommitAllAsync(Rbse, 3);

        await _farmRepository.DidNotReceive().AddAsync(Arg.Any<AddFarmCommand>(), Arg.Any<int>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        await _caseRepository.Received(1).AddCaseAsync(Arg.Any<AddCaseCommand>(), 3, _connection, _transaction);
    }

    [Fact]
    public async Task NewCase_AddCaseFails_RollsBackAndThrows()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _session.HasPendingChanges = true;
        _session.Farm = FarmCommand();
        _session.Case = CaseCommand();
        _caseRepository.AddCaseAsync(Arg.Any<AddCaseCommand>(), Arg.Any<int>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>()).Returns(AddCaseResult.DuplicateRbse);

        var act = () => Sut().CommitAllAsync(Rbse, 1);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("DuplicateRbse");
        _transaction.Received(1).Rollback();
        await _sessionState.DidNotReceive().ClearAsync(Rbse);
    }

    [Fact]
    public async Task NonGbFarmCphhOnNewCase_WithoutStoredFarm_ThrowsNoFarm()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _farmRepository.GetByCphhAsync("00123400001").Returns((FarmRecord?)null);
        _session.HasPendingChanges = true;
        _session.Farm = FarmCommand("00123400001");
        _session.Case = CaseCommand();

        var act = () => Sut().CommitAllAsync(Rbse, 1);

        (await act.Should().ThrowAsync<MandatoryCaseFieldsMissingException>()).Which.Errors
            .Should().Contain("No farm has been specified for the case.");
        _connectionFactory.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task ExistingCase_WhenFarmRecordMissing_ThrowsNoFarm()
    {
        _farmRepository.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        _session.HasPendingChanges = true;
        _session.Case = CaseCommand();

        var act = () => Sut().CommitAllAsync(Rbse, 1);

        (await act.Should().ThrowAsync<MandatoryCaseFieldsMissingException>()).Which.Errors
            .Should().Contain("No farm has been specified for the case.");
    }

    [Fact]
    public async Task MissingCaseFields_AreAllReported()
    {
        _session.HasPendingChanges = true;
        _session.Case = CaseCommand(fate: null, formB: DateTime.Today, eartag: null) with { EartagCountry = null, EartagHerdmark = null, FormADate = null };

        var act = () => Sut().CommitAllAsync(Rbse, 1);

        (await act.Should().ThrowAsync<MandatoryCaseFieldsMissingException>()).Which.Errors.Should().BeEquivalentTo(
        [
            "Please specify an eartag for the case.",
            "Please specify a Form A date for the case.",
            "Please specify a fate (Form B Reason) for the case."
        ]);
    }

    [Fact]
    public async Task NonGbFarm_DoesNotRequireParishAhoOrRegion()
    {
        _farmRepository.GetByCphhAsync(Cphh).Returns(new FarmRecord { CPHH = Cphh, IsNonGBFarm = true, OwnerName = "O", Address1 = "A", County = "C" });
        _session.HasPendingChanges = true;
        _session.Case = CaseCommand();

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
    }

    [Fact]
    public async Task RepositoryException_RollsBackAndRethrows()
    {
        _session.HasPendingChanges = true;
        _session.Case = CaseCommand();
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), Arg.Any<int>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns<Task<EditCaseResult>>(_ => throw new TimeoutException("db"));

        var act = () => Sut().CommitAllAsync(Rbse, 1);

        await act.Should().ThrowAsync<TimeoutException>();
        _transaction.Received(1).Rollback();
        await _sessionState.DidNotReceive().ClearAsync(Rbse);
    }

    [Fact]
    public async Task Batch_MatchingWizardState_AssignsCaseAndClearsWizard()
    {
        _wizardState.GetAsync().Returns(new CaseWizardState(Rbse.ToLowerInvariant(), "B1", 42));
        _batchRepository.AssignCaseToBatchAsync(42, Rbse, "BSE1", _connection, _transaction).Returns(BatchAssignmentResult.AlreadyAssigned);

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        await _batchRepository.Received(1).AssignCaseToBatchAsync(42, Rbse, "BSE1", _connection, _transaction);
        await _wizardState.Received(1).ClearAsync();
        await _sessionState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task Batch_OtherRbse_IsIgnored()
    {
        _wizardState.GetAsync().Returns(new CaseWizardState("999999999", "B1", 42));

        await Sut().CommitAllAsync(Rbse, 1);

        _connectionFactory.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task Batch_AssignmentFailure_RollsBackAndThrows()
    {
        _wizardState.GetAsync().Returns(new CaseWizardState(Rbse, "B1", 42));
        _batchRepository.AssignCaseToBatchAsync(42, Rbse, "BSE1", _connection, _transaction).Returns(BatchAssignmentResult.BatchNotFound);

        var act = () => Sut().CommitAllAsync(Rbse, 1);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("BatchNotFound");
        _transaction.Received(1).Rollback();
        await _wizardState.DidNotReceive().ClearAsync();
    }

    [Fact]
    public async Task Feeds_AddEditDeleteAndStaleRowsProduceExpectedCallsAndWarnings()
    {
        _feedRepository.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseFeedRecord>)
        [
            new CaseFeedRecord { Id = 1, RationType = "A", RationName = "gone", RowStamp = [1] },
            new CaseFeedRecord { Id = 2, RationType = "A", RationName = "orphan" },
            new CaseFeedRecord { Id = 3, RationType = "A", RationName = "same", YearFrom = 2000 },
            new CaseFeedRecord { Id = 4, RationType = "A", RationName = "old", YearFrom = 2000, RowStamp = [4] },
            new CaseFeedRecord { Id = 5, RationType = "A", RationName = "ok", YearFrom = 2000, RowStamp = [5] },
        ]);
        _feedRepository.DeleteAsync(1, Arg.Any<byte[]>(), _connection, _transaction).Returns(0);
        _feedRepository.EditAsync(Arg.Is<EditFeedCommand>(c => c.Id == 4), _connection, _transaction).Returns(0);
        _feedRepository.EditAsync(Arg.Is<EditFeedCommand>(c => c.Id == 5), _connection, _transaction).Returns(1);
        _session.HasFeedsChanges = true;
        _session.Feeds =
        [
            new() { Id = null, RationType = "B", RationName = "new" },
            new() { Id = 3, RationType = "A", RationName = "same", YearFrom = 2000 },
            new() { Id = 4, RationType = "A", RationName = "new name", YearFrom = 2000, RowStampBase64 = Convert.ToBase64String([7]) },
            new() { Id = 5, RationType = "A", RationName = "ok", YearFrom = 2001 },
            new() { Id = 99, RationType = "A" },
        ];

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        await _feedRepository.Received(1).AddAsync(Arg.Any<AddFeedCommand>(), _connection, _transaction);
        await _feedRepository.Received(1).DeleteAsync(1, Arg.Any<byte[]>(), _connection, _transaction);
        await _feedRepository.DidNotReceive().DeleteAsync(2, Arg.Any<byte[]>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        await _feedRepository.Received(2).EditAsync(Arg.Any<EditFeedCommand>(), _connection, _transaction);
        await _feedRepository.Received(1).EditAsync(
            Arg.Is<EditFeedCommand>(c => c.Id == 4 && c.RowStamp.SequenceEqual(new byte[] { 7 })), _connection, _transaction);
        await _feedRepository.Received(1).EditAsync(
            Arg.Is<EditFeedCommand>(c => c.Id == 5 && c.RowStamp.SequenceEqual(new byte[] { 5 })), _connection, _transaction);
        outcome.Warnings.Should().HaveCount(2);
        outcome.Warnings[0].Should().Contain("delete feed").And.Contain("gone");
        outcome.Warnings[1].Should().Contain("update feed").And.Contain("new name");
    }

    [Fact]
    public async Task Relations_AddEditDeleteAndStaleRowsProduceExpectedCallsAndWarnings()
    {
        _relationsRepository.GetRelationsByRbseAsync(Rbse).Returns((IReadOnlyList<RelationRecord>)
        [
            new RelationRecord { Id = 1, RelationType = "D", RelationRbse = "R1", RowStamp = [1] },
            new RelationRecord { Id = 2, RelationType = "D", RelationRbse = "R2" },
            new RelationRecord { Id = 3, RelationType = "D", RelationRbse = "R3", Sex = "F", RowStamp = [3] },
            new RelationRecord { Id = 4, RelationType = "D", RelationRbse = "R4", RowStamp = [4] },
            new RelationRecord { Id = 5, RelationType = "D", RelationRbse = "R5", RowStamp = [5] },
        ]);
        _relationsRepository.DeleteRelationAsync(Arg.Any<DeleteCaseRelationCommand>(), _connection, _transaction).Returns(0);
        _relationsRepository.EditRelationAsync(Arg.Is<EditCaseRelationCommand>(c => c.Id == 3), _connection, _transaction).Returns(0);
        _relationsRepository.EditRelationAsync(Arg.Is<EditCaseRelationCommand>(c => c.Id == 4), _connection, _transaction).Returns(1);
        _session.HasRelationsChanges = true;
        _session.Relations =
        [
            new() { RelationType = "S", RelationRbse = "NEW", BirthDay = 300, BirthMonth = 5, BirthYear = 2001 },
            new() { Id = 3, RelationType = "D", RelationRbse = "R3", Sex = "M", RowStampBase64 = Convert.ToBase64String([8]) },
            new() { Id = 4, RelationType = "D", RelationRbse = "R4", LeftDate = DateTime.Today },
            new() { Id = 5, RelationType = "D", RelationRbse = "R5" },
            new() { Id = 99, RelationType = "D" },
        ];

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        await _relationsRepository.Received(1).AddRelationAsync(
            Arg.Is<AddCaseRelationCommand>(c => c.RelationRbse == "NEW"), _connection, _transaction);
        await _relationsRepository.Received(1).DeleteRelationAsync(Arg.Any<DeleteCaseRelationCommand>(), _connection, _transaction);
        await _relationsRepository.Received(2).EditRelationAsync(Arg.Any<EditCaseRelationCommand>(), _connection, _transaction);
        outcome.Warnings.Should().HaveCount(2);
        outcome.Warnings[0].Should().Contain("delete relation").And.Contain("R1");
        outcome.Warnings[1].Should().Contain("update relation").And.Contain("R3");
    }

    [Fact]
    public async Task TestsAndOtherOwners_AreAddedEditedAndDeleted()
    {
        _testRepository.GetByRbseAsync(Rbse).Returns((IReadOnlyList<CaseTestRecord>)
        [
            new CaseTestRecord(1, Rbse, "T1", "Type One", "P", null, [1]),
            new CaseTestRecord(2, Rbse, "T2", "Type Two", "P", null, null),
            new CaseTestRecord(3, Rbse, "T3", "Type Three", "P", null, [3]),
            new CaseTestRecord(4, Rbse, "T4", "Type Four", "P", null, [4]),
            new CaseTestRecord(5, Rbse, "T5", "Type Five", "P", null, [5]),
        ]);
        _testRepository.DeleteAsync(1, Arg.Any<byte[]>(), _connection, _transaction).Returns(0);
        _testRepository.EditAsync(Arg.Is<EditTestCommand>(c => c.Id == 3), _connection, _transaction).Returns(0);
        _testRepository.EditAsync(Arg.Is<EditTestCommand>(c => c.Id == 4), _connection, _transaction).Returns(1);
        _otherOwnerRepository.GetByRbseAsync(Rbse).Returns((IReadOnlyList<OtherOwnerRecord>)
        [
            new OtherOwnerRecord(10, Rbse, "T", "Gone", null, [1]),
            new OtherOwnerRecord(11, Rbse, "T", "Same", "C1", [2]),
            new OtherOwnerRecord(12, Rbse, "T", "Old", null, [3]),
            new OtherOwnerRecord(13, Rbse, "T", "Old2", null, null),
        ]);
        _otherOwnerRepository.DeleteAsync(10, Arg.Any<byte[]>(), _connection, _transaction).Returns(0);
        _otherOwnerRepository.EditAsync(Arg.Is<EditOtherOwnerCommand>(c => c.Id == 12), _connection, _transaction).Returns(0);
        _otherOwnerRepository.EditAsync(Arg.Is<EditOtherOwnerCommand>(c => c.Id == 13), _connection, _transaction).Returns(1);
        _session.HasCaseEditChanges = true;
        _session.Tests =
        [
            new() { TestType = "NEW", TestResult = "N" },
            new() { Id = 3, TestType = "T3", TestResult = "Q", RowStampBase64 = Convert.ToBase64String([6]) },
            new() { Id = 4, TestType = "T4", TestResult = "Q" },
            new() { Id = 5, TestType = "t5", TestResult = "p" },
            new() { Id = 99, TestType = "X" },
        ];
        _session.OtherOwners =
        [
            new() { Type = "T", Name = " ", Cphh = "" },
            new() { Id = 11, Type = "T", Name = "Same", Cphh = "C1" },
            new() { Id = 12, Type = "T", Name = "Changed" },
            new() { Id = 13, Type = "T", Name = "Changed2" },
            new() { Id = 98, Type = "T", Name = "Stray" },
        ];

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        await _testRepository.Received(1).AddAsync(Arg.Any<AddTestCommand>(), _connection, _transaction);
        await _testRepository.Received(2).EditAsync(Arg.Any<EditTestCommand>(), _connection, _transaction);
        await _testRepository.DidNotReceive().DeleteAsync(2, Arg.Any<byte[]>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        await _otherOwnerRepository.Received(2).AddAsync(Arg.Any<AddOtherOwnerCommand>(), _connection, _transaction);
        await _otherOwnerRepository.Received(2).EditAsync(Arg.Any<EditOtherOwnerCommand>(), _connection, _transaction);
        outcome.Warnings.Should().HaveCount(4);
        outcome.Warnings.Should().Contain(w => w.Contains("delete test") && w.Contains("Type One"));
        outcome.Warnings.Should().Contain(w => w.Contains("update test") && w.Contains("T3"));
        outcome.Warnings.Should().Contain(w => w.Contains("delete owner") && w.Contains("Gone"));
        outcome.Warnings.Should().Contain(w => w.Contains("update owner") && w.Contains("Changed"));
    }

    [Fact]
    public async Task ClinicalVisits_AreAddedEditedAndDeleted()
    {
        var d = new DateTime(2020, 1, 1);
        _clinicalRepository.GetVisitsByRbseAsync(Rbse).Returns((IReadOnlyList<ClinicalVisitRecord>)
        [
            new ClinicalVisitRecord(1, Rbse, d, [1]),
            new ClinicalVisitRecord(2, Rbse, d, null),
            new ClinicalVisitRecord(3, Rbse, d, [3]),
            new ClinicalVisitRecord(4, Rbse, d, [4]),
            new ClinicalVisitRecord(5, Rbse, d, [5]),
        ]);
        _clinicalRepository.DeleteVisitAsync(1, Arg.Any<byte[]>(), _connection, _transaction).Returns(0);
        _clinicalRepository.EditVisitAsync(Arg.Is<EditClinicalVisitCommand>(c => c.Id == 3), _connection, _transaction).Returns(0);
        _clinicalRepository.EditVisitAsync(Arg.Is<EditClinicalVisitCommand>(c => c.Id == 4), _connection, _transaction).Returns(1);
        _session.HasClinicalVisitsChanges = true;
        _session.Visits =
        [
            new() { VisitDate = d.AddYears(5) },
            new() { Id = 3, VisitDate = d.AddDays(1), RowStampBase64 = Convert.ToBase64String([2]) },
            new() { Id = 4, VisitDate = d.AddDays(2) },
            new() { Id = 5, VisitDate = d },
            new() { Id = 99, VisitDate = d },
        ];

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        await _clinicalRepository.Received(1).AddVisitAsync(Arg.Any<AddClinicalVisitCommand>(), _connection, _transaction);
        await _clinicalRepository.Received(2).EditVisitAsync(Arg.Any<EditClinicalVisitCommand>(), _connection, _transaction);
        await _clinicalRepository.DidNotReceive().DeleteVisitAsync(2, Arg.Any<byte[]>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        outcome.Warnings.Should().HaveCount(2);
        outcome.Warnings[0].Should().Contain("delete visit").And.Contain("01/01/2020");
        outcome.Warnings[1].Should().Contain("update visit").And.Contain("02/01/2020");
    }

    [Fact]
    public async Task FarmCollections_LinkedFarmsAndHerdSizes_AreSynchronised()
    {
        _farmRelationRepository.GetRelatedFarmAsync(Cphh).Returns((IEnumerable<FarmRelationRecord>)
        [
            new FarmRelationRecord { ID = 1, RelatedCPHH = "11", RowStamp = [1] },
            new FarmRelationRecord { ID = 2, RelatedCPHH = "22", RowStamp = null },
            new FarmRelationRecord { ID = 3, RelatedCPHH = "33", RowStamp = [3] },
            new FarmRelationRecord { ID = 4, RelatedCPHH = "44", RowStamp = [4] },
            new FarmRelationRecord { ID = 5, RelatedCPHH = "55", RowStamp = [5] },
            new FarmRelationRecord { ID = 6, RelatedCPHH = "66", RowStamp = null },
        ]);
        _farmRelationRepository.DeleteAsync(1, Arg.Any<byte[]>(), _connection, _transaction).Returns(0);
        _farmRelationRepository.UpdateAsync(3, "331", Arg.Any<byte[]>(), _connection, _transaction).Returns(0);
        _farmRelationRepository.UpdateAsync(4, "441", Arg.Any<byte[]>(), _connection, _transaction).Returns(1);
        _herdSizeRepository.GetByCphhAsync(Cphh).Returns((IEnumerable<HerdSizeRecord>)
        [
            new HerdSizeRecord { ID = 1, HerdYear = 2000, TotalSize = 10, RowStamp = [1] },
            new HerdSizeRecord { ID = 2, HerdYear = 2001, TotalSize = 10, RowStamp = null },
            new HerdSizeRecord { ID = 3, HerdYear = 2002, TotalSize = 10, RowStamp = [3] },
            new HerdSizeRecord { ID = 4, HerdYear = 2003, TotalSize = 10, RowStamp = [4] },
            new HerdSizeRecord { ID = 5, HerdYear = 2004, TotalSize = 10, RowStamp = [5] },
            new HerdSizeRecord { ID = 6, HerdYear = 2005, TotalSize = 10, RowStamp = null },
        ]);
        _herdSizeRepository.DeleteAsync(1, Arg.Any<byte[]>(), _connection, _transaction).Returns(0);
        _herdSizeRepository.UpdateAsync(Arg.Is<UpdateHerdSizeCommand>(c => c.ID == 3), _connection, _transaction).Returns(0);
        _herdSizeRepository.UpdateAsync(Arg.Is<UpdateHerdSizeCommand>(c => c.ID == 4), _connection, _transaction).Returns(1);
        _session.HasFarmCollectionsChanges = true;
        _session.LinkedFarms =
        [
            new() { RelatedCphh = "99 9" },
            new() { Id = 3, RelatedCphh = "331", RowStampBase64 = Convert.ToBase64String([3]) },
            new() { Id = 4, RelatedCphh = "441" },
            new() { Id = 5, RelatedCphh = "55" },
            new() { Id = 6, RelatedCphh = "661" },
            new() { Id = 77, RelatedCphh = "77" },
        ];
        _session.HerdSizes =
        [
            new() { HerdYear = 2010, TotalSize = 5, Lactation1Size = 2 },
            new() { Id = 3, HerdYear = 2002, TotalSize = 11, RowStampBase64 = Convert.ToBase64String([3]) },
            new() { Id = 4, HerdYear = 2003, TotalSize = 10, Lactation2Size = 1 },
            new() { Id = 5, HerdYear = 2004, TotalSize = 10 },
            new() { Id = 6, HerdYear = 2005, TotalSize = 12 },
            new() { Id = 77, HerdYear = 2007, TotalSize = 1 },
        ];

        var outcome = await Sut().CommitAllAsync(Rbse, 1);

        await _farmRelationRepository.Received(1).AddAsync(Cphh, "999", _connection, _transaction);
        await _farmRelationRepository.Received(2).UpdateAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<byte[]>(), _connection, _transaction);
        await _farmRelationRepository.DidNotReceive().DeleteAsync(2, Arg.Any<byte[]>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        await _herdSizeRepository.Received(1).AddAsync(Arg.Is<AddHerdSizeCommand>(c => c.HerdYear == 2010 && c.TotalSize == 5), _connection, _transaction);
        await _herdSizeRepository.Received(2).UpdateAsync(Arg.Any<UpdateHerdSizeCommand>(), _connection, _transaction);
        await _herdSizeRepository.DidNotReceive().DeleteAsync(2, Arg.Any<byte[]>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        outcome.Warnings.Should().HaveCount(4);
        outcome.Warnings.Should().Contain(w => w.Contains("delete linked farm") && w.Contains("11"));
        outcome.Warnings.Should().Contain(w => w.Contains("update linked farm") && w.Contains("331"));
        outcome.Warnings.Should().Contain(w => w.Contains("delete herd size") && w.Contains("2000"));
        outcome.Warnings.Should().Contain(w => w.Contains("update herd size") && w.Contains("2002"));
    }

    [Fact]
    public async Task StagedFarmCphh_IsUsedForFarmCollectionsLookup()
    {
        _session.HasPendingChanges = true;
        _session.Farm = FarmCommand();
        _session.HasFarmCollectionsChanges = true;

        await Sut().CommitAllAsync(Rbse, 1);

        await _farmRelationRepository.Received(1).GetRelatedFarmAsync(Cphh);
        await _herdSizeRepository.Received(1).GetByCphhAsync(Cphh);
    }
}
