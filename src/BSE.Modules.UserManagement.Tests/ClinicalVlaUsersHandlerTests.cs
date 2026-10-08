using System.Reflection;
using System.Security.Claims;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Admin;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Services;
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

/// <summary>Covers the OnGet/OnPost request handlers of VlaModel, ClinicalModel and the remaining
/// UsersModel branches, which are not exercised by the sorting/validation-helper focused tests
/// already in <c>PageModelCoverageTests</c>.</summary>
public sealed class ClinicalVlaUsersHandlerTests
{
    private const string Rbse = "002600001";

    // ── VlaModel ─────────────────────────────────────────────────────────────

    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly IBabRepository _babRepository = Substitute.For<IBabRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICaseWizardStateService _wizardState = Substitute.For<ICaseWizardStateService>();
    private readonly ICaseEditDraftStateService _caseEditDraftState = Substitute.For<ICaseEditDraftStateService>();
    private readonly ICaseScalarDraftStateService _caseScalarDraftState = Substitute.For<ICaseScalarDraftStateService>();
    private readonly ICaseEditOrchestrationService _caseEditOrchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly IOtherOwnerRepository _ownerRepository = Substitute.For<IOtherOwnerRepository>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();

    private VlaModel CreateVlaModel(string[]? roles = null)
    {
        _lookups.GetBreedsAsync().Returns(Array.Empty<LuBreed>());
        _lookups.GetSexesAsync().Returns(Array.Empty<LuSex>());
        _lookups.GetAnimalOriginsAsync().Returns(Array.Empty<LuAnimalOrigin>());
        _lookups.GetCountiesAsync().Returns(Array.Empty<LuBSECounty>());
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _wizardState.GetAsync().Returns((CaseWizardState?)null);
        _babRepository.GetByRbseAsync(Arg.Any<string>()).Returns((CaseBabRecord?)null);
        _ownerRepository.GetByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<OtherOwnerRecord>());

        var model = new VlaModel(
            _caseService, _babRepository, _currentUserService, _wizardState, _caseEditDraftState,
            _caseScalarDraftState, _caseEditOrchestration,
            _lookups, _batchRepository, _ownerRepository, _connectionFactory,
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

    [Fact]
    public async Task VlaModel_OnGetAsync_CaseNotFound_SetsWarningAndReturnsPage()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.TempData["Warning"].Should().Be($"Case '{Rbse}' is not saved yet. Complete Farm first.");
    }

