namespace BSE.Modules.BsessIntegration.Models;

/// <summary>
/// Result from the <c>GetBSESSCheckByRBSE</c> stored procedure.
/// All values are returned as strings because the SP formats them for display.
/// </summary>
public sealed record BsessCheckByRbseResult(
    string? NotificationDate,
    string? BsessEartag,
    string? BsessBirthDate,
    string? TestGroupName,
    string? BsssFinalResult,
    string? Barcode,
    string? FormADate,
    string? BseEartag,
    string? BseBirthDate,
    string? Survey,
    string? BseFinalResult);
