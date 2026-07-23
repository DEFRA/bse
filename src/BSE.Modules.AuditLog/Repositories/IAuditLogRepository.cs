using BSE.Modules.AuditLog.Models;

namespace BSE.Modules.AuditLog.Repositories;

/// <summary>
/// Data access contract for the AuditLog module. One method per stored procedure.
/// SP names match filenames in src/BSE.Database/StoredProcedures/AuditLog/ exactly.
/// No write operations — the AuditLog table is written exclusively by stored procedures.
/// </summary>
public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLogEntry>> GetByCaseAsync(string rbse);
    Task<IEnumerable<AuditLogEntry>> GetByDateAsync(DateTime logDate);
    Task<IEnumerable<AuditLogEntry>> GetByFarmAsync(string cphh);
    Task<IEnumerable<AuditLogEntry>> GetByUserAsync(DateTime startDate, DateTime endDate, int userId);
    Task<IEnumerable<AuditLogCaseMoveEntry>> GetCaseMovesAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<AuditLogCPHHChangeEntry>> GetCphhChangesAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<AuditLogNewFarmEntry>> GetNewFarmsAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<AuditLogRBSEChangeEntry>> GetRbseChangesAsync(DateTime startDate, DateTime endDate);
}
