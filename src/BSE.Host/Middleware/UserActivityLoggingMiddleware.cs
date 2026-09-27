using System.Diagnostics;
using System.Security.Claims;
using BSE.Modules.UserManagement.Identity;

namespace BSE.Host.Middleware;

public sealed class UserActivityLoggingMiddleware(RequestDelegate next, ILogger<UserActivityLoggingMiddleware> logger)
{
    private static readonly string[] AuditedGetPathKeywords = ["search", "export", "download", "print", "audit"];

    public async Task Invoke(HttpContext context)
    {
        if (!ShouldAudit(context.Request))
        {
            await next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        await next(context);

        sw.Stop();

        var user = context.User;
        var userUpn = user.Identity?.IsAuthenticated == true
            ? user.FindFirst(ClaimsUserContext.EmailClaimType)?.Value
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst(ClaimTypes.Upn)?.Value
                ?? user.Identity?.Name
                ?? "unknown"
            : "anonymous";

        logger.LogInformation(
            "User activity {Method} {Path} responded {StatusCode} in {ElapsedMs}ms by {UserUpn}",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds,
            userUpn);
    }

    private static bool ShouldAudit(HttpRequest request)
    {
        if (HttpMethods.IsPost(request.Method)
            || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsDelete(request.Method)
            || HttpMethods.IsPatch(request.Method))
        {
            return true;
        }

        if (!HttpMethods.IsGet(request.Method))
            return false;

        var path = request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return AuditedGetPathKeywords.Any(k => path.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
