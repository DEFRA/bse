using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize]
public class PickSupplierModel(
    ILookupRepository lookupRepository,
    IEditableLookupAdminService editableLookupAdminService) : PageModel
{
    public const int PageSize = 10;

    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string Name { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string? SortColumn { get; set; }
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty] public string NewSupplierDetails { get; set; } = string.Empty;

    public IReadOnlyList<LuSupplier> Suppliers { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; } = 1;
    public bool CanCreateNew => !string.IsNullOrWhiteSpace(Name);

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HasAccess())
            return RedirectToPage("/Home");

        var search = (Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage("/Case/Feeds", new { rbse = RbseHelper.ParseToRaw(Rbse) });

        Rbse = RbseHelper.ParseToRaw(Rbse);
        Name = search;

        var all = string.IsNullOrWhiteSpace(search)
            ? Sort((await lookupRepository.GetSuppliersAsync()).ToList())
            : Sort((await lookupRepository.GetPossibleSuppliersAsync(search.ToUpperInvariant())).ToList());
        TotalCount = all.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        Suppliers = all.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostUseSelectedAsync(int id, string? selectedName)
    {
        if (!HasAccess())
            return RedirectToPage("/Home");

        var selected = Suppliers.FirstOrDefault(s => s.Id == id);
        if (selected is null)
        {
            var search = (Name ?? string.Empty).Trim();
            var all = string.IsNullOrWhiteSpace(search)
                ? (await lookupRepository.GetSuppliersAsync()).ToList()
                : (await lookupRepository.GetPossibleSuppliersAsync(search.ToUpperInvariant())).ToList();
            selected = all.FirstOrDefault(s => s.Id == id);
        }

        if (selected is null)
        {
            ModelState.AddModelError(string.Empty, "Selected supplier could not be found.");
            await OnGetAsync();
            return Page();
        }

        var rbse = RbseHelper.ParseToRaw(Rbse);
        return RedirectToPage("/Case/Feeds", new
        {
            rbse,
            pickedSupplierId = selected.Id,
            pickedSupplierName = selectedName ?? selected.Name
        });
    }

    public async Task<IActionResult> OnPostNewAsync()
    {
        if (!HasAccess())
            return RedirectToPage("/Home");

        Rbse = RbseHelper.ParseToRaw(Rbse);
        Name = (Name ?? string.Empty).Trim();
        NewSupplierDetails = (NewSupplierDetails ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(Name))
        {
            await OnGetAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewSupplierDetails))
        {
            ModelState.AddModelError(nameof(NewSupplierDetails), "Enter supplier details.");
            await OnGetAsync();
            return Page();
        }

        await editableLookupAdminService.AddSupplierAsync(Name, NewSupplierDetails);

        var possible = (await lookupRepository.GetPossibleSuppliersAsync(Name.ToUpperInvariant())).ToList();
        var created = possible
            .Where(s => string.Equals(s.Name, Name, StringComparison.OrdinalIgnoreCase)
                     && string.Equals((s.Details ?? string.Empty).Trim(), NewSupplierDetails, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Id)
            .FirstOrDefault();

        if (created is null)
        {
            created = possible
                .Where(s => string.Equals(s.Name, Name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Id)
                .FirstOrDefault();
        }

        if (created is null)
        {
            ModelState.AddModelError(string.Empty, "Supplier was created but could not be selected. Please choose it from the table.");
            await OnGetAsync();
            return Page();
        }

        return RedirectToPage("/Case/Feeds", new
        {
            rbse = Rbse,
            pickedSupplierId = created.Id,
            pickedSupplierName = created.Name
        });
    }

    public IActionResult OnPostCancel()
    {
        if (!HasAccess())
            return RedirectToPage("/Home");

        return RedirectToPage("/Case/Feeds", new { rbse = RbseHelper.ParseToRaw(Rbse), resetSupplier = true });
    }

    private bool HasAccess() => User.IsInRole("DataEntry") && User.IsInRole("VLAAccess");

    private IReadOnlyList<LuSupplier> Sort(IReadOnlyList<LuSupplier> rows)
    {
        IEnumerable<LuSupplier> q = rows;
        q = SortColumn switch
        {
            "Details" => SortDesc ? q.OrderByDescending(s => s.Details) : q.OrderBy(s => s.Details),
            "Name" => SortDesc ? q.OrderByDescending(s => s.Name) : q.OrderBy(s => s.Name),
            _ => q.OrderBy(s => s.Name)
        };

        return q.ToList();
    }
}
