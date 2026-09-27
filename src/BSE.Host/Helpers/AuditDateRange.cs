using BSE.Host.Models.ViewModels;

namespace BSE.Host.Helpers;

/// <summary>Mirrors the legacy Common.vb IsDateRangeValid checks used by the audit log pages.</summary>
internal static class AuditDateRange
{
    public const string MissingDateMessage = "Please enter a valid date";
    public const string StartAfterEndMessage = "Must be earlier than the specified latest log entry date";
    public const string EndBeforeStartMessage = "Must be later than the specified earliest log entry date";

    public static bool Validate(DateTime? startDate, DateTime? endDate, out string? startError, out string? endError)
    {
        startError = startDate is null ? MissingDateMessage : null;
        endError = endDate is null ? MissingDateMessage : null;

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
