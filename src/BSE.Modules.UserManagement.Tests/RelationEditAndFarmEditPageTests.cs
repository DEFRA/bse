using System.Data;
using BSE.Host.Models.ViewModels;
using BSE.Host.Pages.Case;
using BSE.Host.Pages.Farm;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using EditModel = BSE.Host.Pages.Farm.EditModel;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;

namespace BSE.Modules.UserManagement.Tests;

public sealed class RelationEditModelTests
{
    private const string Rbse = "002600001";

    private readonly IAnimalRelationsRepository _repo = Substitute.For<IAnimalRelationsRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();

    public RelationEditModelTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetSexesAsync().Returns(Array.Empty<LuSex>());
    }

    private RelationEditModel Create()
    {
        var model = new RelationEditModel(_repo, _lookups, _factory, NullLogger<RelationEditModel>.Instance) { Rbse = Rbse, RelationId = 5 };
        var http = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    private void GivenRelations(params CaseRelationRecord[] relations) =>
        _repo.GetRelationsDetailsByRbseAsync(Rbse).Returns(new RelationDetailsRecord(null, null, relations));

    private static CaseRelationRecord Rel(int id) => new()
    {
        Id = id, Rbse = Rbse, RelationType = "Offspring", Sex = "F", Eartag = "123", EartagHerdmark = "AB",
        EartagCountry = "UK", BirthDay = 1, BirthMonth = 2, BirthYear = 2020, RelationFate = "A", Sire = "Bull",
        LeftDate = new DateTime(2021, 1, 1), RowStamp = [1]
    };

    [Fact]
    public async Task OnGet_Found_PopulatesFields()
    {
        GivenRelations(Rel(5));
        var model = Create();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.RelationType.Should().Be("Offspring");
        model.Eartag.Should().Be("123");
        model.BirthYear.Should().Be(2020);
        model.RowStamp.Should().Be(Convert.ToBase64String([1]));
    }

    [Fact]
    public async Task OnGet_NotFound_RedirectsToRelations()
    {
        GivenRelations(Rel(9));

        (await Create().OnGetAsync()).Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Relations");
    }

    [Fact]
    public async Task OnPost_InvalidRelation_ReturnsPage()
    {
        GivenRelations(Rel(5), Rel(6));
        var model = Create();
        model.RelationType = null;

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();
        _factory.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task OnPost_Valid_SavesAndRedirects()
    {
        GivenRelations(Rel(5));
        var conn = Substitute.For<IDbConnection>();
        var tx = Substitute.For<IDbTransaction>();
        conn.BeginTransaction().Returns(tx);
        _factory.CreateConnection().Returns(conn);
        var model = Create();
        model.RelationType = "Offspring";
        model.Sex = "M";
        model.RowStamp = Convert.ToBase64String([1]);
        model.BirthDay = 3;

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Relations");
        model.TempData["Success"].Should().Be("Relation updated.");
        await _repo.Received(1).EditRelationAsync(Arg.Any<EditCaseRelationCommand>(), conn, tx);
        tx.Received(1).Commit();
    }

    [Fact]
    public async Task OnPost_NoDetails_StillSaves()
    {
        _repo.GetRelationsDetailsByRbseAsync(Rbse).Returns((RelationDetailsRecord)null!);
        var conn = Substitute.For<IDbConnection>();
        conn.BeginTransaction().Returns(Substitute.For<IDbTransaction>());
        _factory.CreateConnection().Returns(conn);
        var model = Create();
        model.RelationType = "Offspring";
        model.Sex = "M";

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnPost_RepositoryThrows_SetsErrorMessage()
    {
        GivenRelations(Rel(5));
        var conn = Substitute.For<IDbConnection>();
        var tx = Substitute.For<IDbTransaction>();
        conn.BeginTransaction().Returns(tx);
        _factory.CreateConnection().Returns(conn);
        _repo.EditRelationAsync(Arg.Any<EditCaseRelationCommand>(), conn, tx).Returns<Task>(_ => throw new InvalidOperationException());
        var model = Create();
        model.RelationType = "Offspring";
        model.Sex = "M";

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be("Unable to save the relation.");
        tx.DidNotReceive().Commit();
    }
}

public sealed class FarmEditPageModelTests
{
    private const string Cphh = "12345678901";

    private readonly IFarmService _farm = Substitute.For<IFarmService>();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IGeoLookupService _geo = Substitute.For<IGeoLookupService>();

    public FarmEditPageModelTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetHerdTypesAsync().Returns(Array.Empty<LuHerdType>());
        _user.GetUserIdAsync().Returns(3);
    }

    private EditModel Create()
    {
        var model = new EditModel(_farm, _user, _lookups, _geo);
        var http = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = http };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    [Fact]
    public async Task OnGet_NotFound_ReturnsNotFound()
    {
        _farm.GetByCphhAsync(Cphh).Returns((FarmRecord?)null);

        (await Create().OnGetAsync("12/345/6789/01")).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGet_Found_LoadsFarmAndRowStamp()
    {
        _farm.GetByCphhAsync(Cphh).Returns(new FarmRecord { CPHH = Cphh, RowStamp = [7] });
        _farm.GetConfirmedCaseCountAsync(Cphh).Returns(2);
        var model = Create();

        (await model.OnGetAsync(Cphh, "002600001")).Should().BeOfType<PageResult>();

        model.Farm.Should().NotBeNull();
        model.ConfirmedCaseCount.Should().Be(2);
        model.Rbse.Should().Be("002600001");
        model.TempData["FarmRowStamp"].Should().Be(Convert.ToBase64String([7]));
    }

    [Fact]
    public async Task OnPost_MapReferenceOutsideParish_AddsError()
    {
        _geo.IsMapReferenceWithinParishAsync(Cphh, "TQ123456").Returns(false);
        var model = Create();
        model.Farm = new FarmEditViewModel { CPHH = "12/345/6789/01", MapRef1 = "TQ", MapRef2 = "123", MapRef3 = "456" };

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState.ContainsKey("Farm.MapRef1").Should().BeTrue();
        await _farm.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task OnPost_NullFarm_ReturnsPage()
    {
        (await Create().OnPostAsync()).Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPost_Valid_UpdatesAndRedirects()
    {
        var model = Create();
        model.Rbse = "002600001";
        model.Farm = new FarmEditViewModel { CPHH = "12/345/6789/01" };
        model.TempData["FarmRowStamp"] = Convert.ToBase64String([7]);

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Farm");
        model.TempData["Success"].Should().Be("Farm updated successfully.");
        await _farm.Received(1).UpdateAsync(Arg.Is<UpdateFarmCommand>(c => c.CPHH == Cphh), 3);
    }

    [Fact]
    public async Task Ajax_Lookups_DelegateToHelpers()
    {
        _lookups.GetAuthoritiesByCountyAsync(1).Returns(new[] { new LuAuthority { Id = 4, Name = "Auth" } });
        _lookups.GetADNSRegionsByAuthorityAsync(2).Returns(new[] { new LuAdnsRegion { Id = 6, Name = "Reg" } });
        var model = Create();

        (await model.OnGetAuthoritiesAsync(null)).Should().BeOfType<JsonResult>();
        (await model.OnGetAuthoritiesAsync(1)).Should().BeOfType<JsonResult>();
        (await model.OnGetAdnsRegionsAsync(0)).Should().BeOfType<JsonResult>();
        (await model.OnGetAdnsRegionsAsync(2)).Should().BeOfType<JsonResult>();
    }

    [Fact]
    public async Task EstimateMapReference_Branches()
    {
        _geo.EstimateMapReferenceAsync("12345").Returns(new MapReferenceEstimate(null, null, null, "no parish"));
        _geo.EstimateMapReferenceAsync("54321").Returns(new MapReferenceEstimate("TQ", "123", "456", null));
        var model = Create();

        ((await model.OnGetEstimateMapReferenceAsync("12")) as JsonResult)!.Value.Should().BeEquivalentTo(new { error = "CPHH must be at least 5 characters to estimate a map reference." });
        ((await model.OnGetEstimateMapReferenceAsync("12345")) as JsonResult)!.Value.Should().BeEquivalentTo(new { error = "no parish" });
        ((await model.OnGetEstimateMapReferenceAsync("54321")) as JsonResult)!.Value.Should().BeEquivalentTo(new { mapRef1 = "TQ", mapRef2 = "123", mapRef3 = "456" });
    }

    [Fact]
    public async Task ValidateMapReference_Branches()
    {
        _geo.IsMapReferenceWithinParishAsync("12345", "TQ123456").Returns(true);
        _geo.IsMapReferenceWithinParishAsync("54321", "TQ123456").Returns(false);
        var model = Create();

        ((await model.OnGetValidateMapReferenceAsync("12", "TQ123456")) as JsonResult)!.Value.Should().BeEquivalentTo(new { valid = true });
        ((await model.OnGetValidateMapReferenceAsync("12345", "TQ123456")) as JsonResult)!.Value.Should().BeEquivalentTo(new { valid = true, message = (string?)null });
        ((await model.OnGetValidateMapReferenceAsync("54321", "TQ123456")) as JsonResult)!.Value.Should().BeEquivalentTo(
            new { valid = false, message = (string?)"Map reference does not lie within the parish boundaries for this CPHH." });
    }
}
