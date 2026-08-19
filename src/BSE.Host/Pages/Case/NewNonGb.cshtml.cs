using BSE.Host.Services;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class NewNonGbModel(
    ICaseService caseService,
    IFarmService farmService,
    IBatchService batch,
    ILookupDataService lookups,
    ICurrentUserService currentUser) : PageModel
{
    [BindProperty, Required(ErrorMessage = "RBSE number is required.")]
    public string Rbse { get; set; } = "";

    [BindProperty] public string? EartagCountry { get; set; }
    [BindProperty] public string? EartagHerdmark { get; set; }
    [BindProperty] public string? Eartag { get; set; }
    [BindProperty] public string? OwnerName { get; set; }
    [BindProperty] public string? Address1 { get; set; }
    [BindProperty] public string? Address2 { get; set; }
    [BindProperty] public string? Address3 { get; set; }
    [BindProperty] public string? Postcode { get; set; }
    [BindProperty] public string? Herdmark { get; set; }
    [BindProperty] public string? NumericHerdmark { get; set; }
    [BindProperty] public string? Fate { get; set; }
    [BindProperty] public string? FinalResult { get; set; }
    [BindProperty] public DateTime? FinalResultDate { get; set; }
    [BindProperty] public DateTime? SlaughterDate { get; set; }

    public IEnumerable<LuCaseFate> Fates { get; private set; } = [];
    public IEnumerable<LuTestResult> TestResults { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        Fate = "SL";
        FinalResult = "NE";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return Page();
        }

        // For non-GB cases, CPHH is constructed from the herdmark
        var cphh = $"NG/{Herdmark?.Trim() ?? "NONGB"}";

        // Ensure the non-GB farm exists (create a stub if not)
        var existingFarm = await farmService.GetByCphhAsync(cphh);
        if (existingFarm is null)
        {
            var farmCommand = new AddFarmCommand(
                CPHH: cphh,
                OwnerName: OwnerName,
                Address1: Address1, Address2: Address2, Address3: Address3,
                Postcode: Postcode,
                Parish: null, District: null, County: null,
                CorrespondenceAddress1: null, CorrespondenceAddress2: null,
                CorrespondenceAddress3: null, CorrespondencePostcode: null,
                MapReference: null,
                Herdmark1: Herdmark, Herdmark2: null, Herdmark3: null,
                NumericHerdmark1: NumericHerdmark, NumericHerdmark2: null,
                AHO: null, HerdType: null, PedigreeType: null,
                IsDealer: false, ADNSRegionID: null);

            var userId = await currentUser.GetUserIdAsync();
            await farmService.AddAsync(farmCommand, userId);
        }

        var batchRecord = await batch.GetOrCreateBatchNumberAsync();
        var addCase = new AddCaseCommand(
            Rbse: Rbse.Trim(), Cphh: cphh,
            EartagCountry: EartagCountry, EartagHerdmark: EartagHerdmark, Eartag: Eartag,
            PreviousEartag: null,
            Bse1ReceivedDate: null, FormADate: null, FormAResubmittedDate: null,
            FormBDate: null, Fate: Fate ?? "SL", FormCDate: null,
            IsPurchaserBse1Received: false, IsBreederBse1Received: false,
            IsVendor1Bse1Received: false, IsHomebredBse1Received: false,
            IsSummarySheetReceived: false, IsPaperworkComplete: false,
            ReportedLocation: null, Survey: null, Notes: null,
            BirthDate: null, IsBirthDateEst: null, DamStatus: null,
            BirthDateSource: null, ValuationAge: null,
            Sex: null, Breed: null, Origin: null,
            PurchaseDate: null, PurchaseAgeInMonths: null, PurchasedCounty: null,
            HerdEntryDate: null, OnsetDate: null, IsOnsetDateEst: null,
            MonthsPregnant: null, MonthsPostCalving: null, OnsetAgeInMonths: null,
            SlaughterDate: SlaughterDate,
            AlternateDiagnosis: null, LabComment: null, CaseType: null);

        var command = new UpdateCaseDetailsCommand(
            Case: addCase,
            BatchId: batchRecord.BatchId,
            Clinical: null, Bab: null,
            Feeds: [], Tests: [], OtherOwners: [],
            DamSire: null, ClinicalVisits: []);

        var uid = await currentUser.GetUserIdAsync();
        var result = await caseService.CreateCaseAsync(command, uid);

        if (result == AddCaseResult.Success)
        {
            TempData["Success"] = $"Non-GB case {Rbse} created.";
            return RedirectToPage("/Case/Details", new { rbse = Rbse.Trim() });
        }

        ModelState.AddModelError(string.Empty, result switch
        {
            AddCaseResult.DuplicateRbse => $"A case with RBSE '{Rbse}' already exists.",
            _ => $"Failed to create case: {result}"
        });

        await LoadLookupsAsync();
        return Page();
    }

    private async Task LoadLookupsAsync()
    {
        Fates = await lookups.GetCaseFatesAsync();
        TestResults = await lookups.GetTestResultsAsync();
    }
}
