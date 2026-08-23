using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class OpenCasesModel : PageModel
{
    public void OnGet() { }
}
