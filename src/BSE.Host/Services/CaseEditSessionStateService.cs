using BSE.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Distributed;

namespace BSE.Host.Services;

public sealed class CaseEditSessionStateService(
    IDistributedCache cache,
    ICacheKeyProvider keys,
    ICurrentUserService currentUser) : ICaseEditSessionStateService
{
    public async Task<CaseEditSessionState?> GetAsync(string rbse, CancellationToken ct = default)
        => await cache.GetJsonAsync<CaseEditSessionState>(await KeyAsync(rbse), ct);

    public async Task SetAsync(CaseEditSessionState state, CancellationToken ct = default)
        => await cache.SetJsonAsync(await KeyAsync(state.Rbse), state, cancellationToken: ct);

    public async Task ClearAsync(string rbse, CancellationToken ct = default)
        => await cache.RemoveAsync(await KeyAsync(rbse), ct);

    private async Task<string> KeyAsync(string rbse)
        => $"{keys.CaseWizard((await currentUser.GetUserIdAsync()).ToString())}:caseedit:{rbse}";
}
