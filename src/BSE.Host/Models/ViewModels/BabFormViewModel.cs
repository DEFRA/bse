using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;

namespace BSE.Host.Models.ViewModels;

/// <summary>BAB tab field values shared by the Bab and BabEdit pages.</summary>
public class BabFormViewModel
{
    public string? NatalCphh { get; set; }
    public string? Notes { get; set; }
    public string? TracedName { get; set; }
    public string? TracedAddress1 { get; set; }
    public string? TracedAddress2 { get; set; }
    public string? TracedAddress3 { get; set; }
    public string? TracedPostcode { get; set; }
    public string? FeedRisk { get; set; }
    public string? HorizontalRisk { get; set; }
    public string? MaternalRisk { get; set; }

    public static BabFormViewModel FromRecord(CaseBabRecord r) => new()
    {
        NatalCphh      = r.NatalCphh,      Notes          = r.Notes,
        TracedName     = r.TracedName,      TracedAddress1 = r.TracedAddress1,
        TracedAddress2 = r.TracedAddress2,  TracedAddress3 = r.TracedAddress3,
        TracedPostcode = r.TracedPostcode,  FeedRisk       = r.FeedRisk,
        HorizontalRisk = r.HorizontalRisk,  MaternalRisk   = r.MaternalRisk
    };

    /// <summary>Overlays a staged-but-not-yet-committed BAB edit (from another tab's
    /// cross-tab draft) so revisiting this tab shows the pending edit instead of the
    /// last-committed DB values.</summary>
    public void ApplyStagedCommand(EditCaseBabCommand c)
    {
        NatalCphh = c.NatalCphh;
        Notes = c.Notes;
        TracedName = c.TracedName;
        TracedAddress1 = c.TracedAddress1;
        TracedAddress2 = c.TracedAddress2;
        TracedAddress3 = c.TracedAddress3;
        TracedPostcode = c.TracedPostcode;
        FeedRisk = c.FeedRisk;
        HorizontalRisk = c.HorizontalRisk;
        MaternalRisk = c.MaternalRisk;
    }
}
