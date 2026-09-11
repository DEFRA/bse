using BSE.Host.Helpers;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

/// <summary>Migrated equivalent of the legacy "Update Selected" action on CaseEntryRelations.aspx.</summary>
[Authorize(Policy = "DataEntry")]
public class RelationEditModel(
    IAnimalRelationsRepository relationsRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory,
    ILogger<RelationEditModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public int RelationId { get; set; }

    [BindProperty] public string? RelationType { get; set; }
    [BindProperty] public string? RelationRbse { get; set; }
    [BindProperty] public string? EartagCountry { get; set; }
    [BindProperty] public string? EartagHerdmark { get; set; }
    [BindProperty] public string? Eartag { get; set; }
    [BindProperty] public string? Sex { get; set; }
    [BindProperty] public int? BirthDay { get; set; }
    [BindProperty] public int? BirthMonth { get; set; }
    [BindProperty] public int? BirthYear { get; set; }
    [BindProperty] public DateTime? LeftDate { get; set; }
    [BindProperty] public string? RelationFate { get; set; }
    [BindProperty] public string? Sire { get; set; }
    [BindProperty] public string? RowStamp { get; set; }

    public IEnumerable<LookupItem> RelationTypes { get; private set; } = [];
    public IEnumerable<LuRelationFate> RelationFates { get; private set; } = [];
    public IEnumerable<LuSex> Sexes { get; private set; } = [];

    public IDictionary<string, string> FieldErrors { get; private set; } = new Dictionary<string, string>();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();

        var details = await relationsRepository.GetRelationsDetailsByRbseAsync(RbseHelper.ParseToRaw(Rbse));
        var existing = details?.Relations.FirstOrDefault(r => r.Id == RelationId);

        if (existing is null)
        {
            return RedirectToPage("/Case/Relations", new { rbse = Rbse });
        }

        RelationType   = existing.RelationType;
        RelationRbse   = existing.RelationRbse;
        EartagCountry  = existing.EartagCountry;
        EartagHerdmark = existing.EartagHerdmark;
        Eartag         = existing.Eartag;
        Sex            = existing.Sex;
        BirthDay       = existing.BirthDay;
        BirthMonth     = existing.BirthMonth;
        BirthYear      = existing.BirthYear;
        LeftDate       = existing.LeftDate;
        RelationFate   = existing.RelationFate;
        Sire           = existing.Sire;
        RowStamp       = Convert.ToBase64String(existing.RowStamp ?? []);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var details = await relationsRepository.GetRelationsDetailsByRbseAsync(caseRbse);

        // Exclude the row being edited so its own RBSE is not reported as a duplicate.
        var otherRelationRbses = details?.Relations
            .Where(r => r.Id != RelationId)
            .Select(r => r.RelationRbse) ?? [];

        FieldErrors = RelationValidation.Validate(
            new RelationValidation.Input(
                caseRbse, RelationRbse, RelationType, Sex,
                EartagCountry, EartagHerdmark, Eartag,
                BirthDay, BirthMonth, BirthYear, LeftDate),
            otherRelationRbses,
            details?.Dam?.Rbse,
            details?.Sire?.Rbse);

        if (FieldErrors.Count > 0)
        {
            foreach (var error in FieldErrors)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }

            return Page();
        }

        var command = new EditCaseRelationCommand(
            RelationId,
            RelationType!,
            NullIfBlank(RbseHelper.Normalize(RelationRbse)),
            NullIfBlank(Sex),
            ToByte(BirthDay),
            ToByte(BirthMonth),
            ToShort(BirthYear),
            NullIfBlank(RelationFate),
            LeftDate,
            NullIfBlank(EartagCountry),
            NullIfBlank(EartagHerdmark),
            NullIfBlank(Eartag),
            NullIfBlank(Sire),
            Convert.FromBase64String(RowStamp ?? string.Empty));

        try
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            await relationsRepository.EditRelationAsync(command, conn, tx);
            tx.Commit();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EditCaseRelation stored procedure threw an exception");
            ErrorMessage = "Unable to save the relation.";
            return Page();
        }

        TempData["Success"] = "Relation updated.";
        return RedirectToPage("/Case/Relations", new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var rtTask = lookups.GetLookupAsync(LookupTableId.RelationType);
        var rfTask = lookups.GetLookupAsync(LookupTableId.RelationFate);
        var sxTask = lookups.GetSexesAsync();
        await Task.WhenAll(rtTask, rfTask, sxTask);

        RelationTypes = await rtTask;
        RelationFates = (await rfTask).Select(x => new LuRelationFate { Id = x.Id, Code = x.Code, Description = x.Description });
        Sexes = await sxTask;
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    private static byte? ToByte(int? v) => v is > 0 and <= 255 ? (byte)v.Value : null;
    private static short? ToShort(int? v) => v.HasValue ? (short?)v.Value : null;
}
