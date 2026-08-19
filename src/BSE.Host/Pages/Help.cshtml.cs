using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages;

[Authorize]
public class HelpModel : PageModel
{
    public void OnGet() { }
}
