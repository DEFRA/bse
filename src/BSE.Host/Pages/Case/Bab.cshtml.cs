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
public class BabModel(
    IBabRepository babRepository,
    ICaseRepository caseRepository,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : PageModel
{
    private static readonly DateTime BabBirthDateThreshold = new(1988, 7, 18);

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public string? RowStampBase64 { get; private set; }
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];
    public bool CanEditBabControls { get; private set; }
    public bool HasPurchaseData { get; private set; }

    public IEnumerable<LookupItem> AnimalOrigins { get; private set; } = [];
    public IEnumerable<LookupItem> FeedRisks { get; private set; } = [];
    public IEnumerable<LookupItem> HorizontalRisks { get; private set; } = [];
    public IEnumerable<LookupItem> MaternalRisks { get; private set; } = [];

    [BindProperty]
    public string? Origin { get; set; }

    [BindProperty]
    public BabFormViewModel Bab { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveBabAsync(string? rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var currentBabTask = babRepository.GetByRbseAsync(Rbse);
        var currentCaseTask = caseRepository.GetCaseByRbseAsync(Rbse);
        await Task.WhenAll(currentBabTask, currentCaseTask);

        var currentBab = await currentBabTask;
        var currentCase = await currentCaseTask;

        if (currentCase is null)
        {
            TempData["Warning"] = $"Case '{Rbse}' is not saved yet. Complete Farm first.";
            return RedirectToPage(new { rbse = Rbse });
        }

        var canEdit = EvaluateLegacyBabEditPermission(currentCase, currentBab);
        if (!canEdit)
            return RedirectToPage(new { rbse = Rbse });

        var normalisedNatalCphh = CphhNormalizer.Normalize(Bab.NatalCphh);
        if (!string.IsNullOrWhiteSpace(normalisedNatalCphh) && normalisedNatalCphh.Length != 11)
        {
            ModelState.AddModelError("Bab.NatalCphh", "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");
            SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
            await LoadAsync();
            Bab.NatalCphh = normalisedNatalCphh;
            return Page();
        }

        Bab.NatalCphh = string.IsNullOrWhiteSpace(normalisedNatalCphh) ? null : normalisedNatalCphh;

        if (Origin != "P")
        {
            Bab.NatalCphh = Bab.TracedName = Bab.TracedAddress1 =
                Bab.TracedAddress2 = Bab.TracedAddress3 = Bab.TracedPostcode = null;
        }

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
            await babRepository.EditAsync(edit, Origin, conn, tx);
        }
        else
        {
            var add = new AddCaseBabCommand(
                Rbse, Bab.NatalCphh, Bab.Notes,
                Bab.TracedName, Bab.TracedAddress1, Bab.TracedAddress2, Bab.TracedAddress3,
                Bab.TracedPostcode, Bab.FeedRisk, Bab.HorizontalRisk, Bab.MaternalRisk);
            await babRepository.AddAsync(add, Origin, conn, tx);
        }

        tx.Commit();
        TempData["Success"] = "BAB details saved.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public IActionResult OnGetCancelBabEdit()
    {
        return RedirectToPage("/Home");
    }

    private async Task LoadAsync()
    {
        var babTask     = babRepository.GetByRbseAsync(Rbse);
        var caseTask    = caseRepository.GetCaseByRbseAsync(Rbse);
        var batchTask   = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var originsTask = lookups.GetAnimalOriginsAsync();
        var frTask      = lookups.GetLookupAsync(LookupTableId.FeedRisk);
        var hrTask      = lookups.GetLookupAsync(LookupTableId.HorizontalRisk);
        var mrTask      = lookups.GetLookupAsync(LookupTableId.MaternalRisk);

        await Task.WhenAll(babTask, caseTask, batchTask, originsTask, frTask, hrTask, mrTask);

        var bab        = await babTask;
        var caseRecord = await caseTask;
        Bab            = bab is not null ? BabFormViewModel.FromRecord(bab) : new BabFormViewModel();
        RowStampBase64 = bab?.RowStamp is not null ? Convert.ToBase64String(bab.RowStamp) : null;
        Origin         = caseRecord?.Origin;
        HasPurchaseData = caseRecord is not null
            && (caseRecord.PurchaseDate.HasValue
                || !string.IsNullOrWhiteSpace(caseRecord.PurchasedCounty)
                || caseRecord.PurchaseAgeInMonths.HasValue);
        CanEditBabControls = EvaluateLegacyBabEditPermission(caseRecord, bab);
        BatchNumbers   = (await batchTask).ToList().AsReadOnly();
        AnimalOrigins  = (await originsTask).Select(x => new LookupItem(x.Id, x.Code, x.Description)).ToList();
        FeedRisks      = await frTask;
        HorizontalRisks = await hrTask;
        MaternalRisks  = await mrTask;
    }

    private bool EvaluateLegacyBabEditPermission(CaseRecord? caseRecord, CaseBabRecord? babRecord)
    {
        if (!User.IsInRole("DataEntry"))
            return false;

        // Legacy CaseEntryBAB: VLA Data Entry always read-only; VLA Maintenance can edit.
        if (User.IsInRole("VLAAccess") && !User.IsInRole("VLAMaintenance"))
            return false;

        // Legacy MakeControlsWritable: editable only when BirthDate exists and
        // BirthDate >= 18/07/1988 OR a BAB row already exists.
        if (caseRecord?.BirthDate is not DateTime birthDate)
            return false;

        return birthDate.Date >= BabBirthDateThreshold || babRecord is not null;
    }

    public class BabFormViewModel
    {
        public string? NatalCphh { get; set; }
        public string? Notes { get; set; }
        public string? TracedName { get; set; }
        public string? TracedAddress1 { get; set; }
        public string? TracedAddress2 { get; set; }
        public string? TracedAddress3 { get; set; }
        public string? TracedPostcode { get; set; }
        public string? FeedRisk { get; set; }
        public string? HorizontalRisk { get; set; }
        public string? MaternalRisk { get; set; }

        public static BabFormViewModel FromRecord(CaseBabRecord r) => new()
        {
            NatalCphh      = r.NatalCphh,      Notes          = r.Notes,
            TracedName     = r.TracedName,      TracedAddress1 = r.TracedAddress1,
            TracedAddress2 = r.TracedAddress2,  TracedAddress3 = r.TracedAddress3,
            TracedPostcode = r.TracedPostcode,  FeedRisk       = r.FeedRisk,
            HorizontalRisk = r.HorizontalRisk,  MaternalRisk   = r.MaternalRisk
        };
    }
}
