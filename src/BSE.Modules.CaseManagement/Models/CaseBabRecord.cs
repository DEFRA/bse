namespace BSE.Modules.CaseManagement.Models;

/// <summary>Result record for GetBABByRBSE — Born After Ban (BAB) data.</summary>
public sealed record CaseBabRecord
{
    public string Rbse { get; init; } = string.Empty;
    public string? NatalCphh { get; init; }
    public string? Notes { get; init; }
    public string? TracedName { get; init; }
    public string? TracedAddress1 { get; init; }
    public string? TracedAddress2 { get; init; }
    public string? TracedAddress3 { get; init; }
    public string? TracedPostcode { get; init; }
    public string? FeedRisk { get; init; }
    public string? HorizontalRisk { get; init; }
    public string? MaternalRisk { get; init; }
    public byte[]? RowStamp { get; init; }
}
