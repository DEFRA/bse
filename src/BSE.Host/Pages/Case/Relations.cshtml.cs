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

    public RelationDetailsRecord? Details { get; private set; }
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    public IEnumerable<LookupItem> RelationTypes { get; private set; } = [];
    public IEnumerable<LuRelationFate> RelationFates { get; private set; } = [];
    public IEnumerable<LuSex> Sexes { get; private set; } = [];

    [BindProperty]
    public DamSireViewModel DamSire { get; set; } = new();

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

}
