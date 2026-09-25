using BSE.Host.Services;
using BSE.Host.ModelBinding;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
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
    private readonly IFarmService _farms;
    private readonly ILookupDataService _lookups;

    public NewModel(
        ICaseService cases, IBatchService batch, ICurrentUserService currentUser,
        IFarmService farms, ILookupDataService lookups)
    {
        _cases = cases;
        _batch = batch;
        _currentUser = currentUser;
        _farms = farms;
        _lookups = lookups;
    }

    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = "";
    [BindProperty] public string Cphh { get; set; } = "";
    [BindProperty(SupportsGet = true)] public short? BatchYear { get; set; }
    [BindProperty(SupportsGet = true)] public int? BatchNumber { get; set; }
    [BindProperty] public string? Survey { get; set; }
    [BindProperty] public string? Sex { get; set; }
    [BindProperty] public string? Breed { get; set; }
    [BindProperty] public string? EartagCountry { get; set; }
    [BindProperty] public string? EartagHerdmark { get; set; }
    [BindProperty] public string? Eartag { get; set; }
    [BindProperty, ModelBinder(BinderType = typeof(MojDateModelBinder))] public DateTime? BirthDate { get; set; }
    [BindProperty, ModelBinder(BinderType = typeof(MojDateModelBinder))] public DateTime? FormADate { get; set; }
    [BindProperty] public string? Fate { get; set; }
    [BindProperty] public string? Origin { get; set; }
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string? CaseType { get; set; }

    // ── Farm details — only required when the CPHH has no existing farm record
    // (legacy CaseEntryFarm.aspx -> PickFarm.aspx -> NewFarm.aspx chain) ────────
    [BindProperty] public string? OwnerName { get; set; }
    [BindProperty] public string? Address1 { get; set; }
    [BindProperty] public string? Address2 { get; set; }
    [BindProperty] public string? Address3 { get; set; }
    [BindProperty] public string? Postcode { get; set; }
    [BindProperty] public string? Parish { get; set; }
    [BindProperty] public string? County { get; set; }
    [BindProperty] public string? Aho { get; set; }
    [BindProperty] public int? AdnsRegionId { get; set; }

    /// <summary>True once a Farm lookup has confirmed the CPHH has no existing farm — shows the farm fields.</summary>
    public bool RequireFarmDetails { get; private set; }

    public IReadOnlyList<LookupItem> CountyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> AhoOptions { get; private set; } = [];
    public IReadOnlyList<LuADNSRegion> AdnsOptions { get; private set; } = [];

    public async Task OnGetAsync(string? cphh = null)
    {
        Cphh = cphh ?? "";
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse))
            ModelState.AddModelError(nameof(Rbse), "Enter an RBSE.");

        if (string.IsNullOrWhiteSpace(Cphh))
            ModelState.AddModelError(nameof(Cphh), "Enter a CPHH.");

        if (string.IsNullOrWhiteSpace(EartagCountry) && string.IsNullOrWhiteSpace(EartagHerdmark) && string.IsNullOrWhiteSpace(Eartag))
            ModelState.AddModelError(nameof(Eartag), "Enter an eartag.");

        if (FormADate is null)
            ModelState.AddModelError(nameof(FormADate), "Enter a Form A date.");

        var normalisedCphh = CphhNormalizer.Normalize(Cphh);
        Cphh = normalisedCphh;

        if (!string.IsNullOrWhiteSpace(normalisedCphh) && normalisedCphh.Length != 11)
            ModelState.AddModelError(nameof(Cphh), "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");

        FarmRecord? farm = null;
        if (!string.IsNullOrWhiteSpace(normalisedCphh))
            farm = await _farms.GetByCphhAsync(normalisedCphh);

        RequireFarmDetails = farm is null;

        if (RequireFarmDetails)
        {
            if (string.IsNullOrWhiteSpace(OwnerName))
                ModelState.AddModelError(nameof(OwnerName), "Enter an owner name for the farm.");
            if (string.IsNullOrWhiteSpace(Address1))
                ModelState.AddModelError(nameof(Address1), "Enter the first line of the farm address.");
            if (string.IsNullOrWhiteSpace(Parish))
                ModelState.AddModelError(nameof(Parish), "Enter a parish for the farm.");
            if (string.IsNullOrWhiteSpace(County))
                ModelState.AddModelError(nameof(County), "Specify a county for the farm.");
            if (string.IsNullOrWhiteSpace(Aho))
                ModelState.AddModelError(nameof(Aho), "Specify an AHO for the farm.");
            if (AdnsRegionId is null)
                ModelState.AddModelError(nameof(AdnsRegionId), "Specify an ADNS region for the farm.");
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return Page();
        }

        int batchId;

        // If a batch year/number were supplied (via Home redirect), use that batch if it exists.
        if (BatchYear.HasValue && BatchNumber.HasValue)
        {
            var existing = await _batch.GetBatchIdAsync(BatchYear.Value, BatchNumber.Value);
            if (existing is null)
            {
                ModelState.AddModelError(string.Empty, $"Batch {BatchYear}/{BatchNumber} was not found.");
                await LoadLookupsAsync();
                return Page();
            }
            batchId = existing.Value;
        }
        else
        {
            var batch = await _batch.GetOrCreateBatchNumberAsync();
            batchId = batch.BatchId;
        }

        var userId = await _currentUser.GetUserIdAsync();

        if (RequireFarmDetails)
        {
            await _farms.AddAsync(new AddFarmCommand(
                CPHH: normalisedCphh,
                OwnerName: OwnerName, Address1: Address1, Address2: Address2, Address3: Address3,
                Postcode: Postcode, Parish: Parish, District: null, County: County,
                CorrespondenceAddress1: null, CorrespondenceAddress2: null, CorrespondenceAddress3: null,
                CorrespondencePostcode: null, MapReference: null,
                Herdmark1: null, Herdmark2: null, Herdmark3: null,
                NumericHerdmark1: null, NumericHerdmark2: null,
                AHO: Aho, HerdType: null, PedigreeType: null, IsDealer: false,
                ADNSRegionID: AdnsRegionId), userId);
        }

        var addCase = new AddCaseCommand(
            Rbse: Rbse.Trim(), Cphh: normalisedCphh,
            EartagCountry: EartagCountry, EartagHerdmark: EartagHerdmark, Eartag: Eartag,
            PreviousEartag: null, Bse1ReceivedDate: null, FormADate: FormADate,
            FormAResubmittedDate: null, FormBDate: null, Fate: Fate, FormCDate: null,
            IsPurchaserBse1Received: false, IsBreederBse1Received: false,
            IsVendor1Bse1Received: false, IsHomebredBse1Received: false,
            IsSummarySheetReceived: false, IsPaperworkComplete: false,
            ReportedLocation: null, Survey: Survey, Notes: Notes,
            BirthDate: BirthDate, IsBirthDateEst: BirthDate.HasValue ? false : null, DamStatus: null,
            BirthDateSource: null, ValuationAge: null, Sex: Sex, Breed: Breed,
            Origin: Origin, PurchaseDate: null, PurchaseAgeInMonths: null,
            PurchasedCounty: null, HerdEntryDate: null, OnsetDate: null,
            IsOnsetDateEst: null, MonthsPregnant: null, MonthsPostCalving: null,
            OnsetAgeInMonths: null, SlaughterDate: null, AlternateDiagnosis: null,
            LabComment: null, CaseType: CaseType);

        var command = new UpdateCaseDetailsCommand(
            addCase, batchId,
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
            await LoadLookupsAsync();
            return Page();
        }

        TempData["SuccessMessage"] = $"Case {Rbse} created successfully.";
        return RedirectToPage("/Case/Farm", new { rbse = Rbse.Trim() });
    }

    private async Task LoadLookupsAsync()
    {
        CountyOptions = (await _lookups.GetLookupAsync(LookupTableId.BSECounty)).ToList();
        AhoOptions = (await _lookups.GetLookupAsync(LookupTableId.AHO)).ToList();
        AdnsOptions = (await _lookups.GetADNSRegionsAsync()).ToList();
    }
}
