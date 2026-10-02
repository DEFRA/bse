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
    string? BseFinalResult)
{
    public static bool HasAnyValues(
        string? notificationDate,
        string? bsessEartag,
        string? bsessBirthDate,
        string? testGroupName,
        string? bsessFinalResult,
        string? barcode,
        string? formADate,
        string? bseEartag,
        string? bseBirthDate,
        string? survey,
        string? bseFinalResult)
        => !string.IsNullOrWhiteSpace(notificationDate)
            || !string.IsNullOrWhiteSpace(bsessEartag)
            || !string.IsNullOrWhiteSpace(bsessBirthDate)
            || !string.IsNullOrWhiteSpace(testGroupName)
            || !string.IsNullOrWhiteSpace(bsessFinalResult)
            || !string.IsNullOrWhiteSpace(barcode)
            || !string.IsNullOrWhiteSpace(formADate)
            || !string.IsNullOrWhiteSpace(bseEartag)
            || !string.IsNullOrWhiteSpace(bseBirthDate)
            || !string.IsNullOrWhiteSpace(survey)
            || !string.IsNullOrWhiteSpace(bseFinalResult);
}
