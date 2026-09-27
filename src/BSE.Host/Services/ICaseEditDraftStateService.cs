using BSE.SharedKernel;

namespace BSE.Host.Services;

public interface ICaseEditDraftStateService
{
    Task<CaseEditDraftState?> GetAsync(string rbse, CancellationToken ct = default);
    Task SetAsync(CaseEditDraftState state, CancellationToken ct = default);
    Task ClearAsync(string rbse, CancellationToken ct = default);
}
