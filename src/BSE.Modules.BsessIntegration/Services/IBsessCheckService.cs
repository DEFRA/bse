using BSE.Modules.BsessIntegration.Models;

namespace BSE.Modules.BsessIntegration.Services;

public interface IBsessCheckService
{
    Task<BsessCheckByRbseResult?> GetCheckByRbseAsync(string rbse, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BsessDiscrepancyRecord>> GetCheckByDateAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
