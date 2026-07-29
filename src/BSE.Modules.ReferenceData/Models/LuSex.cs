namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luSex via GetluSex.</summary>
public record LuSex
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
