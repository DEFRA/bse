using BSE.Host.Helpers;
using BSE.Host.Models.ViewModels;

namespace BSE.Host.Tests;

public class FeedValidationTests
{
    private static FeedValidation.Input Valid() => new(YearFrom: 2020, YearTo: 2022, RationType: "Cattle", SupplierId: 5);

    [Fact]
    public void Validate_ValidInput_HasNoErrors()
    {
        FeedValidation.Validate(Valid(), new DateTime(2019, 6, 1)).Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyInput_ReportsEveryRequiredField()
    {
        var errors = FeedValidation.Validate(new FeedValidation.Input(null, null, " ", null), null);

        errors.Should().Contain(new KeyValuePair<string, string>("YearFrom", FeedValidation.YearFromEmpty));
        errors.Should().Contain(new KeyValuePair<string, string>("YearTo", FeedValidation.YearToEmpty));
        errors.Should().Contain(new KeyValuePair<string, string>("RationType", FeedValidation.RationTypeEmpty));
        errors.Should().Contain(new KeyValuePair<string, string>("SupplierId", FeedValidation.SupplierEmpty));
    }

    [Fact]
    public void Validate_YearFromBeforeBirthYear_IsRejected()
    {
        var errors = FeedValidation.Validate(Valid() with { YearFrom = 2018 }, new DateTime(2019, 6, 1));

        errors["YearFrom"].Should().Be(FeedValidation.YearFromBeforeBirth);
    }

    [Fact]
    public void Validate_YearToBeforeYearFrom_IsRejected()
    {
        var errors = FeedValidation.Validate(Valid() with { YearTo = 2019 }, null);

        errors["YearTo"].Should().Be(FeedValidation.YearToBeforeYearFrom);
    }
}

public class RelationValidationTests
{
    private static RelationValidation.Input Input(
        string? relationRbse = null, string? relationType = "Dam", string? sex = "F",
        int? day = null, int? month = null, int? year = null, DateTime? leftDate = null) =>
        new("002600001", relationRbse, relationType, sex, null, null, null, day, month, year, leftDate);

    private static Dictionary<string, string> Validate(
        RelationValidation.Input input, string?[]? existing = null, string? dam = null, string? sire = null) =>
        RelationValidation.Validate(input, existing ?? [], dam, sire);

    [Fact]
    public void Validate_ValidInput_HasNoErrors()
    {
        Validate(Input()).Should().BeEmpty();
    }

    [Fact]
    public void Validate_MissingTypeAndSexWithoutRbse_ReportsBoth()
    {
        var errors = Validate(Input(relationType: "", sex: null));

        errors["RelationType"].Should().Be(RelationValidation.RelationTypeRequired);
        errors["Sex"].Should().Be(RelationValidation.SexRequired);
    }

    [Fact]
    public void Validate_SexNotRequiredOnceRbseSupplied()
    {
        Validate(Input(relationRbse: "002500002", sex: null)).Should().NotContainKey("Sex");
    }

    [Theory]
    [InlineData("002600001", RelationValidation.SameAsCaseRbse)]
    [InlineData("00/26/00001", RelationValidation.SameAsCaseRbse)]
    public void Validate_RbseSameAsCase_IsRejected(string rbse, string expected)
    {
        Validate(Input(relationRbse: rbse))["RelationRbse"].Should().Be(expected);
    }

    [Fact]
    public void Validate_RbseSameAsDam_IsRejected()
    {
        Validate(Input(relationRbse: "002500002"), dam: "002500002")["RelationRbse"]
            .Should().Be(RelationValidation.SameAsDamRbse);
    }

    [Fact]
    public void Validate_RbseSameAsSire_IsRejected()
    {
        Validate(Input(relationRbse: "002500003"), sire: "00/25/00003")["RelationRbse"]
            .Should().Be(RelationValidation.SameAsSireRbse);
    }

    [Fact]
    public void Validate_RbseAlreadyARelation_IsRejected()
    {
        Validate(Input(relationRbse: "002500004"), existing: ["002500004"])["RelationRbse"]
            .Should().Be(RelationValidation.AlreadyARelation);
    }

