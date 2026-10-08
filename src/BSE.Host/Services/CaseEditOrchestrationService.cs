using BSE.Host.Models.ViewModels;
using BSE.Infrastructure;
using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
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
    Task<CaseCommitOutcome> CommitAllAsync(string rbse, int userId);
}

/// <summary>
/// Result of <see cref="ICaseEditOrchestrationService.CommitAllAsync"/>. <see cref="Warnings"/> mirrors
/// legacy clsCase.UpdateCaseDetails's <c>objErrorList</c>: non-fatal, per-table "modified by another
/// user" (or similar) messages that do not stop the rest of the transaction from committing. When
/// <see cref="Result"/> is <see cref="EditCaseResult.Success"/> and <see cref="Warnings"/> is non-empty,
/// legacy's CaseEntrySave.aspx shows its "saved with some errors" screen rather than silently
/// succeeding.
/// </summary>
public sealed record CaseCommitOutcome(EditCaseResult Result, IReadOnlyList<string> Warnings)
{
    public static CaseCommitOutcome Success(IReadOnlyList<string> warnings) => new(EditCaseResult.Success, warnings);
    public static CaseCommitOutcome Failure(EditCaseResult result) => new(result, []);
}


public sealed class CaseEditOrchestrationService(
    ICaseScalarDraftStateService scalarDraftState,
    ICaseFeedsDraftStateService feedsDraftState,
    ICaseRelationsDraftStateService relationsDraftState,
    ICaseWizardStateService wizardState,
    ICaseRepository caseRepository,
    IFarmRepository farmRepository,
    IBabRepository babRepository,
    IClinicalRepository clinicalRepository,
    IFeedRepository feedRepository,
    IAnimalRelationsRepository relationsRepository,
    IPedigreeRepository pedigreeRepository,
    IBatchRepository batchRepository,
    IDbConnectionFactory connectionFactory,
    ILogger<CaseEditOrchestrationService> logger) : ICaseEditOrchestrationService
{
    private const string Bse1Document = "BSE1";

    public async Task<CaseCommitOutcome> CommitAllAsync(string rbse, int userId)
    {
        var draft = await scalarDraftState.GetAsync(rbse);
        var feedsDraft = await feedsDraftState.GetAsync(rbse);
        var relationsDraft = await relationsDraftState.GetAsync(rbse);
        var pendingBatch = await wizardState.GetAsync();

        var hasScalarWork = draft is not null && draft.HasPendingChanges
            && (draft.Case is not null || draft.Farm is not null || draft.Bab is not null || draft.Clinical is not null || draft.DamSire is not null);
        var hasFeedsWork = feedsDraft is not null && feedsDraft.HasPendingChanges;
        var hasRelationsWork = relationsDraft is not null && relationsDraft.HasPendingChanges;
        // Legacy parity: Session("BatchID") rides along with whichever tab's Save is clicked next —
        // CreateBatchLink runs inside UpdateCaseDetails regardless of whether the case itself changed.
        var hasBatchWork = pendingBatch is not null
            && string.Equals(pendingBatch.RbseNumber, rbse, StringComparison.OrdinalIgnoreCase);

        if (!hasScalarWork && !hasFeedsWork && !hasRelationsWork && !hasBatchWork)
            return CaseCommitOutcome.Success([]);

        // Legacy parity: a brand-new case lives entirely in the shared in-memory session DataSet
        // until the first Save from *any* tab — there is no "existing row" to fall back to, so this
        // single lookup decides whether every table below is inserted for the first time or updated.
        var existingCase = await caseRepository.GetCaseByRbseAsync(rbse);
        var isNewCase = existingCase is null;

        // Legacy parity: CaseEntrySave.aspx always runs CheckMandatoryFields across the whole case
        // (Case + Farm) before committing, regardless of which tab's Save triggered it, and regardless
        // of whether the case is being created or edited. Nothing is written if this fails.
        var mandatoryErrors = await CheckMandatoryFieldsAsync(rbse, draft?.Case, draft?.Farm, existingCase);
        if (mandatoryErrors.Count > 0)
            throw new MandatoryCaseFieldsMissingException(mandatoryErrors);

        // Bab/Clinical always re-save alongside the Case row (same as legacy, which keeps the
        // whole Case row in the shared session DataSet regardless of which tab is being saved).
        // If no tab staged a Case-scalar edit this round, pass the current row through unchanged
        // so the Bab/Clinical commit still has a valid Case row in the same transaction.
        var caseCommand = draft?.Case;
        if (!isNewCase && caseCommand is null && draft is not null && (draft.Bab is not null || draft.Clinical is not null))
            caseCommand = CaseEditViewModel.FromRecord(existingCase!).ToEditCommand(existingCase!.RowStamp ?? []);

        // A brand-new case can only be created from the Farm tab's own staged fields — mirrors
        // legacy requiring CaseEntryFarm.aspx as the mandatory first stop for a new RBSE.
        if (isNewCase && draft?.Farm is null)
            return CaseCommitOutcome.Failure(EditCaseResult.RbseNotFound);

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        // Legacy parity: clsCase.UpdateCaseDetails accumulates per-table "modified by another user"
        // (and similar) messages into a single objErrorList without rolling back the transaction —
        // only a genuinely hard SP failure (thrown exception) aborts the whole save.
        var warnings = new List<string>();

        try
        {
            string cphh;
            if (isNewCase)
            {
                cphh = CphhNormalizer.Normalize(draft!.Farm!.CPHH);
                var existingFarm = await farmRepository.GetByCphhAsync(cphh);
                if (existingFarm is null)
                    await farmRepository.AddAsync(MapToAddFarm(draft.Farm, cphh), userId, connection, transaction);

                var addResult = await caseRepository.AddCaseAsync(MapToAddCase(rbse, cphh, caseCommand), userId, connection, transaction);
                if (addResult != AddCaseResult.Success)
                    throw new InvalidOperationException($"Failed to create case {rbse} (result: {addResult}).");

                if (draft.Bab is not null)
                    await babRepository.AddAsync(MapToAddBab(draft.Bab), draft.BabOrigin, connection, transaction);

                if (draft.Clinical is not null)
                    await clinicalRepository.AddAsync(MapToAddClinical(draft.Clinical), connection, transaction);
            }
            else
            {
                if (caseCommand is not null)
                {
                    var result = await caseRepository.EditCaseAsync(caseCommand, userId, connection, transaction);
                    if (result == EditCaseResult.ConcurrencyConflict)
                    {
                        // Soft: legacy's EditCase return code 3 is added to objErrorList, not thrown.
                        warnings.Add($"The case record with RBSE {rbse} has been modified by another user");
                    }
                    else if (result != EditCaseResult.Success)
                    {
                        transaction.Rollback();
                        return CaseCommitOutcome.Failure(result);
                    }
                }

                if (draft?.Farm is not null)
                {
                    var farmWarning = await farmRepository.UpdateAsync(draft.Farm, userId, connection, transaction);
                    if (farmWarning is not null)
                        warnings.Add(farmWarning);
                }

                if (draft?.Bab is not null)
                {
                    var babWarning = await babRepository.EditAsync(draft.Bab, draft.BabOrigin, connection, transaction);
                    if (babWarning is not null)
                        warnings.Add(babWarning);
                }

                if (draft?.Clinical is not null)
                {
                    var clinicalWarning = await clinicalRepository.EditAsync(draft.Clinical, connection, transaction);
                    if (clinicalWarning is not null)
                        warnings.Add(clinicalWarning);
                }
            }

            if (draft?.DamSire is not null)
            {
                var damSireWarning = await pedigreeRepository.AddEditDamSireAsync(draft.DamSire, connection, transaction);
                if (damSireWarning is not null)
                    warnings.Add(damSireWarning);
            }

            if (hasFeedsWork)
                await PersistStagedFeedsAsync(rbse, feedsDraft!, connection, transaction, warnings);

            if (hasRelationsWork)
                await PersistStagedRelationsAsync(rbse, relationsDraft!, connection, transaction, warnings);

            if (hasBatchWork)
            {
                // Legacy parity: clsCase.CreateBatchLink wraps the SP call and throws on any
                // non-success/non-duplicate outcome, aborting the whole UpdateCaseDetails transaction.
                var batchResult = await batchRepository.AssignCaseToBatchAsync(
                    pendingBatch!.BatchId, rbse, Bse1Document, connection, transaction);
                if (batchResult is not (BatchAssignmentResult.Success or BatchAssignmentResult.AlreadyAssigned))
                    throw new InvalidOperationException($"Failed to add the case to a batch (result: {batchResult}).");
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
        if (hasBatchWork)
            await wizardState.ClearAsync();

        return CaseCommitOutcome.Success(warnings);
    }

    private async Task PersistStagedFeedsAsync(
        string rbse, CaseFeedsDraftState feedsDraft, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction,
        List<string> warnings)
    {
        var persisted = (await feedRepository.GetByRbseAsync(rbse)).ToList();
        var persistedById = persisted.ToDictionary(f => f.Id);
        var stagedByExistingId = feedsDraft.Feeds.Where(f => f.Id is > 0).ToDictionary(f => f.Id!.Value);

        foreach (var removed in persisted.Where(f => !stagedByExistingId.ContainsKey(f.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            var deletedRows = await feedRepository.DeleteAsync(removed.Id, removed.RowStamp, connection, transaction);
            if (deletedRows == 0)
                warnings.Add($"Failed to delete feed with Ration Name \"{removed.RationName}\" - Data was changed by another user");
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

            var updatedRows = await feedRepository.EditAsync(new EditFeedCommand(
                staged.Id.Value, staged.YearFrom, staged.YearTo, staged.RationType!,
                staged.SupplierId, staged.RationName, staged.IsPrePurchase, rowStamp), connection, transaction);
            if (updatedRows == 0)
                warnings.Add($"Failed to update feed with Ration Name \"{staged.RationName}\" - Data was changed by another user");
        }
    }

    /// <summary>Legacy parity: a stale RowStamp on a relation row is a soft, per-row skip (matching
    /// clsCase.OnRelationRowUpdated's RecordsAffected = 0 check) — it does not abort the rest of the
    /// save, it just adds a warning and leaves that one row as it was.</summary>
    private async Task PersistStagedRelationsAsync(
        string rbse, CaseRelationsDraftState relationsDraft, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction,
        List<string> warnings)
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
                warnings.Add($"Failed to delete relation with RBSE \"{removed.RelationRbse}\" - Data was changed by another user");
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
                warnings.Add($"Failed to update relation with RBSE \"{staged.RelationRbse}\" - Data was changed by another user");
        }
    }

    /// <summary>Mirrors legacy BSELib.clsCase.CheckMandatoryFields: validates the Case and Farm rows
    /// together, using whatever is staged this round in preference to the currently persisted value.
    /// Also runs for a brand-new case (<paramref name="currentCase"/> null) — legacy runs the same
    /// check unconditionally for both Added and Modified session DataSet rows.
    /// The legacy `.IsNull("FormBDate") And Not (.IsNull("FormBDate"))` check is logically impossible
    /// (a value cannot be both null and not-null) and is deliberately not reproduced — confirmed dead
    /// code in the legacy implementation.</summary>
    private async Task<IReadOnlyList<string>> CheckMandatoryFieldsAsync(
        string rbse, EditCaseCommand? stagedCase, UpdateFarmCommand? stagedFarm, CaseRecord? currentCase)
    {
        var errors = new List<string>();
        var isNewCase = currentCase is null;

        // For a new case there is no persisted row at all, so an unstaged field is simply blank —
        // never fall back to a "current" value that doesn't exist.
        var eartagCountry = stagedCase is not null ? stagedCase.EartagCountry : currentCase?.EartagCountry;
        var eartagHerdmark = stagedCase is not null ? stagedCase.EartagHerdmark : currentCase?.EartagHerdmark;
        var eartag = stagedCase is not null ? stagedCase.Eartag : currentCase?.Eartag;
        var formADate = stagedCase is not null ? stagedCase.FormADate : currentCase?.FormADate;
        var formBDate = stagedCase is not null ? stagedCase.FormBDate : currentCase?.FormBDate;
        var fate = stagedCase is not null ? stagedCase.Fate : currentCase?.Fate;
        var isNonGbCase = currentCase?.IsNonGbCase ?? false;

        var cphh = isNewCase ? stagedFarm?.CPHH : currentCase!.Cphh;
        if (string.IsNullOrWhiteSpace(cphh))
        {
            errors.Add("No farm has been specified for the case.");
        }
        else
        {
            var farm = await farmRepository.GetByCphhAsync(cphh);
            if (farm is null && !isNewCase)
            {
                errors.Add("No farm has been specified for the case.");
            }
            else
            {
                // Field-level '??' is wrong here: once the whole Farm command is staged this round,
                // a null property on it means the user genuinely left that field blank, not "untouched"
                // — falling back to the old DB value would silently let a cleared mandatory field (and,
                // for NOT NULL columns like OwnerName, a NULL) reach the database unnoticed.
                var ownerName = stagedFarm is not null ? stagedFarm.OwnerName : farm?.OwnerName;
                var address1 = stagedFarm is not null ? stagedFarm.Address1 : farm?.Address1;
                var parish = stagedFarm is not null ? stagedFarm.Parish : farm?.Parish;
                var county = stagedFarm is not null ? stagedFarm.County : farm?.County;
                var aho = stagedFarm is not null ? stagedFarm.AHO : farm?.AHO;
                var adnsRegionId = stagedFarm is not null ? stagedFarm.ADNSRegionID : farm?.ADNSRegionID;
                // A farm that doesn't exist yet has no stored IsNonGBFarm flag (the AddFarm SP derives
                // it from the CPHH prefix on insert) — derive the same way here, matching
                // Farm.cshtml.cs's own IsNonGbFarmCphh helper used for this exact not-yet-created case.
                var isNonGbFarm = farm?.IsNonGBFarm ?? CphhNormalizer.Normalize(cphh).StartsWith("00", StringComparison.Ordinal);

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

                // Diagnostic: helps pin down reports of "only some of the missing fields show" by
                // capturing exactly what each check saw this round, without changing behaviour.
                logger.LogInformation(
                    "CheckMandatoryFieldsAsync farm check for {Rbse}: isNewCase={IsNewCase} stagedFarmIsNull={StagedFarmIsNull} " +
                    "isNonGbFarm={IsNonGbFarm} ownerName={OwnerName} address1={Address1} parish={Parish} " +
                    "county={County} aho={Aho} adnsRegionId={AdnsRegionId}",
                    rbse, isNewCase, stagedFarm is null, isNonGbFarm, ownerName, address1, parish, county, aho, adnsRegionId);
            }
        }

        if (string.IsNullOrWhiteSpace(eartagCountry) && string.IsNullOrWhiteSpace(eartagHerdmark) && string.IsNullOrWhiteSpace(eartag))
            errors.Add("Please specify an eartag for the case.");

        if (!formADate.HasValue && !isNonGbCase)
            errors.Add("Please specify a Form A date for the case.");

        if (formBDate.HasValue && string.IsNullOrWhiteSpace(fate))
            errors.Add("Please specify a fate (Form B Reason) for the case.");

        logger.LogInformation("CheckMandatoryFieldsAsync for {Rbse} returning {Count} error(s): {Errors}",
            rbse, errors.Count, errors);

        return errors;
    }

    // ── New-case creation: map the same staged Edit-shaped commands used for existing cases to
    // their Add-shaped equivalents (identical fields, minus RowStamp) — reused so every tab's
    // Save handler can trigger first-time creation exactly like editing, with no separate staging
    // model to keep in sync.
    private static AddFarmCommand MapToAddFarm(UpdateFarmCommand c, string cphh) => new(
        CPHH: cphh, OwnerName: c.OwnerName, Address1: c.Address1, Address2: c.Address2, Address3: c.Address3,
        Postcode: c.Postcode, Parish: c.Parish, District: c.District, County: c.County,
        CorrespondenceAddress1: c.CorrespondenceAddress1, CorrespondenceAddress2: c.CorrespondenceAddress2,
        CorrespondenceAddress3: c.CorrespondenceAddress3, CorrespondencePostcode: c.CorrespondencePostcode,
        MapReference: c.MapReference, Herdmark1: c.Herdmark1, Herdmark2: c.Herdmark2, Herdmark3: c.Herdmark3,
        NumericHerdmark1: c.NumericHerdmark1, NumericHerdmark2: c.NumericHerdmark2,
        AHO: c.AHO, HerdType: c.HerdType, PedigreeType: c.PedigreeType, IsDealer: c.IsDealer,
        ADNSRegionID: c.ADNSRegionID);

    private static AddCaseCommand MapToAddCase(string rbse, string cphh, EditCaseCommand? c) => new(
        Rbse: rbse, Cphh: cphh,
        EartagCountry: c?.EartagCountry, EartagHerdmark: c?.EartagHerdmark, Eartag: c?.Eartag,
        PreviousEartag: c?.PreviousEartag, Bse1ReceivedDate: c?.Bse1ReceivedDate, FormADate: c?.FormADate,
        FormAResubmittedDate: c?.FormAResubmittedDate, FormBDate: c?.FormBDate, Fate: c?.Fate,
        FormCDate: c?.FormCDate,
        IsPurchaserBse1Received: c?.IsPurchaserBse1Received ?? false,
        IsBreederBse1Received: c?.IsBreederBse1Received ?? false,
        IsVendor1Bse1Received: c?.IsVendor1Bse1Received ?? false,
        IsHomebredBse1Received: c?.IsHomebredBse1Received ?? false,
        IsSummarySheetReceived: c?.IsSummarySheetReceived ?? false,
        IsPaperworkComplete: c?.IsPaperworkComplete ?? false,
        ReportedLocation: c?.ReportedLocation, Survey: c?.Survey, Notes: c?.Notes,
        BirthDate: c?.BirthDate, IsBirthDateEst: c?.IsBirthDateEst, DamStatus: c?.DamStatus,
        BirthDateSource: c?.BirthDateSource, ValuationAge: c?.ValuationAge, Sex: c?.Sex, Breed: c?.Breed,
        Origin: c?.Origin, PurchaseDate: c?.PurchaseDate, PurchaseAgeInMonths: c?.PurchaseAgeInMonths,
        PurchasedCounty: c?.PurchasedCounty, HerdEntryDate: c?.HerdEntryDate, OnsetDate: c?.OnsetDate,
        IsOnsetDateEst: c?.IsOnsetDateEst, MonthsPregnant: c?.MonthsPregnant, MonthsPostCalving: c?.MonthsPostCalving,
        OnsetAgeInMonths: c?.OnsetAgeInMonths, SlaughterDate: c?.SlaughterDate,
        AlternateDiagnosis: c?.AlternateDiagnosis, LabComment: c?.LabComment, CaseType: c?.CaseType);

    private static AddCaseBabCommand MapToAddBab(EditCaseBabCommand c) => new(
        Rbse: c.Rbse, NatalCphh: c.NatalCphh, Notes: c.Notes, TracedName: c.TracedName,
        TracedAddress1: c.TracedAddress1, TracedAddress2: c.TracedAddress2, TracedAddress3: c.TracedAddress3,
        TracedPostcode: c.TracedPostcode, FeedRisk: c.FeedRisk, HorizontalRisk: c.HorizontalRisk,
        MaternalRisk: c.MaternalRisk);

    private static AddCaseClinicalCommand MapToAddClinical(EditCaseClinicalCommand c) => new(
        Rbse: c.Rbse,
        Apprehension: c.Apprehension, HypersensitiveTouch: c.HypersensitiveTouch, HypersensitiveSound: c.HypersensitiveSound,
        Maniacal: c.Maniacal, PanicStricken: c.PanicStricken, TemperamentChange: c.TemperamentChange,
        AbnormalHeadCarriage: c.AbnormalHeadCarriage, EarTwitching: c.EarTwitching, EarsOddAngle: c.EarsOddAngle,
        AbnormalBehaviour: c.AbnormalBehaviour, HeadShyness: c.HeadShyness, LickingFlank: c.LickingFlank,
        LickingNose: c.LickingNose, Kicking: c.Kicking, ReluctantDoorways: c.ReluctantDoorways,
        HeadPressing: c.HeadPressing, HeadRubbing: c.HeadRubbing, TeethGrinding: c.TeethGrinding,
        Blindness: c.Blindness, Circling: c.Circling, HindAtaxia: c.HindAtaxia, Falling: c.Falling,
        Paresis: c.Paresis, ForeAtaxia: c.ForeAtaxia, Recumbent: c.Recumbent, Tremor: c.Tremor,
        KnucklingFetlock: c.KnucklingFetlock, WeightLoss: c.WeightLoss, ConditionLoss: c.ConditionLoss,
        MilkYield: c.MilkYield);

    private static byte? ToByte(int? v) => v is > 0 and <= 255 ? (byte)v.Value : null;
    private static short? ToShort(int? v) => v.HasValue ? (short?)v.Value : null;
}
