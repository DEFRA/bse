namespace BSE.Modules.OssExport.Models;

/// <summary>
/// Represents a single row in the OSS export file.
/// Retrieved from <c>GetCaseByBatchID</c> stored procedure for a given batch.
/// Used to generate the pipe-delimited export file content.
/// </summary>
public sealed record OssExportFileRecord
{
    /// <summary>Raw 9-digit RBSE (no slashes).</summary>
    public string Rbse { get; init; } = string.Empty;

    /// <summary>Display-formatted CPHH (NN/NNN/NNNN/NN).</summary>
    public string Cphh { get; init; } = string.Empty;
}
