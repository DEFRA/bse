using BSE.Host.Pages.Case;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;

namespace BSE.Host.Tests;

public sealed class RelationEditPageModelTests
{
    private const string Rbse = "002600001";

    private readonly IAnimalRelationsRepository _relations = Substitute.For<IAnimalRelationsRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();
    private RelationDetailsRecord _details;

    public RelationEditPageModelTests()
    {
        _lookups.GetLookupAsync(LookupTableId.RelationType).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "S", "Sibling")]);
        _lookups.GetLookupAsync(LookupTableId.RelationFate).Returns((IEnumerable<LookupItem>)[new LookupItem(1, "D", "Died")]);
        _lookups.GetSexesAsync().Returns((IEnumerable<LuSex>)[new LuSex { Id = 1, Code = "M", Description = "Male" }]);
        _details = new RelationDetailsRecord(null, null,
        [
            new CaseRelationRecord
            {
                Id = 4, RelationType = "S", RelationRbse = "002700004", Sex = "M", BirthDay = 5, BirthMonth = 6, BirthYear = 2000,
                RelationFate = "D", LeftDate = new DateTime(2020, 1, 1), EartagCountry = "UK", EartagHerdmark = "1", Eartag = "2", Sire = "Sr", RowStamp = [1, 2]
            },
            new CaseRelationRecord { Id = 5, RelationType = "S", RelationRbse = "002700005", RowStamp = [3] }
        ]);
        _relations.GetRelationsDetailsByRbseAsync(Arg.Any<string>()).Returns(_ => _details);
        _connection.BeginTransaction().Returns(_transaction);
        _factory.CreateConnection().Returns(_connection);
    }

    private RelationEditModel CreateModel(int relationId = 4)
    {
        var model = new RelationEditModel(_relations, _lookups, _factory, NullLogger<RelationEditModel>.Instance)
        {
            Rbse = Rbse,
            RelationId = relationId,
            RowStamp = Convert.ToBase64String(new byte[] { 1, 2 })
        };
        var http = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = http };
        model.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
        return model;
    }

    [Fact]
    public async Task OnGet_ExistingRelation_PopulatesFields()
    {
        var model = CreateModel();
        model.RowStamp = null;

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.RelationRbse.Should().Be("002700004");
        model.BirthYear.Should().Be(2000);
        model.LeftDate.Should().Be(new DateTime(2020, 1, 1));
        model.Sire.Should().Be("Sr");
        model.RowStamp.Should().Be(Convert.ToBase64String(new byte[] { 1, 2 }));
        model.RelationTypes.Should().ContainSingle();
        model.RelationFates.Single().Description.Should().Be("Died");
        model.Sexes.Should().ContainSingle();
    }

    [Fact]
    public async Task OnGet_UnknownRelation_RedirectsToRelations()
    {
        var result = await CreateModel(99).OnGetAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Relations");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
    }

    [Fact]
    public async Task OnGet_NullRowStamp_ProducesEmptyString()
    {
        _details = new RelationDetailsRecord(null, null, [new CaseRelationRecord { Id = 4, RelationType = "S" }]);
        var model = CreateModel();

        await model.OnGetAsync();

        model.RowStamp.Should().BeEmpty();
    }

    [Fact]
    public async Task OnPost_MissingTypeAndSex_ReturnsPageWithFieldErrors()
    {
        var model = CreateModel();

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.FieldErrors.Should().ContainKeys("RelationType", "Sex");
        model.ModelState.Should().ContainKeys("RelationType", "Sex");
        await _relations.DidNotReceiveWithAnyArgs().EditRelationAsync(default!, default!, default!);
    }

    [Fact]
    public async Task OnPost_DuplicateOfOtherRelation_IsRejectedButOwnRbseIsAllowed()
    {
        var dup = CreateModel();
        dup.RelationType = "S";
        dup.RelationRbse = "002700005";
        await dup.OnPostAsync();
        dup.FieldErrors["RelationRbse"].Should().Contain("already a relation");

        _relations.GetRelationDetailsOfRelatedCaseAsync("002700004").Returns(new RelatedCaseDetailsRecord { RelationRbse = "002700004", Sex = "M" });
        var own = CreateModel();
        own.RelationType = "S";
        own.RelationRbse = "002700004";
        (await own.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnPost_ParentDetailsRbse_IsRejected()
    {
        _details = new RelationDetailsRecord(new DamSireDetailRecord { Id = 1, Rbse = "002700010" }, new DamSireDetailRecord { Id = 2, Rbse = "002700011" }, []);
        var dam = CreateModel();
        dam.RelationType = "S";
        dam.RelationRbse = "002700010";
        await dam.OnPostAsync();
        dam.FieldErrors["RelationRbse"].Should().Contain("dam");

        var sire = CreateModel();
        sire.RelationType = "S";
        sire.RelationRbse = "002700011";
        await sire.OnPostAsync();
        sire.FieldErrors["RelationRbse"].Should().Contain("sire");
    }

    [Fact]
    public async Task OnPost_RelatedCaseNotFound_ReturnsRbseNotFoundError()
    {
        _relations.GetRelationDetailsOfRelatedCaseAsync("002700020").Returns((RelatedCaseDetailsRecord?)null);
        var model = CreateModel();
        model.RelationType = "S";
        model.RelationRbse = "002700020";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.FieldErrors["RelationRbse"].Should().Be("RBSE number not found");
        model.ModelState["RelationRbse"]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task OnPost_RelatedCaseFound_SavesLiveValuesFromRelatedCase()
    {
        _relations.GetRelationDetailsOfRelatedCaseAsync("002700020").Returns(new RelatedCaseDetailsRecord
        {
            RelationRbse = "002700020", Sex = "F", Fate = "D", EartagCountry = "UK", EartagHerdmark = "55", Eartag = "66",
            BirthDay = 3, BirthMonth = 4, BirthYear = 1999, LeftDate = "01/02/2021", Name = "NameX"
        });
        var model = CreateModel();
        model.RelationType = "S";
        model.RelationRbse = "002700020";
        model.Sex = "M";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Relations");
        model.TempData["Success"].Should().Be("Relation updated.");
        await _relations.Received(1).EditRelationAsync(
            Arg.Is<EditCaseRelationCommand>(c => c.Id == 4 && c.RelationRbse == "002700020" && c.Sex == "F" && c.RelationFate == "D"
                && c.Eartag == "66" && c.Sire == "NameX" && c.LeftDate == new DateTime(2021, 1, 2)
                && c.RowStamp.SequenceEqual(new byte[] { 1, 2 })),
            _connection, _transaction);
        _transaction.Received(1).Commit();
    }

    [Fact]
    public async Task OnPost_WithoutRelationRbse_SavesPostedValuesWithNullsForBlanks()
    {
        var model = CreateModel();
        model.RelationType = "S";
        model.Sex = "M";
        model.BirthMonth = 5;
        model.BirthYear = 2001;
        model.RelationFate = " ";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _relations.Received(1).EditRelationAsync(
            Arg.Is<EditCaseRelationCommand>(c => c.RelationRbse == null && c.RelationFate == null && c.EartagCountry == null
                && c.BirthDay == null && c.BirthMonth == 5 && c.BirthYear == 2001),
            _connection, _transaction);
        await _relations.DidNotReceiveWithAnyArgs().GetRelationDetailsOfRelatedCaseAsync(default!);
    }

    [Fact]
    public async Task OnPost_NullDetails_StillValidatesAndSaves()
    {
        _relations.GetRelationsDetailsByRbseAsync(Arg.Any<string>()).Returns((RelationDetailsRecord)null!);
        var model = CreateModel();
        model.RelationType = "S";
        model.Sex = "M";

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnPost_RepositoryThrows_ReportsErrorAndDoesNotCommit()
    {
        _relations.EditRelationAsync(Arg.Any<EditCaseRelationCommand>(), _connection, _transaction)
            .Returns<Task<int>>(_ => throw new InvalidOperationException("sp failed"));
        var model = CreateModel();
        model.RelationType = "S";
        model.Sex = "M";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be("Unable to save the relation.");
        _transaction.DidNotReceive().Commit();
        model.TempData.ContainsKey("Success").Should().BeFalse();
    }

    [Fact]
    public async Task OnPost_InvalidBirthYearAndFutureLeftDate_ReturnsFieldErrors()
    {
        var model = CreateModel();
        model.RelationType = "S";
        model.Sex = "M";
        model.BirthYear = 1900;
        model.LeftDate = DateTime.Today.AddDays(5);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.FieldErrors.Should().ContainKeys("BirthYear", "LeftDate");
    }
}
