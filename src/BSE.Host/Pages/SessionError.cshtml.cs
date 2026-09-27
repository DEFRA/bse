using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages;

[AllowAnonymous]
public class SessionErrorModel : PageModel
{
    public void OnGet()
    {
    }
}
