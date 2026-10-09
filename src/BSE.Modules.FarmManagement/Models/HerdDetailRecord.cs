namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Herd detail record joined with case context. Maps the SELECT columns returned by
/// the <c>GetHerdDetailByBatchID</c> stored procedure. Includes the RBSE case number
/// and does not include a RowStamp (read-only reporting query).
/// </summary>
public record HerdDetailRecord : HerdLactationSizesRecord
{
    public string RBSE { get; init; } = string.Empty;
    public string CPHH { get; init; } = string.Empty;
    public short? HerdYear { get; init; }
    public short? TotalSize { get; init; }
}
