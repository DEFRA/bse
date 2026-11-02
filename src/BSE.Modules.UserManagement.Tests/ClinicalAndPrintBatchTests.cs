using System.Security.Claims;
using BSE.Host.Pages.Batch;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class ClinicalModelFlowTests
{
    private const string Rbse = "002600001";
    private static readonly string[] Vla = ["DataEntry", "VLAAccess"];

    private readonly IClinicalRepository _clinical = Substitute.For<IClinicalRepository>();
    private readonly ICaseRepository _cases = Substitute.For<ICaseRepository>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly ICaseWizardStateService _wizard = Substitute.For<ICaseWizardStateService>();
    private readonly InMemorySessionState _session = new();
    private readonly ICaseDraftDiscardService _discard = Substitute.For<ICaseDraftDiscardService>();
    private readonly ICaseEditOrchestrationService _orchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public ClinicalModelFlowTests()
    {
        _clinical.GetByRbseAsync(Rbse).Returns((CaseClinicalRecord?)null);
        _clinical.GetVisitsByRbseAsync(Rbse).Returns(new[]
        {
            new ClinicalVisitRecord(1, Rbse, new DateTime(2020, 6, 1), [1]),
            new ClinicalVisitRecord(2, Rbse, new DateTime(2020, 7, 1), null)
        });
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns(Array.Empty<BatchNumberEntry>());
        _wizard.GetAsync().Returns((CaseWizardState?)null);
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, BirthDate = new DateTime(2019, 1, 1) });
        _orchestration.CommitAllAsync(Rbse, 6).Returns(CaseCommitOutcome.Success([]));
        _user.GetUserIdAsync().Returns(6);
    }

    private ClinicalModel Create(string[] roles, params string[] trueSigns)
    {
        var model = new ClinicalModel(_clinical, _cases, _batches, _wizard, _session, _discard, _orchestration, _user,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "https://s" }).Build())
        { Rbse = Rbse };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"))
        };
        http.Request.ContentType = "application/x-www-form-urlencoded";
        http.Request.Form = new FormCollection(trueSigns.ToDictionary(s => $"Signs.{s}", _ => new StringValues("true")));
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private async Task<string> SeedAndFirstKeyAsync()
    {
        await Create(Vla).OnGetAsync();
        return _session.Current!.Visits.First().ClientKey;
    }

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToSessionError()
    {
        var model = Create(Vla);
        model.Rbse = "";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_WithPersistedSignsAndStagedOverlay_ShowsStaged()
    {
        _clinical.GetByRbseAsync(Rbse).Returns(new CaseClinicalRecord { Rbse = Rbse, Apprehension = true, RowStamp = [3] });
        _session.Current = new CaseEditSessionState
        {
            Rbse = Rbse,
            Case = new BSE.Host.Models.ViewModels.CaseEditViewModel { Rbse = Rbse, BirthDate = new DateTime(2018, 1, 1) }.ToEditCommand([]),
            Clinical = new ClinicalModel.ClinicalSignsViewModel { Tremor = true }.ToEditCommand(Rbse, [3])
        };
        var model = Create(Vla);

        await model.OnGetAsync();

        model.ClinicalRowStampBase64.Should().Be(Convert.ToBase64String([3]));
        model.Signs.Tremor.Should().BeTrue();
        model.Signs.Apprehension.Should().BeFalse();
        model.BirthDate.Should().Be(new DateTime(2018, 1, 1));
        model.SpolSiteUrl.Should().Be("https://s");
    }

    [Fact]
    public async Task OnGet_PendingBatchAlreadyLinked_LocksVlaEdit()
    {
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns(new[] { new BatchNumberEntry(5, "B", Rbse, "BSE1") });
        _wizard.GetAsync(Arg.Any<CancellationToken>()).Returns(new CaseWizardState(Rbse, "B", 5));
        var model = Create(Vla);

        await model.OnGetAsync();

        model.IsVlaAllowedAdditionalCaseEdit.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_DescendingVisitsAndPaging()
    {
        var model = Create(Vla);
        model.VDir = "desc";
        model.VPage = 9;

        await model.OnGetAsync();

        model.Visits.First().VisitDate.Should().Be(new DateTime(2020, 7, 1));
        model.VisitsTotalCount.Should().Be(2);
        model.VPage.Should().Be(1);
        model.VisitsSortUrl().Should().Contain("VDir=asc");
    }

    // â”€â”€ Save signs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Theory]
    [InlineData("DataEntry")]
    [InlineData("VLAAccess")]
    public async Task SaveSigns_WithoutBothRoles_Forbidden(string role)
    {
        (await Create([role]).OnPostSaveSignsAsync(null)).Should().BeOfType<ForbidResult>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AQ==")]
    public async Task SaveSigns_StagesPostedSignsAndCommits(string? rowStamp)
    {
        var model = Create(Vla, "Apprehension", "Tremor", "MilkYield");

        var result = await model.OnPostSaveSignsAsync(rowStamp);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        _session.Current!.Clinical!.Apprehension.Should().BeTrue();
        _session.Current.Clinical.Tremor.Should().BeTrue();
        _session.Current.Clinical.Kicking.Should().BeFalse();
        _session.Current.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task SaveSigns_CommitFailureAndWarnings()
    {
        _orchestration.CommitAllAsync(Rbse, 6).Returns(CaseCommitOutcome.Failure(EditCaseResult.RbseNotFound));
        var failed = Create(Vla);
        (await failed.OnPostSaveSignsAsync(null)).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        failed.TempData["ErrorMessage"]!.ToString().Should().Contain("Unable to save clinical signs");

        _orchestration.CommitAllAsync(Rbse, 6).Returns(CaseCommitOutcome.Success(["warn"]));
        (await Create(Vla).OnPostSaveSignsAsync(null)).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/SaveResult");
    }

    // â”€â”€ Navigation staging â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task StageAndGoto_WithoutDataEntry_DoesNotStage()
    {
        var result = await Create([]).OnPostStageAndGotoAsync("/Case/Farm", null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _session.Current.Should().BeNull();
    }

    [Fact]
    public async Task StageAndGoto_UntouchedSigns_DoNotFlagPendingChanges()
    {
        await Create(Vla).OnPostStageAndGotoAsync("/Case/Farm", "");

        _session.Current!.Clinical.Should().NotBeNull();
        _session.Current.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task StageAndGoto_ChangedSignsAgainstPersisted_FlagPendingChanges()
    {
        _clinical.GetByRbseAsync(Rbse).Returns(new CaseClinicalRecord { Rbse = Rbse, Apprehension = true, RowStamp = [1] });
        var model = Create(Vla, "Tremor");

        await model.OnPostStageAndGotoAsync("/Case/Farm", Convert.ToBase64String([1]));

        _session.Current!.HasPendingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task ResortVisits_StagesSignsAndReturnsPage()
    {
        var model = Create(Vla, "Blindness");

        (await model.OnPostResortVisitsAsync(null)).Should().BeOfType<PageResult>();
        _session.Current!.Clinical!.Blindness.Should().BeTrue();

        (await Create([]).OnPostResortVisitsAsync(null)).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task Cancel_DiscardsDraftOnPostAndGet()
    {
        var model = Create(Vla);

        (await model.OnPostCancelClinicalEditAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        (await model.OnGetCancelClinicalEditAsync()).Should().BeOfType<RedirectToPageResult>();
        await _discard.Received(2).DiscardAllAsync(Rbse);
    }

    // â”€â”€ Visit rows â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task VisitHandlers_WithoutBothRoles_Forbidden()
    {
        var model = Create(["DataEntry"]);

        (await model.OnPostAddVisitRowAsync(null)).Should().BeOfType<ForbidResult>();
        (await model.OnPostBeginEditVisitRowAsync("k", null)).Should().BeOfType<ForbidResult>();
        (await model.OnPostUpdateVisitRowAsync(null)).Should().BeOfType<ForbidResult>();
        (await model.OnPostDeleteVisitAsync("k", null)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddVisit_Valid_StagesAndRedirects()
    {
        var model = Create(Vla, "Apprehension");
        model.NewVisitDate = new DateTime(2020, 8, 1);

        var result = await model.OnPostAddVisitRowAsync("AQ==");

        result.Should().BeOfType<RedirectToPageResult>();
        _session.Current!.Visits.Should().Contain(v => v.VisitDate == new DateTime(2020, 8, 1));
        _session.Current.HasClinicalVisitsChanges.Should().BeTrue();
        _session.Current.Clinical!.Apprehension.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidDates))]
    public async Task AddVisit_Invalid_ReopensAddRow(DateTime? date)
    {
        var model = Create(Vla);
        model.NewVisitDate = date;

        (await model.OnPostAddVisitRowAsync(null)).Should().BeOfType<PageResult>();

        model.ShowAddVisitRow.Should().BeTrue();
        model.ModelState["NewVisitDate"]!.Errors.Should().NotBeEmpty();
    }

    public static TheoryData<DateTime?> InvalidDates() =>
    [
        (DateTime?)null,
        new DateTime(2018, 1, 1),
        DateTime.Today.AddDays(5),
        new DateTime(2020, 6, 1)
    ];

    [Fact]
    public async Task AddVisit_BinderErrorAlreadyPresent_NotDuplicated()
    {
        var model = Create(Vla);
        model.ModelState.AddModelError("NewVisitDate", "Please enter a valid date");

        await model.OnPostAddVisitRowAsync(null);

        model.ModelState["NewVisitDate"]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task AddVisit_NoBirthDate_UsesEpochMessage()
    {
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse });
        var model = Create(Vla);
        model.NewVisitDate = new DateTime(1960, 1, 1);

        await model.OnPostAddVisitRowAsync(null);

        model.ModelState["NewVisitDate"]!.Errors.Single().ErrorMessage.Should().Contain("1 January 1970");
    }

    [Fact]
    public async Task BeginEditVisit_KnownAndUnknownKey()
    {
        var key = await SeedAndFirstKeyAsync();

        var known = Create(Vla);
        (await known.OnPostBeginEditVisitRowAsync(key, null)).Should().BeOfType<PageResult>();
        known.ReopenEditClientKey.Should().Be(key);
        known.EditVisitDate.Should().Be(new DateTime(2020, 6, 1));

        var unknown = Create(Vla);
        (await unknown.OnPostBeginEditVisitRowAsync("nope", null)).Should().BeOfType<PageResult>();
        unknown.ReopenEditClientKey.Should().BeNull();
    }

    [Fact]
    public async Task UpdateVisit_Valid_UpdatesRow()
    {
        var key = await SeedAndFirstKeyAsync();
        var model = Create(Vla);
        model.EditingClientKey = key;
        model.EditVisitDate = new DateTime(2020, 9, 1);

        (await model.OnPostUpdateVisitRowAsync("AQ==")).Should().BeOfType<RedirectToPageResult>();

        _session.Current!.Visits.Single(v => v.ClientKey == key).VisitDate.Should().Be(new DateTime(2020, 9, 1));
    }

    [Fact]
    public async Task UpdateVisit_UnknownRowAndInvalidDates()
    {
        var key = await SeedAndFirstKeyAsync();

        var unknown = Create(Vla);
        unknown.EditingClientKey = "nope";
        (await unknown.OnPostUpdateVisitRowAsync(null)).Should().BeOfType<RedirectToPageResult>();
        unknown.TempData["ErrorMessage"].Should().NotBeNull();

        foreach (var bad in new DateTime?[] { null, new DateTime(2018, 1, 1), DateTime.Today.AddDays(2), new DateTime(2020, 7, 1) })
        {
            var invalid = Create(Vla);
            invalid.EditingClientKey = key;
            invalid.EditVisitDate = bad;
            (await invalid.OnPostUpdateVisitRowAsync(null)).Should().BeOfType<PageResult>();
            invalid.ReopenEditClientKey.Should().Be(key);
        }
    }

    [Fact]
    public async Task DeleteVisit_RemovesKnownRowAndIgnoresUnknown()
    {
        var key = await SeedAndFirstKeyAsync();

        (await Create(Vla).OnPostDeleteVisitAsync("nope", null)).Should().BeOfType<RedirectToPageResult>();
        _session.Current!.Visits.Should().HaveCount(2);

        (await Create(Vla).OnPostDeleteVisitAsync(key, null)).Should().BeOfType<RedirectToPageResult>();
        _session.Current!.Visits.Should().HaveCount(1);
        _session.Current.HasClinicalVisitsChanges.Should().BeTrue();
    }
}

public sealed class PrintBatchPageFlowTests
{
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();

    private PrintBatchModel Create(string? year, string? number, string? report)
    {
        var model = new PrintBatchModel(_batches) { BatchYear = year, BatchNumber = number, ReportType = report };
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        return model;
    }

    [Fact]
    public async Task OnGet_ReturnsPage()
    {
        (await Create(null, null, null).OnGetAsync()).Should().BeOfType<PageResult>();
        PrintBatchModel.ReportTypes.Should().HaveCount(5);
        Create(null, null, null).BatchCaseCount.Should().BeNull();
    }

    [Fact]
    public async Task Download_NothingEntered_ReportsErrors()
    {
        var model = Create(null, " ", null);

        (await model.OnPostDownloadAsync()).Should().BeOfType<PageResult>();

        model.ModelState.Keys.Should().Contain(["BatchYear", "ReportType"]);
    }

    [Fact]
    public async Task Download_BadYearAndNumber_ReportsErrors()
    {
        var model = Create("20", "abc", "Clinical");

        (await model.OnPostDownloadAsync()).Should().BeOfType<PageResult>();

        model.ModelState.Keys.Should().Contain(["BatchYear", "BatchNumber"]);
    }

    [Theory]
    [InlineData(null, "Batch 7 was not found.")]
    [InlineData("2024", "Batch 2024/7 was not found.")]
    public async Task Download_BatchNotFound_AddsError(string? year, string expected)
    {
        _batches.GetBatchIdAsync(Arg.Any<short?>(), 7).Returns((int?)null);
        var model = Create(year, "7", "Feeds");

        await model.OnPostDownloadAsync();

        model.ModelState["BatchYear"]!.Errors.Single().ErrorMessage.Should().Be(expected);
    }

    [Fact]
    public async Task Download_NoCases_AddsError()
    {
        _batches.GetBatchIdAsync((short)2024, 7).Returns(3);
        _batches.GetCasesByBatchIdAsync(3).Returns(Array.Empty<BatchCaseSummaryRecord>());
        var model = Create("2024", "7", "Feeds");

        await model.OnPostDownloadAsync();

        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Should().Contain("contains no cases");
    }

    [Fact]
    public async Task Download_Valid_RedirectsToReport()
    {
        _batches.GetBatchIdAsync((short)2024, 7).Returns(3);
        _batches.GetCasesByBatchIdAsync(3).Returns(new[] { new BatchCaseSummaryRecord("002600001", null) });
        var model = Create("2024", "7", "Pedigree");

        var result = await model.OnPostDownloadAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Reports/Pedigree");
    }
}
