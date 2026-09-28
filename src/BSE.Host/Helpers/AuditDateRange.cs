using BSE.Host.Models.ViewModels;

namespace BSE.Host.Helpers;

/// <summary>Mirrors the legacy Common.vb IsDateRangeValid checks used by the audit log pages.</summary>
internal static class AuditDateRange
{
    public const string MissingDateMessage = "Enter a date";
    public const string MissingStartDateMessage = "Enter a start date";
    public const string MissingEndDateMessage = "Enter an end date";
    public const string StartAfterEndMessage = "The start date must be on or before the end date";
    public const string EndBeforeStartMessage = "The end date must be on or after the start date";

    public static bool Validate(DateTime? startDate, DateTime? endDate, out string? startError, out string? endError)
    {
        startError = startDate is null ? MissingStartDateMessage : null;
        endError = endDate is null ? MissingEndDateMessage : null;

        if (startDate is not null && endDate is not null && startDate > endDate)
        {
            startError = StartAfterEndMessage;
            endError = EndBeforeStartMessage;
        }

        return startError is null && endError is null;
    }

    public static AuditDateRangeViewModel ToViewModel(DateTime? startDate, DateTime? endDate, string? startError, string? endError)
        => new(startDate, endDate, startError, endError);
}
