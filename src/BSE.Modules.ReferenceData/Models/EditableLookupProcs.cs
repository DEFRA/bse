namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps the four SP-name output columns returned by GetEditableLookupProcs.</summary>
public record EditableLookupProcs
{
    public string SelectStoredProcedure { get; init; } = string.Empty;
    public string UpdateStoredProcedure { get; init; } = string.Empty;
    public string InsertStoredProcedure { get; init; } = string.Empty;
    public string DeleteStoredProcedure { get; init; } = string.Empty;
}
