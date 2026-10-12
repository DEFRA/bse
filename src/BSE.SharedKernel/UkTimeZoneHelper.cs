namespace BSE.SharedKernel;

/// <summary>
/// Converts UTC-sourced timestamps to UK local (DST-aware) time for display.
/// Azure SQL Database's GETDATE() always returns UTC (unlike on-prem SQL Server, which follows
/// the server OS's local timezone) — every column defaulted from GETDATE() is therefore a UTC
/// value and must be converted explicitly before being shown to a UK user as "system time".
/// </summary>
public static class UkTimeZoneHelper
{
    public static readonly TimeZoneInfo UkTimeZone = ResolveUkTimeZone();

    private static TimeZoneInfo ResolveUkTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); } // Windows ID
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); // Linux/IANA ID
        }
    }

    /// <summary>
    /// Converts a UTC (or Dapper-mapped Unspecified-kind, which SQL Server always means UTC here)
    /// timestamp to UK local time, automatically applying the BST offset when in effect.
    /// </summary>
    public static DateTime ToUkTime(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, UkTimeZone);
}