    [Fact]
    public async Task VlaModel_OnGetAsync_CaseFound_LoadsCaseDetails()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "01001000101", RowStamp = [1, 2] });

        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Case.Rbse.Should().Be(Rbse);
    }

    [Fact]
    public async Task VlaModel_OnPostAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateVlaModel(["VLAAccess"]);
        var result = await model.OnPostAsync();
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostAsync_NotAllowedToEdit_RedirectsToSelf()
    {
        // No pending batch and no batch history → CanEditMainCase is false.
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        var result = await model.OnPostAsync();
        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostAsync_CaseNotFound_SetsWarning()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.TempData["Warning"].Should().Be($"Case '{Rbse}' is not saved yet. Complete Farm first.");
    }

    [Fact]
    public async Task VlaModel_OnPostAsync_CommitHasWarnings_RedirectsToPartialSuccessSaveResult()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        var caseRecord = new CaseRecord { Rbse = Rbse, Cphh = "01001000101", RowStamp = [1, 2, 3] };
        _caseService.GetCaseAsync(Rbse).Returns(caseRecord);
        _caseEditOrchestration.CommitAllAsync(Rbse, Arg.Any<int>())
            .Returns(EditCaseResult.ConcurrencyConflict);
        _currentUserService.GetUserIdAsync().Returns(4);

        model.TempData[string.Format("VlaEdit_RowStamp_{0}", Rbse)] = Convert.ToBase64String([1, 2, 3]);
        model.Case = new VlaEditViewModel { Rbse = Rbse };

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task VlaModel_OnPostAsync_Success_UpdatesAndRedirects()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        var caseRecord = new CaseRecord { Rbse = Rbse, Cphh = "01001000101", RowStamp = [1, 2, 3] };
        _caseService.GetCaseAsync(Rbse).Returns(caseRecord);
        _caseEditOrchestration.CommitAllAsync(Rbse, Arg.Any<int>())
            .Returns(EditCaseResult.Success);
        _currentUserService.GetUserIdAsync().Returns(4);

        model.TempData[string.Format("VlaEdit_RowStamp_{0}", Rbse)] = Convert.ToBase64String([1, 2, 3]);
        model.Case = new VlaEditViewModel { Rbse = Rbse, Origin = "B" };

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Home");
        await _caseEditDraftState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task VlaModel_OnGetCancelVlaEditAsync_ClearsDraftAndRedirectsHome()
    {
        var model = CreateVlaModel();
        var result = await model.OnGetCancelVlaEditAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        ((RedirectToPageResult)result).PageName.Should().Be("/Home");
        await _caseEditDraftState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task VlaModel_OnPostBeginEditOwnerRowAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateVlaModel(["VLAAccess"]);
        var result = await model.OnPostBeginEditOwnerRowAsync(11);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostBeginEditOwnerRowAsync_NotAllowedToEdit_RedirectsToSelf()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        var result = await model.OnPostBeginEditOwnerRowAsync(11);
        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostBeginEditOwnerRowAsync_PopulatesEditFieldsFromStagedOwner()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        _caseEditDraftState.GetAsync(Rbse).Returns(new CaseEditDraftState
        {
            Rbse = Rbse,
            OtherOwners = [new CaseEditDraftOtherOwnerItem { Id = 11, Type = "B", Name = "Owner Name", Cphh = "01001000101" }]
        });

        var result = await model.OnPostBeginEditOwnerRowAsync(11);

        result.Should().BeOfType<PageResult>();
        model.ReopenEditOwnerId.Should().Be(11);
        model.EditOwnerType.Should().Be("B");
        model.EditOwnerName.Should().Be("Owner Name");
    }

    [Fact]
    public async Task VlaModel_OnPostBeginEditOwnerRowAsync_OwnerNotFound_RedirectsToSelf()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        _caseEditDraftState.GetAsync(Rbse).Returns(new CaseEditDraftState { Rbse = Rbse });

        var result = await model.OnPostBeginEditOwnerRowAsync(99);

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostAddOwnerRowAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateVlaModel(["VLAAccess"]);
        var result = await model.OnPostAddOwnerRowAsync();
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostAddOwnerRowAsync_NotAllowedToEdit_RedirectsToSelf()
    {
        // No pending batch and no batch history → CanEditMainCase is false.
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        var result = await model.OnPostAddOwnerRowAsync();
        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostAddOwnerRowAsync_Invalid_ShowsAddRow()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        _caseEditDraftState.GetAsync(Rbse).Returns(new CaseEditDraftState { Rbse = Rbse });

        model.NewOwnerType = null;
        model.NewOwnerName = null;
        model.NewOwnerCphh = null;

        var result = await model.OnPostAddOwnerRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ShowAddOwnerRow.Should().BeTrue();
        model.ModelState.Should().ContainKey(nameof(VlaModel.NewOwnerType));
    }

    [Fact]
    public async Task VlaModel_OnPostAddOwnerRowAsync_Valid_AddsToDraftAndRedirects()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        var draft = new CaseEditDraftState { Rbse = Rbse };
        _caseEditDraftState.GetAsync(Rbse).Returns(draft);
        _lookups.GetLookupAsync(LookupTableId.OwnerType).Returns(
            [new LookupItem { Code = "B", Description = "Buyer" }]);

        model.NewOwnerType = "B";
        model.NewOwnerName = "Jane Owner";

        var result = await model.OnPostAddOwnerRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        draft.OtherOwners.Should().Contain(o => o.Name == "Jane Owner");
        await _caseEditDraftState.Received().SetAsync(Arg.Any<CaseEditDraftState>());
    }

    [Fact]
    public async Task VlaModel_OnPostUpdateOwnerRowAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateVlaModel(["VLAAccess"]);
        var result = await model.OnPostUpdateOwnerRowAsync();
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostUpdateOwnerRowAsync_NotAllowedToEdit_RedirectsToSelf()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        var result = await model.OnPostUpdateOwnerRowAsync();
        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostUpdateOwnerRowAsync_Valid_UpdatesDraftRow()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        var draft = new CaseEditDraftState
        {
            Rbse = Rbse,
            OtherOwners = [new CaseEditDraftOtherOwnerItem { Id = 11, Type = "B", Name = "Old Name" }]
        };
        _caseEditDraftState.GetAsync(Rbse).Returns(draft);

        model.EditingOwnerId = 11;
        model.EditOwnerType = "B";
        model.EditOwnerName = "New Name";

        var result = await model.OnPostUpdateOwnerRowAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _caseEditDraftState.Received(1).SetAsync(Arg.Is<CaseEditDraftState>(d =>
            d.OtherOwners.Single().Name == "New Name"));
    }

    [Fact]
    public async Task VlaModel_OnPostDeleteOwnerAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateVlaModel(["VLAAccess"]);
        var result = await model.OnPostDeleteOwnerAsync(11, string.Empty);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostDeleteOwnerAsync_NotAllowedToEdit_RedirectsToSelf()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        var result = await model.OnPostDeleteOwnerAsync(11, string.Empty);
        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostDeleteOwnerAsync_RemovesMatchingOwner()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        var draft = new CaseEditDraftState
        {
            Rbse = Rbse,
            OtherOwners = [new CaseEditDraftOtherOwnerItem { Id = 11, Type = "B", Name = "Owner" }]
        };
        _caseEditDraftState.GetAsync(Rbse).Returns(draft);

        var result = await model.OnPostDeleteOwnerAsync(11, string.Empty);

        result.Should().BeOfType<RedirectToPageResult>();
        await _caseEditDraftState.Received(1).SetAsync(Arg.Is<CaseEditDraftState>(d => d.OtherOwners.Count == 0));
    }

    [Fact]
    public async Task VlaModel_OnPostAsync_WhenUserLacksVlaAccess_RedirectsToSelf()
    {
        var model = CreateVlaModel(["DataEntry"]);
        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task VlaModel_OnPostAddOwnerRowAsync_RejectsDuplicatePreviousOwnerType()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        _batchRepository.GetBatchNumbersByRbseAsync(Rbse).Returns(
            [new BatchNumberEntry(1, "2024/001", Rbse, "BSE1")]);
        _lookups.GetLookupAsync(LookupTableId.OwnerType).Returns(
            [new LookupItem { Code = "Previous", Description = "Previous owner" }]);
        _caseEditDraftState.GetAsync(Rbse).Returns(new CaseEditDraftState
        {
            Rbse = Rbse,
            OtherOwners = [new CaseEditDraftOtherOwnerItem { Id = 1, Type = "Previous", Name = "Existing Owner" }]
        });

        model.NewOwnerType = "Previous";
        model.NewOwnerName = "Replacement Owner";

        var result = await model.OnPostAddOwnerRowAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(VlaModel.NewOwnerType));
        var previousOwnerState = model.ModelState[nameof(VlaModel.NewOwnerType)];
        previousOwnerState.Should().NotBeNull();
        previousOwnerState!.Errors
            .Should().Contain(x => x.ErrorMessage.Contains("one owner of type Previous", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VlaModel_ReplaceUnparseableDateMessage_RewritesInvalidDateText()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        model.ModelState.AddModelError("Case.BirthDate", "The value 'bad' is not valid.");

        typeof(VlaModel)
            .GetMethod("ReplaceUnparseableDateMessage", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(model, new object?[] { "Case.BirthDate", null });

        var birthDateState = model.ModelState["Case.BirthDate"];
        birthDateState.Should().NotBeNull();
        birthDateState!.Errors.Should().ContainSingle();
        birthDateState.Errors[0].ErrorMessage.Should().Be("Please enter a valid date");
    }

    [Fact]
    public void VlaModel_ValidateVlaDomainRules_RejectsOnsetAndSlaughterDatesOutsideAllowedRange()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        model.Case = new VlaEditViewModel
        {
            Rbse = Rbse,
            FormADate = new DateTime(2024, 06, 01),
            OnsetDate = new DateTime(2024, 07, 02),
            SlaughterDate = DateTime.Today.AddDays(1)
        };

        typeof(VlaModel)
            .GetMethod("ValidateVlaDomainRules", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(model, null);

        var onsetState = model.ModelState["Case.OnsetDate"];
        onsetState.Should().NotBeNull();
        onsetState!.Errors.Should().ContainSingle();

        var slaughterState = model.ModelState["Case.SlaughterDate"];
        slaughterState.Should().NotBeNull();
        slaughterState!.Errors.Should().ContainSingle();
    }

    [Fact]
    public void VlaModel_OwnersSortUrl_TogglesDirectionForSameColumn()
    {
        var model = CreateVlaModel(["DataEntry", "VLAAccess"]);
        model.Rbse = Rbse;
        model.OSort = "type";
        model.ODir = "asc";

        model.OwnersSortUrl("type").Should().Contain("ODir=desc");
        model.OwnersSortUrl("name").Should().Contain("ODir=asc");
    }

    // ── ClinicalModel ────────────────────────────────────────────────────────

    private readonly IClinicalRepository _clinicalRepository = Substitute.For<IClinicalRepository>();
    private readonly ICaseRepository _caseRepository = Substitute.For<ICaseRepository>();
    private readonly ICaseClinicalDraftStateService _clinicalDraftState = Substitute.For<ICaseClinicalDraftStateService>();
    private readonly IDbConnectionFactory _clinicalConnectionFactory = Substitute.For<IDbConnectionFactory>();
    private readonly ICaseScalarDraftStateService _clinicalScalarDraftState = Substitute.For<ICaseScalarDraftStateService>();
    private readonly ICaseEditOrchestrationService _clinicalEditOrchestration = Substitute.For<ICaseEditOrchestrationService>();

    private ClinicalModel CreateClinicalModel(string[]? roles = null)
    {
        _clinicalRepository.GetByRbseAsync(Arg.Any<string>()).Returns((CaseClinicalRecord?)null);
        _clinicalRepository.GetVisitsByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<ClinicalVisitRecord>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _caseRepository.GetCaseByRbseAsync(Arg.Any<string>()).Returns((CaseRecord?)null);

        var connection = Substitute.For<System.Data.IDbConnection>();
        var transaction = Substitute.For<System.Data.IDbTransaction>();
        _clinicalConnectionFactory.CreateConnection().Returns(connection);
        connection.BeginTransaction().Returns(transaction);

        var model = new ClinicalModel(
            _clinicalRepository, _caseRepository, _batchRepository, _clinicalDraftState,
            _clinicalScalarDraftState, _clinicalEditOrchestration,
            _currentUserService, _clinicalConnectionFactory, new ConfigurationBuilder().AddInMemoryCollection().Build())
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

    [Fact]
    public async Task ClinicalModel_OnGetAsync_ReturnsPage()
    {
        var model = CreateClinicalModel();
        var result = await model.OnGetAsync();
        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task ClinicalModel_OnPostSaveSignsAsync_Forbidden_WhenNotDataEntry()
    {
        var model = CreateClinicalModel();
        var result = await model.OnPostSaveSignsAsync(null);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ClinicalModel_OnPostSaveSignsAsync_CaseNotFound_SetsWarning()
    {
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);

        var model = CreateClinicalModel(["DataEntry"]);
        var result = await model.OnPostSaveSignsAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["Warning"].Should().Be($"Case '{Rbse}' is not saved yet. Complete Farm first.");
    }

    [Fact]
    public async Task ClinicalModel_OnPostSaveSignsAsync_NoRowStamp_AddsSigns()
    {
        var model = CreateClinicalModel(["DataEntry"]);
        model.PageContext.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
        model.PageContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "01001000101" });

        var result = await model.OnPostSaveSignsAsync(null);

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Home");
        await _clinicalRepository.Received(1).AddAsync(
            Arg.Any<AddCaseClinicalCommand>(), Arg.Any<System.Data.IDbConnection>(), Arg.Any<System.Data.IDbTransaction>());
    }

    [Fact]
    public async Task ClinicalModel_OnPostSaveSignsAsync_WithRowStamp_EditsSigns()
    {
        var model = CreateClinicalModel(["DataEntry"]);
        model.PageContext.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
        model.PageContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "01001000101" });
        _currentUserService.GetUserIdAsync().Returns(7);
        _clinicalEditOrchestration.CommitAllAsync(Rbse, 7).Returns(CaseCommitOutcome.Success([]));

        _clinicalEditOrchestration.CommitAllAsync(Rbse, Arg.Any<int>()).Returns(EditCaseResult.Success);

        var rowStampBase64 = Convert.ToBase64String([9, 9]);
        var result = await model.OnPostSaveSignsAsync(rowStampBase64);

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Home");
        await _clinicalEditOrchestration.Received(1).CommitAllAsync(Rbse, Arg.Any<int>());
    }

    [Fact]
    public async Task ClinicalModel_OnPostCancelClinicalEditAsync_ClearsDraftAndRedirects()
    {
        var model = CreateClinicalModel();
        var result = await model.OnPostCancelClinicalEditAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _clinicalDraftState.Received(1).ClearAsync(Rbse);
    }

    [Fact]
    public async Task ClinicalModel_OnGetCancelClinicalEditAsync_ClearsDraftAndRedirectsHome()
    {
        var model = CreateClinicalModel();
        var result = await model.OnGetCancelClinicalEditAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        ((RedirectToPageResult)result).PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task ClinicalModel_OnPostAddVisitRowAsync_Forbidden_WhenMissingRole()
    {
        var model = CreateClinicalModel(["DataEntry"]); // missing VLAAccess
        var result = await model.OnPostAddVisitRowAsync(null);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ClinicalModel_OnPostAddVisitRowAsync_Invalid_ReopensAddRow()
    {
        _clinicalDraftState.GetAsync(Rbse).Returns(new CaseClinicalDraftState { Rbse = Rbse });

        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        model.NewVisitDate = null;

        var result = await model.OnPostAddVisitRowAsync(null);

        result.Should().BeOfType<PageResult>();
        model.ShowAddVisitRow.Should().BeTrue();
        model.ModelState.Should().ContainKey(nameof(ClinicalModel.NewVisitDate));
    }

    [Fact]
    public async Task ClinicalModel_OnPostAddVisitRowAsync_Valid_AddsToDraft()
    {
        _clinicalDraftState.GetAsync(Rbse).Returns(new CaseClinicalDraftState { Rbse = Rbse });

        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        model.NewVisitDate = new DateTime(2024, 1, 10);

        var result = await model.OnPostAddVisitRowAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        await _clinicalDraftState.Received(1).SetAsync(Arg.Is<CaseClinicalDraftState>(d =>
            d.Visits.Any(v => v.VisitDate == new DateTime(2024, 1, 10))));
    }

    [Fact]
    public async Task ClinicalModel_OnPostBeginEditVisitRowAsync_SetsEditState()
    {
        var draft = new CaseClinicalDraftState
        {
            Rbse = Rbse,
            Visits = [new CaseClinicalDraftVisitItem { ClientKey = "v1", VisitDate = new DateTime(2024, 2, 1) }]
        };
        _clinicalDraftState.GetAsync(Rbse).Returns(draft);

        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        var result = await model.OnPostBeginEditVisitRowAsync("v1", null);

        result.Should().BeOfType<PageResult>();
        model.ReopenEditClientKey.Should().Be("v1");
        model.EditVisitDate.Should().Be(new DateTime(2024, 2, 1));
    }

    [Fact]
    public async Task ClinicalModel_OnPostUpdateVisitRowAsync_Valid_UpdatesDraftRow()
    {
        var draft = new CaseClinicalDraftState
        {
            Rbse = Rbse,
            Visits = [new CaseClinicalDraftVisitItem { ClientKey = "v1", VisitDate = new DateTime(2024, 2, 1) }]
        };
        _clinicalDraftState.GetAsync(Rbse).Returns(draft);

        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        model.EditingClientKey = "v1";
        model.EditVisitDate = new DateTime(2024, 3, 1);

        var result = await model.OnPostUpdateVisitRowAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        await _clinicalDraftState.Received(1).SetAsync(Arg.Is<CaseClinicalDraftState>(d =>
            d.Visits.Single().VisitDate == new DateTime(2024, 3, 1)));
    }

    [Fact]
    public async Task ClinicalModel_OnPostDeleteVisitAsync_RemovesMatchingVisit()
    {
        var draft = new CaseClinicalDraftState
        {
            Rbse = Rbse,
            Visits = [new CaseClinicalDraftVisitItem { ClientKey = "v1", VisitDate = new DateTime(2024, 2, 1) }]
        };
        _clinicalDraftState.GetAsync(Rbse).Returns(draft);

        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        var result = await model.OnPostDeleteVisitAsync("v1", null);

        result.Should().BeOfType<RedirectToPageResult>();
        await _clinicalDraftState.Received(1).SetAsync(Arg.Is<CaseClinicalDraftState>(d => d.Visits.Count == 0));
    }

    // ── UsersModel (remaining gaps) ──────────────────────────────────────────

    private readonly IUserManagementService _userService = Substitute.For<IUserManagementService>();
    private readonly ILookupDataService _userLookups = Substitute.For<ILookupDataService>();

    private UsersModel CreateUsersModel()
    {
        var httpContext = new DefaultHttpContext();
        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>())
        };
        return model;
    }

    [Fact]
    public void UsersModel_OnPostStartEdit_NoSelection_RedirectsWithoutEditUserId()
    {
        var model = CreateUsersModel();
        model.SelectedUserId = 0;

        var result = model.OnPostStartEdit();

        result.Should().BeOfType<RedirectToPageResult>();
        ((RedirectToPageResult)result).RouteValues.Should().NotContainKey("EditUserId");
    }

    [Fact]
    public void UsersModel_OnPostStartEdit_WithSelection_RedirectsWithEditUserId()
    {
        var model = CreateUsersModel();
        model.SelectedUserId = 5;

        var result = model.OnPostStartEdit();

        result.Should().BeOfType<RedirectToPageResult>();
        ((RedirectToPageResult)result).RouteValues.Should().ContainKey("EditUserId").WhoseValue.Should().Be(5);
    }

    [Fact]
    public void UsersModel_OnPostCancel_RedirectsToGrid()
    {
        var model = CreateUsersModel();
        var result = model.OnPostCancel();
        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_UpdatesExistingUser()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [nameof(UsersModel.EditIsActive)] = "true"
        });

        var existingUser = new User(3, "existing", "existing@bse.local", "Existing User", "existing@example.com", true, 2, UserGroup.DataEntry);
        _userService.GetAllUsersAsync().Returns([existingUser]);
        _userLookups.GetUserGroupsAsync().Returns([new LuUserGroup { Id = 2, Name = "Data Entry" }]);

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 3,
            EditNTLogin = "existing",
            EditUserName = "Existing User Updated",
            EditEmail = "existing@example.com",
            EditUserGroupId = 2
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _userService.Received(1).UpdateUserAsync(Arg.Is<User>(u => u.UserName == "Existing User Updated"));
        model.TempData["Success"].Should().Be("User 'Existing User Updated' updated.");
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_DuplicateNTLogin_AddsModelError()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [nameof(UsersModel.EditIsActive)] = "true"
        });

        var existingUser1 = new User(1, "dup", "dup@bse.local", "Dup User", "dup@example.com", true, 2, UserGroup.DataEntry);
        var existingUser2 = new User(2, "other", "other@bse.local", "Other User", "other@example.com", true, 2, UserGroup.DataEntry);
        _userService.GetAllUsersAsync().Returns([existingUser1, existingUser2]);
        _userLookups.GetUserGroupsAsync().Returns([new LuUserGroup { Id = 2, Name = "Data Entry" }]);

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 2,
            EditNTLogin = "dup",
            EditUserName = "Other User",
            EditEmail = "other@example.com",
            EditUserGroupId = 2
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(UsersModel.EditNTLogin));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(7, false)]
    [InlineData(7, true)]
    public void UsersModel_IsEditing_ReturnsExpectedResult(int editUserId, bool queryMatches)
    {
        var model = CreateUsersModel();
        model.EditUserId = editUserId;

        model.IsEditing(queryMatches ? editUserId : editUserId + 1).Should().Be(editUserId != 0 && queryMatches);
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, true, true)]
    [InlineData(3, false, true)]
    [InlineData(3, true, true)]
    public void UsersModel_IsEditingOrAdding_ReturnsExpectedResult(int editUserId, bool isAddingNew, bool expected)
    {
        var model = CreateUsersModel();
        model.EditUserId = editUserId;
        model.IsAddingNew = isAddingNew;

        model.IsEditingOrAdding.Should().Be(expected);
    }

    [Fact]
    public async Task UsersModel_OnGetAsync_EditUserIdNotFound_ResetsToZero()
    {
        _userService.GetAllUsersAsync().Returns([new User(1, "a", "a@example.com", "A", "a@example.com", true, 1, UserGroup.Admin)]);
        _userLookups.GetUserGroupsAsync().Returns(Array.Empty<LuUserGroup>());

        var model = CreateUsersModel();
        model.EditUserId = 99;

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.EditUserId.Should().Be(0);
    }

    [Fact]
    public async Task UsersModel_OnGetAsync_IsAddingNew_ResetsEditFieldsToBlank()
    {
        _userService.GetAllUsersAsync().Returns(Array.Empty<User>());
        _userLookups.GetUserGroupsAsync().Returns(Array.Empty<LuUserGroup>());

        var model = CreateUsersModel();
        model.EditUserId = 0;
        model.IsAddingNew = true;

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.EditUserName.Should().BeEmpty();
        model.EditIsActive.Should().BeTrue();
        model.EditUserGroupId.Should().Be(0);
    }

    [Theory]
    [InlineData("NTLogin", false)]
    [InlineData("NTLogin", true)]
    [InlineData("UserName", false)]
    [InlineData("UserName", true)]
    [InlineData("Email", false)]
    [InlineData("Email", true)]
    [InlineData("Group", false)]
    [InlineData("Group", true)]
    [InlineData("IsActive", false)]
    [InlineData("IsActive", true)]
    [InlineData("Other", false)]
    [InlineData("Other", true)]
    public async Task UsersModel_OnGetAsync_SortsByEveryColumnInBothDirections(string sortColumn, bool sortDesc)
    {
        var low = new User(1, "a-login", "a@bse.local", "A User", "a@example.com", false, 1, UserGroup.ReadOnly);
        var high = new User(2, "b-login", "b@bse.local", "B User", "b@example.com", true, 2, UserGroup.DataEntry);
        _userService.GetAllUsersAsync().Returns([high, low]);
        _userLookups.GetUserGroupsAsync().Returns(
        [
            new LuUserGroup { Id = 1, Name = "A Group" },
            new LuUserGroup { Id = 2, Name = "B Group" }
        ]);

        var model = CreateUsersModel();
        model.SortColumn = sortColumn;
        model.SortDesc = sortDesc;

        await model.OnGetAsync();

        var expectedFirst = sortDesc ? high : low;
        model.Users.First().UserId.Should().Be(expectedFirst.UserId);
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_EmptyUserName_AddsModelError()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        _userService.GetAllUsersAsync().Returns(Array.Empty<User>());
        _userLookups.GetUserGroupsAsync().Returns(Array.Empty<LuUserGroup>());

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 0,
            EditUserName = "   ",
            EditEmail = "new@example.com",
            EditUserGroupId = 1
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(UsersModel.EditUserName));
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_UserNameTooLong_AddsModelError()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        _userService.GetAllUsersAsync().Returns(Array.Empty<User>());
        _userLookups.GetUserGroupsAsync().Returns(Array.Empty<LuUserGroup>());

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 0,
            EditUserName = new string('x', 40),
            EditEmail = "new@example.com",
            EditUserGroupId = 1
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(UsersModel.EditUserName));
    }

    [Theory]
    [InlineData("", "Enter an email address")]
    [InlineData("not-an-email", "Enter an email address in the correct format, like name@example.com")]
    public async Task UsersModel_OnPostEditAsync_Adding_InvalidEmail_AddsModelError(string email, string expectedMessage)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        _userService.GetAllUsersAsync().Returns(Array.Empty<User>());
        _userLookups.GetUserGroupsAsync().Returns(Array.Empty<LuUserGroup>());

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 0,
            EditUserName = "New Person",
            EditEmail = email,
            EditUserGroupId = 1
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(UsersModel.EditEmail)]!.Errors.Should().Contain(e => e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_Editing_EmptyNTLogin_AddsModelError()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        var existingUser = new User(5, "existing", "existing@bse.local", "Existing", "existing@example.com", true, 1, UserGroup.Admin);
        _userService.GetAllUsersAsync().Returns([existingUser]);
        _userLookups.GetUserGroupsAsync().Returns([new LuUserGroup { Id = 1, Name = "Admin" }]);

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 5,
            EditNTLogin = "",
            EditUserName = "Existing",
            EditEmail = "existing@example.com",
            EditUserGroupId = 1
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(UsersModel.EditNTLogin));
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_NoUserGroupSelected_AddsModelError()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        _userService.GetAllUsersAsync().Returns(Array.Empty<User>());
        _userLookups.GetUserGroupsAsync().Returns(Array.Empty<LuUserGroup>());

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 0,
            EditUserName = "New Person",
            EditEmail = "new@example.com",
            EditUserGroupId = 0
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(UsersModel.EditUserGroupId));
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_DuplicateEmail_AddsModelError()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        var existingUser1 = new User(1, "one", "one@bse.local", "One", "shared@example.com", true, 1, UserGroup.Admin);
        var existingUser2 = new User(2, "two", "two@bse.local", "Two", "two@example.com", true, 1, UserGroup.Admin);
        _userService.GetAllUsersAsync().Returns([existingUser1, existingUser2]);
        _userLookups.GetUserGroupsAsync().Returns([new LuUserGroup { Id = 1, Name = "Admin" }]);

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 2,
            EditNTLogin = "two",
            EditUserName = "Two",
            EditEmail = "shared@example.com",
            EditUserGroupId = 1
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(UsersModel.EditEmail));
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_SymbolsOnlyEmailLocalPart_DerivesFallbackNtLogin()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [nameof(UsersModel.EditIsActive)] = "true"
        });
        _userService.GetAllUsersAsync().Returns(Array.Empty<User>());
        _userLookups.GetUserGroupsAsync().Returns([new LuUserGroup { Id = 1, Name = "Admin" }]);

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 0,
            EditUserName = "Symbol Person",
            EditEmail = "###@example.com",
            EditUserGroupId = 1
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _userService.Received(1).AddUserAsync(Arg.Is<User>(u => u.NTLogin == "user"));
    }

    [Fact]
    public async Task UsersModel_OnPostEditAsync_DuplicateDerivedNtLogin_AppendsSuffix()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [nameof(UsersModel.EditIsActive)] = "true"
        });
        var existingUser = new User(1, "jsmith", "jsmith@bse.local", "J Smith", "jsmith@old.com", true, 1, UserGroup.Admin);
        _userService.GetAllUsersAsync().Returns([existingUser]);
        _userLookups.GetUserGroupsAsync().Returns([new LuUserGroup { Id = 1, Name = "Admin" }]);

        var model = new UsersModel(_userService, _userLookups)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            EditUserId = 0,
            EditUserName = "New Smith",
            EditEmail = "jsmith@example.com",
            EditUserGroupId = 1
        };

        var result = await model.OnPostEditAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _userService.Received(1).AddUserAsync(Arg.Is<User>(u => u.NTLogin == "jsmith1"));
    }

    // ── Clinical.cshtml.cs branch coverage ──────────────────────────────────────

    [Fact]
    public async Task ClinicalModel_OnPostBeginEditVisitRowAsync_Forbidden_WhenMissingRole()
    {
        var model = CreateClinicalModel(["DataEntry"]); // missing VLAAccess
        var result = await model.OnPostBeginEditVisitRowAsync("v1", null);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ClinicalModel_OnPostUpdateVisitRowAsync_Forbidden_WhenMissingRole()
    {
        var model = CreateClinicalModel(["VLAAccess"]); // missing DataEntry
        var result = await model.OnPostUpdateVisitRowAsync(null);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ClinicalModel_OnPostDeleteVisitAsync_Forbidden_WhenMissingRole()
    {
        var model = CreateClinicalModel(["VLAAccess"]); // missing DataEntry
        var result = await model.OnPostDeleteVisitAsync("v1", null);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ClinicalModel_OnPostAddVisitRowAsync_DuplicateDate_AddsModelError()
    {
        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        var existingDate = new DateTime(2024, 6, 1);
        _clinicalDraftState.GetAsync(Rbse).Returns(new CaseClinicalDraftState
        {
            Rbse = Rbse,
            Visits = [new CaseClinicalDraftVisitItem { ClientKey = "v1", VisitDate = existingDate }]
        });
        model.NewVisitDate = existingDate;

        var result = await model.OnPostAddVisitRowAsync(null);

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(ClinicalModel.NewVisitDate)]!.Errors.Should()
            .Contain(e => e.ErrorMessage.Contains("already exists"));
    }

    [Fact]
    public async Task ClinicalModel_OnPostAddVisitRowAsync_DateBeforeBirthDate_AddsModelError()
    {
        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, BirthDate = new DateTime(2024, 1, 1) });
        _clinicalDraftState.GetAsync(Rbse).Returns(new CaseClinicalDraftState { Rbse = Rbse });
        model.NewVisitDate = new DateTime(2023, 12, 31);

        var result = await model.OnPostAddVisitRowAsync(null);

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(ClinicalModel.NewVisitDate)]!.Errors.Should()
            .Contain(e => e.ErrorMessage == "The visit date must be after the birth date.");
    }

    [Fact]
    public async Task ClinicalModel_OnPostAddVisitRowAsync_FutureDate_AddsModelError()
    {
        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        _clinicalDraftState.GetAsync(Rbse).Returns(new CaseClinicalDraftState { Rbse = Rbse });
        model.NewVisitDate = DateTime.Today.AddDays(5);

        var result = await model.OnPostAddVisitRowAsync(null);

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(ClinicalModel.NewVisitDate)]!.Errors.Should()
            .Contain(e => e.ErrorMessage == "The visit date must not be in the future.");
    }

    [Fact]
    public async Task ClinicalModel_OnPostUpdateVisitRowAsync_RowNotFound_SetsErrorAndRedirects()
    {
        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        _clinicalDraftState.GetAsync(Rbse).Returns(new CaseClinicalDraftState { Rbse = Rbse });
        model.EditingClientKey = "missing";

        var result = await model.OnPostUpdateVisitRowAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["ErrorMessage"].Should().Be("The clinical visit row being edited no longer exists.");
    }

    [Fact]
    public async Task ClinicalModel_OnPostUpdateVisitRowAsync_DuplicateDate_ReopensRowWithError()
    {
        var model = CreateClinicalModel(["DataEntry", "VLAAccess"]);
        var sharedDate = new DateTime(2024, 5, 1);
        _clinicalDraftState.GetAsync(Rbse).Returns(new CaseClinicalDraftState
        {
            Rbse = Rbse,
            Visits =
            [
                new CaseClinicalDraftVisitItem { ClientKey = "v1", VisitDate = sharedDate },
                new CaseClinicalDraftVisitItem { ClientKey = "v2", VisitDate = new DateTime(2024, 5, 10) }
            ]
        });
        model.EditingClientKey = "v2";
        model.EditVisitDate = sharedDate;

        var result = await model.OnPostUpdateVisitRowAsync(null);

        result.Should().BeOfType<PageResult>();
        model.ReopenEditClientKey.Should().Be("v2");
        model.ModelState[nameof(ClinicalModel.EditVisitDate)]!.Errors.Should()
            .Contain(e => e.ErrorMessage.Contains("already exists"));
    }

    [Fact]
    public async Task ClinicalModel_OnGetAsync_ExistingClinicalRecord_PopulatesSignsAndRowStamp()
    {
        var model = CreateClinicalModel();
        _clinicalRepository.GetByRbseAsync(Rbse).Returns(new CaseClinicalRecord
        {
            Rbse = Rbse,
            Apprehension = true,
            RowStamp = [1, 2, 3]
        });

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Signs.Apprehension.Should().BeTrue();
        model.ClinicalRowStampBase64.Should().Be(Convert.ToBase64String([1, 2, 3]));
    }

    [Fact]
    public async Task ClinicalModel_OnPostSaveSignsAsync_RemovesVisitNoLongerStaged()
    {
        var model = CreateClinicalModel(["DataEntry"]);
        model.PageContext.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
        model.PageContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "01001000101" });
        _clinicalRepository.GetVisitsByRbseAsync(Rbse).Returns(
            [new ClinicalVisitRecord(1, Rbse, new DateTime(2024, 1, 1), [9, 9])]);
        // No existing draft => a fresh one is seeded from persisted visits, then immediately cleared below
        // so the handler's internal reload sees an empty staged list (i.e. the visit was deleted).
        _clinicalDraftState.GetAsync(Rbse).Returns(
            new CaseClinicalDraftState { Rbse = Rbse, Visits = [] },
            new CaseClinicalDraftState { Rbse = Rbse, Visits = [] });

        var result = await model.OnPostSaveSignsAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        await _clinicalRepository.Received(1).DeleteVisitAsync(1, Arg.Is<byte[]>(b => b.SequenceEqual(new byte[] { 9, 9 })),
            Arg.Any<System.Data.IDbConnection>(), Arg.Any<System.Data.IDbTransaction>());
    }

    [Fact]
    public async Task ClinicalModel_OnPostSaveSignsAsync_EditsVisitWhenDateChanged()
    {
        var model = CreateClinicalModel(["DataEntry"]);
        model.PageContext.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
        model.PageContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "01001000101" });
        _clinicalRepository.GetVisitsByRbseAsync(Rbse).Returns(
            [new ClinicalVisitRecord(1, Rbse, new DateTime(2024, 1, 1), [9, 9])]);
        var draft = new CaseClinicalDraftState
        {
            Rbse = Rbse,
            Visits = [new CaseClinicalDraftVisitItem { ClientKey = "v1", Id = 1, VisitDate = new DateTime(2024, 2, 2), RowStampBase64 = Convert.ToBase64String([9, 9]) }]
        };
        _clinicalDraftState.GetAsync(Rbse).Returns(draft);

        var result = await model.OnPostSaveSignsAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        await _clinicalRepository.Received(1).EditVisitAsync(
            Arg.Is<EditClinicalVisitCommand>(c => c.Id == 1 && c.VisitDate == new DateTime(2024, 2, 2)),
            Arg.Any<System.Data.IDbConnection>(), Arg.Any<System.Data.IDbTransaction>());
    }

    [Fact]
    public async Task ClinicalModel_OnPostSaveSignsAsync_SkipsUnchangedPersistedVisit()
    {
        var model = CreateClinicalModel(["DataEntry"]);
        model.PageContext.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
        model.PageContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = "01001000101" });
        _clinicalRepository.GetVisitsByRbseAsync(Rbse).Returns(
            [new ClinicalVisitRecord(1, Rbse, new DateTime(2024, 1, 1), [9, 9])]);
        var draft = new CaseClinicalDraftState
        {
            Rbse = Rbse,
            Visits = [new CaseClinicalDraftVisitItem { ClientKey = "v1", Id = 1, VisitDate = new DateTime(2024, 1, 1), RowStampBase64 = Convert.ToBase64String([9, 9]) }]
        };
        _clinicalDraftState.GetAsync(Rbse).Returns(draft);

        var result = await model.OnPostSaveSignsAsync(null);

        result.Should().BeOfType<RedirectToPageResult>();
        await _clinicalRepository.DidNotReceive().EditVisitAsync(
            Arg.Any<EditClinicalVisitCommand>(), Arg.Any<System.Data.IDbConnection>(), Arg.Any<System.Data.IDbTransaction>());
        await _clinicalRepository.DidNotReceive().DeleteVisitAsync(
            Arg.Any<int>(), Arg.Any<byte[]>(), Arg.Any<System.Data.IDbConnection>(), Arg.Any<System.Data.IDbTransaction>());
    }

    [Fact]
    public void ClinicalModel_VisitsSortUrl_TogglesDirection()
    {
        var model = CreateClinicalModel();
        model.Rbse = Rbse;
        model.VDir = "asc";
        model.VisitsSortUrl().Should().Contain("VDir=desc");

        model.VDir = "desc";
        model.VisitsSortUrl().Should().Contain("VDir=asc");
    }
}

