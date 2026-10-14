namespace BSE.Host.Services;

/// <summary>
/// Cross-tab session storage for an in-progress case edit — the single store every case tab's
/// handlers read from and stage into. See <see cref="CaseEditSessionState"/> for why this exists
/// as one object instead of several.
/// </summary>
public interface ICaseEditSessionStateService
{
    Task<CaseEditSessionState?> GetAsync(string rbse, CancellationToken ct = default);
    Task SetAsync(CaseEditSessionState state, CancellationToken ct = default);
    Task ClearAsync(string rbse, CancellationToken ct = default);
}
