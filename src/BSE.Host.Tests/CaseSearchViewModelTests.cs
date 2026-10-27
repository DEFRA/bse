using BSE.Host.Models;
using BSE.Host.Models.ViewModels;
using BSE.Modules.Search.Models;

namespace BSE.Host.Tests;

public class CaseSearchViewModelTests
{
    private static CaseSearchResult Case(string rbse, string cphh = "C", string? fate = null, DateTime? birth = null) =>
        new(rbse, cphh, null, null, null, birth, null, null, fate, null, null, null, null, null, null, null);

    [Fact]
    public void ValidateDates_AllBlank_IsValid()
    {
        var vm = new CaseSearchViewModel();

        vm.ValidateDates().Should().BeTrue();
        vm.EarliestFormADateError.Should().BeNull();
    }

    [Fact]
    public void ValidateDates_InvalidValueOnEachField_ReportsEveryError()
    {
        var vm = new CaseSearchViewModel
        {
            EarliestFormADate = "x", LatestFormADate = "x",
            EarliestFinalResultDate = "x", LatestFinalResultDate = "x",
            EarliestBirthDate = "x", LatestBirthDate = "x"
        };

        vm.ValidateDates().Should().BeFalse();
        new[]
        {
            vm.EarliestFormADateError, vm.LatestFormADateError,
            vm.EarliestFinalResultDateError, vm.LatestFinalResultDateError,
            vm.EarliestBirthDateError, vm.LatestBirthDateError
        }.Should().OnlyContain(e => e == "Enter a valid date");
    }

    [Fact]
    public void ValidateDates_EarliestAfterLatest_ReportsOrderingErrorsPerRange()
    {
        var vm = new CaseSearchViewModel
        {
            EarliestFormADate = "2024-02-01", LatestFormADate = "2024-01-01",
            EarliestFinalResultDate = "2024-02-01", LatestFinalResultDate = "2024-01-01",
            EarliestBirthDate = "2024-02-01", LatestBirthDate = "2024-01-01"
        };

        vm.ValidateDates().Should().BeFalse();
        vm.EarliestFormADateError.Should().Be("Must be earlier than the specified latest Form A Date");
        vm.LatestFormADateError.Should().Be("Must be later than the specified earliest Form A Date");
        vm.EarliestFinalResultDateError.Should().Contain("Final Result Date");
        vm.LatestBirthDateError.Should().Contain("Birth Date");
    }

    [Fact]
    public void ValidateDates_ValidRange_IsValid()
    {
        var vm = new CaseSearchViewModel { EarliestFormADate = "01/01/2024", LatestFormADate = "2024-01-31" };

        vm.ValidateDates().Should().BeTrue();
    }

    [Fact]
    public void ToQuery_MapsAndNormalisesInputs()
    {
        var vm = new CaseSearchViewModel
        {
            Rbse = "26/1",
            Eartag = "AB1",
            Dbse = "26/00001",
            Fate = "Slaughter",
            Sex = "F",
            IncludeNonGb = true,
            IsImportedCase = true,
            EarliestBirthDate = "2024-03-05",
            LatestBirthDate = "bad"
        };

        var query = vm.ToQuery();

        query.Rbse.Should().Be("002600001");
        query.Eartag.Should().Be("AB1");
        query.Dbse.Should().Be("2600001");
        query.Fate.Should().Be("Slaughter");
        query.Sex.Should().Be("F");
        query.IncludeNonGbCases.Should().BeTrue();
        query.IsImportedCase.Should().BeTrue();
        query.EarliestBirthDate.Should().Be(new DateTime(2024, 3, 5));
        query.LatestBirthDate.Should().BeNull();
    }

    [Fact]
    public void ToQuery_NullStringsBecomeEmpty()
    {
        var vm = new CaseSearchViewModel { Eartag = null!, Dbse = null!, Fate = null!, FinalResult = null!, Sex = null!, Survey = null!, Notes = null!, PassiveActive = null! };

        var query = vm.ToQuery();

        query.Eartag.Should().BeEmpty();
        query.Dbse.Should().BeEmpty();
        query.Notes.Should().BeEmpty();
        query.PassiveActive.Should().BeEmpty();
    }

    [Theory]
    [InlineData("rbse")]
    [InlineData("cphh")]
    [InlineData("sex")]
    [InlineData("survey")]
    [InlineData("eartag")]
    [InlineData("birthdate")]
    [InlineData("isbirthdateest")]
    [InlineData("origin")]
    [InlineData("formadate")]
    [InlineData("fate")]
    [InlineData("finalresult")]
    [InlineData("finalresultdate")]
    [InlineData("dbse")]
    [InlineData("valuationage")]
    [InlineData("notes")]
    [InlineData("babnotes")]
    [InlineData("unknown-column")]
    public void GetSortedResults_AnyColumnAscendingAndDescending_ReturnsAllRows(string column)
    {
        var results = new[] { Case("2"), Case("1"), Case("3") };

        foreach (var desc in new[] { false, true })
        {
            var vm = new CaseSearchViewModel { Results = results, SortColumn = column, SortDesc = desc };

            vm.GetSortedResults().Should().HaveCount(3);
        }
    }

    [Fact]
    public void GetSortedResults_ByRbse_OrdersBothDirections()
    {
        var results = new[] { Case("2"), Case("1"), Case("3") };

        new CaseSearchViewModel { Results = results, SortColumn = "rbse" }
            .GetSortedResults().Select(r => r.Rbse).Should().Equal("1", "2", "3");
        new CaseSearchViewModel { Results = results, SortColumn = "rbse", SortDesc = true }
            .GetSortedResults().Select(r => r.Rbse).Should().Equal("3", "2", "1");
    }
}

public class ViewModelRecordTests
{
    [Fact]
    public void CaseTabsViewModel_DefaultsAndOverrides()
    {
        var defaults = new CaseTabsViewModel("Farm", "002600001");
        defaults.BatchNumbers.Should().BeNull();
        defaults.CanEditCurrentTab.Should().BeFalse();

        var custom = defaults with { CanEditCurrentTab = true, SaveHandlerOverride = "CreateCase", CancelPageOverride = "/Home", CancelHandlerOverride = "Cancel", ViewDocsUrl = "http://x" };
        custom.SaveHandlerOverride.Should().Be("CreateCase");
        custom.ViewDocsUrl.Should().Be("http://x");
        custom.Should().NotBe(defaults);
    }

    [Fact]
    public void CaseEditNotificationsViewModel_CarriesMessagesAndErrors()
    {
        var vm = new CaseEditNotificationsViewModel("feeds", "ok", "warn", ["e1"]);

        vm.IdPrefix.Should().Be("feeds");
        vm.SuccessMessage.Should().Be("ok");
        vm.WarningMessage.Should().Be("warn");
        vm.ExtraFieldErrors.Should().ContainSingle("e1");
    }

    [Fact]
    public void CaseSaveCancelViewModel_CarriesValues()
    {
        var vm = new CaseSaveCancelViewModel("002600001", "/Case/Farm", "Cancel", "Save", MarginTop: true, NoMargin: false);

        vm.Rbse.Should().Be("002600001");
        vm.SaveHandler.Should().Be("Save");
        vm.MarginTop.Should().BeTrue();
        vm.NoMargin.Should().BeFalse();
    }
}
