namespace BSE.SharedKernel;

/// <summary>
/// Typed, JSON-serialisable replacement for the legacy <c>Session["FarmDetails"]</c> DataSet pattern.
/// Stores only the farm wizard navigation context (CPHH identifiers) —
/// live farm data is fetched from the database on each request via the FarmManagement module.
/// Serialised to Redis (or MemoryCache fallback) via
/// <c>BSE.Infrastructure.Cache.DistributedCacheExtensions</c>.
/// </summary>
public sealed record FarmWizardState(
    string CphhNumber,
    string? NewCphhNumber = null);
