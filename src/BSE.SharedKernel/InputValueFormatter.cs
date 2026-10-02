using System.Globalization;

namespace BSE.SharedKernel;

public static class InputValueFormatter
{
    public static string ToInputValue(int? value) => value is null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture);

    public static string ToInputValue(int value) => value.ToString(CultureInfo.InvariantCulture);
}
