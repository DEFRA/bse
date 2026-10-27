using System.Security.Claims;
using BSE.Host.Pages.Admin;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BSE.Host.Tests;

public class PickListsEditModelTests
{
    private readonly IEditableLookupAdminService _admin = Substitute.For<IEditableLookupAdminService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private PickListsEditModel CreateModel(int tableId, bool canEdit = true, bool hasProcs = true)
    {
        var http = new DefaultHttpContext();
        if (canEdit)
            http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "VLAMaintenance")], "test"));

        _admin.GetEditableLookupsAsync().Returns(Task.FromResult<IEnumerable<EditableLookup>>(
            [new EditableLookup { Id = tableId, Description = "T" }]));
        _admin.GetEditableLookupProcsAsync(tableId).Returns(Task.FromResult<EditableLookupProcs?>(
            hasProcs ? new EditableLookupProcs { SelectStoredProcedure = "sel", UpdateStoredProcedure = "upd" } : null));
        _admin.GetLookupRowsAsync("sel").Returns(Task.FromResult<IEnumerable<IDictionary<string, object?>>>(
        [
            new Dictionary<string, object?> { ["Code"] = "A", ["CPH"] = "A", ["ID"] = 3, ["Name"] = "One", ["IsActive"] = true, ["Description"] = "d" },
            new Dictionary<string, object?> { ["Code"] = "B", ["CPH"] = "B", ["ID"] = 4, ["Name"] = "Two", ["IsActive"] = false, ["Description"] = "e" }
        ]));
        _lookups.GetBSERegionsAsync().Returns(Task.FromResult<IEnumerable<LuBSERegion>>([new LuBSERegion { Id = 1, Name = "N" }]));
        _lookups.GetLookupAsync(Arg.Any<BSE.SharedKernel.LookupTableId>())
            .Returns(Task.FromResult<IEnumerable<LookupItem>>([new LookupItem(1, "A1", "Aho")]));

        return new PickListsEditModel(_admin, _lookups)
        {
            TableId = tableId,
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task OnGet_ExistingRow_PopulatesFields()
    {
        var model = CreateModel(11);
        model.Key = "b";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.OriginalKey.Should().Be("b");
        model.Fields["Description"].Should().Be("e");
    }

    [Fact]
    public async Task OnGet_UnknownKey_Redirects()
    {
        var model = CreateModel(11);
        model.Key = "zzz";

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnGet_NotEditor_Redirects()
    {
        var model = CreateModel(11, canEdit: false);

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnGet_NoProcs_Redirects()
    {
        var model = CreateModel(11, hasProcs: false);

        (await model.OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Theory]
    [InlineData(17, "ID")]
    [InlineData(28, "ID")]
    [InlineData(27, "CPH")]
    [InlineData(11, "Code")]
    public void KeyColumn_ByTable(int tableId, string expected) =>
        CreateModel(tableId).KeyColumn.Should().Be(expected);

    [Theory]
    [InlineData(16)]
    [InlineData(23)]
    [InlineData(27)]
    public async Task OnGet_LookupTables_LoadOptions(int tableId)
    {
        var model = CreateModel(tableId);
        model.Key = tableId == 27 ? "A" : "A";

        await model.OnGetAsync();

        (model.BseRegionOptions.Count + model.AhoOptions.Count).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPost_NotEditor_Redirects()
    {
        var model = CreateModel(11, canEdit: false);

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnPost_MissingFields_ReturnsPageWithErrors()
    {
        var model = CreateModel(11);
        model.OriginalKey = "A";

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState.Keys.Should().Contain(["Code", "Description"]);
    }

    [Fact]
    public async Task OnPost_CodeCollidesWithOtherRow_AddsDuplicateError()
    {
        var model = CreateModel(11);
        model.OriginalKey = "A";
        model.Fields = new() { ["Code"] = "b", ["Description"] = "x" };

        await model.OnPostAsync();

        model.ModelState["Code"]!.Errors.Single().ErrorMessage.Should().Be(PickListsModel.DuplicateCodeMessage);
    }

    [Fact]
    public async Task OnPost_AhroNameCollidesWithOtherRow_AddsDuplicateNameError()
    {
        var model = CreateModel(28);
        model.OriginalKey = "3";
        model.Fields = new() { ["Name"] = "two" };

        await model.OnPostAsync();

        model.ModelState["Name"]!.Errors.Single().ErrorMessage.Should().Be(PickListsModel.DuplicateNameMessage);
    }

    [Fact]
    public async Task OnPost_KeepingOwnCode_UpdatesViaProc()
    {
        var model = CreateModel(11);
        model.OriginalKey = "A";
        model.Fields = new() { ["Code"] = "a", ["Description"] = " new " };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _admin.Received(1).EditCodeDescriptionItemAsync("upd", "A", "a", "new");
        model.TempData["SuccessMessage"].Should().Be("Record updated.");
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
    public async Task OnPost_PerTable_Succeeds(int tableId)
    {
        var model = CreateModel(tableId);
        model.OriginalKey = tableId is 17 or 28 ? "3" : "A";
        model.Fields = new()
        {
            ["Code"] = "N1", ["Description"] = "D", ["FullName"] = "F", ["AmalgamatedName"] = "A", ["Name"] = "Fresh",
            ["Details"] = "d", ["IDColumn"] = "9", ["CPH"] = "A", ["Address"] = "addr", ["AHO"] = "A1",
            ["BSERegionID"] = "1", ["IsActive"] = "Yes"
        };

        var result = await model.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnPost_ServiceThrows_SetsErrorMessage()
    {
        var model = CreateModel(11);
        model.OriginalKey = "A";
        model.Fields = new() { ["Code"] = "A", ["Description"] = "x" };
        _admin.EditCodeDescriptionItemAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("db")));

        var result = await model.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        model.TempData["ErrorMessage"].Should().Be("Update failed: db");
    }

    [Fact]
    public void RowValue_FormatsValues()
    {
        var row = new Dictionary<string, object?> { ["a"] = true, ["b"] = null, ["c"] = 4 };

        PickListsEditModel.RowValue(row, "a").Should().Be("Yes");
        PickListsEditModel.RowValue(row, "b").Should().BeEmpty();
        PickListsEditModel.RowValue(row, "c").Should().Be("4");
        PickListsEditModel.RowValue(row, "zz").Should().BeEmpty();
    }
}
