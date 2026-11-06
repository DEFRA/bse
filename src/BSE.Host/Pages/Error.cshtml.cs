using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace BSE.Host.Pages;

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
[SuppressMessage("Security", "S4502", Justification = "The exception handler re-executes the failed request on /Error; antiforgery validation would reject that re-execution (including when the failure was an antiforgery error). The page has no state-changing handler.")]
[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? Message { get; private set; }

    // The exception handler re-executes the original request, so /Error must answer every verb.
    public void OnGet(string? message = null) => Populate(message);
    public void OnPost(string? message = null) => Populate(message);
    public void OnPut(string? message = null) => Populate(message);
    public void OnPatch(string? message = null) => Populate(message);
    public void OnDelete(string? message = null) => Populate(message);

    private void Populate(string? message)
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        Message = exception switch
        {
            SqlException => "The service is temporarily unavailable because the database cannot be reached. Please try again shortly or contact support if the problem persists.",
            _ when IsSqlConnectivityException(exception) => "The service is temporarily unavailable because the database cannot be reached. Please try again shortly or contact support if the problem persists.",
            not null => message ?? "An unexpected error occurred. Try again or contact support if the problem persists.",
            _ => message
        };
    }

    private static bool IsSqlConnectivityException(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is SqlException)
                return true;
            ex = ex.InnerException;
        }
        return false;
    }
}
