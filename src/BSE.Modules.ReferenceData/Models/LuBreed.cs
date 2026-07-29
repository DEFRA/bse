namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luBreed via GetluBreed.</summary>
public record LuBreed
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string AmalgamatedName { get; init; } = string.Empty;
}
