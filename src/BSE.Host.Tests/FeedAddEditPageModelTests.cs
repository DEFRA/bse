using System.Data;
using BSE.Host.Helpers;
using BSE.Host.Pages.Case;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Tests;

public sealed class FeedAddEditPageModelTests
{
    private const string Rbse = "002600001";

    private readonly IFeedRepository _feeds = Substitute.For<IFeedRepository>();
    private readonly ICaseService _cases = Substitute.For<ICaseService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();

    public FeedAddEditPageModelTests()
    {
        _lookups.GetLookupAsync(LookupTableId.RationType).Returns(Task.FromResult<IEnumerable<LookupItem>>([new LookupItem(1, "HAY", "Hay")]));
        _lookups.GetLookupAsync(LookupTableId.Supplier).Returns(Task.FromResult<IEnumerable<LookupItem>>([new LookupItem(9, "Sup")]));
        _connection.BeginTransaction().Returns(_transaction);
        _factory.CreateConnection().Returns(_connection);
    }

    private FeedAddModel CreateAdd(string? spol = "http://spol")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(spol is null ? [] : new Dictionary<string, string?> { ["SpolSiteUrl"] = spol }).Build();
        var model = new FeedAddModel(_feeds, _cases, _lookups, _factory, config) { Rbse = "00/26/00001" };
        PageTestContext.Attach(model);
        return model;
    }

    private FeedEditModel CreateEdit()
    {
        var model = new FeedEditModel(_feeds, _cases, _lookups, _factory) { Rbse = Rbse, FeedId = 4 };
        PageTestContext.Attach(model);
        return model;
    }

    [Fact]
    public async Task Add_OnGet_LoadsLookupsAndConfig()
    {
        var model = CreateAdd();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.SpolSiteUrl.Should().Be("http://spol");
        model.RationTypes.Should().HaveCount(1);
        model.Suppliers.Should().HaveCount(1);
    }

    [Fact]
    public async Task Add_OnGet_MissingConfig_IsEmpty()
    {
        var model = CreateAdd(null);

        await model.OnGetAsync();

        model.SpolSiteUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_OnPost_Invalid_ReturnsPageWithFieldErrors()
    {
        var model = CreateAdd();

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.FieldErrors.Keys.Should().BeEquivalentTo("YearFrom", "YearTo", "RationType", "SupplierId");
        await _feeds.DidNotReceiveWithAnyArgs().AddAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Add_OnPost_YearBeforeBirth_IsRejected()
    {
        _cases.GetCaseAsync(Rbse).Returns(new CaseRecord { BirthDate = new DateTime(2020, 1, 1) });
        var model = CreateAdd();
        model.NewFeed.YearFrom = 2019;
        model.NewFeed.YearTo = 2021;
        model.NewFeed.RationType = "HAY";
        model.NewFeed.SupplierId = 9;

        await model.OnPostAsync();

        model.FieldErrors["YearFrom"].Should().Be(FeedValidation.YearFromBeforeBirth);
    }

    [Fact]
    public async Task Add_OnPost_Valid_AddsFeedAndRedirects()
    {
        var model = CreateAdd();
        model.NewFeed.YearFrom = 2020;
        model.NewFeed.YearTo = 2021;
        model.NewFeed.RationType = "HAY";
        model.NewFeed.SupplierId = 9;
        model.NewFeed.RationName = " ";
        model.NewFeed.IsPrePurchase = true;

        var result = await model.OnPostAsync();

        await _feeds.Received(1).AddAsync(
            Arg.Is<AddFeedCommand>(c => c.Rbse == Rbse && c.YearFrom == 2020 && c.YearTo == 2021 && c.RationType == "HAY"
                && c.SupplierId == 9 && c.RationName == null && c.IsPrePurchase),
            _connection, _transaction);
        _transaction.Received(1).Commit();
        model.TempData["Success"].Should().Be("Feed record added.");
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Feeds");
        redirect.RouteValues!["rbse"].Should().Be("00/26/00001");
    }

    [Fact]
    public async Task Edit_OnGet_UnknownFeed_RedirectsToFeeds()
    {
        _feeds.GetByRbseAsync(Rbse).Returns([new CaseFeedRecord { Id = 99 }]);

        var result = await CreateEdit().OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Feeds");
    }

    [Fact]
    public async Task Edit_OnGet_PopulatesFeed()
    {
        _feeds.GetByRbseAsync(Rbse).Returns([
            new CaseFeedRecord { Id = 4, Rbse = Rbse, YearFrom = 2020, YearTo = 2021, RationType = "HAY", RationName = "R", SupplierId = 9, IsPrePurchase = true, RowStamp = [1, 2] }]);
        var model = CreateEdit();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Feed.Id.Should().Be(4);
        model.Feed.YearFrom.Should().Be(2020);
        model.Feed.RationName.Should().Be("R");
        model.Feed.IsPrePurchase.Should().BeTrue();
        model.Feed.RowStamp.Should().Be(Convert.ToBase64String(new byte[] { 1, 2 }));
    }

    [Fact]
    public async Task Edit_OnGet_NullRowStamp_GivesEmptyString()
    {
        _feeds.GetByRbseAsync(Rbse).Returns([new CaseFeedRecord { Id = 4, RowStamp = null }]);
        var model = CreateEdit();

        await model.OnGetAsync();

        model.Feed.RowStamp.Should().BeEmpty();
    }

    [Fact]
    public async Task Edit_OnPost_Invalid_ReturnsPage()
    {
        var model = CreateEdit();
        model.Feed.YearFrom = 2022;
        model.Feed.YearTo = 2020;
        model.Feed.RationType = "HAY";
        model.Feed.SupplierId = 9;

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.FieldErrors["YearTo"].Should().Be(FeedValidation.YearToBeforeYearFrom);
        await _feeds.DidNotReceiveWithAnyArgs().EditAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Edit_OnPost_Valid_EditsAndRedirects()
    {
        var model = CreateEdit();
        model.Feed.YearFrom = 2020;
        model.Feed.YearTo = 2021;
        model.Feed.RationType = "HAY";
        model.Feed.SupplierId = 9;
        model.Feed.RationName = "Name";
        model.Feed.RowStamp = Convert.ToBase64String(new byte[] { 7, 8 });

        var result = await model.OnPostAsync();

        await _feeds.Received(1).EditAsync(
            Arg.Is<EditFeedCommand>(c => c.Id == 4 && c.RationName == "Name" && c.RowStamp.SequenceEqual(new byte[] { 7, 8 })),
            _connection, _transaction);
        _transaction.Received(1).Commit();
        model.TempData["Success"].Should().Be("Feed record updated.");
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Feeds");
    }

    [Fact]
    public async Task Edit_OnPost_BlankRowStamp_UsesEmptyArray()
    {
        var model = CreateEdit();
        model.Feed.YearFrom = 2020;
        model.Feed.YearTo = 2020;
        model.Feed.RationType = "HAY";
        model.Feed.SupplierId = 9;

        await model.OnPostAsync();

        await _feeds.Received(1).EditAsync(
            Arg.Is<EditFeedCommand>(c => c.RowStamp.Length == 0 && c.RationName == null), _connection, _transaction);
    }
}
