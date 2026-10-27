using BSE.Host.Pages.CaseWork;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class MinutePageModelTests
{
    private readonly ICaseWorkService _service = Substitute.For<ICaseWorkService>();
    private readonly TestSession _session = new();

    private sealed class SessionFeature(ISession session) : ISessionFeature
    {
        public ISession Session { get; set; } = session;
    }

    private MinuteModel CreateModel(string type, string query = "")
    {
        var http = new DefaultHttpContext();
        http.Features.Set<ISessionFeature>(new SessionFeature(_session));
        http.Request.QueryString = new QueryString(query);
        return new MinuteModel(_service)
        {
            Rbse = "002600001",
            Type = type,
            PageContext = new PageContext { HttpContext = http }
        };
    }

    private void ServiceReturns(MinuteDetailsRecord? record) =>
        _service.GetMinuteDetailsAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(record);

    [Fact]
    public async Task OnGet_UnsupportedType_ReturnsBadRequest()
    {
        var model = CreateModel("Bogus");

        (await model.OnGetAsync()).Should().BeOfType<BadRequestResult>();
        await _service.DidNotReceiveWithAnyArgs().GetMinuteDetailsAsync(default!, default!);
    }

    [Theory]
    [InlineData("ActiveMemo", "Active Memo")]
    [InlineData("ActiveMemoFS", "Active Memo")]
    [InlineData("AnnexA", "Annex A")]
    [InlineData("Annex C", "Annex C")]
    public async Task OnGet_SupportedType_NormalisesAndLoadsDetails(string type, string label)
    {
        ServiceReturns(new MinuteDetailsRecord { ActiveMemoDate = new DateTime(2026, 1, 2), AnnexADate = new DateTime(2026, 1, 2), AnnexCDate = new DateTime(2026, 1, 2) });
        var model = CreateModel(type);

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.MinuteTypeLabel.Should().Be(label);
        model.Details.Should().NotBeNull();
        model.MinuteDate.Should().Be(new DateTime(2026, 1, 2));
    }

    [Theory]
    [InlineData("AnnexB")]
    [InlineData("AnnexD")]
    public async Task OnGet_AnnexBAndD_PicksCorrespondingDate(string type)
    {
        ServiceReturns(new MinuteDetailsRecord { AnnexBDate = new DateTime(2026, 3, 4), AnnexDDate = new DateTime(2026, 3, 4) });
        var model = CreateModel(type);

        await model.OnGetAsync();

        model.MinuteDate.Should().Be(new DateTime(2026, 3, 4));
    }

    [Fact]
    public async Task OnGet_NoDetails_MinuteDateNull()
    {
        ServiceReturns(null);
        var model = CreateModel("AnnexA");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Details.Should().BeNull();
        model.MinuteDate.Should().BeNull();
    }

    [Fact]
    public async Task OnGet_TypeFromLegacyQuery_IsUsed()
    {
        ServiceReturns(new MinuteDetailsRecord());
        var model = CreateModel("", "?minute=AnnexA");

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Type.Should().Be("AnnexA");
    }

    [Fact]
    public async Task OnGet_AnnexC_RestoresOutstandingSelectionFromSession()
    {
        ServiceReturns(new MinuteDetailsRecord());
        _session.SetString("CaseWork.OutstandingForms.Homebred", "True");
        _session.SetString("CaseWork.OutstandingForms.Vendor", "True");
        _session.SetString("CaseWork.OutstandingForms.Breeder", "garbage");
        var model = CreateModel("AnnexC");

        await model.OnGetAsync();

        model.ShowOutstandingForms.Should().BeTrue();
        model.OutstandingHomebred.Should().BeTrue();
        model.OutstandingVendor.Should().BeTrue();
        model.OutstandingBreeder.Should().BeFalse();
        model.OutstandingPurchaser.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_AnnexA_DoesNotShowOutstandingForms()
    {
        ServiceReturns(new MinuteDetailsRecord());
        var model = CreateModel("AnnexA");

        await model.OnGetAsync();

        model.ShowOutstandingForms.Should().BeFalse();
    }

    [Fact]
    public async Task Download_UnsupportedType_ReturnsBadRequest()
    {
        (await CreateModel("x").OnPostDownloadAsync()).Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Download_NoDetails_ReturnsNotFound()
    {
        ServiceReturns(null);

        (await CreateModel("AnnexA").OnPostDownloadAsync()).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Download_AnnexCWithoutSelection_ReturnsPageWithModelError()
    {
        ServiceReturns(new MinuteDetailsRecord());
        var model = CreateModel("AnnexC");

        (await model.OnPostDownloadAsync()).Should().BeOfType<PageResult>();

        model.ModelState["OutstandingForms"]!.Errors.Should().ContainSingle();
        _session.Keys.Should().BeEmpty();
    }

    [Fact]
    public async Task Download_AnnexDWithSelection_SavesSessionAndRedirects()
    {
        ServiceReturns(new MinuteDetailsRecord());
        var model = CreateModel("AnnexD");
        model.OutstandingBreeder = true;

        var result = await model.OnPostDownloadAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/CaseWork/MinuteDocument");
        redirect.RouteValues!["type"].Should().Be("AnnexD");
        redirect.RouteValues["rbse"].Should().Be("002600001");
        _session.GetString("CaseWork.OutstandingForms.Breeder").Should().Be("True");
        _session.GetString("CaseWork.OutstandingForms.Homebred").Should().Be("False");
    }

    [Fact]
    public async Task Download_AnnexA_RedirectsWithoutValidation()
    {
        ServiceReturns(new MinuteDetailsRecord());

        (await CreateModel("AnnexA").OnPostDownloadAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task PrintMemo_UnsupportedType_ReturnsBadRequest()
    {
        (await CreateModel("x").OnPostPrintMemoAsync()).Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task PrintMemo_NoDetails_ReturnsNotFound()
    {
        ServiceReturns(null);

        (await CreateModel("AnnexA").OnPostPrintMemoAsync()).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PrintMemo_AnnexCWithoutSelection_ReturnsPageWithoutPopup()
    {
        ServiceReturns(new MinuteDetailsRecord());
        var model = CreateModel("AnnexC");

        (await model.OnPostPrintMemoAsync()).Should().BeOfType<PageResult>();

        model.TriggerPrintPopup.Should().BeFalse();
        model.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PrintMemo_Valid_TriggersPopupWithUrl()
    {
        ServiceReturns(new MinuteDetailsRecord());
        var model = CreateModel("AnnexC");
        model.OutstandingAllPaperwork = true;
        var url = Substitute.For<IUrlHelper>();
        url.ActionContext.Returns(new ActionContext(model.HttpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
        url.RouteUrl(Arg.Any<Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext>()).Returns("/doc?print=Print");
        model.Url = url;

        (await model.OnPostPrintMemoAsync()).Should().BeOfType<PageResult>();

        model.TriggerPrintPopup.Should().BeTrue();
        model.PrintPopupUrl.Should().Be("/doc?print=Print");
        _session.GetString("CaseWork.OutstandingForms.AllPaperwork").Should().Be("True");
    }
}
