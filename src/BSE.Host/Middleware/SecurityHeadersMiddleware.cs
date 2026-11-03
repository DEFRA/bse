namespace BSE.Host.Middleware;

/// <summary>
/// Adds baseline security headers to every response. Headers are applied in OnStarting so they also
/// cover redirects, error-handler output and static files.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, bool enableHsts)
{
    public const string NonceItemKey = "csp-nonce";

    // Inline scripts must carry the per-request nonce (added by CspNonceTagHelper); inline styles remain allowed.
    public static string BuildContentSecurityPolicy(string nonce) =>
        "default-src 'self'; " +
        $"script-src 'self' 'nonce-{nonce}'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "frame-ancestors 'none'";

    public const string StrictTransportSecurity = "max-age=31536000; includeSubDomains";

    public Task Invoke(HttpContext context)
    {
        var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        context.Items[NonceItemKey] = nonce;

        context.Response.OnStarting(static state =>
        {
            var (response, hsts, scriptNonce) = ((HttpResponse, bool, string))state;
            var headers = response.Headers;

            headers.ContentSecurityPolicy = BuildContentSecurityPolicy(scriptNonce);
            headers.XFrameOptions = "DENY";
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            if (hsts)
                headers.StrictTransportSecurity = StrictTransportSecurity;

            return Task.CompletedTask;
        }, (context.Response, enableHsts, nonce));

        return next(context);
    }
}
