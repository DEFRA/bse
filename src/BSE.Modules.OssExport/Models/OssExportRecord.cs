namespace BSE.Modules.OssExport.Models;

/// <summary>
/// Result from <c>GetOSSExportByRBSE</c> — display-formatted RBSE/CPHH plus farm owner details.
/// Used to generate the OSS batch report cover sheet per case.
/// Note: both RBSE and CPHH are returned as display-formatted strings by the SP
/// (e.g. "01/23/45678", "12/345/6789/01").
/// </summary>
public sealed record OssExportRecord
{
    /// <summary>Display-formatted RBSE (NN/NN/NNNNN).</summary>
    public string Rbse { get; init; } = string.Empty;
    /// <summary>Display-formatted CPHH (NN/NNN/NNNN/NN).</summary>
    public string Cphh { get; init; } = string.Empty;
    public string? OwnerName { get; init; }
    public string? Address1 { get; init; }
}
