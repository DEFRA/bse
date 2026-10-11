namespace BSE.SharedKernel;

/// <summary>
/// UK wall-clock time, correctly observing British Summer Time. <see cref="DateTime.Now"/>/
/// <see cref="DateTime.Today"/> follow the host OS's local timezone, which defaults to UTC on
/// typical Linux/container deployments (unlike legacy's on-prem Windows server, which ran in UK
/// local time) — anything that needs "now" in UK wall-clock terms (e.g. a report's printed
/// generation time, or a date stamp in an export filename) must convert explicitly instead of
/// relying on host configuration.
/// </summary>
public static class UkClock
{
    private static readonly TimeZoneInfo UkTimeZone = ResolveUkTimeZone();

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UkTimeZone);
    public static DateTime Today => Now.Date;

    private static TimeZoneInfo ResolveUkTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); } // Windows ID
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); // Linux/IANA ID
        }
    }
}
