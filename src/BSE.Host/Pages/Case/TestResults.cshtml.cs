using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class TestResultsModel(
    ITestRepository testRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public IReadOnlyList<CaseTestRecord> Tests { get; private set; } = [];
    public IEnumerable<LuTestType> TestTypes { get; private set; } = [];
    public IEnumerable<LuTestResult> TestResults { get; private set; } = [];

    [BindProperty]
    public NewTestViewModel NewTest { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddTestAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTest.TestType))
        {
            ModelState.AddModelError(string.Empty, "Test type is required.");
            await LoadAsync();
            return Page();
        }

        var command = new AddTestCommand(Rbse, NewTest.TestType, NewTest.TestResult);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await testRepository.AddAsync(command, conn, tx);
        tx.Commit();

        TempData["Success"] = "Test record added.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteTestAsync(int testId)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await testRepository.DeleteAsync(testId, conn, tx);
        tx.Commit();

        TempData["Success"] = "Test record deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    private async Task LoadAsync()
    {
        Tests = await testRepository.GetByRbseAsync(Rbse);
        TestTypes = await lookups.GetTestTypesAsync();
        TestResults = await lookups.GetTestResultsAsync();
    }

    public class NewTestViewModel
    {
        public string? TestType { get; set; }
        public string? TestResult { get; set; }
    }
}
