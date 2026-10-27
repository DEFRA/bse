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

public class NiModelTests
{
    private readonly IAdnsExportService _service = Substitute.For<IAdnsExportService>();

    private NiModel CreateModel(string query = "")
    {
        var options = Options.Create(new AdnsMsGraphOptions { FromAddress = "from@example.com", ToAddress = "to@example.com" });
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString(query);
        return new NiModel(_service, options)
        {
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    private static AdnsExportPreview PreviewFor(IReadOnlyList<AdnsCaseRecord> cases) =>
        new(cases, [], [new AdnsRegionSummaryRecord(7300, "Region", cases.Count)], "subject", "body", "2026/00001", "2026/00002");

    private static void FillEntry(NiModel model, int number = 5, int region = 7300, string date = "15/01/2026")
    {
        model.EmailReference = "DBSE00005";
        model.InputAdnsYear = 2026;
        model.InputAdnsNumber = number;
        model.InputAdnsRegionId = region;
        model.InputConfirmationDate = date;
    }

    [Fact]
    public void ExposesConfiguredAddresses()
    {
        var model = CreateModel();

        model.FromAddress.Should().Be("from@example.com");
        model.DefaultToEmailAddress.Should().Be("to@example.com");
    }

    [Fact]
    public async Task OnGet_WithLastReference_PrefillsNextNumber()
    {
        _service.GetLastReferenceAsync("NI").Returns(Task.FromResult<LastAdnsReferenceRecord?>(
            new LastAdnsReferenceRecord { LastAdnsReferenceYear = 2025, LastAdnsReferenceNumber = 41 }));
        var model = CreateModel();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.InputAdnsYear.Should().Be(2025);
        model.InputAdnsNumber.Should().Be(42);
        model.EmailReference.Should().Be("DBSE00042");
        model.UserEmailAddress.Should().Be("to@example.com");
        model.InputConfirmationDate.Should().Be(DateTime.Today.ToString("dd/MM/yyyy"));
    }

    [Fact]
    public async Task OnGet_WithoutLastReference_KeepsDefaults()
    {
        _service.GetLastReferenceAsync("NI").Returns(Task.FromResult<LastAdnsReferenceRecord?>(null));
        var model = CreateModel();

        await model.OnGetAsync();

        model.InputAdnsNumber.Should().Be(0);
        model.EmailReference.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_NavigationQuery_RestoresStateWithoutCallingService()
    {
        var model = CreateModel("?PageNumber=1");
        model.UserEmailAddress = "";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.UserEmailAddress.Should().Be("to@example.com");
        await _service.DidNotReceive().GetLastReferenceAsync(Arg.Any<string>());
    }

    [Fact]
    public void AddToGrid_InvalidModelState_DoesNotAdd()
    {
        var model = CreateModel();
        model.ModelState.AddModelError("EmailReference", "bad");

        model.OnPostAddToGrid();

        model.DraftCases.Should().BeEmpty();
    }

    [Fact]
    public void AddToGrid_NullDate_AddsError()
    {
        var model = CreateModel();
        FillEntry(model);
        model.InputConfirmationDate = null;

        model.OnPostAddToGrid();

        model.ModelState.Should().ContainKey("InputConfirmationDate");
        model.DraftCases.Should().BeEmpty();
    }

    [Fact]
    public void AddToGrid_UnparseableDate_AddsError()
    {
        var model = CreateModel();
        FillEntry(model, date: "not a date");

        model.OnPostAddToGrid();

        model.ModelState["InputConfirmationDate"]!.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void AddToGrid_Valid_AddsRowAndResetsEntryFields()
    {
        var model = CreateModel();
        FillEntry(model);

        var result = model.OnPostAddToGrid();

        result.Should().BeOfType<PageResult>();
        model.DraftCases.Should().ContainSingle();
        model.DraftCases[0].AdnsNumber.Should().Be(5);
        model.DraftCases[0].ConfirmationDate.Should().Be(new DateTime(2026, 1, 15));
        model.InputAdnsNumber.Should().Be(0);
        model.InputAdnsRegionId.Should().Be(0);
        model.TotalCount.Should().Be(1);
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public void AddToGrid_SecondCall_RetainsPriorRowsFromTempData()
    {
        var model = CreateModel();
        FillEntry(model, number: 1);
        model.OnPostAddToGrid();
        FillEntry(model, number: 2);

        model.OnPostAddToGrid();

        model.DraftCases.Select(c => c.AdnsNumber).Should().Equal(1, 2);
    }

    [Fact]
    public void DeleteFromGrid_EmptyKey_AddsError()
    {
        var model = CreateModel();

        model.OnPostDeleteFromGrid("");

        model.ModelState[string.Empty]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public void DeleteFromGrid_UnknownKey_AddsError()
    {
        var model = CreateModel();

        model.OnPostDeleteFromGrid("nope");

        model.ModelState[string.Empty]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public void DeleteFromGrid_ExistingKey_RemovesRow()
    {
        var model = CreateModel();
        FillEntry(model);
        model.OnPostAddToGrid();
        var key = model.GetCurrentRows()[0].RowKey;

        model.OnPostDeleteFromGrid(key);

        model.DraftCases.Should().BeEmpty();
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public void GenerateReport_NoEmailReference_AddsError()
    {
        var model = CreateModel();
        model.EmailReference = "";

        model.OnPostGenerateReport();

        model.ModelState.Should().ContainKey("EmailReference");
        _service.DidNotReceive().PreviewNiExport(Arg.Any<string>(), Arg.Any<IReadOnlyList<NiCaseInput>>());
    }

    [Fact]
    public void GenerateReport_EmptyGrid_AddsError()
    {
        var model = CreateModel();
        model.EmailReference = "DBSE00001";

        model.OnPostGenerateReport();

        model.ModelState[string.Empty]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public void GenerateReport_WithRows_BuildsPreviewAndMessage()
    {
        var model = CreateModel();
        FillEntry(model);
        model.OnPostAddToGrid();
        var cases = new[] { new AdnsCaseRecord { AdnsYear = 2026, AdnsNumber = 5, AdnsRegionId = 7300, ConfirmationDate = new DateTime(2026, 1, 15) } };
        _service.PreviewNiExport("DBSE00005", Arg.Any<IReadOnlyList<NiCaseInput>>()).Returns(PreviewFor(cases));

        model.OnPostGenerateReport();

        model.Preview.Should().NotBeNull();
        model.Message.Should().Be("body");
        model.UserEmailAddress.Should().Be("to@example.com");
        model.GetCurrentRows().Should().ContainSingle(r => r.RegionName == "07300");
    }

    [Fact]
    public async Task Dispatch_MissingEmail_AddsError()
    {
        var model = CreateModel();
        model.UserEmailAddress = "";

        await model.OnPostDispatchAsync();

        model.ModelState.Should().ContainKey("UserEmailAddress");
    }

    [Fact]
    public async Task Dispatch_InvalidEmail_AddsError()
    {
        var model = CreateModel();
        model.UserEmailAddress = "not-an-email";

        await model.OnPostDispatchAsync();

        model.ModelState["UserEmailAddress"]!.Errors.Single().ErrorMessage.Should().Contain("correct format");
    }

    [Fact]
    public async Task Dispatch_NoPreviewInSession_ShowsExpiredMessage()
    {
        var model = CreateModel();
        model.UserEmailAddress = "me@example.com";

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Contain("Session expired");
    }

    private NiModel ModelWithGeneratedPreview()
    {
        var model = CreateModel();
        FillEntry(model);
        model.OnPostAddToGrid();
        var cases = new[] { new AdnsCaseRecord { AdnsYear = 2026, AdnsNumber = 5, AdnsRegionId = 7300, ConfirmationDate = new DateTime(2026, 1, 15) } };
        _service.PreviewNiExport(Arg.Any<string>(), Arg.Any<IReadOnlyList<NiCaseInput>>()).Returns(PreviewFor(cases));
        model.OnPostGenerateReport();
        model.UserEmailAddress = "me@example.com";
        model.Message = "  hello  ";
        return model;
    }

    [Fact]
    public async Task Dispatch_Success_SendsTrimmedMessageAndRedirects()
    {
        var model = ModelWithGeneratedPreview();

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _service.Received(1).DispatchAsync(Arg.Is<DispatchAdnsCommand>(c =>
            c.Area == "NI" && c.Message == "hello" && !c.SaveAdnsData && c.UserEmailAddress == "me@example.com"));
        model.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Dispatch_ServiceThrows_ShowsError()
    {
        var model = ModelWithGeneratedPreview();
        _service.DispatchAsync(Arg.Any<DispatchAdnsCommand>()).Returns(Task.FromException(new InvalidOperationException("boom")));

        var result = await model.OnPostDispatchAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be("Dispatch failed: boom");
    }

    [Theory]
    [InlineData("Region", false)]
    [InlineData("Region", true)]
    [InlineData("ConfirmationDate", false)]
    [InlineData("ConfirmationDate", true)]
    [InlineData("AdnsReference", false)]
    [InlineData("AdnsReference", true)]
    [InlineData("Other", true)]
    public void GetPagedRows_SortsAndPages(string column, bool desc)
    {
        var model = CreateModel();
        for (var i = 1; i <= 12; i++)
        {
            FillEntry(model, number: i, region: 7300 + i, date: $"{i:00}/01/2026");
            model.OnPostAddToGrid();
        }

        model.SortColumn = column;
        model.SortDesc = desc;
        model.PageNumber = 1;
        var first = model.GetPagedRows();
        model.PageNumber = 2;
        var second = model.GetPagedRows();

        first.Should().HaveCount(10);
        second.Should().HaveCount(2);
        model.TotalPages.Should().Be(2);
        if (column == "AdnsReference" && !desc)
            first[0].AdnsReference.Should().Be("2026/00001");
        if (column == "AdnsReference" && desc)
            first[0].AdnsReference.Should().Be("2026/00012");
    }
}
