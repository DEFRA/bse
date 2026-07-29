namespace BSE.Modules.AdnsExport.Email;

/// <summary>
/// Abstraction over <c>System.Net.Mail.SmtpClient</c> for testability.
/// Injected as a singleton; the concrete implementation wraps SmtpClient.
/// </summary>
public interface ISmtpClient : IDisposable
{
    Task SendAsync(string from, string to, string subject, string body);
}
