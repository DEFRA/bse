using System.Security.Claims;
using BSE.Host.Pages;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OwnerRecord = BSE.Modules.CaseManagement.Models.OtherOwnerRecord;

namespace BSE.Modules.UserManagement.Tests;

public sealed class CaseTestPagesTests
{
    private const string Rbse = "002600001";

    private readonly ITestRepository _tests = Substitute.For<ITestRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    public CaseTestPagesTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(new[] { new LookupItem { Code = "X", Description = "X" } });
        _tests.GetByRbseAsync(Rbse).Returns(new[] { new CaseTestRecord(3, Rbse, "T1", "Type", "Neg", "Negative", [4]) });
    }

    [Fact]
    public async Task Edit_Post_RecordOfAnotherCase_ReturnsNotFound()
    {
        _tests.GetByRbseAsync(Rbse).Returns(Array.Empty<CaseTestRecord>());
        var m = Edit();
        m.TestType = "T1";

        (await m.OnPostAsync()).Should().BeOfType<NotFoundResult>();
        await _tests.DidNotReceiveWithAnyArgs().EditAsync(default!);
    }

    private static void Wire(PageModel m)
    {
        var http = new DefaultHttpContext();
        m.PageContext = new PageContext { HttpContext = http };
        m.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
    }

    private CaseTestAddModel Add(string? from = null)
    {
        var m = new CaseTestAddModel(_tests, _lookups) { Rbse = "00/26/00001", From = from };
        Wire(m);
        return m;
    }

    private CaseTestEditModel Edit()
    {
        var m = new CaseTestEditModel(_tests, _lookups) { Rbse = Rbse, Id = 3 };
        Wire(m);
        return m;
    }

    [Fact]
    public async Task Add_OnGet_LoadsLookups()
    {
        var m = Add();

        (await m.OnGetAsync()).Should().BeOfType<PageResult>();
        m.TestTypeOptions.Should().NotBeEmpty();
        m.TestResultOptions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Add_Post_NoType_ReturnsPageWithError()
    {
        var m = Add();

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        m.ModelState.ContainsKey("TestType").Should().BeTrue();
        await _tests.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Theory]
    [InlineData("defra", "/Case/Edit")]
    [InlineData("DEFRA", "/Case/Edit")]
    [InlineData(null, "/Case/TestResults")]
    public async Task Add_Post_Valid_SavesAndRedirects(string? from, string expectedPage)
    {
        var m = Add(from);
        m.TestType = "T1";
        m.TestResult = "Neg";

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be(expectedPage);
        await _tests.Received(1).AddAsync(Arg.Is<AddTestCommand>(c => c.Rbse == "002600001" && c.TestType == "T1"));
        m.TempData["Success"].Should().Be("Test record added.");
    }

    [Fact]
    public async Task Edit_OnGet_NotFound_Redirects()
    {
        _tests.GetByRbseAsync(Rbse).Returns(Array.Empty<CaseTestRecord>());

        (await Edit().OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Edit");
    }

    [Fact]
    public async Task Edit_OnGet_Found_Populates()
    {
        _tests.GetByRbseAsync(Rbse).Returns(new[] { new CaseTestRecord(3, Rbse, "T1", "Type", "Neg", "Negative", [4]) });
        var m = Edit();

        (await m.OnGetAsync()).Should().BeOfType<PageResult>();

        m.TestType.Should().Be("T1");
        m.TestResult.Should().Be("Neg");
        m.RowStampBase64.Should().Be(Convert.ToBase64String([4]));
    }

    [Fact]
    public async Task Edit_Post_NoType_ReturnsPage()
    {
        var m = Edit();

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        await _tests.DidNotReceiveWithAnyArgs().EditAsync(default!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("BA==")]
    public async Task Edit_Post_Valid_SavesAndRedirects(string rowStamp)
    {
        var m = Edit();
        m.TestType = "T1";
        m.RowStampBase64 = rowStamp;

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Edit");
        await _tests.Received(1).EditAsync(Arg.Is<EditTestCommand>(c => c.Id == 3));
        m.TempData["Success"].Should().Be("Test record updated.");
    }
}

public sealed class DeleteCasePageTests
{
    private const string Rbse = "002600001";
    private const string Cphh = "01001000101";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly IBatchService _batches = Substitute.For<IBatchService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public DeleteCasePageTests()
    {
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns(Array.Empty<BatchNumberEntry>());
        _user.GetUserIdAsync().Returns(5);
    }

    private DeleteModel Create(string rbse = Rbse)
    {
        var m = new DeleteModel(_cases, _farms, _batches, _user, NullLogger<DeleteModel>.Instance) { Rbse = rbse };
        var http = new DefaultHttpContext();
        m.PageContext = new PageContext { HttpContext = http };
        m.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return m;
    }

    private void GivenCase(bool vlaData = false, int casesOnFarm = 2)
    {
        var details = new CaseDetailRecord(
            new CaseRecord { Rbse = Rbse, Cphh = Cphh, EartagHerdmark = "AB", Eartag = "12" },
            null, null,
            vlaData ? [new OwnerRecord(1, Rbse, "T", "N", null, null)] : [],
            [], [], null, null, [], [], null);
        _cases.GetCaseDetailsAsync(Rbse).Returns(details);
        _farms.GetByCphhAsync(Cphh).Returns(new FarmRecord { CPHH = Cphh });
        _farms.GetCaseCountByCphhAsync(Cphh).Returns(casesOnFarm);
    }

    [Fact]
    public async Task OnGet_BlankRbse_DoesNotLookUp()
    {
        (await Create("").OnGetAsync()).Should().BeOfType<PageResult>();
        await _cases.DidNotReceiveWithAnyArgs().GetCaseDetailsAsync(default!);
    }

    [Fact]
    public async Task OnGet_Found_LoadsFarmAndFlags()
    {
        GivenCase(casesOnFarm: 1);
        var m = Create();

        await m.OnGetAsync();

        m.CaseFound.Should().BeTrue();
        m.CanDelete.Should().BeTrue();
        m.FarmWillBeDeleted.Should().BeTrue();
        m.Farm.Should().NotBeNull();
        m.EartagDisplay.Should().Be("AB 12");
    }

    [Fact]
    public async Task LookUp_VlaData_BlocksDelete()
    {
        GivenCase(vlaData: true);
        var m = Create();

        await m.OnPostLookUpAsync();

        m.HasVlaData.Should().BeTrue();
        m.CanDelete.Should().BeFalse();
    }

    [Fact]
    public async Task LookUp_BatchLinks_CountAsVlaData()
    {
        GivenCase();
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns(new[] { new BatchNumberEntry(1, "B", Rbse, null) });
        var m = Create();

        await m.OnPostLookUpAsync();

        m.HasVlaData.Should().BeTrue();
    }

    [Fact]
    public async Task LookUp_BlankAndMissing_SetNotFound()
    {
        var blank = Create("");
        await blank.OnPostLookUpAsync();
        blank.RbseError.Should().Be(DeleteModel.CaseNotFoundMessage);

        _cases.GetCaseDetailsAsync(Rbse).Returns((CaseDetailRecord?)null);
        var missing = Create();
        await missing.OnPostLookUpAsync();
        missing.RbseError.Should().Be(DeleteModel.CaseNotFoundMessage);
        missing.EartagDisplay.Should().BeEmpty();
    }

    [Fact]
    public async Task LookUp_ServiceThrows_SetsLoadFailed()
    {
        _cases.GetCaseDetailsAsync(Rbse).Returns<CaseDetailRecord?>(_ => throw new InvalidOperationException());
        var m = Create();

        await m.OnPostLookUpAsync();

        m.ErrorMessage.Should().Be(DeleteModel.LoadFailedMessage);
    }

    [Fact]
    public async Task Delete_CaseNotFound_ReturnsPage()
    {
        _cases.GetCaseDetailsAsync(Rbse).Returns((CaseDetailRecord?)null);

        (await Create().OnPostDeleteAsync()).Should().BeOfType<PageResult>();
        await _cases.DidNotReceiveWithAnyArgs().DeleteCaseAsync(default!, default);
    }

    [Fact]
    public async Task Delete_VlaData_IsRejectedServerSide()
    {
        GivenCase(vlaData: true);
        var m = Create();

        (await m.OnPostDeleteAsync()).Should().BeOfType<PageResult>();

        m.ErrorMessage.Should().Be(DeleteModel.VlaDataMessage);
    }

    [Fact]
    public async Task Delete_ServiceThrows_SetsDeleteFailed()
    {
        GivenCase();
        _cases.DeleteCaseAsync(Rbse, 5).Returns<DeleteCaseResult>(_ => throw new InvalidOperationException());
        var m = Create();

        await m.OnPostDeleteAsync();

        m.ErrorMessage.Should().Be(DeleteModel.DeleteFailedMessage);
    }

    [Theory]
    [InlineData(DeleteCaseResult.RbseNotFound, DeleteModel.CaseNotFoundMessage)]
    [InlineData(DeleteCaseResult.HasLinkedRecords, DeleteModel.VlaDataMessage)]
    [InlineData(DeleteCaseResult.DeleteError, DeleteModel.DeleteFailedMessage)]
    public async Task Delete_FailureResults_SetMessage(DeleteCaseResult result, string expected)
    {
        GivenCase();
        _cases.DeleteCaseAsync(Rbse, 5).Returns(result);
        var m = Create();

        (await m.OnPostDeleteAsync()).Should().BeOfType<PageResult>();

        m.ErrorMessage.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "only case")]
    [InlineData(3, "Other cases remain")]
    public async Task Delete_Success_RedirectsWithFarmMessage(int casesOnFarm, string fragment)
    {
        GivenCase(casesOnFarm: casesOnFarm);
        _cases.DeleteCaseAsync(Rbse, 5).Returns(DeleteCaseResult.Success);
        var m = Create();

        var result = await m.OnPostDeleteAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        m.TempData[MaintenanceConfirmationModel.MessageKey]!.ToString().Should().Contain(fragment);
        m.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("Case deleted");
    }
}
