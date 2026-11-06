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
using BSE.Modules.UserManagement.Tests.Db;
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

/// <summary>Covers the Dam/Sire linking paths of <see cref="RelationsModel"/>: Look Up rejections,
/// staged-parent hydration after a failed Look Up, and resolving / refreshing linked parents on Save.</summary>
public sealed class RelationsModelParentLinkTests
{
    private const string Rbse = "002600001";
    private const string ParentRbse = "002700001";
    private static readonly string[] Editor = ["DataEntry", "VLAAccess"];

    private readonly IAnimalRelationsRepository _relations = Substitute.For<IAnimalRelationsRepository>();
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly InMemorySessionState _session = new();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly FakeDbConnection _db = new();

    public RelationsModelParentLinkTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetSexesAsync().Returns(Array.Empty<LuSex>());
        _lookups.GetAnimalStatusesAsync().Returns(Array.Empty<LuAnimalStatus>());
        _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _relations.GetRelationsDetailsByRbseAsync(Arg.Any<string>())
            .Returns(new RelationDetailsRecord(null, null, Array.Empty<CaseRelationRecord>()));
        _relations.GetDamSireDetailsMatchesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>())
            .Returns(Array.Empty<DamSireDetailRecord>());
        _caseService.GetCaseAsync(Arg.Any<string>()).Returns(new CaseRecord { Rbse = Rbse, Herdbook = "HB", DamStatus = "A" });
        _orchestration.CommitAllAsync(Rbse, 3).Returns(CaseCommitOutcome.Success([]));
        _user.GetUserIdAsync().Returns(3);
        _factory.CreateConnection().Returns(_db);
    }

    private RelationsModel Create(params string[] roles)
    {
        var model = new RelationsModel(_relations, _caseService, _lookups, _batches, _session,
            Substitute.For<ICaseDraftDiscardService>(), _orchestration, _factory, _user,
            NullLogger<RelationsModel>.Instance, new ConfigurationBuilder().AddInMemoryCollection().Build())
        { Rbse = Rbse };
        PageTestHelpers.Attach(model, roles);
        return model;
    }

    private static DamSireDetailRecord Parent(int id, string rbse = ParentRbse, byte[]? rowStamp = null) =>
        new() { Id = id, Rbse = rbse, Eartag = "E", Name = "N", Herdbook = "H", BirthYear = 2010, RowStamp = rowStamp };

    private static System.Data.DataTable Snapshot() => FakeDbConnection.Table(new
    {
        Eartag = "SNAP", Name = "Snapshot", Herdbook = "SH", BirthDay = (int?)1, BirthMonth = (int?)2, BirthYear = (int?)2000,
        RowStamp = new byte[] { 9 }
    });

    [Fact]
    public async Task LookUpDam_WhenTheRbseIsAlreadyTheSire_ReportsAlreadyARelation()
    {
        _relations.GetRelationsDetailsByRbseAsync(Rbse).Returns(new RelationDetailsRecord(
            null, Parent(2, "002700002", [1]), Array.Empty<CaseRelationRecord>()));
        var model = Create(Editor);
        model.DamSire.DamSearchRbse = "002700002";

        (await model.OnPostLookUpDamAsync()).Should().BeOfType<PageResult>();

        model.DamError.Should().Be(RelationsModel.AlreadyARelation);
    }

    [Fact]
    public async Task LookUpSire_WhenTheRbseIsAStagedRelation_ReportsAlreadyARelationAndKeepsTheDam()
    {
        _relations.GetRelationsDetailsByRbseAsync(Rbse).Returns(new RelationDetailsRecord(
            Parent(1, rowStamp: [1]), null,
            (IReadOnlyList<CaseRelationRecord>)new[] { new CaseRelationRecord { Id = 5, RelationType = "Twin", RelationRbse = "002700011", RowStamp = [1] } }));
        var model = Create(Editor);
        model.DamSire.SireSearchRbse = "002700011";

        (await model.OnPostLookUpSireAsync()).Should().BeOfType<PageResult>();

        model.SireError.Should().Be(RelationsModel.AlreadyARelation);
        model.DamSire.DamId.Should().Be(1);
    }

    [Fact]
    public async Task LookUpDam_WhenNotFound_KeepsTheStagedSire()
    {
        _session.Current = new CaseEditSessionState
        {
            Rbse = Rbse,
            PendingSire = new PendingParentDraft
            {
                Id = 8, Rbse = "002700008", Eartag = "E8", Name = "Sire", Herdbook = "H", BirthYear = 2008, RowStampBase64 = "AQ==", ChildCount = 1
            }
        };
        var model = Create(Editor);
        model.DamSire.DamSearchRbse = "002700003";

        (await model.OnPostLookUpDamAsync()).Should().BeOfType<PageResult>();

        model.DamError.Should().Be(RelationsModel.DamNotFound);
        model.DamSire.HasSire.Should().BeTrue();
        model.DamSire.SireId.Should().Be(8);
        model.DamSire.SireRbse.Should().Be("002700008");
    }

    [Fact]
    public async Task Save_WhenTheRbsesMatchASingleParentEach_LinksThemAndRefreshesTheirPedigreeDetails()
    {
        _relations.GetDamSireDetailsMatchesAsync(null, null, ParentRbse, null, "F").Returns(new[] { Parent(4, rowStamp: [1]) });
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700002", null, "M").Returns(new[] { Parent(5, "002700002", [2]) });
        _db.Enqueue(Snapshot());
        _db.Enqueue(Snapshot());
        var model = Create(Editor);
        model.DamSire.DamRbse = ParentRbse;
        model.DamSire.DamBirthDay = 1;
        model.DamSire.DamBirthMonth = 2;
        model.DamSire.DamBirthYear = 2000;
        model.DamSire.SireRbse = "002700002";

        (await model.OnPostSaveRelationsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");

        model.DamSire.DamId.Should().Be(4);
        model.DamSire.DamEartag.Should().Be("SNAP");
        model.DamSire.DamRowStamp.Should().Be(Convert.ToBase64String([9]));
        model.DamSire.SireId.Should().Be(5);
        model.DamSire.SireName.Should().Be("Snapshot");
        model.DamSire.SireBirthYear.Should().Be(2000);
    }

    [Fact]
    public async Task Save_WhenALinkedParentIsMissingItsRowStamp_ReloadsItFromTheLookUp()
    {
        _relations.GetDamSireDetailsMatchesAsync(null, null, ParentRbse, null, "F")
            .Returns(Array.Empty<DamSireDetailRecord>(), new[] { Parent(4, rowStamp: [1]) });
        _relations.GetDamSireDetailsMatchesAsync(null, null, "002700002", null, "M")
            .Returns(Array.Empty<DamSireDetailRecord>(), new[] { Parent(5, "002700002", [2]) });
        var model = Create(Editor);
        model.DamSire.HasDam = true;
        model.DamSire.DamId = 4;
        model.DamSire.DamRbse = ParentRbse;
        model.DamSire.HasSire = true;
        model.DamSire.SireId = 5;
        model.DamSire.SireRbse = "002700002";

        (await model.OnPostSaveRelationsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");

        model.DamSire.DamRowStamp.Should().Be(Convert.ToBase64String([1]));
        model.DamSire.SireRowStamp.Should().Be(Convert.ToBase64String([2]));
    }
}
