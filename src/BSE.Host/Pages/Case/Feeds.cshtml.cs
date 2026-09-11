using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class FeedsModel(
    IFeedRepository feedRepository,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? SortColumn { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDesc { get; set; }

    public IReadOnlyList<CaseFeedRecord> Feeds { get; private set; } = [];
    public IEnumerable<LookupItem> RationTypes { get; private set; } = [];
    public IEnumerable<LookupItem> Suppliers { get; private set; } = [];
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteFeedAsync(int feedId)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await feedRepository.DeleteAsync(feedId, conn, tx);
        tx.Commit();

        TempData["Success"] = "Feed record deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    private async Task LoadAsync()
    {
        var feedsTask  = feedRepository.GetByRbseAsync(Rbse);
        var batchTask  = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var rtTask     = lookups.GetLookupAsync(LookupTableId.RationType);
        var supTask    = lookups.GetLookupAsync(LookupTableId.Supplier);

        await Task.WhenAll(feedsTask, batchTask, rtTask, supTask);

        Feeds = SortFeeds(await feedsTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        RationTypes = await rtTask;
        Suppliers = await supTask;
    }

    private IReadOnlyList<CaseFeedRecord> SortFeeds(IReadOnlyList<CaseFeedRecord> feeds)
    {
        IEnumerable<CaseFeedRecord> q = feeds;
        q = SortColumn switch
        {
            "YearFrom"          => SortDesc ? q.OrderByDescending(f => f.YearFrom)          : q.OrderBy(f => f.YearFrom),
            "YearTo"            => SortDesc ? q.OrderByDescending(f => f.YearTo)            : q.OrderBy(f => f.YearTo),
            "RationDescription" => SortDesc ? q.OrderByDescending(f => f.RationDescription) : q.OrderBy(f => f.RationDescription),
            "SupplierName"      => SortDesc ? q.OrderByDescending(f => f.SupplierName)      : q.OrderBy(f => f.SupplierName),
            "RationName"        => SortDesc ? q.OrderByDescending(f => f.RationName)        : q.OrderBy(f => f.RationName),
            "IsPrePurchase"     => SortDesc ? q.OrderByDescending(f => f.IsPrePurchase)     : q.OrderBy(f => f.IsPrePurchase),
            _                   => q.OrderBy(f => f.YearFrom).ThenBy(f => f.YearTo)
        };
        return q.ToList().AsReadOnly();
    }
}
