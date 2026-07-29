using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

public class MinuteModel(ICaseWorkService caseWorkService) : PageModel
{
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ActiveMemo"] = "Active Memo",
        ["AnnexA"] = "Annex A",
        ["AnnexB"] = "Annex B",
        ["AnnexC"] = "Annex C",
        ["AnnexD"] = "Annex D",
        ["AMFS"] = "AMFS (Fallen Stock)"
    };

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Type { get; set; } = string.Empty;

    public string MinuteTypeLabel => Labels.TryGetValue(Type, out var label) ? label : Type;

    public MinuteDetailsRecord? Details { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Details = await caseWorkService.GetMinuteDetailsAsync(Rbse, Type);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await caseWorkService.SetMinuteSentDateAsync(Rbse, Type);
        TempData["Success"] = $"{MinuteTypeLabel} sent date set to today.";
        return RedirectToPage("/CaseWork/Menu", new { rbse = Rbse });
    }
}
