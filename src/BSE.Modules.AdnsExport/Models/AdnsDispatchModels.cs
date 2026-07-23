namespace BSE.Modules.AdnsExport.Models;

/// <summary>
/// ADNS case for Channel Islands export (Jersey/Guernsey/Isle of Man).
/// These cases are not in the database — they are entered manually by the user on the UI.
/// </summary>
public sealed record ChannelIslandCaseInput(
    int AdnsYear,
    int AdnsNumber,
    int AdnsRegionId,
    string AdnsRegionName,
    DateTime ConfirmationDate);

/// <summary>Input from the NI export form — a user-built table of cases.</summary>
public sealed record NiCaseInput(
    string Rbse,
    int AdnsYear,
    int AdnsNumber,
    int AdnsRegionId,
    string AdnsRegionName,
    DateTime ConfirmationDate);

/// <summary>
/// Command to dispatch an already-generated ADNS export preview.
/// The preview must have been generated in the same session.
/// </summary>
public sealed record DispatchAdnsCommand(
    /// <summary>Area code: "GB", "NI", or "CI".</summary>
    string Area,
    string EmailReference,
    IReadOnlyList<AdnsCaseRecord> Cases,
    /// <summary>User's own email address — receives a copy of the notification.</summary>
    string UserEmailAddress,
    /// <summary>Whether to persist the ADNS reference assignments to the database.</summary>
    bool SaveAdnsData);
