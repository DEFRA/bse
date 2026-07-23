namespace BSE.Modules.AuditLog.Models;

/// <summary>
/// Common audit log entry. Maps the SELECT columns shared by all GetAuditLog* stored procedures.
/// Column [DateTime] maps to the [LogDate] AS [DateTime] alias in SQL.
/// </summary>
public record AuditLogEntry
{
    public int Id { get; init; }
    public string TableName { get; init; } = "";
    public string FieldName { get; init; } = "";
    public DateTime DateTime { get; init; }
    public string UserName { get; init; } = "";
    public string? BeforeValue { get; init; }
    public string? AfterValue { get; init; }
    public string? Reason { get; init; }
    public string? Key { get; init; }
}
