using BSE.SharedKernel;

namespace BSE.Host.Services;

public interface ICaseFeedsDraftStateService
{
    Task<CaseFeedsDraftState?> GetAsync(string rbse, CancellationToken ct = default);
    Task SetAsync(CaseFeedsDraftState state, CancellationToken ct = default);
    Task ClearAsync(string rbse, CancellationToken ct = default);
}
