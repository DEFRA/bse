using BSE.Host.Helpers;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "CaseEntryAccess")]
[Authorize(Policy = "VlaSireEdit")]
public class RelationAddModel(
    IAnimalRelationsRepository relationsRepository,
    ILookupDataService lookups,
    IDbConnectionFactory connectionFactory,
    ILogger<RelationAddModel> logger) : RelationPageModelBase(relationsRepository, lookups)
{
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        return Page();
    }

    /// <summary>Live lookup for the Relation RBSE field, mirrors legacy's
    /// ctlRelationRBSE_RBSEChanged autopostback (populates Sex/Fate/Eartag/birth date/left
    /// date/Sire from the related case as soon as a valid RBSE is entered, before Save).</summary>
    public async Task<IActionResult> OnGetRelationDetailsAsync(string? rbse)
    {
        var normalized = RbseHelper.ParseToRaw(rbse);
        if (normalized.Length == 0)
            return new JsonResult(new { found = false });

        var related = await RelationsRepository.GetRelationDetailsOfRelatedCaseAsync(normalized);
        if (related is null)
            return new JsonResult(new { found = false });

        return new JsonResult(new
        {
            found = true,
            sex = related.Sex,
            fate = related.Fate,
            eartagCountry = related.EartagCountry,
            eartagHerdmark = related.EartagHerdmark,
            eartag = related.Eartag,
            birthDay = related.BirthDay,
            birthMonth = related.BirthMonth,
            birthYear = related.BirthYear,
            leftDate = related.LeftDate,
            sire = related.Name
        });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var details = await RelationsRepository.GetRelationsDetailsByRbseAsync(caseRbse);

        if (!await ValidateAndApplyRelatedCaseAsync(
                caseRbse,
                details?.Relations.Select(r => r.RelationRbse) ?? [],
                details?.Dam?.Rbse,
                details?.Sire?.Rbse))
        {
            return Page();
        }

        var command = new AddCaseRelationCommand(
            caseRbse,
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
            NullIfBlank(Sire));

        try
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            await RelationsRepository.AddRelationAsync(command, conn, tx);
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
}
