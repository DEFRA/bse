namespace BSE.Modules.Batch.Models;

/// <summary>
/// Outcome of assigning a case to a batch. Values mirror the RETURN codes of the
/// <c>AddBatchNumberLink</c> stored procedure.
/// </summary>
public enum BatchAssignmentResult
{
    /// <summary>SP returned 0 — the link row was inserted.</summary>
    Success = 0,

    /// <summary>SP returned 1 — the supplied BatchID does not exist in the Batch table.</summary>
    BatchNotFound = 1,

    /// <summary>SP returned 2 — the case is already linked to this batch for this document.</summary>
    AlreadyAssigned = 2,

    /// <summary>SP returned 3 — the insert failed.</summary>
    InsertError = 3
}
