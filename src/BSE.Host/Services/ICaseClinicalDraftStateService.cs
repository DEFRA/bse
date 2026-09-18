using BSE.SharedKernel;

namespace BSE.Host.Services;

public interface ICaseClinicalDraftStateService
{
    Task<CaseClinicalDraftState?> GetAsync(string rbse, CancellationToken ct = default);
    Task SetAsync(CaseClinicalDraftState state, CancellationToken ct = default);
    Task ClearAsync(string rbse, CancellationToken ct = default);
}
