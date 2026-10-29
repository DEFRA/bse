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
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Repositories;
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
    ICaseEditSessionStateService sessionState,
    ICaseWizardStateService wizardState,
    ICaseRepository caseRepository,
    IFarmRepository farmRepository,
    IBabRepository babRepository,
    IClinicalRepository clinicalRepository,
    IFeedRepository feedRepository,
    IAnimalRelationsRepository relationsRepository,
    IPedigreeRepository pedigreeRepository,
    ICaseWorkRepository caseWorkRepository,
    IBatchRepository batchRepository,
    ITestRepository testRepository,
    IOtherOwnerRepository otherOwnerRepository,
    IHerdSizeRepository herdSizeRepository,
    IFarmRelationRepository farmRelationRepository,
    IDbConnectionFactory connectionFactory,
    ILogger<CaseEditOrchestrationService> logger) : ICaseEditOrchestrationService
{
    private const string Bse1Document = "BSE1";

    public async Task<CaseCommitOutcome> CommitAllAsync(string rbse, int userId)
    {
        var session = await sessionState.GetAsync(rbse);
        var pendingBatch = await wizardState.GetAsync();

        var hasScalarWork = session is not null && session.HasPendingChanges
            && (session.Case is not null || session.Farm is not null || session.Bab is not null || session.Clinical is not null || session.DamSire is not null || session.CaseWork is not null);
        var hasFeedsWork = session is not null && session.HasFeedsChanges;
        var hasRelationsWork = session is not null && session.HasRelationsChanges;
        // Folded in alongside Feeds/Relations for the same reason: legacy's UpdateCaseDetails
        // persists Tests/Other Owners/Visits/Herd Size/Linked Farms in the exact same transaction
        // as everything else, regardless of which tab's Save button triggered it.
        var hasCaseEditWork = session is not null && session.HasCaseEditChanges;
        var hasClinicalVisitsWork = session is not null && session.HasClinicalVisitsChanges;
        var hasFarmCollectionsWork = session is not null && session.HasFarmCollectionsChanges;
        // Legacy parity: Session("BatchID") rides along with whichever tab's Save is clicked next —
        // CreateBatchLink runs inside UpdateCaseDetails regardless of whether the case itself changed.
        var hasBatchWork = pendingBatch is not null
            && string.Equals(pendingBatch.RbseNumber, rbse, StringComparison.OrdinalIgnoreCase);

        if (!hasScalarWork && !hasFeedsWork && !hasRelationsWork && !hasCaseEditWork && !hasClinicalVisitsWork && !hasFarmCollectionsWork && !hasBatchWork)
            return CaseCommitOutcome.Success([]);

        // Legacy parity: a brand-new case lives entirely in the shared in-memory session DataSet
        // until the first Save from *any* tab — there is no "existing row" to fall back to, so this
        // single lookup decides whether every table below is inserted for the first time or updated.
        var existingCase = await caseRepository.GetCaseByRbseAsync(rbse);
        var isNewCase = existingCase is null;

        // Legacy parity: CaseEntrySave.aspx always runs CheckMandatoryFields across the whole case
        // (Case + Farm) before committing, regardless of which tab's Save triggered it, and regardless
        // of whether the case is being created or edited. Nothing is written if this fails.
        var mandatoryErrors = await CheckMandatoryFieldsAsync(rbse, session?.Case, session?.Farm, existingCase);
        if (mandatoryErrors.Count > 0)
            throw new MandatoryCaseFieldsMissingException(mandatoryErrors);

        // Bab/Clinical always re-save alongside the Case row (same as legacy, which keeps the
        // whole Case row in the shared session DataSet regardless of which tab is being saved).
        // If no tab staged a Case-scalar edit this round, pass the current row through unchanged
        // so the Bab/Clinical commit still has a valid Case row in the same transaction.
        var caseCommand = session?.Case;
        if (!isNewCase && caseCommand is null && session is not null && (session.Bab is not null || session.Clinical is not null))
            caseCommand = CaseEditViewModel.FromRecord(existingCase!).ToEditCommand(existingCase!.RowStamp ?? []);

        // A brand-new case can only be created from the Farm tab's own staged fields — mirrors
        // legacy requiring CaseEntryFarm.aspx as the mandatory first stop for a new RBSE.
        if (isNewCase && session?.Farm is null)
            return CaseCommitOutcome.Failure(EditCaseResult.RbseNotFound);

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        // Legacy parity: clsCase.UpdateCaseDetails accumulates per-table "modified by another user"
        // (and similar) messages into a single objErrorList without rolling back the transaction —
        // only a genuinely hard SP failure (thrown exception) aborts the whole save.
        var warnings = new List<string>();

        // Needed by the Linked Farms/Herd Size persistence further down, which is CPHH-keyed, not
        // RBSE-keyed — resolved once here (staged CPHH in preference to the persisted Case's CPHH
        // FK) rather than re-deriving it separately for every grid.
        string? cphhSource;
        if (isNewCase) cphhSource = session!.Farm!.CPHH;
        else cphhSource = session?.Farm?.CPHH is { Length: > 0 } stagedCphh ? stagedCphh : existingCase!.Cphh;
        var cphh = CphhNormalizer.Normalize(cphhSource);

        try
        {
            if (isNewCase)
            {
                var existingFarm = await farmRepository.GetByCphhAsync(cphh);
                if (existingFarm is null)
                    await farmRepository.AddAsync(MapToAddFarm(session.Farm, cphh), userId, connection, transaction);

                var addResult = await caseRepository.AddCaseAsync(MapToAddCase(rbse, cphh, caseCommand), userId, connection, transaction);
                if (addResult != AddCaseResult.Success)
                    throw new InvalidOperationException($"Failed to create case {rbse} (result: {addResult}).");

                // Legacy parity: Common.vb's GetCaseDetailsFromDatabase always calls AddEmptyRow
                // against CASEWORK_TABLE for a brand-new case — without this, a case created via
                // this cross-tab Save path (as opposed to Farm's own dedicated create-case form,
                // which already does this via CaseService.CreateCaseAsync) never gets a CaseWork row.
                // Uses whatever BSE1/Paperwork fields were staged from the Case (DEFRA) tab before
                // the case itself existed, rather than hardcoding every optional field to null.
                var cw = session.CaseWork;
                await caseWorkRepository.AddAsync(
                    new AddCaseWorkCommand(
                        rbse,
                        cw?.RbseDate ?? DateTime.Today,
                        cw?.Barcode,
                        cw?.AhfReference,
                        cw?.PurchaserBse1ReceivedDate,
                        cw?.BreederBse1ReceivedDate,
                        cw?.Vendor1Bse1ReceivedDate,
                        cw?.HomebredBse1ReceivedDate,
                        cw?.SummarySheetReceivedDate,
                        cw?.PaperworkCompleteDate),
                    connection, transaction);

                if (session.Bab is not null)
                    await babRepository.AddAsync(MapToAddBab(session.Bab), session.BabOrigin, connection, transaction);

                if (session.Clinical is not null)
                    await clinicalRepository.AddAsync(MapToAddClinical(session.Clinical), connection, transaction);
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

                if (session?.Farm is not null)
                {
                    var farmWarning = await farmRepository.UpdateAsync(session.Farm, userId, connection, transaction);
                    if (farmWarning is not null)
                        warnings.Add(farmWarning);
                }

                if (session?.Bab is not null)
                {
                    // An existing case can still have no Bab row yet (the tab was never saved
                    // before) — EditAsync would then compare against a RowStamp that belongs to
                    // no real row, always reporting a false "modified by another user".
                    var existingBab = await babRepository.GetByRbseAsync(rbse);
                    if (existingBab is null)
                    {
                        await babRepository.AddAsync(MapToAddBab(session.Bab), session.BabOrigin, connection, transaction);
                    }
                    else
                    {
                        var babWarning = await babRepository.EditAsync(session.Bab, session.BabOrigin, connection, transaction);
                        if (babWarning is not null)
                            warnings.Add(babWarning);
                    }
                }

                if (session?.Clinical is not null)
                {
                    // Same reasoning as Bab above: the case may exist with no Clinical row yet.
                    var existingClinical = await clinicalRepository.GetByRbseAsync(rbse);
                    if (existingClinical is null)
                    {
                        await clinicalRepository.AddAsync(MapToAddClinical(session.Clinical), connection, transaction);
                    }
                    else
                    {
                        var clinicalWarning = await clinicalRepository.EditAsync(session.Clinical, connection, transaction);
                        if (clinicalWarning is not null)
                            warnings.Add(clinicalWarning);
                    }
                }

                if (session?.CaseWork is not null)
                    await caseWorkRepository.EditAsync(session.CaseWork, connection, transaction);
            }

            if (session?.DamSire is not null)
            {
                var damSireWarning = await pedigreeRepository.AddEditDamSireAsync(session.DamSire, connection, transaction);
                if (damSireWarning is not null)
                    warnings.Add(damSireWarning);
            }

            if (hasCaseEditWork)
                await PersistStagedCaseEditAsync(rbse, session!, connection, transaction, warnings);

            if (hasClinicalVisitsWork)
                await PersistStagedClinicalVisitsAsync(rbse, session!, connection, transaction, warnings);

            if (hasFarmCollectionsWork)
                await PersistStagedFarmCollectionsAsync(cphh, session!, connection, transaction, warnings);

            if (hasFeedsWork)
                await PersistStagedFeedsAsync(rbse, session!, connection, transaction, warnings);

            if (hasRelationsWork)
                await PersistStagedRelationsAsync(rbse, session!, connection, transaction, warnings);

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

        await sessionState.ClearAsync(rbse);
        if (hasBatchWork)
            await wizardState.ClearAsync();

        return CaseCommitOutcome.Success(warnings);
    }

    private async Task PersistStagedFeedsAsync(
        string rbse, CaseEditSessionState session, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction,
        List<string> warnings)
    {
        var persisted = (await feedRepository.GetByRbseAsync(rbse)).ToList();
        var persistedById = persisted.ToDictionary(f => f.Id);
        var stagedByExistingId = session.Feeds.Where(f => f.Id is > 0).ToDictionary(f => f.Id!.Value);

        foreach (var removed in persisted.Where(f => !stagedByExistingId.ContainsKey(f.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            var deletedRows = await feedRepository.DeleteAsync(removed.Id, removed.RowStamp, connection, transaction);
            if (deletedRows == 0)
                warnings.Add($"Failed to delete feed with Ration Name \"{removed.RationName}\" - Data was changed by another user");
        }

        foreach (var staged in session.Feeds)
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
        string rbse, CaseEditSessionState session, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction,
        List<string> warnings)
    {
        var persisted = (await relationsRepository.GetRelationsByRbseAsync(rbse)).ToList();
        var persistedById = persisted.ToDictionary(r => r.Id);
        var stagedByExistingId = session.Relations.Where(r => r.Id is > 0).ToDictionary(r => r.Id!.Value);

        foreach (var removed in persisted.Where(r => !stagedByExistingId.ContainsKey(r.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            var deleted = await relationsRepository.DeleteRelationAsync(new DeleteCaseRelationCommand(removed.Id, removed.RowStamp), connection, transaction);
            if (deleted == 0)
                warnings.Add($"Failed to delete relation with RBSE \"{removed.RelationRbse}\" - Data was changed by another user");
        }

        foreach (var staged in session.Relations)
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

    /// <summary>Legacy parity: a stale RowStamp on a Test or Other Owner row is a soft, per-row skip
    /// (matching clsCase.OnOtherOwnerRowUpdated's RecordsAffected = 0 check, applied the same way to
    /// Tests) — it does not abort the rest of the save, it just adds a warning and leaves that one
    /// row as it was. Tests and Other Owners share one HasCaseEditChanges flag on CaseEditSessionState,
    /// so both lists are always processed together here.</summary>
    private async Task PersistStagedCaseEditAsync(
        string rbse, CaseEditSessionState session, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction,
        List<string> warnings)
    {
        var normalizedRbse = rbse.Replace("/", "");

        var persistedTests = (await testRepository.GetByRbseAsync(rbse)).ToList();
        var persistedTestsById = persistedTests.ToDictionary(t => t.Id);
        var stagedTestsByExistingId = session.Tests.Where(t => t.Id is > 0).ToDictionary(t => t.Id!.Value);

        foreach (var removed in persistedTests.Where(p => !stagedTestsByExistingId.ContainsKey(p.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            var deletedRows = await testRepository.DeleteAsync(removed.Id, removed.RowStamp, connection, transaction);
            if (deletedRows == 0)
                warnings.Add($"Failed to delete test \"{removed.TestTypeDescription}\" - Data was changed by another user");
        }

        foreach (var staged in session.Tests)
        {
            if (staged.Id is null or <= 0)
            {
                await testRepository.AddAsync(new AddTestCommand(normalizedRbse, staged.TestType, staged.TestResult), connection, transaction);
                continue;
            }

            if (!persistedTestsById.TryGetValue(staged.Id.Value, out var current))
                continue;

            var changed = !string.Equals(current.TestType, staged.TestType, StringComparison.OrdinalIgnoreCase)
                          || !string.Equals(current.TestResult ?? "", staged.TestResult ?? "", StringComparison.OrdinalIgnoreCase);
            if (!changed)
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? current.RowStamp ?? []
                : Convert.FromBase64String(staged.RowStampBase64);

            var updatedRows = await testRepository.EditAsync(new EditTestCommand(staged.Id.Value, normalizedRbse, staged.TestType, staged.TestResult, rowStamp), connection, transaction);
            if (updatedRows == 0)
                warnings.Add($"Failed to update test \"{staged.TestType}\" - Data was changed by another user");
        }

        var persistedOwners = (await otherOwnerRepository.GetByRbseAsync(rbse)).ToList();
        var persistedOwnersById = persistedOwners.ToDictionary(o => o.Id);
        var stagedOwnersByExistingId = session.OtherOwners.Where(o => o.Id is > 0).ToDictionary(o => o.Id!.Value);

        foreach (var removed in persistedOwners.Where(p => !stagedOwnersByExistingId.ContainsKey(p.Id)))
        {
            var deletedRows = await otherOwnerRepository.DeleteAsync(removed.Id, removed.RowStamp ?? [], connection, transaction);
            if (deletedRows == 0)
                warnings.Add($"Failed to delete owner \"{removed.Name}\" - Data was changed by another user");
        }

        foreach (var staged in session.OtherOwners)
        {
            var normalizedName = string.IsNullOrWhiteSpace(staged.Name) ? null : staged.Name;
            var normalizedCphh = string.IsNullOrWhiteSpace(staged.Cphh) ? null : staged.Cphh;

            if (staged.Id is > 0 && persistedOwnersById.TryGetValue(staged.Id.Value, out var persisted))
            {
                var changed = !string.Equals(staged.Type, persisted.Type, StringComparison.Ordinal)
                              || !string.Equals(normalizedName, persisted.Name, StringComparison.Ordinal)
                              || !string.Equals(normalizedCphh, persisted.Cphh, StringComparison.Ordinal);
                if (!changed)
                    continue;

                var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                    ? persisted.RowStamp ?? []
                    : Convert.FromBase64String(staged.RowStampBase64);

                var updatedRows = await otherOwnerRepository.EditAsync(new EditOtherOwnerCommand(staged.Id.Value, staged.Type, normalizedName, normalizedCphh, rowStamp), connection, transaction);
                if (updatedRows == 0)
                    warnings.Add($"Failed to update owner \"{normalizedName}\" - Data was changed by another user");
            }
            else
            {
                await otherOwnerRepository.AddAsync(new AddOtherOwnerCommand(rbse, staged.Type, normalizedName, normalizedCphh), connection, transaction);
            }
        }
    }

    /// <summary>Legacy parity: a stale RowStamp on a Clinical Visit row is a soft, per-row skip
    /// (matching clsCase.OnClinicalVisitRowUpdated's RecordsAffected = 0 check) — it does not abort
    /// the rest of the save, it just adds a warning and leaves that one row as it was.</summary>
    private async Task PersistStagedClinicalVisitsAsync(
        string rbse, CaseEditSessionState session, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction,
        List<string> warnings)
    {
        var persisted = (await clinicalRepository.GetVisitsByRbseAsync(rbse)).ToList();
        var persistedById = persisted.ToDictionary(v => v.Id);
        var stagedByExistingId = session.Visits.Where(v => v.Id is > 0).ToDictionary(v => v.Id!.Value);

        foreach (var removed in persisted.Where(v => !stagedByExistingId.ContainsKey(v.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            var deletedRows = await clinicalRepository.DeleteVisitAsync(removed.Id, removed.RowStamp, connection, transaction);
            if (deletedRows == 0)
                warnings.Add($"Failed to delete visit dated \"{removed.VisitDate:dd/MM/yyyy}\" - Data was changed by another user");
        }

        foreach (var staged in session.Visits)
        {
            if (staged.Id is null or <= 0)
            {
                await clinicalRepository.AddVisitAsync(new AddClinicalVisitCommand(rbse, staged.VisitDate), connection, transaction);
                continue;
            }

            if (!persistedById.TryGetValue(staged.Id.Value, out var persistedVisit))
                continue;

            if (persistedVisit.VisitDate?.Date == staged.VisitDate?.Date)
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? persistedVisit.RowStamp ?? []
                : Convert.FromBase64String(staged.RowStampBase64);

            var updatedRows = await clinicalRepository.EditVisitAsync(new EditClinicalVisitCommand(staged.Id.Value, staged.VisitDate, rowStamp), connection, transaction);
            if (updatedRows == 0)
                warnings.Add($"Failed to update visit dated \"{staged.VisitDate:dd/MM/yyyy}\" - Data was changed by another user");
        }
    }

    /// <summary>Legacy parity: a stale RowStamp on a Linked Farm or Herd Size row is a soft, per-row
    /// skip (matching clsFarm.OnHerdSizeRowUpdated's RecordsAffected = 0 check, applied the same way
    /// to Linked Farms) — it does not abort the rest of the save, it just adds a warning and leaves
    /// that one row as it was. Keyed by CPHH, not RBSE, unlike every other grid persisted here.</summary>
    private async Task PersistStagedFarmCollectionsAsync(
        string cphh, CaseEditSessionState session, System.Data.IDbConnection connection, System.Data.IDbTransaction transaction,
        List<string> warnings)
    {
        var persistedLinked = (await farmRelationRepository.GetRelatedFarmAsync(cphh)).ToList();
        var persistedLinkedById = persistedLinked.ToDictionary(x => x.ID);
        var stagedLinkedByExistingId = session.LinkedFarms.Where(x => x.Id is > 0).ToDictionary(x => x.Id!.Value);

        foreach (var removed in persistedLinked.Where(x => !stagedLinkedByExistingId.ContainsKey(x.ID) && x.RowStamp is not null))
        {
            var deletedRows = await farmRelationRepository.DeleteAsync(removed.ID, removed.RowStamp!, connection, transaction);
            if (deletedRows == 0)
                warnings.Add($"Failed to delete linked farm \"{removed.RelatedCPHH}\" - Data was changed by another user");
        }

        foreach (var newRow in session.LinkedFarms.Where(x => x.Id is null or <= 0))
            await farmRelationRepository.AddAsync(cphh, CphhNormalizer.Normalize(newRow.RelatedCphh), connection, transaction);

        foreach (var staged in session.LinkedFarms.Where(x => x.Id is > 0))
        {
            if (!persistedLinkedById.TryGetValue(staged.Id!.Value, out var persistedLink))
                continue;

            var persistedCphh = CphhNormalizer.Normalize(persistedLink.RelatedCPHH);
            var stagedCphh = CphhNormalizer.Normalize(staged.RelatedCphh);
            if (string.Equals(persistedCphh, stagedCphh, StringComparison.OrdinalIgnoreCase))
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? persistedLink.RowStamp
                : Convert.FromBase64String(staged.RowStampBase64);
            if (rowStamp is null)
                continue;

            var updatedRows = await farmRelationRepository.UpdateAsync(staged.Id.Value, stagedCphh, rowStamp, connection, transaction);
            if (updatedRows == 0)
                warnings.Add($"Failed to update linked farm \"{stagedCphh}\" - Data was changed by another user");
        }

        var persistedHerd = (await herdSizeRepository.GetByCphhAsync(cphh)).ToList();
        var persistedHerdById = persistedHerd.ToDictionary(x => x.ID);
        var stagedHerdByExistingId = session.HerdSizes.Where(x => x.Id is > 0).ToDictionary(x => x.Id!.Value);

        foreach (var removed in persistedHerd.Where(x => !stagedHerdByExistingId.ContainsKey(x.ID) && x.RowStamp is not null))
        {
            var deletedRows = await herdSizeRepository.DeleteAsync(removed.ID, removed.RowStamp!, connection, transaction);
            if (deletedRows == 0)
                warnings.Add($"Failed to delete herd size for {removed.HerdYear} - Data was changed by another user");
        }

        foreach (var newRow in session.HerdSizes.Where(x => x.Id is null or <= 0))
        {
            await herdSizeRepository.AddAsync(new AddHerdSizeCommand(
                cphh,
                (short)newRow.HerdYear,
                (short)newRow.TotalSize,
                (short?)newRow.Lactation1Size,
                (short?)newRow.Lactation2Size,
                (short?)newRow.Lactation3Size,
                (short?)newRow.Lactation4Size,
                (short?)newRow.Lactation5Size,
                (short?)newRow.Lactation6Size,
                (short?)newRow.Lactation7Size,
                (short?)newRow.Lactation8Size,
                (short?)newRow.Lactation9Size,
                (short?)newRow.Lactation10Size,
                (short?)newRow.Lactation10PlusSize), connection, transaction);
        }

        foreach (var staged in session.HerdSizes.Where(x => x.Id is > 0))
        {
            if (!persistedHerdById.TryGetValue(staged.Id!.Value, out var persistedHerdRow))
                continue;

            if (!HasHerdSizeChanges(persistedHerdRow, staged))
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? persistedHerdRow.RowStamp
                : Convert.FromBase64String(staged.RowStampBase64);
            if (rowStamp is null)
                continue;

            var updatedRows = await herdSizeRepository.UpdateAsync(new UpdateHerdSizeCommand(
                staged.Id.Value,
                (short)staged.HerdYear,
                (short)staged.TotalSize,
                (short?)staged.Lactation1Size,
                (short?)staged.Lactation2Size,
                (short?)staged.Lactation3Size,
                (short?)staged.Lactation4Size,
                (short?)staged.Lactation5Size,
                (short?)staged.Lactation6Size,
                (short?)staged.Lactation7Size,
                (short?)staged.Lactation8Size,
                (short?)staged.Lactation9Size,
                (short?)staged.Lactation10Size,
                (short?)staged.Lactation10PlusSize,
                rowStamp), connection, transaction);
            if (updatedRows == 0)
                warnings.Add($"Failed to update herd size for {staged.HerdYear} - Data was changed by another user");
        }
    }

    private static bool HasHerdSizeChanges(HerdSizeRecord persisted, CaseFarmDraftHerdSizeItem staged) =>
        persisted.HerdYear != staged.HerdYear
        || persisted.TotalSize != staged.TotalSize
        || persisted.Lactation1Size != staged.Lactation1Size
        || persisted.Lactation2Size != staged.Lactation2Size
        || persisted.Lactation3Size != staged.Lactation3Size
        || persisted.Lactation4Size != staged.Lactation4Size
        || persisted.Lactation5Size != staged.Lactation5Size
        || persisted.Lactation6Size != staged.Lactation6Size
        || persisted.Lactation7Size != staged.Lactation7Size
        || persisted.Lactation8Size != staged.Lactation8Size
        || persisted.Lactation9Size != staged.Lactation9Size
        || persisted.Lactation10Size != staged.Lactation10Size
        || persisted.Lactation10PlusSize != staged.Lactation10PlusSize;

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

        // Legacy parity: for a brand-new case, CaseEntryFarm.aspx's btnLookUp_Click never links a
        // "00"-prefixed CPHH to the case at all — it skips GetFarmDetailsFromDatabase entirely, so
        // dsCase's CPHH stays null and CheckMandatoryFields falls straight into its generic "No farm
        // has been specified for the case." message, never reaching the farm-field checks below.
        // Reproduce that exact outcome: a non-GB farm CPHH on a new (GB) case counts as no CPHH here.
        if (isNewCase && !string.IsNullOrWhiteSpace(cphh) && !isNonGbCase)
        {
            var isNonGbCphh = (await farmRepository.GetByCphhAsync(cphh))?.IsNonGBFarm
                ?? CphhNormalizer.Normalize(cphh).StartsWith("00", StringComparison.Ordinal);
            if (isNonGbCphh)
                cphh = null;
        }

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
                if (logger.IsEnabled(LogLevel.Information))
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

        if (logger.IsEnabled(LogLevel.Information))
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
