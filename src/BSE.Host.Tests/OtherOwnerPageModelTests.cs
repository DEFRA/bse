using System.Data;
using BSE.Host.Pages.Case;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class OtherOwnerPageModelTests
{
    private const string Rbse = "002600001";
    private const string Cphh = "01/001/0001/01";
    private const string RawCphh = "01001000101";

    private readonly IOtherOwnerRepository _owners = Substitute.For<IOtherOwnerRepository>();
    private readonly ILookupDataService _lookups = Substitute.For<ILookupDataService>();
    private readonly IDbConnectionFactory _factory = Substitute.For<IDbConnectionFactory>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();

    public OtherOwnerPageModelTests()
    {
        _lookups.GetLookupAsync(LookupTableId.OwnerType).Returns(Task.FromResult<IEnumerable<LookupItem>>(
            [new LookupItem(1, "PRV", "Previous Owner"), new LookupItem(2, "CUR", "Current Owner")]));
        _owners.GetByRbseAsync(Arg.Any<string>()).Returns(Task.FromResult<IReadOnlyList<OtherOwnerRecord>>([]));
        _connection.BeginTransaction().Returns(_transaction);
        _factory.CreateConnection().Returns(_connection);
    }

    private OtherOwnerAddModel CreateAdd()
    {
        var model = new OtherOwnerAddModel(_owners, _lookups, _factory) { Rbse = "00/26/00001" };
        PageTestContext.Attach(model);
        return model;
    }

    private OtherOwnerEditModel CreateEdit()
    {
        var model = new OtherOwnerEditModel(_owners, _lookups, _factory) { Rbse = "00/26/00001", Id = 3 };
        PageTestContext.Attach(model);
        return model;
    }

    private void Existing(params OtherOwnerRecord[] rows) =>
        _owners.GetByRbseAsync(Rbse).Returns(Task.FromResult<IReadOnlyList<OtherOwnerRecord>>(rows));

    [Fact]
    public async Task Add_OnGet_LoadsOwnerTypes()
    {
        var model = CreateAdd();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.OwnerTypes.Should().HaveCount(2);
    }

    [Fact]
    public async Task Add_OnPost_NothingEntered_AddsTypeAndNameErrors()
    {
        var model = CreateAdd();

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState.Keys.Should().BeEquivalentTo("OwnerType", "OwnerName");
        await _owners.DidNotReceiveWithAnyArgs().AddAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Add_OnPost_ShortCphh_IsRejected()
    {
        var model = CreateAdd();
        model.OwnerType = "CUR";
        model.OwnerCphh = "01/001";

        await model.OnPostAsync();

        model.ModelState["OwnerCphh"]!.Errors[0].ErrorMessage.Should().Be("CPHH must be 11 digits.");
    }

    [Fact]
    public async Task Add_OnPost_SecondPreviousOwner_IsRejected()
    {
        Existing(new OtherOwnerRecord(1, Rbse, "PRV", "Old", null, null));
        var model = CreateAdd();
        model.OwnerType = "PRV";
        model.OwnerName = "Another";

        await model.OnPostAsync();

        model.ModelState["OwnerType"]!.Errors[0].ErrorMessage.Should().Contain("only have one owner of type Previous");
    }

    [Fact]
    public async Task Add_OnPost_Valid_AddsNormalizedOwner()
    {
        var model = CreateAdd();
        model.OwnerType = "CUR";
        model.OwnerName = "  ";
        model.OwnerCphh = Cphh;

        var result = await model.OnPostAsync();

        await _owners.Received(1).AddAsync(
            Arg.Is<AddOtherOwnerCommand>(c => c.Rbse == Rbse && c.Type == "CUR" && c.Name == null && c.Cphh == RawCphh),
            _connection, _transaction);
        _transaction.Received(1).Commit();
        model.TempData["Success"].Should().Be("Owner record added.");
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Vla");
    }

    [Fact]
    public async Task Edit_OnGet_UnknownOwner_RedirectsToVla()
    {
        Existing(new OtherOwnerRecord(8, Rbse, "CUR", "X", null, null));
        var model = CreateEdit();

        var result = await model.OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.RouteValues!["rbse"].Should().Be(Rbse);
    }

    [Fact]
    public async Task Edit_OnGet_PopulatesFields()
    {
        Existing(new OtherOwnerRecord(3, Rbse, "CUR", "Bob", RawCphh, [4, 5]));
        var model = CreateEdit();

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Rbse.Should().Be(Rbse);
        model.OwnerType.Should().Be("CUR");
        model.OwnerName.Should().Be("Bob");
        model.OwnerCphh.Should().Be(RawCphh);
        model.RowStampBase64.Should().Be(Convert.ToBase64String(new byte[] { 4, 5 }));
    }

    [Fact]
    public async Task Edit_OnPost_NothingEntered_ReturnsPageWithErrors()
    {
        var model = CreateEdit();

        (await model.OnPostAsync()).Should().BeOfType<PageResult>();

        model.ModelState.Keys.Should().BeEquivalentTo("OwnerType", "OwnerName");
    }

    [Fact]
    public async Task Edit_OnPost_BadCphhLength_IsRejected()
    {
        var model = CreateEdit();
        model.OwnerType = "CUR";
        model.OwnerCphh = "123";

        await model.OnPostAsync();

        model.ModelState.ContainsKey("OwnerCphh").Should().BeTrue();
    }

    [Fact]
    public async Task Edit_OnPost_PreviousOwnerOnAnotherRow_IsRejected()
    {
        Existing(new OtherOwnerRecord(1, Rbse, "PRV", "Old", null, null));
        var model = CreateEdit();
        model.OwnerType = "PRV";
        model.OwnerName = "N";

        await model.OnPostAsync();

        model.ModelState.ContainsKey("OwnerType").Should().BeTrue();
    }

    [Fact]
    public async Task Edit_OnPost_PreviousOwnerSameRow_IsAllowed()
    {
        Existing(new OtherOwnerRecord(3, Rbse, "PRV", "Old", null, null));
        var model = CreateEdit();
        model.OwnerType = "PRV";
        model.OwnerName = "N";

        (await model.OnPostAsync()).Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task Edit_OnPost_Valid_EditsAndRedirects()
    {
        var model = CreateEdit();
        model.OwnerType = "CUR";
        model.OwnerName = "Bob";
        model.OwnerCphh = Cphh;
        model.RowStampBase64 = Convert.ToBase64String(new byte[] { 9 });

        var result = await model.OnPostAsync();

        await _owners.Received(1).EditAsync(
            Arg.Is<EditOtherOwnerCommand>(c => c.Id == 3 && c.Type == "CUR" && c.Name == "Bob" && c.Cphh == RawCphh
                && c.RowStamp.SequenceEqual(new byte[] { 9 })),
            _connection, _transaction);
        _transaction.Received(1).Commit();
        model.TempData["Success"].Should().Be("Owner record updated.");
        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Case/Vla");
    }

    [Fact]
    public async Task Edit_OnPost_BlankRowStampAndCphh_UsesNulls()
    {
        var model = CreateEdit();
        model.OwnerType = "CUR";
        model.OwnerName = "Bob";

        await model.OnPostAsync();

        await _owners.Received(1).EditAsync(
            Arg.Is<EditOtherOwnerCommand>(c => c.Cphh == null && c.RowStamp.Length == 0), _connection, _transaction);
    }
}
