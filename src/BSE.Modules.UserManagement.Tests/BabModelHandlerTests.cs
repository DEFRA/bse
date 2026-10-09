using System.Security.Claims;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
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
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>Covers the request-handling behaviour of <see cref="BabModel"/> (0% new-code coverage) —
/// OnGetAsync, OnPostSaveBabAsync (both the staged-edit and first-time-create paths), and
/// OnPostStageAndGotoAsync, none of which had any prior test.</summary>
public sealed class BabModelHandlerTests
{
    private const string Rbse = "002600001";
    private const string Cphh = "01001000101";

    private readonly IBabRepository _babRepository = Substitute.For<IBabRepository>();
    private readonly ICaseRepository _caseRepository = Substitute.For<ICaseRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly ICaseScalarDraftStateService _caseScalarDraftState = Substitute.For<ICaseScalarDraftStateService>();
    private readonly ICaseEditOrchestrationService _caseEditOrchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public BabModelHandlerTests()
    {
        _lookups.GetAnimalOriginsAsync().Returns(Array.Empty<LuAnimalOrigin>());
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _caseScalarDraftState.GetAsync(Arg.Any<string>()).Returns((CaseScalarDraftState?)null);
    }

    private BabModel CreateModel(string[]? roles = null)
    {
        var model = new BabModel(
            _babRepository, _caseRepository, _lookups, _batchRepository, _connectionFactory,
            _caseScalarDraftState, _caseEditOrchestration, _currentUser,
            new ConfigurationBuilder().AddInMemoryCollection().Build())
        {
            Rbse = Rbse
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            (roles ?? []).Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"));
        model.PageContext = new PageContext { HttpContext = httpContext };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static CaseRecord MakeCase(DateTime? birthDate = null, string? origin = null, byte[]? rowStamp = null) => new()
    {
        Rbse = Rbse,
        Cphh = Cphh,
        BirthDate = birthDate,
        Origin = origin,
        RowStamp = rowStamp ?? [1, 2, 3]
    };

    // ── OnGetAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_MissingRbse_RedirectsToSessionError()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        var model = CreateModel();
        model.Rbse = string.Empty;

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>()
            .Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGetAsync_WithBirthDateAfterThreshold_EnablesEditingForDataEntry()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);

        var model = CreateModel(["DataEntry"]);
        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.CanEditBabControls.Should().BeTrue();
    }

