using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages;

[AllowAnonymous]
public class UnauthorizedModel : PageModel
{
    public void OnGet()
    {
    }
}
