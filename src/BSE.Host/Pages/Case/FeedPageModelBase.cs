using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

/// <summary>State shared by the feed add and edit pages.</summary>
public abstract class FeedPageModelBase(ILookupDataService lookups) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public IEnumerable<LookupItem> RationTypes { get; private set; } = [];
    public IEnumerable<LookupItem> Suppliers { get; private set; } = [];

    /// <summary>Field-keyed validation messages, mirroring the legacy per-field markers.</summary>
    public IDictionary<string, string> FieldErrors { get; protected set; } = new Dictionary<string, string>();

    protected async Task LoadLookupsAsync()
    {
        var rtTask = lookups.GetLookupAsync(LookupTableId.RationType);
        var supTask = lookups.GetLookupAsync(LookupTableId.Supplier);
        await Task.WhenAll(rtTask, supTask);
        RationTypes = await rtTask;
        Suppliers = await supTask;
    }
}
