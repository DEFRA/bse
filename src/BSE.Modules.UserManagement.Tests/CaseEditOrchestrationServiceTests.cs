using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Data;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>Direct coverage of <see cref="CaseEditOrchestrationService"/>'s mandatory-fields check,
/// specifically that it accumulates every failing field into one exception rather than stopping at
/// the first one — reported symptom: SaveResult only ever showed one message when multiple fields
/// (e.g. farm Owner Name and ADNS Region) were missing at once.</summary>
public sealed class CaseEditOrchestrationServiceTests
{
    private const string Rbse = "002600001";
    private const string Cphh = "01001000101";

    private readonly ICaseScalarDraftStateService _scalarDraftState = Substitute.For<ICaseScalarDraftStateService>();
    private readonly ICaseFeedsDraftStateService _feedsDraftState = Substitute.For<ICaseFeedsDraftStateService>();
    private readonly ICaseRelationsDraftStateService _relationsDraftState = Substitute.For<ICaseRelationsDraftStateService>();
    private readonly ICaseWizardStateService _wizardState = Substitute.For<ICaseWizardStateService>();
    private readonly ICaseRepository _caseRepository = Substitute.For<ICaseRepository>();
    private readonly IFarmRepository _farmRepository = Substitute.For<IFarmRepository>();
    private readonly IBabRepository _babRepository = Substitute.For<IBabRepository>();
    private readonly IClinicalRepository _clinicalRepository = Substitute.For<IClinicalRepository>();
    private readonly IFeedRepository _feedRepository = Substitute.For<IFeedRepository>();
    private readonly IAnimalRelationsRepository _relationsRepository = Substitute.For<IAnimalRelationsRepository>();
    private readonly IPedigreeRepository _pedigreeRepository = Substitute.For<IPedigreeRepository>();
    private readonly BSE.Modules.CaseWork.Repositories.ICaseWorkRepository _caseWorkRepository = Substitute.For<BSE.Modules.CaseWork.Repositories.ICaseWorkRepository>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();

    private CaseEditOrchestrationService CreateService() => new(
        new CaseEditDraftStores(_scalarDraftState, _feedsDraftState, _relationsDraftState, _wizardState),
        new CaseEditRepositories(
            _caseRepository, _farmRepository, _babRepository, _clinicalRepository,
            _feedRepository, _relationsRepository, _pedigreeRepository, _caseWorkRepository, _batchRepository),
        _connectionFactory,
        NullLogger<CaseEditOrchestrationService>.Instance);

    private static CaseRecord ValidCaseRecord() => new()
    {
        Rbse = Rbse,
        Cphh = Cphh,
        EartagCountry = "UK",
        EartagHerdmark = "12345",
        Eartag = "1",
        FormADate = DateTime.Today.AddDays(-10),
        IsNonGbCase = false,
        FormBDate = null,
        RowStamp = [1, 2, 3]
    };

