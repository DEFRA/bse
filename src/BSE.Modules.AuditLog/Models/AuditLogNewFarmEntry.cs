namespace BSE.Modules.AuditLog.Models;

/// <summary>
/// Audit log entry returned by GetAuditLogNewFarms.
/// Extends <see cref="AuditLogEntry"/> with farm owner and address details.
/// </summary>
public record AuditLogNewFarmEntry : AuditLogEntry
{
    public string? OwnerName { get; init; }
    public string? Address { get; init; }
    public string? County { get; init; }
}
