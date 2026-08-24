using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
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
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : PageModel
{
    private const int PageSize = 10;
    private List<ClinicalVisitRecord> _allVisits = [];

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)] public int    VPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string VDir  { get; set; } = "asc";
    public int VisitsTotalPages { get; private set; } = 1;
    public int VisitsTotalCount { get; private set; }

    [BindProperty(SupportsGet = true)] public int EditVisitId { get; set; }
    [BindProperty] public DateTime? EditVisitDate { get; set; }
    [BindProperty] public string EditVisitRowStampBase64 { get; set; } = string.Empty;

    public ClinicalSignsViewModel Signs { get; private set; } = new();
    public IReadOnlyList<ClinicalVisitRecord> Visits { get; private set; } = [];
    public DateTime? BirthDate { get; private set; }
    public string? ClinicalRowStampBase64 { get; private set; }
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    [BindProperty]
    public DateTime? VisitDate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveSignsAsync(string? clinicalRowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
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

        using var conn = connectionFactory.CreateConnection();
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
        TempData["Success"] = "Clinical signs saved.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostAddVisitAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadAsync();

        // Mirrors legacy ClinicalVisitPager_RowSave validation
        ValidateVisitDate(VisitDate, null, nameof(VisitDate));

        if (ModelState.IsValid && _allVisits.Any(v => v.VisitDate?.Date == VisitDate!.Value.Date))
            ModelState.AddModelError(nameof(VisitDate), "A visit on this date already exists. The visit date must be unique.");

        if (!ModelState.IsValid)
            return Page();

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await clinicalRepository.AddVisitAsync(new AddClinicalVisitCommand(Rbse, VisitDate), conn, tx);
        tx.Commit();

        TempData["Success"] = "Clinical visit added.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostEditVisitAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        await LoadAsync();

        ValidateVisitDate(EditVisitDate, EditVisitId, nameof(EditVisitDate));

        if (ModelState.IsValid && _allVisits.Any(v => v.Id != EditVisitId && v.VisitDate?.Date == EditVisitDate!.Value.Date))
            ModelState.AddModelError(nameof(EditVisitDate), "A visit on this date already exists. The visit date must be unique.");

        if (!ModelState.IsValid)
            return Page();

        var rowStamp = string.IsNullOrEmpty(EditVisitRowStampBase64) ? [] : Convert.FromBase64String(EditVisitRowStampBase64);
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await clinicalRepository.EditVisitAsync(new EditClinicalVisitCommand(EditVisitId, EditVisitDate, rowStamp), conn, tx);
        tx.Commit();

        TempData["Success"] = "Clinical visit updated.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteVisitAsync(int visitId)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await clinicalRepository.DeleteVisitAsync(visitId, conn, tx);
        tx.Commit();

        TempData["Success"] = "Clinical visit deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    // Mirrors legacy: visit date must be after birth date (default 1 Jan 1970) and not in the future
    private void ValidateVisitDate(DateTime? date, int? excludeId, string key)
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

        _allVisits     = (await visitsTask).ToList();
        BirthDate      = (await caseTask)?.BirthDate;
        BatchNumbers   = (await batchTask).ToList().AsReadOnly();

        IEnumerable<ClinicalVisitRecord> sorted = VDir == "desc"
            ? _allVisits.OrderByDescending(v => v.VisitDate)
            : _allVisits.OrderBy(v => v.VisitDate);

        VisitsTotalCount = _allVisits.Count;
        VisitsTotalPages = Math.Max(1, (int)Math.Ceiling(_allVisits.Count / (double)PageSize));
        VPage = Math.Clamp(VPage, 1, VisitsTotalPages);
        Visits = sorted.Skip((VPage - 1) * PageSize).Take(PageSize).ToList().AsReadOnly();
    }

    public string VisitsSortUrl() =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&VDir={(VDir == "asc" ? "desc" : "asc")}&VPage=1";

    public string VisitsPageUrl(int page) =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&VPage={page}&VDir={VDir}";

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
