namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Herd size record for a farm/year. Maps the SELECT columns returned by
/// the <c>GetHerdSizeByCPHH</c> stored procedure.
/// </summary>
public record HerdSizeRecord
{
    public int ID { get; init; }
    public string CPHH { get; init; } = string.Empty;
    public short HerdYear { get; init; }
    public short TotalSize { get; init; }
    public short Lactation1Size { get; init; }
    public short Lactation2Size { get; init; }
    public short Lactation3Size { get; init; }
    public short Lactation4Size { get; init; }
    public short Lactation5Size { get; init; }
    public short Lactation6Size { get; init; }
    public short Lactation7Size { get; init; }
    public short Lactation8Size { get; init; }
    public short Lactation9Size { get; init; }
    public short Lactation10Size { get; init; }
    public short Lactation10PlusSize { get; init; }
    public byte[]? RowStamp { get; init; }
}
