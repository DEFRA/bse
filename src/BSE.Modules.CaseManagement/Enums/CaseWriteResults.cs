namespace BSE.Modules.CaseManagement.Enums;

/// <summary>Return codes from the AddCase stored procedure.</summary>
public enum AddCaseResult
{
    Success = 0,
    /// <summary>An unexpected SQL error occurred during the INSERT.</summary>
    InsertError = 1,
    /// <summary>An unexpected SQL error occurred during the AuditLog INSERT.</summary>
    AuditLogError = 2,
    /// <summary>A case with this RBSE already exists.</summary>
    DuplicateRbse = 3
}

/// <summary>Return codes from the EditCase stored procedure.</summary>
public enum EditCaseResult
{
    Success = 0,
    /// <summary>RBSE not found in [Case].</summary>
    RbseNotFound = 1,
    /// <summary>An unexpected SQL error occurred during audit log writes.</summary>
    AuditLogError = 2,
    /// <summary>RowStamp mismatch — concurrency conflict. Maps to <see cref="Exceptions.ConcurrencyViolationException"/>.</summary>
    ConcurrencyConflict = 3,
    /// <summary>An unexpected SQL error occurred after the main UPDATE.</summary>
    PostUpdateError = 4
}
