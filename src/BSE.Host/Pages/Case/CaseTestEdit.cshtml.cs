using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.SharedKernel;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class CaseTestEditModel(
    ITestRepository testRepository,
    ILookupDataService lookups) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    [BindProperty] public string TestType { get; set; } = string.Empty;
    [BindProperty] public string? TestResult { get; set; }
    [BindProperty] public string RowStampBase64 { get; set; } = string.Empty;

    public IEnumerable<ILookupItem> TestTypeOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> TestResultOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();

        var test = (await testRepository.GetByRbseAsync(Rbse)).FirstOrDefault(t => t.Id == Id);
        if (test is null)
            return RedirectToPage("/Case/Edit", new { rbse = Rbse });

        TestType = test.TestType;
        TestResult = test.TestResult;
        RowStampBase64 = Convert.ToBase64String(test.RowStamp ?? []);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();

        if (string.IsNullOrWhiteSpace(TestType))
            ModelState.AddModelError(nameof(TestType), "Select a test type.");

        if (!ModelState.IsValid)
            return Page();

        var rowStamp = string.IsNullOrEmpty(RowStampBase64) ? [] : Convert.FromBase64String(RowStampBase64);
        await testRepository.EditAsync(new EditTestCommand(Id, Rbse.Replace("/", ""), TestType, TestResult, rowStamp));

        TempData["Success"] = "Test record updated.";
        return RedirectToPage("/Case/Edit", new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var testTypeTask = lookups.GetLookupAsync(LookupTableId.TestType);
        var testResultTask = lookups.GetLookupAsync(LookupTableId.TestResult);
        await Task.WhenAll(testTypeTask, testResultTask);
        TestTypeOptions = await testTypeTask;
        TestResultOptions = await testResultTask;
    }
}
