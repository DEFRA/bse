using System.Security.Claims;
using BSE.Host.Pages;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BSE.Host.Tests;

public sealed class HomePageModelTests
{
    private readonly IBatchService _batches = Substitute.For<IBatchService>();
    private readonly ICaseRepository _cases = Substitute.For<ICaseRepository>();
    private readonly ICaseWizardStateService _wizard = Substitute.For<ICaseWizardStateService>();

    private HomeModel CreateModel(params string[] roles)
    {
        var model = new HomeModel(_batches, _cases, _wizard);
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "test"))
        };
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    [Fact]
    public async Task OnGet_VlaAccess_LoadsBatchesAndFormattedReferences()
    {
        var year = (short)DateTime.Today.Year;
        _batches.GetLatestBatchNumbersAsync().Returns(new List<LatestBatchRecord> { new("2026/1", 3) });
        _cases.GetLatestRbseForYearAsync(year).Returns("002600005");
        _cases.GetLatestRbseForYearAsync((short)(year - 1)).Returns("002500007");
        _cases.GetLatestDbseForYearAsync(year).Returns("2600002");
        _cases.GetLatestDbseForYearAsync((short)(year - 1)).Returns((string?)null);
        var model = CreateModel("VLAAccess");

        await model.OnGetAsync();

        model.LatestBatches.Should().ContainSingle();
        model.LatestRbseCurrentYear.Should().Be("00/26/00005");
        model.LatestRbsePreviousYear.Should().Be("00/25/00007");
        model.LatestDbseCurrentYear.Should().Be("26/00002");
        model.LatestDbsePreviousYear.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_DefraAccessOnly_SkipsBatchesButLoadsRbse()
    {
        var model = CreateModel("DEFRAAccess");

        await model.OnGetAsync();

        await _batches.DidNotReceive().GetLatestBatchNumbersAsync();
        await _cases.Received(2).GetLatestRbseForYearAsync(Arg.Any<short>());
        await _cases.Received(2).GetLatestDbseForYearAsync(Arg.Any<short>());
    }

    [Fact]
    public async Task OnGet_NoRoles_LoadsNothing()
    {
        var model = CreateModel();

        await model.OnGetAsync();

        await _batches.DidNotReceive().GetLatestBatchNumbersAsync();
        await _cases.DidNotReceiveWithAnyArgs().GetLatestRbseForYearAsync(default);
    }

    [Fact]
    public async Task OnGet_PendingWizardBatch_RestoredOnceAndCleared()
    {
        _wizard.GetAsync().Returns(new CaseWizardState("002600001", "2026/12", 9));
        var model = CreateModel();

        await model.OnGetAsync();

        model.BatchYear.Should().Be(2026);
        model.BatchNumber.Should().Be(12);
        await _wizard.Received(1).ClearAsync();
    }

    [Fact]
    public async Task OnGet_PendingWizardBatchWithBlankYear_RestoresNumberOnly()
    {
        _wizard.GetAsync().Returns(new CaseWizardState("002600001", "/3001", 9));
        var model = CreateModel();

        await model.OnGetAsync();

        model.BatchYear.Should().BeNull();
        model.BatchNumber.Should().Be(3001);
    }

    [Fact]
    public async Task OnGet_MalformedWizardBatch_DoesNotRestoreButClears()
    {
        _wizard.GetAsync().Returns(new CaseWizardState("002600001", "garbage", 9));
        var model = CreateModel();

        await model.OnGetAsync();

        model.BatchNumber.Should().BeNull();
        await _wizard.Received(1).ClearAsync();
    }

    [Fact]
    public async Task OnGet_BatchAlreadyBound_DoesNotTouchWizardState()
    {
        var model = CreateModel();
        model.BatchNumber = 5;

        await model.OnGetAsync();

        await _wizard.DidNotReceive().GetAsync();
    }

    [Fact]
    public async Task LookupBatch_MissingFields_AddsErrors()
    {
        var model = CreateModel();

        var result = await model.OnPostLookupBatchAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKeys(nameof(model.BatchYear), nameof(model.BatchNumber));
    }

    [Fact]
    public async Task LookupBatch_NotFound_FlagsBatchNotFound()
    {
        _batches.GetBatchIdAsync((short?)2026, 7).Returns((int?)null);
        var model = CreateModel();
        model.BatchYear = 2026;
        model.BatchNumber = 7;

        var result = await model.OnPostLookupBatchAsync();

        result.Should().BeOfType<PageResult>();
        model.BatchNotFound.Should().BeTrue();
        model.ModelState[nameof(model.BatchYear)]!.Errors.Single().ErrorMessage.Should().Contain("2026/7");
    }

    [Fact]
    public async Task LookupBatch_Found_RedirectsToNewCase()
    {
        _batches.GetBatchIdAsync((short?)2026, 7).Returns(11);
        var model = CreateModel();
        model.BatchYear = 2026;
        model.BatchNumber = 7;

        var result = await model.OnPostLookupBatchAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/New");
        redirect.RouteValues!["batchNumber"].Should().Be(7);
    }

    [Fact]
    public async Task RbseLookup_BatchNotFound_ReturnsPageWithError()
    {
        _batches.GetBatchIdAsync((short?)2026, 7).Returns((int?)null);
        var model = CreateModel("DEFRAMaintenance");
        model.BatchYear = 2026;
        model.BatchNumber = 7;
        model.LookupRbse = "002600001";

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.BatchNotFound.Should().BeTrue();
        await _cases.DidNotReceiveWithAnyArgs().GetCaseByRbseAsync(default!);
    }

    [Fact]
    public async Task RbseLookup_YearWithoutNumber_AddsNumberError()
    {
        var model = CreateModel();
        model.BatchYear = 2026;
        model.LookupRbse = "002600001";

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(model.BatchNumber));
    }

    [Fact]
    public async Task RbseLookup_BlankRbse_AddsRequiredError()
    {
        var model = CreateModel();

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(model.LookupRbse)]!.Errors.Single().ErrorMessage.Should().Be("You must enter a RBSE number");
    }

    [Fact]
    public async Task RbseLookup_WithBatch_StoresWizardState()
    {
        _batches.GetBatchIdAsync((short?)2026, 7).Returns(11);
        _cases.GetCaseByRbseAsync("002600001").Returns(new CaseRecord { Rbse = "002600001" });
        var model = CreateModel("DEFRAMaintenance");
        model.BatchYear = 2026;
        model.BatchNumber = 7;
        model.LookupRbse = "26/1";

        var result = await model.OnPostRbseLookupAsync();

        await _wizard.Received(1).SetAsync(Arg.Is<CaseWizardState>(s =>
            s.RbseNumber == "002600001" && s.BatchNumber == "2026/7" && s.BatchId == 11));
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
    }

    [Fact]
    public async Task RbseLookup_WithoutBatch_ClearsWizardState()
    {
        _cases.GetCaseByRbseAsync("002600001").Returns(new CaseRecord { Rbse = "002600001" });
        var model = CreateModel("DEFRAMaintenance");
        model.LookupRbse = "002600001";

        await model.OnPostRbseLookupAsync();

        await _wizard.Received(1).ClearAsync();
        await _wizard.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Fact]
    public async Task RbseLookup_ExistingNonGbCase_DefraViewer_Rejected()
    {
        _cases.GetCaseByRbseAsync("630000001").Returns(new CaseRecord { Rbse = "630000001", IsNonGbCase = true });
        var model = CreateModel("DEFRAAccess");
        model.LookupRbse = "630000001";

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(model.LookupRbse)]!.Errors.Single().ErrorMessage.Should().Be("This is not a GB case");
    }

    [Theory]
    [InlineData("VLAAccess")]
    [InlineData("VLAMaintenance")]
    public async Task RbseLookup_ExistingNonGbCase_VlaUser_Opens(string role)
    {
        _cases.GetCaseByRbseAsync("630000001").Returns(new CaseRecord { Rbse = "630000001", IsNonGbCase = true });
        var model = CreateModel(role);
        model.LookupRbse = "630000001";

        var result = await model.OnPostRbseLookupAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues!["Rbse"].Should().Be("630000001");
    }

    [Theory]
    [InlineData("630000001")]
    [InlineData("230000001")]
    public async Task RbseLookup_NewNonGbReference_VlaMaintenance_GoesToNewNonGb(string rbse)
    {
        var model = CreateModel("VLAMaintenance");
        model.LookupRbse = rbse;

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/NewNonGb");
    }

    [Theory]
    [InlineData("VLAAccess", "This case does not exist")]
    [InlineData("DEFRAMaintenance", "This is not a GB case")]
    public async Task RbseLookup_NewNonGbReference_OtherRoles_Rejected(string role, string message)
    {
        var model = CreateModel(role);
        model.LookupRbse = "630000001";

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(model.LookupRbse)]!.Errors.Single().ErrorMessage.Should().Be(message);
    }

    [Theory]
    [InlineData("DEFRAAccess")]
    [InlineData("VLAAccess")]
    public async Task RbseLookup_NewGbCase_ViewerOrDataEntry_Rejected(string role)
    {
        var model = CreateModel(role);
        model.LookupRbse = "002600001";

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(model.LookupRbse)]!.Errors.Single().ErrorMessage.Should().Be("This case does not exist");
    }

    [Fact]
    public async Task RbseLookup_NewGbCaseWithXInYear_Rejected()
    {
        var model = CreateModel("DEFRAMaintenance");
        model.LookupRbse = "00X600001";

        var result = await model.OnPostRbseLookupAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(model.LookupRbse)]!.Errors.Single().ErrorMessage.Should().Contain("not valid for a new case");
    }

    [Fact]
    public async Task RbseLookup_NewGbCase_Maintenance_GoesToFarm()
    {
        var model = CreateModel("DEFRAMaintenance");
        model.LookupRbse = "002600001";

        var result = await model.OnPostRbseLookupAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        redirect.RouteValues!["rbse"].Should().Be("002600001");
    }

    [Fact]
    public async Task CreateBatch_SetsSuccessMessageAndRedirectsWithBatch()
    {
        _batches.GetOrCreateBatchNumberAsync().Returns(new BatchRecord(5, 2026, 14));
        var model = CreateModel("VLAAccess");

        var result = await model.OnPostCreateBatchAsync();

        model.TempData["SuccessMessage"]!.ToString().Should().Contain("2026/14");
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Home");
        redirect.RouteValues!["batchNumber"].Should().Be(14);
    }
}
