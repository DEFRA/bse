using BSE.Host.Pages;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;

namespace BSE.Host.Tests;

public sealed class MoveCasePageModelTests
{
    private const string Rbse = "002600001";
    private const string CurrentCphh = "01001000101";
    private const string NewCphh = "01001000202";

    private readonly ICaseService _caseService = Substitute.For<ICaseService>();
    private readonly IFarmService _farmService = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public MoveCasePageModelTests()
    {
        _user.GetUserIdAsync().Returns(4);
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = CurrentCphh, EartagHerdmark = "HM", Eartag = "123" });
        _farmService.GetByCphhAsync(CurrentCphh).Returns(new FarmRecord { CPHH = CurrentCphh });
        _farmService.GetCaseCountByCphhAsync(CurrentCphh).Returns(1);
        _farmService.GetByCphhAsync(NewCphh).Returns(new FarmRecord { CPHH = NewCphh, IsNonGBFarm = false });
    }

    private MoveCaseModel CreateModel(string rbse = Rbse, string newCphh = "")
    {
        var model = new MoveCaseModel(_caseService, _farmService, _user, NullLogger<MoveCaseModel>.Instance)
        {
            Rbse = rbse,
            NewCphh = newCphh
        };
        var http = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    [Fact]
    public async Task OnGet_BlankRbse_RendersPageWithoutLookup()
    {
        var model = CreateModel("");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.CaseFound.Should().BeFalse();
        await _caseService.DidNotReceiveWithAnyArgs().GetCaseAsync(default!);
    }

    [Fact]
    public async Task OnGet_KnownCase_LoadsCaseFarmAndCount()
    {
        var model = CreateModel(Rbse);

        await model.OnGetAsync();

        model.CaseFound.Should().BeTrue();
        model.CurrentFarm!.CPHH.Should().Be(CurrentCphh);
        model.NumberOfCasesOnFarm.Should().Be(1);
        model.FarmWillBeDeleted.Should().BeTrue();
        model.EartagDisplay.Should().Be("HM 123");
        model.CanMove.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_UnknownCase_SetsRbseError()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        var model = CreateModel(Rbse, NewCphh);

        await model.OnGetAsync();

        model.RbseError.Should().Be(MoveCaseModel.CaseNotFoundMessage);
        model.CaseFound.Should().BeFalse();
        await _farmService.DidNotReceive().GetByCphhAsync(NewCphh);
    }

    [Fact]
    public async Task OnGet_LookupThrows_ReportsLoadFailure()
    {
        _caseService.GetCaseAsync(Rbse).Returns<Task<CaseRecord?>>(_ => throw new InvalidOperationException("db"));
        var model = CreateModel(Rbse);

        await model.OnGetAsync();

        model.ErrorMessage.Should().Be(MoveCaseModel.LoadFailedMessage);
        model.CaseFound.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_WithValidNewCphh_EnablesMove()
    {
        var model = CreateModel(Rbse, "01/001/0002/02");

        await model.OnGetAsync();

        model.NewFarm!.CPHH.Should().Be(NewCphh);
        model.CphhError.Should().BeNull();
        model.CanMove.Should().BeTrue();
    }

    [Fact]
    public async Task LookUp_FormatsShortRbseAndFindsCase()
    {
        var model = CreateModel("   ");

        var result = await model.OnPostLookUpAsync();

        result.Should().BeOfType<PageResult>();
        model.RbseError.Should().Be(MoveCaseModel.CaseNotFoundMessage);
    }

    [Theory]
    [InlineData("", "A farm with this CPHH was not found")]
    [InlineData("0100100", "Enter CPHH as 11 digits")]
    [InlineData(CurrentCphh, "same as the existing one")]
    public async Task Check_InvalidDestination_SetsCphhError(string cphh, string expected)
    {
        var model = CreateModel(Rbse, cphh);

        await model.OnPostCheckAsync();

        model.CphhError.Should().Contain(expected);
        model.NewFarm.Should().BeNull();
        model.CanMove.Should().BeFalse();
    }

    [Fact]
    public async Task Check_UnknownCase_SkipsFarmCheck()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        var model = CreateModel(Rbse, NewCphh);

        (await model.OnPostCheckAsync()).Should().BeOfType<PageResult>();

        model.CphhError.Should().BeNull();
    }

    [Theory]
    [InlineData("01001000199", true)]
    [InlineData("02001000299", false)]
    public async Task Check_DestinationFarmMissing_OffersNewFarmOnlyWhenSameCph(string cphh, bool prompt)
    {
        _farmService.GetByCphhAsync(cphh).Returns((FarmRecord?)null);
        var model = CreateModel(Rbse, cphh);

        await model.OnPostCheckAsync();

        model.CphhError.Should().Be(MoveCaseModel.FarmNotFoundMessage);
        model.ShowNewFarmPrompt.Should().Be(prompt);
    }

    [Fact]
    public async Task Check_DestinationLookupThrows_ReportsLoadFailure()
    {
        _farmService.GetByCphhAsync(NewCphh).Returns<Task<FarmRecord?>>(_ => throw new TimeoutException());
        var model = CreateModel(Rbse, NewCphh);

        await model.OnPostCheckAsync();

        model.ErrorMessage.Should().Be(MoveCaseModel.LoadFailedMessage);
        model.CanMove.Should().BeFalse();
    }

    [Fact]
    public async Task Check_GbCaseOntoNonGbFarm_IsRejected()
    {
        _farmService.GetByCphhAsync(NewCphh).Returns(new FarmRecord { CPHH = NewCphh, IsNonGBFarm = true });
        var model = CreateModel(Rbse, NewCphh);

        await model.OnPostCheckAsync();

        model.CphhError.Should().Be(MoveCaseModel.GbCaseNonGbFarmMessage);
        model.NewFarm.Should().BeNull();
    }

    [Fact]
    public async Task Check_NonGbCaseOntoGbFarm_IsRejected()
    {
        _caseService.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = CurrentCphh, IsNonGbCase = true });
        var model = CreateModel(Rbse, NewCphh);

        await model.OnPostCheckAsync();

        model.CphhError.Should().Be(MoveCaseModel.NonGbCaseGbFarmMessage);
        model.NewFarm.Should().BeNull();
    }

    [Fact]
    public async Task Move_Success_CallsServiceAndRedirectsToConfirmation()
    {
        _caseService.MoveCaseAsync(Rbse, NewCphh, 4).Returns(MoveCaseResult.Success);
        var model = CreateModel(Rbse, NewCphh);

        var result = await model.OnPostMoveAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        await _caseService.Received(1).MoveCaseAsync(Rbse, NewCphh, 4);
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("Case move complete");
        ((string)model.TempData[MaintenanceConfirmationModel.SummaryKey]!).Should().Contain("moved to");
    }

    [Fact]
    public async Task Move_UnknownCase_DoesNotMove()
    {
        _caseService.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        (await CreateModel(Rbse, NewCphh).OnPostMoveAsync()).Should().BeOfType<PageResult>();

        await _caseService.DidNotReceiveWithAnyArgs().MoveCaseAsync(default!, default!, default);
    }

    [Fact]
    public async Task Move_InvalidDestination_DoesNotMove()
    {
        (await CreateModel(Rbse, "123").OnPostMoveAsync()).Should().BeOfType<PageResult>();

        await _caseService.DidNotReceiveWithAnyArgs().MoveCaseAsync(default!, default!, default);
    }

    [Theory]
    [InlineData(MoveCaseResult.RbseNotFound, MoveCaseModel.CaseNotFoundMessage)]
    [InlineData(MoveCaseResult.NewFarmNotFound, MoveCaseModel.FarmNotFoundMessage)]
    [InlineData(MoveCaseResult.AuditLogError, MoveCaseModel.MoveFailedMessage)]
    public async Task Move_FailureResult_MapsToMessage(MoveCaseResult failure, string message)
    {
        _caseService.MoveCaseAsync(Rbse, NewCphh, 4).Returns(failure);
        var model = CreateModel(Rbse, NewCphh);

        var result = await model.OnPostMoveAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be(message);
    }

    [Fact]
    public async Task Move_ServiceThrows_ReportsMoveFailed()
    {
        _caseService.MoveCaseAsync(Rbse, NewCphh, 4).Returns<Task<MoveCaseResult>>(_ => throw new InvalidOperationException("sp"));
        var model = CreateModel(Rbse, NewCphh);

        var result = await model.OnPostMoveAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be(MoveCaseModel.MoveFailedMessage);
    }
}
