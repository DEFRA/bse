namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luCaseType via GetluCaseType.</summary>
public record LuCaseType
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
