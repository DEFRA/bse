namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luAnimalStatus via GetluAnimalStatus.</summary>
public record LuAnimalStatus
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
