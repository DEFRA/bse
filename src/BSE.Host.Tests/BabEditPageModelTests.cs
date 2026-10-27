using System.Data;
using BSE.Host.Pages.Case;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Tests;

public sealed class BabEditPageModelTests
{
    private const string Rbse = "002600001";

    private readonly IBabRepository _bab = Substitute.For<IBabRepository>();
    private readonly ICaseRepository _cases = Substitute.For<ICaseRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IBatchRepository _batches = Substitute.For<IBatchRepository>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();

    public BabEditPageModelTests()
    {
        _bab.GetByRbseAsync(Rbse).Returns((CaseBabRecord?)null);
        _cases.GetCaseByRbseAsync(Rbse).Returns(new CaseRecord { Rbse = Rbse, Origin = "P" });
        _batches.GetBatchNumbersByRbseAsync(Rbse).Returns((IReadOnlyList<BatchNumberEntry>)[new BatchNumberEntry(1, "B1", Rbse, null)]);
        _lookups.GetAnimalOriginsAsync().Returns([new LuAnimalOrigin { Id = 1, Code = "P", Description = "Purchased" }]);
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns([new LookupItem(1, "Risk")]);
        _connection.BeginTransaction().Returns(_transaction);
        _factory.CreateConnection().Returns(_connection);
    }

    private BabEditModel CreateModel()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SpolSiteUrl"] = "http://spol" }).Build();
        var model = new BabEditModel(_bab, _cases, _lookups, _batches, _factory, config) { Rbse = Rbse };
        PageTestContext.Attach(model);
        return model;
    }

    [Fact]
    public async Task OnGet_NoBabRecord_LoadsLookupsAndEmptyForm()
    {
        var model = CreateModel();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.SpolSiteUrl.Should().Be("http://spol");
        model.RowStampBase64.Should().BeNull();
        model.Origin.Should().Be("P");
        model.Bab.Notes.Should().BeNull();
        model.BatchNumbers.Should().ContainSingle();
        model.AnimalOrigins.Should().ContainSingle().Which.Code.Should().Be("P");
        model.FeedRisks.Should().ContainSingle();
        model.HorizontalRisks.Should().ContainSingle();
        model.MaternalRisks.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGet_ExistingBab_PopulatesFormAndRowStamp()
    {
        _bab.GetByRbseAsync(Rbse).Returns(new CaseBabRecord
        {
            Rbse = Rbse, NatalCphh = "01001000101", Notes = "n", TracedName = "t", TracedAddress1 = "a1",
            TracedAddress2 = "a2", TracedAddress3 = "a3", TracedPostcode = "pc", FeedRisk = "f",
            HorizontalRisk = "h", MaternalRisk = "m", RowStamp = [1, 2]
        });
        _cases.GetCaseByRbseAsync(Rbse).Returns((CaseRecord?)null);
        var model = CreateModel();

        await model.OnGetAsync();

        model.RowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 1, 2 }));
        model.Origin.Should().BeNull();
        model.Bab.NatalCphh.Should().Be("01001000101");
        model.Bab.TracedPostcode.Should().Be("pc");
        model.Bab.MaternalRisk.Should().Be("m");
    }

    [Fact]
    public async Task OnGet_BabWithoutRowStamp_LeavesRowStampNull()
    {
        _bab.GetByRbseAsync(Rbse).Returns(new CaseBabRecord { Rbse = Rbse });
        var model = CreateModel();

        await model.OnGetAsync();

        model.RowStampBase64.Should().BeNull();
    }

    [Fact]
    public async Task Save_ShortNatalCphh_ReturnsPageWithError()
    {
        var model = CreateModel();
        model.Bab.NatalCphh = "01/001/0001";

        (await model.OnPostSaveBabAsync(null)).Should().BeOfType<PageResult>();

        model.ModelState["Bab.NatalCphh"]!.Errors.Should().ContainSingle();
        model.Bab.NatalCphh.Should().Be("010010001");
        model.SpolSiteUrl.Should().Be("http://spol");
        _factory.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task Save_NoRowStamp_AddsBabAndRedirects()
    {
        var model = CreateModel();
        model.Origin = "P";
        model.Bab.NatalCphh = "01/001/0001/01";
        model.Bab.Notes = "note";
        model.Bab.TracedName = "name";

        var result = await model.OnPostSaveBabAsync(null);

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Bab");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
        await _bab.Received(1).AddAsync(
            Arg.Is<AddCaseBabCommand>(c => c.NatalCphh == "01001000101" && c.Notes == "note" && c.TracedName == "name"),
            "P", _connection, _transaction);
        _transaction.Received(1).Commit();
        model.TempData["Success"].Should().Be("BAB details saved.");
    }

    [Fact]
    public async Task Save_WithRowStamp_EditsBab()
    {
        var model = CreateModel();
        model.Origin = "P";
        model.Bab.Notes = "edited";

        var result = await model.OnPostSaveBabAsync(Convert.ToBase64String(new byte[] { 9, 8 }));

        result.Should().BeOfType<RedirectToPageResult>();
        await _bab.Received(1).EditAsync(
            Arg.Is<EditCaseBabCommand>(c => c.Notes == "edited" && c.RowStamp.SequenceEqual(new byte[] { 9, 8 })),
            "P", _connection, _transaction);
        await _bab.DidNotReceiveWithAnyArgs().AddAsync(default!, default, default!, default!);
    }

    [Fact]
    public async Task Save_NonPurchasedOrigin_ClearsTracedFields()
    {
        var model = CreateModel();
        model.Origin = "H";
        model.Bab.NatalCphh = "01001000101";
        model.Bab.TracedName = "x";
        model.Bab.TracedAddress1 = "x";
        model.Bab.TracedAddress2 = "x";
        model.Bab.TracedAddress3 = "x";
        model.Bab.TracedPostcode = "x";
        model.Bab.Notes = "keep";

        await model.OnPostSaveBabAsync(null);

        await _bab.Received(1).AddAsync(
            Arg.Is<AddCaseBabCommand>(c => c.NatalCphh == null && c.TracedName == null && c.TracedAddress1 == null
                && c.TracedAddress2 == null && c.TracedAddress3 == null && c.TracedPostcode == null && c.Notes == "keep"),
            "H", _connection, _transaction);
    }

    [Fact]
    public async Task Save_BlankNatalCphh_BecomesNull()
    {
        var model = CreateModel();
        model.Origin = "P";
        model.Bab.NatalCphh = "  ";

        await model.OnPostSaveBabAsync(null);

        await _bab.Received(1).AddAsync(Arg.Is<AddCaseBabCommand>(c => c.NatalCphh == null), "P", _connection, _transaction);
    }
}
