using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Enums;
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

namespace BSE.Modules.UserManagement.Tests;

public sealed class RelationsModelCoverageTests
{
    private const string Rbse = "002600001";
    private static readonly string[] Editor = ["DataEntry", "VLAAccess"];
    private static readonly string[] DataEntryOnly = ["DataEntry"];

    private readonly IAnimalRelationsRepository _relations = Substitute.For<IAnimalRelationsRepository>();
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly InMemorySessionState _sessionState = new();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public RelationsModelCoverageTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(new[] { new LookupItem(1, "Offspring", "Offspring"), new LookupItem(2, "Twin", "Twin") });
        _lookups.GetSexesAsync().Returns(new[] { new LuSex { Code = "F", Description = "Female" } });
        _lookups.GetAnimalStatusesAsync().Returns(Array.Empty<LuAnimalStatus>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _relations.GetRelationsDetailsByRbseAsync(Arg.Any<string>()).Returns(new RelationDetailsRecord(null, null, (IReadOnlyList<CaseRelationRecord>)new[]
        {
            new CaseRelationRecord { Id = 1, RelationType = "Offspring", RelationRbse = "002700011", Sex = "F", RowStamp = [1] },
            new CaseRelationRecord { Id = 2, RelationType = "Twin", RelationRbse = "002700012", Sex = "M", RowStamp = [2] },
        }));
        _caseService.GetCaseAsync(Arg.Any<string>()).Returns(new CaseRecord { Rbse = Rbse, Herdbook = "HB1", DamStatus = "A" });
        _currentUser.GetUserIdAsync().Returns(3);
    }

    private RelationsModel Create(params string[] roles)
    {
        var model = new RelationsModel(_relations, _caseService, _lookups, _batchRepository, _sessionState, _discard,
            _orchestration, _connectionFactory, _currentUser, NullLogger<RelationsModel>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection().Build())
        {
            Rbse = Rbse
        };
        PageTestHelpers.Attach(model, roles);
        return model;
    }

