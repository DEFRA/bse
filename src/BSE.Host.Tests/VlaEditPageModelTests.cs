using System.Security.Claims;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Tests;

public sealed class VlaEditPageModelTests
{
    private const string Rbse = "002600001";
    private const string RowStampKey = "VlaEdit_RowStamp_" + Rbse;

    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batch = Substitute.For<IBatchRepository>();

    public VlaEditPageModelTests()
    {
        _user.GetUserIdAsync().Returns(3);
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "X", "Ex")]);
        _batch.GetBatchNumbersByRbseAsync(Rbse).Returns((IReadOnlyList<BatchNumberEntry>)[new BatchNumberEntry(1, "B1", Rbse, "BSE1")]);
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord
        {
            Rbse = Rbse, Cphh = "01001000101", FormADate = new DateTime(2020, 1, 1), BirthDate = new DateTime(2015, 1, 1), RowStamp = [7, 8]
        });
    }

    private VlaEditModel CreateModel(bool dataEntry = true, bool withRowStamp = true)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build();
        var model = new VlaEditModel(_caseService, _user, _lookups, _batch, config) { Rbse = Rbse };
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(dataEntry ? [new Claim(ClaimTypes.Role, "DataEntry")] : [], "Test"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        if (withRowStamp)
            model.TempData[RowStampKey] = Convert.ToBase64String(new byte[] { 7, 8 });
        model.Case.Rbse = Rbse;
        return model;
    }

    [Fact]
    public async Task OnGet_UnknownCase_RedirectsHomeWithWarning()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        var model = CreateModel();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        ((string)model.TempData["Warning"]!).Should().Contain(Rbse);
    }

    [Fact]
    public async Task OnGet_KnownCase_LoadsViewModelLookupsAndRowStamp()
    {
        var model = CreateModel(withRowStamp: false);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.Case.BirthDate.Should().Be(new DateTime(2015, 1, 1));
        model.BatchNumbers.Should().ContainSingle();
        model.SpolSiteUrl.Should().Be("http://spol");
        model.SexOptions.Should().NotBeEmpty();
        model.PurchasedCountyOptions.Should().NotBeEmpty();
        model.TempData[RowStampKey].Should().Be(Convert.ToBase64String(new byte[] { 7, 8 }));
    }

    [Fact]
    public async Task OnPost_WithoutDataEntry_Forbids()
    {
        (await CreateModel(dataEntry: false).OnPostAsync()).Should().BeOfType<ForbidResult>();
        await _caseService.DidNotReceiveWithAnyArgs().EditCaseAsync(default!, default);
    }

    [Fact]
    public async Task OnPost_Success_EditsCaseAndRedirects()
    {
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 3).Returns(EditCaseResult.Success);
        var model = CreateModel();
        model.Case.Origin = "P";
        model.Case.PurchaseDate = new DateTime(2017, 1, 1);
        model.Case.FormADate = new DateTime(2020, 1, 1);
        model.Case.FormBDate = new DateTime(2020, 6, 1);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["Success"].Should().Be($"Case {Rbse} has been updated.");
        await _caseService.Received(1).EditCaseAsync(
            Arg.Is<EditCaseDetailsCommand>(c => c.Case.Rbse == Rbse
                && c.Case.RowStamp.SequenceEqual(new byte[] { 7, 8 })
                && c.Case.PurchaseDate == new DateTime(2017, 1, 1)
                && c.Case.SlaughterDate == new DateTime(2020, 6, 1)),
            3);
    }

    [Fact]
    public async Task OnPost_NonPurchasedOrigin_ClearsPurchaseFields()
    {
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 3).Returns(EditCaseResult.Success);
        var model = CreateModel();
        model.Case.Origin = "HB";
        model.Case.PurchaseDate = new DateTime(2017, 1, 1);
        model.Case.PurchaseAgeInMonths = 9;
        model.Case.PurchasedCounty = "C";

        await model.OnPostAsync();

        await _caseService.Received(1).EditCaseAsync(
            Arg.Is<EditCaseDetailsCommand>(c => c.Case.PurchaseDate == null && c.Case.PurchaseAgeInMonths == null && c.Case.PurchasedCounty == null), 3);
    }

    [Fact]
    public async Task OnPost_MissingRowStamp_ReportsConcurrencyError()
    {
        var model = CreateModel(withRowStamp: false);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Contain("Session expired");
        await _caseService.DidNotReceiveWithAnyArgs().EditCaseAsync(default!, default);
    }

    [Fact]
    public async Task OnPost_ConcurrencyConflict_ReportsErrorAndRefreshesRowStamp()
    {
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 3).Returns(EditCaseResult.ConcurrencyConflict);
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, RowStamp = [9, 9] });
        var model = CreateModel();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ConcurrencyError.Should().Contain("Another user has modified");
        model.TempData[RowStampKey].Should().Be(Convert.ToBase64String(new byte[] { 9, 9 }));
    }

    [Fact]
    public async Task OnPost_ConcurrencyConflictAndCaseGone_KeepsOldRowStamp()
    {
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 3).Returns(EditCaseResult.ConcurrencyConflict);
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        var model = CreateModel();

        await model.OnPostAsync();

        model.ConcurrencyError.Should().NotBeNull();
        model.TempData[RowStampKey].Should().Be(Convert.ToBase64String(new byte[] { 7, 8 }));
    }

    [Theory]
    [InlineData(EditCaseResult.RbseNotFound, "not found")]
    [InlineData(EditCaseResult.AuditLogError, "Audit log error")]
    [InlineData(EditCaseResult.PostUpdateError, "Database error")]
    public async Task OnPost_OtherFailures_AddModelError(EditCaseResult failure, string expected)
    {
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 3).Returns(failure);
        var model = CreateModel();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[""]!.Errors.Single().ErrorMessage.Should().Contain(expected);
    }

    [Fact]
    public async Task OnPost_UnmappedFailure_UsesGenericMessage()
    {
        _caseService.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), 3).Returns((EditCaseResult)99);
        var model = CreateModel();

        await model.OnPostAsync();

        model.ModelState[""]!.Errors.Single().ErrorMessage.Should().StartWith("Update failed");
    }

    public static TheoryData<string, string> InvalidScenarios => new()
    {
        { "BirthBefore1970", "Case.BirthDate" },
        { "BirthNotBeforeFormA", "Case.BirthDate" },
        { "BirthNotPastWithoutFormA", "Case.BirthDate" },
        { "PurchaseBeforeBirth", "Case.PurchaseDate" },
        { "PurchaseNotBeforeFormA", "Case.PurchaseDate" },
        { "PurchaseNotPastWithoutFormA", "Case.PurchaseDate" },
        { "HerdEntryAfterFormA", "Case.HerdEntryDate" },
        { "HerdEntryFutureWithoutFormA", "Case.HerdEntryDate" },
        { "OnsetBeforeBirth", "Case.OnsetDate" },
        { "OnsetAfterFormA", "Case.OnsetDate" },
        { "OnsetFutureWithoutFormA", "Case.OnsetDate" },
        { "PregnantAndPostCalving", "Case.MonthsPostCalving" },
        { "PregnantOutOfRange", "Case.MonthsPregnant" },
        { "PostCalvingOutOfRange", "Case.MonthsPostCalving" },
        { "SlaughterFuture", "Case.SlaughterDate" },
        { "SlaughterBeforeFormA", "Case.SlaughterDate" },
        { "SlaughterBeforeBirthWithoutFormA", "Case.SlaughterDate" },
    };

    [Theory]
    [MemberData(nameof(InvalidScenarios))]
    public async Task OnPost_InvalidDomainRules_ReturnPageWithModelError(string scenario, string key)
    {
        var model = CreateModel();
        var c = model.Case;
        c.Origin = "P";
        c.FormADate = new DateTime(2020, 1, 1);
        switch (scenario)
        {
            case "BirthBefore1970": c.BirthDate = new DateTime(1960, 1, 1); break;
            case "BirthNotBeforeFormA": c.BirthDate = new DateTime(2020, 1, 1); break;
            case "BirthNotPastWithoutFormA": c.FormADate = null; c.BirthDate = DateTime.Today.AddDays(1); break;
            case "PurchaseBeforeBirth": c.BirthDate = new DateTime(2015, 1, 1); c.PurchaseDate = new DateTime(2014, 1, 1); break;
            case "PurchaseNotBeforeFormA": c.PurchaseDate = new DateTime(2020, 1, 1); break;
            case "PurchaseNotPastWithoutFormA": c.FormADate = null; c.PurchaseDate = DateTime.Today; break;
            case "HerdEntryAfterFormA": c.HerdEntryDate = new DateTime(2021, 1, 1); break;
            case "HerdEntryFutureWithoutFormA": c.FormADate = null; c.HerdEntryDate = DateTime.Today.AddDays(2); break;
            case "OnsetBeforeBirth": c.BirthDate = new DateTime(2015, 1, 1); c.OnsetDate = new DateTime(2014, 1, 1); break;
            case "OnsetAfterFormA": c.OnsetDate = new DateTime(2021, 1, 1); break;
            case "OnsetFutureWithoutFormA": c.FormADate = null; c.OnsetDate = DateTime.Today.AddDays(2); break;
            case "PregnantAndPostCalving": c.MonthsPregnant = 2; c.MonthsPostCalving = 1; break;
            case "PregnantOutOfRange": c.MonthsPregnant = 10; break;
            case "PostCalvingOutOfRange": c.MonthsPostCalving = 4; break;
            case "SlaughterFuture": c.SlaughterDate = DateTime.Today.AddDays(3); break;
            case "SlaughterBeforeFormA": c.SlaughterDate = new DateTime(2019, 1, 1); break;
            case "SlaughterBeforeBirthWithoutFormA": c.FormADate = null; c.BirthDate = new DateTime(2015, 1, 1); c.SlaughterDate = new DateTime(2014, 1, 1); break;
        }

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(key);
        model.BatchNumbers.Should().ContainSingle();
        await _caseService.DidNotReceiveWithAnyArgs().EditCaseAsync(default!, default);
    }
}
