using BSE.SharedKernel;

namespace BSE.Host.Services;

public interface ICaseRelationsDraftStateService
{
    Task<CaseRelationsDraftState?> GetAsync(string rbse, CancellationToken ct = default);
    Task SetAsync(CaseRelationsDraftState state, CancellationToken ct = default);
    Task ClearAsync(string rbse, CancellationToken ct = default);
}
