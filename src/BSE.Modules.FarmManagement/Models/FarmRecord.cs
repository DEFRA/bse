namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Core farm record. Maps the SELECT columns returned by the <c>GetFarmByCPHH</c> stored procedure.
/// </summary>
public record FarmRecord
{
    public string CPHH { get; init; } = string.Empty;
    public bool IsNonGBFarm { get; init; }
    public string? OwnerName { get; init; }
    public string? Address1 { get; init; }
    public string? Address2 { get; init; }
    public string? Address3 { get; init; }
    public string? Postcode { get; init; }
    public string? Parish { get; init; }
    public string? District { get; init; }
    public string? County { get; init; }
    public string? CorrespondenceAddress1 { get; init; }
    public string? CorrespondenceAddress2 { get; init; }
    public string? CorrespondenceAddress3 { get; init; }
    public string? CorrespondencePostcode { get; init; }
    public string? MapReference { get; init; }
    public string? Herdmark1 { get; init; }
    public string? Herdmark2 { get; init; }
    public string? Herdmark3 { get; init; }
    public string? NumericHerdmark1 { get; init; }
    public string? NumericHerdmark2 { get; init; }
    public string? AHO { get; init; }
    public string? HerdType { get; init; }
    public string? PedigreeType { get; init; }
    public bool IsDealer { get; init; }
    public int? ADNSRegionID { get; init; }
    public int? AuthorityID { get; init; }
    public int? AuthorityCountyID { get; init; }
    public byte[]? RowStamp { get; init; }
}
