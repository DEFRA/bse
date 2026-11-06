using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace BSE.Host.ModelBinding;

/// <summary>Binds bool/bool? leniently: an unparseable value (e.g. <c>?IncludeNonGb=notabool</c>) is ignored
/// instead of leaving an invalid attempted value that makes the checkbox tag helper throw while rendering.</summary>
public sealed class LenientBooleanModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
        => context.Metadata.UnderlyingOrModelType == typeof(bool) ? new LenientBooleanModelBinder(context.Metadata.IsNullableValueType) : null;
}

public sealed class LenientBooleanModelBinder(bool nullable) : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (result == ValueProviderResult.None)
            return Task.CompletedTask;

        // A checkbox posts "true,false" when ticked; the first value wins.
        if (bool.TryParse(result.FirstValue?.Trim(), out var value))
        {
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);
            bindingContext.Result = ModelBindingResult.Success(value);
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Success(nullable ? null : false);
        }

        return Task.CompletedTask;
    }
}
