using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages;

[Authorize]
public class HomeModel : PageModel
{
    public void OnGet() { }
}
