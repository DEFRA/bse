using System.Data;
using BSE.Host.Pages;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
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

namespace BSE.Modules.UserManagement.Tests;

public sealed class OtherOwnerPagesTests
{
    private const string Rbse = "002600001";

    private readonly IOtherOwnerRepository _owners = Substitute.For<IOtherOwnerRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _conn = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _tx = Substitute.For<IDbTransaction>();

    public OtherOwnerPagesTests()
    {
        _lookups.GetLookupAsync(LookupTableId.OwnerType).Returns(new[]
        {
            new LookupItem { Code = "P", Description = "Previous Owner" },
            new LookupItem { Code = "C", Description = "Current Owner" }
        });
        _owners.GetByRbseAsync(Arg.Any<string>()).Returns(Array.Empty<OtherOwnerRecord>());
        _conn.BeginTransaction().Returns(_tx);
        _factory.CreateConnection().Returns(_conn);
    }

    private static void Wire(PageModel model)
    {
        var http = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
    }

    private OtherOwnerAddModel Add()
    {
        var m = new OtherOwnerAddModel(_owners, _lookups, _factory) { Rbse = Rbse };
        Wire(m);
        return m;
    }

    private OtherOwnerEditModel Edit()
    {
        var m = new OtherOwnerEditModel(_owners, _lookups, _factory) { Rbse = Rbse, Id = 4 };
        Wire(m);
        return m;
    }

    // â”€â”€ Add â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Add_OnGet_LoadsOwnerTypes()
    {
        var m = Add();

        (await m.OnGetAsync()).Should().BeOfType<PageResult>();
        m.OwnerTypes.Should().HaveCount(2);
    }

    [Fact]
    public async Task Add_Post_NothingEntered_ReportsAllErrors()
    {
        var m = Add();

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();

        m.ModelState.Keys.Should().Contain(["OwnerType", "OwnerName"]);
    }

    [Fact]
    public async Task Add_Post_BadCphh_ReportsError()
    {
        var m = Add();
        m.OwnerType = "C";
        m.OwnerCphh = "123";

        await m.OnPostAsync();

        m.ModelState.ContainsKey("OwnerCphh").Should().BeTrue();
    }

