using BSE.Infrastructure;
using BSE.Modules.AuditLog.Models;

namespace BSE.Modules.AuditLog.Repositories;

/// <summary>
/// Dapper-backed repository for audit log stored procedure calls.
/// SP names match filenames in src/BSE.Database/StoredProcedures/AuditLog/ exactly.
/// SP parameter names match @-parameter names defined in each .sql file exactly.
/// </summary>
public sealed class AuditLogRepository : DapperRepository, IAuditLogRepository
{
    public AuditLogRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    public Task<IEnumerable<AuditLogEntry>> GetByCaseAsync(string rbse)
        => QueryAsync<AuditLogEntry>("GetAuditLogByCase", new { RBSE = rbse });

    public Task<IEnumerable<AuditLogEntry>> GetByDateAsync(DateTime logDate)
        => QueryAsync<AuditLogEntry>("GetAuditLogByDate", new { LogDate = logDate });

    public Task<IEnumerable<AuditLogEntry>> GetByFarmAsync(string cphh)
        => QueryAsync<AuditLogEntry>("GetAuditLogByFarm", new { CPHH = cphh });

    public Task<IEnumerable<AuditLogEntry>> GetByUserAsync(
        DateTime startDate, DateTime endDate, int userId)
        => QueryAsync<AuditLogEntry>("GetAuditLogByUser",
            new { StartDate = startDate, EndDate = endDate, UserID = userId });

    public Task<IEnumerable<AuditLogCaseMoveEntry>> GetCaseMovesAsync(
        DateTime startDate, DateTime endDate)
        => QueryAsync<AuditLogCaseMoveEntry>("GetAuditLogCaseMoves",
            new { StartDate = startDate, EndDate = endDate });

    public Task<IEnumerable<AuditLogCPHHChangeEntry>> GetCphhChangesAsync(
        DateTime startDate, DateTime endDate)
        => QueryAsync<AuditLogCPHHChangeEntry>("GetAuditLogCPHHChanges",
            new { StartDate = startDate, EndDate = endDate });

    public Task<IEnumerable<AuditLogNewFarmEntry>> GetNewFarmsAsync(
        DateTime startDate, DateTime endDate)
        => QueryAsync<AuditLogNewFarmEntry>("GetAuditLogNewFarms",
            new { StartDate = startDate, EndDate = endDate });

    public Task<IEnumerable<AuditLogRBSEChangeEntry>> GetRbseChangesAsync(
        DateTime startDate, DateTime endDate)
        => QueryAsync<AuditLogRBSEChangeEntry>("GetAuditLogRBSEChanges",
            new { StartDate = startDate, EndDate = endDate });
}
