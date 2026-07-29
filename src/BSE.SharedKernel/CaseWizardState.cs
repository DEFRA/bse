namespace BSE.SharedKernel;

/// <summary>
/// Typed, JSON-serialisable replacement for the legacy <c>Session["CaseDetails"]</c> DataSet pattern.
/// Stores only navigation context (identifiers + form-outstanding flags) —
/// live case and farm data is fetched from the database on each request via the module services.
/// Serialised to Redis (or MemoryCache fallback) via
/// <c>BSE.Infrastructure.Cache.DistributedCacheExtensions</c>.
/// </summary>
public sealed record CaseWizardState(
    string RbseNumber,
    string BatchNumber,
    int BatchId,
    bool IsCaseClosed = false,
    bool HomebredFormOutstanding = false,
    bool BreederFormOutstanding = false,
    bool PurchaserFormOutstanding = false,
    bool VendorFormOutstanding = false,
    bool SummarySheetOutstanding = false,
    bool AllPaperworkOutstanding = false);
