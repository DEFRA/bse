using BSE.Host.Models.ViewModels;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Models;
using BSE.SharedKernel;

namespace BSE.Host.Services;

public interface ICaseEditOrchestrationService
{
    /// <summary>
    /// Commits every scalar field, staged feed record and staged related-animal/herdbook edit for
    /// this RBSE (Case, Farm, Bab, Clinical, Feeds and/or Relations) in one transaction, then clears
    /// the staging drafts on success. Mirrors legacy CaseEntrySave.aspx's single
    /// "UpdateCaseDetails(dsCase, dsFarm, ...)" commit reached from whichever tab's Save button was
    /// pressed. Returns Success (with nothing to do) if nothing is staged.
    /// </summary>
    /// <exception cref="MandatoryCaseFieldsMissingException">
    /// Thrown before anything is written if the case is missing mandatory data anywhere across tabs
    /// (mirrors legacy CaseEntrySave.aspx's CheckMandatoryFields, which runs regardless of which tab's
    /// Save button triggered the commit).
    /// </exception>
    Task<EditCaseResult> CommitAllAsync(string rbse, int userId);
}

public sealed class CaseEditOrchestrationService(
    ICaseScalarDraftStateService scalarDraftState,
    ICaseFeedsDraftStateService feedsDraftState,
    ICaseRelationsDraftStateService relationsDraftState,
    ICaseRepository caseRepository,
    IFarmRepository farmRepository,
    IBabRepository babRepository,
    IClinicalRepository clinicalRepository,
    IFeedRepository feedRepository,
    IAnimalRelationsRepository relationsRepository,
    IPedigreeRepository pedigreeRepository,
    IDbConnectionFactory connectionFactory) : ICaseEditOrchestrationService
{
    public async Task<EditCaseResult> CommitAllAsync(string rbse, int userId)
    {
        var draft = await scalarDraftState.GetAsync(rbse);
        var feedsDraft = await feedsDraftState.GetAsync(rbse);
        var relationsDraft = await relationsDraftState.GetAsync(rbse);

        var hasScalarWork = draft is not null && draft.HasPendingChanges
            && (draft.Case is not null || draft.Farm is not null || draft.Bab is not null || draft.Clinical is not null || draft.DamSire is not null);
        var hasFeedsWork = feedsDraft is not null && feedsDraft.HasPendingChanges;
        var hasRelationsWork = relationsDraft is not null && relationsDraft.HasPendingChanges;

        if (!hasScalarWork && !hasFeedsWork && !hasRelationsWork)
            return EditCaseResult.Success;

        // Legacy parity: CaseEntrySave.aspx always runs CheckMandatoryFields across the whole case
        // (Case + Farm) before committing, regardless of which tab's Save triggered it. Nothing is
        // written if this fails.
        var mandatoryErrors = await CheckMandatoryFieldsAsync(rbse, draft?.Case, draft?.Farm);
        if (mandatoryErrors.Count > 0)
            throw new MandatoryCaseFieldsMissingException(mandatoryErrors);

        // Bab/Clinical always re-save alongside the Case row (same as legacy, which keeps the
        // whole Case row in the shared session DataSet regardless of which tab is being saved).
        // If no tab staged a Case-scalar edit this round, pass the current row through unchanged
        // so the Bab/Clinical commit still has a valid Case row in the same transaction.
        var caseCommand = draft?.Case;
        if (caseCommand is null && draft is not null && (draft.Bab is not null || draft.Clinical is not null))
        {
            var current = await caseRepository.GetCaseByRbseAsync(rbse);
            if (current is null)
                return EditCaseResult.RbseNotFound;

            caseCommand = CaseEditViewModel.FromRecord(current).ToEditCommand(current.RowStamp ?? []);
        }

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            if (caseCommand is not null)
            {
                var result = await caseRepository.EditCaseAsync(caseCommand, userId, connection, transaction);
                if (result != EditCaseResult.Success)
                {
                    transaction.Rollback();
                    return result;
                }
            }

            if (draft?.Farm is not null)
                await farmRepository.UpdateAsync(draft.Farm, userId, connection, transaction);

            if (draft?.Bab is not null)
                await babRepository.EditAsync(draft.Bab, draft.BabOrigin, connection, transaction);

            if (draft?.Clinical is not null)
                await clinicalRepository.EditAsync(draft.Clinical, connection, transaction);

            if (draft?.DamSire is not null)
                await pedigreeRepository.AddEditDamSireAsync(draft.DamSire, connection, transaction);

            if (hasFeedsWork)
                await PersistStagedFeedsAsync(rbse, feedsDraft!, connection, transaction);

            if (hasRelationsWork)
            {
                var relationsOk = await PersistStagedRelationsAsync(rbse, relationsDraft!, connection, transaction);
                if (!relationsOk)
                {
                    transaction.Rollback();
                    return EditCaseResult.ConcurrencyConflict;
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        await scalarDraftState.ClearAsync(rbse);
        if (hasFeedsWork)
            await feedsDraftState.ClearAsync(rbse);
        if (hasRelationsWork)
            await relationsDraftState.ClearAsync(rbse);

        return EditCaseResult.Success;
    }

    private async Task PersistStagedFeedsAsync(
        string rbse, CaseFeedsDraftState feedsDraft, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction)
    {
        var persisted = (await feedRepository.GetByRbseAsync(rbse)).ToList();
        var persistedById = persisted.ToDictionary(f => f.Id);
        var stagedByExistingId = feedsDraft.Feeds.Where(f => f.Id is > 0).ToDictionary(f => f.Id!.Value);

        foreach (var removed in persisted.Where(f => !stagedByExistingId.ContainsKey(f.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            await feedRepository.DeleteAsync(removed.Id, removed.RowStamp, connection, transaction);
        }

        foreach (var staged in feedsDraft.Feeds)
        {
            if (staged.Id is null or <= 0)
            {
                await feedRepository.AddAsync(new AddFeedCommand(
                    rbse, staged.YearFrom, staged.YearTo, staged.RationType!,
                    staged.SupplierId, staged.RationName, staged.IsPrePurchase), connection, transaction);
                continue;
            }

            if (!persistedById.TryGetValue(staged.Id.Value, out var current))
                continue;

            var changed = current.YearFrom != staged.YearFrom
                          || current.YearTo != staged.YearTo
                          || !string.Equals(current.RationType, staged.RationType, StringComparison.OrdinalIgnoreCase)
                          || !string.Equals(current.RationName ?? "", staged.RationName ?? "", StringComparison.OrdinalIgnoreCase)
                          || current.IsPrePurchase != staged.IsPrePurchase
                          || current.SupplierId != staged.SupplierId;

            if (!changed)
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? current.RowStamp ?? []
                : Convert.FromBase64String(staged.RowStampBase64);

            await feedRepository.EditAsync(new EditFeedCommand(
                staged.Id.Value, staged.YearFrom, staged.YearTo, staged.RationType!,
                staged.SupplierId, staged.RationName, staged.IsPrePurchase, rowStamp), connection, transaction);
        }
    }

    /// <summary>Returns false if a staged relation row was changed by another user (stale RowStamp).</summary>
    private async Task<bool> PersistStagedRelationsAsync(
        string rbse, CaseRelationsDraftState relationsDraft, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction)
    {
        var persisted = (await relationsRepository.GetRelationsByRbseAsync(rbse)).ToList();
        var persistedById = persisted.ToDictionary(r => r.Id);
        var stagedByExistingId = relationsDraft.Relations.Where(r => r.Id is > 0).ToDictionary(r => r.Id!.Value);

        foreach (var removed in persisted.Where(r => !stagedByExistingId.ContainsKey(r.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            var deleted = await relationsRepository.DeleteRelationAsync(new DeleteCaseRelationCommand(removed.Id, removed.RowStamp), connection, transaction);
            if (deleted == 0)
                return false;
        }

        foreach (var staged in relationsDraft.Relations)
        {
            if (staged.Id is null or <= 0)
            {
                await relationsRepository.AddRelationAsync(new AddCaseRelationCommand(
                    rbse, staged.RelationType, staged.RelationRbse, staged.Sex,
                    ToByte(staged.BirthDay), ToByte(staged.BirthMonth), ToShort(staged.BirthYear),
                    staged.RelationFate, staged.LeftDate,
                    staged.EartagCountry, staged.EartagHerdmark, staged.Eartag, staged.Sire), connection, transaction);
                continue;
            }

            if (!persistedById.TryGetValue(staged.Id.Value, out var current))
                continue;

            var changed = !string.Equals(current.RelationType, staged.RelationType, StringComparison.OrdinalIgnoreCase)
                          || !string.Equals(current.RelationRbse ?? "", staged.RelationRbse ?? "", StringComparison.OrdinalIgnoreCase)
                          || !string.Equals(current.Sex ?? "", staged.Sex ?? "", StringComparison.OrdinalIgnoreCase)
                          || current.BirthDay != staged.BirthDay || current.BirthMonth != staged.BirthMonth || current.BirthYear != staged.BirthYear
                          || !string.Equals(current.RelationFate ?? "", staged.RelationFate ?? "", StringComparison.OrdinalIgnoreCase)
                          || current.LeftDate != staged.LeftDate
                          || !string.Equals(current.Eartag ?? "", staged.Eartag ?? "", StringComparison.OrdinalIgnoreCase);

            if (!changed)
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? current.RowStamp ?? []
                : Convert.FromBase64String(staged.RowStampBase64);

            var updated = await relationsRepository.EditRelationAsync(new EditCaseRelationCommand(
                staged.Id.Value, staged.RelationType, staged.RelationRbse, staged.Sex,
                ToByte(staged.BirthDay), ToByte(staged.BirthMonth), ToShort(staged.BirthYear),
                staged.RelationFate, staged.LeftDate,
                staged.EartagCountry, staged.EartagHerdmark, staged.Eartag, staged.Sire, rowStamp), connection, transaction);
            if (updated == 0)
                return false;
        }

        return true;
    }

    /// <summary>Mirrors legacy BSELib.clsCase.CheckMandatoryFields: validates the Case and Farm rows
    /// together, using whatever is staged this round in preference to the currently persisted value.
    /// The legacy `.IsNull("FormBDate") And Not (.IsNull("FormBDate"))` check is logically impossible
    /// (a value cannot be both null and not-null) and is deliberately not reproduced — confirmed dead
    /// code in the legacy implementation.</summary>
    private async Task<IReadOnlyList<string>> CheckMandatoryFieldsAsync(
        string rbse, EditCaseCommand? stagedCase, UpdateFarmCommand? stagedFarm)
    {
        var errors = new List<string>();

        var currentCase = await caseRepository.GetCaseByRbseAsync(rbse);
        if (currentCase is null)
            return errors; // RbseNotFound — surfaced by the EditCase call that follows this check.

        var eartagCountry = stagedCase?.EartagCountry ?? currentCase.EartagCountry;
        var eartagHerdmark = stagedCase?.EartagHerdmark ?? currentCase.EartagHerdmark;
        var eartag = stagedCase?.Eartag ?? currentCase.Eartag;
        var formADate = stagedCase?.FormADate ?? currentCase.FormADate;
        var formBDate = stagedCase?.FormBDate ?? currentCase.FormBDate;
        var fate = stagedCase?.Fate ?? currentCase.Fate;

        if (string.IsNullOrWhiteSpace(currentCase.Cphh))
        {
            errors.Add("No farm has been specified for the case.");
        }
        else
        {
            var farm = await farmRepository.GetByCphhAsync(currentCase.Cphh);
            if (farm is null)
            {
                errors.Add("No farm has been specified for the case.");
            }
            else
            {
                var ownerName = stagedFarm?.OwnerName ?? farm.OwnerName;
                var address1 = stagedFarm?.Address1 ?? farm.Address1;
                var parish = stagedFarm?.Parish ?? farm.Parish;
                var county = stagedFarm?.County ?? farm.County;
                var aho = stagedFarm?.AHO ?? farm.AHO;
                var adnsRegionId = stagedFarm is not null ? stagedFarm.ADNSRegionID : farm.ADNSRegionID;
                var isNonGbFarm = farm.IsNonGBFarm;

                if (string.IsNullOrWhiteSpace(ownerName))
                    errors.Add("Please enter an owner name for the farm.");
                if (string.IsNullOrWhiteSpace(address1))
                    errors.Add("Please enter the first line of the farm address.");
                if (string.IsNullOrWhiteSpace(parish) && !isNonGbFarm)
                    errors.Add("Please enter a parish for the farm.");
                if (string.IsNullOrWhiteSpace(county))
                    errors.Add("Please specify a county for the farm.");
                if (string.IsNullOrWhiteSpace(aho) && !isNonGbFarm)
                    errors.Add("Please specify an AHO for the farm.");
                if (adnsRegionId is null && !isNonGbFarm)
                    errors.Add("Please specify an ADNS Region for the farm.");
            }
        }

        if (string.IsNullOrWhiteSpace(eartagCountry) && string.IsNullOrWhiteSpace(eartagHerdmark) && string.IsNullOrWhiteSpace(eartag))
            errors.Add("Please specify an eartag for the case.");

        if (!formADate.HasValue && !currentCase.IsNonGbCase)
            errors.Add("Please specify a Form A date for the case.");

        if (formBDate.HasValue && string.IsNullOrWhiteSpace(fate))
            errors.Add("Please specify a fate (Form B Reason) for the case.");

        return errors;
    }

    private static byte? ToByte(int? v) => v is > 0 and <= 255 ? (byte)v.Value : null;
    private static short? ToShort(int? v) => v.HasValue ? (short?)v.Value : null;
}
