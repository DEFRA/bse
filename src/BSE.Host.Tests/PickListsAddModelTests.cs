using System.Security.Claims;
using BSE.Host.Pages.Admin;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BSE.Host.Tests;

public class PickListsAddModelTests
{
    private readonly IEditableLookupAdminService _admin = Substitute.For<IEditableLookupAdminService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private PickListsAddModel CreateModel(int tableId, bool canEdit = true)
    {
        var http = new DefaultHttpContext();
        if (canEdit)
            http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "VLAMaintenance")], "test"));

        _admin.GetEditableLookupsAsync().Returns(Task.FromResult<IEnumerable<EditableLookup>>(
            [new EditableLookup { Id = tableId, Description = "T" }]));
        _admin.GetEditableLookupProcsAsync(tableId).Returns(Task.FromResult<EditableLookupProcs?>(
            new EditableLookupProcs { SelectStoredProcedure = "sel", InsertStoredProcedure = "ins" }));
        _admin.GetLookupRowsAsync("sel").Returns(Task.FromResult<IEnumerable<IDictionary<string, object?>>>(
            [new Dictionary<string, object?> { ["Code"] = "DUP", ["CPH"] = "DUP", ["Name"] = "Taken" }]));
        _lookups.GetBSERegionsAsync().Returns(Task.FromResult<IEnumerable<LuBSERegion>>([new LuBSERegion { Id = 1, Name = "N" }]));
        _lookups.GetLookupAsync(Arg.Any<BSE.SharedKernel.LookupTableId>())
            .Returns(Task.FromResult<IEnumerable<LookupItem>>([new LookupItem(1, "A1", "Aho")]));

        return new PickListsAddModel(_admin, _lookups)
        {
            TableId = tableId,
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task OnGet_Editor_ReturnsPage()
    {
        var model = CreateModel(11);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.DisplayFields.Should().HaveCount(2);
    }

    [Fact]
    public async Task OnGet_NotEditor_Redirects()
    {
        var model = CreateModel(11, canEdit: false);

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Theory]
    [InlineData(17, "ID")]
    [InlineData(28, "ID")]
    [InlineData(27, "CPH")]
    [InlineData(11, "Code")]
    public void KeyColumn_ByTable(int tableId, string expected) =>
        CreateModel(tableId).KeyColumn.Should().Be(expected);

    [Fact]
    public async Task OnPost_NotEditor_RedirectsWithoutAdding()
    {
        var model = CreateModel(11, canEdit: false);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _admin.DidNotReceiveWithAnyArgs().AddCodeDescriptionItemAsync(default!, default!, default!);
    }

    [Fact]
    public async Task OnPost_MissingFields_AddsErrors()
    {
        var model = CreateModel(11);

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain(["Code", "Description"]);
    }

    [Fact]
    public async Task OnPost_DuplicateCode_AddsDuplicateError()
    {
        var model = CreateModel(11);
        model.Fields = new() { ["Code"] = "dup", ["Description"] = "x" };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState["Code"]!.Errors.Single().ErrorMessage.Should().Be(PickListsModel.DuplicateCodeMessage);
    }

    [Fact]
    public async Task OnPost_DuplicateAhroName_AddsDuplicateNameError()
    {
        var model = CreateModel(28);
        model.Fields = new() { ["Name"] = "taken" };

        await model.OnPostAsync();

        model.ModelState["Name"]!.Errors.Single().ErrorMessage.Should().Be(PickListsModel.DuplicateNameMessage);
    }

    [Fact]
    public async Task OnPost_ValidDefaultTable_AddsViaProcAndRedirects()
    {
        var model = CreateModel(11);
        model.Fields = new() { ["Code"] = " N1 ", ["Description"] = " Desc " };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _admin.Received(1).AddCodeDescriptionItemAsync("ins", "N1", "Desc");
        model.TempData["SuccessMessage"].Should().Be("Record added.");
    }

    [Theory]
    [InlineData(7)]
    [InlineData(13)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(19)]
    [InlineData(23)]
    [InlineData(27)]
    [InlineData(28)]
    public async Task OnPost_ValidPerTable_Succeeds(int tableId)
    {
        var model = CreateModel(tableId);
        model.Fields = new()
        {
            ["Code"] = "N1", ["Description"] = "D", ["FullName"] = "F", ["AmalgamatedName"] = "A", ["Name"] = "New",
            ["Details"] = "d", ["IDColumn"] = "9", ["CPH"] = "NEWCPH", ["Address"] = "addr", ["AHO"] = "A1",
            ["BSERegionID"] = "1", ["IsActive"] = "on"
        };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["SuccessMessage"].Should().Be("Record added.");
    }

    [Fact]
    public async Task OnPost_TestType_PassesBooleanFlag()
    {
        var model = CreateModel(7);
        model.Fields = new() { ["Code"] = "N1", ["Description"] = "D", ["IsActive"] = "true" };

        await model.OnPostAsync();

        await _admin.Received(1).AddTestTypeAsync("N1", "D", true);
    }

    [Fact]
    public async Task OnPost_ServiceThrows_SetsErrorAndReturnsPage()
    {
        var model = CreateModel(11);
        model.Fields = new() { ["Code"] = "N1", ["Description"] = "D" };
        _admin.AddCodeDescriptionItemAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("db")));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.TempData["ErrorMessage"].Should().Be(PickListsModel.AddFailedMessage);
    }

    [Fact]
    public async Task OnPost_AhoWithoutRegion_RequiresSelection()
    {
        var model = CreateModel(16);
        model.Fields = new() { ["Code"] = "N1", ["Name"] = "n" };

        await model.OnPostAsync();

        model.ModelState["BSERegionID"]!.Errors.Single().ErrorMessage.Should().StartWith("Select a");
    }
}
