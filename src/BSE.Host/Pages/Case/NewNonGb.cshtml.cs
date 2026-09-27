using BSE.Host.Services;
using BSE.Modules.BsessIntegration.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

// Legacy NonGBCaseCreation.aspx.vb EnableControls() redirects every group except VLA Maintenance away.
[Authorize(Policy = "VLAMaintenance")]
public class NewNonGbModel : PageModel
{
    private readonly ICaseService _cases;
    private readonly ICurrentUserService _currentUser;
    private readonly IFarmService _farmService;
    private readonly ILookupDataService _lookups;
    private readonly BSE.Modules.ReferenceData.Services.IGeoLookupService _geoLookup;
    private readonly IBsessCheckService _bsessCheckService;
    private readonly ILogger<NewNonGbModel> _logger;

    public NewNonGbModel(
        ICaseService cases, ICurrentUserService currentUser,
        IFarmService farmService,
        ILookupDataService lookups, BSE.Modules.ReferenceData.Services.IGeoLookupService geoLookup,
        IBsessCheckService bsessCheckService,
        ILogger<NewNonGbModel> logger)
    {
        _cases = cases;
        _currentUser = currentUser;
        _farmService = farmService;
        _lookups = lookups;
        _geoLookup = geoLookup;
        _bsessCheckService = bsessCheckService;
        _logger = logger;
    }

    // Pre-filled from query string when redirected from Lookup
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = "";

    [BindProperty] public string Cphh { get; set; } = "";
    [BindProperty] public string? EartagCountry { get; set; }
    [BindProperty] public string? EartagHerdmark { get; set; }
    [BindProperty] public string? Eartag { get; set; }
    [BindProperty] public string? Fate { get; set; } = "SL";
    [BindProperty] public string? FinalResult { get; set; } = "NE";
    [BindProperty] public DateTime? FinalResultDate { get; set; }
    [BindProperty] public DateTime? SlaughterDate { get; set; }
    [BindProperty] public string? OwnerName { get; set; }
    [BindProperty] public string? Address1 { get; set; }
    [BindProperty] public string? Address2 { get; set; }
    [BindProperty] public string? Address3 { get; set; }
    [BindProperty] public string? Postcode { get; set; }
    [BindProperty] public string? County { get; set; }
    [BindProperty] public string? Herdmark1 { get; set; }
    [BindProperty] public string? NumericHerdmark1 { get; set; }
    [BindProperty] public DateTime? RbseDate { get; set; }
    [BindProperty] public string? Barcode { get; set; }
    [BindProperty] public string? AhfReference { get; set; }

    /// <summary>Mirrors legacy EnableFarmControls/DisableFarmControls: farm fields are only
    /// editable once a CPHH has been entered, recomputed on every postback.</summary>
    public bool FarmControlsEnabled => !string.IsNullOrWhiteSpace(Cphh);

    /// <summary>Set after Look Up runs, so the view can confirm whether an existing farm was found.</summary>
    public bool? FarmFoundOnLookup { get; private set; }

