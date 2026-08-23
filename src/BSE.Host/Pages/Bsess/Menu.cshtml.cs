using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Bsess;

[Authorize(Policy = "AuditAccess")]
public class MenuModel : PageModel
{
    public void OnGet() { }
}
