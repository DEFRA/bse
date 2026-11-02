using System.Data;
using System.Diagnostics.CodeAnalysis;
using BSE.Host.Pages.Case;
using BSE.Host.Pages.CaseWork;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class ClinicalVisitPagesTests
{
    private const string Rbse = "002600001";

    private readonly IClinicalRepository _clinical = Substitute.For<IClinicalRepository>();
    private readonly ICaseRepository _cases = Substitute.For<ICaseRepository>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _conn = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _tx = Substitute.For<IDbTransaction>();

    public ClinicalVisitPagesTests()
    {
        _conn.BeginTransaction().Returns(_tx);
        _factory.CreateConnection().Returns(_conn);
        _clinical.GetVisitsByRbseAsync(Rbse).Returns(new[]
        {
            new ClinicalVisitRecord(1, Rbse, new DateTime(2020, 6, 1), [1]),
            new ClinicalVisitRecord(2, Rbse, new DateTime(2020, 7, 1), [2])
        });
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, BirthDate = new DateTime(2019, 1, 1) });
    }

    private static void Wire(PageModel m)
    {
        var http = new DefaultHttpContext();
        m.PageContext = new PageContext { HttpContext = http };
        m.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
    }

    private ClinicalVisitAddModel Add()
    {
        var m = new ClinicalVisitAddModel(_clinical, _cases, _factory) { Rbse = Rbse };
        Wire(m);
        return m;
    }

    private ClinicalVisitEditModel Edit()
    {
        var m = new ClinicalVisitEditModel(_clinical, _cases, _factory) { Rbse = Rbse, Id = 1 };
        Wire(m);
        return m;
    }

    [Fact]
    public async Task Add_OnGet_RedirectsWhenCaseMissing()
    {
        _cases.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);

        (await Add().OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task Add_OnGet_ReturnsPage()
    {
        (await Add().OnGetAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task Add_Post_NoDate_AddsRequiredError()
    {
        var m = Add();

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        m.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Be("Enter a visit date.");
    }

    [Fact]
    public async Task Add_Post_BinderErrorAlreadyPresent_NotDuplicated()
    {
        var m = Add();
        m.ModelState.AddModelError("VisitDate", "Please enter a valid date");

        await m.OnPostAsync();

        m.ModelState["VisitDate"]!.Errors.Should().ContainSingle();
    }

    [Theory]
    [InlineData(2018, "on or after the birth date")]
    public async Task Add_Post_BeforeBirth_Rejected(int year, string fragment)
    {
        var m = Add();
        m.VisitDate = new DateTime(year, 1, 1);

        await m.OnPostAsync();

        m.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Contain(fragment);
    }

    [Fact]
    public async Task Add_Post_NoBirthDate_UsesEpochMessage()
    {
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse });
        var m = Add();
        m.VisitDate = new DateTime(1960, 1, 1);

        await m.OnPostAsync();

        m.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Contain("1 January 1970");
    }

    [Fact]
    public async Task Add_Post_Future_Rejected()
    {
        var m = Add();
        m.VisitDate = DateTime.Today.AddDays(3);

        await m.OnPostAsync();

        m.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Contain("future");
    }

    [Fact]
    public async Task Add_Post_Duplicate_Rejected()
    {
        var m = Add();
        m.VisitDate = new DateTime(2020, 6, 1);

        await m.OnPostAsync();

        m.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Contain("unique");
    }

    [Fact]
    public async Task Add_Post_Valid_SavesAndRedirects()
    {
        var m = Add();
        m.VisitDate = new DateTime(2020, 8, 1);

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Clinical");
        await _clinical.Received(1).AddVisitAsync(Arg.Any<AddClinicalVisitCommand>(), _conn, _tx);
        _tx.Received(1).Commit();
        m.TempData["Success"].Should().Be("Clinical visit added.");
    }

    [Fact]
    public async Task Edit_OnGet_NotFoundAndFound()
    {
        var missing = Edit();
        missing.Id = 99;
        (await missing.OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Clinical");

        var m = Edit();
        (await m.OnGetAsync()).Should().BeOfType<PageResult>();
        m.VisitDate.Should().Be(new DateTime(2020, 6, 1));
        m.RowStampBase64.Should().Be(Convert.ToBase64String([1]));
    }

    [Fact]
    public async Task Edit_Post_NoDate_Rejected()
    {
        var m = Edit();

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        m.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Be("Enter a visit date.");
    }

    [Fact]
    public async Task Edit_Post_BinderErrorPresent_NotDuplicated()
    {
        var m = Edit();
        m.ModelState.AddModelError("VisitDate", "Please enter a valid date");

        await m.OnPostAsync();

        m.ModelState["VisitDate"]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Edit_Post_BeforeBirth_AndEpochVariants()
    {
        var m = Edit();
        m.VisitDate = new DateTime(2018, 1, 1);
        await m.OnPostAsync();
        m.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Contain("birth date");

        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse });
        var epoch = Edit();
        epoch.VisitDate = new DateTime(1960, 1, 1);
        await epoch.OnPostAsync();
        epoch.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Contain("1 January 1970");
    }

    [Fact]
    public async Task Edit_Post_FutureAndDuplicateOfOtherVisit_Rejected()
    {
        var future = Edit();
        future.VisitDate = DateTime.Today.AddDays(2);
        await future.OnPostAsync();
        future.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Contain("future");

        var dup = Edit();
        dup.VisitDate = new DateTime(2020, 7, 1);
        await dup.OnPostAsync();
        dup.ModelState["VisitDate"]!.Errors.Single().ErrorMessage.Should().Contain("unique");
    }

    [Theory]
    [InlineData("")]
    [InlineData("AQ==")]
    public async Task Edit_Post_Valid_SavesAndRedirects(string rowStamp)
    {
        var m = Edit();
        m.VisitDate = new DateTime(2020, 6, 1);
        m.RowStampBase64 = rowStamp;

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Clinical");
        await _clinical.Received(1).EditVisitAsync(Arg.Any<EditClinicalVisitCommand>(), _conn, _tx);
        m.TempData["Success"].Should().Be("Clinical visit updated.");
    }
}

