namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luTSETestingSite via GetluTSETestingSite.</summary>
public record LuTSETestingSite
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string CPH { get; init; } = string.Empty;
    public string AHO { get; init; } = string.Empty;
}
