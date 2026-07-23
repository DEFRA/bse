namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Command parameters for the <c>EditFarm</c> stored procedure.
/// <c>RowStamp</c> is the current concurrency token; the SP will return code 1 if the
/// row has changed since it was read. <c>UserID</c> is passed separately.
/// </summary>
public record UpdateFarmCommand(
    string CPHH,
    string? OwnerName,
    string? Address1,
    string? Address2,
    string? Address3,
    string? Postcode,
    string? Parish,
    string? District,
    string? County,
    string? CorrespondenceAddress1,
    string? CorrespondenceAddress2,
    string? CorrespondenceAddress3,
    string? CorrespondencePostcode,
    string? MapReference,
    string? Herdmark1,
    string? Herdmark2,
    string? Herdmark3,
    string? NumericHerdmark1,
    string? NumericHerdmark2,
    string? AHO,
    string? HerdType,
    string? PedigreeType,
    bool IsDealer,
    int? ADNSRegionID,
    byte[]? RowStamp
);