    public IReadOnlyList<LuCaseFate> FateOptions { get; private set; } = [];
    public IReadOnlyList<LuTestResult> FinalResultOptions { get; private set; } = [];
    public IReadOnlyList<LuBSECounty> CountyOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse))
        {
            TempData["ErrorMessage"] = "No RBSE was supplied. Look up an RBSE from the home page to create a non-GB case.";
            return RedirectToPage("/Home");
        }

        RbseDate ??= DateTime.Today;
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse))
        {
            TempData["ErrorMessage"] = "No RBSE was supplied. Look up an RBSE from the home page to create a non-GB case.";
            return RedirectToPage("/Home");
        }

        if (string.IsNullOrWhiteSpace(EartagCountry)
            && string.IsNullOrWhiteSpace(EartagHerdmark)
            && string.IsNullOrWhiteSpace(Eartag))
            ModelState.AddModelError(nameof(Eartag), "You need to enter an Eartag.");
        else
        {
            // Legacy ThreePartEartag.ascx.vb Validate() checks the eartag against BSELib.Eartag's
            // country-specific format/checksum rules, not just presence.
            var eartagError = EartagValidator.Validate(EartagCountry, EartagHerdmark, Eartag);
            if (eartagError is not null)
                ModelState.AddModelError(nameof(Eartag), eartagError);
        }
        if (string.IsNullOrWhiteSpace(Fate))
            ModelState.AddModelError(nameof(Fate), "You must choose a Fate.");
        if (string.IsNullOrWhiteSpace(FinalResult))
            ModelState.AddModelError(nameof(FinalResult), "You must choose a Final Result.");
        if (FinalResultDate is null)
            ModelState.AddModelError(nameof(FinalResultDate), "You need to enter a Final Result Date.");
        if (SlaughterDate is null)
            ModelState.AddModelError(nameof(SlaughterDate), "You need to enter a Slaughter Date.");

        // Legacy DatesValid(): Slaughter Date cannot be later than today, and Final Result Date
        // must fall between the Slaughter Date and today.
        if (SlaughterDate is not null && SlaughterDate.Value.Date > DateTime.Today)
            ModelState.AddModelError(nameof(SlaughterDate), "Slaughter Date is later than today.");

        if (FinalResultDate is not null && SlaughterDate is not null
            && (FinalResultDate.Value.Date < SlaughterDate.Value.Date || FinalResultDate.Value.Date > DateTime.Today))
            ModelState.AddModelError(nameof(FinalResultDate), "Final Result date must be between the Slaughter Date and today.");

        var normalizedCphh = CphhNormalizer.Normalize(Cphh);
        Cphh = normalizedCphh;

        if (string.IsNullOrWhiteSpace(normalizedCphh))
            ModelState.AddModelError(nameof(Cphh), "You need to enter a CPHH.");
        else if (normalizedCphh.Length != 11)
            ModelState.AddModelError(nameof(Cphh), "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");
        else if (!normalizedCphh.StartsWith("009999", StringComparison.Ordinal))
            ModelState.AddModelError(nameof(Cphh), "Please enter the CPHH of a non-GB farm.");
        // Legacy rfvOwnerName/rfvAddress1 are enabled together with the farm fields once a CPHH is entered.
        if (string.IsNullOrWhiteSpace(OwnerName))
            ModelState.AddModelError(nameof(OwnerName), "You must enter an Owner Name.");
        if (string.IsNullOrWhiteSpace(Address1))
            ModelState.AddModelError(nameof(Address1), "You must enter an Address.");
        if (string.IsNullOrWhiteSpace(County))
            ModelState.AddModelError(nameof(County), "You must select a County.");
        if (!string.IsNullOrWhiteSpace(NumericHerdmark1)
            && (NumericHerdmark1.Trim().Length != 6 || !NumericHerdmark1.Trim().All(char.IsDigit)))
            ModelState.AddModelError(nameof(NumericHerdmark1), "Numeric herdmark must be 6 digits.");

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return Page();
        }

        // Legacy CreateCase() always passes today's date for RBSEDate.
        RbseDate ??= DateTime.Today;

        var userId = await _currentUser.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(Barcode) || string.IsNullOrWhiteSpace(AhfReference))
        {
            try
            {
                var bsessResult = await _bsessCheckService.GetCheckByRbseAsync(Rbse.Trim());
                Barcode ??= bsessResult?.Barcode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve BSESS barcode for non-GB case creation RBSE {Rbse}", Rbse);
            }
        }

        var command = new AddNonGbCaseCommand(
            Rbse: Rbse.Trim(),
            Cphh: normalizedCphh,
            EartagCountry: EartagCountry,
            EartagHerdmark: EartagHerdmark,
            Eartag: Eartag,
            Fate: Fate,
            FinalResult: FinalResult,
            FinalResultDate: FinalResultDate,
            SlaughterDate: SlaughterDate,
            OwnerName: OwnerName,
            Address1: Address1,
            Address2: Address2,
            Address3: Address3,
            Postcode: Postcode,
            County: County,
            Herdmark1: Herdmark1,
            NumericHerdmark1: NumericHerdmark1,
            RbseDate: RbseDate,
            Barcode: Barcode,
            AhfReference: AhfReference);

        AddNonGbCaseResult result;
        try
        {
            result = await _cases.CreateNonGbCaseAsync(command, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddNonGBCase stored procedure threw an exception");
            ModelState.AddModelError(string.Empty, "Failed to save new case details.");
            await LoadLookupsAsync();
            return Page();
        }

        if (result != AddNonGbCaseResult.Success)
        {
            var message = result switch
            {
                AddNonGbCaseResult.AlreadyExists => $"Case '{Rbse}' already exists.",
                AddNonGbCaseResult.FarmCreateError or AddNonGbCaseResult.FarmCreateAuditError
                    or AddNonGbCaseResult.FarmUpdateError or AddNonGbCaseResult.FarmUpdateAuditError
                    => "An error occurred saving the farm record.",
                AddNonGbCaseResult.CaseInsertError or AddNonGbCaseResult.CaseAuditError
                    => "An error occurred saving the case record.",
                AddNonGbCaseResult.CaseWorkInsertError => "An error occurred saving the casework record.",
                _ => $"Failed to create case (error {(int)result})."
            };
            ModelState.AddModelError(string.Empty, message);
            await LoadLookupsAsync();
            return Page();
        }

        TempData["SuccessMessage"] = $"Non-GB case {Rbse} created successfully.";
        return RedirectToPage("/Case/Farm", new { rbse = Rbse.Trim() });
    }

    // Legacy btnLookUp_Click: validates the CPHH, then LoadFarmDetails()/LoadCaseDetails()
    // pre-fill Address1-3/Postcode/OwnerName/County from an existing farm (herdmark is left blank).
    public async Task<IActionResult> OnPostLookupAsync()
    {
        FarmFoundOnLookup = null;

        if (string.IsNullOrWhiteSpace(Rbse))
        {
            TempData["ErrorMessage"] = "No RBSE was supplied. Look up an RBSE from the home page to create a non-GB case.";
            return RedirectToPage("/Home");
        }

        if (string.IsNullOrWhiteSpace(Cphh))
        {
            ModelState.AddModelError(nameof(Cphh), "You need to enter a CPHH.");
            await LoadLookupsAsync();
            return Page();
        }

        var normalized = CphhNormalizer.Normalize(Cphh);
        Cphh = normalized;

        if (normalized.Length != 11)
        {
            ModelState.AddModelError(nameof(Cphh), "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");
            await LoadLookupsAsync();
            return Page();
        }

        if (!normalized.StartsWith("009999", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(Cphh), "Please enter the CPHH of a non-GB farm.");
            await LoadLookupsAsync();
            return Page();
        }

        var farm = await _farmService.GetByCphhAsync(normalized);
        FarmFoundOnLookup = farm is not null;

        // Posted form values in ModelState take precedence over updated bound properties.
        // Clear ModelState after successful lookup so populated farm values are rendered.
        ModelState.Clear();

        if (farm is not null)
        {
            OwnerName = farm.OwnerName?.Trim();
            Address1 = (string.IsNullOrWhiteSpace(farm.Address1) ? farm.CorrespondenceAddress1 : farm.Address1)?.Trim();
            Address2 = (string.IsNullOrWhiteSpace(farm.Address2) ? farm.CorrespondenceAddress2 : farm.Address2)?.Trim();
            Address3 = (string.IsNullOrWhiteSpace(farm.Address3) ? farm.CorrespondenceAddress3 : farm.Address3)?.Trim();
            Postcode = farm.Postcode;
            County = farm.County?.Trim();
            Herdmark1 = farm.Herdmark1?.Trim();
            NumericHerdmark1 = farm.NumericHerdmark1?.Trim();
        }

        await LoadLookupsAsync();
        return Page();
    }

    private async Task LoadLookupsAsync()
    {
        FateOptions = (await _lookups.GetCaseFatesAsync()).ToList();
        FinalResultOptions = (await _lookups.GetTestResultsAsync()).ToList();
        CountyOptions = (await _geoLookup.GetNonGBCountyAsync()).ToList();

        if (!string.IsNullOrWhiteSpace(County)
            && !CountyOptions.Any(x => string.Equals(x.Code, County, StringComparison.OrdinalIgnoreCase)))
        {
            var countyOption = CountyOptions.FirstOrDefault(x => string.Equals(x.Description, County, StringComparison.OrdinalIgnoreCase));
            if (countyOption is not null)
                County = countyOption.Code;
        }

        if (!string.IsNullOrWhiteSpace(Fate)
            && !FateOptions.Any(x => string.Equals(x.Code, Fate, StringComparison.OrdinalIgnoreCase)))
            Fate = "SL";

        if (!string.IsNullOrWhiteSpace(FinalResult)
            && !FinalResultOptions.Any(x => string.Equals(x.Code, FinalResult, StringComparison.OrdinalIgnoreCase)))
            FinalResult = "NE";
    }
}

