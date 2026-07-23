namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luHerdType via GetluHerdType.</summary>
public record LuHerdType
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
