namespace BSE.Modules.CaseManagement.Models;

/// <summary>
/// Composite result of GetCaseDetailsByRBSE — all 11 result sets mapped to typed records.
/// Each property corresponds to one child SP called by GetCaseDetailsByRBSE:
///   1. GetCaseByRBSE → <see cref="Case"/>
///   2. GetClinicalByRBSE → <see cref="Clinical"/>
///   3. GetBABByRBSE → <see cref="Bab"/>
///   4. GetOtherOwnerByRBSE → <see cref="OtherOwners"/>
///   5. GetFeedByRBSE → <see cref="Feeds"/>
///   6. GetClinicalVisitByRBSE → <see cref="ClinicalVisits"/>
///   7. GetDamDetailsByRBSE (via GetRelationsDetailsByRBSE) → <see cref="Dam"/>
///   8. GetSireDetailsByRBSE (via GetRelationsDetailsByRBSE) → <see cref="Sire"/>
///   9. GetRelationsByRBSE (via GetRelationsDetailsByRBSE) → <see cref="Relations"/>
///  10. GetTestByRBSE → <see cref="Tests"/>
///  11. GetCaseWorkByRBSE → <see cref="CaseWork"/>
/// </summary>
public sealed record CaseDetailRecord(
    CaseRecord? Case,
    CaseClinicalRecord? Clinical,
    CaseBabRecord? Bab,
    IReadOnlyList<OtherOwnerRecord> OtherOwners,
    IReadOnlyList<CaseFeedRecord> Feeds,
    IReadOnlyList<ClinicalVisitRecord> ClinicalVisits,
    DamSireDetailRecord? Dam,
    DamSireDetailRecord? Sire,
    IReadOnlyList<CaseRelationRecord> Relations,
    IReadOnlyList<CaseTestRecord> Tests,
    CaseWorkRecord? CaseWork);
