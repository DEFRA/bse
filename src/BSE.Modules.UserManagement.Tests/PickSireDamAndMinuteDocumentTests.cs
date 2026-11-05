using BSE.Host.Pages.Case;
using BSE.Host.Pages.CaseWork;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class PickSireDamModelTests
{
    private readonly IAnimalRelationsRepository _repo = Substitute.For<IAnimalRelationsRepository>();

    private PickSireDamModel Create(string rbse = "002600001", string sex = "F")
    {
        var model = new PickSireDamModel(_repo) { Rbse = rbse, Sex = sex };
        var http = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private static DamSireDetailRecord M(int id, string name, string eartag = "E", string herdbook = "H", string rbse = "R") =>
        new() { Id = id, Name = name, Eartag = eartag, Herdbook = herdbook, Rbse = rbse };

    [Fact]
    public async Task OnGet_MissingRbseOrSex_RedirectsHome()
    {
        (await Create(rbse: "").OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        (await Create(sex: "").OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnGet_RestoresContextFromTempData()
    {
        _repo.GetDamSireDetailsMatchesAsync("ET", "Nm", null, "HB", "F").Returns([M(1, "Nm")]);
        var model = Create(rbse: "");
        model.TempData[PickSireDamContextKeys.Rbse] = "002600001";
        model.TempData[PickSireDamContextKeys.ReturnTo] = "Other";
        model.TempData[PickSireDamContextKeys.Eartag] = "ET";
        model.TempData[PickSireDamContextKeys.Name] = "Nm";
        model.TempData[PickSireDamContextKeys.Herdbook] = "HB";

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Rbse.Should().Be("002600001");
        model.ReturnTo.Should().Be("Other");
        model.TotalMatchCount.Should().Be(1);
        model.IsDam.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_BlankSearch_DefaultsReturnToAndSkipsTempDataCriteria()
    {
        _repo.GetDamSireDetailsMatchesAsync(null, null, null, null, "M").Returns([]);
        var model = Create(sex: "M");
        model.UseBlankSearch = true;
        model.TempData[PickSireDamContextKeys.Eartag] = "ignored";

        await model.OnGetAsync();

        model.ReturnTo.Should().Be("Relations");
        model.IsDam.Should().BeFalse();
        model.TotalPages.Should().Be(1);
    }

    [Theory]
    [InlineData("Eartag", true)] [InlineData("Eartag", false)]
    [InlineData("Herdbook", true)] [InlineData("Herdbook", false)]
    [InlineData("Name", true)] [InlineData("Name", false)]
    [InlineData("Rbse", true)] [InlineData("Rbse", false)]
    [InlineData(null, false)]
    public async Task OnGet_SortsAndPages(string? column, bool desc)
    {
        _repo.GetDamSireDetailsMatchesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>())
            .Returns(Enumerable.Range(1, 12).Select(i => M(i, $"N{i:00}", $"E{i:00}", $"H{i:00}", $"R{i:00}")).ToList());
        var model = Create();
        model.Name = "x";
        model.SortColumn = column;
        model.SortDesc = desc;
        model.PageNumber = 9;

        await model.OnGetAsync();

        model.TotalMatchCount.Should().Be(12);
        model.TotalPages.Should().Be(2);
        model.PageNumber.Should().Be(2);
        model.PagedMatches.Should().HaveCount(2);
    }

    [Fact]
    public void UseSelected_StoresPendingDamAndRedirects()
    {
        var model = Create(sex: "F");

        var result = model.OnPostUseSelected(new PickedPedigreeForm
        {
            Id = 7, MatchedRbse = " ", Eartag = "E1", Name = "Name", Herdbook = "HB",
            BirthDay = 1, BirthMonth = 2, BirthYear = 2020, RowStampBase64 = "AQ==",
            Fate = "fate", FinalResult = "res", ChildCount = 3
        });

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Relations");
        model.TempData[PendingDamSireKeys.Dam]!.ToString().Should().Contain("\"Id\":7").And.Contain("\"Rbse\":null");
    }

    [Fact]
    public void UseSelected_ForSire_UsesSireKey()
    {
        var model = Create(sex: "M");

        model.OnPostUseSelected(new PickedPedigreeForm { Id = 8, MatchedRbse = "002600002", RowStampBase64 = "" });

        model.TempData.ContainsKey(PendingDamSireKeys.Sire).Should().BeTrue();
        model.TempData.ContainsKey(PendingDamSireKeys.Dam).Should().BeFalse();
    }

    [Fact]
    public void New_StagesNewEntryAndFlagsStartNew()
    {
        var model = Create();
        model.Eartag = "E9";

        var redirect = model.OnPostNew().Should().BeOfType<RedirectToPageResult>().Subject;

        redirect.RouteValues!["startNew"].Should().Be(true);
        model.TempData[PendingDamSireKeys.Dam]!.ToString().Should().Contain("E9");
    }

    [Fact]
    public void Exit_RedirectsBackToRelations()
    {
        Create().OnPostExit().Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Relations");
    }
}

public sealed class MinuteDocumentModelTests
{
    private readonly ICaseWorkService _service = Substitute.For<ICaseWorkService>();
    private readonly Dictionary<string, string> _sessionData = [];

    private MinuteDocumentModel Create()
    {
        var session = Substitute.For<ISession>();
        session.TryGetValue(Arg.Any<string>(), out Arg.Any<byte[]?>()).Returns(call =>
        {
            if (_sessionData.TryGetValue((string)call[0], out var v))
            {
                call[1] = System.Text.Encoding.UTF8.GetBytes(v);
                return true;
            }
            call[1] = null;
            return false;
        });
        session.When(s => s.Remove(Arg.Any<string>())).Do(c => _sessionData.Remove((string)c[0]));

        var http = new DefaultHttpContext();
        http.Features.Set<ISessionFeature>(new SessionFeature { Session = session });
        return new MinuteDocumentModel(_service) { PageContext = new PageContext { HttpContext = http } };
    }

    private sealed class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = default!;
    }

    [Fact]
    public async Task OnGet_UnsupportedType_ReturnsBadRequest()
    {
        (await Create().OnGetAsync("002600001", "Nope", null)).Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnGet_NoDetails_ReturnsNotFound()
    {
        _service.GetMinuteDetailsAsync("002600001", "AnnexA").Returns((MinuteDetailsRecord?)null);

        (await Create().OnGetAsync("002600001", "AnnexA", null)).Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData("ActiveMemo", "ActiveMemo", "activememo002600001.doc")]
    [InlineData("ActiveMemoFS", "AMFS", "activememo002600001.doc")]
    [InlineData("AnnexA", "AnnexA", "annexa002600001.doc")]
    [InlineData("AnnexB", "AnnexB", "annexb002600001.doc")]
    public async Task OnGet_Download_SetsWordHeadersAndDate(string type, string normalised, string fileName)
    {
        var date = new DateTime(2024, 3, 4);
        _service.GetMinuteDetailsAsync("00/2600001", normalised).Returns(new MinuteDetailsRecord
        {
            ActiveMemoDate = date, AnnexADate = date, AnnexBDate = date
        });
        var model = Create();

        var result = await model.OnGetAsync("00/2600001", type, null);

        result.Should().BeOfType<PageResult>();
        model.MinuteDate.Should().Be(date);
        model.MinuteType.Should().Be(normalised);
        model.IsPrintMode.Should().BeFalse();
        model.Response.ContentType.Should().Be("application/vnd.ms-word");
        model.Response.Headers.ContentDisposition.ToString().Should().Contain(fileName);
    }

    [Fact]
    public async Task OnGet_PrintMode_DoesNotSetDownloadHeaders()
    {
        _service.GetMinuteDetailsAsync("002600001", "AnnexA").Returns(new MinuteDetailsRecord());
        var model = Create();

        await model.OnGetAsync("002600001", "AnnexA", "print");

        model.IsPrintMode.Should().BeTrue();
        model.Response.Headers.ContentDisposition.ToString().Should().BeEmpty();
    }

    [Theory]
    [InlineData("Annex C")]
    [InlineData("Annex D")]
    public async Task OnGet_AnnexCOrD_ListsOutstandingFormsAndClearsSession(string type)
    {
        _service.GetMinuteDetailsAsync("002600001", Arg.Any<string>()).Returns(new MinuteDetailsRecord
        {
            AnnexCDate = new DateTime(2024, 1, 1), AnnexDDate = new DateTime(2024, 2, 2)
        });
        foreach (var key in new[] { "Homebred", "Breeder", "Purchaser", "Vendor", "SummarySheet", "AllPaperwork" })
            _sessionData[$"CaseWork.OutstandingForms.{key}"] = "True";
        var model = Create();

        var result = await model.OnGetAsync("002600001", type, null);

        result.Should().BeOfType<PageResult>();
        model.OutstandingForms.Should().HaveCount(6);
        _sessionData.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGet_AnnexCWithNoOutstandingForms_RedirectsToSessionError()
    {
        _service.GetMinuteDetailsAsync("002600001", "AnnexC").Returns(new MinuteDetailsRecord());

        var result = await Create().OnGetAsync("002600001", "AnnexC", null);

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/SessionError");
    }
}
