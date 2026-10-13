using System.Globalization;
using BSE.Host.Models.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

/// <summary>
/// The Case (APHA) tab round-trips the DEFRA-owned Form A date as a hidden dd/MM/yyyy value; it
/// must bind independently of server culture (Azure runs a non-en-GB culture).
/// </summary>
public sealed class VlaEditViewModelBindingTests
{
    private static async Task<(VlaEditViewModel Model, ModelStateDictionary State)> BindAsync(string formADate)
    {
        using var services = new ServiceCollection().AddLogging().AddMvcCore().Services.BuildServiceProvider();
        var provider = services.GetRequiredService<IModelMetadataProvider>();
        var factory = services.GetRequiredService<IModelBinderFactory>();

        var metadata = provider.GetMetadataForType(typeof(VlaEditViewModel));
        var binder = factory.CreateBinder(new ModelBinderFactoryContext { Metadata = metadata, CacheToken = metadata });

        var form = new FormCollection(new Dictionary<string, StringValues> { ["Case.FormADate"] = formADate });
        var context = DefaultModelBindingContext.CreateBindingContext(
            new ActionContext(new DefaultHttpContext { RequestServices = services }, new RouteData(), new ActionDescriptor()),
            new FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture),
            metadata,
            new BindingInfo(),
            "Case");

        await binder.BindModelAsync(context);
        return ((VlaEditViewModel)context.Result.Model!, context.ModelState);
    }

    [Fact]
    public async Task FormADate_DdMmYyyy_BindsUnderInvariantCulture()
    {
        var (model, state) = await BindAsync("26/04/2024");

        state["Case.FormADate"]?.Errors.Should().BeNullOrEmpty();
        model.FormADate.Should().Be(new DateTime(2024, 4, 26));
    }
}
