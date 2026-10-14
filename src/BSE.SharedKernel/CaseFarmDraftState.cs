namespace BSE.SharedKernel;

public sealed class CaseFarmDraftLinkedFarmItem
{
    public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    public int? Id { get; set; }
    public string RelatedCphh { get; set; } = string.Empty;
    public string RowStampBase64 { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class CaseFarmDraftHerdSizeItem : LactationSizeFields
{
    // Stable identifier for this draft row, used by the inline add/edit/delete handlers
    // instead of Id, since newly staged rows have no database Id until Save.
    public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    public int? Id { get; set; }
    public int HerdYear { get; set; }
    public int TotalSize { get; set; }
    public string RowStampBase64 { get; set; } = string.Empty;
}