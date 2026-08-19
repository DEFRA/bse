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
    IBatchRepository batchRepository,
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public ClinicalSignsViewModel Signs { get; private set; } = new();
    public IReadOnlyList<ClinicalVisitRecord> Visits { get; private set; } = [];
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
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await clinicalRepository.AddVisitAsync(new AddClinicalVisitCommand(Rbse, VisitDate), conn, tx);
        tx.Commit();

        TempData["Success"] = "Clinical visit added.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteVisitAsync(int visitId)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        // Load RowStamp for the visit before deletion
        var visits = await clinicalRepository.GetVisitsByRbseAsync(Rbse);
        var visit = visits.FirstOrDefault(v => v.Id == visitId);
        if (visit is null)
            return RedirectToPage(new { rbse = Rbse });

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await clinicalRepository.DeleteVisitAsync(visitId, conn, tx);
        tx.Commit();

        TempData["Success"] = "Clinical visit deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    private async Task LoadAsync()
    {
        var clinicalTask = clinicalRepository.GetByRbseAsync(Rbse);
        var visitsTask   = clinicalRepository.GetVisitsByRbseAsync(Rbse);
        var batchTask    = batchRepository.GetBatchNumbersByRbseAsync(Rbse);

        await Task.WhenAll(clinicalTask, visitsTask, batchTask);

        var clinical = await clinicalTask;
        if (clinical is not null)
        {
            Signs = ClinicalSignsViewModel.FromRecord(clinical);
            ClinicalRowStampBase64 = clinical.RowStamp is not null
                ? Convert.ToBase64String(clinical.RowStamp)
                : null;
        }
        Visits = await visitsTask;
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
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
