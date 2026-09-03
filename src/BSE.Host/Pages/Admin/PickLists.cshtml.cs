using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "PickListAccess")]
public class PickListsModel(IEditableLookupAdminService lookupAdminService) : PageModel
{
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IReadOnlyList<EditableLookup> Lookups { get; private set; } = [];

    public int TotalCount => Lookups.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<EditableLookup> PagedLookups =>
        ApplySorting(Lookups).Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public async Task<IActionResult> OnGetAsync()
    {
        Lookups = (await lookupAdminService.GetEditableLookupsAsync()).ToList();
        return Page();
    }

    private IEnumerable<EditableLookup> ApplySorting(IEnumerable<EditableLookup> lookups) => SortColumn switch
    {
        "Table" => SortDesc ? lookups.OrderByDescending(l => l.TableName) : lookups.OrderBy(l => l.TableName),
        "Description" => SortDesc ? lookups.OrderByDescending(l => l.Description) : lookups.OrderBy(l => l.Description),
        "Id" => SortDesc ? lookups.OrderByDescending(l => l.Id) : lookups.OrderBy(l => l.Id),
        _ => lookups.OrderBy(l => l.Id),
    };
}
