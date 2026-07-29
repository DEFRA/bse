namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Herd detail record joined with case context. Maps the SELECT columns returned by
/// the <c>GetHerdDetailByBatchID</c> stored procedure. Includes the RBSE case number
/// and does not include a RowStamp (read-only reporting query).
/// </summary>
public record HerdDetailRecord
{
    public string RBSE { get; init; } = string.Empty;
    public string CPHH { get; init; } = string.Empty;
    public short? HerdYear { get; init; }
    public short? TotalSize { get; init; }
    public short? Lactation1Size { get; init; }
    public short? Lactation2Size { get; init; }
    public short? Lactation3Size { get; init; }
    public short? Lactation4Size { get; init; }
    public short? Lactation5Size { get; init; }
    public short? Lactation6Size { get; init; }
    public short? Lactation7Size { get; init; }
    public short? Lactation8Size { get; init; }
    public short? Lactation9Size { get; init; }
    public short? Lactation10Size { get; init; }
    public short? Lactation10PlusSize { get; init; }
}
