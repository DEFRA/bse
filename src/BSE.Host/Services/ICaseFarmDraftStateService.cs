using BSE.SharedKernel;

namespace BSE.Host.Services;

public interface ICaseFarmDraftStateService
{
    Task<CaseFarmDraftState?> GetAsync(string rbse, CancellationToken ct = default);
    Task SetAsync(CaseFarmDraftState state, CancellationToken ct = default);
    Task ClearAsync(string rbse, CancellationToken ct = default);
}