namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luCaseFate via GetluCaseFate.</summary>
public record LuCaseFate
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
