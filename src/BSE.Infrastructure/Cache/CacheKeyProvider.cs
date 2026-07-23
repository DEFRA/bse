namespace BSE.Infrastructure.Cache;

/// <summary>
/// Generates namespaced, user-scoped <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> keys.
/// All keys are prefixed with <c>bse:wizard:</c> so Redis key-space tooling can identify them easily.
/// </summary>
public sealed class CacheKeyProvider : ICacheKeyProvider
{
    private const string Prefix = "bse:wizard";

    public string CaseWizard(string userId) => $"{Prefix}:case:{userId}";
    public string FarmWizard(string userId) => $"{Prefix}:farm:{userId}";
}
