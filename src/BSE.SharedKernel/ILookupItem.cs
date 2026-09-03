namespace BSE.SharedKernel;

/// <summary>Minimal contract for a two-column lookup item (Id + human-readable label).</summary>
public interface ILookupItem
{
    int Id { get; }
    string Code { get; }
    string Description { get; }
}
