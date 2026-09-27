namespace BSE.Host.Middleware;

public sealed class UnhandledExceptionLoggingMiddleware(RequestDelegate next, ILogger<UnhandledExceptionLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unhandled exception during {Method} {Path}. TraceId: {TraceIdentifier}",
                context.Request.Method,
                context.Request.Path.Value,
                context.TraceIdentifier);

            throw;
        }
    }
}
