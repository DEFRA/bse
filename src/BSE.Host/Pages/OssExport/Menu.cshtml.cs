using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace BSE.Host.Pages.OssExport;

[Authorize(Policy = "VLAAccess")]
public class OssExportMenuModel : PageModel
{
    public void OnGet()
    {
        // Navigation page - no action needed
    }
}