    [Fact]
    public void Validate_ShortRbseIsPaddedBeforeComparison()
    {
        Validate(Input(relationRbse: "26/1"))["RelationRbse"].Should().Be(RelationValidation.SameAsCaseRbse);
    }

    [Theory]
    [InlineData(0, 1, "BirthDay")]
    [InlineData(32, 1, "BirthDay")]
    [InlineData(1, 0, "BirthMonth")]
    [InlineData(1, 13, "BirthMonth")]
    public void Validate_BirthDayOrMonthOutOfRange_IsRejected(int day, int month, string expectedKey)
    {
        Validate(Input(day: day, month: month)).Should().ContainKey(expectedKey);
    }

    [Fact]
    public void Validate_LeftDateInFuture_IsRejected()
    {
        Validate(Input(leftDate: DateTime.Today.AddDays(2)))["LeftDate"].Should().Be(RelationValidation.LeftDateFuture);
    }

    [Theory]
    [InlineData(1969)]
    [InlineData(2999)]
    public void Validate_BirthYearOutsideRange_IsRejected(int year)
    {
        Validate(Input(year: year)).Should().ContainKey("BirthYear");
    }

    [Fact]
    public void Validate_BirthYearAfterLeftDate_IsRejected()
    {
        var left = new DateTime(2020, 1, 1);

        Validate(Input(year: 2021, leftDate: left)).Should().ContainKey("BirthYear");
        Validate(Input(year: 2019, leftDate: left)).Should().NotContainKey("BirthYear");
    }
}

public class SearchDateFieldTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_Blank_IsValidAndNull(string? raw)
    {
        SearchDateField.TryParse(raw, out var value, out var error).Should().BeTrue();
        value.Should().BeNull();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("2024-03-05", 2024, 3, 5)]
    [InlineData("5/3/2024", 2024, 3, 5)]
    [InlineData("05/03/2024", 2024, 3, 5)]
    [InlineData("5/03/2024", 2024, 3, 5)]
    [InlineData("05/3/2024", 2024, 3, 5)]
    [InlineData(" 2024-03-05 ", 2024, 3, 5)]
    public void TryParse_SupportedFormats_ParseToDate(string raw, int year, int month, int day)
    {
        SearchDateField.TryParse(raw, out var value, out var error).Should().BeTrue();
        value.Should().Be(new DateTime(year, month, day));
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("not a date")]
    [InlineData("31/02/2024")]
    [InlineData("1899-12-31")]
    public void TryParse_InvalidOrPre1900_ReturnsError(string raw)
    {
        SearchDateField.TryParse(raw, out var value, out var error).Should().BeFalse();
        value.Should().BeNull();
        error.Should().Be(SearchDateField.InvalidDateMessage);
    }
}

public class AuditDateRangeTests
{
    [Fact]
    public void Validate_ValidRange_ReturnsTrue()
    {
        AuditDateRange.Validate(new DateTime(2024, 1, 1), new DateTime(2024, 1, 2), out var s, out var e)
            .Should().BeTrue();
        s.Should().BeNull();
        e.Should().BeNull();
    }

    [Fact]
    public void Validate_SameDay_IsValid()
    {
        var day = new DateTime(2024, 1, 1);

        AuditDateRange.Validate(day, day, out _, out _).Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingDates_ReportBothMessages()
    {
        AuditDateRange.Validate(null, null, out var s, out var e).Should().BeFalse();
        s.Should().Be(AuditDateRange.MissingStartDateMessage);
        e.Should().Be(AuditDateRange.MissingEndDateMessage);
    }

    [Fact]
    public void Validate_StartAfterEnd_ReportsOrderingMessages()
    {
        AuditDateRange.Validate(new DateTime(2024, 2, 1), new DateTime(2024, 1, 1), out var s, out var e)
            .Should().BeFalse();
        s.Should().Be(AuditDateRange.StartAfterEndMessage);
        e.Should().Be(AuditDateRange.EndBeforeStartMessage);
    }

    [Fact]
    public void ToViewModel_CarriesAllValues()
    {
        var vm = AuditDateRange.ToViewModel(new DateTime(2024, 1, 1), null, null, "err");

        vm.Should().BeOfType<AuditDateRangeViewModel>();
    }
}
