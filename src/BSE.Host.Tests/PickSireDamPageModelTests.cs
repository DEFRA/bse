using System.Text.Json;
using BSE.Host.Pages.Case;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Tests;

public sealed class PickSireDamPageModelTests
{
    private readonly IAnimalRelationsRepository _repo = Substitute.For<IAnimalRelationsRepository>();

    private PickSireDamModel CreateModel(string rbse = "002600001", string sex = "F")
    {
        var model = new PickSireDamModel(_repo) { Rbse = rbse, Sex = sex };
        PageTestContext.Attach(model);
        return model;
    }

    private static List<DamSireDetailRecord> Matches(int count) =>
        Enumerable.Range(1, count).Select(i => new DamSireDetailRecord
        {
            Id = i, Rbse = $"00260{i:0000}", Eartag = $"E{i:00}", Name = $"N{count - i:00}", Herdbook = $"H{i:00}"
        }).ToList();

    private void Returns(List<DamSireDetailRecord> rows) =>
        _repo.GetDamSireDetailsMatchesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>())
            .Returns(rows);

    [Theory]
    [InlineData("", "F")]
    [InlineData("002600001", "")]
    public async Task OnGet_MissingRbseOrSex_RedirectsHome(string rbse, string sex)
    {
        var result = await CreateModel(rbse, sex).OnGetAsync();

        result.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Home");
    }

    [Fact]
    public async Task OnGet_RestoresContextFromTempData()
    {
        Returns(Matches(2));
        var model = CreateModel("", "M");
        model.TempData[PickSireDamContextKeys.Rbse] = "002600009";
        model.TempData[PickSireDamContextKeys.ReturnTo] = "Elsewhere";
        model.TempData[PickSireDamContextKeys.Eartag] = "UK1";
        model.TempData[PickSireDamContextKeys.Name] = "Bessie";
        model.TempData[PickSireDamContextKeys.Herdbook] = "HB";

        (await model.OnGetAsync()).Should().BeOfType<PageResult>();

        model.Rbse.Should().Be("002600009");
        model.ReturnTo.Should().Be("Elsewhere");
        model.IsDam.Should().BeFalse();
        await _repo.Received(1).GetDamSireDetailsMatchesAsync("UK1", "Bessie", null, "HB", "M");
    }

    [Fact]
    public async Task OnGet_DefaultReturnToAndBlankCriteria()
    {
        Returns([]);
        var model = CreateModel();
        model.UseBlankSearch = true;
        model.TempData[PickSireDamContextKeys.Eartag] = "IGNORED";

        await model.OnGetAsync();

        model.ReturnTo.Should().Be("Relations");
        await _repo.Received(1).GetDamSireDetailsMatchesAsync(null, null, null, null, "F");
        model.TotalPages.Should().Be(1);
        model.TotalMatchCount.Should().Be(0);
    }

    [Fact]
    public async Task OnGet_PagesAndClamps()
    {
        Returns(Matches(23));
        var model = CreateModel();
        model.Eartag = " ";
        model.Name = "x";
        model.PageNumber = 99;

        await model.OnGetAsync();

        model.TotalMatchCount.Should().Be(23);
        model.TotalPages.Should().Be(3);
        model.PageNumber.Should().Be(3);
        model.PagedMatches.Should().HaveCount(3);
        await _repo.Received(1).GetDamSireDetailsMatchesAsync(null, "x", null, null, "F");
    }

    [Theory]
    [InlineData("Eartag", false, "E01")]
    [InlineData("Eartag", true, "E03")]
    [InlineData("Herdbook", true, "E03")]
    [InlineData("Name", false, "E03")]
    [InlineData("Rbse", true, "E03")]
    [InlineData(null, false, "E03")]
    public async Task OnGet_Sorts(string? column, bool desc, string firstEartag)
    {
        Returns(Matches(3));
        var model = CreateModel();
        model.SortColumn = column;
        model.SortDesc = desc;

        await model.OnGetAsync();

        model.PagedMatches[0].Eartag.Should().Be(firstEartag);
    }

    [Fact]
    public void UseSelected_Dam_StoresPendingDamAndRedirects()
    {
        var model = CreateModel("002600001", "F");

        var result = model.OnPostUseSelected(5, " ", "E1", "N1", "H1", 1, 2, 2020, "AQI=", "Fate", "Res", 3);

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Case/Relations");
        redirect.RouteValues!["rbse"].Should().Be("002600001");
        redirect.RouteValues["sex"].Should().Be("F");
        var pending = JsonSerializer.Deserialize<PendingDamSire>((string)model.TempData[PendingDamSireKeys.Dam]!)!;
        pending.Id.Should().Be(5);
        pending.Rbse.Should().BeNull();
        pending.Eartag.Should().Be("E1");
        pending.ChildCount.Should().Be(3);
        model.TempData.ContainsKey(PendingDamSireKeys.Sire).Should().BeFalse();
    }

    [Fact]
    public void UseSelected_Sire_StoresPendingSire()
    {
        var model = CreateModel("002600001", "M");

        model.OnPostUseSelected(6, "002600002", null, null, null, null, null, null, "AA==", null, null, null);

        var pending = JsonSerializer.Deserialize<PendingDamSire>((string)model.TempData[PendingDamSireKeys.Sire]!)!;
        pending.Rbse.Should().Be("002600002");
    }

    [Fact]
    public void New_StoresUnlinkedEntryAndFlagsStartNew()
    {
        var model = CreateModel("002600001", "F");
        model.Eartag = "UK9";
        model.Name = "Daisy";
        model.Herdbook = "HB9";

        var result = model.OnPostNew();

        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.RouteValues!["startNew"].Should().Be(true);
        var pending = JsonSerializer.Deserialize<PendingDamSire>((string)model.TempData[PendingDamSireKeys.Dam]!)!;
        pending.Id.Should().Be(0);
        pending.Eartag.Should().Be("UK9");
        pending.Name.Should().Be("Daisy");
        pending.Herdbook.Should().Be("HB9");
    }

    [Fact]
    public void Exit_RedirectsBackToRelations()
    {
        var redirect = CreateModel("002600001", "M").OnPostExit().Should().BeOfType<RedirectToPageResult>().Subject;

        redirect.PageName.Should().Be("/Case/Relations");
        redirect.RouteValues!["sex"].Should().Be("M");
    }
}
