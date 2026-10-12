using BSE.Infrastructure;
using BSE.Modules.AuditLog.Models;
using BSE.SharedKernel;

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

    // AuditLog.LogDate defaults to GETDATE(), which Azure SQL Database always returns in UTC
    // (unlike on-prem SQL Server, which follows the server OS's local timezone) — every row
    // read back here must be converted to UK local time before display, or every audit
    // timestamp is silently wrong by the UTC/BST offset.
    private async Task<IEnumerable<T>> QueryWithUkTimesAsync<T>(string spName, object? param)
        where T : AuditLogEntry
    {
        var rows = await QueryAsync<T>(spName, param);
        return rows.Select(r => r with { DateTime = UkTimeZoneHelper.ToUkTime(r.DateTime) });
    }

    public Task<IEnumerable<AuditLogEntry>> GetByCaseAsync(string rbse)
        => QueryWithUkTimesAsync<AuditLogEntry>("GetAuditLogByCase", new { RBSE = rbse });

    public Task<IEnumerable<AuditLogEntry>> GetByDateAsync(DateTime logDate)
        => QueryWithUkTimesAsync<AuditLogEntry>("GetAuditLogByDate", new { LogDate = logDate });

    public Task<IEnumerable<AuditLogEntry>> GetByFarmAsync(string cphh)
        => QueryWithUkTimesAsync<AuditLogEntry>("GetAuditLogByFarm", new { CPHH = cphh });

    public Task<IEnumerable<AuditLogEntry>> GetByUserAsync(
        DateTime startDate, DateTime endDate, int userId)
        => QueryWithUkTimesAsync<AuditLogEntry>("GetAuditLogByUser",
            new { StartDate = startDate, EndDate = endDate, UserID = userId });

    public Task<IEnumerable<AuditLogCaseMoveEntry>> GetCaseMovesAsync(
        DateTime startDate, DateTime endDate)
        => QueryWithUkTimesAsync<AuditLogCaseMoveEntry>("GetAuditLogCaseMoves",
            new { StartDate = startDate, EndDate = endDate });

    public Task<IEnumerable<AuditLogCPHHChangeEntry>> GetCphhChangesAsync(
        DateTime startDate, DateTime endDate)
        => QueryWithUkTimesAsync<AuditLogCPHHChangeEntry>("GetAuditLogCPHHChanges",
            new { StartDate = startDate, EndDate = endDate });

    public Task<IEnumerable<AuditLogNewFarmEntry>> GetNewFarmsAsync(
        DateTime startDate, DateTime endDate)
        => QueryWithUkTimesAsync<AuditLogNewFarmEntry>("GetAuditLogNewFarms",
            new { StartDate = startDate, EndDate = endDate });

    public Task<IEnumerable<AuditLogRBSEChangeEntry>> GetRbseChangesAsync(
        DateTime startDate, DateTime endDate)
        => QueryWithUkTimesAsync<AuditLogRBSEChangeEntry>("GetAuditLogRBSEChanges",
            new { StartDate = startDate, EndDate = endDate });
}
