using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class ClinicalVisitEditModel(
    IClinicalRepository clinicalRepository,
    ICaseRepository caseRepository,
    IDbConnectionFactory connectionFactory) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public int Id { get; set; }

    [BindProperty] public DateTime? VisitDate { get; set; }
    [BindProperty] public string RowStampBase64 { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var visit = (await clinicalRepository.GetVisitsByRbseAsync(Rbse)).FirstOrDefault(v => v.Id == Id);
        if (visit is null) return RedirectToPage("/Case/Clinical", new { rbse = Rbse });

        VisitDate = visit.VisitDate;
        RowStampBase64 = Convert.ToBase64String(visit.RowStamp ?? []);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!VisitDate.HasValue)
            ModelState.AddModelError(nameof(VisitDate), "Enter a visit date.");

        var minDate = (await caseRepository.GetCaseByRbseAsync(Rbse))?.BirthDate?.Date ?? new DateTime(1970, 1, 1);
        if (VisitDate.HasValue && VisitDate.Value.Date <= minDate)
            ModelState.AddModelError(nameof(VisitDate), minDate == new DateTime(1970, 1, 1)
                ? "The visit date must be after 1 January 1970."
                : "The visit date must be after the birth date.");

        if (VisitDate.HasValue && VisitDate.Value.Date > DateTime.Today)
            ModelState.AddModelError(nameof(VisitDate), "The visit date must not be in the future.");

        var allVisits = (await clinicalRepository.GetVisitsByRbseAsync(Rbse)).ToList();
        if (VisitDate.HasValue && allVisits.Any(v => v.Id != Id && v.VisitDate?.Date == VisitDate.Value.Date))
            ModelState.AddModelError(nameof(VisitDate), "A visit on this date already exists. The visit date must be unique.");

        if (!ModelState.IsValid)
            return Page();

        var rowStamp = string.IsNullOrEmpty(RowStampBase64) ? [] : Convert.FromBase64String(RowStampBase64);
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await clinicalRepository.EditVisitAsync(new EditClinicalVisitCommand(Id, VisitDate, rowStamp), conn, tx);
        tx.Commit();

        TempData["Success"] = "Clinical visit updated.";
        return RedirectToPage("/Case/Clinical", new { rbse = Rbse });
    }
}