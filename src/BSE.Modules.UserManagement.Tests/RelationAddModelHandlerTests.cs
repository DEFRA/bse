using System.Data;
using BSE.Host.Helpers;
using BSE.Host.Pages.Case;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
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
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>
/// Covers <see cref="RelationAddModel"/> — previously had zero tests, leaving the
/// validation-failure, related-case-lookup, exception, and DB-save branches of
/// OnPostAsync entirely uncovered.
/// </summary>
public sealed class RelationAddModelHandlerTests
{
    private readonly IAnimalRelationsRepository _relationsRepository = Substitute.For<IAnimalRelationsRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IDbConnectionFactory _connectionFactory = Substitute.For<IDbConnectionFactory>();

    private const string Rbse = "002600001";

    public RelationAddModelHandlerTests()
    {
        _lookups.GetLookupAsync(Arg.Any<LookupTableId>()).Returns(Array.Empty<LookupItem>());
        _lookups.GetSexesAsync().Returns(Array.Empty<LuSex>());
        _relationsRepository.GetRelationsDetailsByRbseAsync(Arg.Any<string>())
            .Returns(new RelationDetailsRecord(null, null, Array.Empty<CaseRelationRecord>()));
    }

    private RelationAddModel CreateModel()
    {
        var model = new RelationAddModel(_relationsRepository, _lookups, _connectionFactory, NullLogger<RelationAddModel>.Instance)
        {
            Rbse = Rbse
        };

        var httpContext = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = httpContext };
        model.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), model.ModelState);
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
        return model;
    }

    // Uncovered path: OnGetAsync — loads lookup options and renders the page.
    [Fact]
    public async Task OnGetAsync_LoadsLookupsAndReturnsPage()
    {
        var model = CreateModel();
        _lookups.GetLookupAsync(LookupTableId.RelationType).Returns([new LookupItem { Code = "Offspring", Description = "Offspring" }]);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.RelationTypes.Should().ContainSingle(t => t.Code == "Offspring");
    }

    // Uncovered path: OnGetRelationDetailsAsync's blank-rbse short-circuit.
    [Fact]
    public async Task OnGetRelationDetailsAsync_WhenRbseBlank_ReturnsNotFoundWithoutQuerying()
    {
        var model = CreateModel();

        var result = await model.OnGetRelationDetailsAsync(null);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        json.Value.Should().BeEquivalentTo(new { found = false });
        await _relationsRepository.DidNotReceive().GetRelationDetailsOfRelatedCaseAsync(Arg.Any<string>());
    }

    // Uncovered path: OnGetRelationDetailsAsync's found branch — maps all fields.
    [Fact]
    public async Task OnGetRelationDetailsAsync_WhenFound_ReturnsMappedFields()
    {
        var model = CreateModel();
        _relationsRepository.GetRelationDetailsOfRelatedCaseAsync("002700002").Returns(new RelatedCaseDetailsRecord
        {
            RelationRbse = "002700002",
            Sex = "F",
            Fate = "Alive",
            Name = "Dam Name"
        });

        var result = await model.OnGetRelationDetailsAsync("002700002");

        var json = result.Should().BeOfType<JsonResult>().Subject;
        json.Value.Should().BeEquivalentTo(new
        {
            found = true,
            sex = "F",
            fate = "Alive",
            eartagCountry = (string?)null,
            eartagHerdmark = (string?)null,
            eartag = (string?)null,
            birthDay = (int?)null,
            birthMonth = (int?)null,
            birthYear = (int?)null,
            leftDate = (string?)null,
            sire = "Dam Name"
        });
    }

    // Uncovered path: OnPostAsync's validation-failure branch — RelationType blank
    // triggers RelationValidation.Validate to populate FieldErrors, which are copied
    // into ModelState and the page is re-rendered without attempting a save.
    [Fact]
    public async Task OnPostAsync_WhenRelationTypeMissing_AddsModelErrorsAndDoesNotSave()
    {
        var model = CreateModel();
        model.RelationType = null;
        model.Sex = "M";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("RelationType");
        model.ModelState["RelationType"]!.Errors.Should().Contain(e => e.ErrorMessage == RelationValidation.RelationTypeRequired);
        _connectionFactory.DidNotReceive().CreateConnection();
    }

    // Uncovered path: OnPostAsync's "related RBSE not found" branch — overrides any
    // field errors with RbseNotFound once a RelationRbse is supplied but not found.
    [Fact]
    public async Task OnPostAsync_WhenRelationRbseNotFound_SetsRbseNotFoundError()
    {
        var model = CreateModel();
        model.RelationType = "Offspring";
        model.RelationRbse = "002700003";
        _relationsRepository.GetRelationDetailsOfRelatedCaseAsync("002700003").Returns((RelatedCaseDetailsRecord?)null);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Should().ContainKey("RelationRbse");
        model.ModelState["RelationRbse"]!.Errors.Should().Contain(e => e.ErrorMessage == RelationValidation.RbseNotFound);
    }

    // Uncovered path: OnPostAsync's happy path — builds the command, opens a connection
    // and transaction, saves via the repository, commits, and redirects.
    [Fact]
    public async Task OnPostAsync_WhenValid_SavesRelationAndRedirectsToRelations()
    {
        var model = CreateModel();
        model.RelationType = "Offspring";
        model.Sex = "M";

        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction().Returns(transaction);
        _connectionFactory.CreateConnection().Returns(connection);

        var result = await model.OnPostAsync();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Relations");
        redirect.RouteValues!["rbse"].Should().Be(Rbse);
        model.TempData["Success"].Should().Be("Relation added successfully.");
        connection.Received(1).Open();
        transaction.Received(1).Commit();
        await _relationsRepository.Received(1).AddRelationAsync(Arg.Any<AddCaseRelationCommand>(), connection, transaction);
    }

    // Uncovered path: OnPostAsync's catch block — a repository exception is logged and
    // surfaced as a user-facing ErrorMessage instead of propagating.
    [Fact]
    public async Task OnPostAsync_WhenRepositoryThrows_SetsErrorMessageAndReturnsPage()
    {
        var model = CreateModel();
        model.RelationType = "Offspring";
        model.Sex = "M";

        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction().Returns(transaction);
        _connectionFactory.CreateConnection().Returns(connection);
        _relationsRepository.AddRelationAsync(Arg.Any<AddCaseRelationCommand>(), connection, transaction)
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ErrorMessage.Should().Be("Unable to save the relation.");
        transaction.DidNotReceive().Commit();
    }
}