    [Fact]
    public async Task CommitAllAsync_WithMultipleMissingFarmFields_ThrowsExceptionListingAllOfThem()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = true,
            Farm = new UpdateFarmCommand(
                CPHH: Cphh, OwnerName: null, Address1: "1 Farm Lane", Address2: null, Address3: null,
                Postcode: null, Parish: "Some Parish", District: null, County: "Some County",
                CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
                CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
                Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "Some AHO",
                HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: null, RowStamp: [1])
        });
        _feedsDraftState.GetAsync(Rbse).Returns((CaseFeedsDraftState?)null);
        _relationsDraftState.GetAsync(Rbse).Returns((CaseRelationsDraftState?)null);

        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(ValidCaseRecord());
        _farmRepository.GetByCphhAsync(Cphh).Returns(new FarmRecord
        {
            CPHH = Cphh,
            IsNonGBFarm = false,
            OwnerName = null,
            Address1 = "1 Farm Lane",
            Parish = "Some Parish",
            County = "Some County",
            AHO = "Some AHO",
            ADNSRegionID = null,
            RowStamp = [1, 2, 3]
        });

        var sut = CreateService();

        var act = () => sut.CommitAllAsync(Rbse, userId: 1);

        var thrown = await act.Should().ThrowAsync<MandatoryCaseFieldsMissingException>();
        thrown.Which.Errors.Should().BeEquivalentTo(
        [
            "Please enter an owner name for the farm.",
            "Please specify an ADNS Region for the farm."
        ]);
    }

    [Fact]
    public async Task CommitAllAsync_WhenStagedFarmClearsAFieldThatStillHasAnOldDbValue_StillFlagsItMissing()
    {
        // Reproduces the reported bug: the farm ALREADY has a non-null OwnerName in the database from a
        // previous save, but the user clears it on this round's Farm tab submission. The effective-value
        // resolution must use the staged (now-null) value, not silently fall back to the stale DB value
        // — otherwise the mandatory check passes incorrectly and a NULL reaches the database's NOT NULL
        // OwnerName column, causing a SQL exception instead of the expected SaveResult screen.
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = true,
            Farm = new UpdateFarmCommand(
                CPHH: Cphh, OwnerName: null, Address1: "1 Farm Lane", Address2: null, Address3: null,
                Postcode: null, Parish: "Some Parish", District: null, County: "Some County",
                CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
                CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
                Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "Some AHO",
                HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: 5, RowStamp: [1])
        });
        _feedsDraftState.GetAsync(Rbse).Returns((CaseFeedsDraftState?)null);
        _relationsDraftState.GetAsync(Rbse).Returns((CaseRelationsDraftState?)null);

        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(ValidCaseRecord());
        _farmRepository.GetByCphhAsync(Cphh).Returns(new FarmRecord
        {
            CPHH = Cphh,
            IsNonGBFarm = false,
            OwnerName = "Previously Saved Owner", // stale DB value — must NOT mask the staged clear
            Address1 = "1 Farm Lane",
            Parish = "Some Parish",
            County = "Some County",
            AHO = "Some AHO",
            ADNSRegionID = 5,
            RowStamp = [1, 2, 3]
        });

        var sut = CreateService();

        var act = () => sut.CommitAllAsync(Rbse, userId: 1);

        var thrown = await act.Should().ThrowAsync<MandatoryCaseFieldsMissingException>();
        thrown.Which.Errors.Should().BeEquivalentTo(["Please enter an owner name for the farm."]);
    }

    [Fact]
    public async Task CommitAllAsync_WhenCaseDoesNotExistYet_CreatesFarmAndCaseInOneTransaction()
    {
        // Legacy parity: a brand-new case lives only in the shared session DataSet until the first
        // Save from any tab — CommitAllAsync must insert both Farm and Case rows together rather
        // than trying (and failing) to UPDATE rows that don't exist yet.
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = true,
            Farm = new UpdateFarmCommand(
                CPHH: Cphh, OwnerName: "New Owner", Address1: "1 Farm Lane", Address2: null, Address3: null,
                Postcode: null, Parish: "Some Parish", District: null, County: "Some County",
                CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
                CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
                Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "Some AHO",
                HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: 5, RowStamp: []),
            Case = new EditCaseCommand(
                Rbse: Rbse, EartagCountry: "UK", EartagHerdmark: "12345", Eartag: "1", PreviousEartag: null,
                Bse1ReceivedDate: null, FormADate: DateTime.Today, FormAResubmittedDate: null, FormBDate: null,
                Fate: null, FormCDate: null, IsPurchaserBse1Received: false, IsBreederBse1Received: false,
                IsVendor1Bse1Received: false, IsHomebredBse1Received: false, IsSummarySheetReceived: false,
                IsPaperworkComplete: false, ReportedLocation: null, Survey: null, Notes: null, BirthDate: null,
                IsBirthDateEst: null, DamStatus: null, BirthDateSource: null, ValuationAge: null, Sex: null,
                Breed: null, Origin: null, PurchaseDate: null, PurchaseAgeInMonths: null, PurchasedCounty: null,
                HerdEntryDate: null, OnsetDate: null, IsOnsetDateEst: null, MonthsPregnant: null,
                MonthsPostCalving: null, OnsetAgeInMonths: null, SlaughterDate: null, RowStamp: [],
                AlternateDiagnosis: null, LabComment: null, CaseType: null)
        });
        _feedsDraftState.GetAsync(Rbse).Returns((CaseFeedsDraftState?)null);
        _relationsDraftState.GetAsync(Rbse).Returns((CaseRelationsDraftState?)null);
        _wizardState.GetAsync().Returns((CaseWizardState?)null);

        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _farmRepository.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        _caseRepository.AddCaseAsync(Arg.Any<AddCaseCommand>(), Arg.Any<int>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(AddCaseResult.Success);

        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction().Returns(transaction);
        _connectionFactory.CreateConnection().Returns(connection);

        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        await _farmRepository.Received(1).AddAsync(
            Arg.Is<AddFarmCommand>(f => f.CPHH == Cphh && f.OwnerName == "New Owner"), 1, connection, transaction);
        await _caseRepository.Received(1).AddCaseAsync(
            Arg.Is<AddCaseCommand>(c => c.Rbse == Rbse && c.Cphh == Cphh && c.Eartag == "1"), 1, connection, transaction);
        await _farmRepository.DidNotReceive().UpdateAsync(Arg.Any<UpdateFarmCommand>(), Arg.Any<int>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        await _caseRepository.DidNotReceive().EditCaseAsync(Arg.Any<EditCaseCommand>(), Arg.Any<int>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        transaction.Received(1).Commit();
    }

    // ── Characterisation tests ───────────────────────────────────────────────────
    // Pin the observable outcomes of every branch of CommitAllAsync before it is restructured
    // to reduce its cognitive complexity. A refactor that alters any of these is a regression.

    private IDbTransaction ArrangeConnection()
    {
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction().Returns(transaction);
        _connectionFactory.CreateConnection().Returns(connection);
        return transaction;
    }

    private static EditCaseCommand ValidEditCase() => new(
        Rbse: Rbse, EartagCountry: "UK", EartagHerdmark: "12345", Eartag: "1", PreviousEartag: null,
        Bse1ReceivedDate: null, FormADate: DateTime.Today, FormAResubmittedDate: null, FormBDate: null,
        Fate: null, FormCDate: null, IsPurchaserBse1Received: false, IsBreederBse1Received: false,
        IsVendor1Bse1Received: false, IsHomebredBse1Received: false, IsSummarySheetReceived: false,
        IsPaperworkComplete: false, ReportedLocation: null, Survey: null, Notes: null, BirthDate: null,
        IsBirthDateEst: null, DamStatus: null, BirthDateSource: null, ValuationAge: null, Sex: null,
        Breed: null, Origin: null, PurchaseDate: null, PurchaseAgeInMonths: null, PurchasedCounty: null,
        HerdEntryDate: null, OnsetDate: null, IsOnsetDateEst: null, MonthsPregnant: null,
        MonthsPostCalving: null, OnsetAgeInMonths: null, SlaughterDate: null, RowStamp: [1, 2, 3],
        AlternateDiagnosis: null, LabComment: null, CaseType: null);

    private static FarmRecord ValidFarmRecord() => new()
    {
        CPHH = Cphh,
        IsNonGBFarm = false,
        OwnerName = "Owner",
        Address1 = "1 Farm Lane",
        Parish = "Some Parish",
        County = "Some County",
        AHO = "Some AHO",
        ADNSRegionID = 5,
        RowStamp = [1, 2, 3]
    };

    private void ArrangeExistingValidCase()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(ValidCaseRecord());
        _farmRepository.GetByCphhAsync(Cphh).Returns(ValidFarmRecord());
    }

    [Fact]
    public async Task CommitAllAsync_WhenNothingIsStaged_ReturnsSuccessAndNeverOpensAConnection()
    {
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().BeEmpty();
        _connectionFactory.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task CommitAllAsync_WhenDraftExistsButHasNoPendingChanges_WritesNothing()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            Case = ValidEditCase(),
            HasPendingChanges = false
        });
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        _connectionFactory.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task CommitAllAsync_WhenCaseEditSucceeds_CommitsAndClearsTheScalarDraft()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            Case = ValidEditCase(),
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(EditCaseResult.Success);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().BeEmpty();
        transaction.Received(1).Commit();
        transaction.DidNotReceive().Rollback();
        await _scalarDraftState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task CommitAllAsync_WhenCaseEditHitsConcurrencyConflict_StillCommitsWithASoftWarning()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            Case = ValidEditCase(),
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(EditCaseResult.ConcurrencyConflict);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().ContainSingle()
            .Which.Should().Be($"The case record with RBSE {Rbse} has been modified by another user");
        transaction.Received(1).Commit();
        await _scalarDraftState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task CommitAllAsync_WhenCaseEditFailsHard_RollsBackAndKeepsTheDraft()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            Case = ValidEditCase(),
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(EditCaseResult.PostUpdateError);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.PostUpdateError);
        transaction.Received(1).Rollback();
        transaction.DidNotReceive().Commit();
        await _scalarDraftState.DidNotReceiveWithAnyArgs().ClearAsync(default!);
    }

    [Fact]
    public async Task CommitAllAsync_WhenFarmBabAndClinicalAllWarn_AccumulatesEveryWarningAndCommits()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            Farm = new UpdateFarmCommand(
                CPHH: Cphh, OwnerName: "Owner", Address1: "1 Farm Lane", Address2: null, Address3: null,
                Postcode: null, Parish: "Some Parish", District: null, County: "Some County",
                CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
                CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
                Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "Some AHO",
                HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: 5, RowStamp: [1]),
            Bab = new EditCaseBabCommand(Rbse, null, null, null, null, null, null, null, null, null, null, [1]),
            Clinical = new EditCaseClinicalCommand(Rbse,
                false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false, [1]),
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(EditCaseResult.Success);
        _farmRepository.UpdateAsync(Arg.Any<UpdateFarmCommand>(), 1, Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns("farm warning");
        _babRepository.EditAsync(Arg.Any<EditCaseBabCommand>(), Arg.Any<string?>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns("bab warning");
        _clinicalRepository.EditAsync(Arg.Any<EditCaseClinicalCommand>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns("clinical warning");
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().BeEquivalentTo(["farm warning", "bab warning", "clinical warning"]);
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAllAsync_WhenARepositoryThrows_RollsBackAndRethrows()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            Case = ValidEditCase(),
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns<EditCaseResult>(_ => throw new InvalidOperationException("boom"));
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var act = () => sut.CommitAllAsync(Rbse, userId: 1);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        transaction.Received(1).Rollback();
        await _scalarDraftState.DidNotReceiveWithAnyArgs().ClearAsync(default!);
    }

    [Fact]
    public async Task CommitAllAsync_WhenOnlyABatchIsStaged_LinksTheBatchAndClearsWizardState()
    {
        _wizardState.GetAsync().Returns(new CaseWizardState(Rbse, "1989/731", 42));
        ArrangeExistingValidCase();
        _batchRepository.AssignCaseToBatchAsync(42, Rbse, Arg.Any<string>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(BatchAssignmentResult.Success);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        await _batchRepository.Received(1)
            .AssignCaseToBatchAsync(42, Rbse, "BSE1", Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        await _wizardState.Received(1).ClearAsync();
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAllAsync_WhenBatchAlreadyAssigned_IsTreatedAsSuccess()
    {
        _wizardState.GetAsync().Returns(new CaseWizardState(Rbse, "1989/731", 42));
        ArrangeExistingValidCase();
        _batchRepository.AssignCaseToBatchAsync(42, Rbse, Arg.Any<string>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(BatchAssignmentResult.AlreadyAssigned);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAllAsync_WhenBatchAssignmentFails_RollsBackAndThrows()
    {
        _wizardState.GetAsync().Returns(new CaseWizardState(Rbse, "1989/731", 42));
        ArrangeExistingValidCase();
        _batchRepository.AssignCaseToBatchAsync(42, Rbse, Arg.Any<string>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(BatchAssignmentResult.BatchNotFound);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var act = () => sut.CommitAllAsync(Rbse, userId: 1);

        await act.Should().ThrowAsync<InvalidOperationException>();
        transaction.Received(1).Rollback();
        await _wizardState.DidNotReceive().ClearAsync();
    }

    [Fact]
    public async Task CommitAllAsync_WhenBatchIsStagedForADifferentRbse_IgnoresIt()
    {
        _wizardState.GetAsync().Returns(new CaseWizardState("999999999", "1989/731", 42));
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        _connectionFactory.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task CommitAllAsync_WhenAStagedFeedWasRemoved_DeletesItAndWarnsOnAStaleRowStamp()
    {
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState
        {
            Rbse = Rbse,
            Feeds = [],
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _feedRepository.GetByRbseAsync(Rbse).Returns([
            new CaseFeedRecord { Id = 7, Rbse = Rbse, RationName = "Ration A", RowStamp = [9, 9, 9] }
        ]);
        _feedRepository.DeleteAsync(7, Arg.Any<byte[]>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(0);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().ContainSingle()
            .Which.Should().Be("Failed to delete feed with Ration Name \"Ration A\" - Data was changed by another user");
        transaction.Received(1).Commit();
        await _feedsDraftState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task CommitAllAsync_WhenAStagedFeedIsNew_InsertsItWithoutWarnings()
    {
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState
        {
            Rbse = Rbse,
            Feeds = [new CaseFeedsDraftItem { Id = null, YearFrom = 1995, YearTo = 1996, RationType = "C", RationName = "New Ration" }],
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _feedRepository.GetByRbseAsync(Rbse).Returns([]);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Warnings.Should().BeEmpty();
        await _feedRepository.Received(1).AddAsync(
            Arg.Is<AddFeedCommand>(f => f.Rbse == Rbse && f.RationName == "New Ration" && f.YearFrom == 1995),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAllAsync_WhenAStagedFeedIsUnchanged_DoesNotUpdateIt()
    {
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState
        {
            Rbse = Rbse,
            Feeds = [new CaseFeedsDraftItem { Id = 7, YearFrom = 1995, YearTo = 1996, RationType = "C", RationName = "Ration A", IsPrePurchase = false, SupplierId = null }],
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _feedRepository.GetByRbseAsync(Rbse).Returns([
            new CaseFeedRecord { Id = 7, Rbse = Rbse, YearFrom = 1995, YearTo = 1996, RationType = "C", RationName = "Ration A", IsPrePurchase = false, SupplierId = null, RowStamp = [9, 9, 9] }
        ]);
        ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Warnings.Should().BeEmpty();
        await _feedRepository.DidNotReceiveWithAnyArgs().EditAsync(default!, default!, default!);
        await _feedRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default, default!, default!, default!);
    }

    [Fact]
    public async Task CommitAllAsync_WhenAStagedRelationWasRemoved_DeletesItAndWarnsOnAStaleRowStamp()
    {
        _relationsDraftState.GetAsync(Rbse).Returns(new CaseRelationsDraftState
        {
            Rbse = Rbse,
            Relations = [],
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _relationsRepository.GetRelationsByRbseAsync(Rbse).Returns([
            new BSE.Modules.AnimalRelations.Models.CaseRelationRecord
            {
                Id = 3, Rbse = Rbse, RelationRbse = "008900184", RowStamp = [8, 8, 8]
            }
        ]);
        _relationsRepository.DeleteRelationAsync(
                Arg.Any<BSE.Modules.AnimalRelations.Commands.DeleteCaseRelationCommand>(),
                Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(0);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Warnings.Should().ContainSingle()
            .Which.Should().Be("Failed to delete relation with RBSE \"008900184\" - Data was changed by another user");
        transaction.Received(1).Commit();
        await _relationsDraftState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task CommitAllAsync_WhenDamSireIsStaged_SurfacesThePedigreeWarning()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            DamSire = new AddEditDamSireCommand(
                Rbse, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null, null),
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _pedigreeRepository.AddEditDamSireAsync(Arg.Any<AddEditDamSireCommand>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns("dam sire warning");
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        outcome.Warnings.Should().ContainSingle().Which.Should().Be("dam sire warning");
        transaction.Received(1).Commit();
    }

    // ── CheckCaseMandatoryFields coverage ───────────────────────────────────────
    // CommitAllAsync_WithMultipleMissingFarmFields_ThrowsExceptionListingAllOfThem (above) only
    // exercises the farm half of CheckMandatoryFieldsAsync. The case-side checks (eartag, Form A
    // date, Form B fate) were previously untested — none of the existing fixtures leave them blank.

    [Fact]
    public async Task CommitAllAsync_WhenEartagAndFormADateAreMissing_ThrowsListingBothCaseErrors()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = true,
            Case = ValidEditCase() with { EartagCountry = null, EartagHerdmark = null, Eartag = null, FormADate = null }
        });
        ArrangeExistingValidCase();

        var sut = CreateService();

        var act = () => sut.CommitAllAsync(Rbse, userId: 1);

        var thrown = await act.Should().ThrowAsync<MandatoryCaseFieldsMissingException>();
        thrown.Which.Errors.Should().BeEquivalentTo(
        [
            "Please specify an eartag for the case.",
            "Please specify a Form A date for the case."
        ]);
    }

    [Fact]
    public async Task CommitAllAsync_WhenFormBDateIsSetWithoutAFate_ThrowsFateMissingError()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = true,
            Case = ValidEditCase() with { FormBDate = DateTime.Today, Fate = null }
        });
        ArrangeExistingValidCase();

        var sut = CreateService();

        var act = () => sut.CommitAllAsync(Rbse, userId: 1);

        var thrown = await act.Should().ThrowAsync<MandatoryCaseFieldsMissingException>();
        thrown.Which.Errors.Should().BeEquivalentTo(["Please specify a fate (Form B Reason) for the case."]);
    }

    [Fact]
    public async Task CommitAllAsync_WhenCaseIsNonGb_SkipsTheFormADateCheck()
    {
        // isNonGbCase is read from the persisted CaseRecord, not the staged command — a non-GB case
        // with no staged Form A date must not be flagged, unlike the GB case tested above.
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = true,
            Case = ValidEditCase() with { FormADate = null }
        });
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(ValidCaseRecord() with { IsNonGbCase = true });
        _farmRepository.GetByCphhAsync(Cphh).Returns(ValidFarmRecord());
        _caseRepository.EditCaseAsync(Arg.Any<EditCaseCommand>(), 1, Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(EditCaseResult.Success);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAllAsync_WhenFarmCphhIsNonGb_SkipsParishAhoAndAdnsRegionChecks()
    {
        // A brand-new, not-yet-created farm has no stored IsNonGBFarm flag — the farm-half check must
        // derive it from the CPHH prefix ("00") instead, exactly like Farm.cshtml.cs's own helper.
        const string nonGbCphh = "00123456789";
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = true,
            Farm = new UpdateFarmCommand(
                CPHH: nonGbCphh, OwnerName: "New Owner", Address1: "1 Farm Lane", Address2: null, Address3: null,
                Postcode: null, Parish: null, District: null, County: "Some County",
                CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
                CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
                Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: null,
                HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: null, RowStamp: []),
            Case = ValidEditCase()
        });
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _farmRepository.GetByCphhAsync(nonGbCphh).Returns((FarmRecord?)null);
        _caseRepository.AddCaseAsync(Arg.Any<AddCaseCommand>(), Arg.Any<int>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(AddCaseResult.Success);
        ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        // Parish/AHO/ADNSRegionID are all blank above — if the non-GB derivation didn't fire, this
        // would throw MandatoryCaseFieldsMissingException instead of succeeding.
        outcome.Result.Should().Be(EditCaseResult.Success);
    }

    [Fact]
    public async Task CommitAllAsync_WhenFarmDoesNotExistForAnExistingCase_ThrowsNoFarmSpecified()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = true,
            Case = ValidEditCase()
        });
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(ValidCaseRecord());
        _farmRepository.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        var sut = CreateService();

        var act = () => sut.CommitAllAsync(Rbse, userId: 1);

        var thrown = await act.Should().ThrowAsync<MandatoryCaseFieldsMissingException>();
        thrown.Which.Errors.Should().BeEquivalentTo(["No farm has been specified for the case."]);
    }

    // ── PersistStagedFeedsAsync edit-path coverage ──────────────────────────────
    // Existing tests cover a brand-new feed and an unchanged feed; the "existing feed whose fields
    // changed" branch (the EditAsync call itself, both success and stale-RowStamp warning) was untested.

    [Fact]
    public async Task CommitAllAsync_WhenAStagedFeedIsChanged_UpdatesItWithoutWarning()
    {
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState
        {
            Rbse = Rbse,
            Feeds = [new CaseFeedsDraftItem { Id = 7, YearFrom = 1995, YearTo = 1996, RationType = "C", RationName = "Ration B (renamed)", IsPrePurchase = false, SupplierId = null }],
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _feedRepository.GetByRbseAsync(Rbse).Returns([
            new CaseFeedRecord { Id = 7, Rbse = Rbse, YearFrom = 1995, YearTo = 1996, RationType = "C", RationName = "Ration A", IsPrePurchase = false, SupplierId = null, RowStamp = [9, 9, 9] }
        ]);
        _feedRepository.EditAsync(Arg.Any<EditFeedCommand>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(1);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Warnings.Should().BeEmpty();
        await _feedRepository.Received(1).EditAsync(
            Arg.Is<EditFeedCommand>(f => f.Id == 7 && f.RationName == "Ration B (renamed)"),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAllAsync_WhenAStagedFeedEditHitsAStaleRowStamp_WarnsAndDoesNotRollBack()
    {
        _feedsDraftState.GetAsync(Rbse).Returns(new CaseFeedsDraftState
        {
            Rbse = Rbse,
            Feeds = [new CaseFeedsDraftItem { Id = 7, YearFrom = 1995, YearTo = 1996, RationType = "C", RationName = "Ration B (renamed)", IsPrePurchase = false, SupplierId = null }],
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _feedRepository.GetByRbseAsync(Rbse).Returns([
            new CaseFeedRecord { Id = 7, Rbse = Rbse, YearFrom = 1995, YearTo = 1996, RationType = "C", RationName = "Ration A", IsPrePurchase = false, SupplierId = null, RowStamp = [9, 9, 9] }
        ]);
        _feedRepository.EditAsync(Arg.Any<EditFeedCommand>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(0);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Warnings.Should().ContainSingle()
            .Which.Should().Be("Failed to update feed with Ration Name \"Ration B (renamed)\" - Data was changed by another user");
        transaction.Received(1).Commit();
    }

    // ── PersistStagedRelationsAsync coverage ────────────────────────────────────
    // Existing tests cover only the "removed relation" delete path. The new-relation insert and the
    // changed-existing-relation edit (success and stale-RowStamp warning) paths were untested.

    [Fact]
    public async Task CommitAllAsync_WhenAStagedRelationIsNew_InsertsItWithoutWarnings()
    {
        _relationsDraftState.GetAsync(Rbse).Returns(new CaseRelationsDraftState
        {
            Rbse = Rbse,
            Relations = [new CaseRelationsDraftItem { Id = null, RelationType = "O", RelationRbse = "008900184", Sex = "M" }],
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _relationsRepository.GetRelationsByRbseAsync(Rbse).Returns([]);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Warnings.Should().BeEmpty();
        await _relationsRepository.Received(1).AddRelationAsync(
            Arg.Is<BSE.Modules.AnimalRelations.Commands.AddCaseRelationCommand>(r => r.Rbse == Rbse && r.RelationType == "O" && r.RelationRbse == "008900184"),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAllAsync_WhenAStagedRelationIsChanged_UpdatesItWithoutWarning()
    {
        _relationsDraftState.GetAsync(Rbse).Returns(new CaseRelationsDraftState
        {
            Rbse = Rbse,
            Relations = [new CaseRelationsDraftItem { Id = 3, RelationType = "O", RelationRbse = "008900184", Sex = "F" }],
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _relationsRepository.GetRelationsByRbseAsync(Rbse).Returns([
            new BSE.Modules.AnimalRelations.Models.CaseRelationRecord
            {
                Id = 3, Rbse = Rbse, RelationType = "O", RelationRbse = "008900184", Sex = "M", RowStamp = [8, 8, 8]
            }
        ]);
        _relationsRepository.EditRelationAsync(
                Arg.Any<BSE.Modules.AnimalRelations.Commands.EditCaseRelationCommand>(),
                Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(1);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Warnings.Should().BeEmpty();
        await _relationsRepository.Received(1).EditRelationAsync(
            Arg.Is<BSE.Modules.AnimalRelations.Commands.EditCaseRelationCommand>(r => r.Id == 3 && r.Sex == "F"),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAllAsync_WhenAStagedRelationEditHitsAStaleRowStamp_WarnsAndDoesNotRollBack()
    {
        _relationsDraftState.GetAsync(Rbse).Returns(new CaseRelationsDraftState
        {
            Rbse = Rbse,
            Relations = [new CaseRelationsDraftItem { Id = 3, RelationType = "O", RelationRbse = "008900184", Sex = "F" }],
            HasPendingChanges = true
        });
        ArrangeExistingValidCase();
        _relationsRepository.GetRelationsByRbseAsync(Rbse).Returns([
            new BSE.Modules.AnimalRelations.Models.CaseRelationRecord
            {
                Id = 3, Rbse = Rbse, RelationType = "O", RelationRbse = "008900184", Sex = "M", RowStamp = [8, 8, 8]
            }
        ]);
        _relationsRepository.EditRelationAsync(
                Arg.Any<BSE.Modules.AnimalRelations.Commands.EditCaseRelationCommand>(),
                Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(0);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Warnings.Should().ContainSingle()
            .Which.Should().Be("Failed to update relation with RBSE \"008900184\" - Data was changed by another user");
        transaction.Received(1).Commit();
    }

    // ── New-case Bab/Clinical creation coverage ─────────────────────────────────
    // CommitAllAsync_WhenCaseDoesNotExistYet_CreatesFarmAndCaseInOneTransaction only stages a Case +
    // Farm; the branches that also add Bab/Clinical rows for a brand-new case were untested.

    [Fact]
    public async Task CommitAllAsync_WhenNewCaseHasBabAndClinicalStaged_AddsBothAndTheCaseWorkRow()
    {
        _scalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = true,
            Farm = new UpdateFarmCommand(
                CPHH: Cphh, OwnerName: "New Owner", Address1: "1 Farm Lane", Address2: null, Address3: null,
                Postcode: null, Parish: "Some Parish", District: null, County: "Some County",
                CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
                CorrespondencePostcode: null, MapReference: null, Herdmark1: null, Herdmark2: null,
                Herdmark3: null, NumericHerdmark1: null, NumericHerdmark2: null, AHO: "Some AHO",
                HerdType: null, PedigreeType: null, IsDealer: false, ADNSRegionID: 5, RowStamp: []),
            Case = ValidEditCase(),
            Bab = new EditCaseBabCommand(Rbse, null, "notes", null, null, null, null, null, null, null, null, [1]),
            Clinical = new EditCaseClinicalCommand(Rbse,
                false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false, false, [1])
        });
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _farmRepository.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);
        _caseRepository.AddCaseAsync(Arg.Any<AddCaseCommand>(), Arg.Any<int>(), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>())
            .Returns(AddCaseResult.Success);
        var transaction = ArrangeConnection();
        var sut = CreateService();

        var outcome = await sut.CommitAllAsync(Rbse, userId: 1);

        outcome.Result.Should().Be(EditCaseResult.Success);
        await _caseWorkRepository.Received(1).AddAsync(
            Arg.Is<BSE.Modules.CaseWork.Commands.AddCaseWorkCommand>(c => c.Rbse == Rbse),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        await _babRepository.Received(1).AddAsync(
            Arg.Is<AddCaseBabCommand>(b => b.Rbse == Rbse && b.Notes == "notes"), Arg.Any<string?>(),
            Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        await _clinicalRepository.Received(1).AddAsync(
            Arg.Is<AddCaseClinicalCommand>(c => c.Rbse == Rbse), Arg.Any<IDbConnection>(), Arg.Any<IDbTransaction>());
        transaction.Received(1).Commit();
    }
}
