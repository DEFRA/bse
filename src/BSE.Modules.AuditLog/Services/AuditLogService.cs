using BSE.Modules.AuditLog.Models;
using BSE.Modules.AuditLog.Repositories;

namespace BSE.Modules.AuditLog.Services;

/// <summary>
/// Thin delegation layer over <see cref="IAuditLogRepository"/>.
/// No business logic — audit records are immutable from the application perspective.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
        => _repository = repository;

    public Task<IEnumerable<AuditLogEntry>> GetByCaseAsync(string rbse)
        => _repository.GetByCaseAsync(rbse);

    public Task<IEnumerable<AuditLogEntry>> GetByDateAsync(DateTime logDate)
        => _repository.GetByDateAsync(logDate);

    public Task<IEnumerable<AuditLogEntry>> GetByFarmAsync(string cphh)
        => _repository.GetByFarmAsync(cphh);

    public Task<IEnumerable<AuditLogEntry>> GetByUserAsync(
        DateTime startDate, DateTime endDate, int userId)
        => _repository.GetByUserAsync(startDate, endDate, userId);

    public async Task<IEnumerable<AuditLogEntry>> GetCaseMovesAsync(
        DateTime startDate, DateTime endDate)
        => await _repository.GetCaseMovesAsync(startDate, endDate);

    public async Task<IEnumerable<AuditLogEntry>> GetCphhChangesAsync(
        DateTime startDate, DateTime endDate)
        => await _repository.GetCphhChangesAsync(startDate, endDate);

    public async Task<IEnumerable<AuditLogEntry>> GetNewFarmsAsync(
        DateTime startDate, DateTime endDate)
        => await _repository.GetNewFarmsAsync(startDate, endDate);

    public async Task<IEnumerable<AuditLogEntry>> GetRbseChangesAsync(
        DateTime startDate, DateTime endDate)
        => await _repository.GetRbseChangesAsync(startDate, endDate);
}
