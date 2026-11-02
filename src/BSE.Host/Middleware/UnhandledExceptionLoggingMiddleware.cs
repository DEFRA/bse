namespace BSE.Host.Middleware;

public sealed class UnhandledExceptionLoggingMiddleware(RequestDelegate next, ILogger<UnhandledExceptionLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (LogUnhandled(ex, context))
        {
            throw;
        }
    }

    // Always returns false so the exception filter logs without catching.
    private bool LogUnhandled(Exception ex, HttpContext context)
    {
        logger.LogError(
            ex,
            "Unhandled exception during {Method} {Path}. TraceId: {TraceIdentifier}",
            context.Request.Method,
            context.Request.Path.Value,
            context.TraceIdentifier);
        return false;
    }
}
