namespace BSE.SharedKernel;

public static class CphhNormalizer
{
    // Legacy CPHH.ascx RemoveAlphas kept digits only, so any other punctuation is dropped too.
    public static string Normalize(string? cphh)
    {
        if (string.IsNullOrWhiteSpace(cphh))
        {
            return string.Empty;
        }

        return string.Concat(cphh.Where(char.IsDigit));
    }
}
