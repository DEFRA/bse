namespace BSE.SharedKernel.Tests;

/// <summary>
/// Covers the UTC-to-UK-local conversion added for the Audit Log "doesn't show system time"
/// defect: Azure SQL Database's GETDATE() always returns UTC (unlike on-prem SQL Server), so
/// AuditLog.LogDate values must be converted to UK local time, honouring BST, before display.
/// </summary>
public class UkTimeZoneHelperTests
{
    [Fact]
    public void ToUkTime_DuringBst_AddsOneHour()
    {
        // 23 Sep 2026 13:07:04 UTC falls within British Summer Time.
        var utc = new DateTime(2026, 9, 23, 13, 7, 4, DateTimeKind.Utc);

        var result = UkTimeZoneHelper.ToUkTime(utc);

        result.Should().Be(new DateTime(2026, 9, 23, 14, 7, 4));
    }

    [Fact]
    public void ToUkTime_DuringGmt_AppliesNoOffset()
    {
        // 15 Jan 2026 09:30:00 UTC falls outside BST (GMT = UTC+0).
        var utc = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc);

        var result = UkTimeZoneHelper.ToUkTime(utc);

        result.Should().Be(new DateTime(2026, 1, 15, 9, 30, 0));
    }

    [Fact]
    public void ToUkTime_UnspecifiedKind_IsTreatedAsUtc()
    {
        // Dapper maps SQL Server DATETIME columns back with Kind=Unspecified, never Utc —
        // the conversion must still apply the BST offset in that case.
        var unspecified = new DateTime(2026, 9, 23, 13, 7, 4, DateTimeKind.Unspecified);

        var result = UkTimeZoneHelper.ToUkTime(unspecified);

        result.Should().Be(new DateTime(2026, 9, 23, 14, 7, 4));
    }
}
