using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class FeedAddModel(
    IFeedRepository feedRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public IEnumerable<LookupItem> RationTypes { get; private set; } = [];
    public IEnumerable<LookupItem> Suppliers { get; private set; } = [];
    public string SpolSiteUrl { get; private set; } = string.Empty;

    [BindProperty]
    public NewFeedViewModel NewFeed { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFeed.RationType))
        {
            ModelState.AddModelError(string.Empty, "Ration type is required.");
            await LoadLookupsAsync();
            return Page();
        }

        var command = new AddFeedCommand(
            Rbse: Rbse,
            YearFrom: NewFeed.YearFrom,
            YearTo: NewFeed.YearTo,
            RationType: NewFeed.RationType,
            SupplierId: NewFeed.SupplierId,
            RationName: string.IsNullOrWhiteSpace(NewFeed.RationName) ? null : NewFeed.RationName,
            IsPrePurchase: NewFeed.IsPrePurchase);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await feedRepository.AddAsync(command, conn, tx);
        tx.Commit();

        TempData["Success"] = "Feed record added.";
        return RedirectToPage("/Case/Feeds", new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var rtTask  = lookups.GetLookupAsync(LookupTableId.RationType);
        var supTask = lookups.GetLookupAsync(LookupTableId.Supplier);
        await Task.WhenAll(rtTask, supTask);
        RationTypes = await rtTask;
        Suppliers   = await supTask;
    }

    public class NewFeedViewModel
    {
        public short? YearFrom { get; set; }
        public short? YearTo { get; set; }
        public string? RationType { get; set; }
        public string? RationName { get; set; }
        public int? SupplierId { get; set; }
        public bool IsPrePurchase { get; set; }
    }
}
