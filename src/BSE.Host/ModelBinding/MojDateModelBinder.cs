using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BSE.Host.ModelBinding;

/// <summary>
/// Binds a MOJ date-picker free-text field (d/M/yyyy or dd/MM/yyyy) to <see cref="DateTime"/> /
/// <see cref="Nullable{DateTime}"/> properties, independent of server locale. Apply via
/// <c>[ModelBinder(BinderType = typeof(MojDateModelBinder))]</c> on the bound property.
/// </summary>
public sealed class MojDateModelBinder : IModelBinder
{
    public const string InvalidFormatMessage = "Enter a date in the format 17/05/2024.";

    // yyyy-MM-dd is also accepted so hidden pass-through fields on the same view model
    // (rendered with an explicit ISO value, not via the MOJ picker) keep round-tripping.
    private static readonly string[] Formats = ["d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "yyyy-MM-dd"];

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        var raw = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(raw))
        {
            // Blank is valid for an optional (nullable) date; required-ness is enforced separately.
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (DateTime.TryParseExact(raw.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, InvalidFormatMessage);
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}
