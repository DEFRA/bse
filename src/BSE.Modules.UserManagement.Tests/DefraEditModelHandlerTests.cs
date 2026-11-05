using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.CaseWork.Repositories;
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
/// Covers <see cref="DefraEditModel"/> (Case/DefraEdit — the embeddable DEFRA edit panel).
/// OnPostAsync had 0% coverage: no test in the suite instantiated this page model before.
/// </summary>
public sealed class DefraEditModelHandlerTests
{
    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ICaseWorkRepository _caseWorkRepository = Substitute.For<ICaseWorkRepository>();
    private readonly IBatchRepository _batchRepository = Substitute.For<IBatchRepository>();

    private const string Rbse = "002600001";
    private static string RowStampTempDataKey => $"DefraEdit_RowStamp_{Rbse}";

    public DefraEditModelHandlerTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _batchRepository.GetBatchNumbersByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<BatchNumberEntry>());
        _caseWorkRepository.GetByRbseAsync(Arg.Any<string>()).Returns((BSE.Modules.CaseWork.Models.CaseWorkRecord?)null);
        _currentUserService.GetUserIdAsync().Returns(7);
    }

    private DefraEditModel CreateModel()
    {
        var model = new DefraEditModel(
            _caseService, _currentUserService, _lookups, _caseWorkRepository,
            _batchRepository, new ConfigurationBuilder().AddInMemoryCollection().Build())
        {
            Rbse = Rbse
        };

        var httpContext = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = httpContext };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static CaseRecord MakeValidCaseRecord() => new()
    {
        Rbse = Rbse,
        Cphh = "12345678901",
        IsNonGbCase = false,
        FormADate = new DateTime(2024, 1, 1),
        RowStamp = [1, 2, 3]
    };

    // Uncovered path: OnGetAsync's "case not found" branch — redirects home with a warning.
    [Fact]
    public async Task OnGetAsync_WhenCaseNotFound_RedirectsHomeWithWarning()
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var result = await model.OnGetAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Home");
        model.TempData["Warning"].Should().Be($"Case '{Rbse}' not found.");
    }

    // Uncovered path: OnGetAsync's "case found" branch — populates Case and RowStamp.
    [Fact]
    public async Task OnGetAsync_WhenCaseFound_PopulatesCaseFromRecord()
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.IsNonGbCase.Should().BeFalse();
        model.TempData[RowStampTempDataKey].Should().NotBeNull();
    }

    // Uncovered path: OnPostAsync's "case not found" branch.
    [Fact]
    public async Task OnPostAsync_WhenCaseNotFound_RedirectsHomeWithoutSaving()
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Home");
        await _caseService.DidNotReceive().EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), Arg.Any<int>());
    }

    // Uncovered path: ValidateDomainRules's "Enter a Form A date" rule for a GB case,
    // and the "!ModelState.IsValid -> return Page()" short-circuit.
    [Fact]
    public async Task OnPostAsync_WhenFormADateMissingForGbCase_FailsValidationWithoutSaving()
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.Case = new CaseEditViewModel { Rbse = Rbse, FormADate = null };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("Case.FormADate");
        model.ModelState["Case.FormADate"]!.Errors.Should().Contain(e => e.ErrorMessage == "Enter a Form A date.");
        await _caseService.DidNotReceive().EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), Arg.Any<int>());
    }

    // Uncovered path: OnPostAsync's "session expired" branch.
    [Fact]
    public async Task OnPostAsync_WhenRowStampMissingFromTempData_SetsConcurrencyErrorAndReturnsPage()
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.Case = new CaseEditViewModel
        {
            Rbse = Rbse,
            FormADate = new DateTime(2024, 1, 1),
            Eartag = "1",
            EartagHerdmark = "GY1",
            EartagCountry = "UK"
        };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Be("Session expired — please reload the page and try again.");
    }

    // Uncovered path: the happy-path save pipeline — builds the command, saves, redirects
    // to /Case/Edit (not self, unlike EditModel).
    [Fact]
    public async Task OnPostAsync_WhenValid_SavesAndRedirectsToCaseEdit()
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.TempData[RowStampTempDataKey] = Convert.ToBase64String([1, 2, 3]);
        model.Case = new CaseEditViewModel
        {
            Rbse = Rbse,
            FormADate = new DateTime(2024, 1, 1),
            Eartag = "1",
            EartagHerdmark = "GY1",
            EartagCountry = "UK"
        };
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 7).Returns(EditCaseResult.Success);

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Edit");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
        model.TempData["Success"].Should().Be($"Case {Rbse} has been updated.");
    }

    [Fact]
    public void EmbedFlag_RoundTrips()
    {
        var model = CreateModel();
        model.Embed = true;

        model.Embed.Should().BeTrue();
    }

    [Fact]
    public async Task OnGetAsync_WithPersistedCaseWork_AppliesItToTheCase()
    {
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        _caseWorkRepository.GetByRbseAsync(Rbse).Returns(new BSE.Modules.CaseWork.Models.CaseWorkRecord { Rbse = Rbse, Barcode = "BC" });
        var model = CreateModel();

        await model.OnGetAsync();

        model.Case.Barcode.Should().Be("BC");
    }

    [Fact]
    public async Task OnPostAsync_ForANonGbCase_KeepsThePersistedFormADate()
    {
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord() with { IsNonGbCase = true });
        var model = CreateModel();
        model.Case = new CaseEditViewModel { Rbse = Rbse, FormADate = null };

        await model.OnPostAsync();

        model.Case.FormADate.Should().Be(new DateTime(2024, 1, 1));
    }

    [Theory]
    [InlineData(EditCaseResult.RbseNotFound, "not found")]
    [InlineData(EditCaseResult.AuditLogError, "Audit log error")]
    [InlineData(EditCaseResult.PostUpdateError, "Database error")]
    [InlineData((EditCaseResult)999, "Update failed")]
    public async Task OnPostAsync_WhenTheSaveFails_ShowsAMessage(EditCaseResult failure, string expected)
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.TempData[RowStampTempDataKey] = Convert.ToBase64String([1, 2, 3]);
        model.Case = new CaseEditViewModel { Rbse = Rbse, FormADate = new DateTime(2024, 1, 1), Eartag = "1", EartagHerdmark = "GY1", EartagCountry = "UK" };
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 7).Returns(failure);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState[string.Empty]!.Errors.Should().Contain(e => e.ErrorMessage.Contains(expected));
    }

    [Fact]
    public async Task OnPostAsync_WhenTheCaseHasCaseWork_SavesItToo()
    {
        var model = CreateModel();
        _caseService.GetCaseAsync(Rbse).Returns(MakeValidCaseRecord());
        model.TempData[RowStampTempDataKey] = Convert.ToBase64String([1, 2, 3]);
        model.Case = new CaseEditViewModel
        {
            Rbse = Rbse, FormADate = new DateTime(2024, 1, 1), Eartag = "1", EartagHerdmark = "GY1", EartagCountry = "UK",
            HasCaseWork = true, Barcode = "BC"
        };
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 7).Returns(EditCaseResult.Success);

        await model.OnPostAsync();

        await _caseWorkRepository.Received(1).EditAsync(Arg.Is<BSE.Modules.CaseWork.Commands.EditCaseWorkCommand>(c => c.Barcode == "BC"));
    }

    // Uncovered path: OnPostAsync's ConcurrencyConflict branch.
    [Fact]
    public async Task OnPostAsync_WhenConcurrencyConflict_SetsErrorAndRefreshesRowStamp()
    {
        var model = CreateModel();
        var original = MakeValidCaseRecord();
        var refreshed = original with { RowStamp = [9, 9, 9] };
        _caseService.GetCaseAsync(Rbse).Returns(original, refreshed);
        model.TempData[RowStampTempDataKey] = Convert.ToBase64String([1, 2, 3]);
        model.Case = new CaseEditViewModel
        {
            Rbse = Rbse,
            FormADate = new DateTime(2024, 1, 1),
            Eartag = "1",
            EartagHerdmark = "GY1",
            EartagCountry = "UK"
        };
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 7).Returns(EditCaseResult.ConcurrencyConflict);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Contain("Another user has modified this case");
        model.TempData[RowStampTempDataKey].Should().Be(Convert.ToBase64String([9, 9, 9]));
    }
}
