using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages;

[AllowAnonymous]
public class AccessibilityStatementModel : PageModel
{
    public void OnGet()
    {
        // Static content page; nothing to load.
    }
}
