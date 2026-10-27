using BSE.Host.Pages.AdnsExport;
using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace BSE.Host.Tests;

public sealed class GbPageModelTests
{
    private readonly IAdnsExportService _service = Substitute.For<IAdnsExportService>();

    private GbModel CreateModel()
    {
        var options = Options.Create(new AdnsMsGraphOptions { FromAddress = "from@example.com", ToAddress = "to@example.com" });
        var http = new DefaultHttpContext();
        return new GbModel(_service, options)
        {
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    private static AdnsCaseRecord Case(string rbse, string region = "R", string reference = "2026/00001", int day = 1) =>
        new() { Rbse = rbse, AdnsRegionName = region, AdnsReference = reference, ConfirmationDate = new DateTime(2026, 1, day), AdnsYear = 2026, AdnsNumber = 1, AdnsRegionId = 1 };

    private static AdnsExportPreview PreviewOf(params AdnsCaseRecord[] cases) =>
        new(cases, [], [new AdnsRegionSummaryRecord(1, "R", cases.Length)], "subj", "body text", "2026/00001", "2026/00002");

    private async Task<GbModel> GeneratedModel(params AdnsCaseRecord[] cases)
    {
        _service.PreviewGbExportAsync("DBSE00005", 2026, 5).Returns(PreviewOf(cases));
        var model = CreateModel();
        model.EmailReference = "DBSE00005";
        model.AdnsYear = 2026;
        model.StartAdnsNumber = 5;
        await model.OnPostGenerateReportAsync();
        return model;
    }

    [Fact]
    public void ExposesConfiguredAddresses()
    {
        var model = CreateModel();

        model.FromEmailAddress.Should().Be("from@example.com");
        model.DefaultToEmailAddress.Should().Be("to@example.com");
    }

    [Fact]
    public async Task OnGet_WithLastReference_PrefillsNextNumberAndClearsTempData()
    {
        _service.GetLastReferenceAsync("GB").Returns(Task.FromResult<LastAdnsReferenceRecord?>(
            new LastAdnsReferenceRecord { LastAdnsReferenceYear = 2025, LastAdnsReferenceNumber = 41 }));
        var model = CreateModel();
        model.TempData["AdnsGbPreview"] = "x";
        model.TempData["AdnsGbContext"] = "y";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.AdnsYear.Should().Be(2025);
        model.StartAdnsNumber.Should().Be(42);
        model.EmailReference.Should().Be("DBSE00042");
        model.UserEmailAddress.Should().Be("to@example.com");
        model.TempData.ContainsKey("AdnsGbPreview").Should().BeFalse();
        model.TempData.ContainsKey("AdnsGbContext").Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_LastReferenceWithNullParts_UsesDefaults()
    {
        _service.GetLastReferenceAsync("GB").Returns(Task.FromResult<LastAdnsReferenceRecord?>(new LastAdnsReferenceRecord()));
        var model = CreateModel();

        await model.OnGetAsync();

        model.AdnsYear.Should().Be(DateTime.Today.Year);
        model.StartAdnsNumber.Should().Be(1);
        model.EmailReference.Should().Be("DBSE00001");
    }

    [Fact]
    public async Task OnGet_NoLastReference_KeepsDefaultsAndExistingEmail()
    {
        _service.GetLastReferenceAsync("GB").Returns(Task.FromResult<LastAdnsReferenceRecord?>(null));
        var model = CreateModel();
        model.UserEmailAddress = "me@example.com";

        await model.OnGetAsync();

        model.EmailReference.Should().BeEmpty();
        model.UserEmailAddress.Should().Be("me@example.com");
    }

    [Fact]
    public async Task GenerateReport_InvalidModel_ReturnsPageWithDefaultEmail()
    {
        var model = CreateModel();
        model.ModelState.AddModelError(nameof(model.EmailReference), "required");

        var result = await model.OnPostGenerateReportAsync();

        result.Should().BeOfType<PageResult>();
        model.UserEmailAddress.Should().Be("to@example.com");
        model.Preview.Should().BeNull();
        await _service.DidNotReceiveWithAnyArgs().PreviewGbExportAsync(default!, default, default);
    }

    [Fact]
    public async Task GenerateReport_InvalidModelWithEmail_KeepsEmail()
    {
        var model = CreateModel();
        model.UserEmailAddress = "me@example.com";
        model.ModelState.AddModelError(nameof(model.EmailReference), "required");

        await model.OnPostGenerateReportAsync();

        model.UserEmailAddress.Should().Be("me@example.com");
    }

    [Fact]
    public async Task GenerateReport_Success_SetsPreviewMessageAndTempData()
    {
        var model = await GeneratedModel(Case("000000001"));

        model.Preview.Should().NotBeNull();
        model.Message.Should().Be("body text");
        model.ErrorMessage.Should().BeNull();
        model.TempData.ContainsKey("AdnsGbPreview").Should().BeTrue();
        model.TempData.ContainsKey("AdnsGbContext").Should().BeTrue();
        model.UserEmailAddress.Should().Be("to@example.com");
    }

    [Fact]
    public async Task GenerateReport_ServiceThrows_SetsErrorMessage()
    {
        _service.PreviewGbExportAsync(default!, default, default).ReturnsForAnyArgs<Task<AdnsExportPreview>>(_ => throw new InvalidOperationException("boom"));
        var model = CreateModel();
        model.EmailReference = "DBSE00005";

        var result = await model.OnPostGenerateReportAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be("Report generation failed: boom");
        model.Preview.Should().BeNull();
    }

    [Fact]
    public async Task Dispatch_NoPreview_ReportsSessionExpired()
    {
        var model = CreateModel();

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Contain("Session expired");
        await _service.DidNotReceiveWithAnyArgs().DispatchAsync(default!);
    }

    [Fact]
    public async Task Dispatch_BlankEmail_AddsModelError()
    {
        var model = await GeneratedModel(Case("000000001"));
        model.UserEmailAddress = " ";

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[nameof(model.UserEmailAddress)]!.Errors.Single().ErrorMessage.Should().Be("Enter your email address.");
        await _service.DidNotReceiveWithAnyArgs().DispatchAsync(default!);
    }

    [Fact]
    public async Task Dispatch_BadEmail_AddsFormatError()
    {
        var model = await GeneratedModel(Case("000000001"));
        model.UserEmailAddress = "not-an-email";

        await model.OnPostDispatchAsync();

        model.ModelState[nameof(model.UserEmailAddress)]!.Errors.Single().ErrorMessage.Should().Contain("correct format");
        await _service.DidNotReceiveWithAnyArgs().DispatchAsync(default!);
    }

    [Fact]
    public async Task Dispatch_Valid_SendsCommandAndRedirects()
    {
        var model = await GeneratedModel(Case("000000001"), Case("000000002"));
        model.EmailReference = "tampered";
        model.AdnsYear = 1999;
        model.UserEmailAddress = "me@example.com";
        model.Message = "  hello  ";
        model.SaveAdnsData = false;

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["SuccessMessage"].Should().NotBeNull();
        model.TempData.ContainsKey("AdnsGbPreview").Should().BeFalse();
        await _service.Received(1).DispatchAsync(Arg.Is<DispatchAdnsCommand>(c =>
            c.Area == "GB" && c.EmailReference == "DBSE00005" && c.Cases.Count == 2 &&
            c.UserEmailAddress == "me@example.com" && !c.SaveAdnsData && c.Message == "hello"));
        model.AdnsYear.Should().Be(2026);
    }

    [Fact]
    public async Task Dispatch_ServiceThrows_SetsErrorMessage()
    {
        _service.DispatchAsync(default!).ReturnsForAnyArgs(_ => throw new InvalidOperationException("smtp"));
        var model = await GeneratedModel(Case("000000001"));
        model.UserEmailAddress = "me@example.com";

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be("Dispatch failed: smtp");
    }

    [Theory]
    [InlineData("Region", false, "000000002")]
    [InlineData("Region", true, "000000003")]
    [InlineData("AdnsReference", false, "000000003")]
    [InlineData("AdnsReference", true, "000000002")]
    [InlineData("ConfirmationDate", false, "000000002")]
    [InlineData("ConfirmationDate", true, "000000003")]
    [InlineData("Rbse", true, "000000003")]
    [InlineData("", false, "000000001")]
    public async Task GetPagedCases_SortsByColumn(string column, bool desc, string expectedFirst)
    {
        var model = await GeneratedModel(
            Case("000000001", "B", "2026/00002", 5),
            Case("000000002", "A", "2026/00003", 1),
            Case("000000003", "C", "2026/00001", 9));
        model.SortColumn = column;
        model.SortDesc = desc;

        model.GetPagedCases().First().Rbse.Should().Be(expectedFirst);
    }

    [Fact]
    public async Task Paging_ComputesTotalsAndSlices()
    {
        var model = await GeneratedModel(Enumerable.Range(1, 12).Select(i => Case(i.ToString("D9"))).ToArray());
        model.PageNumber = 2;

        model.TotalCount.Should().Be(12);
        model.TotalPages.Should().Be(2);
        model.GetPagedCases().Should().HaveCount(2);
    }

    [Fact]
    public void Paging_NoPreview_EmptyAndSinglePage()
    {
        var model = CreateModel();

        model.TotalCount.Should().Be(0);
        model.TotalPages.Should().Be(1);
        model.GetPagedCases().Should().BeEmpty();
    }
}
