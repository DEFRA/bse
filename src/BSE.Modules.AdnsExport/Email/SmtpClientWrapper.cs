using System.Net.Mail;
using BSE.Modules.AdnsExport.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace BSE.Modules.AdnsExport.Email;

/// <summary>
/// Production SMTP sender. Wraps <c>System.Net.Mail.SmtpClient</c> with a Polly
/// exponential back-off retry policy (3 attempts, 2s / 4s / 8s waits).
/// All configuration is sourced from <see cref="AdnsSmtpOptions"/> (environment variables).
/// </summary>
public sealed class SmtpClientWrapper : ISmtpClient
{
    private readonly SmtpClient _inner;
    private readonly AsyncRetryPolicy _retryPolicy;

    public SmtpClientWrapper(IOptions<AdnsSmtpOptions> options)
    {
        var opts = options.Value;
        _inner = new SmtpClient(opts.Host, opts.Port);

        _retryPolicy = Policy
            .Handle<SmtpException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    public Task SendAsync(string from, string to, string subject, string body)
        => _retryPolicy.ExecuteAsync(() => _inner.SendMailAsync(new MailMessage(from, to, subject, body)
        {
            IsBodyHtml = false
        }));

    public void Dispose() => _inner.Dispose();
}
