using BSE.Infrastructure.Cache;
using BSE.SharedKernel;
using Microsoft.Extensions.Caching.Distributed;

namespace BSE.Host.Services;

public sealed class CaseRelationsDraftStateService(
    IDistributedCache cache,
    ICacheKeyProvider keys,
    ICurrentUserService currentUser) : ICaseRelationsDraftStateService
{
    public async Task<CaseRelationsDraftState?> GetAsync(string rbse, CancellationToken ct = default)
        => await cache.GetJsonAsync<CaseRelationsDraftState>(await KeyAsync(rbse), ct);

    public async Task SetAsync(CaseRelationsDraftState state, CancellationToken ct = default)
        => await cache.SetJsonAsync(await KeyAsync(state.Rbse), state, cancellationToken: ct);

    public async Task ClearAsync(string rbse, CancellationToken ct = default)
        => await cache.RemoveAsync(await KeyAsync(rbse), ct);

    private async Task<string> KeyAsync(string rbse)
        => $"{keys.CaseWizard((await currentUser.GetUserIdAsync()).ToString())}:relations:{rbse}";
}
