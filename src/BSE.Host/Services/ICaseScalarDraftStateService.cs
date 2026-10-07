namespace BSE.Host.Services;

public interface ICaseScalarDraftStateService
{
    Task<CaseScalarDraftState?> GetAsync(string rbse, CancellationToken ct = default);
    Task SetAsync(CaseScalarDraftState state, CancellationToken ct = default);
    Task ClearAsync(string rbse, CancellationToken ct = default);
}
