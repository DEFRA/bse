namespace BSE.Host.Services;

/// <summary>
/// Clears the cross-tab session state for a case in one call. Legacy's Cancel discards the whole
/// shared session DataSet regardless of which tab's Cancel button was clicked — now a single
/// <see cref="ICaseEditSessionStateService.ClearAsync"/> call does the same, since everything
/// staged across every tab lives in that one object.
/// </summary>
public interface ICaseDraftDiscardService
{
    Task DiscardAllAsync(string rbse);
}

public sealed class CaseDraftDiscardService(ICaseEditSessionStateService sessionState) : ICaseDraftDiscardService
{
    public async Task DiscardAllAsync(string rbse) => await sessionState.ClearAsync(rbse);
}
