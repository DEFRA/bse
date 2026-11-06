using BSE.Host.Pages;
using BSE.Host.Pages.Case;
using BSE.Host.Pages.Farm;
using BSE.Host.Services;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class CphhChangeModelTests
{
    private const string Old = "01001000101";
    private const string New = "01001000202";

    private readonly IFarmService _farm = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    private CphhChangeModel Create(string oldCphh = Old, string newCphh = New)
    {
        var model = new CphhChangeModel(_farm, _user, NullLogger<CphhChangeModel>.Instance)
        {
            OldCphh = oldCphh,
            NewCphh = newCphh
        };
        var http = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private void FarmExists() => _farm.GetByCphhAsync(Old).Returns(new FarmRecord());

    [Fact]
    public async Task OnGet_BlankOld_DoesNotLookUp()
    {
        var model = Create("");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.OldCphhError.Should().BeNull();
        await _farm.DidNotReceiveWithAnyArgs().GetByCphhAsync(default!);
    }

    [Fact]
    public async Task OnGet_Found_LoadsFarmAndCaseCount()
    {
        FarmExists();
        _farm.GetConfirmedCaseCountAsync(Old).Returns(4);
        var model = Create();

        await model.OnGetAsync();

        model.FarmFound.Should().BeTrue();
        model.ConfirmedCases.Should().Be(4);
    }

    [Theory]
    [InlineData("", CphhChangeModel.OldCphhRequiredMessage)]
    [InlineData("123", CphhChangeModel.InvalidCphhMessage)]
    public async Task LookUp_InvalidOld_SetsError(string input, string expected)
    {
        var model = Create(input);

        await model.OnPostLookUpAsync();

        model.OldCphhError.Should().Be(expected);
        model.FarmFound.Should().BeFalse();
    }

    [Fact]
    public async Task LookUp_NotFound_SetsError()
    {
        _farm.GetByCphhAsync(Old).Returns((FarmRecord?)null);
        var model = Create();

        await model.OnPostLookUpAsync();

        model.OldCphhError.Should().Be(CphhChangeModel.FarmNotFoundMessage);
    }

    [Fact]
    public async Task LookUp_ServiceThrows_SetsLoadFailed()
    {
        _farm.GetByCphhAsync(Old).Returns<FarmRecord?>(_ => throw new InvalidOperationException());
        var model = Create();

        await model.OnPostLookUpAsync();

        model.ErrorMessage.Should().Be(CphhChangeModel.LoadFailedMessage);
        model.FarmFound.Should().BeFalse();
    }

    [Fact]
    public async Task Change_FarmNotFound_ReturnsPage()
    {
        _farm.GetByCphhAsync(Old).Returns((FarmRecord?)null);

        (await Create().OnPostChangeAsync()).Should().BeOfType<PageResult>();

        await _farm.DidNotReceiveWithAnyArgs().ChangeCphhAsync(default!, default!, default);
    }

    [Theory]
    [InlineData("", CphhChangeModel.NewCphhRequiredMessage)]
    [InlineData("12", CphhChangeModel.InvalidCphhMessage)]
    [InlineData(Old, CphhChangeModel.SameCphhMessage)]
    public async Task Change_InvalidNew_SetsError(string newCphh, string expected)
    {
        FarmExists();
        var model = Create(newCphh: newCphh);

        await model.OnPostChangeAsync();

        model.NewCphhError.Should().Be(expected);
    }

    [Fact]
    public async Task Change_NewAlreadyExists_SetsError()
    {
        FarmExists();
        _farm.GetByCphhAsync(New).Returns(new FarmRecord());
        var model = Create();

        await model.OnPostChangeAsync();

        model.NewCphhError.Should().Be(CphhChangeModel.NewCphhExistsMessage);
    }

    [Fact]
    public async Task Change_ServiceThrows_SetsUpdateFailed()
    {
        FarmExists();
        _farm.GetByCphhAsync(New).Returns((FarmRecord?)null);
        _farm.ChangeCphhAsync(Old, New, Arg.Any<int>()).Returns<ChangeCphhResult>(_ => throw new InvalidOperationException());
        var model = Create();

        await model.OnPostChangeAsync();

        model.ErrorMessage.Should().Be(CphhChangeModel.UpdateFailedMessage);
    }

    [Theory]
    [InlineData(ChangeCphhResult.OldCphhNotFoundOrNewCphhAlreadyExists, CphhChangeModel.NewCphhExistsMessage)]
    [InlineData(ChangeCphhResult.ErrorInsertingNewFarmRecord, CphhChangeModel.UpdateFailedMessage)]
    public async Task Change_FailureResult_SetsMessage(ChangeCphhResult result, string expected)
    {
        FarmExists();
        _farm.GetByCphhAsync(New).Returns((FarmRecord?)null);
        _farm.ChangeCphhAsync(Old, New, Arg.Any<int>()).Returns(result);
        var model = Create();

        (await model.OnPostChangeAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be(expected);
    }

    [Fact]
    public async Task Change_Success_RedirectsToConfirmation()
    {
        FarmExists();
        _farm.GetByCphhAsync(New).Returns((FarmRecord?)null);
        _user.GetUserIdAsync().Returns(7);
        _farm.ChangeCphhAsync(Old, New, 7).Returns(ChangeCphhResult.Success);
        var model = Create();

        var result = await model.OnPostChangeAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("CPHH change complete");
    }
}

public sealed class RbseChangeModelTests
{
    private const string Old = "002600001";
    private const string New = "002600002";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    private RbseChangeModel Create(string oldRbse = Old, string newRbse = New)
    {
        var model = new RbseChangeModel(_cases, _farms, _user, NullLogger<RbseChangeModel>.Instance)
        {
            OldRbse = oldRbse,
            NewRbse = newRbse
        };
        var http = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private void CaseExists(string cphh = "") =>
        _cases.GetCaseAsync(Old).Returns(new CaseRecord { Rbse = Old, Cphh = cphh, EartagHerdmark = "AB1", Eartag = "123" });

    [Fact]
    public async Task OnGet_BlankOld_DoesNotLookUp()
    {
        (await Create("").OnGetAsync()).Should().BeOfType<PageResult>();

        await _cases.DidNotReceiveWithAnyArgs().GetCaseAsync(default!);
    }

    [Fact]
    public async Task OnGet_FoundWithCphh_LoadsFarmAndEartag()
    {
        CaseExists("01001000101");
        _farms.GetByCphhAsync("01001000101").Returns(new FarmRecord());
        var model = Create();

        await model.OnGetAsync();

        model.CaseFound.Should().BeTrue();
        model.Farm.Should().NotBeNull();
        model.EartagDisplay.Should().Be("AB1 123");
    }

    [Fact]
    public async Task LookUp_BlankOld_SetsNotFound()
    {
        var model = Create("");

        await model.OnPostLookUpAsync();

        model.OldRbseError.Should().Be(RbseChangeModel.CaseNotFoundMessage);
    }

    [Fact]
    public async Task LookUp_NotFound_SetsError()
    {
        _cases.GetCaseAsync(Old).Returns((CaseRecord?)null);
        var model = Create();

        await model.OnPostLookUpAsync();

        model.OldRbseError.Should().Be(RbseChangeModel.CaseNotFoundMessage);
        model.EartagDisplay.Should().BeEmpty();
    }

    [Fact]
    public async Task LookUp_ServiceThrows_SetsLoadFailed()
    {
        _cases.GetCaseAsync(Old).Returns<CaseRecord?>(_ => throw new InvalidOperationException());
        var model = Create();

        await model.OnPostLookUpAsync();

        model.ErrorMessage.Should().Be(RbseChangeModel.LoadFailedMessage);
    }

    [Fact]
    public async Task Change_CaseNotFound_ReturnsPage()
    {
        _cases.GetCaseAsync(Old).Returns((CaseRecord?)null);

        (await Create().OnPostChangeAsync()).Should().BeOfType<PageResult>();

        await _cases.DidNotReceiveWithAnyArgs().ChangeRbseAsync(default!, default!, default);
    }

    [Fact]
    public async Task Change_BlankNew_SetsRequired()
    {
        CaseExists();
        var model = Create(newRbse: "");

        await model.OnPostChangeAsync();

        model.NewRbseError.Should().Be(RbseChangeModel.NewRbseRequiredMessage);
    }

    [Fact]
    public async Task Change_SameRbse_SetsError()
    {
        CaseExists();
        var model = Create(newRbse: Old);

        await model.OnPostChangeAsync();

        model.NewRbseError.Should().Be(RbseChangeModel.SameRbseMessage);
    }

    [Fact]
    public async Task Change_ServiceThrows_SetsChangeFailed()
    {
        CaseExists();
        _cases.ChangeRbseAsync(Old, New, Arg.Any<int>()).Returns<ChangeRbseResult>(_ => throw new InvalidOperationException());
        var model = Create();

        await model.OnPostChangeAsync();

        model.ErrorMessage.Should().Be(RbseChangeModel.ChangeFailedMessage);
    }

    [Theory]
    [InlineData(ChangeRbseResult.NewRbseAlreadyExists)]
    [InlineData(ChangeRbseResult.OldRbseNotFound)]
    [InlineData(ChangeRbseResult.CaseInsertError)]
    public async Task Change_FailureResults_SetMatchingMessage(ChangeRbseResult result)
    {
        CaseExists();
        _cases.ChangeRbseAsync(Old, New, Arg.Any<int>()).Returns(result);
        var model = Create();

        (await model.OnPostChangeAsync()).Should().BeOfType<PageResult>();

        switch (result)
        {
            case ChangeRbseResult.NewRbseAlreadyExists:
                model.NewRbseError.Should().Be(RbseChangeModel.NewRbseExistsMessage);
                break;
            case ChangeRbseResult.OldRbseNotFound:
                model.OldRbseError.Should().Be(RbseChangeModel.CaseNotFoundMessage);
                break;
            default:
                model.ErrorMessage.Should().Be(RbseChangeModel.ChangeFailedMessage);
                break;
        }
    }

    [Fact]
    public async Task Change_Success_RedirectsToConfirmation()
    {
        CaseExists();
        _user.GetUserIdAsync().Returns(3);
        _cases.ChangeRbseAsync(Old, New, 3).Returns(ChangeRbseResult.Success);
        var model = Create();

        var result = await model.OnPostChangeAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("RBSE change complete");
    }
}
