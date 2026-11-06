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
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntryVla")]
public class FeedAddModel(
    IFeedRepository feedRepository,
    ICaseService caseService,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : FeedPageModelBase(lookups)
{
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
        await LoadLookupsAsync();

        var rbse = RbseHelper.ParseToRaw(Rbse);
        var caseRecord = await caseService.GetCaseAsync(rbse);

        FieldErrors = FeedValidation.Validate(
            new FeedValidation.Input(NewFeed.YearFrom, NewFeed.YearTo, NewFeed.RationType, NewFeed.SupplierId),
            caseRecord?.BirthDate);

        if (FieldErrors.Count > 0)
        {
            return Page();
        }

        var command = new AddFeedCommand(
            Rbse: rbse,
            YearFrom: NewFeed.YearFrom,
            YearTo: NewFeed.YearTo,
            RationType: NewFeed.RationType!,
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
