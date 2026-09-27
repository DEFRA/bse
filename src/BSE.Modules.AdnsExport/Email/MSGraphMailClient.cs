using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using BSE.Modules.AdnsExport.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace BSE.Modules.AdnsExport.Email;

public sealed class MSGraphMailClient : IMSGraphMailClient
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    private readonly AdnsMsGraphOptions _options;
    private readonly TokenRequestContext _tokenRequestContext = new(GraphScopes);
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy _retryPolicy;

    public MSGraphMailClient(HttpClient httpClient, IOptions<AdnsMsGraphOptions> options)
    {
        _options = options.Value;
        _httpClient = httpClient;

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<InvalidOperationException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    public async Task SendAsync(string from, string to, string subject, string body)
    {
        ValidateRequiredConfiguration(_options);

        var fromAddress = from?.Trim() ?? string.Empty;
        var toAddress = to?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("ADNS MSGraph configuration is missing required value(s): AdnsMsGraph:FromAddress");

        if (string.IsNullOrWhiteSpace(toAddress))
            throw new InvalidOperationException("ADNS email recipient cannot be empty.");

        var senderUser = string.IsNullOrWhiteSpace(_options.MsGraphSenderUserId)
            ? fromAddress
            : _options.MsGraphSenderUserId;

        var credential = new ClientSecretCredential(
            _options.MsGraphTenantId,
            _options.MsGraphClientId,
            _options.MsGraphClientSecret);

        var accessToken = await credential.GetTokenAsync(_tokenRequestContext, CancellationToken.None);

        await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(senderUser)}/sendMail");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

            var payload = JsonSerializer.Serialize(new
            {
                message = new
                {
                    subject,
                    body = new { contentType = "Text", content = body },
                    toRecipients = new[]
                    {
                        new { emailAddress = new { address = toAddress } }
                    }
                },
                saveToSentItems = false
            });

            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
            {
                throw new HttpRequestException($"MSGraph send failed with status {(int)response.StatusCode}: {responseBody}");
            }

            throw new InvalidOperationException($"MSGraph send failed with status {(int)response.StatusCode}: {responseBody}");
        });
    }

    public void Dispose()
    {
    }

    private static void ValidateRequiredConfiguration(AdnsMsGraphOptions options)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(options.MsGraphTenantId))
            missing.Add("MsGraphTenantId (AdnsMsGraph:MsGraphTenantId | AdnsMsGraph__MsGraphTenantId | AdnsMsGraph_MsGraphTenantId)");
        if (string.IsNullOrWhiteSpace(options.MsGraphClientId))
            missing.Add("MsGraphClientId (AdnsMsGraph:MsGraphClientId | AdnsMsGraph__MsGraphClientId | AdnsMsGraph_MsGraphClientId)");
        if (string.IsNullOrWhiteSpace(options.MsGraphClientSecret))
            missing.Add("MsGraphClientSecret (AdnsMsGraph:MsGraphClientSecret | AdnsMsGraph__MsGraphClientSecret | AdnsMsGraph_MsGraphClientSecret | MsGraphClientSecret)");
        if (string.IsNullOrWhiteSpace(options.FromAddress))
            missing.Add("FromAddress (AdnsMsGraph:FromAddress | AdnsMsGraph__FromAddress | AdnsMsGraph_FromAddress)");
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"ADNS MSGraph configuration is missing required value(s): {string.Join(", ", missing)}");
        }
    }
}
