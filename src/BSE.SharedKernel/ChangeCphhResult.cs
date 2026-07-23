namespace BSE.SharedKernel;

/// <summary>
/// Maps the integer RETURN values from the <c>ChangeCPHH</c> stored procedure.
/// Each value corresponds to a specific failure path or the success path (0).
/// </summary>
public enum ChangeCphhResult
{
    /// <summary>Transaction committed successfully; CPHH renamed across all tables.</summary>
    Success = 0,

    /// <summary>
    /// The old CPHH was not found in the Farm table, or the new CPHH already exists.
    /// Both validation checks in the SP share return code 1.
    /// </summary>
    OldCphhNotFoundOrNewCphhAlreadyExists = 1,

    /// <summary>Error inserting the new Farm row copied from the old CPHH record.</summary>
    ErrorInsertingNewFarmRecord = 2,

    /// <summary>Error updating the FarmHistorical table to the new CPHH.</summary>
    ErrorUpdatingFarmHistorical = 3,

    /// <summary>Error updating CPHH or RelatedCPHH references in the FarmRelation table.</summary>
    ErrorUpdatingFarmRelation = 4,

    /// <summary>Error updating the HerdSize table to the new CPHH.</summary>
    ErrorUpdatingHerdSize = 5,

    /// <summary>Error updating the Case table to the new CPHH.</summary>
    ErrorUpdatingCase = 6,

    /// <summary>Error updating the OtherOwner table to the new CPHH.</summary>
    ErrorUpdatingOtherOwner = 7,

    /// <summary>Error deleting the old Farm row after all dependent tables were updated.</summary>
    ErrorDeletingOldFarmRecord = 8
}
