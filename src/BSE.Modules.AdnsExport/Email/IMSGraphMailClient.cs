namespace BSE.Modules.AdnsExport.Email;

/// <summary>
/// Abstraction over outbound MSGraph mail dispatch.
/// </summary>
public interface IMSGraphMailClient : IDisposable
{
    Task SendAsync(string from, string to, string subject, string body);
}
