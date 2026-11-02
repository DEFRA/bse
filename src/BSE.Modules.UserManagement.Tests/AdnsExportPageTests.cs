using BSE.Host.Pages.AdnsExport;
using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class AdnsExportPageTests
{
    private readonly IAdnsExportService _service = Substitute.For<IAdnsExportService>();
    private readonly IOptions<AdnsMsGraphOptions> _options =
        Options.Create(new AdnsMsGraphOptions { FromAddress = "from@example.com", ToAddress = "to@example.com" });

    private static AdnsExportPreview Preview(params AdnsCaseRecord[] cases) =>
        new(cases, [], [], "subject", "body text", "2026/00001", "2026/00002");

    private static AdnsCaseRecord Case(string rbse, int number, string region, DateTime date) => new()
    {
        Rbse = rbse, AdnsYear = 2026, AdnsNumber = number, AdnsRegionName = region, ConfirmationDate = date,
        AdnsReference = $"2026/{number:00000}"
    };

    private T Create<T>(Func<IAdnsExportService, IOptions<AdnsMsGraphOptions>, T> factory) where T : PageModel
    {
        var model = factory(_service, _options);
        PageTestHelpers.Attach(model, "DEFRAMaintenance");
        return model;
    }

    // ── GB ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Gb_OnGetAsync_PrefillsFromTheLastReference()
    {
        _service.GetLastReferenceAsync("GB").Returns(new LastAdnsReferenceRecord { LastAdnsReferenceYear = 2025, LastAdnsReferenceNumber = 41 });
        var model = Create((s, o) => new GbModel(s, o));

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.AdnsYear.Should().Be(2025);
        model.StartAdnsNumber.Should().Be(42);
        model.EmailReference.Should().Be("DBSE00042");
        model.UserEmailAddress.Should().Be("to@example.com");
        model.FromEmailAddress.Should().Be("from@example.com");
    }

    [Fact]
    public async Task Gb_OnPostGenerateReportAsync_BuildsAndStoresThePreview()
    {
        var model = Create((s, o) => new GbModel(s, o));
        model.EmailReference = "DBSE00001";
        _service.PreviewGbExportAsync("DBSE00001", Arg.Any<int>(), Arg.Any<int>())
            .Returns(Preview(Case("A", 2, "R2", new DateTime(2026, 1, 2)), Case("B", 1, "R1", new DateTime(2026, 1, 3))));

        await model.OnPostGenerateReportAsync();

        model.Preview.Should().NotBeNull();
        model.Message.Should().Be("body text");
        model.TotalCount.Should().Be(2);
        model.TotalPages.Should().Be(1);
        model.PagedCases().First().Rbse.Should().Be("A");
    }

    [Theory]
    [InlineData("Region", false, "B")]
    [InlineData("Region", true, "A")]
    [InlineData("AdnsReference", false, "B")]
    [InlineData("ConfirmationDate", true, "B")]
    [InlineData("Rbse", true, "B")]
    public async Task Gb_PagedCases_SortsByTheRequestedColumn(string column, bool desc, string expectedFirst)
    {
        var model = Create((s, o) => new GbModel(s, o));
        model.EmailReference = "DBSE00001";
        model.SortColumn = column;
        model.SortDesc = desc;
        _service.PreviewGbExportAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Preview(Case("A", 2, "R2", new DateTime(2026, 1, 2)), Case("B", 1, "R1", new DateTime(2026, 1, 3))));
        await model.OnPostGenerateReportAsync();

        model.PagedCases().First().Rbse.Should().Be(expectedFirst);
    }

    [Fact]
    public async Task Gb_OnPostGenerateReportAsync_WhenTheServiceFails_ShowsAnError()
    {
        var model = Create((s, o) => new GbModel(s, o));
        model.EmailReference = "DBSE00001";
        _service.PreviewGbExportAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns<AdnsExportPreview>(_ => throw new InvalidOperationException("db down"));

        await model.OnPostGenerateReportAsync();

        model.ErrorMessage.Should().Contain("db down");
    }

    [Fact]
    public async Task Gb_OnPostGenerateReportAsync_WithoutAReference_FailsValidation()
    {
        var model = Create((s, o) => new GbModel(s, o));
        model.ModelState.AddModelError("EmailReference", "required");

        await model.OnPostGenerateReportAsync();

        model.Preview.Should().BeNull();
        model.UserEmailAddress.Should().Be("to@example.com");
    }

    [Fact]
    public async Task Gb_OnPostDispatchAsync_WithoutAPreview_ReportsAnExpiredSession()
    {
        var model = Create((s, o) => new GbModel(s, o));

        await model.OnPostDispatchAsync();

        model.ErrorMessage.Should().Contain("Session expired");
    }

    [Fact]
    public async Task Gb_Dispatch_WithAPreview_ValidatesTheRecipientThenSends()
    {
        var model = Create((s, o) => new GbModel(s, o));
        model.EmailReference = "DBSE00001";
        _service.PreviewGbExportAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>()).Returns(Preview(Case("A", 1, "R", DateTime.Today)));
        await model.OnPostGenerateReportAsync();

        model.UserEmailAddress = "not-an-email";
        await model.OnPostDispatchAsync();
        PageTestHelpers.ErrorMessages(model, "UserEmailAddress").Should().NotBeEmpty();

        model.ModelState.Clear();
        model.UserEmailAddress = "me@example.com";
        model.Message = " hi ";
        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _service.Received(1).DispatchAsync(Arg.Is<DispatchAdnsCommand>(c => c.Area == "GB" && c.Message == "hi"));
    }

    [Fact]
    public async Task Gb_Dispatch_WhenSendingFails_ShowsTheError()
    {
        var model = Create((s, o) => new GbModel(s, o));
        model.EmailReference = "DBSE00001";
        _service.PreviewGbExportAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>()).Returns(Preview(Case("A", 1, "R", DateTime.Today)));
        await model.OnPostGenerateReportAsync();
        model.UserEmailAddress = "me@example.com";
        _service.DispatchAsync(Arg.Any<DispatchAdnsCommand>()).Returns(Task.FromException(new InvalidOperationException("smtp")));

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Contain("Dispatch failed: smtp");
    }

    // ── CI ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ci_OnGetAsync_PrefillsFromTheLastReference()
    {
        _service.GetLastReferenceAsync("CI").Returns(new LastAdnsReferenceRecord { LastAdnsReferenceNumber = 9 });
        var model = Create((s, o) => new CiModel(s, o));

        await model.OnGetAsync();

        model.StartAdnsNumber.Should().Be(10);
        model.EmailReference.Should().Be("DBSE00010");
    }

    [Fact]
    public void Ci_OnPostGenerateReport_BuildsThePreviewFromTheCaseCounts()
    {
        var model = Create((s, o) => new CiModel(s, o));
        model.EmailReference = "DBSE00001";
        model.JerseyCases = 2;
        model.ConfirmationDate = "5/3/2026";
        _service.PreviewCiExport("DBSE00001", Arg.Any<int>(), Arg.Any<int>(), Arg.Is<int>(i => i == 2), Arg.Any<int>(), Arg.Any<int>(), new DateTime(2026, 3, 5)).Returns(Preview());

        var result = model.OnPostGenerateReport();

        result.Should().BeOfType<PageResult>();
        model.Preview.Should().NotBeNull();
        model.Message.Should().Be("body text");
    }

    [Fact]
    public void Ci_OnPostGenerateReport_RejectsAnUnparseableDate()
    {
        var model = Create((s, o) => new CiModel(s, o));
        model.ConfirmationDate = "garbage";

        model.OnPostGenerateReport();

        PageTestHelpers.ErrorMessages(model, "ConfirmationDate").Should().ContainSingle();
    }

    [Fact]
    public void Ci_OnPostGenerateReport_WhenInvalid_DefaultsTheRecipient()
    {
        var model = Create((s, o) => new CiModel(s, o));
        model.ModelState.AddModelError("EmailReference", "required");

        model.OnPostGenerateReport();

        model.UserEmailAddress.Should().Be("to@example.com");
    }

    [Fact]
    public void Ci_OnPostGenerateReport_WhenTheServiceThrows_ShowsAnError()
    {
        var model = Create((s, o) => new CiModel(s, o));
        model.EmailReference = "DBSE00001";
        _service.PreviewCiExport(default!, default, default, default, default, default, default)
            .ReturnsForAnyArgs(_ => throw new InvalidOperationException("boom"));

        model.OnPostGenerateReport();

        model.ErrorMessage.Should().Contain("boom");
    }

    [Fact]
    public async Task Ci_Dispatch_RequiresAPreviewAndAValidRecipient()
    {
        var model = Create((s, o) => new CiModel(s, o));
        await model.OnPostDispatchAsync();
        model.ErrorMessage.Should().Contain("Session expired");

        model.EmailReference = "DBSE00001";
        _service.PreviewCiExport(default!, default, default, default, default, default, default).ReturnsForAnyArgs(Preview());
        model.OnPostGenerateReport();

        model.UserEmailAddress = "";
        await model.OnPostDispatchAsync();
        PageTestHelpers.ErrorMessages(model, "UserEmailAddress").Should().NotBeEmpty();

        model.UserEmailAddress = "me@example.com";
        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _service.Received(1).DispatchAsync(Arg.Is<DispatchAdnsCommand>(c => c.Area == "CI"));
    }

    // ── NI ──────────────────────────────────────────────────────────────────────

    private NiModel CreateNi()
    {
        var model = Create((s, o) => new NiModel(s, o));
        model.EmailReference = "DBSE00001";
        model.InputAdnsYear = 2026;
        model.InputAdnsNumber = 1;
        model.InputAdnsRegionId = 7300;
        model.InputConfirmationDate = "01/02/2026";
        return model;
    }

    [Fact]
    public async Task Ni_OnGetAsync_StartsCleanAndPrefillsTheReference()
    {
        _service.GetLastReferenceAsync("NI").Returns(new LastAdnsReferenceRecord { LastAdnsReferenceYear = 2025, LastAdnsReferenceNumber = 3 });
        var model = Create((s, o) => new NiModel(s, o));

        await model.OnGetAsync();

        model.InputAdnsNumber.Should().Be(4);
        model.EmailReference.Should().Be("DBSE00004");
        model.InputConfirmationDate.Should().Be(DateTime.Today.ToString("dd/MM/yyyy"));
        model.TotalCount.Should().Be(0);
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public void Ni_AddToGrid_AddsRowsAndResetsTheEntryFields()
    {
        var model = CreateNi();

        model.OnPostAddToGrid();
        model.InputAdnsNumber = 2;
        model.InputAdnsRegionId = 7301;
        model.InputConfirmationDate = "02/02/2026";
        model.OnPostAddToGrid();

        model.TotalCount.Should().Be(2);
        model.InputAdnsNumber.Should().Be(0);
        model.InputConfirmationDate.Should().Be(DateTime.Today.ToString("dd/MM/yyyy"));
        model.PagedRows().Select(r => r.AdnsReference).Should().Equal("2026/00001", "2026/00002");
    }

    [Theory]
    [InlineData("Region", true)]
    [InlineData("ConfirmationDate", false)]
    [InlineData("AdnsReference", true)]
    [InlineData("Other", false)]
    public void Ni_PagedRows_SortsByTheRequestedColumn(string column, bool desc)
    {
        var model = CreateNi();
        model.OnPostAddToGrid();
        model.InputAdnsNumber = 2;
        model.InputAdnsRegionId = 7301;
        model.InputConfirmationDate = "02/02/2026";
        model.OnPostAddToGrid();
        model.SortColumn = column;
        model.SortDesc = desc;

        model.PagedRows().Should().HaveCount(2);
    }

    [Fact]
    public void Ni_AddToGrid_RejectsABadDateAndInvalidModelState()
    {
        var model = CreateNi();
        model.InputConfirmationDate = "nope";
        model.OnPostAddToGrid();
        PageTestHelpers.ErrorMessages(model, "InputConfirmationDate").Should().ContainSingle();

        var invalid = CreateNi();
        invalid.ModelState.AddModelError("InputAdnsNumber", "bad");
        invalid.OnPostAddToGrid();
        invalid.TotalCount.Should().Be(0);

        var nullDate = CreateNi();
        nullDate.InputConfirmationDate = null;
        nullDate.OnPostAddToGrid();
        PageTestHelpers.ErrorMessages(nullDate, "InputConfirmationDate").Should().ContainSingle();
    }

    [Fact]
    public void Ni_DeleteFromGrid_RemovesTheMatchingRow()
    {
        var model = CreateNi();
        model.OnPostAddToGrid();
        var key = model.PagedRows().Single().RowKey;

        model.OnPostDeleteFromGrid("missing");
        PageTestHelpers.ErrorMessages(model, string.Empty).Should().Contain("Selected record could not be found.");

        model.OnPostDeleteFromGrid(key);
        model.TotalCount.Should().Be(0);

        model.OnPostDeleteFromGrid(" ");
        PageTestHelpers.ErrorMessages(model, string.Empty).Should().NotBeEmpty();
    }

    [Fact]
    public void Ni_GenerateReport_NeedsAtLeastOneRowThenBuildsThePreview()
    {
        var model = CreateNi();
        model.OnPostGenerateReport();
        PageTestHelpers.ErrorMessages(model, string.Empty).Should().Contain(m => m.StartsWith("Add at least one case"));

        model.ModelState.Clear();
        model.OnPostAddToGrid();
        _service.PreviewNiExport("DBSE00001", Arg.Any<IReadOnlyList<NiCaseInput>>())
            .Returns(Preview(Case("", 1, "07300", new DateTime(2026, 2, 1))));

        model.OnPostGenerateReport();

        model.Preview.Should().NotBeNull();
        model.CurrentRows().Should().ContainSingle();
        model.UserEmailAddress.Should().Be("to@example.com");

        var blankRef = CreateNi();
        blankRef.EmailReference = "";
        blankRef.OnPostGenerateReport();
        PageTestHelpers.ErrorMessages(blankRef, "EmailReference").Should().NotBeEmpty();
    }

    [Fact]
    public async Task Ni_Dispatch_RequiresARecipientAndAPreview()
    {
        var model = CreateNi();
        model.OnPostAddToGrid();

        model.UserEmailAddress = "";
        await model.OnPostDispatchAsync();
        PageTestHelpers.ErrorMessages(model, "UserEmailAddress").Should().NotBeEmpty();

        model.ModelState.Clear();
        model.UserEmailAddress = "me@example.com";
        await model.OnPostDispatchAsync();
        model.ErrorMessage.Should().Contain("Session expired");

        _service.PreviewNiExport(Arg.Any<string>(), Arg.Any<IReadOnlyList<NiCaseInput>>()).Returns(Preview(Case("", 1, "R", DateTime.Today)));
        model.OnPostGenerateReport();
        model.ModelState.Clear();
        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _service.Received(1).DispatchAsync(Arg.Is<DispatchAdnsCommand>(c => c.Area == "NI" && !c.SaveAdnsData));
    }

    [Fact]
    public async Task Ni_Dispatch_WhenSendingFails_ShowsTheError()
    {
        var model = CreateNi();
        model.OnPostAddToGrid();
        _service.PreviewNiExport(Arg.Any<string>(), Arg.Any<IReadOnlyList<NiCaseInput>>()).Returns(Preview(Case("", 1, "R", DateTime.Today)));
        model.OnPostGenerateReport();
        model.ModelState.Clear();
        model.UserEmailAddress = "me@example.com";
        _service.DispatchAsync(Arg.Any<DispatchAdnsCommand>()).Returns(Task.FromException(new InvalidOperationException("smtp")));

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Contain("smtp");
    }
}
