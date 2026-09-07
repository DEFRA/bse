namespace BSE.Host.Models.ViewModels;

public record AuditDateRangeViewModel(
    DateTime? StartDate,
    DateTime? EndDate,
    string? StartDateError = null,
    string? EndDateError = null);
