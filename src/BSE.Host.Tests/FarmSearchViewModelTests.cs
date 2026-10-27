using BSE.Host.Models.ViewModels;
using BSE.Modules.Search.Models;

namespace BSE.Host.Tests;

public class FarmSearchViewModelTests
{
    private static FarmSearchResult Farm(string cphh, string owner = "Owner", int cases = 0) =>
        new(cphh, owner, null, null, null, null, null, null, null, null, cases, 0);

    private static FarmSearchViewModel ViewModelWith(int count)
    {
        var results = Enumerable.Range(1, count)
            .Select(i => Farm($"CPHH{i:000}"))
            .ToList();
        return new FarmSearchViewModel { Results = results };
    }

    [Fact]
    public void GetPagedResults_FirstPage_ReturnsFirstTen()
    {
        var vm = ViewModelWith(25);

        var page = vm.GetPagedResults();

        page.Should().HaveCount(10);
        page[0].Cphh.Should().Be("CPHH001");
        page[9].Cphh.Should().Be("CPHH010");
    }

    [Fact]
    public void GetPagedResults_LastPage_ReturnsRemainder()
    {
        var vm = ViewModelWith(25);
        vm.PageNumber = 3;

        var page = vm.GetPagedResults();

        page.Should().HaveCount(5);
        page[0].Cphh.Should().Be("CPHH021");
    }

    [Fact]
    public void GetPagedResults_PageBeyondResults_ReturnsEmpty()
    {
        var vm = ViewModelWith(5);
        vm.PageNumber = 4;

        vm.GetPagedResults().Should().BeEmpty();
    }

    [Fact]
    public void GetSortedResults_ReturnsEveryRowUnpaged()
    {
        var vm = ViewModelWith(25);

        vm.GetSortedResults().Should().HaveCount(25);
    }

    [Fact]
    public void GetSortedResults_DefaultsToCphhAscending()
    {
        var vm = new FarmSearchViewModel { Results = [Farm("C"), Farm("A"), Farm("B")] };

        vm.GetSortedResults().Select(r => r.Cphh).Should().Equal("A", "B", "C");
    }

    [Theory]
    [InlineData("ownername", false, new[] { "A", "B", "C" })]
    [InlineData("ownername", true, new[] { "C", "B", "A" })]
    [InlineData("OwnerName", false, new[] { "A", "B", "C" })]
    public void GetSortedResults_SortsByRequestedColumnAndDirection(string column, bool desc, string[] expectedOwners)
    {
        var vm = new FarmSearchViewModel
        {
            SortColumn = column,
            SortDesc = desc,
            Results = [Farm("1", "B"), Farm("2", "C"), Farm("3", "A")]
        };

        vm.GetSortedResults().Select(r => r.OwnerName).Should().Equal(expectedOwners);
    }

    [Fact]
    public void GetSortedResults_SortsNumericColumns()
    {
        var vm = new FarmSearchViewModel
        {
            SortColumn = "casescount",
            SortDesc = true,
            Results = [Farm("1", cases: 2), Farm("2", cases: 9), Farm("3", cases: 5)]
        };

        vm.GetSortedResults().Select(r => r.CasesCount).Should().Equal(9, 5, 2);
    }

    [Fact]
    public void GetPagedResults_AppliesSortingBeforePaging()
    {
        var results = Enumerable.Range(1, 12).Select(i => Farm($"CPHH{i:000}")).ToList();
        var vm = new FarmSearchViewModel { SortColumn = "cphh", SortDesc = true, Results = results };

        var page = vm.GetPagedResults();

        page[0].Cphh.Should().Be("CPHH012");
        page.Should().HaveCount(10);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 1)]
    [InlineData(11, 2)]
    [InlineData(25, 3)]
    public void TotalPages_RoundsUpAndNeverDropsBelowOne(int count, int expectedPages)
    {
        var vm = ViewModelWith(count);

        vm.TotalPages.Should().Be(expectedPages);
        vm.TotalCount.Should().Be(count);
    }
}
