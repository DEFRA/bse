using BSE.SharedKernel;

namespace BSE.Modules.ReferenceData.Models;

/// <summary>Generic two-column lookup item used for simple dropdown binding.</summary>
public record LookupItem : ILookupItem
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Description { get; init; } = "";

    public LookupItem() { }

    public LookupItem(int id, string description)
    {
        Id = id;
        Description = description;
    }

    public LookupItem(int id, string code, string description)
    {
        Id = id;
        Code = code;
        Description = description;
    }
}
