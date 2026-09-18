namespace BSE.SharedKernel;

public sealed class CaseFarmDraftState
{
    public string Rbse { get; set; } = string.Empty;
    public string Cphh { get; set; } = string.Empty;
    public List<CaseFarmDraftLinkedFarmItem> LinkedFarms { get; set; } = [];
    public List<CaseFarmDraftHerdSizeItem> HerdSizes { get; set; } = [];

    // True once any add/edit/delete has been staged since the draft was created from the DB.
    public bool HasPendingChanges { get; set; }
}

public sealed class CaseFarmDraftLinkedFarmItem
{
    public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    public int? Id { get; set; }
    public string RelatedCphh { get; set; } = string.Empty;
    public string RowStampBase64 { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class CaseFarmDraftHerdSizeItem
{
    // Stable identifier for this draft row, used by the inline add/edit/delete handlers
    // instead of Id, since newly staged rows have no database Id until Save.
    public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    public int? Id { get; set; }
    public int HerdYear { get; set; }
    public int TotalSize { get; set; }
    public int Lactation1Size { get; set; }
    public int Lactation2Size { get; set; }
    public int Lactation3Size { get; set; }
    public int Lactation4Size { get; set; }
    public int Lactation5Size { get; set; }
    public int Lactation6Size { get; set; }
    public int Lactation7Size { get; set; }
    public int Lactation8Size { get; set; }
    public int Lactation9Size { get; set; }
    public int Lactation10Size { get; set; }
    public int Lactation10PlusSize { get; set; }
    public string RowStampBase64 { get; set; } = string.Empty;
}