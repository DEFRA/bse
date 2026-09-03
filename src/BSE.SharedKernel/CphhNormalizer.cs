namespace BSE.SharedKernel;

public static class CphhNormalizer
{
    public static string Normalize(string? cphh)
    {
        if (string.IsNullOrWhiteSpace(cphh))
        {
            return string.Empty;
        }

        return cphh
            .Replace("/", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
    }
}
