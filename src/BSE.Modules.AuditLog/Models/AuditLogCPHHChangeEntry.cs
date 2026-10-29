namespace BSE.Modules.AuditLog.Models;

/// <summary>
/// Audit log entry returned by GetAuditLogCPHHChanges.
/// Extends <see cref="AuditLogEntry"/> with the number of cases currently linked to the CPHH.
/// </summary>
public record AuditLogCphhChangeEntry : AuditLogEntry
{
    public int CaseCount { get; init; }
}
