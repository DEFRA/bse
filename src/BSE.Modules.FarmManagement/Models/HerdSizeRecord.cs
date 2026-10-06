namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Herd size record for a farm/year. Maps the SELECT columns returned by
/// the <c>GetHerdSizeByCPHH</c> stored procedure.
/// </summary>
public record HerdSizeRecord : HerdLactationSizesRecord
{
    public int ID { get; init; }
    public string CPHH { get; init; } = string.Empty;
    public short HerdYear { get; init; }
    public short TotalSize { get; init; }
    public byte[]? RowStamp { get; init; }
}
