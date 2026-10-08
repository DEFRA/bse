using System.Security.Claims;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>
/// Covers <see cref="RelationsModel"/>'s dam/sire Look Up and relation-details handlers —
/// previously untested. Exercises the LookUpAsync/IsSearchRbseRejected/FinishLookUpAsync/
/// RedirectToPickSireDam refactor extracted while fixing SonarCloud cognitive-complexity
/// findings, so these tests both add coverage and prove the refactor preserved behaviour.
/// </summary>
public sealed class RelationsModelHandlerTests
{
    private readonly IAnimalRelationsRepository _relationsRepository = Substitute.For<IAnimalRelationsRepository>();
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly ICaseRelationsDraftStateService _relationsDraftState = Substitute.For<ICaseRelationsDraftStateService>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private const string Rbse = "002600001";

    public RelationsModelHandlerTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetSexesAsync().Returns(Array.Empty<LuSex>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _relationsRepository.GetRelationsDetailsByRbseAsync(Arg.Any<string>())
            .Returns(new RelationDetailsRecord(null, null, Array.Empty<CaseRelationRecord>()));
        _relationsDraftState.GetAsync(Arg.Any<string>()).Returns((CaseRelationsDraftState?)null);
    }

    private RelationsModel CreateModel(string[]? roles = null)
    {
        var model = new RelationsModel(
            _relationsRepository, _caseService, _lookups,
            _batchRepository, _relationsDraftState, Substitute.For<ICaseScalarDraftStateService>(), Substitute.For<ICaseEditOrchestrationService>(), _connectionFactory, _currentUser,
            NullLogger<RelationsModel>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection().Build())
        {
            Rbse = Rbse
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = roles is { Length: > 0 }
            ? new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"))
            : new ClaimsPrincipal(new ClaimsIdentity());

        model.PageContext = new PageContext { HttpContext = httpContext };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        return model;
    }

    // ── OnPostLookUpSireAsync: role gate ─────────────────────────────────────

    [Fact]
    public async Task OnPostLookUpSireAsync_WhenUserLacksVlaAccess_ReturnsForbid()
    {
        var model = CreateModel(["DataEntry"]);

        var result = await model.OnPostLookUpSireAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    // ── OnPostLookUpDamAsync: IsSearchRbseRejected → SetError(SameAsCaseRbse) ───

    [Fact]
    public async Task OnPostLookUpDamAsync_WhenSearchRbseMatchesCaseRbse_SetsDamErrorAndReturnsPage()
    {
        var model = CreateModel();
        model.DamSire.DamSearchRbse = Rbse;

        var result = await model.OnPostLookUpDamAsync();

        result.Should().BeOfType<PageResult>();
        model.DamError.Should().Be(RelationsModel.SameAsCaseRbse);
    }

    // ── LookUpAsync: blank search RBSE → open picker (no RBSE in TempData route) ──

    [Fact]
    public async Task OnPostLookUpDamAsync_WhenSearchRbseBlank_RedirectsToPickSireDamWithBlankCriteria()
    {
        var model = CreateModel();
        model.DamSire.DamSearchRbse = string.Empty;

        var result = await model.OnPostLookUpDamAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/PickSireDam");
        redirect.RouteValues.Should().NotBeNull();
        redirect.RouteValues!["sex"].Should().Be("F");
        redirect.RouteValues!["eartag"].Should().Be(string.Empty);
        redirect.RouteValues.Should().NotContainKey("rbse");
    }

    // ── LookUpByRbseAsync: single match → ApplyMatchAsync populates DamSire ──────

    [Fact]
    public async Task OnPostLookUpDamAsync_WhenSingleMatchFound_PopulatesDamSireAndReturnsPage()
    {
        var model = CreateModel();
        model.DamSire.DamSearchRbse = "002700002";

        _relationsRepository.GetDamSireDetailsMatchesAsync(null, null, "002700002", null, "F")
            .Returns(new List<DamSireDetailRecord>
            {
                new() { Id = 42, Rbse = "002700002", Eartag = "UK123", Name = "Bessie", Herdbook = "HB1", ChildCount = 2 }
            });

        var result = await model.OnPostLookUpDamAsync();

        result.Should().BeOfType<PageResult>();
        model.DamSire.HasDam.Should().BeTrue();
        model.DamSire.DamId.Should().Be(42);
        model.DamSire.DamEartag.Should().Be("UK123");
        model.DamSire.DamName.Should().Be("Bessie");
    }

    // ── LookUpByRbseAsync: no matches → SetError(DamNotFound) ───────────────────

    [Fact]
    public async Task OnPostLookUpDamAsync_WhenNoMatchesFound_SetsDamNotFoundError()
    {
        var model = CreateModel();
        model.DamSire.DamSearchRbse = "002700003";

        _relationsRepository.GetDamSireDetailsMatchesAsync(null, null, "002700003", null, "F")
            .Returns(new List<DamSireDetailRecord>());

        var result = await model.OnPostLookUpDamAsync();

        result.Should().BeOfType<PageResult>();
        model.DamError.Should().Be(RelationsModel.DamNotFound);
    }

    // ── LookUpByRbseAsync: multiple matches → RedirectToPickSireDam with populated criteria ──

    [Fact]
    public async Task OnPostLookUpSireAsync_WhenMultipleMatchesFound_RedirectsToPickSireDamWithSearchCriteria()
    {
        var model = CreateModel(["DataEntry", "VLAAccess"]);
        model.DamSire.SireSearchRbse = "002700004";
        model.DamSire.SireSearchEartag = "UK999";

        _relationsRepository.GetDamSireDetailsMatchesAsync("UK999", null, "002700004", null, "M")
            .Returns(new List<DamSireDetailRecord>
            {
                new() { Id = 1, Rbse = "002700004" },
                new() { Id = 2, Rbse = "002700004" }
            });

        var result = await model.OnPostLookUpSireAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/PickSireDam");
        redirect.RouteValues.Should().NotBeNull();
        redirect.RouteValues!["sex"].Should().Be("M");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
        redirect.RouteValues!["eartag"].Should().Be("UK999");
    }

    // ── OnGetRelationDetailsAsync: blank rbse → {found:false} without a repository call ──

    [Fact]
    public async Task OnGetRelationDetailsAsync_WhenRbseBlank_ReturnsNotFoundWithoutQuerying()
    {
        var model = CreateModel();

        var result = await model.OnGetRelationDetailsAsync(null);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        json.Value.Should().BeEquivalentTo(new { found = false });
        await _relationsRepository.DidNotReceive().GetRelationDetailsOfRelatedCaseAsync(Arg.Any<string>());
    }

    // ── OnGetRelationDetailsAsync: not found in DB → {found:false} ──────────────

    [Fact]
    public async Task OnGetRelationDetailsAsync_WhenRelatedCaseNotFound_ReturnsNotFound()
    {
        var model = CreateModel();
        _relationsRepository.GetRelationDetailsOfRelatedCaseAsync("002700005").Returns((RelatedCaseDetailsRecord?)null);

        var result = await model.OnGetRelationDetailsAsync("002700005");

        var json = result.Should().BeOfType<JsonResult>().Subject;
        json.Value.Should().BeEquivalentTo(new { found = false });
    }

    // ── OnGetRelationDetailsAsync: found → maps fields, birthDate formatted dd/MM/yyyy ──

    [Fact]
    public async Task OnGetRelationDetailsAsync_WhenRelatedCaseFound_ReturnsMappedFieldsWithFormattedBirthDate()
    {
        var model = CreateModel();
        _relationsRepository.GetRelationDetailsOfRelatedCaseAsync("002700006").Returns(new RelatedCaseDetailsRecord
        {
            RelationRbse = "002700006",
            Sex = "F",
            Fate = "Alive",
            EartagCountry = "UK",
            EartagHerdmark = "123",
            Eartag = "45678",
            BirthDay = 15,
            BirthMonth = 6,
            BirthYear = 2020,
            LeftDate = "01/01/2021",
            Name = "Dam Name"
        });

        var result = await model.OnGetRelationDetailsAsync("002700006");

        var json = result.Should().BeOfType<JsonResult>().Subject;
        json.Value.Should().BeEquivalentTo(new
        {
            found = true,
            sex = "F",
            fate = "Alive",
            eartagCountry = "UK",
            eartagHerdmark = "123",
            eartag = "45678",
            birthDate = "15/06/2020",
            leftDate = "01/01/2021",
            sire = "Dam Name"
        });
    }

    // ── LookUpByRbseAsync / ApplyMatchAsync: single match found for the SIRE side ────
    // (the earlier single-match test only exercised the isDam=true branch of
    // ApplyMatchAsync; this covers the isDam=false branch, which is a separate
    // set of property assignments).

    [Fact]
    public async Task OnPostLookUpSireAsync_WhenSingleMatchFound_PopulatesSireFields()
    {
        var model = CreateModel(["DataEntry", "VLAAccess"]);
        model.DamSire.SireSearchRbse = "002700010";

        _relationsRepository.GetDamSireDetailsMatchesAsync(null, null, "002700010", null, "M")
            .Returns(new List<DamSireDetailRecord>
            {
                new() { Id = 7, Rbse = "002700010", Eartag = "UK777", Name = "Big Bull", Herdbook = "HB7", ChildCount = 3 }
            });

        var result = await model.OnPostLookUpSireAsync();

        result.Should().BeOfType<PageResult>();
        model.DamSire.HasSire.Should().BeTrue();
        model.DamSire.SireId.Should().Be(7);
        model.DamSire.SireEartag.Should().Be("UK777");
        model.DamSire.SireName.Should().Be("Big Bull");
        model.DamSire.SireChildCount.Should().Be(3);
    }

    // ── OnPostAddRelationRowAsync: ValidateAndDeriveRelationFieldsAsync fails ────────

    [Fact]
    public async Task OnPostAddRelationRowAsync_WhenRelationTypeMissing_ShowsRowAndDoesNotStage()
    {
        var model = CreateModel(["DataEntry", "VLAAccess"]);
        model.RelationType = null;
        model.Sex = "M";

        var result = await model.OnPostAddRelationRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ShowAddRelationRow.Should().BeTrue();
        model.RelationFieldErrors.Should().ContainKey("RelationType");
        model.PagedRelations.Should().BeEmpty();
    }

    // ── OnPostAddRelationRowAsync: happy path with a related-case RBSE supplied ─────
    // Exercises ValidateAndDeriveRelationFieldsAsync -> ApplyRelatedCaseFieldsAsync,
    // which derives Sex/Fate/Eartag/BirthDate from the related case (legacy
    // ctlRelationRBSE_RBSEChanged), then stages the row and redirects.

    [Fact]
    public async Task OnPostAddRelationRowAsync_WhenRelatedRbseSupplied_DerivesFieldsAndStagesRow()
    {
        var model = CreateModel(["DataEntry", "VLAAccess"]);
        model.RelationType = "Offspring";
        model.RelationRbse = "002700011";
        _relationsRepository.GetRelationDetailsOfRelatedCaseAsync("002700011").Returns(new RelatedCaseDetailsRecord
        {
            RelationRbse = "002700011",
            Sex = "F",
            Fate = "Alive",
            Name = "Derived Sire"
        });

        CaseRelationsDraftState? savedDraft = null;
        _relationsDraftState.SetAsync(Arg.Do<CaseRelationsDraftState>(d => savedDraft = d)).Returns(Task.CompletedTask);

        var result = await model.OnPostAddRelationRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.RelationFieldErrors.Should().BeEmpty();
        model.Sex.Should().Be("F");
        model.Sire.Should().Be("Derived Sire");
        savedDraft.Should().NotBeNull();
        savedDraft!.Relations.Should().ContainSingle(r => r.RelationType == "Offspring" && r.Sex == "F");
    }
}

