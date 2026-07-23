namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Vetnet herdmark record. Maps the SELECT columns returned by the
/// <c>GetVetnetDetailsByCPHH</c> stored procedure.
/// </summary>
public record VetnetRecord
{
    public string CPHH { get; init; } = string.Empty;
    public string? Herdmark { get; init; }
    public string? NumericHerdmark { get; init; }
}
