using BSE.Host.ModelBinding;
using BSE.Host.Pages;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace BSE.Modules.UserManagement.Tests;

public sealed class LenientBooleanModelBinderTests
{
    private static async Task<ModelBindingContext> BindAsync(string? raw, bool nullable)
    {
        var values = new Dictionary<string, StringValues>();
        if (raw is not null) values["Flag"] = new StringValues(raw.Split(','));
        var provider = new QueryStringValueProvider(BindingSource.Query, new Microsoft.AspNetCore.Http.QueryCollection(values), System.Globalization.CultureInfo.InvariantCulture);
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(nullable ? typeof(bool?) : typeof(bool));
        var context = new DefaultModelBindingContext
        {
            ModelName = "Flag",
            ModelMetadata = metadata,
            ValueProvider = provider,
            ModelState = new ModelStateDictionary()
        };
        await new LenientBooleanModelBinder(nullable).BindModelAsync(context);
        return context;
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("true,false", true)]
    [InlineData("false", false)]
    public async Task ValidValues_AreParsedAndRecorded(string raw, bool expected)
    {
        var ctx = await BindAsync(raw, nullable: false);

        ctx.Result.Model.Should().Be(expected);
        ctx.ModelState.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task MalformedValue_IsIgnoredWithoutModelStateError()
    {
        var ctx = await BindAsync("notabool", nullable: false);

        ctx.Result.IsModelSet.Should().BeTrue();
        ctx.Result.Model.Should().Be(false);
        ctx.ModelState.ErrorCount.Should().Be(0);
        ctx.ModelState.ContainsKey("Flag").Should().BeFalse();
    }

    [Fact]
    public async Task MalformedValue_ForNullableBool_BindsNull()
    {
        var ctx = await BindAsync("notabool", nullable: true);

        ctx.Result.IsModelSet.Should().BeTrue();
        ctx.Result.Model.Should().BeNull();
    }

    [Fact]
    public async Task MissingValue_LeavesModelUnset()
    {
        var ctx = await BindAsync(null, nullable: false);

        ctx.Result.IsModelSet.Should().BeFalse();
    }

    [Fact]
    public void ErrorModel_AnswersEveryHttpVerb()
    {
        var model = new ErrorModel
        {
            PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() }
        };

        var act = () => { model.OnPost(); model.OnPut(); model.OnPatch(); model.OnDelete(); };

        act.Should().NotThrow();
    }
}
