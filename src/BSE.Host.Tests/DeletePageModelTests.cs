using BSE.Host.Pages;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;

namespace BSE.Host.Tests;

public sealed class DeletePageModelTests
{
    private const string Rbse = "002600001";
    private const string Cphh = "01001000101";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly IBatchService _batches = Substitute.For<IBatchService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    public DeletePageModelTests()
    {
        _user.GetUserIdAsync().Returns(9);
        _farms.GetByCphhAsync(Cphh).Returns(new FarmRecord { CPHH = Cphh });
        _farms.GetCaseCountByCphhAsync(Cphh).Returns(2);
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns(new List<BatchNumberEntry>());
        SetDetails();
    }

    private void SetDetails(int feeds = 0, bool clinical = false, int relations = 0, int owners = 0, int visits = 0, string cphh = Cphh)
    {
        var details = new CaseDetailRecord(
            new CaseRecord { Rbse = Rbse, Cphh = cphh, EartagHerdmark = "HM", Eartag = "1" },
            clinical ? PageTestContext.Blank<CaseClinicalRecord>() : null,
            null,
            Enumerable.Range(0, owners).Select(_ => PageTestContext.Blank<OtherOwnerRecord>()).ToList(),
            Enumerable.Range(0, feeds).Select(_ => PageTestContext.Blank<CaseFeedRecord>()).ToList(),
            Enumerable.Range(0, visits).Select(_ => PageTestContext.Blank<ClinicalVisitRecord>()).ToList(),
            null, null,
            Enumerable.Range(0, relations).Select(_ => PageTestContext.Blank<CaseRelationRecord>()).ToList(),
            [], null);
        _cases.GetCaseDetailsAsync(Rbse).Returns(details);
    }

    private DeleteModel CreateModel(string rbse = Rbse)
    {
        var model = new DeleteModel(_cases, _farms, _batches, _user, NullLogger<DeleteModel>.Instance) { Rbse = rbse };
        PageTestContext.Attach(model);
        return model;
    }

    [Fact]
    public async Task OnGet_BlankRbse_DoesNotLookUp()
    {
        var model = CreateModel("");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.CaseFound.Should().BeFalse();
        await _cases.DidNotReceiveWithAnyArgs().GetCaseDetailsAsync(default!);
    }

    [Fact]
    public async Task OnGet_KnownCase_LoadsFarmAndFormatsRbse()
    {
        var model = CreateModel("26/1");

        await model.OnGetAsync();

        model.CaseFound.Should().BeTrue();
        model.Rbse.Should().Be("00/26/00001");
        model.Farm!.CPHH.Should().Be(Cphh);
        model.NumberOfCasesOnFarm.Should().Be(2);
        model.FarmWillBeDeleted.Should().BeFalse();
        model.HasVlaData.Should().BeFalse();
        model.CanDelete.Should().BeTrue();
        model.EartagDisplay.Should().Be("HM 1");
    }

    [Fact]
    public async Task OnGet_CaseWithoutCphh_SkipsFarmLookup()
    {
        SetDetails(cphh: "");
        var model = CreateModel();

        await model.OnGetAsync();

        model.Farm.Should().BeNull();
        await _farms.DidNotReceiveWithAnyArgs().GetByCphhAsync(default!);
    }

