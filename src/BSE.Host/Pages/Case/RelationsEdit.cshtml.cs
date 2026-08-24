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

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class RelationsEditModel(
    IAnimalRelationsRepository relationsRepository,
    IPedigreeRepository pedigreeRepository,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IDbConnectionFactory connectionFactory) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public RelationDetailsRecord? Details { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    [BindProperty]
    public DamSireViewModel DamSire { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Details?.Dam is not null || Details?.Sire is not null)
        {
            var dam  = Details.Dam;
            var sire = Details.Sire;
            DamSire = new DamSireViewModel
            {
                DamEartag      = dam?.Eartag,      DamName        = dam?.Name,
                DamHerdbook    = dam?.Herdbook,    DamBirthDay    = dam?.BirthDay,
                DamBirthMonth  = dam?.BirthMonth,  DamBirthYear   = dam?.BirthYear,
                SireEartag     = sire?.Eartag,     SireName       = sire?.Name,
                SireHerdbook   = sire?.Herdbook,   SireBirthDay   = sire?.BirthDay,
                SireBirthMonth = sire?.BirthMonth, SireBirthYear  = sire?.BirthYear
            };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostEditDamSireAsync()
    {
        var command = new AddEditDamSireCommand(
            Rbse: Rbse,
            DamEartag: DamSire.DamEartag,   DamName: DamSire.DamName,
            DamHerdbook: DamSire.DamHerdbook,
            DamBirthDay: DamSire.DamBirthDay,     DamBirthMonth: DamSire.DamBirthMonth,
            DamBirthYear: DamSire.DamBirthYear,
            SireEartag: DamSire.SireEartag,  SireName: DamSire.SireName,
            SireHerdbook: DamSire.SireHerdbook,
            SireBirthDay: DamSire.SireBirthDay,   SireBirthMonth: DamSire.SireBirthMonth,
            SireBirthYear: DamSire.SireBirthYear);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        await pedigreeRepository.AddEditDamSireAsync(command, conn, tx);
        tx.Commit();

        TempData["Success"] = "Dam and sire details saved.";
        return RedirectToPage("/Case/Relations", new { rbse = Rbse });
    }

    private async Task LoadAsync()
    {
        var detailsTask = relationsRepository.GetRelationsDetailsByRbseAsync(Rbse);
        var batchTask   = batchRepository.GetBatchNumbersByRbseAsync(Rbse);

        await Task.WhenAll(detailsTask, batchTask);

        Details      = await detailsTask;
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
    }

    public class DamSireViewModel
    {
        public string? DamEartag      { get; set; }
        public string? DamName        { get; set; }
        public string? DamHerdbook    { get; set; }
        public int?    DamBirthDay    { get; set; }
        public int?    DamBirthMonth  { get; set; }
        public int?    DamBirthYear   { get; set; }
        public string? SireEartag     { get; set; }
        public string? SireName       { get; set; }
        public string? SireHerdbook   { get; set; }
        public int?    SireBirthDay   { get; set; }
        public int?    SireBirthMonth { get; set; }
        public int?    SireBirthYear  { get; set; }
    }
}
