namespace BSE.Modules.CaseManagement.Enums;

/// <summary>Return codes from the MoveCase stored procedure.</summary>
public enum MoveCaseResult
{
    Success = 0,
    /// <summary>Target Farm (NewCPHH) not found.</summary>
    NewFarmNotFound = 1,
    /// <summary>RBSE not found in [Case] table.</summary>
    RbseNotFound = 2,
    /// <summary>No cases exist on the old farm (data integrity issue).</summary>
    NoOldFarmCases = 3,
    /// <summary>Audit log insert for the case move failed.</summary>
    AuditLogError = 4,
    /// <summary>UPDATE of [Case].[CPHH] failed.</summary>
    CaseUpdateError = 5,
    /// <summary>Audit log or DELETE of now-empty old Farm record failed.</summary>
    OldFarmDeleteError = 6
}
