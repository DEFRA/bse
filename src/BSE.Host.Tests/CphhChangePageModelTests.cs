using BSE.Host.Pages;
using BSE.Host.Pages.Farm;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;

namespace BSE.Host.Tests;

public sealed class CphhChangePageModelTests
{
    private const string Old = "01001000101";
    private const string New = "01001000202";

    private readonly IFarmService _farm = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public CphhChangePageModelTests()
    {
        _user.GetUserIdAsync().Returns(6);
        _farm.GetByCphhAsync(Old).Returns(new FarmRecord { CPHH = Old });
        _farm.GetByCphhAsync(New).Returns((FarmRecord?)null);
        _farm.GetConfirmedCaseCountAsync(Old).Returns(3);
    }

    private CphhChangeModel CreateModel(string oldCphh = Old, string newCphh = New)
    {
        var model = new CphhChangeModel(_farm, _user, NullLogger<CphhChangeModel>.Instance)
        {
            OldCphh = oldCphh,
            NewCphh = newCphh
        };
        PageTestContext.Attach(model);
        return model;
    }

    [Fact]
    public async Task OnGet_BlankOld_DoesNotLookUp()
    {
        var model = CreateModel("");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.OldCphhError.Should().BeNull();
        await _farm.DidNotReceiveWithAnyArgs().GetByCphhAsync(default!);
    }

    [Fact]
    public async Task OnGet_KnownFarm_LoadsCaseCount()
    {
        var model = CreateModel("01/001/0001/01");

        await model.OnGetAsync();

        model.FarmFound.Should().BeTrue();
        model.ConfirmedCases.Should().Be(3);
    }

    [Fact]
    public async Task LookUp_BlankOld_RequiresOldCphh()
    {
        var model = CreateModel("");

        (await model.OnPostLookUpAsync()).Should().BeOfType<PageResult>();

        model.OldCphhError.Should().Be(CphhChangeModel.OldCphhRequiredMessage);
    }

    [Fact]
    public async Task LookUp_WrongLength_IsInvalid()
    {
        var model = CreateModel("0100");

        await model.OnPostLookUpAsync();

        model.OldCphhError.Should().Be(CphhChangeModel.InvalidCphhMessage);
        await _farm.DidNotReceiveWithAnyArgs().GetByCphhAsync(default!);
    }

    [Fact]
    public async Task LookUp_UnknownFarm_SetsNotFound()
    {
        _farm.GetByCphhAsync(Old).Returns((FarmRecord?)null);
        var model = CreateModel();

        await model.OnPostLookUpAsync();

        model.OldCphhError.Should().Be(CphhChangeModel.FarmNotFoundMessage);
        model.FarmFound.Should().BeFalse();
    }

    [Fact]
    public async Task LookUp_ServiceThrows_ReportsLoadFailure()
    {
        _farm.GetByCphhAsync(Old).Returns<Task<FarmRecord?>>(_ => throw new InvalidOperationException());
        var model = CreateModel();

        await model.OnPostLookUpAsync();

        model.ErrorMessage.Should().Be(CphhChangeModel.LoadFailedMessage);
        model.FarmFound.Should().BeFalse();
    }

    [Fact]
    public async Task Change_FarmNotFound_ReturnsPageWithoutChanging()
    {
        _farm.GetByCphhAsync(Old).Returns((FarmRecord?)null);

        (await CreateModel().OnPostChangeAsync()).Should().BeOfType<PageResult>();

        await _farm.DidNotReceiveWithAnyArgs().ChangeCphhAsync(default!, default!, default);
    }

    [Theory]
    [InlineData("", CphhChangeModel.NewCphhRequiredMessage)]
    [InlineData("0100", CphhChangeModel.InvalidCphhMessage)]
    [InlineData("01/001/0001/01", CphhChangeModel.SameCphhMessage)]
    public async Task Change_InvalidNewCphh_SetsError(string newCphh, string message)
    {
        var model = CreateModel(Old, newCphh);

        (await model.OnPostChangeAsync()).Should().BeOfType<PageResult>();

        model.NewCphhError.Should().Be(message);
        await _farm.DidNotReceiveWithAnyArgs().ChangeCphhAsync(default!, default!, default);
    }

    [Fact]
    public async Task Change_NewCphhExists_SetsError()
    {
        _farm.GetByCphhAsync(New).Returns(new FarmRecord { CPHH = New });
        var model = CreateModel();

        await model.OnPostChangeAsync();

        model.NewCphhError.Should().Be(CphhChangeModel.NewCphhExistsMessage);
        await _farm.DidNotReceiveWithAnyArgs().ChangeCphhAsync(default!, default!, default);
    }

    [Fact]
    public async Task Change_ServiceThrows_ReportsUpdateFailure()
    {
        _farm.ChangeCphhAsync(Old, New, 6).Returns<Task<ChangeCphhResult>>(_ => throw new InvalidOperationException());
        var model = CreateModel();

        (await model.OnPostChangeAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be(CphhChangeModel.UpdateFailedMessage);
    }

    [Theory]
    [InlineData(ChangeCphhResult.OldCphhNotFoundOrNewCphhAlreadyExists, CphhChangeModel.NewCphhExistsMessage)]
    [InlineData(ChangeCphhResult.ErrorInsertingNewFarmRecord, CphhChangeModel.UpdateFailedMessage)]
    public async Task Change_NonSuccess_MapsMessage(ChangeCphhResult result, string message)
    {
        _farm.ChangeCphhAsync(Old, New, 6).Returns(result);
        var model = CreateModel();

        (await model.OnPostChangeAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be(message);
    }

    [Fact]
    public async Task Change_Success_RedirectsToConfirmation()
    {
        _farm.ChangeCphhAsync(Old, New, 6).Returns(ChangeCphhResult.Success);
        var model = CreateModel("01/001/0001/01", "01/001/0002/02");

        var result = await model.OnPostChangeAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("CPHH change complete");
        model.TempData[MaintenanceConfirmationModel.SummaryKey].Should().Be("01/001/0001/01 changed to 01/001/0002/02");
        ((string)model.TempData[MaintenanceConfirmationModel.MessageKey]!).Should().Contain("has not been changed");
    }
}
