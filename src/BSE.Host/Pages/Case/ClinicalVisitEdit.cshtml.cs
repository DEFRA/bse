using BSE.Modules.CaseManagement.Commands;
using BSE.Host.ModelBinding;
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

    [BindProperty, ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? VisitDate { get; set; }
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
        // The DatePickerModelBinder already added "Please enter a valid date" when the posted
        // text failed to parse — don't stack a second, contradictory "blank" message on it.
        if (!VisitDate.HasValue && ModelState[nameof(VisitDate)] is not { Errors.Count: > 0 })
            ModelState.AddModelError(nameof(VisitDate), "Enter a visit date.");

        // Legacy's CalendarDate.Validate treats the earliest bound as inclusive — a visit on the
        // birth date itself is valid.
        var minDate = (await caseRepository.GetCaseByRbseAsync(Rbse))?.BirthDate?.Date ?? DateTime.UnixEpoch;
        if (VisitDate.HasValue && VisitDate.Value.Date < minDate)
            ModelState.AddModelError(nameof(VisitDate), minDate == DateTime.UnixEpoch
                ? "The visit date must be on or after 1 January 1970."
                : "The visit date must be on or after the birth date.");

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