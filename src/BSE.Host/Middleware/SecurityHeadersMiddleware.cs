namespace BSE.Host.Middleware;

/// <summary>
/// Adds baseline security headers to every response. Headers are applied in OnStarting so they also
/// cover redirects, error-handler output and static files.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, bool enableHsts)
{
    // Views still use inline scripts/styles, so 'unsafe-inline' stays until they move to nonces.
    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
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
        context.Response.OnStarting(static state =>
        {
            var (response, hsts) = ((HttpResponse, bool))state;
            var headers = response.Headers;

            headers.ContentSecurityPolicy = ContentSecurityPolicy;
            headers.XFrameOptions = "DENY";
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            if (hsts)
                headers.StrictTransportSecurity = StrictTransportSecurity;

            return Task.CompletedTask;
        }, (context.Response, enableHsts));

        return next(context);
    }
}
