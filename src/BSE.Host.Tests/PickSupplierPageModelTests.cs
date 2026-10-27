using BSE.Host.Pages.Case;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class PickSupplierPageModelTests
{
    private readonly ILookupRepository _lookup = Substitute.For<ILookupRepository>();
    private readonly IEditableLookupAdminService _admin = Substitute.For<IEditableLookupAdminService>();

    private PickSupplierModel CreateModel(string rbse = "002600001", bool access = true)
    {
        var model = new PickSupplierModel(_lookup, _admin) { Rbse = rbse };
        if (access) PageTestContext.Attach(model, "DataEntry", "VLAAccess");
        else PageTestContext.Attach(model, "DataEntry");
        return model;
    }

    private static List<LuSupplier> Suppliers(int count) =>
        Enumerable.Range(1, count).Select(i => new LuSupplier { Id = i, Name = $"S{i:00}", Details = $"D{count - i:00}" }).ToList();

    private static void AssertHome(IActionResult r) =>
        r.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");

    [Fact]
    public async Task OnGet_WithoutRoles_RedirectsHome()
    {
        AssertHome(await CreateModel(access: false).OnGetAsync());
    }

    [Fact]
    public async Task OnGet_BlankRbse_RedirectsToFeeds()
    {
        var result = await CreateModel("").OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Feeds");
    }

    [Fact]
    public async Task OnGet_NoName_ListsAllSuppliersPaged()
    {
        _lookup.GetSuppliersAsync().Returns(Suppliers(25));
        var model = CreateModel("26/1");
        model.PageNumber = 3;

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Rbse.Should().Be("002600001");
        model.TotalCount.Should().Be(25);
        model.TotalPages.Should().Be(3);
        model.Suppliers.Should().HaveCount(5);
        model.Suppliers[0].Name.Should().Be("S21");
        model.CanCreateNew.Should().BeFalse();
    }

    [Fact]
    public async Task OnGet_PageNumberOutOfRange_IsClamped()
    {
        _lookup.GetSuppliersAsync().Returns(Suppliers(3));
        var model = CreateModel();
        model.PageNumber = 99;

        await model.OnGetAsync();

        model.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task OnGet_WithName_SearchesUppercase()
    {
        _lookup.GetPossibleSuppliersAsync("ABC").Returns(Suppliers(2));
        var model = CreateModel();
        model.Name = "  abc ";

        await model.OnGetAsync();

        model.Name.Should().Be("abc");
        model.CanCreateNew.Should().BeTrue();
        model.Suppliers.Should().HaveCount(2);
        await _lookup.DidNotReceive().GetSuppliersAsync();
    }

    [Theory]
    [InlineData("Name", true, "S03")]
    [InlineData("Name", false, "S01")]
    [InlineData("Details", true, "S01")]
    [InlineData("Details", false, "S03")]
    [InlineData("Other", true, "S01")]
    public async Task OnGet_Sorting_OrdersRows(string column, bool desc, string first)
    {
        _lookup.GetSuppliersAsync().Returns(Suppliers(3));
        var model = CreateModel();
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.Suppliers[0].Name.Should().Be(first);
    }

    [Fact]
    public async Task UseSelected_WithoutRoles_RedirectsHome()
    {
        AssertHome(await CreateModel(access: false).OnPostUseSelectedAsync(1, null));
    }

    [Fact]
    public async Task UseSelected_Found_RedirectsWithPickedSupplier()
    {
        _lookup.GetSuppliersAsync().Returns(Suppliers(3));
        var model = CreateModel();

        var result = await model.OnPostUseSelectedAsync(2, null);

        var r = result.Should().BeOfType<RedirectToPageResult>().Subject;
        r.PageName.Should().Be("/Case/Feeds");
        r.RouteValues!["pickedSupplierId"].Should().Be(2);
        r.RouteValues["pickedSupplierName"].Should().Be("S02");
    }

    [Fact]
    public async Task UseSelected_SearchedAndSelectedNameOverride_UsesOverride()
    {
        _lookup.GetPossibleSuppliersAsync("X").Returns(Suppliers(2));
        var model = CreateModel();
        model.Name = "x";

        var result = await model.OnPostUseSelectedAsync(1, "Custom");

        result.Should().BeOfType<RedirectToPageResult>().Which.RouteValues!["pickedSupplierName"].Should().Be("Custom");
    }

    [Fact]
    public async Task UseSelected_NotFound_ReturnsPageWithError()
    {
        _lookup.GetSuppliersAsync().Returns(Suppliers(2));
        var model = CreateModel();

        (await model.OnPostUseSelectedAsync(99, null)).Should().BeOfType<PageResult>();

        model.ModelState[string.Empty]!.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("could not be found"));
    }

    [Fact]
    public async Task New_WithoutRoles_RedirectsHome()
    {
        AssertHome(await CreateModel(access: false).OnPostNewAsync());
    }

    [Fact]
    public async Task New_BlankName_ReturnsPageWithoutCreating()
    {
        _lookup.GetSuppliersAsync().Returns(Suppliers(1));
        var model = CreateModel();

        (await model.OnPostNewAsync()).Should().BeOfType<PageResult>();

        await _admin.DidNotReceiveWithAnyArgs().AddSupplierAsync(default!, default);
    }

    [Fact]
    public async Task New_BlankDetails_AddsModelError()
    {
        _lookup.GetPossibleSuppliersAsync("ACME").Returns(Suppliers(1));
        var model = CreateModel();
        model.Name = "Acme";

        (await model.OnPostNewAsync()).Should().BeOfType<PageResult>();

        model.ModelState[nameof(PickSupplierModel.NewSupplierDetails)]!.Errors.Should().ContainSingle();
        await _admin.DidNotReceiveWithAnyArgs().AddSupplierAsync(default!, default);
    }

    [Fact]
    public async Task New_ExactMatch_RedirectsToCreatedSupplier()
    {
        _lookup.GetPossibleSuppliersAsync("ACME").Returns(
        [
            new LuSupplier { Id = 4, Name = "Acme", Details = "other" },
            new LuSupplier { Id = 5, Name = "ACME", Details = "Town" },
            new LuSupplier { Id = 3, Name = "Acme", Details = "Town" }
        ]);
        var model = CreateModel();
        model.Name = "Acme";
        model.NewSupplierDetails = " Town ";

        var result = await model.OnPostNewAsync();

        await _admin.Received(1).AddSupplierAsync("Acme", "Town");
        var r = result.Should().BeOfType<RedirectToPageResult>().Subject;
        r.RouteValues!["pickedSupplierId"].Should().Be(5);
    }

    [Fact]
    public async Task New_NoDetailsMatch_FallsBackToNameMatch()
    {
        _lookup.GetPossibleSuppliersAsync("ACME").Returns([new LuSupplier { Id = 7, Name = "Acme", Details = null }]);
        var model = CreateModel();
        model.Name = "Acme";
        model.NewSupplierDetails = "Town";

        var result = await model.OnPostNewAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.RouteValues!["pickedSupplierId"].Should().Be(7);
    }

    [Fact]
    public async Task New_CreatedNotFound_ReturnsPageWithError()
    {
        _lookup.GetPossibleSuppliersAsync("ACME").Returns([new LuSupplier { Id = 7, Name = "Other" }]);
        var model = CreateModel();
        model.Name = "Acme";
        model.NewSupplierDetails = "Town";

        (await model.OnPostNewAsync()).Should().BeOfType<PageResult>();

        model.ModelState[string.Empty]!.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("could not be selected"));
    }

    [Fact]
    public void Cancel_WithoutRoles_RedirectsHome()
    {
        AssertHome(CreateModel(access: false).OnPostCancel());
    }

    [Fact]
    public void Cancel_RedirectsToFeedsWithReset()
    {
        var r = CreateModel("26/1").OnPostCancel().Should().BeOfType<RedirectToPageResult>().Subject;

        r.PageName.Should().Be("/Case/Feeds");
        r.RouteValues!["rbse"].Should().Be("002600001");
        r.RouteValues["resetSupplier"].Should().Be(true);
    }
}
