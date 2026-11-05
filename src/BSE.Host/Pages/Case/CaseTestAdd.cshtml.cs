using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class CaseTestAddModel(
    ITestRepository testRepository,
    ILookupDataService lookups) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string? From { get; set; }

    [BindProperty] public string TestType { get; set; } = string.Empty;
    [BindProperty] public string? TestResult { get; set; }

    public IEnumerable<ILookupItem> TestTypeOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> TestResultOptions { get; private set; } = [];

    public bool ReturnToDefra => string.Equals(From, "defra", StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();

        if (string.IsNullOrWhiteSpace(TestType))
            ModelState.AddModelError(nameof(TestType), "Select a test type.");

        if (!ModelState.IsValid)
            return Page();

        await testRepository.AddAsync(new AddTestCommand(Rbse.Replace("/", ""), TestType, TestResult));
        TempData["Success"] = "Test record added.";

        if (ReturnToDefra)
            return RedirectToPage("/Case/Edit", new { rbse = Rbse });

        return RedirectToPage("/Case/TestResults", new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var typeTask = lookups.GetLookupAsync(LookupTableId.TestType);
        var resultTask = lookups.GetLookupAsync(LookupTableId.TestResult);
        await Task.WhenAll(typeTask, resultTask);
        TestTypeOptions = await typeTask;
        TestResultOptions = await resultTask;
    }
}
