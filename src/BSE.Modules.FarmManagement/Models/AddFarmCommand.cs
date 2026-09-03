namespace BSE.Modules.FarmManagement.Models;

/// <summary>
/// Command parameters for the <c>AddFarm</c> stored procedure.
/// <c>IsNonGBFarm</c> is derived by the SP from the CPHH prefix and is not a parameter.
/// <c>UserID</c> is passed separately so it is not embedded in the domain command.
/// </summary>
public record AddFarmCommand(
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
    int? ADNSRegionID
);
