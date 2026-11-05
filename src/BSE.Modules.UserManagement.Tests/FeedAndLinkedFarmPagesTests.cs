using System.Data;
using BSE.Host.Helpers;
using BSE.Host.Pages.Case;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class FeedAddEditPagesTests
{
    private const string Rbse = "002600001";

    private readonly IFeedRepository _feeds = Substitute.For<IFeedRepository>();
    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _conn = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _tx = Substitute.For<IDbTransaction>();

    public FeedAddEditPagesTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(new[] { new LookupItem { Code = "X", Description = "X" } });
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, BirthDate = new DateTime(2015, 1, 1) });
        _conn.BeginTransaction().Returns(_tx);
        _factory.CreateConnection().Returns(_conn);
    }

    private static void Wire(PageModel m)
    {
        var http = new DefaultHttpContext();
        m.PageContext = new PageContext { HttpContext = http };
        m.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
    }

    private FeedAddModel Add()
    {
        var m = new FeedAddModel(_feeds, _cases, _lookups, _factory,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "https://s" }).Build())
        { Rbse = Rbse };
        Wire(m);
        return m;
    }

    private FeedEditModel Edit()
    {
        var m = new FeedEditModel(_feeds, _cases, _lookups, _factory) { Rbse = Rbse, FeedId = 3 };
        Wire(m);
        return m;
    }

    [Fact]
    public async Task Add_OnGet_LoadsLookupsAndSpolUrl()
    {
        var m = Add();

        (await m.OnGetAsync()).Should().BeOfType<PageResult>();
        m.SpolSiteUrl.Should().Be("https://s");
        m.RationTypes.Should().NotBeEmpty();
        m.Suppliers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Add_Post_Invalid_ReturnsPageWithFieldErrors()
    {
        var m = Add();
        m.NewFeed.YearFrom = 2000;
        m.NewFeed.YearTo = 1999;

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();

        m.FieldErrors.Keys.Should().Contain(["YearFrom", "YearTo", "RationType", "SupplierId"]);
        m.FieldErrors["YearFrom"].Should().Be(FeedValidation.YearFromBeforeBirth);
        await _feeds.DidNotReceiveWithAnyArgs().AddAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Add_Post_Valid_SavesAndRedirects()
    {
        var m = Add();
        m.NewFeed.YearFrom = 2016;
        m.NewFeed.YearTo = 2017;
        m.NewFeed.RationType = "X";
        m.NewFeed.SupplierId = 4;
        m.NewFeed.RationName = " ";

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Feeds");
        m.TempData["Success"].Should().Be("Feed record added.");
        await _feeds.Received(1).AddAsync(Arg.Is<AddFeedCommand>(c => c.RationName == null && c.Rbse == Rbse), _conn, _tx);
        _tx.Received(1).Commit();
    }

    [Fact]
    public async Task Edit_OnGet_NotFound_RedirectsToFeeds()
    {
        _feeds.GetByRbseAsync(Rbse).Returns(Array.Empty<CaseFeedRecord>());

        (await Edit().OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Feeds");
    }

    [Fact]
    public async Task Edit_OnGet_Found_PopulatesViewModel()
    {
        _feeds.GetByRbseAsync(Rbse).Returns(new[]
        {
            new CaseFeedRecord { Id = 3, Rbse = Rbse, YearFrom = 2016, YearTo = 2017, RationType = "X", SupplierId = 4, RationName = "R", IsPrePurchase = true, RowStamp = [9] }
        });
        var m = Edit();

        (await m.OnGetAsync()).Should().BeOfType<PageResult>();

        m.Feed.YearFrom.Should().Be(2016);
        m.Feed.IsPrePurchase.Should().BeTrue();
        m.Feed.RowStamp.Should().Be(Convert.ToBase64String([9]));
    }

    [Fact]
    public async Task Edit_Post_Invalid_ReturnsPage()
    {
        var m = Edit();

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        m.FieldErrors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("AQ==", "Name")]
    public async Task Edit_Post_Valid_SavesAndRedirects(string rowStamp, string name)
    {
        var m = Edit();
        m.Feed.YearFrom = 2016;
        m.Feed.YearTo = 2017;
        m.Feed.RationType = "X";
        m.Feed.SupplierId = 4;
        m.Feed.RowStamp = rowStamp;
        m.Feed.RationName = name;

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Feeds");
        m.TempData["Success"].Should().Be("Feed record updated.");
        await _feeds.Received(1).EditAsync(Arg.Is<EditFeedCommand>(c => c.Id == 3), _conn, _tx);
        _tx.Received(1).Commit();
    }
}

public sealed class FarmLinkedFarmPagesTests
{
    private const string Rbse = "002600001";
    private const string Own = "01001000101";
    private const string Other = "01001000202";

    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly IFarmService _farms = Substitute.For<IFarmService>();
    private readonly IFarmRelationRepository _relations = Substitute.For<IFarmRelationRepository>();
    private readonly ICaseEditSessionStateService _session = Substitute.For<ICaseEditSessionStateService>();
    private CaseEditSessionState? _stored;

    public FarmLinkedFarmPagesTests()
    {
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = Own });
        _farms.GetRelatedFarmsAsync(Own).Returns(new[] { new FarmRelationRecord { ID = 5, CPHH = Own, RelatedCPHH = "01001000303", RowStamp = [2] } });
        _session.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => _stored);
        _session.SetAsync(Arg.Do<CaseEditSessionState>(d => _stored = d), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    private static void Wire(PageModel m)
    {
        var http = new DefaultHttpContext();
        m.PageContext = new PageContext { HttpContext = http };
        m.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
    }

    private FarmLinkedFarmAddModel Add()
    {
        var m = new FarmLinkedFarmAddModel(_cases, _farms, _session) { Rbse = Rbse };
        Wire(m);
        return m;
    }

    private FarmLinkedFarmEditModel Edit(int id = 5)
    {
        var m = new FarmLinkedFarmEditModel(_cases, _farms, _relations) { Rbse = Rbse, Id = id, RowStampBase64 = Convert.ToBase64String([2]) };
        Wire(m);
        return m;
    }

    // ── Add ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_OnGet_CaseMissing_RedirectsHome()
    {
        _cases.GetCaseAsync(Rbse).Returns((CaseRecord?)null);

        (await Add().OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
        (await Add().OnPostAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
    }

    [Fact]
    public async Task Add_OnGet_ReturnsPage()
    {
        (await Add().OnGetAsync()).Should().BeOfType<PageResult>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData(Own)]
    public async Task Add_Post_Invalid_ReturnsPage(string? cphh)
    {
        var m = Add();
        m.RelatedCphh = cphh;

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        m.ModelState[nameof(m.RelatedCphh)]!.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Add_Post_AlreadyInPersistedList_Rejected()
    {
        var m = Add();
        m.RelatedCphh = "01001000303";

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task Add_Post_AlreadyInDraft_Rejected()
    {
        _stored = new CaseEditSessionState { Rbse = Rbse, Cphh = Own };
        _stored.LinkedFarms.Add(new CaseFarmDraftLinkedFarmItem { RelatedCphh = Other, RowStampBase64 = "", Status = "" });
        var m = Add();
        m.RelatedCphh = Other;

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task Add_Post_Valid_StagesLinkedFarm()
    {
        var m = Add();
        m.RelatedCphh = "01/001/0002/02";

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        _stored!.LinkedFarms.Should().ContainSingle().Which.RelatedCphh.Should().Be(Other);
        m.TempData["Success"].Should().Be($"Linked farm {Other} added to pending changes.");
    }

    // ── Edit ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Edit_NoCase_RedirectsOnGetAndPost()
    {
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Cphh = null! });

        (await Edit().OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
        (await Edit().OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Edit_OnGet_UnknownLink_Redirects()
    {
        (await Edit(99).OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Edit_OnGet_Found_PopulatesFields()
    {
        var m = Edit();

        (await m.OnGetAsync()).Should().BeOfType<PageResult>();

        m.RelatedCphh.Should().Be("01001000303");
        m.RowStampBase64.Should().Be(Convert.ToBase64String([2]));
    }

    [Fact]
    public async Task Edit_OnGet_NullRowStamp_GivesEmptyString()
    {
        _farms.GetRelatedFarmsAsync(Own).Returns(new[] { new FarmRelationRecord { ID = 5, RelatedCPHH = "01001000303", RowStamp = null } });
        var m = Edit();

        await m.OnGetAsync();

        m.RowStampBase64.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData(Own)]
    public async Task Edit_Post_Invalid_ReturnsPage(string cphh)
    {
        var m = Edit();
        m.RelatedCphh = cphh;

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
        await _relations.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!, default!);
    }

    [Fact]
    public async Task Edit_Post_DuplicateOfAnotherLink_Rejected()
    {
        _farms.GetRelatedFarmsAsync(Own).Returns(new[]
        {
            new FarmRelationRecord { ID = 5, RelatedCPHH = "01001000303" },
            new FarmRelationRecord { ID = 6, RelatedCPHH = Other }
        });
        var m = Edit();
        m.RelatedCphh = Other;

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task Edit_Post_Valid_UpdatesAndRedirects()
    {
        var m = Edit();
        m.RelatedCphh = "01/001/0002/02";

        var result = await m.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Farm");
        await _relations.Received(1).UpdateAsync(5, Other, Arg.Any<byte[]>());
        m.TempData["Success"].Should().Be("Linked farm updated.");
    }
}