    [Fact]
    public async Task OnGetAsync_WithBirthDateBeforeThresholdAndNoBabRow_DisablesEditing()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1980, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);

        var model = CreateModel(["DataEntry"]);
        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.CanEditBabControls.Should().BeFalse();
    }

    [Fact]
    public async Task OnGetAsync_VlaAccessWithoutMaintenance_IsAlwaysReadOnly()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);

        var model = CreateModel(["DataEntry", "VLAAccess"]);
        await model.OnGetAsync();

        model.CanEditBabControls.Should().BeFalse();
    }

    [Fact]
    public async Task OnGetAsync_ApplyStagedOverlay_WhenDraftHasBab_OverlaysStagedFields()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);
        _caseScalarDraftState.GetAsync(Rbse).Returns(new CaseScalarDraftState
        {
            Rbse = Rbse,
            Bab = new EditCaseBabCommand(Rbse, null, "staged notes", null, null, null, null, null, null, null, null, [1])
        });

        var model = CreateModel(["DataEntry"]);
        await model.OnGetAsync();

        model.Bab.Notes.Should().Be("staged notes");
    }

    // ── OnPostSaveBabAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task OnPostSaveBabAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateModel();

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSaveBabAsync_WhenNotEditable_RedirectsToSelf()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: null));
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);

        var model = CreateModel(["DataEntry"]);
        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>()
            .Which.RouteValues.Should().ContainValue(Rbse);
    }

    [Fact]
    public async Task OnPostSaveBabAsync_WithInvalidNatalCphhLength_AddsModelErrorAndReturnsPage()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);

        var model = CreateModel(["DataEntry"]);
        model.Bab.NatalCphh = "123"; // not 11 digits

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<PageResult>();
        model.ModelState.IsValid.Should().BeFalse();
        model.ModelState["Bab.NatalCphh"]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task OnPostSaveBabAsync_NewRow_AddsViaRepositoryAndRedirectsToHome()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);
        var connection = Substitute.For<System.Data.IDbConnection>();
        var transaction = Substitute.For<System.Data.IDbTransaction>();
        connection.BeginTransaction().Returns(transaction);
        _connectionFactory.CreateConnection().Returns(connection);

        var model = CreateModel(["DataEntry"]);
        model.Origin = "H";
        model.Bab.Notes = "new bab notes";

        var result = await model.OnPostSaveBabAsync(rowStampBase64: null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _babRepository.Received(1).AddAsync(
            Arg.Is<AddCaseBabCommand>(c => c.Rbse == Rbse && c.Notes == "new bab notes"), "H", connection, transaction);
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task OnPostSaveBabAsync_ExistingRow_StagesAndCommitsThenRedirectsToHome()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, Notes = "old", RowStamp = [9, 9, 9] });
        _currentUser.GetUserIdAsync().Returns(7);
        _caseEditOrchestration.CommitAllAsync(Rbse, 7).Returns(CaseCommitOutcome.Success([]));

        var model = CreateModel(["DataEntry"]);
        model.Origin = "P";
        model.Bab.Notes = "updated notes";

        var result = await model.OnPostSaveBabAsync(Convert.ToBase64String([9, 9, 9]));

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _caseScalarDraftState.Received(1).SetAsync(Arg.Is<CaseScalarDraftState>(d => d.Bab!.Notes == "updated notes" && d.HasPendingChanges));
        await _caseEditOrchestration.Received(1).CommitAllAsync(Rbse, 7);
    }

    [Fact]
    public async Task OnPostSaveBabAsync_WhenCommitThrowsMandatoryFieldsMissing_RedirectsToSaveResult()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, RowStamp = [9, 9, 9] });
        _currentUser.GetUserIdAsync().Returns(7);
        _caseEditOrchestration.CommitAllAsync(Rbse, 7)
            .Returns<CaseCommitOutcome>(_ => throw new MandatoryCaseFieldsMissingException(["Missing field"]));

        var model = CreateModel(["DataEntry"]);

        var result = await model.OnPostSaveBabAsync(Convert.ToBase64String([9, 9, 9]));

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task OnPostSaveBabAsync_WhenOriginIsNotPurchased_ClearsTracedAndNatalFields()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, RowStamp = [9, 9, 9] });
        _currentUser.GetUserIdAsync().Returns(7);
        _caseEditOrchestration.CommitAllAsync(Rbse, 7).Returns(CaseCommitOutcome.Success([]));

        var model = CreateModel(["DataEntry"]);
        model.Origin = "H"; // not "P"
        model.Bab.TracedName = "Someone";
        model.Bab.NatalCphh = "01001000101";

        await model.OnPostSaveBabAsync(Convert.ToBase64String([9, 9, 9]));

        await _caseScalarDraftState.Received(1).SetAsync(Arg.Is<CaseScalarDraftState>(
            d => d.Bab!.TracedName == null && d.Bab.NatalCphh == null));
    }

    // ── OnPostStageAndGotoAsync ──────────────────────────────────────────────

    [Fact]
    public async Task OnPostStageAndGotoAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateModel();

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm", null);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostStageAndGotoAsync_WithNoRowStamp_RedirectsWithoutStaging()
    {
        var model = CreateModel(["DataEntry"]);

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm", null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _caseScalarDraftState.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Fact]
    public async Task OnPostStageAndGotoAsync_WhenCaseIsMissing_RedirectsWithoutStaging()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);

        var model = CreateModel(["DataEntry"]);
        var result = await model.OnPostStageAndGotoAsync("/Case/Farm", Convert.ToBase64String([1, 2, 3]));

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _caseScalarDraftState.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Fact]
    public async Task OnPostStageAndGotoAsync_WhenFieldsAreUnchanged_DoesNotFlagPendingChanges()
    {
        var baseRowStamp = Convert.ToBase64String([9, 9, 9]);
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1), origin: "H"));
        _babRepository.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, Notes = "same notes", RowStamp = [9, 9, 9] });

        var model = CreateModel(["DataEntry"]);
        model.Origin = "H";
        model.Bab.Notes = "same notes";

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm", baseRowStamp);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _caseScalarDraftState.Received(1).SetAsync(Arg.Is<CaseScalarDraftState>(d => !d.HasPendingChanges));
    }

    [Fact]
    public async Task OnPostStageAndGotoAsync_WhenNotesChanged_FlagsPendingChanges()
    {
        var baseRowStamp = Convert.ToBase64String([9, 9, 9]);
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1), origin: "H"));
        _babRepository.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, Notes = "old notes", RowStamp = [9, 9, 9] });

        var model = CreateModel(["DataEntry"]);
        model.Origin = "H";
        model.Bab.Notes = "new notes";

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm", baseRowStamp);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _caseScalarDraftState.Received(1).SetAsync(Arg.Is<CaseScalarDraftState>(d => d.HasPendingChanges));
    }

    [Fact]
    public async Task OnPostStageAndGotoAsync_WithInvalidNatalCphhLength_AddsModelErrorAndReturnsPage()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(MakeCase(birthDate: new DateTime(1990, 1, 1)));
        _babRepository.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, RowStamp = [9, 9, 9] });

        var model = CreateModel(["DataEntry"]);
        model.Bab.NatalCphh = "123";

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm", Convert.ToBase64String([9, 9, 9]));

        result.Should().BeOfType<PageResult>();
        model.ModelState.IsValid.Should().BeFalse();
    }

    // ── OnGetCancelBabEdit ───────────────────────────────────────────────────

    [Fact]
    public async Task OnGetCancelBabEdit_ClearsDraftAndRedirectsToHome()
    {
        var model = CreateModel(["DataEntry"]);

        var result = await model.OnGetCancelBabEdit();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _caseScalarDraftState.Received(1).ClearAsync(Rbse);
    }
}
