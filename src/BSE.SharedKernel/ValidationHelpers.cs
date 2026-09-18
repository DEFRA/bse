using System.Net.Mail;

namespace BSE.SharedKernel;

/// <summary>
/// Shared input validation helpers used across Razor Page models.
/// </summary>
public static class ValidationHelpers
{
    /// <summary>
    /// Validates that <paramref name="email"/> is a well-formed email address
    /// (e.g. "name@example.com"), without performing any network/DNS lookups.
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var parsed = new MailAddress(email);
            return parsed.Address.Equals(email, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
