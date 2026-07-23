namespace BSE.Modules.CaseManagement.Exceptions;

/// <summary>
/// Raised when an optimistic concurrency conflict is detected on <c>EditCase</c> or
/// <c>EditFarm</c> — i.e. the stored procedure returns code 3 (RowStamp mismatch).
/// The caller should reload the record and prompt the user to re-apply their changes.
/// </summary>
public sealed class ConcurrencyViolationException : Exception
{
    public ConcurrencyViolationException(string entity, string key)
        : base($"Concurrency conflict on {entity} '{key}': the record was modified by another user. Reload and retry.")
    {
        Entity = entity;
        Key = key;
    }

    public string Entity { get; }
    public string Key { get; }
}
