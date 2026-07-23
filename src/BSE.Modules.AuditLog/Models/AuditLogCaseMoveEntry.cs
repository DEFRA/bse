namespace BSE.Modules.AuditLog.Models;

/// <summary>
/// Audit log entry returned by GetAuditLogCaseMoves.
/// Extends <see cref="AuditLogEntry"/> with a batch indicator.
/// HasBatches is 'Y' or 'N' (CASE WHEN BatchCount > 0).
/// </summary>
public record AuditLogCaseMoveEntry : AuditLogEntry
{
    public string HasBatches { get; init; } = "N";
}
