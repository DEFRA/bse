using BSE.Host.Models.ViewModels;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

/// <summary>Shared state, lookup loading and CPHH validation for the BAB tab pages.</summary>
public abstract class BabPageModelBase(
    IBabRepository babRepository,
    ICaseRepository caseRepository,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IConfiguration configuration) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public string? RowStampBase64 { get; private set; }
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    public IEnumerable<LookupItem> AnimalOrigins { get; private set; } = [];
    public IEnumerable<LookupItem> FeedRisks { get; private set; } = [];
    public IEnumerable<LookupItem> HorizontalRisks { get; private set; } = [];
    public IEnumerable<LookupItem> MaternalRisks { get; private set; } = [];

    [BindProperty]
    public string? Origin { get; set; }

    [BindProperty]
    public BabFormViewModel Bab { get; set; } = new();

    protected IBabRepository BabRepository => babRepository;

    protected ICaseRepository CaseRepository => caseRepository;

    protected void LoadSpolSiteUrl() => SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

    protected abstract Task LoadAsync();

    /// <summary>Loads the BAB row, case record, batch numbers and lookups shared by both BAB pages.</summary>
    protected async Task<(CaseBabRecord? Bab, CaseRecord? Case)> LoadSharedAsync()
    {
        var babTask     = babRepository.GetByRbseAsync(Rbse);
        var caseTask    = caseRepository.GetCaseByRbseAsync(Rbse);
        var batchTask   = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var originsTask = lookups.GetAnimalOriginsAsync();
        var frTask      = lookups.GetLookupAsync(LookupTableId.FeedRisk);
        var hrTask      = lookups.GetLookupAsync(LookupTableId.HorizontalRisk);
        var mrTask      = lookups.GetLookupAsync(LookupTableId.MaternalRisk);

        await Task.WhenAll(babTask, caseTask, batchTask, originsTask, frTask, hrTask, mrTask);

        var bab = await babTask;
        Bab            = bab is not null ? BabFormViewModel.FromRecord(bab) : new BabFormViewModel();
        RowStampBase64 = bab?.RowStamp is not null ? Convert.ToBase64String(bab.RowStamp) : null;
        BatchNumbers   = (await batchTask).ToList().AsReadOnly();
        AnimalOrigins  = (await originsTask).Select(x => new LookupItem(x.Id, x.Code, x.Description)).ToList();
        FeedRisks      = await frTask;
        HorizontalRisks = await hrTask;
        MaternalRisks  = await mrTask;
        return (bab, await caseTask);
    }

    /// <summary>Normalises the posted natal CPHH. Returns the page to redisplay when it is invalid,
    /// otherwise null.</summary>
    protected async Task<IActionResult?> NormaliseNatalCphhAsync()
    {
        var normalisedNatalCphh = CphhNormalizer.Normalize(Bab.NatalCphh);
        if (!string.IsNullOrWhiteSpace(normalisedNatalCphh) && normalisedNatalCphh.Length != 11)
        {
            ModelState.AddModelError("Bab.NatalCphh", "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");
            LoadSpolSiteUrl();
            await LoadAsync();
            Bab.NatalCphh = normalisedNatalCphh;
            return Page();
        }

        Bab.NatalCphh = string.IsNullOrWhiteSpace(normalisedNatalCphh) ? null : normalisedNatalCphh;
        return null;
    }

    /// <summary>Traced fields only apply when Origin = "P" (Purchased); mirrors legacy EmptyTracedFields.
    /// Returns true when they were cleared.</summary>
    protected bool ClearTracedFieldsUnlessPurchased()
    {
        if (Origin == "P")
            return false;

        Bab.NatalCphh = Bab.TracedName = Bab.TracedAddress1 =
            Bab.TracedAddress2 = Bab.TracedAddress3 = Bab.TracedPostcode = null;
        return true;
    }
}