    [Fact]
    public async Task OnGet_UnknownCase_SetsRbseError()
    {
        _cases.GetCaseDetailsAsync(Rbse).Returns((CaseDetailRecord?)null);
        var model = CreateModel();

        await model.OnGetAsync();

        model.RbseError.Should().Be(DeleteModel.CaseNotFoundMessage);
        model.CanDelete.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_LookupThrows_ReportsLoadFailure()
    {
        _cases.GetCaseDetailsAsync(Rbse).Returns<Task<CaseDetailRecord?>>(_ => throw new InvalidOperationException());
        var model = CreateModel();

        await model.OnGetAsync();

        model.ErrorMessage.Should().Be(DeleteModel.LoadFailedMessage);
        model.CaseFound.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, false, 0, 0, 0, 0)]
    [InlineData(0, true, 0, 0, 0, 0)]
    [InlineData(0, false, 1, 0, 0, 0)]
    [InlineData(0, false, 0, 1, 0, 0)]
    [InlineData(0, false, 0, 0, 1, 0)]
    [InlineData(0, false, 0, 0, 0, 1)]
    public async Task OnGet_LinkedData_SetsHasVlaData(int feeds, bool clinical, int relations, int owners, int visits, int batches)
    {
        SetDetails(feeds, clinical, relations, owners, visits);
        _batches.GetBatchNumbersByRbseAsync(Rbse)
            .Returns(Enumerable.Range(0, batches).Select(i => new BatchNumberEntry(i, "B", Rbse, null)).ToList());
        var model = CreateModel();

        await model.OnGetAsync();

        model.HasVlaData.Should().BeTrue();
        model.CanDelete.Should().BeFalse();
    }

    [Fact]
    public async Task LookUp_BlankRbse_SetsRbseError()
    {
        var model = CreateModel("");

        (await model.OnPostLookUpAsync()).Should().BeOfType<PageResult>();

        model.RbseError.Should().Be(DeleteModel.CaseNotFoundMessage);
    }

    [Fact]
    public async Task Delete_CaseNotFound_ReturnsPageWithoutDeleting()
    {
        _cases.GetCaseDetailsAsync(Rbse).Returns((CaseDetailRecord?)null);

        (await CreateModel().OnPostDeleteAsync()).Should().BeOfType<PageResult>();

        await _cases.DidNotReceiveWithAnyArgs().DeleteCaseAsync(default!, default);
    }

    [Fact]
    public async Task Delete_HasVlaData_Blocks()
    {
        SetDetails(feeds: 1);
        var model = CreateModel();

        (await model.OnPostDeleteAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be(DeleteModel.VlaDataMessage);
        await _cases.DidNotReceiveWithAnyArgs().DeleteCaseAsync(default!, default);
    }

    [Fact]
    public async Task Delete_ServiceThrows_ReportsFailure()
    {
        _cases.DeleteCaseAsync(Rbse, 9).Returns<Task<DeleteCaseResult>>(_ => throw new InvalidOperationException());
        var model = CreateModel();

        (await model.OnPostDeleteAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be(DeleteModel.DeleteFailedMessage);
    }

    [Theory]
    [InlineData(DeleteCaseResult.RbseNotFound, DeleteModel.CaseNotFoundMessage)]
    [InlineData(DeleteCaseResult.HasLinkedRecords, DeleteModel.VlaDataMessage)]
    [InlineData(DeleteCaseResult.FarmCountError, DeleteModel.DeleteFailedMessage)]
    public async Task Delete_NonSuccessResult_MapsMessage(DeleteCaseResult code, string message)
    {
        _cases.DeleteCaseAsync(Rbse, 9).Returns(code);
        var model = CreateModel();

        (await model.OnPostDeleteAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be(message);
    }

    [Theory]
    [InlineData(2, "farm has been kept")]
    [InlineData(1, "farm record was deleted")]
    public async Task Delete_Success_RedirectsToConfirmation(int casesOnFarm, string fragment)
    {
        _farms.GetCaseCountByCphhAsync(Cphh).Returns(casesOnFarm);
        _cases.DeleteCaseAsync(Rbse, 9).Returns(DeleteCaseResult.Success);
        var model = CreateModel("26/1");

        var result = await model.OnPostDeleteAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/MaintenanceConfirmation");
        model.TempData[MaintenanceConfirmationModel.TitleKey].Should().Be("Case deleted");
        ((string)model.TempData[MaintenanceConfirmationModel.SummaryKey]!).Should().Contain("00/26/00001");
        ((string)model.TempData[MaintenanceConfirmationModel.MessageKey]!).Should().Contain(fragment.Split(' ')[^1]);
    }
}
