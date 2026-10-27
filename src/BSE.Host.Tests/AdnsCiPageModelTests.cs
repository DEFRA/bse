using BSE.Host.Pages.AdnsExport;
using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BSE.Host.Tests;

public sealed class AdnsCiPageModelTests
{
    private const string PreviewKey = "AdnsCiPreview";

    private readonly IAdnsExportService _service = Substitute.For<IAdnsExportService>();

    private static AdnsExportPreview Preview() =>
        new([], [], [], "Subject", "Body text", "DBSE00005", "DBSE00007");

    private CiModel CreateModel()
    {
        var options = Options.Create(new AdnsMsGraphOptions { FromAddress = "from@x.gov.uk", ToAddress = "to@x.gov.uk" });
        var model = new CiModel(_service, options)
        {
            EmailReference = "DBSE00005",
            ConfirmationDate = "17/05/2024"
        };
        PageTestContext.Attach(model);
        return model;
    }

    [Fact]
    public void Addresses_ComeFromOptions()
    {
        var model = CreateModel();

        model.FromEmailAddress.Should().Be("from@x.gov.uk");
        model.DefaultToEmailAddress.Should().Be("to@x.gov.uk");
        model.SaveAdnsData.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_NoLastReference_KeepsDefaultsAndClearsPreview()
    {
        _service.GetLastReferenceAsync("CI").Returns((LastAdnsReferenceRecord?)null);
        var model = CreateModel();
        model.TempData[PreviewKey] = "stale";

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.TempData.ContainsKey(PreviewKey).Should().BeFalse();
        model.StartAdnsNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_WithLastReference_PrefillsNextNumber()
    {
        _service.GetLastReferenceAsync("CI").Returns(new LastAdnsReferenceRecord { LastAdnsReferenceYear = 2025, LastAdnsReferenceNumber = 41 });
        var model = CreateModel();

        await model.OnGetAsync();

        model.AdnsYear.Should().Be(2025);
        model.StartAdnsNumber.Should().Be(42);
        model.EmailReference.Should().Be("DBSE00042");
    }

    [Fact]
    public async Task OnGet_LastReferenceWithNulls_FallsBack()
    {
        _service.GetLastReferenceAsync("CI").Returns(new LastAdnsReferenceRecord());
        var model = CreateModel();

        await model.OnGetAsync();

        model.AdnsYear.Should().Be(DateTime.Today.Year);
        model.StartAdnsNumber.Should().Be(1);
    }

    [Fact]
    public void Generate_InvalidModelState_ReturnsPageWithDefaultRecipient()
    {
        var model = CreateModel();
        model.ModelState.AddModelError(nameof(CiModel.EmailReference), "bad");

        model.OnPostGenerateReport().Should().BeOfType<PageResult>();

        model.UserEmailAddress.Should().Be("to@x.gov.uk");
        _service.DidNotReceiveWithAnyArgs().PreviewCiExport(default!, default, default, default, default, default, default);
    }

    [Fact]
    public void Generate_UserEmailErrorIsIgnored()
    {
        _service.PreviewCiExport(default!, default, default, default, default, default, default).ReturnsForAnyArgs(Preview());
        var model = CreateModel();
        model.ModelState.AddModelError(nameof(CiModel.UserEmailAddress), "ignored");

        model.OnPostGenerateReport();

        model.Preview.Should().NotBeNull();
    }

    [Fact]
    public void Generate_BadDate_AddsModelError()
    {
        var model = CreateModel();
        model.ConfirmationDate = "2024-05-17";

        model.OnPostGenerateReport().Should().BeOfType<PageResult>();

        model.ModelState[nameof(CiModel.ConfirmationDate)]!.Errors.Should().ContainSingle();
        model.Preview.Should().BeNull();
        model.UserEmailAddress.Should().Be("to@x.gov.uk");
    }

    [Fact]
    public void Generate_Valid_BuildsPreviewAndStoresInTempData()
    {
        _service.PreviewCiExport(default!, default, default, default, default, default, default).ReturnsForAnyArgs(Preview());
        var model = CreateModel();
        model.AdnsYear = 2024;
        model.StartAdnsNumber = 5;
        model.JerseyCases = 1;
        model.GuernseyCases = 2;
        model.IsleOfManCases = 3;
        model.UserEmailAddress = "me@x.gov.uk";

        model.OnPostGenerateReport().Should().BeOfType<PageResult>();

        _service.Received(1).PreviewCiExport("DBSE00005", 2024, 5, 1, 2, 3, new DateTime(2024, 5, 17));
        model.Message.Should().Be("Body text");
        model.UserEmailAddress.Should().Be("me@x.gov.uk");
        model.TempData[PreviewKey].Should().NotBeNull();
    }

    [Fact]
    public void Generate_ServiceThrows_SetsErrorMessage()
    {
        _service.PreviewCiExport(default!, default, default, default, default, default, default)
            .ReturnsForAnyArgs(_ => throw new InvalidOperationException("boom"));
        var model = CreateModel();

        model.OnPostGenerateReport().Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be("Report generation failed: boom");
        model.UserEmailAddress.Should().Be("to@x.gov.uk");
    }

    [Fact]
    public async Task Dispatch_NoPreview_ReportsSessionExpired()
    {
        var model = CreateModel();

        (await model.OnPostDispatchAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Contain("Session expired");
        await _service.DidNotReceiveWithAnyArgs().DispatchAsync(default!);
    }

    private CiModel ModelWithPreview()
    {
        var model = CreateModel();
        model.TempData[PreviewKey] = System.Text.Json.JsonSerializer.Serialize(Preview());
        return model;
    }

    [Fact]
    public async Task Dispatch_BlankEmail_AddsModelError()
    {
        var model = ModelWithPreview();

        (await model.OnPostDispatchAsync()).Should().BeOfType<PageResult>();

        model.ModelState[nameof(CiModel.UserEmailAddress)]!.Errors.Single().ErrorMessage.Should().Be("Enter your email address.");
        model.Preview.Should().NotBeNull();
    }

    [Fact]
    public async Task Dispatch_InvalidEmail_AddsFormatError()
    {
        var model = ModelWithPreview();
        model.UserEmailAddress = "not-an-email";

        await model.OnPostDispatchAsync();

        model.ModelState[nameof(CiModel.UserEmailAddress)]!.Errors.Single().ErrorMessage.Should().Contain("correct format");
        await _service.DidNotReceiveWithAnyArgs().DispatchAsync(default!);
    }

    [Fact]
    public async Task Dispatch_Valid_SendsCommandAndRedirects()
    {
        var model = ModelWithPreview();
        model.UserEmailAddress = "me@x.gov.uk";
        model.Message = "  hello ";

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _service.Received(1).DispatchAsync(Arg.Is<DispatchAdnsCommand>(c =>
            c.Area == "CI" && c.EmailReference == "DBSE00005" && c.UserEmailAddress == "me@x.gov.uk"
            && c.Message == "hello" && !c.SaveAdnsData));
        model.TempData.ContainsKey(PreviewKey).Should().BeFalse();
        model.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Dispatch_ServiceThrows_ReportsFailure()
    {
        _service.DispatchAsync(default!).ReturnsForAnyArgs(_ => throw new InvalidOperationException("smtp"));
        var model = ModelWithPreview();
        model.UserEmailAddress = "me@x.gov.uk";

        (await model.OnPostDispatchAsync()).Should().BeOfType<PageResult>();

        model.ErrorMessage.Should().Be("Dispatch failed: smtp");
    }
}
