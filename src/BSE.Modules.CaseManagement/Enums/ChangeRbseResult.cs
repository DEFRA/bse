namespace BSE.Modules.CaseManagement.Enums;

/// <summary>
/// Return codes from the ChangeRBSE stored procedure.
/// Codes map to the 15 distinct RETURN statements in the SP.
/// Success = 0; all others are specific cascade-step failures.
/// </summary>
public enum ChangeRbseResult
{
    Success = 0,
    /// <summary>OldRBSE not found in [Case].</summary>
    OldRbseNotFound = 1,
    /// <summary>NewRBSE already exists in [Case].</summary>
    NewRbseAlreadyExists = 2,
    /// <summary>INSERT of new [Case] row failed.</summary>
    CaseInsertError = 3,
    /// <summary>UPDATE of [Pedigree] RBSE failed.</summary>
    PedigreeUpdateError = 4,
    /// <summary>UPDATE of [CaseHistorical] RBSE failed.</summary>
    CaseHistoricalUpdateError = 5,
    /// <summary>UPDATE of [CaseRelation].[RBSE] failed.</summary>
    CaseRelationRbseUpdateError = 6,
    /// <summary>UPDATE of [CaseRelation].[RelationRBSE] failed.</summary>
    CaseRelationRelationRbseUpdateError = 7,
    /// <summary>UPDATE of [CaseBAB] RBSE failed.</summary>
    CaseBabUpdateError = 8,
    /// <summary>UPDATE of [CaseFeed] RBSE failed.</summary>
    CaseFeedUpdateError = 9,
    /// <summary>UPDATE of [OtherOwner] RBSE failed.</summary>
    OtherOwnerUpdateError = 10,
    /// <summary>UPDATE of [CaseTest] RBSE failed.</summary>
    CaseTestUpdateError = 14,
    /// <summary>INSERT into / UPDATE / DELETE of [CaseClinical] failed.</summary>
    CaseClinicalError = 11,
    /// <summary>INSERT into / UPDATE / DELETE of [CaseWork] failed.</summary>
    CaseWorkError = 15,
    /// <summary>UPDATE of [lnkBatchCase] RBSE failed.</summary>
    BatchCaseLinkUpdateError = 12,
    /// <summary>DELETE of the old [Case] row failed.</summary>
    OldCaseDeleteError = 13
}
