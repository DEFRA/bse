namespace BSE.Host.Services;

/// <summary>
/// Clears every cross-tab draft-state service for a case in one call. Legacy's Cancel discards
/// the whole shared session DataSet regardless of which tab's Cancel button was clicked; each
/// tab's own Cancel handler must do the same here — clear all six draft stores, not just the
/// one(s) that tab itself stages into — otherwise an edit staged on another tab survives a
/// "discard" and resurfaces next time that tab is revisited.
/// </summary>
public interface ICaseDraftDiscardService
{
    Task DiscardAllAsync(string rbse);
}

public sealed class CaseDraftDiscardService(
    ICaseEditDraftStateService caseEditDraftState,
    ICaseFarmDraftStateService caseFarmDraftState,
    ICaseClinicalDraftStateService caseClinicalDraftState,
    ICaseFeedsDraftStateService caseFeedsDraftState,
    ICaseRelationsDraftStateService caseRelationsDraftState,
    ICaseScalarDraftStateService caseScalarDraftState) : ICaseDraftDiscardService
{
    public async Task DiscardAllAsync(string rbse)
    {
        await caseEditDraftState.ClearAsync(rbse);
        await caseFarmDraftState.ClearAsync(rbse);
        await caseClinicalDraftState.ClearAsync(rbse);
        await caseFeedsDraftState.ClearAsync(rbse);
        await caseRelationsDraftState.ClearAsync(rbse);
        await caseScalarDraftState.ClearAsync(rbse);
    }
}
