using System.Security.Claims;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
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

public sealed class BabModelFlowTests
{
    private const string Rbse = "002600001";

    private readonly IBabRepository _bab = Substitute.For<IBabRepository>();
    private readonly ICaseRepository _cases = Substitute.For<ICaseRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly ICaseEditSessionStateService _session = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private CaseEditSessionState? _stored;

    public BabModelFlowTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetAnimalOriginsAsync().Returns(new[] { new LuAnimalOrigin { Id = 1, Code = "N", Description = "Native" } });
        _batches.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _user.GetUserIdAsync().Returns(1);
        _session.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => _stored);
        _session.SetAsync(Arg.Do<CaseEditSessionState>(d => _stored = d), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _orchestration.CommitAllAsync(Rbse, 1).Returns(CaseCommitOutcome.Success([]));
    }

    private BabModel Create(params string[] roles)
    {
        var model = new BabModel(_bab, _cases, _lookups, _batches, _session, _discard, _orchestration, _user,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "https://spol" }).Build())
        {
            Rbse = Rbse
        };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private void GivenCase(DateTime? dob, string? origin = "N", string? purchasedCounty = null) =>
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord
        {
            Rbse = Rbse, BirthDate = dob, Origin = origin, PurchasedCounty = purchasedCounty
        });

    private void GivenBab() => _bab.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse, Notes = "n", RowStamp = [1, 2] });

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var model = Create("DataEntry");
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_EditableCase_LoadsAndOverlaysStagedBab()
    {
        GivenCase(new DateTime(2020, 1, 1), purchasedCounty: "Devon");
        GivenBab();
        _stored = new CaseEditSessionState
        {
            Rbse = Rbse,
            Bab = new EditCaseBabCommand(Rbse, null, "staged", null, null, null, null, null, null, null, null, [])
        };
        var model = Create("DataEntry");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.CanEditBabControls.Should().BeTrue();
        model.HasPurchaseData.Should().BeTrue();
        model.Bab.Notes.Should().Be("staged");
        model.SpolSiteUrl.Should().Be("https://spol");
        model.RowStampBase64.Should().NotBeNull();
    }

    [Theory]
    [InlineData(false, false, false, 2020, false, false)]   // no DataEntry
    [InlineData(true, true, false, 2020, false, false)]     // VLA read-only
    [InlineData(true, true, true, 2020, false, true)]       // VLA maintenance
    [InlineData(true, false, false, 0, false, false)]       // no DOB
    [InlineData(true, false, false, 1980, false, false)]    // pre-threshold, no BAB row
    [InlineData(true, false, false, 1980, true, true)]      // pre-threshold, BAB row
    public async Task OnGet_EditPermission(bool dataEntry, bool vla, bool vlaMaint, int year, bool hasBab, bool expected)
    {
        GivenCase(year == 0 ? null : new DateTime(year, 1, 1));
        if (hasBab) GivenBab();
        var roles = new List<string>();
        if (dataEntry) roles.Add("DataEntry");
        if (vla) roles.Add("VLAAccess");
        if (vlaMaint) roles.Add("VLAMaintenance");
        var model = Create([.. roles]);

        await model.OnGetAsync();

        model.CanEditBabControls.Should().Be(expected);
    }

    [Fact]
    public async Task OnGet_StagedCaseOverridesPersistedScalars()
    {
        GivenCase(null);
        _stored = new CaseEditSessionState
        {
            Rbse = Rbse,
            Case = new BSE.Host.Models.ViewModels.CaseEditViewModel { Rbse = Rbse, BirthDate = new DateTime(2021, 1, 1), Origin = "P" }.ToEditCommand([])
        };
        var model = Create("DataEntry");

        await model.OnGetAsync();

        model.CanEditBabControls.Should().BeTrue();
        model.Origin.Should().Be("P");
    }

    [Fact]
    public async Task Save_WithoutDataEntry_Forbids()
    {
        (await Create().OnPostSaveBabAsync(null)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Save_NotEditable_OnlyCommitsStaged()
    {
        GivenCase(null);
        var model = Create("DataEntry");

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        await _orchestration.Received(1).CommitAllAsync(Rbse, 1);
        _stored.Should().BeNull();
    }

    [Fact]
    public async Task Save_InvalidNatalCphh_RedisplaysPage()
    {
        GivenCase(new DateTime(2020, 1, 1));
        var model = Create("DataEntry");
        model.Origin = "P";
        model.Bab.NatalCphh = "123";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<PageResult>();
        model.ModelState.ContainsKey("Bab.NatalCphh").Should().BeTrue();
        await _orchestration.DidNotReceiveWithAnyArgs().CommitAllAsync(default!, default);
    }

    [Fact]
    public async Task Save_PurchasedWithValidCphh_StagesNormalisedBabAndOrigin()
    {
        GivenCase(new DateTime(2020, 1, 1), origin: "N");
        var model = Create("DataEntry");
        model.Origin = "P";
        model.Bab.NatalCphh = "12/345/6789/01";
        model.Bab.TracedName = "Trader";

        var result = await model.OnPostSaveBabAsync(Convert.ToBase64String([9]));

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _stored!.Bab!.NatalCphh.Should().Be("12345678901");
        _stored.Bab.TracedName.Should().Be("Trader");
        _stored.BabOrigin.Should().Be("P");
        _stored.Case!.Origin.Should().Be("P");
        _stored.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task Save_NotPurchased_ClearsTracedFieldsAndPurchaseFields()
    {
        GivenCase(new DateTime(2020, 1, 1), origin: "P", purchasedCounty: "Devon");
        var model = Create("DataEntry");
        model.Origin = "N";
        model.Bab.TracedName = "Trader";
        model.Bab.NatalCphh = "12345678901";

        await model.OnPostSaveBabAsync(null);

        _stored!.Bab!.TracedName.Should().BeNull();
        _stored.Bab.NatalCphh.Should().BeNull();
        _stored.Case!.PurchasedCounty.Should().BeNull();
    }

    [Fact]
    public async Task Save_NotPurchased_WithStagedCaseAndNothingToClear_LeavesStagedCase()
    {
        GivenCase(new DateTime(2020, 1, 1), origin: "N");
        var staged = new BSE.Host.Models.ViewModels.CaseEditViewModel { Rbse = Rbse, BirthDate = new DateTime(2020, 1, 1), Origin = "N" }.ToEditCommand([]);
        _stored = new CaseEditSessionState { Rbse = Rbse, Case = staged };
        var model = Create("DataEntry");
        model.Origin = "N";

        await model.OnPostSaveBabAsync(null);

        _stored!.Case!.PurchaseDate.Should().BeNull();
    }

    [Fact]
    public async Task Save_NotPurchased_WithStagedPurchaseData_ClearsIt()
    {
        GivenCase(new DateTime(2020, 1, 1), origin: "N");
        var staged = new BSE.Host.Models.ViewModels.CaseEditViewModel
        {
            Rbse = Rbse, BirthDate = new DateTime(2020, 1, 1), Origin = "N", PurchasedCounty = "Kent"
        }.ToEditCommand([]);
        _stored = new CaseEditSessionState { Rbse = Rbse, Case = staged };
        var model = Create("DataEntry");
        model.Origin = "N";

        await model.OnPostSaveBabAsync(null);

        _stored!.Case!.PurchasedCounty.Should().BeNull();
    }

    [Fact]
    public async Task Save_CommitFails_RedirectsHomeWithError()
    {
        GivenCase(new DateTime(2020, 1, 1));
        _orchestration.CommitAllAsync(Rbse, 1).Returns(CaseCommitOutcome.Failure(EditCaseResult.ConcurrencyConflict));
        var model = Create("DataEntry");
        model.Origin = "N";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["ErrorMessage"].Should().Be("Unable to save BAB changes: ConcurrencyConflict.");
    }

    [Fact]
    public async Task Save_Warnings_RedirectsToSaveResult()
    {
        GivenCase(new DateTime(2020, 1, 1));
        _orchestration.CommitAllAsync(Rbse, 1).Returns(CaseCommitOutcome.Success(["warn"]));
        var model = Create("DataEntry");
        model.Origin = "N";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task Save_MandatoryMissing_RedirectsToSaveResult()
    {
        GivenCase(new DateTime(2020, 1, 1));
        _orchestration.CommitAllAsync(Rbse, 1)
            .Returns<CaseCommitOutcome>(_ => throw new MandatoryCaseFieldsMissingException(["missing"]));
        var model = Create("DataEntry");
        model.Origin = "N";

        var result = await model.OnPostSaveBabAsync(null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    [Fact]
    public async Task StageAndGoto_WithoutDataEntry_JustRedirects()
    {
        var result = await Create().OnPostStageAndGotoAsync("/Case/Farm", null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _stored.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_NotEditable_JustRedirects()
    {
        GivenCase(null);

        var result = await Create("DataEntry").OnPostStageAndGotoAsync("/Case/Farm", null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _stored.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_InvalidCphh_RedisplaysPage()
    {
        GivenCase(new DateTime(2020, 1, 1));
        var model = Create("DataEntry");
        model.Origin = "P";
        model.Bab.NatalCphh = "1";

        (await model.OnPostStageAndGotoAsync("/Case/Farm", null)).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task StageAndGoto_EditedFields_FlagsPendingChanges()
    {
        GivenCase(new DateTime(2020, 1, 1));
        GivenBab();
        var model = Create("DataEntry");
        model.Origin = "N";
        model.Bab.Notes = "edited";

        var result = await model.OnPostStageAndGotoAsync("/Case/Farm", "");

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _stored!.Bab!.Notes.Should().Be("edited");
        _stored.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task StageAndGoto_NewCaseUntouched_DoesNotFlagPendingChanges()
    {
        GivenCase(new DateTime(2020, 1, 1), origin: "N");
        var model = Create("DataEntry");
        model.Origin = "N";

        await model.OnPostStageAndGotoAsync("/Case/Farm", null);

        _stored.Should().NotBeNull();
        _stored!.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task StageAndGoto_OriginChanged_FlagsPendingChanges()
    {
        GivenCase(new DateTime(2020, 1, 1), origin: "N");
        var model = Create("DataEntry");
        model.Origin = "P";
        model.Bab.NatalCphh = "12345678901";

        await model.OnPostStageAndGotoAsync("/Case/Farm", null);

        _stored!.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task CancelBabEdit_DiscardsDraftAndGoesHome()
    {
        var result = await Create("DataEntry").OnGetCancelBabEdit();

        await _discard.Received(1).DiscardAllAsync(Rbse);
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }
}
