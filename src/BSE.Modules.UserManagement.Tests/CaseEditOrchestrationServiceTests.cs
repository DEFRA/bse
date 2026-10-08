using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Repositories;
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
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();

    private CaseEditOrchestrationService CreateService() => new(
        _scalarDraftState, _feedsDraftState, _relationsDraftState, _wizardState,
        _caseRepository, _farmRepository, _babRepository, _clinicalRepository,
        _feedRepository, _relationsRepository, _pedigreeRepository, _batchRepository, _connectionFactory,
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
}
