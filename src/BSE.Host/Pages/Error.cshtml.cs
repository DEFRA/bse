using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages;

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class ErrorModel : PageModel
{
    public string? Message { get; private set; }

    public void OnGet(string? message = null)
    {
        Message = message;
    }
}
