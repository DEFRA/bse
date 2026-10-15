using System.Security.Claims;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
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
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>
/// Covers <see cref="BabModel"/>'s Save handler for a brand-new case. Previously, saving BAB with
/// no existing BAB row (the normal case when BAB is the first tab a new-case user actually saves)
/// bypassed cross-tab staging entirely and inserted BAB in its own standalone transaction — before
/// Case/Farm/CaseWork had been created, since CommitAllAsync is the only thing that creates them.
/// Reported symptom: Save redirects to Home as if successful, but BAB data is missing on revisit,
/// observed intermittently depending on whether another tab happened to create the case first.
/// </summary>
public sealed class BabModelHandlerTests
{
    private readonly IBabRepository _babRepository = Substitute.For<IBabRepository>();
    private readonly ICaseRepository _caseRepository = Substitute.For<ICaseRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();
    private readonly ICaseEditSessionStateService _sessionState = Substitute.For<ICaseEditSessionStateService>();
    private readonly ICaseEditOrchestrationService _caseEditOrchestration = Substitute.For<ICaseEditOrchestrationService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private const string Rbse = "002600001";

    public BabModelHandlerTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _caseRepository.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        _babRepository.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);
        _currentUser.GetUserIdAsync().Returns(1);
    }

    private BabModel CreateModel(string[]? roles = null)
    {
        var model = new BabModel(
            _babRepository, _caseRepository, _lookups, _batchRepository,
            _sessionState, Substitute.For<ICaseDraftDiscardService>(), _caseEditOrchestration, _currentUser,
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

    [Fact]
    public async Task OnPostSaveBabAsync_ForBrandNewCaseWithNoRowStamp_StagesBabAndCommitsViaOrchestration()
    {
        var model = CreateModel(["DataEntry"]);
        model.Origin = "N";
        model.Bab.Notes = "First BAB entry for a brand-new case";

        // A staged Case.BirthDate (e.g. from the DEFRA tab, not yet committed) is what makes BAB
        // editable for a case that doesn't exist in the DB yet — EvaluateLegacyBabEditPermission
        // requires BirthDate >= 18/07/1988 OR an existing BAB row; neither exists here except the
        // staged BirthDate.
        var stagedCase = new EditCaseCommand(
            Rbse: Rbse, EartagCountry: null, EartagHerdmark: null, Eartag: null, PreviousEartag: null,
            Bse1ReceivedDate: null, FormADate: null, FormAResubmittedDate: null, FormBDate: null,
            Fate: null, FormCDate: null, IsPurchaserBse1Received: false, IsBreederBse1Received: false,
            IsVendor1Bse1Received: false, IsHomebredBse1Received: false, IsSummarySheetReceived: false,
            IsPaperworkComplete: false, ReportedLocation: null, Survey: null, Notes: null,
            BirthDate: new DateTime(2020, 1, 1), IsBirthDateEst: null, DamStatus: null, BirthDateSource: null,
            ValuationAge: null, Sex: null, Breed: null, Origin: null, PurchaseDate: null,
            PurchaseAgeInMonths: null, PurchasedCounty: null, HerdEntryDate: null, OnsetDate: null,
            IsOnsetDateEst: null, MonthsPregnant: null, MonthsPostCalving: null, OnsetAgeInMonths: null,
            SlaughterDate: null, RowStamp: [], AlternateDiagnosis: null, LabComment: null, CaseType: null);

        CaseEditSessionState? stored = null;
        _sessionState.GetAsync(Arg.Any<string>()).Returns(_ => stored ?? new CaseEditSessionState { Rbse = Rbse, Case = stagedCase });
        _sessionState.SetAsync(Arg.Do<CaseEditSessionState>(d => stored = d)).Returns(Task.CompletedTask);
        _caseEditOrchestration.CommitAllAsync(Rbse, 1).Returns(CaseCommitOutcome.Success([]));

        var result = await model.OnPostSaveBabAsync(rowStampBase64: null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        stored.Should().NotBeNull();
        stored!.Bab.Should().NotBeNull();
        stored.Bab!.Notes.Should().Be("First BAB entry for a brand-new case");
        await _caseEditOrchestration.Received(1).CommitAllAsync(Rbse, 1);
        await _babRepository.DidNotReceive().AddAsync(
            Arg.Any<AddCaseBabCommand>(), Arg.Any<string?>(), Arg.Any<System.Data.IDbConnection>(), Arg.Any<System.Data.IDbTransaction>());
    }
}
