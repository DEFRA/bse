using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using CaseWorkRow = BSE.Modules.CaseWork.Models.CaseWorkRecord;

namespace BSE.Host.Tests;

public sealed class DefraEditPageModelTests
{
    private const string Rbse = "002600001";
    private const string RowStampKey = "DefraEdit_RowStamp_" + Rbse;
    private static readonly DateTime Today = DateTime.Today;

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly ICaseWorkRepository _caseWork = Substitute.For<ICaseWorkRepository>();
    private readonly IBatchRepository _batch = Substitute.For<IBatchRepository>();
    private CaseRecord? _record = new() { Rbse = Rbse, Cphh = "01001000101", FormADate = Today.AddDays(-50), RowStamp = [1, 2] };

    public DefraEditPageModelTests()
    {
        _cases.GetCaseAsync(Rbse).Returns(_ => _record);
        _user.GetUserIdAsync().Returns(7);
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Task.FromResult<IEnumerable<LookupItem>>([new LookupItem(1, "X", "Ex")]));
        _batch.GetBatchNumbersByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<BatchNumberEntry>>([new BatchNumberEntry(1, "B1", Rbse, "BSE1")]));
        _cases.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), Arg.Any<int>()).Returns(EditCaseResult.Success);
    }

    private DefraEditModel CreateModel(bool rowStamp = true)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build();
        var model = new DefraEditModel(_cases, _user, _lookups, _caseWork, _batch, config) { Rbse = Rbse };
        PageTestContext.Attach(model);
        if (rowStamp) model.TempData[RowStampKey] = Convert.ToBase64String(new byte[] { 1, 2 });
        return model;
    }

    private static void MakeValid(DefraEditModel model)
    {
        model.Case.EartagCountry = "UK";
        model.Case.EartagHerdmark = "MN123";
        model.Case.Eartag = "1";
        model.Case.FormADate = Today.AddDays(-50);
        model.Case.BirthDate = Today.AddDays(-1000);
    }

    [Fact]
    public async Task OnGet_UnknownCase_WarnsAndRedirectsHome()
    {
        _record = null;
        var model = CreateModel(false);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["Warning"].Should().Be($"Case '{Rbse}' not found.");
    }

    [Fact]
    public async Task OnGet_Found_PopulatesViewModelLookupsAndBatches()
    {
        _record = new CaseRecord { Rbse = Rbse, Cphh = "01001000101", IsNonGbCase = true, FormADate = Today.AddDays(-5), RowStamp = [1, 2] };
        _caseWork.GetByRbseAsync(Rbse).Returns(new CaseWorkRow { Rbse = Rbse, Barcode = "BC", AhfReference = "AHF" });
        var model = CreateModel(false);

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.SpolSiteUrl.Should().Be("http://spol");
        model.IsNonGbCase.Should().BeTrue();
        model.Case.Rbse.Should().Be(Rbse);
        model.Case.HasCaseWork.Should().BeTrue();
        model.Case.Barcode.Should().Be("BC");
        model.BatchNumbers.Should().HaveCount(1);
        model.FateOptions.Should().HaveCount(1);
        model.CaseTypeOptions.Should().HaveCount(1);
        model.TempData[RowStampKey].Should().Be(Convert.ToBase64String(new byte[] { 1, 2 }));
    }

    [Fact]
    public async Task OnGet_NoCaseWork_LeavesHasCaseWorkFalse()
    {
        var model = CreateModel(false);

        await model.OnGetAsync();

        model.Case.HasCaseWork.Should().BeFalse();
    }

    [Fact]
    public async Task OnPost_UnknownCase_RedirectsHome()
    {
        _record = null;
        var model = CreateModel();

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        model.TempData["Warning"].Should().Be($"Case '{Rbse}' not found.");
    }

    [Fact]
    public async Task OnPost_Valid_EditsCaseAndRedirects()
    {
        var model = CreateModel();
        MakeValid(model);

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Edit");
        await _cases.Received(1).EditCaseAsync(
            Arg.Is<EditCaseDetailsCommand>(c => c.Case.RowStamp.SequenceEqual(new byte[] { 1, 2 })), 7);
        await _caseWork.DidNotReceiveWithAnyArgs().EditAsync(default!);
        model.TempData["Success"].Should().Be($"Case {Rbse} has been updated.");
    }

    [Fact]
    public async Task OnPost_Valid_WithCaseWork_EditsCaseWork()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.HasCaseWork = true;
        model.Case.Barcode = "BC1";

        await model.OnPostAsync();

        await _caseWork.Received(1).EditAsync(Arg.Is<EditCaseWorkCommand>(c => c.Rbse == Rbse && c.Barcode == "BC1"));
    }

    [Fact]
    public async Task OnPost_MissingRowStamp_ReportsSessionExpired()
    {
        var model = CreateModel(false);
        MakeValid(model);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ConcurrencyError.Should().Contain("Session expired");
        await _cases.DidNotReceiveWithAnyArgs().EditCaseAsync(default!, default);
    }

    [Fact]
    public async Task OnPost_ConcurrencyConflict_ReportsAndRefreshesRowStamp()
    {
        _cases.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), Arg.Any<int>()).Returns(EditCaseResult.ConcurrencyConflict);
        _record = new CaseRecord { Rbse = Rbse, FormADate = Today.AddDays(-50), RowStamp = [9] };
        var model = CreateModel();
        MakeValid(model);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ConcurrencyError.Should().Contain("Another user has modified");
        model.TempData[RowStampKey].Should().Be(Convert.ToBase64String(new byte[] { 9 }));
    }

    [Theory]
    [InlineData(EditCaseResult.RbseNotFound, "not found")]
    [InlineData(EditCaseResult.AuditLogError, "Audit log error during update.")]
    [InlineData(EditCaseResult.PostUpdateError, "Database error after update.")]
    public async Task OnPost_OtherFailures_AddModelError(EditCaseResult failure, string expected)
    {
        _cases.EditCaseAsync(Arg.Any<EditCaseDetailsCommand>(), Arg.Any<int>()).Returns(failure);
        var model = CreateModel();
        MakeValid(model);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState[""]!.Errors[0].ErrorMessage.Should().Contain(expected);
    }

    [Fact]
    public async Task OnPost_NoEartag_AddsError()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.EartagCountry = null;
        model.Case.EartagHerdmark = null;
        model.Case.Eartag = null;

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState["Case.EartagCountry"]!.Errors[0].ErrorMessage.Should().Be("Enter an eartag.");
    }

    [Fact]
    public async Task OnPost_InvalidEartag_AddsValidatorError()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.Eartag = "bad";

        await model.OnPostAsync();

        model.ModelState.ContainsKey("Case.EartagCountry").Should().BeTrue();
    }

    [Fact]
    public async Task OnPost_NoFormA_GbCase_RequiresFormAAndClearsDependents()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormADate = null;
        model.Case.FormAResubmittedDate = Today.AddDays(-1);
        model.Case.FormBDate = Today.AddDays(-2);
        model.Case.FormCDate = Today.AddDays(-2);
        model.Case.Fate = "X";

        await model.OnPostAsync();

        model.ModelState["Case.FormADate"]!.Errors[0].ErrorMessage.Should().Be("Enter a Form A date.");
        model.Case.FormBDate.Should().BeNull();
        model.Case.FormAResubmittedDate.Should().BeNull();
        model.Case.FormCDate.Should().BeNull();
        model.Case.Fate.Should().BeNull();
    }

    [Fact]
    public async Task OnPost_NonGbCase_FormADateIsTakenFromRecord()
    {
        _record = new CaseRecord { Rbse = Rbse, IsNonGbCase = true, FormADate = null, RowStamp = [1, 2] };
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormADate = Today.AddDays(-3);

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();

        model.Case.FormADate.Should().BeNull();
        model.IsNonGbCase.Should().BeTrue();
    }

    [Fact]
    public async Task OnPost_FormADateWithoutFormB_ClearsFormCAndFate()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormCDate = Today.AddDays(-1);
        model.Case.Fate = "F";

        await model.OnPostAsync();

        model.Case.FormCDate.Should().BeNull();
        model.Case.Fate.Should().BeNull();
    }

    [Fact]
    public async Task OnPost_FormBWithoutSlaughter_DefaultsSlaughterToFormB()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormBDate = Today.AddDays(-10);
        model.Case.Fate = "F";

        await model.OnPostAsync();

        model.Case.SlaughterDate.Should().Be(Today.AddDays(-10));
    }

    [Fact]
    public async Task OnPost_FutureBse1ReceivedDate_IsInvalid()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.Bse1ReceivedDate = Today.AddDays(5);

        await model.OnPostAsync();

        model.ModelState["Case.Bse1ReceivedDate"]!.Errors[0].ErrorMessage.Should().Be("Invalid Date");
    }

    [Fact]
    public async Task OnPost_FormAAfterSlaughter_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.SlaughterDate = Today.AddDays(-80);

        await model.OnPostAsync();

        model.ModelState["Case.FormADate"]!.Errors[0].ErrorMessage.Should().Contain("before the slaughter date");
    }

    [Fact]
    public async Task OnPost_FormAInFuture_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormADate = Today.AddDays(3);

        await model.OnPostAsync();

        model.ModelState["Case.FormADate"]!.Errors[0].ErrorMessage.Should().Be("Form A date must be a past date.");
    }

    [Fact]
    public async Task OnPost_ResubmittedNotAfterFormA_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormAResubmittedDate = model.Case.FormADate;

        await model.OnPostAsync();

        model.ModelState["Case.FormAResubmittedDate"]!.Errors[0].ErrorMessage.Should().Contain("must be after the Form A date");
    }

    [Fact]
    public async Task OnPost_ResubmittedInFuture_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormAResubmittedDate = Today.AddDays(2);

        await model.OnPostAsync();

        model.ModelState["Case.FormAResubmittedDate"]!.Errors[0].ErrorMessage.Should().Contain("past date");
    }

    [Fact]
    public async Task OnPost_FormBNotAfterFormA_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormBDate = model.Case.FormADate;
        model.Case.Fate = "F";

        await model.OnPostAsync();

        model.ModelState["Case.FormBDate"]!.Errors[0].ErrorMessage.Should().Contain("must be after the Form A date");
    }

    [Fact]
    public async Task OnPost_FormBInFuture_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormBDate = Today.AddDays(4);
        model.Case.Fate = "F";
        model.Case.SlaughterDate = Today.AddDays(5);

        await model.OnPostAsync();

        model.ModelState["Case.FormBDate"]!.Errors[0].ErrorMessage.Should().Contain("past date");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("-1")]
    public async Task OnPost_FormBWithoutFate_IsRejected(string? fate)
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormBDate = Today.AddDays(-10);
        model.Case.Fate = fate;

        await model.OnPostAsync();

        model.ModelState["Case.Fate"]!.Errors[0].ErrorMessage.Should().Contain("Fate");
    }

    [Fact]
    public async Task OnPost_FormCInFuture_IsInvalid()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.FormBDate = Today.AddDays(-10);
        model.Case.Fate = "F";
        model.Case.FormCDate = Today.AddDays(3);

        await model.OnPostAsync();

        model.ModelState["Case.FormCDate"]!.Errors[0].ErrorMessage.Should().Be("Invalid Date");
    }

    [Fact]
    public async Task OnPost_BirthBefore1970_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.BirthDate = new DateTime(1969, 12, 31);

        await model.OnPostAsync();

        model.ModelState["Case.BirthDate"]!.Errors[0].ErrorMessage.Should().Contain("1 January 1970");
    }

    [Fact]
    public async Task OnPost_BirthNotBeforeFormA_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.BirthDate = model.Case.FormADate;

        await model.OnPostAsync();

        model.ModelState["Case.BirthDate"]!.Errors[0].ErrorMessage.Should().Be("Date of birth must be before the Form A date.");
    }

    [Fact]
    public async Task OnPost_BirthNotBeforePurchaseOrOnset_IsRejected()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.PurchaseDate = model.Case.BirthDate;
        model.Case.OnsetDate = model.Case.BirthDate;

        await model.OnPostAsync();

        model.ModelState["Case.BirthDate"]!.Errors.Select(e => e.ErrorMessage)
            .Should().Contain(["Date of birth must be before the purchase date.", "Date of birth must be before the onset date."]);
    }

    [Fact]
    public async Task OnPost_Bse1ReceiptDates_AreCheckedAgainstRbseDate()
    {
        var model = CreateModel();
        MakeValid(model);
        model.Case.HasCaseWork = true;
        model.Case.RbseDate = Today.AddDays(-20);
        model.Case.PurchaserBse1ReceivedDate = Today.AddDays(-30);
        model.Case.BreederBse1ReceivedDate = Today.AddDays(5);
        model.Case.HomebredBse1ReceivedDate = Today.AddDays(-10);

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState["Case.PurchaserBse1ReceivedDate"]!.Errors[0].ErrorMessage.Should().Contain("must be after the RBSE date");
        model.ModelState["Case.BreederBse1ReceivedDate"]!.Errors[0].ErrorMessage.Should().Contain("past date");
        model.ModelState.ContainsKey("Case.HomebredBse1ReceivedDate").Should().BeFalse();
        await _cases.DidNotReceiveWithAnyArgs().EditCaseAsync(default!, default);
    }
}
