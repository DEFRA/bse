using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class ClinicalVisitAddModel(
    IClinicalRepository clinicalRepository,
    ICaseRepository caseRepository,
    IDbConnectionFactory connectionFactory) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty] public DateTime? VisitDate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var @case = await caseRepository.GetCaseByRbseAsync(Rbse);
        if (@case is null)
            return RedirectToPage("/Case/Lookup");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var allVisits = (await clinicalRepository.GetVisitsByRbseAsync(Rbse)).ToList();
        var birthDate = (await caseRepository.GetCaseByRbseAsync(Rbse))?.BirthDate;

        ValidateVisitDate(VisitDate, birthDate, nameof(VisitDate));

        if (ModelState.IsValid && VisitDate.HasValue && allVisits.Any(v => v.VisitDate?.Date == VisitDate.Value.Date))
            ModelState.AddModelError(nameof(VisitDate), "A visit on this date already exists. The visit date must be unique.");

        if (!ModelState.IsValid)
            return Page();

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await clinicalRepository.AddVisitAsync(new AddClinicalVisitCommand(Rbse, VisitDate), conn, tx);
        tx.Commit();

        TempData["Success"] = "Clinical visit added.";
        return RedirectToPage("/Case/Clinical", new { rbse = Rbse });
    }

    private void ValidateVisitDate(DateTime? date, DateTime? birthDate, string key)
    {
        if (!date.HasValue)
        {
            ModelState.AddModelError(key, "Enter a visit date.");
            return;
        }

        var minDate = birthDate?.Date ?? new DateTime(1970, 1, 1);
        if (date.Value.Date <= minDate)
            ModelState.AddModelError(key, birthDate.HasValue
                ? "The visit date must be after the birth date."
                : "The visit date must be after 1 January 1970.");

        if (date.Value.Date > DateTime.Today)
            ModelState.AddModelError(key, "The visit date must not be in the future.");
    }
}