    [Fact]
    public async Task OnGetAsync_SeedsTheDraftAndPaginatesRelations()
    {
        var model = Create(Editor);

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.TotalRelationsCount.Should().Be(2);
        model.PagedRelations.Should().HaveCount(2);
        _sessionState.Current!.Relations.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("RelationType", false)]
    [InlineData("RelationType", true)]
    [InlineData("RelationRbse", false)]
    [InlineData("Sex", true)]
    [InlineData("BirthDate", false)]
    [InlineData("Fate", true)]
    [InlineData("RelationFate", false)]
    [InlineData("LeftDate", false)]
    [InlineData("Eartag", true)]
    [InlineData("Sire", false)]
    [InlineData("nothing", false)]
    public async Task OnGetAsync_SortsTheGrid(string column, bool desc)
    {
        var model = Create(Editor);
        model.SortColumn = column;
        model.SortDesc = desc;
        model.PageNumber = 5;

        await model.OnGetAsync();

        model.PagedRelations.Should().HaveCount(2);
        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGetEditCaseHerdbookAsync_ReturnsThePage() =>
        (await Create(DataEntryOnly).OnGetEditCaseHerdbookAsync()).Should().BeOfType<PageResult>();

    [Fact]
    public async Task RowHandlers_AreForbiddenWithoutTheVlaEditorRoles()
    {
        var model = Create(DataEntryOnly);

        (await model.OnPostAddRelationRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostBeginEditRelationRowAsync("k")).Should().BeOfType<ForbidResult>();
        (await model.OnPostUpdateRelationRowAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostDeleteRelationRowAsync("k")).Should().BeOfType<ForbidResult>();
        (await model.OnPostRemoveSireAsync()).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DataEntryHandlers_AreForbiddenForViewers()
    {
        var model = Create();

        (await model.OnPostSaveDamSireAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostSaveCaseHerdbookAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostSaveRelationsAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostRemoveDamAsync()).Should().BeOfType<ForbidResult>();
        (await model.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<RedirectToPageResult>();
        (await model.OnPostResortRelationsAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task AddUpdateAndDeleteRows_ChangeTheStagedDraft()
    {
        var add = Create(Editor);
        add.RelationType = "Twin";
        add.Sex = "F";
        var addResult = await add.OnPostAddRelationRowAsync();
        add.RelationFieldErrors.Should().BeEmpty();
        addResult.Should().BeOfType<RedirectToPageResult>();
        var staged = _sessionState.Current!.Relations;
        staged.Should().HaveCount(3);
        _sessionState.Current.HasRelationsChanges.Should().BeTrue();

        var key = staged.First(r => r.Id == 1).ClientKey;
        var begin = Create(Editor);
        (await begin.OnPostBeginEditRelationRowAsync(key)).Should().BeOfType<PageResult>();
        begin.RelationType.Should().Be("Offspring");
        begin.EditingClientKey.Should().Be(key);

        var update = Create(Editor);
        update.EditingClientKey = key;
        update.RelationType = "Twin";
        update.Sex = "F";
        (await update.OnPostUpdateRelationRowAsync()).Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.Relations.First(r => r.Id == 1).RelationType.Should().Be("Twin");

        var delete = Create(Editor);
        (await delete.OnPostDeleteRelationRowAsync(key)).Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.Relations.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddRow_WithARelatedRbse_ValidatesAgainstTheOtherStagedRelations()
    {
        var model = Create(Editor);
        model.RelationType = "Twin";
        model.RelationRbse = "002700011";

        (await model.OnPostAddRelationRowAsync()).Should().BeOfType<PageResult>();

        model.RelationFieldErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task StagedRelationRows_ReportWhetherTheyAreUnsaved()
    {
        var add = Create(Editor);
        add.RelationType = "Twin";
        add.Sex = "F";
        await add.OnPostAddRelationRowAsync();

        var model = Create(Editor);
        await model.OnGetAsync();

        model.PagedRelations.Should().ContainSingle(r => r.IsUnsaved);
        model.PagedRelations.Should().Contain(r => !r.IsUnsaved);
    }

    [Fact]
    public async Task UpdateRow_ReportsAMissingRowAndKeepsTheEditorOpenOnErrors()
    {
        await Create(Editor).OnGetAsync();

        var missing = Create(Editor);
        missing.EditingClientKey = "nope";
        (await missing.OnPostUpdateRelationRowAsync()).Should().BeOfType<RedirectToPageResult>();
        missing.TempData["ErrorMessage"].Should().NotBeNull();

        var invalid = Create(Editor);
        invalid.EditingClientKey = _sessionState.Current!.Relations[0].ClientKey;
        (await invalid.OnPostUpdateRelationRowAsync()).Should().BeOfType<PageResult>();
        invalid.ReopenEditClientKey.Should().Be(invalid.EditingClientKey);
    }

    [Fact]
    public async Task BeginEdit_WithAnUnknownKey_JustRedisplaysThePage() =>
        (await Create(Editor).OnPostBeginEditRelationRowAsync("nope")).Should().BeOfType<PageResult>();

    [Fact]
    public async Task SaveDamSireAndCaseHerdbook_MarkTheDraftPending()
    {
        (await Create(DataEntryOnly).OnPostSaveDamSireAsync()).Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.HasRelationsChanges.Should().BeTrue();

        _sessionState.Current = null;
        var herdbook = Create(DataEntryOnly);
        herdbook.CaseHerdbook = "NEWHB";
        (await herdbook.OnPostSaveCaseHerdbookAsync()).Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.HasRelationsChanges.Should().BeTrue();
    }

    [Fact]
    public async Task StageAndGoto_OnlyStagesWhenTheHerdbookOrDamStatusChanged()
    {
        var unchanged = Create(DataEntryOnly);
        unchanged.CaseHerdbook = "HB1";
        unchanged.DamSire.DamStatus = "A";
        (await unchanged.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current.Should().BeNull();

        var changed = Create(DataEntryOnly);
        changed.CaseHerdbook = "CHANGED";
        changed.DamSire.DamStatus = "A";
        (await changed.OnPostStageAndGotoAsync("/Case/Farm")).Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current.Should().NotBeNull();
    }

    [Fact]
    public async Task ResortRelations_ReturnsThePageForEditors()
    {
        var model = Create(DataEntryOnly);
        model.CaseHerdbook = "HB1";

        (await model.OnPostResortRelationsAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public void LegacyDeleteHandler_JustRedirects() =>
        Create(DataEntryOnly).OnPostDeleteRelationAsync(1, "x").Should().BeOfType<RedirectToPageResult>();

    [Fact]
    public async Task RemoveSireAndDam_FlagThePendingRemoval()
    {
        (await Create(Editor).OnPostRemoveSireAsync()).Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.RemoveSirePending.Should().BeTrue();

        (await Create(Editor).OnPostRemoveDamAsync()).Should().BeOfType<RedirectToPageResult>();
        _sessionState.Current!.RemoveDamPending.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_DiscardsTheDraft()
    {
        (await Create(Editor).OnPostCancelRelationsEditAsync()).Should().BeOfType<RedirectToPageResult>();
        (await Create(Editor).OnGetCancelRelationsEditAsync()).Should().BeOfType<RedirectToPageResult>();

        await _discard.Received(2).DiscardAllAsync(Rbse);
    }

    [Fact]
    public async Task SaveRelations_Success_RedirectsHome()
    {
        _orchestration.CommitAllAsync(Rbse, 3).Returns(CaseCommitOutcome.Success([]));

        var result = await Create(DataEntryOnly).OnPostSaveRelationsAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task SaveRelations_WithWarnings_ShowsThePartialSuccessScreen()
    {
        _orchestration.CommitAllAsync(Rbse, 3).Returns(CaseCommitOutcome.Success(["warn"]));

        (await Create(DataEntryOnly).OnPostSaveRelationsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task SaveRelations_WhenTheCommitFails_ReturnsHomeWithAnError()
    {
        _orchestration.CommitAllAsync(Rbse, 3).Returns(CaseCommitOutcome.Failure(EditCaseResult.RbseNotFound));
        var model = Create(DataEntryOnly);

        (await model.OnPostSaveRelationsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task SaveRelations_WhenMandatoryFieldsAreMissing_ShowsTheSaveResultScreen()
    {
        _orchestration.CommitAllAsync(Rbse, 3).Returns<CaseCommitOutcome>(_ => throw new MandatoryCaseFieldsMissingException(["missing"]));

        (await Create(DataEntryOnly).OnPostSaveRelationsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task SaveRelations_WhenTheDatabaseFails_ShowsTheFailureScreen()
    {
        _orchestration.CommitAllAsync(Rbse, 3).Returns<CaseCommitOutcome>(_ => throw new InvalidOperationException("db"));

        (await Create(DataEntryOnly).OnPostSaveRelationsAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }
    [Fact]
    public async Task SaveRelations_ValidatesTheDamBirthYear()
    {
        var model = Create(DataEntryOnly);
        model.DamSire.HasDam = true;
        model.DamSire.DamId = 1;
        model.DamSire.DamRowStamp = Convert.ToBase64String([1, 2, 3]);
        model.DamSire.DamBirthYear = DateTime.Today.Year + 1;

        var result = await model.OnPostSaveRelationsAsync();

        result.Should().BeOfType<PageResult>();
        model.DamError.Should().NotBeNull();
    }
}
