namespace BSE.Modules.AdnsExport.Configuration;

/// <summary>
/// ADNS MSGraph mail settings.
/// </summary>
public sealed class AdnsMsGraphOptions
{
    public const string SectionName = "AdnsMsGraph";

    public string MsGraphTenantId { get; set; } = string.Empty;
    public string MsGraphClientId { get; set; } = string.Empty;
    public string MsGraphClientSecret { get; set; } = string.Empty;
    public string MsGraphSenderUserId { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
}
