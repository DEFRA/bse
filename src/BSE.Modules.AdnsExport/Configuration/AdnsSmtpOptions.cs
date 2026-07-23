namespace BSE.Modules.AdnsExport.Configuration;

/// <summary>
/// SMTP and email configuration bound from environment variables.
/// Registered via <c>IOptions&lt;AdnsSmtpOptions&gt;</c>.
///
/// Environment variable names (set via <c>appsettings.json</c> or container env):
/// <list type="bullet">
///   <item>BSE_SMTP_HOST</item>
///   <item>BSE_SMTP_PORT</item>
///   <item>BSE_ADNS_FROM_ADDRESS</item>
///   <item>BSE_ADNS_TO_ADDRESS</item>
/// </list>
/// </summary>
public sealed class AdnsSmtpOptions
{
    public const string SectionName = "AdnsSmtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string FromAddress { get; set; } = string.Empty;
    /// <summary>The Brussels ADNS recipient address.</summary>
    public string ToAddress { get; set; } = string.Empty;
}
