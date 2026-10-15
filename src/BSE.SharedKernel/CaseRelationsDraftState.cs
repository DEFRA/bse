namespace BSE.SharedKernel;

/// <summary>A dam/sire match staged from Look Up (or PickSireDam), not yet committed to the database.</summary>
public sealed class PendingParentDraft
{
    public int Id { get; set; }
    public string? Rbse { get; set; }
    public string? Eartag { get; set; }
    public string? Name { get; set; }
    public string? Herdbook { get; set; }
    public int? BirthDay { get; set; }
    public int? BirthMonth { get; set; }
    public int? BirthYear { get; set; }
    public string? RowStampBase64 { get; set; }
    public string? Fate { get; set; }
    public string? FinalResult { get; set; }
    public int? ChildCount { get; set; }
}

public sealed class CaseRelationsDraftItem
{
    // Stable identifier for this draft row, used by the inline add/edit/delete handlers
    // instead of Id, since newly staged rows have no database Id until Save.
    public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    public int? Id { get; set; }
    public string RelationType { get; set; } = string.Empty;
    public string? RelationTypeDesc { get; set; }
    public string? RelationRbse { get; set; }
    public string? Sex { get; set; }
    public string? SexDesc { get; set; }
    public int? BirthDay { get; set; }
    public int? BirthMonth { get; set; }
    public int? BirthYear { get; set; }
    public string? RelationFate { get; set; }
    public string? RelationFateDesc { get; set; }
    public DateTime? LeftDate { get; set; }
    public string? EartagCountry { get; set; }
    public string? EartagHerdmark { get; set; }
    public string? Eartag { get; set; }
    public string? Sire { get; set; }
    public string RowStampBase64 { get; set; } = string.Empty;
}
