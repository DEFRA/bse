using BSE.Host.Pages.Admin;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class PickListsFormModelTests
{
    private readonly IEditableLookupAdminService _admin = Substitute.For<IEditableLookupAdminService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values) =>
        values.ToDictionary(v => v.Key, v => v.Value);

    private void Setup(int tableId, bool hasProcs = true, params Dictionary<string, object?>[] rows)
    {
        _admin.GetEditableLookupsAsync().Returns(new[] { new EditableLookup { Id = tableId, Description = "T" } });
        _admin.GetEditableLookupProcsAsync(tableId).Returns(hasProcs
            ? new EditableLookupProcs { SelectStoredProcedure = "sel", UpdateStoredProcedure = "upd", InsertStoredProcedure = "ins", DeleteStoredProcedure = "del" }
            : null);
        _admin.GetLookupRowsAsync("sel").Returns(rows.AsEnumerable<IDictionary<string, object?>>());
        _lookups.GetBSERegionsAsync().Returns(new[] { new LuBseRegion { Id = 5, Name = "North" } });
        _lookups.GetLookupAsync(Arg.Any<BSE.SharedKernel.LookupTableId>()).Returns(new[] { new LookupItem(1, "AH1", "Aho One") });
    }

    private PickListsAddModel Add(int tableId, Dictionary<string, string> fields, bool canEdit = true)
    {
        var m = new PickListsAddModel(_admin, _lookups) { TableId = tableId, Fields = fields };
        PageTestHelpers.Attach(m, canEdit ? new[] { "VLAMaintenance" } : Array.Empty<string>());
        return m;
    }

    private PickListsEditModel Edit(int tableId, Dictionary<string, string> fields, string original = "", bool canEdit = true)
    {
        var m = new PickListsEditModel(_admin, _lookups) { TableId = tableId, Fields = fields, OriginalKey = original, Key = original };
        PageTestHelpers.Attach(m, canEdit ? new[] { "VLAMaintenance" } : Array.Empty<string>());
        return m;
    }

    [Fact]
    public async Task Add_Get_ReturnsPage_OrRedirects()
    {
        Setup(11);
        (await Add(11, []).OnGetAsync()).Should().BeOfType<PageResult>();
        (await Add(11, [], canEdit: false).OnGetAsync()).Should().BeOfType<RedirectToPageResult>();

        Setup(11, hasProcs: false);
        (await Add(11, []).OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Add_Post_NoPermission_Redirects()
    {
        Setup(11);
        (await Add(11, [], canEdit: false).OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
        await _admin.DidNotReceiveWithAnyArgs().AddCodeDescriptionItemAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Add_Post_MissingRequired_ReturnsPageWithErrors()
    {
        Setup(27);
        var m = Add(27, new Dictionary<string, string> { ["CPH"] = "", ["Name"] = "n", ["Address"] = "a", ["AHO"] = "" });

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();

        m.ModelState.Keys.Should().Contain(["CPH", "AHO"]);
        m.ModelState["AHO"]!.Errors[0].ErrorMessage.Should().Be("Enter a aho");
        m.AhoOptions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Add_Post_DuplicateKey_ReturnsPage()
    {
        Setup(11, true, Row(("Code", "x")));
        var m = Add(11, new Dictionary<string, string> { ["Code"] = "X", ["Description"] = "d" });

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();

        m.ModelState["Code"]!.Errors[0].ErrorMessage.Should().Be(PickListsModel.DuplicateCodeMessage);
    }

    [Fact]
    public async Task Add_Post_AhroDuplicateName_ReturnsPage()
    {
        Setup(28, true, Row(("Name", "dup")));
        var m = Add(28, new Dictionary<string, string> { ["Name"] = "Dup" });

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();

        m.ModelState["Name"]!.Errors[0].ErrorMessage.Should().Be(PickListsModel.DuplicateNameMessage);
    }

    [Fact]
    public async Task Add_Post_CallsMatchingService_ForEachTable()
    {
        Setup(7);
        var tt = Add(7, new Dictionary<string, string> { ["Code"] = "T", ["Description"] = "d", ["IsActive"] = "on" });
        (await tt.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
        await _admin.Received().AddTestTypeAsync("T", "d", true);
        tt.TempData["SuccessMessage"].Should().Be("Record added.");

        Setup(19);
        await Add(19, new Dictionary<string, string> { ["Code"] = "F", ["Description"] = "d", ["IsActive"] = "0" }).OnPostAsync();
        await _admin.Received().AddRelationFateAsync("F", "d", false);

        Setup(13);
        await Add(13, new Dictionary<string, string> { ["Code"] = "B", ["FullName"] = "f", ["AmalgamatedName"] = "a" }).OnPostAsync();
        await _admin.Received().AddBreedAsync("B", "f", "a");

        Setup(16);
        await Add(16, new Dictionary<string, string> { ["Code"] = "A", ["Name"] = "n", ["BSERegionID"] = "5" }).OnPostAsync();
        await _admin.Received().AddAHOAsync("A", "n", 5);

        Setup(17);
        await Add(17, new Dictionary<string, string> { ["Name"] = "s", ["Details"] = "d" }).OnPostAsync();
        await _admin.Received().AddSupplierAsync("s", "d");

        Setup(23);
        await Add(23, new Dictionary<string, string> { ["IDColumn"] = "1", ["Code"] = "C", ["Description"] = "d", ["BSERegionID"] = "x" }).OnPostAsync();
        await _admin.Received().AddBSECountyAsync("1", "C", "d", null);

        Setup(27);
        await Add(27, new Dictionary<string, string> { ["CPH"] = "c", ["Name"] = "n", ["Address"] = "a", ["AHO"] = "AH1" }).OnPostAsync();
        await _admin.Received().AddTSETestingSiteAsync("n", "a", "c", "AH1");

        Setup(28);
        await Add(28, new Dictionary<string, string> { ["Name"] = "r" }).OnPostAsync();
        await _admin.Received().AddAHROAsync("r");

        Setup(11);
        await Add(11, new Dictionary<string, string> { ["Code"] = "1", ["Description"] = "d" }).OnPostAsync();
        await _admin.Received().AddCodeDescriptionItemAsync("ins", "1", "d");
    }

    [Fact]
    public async Task Add_Post_ServiceThrows_SetsErrorAndReturnsPage()
    {
        Setup(11);
        _admin.AddCodeDescriptionItemAsync("ins", "1", "d").Returns(Task.FromException(new InvalidOperationException("x")));
        var m = Add(11, new Dictionary<string, string> { ["Code"] = "1", ["Description"] = "d" });

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();

        m.TempData["ErrorMessage"].Should().Be(PickListsModel.AddFailedMessage);
    }

    [Fact]
    public async Task Edit_Get_LoadsRow_OrRedirects()
    {
        Setup(7, true, Row(("Code", "T"), ("Description", "d"), ("IsActive", true)));
        var m = Edit(7, [], "t");
        (await m.OnGetAsync()).Should().BeOfType<PageResult>();
        m.Fields["IsActive"].Should().Be("Yes");
        m.Fields["Description"].Should().Be("d");
        m.OriginalKey.Should().Be("t");

        (await Edit(7, [], "zzz").OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
        (await Edit(7, [], "t", canEdit: false).OnGetAsync()).Should().BeOfType<RedirectToPageResult>();

        Setup(7, false);
        (await Edit(7, [], "t").OnGetAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Edit_Post_NoPermission_Redirects()
    {
        Setup(11);
        (await Edit(11, [], canEdit: false).OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Edit_Post_Duplicate_ReturnsPage_ButSameKeyAllowed()
    {
        Setup(11, true, Row(("Code", "A")), Row(("Code", "B")));
        var dup = Edit(11, new Dictionary<string, string> { ["Code"] = "B", ["Description"] = "d" }, "A");
        (await dup.OnPostAsync()).Should().BeOfType<PageResult>();
        dup.ModelState["Code"]!.Errors[0].ErrorMessage.Should().Be(PickListsModel.DuplicateCodeMessage);

        var same = Edit(11, new Dictionary<string, string> { ["Code"] = "A", ["Description"] = "d" }, "A");
        (await same.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
        same.TempData["SuccessMessage"].Should().Be("Record updated.");
    }

    [Fact]
    public async Task Edit_Post_AhroDuplicateName_ReturnsPage()
    {
        Setup(28, true, Row(("ID", 1), ("Name", "one")), Row(("ID", 2), ("Name", "two")));
        var m = Edit(28, new Dictionary<string, string> { ["Name"] = "Two" }, "1");

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();

        m.ModelState["Name"]!.Errors[0].ErrorMessage.Should().Be(PickListsModel.DuplicateNameMessage);
    }

    [Fact]
    public async Task Edit_Post_CallsMatchingService_ForEachTable()
    {
        Setup(7);
        await Edit(7, new Dictionary<string, string> { ["Code"] = "T", ["Description"] = "d", ["IsActive"] = "Yes" }, "O").OnPostAsync();
        await _admin.Received().EditTestTypeAsync("O", "T", "d", true);

        Setup(19);
        await Edit(19, new Dictionary<string, string> { ["Code"] = "F", ["Description"] = "d" }, "O").OnPostAsync();
        await _admin.Received().EditRelationFateAsync("O", "F", "d", false);

        Setup(13);
        await Edit(13, new Dictionary<string, string> { ["Code"] = "B", ["FullName"] = "f", ["AmalgamatedName"] = "a" }, "O").OnPostAsync();
        await _admin.Received().EditBreedAsync("O", "B", "f", "a");

        Setup(16);
        await Edit(16, new Dictionary<string, string> { ["Code"] = "A", ["Name"] = "n", ["BSERegionID"] = "5" }, "O").OnPostAsync();
        await _admin.Received().EditAHOAsync("O", "A", "n", 5);

        Setup(17);
        await Edit(17, new Dictionary<string, string> { ["Name"] = "s", ["Details"] = "d" }, "7").OnPostAsync();
        await _admin.Received().EditSupplierAsync(7, "s", "d");

        Setup(23);
        await Edit(23, new Dictionary<string, string> { ["IDColumn"] = "1", ["Code"] = "C", ["Description"] = "d", ["BSERegionID"] = "5" }, "O").OnPostAsync();
        await _admin.Received().EditBSECountyAsync("1", "O", "C", "d", 5);

        Setup(27);
        await Edit(27, new Dictionary<string, string> { ["CPH"] = "c", ["Name"] = "n", ["Address"] = "a", ["AHO"] = "AH1" }, "O").OnPostAsync();
        await _admin.Received().EditTSETestingSiteAsync("O", "n", "a", "c", "AH1");

        Setup(28);
        await Edit(28, new Dictionary<string, string> { ["Name"] = "r" }, "bad").OnPostAsync();
        await _admin.Received().EditAHROAsync(0, "r");

        Setup(11);
        await Edit(11, new Dictionary<string, string> { ["Code"] = "1", ["Description"] = "d" }, "O").OnPostAsync();
        await _admin.Received().EditCodeDescriptionItemAsync("upd", "O", "1", "d");
    }

    [Fact]
    public async Task Edit_Post_ServiceThrows_SetsErrorAndReturnsPage()
    {
        Setup(11);
        _admin.EditCodeDescriptionItemAsync("upd", "O", "1", "d").Returns(Task.FromException(new InvalidOperationException("bad")));
        var m = Edit(11, new Dictionary<string, string> { ["Code"] = "1", ["Description"] = "d" }, "O");

        (await m.OnPostAsync()).Should().BeOfType<PageResult>();

        m.TempData["ErrorMessage"].Should().Be("Update failed: bad");
    }

    [Fact]
    public void Edit_RowValue_FormatsBooleans()
    {
        PickListsEditModel.RowValue(Row(("A", true)), "A").Should().Be("Yes");
        PickListsEditModel.RowValue(Row(("A", false)), "A").Should().Be("No");
        PickListsEditModel.RowValue(Row(("A", 2)), "A").Should().Be("2");
        PickListsEditModel.RowValue(Row(("A", null)), "A").Should().BeEmpty();
        PickListsEditModel.RowValue(Row(), "A").Should().BeEmpty();
    }
}
