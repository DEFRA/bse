using BSE.Infrastructure;
using BSE.Host.Models.ViewModels;
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

[Authorize(Policy = "CaseEntryAccess")]
[Authorize(Policy = "DataEntry")]
public class BabEditModel(
    IBabRepository babRepository,
    ICaseRepository caseRepository,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : BabPageModelBase(babRepository, caseRepository, lookups, batchRepository, configuration)
{
    public async Task<IActionResult> OnGetAsync()
    {
        LoadSpolSiteUrl();
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveBabAsync(string? rowStampBase64)
    {
        if (await NormaliseNatalCphhAsync() is { } invalidPage)
            return invalidPage;

        ClearTracedFieldsUnlessPurchased();

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        if (!string.IsNullOrEmpty(rowStampBase64))
        {
            var rowStamp = Convert.FromBase64String(rowStampBase64);
            var edit = new EditCaseBabCommand(
                Rbse, Bab.NatalCphh, Bab.Notes,
                Bab.TracedName, Bab.TracedAddress1, Bab.TracedAddress2, Bab.TracedAddress3,
                Bab.TracedPostcode, Bab.FeedRisk, Bab.HorizontalRisk, Bab.MaternalRisk,
                rowStamp);
            await BabRepository.EditAsync(edit, Origin, conn, tx);
        }
        else
        {
            var add = new AddCaseBabCommand(
                Rbse, Bab.NatalCphh, Bab.Notes,
                Bab.TracedName, Bab.TracedAddress1, Bab.TracedAddress2, Bab.TracedAddress3,
                Bab.TracedPostcode, Bab.FeedRisk, Bab.HorizontalRisk, Bab.MaternalRisk);
            await BabRepository.AddAsync(add, Origin, conn, tx);
        }

        tx.Commit();
        TempData["Success"] = "BAB details saved.";
        return RedirectToPage("/Case/Bab", new { rbse = Rbse });
    }

    protected override async Task LoadAsync()
    {
        var (_, caseRecord) = await LoadSharedAsync();
        Origin = caseRecord?.Origin;
    }
}
