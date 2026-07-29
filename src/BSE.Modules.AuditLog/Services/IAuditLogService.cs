using BSE.Modules.AuditLog.Models;

namespace BSE.Modules.AuditLog.Services;

/// <summary>
/// Read-only audit log query service. No write operations exist — the AuditLog table is
/// written exclusively by database stored procedures.
/// </summary>
public interface IAuditLogService
{
    /// <summary>Returns all audit log entries for a given RBSE case number.</summary>
    Task<IEnumerable<AuditLogEntry>> GetByCaseAsync(string rbse);

    /// <summary>Returns all audit log entries recorded on the specified date.</summary>
    Task<IEnumerable<AuditLogEntry>> GetByDateAsync(DateTime logDate);

    /// <summary>Returns all audit log entries for a given CPHH farm number.</summary>
    Task<IEnumerable<AuditLogEntry>> GetByFarmAsync(string cphh);

    /// <summary>Returns audit log entries for a specific user over a date range.</summary>
    Task<IEnumerable<AuditLogEntry>> GetByUserAsync(DateTime startDate, DateTime endDate, int userId);

    /// <summary>Returns case-move audit entries over a date range.</summary>
    Task<IEnumerable<AuditLogEntry>> GetCaseMovesAsync(DateTime startDate, DateTime endDate);

    /// <summary>Returns CPHH-change audit entries over a date range.</summary>
    Task<IEnumerable<AuditLogEntry>> GetCphhChangesAsync(DateTime startDate, DateTime endDate);

    /// <summary>Returns new-farm creation audit entries over a date range.</summary>
    Task<IEnumerable<AuditLogEntry>> GetNewFarmsAsync(DateTime startDate, DateTime endDate);

    /// <summary>Returns RBSE-change audit entries over a date range.</summary>
    Task<IEnumerable<AuditLogEntry>> GetRbseChangesAsync(DateTime startDate, DateTime endDate);
}
