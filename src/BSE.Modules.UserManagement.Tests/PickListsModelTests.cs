using BSE.Host.Pages.Admin;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests;

public sealed class PickListsModelTests
{
    private readonly IEditableLookupAdminService _admin = Substitute.For<IEditableLookupAdminService>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values) =>
        values.ToDictionary(v => v.Key, v => v.Value);

    private PickListsModel Create(int tableId, bool canEdit = true, params Dictionary<string, object?>[] rows)
    {
        _admin.GetEditableLookupsAsync().Returns(new[]
        {
            new EditableLookup { Id = 1, TableName = "luA", Description = "A" },
            new EditableLookup { Id = tableId, TableName = "luT", Description = "T" }
        });
        _admin.GetEditableLookupProcsAsync(tableId).Returns(new EditableLookupProcs
        {
            SelectStoredProcedure = "sel",
            UpdateStoredProcedure = "upd",
            InsertStoredProcedure = "ins",
            DeleteStoredProcedure = "del"
        });
        _admin.GetLookupRowsAsync("sel").Returns(rows.AsEnumerable<IDictionary<string, object?>>());
        _lookups.GetBSERegionsAsync().Returns(new[] { new LuBseRegion { Id = 5, Name = "North" } });
        _lookups.GetLookupAsync(Arg.Any<BSE.SharedKernel.LookupTableId>())
            .Returns(new[] { new LookupItem(1, "AH1", "Aho One") });

        var model = new PickListsModel(_admin, _lookups) { TableId = tableId };
        PageTestHelpers.Attach(model, canEdit ? new[] { "VLAMaintenance" } : Array.Empty<string>());
        return model;
    }

    [Fact]
    public async Task Get_DefaultsToValuationAgeTable_WhenNoneRequested()
    {
        var model = Create(11);
        model.TableId = 0;

        await model.OnGetAsync();

        model.TableId.Should().Be(11);
        model.Lookup!.Id.Should().Be(11);
        model.DisplayFields.Select(f => f.Column).Should().Equal("Code", "Description");
        model.KeyColumn.Should().Be("Code");
    }

    [Fact]
    public async Task Get_FallsBackToFirstTable_WhenDefaultMissing()
    {
        var model = Create(7);
        model.TableId = 0;
        _admin.GetEditableLookupProcsAsync(1).Returns((EditableLookupProcs?)null);

        await model.OnGetAsync();

        model.TableId.Should().Be(1);
        model.Rows.Should().BeEmpty();
        model.TotalPages.Should().Be(1);
    }

    [Theory]
    [InlineData(7, new[] { "Code", "Description", "IsActive" }, "Code")]
    [InlineData(19, new[] { "Code", "Description", "IsActive" }, "Code")]
    [InlineData(13, new[] { "Code", "FullName", "AmalgamatedName" }, "Code")]
    [InlineData(16, new[] { "Code", "Name", "BSERegionID" }, "Code")]
    [InlineData(17, new[] { "Name", "Details" }, "ID")]
    [InlineData(23, new[] { "IDColumn", "Code", "Description", "BSERegionID" }, "Code")]
    [InlineData(27, new[] { "CPH", "Name", "Address", "AHO" }, "CPH")]
    [InlineData(28, new[] { "Name" }, "ID")]
    public async Task Get_BuildsFieldsPerTable(int tableId, string[] columns, string key)
    {
        var model = Create(tableId);

        await model.OnGetAsync();

        model.DisplayFields.Select(f => f.Column).Should().Equal(columns);
        model.KeyColumn.Should().Be(key);
    }

    [Fact]
    public async Task Get_LoadsRegionAndAhoOptions_AndDisplaysNames()
    {
        var model = Create(27, true, Row(("CPH", "01/001/0001"), ("AHO", "ah1")));
        await model.OnGetAsync();
        model.AhoOptions.Should().HaveCount(1);
        var aho = model.DisplayFields.First(f => f.IsAhoLookup);
        model.DisplayValue(model.Rows[0], aho).Should().Be("Aho One");

        var region = Create(16, true, Row(("BSERegionID", 5)), Row(("BSERegionID", "abc")), Row(("BSERegionID", 9)));
        await region.OnGetAsync();
        var field = region.DisplayFields.First(f => f.IsRegionLookup);
        region.DisplayValue(region.Rows[0], field).Should().Be("North");
        region.DisplayValue(region.Rows[1], field).Should().Be("abc");
        region.DisplayValue(region.Rows[2], field).Should().BeEmpty();
        region.DisplayValue(Row(("BSERegionID", null)), field).Should().BeEmpty();
    }

    [Fact]
    public async Task Paging_ClampsPageNumber_AndSorts()
    {
        var rows = Enumerable.Range(1, 25).Select(i => Row(("Code", $"C{i:00}"), ("Description", $"D{26 - i}"))).ToArray();
        var model = Create(11, true, rows);
        model.PageNumber = 99;
        model.SortColumn = "Code";
        model.SortDesc = true;

        await model.OnGetAsync();

        model.TotalCount.Should().Be(25);
        model.TotalPages.Should().Be(3);
        model.PageNumber.Should().Be(3);
        model.PagedRows().Should().HaveCount(5);

        model.PageNumber = 1;
        model.PagedRows()[0]["Code"].Should().Be("C25");
        model.SortDesc = false;
        model.PagedRows()[0]["Code"].Should().Be("C01");
        model.SortColumn = "Nope";
        model.PagedRows()[0]["Code"].Should().Be("C01");
    }

    [Fact]
    public async Task Paging_NegativePage_IsClampedToOne()
    {
        var model = Create(11, true, Row(("Code", "A")));
        model.PageNumber = -3;
        await model.OnGetAsync();
        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Sorting_Numeric_AndLookupColumns()
    {
        var model = Create(16, true,
            Row(("Code", "A"), ("BSERegionID", "10")),
            Row(("Code", "B"), ("BSERegionID", "x")),
            Row(("Code", "C"), ("BSERegionID", "5")));
        model.SortColumn = "BSERegionID";
        await model.OnGetAsync();
        model.PagedRows().Select(r => r["Code"]).Should().Equal("A", "C", "B");

        model.SortDesc = true;
        model.PagedRows().Should().HaveCount(3);
    }

    [Fact]
    public async Task Sorting_NumericNonLookup_UsesDecimalValues()
    {
        var model = Create(23, true,
            Row(("Code", "A"), ("IDColumn", "10")),
            Row(("Code", "B"), ("IDColumn", "2")));
        model.SortColumn = "IDColumn";
        await model.OnGetAsync();
        model.PagedRows().Should().HaveCount(2);
    }

    [Theory]
    [InlineData(7, "T")]
    [InlineData(19, "F")]
    [InlineData(13, "B")]
    [InlineData(16, "AH")]
    [InlineData(23, "CO")]
    [InlineData(27, "CPH1")]
    [InlineData(11, "X")]
    public async Task Delete_CallsMatchingService(int tableId, string key)
    {
        var model = Create(tableId);
        model.Fields = new Dictionary<string, string> { [model.KeyColumn] = key };
        await model.OnGetAsync();

        var result = await model.OnPostDeleteAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["SuccessMessage"].Should().Be("Record deleted.");
        switch (tableId)
        {
            case 7: await _admin.Received().DeleteTestTypeAsync(key); break;
            case 19: await _admin.Received().DeleteRelationFateAsync(key); break;
            case 13: await _admin.Received().DeleteBreedAsync(key); break;
            case 16: await _admin.Received().DeleteAHOAsync(key); break;
            case 23: await _admin.Received().DeleteBSECountyAsync(key); break;
            case 27: await _admin.Received().DeleteTSETestingSiteAsync(key); break;
            default: await _admin.Received().DeleteCodeDescriptionItemAsync("del", key); break;
        }
    }

    [Fact]
    public async Task Delete_Supplier_AndAhro_UseKeys()
    {
        var supplier = Create(17);
        supplier.Fields = new Dictionary<string, string> { ["ID"] = "42" };
        await supplier.OnPostDeleteAsync();
        await _admin.Received().DeleteSupplierAsync(42);

        var ahro = Create(28);
        ahro.Fields = new Dictionary<string, string> { ["ID"] = "x", ["Name"] = " Ahro " };
        await ahro.OnPostDeleteAsync();
        await _admin.Received().DeleteAHROAsync("Ahro");

        var bad = Create(17);
        bad.Fields = new Dictionary<string, string> { ["ID"] = "zz" };
        await bad.OnPostDeleteAsync();
        await _admin.Received().DeleteSupplierAsync(0);
    }

    [Fact]
    public async Task Delete_WithoutEditRole_DoesNothing()
    {
        var model = Create(11, false);
        var result = await model.OnPostDeleteAsync();
        result.Should().BeOfType<RedirectToPageResult>();
        await _admin.DidNotReceiveWithAnyArgs().DeleteCodeDescriptionItemAsync(default!, default!);
    }

    [Fact]
    public async Task Delete_WhenServiceThrows_SetsErrorMessage()
    {
        var model = Create(11);
        model.Fields = new Dictionary<string, string> { ["Code"] = "X" };
        _admin.DeleteCodeDescriptionItemAsync("del", "X").Returns(Task.FromException(new InvalidOperationException("boom")));

        var result = await model.OnPostDeleteAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        model.TempData["ErrorMessage"].Should().Be("Delete failed: boom");
    }

    [Fact]
    public void StaticHelpers_BehaveAsExpected()
    {
        PickListsModel.MaxLengthFor(28, "Name").Should().Be(100);
        PickListsModel.MaxLengthFor(28, "Other").Should().BeNull();
        PickListsModel.MaxLengthFor(1, "Name").Should().BeNull();
        PickListsModel.RowValue(Row(("A", true)), "A").Should().Be("Yes");
        PickListsModel.RowValue(Row(("A", false)), "A").Should().Be("No");
        PickListsModel.RowValue(Row(("A", 3)), "A").Should().Be("3");
        PickListsModel.RowValue(Row(("A", null)), "A").Should().BeEmpty();
        PickListsModel.RowValue(Row(), "A").Should().BeEmpty();
        PickListsModel.RowBool(Row(("A", true)), "A").Should().BeTrue();
        PickListsModel.RowBool(Row(("A", "x")), "A").Should().BeFalse();
        PickListsModel.RowBool(Row(), "A").Should().BeFalse();
    }
}
