using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Help;

[Authorize]
public class HelpModel : PageModel
{
    public void OnGet() { }
}
