using System.Data;
using System.Text.Json;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using CaseRelationRecord = BSE.Modules.AnimalRelations.Models.CaseRelationRecord;
using DamSireDetailRecord = BSE.Modules.AnimalRelations.Models.DamSireDetailRecord;

namespace BSE.Modules.UserManagement.Tests;

public sealed class RelationsModelFlowTests
{
    private const string Rbse = "002600001";
    private static readonly string[] Editor = ["DataEntry", "VLAAccess"];

    private readonly IAnimalRelationsRepository _relations = Substitute.For<IAnimalRelationsRepository>();
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly InMemorySessionState _session = new();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public RelationsModelFlowTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(new[] { new LookupItem(1, "Offspring", "Offspring") });
        _lookups.GetSexesAsync().Returns(new[] { new LuSex { Code = "F", Description = "Female" } });
        _lookups.GetAnimalStatusesAsync().Returns(new[] { new LuAnimalStatus { Id = 1, Code = "A", Description = "Alive" } });
        _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _relations.GetRelationsDetailsByRbseAsync(Arg.Any<string>())
            .Returns(new RelationDetailsRecord(null, null, Array.Empty<CaseRelationRecord>()));
        _relations.GetDamSireDetailsMatchesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>())
            .Returns(Array.Empty<DamSireDetailRecord>());
        _caseService.GetCaseAsync(Arg.Any<string>()).Returns(new CaseRecord { Rbse = Rbse, Herdbook = "HB", DamStatus = "A" });
        _orchestration.CommitAllAsync(Rbse, 3).Returns(CaseCommitOutcome.Success([]));
        _user.GetUserIdAsync().Returns(3);

        var conn = Substitute.For<IDbConnection>();
        _factory.CreateConnection().Returns(conn);
    }

    private RelationsModel Create(params string[] roles)
    {
        var model = new RelationsModel(_relations, _caseService, _lookups, _batches, _session,
            Substitute.For<ICaseDraftDiscardService>(), _orchestration, _factory, _user,
            NullLogger<RelationsModel>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "https://s" }).Build())
        { Rbse = Rbse };
        PageTestHelpers.Attach(model, roles);
        return model;
    }

    private static DamSireDetailRecord Parent(int id, byte[]? rowStamp = null, string? rbse = "002700001") =>
        new() { Id = id, Rbse = rbse, Eartag = "E", Name = "N", Herdbook = "H", BirthYear = 2010, RowStamp = rowStamp };

    // â”€â”€ GET: pending parent hand-back from PickSireDam â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var model = Create(Editor);
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_PendingDamAndSireInTempData_AppliedAndPersistedOnDraft()
    {
        _session.Current = new CaseEditSessionState { Rbse = Rbse, RemoveDamPending = true, RemoveSirePending = true, HasRelationsChanges = true };
        var model = Create(Editor);
        model.TempData[PendingDamSireKeys.Dam] = JsonSerializer.Serialize(
            new PendingDamSire(7, "002700001", "E1", "Dam", "HB", 1, 2, 2010, "AQ==", "Dead", "Pos", 3));
        model.TempData[PendingDamSireKeys.Sire] = JsonSerializer.Serialize(
            new PendingDamSire(8, null, "E2", "Sire", "HB", null, null, 2008, "Ag==", "Dead", null, 1));

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.DamSire.HasDam.Should().BeTrue();
        model.DamSire.DamId.Should().Be(7);
        model.DamSire.DamChildCount.Should().Be(3);
        model.DamSire.SireId.Should().Be(8);
        _session.Current!.PendingDam!.Id.Should().Be(7);
        _session.Current.PendingSire!.Id.Should().Be(8);
        _session.Current.RemoveDamPending.Should().BeFalse();
        _session.Current.RemoveSirePending.Should().BeFalse();
        _session.Current.HasRelationsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_PendingParentSameAsCaseRbse_SetsError()
    {
        var model = Create(Editor);
        model.TempData[PendingDamSireKeys.Dam] = JsonSerializer.Serialize(
            new PendingDamSire(7, Rbse, null, null, null, null, null, null, null, null, null, null));

        await model.OnGetAsync();

        model.DamError.Should().Be(RelationsModel.SameAsCaseRbse);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData(" ")]
    public async Task OnGet_UnusablePendingJson_IsIgnored(string json)
    {
        var model = Create(Editor);
        model.TempData[PendingDamSireKeys.Sire] = json;

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();
        model.DamSire.HasSire.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_StagedPendingParentsAndPersistedParents_Hydrate()
    {
        _relations.GetRelationsDetailsByRbseAsync(Rbse).Returns(new RelationDetailsRecord(
            Parent(1), Parent(2, rbse: "002700002"), Array.Empty<CaseRelationRecord>()));
        var model = Create(Editor);

        await model.OnGetAsync();

        model.DamSire.HasDam.Should().BeTrue();
        model.DamSire.DamId.Should().Be(1);
        model.DamSire.SireId.Should().Be(2);
        model.DamStatusDescription.Should().Be("Alive");
        model.SpolSiteUrl.Should().Be("https://s");
    }

    [Fact]
    public async Task OnGet_DraftWithPendingAndRemovals_OverridesPersistedParents()
    {
        _relations.GetRelationsDetailsByRbseAsync(Rbse).Returns(new RelationDetailsRecord(
            Parent(1), Parent(2), Array.Empty<CaseRelationRecord>()));
        _session.Current = new CaseEditSessionState
        {
            Rbse = Rbse,
            PendingDam = new PendingParentDraft { Id = 11, Rbse = "002700011", Name = "New dam", RowStampBase64 = "AQ==" },
            RemoveSirePending = true,
            HasRelationsChanges = true
        };
        var model = Create(Editor);

        await model.OnGetAsync();

        model.DamSire.DamId.Should().Be(11);
        model.DamSire.SireId.Should().Be(0);
        model.DamSire.HasSire.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_RemovedDamInDraft_ClearsDam()
    {
        _relations.GetRelationsDetailsByRbseAsync(Rbse).Returns(new RelationDetailsRecord(
            Parent(1), null, Array.Empty<CaseRelationRecord>()));
        _session.Current = new CaseEditSessionState { Rbse = Rbse, RemoveDamPending = true, HasRelationsChanges = true };
        var model = Create(Editor);

        await model.OnGetAsync();

        model.DamSire.HasDam.Should().BeFalse();
        model.DamSire.DamId.Should().Be(0);
    }

    [Fact]
    public async Task OnGet_StagedCaseAndDamSire_OverlayDamStatusAndHerdbook()
    {
        _session.Current = new CaseEditSessionState
        {
            Rbse = Rbse,
            Case = new BSE.Host.Models.ViewModels.CaseEditViewModel { Rbse = Rbse, DamStatus = "X" }.ToEditCommand([]),
            DamSire = new BSE.Modules.CaseManagement.Commands.AddEditDamSireCommand(
                Rbse, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                "StagedHB", null)
        };
        var model = Create(Editor);

        await model.OnGetAsync();

        model.DamSire.DamStatus.Should().Be("X");
        model.CaseHerdbook.Should().Be("StagedHB");
    }

    // â”€â”€ POST: Save validation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Save_DamAndSireRbseSameAsCase_Rejected()
    {
        var dam = Create("DataEntry");
        dam.DamSire.DamRbse = Rbse;
        (await dam.OnPostSaveRelationsAsync()).Should().BeOfType<PageResult>();
        dam.DamError.Should().Be(RelationsModel.SameAsCaseRbse);

        var sire = Create("DataEntry");
        sire.DamSire.SireRbse = Rbse;
        (await sire.OnPostSaveRelationsAsync()).Should().BeOfType<PageResult>();
        sire.SireError.Should().Be(RelationsModel.SameAsCaseRbse);
    }

    [Theory]
    [InlineData(32, null, 2000, "valid day")]
    [InlineData(null, 13, 2000, "month between 1 and 12")]
    [InlineData(null, null, 999, "four digit year")]
    [InlineData(1, null, 2000, "remove the day")]
    [InlineData(30, 2, 2000, "valid date")]
    public async Task Save_InvalidPartialBirthDates_RejectedForDamAndSire(int? day, int? month, int? year, string fragment)
    {
        var dam = Create("DataEntry");
        dam.DamSire.DamBirthDay = day; dam.DamSire.DamBirthMonth = month; dam.DamSire.DamBirthYear = year;
        (await dam.OnPostSaveRelationsAsync()).Should().BeOfType<PageResult>();
        dam.DamError.Should().Contain(fragment);

        var sire = Create("DataEntry");
        sire.DamSire.SireBirthDay = day; sire.DamSire.SireBirthMonth = month; sire.DamSire.SireBirthYear = year;
        (await sire.OnPostSaveRelationsAsync()).Should().BeOfType<PageResult>();
        sire.SireError.Should().Contain(fragment);
    }

    [Fact]
    public async Task Save_ParentWithIdButNoRowStampOrRbse_AsksToLookUpAgain()
    {
        var dam = Create("DataEntry");
        dam.DamSire.HasDam = true; dam.DamSire.DamId = 4;
        (await dam.OnPostSaveRelationsAsync()).Should().BeOfType<PageResult>();
        dam.DamError.Should().Contain("could not be refreshed");

        var sire = Create("DataEntry");
        sire.DamSire.HasSire = true; sire.DamSire.SireId = 5;
        (await sire.OnPostSaveRelationsAsync()).Should().BeOfType<PageResult>();
        sire.SireError.Should().Contain("could not be refreshed");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Save_RbseMatchesSeveralParents_AsksForLookUp(bool isDam)
    {
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700001", null, isDam ? "F" : "M")
            .Returns(new[] { Parent(1), Parent(2) });
        var model = Create("DataEntry");
        if (isDam) model.DamSire.DamRbse = "002700001"; else model.DamSire.SireRbse = "002700001";

        (await model.OnPostSaveRelationsAsync()).Should().BeOfType<PageResult>();

        (isDam ? model.DamError : model.SireError).Should().Contain("Multiple");
    }

    [Fact]
    public async Task Save_RbseMatchesNoParent_ContinuesWithoutLinking()
    {
        var model = Create("DataEntry");
        model.DamSire.DamRbse = "002700001";

        (await model.OnPostSaveRelationsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _session.Current!.DamSire!.DamId.Should().Be(0);
    }

    [Fact]
    public async Task Save_PendingRemovals_ClearPostedParents()
    {
        _session.Current = new CaseEditSessionState { Rbse = Rbse, RemoveDamPending = true, RemoveSirePending = true, HasRelationsChanges = true };
        var model = Create("DataEntry");

        (await model.OnPostSaveRelationsAsync()).Should().BeOfType<RedirectToPageResult>();

        _session.Current!.DamSire!.DamId.Should().BeNull();
        _session.Current.DamSire.SireId.Should().BeNull();
    }

    [Fact]
    public async Task Save_CaseNotYetPersisted_StagesDamStatusOnBlankCommand()
    {
        _caseService.GetCaseAsync(Arg.Any<string>()).Returns((CaseRecord?)null);
        var model = Create("DataEntry");
        model.DamSire.DamStatus = "A";

        (await model.OnPostSaveRelationsAsync()).Should().BeOfType<RedirectToPageResult>();

        _session.Current!.Case!.DamStatus.Should().Be("A");
    }

    [Fact]
    public async Task Save_WithExistingStagedCase_MergesDamStatus()
    {
        _session.Current = new CaseEditSessionState
        {
            Rbse = Rbse,
            Case = new BSE.Host.Models.ViewModels.CaseEditViewModel { Rbse = Rbse, Notes = "keep" }.ToEditCommand([])
        };
        var model = Create("DataEntry");
        model.DamSire.DamStatus = "A";

        await model.OnPostSaveRelationsAsync();

        _session.Current!.Case!.Notes.Should().Be("keep");
        _session.Current.Case.DamStatus.Should().Be("A");
    }

    // â”€â”€ Relation panel derived fields â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task AddRelationRow_RelatedCaseNotFound_ReportsError()
    {
        _relations.GetRelationDetailsOfRelatedCaseAsync("002700005").Returns((RelatedCaseDetailsRecord?)null);
        var model = Create(Editor);
        model.RelationType = "Offspring";
        model.RelationRbse = "002700005";

        (await model.OnPostAddRelationRowAsync()).Should().BeOfType<PageResult>();

        model.RelationFieldErrors.Should().ContainKey("RelationRbse");
    }

    [Fact]
    public async Task AddRelationRow_RelatedCaseWithFullDetails_CopiesThemAndStagesRow()
    {
        _relations.GetRelationDetailsOfRelatedCaseAsync("002700005").Returns(new RelatedCaseDetailsRecord
        {
            RelationRbse = "002700005", Sex = "F", Fate = "D", FateDesc = "Dead", EartagCountry = "UK", EartagHerdmark = "AB",
            Eartag = "123", BirthDay = 3, BirthMonth = 4, BirthYear = 2015, LeftDate = "05/06/2018", Name = "Bull"
        });
        var model = Create(Editor);
        model.RelationType = "Offspring";
        model.RelationRbse = "002700005";

        var result = await model.OnPostAddRelationRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _session.Current!.Relations.Should().ContainSingle(r => r.RelationRbse == "002700005" && r.Eartag == "123" && r.Sire == "Bull");
    }

    [Fact]
    public async Task AddRelationRow_RelatedCaseWithoutBirthDateOrLeftDate_StillStages()
    {
        _relations.GetRelationDetailsOfRelatedCaseAsync("002700005").Returns(new RelatedCaseDetailsRecord
        {
            RelationRbse = "002700005", Sex = "M", LeftDate = "not a date"
        });
        var model = Create(Editor);
        model.RelationType = "Offspring";
        model.RelationRbse = "002700005";

        (await model.OnPostAddRelationRowAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnGetRelationDetails_WithoutFullBirthDate_ReturnsNullBirthDate()
    {
        _relations.GetRelationDetailsOfRelatedCaseAsync("002700005").Returns(new RelatedCaseDetailsRecord
        {
            RelationRbse = "002700005", BirthYear = 2015
        });

        var json = (await Create(Editor).OnGetRelationDetailsAsync("002700005")).Should().BeOfType<JsonResult>().Subject;

        json.Value!.GetType().GetProperty("birthDate")!.GetValue(json.Value).Should().BeNull();
    }
}
