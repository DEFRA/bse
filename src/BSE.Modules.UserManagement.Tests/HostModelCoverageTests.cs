using BSE.Host.ModelBinding;
using BSE.Host.Models.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public class SamplingDateModelBinderTests
{
    private static async Task<DefaultModelBindingContext> BindAsync(string? value, bool provided = true)
    {
        var values = provided
            ? new Dictionary<string, StringValues> { ["SamplingDate"] = value }
            : new Dictionary<string, StringValues>();
        var context = new DefaultModelBindingContext
        {
            ModelName = "SamplingDate",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new QueryStringValueProvider(
                BindingSource.Query, new QueryCollection(values), CultureInfo.InvariantCulture)
        };

        await new SamplingDateModelBinder().BindModelAsync(context);
        return context;
    }

    [Fact]
    public async Task MissingValue_LeavesResultUnset()
    {
        var context = await BindAsync(null, provided: false);

        context.Result.IsModelSet.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankValue_BindsNull(string raw)
    {
        var context = await BindAsync(raw);

        context.Result.IsModelSet.Should().BeTrue();
        context.Result.Model.Should().BeNull();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData(" UNKNOWN ")]
    public async Task UnknownText_BindsMaxValue(string raw)
    {
        var context = await BindAsync(raw);

        context.Result.Model.Should().Be(DateTime.MaxValue);
    }

    [Theory]
    [InlineData("17/05/2024", 2024, 5, 17)]
    [InlineData("1/2/2023", 2023, 2, 1)]
    public async Task ValidDate_BindsParsedDate(string raw, int year, int month, int day)
    {
        var context = await BindAsync(raw);

        context.Result.Model.Should().Be(new DateTime(year, month, day));
    }

    [Fact]
    public async Task InvalidDate_AddsModelError()
    {
        var context = await BindAsync("not a date");

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState["SamplingDate"]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task NullContext_Throws()
    {
        var act = () => new SamplingDateModelBinder().BindModelAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

public class CaseSearchViewModelDateTests
{
    [Fact]
    public void BlankDates_AreValid()
    {
        new CaseSearchViewModel().ValidateDates().Should().BeTrue();
    }

    [Fact]
    public void InvalidDate_SetsFieldError()
    {
        var model = new CaseSearchViewModel { EarliestFormADate = "nonsense" };

        model.ValidateDates().Should().BeFalse();
        model.EarliestFormADateError.Should().Be("Enter a valid date");
    }

    [Fact]
    public void ValidOrderedRanges_AreAccepted()
    {
        var model = new CaseSearchViewModel
        {
            EarliestFormADate = "2024-01-01",
            LatestFormADate = "2024-02-01",
            EarliestFinalResultDate = "01/01/2024",
            LatestFinalResultDate = "02/01/2024",
            EarliestBirthDate = "2020-01-01",
            LatestBirthDate = "2021-01-01"
        };

        model.ValidateDates().Should().BeTrue();
    }

    [Fact]
    public void EarliestAfterLatest_IsRejectedForEachRange()
    {
        var model = new CaseSearchViewModel
        {
            EarliestFormADate = "2024-03-01",
            LatestFormADate = "2024-02-01",
            EarliestFinalResultDate = "2024-03-01",
            LatestFinalResultDate = "2024-02-01",
            EarliestBirthDate = "2024-03-01",
            LatestBirthDate = "2024-02-01"
        };

        model.ValidateDates().Should().BeFalse();
        model.EarliestFormADateError.Should().NotBeNullOrEmpty();
        model.LatestFinalResultDateError.Should().NotBeNullOrEmpty();
        model.EarliestBirthDateError.Should().NotBeNullOrEmpty();
    }
}
