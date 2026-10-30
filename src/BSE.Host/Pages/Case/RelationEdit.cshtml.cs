using BSE.Host.Helpers;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Pages.Case;

/// <summary>Migrated equivalent of the legacy "Update Selected" action on CaseEntryRelations.aspx.</summary>
[Authorize(Policy = "DataEntry")]
public class RelationEditModel(
    IAnimalRelationsRepository relationsRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory,
    ILogger<RelationEditModel> logger) : RelationPageModelBase(relationsRepository, lookups)
{
    [BindProperty(SupportsGet = true)] public int RelationId { get; set; }
    [BindProperty] public string? RowStamp { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();

        var details = await RelationsRepository.GetRelationsDetailsByRbseAsync(RbseHelper.ParseToRaw(Rbse));
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
        var details = await RelationsRepository.GetRelationsDetailsByRbseAsync(caseRbse);

        // Exclude the row being edited so its own RBSE is not reported as a duplicate.
        var otherRelationRbses = details?.Relations
            .Where(r => r.Id != RelationId)
            .Select(r => r.RelationRbse) ?? [];

        if (!await ValidateAndApplyRelatedCaseAsync(caseRbse, otherRelationRbses, details?.Dam?.Rbse, details?.Sire?.Rbse))
        {
            return Page();
        }

        var command = new EditCaseRelationCommand(
            RelationId,
            RelationType!,
            NullIfBlank(RbseHelper.ParseToRaw(RelationRbse)),
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
            await RelationsRepository.EditRelationAsync(command, conn, tx);
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
}