public sealed class MinutePageTests
{
    private const string Rbse = "002600001";

    private readonly ICaseWorkService _service = Substitute.For<ICaseWorkService>();
    private readonly FakeSession _session = new();

    private MinuteModel Create(string type, string? queryMinute = null)
    {
        var http = new DefaultHttpContext();
        http.Features.Set<ISessionFeature>(new SessionFeature { Session = _session });
        if (queryMinute is not null)
            http.Request.QueryString = new QueryString($"?minute={queryMinute}");
        var urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.ActionContext.Returns(new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
        urlHelper.RouteUrl(Arg.Any<UrlRouteContext>()).Returns("/popup");
        return new MinuteModel(_service)
        {
            Rbse = Rbse,
            Type = type,
            PageContext = new PageContext { HttpContext = http },
            Url = urlHelper
        };
    }

    private sealed class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = default!;
    }

    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = [];
        public bool IsAvailable => true;
        public string Id => "test";
        public IEnumerable<string> Keys => _data.Keys;
        public void Clear() => _data.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _data.Remove(key);
        public void Set(string key, byte[] value) => _data[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _data.TryGetValue(key, out value);
    }

    [Fact]
    public async Task OnGet_Unsupported_BadRequest()
    {
        (await Create("Nope").OnGetAsync()).Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnGet_TypeFromLegacyQueryString()
    {
        _service.GetMinuteDetailsAsync(Rbse, "AnnexA").Returns(new MinuteDetailsRecord { AnnexADate = new DateTime(2024, 1, 1) });
        var m = Create("", "AnnexA");

        (await m.OnGetAsync()).Should().BeOfType<PageResult>();

        m.Type.Should().Be("AnnexA");
        m.MinuteDate.Should().Be(new DateTime(2024, 1, 1));
        m.MinuteTypeLabel.Should().Be("Annex A");
    }

    [Fact]
    public async Task OnGet_AnnexC_RestoresSessionSelection()
    {
        _service.GetMinuteDetailsAsync(Rbse, "AnnexC").Returns((MinuteDetailsRecord?)null);
        foreach (var k in new[] { "Homebred", "Breeder", "Purchaser", "Vendor", "SummarySheet", "AllPaperwork" })
            _session.SetString($"CaseWork.OutstandingForms.{k}", "True");
        var m = Create("Annex C");

        await m.OnGetAsync();

        m.ShowOutstandingForms.Should().BeTrue();
        m.OutstandingHomebred.Should().BeTrue();
        m.OutstandingAllPaperwork.Should().BeTrue();
        m.MinuteDate.Should().BeNull();
    }

    [Theory]
    [InlineData("ActiveMemoFS", "AMFS")]
    [InlineData("Annex D", "AnnexD")]
    public async Task OnGet_NormalisesLegacyNames(string input, string expected)
    {
        _service.GetMinuteDetailsAsync(Rbse, expected).Returns(new MinuteDetailsRecord());
        var m = Create(input);

        await m.OnGetAsync();

        m.Type.Should().Be(expected);
    }

    [Fact]
    public async Task Download_UnsupportedOrMissing()
    {
        (await Create("Nope").OnPostDownloadAsync()).Should().BeOfType<BadRequestResult>();

        _service.GetMinuteDetailsAsync(Rbse, "AnnexA").Returns((MinuteDetailsRecord?)null);
        (await Create("AnnexA").OnPostDownloadAsync()).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Download_AnnexCWithoutSelection_ReturnsPageWithError()
    {
        _service.GetMinuteDetailsAsync(Rbse, "AnnexC").Returns(new MinuteDetailsRecord());
        var m = Create("AnnexC");

        (await m.OnPostDownloadAsync()).Should().BeOfType<PageResult>();
        m.ModelState.ContainsKey("OutstandingForms").Should().BeTrue();
    }

    [Fact]
    public async Task Download_Valid_SavesSelectionAndRedirects()
    {
        _service.GetMinuteDetailsAsync(Rbse, "AnnexC").Returns(new MinuteDetailsRecord { AnnexCDate = new DateTime(2024, 1, 1) });
        var m = Create("AnnexC");
        m.OutstandingBreeder = true;

        var result = await m.OnPostDownloadAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/CaseWork/MinuteDocument");
        _session.GetString("CaseWork.OutstandingForms.Breeder").Should().Be("True");
        m.MinuteDate.Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public async Task Download_NonAnnexC_DoesNotRequireSelection()
    {
        _service.GetMinuteDetailsAsync(Rbse, "ActiveMemo").Returns(new MinuteDetailsRecord());

        (await Create("ActiveMemo").OnPostDownloadAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task PrintMemo_Branches()
    {
        (await Create("Nope").OnPostPrintMemoAsync()).Should().BeOfType<BadRequestResult>();

        _service.GetMinuteDetailsAsync(Rbse, "AnnexD").Returns((MinuteDetailsRecord?)null);
        (await Create("AnnexD").OnPostPrintMemoAsync()).Should().BeOfType<NotFoundResult>();

        _service.GetMinuteDetailsAsync(Rbse, "AnnexD").Returns(new MinuteDetailsRecord());
        var invalid = Create("AnnexD");
        (await invalid.OnPostPrintMemoAsync()).Should().BeOfType<PageResult>();
        invalid.TriggerPrintPopup.Should().BeFalse();

        var ok = Create("AnnexD");
        ok.OutstandingVendor = true;
        (await ok.OnPostPrintMemoAsync()).Should().BeOfType<PageResult>();
        ok.TriggerPrintPopup.Should().BeTrue();
        ok.PrintPopupUrl.Should().Be("/popup");
    }
}
