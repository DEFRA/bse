using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace BSE.Host.Pages;

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class ErrorModel : PageModel
{
    public string? Message { get; private set; }

    public void OnGet(string? message = null)
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
