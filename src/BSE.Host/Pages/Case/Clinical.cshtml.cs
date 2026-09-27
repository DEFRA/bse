using BSE.Infrastructure;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class ClinicalModel(
    IClinicalRepository clinicalRepository,
    ICaseRepository caseRepository,
    IBatchRepository batchRepository,
    ICaseClinicalDraftStateService clinicalDraftState,
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : PageModel
{
    private const int PageSize = 10;
    private List<ClinicalVisitRecord> _persistedVisits = [];

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)] public int    VPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string VDir  { get; set; } = "asc";
    public int VisitsTotalPages { get; private set; } = 1;
    public int VisitsTotalCount { get; private set; }

    public ClinicalSignsViewModel Signs { get; private set; } = new();
    public DateTime? BirthDate { get; private set; }
    public string? ClinicalRowStampBase64 { get; private set; }
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    // ── Inline "add/edit clinical visit" row state (GDS editable-grid pattern, matches /Case/Farm) ──
    [BindProperty] public List<StagedVisitItem> StagedVisits { get; set; } = [];
    [BindProperty] public DateTime? NewVisitDate { get; set; }
    [BindProperty] public DateTime? EditVisitDate { get; set; }
    [BindProperty] public string? EditingClientKey { get; set; }
    public bool ShowAddVisitRow { get; private set; }
    public string? ReopenEditClientKey { get; private set; }

    /// <summary>True when the draft has staged visit changes not yet committed by Save.</summary>
    public bool HasUnsavedChanges { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        await LoadOrInitializeDraftStateAsync();
        return Page();
    }

    /// <summary>Commits clinical signs and all staged visit changes to the database in one go.</summary>
    public async Task<IActionResult> OnPostSaveSignsAsync(string? clinicalRowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var caseRecord = await caseRepository.GetCaseByRbseAsync(Rbse);
        if (caseRecord is null)
        {
            TempData["Warning"] = $"Case '{Rbse}' is not saved yet. Complete Farm first.";
            return RedirectToPage(new { rbse = Rbse });
        }

        var signs = new ClinicalSignsViewModel();
        // Manually bind from form — avoid ambiguous binding with Signs property
        signs.Apprehension = Request.Form["Signs.Apprehension"] == "true";
        signs.HypersensitiveTouch = Request.Form["Signs.HypersensitiveTouch"] == "true";
        signs.HypersensitiveSound = Request.Form["Signs.HypersensitiveSound"] == "true";
        signs.Maniacal = Request.Form["Signs.Maniacal"] == "true";
        signs.PanicStricken = Request.Form["Signs.PanicStricken"] == "true";
        signs.TemperamentChange = Request.Form["Signs.TemperamentChange"] == "true";
        signs.AbnormalHeadCarriage = Request.Form["Signs.AbnormalHeadCarriage"] == "true";
        signs.EarTwitching = Request.Form["Signs.EarTwitching"] == "true";
        signs.EarsOddAngle = Request.Form["Signs.EarsOddAngle"] == "true";
        signs.AbnormalBehaviour = Request.Form["Signs.AbnormalBehaviour"] == "true";
        signs.HeadShyness = Request.Form["Signs.HeadShyness"] == "true";
        signs.LickingFlank = Request.Form["Signs.LickingFlank"] == "true";
        signs.LickingNose = Request.Form["Signs.LickingNose"] == "true";
        signs.Kicking = Request.Form["Signs.Kicking"] == "true";
        signs.ReluctantDoorways = Request.Form["Signs.ReluctantDoorways"] == "true";
        signs.HeadPressing = Request.Form["Signs.HeadPressing"] == "true";
        signs.HeadRubbing = Request.Form["Signs.HeadRubbing"] == "true";
        signs.TeethGrinding = Request.Form["Signs.TeethGrinding"] == "true";
        signs.Blindness = Request.Form["Signs.Blindness"] == "true";
        signs.Circling = Request.Form["Signs.Circling"] == "true";
        signs.HindAtaxia = Request.Form["Signs.HindAtaxia"] == "true";
        signs.Falling = Request.Form["Signs.Falling"] == "true";
        signs.Paresis = Request.Form["Signs.Paresis"] == "true";
        signs.ForeAtaxia = Request.Form["Signs.ForeAtaxia"] == "true";
        signs.Recumbent = Request.Form["Signs.Recumbent"] == "true";
        signs.Tremor = Request.Form["Signs.Tremor"] == "true";
        signs.KnucklingFetlock = Request.Form["Signs.KnucklingFetlock"] == "true";
        signs.WeightLoss = Request.Form["Signs.WeightLoss"] == "true";
        signs.ConditionLoss = Request.Form["Signs.ConditionLoss"] == "true";
        signs.MilkYield = Request.Form["Signs.MilkYield"] == "true";

        await LoadAsync();
        await LoadOrInitializeDraftStateAsync();

        using (var conn = connectionFactory.CreateConnection())
        {
            conn.Open();
            using var tx = conn.BeginTransaction();

            if (!string.IsNullOrEmpty(clinicalRowStampBase64))
            {
                var rowStamp = Convert.FromBase64String(clinicalRowStampBase64);
                await clinicalRepository.EditAsync(signs.ToEditCommand(Rbse, rowStamp), conn, tx);
            }
            else
            {
                await clinicalRepository.AddAsync(signs.ToAddCommand(Rbse), conn, tx);
            }

            tx.Commit();
        }

        await PersistStagedVisitsAsync();
        await clinicalDraftState.ClearAsync(Rbse);

        TempData["Success"] = "Clinical signs and visits saved.";
        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Discards all staged visit changes without persisting them.</summary>
    public async Task<IActionResult> OnPostCancelClinicalEditAsync()
    {
        await clinicalDraftState.ClearAsync(Rbse);
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnGetCancelClinicalEditAsync()
    {
        await clinicalDraftState.ClearAsync(Rbse);
        return RedirectToPage("/Home");
    }

    /// <summary>Adds a clinical visit to the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostAddVisitRowAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var postedDate = NewVisitDate;

        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        ValidateVisitDate(postedDate, nameof(NewVisitDate));

        if (ModelState.IsValid && postedDate.HasValue
            && draft.Visits.Any(v => v.VisitDate?.Date == postedDate.Value.Date))
            ModelState.AddModelError(nameof(NewVisitDate), "A visit on this date already exists. The visit date must be unique.");

        if (!ModelState.IsValid)
        {
            NewVisitDate = postedDate;
            ShowAddVisitRow = true;
            return Page();
        }

        draft.Visits.Add(new CaseClinicalDraftVisitItem
        {
            ClientKey = Guid.NewGuid().ToString("N"),
            Id = null,
            VisitDate = postedDate,
            RowStampBase64 = string.Empty
        });
        draft.HasPendingChanges = true;
        await clinicalDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Opens the inline edit view for one staged clinical visit row (no changes saved yet).</summary>
    public async Task<IActionResult> OnPostBeginEditVisitRowAsync(string clientKey)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        var visit = draft.Visits.FirstOrDefault(v => v.ClientKey == clientKey);
        if (visit is not null)
        {
            ReopenEditClientKey = clientKey;
            EditingClientKey = clientKey;
            EditVisitDate = visit.VisitDate;
        }

        return Page();
    }

    /// <summary>Updates a staged clinical visit row in the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostUpdateVisitRowAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var clientKey = EditingClientKey;
        var postedDate = EditVisitDate;

        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.Visits.FirstOrDefault(v => v.ClientKey == clientKey);
        if (item is null)
        {
            TempData["ErrorMessage"] = "The clinical visit row being edited no longer exists.";
            return RedirectToPage(new { rbse = Rbse });
        }

        ValidateVisitDate(postedDate, nameof(EditVisitDate));

        if (ModelState.IsValid && postedDate.HasValue
            && draft.Visits.Any(v => v.ClientKey != clientKey && v.VisitDate?.Date == postedDate.Value.Date))
            ModelState.AddModelError(nameof(EditVisitDate), "A visit on this date already exists. The visit date must be unique.");

        if (!ModelState.IsValid)
        {
            EditingClientKey = clientKey;
            EditVisitDate = postedDate;
            ReopenEditClientKey = clientKey;
            return Page();
        }

        item.VisitDate = postedDate;
        draft.HasPendingChanges = true;
        await clinicalDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Removes a staged clinical visit row from the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostDeleteVisitAsync(string clientKey)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.Visits.FirstOrDefault(v => v.ClientKey == clientKey);
        if (item is not null)
        {
            draft.Visits.Remove(item);
            draft.HasPendingChanges = true;
            await clinicalDraftState.SetAsync(draft);
        }

        return RedirectToPage(new { rbse = Rbse });
    }

    // Mirrors legacy: visit date must be after birth date (default 1 Jan 1970) and not in the future
    private void ValidateVisitDate(DateTime? date, string key)
    {
        if (!date.HasValue)
        {
            ModelState.AddModelError(key, "Enter a visit date.");
            return;
        }

        var minDate = BirthDate?.Date ?? new DateTime(1970, 1, 1);
        if (date.Value.Date <= minDate)
            ModelState.AddModelError(key, BirthDate.HasValue
                ? "The visit date must be after the birth date."
                : "The visit date must be after 1 January 1970.");

        if (date.Value.Date > DateTime.Today)
            ModelState.AddModelError(key, "The visit date must not be in the future.");
    }

    private async Task LoadAsync()
    {
        var clinicalTask = clinicalRepository.GetByRbseAsync(Rbse);
        var visitsTask   = clinicalRepository.GetVisitsByRbseAsync(Rbse);
        var batchTask    = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var caseTask     = caseRepository.GetCaseByRbseAsync(Rbse);

        await Task.WhenAll(clinicalTask, visitsTask, batchTask, caseTask);

        var clinical = await clinicalTask;
        if (clinical is not null)
        {
            Signs = ClinicalSignsViewModel.FromRecord(clinical);
            ClinicalRowStampBase64 = clinical.RowStamp is not null
                ? Convert.ToBase64String(clinical.RowStamp)
                : null;
        }

        _persistedVisits = (await visitsTask).ToList();
        BirthDate        = (await caseTask)?.BirthDate;
        BatchNumbers     = (await batchTask).ToList().AsReadOnly();
    }

    private async Task<CaseClinicalDraftState> LoadOrInitializeDraftStateAsync()
    {
        var draft = await clinicalDraftState.GetAsync(Rbse);
        if (draft is null)
        {
            draft = new CaseClinicalDraftState
            {
                Rbse = Rbse,
                Visits = _persistedVisits.Select(v => new CaseClinicalDraftVisitItem
                {
                    ClientKey = Guid.NewGuid().ToString("N"),
                    Id = v.Id,
                    VisitDate = v.VisitDate,
                    RowStampBase64 = v.RowStamp is null ? string.Empty : Convert.ToBase64String(v.RowStamp)
                }).ToList(),
                HasPendingChanges = false
            };
            await clinicalDraftState.SetAsync(draft);
        }

        StagedVisits = draft.Visits.Select(v => new StagedVisitItem
        {
            ClientKey = v.ClientKey,
            Id = v.Id,
            VisitDate = v.VisitDate,
            RowStampBase64 = v.RowStampBase64
        }).ToList();

        HasUnsavedChanges = draft.HasPendingChanges;

        IEnumerable<StagedVisitItem> sorted = VDir == "desc"
            ? StagedVisits.OrderByDescending(v => v.VisitDate)
            : StagedVisits.OrderBy(v => v.VisitDate);

        VisitsTotalCount = StagedVisits.Count;
        VisitsTotalPages = Math.Max(1, (int)Math.Ceiling(StagedVisits.Count / (double)PageSize));
        VPage = Math.Clamp(VPage, 1, VisitsTotalPages);
        Visits = sorted.Skip((VPage - 1) * PageSize).Take(PageSize).ToList();

        return draft;
    }

    private async Task PersistStagedVisitsAsync()
    {
        var persistedById = _persistedVisits.ToDictionary(v => v.Id);
        var stagedByExistingId = StagedVisits.Where(v => v.Id is > 0).ToDictionary(v => v.Id!.Value);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        foreach (var removed in _persistedVisits.Where(v => !stagedByExistingId.ContainsKey(v.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            await clinicalRepository.DeleteVisitAsync(removed.Id, removed.RowStamp, conn, tx);
        }

        foreach (var staged in StagedVisits)
        {
            if (staged.Id is null || staged.Id <= 0)
            {
                await clinicalRepository.AddVisitAsync(new AddClinicalVisitCommand(Rbse, staged.VisitDate), conn, tx);
                continue;
            }

            if (!persistedById.TryGetValue(staged.Id.Value, out var persisted))
                continue;

            if (persisted.VisitDate?.Date == staged.VisitDate?.Date)
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? persisted.RowStamp ?? []
                : Convert.FromBase64String(staged.RowStampBase64);

            await clinicalRepository.EditVisitAsync(new EditClinicalVisitCommand(staged.Id.Value, staged.VisitDate, rowStamp), conn, tx);
        }

        tx.Commit();
    }

    public string VisitsSortUrl() =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&VDir={(VDir == "asc" ? "desc" : "asc")}&VPage=1";

    public string VisitsPageUrl(int page) =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&VPage={page}&VDir={VDir}";

    public IReadOnlyList<StagedVisitItem> Visits { get; private set; } = [];

    public sealed class StagedVisitItem
    {
        public string ClientKey { get; set; } = string.Empty;
        public int? Id { get; set; }
        public DateTime? VisitDate { get; set; }
        public string RowStampBase64 { get; set; } = string.Empty;

        /// <summary>True for rows staged locally that do not yet exist in the database.</summary>
        public bool IsUnsaved => Id is null or <= 0;
    }

    // ── View model ────────────────────────────────────────────────────────────

    public class ClinicalSignsViewModel
    {
        public bool Apprehension { get; set; }
        public bool HypersensitiveTouch { get; set; }
        public bool HypersensitiveSound { get; set; }
        public bool Maniacal { get; set; }
        public bool PanicStricken { get; set; }
        public bool TemperamentChange { get; set; }
        public bool AbnormalHeadCarriage { get; set; }
        public bool EarTwitching { get; set; }
        public bool EarsOddAngle { get; set; }
        public bool AbnormalBehaviour { get; set; }
        public bool HeadShyness { get; set; }
        public bool LickingFlank { get; set; }
        public bool LickingNose { get; set; }
        public bool Kicking { get; set; }
        public bool ReluctantDoorways { get; set; }
        public bool HeadPressing { get; set; }
        public bool HeadRubbing { get; set; }
        public bool TeethGrinding { get; set; }
        public bool Blindness { get; set; }
        public bool Circling { get; set; }
        public bool HindAtaxia { get; set; }
        public bool Falling { get; set; }
        public bool Paresis { get; set; }
        public bool ForeAtaxia { get; set; }
        public bool Recumbent { get; set; }
        public bool Tremor { get; set; }
        public bool KnucklingFetlock { get; set; }
        public bool WeightLoss { get; set; }
        public bool ConditionLoss { get; set; }
        public bool MilkYield { get; set; }

        public static ClinicalSignsViewModel FromRecord(CaseClinicalRecord r) => new()
        {
            Apprehension = r.Apprehension, HypersensitiveTouch = r.HypersensitiveTouch,
            HypersensitiveSound = r.HypersensitiveSound, Maniacal = r.Maniacal,
            PanicStricken = r.PanicStricken, TemperamentChange = r.TemperamentChange,
            AbnormalHeadCarriage = r.AbnormalHeadCarriage, EarTwitching = r.EarTwitching,
            EarsOddAngle = r.EarsOddAngle, AbnormalBehaviour = r.AbnormalBehaviour,
            HeadShyness = r.HeadShyness, LickingFlank = r.LickingFlank, LickingNose = r.LickingNose,
            Kicking = r.Kicking, ReluctantDoorways = r.ReluctantDoorways, HeadPressing = r.HeadPressing,
            HeadRubbing = r.HeadRubbing, TeethGrinding = r.TeethGrinding, Blindness = r.Blindness,
            Circling = r.Circling, HindAtaxia = r.HindAtaxia, Falling = r.Falling, Paresis = r.Paresis,
            ForeAtaxia = r.ForeAtaxia, Recumbent = r.Recumbent, Tremor = r.Tremor,
            KnucklingFetlock = r.KnucklingFetlock, WeightLoss = r.WeightLoss,
            ConditionLoss = r.ConditionLoss, MilkYield = r.MilkYield
        };

        public AddCaseClinicalCommand ToAddCommand(string rbse) => new(
            rbse, Apprehension, HypersensitiveTouch, HypersensitiveSound, Maniacal, PanicStricken,
            TemperamentChange, AbnormalHeadCarriage, EarTwitching, EarsOddAngle, AbnormalBehaviour,
            HeadShyness, LickingFlank, LickingNose, Kicking, ReluctantDoorways, HeadPressing,
            HeadRubbing, TeethGrinding, Blindness, Circling, HindAtaxia, Falling, Paresis,
            ForeAtaxia, Recumbent, Tremor, KnucklingFetlock, WeightLoss, ConditionLoss, MilkYield);

        public EditCaseClinicalCommand ToEditCommand(string rbse, byte[] rowStamp) => new(
            rbse, Apprehension, HypersensitiveTouch, HypersensitiveSound, Maniacal, PanicStricken,
            TemperamentChange, AbnormalHeadCarriage, EarTwitching, EarsOddAngle, AbnormalBehaviour,
            HeadShyness, LickingFlank, LickingNose, Kicking, ReluctantDoorways, HeadPressing,
            HeadRubbing, TeethGrinding, Blindness, Circling, HindAtaxia, Falling, Paresis,
            ForeAtaxia, Recumbent, Tremor, KnucklingFetlock, WeightLoss, ConditionLoss, MilkYield,
            rowStamp);
    }
}
