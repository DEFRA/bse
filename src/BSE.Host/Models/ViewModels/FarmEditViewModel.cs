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
    public string? CorrespondenceAddress1 { get; set; }
    public string? CorrespondenceAddress2 { get; set; }
    public string? CorrespondenceAddress3 { get; set; }
    public string? CorrespondencePostcode { get; set; }

    /// <summary>First 2 characters of the map reference (OS grid square letters).</summary>
    public string? MapRef1 { get; set; }
    /// <summary>Characters 2–4 of the map reference (easting, 3 digits).</summary>
    public string? MapRef2 { get; set; }
    /// <summary>Characters 5–7 of the map reference (northing, 3 digits).</summary>
    public string? MapRef3 { get; set; }

    /// <summary>Computed full map reference (MapRef1 + MapRef2 + MapRef3).</summary>
    public string? MapReference =>
        string.IsNullOrWhiteSpace(MapRef1) && string.IsNullOrWhiteSpace(MapRef2) && string.IsNullOrWhiteSpace(MapRef3)
            ? null
            : $"{MapRef1}{MapRef2}{MapRef3}";

    public string? Herdmark1 { get; set; }
    public string? Herdmark2 { get; set; }
    public string? Herdmark3 { get; set; }
    public string? NumericHerdmark1 { get; set; }
    public string? NumericHerdmark2 { get; set; }
    public string? AHO { get; set; }
    public string? HerdType { get; set; }
    public string? PedigreeType { get; set; }
    public bool IsDealer { get; set; }
    public int? ADNSRegionID { get; set; }
    public int? AuthorityID { get; set; }
    public int? AuthorityCountyID { get; set; }

    /// <summary>Authoritative, stored non-GB flag (not re-derived from the CPHH prefix) — must match
    /// what CaseEditOrchestrationService's mandatory-fields check uses, so the UI never locks/excludes
    /// a field (e.g. ADNS Region) that the backend still requires.</summary>
    public bool IsNonGBFarm { get; set; }

    public AddFarmCommand ToAddCommand() => new(
        CPHH, OwnerName, Address1, Address2, Address3, Postcode,
        Parish, District, County,
        CorrespondenceAddress1, CorrespondenceAddress2, CorrespondenceAddress3, CorrespondencePostcode,
        MapReference, Herdmark1, Herdmark2, Herdmark3, NumericHerdmark1, NumericHerdmark2,
        AHO, HerdType, PedigreeType, IsDealer, ADNSRegionID);

    public UpdateFarmCommand ToUpdateCommand(byte[]? rowStamp) => new(
        CPHH, OwnerName, Address1, Address2, Address3, Postcode,
        Parish, District, County,
        CorrespondenceAddress1, CorrespondenceAddress2, CorrespondenceAddress3, CorrespondencePostcode,
        MapReference, Herdmark1, Herdmark2, Herdmark3, NumericHerdmark1, NumericHerdmark2,
        AHO, HerdType, PedigreeType, IsDealer, ADNSRegionID,
        rowStamp, AuthorityID, AuthorityCountyID);

    /// <summary>Overlays a staged-but-not-yet-committed Farm edit (from another tab's
    /// cross-tab draft) onto this view model, so revisiting the Farm tab shows pending
    /// edits made elsewhere instead of silently reverting to the last-committed DB values.</summary>
    public void ApplyStagedCommand(UpdateFarmCommand c)
    {
        CPHH = c.CPHH;
        OwnerName = c.OwnerName;
        Address1 = c.Address1;
        Address2 = c.Address2;
        Address3 = c.Address3;
        Postcode = c.Postcode;
        Parish = c.Parish;
        District = c.District;
        County = c.County;
        CorrespondenceAddress1 = c.CorrespondenceAddress1;
        CorrespondenceAddress2 = c.CorrespondenceAddress2;
        CorrespondenceAddress3 = c.CorrespondenceAddress3;
        CorrespondencePostcode = c.CorrespondencePostcode;
        MapRef1 = c.MapReference?.Length >= 2 ? c.MapReference[..2]  : c.MapReference;
        MapRef2 = c.MapReference?.Length >= 5 ? c.MapReference[2..5] : null;
        MapRef3 = c.MapReference?.Length >= 8 ? c.MapReference[5..8] : null;
        Herdmark1 = c.Herdmark1;
        Herdmark2 = c.Herdmark2;
        Herdmark3 = c.Herdmark3;
        NumericHerdmark1 = c.NumericHerdmark1;
        NumericHerdmark2 = c.NumericHerdmark2;
        AHO = c.AHO;
        HerdType = c.HerdType;
        PedigreeType = c.PedigreeType;
        IsDealer = c.IsDealer;
        ADNSRegionID = c.ADNSRegionID;
        AuthorityID = c.AuthorityID;
        AuthorityCountyID = c.AuthorityCountyID;
    }

    public static FarmEditViewModel FromRecord(FarmRecord r) => new()
    {
        CPHH                   = r.CPHH,
        OwnerName              = r.OwnerName,
        Address1               = r.Address1,
        Address2               = r.Address2,
        Address3               = r.Address3,
        Postcode               = r.Postcode,
        Parish                 = r.Parish,
        District               = r.District,
        County                 = r.County,
        CorrespondenceAddress1 = r.CorrespondenceAddress1,
        CorrespondenceAddress2 = r.CorrespondenceAddress2,
        CorrespondenceAddress3 = r.CorrespondenceAddress3,
        CorrespondencePostcode = r.CorrespondencePostcode,
        // Split stored 8-char map reference as 2 + 3 + 3
        MapRef1                = r.MapReference?.Length >= 2 ? r.MapReference[..2]  : r.MapReference,
        MapRef2                = r.MapReference?.Length >= 5 ? r.MapReference[2..5] : null,
        MapRef3                = r.MapReference?.Length >= 8 ? r.MapReference[5..8] : null,
        Herdmark1              = r.Herdmark1,
        Herdmark2              = r.Herdmark2,
        Herdmark3              = r.Herdmark3,
        NumericHerdmark1       = r.NumericHerdmark1,
        NumericHerdmark2       = r.NumericHerdmark2,
        AHO                    = r.AHO,
        HerdType               = r.HerdType,
        PedigreeType           = r.PedigreeType,
        IsDealer               = r.IsDealer,
        ADNSRegionID           = r.ADNSRegionID,
        AuthorityID            = r.AuthorityID,
        AuthorityCountyID      = r.AuthorityCountyID,
        IsNonGBFarm            = r.IsNonGBFarm
    };
}
