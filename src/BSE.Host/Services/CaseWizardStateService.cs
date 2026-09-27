using BSE.Infrastructure.Cache;
using BSE.SharedKernel;
using Microsoft.Extensions.Caching.Distributed;

namespace BSE.Host.Services;

/// <summary>
/// Carries the batch selected on the home page through to case entry, replacing the
/// legacy Session["BatchID"] / Session["BatchNumber"] pair. Scoped to the signed-in user.
/// </summary>
public interface ICaseWizardStateService
{
    Task<CaseWizardState?> GetAsync(CancellationToken ct = default);
    Task SetAsync(CaseWizardState state, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}

public sealed class CaseWizardStateService(
    IDistributedCache cache,
    ICacheKeyProvider keys,
    ICurrentUserService currentUser) : ICaseWizardStateService
{
    public async Task<CaseWizardState?> GetAsync(CancellationToken ct = default)
        => await cache.GetJsonAsync<CaseWizardState>(await KeyAsync(), ct);

    public async Task SetAsync(CaseWizardState state, CancellationToken ct = default)
        => await cache.SetJsonAsync(await KeyAsync(), state, cancellationToken: ct);

    public async Task ClearAsync(CancellationToken ct = default)
        => await cache.RemoveAsync(await KeyAsync(), ct);

    private async Task<string> KeyAsync()
        => keys.CaseWizard((await currentUser.GetUserIdAsync()).ToString());
}
