namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Farm relation record. Maps the SELECT columns returned by the <c>GetRelatedFarm</c>
/// stored procedure. <c>Status</c> is a computed column: the related farm owner/address,
/// or "BSE Free" when the related CPHH has no Farm row.
/// </summary>
public record FarmRelationRecord
{
    public int ID { get; init; }
    public string CPHH { get; init; } = string.Empty;
    public string RelatedCPHH { get; init; } = string.Empty;
    public string? Status { get; init; }
    public byte[]? RowStamp { get; init; }
}