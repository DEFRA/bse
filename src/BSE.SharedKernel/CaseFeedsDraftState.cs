namespace BSE.SharedKernel;

public sealed class CaseFeedsDraftState
{
    public string Rbse { get; set; } = string.Empty;
    public List<CaseFeedsDraftItem> Feeds { get; set; } = [];
    public bool HasPendingChanges { get; set; }
}

public sealed class CaseFeedsDraftItem
{
    // Stable identifier for this draft row, used by the inline add/edit/delete handlers
    // instead of Id, since newly staged rows have no database Id until Save.
    public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    public int? Id { get; set; }
    public short? YearFrom { get; set; }
    public short? YearTo { get; set; }
    public string? RationType { get; set; }
    public string? RationDescription { get; set; }
    public string? RationName { get; set; }
    public bool IsPrePurchase { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string RowStampBase64 { get; set; } = string.Empty;
}
