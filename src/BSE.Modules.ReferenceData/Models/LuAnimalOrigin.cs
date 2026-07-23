namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luAnimalOrigin via GetluAnimalOrigin.</summary>
public record LuAnimalOrigin
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
