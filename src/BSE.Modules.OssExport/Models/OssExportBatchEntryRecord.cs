namespace BSE.Modules.OssExport.Models;

/// <summary>
/// Represents a single RBSE entry in the OSS Export batch grid for BSE1b.
/// This is a client-side grid item that combines the lookup result with batch context.
/// </summary>
public sealed record OssExportBatchEntryRecord
{
    /// <summary>Unique identifier for this grid row (auto-incremented by client).</summary>
    public int Id { get; init; }

    /// <summary>Raw 9-digit RBSE (no slashes).</summary>
    public string Rbse { get; init; } = string.Empty;

    /// <summary>Display-formatted CPHH (NN/NNN/NNNN/NN) from the database.</summary>
    public string Cphh { get; init; } = string.Empty;

    /// <summary>Farm owner name from the database.</summary>
    public string? OwnerName { get; init; }

    /// <summary>First line of farm address from the database.</summary>
    public string? Address1 { get; init; }

    /// <summary>The batch ID this entry belongs to (set when first entry is added).</summary>
    public int? BatchId { get; init; }
}
