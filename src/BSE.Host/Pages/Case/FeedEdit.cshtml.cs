using BSE.Host.Helpers;
using BSE.Infrastructure;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class FeedEditModel(
    IFeedRepository feedRepository,
    ICaseService caseService,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int FeedId { get; set; }

    [BindProperty]
    public EditFeedViewModel Feed { get; set; } = new();

    public IEnumerable<LookupItem> RationTypes { get; private set; } = [];
    public IEnumerable<LookupItem> Suppliers { get; private set; } = [];

    public IDictionary<string, string> FieldErrors { get; private set; } = new Dictionary<string, string>();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();

        var rbse = RbseHelper.ParseToRaw(Rbse);
        var existingFeed = (await feedRepository.GetByRbseAsync(rbse)).FirstOrDefault(f => f.Id == FeedId);
        if (existingFeed is null)
            return RedirectToPage("/Case/Feeds", new { rbse = Rbse });

        Feed = new EditFeedViewModel
        {
            Id = existingFeed.Id,
            Rbse = existingFeed.Rbse,
            YearFrom = existingFeed.YearFrom,
            YearTo = existingFeed.YearTo,
            RationType = existingFeed.RationType,
            RationName = existingFeed.RationName,
            IsPrePurchase = existingFeed.IsPrePurchase,
            SupplierId = existingFeed.SupplierId,
            RowStamp = Convert.ToBase64String(existingFeed.RowStamp ?? [])
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();

        var rbse = RbseHelper.ParseToRaw(Rbse);
        var caseRecord = await caseService.GetCaseAsync(rbse);

        FieldErrors = FeedValidation.Validate(
            new FeedValidation.Input(Feed.YearFrom, Feed.YearTo, Feed.RationType, Feed.SupplierId),
            caseRecord);

        if (FieldErrors.Count > 0)
            return Page();

        var rowStamp = string.IsNullOrWhiteSpace(Feed.RowStamp)
            ? []
            : Convert.FromBase64String(Feed.RowStamp);

        var command = new EditFeedCommand(
            Id: FeedId,
            Rbse: rbse,
            YearFrom: Feed.YearFrom,
            YearTo: Feed.YearTo,
            RationType: Feed.RationType!,
            SupplierId: Feed.SupplierId,
            RationName: string.IsNullOrWhiteSpace(Feed.RationName) ? null : Feed.RationName,
            IsPrePurchase: Feed.IsPrePurchase,
            RowStamp: rowStamp);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await feedRepository.EditAsync(command, conn, tx);
        tx.Commit();

        TempData["Success"] = "Feed record updated.";
        return RedirectToPage("/Case/Feeds", new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var rtTask = lookups.GetLookupAsync(LookupTableId.RationType);
        var supTask = lookups.GetLookupAsync(LookupTableId.Supplier);
        await Task.WhenAll(rtTask, supTask);
        RationTypes = await rtTask;
        Suppliers = await supTask;
    }

    public sealed class EditFeedViewModel
    {
        public int Id { get; set; }
        public string Rbse { get; set; } = string.Empty;
        public short? YearFrom { get; set; }
        public short? YearTo { get; set; }
        public string? RationType { get; set; }
        public string? RationName { get; set; }
        public int? SupplierId { get; set; }
        public bool IsPrePurchase { get; set; }
        public string RowStamp { get; set; } = string.Empty;
    }
}
