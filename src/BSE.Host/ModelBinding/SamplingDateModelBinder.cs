using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BSE.Host.ModelBinding;

/// <summary>
/// Binds the CaseWork Entry Sampling Date field, additionally accepting the literal text
/// "unknown" (case-insensitive) as <see cref="DateTime.MaxValue"/> — the legacy sentinel for
/// "sampling date not known" — matching legacy's <c>CalendarDate.AllowUnknown</c> behaviour
/// where the same textbox accepted either a date or the word "unknown".
/// </summary>
public sealed class SamplingDateModelBinder : IModelBinder
{
    private const string UnknownText = "unknown";
    private static readonly string[] Formats = ["d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy"];

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        var raw = valueProviderResult.FirstValue?.Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (string.Equals(raw, UnknownText, StringComparison.OrdinalIgnoreCase))
        {
            bindingContext.Result = ModelBindingResult.Success(DateTime.MaxValue);
            return Task.CompletedTask;
        }

        if (DateTime.TryParseExact(raw, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                "Enter a sampling date in the format 17/05/2024, or type \"unknown\".");
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}
