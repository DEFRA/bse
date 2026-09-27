using BSE.Infrastructure.Cache;
using BSE.SharedKernel;
using Microsoft.Extensions.Caching.Distributed;

namespace BSE.Host.Services;

public sealed class CaseClinicalDraftStateService(
    IDistributedCache cache,
    ICacheKeyProvider keys,
    ICurrentUserService currentUser) : ICaseClinicalDraftStateService
{
    public async Task<CaseClinicalDraftState?> GetAsync(string rbse, CancellationToken ct = default)
        => await cache.GetJsonAsync<CaseClinicalDraftState>(await KeyAsync(rbse), ct);

    public async Task SetAsync(CaseClinicalDraftState state, CancellationToken ct = default)
        => await cache.SetJsonAsync(await KeyAsync(state.Rbse), state, cancellationToken: ct);

    public async Task ClearAsync(string rbse, CancellationToken ct = default)
        => await cache.RemoveAsync(await KeyAsync(rbse), ct);

    private async Task<string> KeyAsync(string rbse)
        => $"{keys.CaseWizard((await currentUser.GetUserIdAsync()).ToString())}:clinical:{rbse}";
}
