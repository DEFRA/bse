namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Summary farm record for list results. Maps the SELECT columns returned by
/// the <c>GetFarmsByCPH</c> stored procedure. CPHH is returned pre-formatted
/// (XX/NNN/NNNN/NN) by the SP.
/// </summary>
public record FarmSummaryRecord
{
    public string CPHH { get; init; } = string.Empty;
    public string? OwnerName { get; init; }
    public string? Address1 { get; init; }
}
