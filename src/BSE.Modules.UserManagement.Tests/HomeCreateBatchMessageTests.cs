using BSE.Host.Pages;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public class HomeCreateBatchMessageTests
{
    [Fact]
    public async Task OnPostCreateBatchAsync_SetsCreatedMessage_AndRedirectsWithBatchPrefilled()
    {
        var batchService = Substitute.For<IBatchService>();
        batchService.GetOrCreateBatchNumberAsync().Returns(new BatchRecord(7, 2026, 26));
        var httpContext = new DefaultHttpContext();
        var model = new HomeModel(batchService, Substitute.For<ICaseRepository>(), Substitute.For<ICaseWizardStateService>())
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>())
        };

        var result = await model.OnPostCreateBatchAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.RouteValues!["batchNumber"].Should().Be(26);
        model.TempData["SuccessMessage"].Should().Be(
            "Batch 2026/26 has been created. To add a case to this batch, enter an RBSE number in the RBSE Number section and select Go.");
    }

    [Fact]
    public async Task OnPostLookupBatchAsync_WhenYearMissing_AddsValidationErrorAndReturnsPage()
    {
        var batchService = Substitute.For<IBatchService>();
        var httpContext = new DefaultHttpContext();
        var model = new HomeModel(batchService, Substitute.For<ICaseRepository>(), Substitute.For<ICaseWizardStateService>())
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            BatchNumber = 12
        };

        var result = await model.OnPostLookupBatchAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey(nameof(HomeModel.BatchYear));
        model.ModelState[nameof(HomeModel.BatchYear)]!.Errors.Should().Contain(e => e.ErrorMessage == "Enter a batch year.");
    }

    [Fact]
    public async Task OnPostLookupBatchAsync_WhenBatchNotFound_SetsNotFoundFlagAndAddsError()
    {
        var batchService = Substitute.For<IBatchService>();
        batchService.GetBatchIdAsync(2026, 12).Returns((int?)null);
        var httpContext = new DefaultHttpContext();
        var model = new HomeModel(batchService, Substitute.For<ICaseRepository>(), Substitute.For<ICaseWizardStateService>())
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            BatchYear = 2026,
            BatchNumber = 12
        };

        var result = await model.OnPostLookupBatchAsync();

        result.Should().BeOfType<PageResult>();
        model.BatchNotFound.Should().BeTrue();
        model.ModelState.Should().ContainKey(nameof(HomeModel.BatchYear));
        model.ModelState[nameof(HomeModel.BatchYear)]!.Errors.Should().Contain(e => e.ErrorMessage == "Batch 2026/12 was not found.");
    }

    [Fact]
    public async Task OnPostLookupBatchAsync_WhenBatchExists_RedirectsToNewCasePage()
    {
        var batchService = Substitute.For<IBatchService>();
        batchService.GetBatchIdAsync(2026, 12).Returns(99);
        var httpContext = new DefaultHttpContext();
        var model = new HomeModel(batchService, Substitute.For<ICaseRepository>(), Substitute.For<ICaseWizardStateService>())
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>()),
            BatchYear = 2026,
            BatchNumber = 12
        };

        var result = await model.OnPostLookupBatchAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/New");
        redirect.RouteValues.Should().ContainKey("batchYear").WhoseValue.Should().Be((short)2026);
        redirect.RouteValues.Should().ContainKey("batchNumber").WhoseValue.Should().Be(12);
    }
}
