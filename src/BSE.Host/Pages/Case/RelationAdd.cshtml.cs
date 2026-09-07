using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class RelationAddModel(
    IAnimalRelationsRepository relationsRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory) : PageModel
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

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();

        if (string.IsNullOrWhiteSpace(RelationType))
            ModelState.AddModelError(nameof(RelationType), "Relation type is required.");

        var hasRelationRbse = !string.IsNullOrWhiteSpace(RelationRbse);
        var hasEartag = !string.IsNullOrWhiteSpace(EartagCountry)
                        || !string.IsNullOrWhiteSpace(EartagHerdmark)
                        || !string.IsNullOrWhiteSpace(Eartag);
        if (!hasRelationRbse && !hasEartag)
            ModelState.AddModelError(nameof(RelationRbse), "Either a relation RBSE or an eartag must be provided.");

        if (BirthDay.HasValue && (BirthDay < 1 || BirthDay > 31))
            ModelState.AddModelError(nameof(BirthDay), "Birth day must be between 1 and 31.");

        if (BirthMonth.HasValue && (BirthMonth < 1 || BirthMonth > 12))
            ModelState.AddModelError(nameof(BirthMonth), "Birth month must be between 1 and 12.");

        if (BirthYear.HasValue && (BirthYear < 1980 || BirthYear > DateTime.Today.Year))
            ModelState.AddModelError(nameof(BirthYear), $"Birth year must be between 1980 and {DateTime.Today.Year}.");

        if (!ModelState.IsValid)
            return Page();

        var command = new AddCaseRelationCommand(
            Rbse,
            RelationType!,
            NullIfBlank(RelationRbse),
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
            ModelState.AddModelError(string.Empty, $"Unable to save relation: {ex.Message}");
            return Page();
        }

        TempData["Success"] = "Relation added successfully.";
        return RedirectToPage("/Case/Relations", new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var rtTask = lookups.GetLookupAsync(BSE.SharedKernel.LookupTableId.RelationType);
        var rfTask = lookups.GetLookupAsync(BSE.SharedKernel.LookupTableId.RelationFate);
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
