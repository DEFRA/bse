using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.SharedKernel;
using BSE.Host.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize]
public class PickFarmModel(IFarmService farmService) : PageModel
{
    private const string HomePage = "/Home";

    public const int PageSize = 10;

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Cphh { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? SortColumn { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDesc { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<FarmSummaryRecord> Farms { get; private set; } = [];
    public int TotalFarmCount { get; private set; }
    public int TotalPages { get; private set; } = 1;

    public string FormattedRequestedCphh { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HasLegacyPickFarmAccess())
            return RedirectToPage(HomePage);

        var normalized = CphhNormalizer.Normalize(Cphh);
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage(HomePage);

        Cphh = normalized;
        FormattedRequestedCphh = BseFormat.FormatCphh(Cphh) ?? Cphh;

        var cphPrefix = normalized.Length <= 9 ? normalized : normalized[..9];
        var farms = (await farmService.GetByCphAsync(cphPrefix)).ToList();
        var sorted = Sort(farms);

        TotalFarmCount = sorted.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalFarmCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        Farms = sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList().AsReadOnly();

        return Page();
    }

    public async Task<IActionResult> OnPostUseSelectedAsync(string? selectedFarmCphh)
    {
        if (!HasLegacyPickFarmAccess())
            return RedirectToPage(HomePage);

        var normalized = CphhNormalizer.Normalize(Cphh);
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage(HomePage);

        if (string.IsNullOrWhiteSpace(selectedFarmCphh))
        {
            var cphPrefix = normalized.Length <= 9 ? normalized : normalized[..9];
            var farms = (await farmService.GetByCphAsync(cphPrefix)).ToList();
            var sorted = Sort(farms);
            TotalFarmCount = sorted.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalFarmCount / (double)PageSize));
            PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
            Farms = sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList().AsReadOnly();
            FormattedRequestedCphh = BseFormat.FormatCphh(normalized) ?? normalized;
            return Page();
        }

        var selected = CphhNormalizer.Normalize(selectedFarmCphh);
        if (string.IsNullOrWhiteSpace(selected))
        {
            var cphPrefix = normalized.Length <= 9 ? normalized : normalized[..9];
            var farms = (await farmService.GetByCphAsync(cphPrefix)).ToList();
            var sorted = Sort(farms);
            TotalFarmCount = sorted.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalFarmCount / (double)PageSize));
            PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
            Farms = sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList().AsReadOnly();
            FormattedRequestedCphh = BseFormat.FormatCphh(normalized) ?? normalized;
            return Page();
        }

        return RedirectToPage("/Case/Farm", new { rbse = Rbse, selectedCphh = selected });
    }

    public IActionResult OnPostNewAsync()
    {
        if (!HasLegacyPickFarmAccess())
            return RedirectToPage(HomePage);

        var normalized = CphhNormalizer.Normalize(Cphh);
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage(HomePage);

        return RedirectToPage("/Farm/New", new { returnRbse = Rbse, returnCphh = normalized, returnToCaseFarm = true });
    }

    public IActionResult OnPostCancelAsync()
    {
        if (!HasLegacyPickFarmAccess())
            return RedirectToPage(HomePage);

        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage(HomePage);

        return RedirectToPage("/Case/Farm", new { rbse = Rbse, newCphh = CphhNormalizer.Normalize(Cphh) });
    }

    private bool HasLegacyPickFarmAccess()
    {
        var isDefraViewer = User.IsInRole("DEFRAAccess") && !User.IsInRole("DataEntry");
        var isVlaDataEntry = User.IsInRole("VLAAccess") && !User.IsInRole("VLAMaintenance");
        return !isDefraViewer && !isVlaDataEntry;
    }

    private List<FarmSummaryRecord> Sort(IReadOnlyList<FarmSummaryRecord> farms)
    {
        IEnumerable<FarmSummaryRecord> q = farms;
        q = SortColumn switch
        {
            "CPHH" => SortDesc ? q.OrderByDescending(f => f.CPHH) : q.OrderBy(f => f.CPHH),
            "OwnerName" => SortDesc ? q.OrderByDescending(f => f.OwnerName) : q.OrderBy(f => f.OwnerName),
            "Address1" => SortDesc ? q.OrderByDescending(f => f.Address1) : q.OrderBy(f => f.Address1),
            _ => q.OrderBy(f => f.CPHH)
        };

        return q.ToList();
    }
}
