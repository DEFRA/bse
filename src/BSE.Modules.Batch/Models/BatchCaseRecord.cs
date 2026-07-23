namespace BSE.Modules.Batch.Models;

/// <summary>
/// Full RBSE/CPHH result with display-formatted counterparts, returned by GetCPHHRBSEForBatchID.
/// DisplayRbse is formatted as "XX/XX/XXXXX"; DisplayCphh as "XX/XXX/XXXX/XX".
/// </summary>
public record BatchCaseRecord(
    string Rbse,
    string DisplayRbse,
    string? Cphh,
    string? DisplayCphh);