    [Fact]
    public async Task Add_Post_SecondPreviousOwner_Rejected()
    {
        _owners.GetByRbseAsync(Rbse).Returns(new[] { new OtherOwnerRecord(1, Rbse, "P", "x", null, null) });
        var m = Add();
        m.OwnerType = "P";
        m.OwnerName = "n";

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        m.ModelState["OwnerType"]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Add_Post_Valid_SavesAndRedirects()
    {
        var m = Add();
        m.OwnerType = "C";
        m.OwnerName = "Name";
        m.OwnerCphh = "12/345/6789/01";

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Vla");
        m.TempData["Success"].Should().Be("Owner record added.");
        await _owners.Received(1).AddAsync(Arg.Is<AddOtherOwnerCommand>(c => c.Cphh == "12345678901"), _conn, _tx);
        _tx.Received(1).Commit();
    }

    // â”€â”€ Edit â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Edit_OnGet_NotFound_RedirectsToVla()
    {
        (await Edit().OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Vla");
    }

    [Fact]
    public async Task Edit_OnGet_Found_PopulatesFields()
    {
        _owners.GetByRbseAsync(Rbse).Returns(new[] { new OtherOwnerRecord(4, Rbse, "C", "Nm", "12345678901", [5]) });
        var m = Edit();

        (await m.OnGetAsync()).Should().BeOfType<PageResult>();

        m.OwnerType.Should().Be("C");
        m.OwnerName.Should().Be("Nm");
        m.RowStampBase64.Should().Be(Convert.ToBase64String([5]));
    }

    [Fact]
    public async Task Edit_Post_Invalid_ReturnsPage()
    {
        var m = Edit();
        m.OwnerCphh = "12";

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        m.ModelState.Keys.Should().Contain(["OwnerType", "OwnerCphh"]);
    }

    [Fact]
    public async Task Edit_Post_DuplicatePrevious_OnlyOtherRowsCount()
    {
        _owners.GetByRbseAsync(Rbse).Returns(new[]
        {
            new OtherOwnerRecord(4, Rbse, "P", "self", null, null),
            new OtherOwnerRecord(8, Rbse, "P", "other", null, null)
        });
        var m = Edit();
        m.OwnerType = "P";
        m.OwnerName = "n";

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        m.ModelState["OwnerType"]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Edit_Post_Valid_SavesAndRedirects()
    {
        var m = Edit();
        m.OwnerType = "P";
        m.OwnerName = "n";
        m.RowStampBase64 = Convert.ToBase64String([1]);

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Vla");
        m.TempData["Success"].Should().Be("Owner record updated.");
        await _owners.Received(1).EditAsync(Arg.Any<EditOtherOwnerCommand>(), _conn, _tx);
        _tx.Received(1).Commit();
    }

    [Fact]
    public async Task Edit_Post_Valid_WithoutRowStamp_StillSaves()
    {
        var m = Edit();
        m.OwnerType = "C";
        m.OwnerCphh = "12345678901";
        m.RowStampBase64 = "";

        (await m.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
    }
}

public sealed class MoveCasePageFlowTests
{
    private const string Rbse = "002600001";
    private const string Old = "01001000101";
    private const string New = "01001000202";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    private MoveCaseModel Create(string newCphh = New)
    {
        var m = new MoveCaseModel(_cases, _farms, _user, NullLogger<MoveCaseModel>.Instance) { Rbse = Rbse, NewCphh = newCphh };
        var http = new DefaultHttpContext();
        m.PageContext = new PageContext { HttpContext = http };
        m.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return m;
    }

    private void GivenCase(bool nonGb = false) =>
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Old, IsNonGbCase = nonGb, EartagHerdmark = "AB", Eartag = "12" });

    private void GivenFarms(bool newFarmNonGb = false, bool newExists = true)
    {
        _farms.GetByCphhAsync(Old).Returns(new FarmRecord { CPHH = Old });
        _farms.GetCaseCountByCphhAsync(Old).Returns(1);
        _farms.GetByCphhAsync(New).Returns(newExists ? new FarmRecord { CPHH = New, IsNonGBFarm = newFarmNonGb } : null);
    }

    [Fact]
    public async Task OnGet_NoRbse_ShowsBlankPage()
    {
        var m = Create();
        m.Rbse = "";

        (await m.OnGetAsync()).Should().BeOfType<PageResult>();
        m.CaseFound.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_FoundWithNewCphh_ChecksFarm()
    {
        GivenCase();
        GivenFarms();
        var m = Create();

        await m.OnGetAsync();

        m.CaseFound.Should().BeTrue();
        m.CanMove.Should().BeTrue();
        m.FarmWillBeDeleted.Should().BeTrue();
        m.EartagDisplay.Should().Be("AB 12");
    }

    [Fact]
    public async Task LookUp_BlankAndMissingCase()
    {
        var blank = Create();
        blank.Rbse = "";
        await blank.OnPostLookUpAsync();
        blank.RbseError.Should().Be(MoveCaseModel.CaseNotFoundMessage);

        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        var missing = Create();
        await missing.OnPostLookUpAsync();
        missing.RbseError.Should().Be(MoveCaseModel.CaseNotFoundMessage);
        missing.EartagDisplay.Should().BeEmpty();
    }

    [Fact]
    public async Task LookUp_ServiceThrows_SetsLoadFailed()
    {
        _cases.GetCaseAsync(Rbse).Returns<CaseRecord?>(_ => throw new InvalidOperationException());
        var m = Create();

        await m.OnPostLookUpAsync();

        m.ErrorMessage.Should().Be(MoveCaseModel.LoadFailedMessage);
    }

    [Theory]
    [InlineData("", MoveCaseModel.FarmNotFoundMessage)]
    [InlineData("12", MoveCaseModel.InvalidCphhMessage)]
    [InlineData(Old, MoveCaseModel.SameCphhMessage)]
    public async Task Check_InvalidDestination_SetsError(string newCphh, string expected)
    {
        GivenCase();
        GivenFarms();
        var m = Create(newCphh);

        await m.OnPostCheckAsync();

        m.CphhError.Should().Be(expected);
        m.CanMove.Should().BeFalse();
    }

    [Fact]
    public async Task Check_CaseNotFound_ReturnsEarly()
    {
        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        (await Create().OnPostCheckAsync()).Should().BeOfType<PageResult>();
    }

    [Theory]
    [InlineData("01001000102", true)]
    [InlineData("99999999901", false)]
    public async Task Check_FarmMissing_OffersNewFarmOnlyWhenCphUnchanged(string newCphh, bool prompt)
    {
        GivenCase();
        GivenFarms(newExists: false);
        _farms.GetByCphhAsync("99999999901").Returns((FarmRecord?)null);_farms.GetByCphhAsync("01001000102").Returns((FarmRecord?)null);
        var m = Create(newCphh);

        await m.OnPostCheckAsync();

        m.CphhError.Should().Be(MoveCaseModel.FarmNotFoundMessage);
        m.ShowNewFarmPrompt.Should().Be(prompt);
    }

    [Fact]
    public async Task Check_FarmLookupThrows_SetsLoadFailed()
    {
        GivenCase();
        GivenFarms();
        _farms.GetByCphhAsync(New).Returns<FarmRecord?>(_ => throw new InvalidOperationException());
        var m = Create();

        await m.OnPostCheckAsync();

        m.ErrorMessage.Should().Be(MoveCaseModel.LoadFailedMessage);
    }

    [Theory]
    [InlineData(false, true, MoveCaseModel.GbCaseNonGbFarmMessage)]
    [InlineData(true, false, MoveCaseModel.NonGbCaseGbFarmMessage)]
    public async Task Check_GbMismatch_Blocked(bool caseNonGb, bool farmNonGb, string expected)
    {
        GivenCase(caseNonGb);
        GivenFarms(farmNonGb);
        var m = Create();

        await m.OnPostCheckAsync();

        m.CphhError.Should().Be(expected);
        m.CanMove.Should().BeFalse();
    }

    [Fact]
    public async Task Move_CaseNotFoundOrCannotMove_ReturnsPage()
    {
        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);
        (await Create().OnPostMoveAsync()).Should().BeOfType<PageResult>();

        GivenCase();
        GivenFarms(newExists: false);
        (await Create().OnPostMoveAsync()).Should().BeOfType<PageResult>();
        await _cases.DidNotReceiveWithAnyArgs().MoveCaseAsync(default!, default!, default);
    }

    [Fact]
    public async Task Move_ServiceThrows_SetsMoveFailed()
    {
        GivenCase();
        GivenFarms();
        _cases.MoveCaseAsync(Rbse, New, Arg.Any<int>()).Returns<MoveCaseResult>(_ => throw new InvalidOperationException());
        var m = Create();

        await m.OnPostMoveAsync();

        m.ErrorMessage.Should().Be(MoveCaseModel.MoveFailedMessage);
    }

    [Theory]
    [InlineData(MoveCaseResult.RbseNotFound, MoveCaseModel.CaseNotFoundMessage)]
    [InlineData(MoveCaseResult.NewFarmNotFound, MoveCaseModel.FarmNotFoundMessage)]
    [InlineData(MoveCaseResult.AuditLogError, MoveCaseModel.MoveFailedMessage)]
    public async Task Move_FailureResult_SetsMessage(MoveCaseResult result, string expected)
    {
        GivenCase();
        GivenFarms();
        _cases.MoveCaseAsync(Rbse, New, Arg.Any<int>()).Returns(result);
        var m = Create();

        (await m.OnPostMoveAsync()).Should().BeOfType<PageResult>();

        m.ErrorMessage.Should().Be(expected);
    }

    [Fact]
    public async Task Move_Success_RedirectsToConfirmation()
    {
        GivenCase();
        GivenFarms();
        _user.GetUserIdAsync().Returns(2);
        _cases.MoveCaseAsync(Rbse, New, 2).Returns(MoveCaseResult.Success);
        var m = Create();

        var result = await m.OnPostMoveAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        m.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("Case move complete");
    }
}
