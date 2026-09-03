using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class RelationsModel(
    IAnimalRelationsRepository relationsRepository,
    IPedigreeRepository pedigreeRepository,
    IFeedRepository feedRepository,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IDbConnectionFactory connectionFactory,
    ICurrentUserService currentUser,
    IConfiguration configuration) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? SortColumn { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDesc { get; set; }

    public RelationDetailsRecord? Details { get; private set; }
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    public IEnumerable<LookupItem> RelationTypes { get; private set; } = [];
    public IEnumerable<LuRelationFate> RelationFates { get; private set; } = [];
    public IEnumerable<LuSex> Sexes { get; private set; } = [];

    // Whether to re-open the Add Relation details panel (set true when add validation fails)
    public bool ShowAddRelationPanel { get; private set; }

    [BindProperty]
    public DamSireViewModel DamSire { get; set; } = new();

    [BindProperty]
    public NewRelationViewModel NewRelation { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEditDamSireAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        await LoadAsync();
        var command = new AddEditDamSireCommand(
            Rbse: Rbse,
            DamEartag: DamSire.DamEartag, DamName: DamSire.DamName, DamHerdbook: DamSire.DamHerdbook,
            DamBirthDay: DamSire.DamBirthDay, DamBirthMonth: DamSire.DamBirthMonth, DamBirthYear: DamSire.DamBirthYear,
            SireEartag: DamSire.SireEartag, SireName: DamSire.SireName, SireHerdbook: DamSire.SireHerdbook,
            SireBirthDay: DamSire.SireBirthDay, SireBirthMonth: DamSire.SireBirthMonth, SireBirthYear: DamSire.SireBirthYear);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await pedigreeRepository.AddEditDamSireAsync(command, conn, tx);
        tx.Commit();

        TempData["Success"] = "Dam and sire details saved.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostAddRelationAsync()
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        await LoadAsync();

        // ── Mandatory field validation ────────────────────────────────────
        if (string.IsNullOrWhiteSpace(NewRelation.RelationType))
            ModelState.AddModelError("NewRelation.RelationType", "Relation type is required.");

        // At least one animal identifier must be provided
        bool hasRelationRbse = !string.IsNullOrWhiteSpace(NewRelation.RelationRbse);
        bool hasEartag = !string.IsNullOrWhiteSpace(NewRelation.EartagCountry)
                      || !string.IsNullOrWhiteSpace(NewRelation.EartagHerdmark)
                      || !string.IsNullOrWhiteSpace(NewRelation.Eartag);
        if (!hasRelationRbse && !hasEartag)
            ModelState.AddModelError("NewRelation.RelationRbse", "Either a relation RBSE or an eartag must be provided.");

        // ── Birth date component range validation ─────────────────────────
        if (NewRelation.BirthDay.HasValue && (NewRelation.BirthDay < 1 || NewRelation.BirthDay > 31))
            ModelState.AddModelError("NewRelation.BirthDay", "Birth day must be between 1 and 31.");

        if (NewRelation.BirthMonth.HasValue && (NewRelation.BirthMonth < 1 || NewRelation.BirthMonth > 12))
            ModelState.AddModelError("NewRelation.BirthMonth", "Birth month must be between 1 and 12.");

        if (NewRelation.BirthYear.HasValue && (NewRelation.BirthYear < 1980 || NewRelation.BirthYear > DateTime.Today.Year))
            ModelState.AddModelError("NewRelation.BirthYear", $"Birth year must be between 1980 and {DateTime.Today.Year}.");

        if (!ModelState.IsValid)
        {
            ShowAddRelationPanel = true;
            return Page();
        }

        var command = new AddCaseRelationCommand(
            Rbse: Rbse,
            RelationType: NewRelation.RelationType!,
            RelationRbse: NullIfBlank(NewRelation.RelationRbse),
            Sex: NullIfBlank(NewRelation.Sex),
            BirthDay: ToByte(NewRelation.BirthDay),
            BirthMonth: ToByte(NewRelation.BirthMonth),
            BirthYear: ToShort(NewRelation.BirthYear),
            RelationFate: NullIfBlank(NewRelation.RelationFate),
            LeftDate: NewRelation.LeftDate,
            EartagCountry: NullIfBlank(NewRelation.EartagCountry),
            EartagHerdmark: NullIfBlank(NewRelation.EartagHerdmark),
            Eartag: NullIfBlank(NewRelation.Eartag),
            Sire: NullIfBlank(NewRelation.Sire));

        try
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            await relationsRepository.AddRelationAsync(command, conn, tx);
            tx.Commit();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Unable to save relation: {ex.Message}");
            ShowAddRelationPanel = true;
            return Page();
        }

        TempData["Success"] = "Relation added successfully.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteRelationAsync(int relationId, string rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();
        var rowStamp = Convert.FromBase64String(rowStampBase64);
        var command = new DeleteCaseRelationCommand(relationId, rowStamp);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await relationsRepository.DeleteRelationAsync(command, conn, tx);
        tx.Commit();

        TempData["Success"] = "Relation deleted.";
        return RedirectToPage(new { rbse = Rbse });
    }

    private async Task LoadAsync()
    {
        var detailsTask  = relationsRepository.GetRelationsDetailsByRbseAsync(Rbse);
        var batchTask    = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var rtTask       = lookups.GetLookupAsync(BSE.SharedKernel.LookupTableId.RelationType);
        var rfTask       = lookups.GetLookupAsync(BSE.SharedKernel.LookupTableId.RelationFate);
        var sxTask       = lookups.GetSexesAsync();

        await Task.WhenAll(detailsTask, batchTask, rtTask, rfTask, sxTask);

        Details = SortRelations(await detailsTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        RelationTypes = await rtTask;
        RelationFates = (await rfTask)
            .Select(x => new LuRelationFate { Id = x.Id, Code = x.Code, Description = x.Description });
        Sexes = await sxTask;
    }

    private RelationDetailsRecord? SortRelations(RelationDetailsRecord? details)
    {
        if (details?.Relations is null || details.Relations.Count == 0)
            return details;
        IEnumerable<CaseRelationRecord> q = details.Relations;
        q = SortColumn switch
        {
            "Sex"       => SortDesc ? q.OrderByDescending(r => r.Sex)      : q.OrderBy(r => r.Sex),
            "BirthDate" => SortDesc ? q.OrderByDescending(r => r.BirthYear).ThenByDescending(r => r.BirthMonth).ThenByDescending(r => r.BirthDay)
                                    : q.OrderBy(r => r.BirthYear).ThenBy(r => r.BirthMonth).ThenBy(r => r.BirthDay),
            "LeftDate"  => SortDesc ? q.OrderByDescending(r => r.LeftDate) : q.OrderBy(r => r.LeftDate),
            _           => q
        };
        return details with { Relations = q.ToList() };
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    private static byte? ToByte(int? v) => v is > 0 and <= 255 ? (byte)v.Value : null;
    private static short? ToShort(int? v) => v.HasValue ? (short?)v.Value : null;

    // ── View models ──────────────────────────────────────────────────────────

    public class DamSireViewModel
    {
        public string? DamEartag { get; set; }
        public string? DamName { get; set; }
        public string? DamHerdbook { get; set; }
        public int? DamBirthDay { get; set; }
        public int? DamBirthMonth { get; set; }
        public int? DamBirthYear { get; set; }
        public string? SireEartag { get; set; }
        public string? SireName { get; set; }
        public string? SireHerdbook { get; set; }
        public int? SireBirthDay { get; set; }
        public int? SireBirthMonth { get; set; }
        public int? SireBirthYear { get; set; }
    }

    public class NewRelationViewModel
    {
        public string? RelationType { get; set; }
        public string? RelationRbse { get; set; }
        public string? EartagCountry { get; set; }
        public string? EartagHerdmark { get; set; }
        public string? Eartag { get; set; }
        public string? Sex { get; set; }
        public int? BirthDay { get; set; }
        public int? BirthMonth { get; set; }
        public int? BirthYear { get; set; }
        public DateTime? LeftDate { get; set; }
        public string? RelationFate { get; set; }
        public string? Sire { get; set; }
    }
}
