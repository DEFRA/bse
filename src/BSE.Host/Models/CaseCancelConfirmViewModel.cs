namespace BSE.Host.Models;

/// <summary>Inputs for the shared Cancel-link confirmation script: the page's dirty-flag variable and its server-side unsaved state.</summary>
public sealed record CaseCancelConfirmViewModel(string DirtyVar, bool HasUnsavedChanges);
