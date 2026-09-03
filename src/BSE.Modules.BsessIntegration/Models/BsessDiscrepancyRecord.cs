namespace BSE.Modules.BsessIntegration.Models;

/// <summary>
/// A single discrepancy row returned by <c>GetBSESSCheckByDate</c>.
/// Populated when BSESS and BSE data differ in birth date, eartag, or test group.
/// </summary>
public sealed record BsessDiscrepancyRecord(
    string Rbse,
    DateTime? BsessBirthDate,
    DateTime? BseBirthDate,
    string? BsessEartag,
    string? BseEartag,
    string? BsessTestGroup,
    string? BseTestGroup);
