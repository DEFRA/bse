namespace BSE.Modules.CaseManagement.Enums;

/// <summary>Return codes from the AddNonGBCase stored procedure.</summary>
public enum AddNonGbCaseResult
{
    Success = 0,
    /// <summary>A case with this RBSE already exists.</summary>
    AlreadyExists = 1,
    /// <summary>Error updating an existing farm record.</summary>
    FarmUpdateError = 2,
    /// <summary>Audit log error during farm update.</summary>
    FarmUpdateAuditError = 3,
    /// <summary>Error inserting a new farm record.</summary>
    FarmCreateError = 4,
    /// <summary>Audit log error during farm creation.</summary>
    FarmCreateAuditError = 5,
    /// <summary>Error inserting the Case row.</summary>
    CaseInsertError = 6,
    /// <summary>Audit log error during case creation.</summary>
    CaseAuditError = 7,
    /// <summary>Error inserting the CaseWork row.</summary>
    CaseWorkInsertError = 8
}
