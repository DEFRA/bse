using Microsoft.AspNetCore.Http;

namespace BSE.Host.Helpers;

/// <summary>
/// Builds the base query string for sortable-table headers, preserving all existing
/// query parameters except the ones that control sorting and paging.
/// </summary>
public static class SortUrlHelper
{
    private static readonly string[] DefaultExcludedKeys = ["SortColumn", "SortDesc", "PageNumber"];

    public static string BuildBaseUrl(IQueryCollection query, params string[] excludedKeys)
    {
        var excluded = excludedKeys.Length > 0 ? excludedKeys : DefaultExcludedKeys;

        var parts = query
            .Where(k => !excluded.Any(e => string.Equals(k.Key, e, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(k => k.Value, (k, v) => $"{Uri.EscapeDataString(k.Key)}={Uri.EscapeDataString(v ?? "")}");

        return "?" + string.Join("&", parts);
    }
}
