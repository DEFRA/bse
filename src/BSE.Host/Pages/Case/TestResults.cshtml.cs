using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class TestResultsModel(
    ITestRepository testRepository) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public IReadOnlyList<CaseTestRecord> Tests { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteTestAsync(int testId, string rowStampBase64)
    {
        var rowStamp = Convert.FromBase64String(rowStampBase64);
        await testRepository.DeleteAsync(testId, rowStamp);

        TempData["Success"] = "Test record deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    private async Task LoadAsync()
    {
        Tests = await testRepository.GetByRbseAsync(Rbse);
    }
}
