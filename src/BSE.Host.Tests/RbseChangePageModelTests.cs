using BSE.Host.Pages;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;

namespace BSE.Host.Tests;

public sealed class RbseChangePageModelTests
{
    private const string Old = "002600001";
    private const string New = "002600002";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public RbseChangePageModelTests()
    {
        _user.GetUserIdAsync().Returns(2);
        _cases.GetCaseAsync(Old).Returns(new CaseRecord { Rbse = Old, Cphh = "01001000101", EartagHerdmark = "HM", Eartag = "9" });
        _farms.GetByCphhAsync("01001000101").Returns(new FarmRecord { CPHH = "01001000101" });
    }

    private RbseChangeModel CreateModel(string oldRbse = Old, string newRbse = New)
    {
        var model = new RbseChangeModel(_cases, _farms, _user, NullLogger<RbseChangeModel>.Instance)
        {
            OldRbse = oldRbse,
            NewRbse = newRbse
        };
        PageTestContext.Attach(model);
        return model;
    }

    [Fact]
    public async Task OnGet_Blank_DoesNotLookUp()
    {
        (await CreateModel("").OnGetAsync()).Should().BeOfType<PageResult>();

        await _cases.DidNotReceiveWithAnyArgs().GetCaseAsync(default!);
    }

    [Fact]
    public async Task OnGet_KnownCase_LoadsCaseAndFarm()
    {
        var model = CreateModel("26/1");

        await model.OnGetAsync();

        model.CaseFound.Should().BeTrue();
        model.OldRbse.Should().Be("00/26/00001");
        model.Farm!.CPHH.Should().Be("01001000101");
        model.EartagDisplay.Should().Be("HM 9");
    }

    [Fact]
    public async Task OnGet_CaseWithoutCphh_SkipsFarm()
    {
        _cases.GetCaseAsync(Old).Returns(new CaseRecord { Rbse = Old, Cphh = "" });
        var model = CreateModel();

        await model.OnGetAsync();

        model.Farm.Should().BeNull();
        model.EartagDisplay.Should().BeEmpty();
        await _farms.DidNotReceiveWithAnyArgs().GetByCphhAsync(default!);
    }

    [Fact]
    public async Task LookUp_Blank_SetsNotFound()
    {
        var model = CreateModel("");

        await model.OnPostLookUpAsync();

        model.OldRbseError.Should().Be(RbseChangeModel.CaseNotFoundMessage);
    }

    [Fact]
    public async Task LookUp_Unknown_SetsNotFound()
    {
        _cases.GetCaseAsync(Old).Returns((CaseRecord?)null);
        var model = CreateModel();

        await model.OnPostLookUpAsync();

        model.OldRbseError.Should().Be(RbseChangeModel.CaseNotFoundMessage);
        model.CaseFound.Should().BeFalse();
    }

    [Fact]
    public async Task LookUp_Throws_ReportsLoadFailure()
    {
        _cases.GetCaseAsync(Old).Returns<Task<CaseRecord?>>(_ => throw new InvalidOperationException());
        var model = CreateModel();

        await model.OnPostLookUpAsync();

        model.ErrorMessage.Should().Be(RbseChangeModel.LoadFailedMessage);
    }

    [Fact]
    public async Task Change_CaseNotFound_ReturnsPage()
    {
        _cases.GetCaseAsync(Old).Returns((CaseRecord?)null);

        (await CreateModel().OnPostChangeAsync()).Should().BeOfType<PageResult>();

        await _cases.DidNotReceiveWithAnyArgs().ChangeRbseAsync(default!, default!, default);
    }

    [Fact]
    public async Task Change_BlankNew_RequiresNewRbse()
    {
        var model = CreateModel(Old, "");

        await model.OnPostChangeAsync();

        model.NewRbseError.Should().Be(RbseChangeModel.NewRbseRequiredMessage);
    }

    [Fact]
    public async Task Change_SameRbse_IsRejected()
    {
        var model = CreateModel(Old, "00/26/00001");

        await model.OnPostChangeAsync();

        model.NewRbseError.Should().Be(RbseChangeModel.SameRbseMessage);
        await _cases.DidNotReceiveWithAnyArgs().ChangeRbseAsync(default!, default!, default);
    }

    [Fact]
    public async Task Change_ServiceThrows_ReportsFailure()
    {
        _cases.ChangeRbseAsync(Old, New, 2).Returns<Task<ChangeRbseResult>>(_ => throw new InvalidOperationException());
        var model = CreateModel();

        (await model.OnPostChangeAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be(RbseChangeModel.ChangeFailedMessage);
    }

    [Fact]
    public async Task Change_NewExists_SetsNewRbseError()
    {
        _cases.ChangeRbseAsync(Old, New, 2).Returns(ChangeRbseResult.NewRbseAlreadyExists);
        var model = CreateModel();

        await model.OnPostChangeAsync();

        model.NewRbseError.Should().Be(RbseChangeModel.NewRbseExistsMessage);
        model.NewRbse.Should().Be("00/26/00002");
    }

    [Fact]
    public async Task Change_OldNotFound_SetsOldRbseError()
    {
        _cases.ChangeRbseAsync(Old, New, 2).Returns(ChangeRbseResult.OldRbseNotFound);
        var model = CreateModel();

        await model.OnPostChangeAsync();

        model.OldRbseError.Should().Be(RbseChangeModel.CaseNotFoundMessage);
    }

    [Fact]
    public async Task Change_OtherFailure_SetsGenericError()
    {
        _cases.ChangeRbseAsync(Old, New, 2).Returns(ChangeRbseResult.CaseBabUpdateError);
        var model = CreateModel();

        await model.OnPostChangeAsync();

        model.ErrorMessage.Should().Be(RbseChangeModel.ChangeFailedMessage);
    }

    [Fact]
    public async Task Change_Success_RedirectsToConfirmation()
    {
        _cases.ChangeRbseAsync(Old, New, 2).Returns(ChangeRbseResult.Success);
        var model = CreateModel();

        var result = await model.OnPostChangeAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("RBSE change complete");
        model.TempData[MaintenanceConfirmationModel.SummaryKey].Should().Be("00/26/00001 changed to 00/26/00002");
        model.TempData[MaintenanceConfirmationModel.MessageKey].Should().NotBeNull();
    }
}
