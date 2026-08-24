using BSE.Host.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class NewNonGbModel : PageModel
{
    private readonly ICaseService _cases;
    private readonly ICurrentUserService _currentUser;

    public NewNonGbModel(ICaseService cases, ICurrentUserService currentUser)
    {
        _cases = cases;
        _currentUser = currentUser;
    }

    // Pre-filled from query string when redirected from Lookup
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = "";

    [BindProperty] public string Cphh { get; set; } = "";
    [BindProperty] public string? EartagCountry { get; set; }
    [BindProperty] public string? EartagHerdmark { get; set; }
    [BindProperty] public string? Eartag { get; set; }
    [BindProperty] public string? Fate { get; set; } = "SLOS";
    [BindProperty] public string? FinalResult { get; set; } = "NOT";
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

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Eartag))
            ModelState.AddModelError(nameof(Eartag), "Enter an eartag.");
        if (string.IsNullOrWhiteSpace(Fate))
            ModelState.AddModelError(nameof(Fate), "Enter a fate code.");
        if (string.IsNullOrWhiteSpace(FinalResult))
            ModelState.AddModelError(nameof(FinalResult), "Enter a final result code.");
        if (FinalResultDate is null)
            ModelState.AddModelError(nameof(FinalResultDate), "Enter a final result date.");
        if (SlaughterDate is null)
            ModelState.AddModelError(nameof(SlaughterDate), "Enter a slaughter date.");
        if (string.IsNullOrWhiteSpace(Cphh))
            ModelState.AddModelError(nameof(Cphh), "Enter a CPHH.");
        else if (!Cphh.Trim().StartsWith("00999", StringComparison.Ordinal))
            ModelState.AddModelError(nameof(Cphh), "CPHH for a non-GB farm must begin with 00999.");
        if (string.IsNullOrWhiteSpace(OwnerName))
            ModelState.AddModelError(nameof(OwnerName), "Enter an owner name.");
        if (string.IsNullOrWhiteSpace(Address1))
            ModelState.AddModelError(nameof(Address1), "Enter address line 1.");
        if (string.IsNullOrWhiteSpace(County))
            ModelState.AddModelError(nameof(County), "Enter a county or territory.");

        if (!ModelState.IsValid) return Page();

        var userId = await _currentUser.GetUserIdAsync();

        var command = new AddNonGbCaseCommand(
            Rbse: Rbse.Trim(),
            Cphh: Cphh.Trim(),
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

        var result = await _cases.CreateNonGbCaseAsync(command, userId);

        if (result != AddNonGbCaseResult.Success)
        {
            var message = result switch
            {
                AddNonGbCaseResult.AlreadyExists => $"Case '{Rbse}' already exists.",
                AddNonGbCaseResult.FarmCreateError or AddNonGbCaseResult.FarmUpdateError
                    => "An error occurred saving the farm record.",
                _ => $"Failed to create case (error {(int)result})."
            };
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }

        TempData["SuccessMessage"] = $"Non-GB case {Rbse} created successfully.";
        return RedirectToPage("/Case/Details", new { rbse = Rbse.Trim() });
    }
}
