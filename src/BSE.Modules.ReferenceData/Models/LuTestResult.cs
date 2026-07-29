namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luTestResult via GetluTestResult.</summary>
public record LuTestResult
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
