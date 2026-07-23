using BSE.Modules.FarmManagement.Models;

namespace BSE.Host.Models.ViewModels;

public class FarmEditViewModel
{
    public string CPHH { get; set; } = "";
    public string? OwnerName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? Postcode { get; set; }
    public string? Parish { get; set; }
    public string? District { get; set; }
    public string? County { get; set; }
    public string? Herdmark1 { get; set; }
    public string? Herdmark2 { get; set; }
    public string? AHO { get; set; }
    public string? HerdType { get; set; }
    public string? PedigreeType { get; set; }
    public bool IsDealer { get; set; }
    public int? ADNSRegionID { get; set; }
    public string? MapReference { get; set; }
    public string? CorrespondenceAddress1 { get; set; }
    public string? CorrespondenceAddress2 { get; set; }
    public string? CorrespondenceAddress3 { get; set; }
    public string? CorrespondencePostcode { get; set; }
    public string? NumericHerdmark1 { get; set; }

    public AddFarmCommand ToAddCommand() => new(
        CPHH, OwnerName, Address1, Address2, Address3, Postcode,
        Parish, District, County,
        CorrespondenceAddress1, CorrespondenceAddress2, CorrespondenceAddress3, CorrespondencePostcode,
        MapReference, Herdmark1, Herdmark2, null, NumericHerdmark1, null, AHO, HerdType, PedigreeType, IsDealer, ADNSRegionID);

    public UpdateFarmCommand ToUpdateCommand(byte[]? rowStamp) => new(
        CPHH, OwnerName, Address1, Address2, Address3, Postcode,
        Parish, District, County,
        CorrespondenceAddress1, CorrespondenceAddress2, CorrespondenceAddress3, CorrespondencePostcode,
        MapReference, Herdmark1, Herdmark2, null, NumericHerdmark1, null, AHO, HerdType, PedigreeType, IsDealer, ADNSRegionID,
        rowStamp);

    public static FarmEditViewModel FromRecord(FarmRecord r) => new()
    {
        CPHH = r.CPHH,
        OwnerName = r.OwnerName,
        Address1 = r.Address1,
        Address2 = r.Address2,
        Address3 = r.Address3,
        Postcode = r.Postcode,
        Parish = r.Parish,
        District = r.District,
        County = r.County,
        Herdmark1 = r.Herdmark1,
        Herdmark2 = r.Herdmark2,
        AHO = r.AHO,
        HerdType = r.HerdType,
        PedigreeType = r.PedigreeType,
        IsDealer = r.IsDealer,
        ADNSRegionID = r.ADNSRegionID,
        MapReference = r.MapReference,
        CorrespondenceAddress1 = r.CorrespondenceAddress1,
        CorrespondenceAddress2 = r.CorrespondenceAddress2,
        CorrespondenceAddress3 = r.CorrespondenceAddress3,
        CorrespondencePostcode = r.CorrespondencePostcode,
        NumericHerdmark1 = r.NumericHerdmark1
    };
}
