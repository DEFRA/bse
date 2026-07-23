namespace BSE.Modules.CaseManagement.Enums;

/// <summary>Return codes from the DeleteCase stored procedure.</summary>
public enum DeleteCaseResult
{
    Success = 0,
    /// <summary>RBSE not found in [Case] table.</summary>
    RbseNotFound = 1,
    /// <summary>Farm case count query returned 0 rows (data integrity issue).</summary>
    FarmCountError = 2,
    /// <summary>Case has linked records (Feed, Clinical, Relation, OtherOwner, Batch, ClinicalVisit).</summary>
    HasLinkedRecords = 3,
    /// <summary>Audit log insert failed.</summary>
    AuditLogError = 4,
    /// <summary>DELETE of Case (or child BAB/Test/CaseWork) failed.</summary>
    DeleteError = 5,
    /// <summary>Audit log or DELETE of orphaned Farm record failed.</summary>
    FarmDeleteError = 6
}
