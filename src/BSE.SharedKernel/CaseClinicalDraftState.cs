namespace BSE.SharedKernel;

public sealed class CaseClinicalDraftVisitItem
{
    // Stable identifier for this draft row, used by the inline add/edit/delete handlers
    // instead of Id, since newly staged rows have no database Id until Save.
    public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    public int? Id { get; set; }
    public DateTime? VisitDate { get; set; }
    public string RowStampBase64 { get; set; } = string.Empty;
}
