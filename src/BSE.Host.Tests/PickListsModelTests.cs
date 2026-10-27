using System.Security.Claims;
using BSE.Host.Pages.Admin;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BSE.Host.Tests;

public class PickListsModelTests
{
    private readonly IEditableLookupAdminService _admin = Substitute.For<IEditableLookupAdminService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private PickListsModel CreateModel(bool canEdit = true, int tableId = 0)
    {
        var http = new DefaultHttpContext();
        if (canEdit)
            http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "VLAMaintenance")], "test"));

        _admin.GetEditableLookupsAsync().Returns(Task.FromResult<IEnumerable<EditableLookup>>(
            [new EditableLookup { Id = 11, Description = "Valuation" }, new EditableLookup { Id = 16, Description = "AHO" }]));
        _admin.GetEditableLookupProcsAsync(Arg.Any<int>()).Returns(Task.FromResult<EditableLookupProcs?>(
            new EditableLookupProcs { SelectStoredProcedure = "sel", DeleteStoredProcedure = "del" }));
        _admin.GetLookupRowsAsync("sel").Returns(Task.FromResult<IEnumerable<IDictionary<string, object?>>>([]));
        _lookups.GetBSERegionsAsync().Returns(Task.FromResult<IEnumerable<LuBSERegion>>(
            [new LuBSERegion { Id = 1, Name = "North" }, new LuBSERegion { Id = 2, Name = "South" }]));
        _lookups.GetLookupAsync(Arg.Any<BSE.SharedKernel.LookupTableId>()).Returns(Task.FromResult<IEnumerable<LookupItem>>(
            [new LookupItem(1, "A1", "Aho One")]));

        return new PickListsModel(_admin, _lookups)
        {
            TableId = tableId,
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>())
        };
    }

    private void Rows(params IDictionary<string, object?>[] rows) =>
        _admin.GetLookupRowsAsync("sel").Returns(Task.FromResult<IEnumerable<IDictionary<string, object?>>>(rows));

    private static IDictionary<string, object?> Row(params (string, object?)[] values) =>
        values.ToDictionary(v => v.Item1, v => v.Item2);

    [Fact]
    public async Task OnGet_NoTable_DefaultsToValuationAge()
    {
        var model = CreateModel();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        model.TableId.Should().Be(11);
        model.Lookup!.Description.Should().Be("Valuation");
        model.DisplayFields.Select(f => f.Column).Should().Equal("Code", "Description");
        model.TotalPages.Should().Be(1);
        model.KeyColumn.Should().Be("Code");
    }

    [Fact]
    public async Task OnGet_AhoTable_LoadsRegionAndAhoOptions()
    {
        var model = CreateModel(tableId: 16);

        await model.OnGetAsync();

        model.BseRegionOptions.Should().HaveCount(2);
        model.AhoOptions.Should().BeEmpty();
        model.CanEdit.Should().BeTrue();
    }

    [Fact]
    public async Task OnGet_TseSite_LoadsAhoOptions()
    {
        var model = CreateModel(tableId: 27);

        await model.OnGetAsync();

        model.AhoOptions.Should().ContainSingle();
        model.KeyColumn.Should().Be("CPH");
    }

    [Fact]
    public async Task OnGet_NoProcs_LeavesRowsEmpty()
    {
        var model = CreateModel(tableId: 11);
        _admin.GetEditableLookupProcsAsync(11).Returns(Task.FromResult<EditableLookupProcs?>(null));

        await model.OnGetAsync();

        model.Rows.Should().BeEmpty();
    }

    [Theory]
    [InlineData(7, "IsActive")]
    [InlineData(13, "AmalgamatedName")]
    [InlineData(17, "Details")]
    [InlineData(23, "IDColumn")]
    [InlineData(28, "Name")]
    public async Task OnGet_PerTableFieldShape(int tableId, string expectedColumn)
    {
        var model = CreateModel(tableId: tableId);

        await model.OnGetAsync();

        model.DisplayFields.Select(f => f.Column).Should().Contain(expectedColumn);
    }

    [Fact]
    public async Task OnGet_PageNumberBeyondEnd_IsClamped()
    {
        var model = CreateModel(tableId: 11);
        Rows(Row(("Code", "A")), Row(("Code", "B")));
        model.PageNumber = 50;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
        model.PageNumber = 0;
        await model.OnGetAsync();
        model.PageNumber.Should().Be(1);
    }

    [Theory]
    [InlineData("Code", false, "A")]
    [InlineData("Code", true, "C")]
    [InlineData("Unknown", false, "C")]
    public async Task GetPagedRows_SortsStrings(string column, bool desc, string expectedFirst)
    {
        var model = CreateModel(tableId: 11);
        Rows(Row(("Code", "C")), Row(("Code", "A")), Row(("Code", "B")));
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.GetPagedRows()[0]["Code"].Should().Be(expectedFirst);
    }

    [Theory]
    [InlineData(false, "North")]
    [InlineData(true, "South")]
    public async Task GetPagedRows_SortsByRegionName(bool desc, string expectedRegion)
    {
        var model = CreateModel(tableId: 16);
        Rows(Row(("Code", "X"), ("BSERegionID", 2)), Row(("Code", "Y"), ("BSERegionID", 1)));
        model.SortColumn = "BSERegionID";
        model.SortDesc = desc;

        await model.OnGetAsync();

        var field = model.DisplayFields.Single(f => f.Column == "BSERegionID");
        model.DisplayValue(model.GetPagedRows()[0], field).Should().Be(expectedRegion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPagedRows_SortsByAhoName(bool desc)
    {
        var model = CreateModel(tableId: 27);
        Rows(Row(("CPH", "1"), ("AHO", "A1")), Row(("CPH", "2"), ("AHO", "zz")));
        model.SortColumn = "AHO";
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.GetPagedRows().Should().HaveCount(2);
    }

    [Fact]
    public void RowHelpers_HandleNullBoolAndMissing()
    {
        var row = Row(("A", null), ("B", true), ("C", false), ("D", 5));

        PickListsModel.RowValue(row, "A").Should().BeEmpty();
        PickListsModel.RowValue(row, "B").Should().Be("Yes");
        PickListsModel.RowValue(row, "C").Should().Be("No");
        PickListsModel.RowValue(row, "D").Should().Be("5");
        PickListsModel.RowValue(row, "Z").Should().BeEmpty();
        PickListsModel.RowBool(row, "B").Should().BeTrue();
        PickListsModel.RowBool(row, "C").Should().BeFalse();
        PickListsModel.RowBool(row, "Z").Should().BeFalse();
    }

    [Fact]
    public async Task DisplayValue_UnknownRegionAndNonNumericRaw()
    {
        var model = CreateModel(tableId: 16);
        await model.OnGetAsync();
        var field = model.DisplayFields.Single(f => f.Column == "BSERegionID");

        model.DisplayValue(Row(("BSERegionID", 99)), field).Should().BeEmpty();
        model.DisplayValue(Row(("BSERegionID", "abc")), field).Should().Be("abc");
        model.DisplayValue(Row(("BSERegionID", null)), field).Should().BeEmpty();
    }

    [Fact]
    public async Task DisplayValue_AhoResolvesDescriptionOrBlank()
    {
        var model = CreateModel(tableId: 27);
        await model.OnGetAsync();
        var field = model.DisplayFields.Single(f => f.Column == "AHO");

        model.DisplayValue(Row(("AHO", "a1")), field).Should().Be("Aho One");
        model.DisplayValue(Row(("AHO", "none")), field).Should().BeEmpty();
    }

    [Fact]
    public void MaxLengthFor_OnlyAhroName()
    {
        PickListsModel.MaxLengthFor(28, "Name").Should().Be(100);
        PickListsModel.MaxLengthFor(28, "Other").Should().BeNull();
        PickListsModel.MaxLengthFor(11, "Name").Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithoutEditRole_RedirectsWithoutDeleting()
    {
        var model = CreateModel(canEdit: false, tableId: 11);

        var result = await model.OnPostDeleteAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        await _admin.DidNotReceiveWithAnyArgs().DeleteCodeDescriptionItemAsync(default!, default!);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(19)]
    [InlineData(23)]
    [InlineData(27)]
    [InlineData(28)]
    public async Task Delete_RoutesToMatchingServiceCall(int tableId)
    {
        var model = CreateModel(tableId: tableId);
        model.Fields = new() { ["Code"] = " K1 ", ["CPH"] = "K1", ["ID"] = "12", ["Name"] = "N1" };

        var result = await model.OnPostDeleteAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["SuccessMessage"].Should().Be("Record deleted.");
    }

    [Fact]
    public async Task Delete_DefaultTable_UsesProcAndTrimmedCode()
    {
        var model = CreateModel(tableId: 11);
        model.Fields = new() { ["Code"] = " K1 " };

        await model.OnPostDeleteAsync();

        await _admin.Received(1).DeleteCodeDescriptionItemAsync("del", "K1");
    }

    [Fact]
    public async Task Delete_ServiceThrows_SetsErrorMessage()
    {
        var model = CreateModel(tableId: 11);
        _admin.DeleteCodeDescriptionItemAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("in use")));

        await model.OnPostDeleteAsync();

        model.TempData["ErrorMessage"].Should().Be("Delete failed: in use");
    }
}
