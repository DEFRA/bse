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

[Authorize(Policy = "DataEntry")]
public class RelationAddModel(
    IAnimalRelationsRepository relationsRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory,
    ILogger<RelationAddModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = string.Empty;

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

    public IEnumerable<LookupItem> RelationTypes { get; private set; } = [];
    public IEnumerable<LuRelationFate> RelationFates { get; private set; } = [];
    public IEnumerable<LuSex> Sexes { get; private set; } = [];

    public IDictionary<string, string> FieldErrors { get; private set; } = new Dictionary<string, string>();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var details = await relationsRepository.GetRelationsDetailsByRbseAsync(caseRbse);

        FieldErrors = RelationValidation.Validate(
            new RelationValidation.Input(
                caseRbse, RelationRbse, RelationType, Sex,
                EartagCountry, EartagHerdmark, Eartag,
                BirthDay, BirthMonth, BirthYear, LeftDate),
            details?.Relations.Select(r => r.RelationRbse) ?? [],
            details?.Dam?.Rbse,
            details?.Sire?.Rbse);

        if (FieldErrors.Count == 0)
        {
            // Legacy ctlRelationRBSE_RBSEChanged: once a relation RBSE is supplied, Sex, Fate,
            // Eartag, birth date, left date and Sire are always taken live from that case —
            // the corresponding form fields are disabled there and must not be trusted here.
            var normalizedRbse = RbseHelper.Normalize(RelationRbse);
            if (normalizedRbse.Length > 0)
            {
                var related = await relationsRepository.GetRelationDetailsOfRelatedCaseAsync(normalizedRbse);
                if (related is null)
                {
                    FieldErrors = new Dictionary<string, string> { ["RelationRbse"] = RelationValidation.RbseNotFound };
                }
                else
                {
                    Sex = related.Sex;
                    RelationFate = related.Fate;
                    EartagCountry = related.EartagCountry;
                    EartagHerdmark = related.EartagHerdmark;
                    Eartag = related.Eartag;
                    BirthDay = related.BirthDay;
                    BirthMonth = related.BirthMonth;
                    BirthYear = related.BirthYear;
                    LeftDate = DateTime.TryParse(related.LeftDate, out var leftDate) ? leftDate : null;
                    Sire = related.Name;
                }
            }
        }

        if (FieldErrors.Count > 0)
        {
            foreach (var error in FieldErrors)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }

            return Page();
        }

        var command = new AddCaseRelationCommand(
            caseRbse,
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
            NullIfBlank(Sire));

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
            logger.LogError(ex, "AddCaseRelation stored procedure threw an exception");
            ErrorMessage = "Unable to save the relation.";
            return Page();
        }

        TempData["Success"] = "Relation added successfully.";
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
