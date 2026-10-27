using BSE.Host.Pages.CaseWork;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class MinuteDocumentPageModelTests
{
    private const string Rbse = "00/26/00001";
    private readonly ICaseWorkService _svc = Substitute.For<ICaseWorkService>();
    private readonly TestSession _session = new();

    private MinuteDocumentModel CreateModel()
    {
        var model = new MinuteDocumentModel(_svc);
        PageTestContext.Attach(model);
        model.HttpContext.Session = _session;
        return model;
    }

    private void Details(string type, MinuteDetailsRecord? record) =>
        _svc.GetMinuteDetailsAsync(Rbse, type).Returns(record);

    [Fact]
    public async Task OnGet_UnsupportedType_ReturnsBadRequest()
    {
        (await CreateModel().OnGetAsync(Rbse, "Nope", null)).Should().BeOfType<BadRequestResult>();
        await _svc.DidNotReceiveWithAnyArgs().GetMinuteDetailsAsync(default!, default!);
    }

    [Fact]
    public async Task OnGet_NoDetails_ReturnsNotFound()
    {
        Details("ActiveMemo", null);

        (await CreateModel().OnGetAsync(Rbse, "ActiveMemo", null)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGet_ActiveMemo_SetsWordDownloadHeaders()
    {
        var date = new DateTime(2024, 3, 4);
        Details("ActiveMemo", new MinuteDetailsRecord { Rbse = Rbse, ActiveMemoDate = date });
        var model = CreateModel();

        (await model.OnGetAsync(Rbse, "ActiveMemo", null)).Should().BeOfType<PageResult>();

        model.MinuteType.Should().Be("ActiveMemo");
        model.Rbse.Should().Be(Rbse);
        model.MinuteDate.Should().Be(date);
        model.IsPrintMode.Should().BeFalse();
        model.Response.ContentType.Should().Be("application/vnd.ms-word");
        model.Response.Headers["Content-Disposition"].ToString().Should().Contain("activememo002600001.doc");
    }

    [Fact]
    public async Task OnGet_ActiveMemoFs_NormalizesToAmfs()
    {
        Details("AMFS", new MinuteDetailsRecord { ActiveMemoDate = new DateTime(2024, 1, 1) });
        var model = CreateModel();

        await model.OnGetAsync(Rbse, "ActiveMemoFS", null);

        model.MinuteType.Should().Be("AMFS");
        model.MinuteDate.Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public async Task OnGet_PrintMode_DoesNotSetDownloadHeaders()
    {
        Details("AnnexA", new MinuteDetailsRecord { AnnexADate = new DateTime(2023, 2, 2) });
        var model = CreateModel();

        await model.OnGetAsync(Rbse, "AnnexA", "print");

        model.IsPrintMode.Should().BeTrue();
        model.Response.Headers.ContainsKey("Content-Disposition").Should().BeFalse();
        model.MinuteDate.Should().Be(new DateTime(2023, 2, 2));
    }

    [Fact]
    public async Task OnGet_AnnexB_UsesAnnexBDateAndFileName()
    {
        Details("AnnexB", new MinuteDetailsRecord { AnnexBDate = new DateTime(2023, 5, 5) });
        var model = CreateModel();

        await model.OnGetAsync(Rbse, "AnnexB", null);

        model.MinuteDate.Should().Be(new DateTime(2023, 5, 5));
        model.Response.Headers["Content-Disposition"].ToString().Should().Contain("annexb002600001.doc");
    }

    [Fact]
    public async Task OnGet_AnnexCWithoutSessionForms_RedirectsToSessionError()
    {
        Details("AnnexC", new MinuteDetailsRecord());

        var result = await CreateModel().OnGetAsync(Rbse, "Annex C", null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }

    [Fact]
    public async Task OnGet_AnnexDWithSessionForms_ListsAndClearsThem()
    {
        Details("AnnexD", new MinuteDetailsRecord { AnnexDDate = new DateTime(2022, 9, 9) });
        _session.SetString("CaseWork.OutstandingForms.Homebred", "True");
        _session.SetString("CaseWork.OutstandingForms.Vendor", "True");
        _session.SetString("CaseWork.OutstandingForms.Breeder", "False");
        _session.SetString("CaseWork.OutstandingForms.Purchaser", "True");
        _session.SetString("CaseWork.OutstandingForms.SummarySheet", "True");
        _session.SetString("CaseWork.OutstandingForms.AllPaperwork", "True");
        var model = CreateModel();

        (await model.OnGetAsync(Rbse, "Annex D", null)).Should().BeOfType<PageResult>();

        model.OutstandingForms.Should().Equal("BSE 1 - Homebred", "BSE 1 - Purchaser", "BSE 1 - Vendor", "Summary Sheet", "All Paperwork");
        _session.Keys.Should().BeEmpty();
        model.MinuteDate.Should().Be(new DateTime(2022, 9, 9));
    }
}
