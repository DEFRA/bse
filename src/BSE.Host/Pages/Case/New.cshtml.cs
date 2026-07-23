using BSE.Host.Services;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class NewModel : PageModel
{
    private readonly ICaseService _cases;
    private readonly IBatchService _batch;
    private readonly ICurrentUserService _currentUser;

    public NewModel(ICaseService cases, IBatchService batch, ICurrentUserService currentUser)
    {
        _cases = cases;
        _batch = batch;
        _currentUser = currentUser;
    }

    [BindProperty] public string Rbse { get; set; } = "";
    [BindProperty] public string Cphh { get; set; } = "";
    [BindProperty] public string? Survey { get; set; }
    [BindProperty] public string? Sex { get; set; }
    [BindProperty] public string? Breed { get; set; }
    [BindProperty] public string? EartagCountry { get; set; }
    [BindProperty] public string? EartagHerdmark { get; set; }
    [BindProperty] public string? Eartag { get; set; }
    [BindProperty] public DateTime? BirthDate { get; set; }
    [BindProperty] public DateTime? FormADate { get; set; }
    [BindProperty] public string? Fate { get; set; }
    [BindProperty] public string? Origin { get; set; }
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string? CaseType { get; set; }

    public void OnGet(string? cphh = null) => Cphh = cphh ?? "";

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var batch = await _batch.GetOrCreateBatchNumberAsync();
        var userId = await _currentUser.GetUserIdAsync();

        var addCase = new AddCaseCommand(
            Rbse: Rbse.Trim(), Cphh: Cphh.Trim(),
            EartagCountry: EartagCountry, EartagHerdmark: EartagHerdmark, Eartag: Eartag,
            PreviousEartag: null, Bse1ReceivedDate: null, FormADate: FormADate,
            FormAResubmittedDate: null, FormBDate: null, Fate: Fate, FormCDate: null,
            IsPurchaserBse1Received: false, IsBreederBse1Received: false,
            IsVendor1Bse1Received: false, IsHomebredBse1Received: false,
            IsSummarySheetReceived: false, IsPaperworkComplete: false,
            ReportedLocation: null, Survey: Survey, Notes: Notes,
            BirthDate: BirthDate, IsBirthDateEst: false, DamStatus: null,
            BirthDateSource: null, ValuationAge: null, Sex: Sex, Breed: Breed,
            Origin: Origin, PurchaseDate: null, PurchaseAgeInMonths: null,
            PurchasedCounty: null, HerdEntryDate: null, OnsetDate: null,
            IsOnsetDateEst: false, MonthsPregnant: null, MonthsPostCalving: null,
            OnsetAgeInMonths: null, SlaughterDate: null, AlternateDiagnosis: null,
            LabComment: null, CaseType: CaseType);

        var command = new UpdateCaseDetailsCommand(
            addCase, batch.BatchId,
            Clinical: null, Bab: null,
            Feeds: [], Tests: [], OtherOwners: [],
            DamSire: null, ClinicalVisits: []);

        var result = await _cases.CreateCaseAsync(command, userId);

        if (result != AddCaseResult.Success)
        {
            var message = result switch
            {
                AddCaseResult.DuplicateRbse => $"Case '{Rbse}' already exists.",
                AddCaseResult.InsertError => "Database error during insert.",
                AddCaseResult.AuditLogError => "Audit log error during create.",
                _ => $"Failed to create case: {result}"
            };
            ModelState.AddModelError("", message);
            return Page();
        }

        TempData["SuccessMessage"] = $"Case {Rbse} created successfully.";
        return RedirectToPage("/Case/Details", new { rbse = Rbse.Trim() });
    }
}
