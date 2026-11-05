using BSE.Modules.AuditLog.Models;

namespace BSE.Host.Models.ViewModels;

public record AuditReportHeading(string Heading, string Breadcrumb);

public record AuditNoResults(string TitleId, string Message);

/// <summary>An extra audit-table column beyond the eight shared by every <see cref="AuditLogEntry"/>.</summary>
public record AuditExtraColumn(
    string Label,
    string Column,
    Func<AuditLogEntry, object?> Value,
    string? LabelHtml = null,
    string? HeaderClass = null,
    string? CellClass = null)
{
    public static AuditExtraColumn Of<T>(string label, string column, Func<T, object?> value,
        string? labelHtml = null, string? headerClass = null, string? cellClass = null) where T : AuditLogEntry =>
        new(label, column, e => value((T)e), labelHtml, headerClass, cellClass);
}

public record AuditResultsViewModel(
    IReadOnlyList<AuditLogEntry> Rows,
    string SortColumn,
    bool SortDesc,
    int PageNumber,
    int TotalPages,
    string Caption,
    string? ResultsId,
    IReadOnlyList<AuditExtraColumn>? ExtraColumns = null);
