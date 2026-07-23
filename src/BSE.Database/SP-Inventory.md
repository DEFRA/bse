# BSE Database — Stored Procedure Inventory

> **Generated:** Slice 1 — Stored Procedure Source Control  
> **Total SPs:** 257  
> **Source:** `legacy/BSEDatabase/dbo/Stored Procedures/` — copied verbatim, no content changes  
> **Constraint:** SP content must NOT be modified until the owning slice is implemented (see Owned By Slice column).  
> The `@@ERROR` → `TRY/CATCH` refactor is deferred to Slice 8.

---

## Summary by Domain Folder

| Domain Folder | Count | Owning Slice |
|---------------|------:|-------------|
| ReferenceData | 136 | Slice 2 |
| CaseManagement | 44 | Slice 8 |
| AnimalRelations | 12 | Slice 9 |
| FarmManagement | 16 | Slice 5 |
| AuditLog | 8 | Slice 4 |
| Search | 8 | Slice 6 |
| CaseWork | 7 | Slice 10 |
| Batch | 7 | Slice 7 |
| OssExport | 6 | Slice 12 |
| UserManagement | 6 | Slice 3 |
| AdnsExport | 4 | Slice 11 |
| BsessIntegration | 2 | Slice 13 |
| Shared | 1 | — |
| Legacy | 0 | — |
| **Total** | **257** | |

---

## Flags

> **Note on `GetluUserGroup`:** The Migration Plan Slice 2 (Reference Data) lists `GetluUserGroup` as a Reference Data SP. This inventory places it in `UserManagement/` per the Slice 1 implementation spec. This should be confirmed by the Tech Lead before Slice 2 and Slice 3 are implemented.

> **Note on `AddluUserGroup`:** Follows the `lu*` naming convention and is placed in `ReferenceData/`. Unlike `GetluUserGroup`, it is not explicitly claimed by UserManagement in the implementation spec. Confirm ownership at Slice 3.

> **Note on case-sensitivity warnings (SQL71558):** The SQL project build produces 126 pre-existing warnings about case-sensitivity differences between SP identifier references and table/column definitions (e.g., `[FARM]` vs `[Farm]`). These are runtime-safe under the database's `Latin1_General_CI_AS` collation. They must NOT be fixed in this slice — they are deferred to the per-domain slices where SPs are refactored.

---

## Full Inventory

| SP Name | File | Domain Folder | Owned by Slice | Notes |
|---------|------|---------------|---------------|-------|
| AddBatchNumber | AddBatchNumber.sql | Batch | Slice 7 | |
| AddBatchNumber1989 | AddBatchNumber1989.sql | OssExport | Slice 12 | Used to assign 1989 scheme batch numbers for OSS export |
| AddBatchNumberLink | AddBatchNumberLink.sql | Batch | Slice 7 | |
| AddCase | AddCase.sql | CaseManagement | Slice 8 | Core case insert — must be wrapped in outer transaction (Risk R01) |
| AddCaseBAB | AddCaseBAB.sql | CaseManagement | Slice 8 | |
| AddCaseClinical | AddCaseClinical.sql | CaseManagement | Slice 8 | |
| AddCaseFeed | AddCaseFeed.sql | CaseManagement | Slice 8 | |
| AddCaseRelation | AddCaseRelation.sql | AnimalRelations | Slice 9 | |
| AddCaseWork | AddCaseWork.sql | CaseWork | Slice 10 | |
| AddClinicalVisit | AddClinicalVisit.sql | CaseManagement | Slice 8 | |
| AddEditDamSireDetails | AddEditDamSireDetails.sql | AnimalRelations | Slice 9 | |
| AddFarm | AddFarm.sql | FarmManagement | Slice 5 | |
| AddFarmRelation | AddFarmRelation.sql | FarmManagement | Slice 5 | |
| AddHerdSize | AddHerdSize.sql | FarmManagement | Slice 5 | |
| AddluADNSRegion | AddluADNSRegion.sql | ReferenceData | Slice 2 | |
| AddluAHO | AddluAHO.sql | ReferenceData | Slice 2 | |
| AddluAHRO | AddluAHRO.sql | ReferenceData | Slice 2 | |
| AddluAnimalOrigin | AddluAnimalOrigin.sql | ReferenceData | Slice 2 | |
| AddluAnimalStatus | AddluAnimalStatus.sql | ReferenceData | Slice 2 | |
| AddluBirthDateSource | AddluBirthDateSource.sql | ReferenceData | Slice 2 | |
| AddluBreed | AddluBreed.sql | ReferenceData | Slice 2 | |
| AddluBSECounty | AddluBSECounty.sql | ReferenceData | Slice 2 | |
| AddluBSEForm | AddluBSEForm.sql | ReferenceData | Slice 2 | |
| AddluCaseFate | AddluCaseFate.sql | ReferenceData | Slice 2 | |
| AddluCaseType | AddluCaseType.sql | ReferenceData | Slice 2 | |
| AddluDocumentType | AddluDocumentType.sql | ReferenceData | Slice 2 | |
| AddluFeedRisk | AddluFeedRisk.sql | ReferenceData | Slice 2 | |
| AddluHerdType | AddluHerdType.sql | ReferenceData | Slice 2 | |
| AddluHorizontalRisk | AddluHorizontalRisk.sql | ReferenceData | Slice 2 | |
| AddluMaternalRisk | AddluMaternalRisk.sql | ReferenceData | Slice 2 | |
| AddluOwnerType | AddluOwnerType.sql | ReferenceData | Slice 2 | |
| AddluPedigreeType | AddluPedigreeType.sql | ReferenceData | Slice 2 | |
| AddluRationType | AddluRationType.sql | ReferenceData | Slice 2 | |
| AddluRegionalLab | AddluRegionalLab.sql | ReferenceData | Slice 2 | |
| AddluRelationFate | AddluRelationFate.sql | ReferenceData | Slice 2 | |
| AddluRelationType | AddluRelationType.sql | ReferenceData | Slice 2 | |
| AddluReportedLocation | AddluReportedLocation.sql | ReferenceData | Slice 2 | |
| AddluSex | AddluSex.sql | ReferenceData | Slice 2 | |
| AddluSupplier | AddluSupplier.sql | ReferenceData | Slice 2 | |
| AddluSurvey | AddluSurvey.sql | ReferenceData | Slice 2 | |
| AddluTestResult | AddluTestResult.sql | ReferenceData | Slice 2 | |
| AddluTestType | AddluTestType.sql | ReferenceData | Slice 2 | |
| AddluTSETestingSite | AddluTSETestingSite.sql | ReferenceData | Slice 2 | |
| AddluUserGroup | AddluUserGroup.sql | ReferenceData | Slice 2 | ⚠ Ownership: placed in ReferenceData (lu* prefix); confirm vs UserManagement at Slice 3 |
| AddluValuationAge | AddluValuationAge.sql | ReferenceData | Slice 2 | |
| AddNonGBCase | AddNonGBCase.sql | CaseManagement | Slice 8 | |
| AddOtherOwner | AddOtherOwner.sql | CaseManagement | Slice 8 | |
| AddTest | AddTest.sql | CaseManagement | Slice 8 | Adds a CaseTest record (BSE lab test) |
| AddUser | AddUser.sql | UserManagement | Slice 3 | |
| ChangeCPHH | ChangeCPHH.sql | FarmManagement | Slice 5 | Cascades CPHH change across farm-linked records |
| ChangeRBSE | ChangeRBSE.sql | CaseManagement | Slice 8 | |
| CopyCaseToExportTable | CopyCaseToExportTable.sql | OssExport | Slice 12 | Populates exp* staging table |
| CopyFarmToExportTable | CopyFarmToExportTable.sql | OssExport | Slice 12 | Populates exp* staging table |
| CopyHerdSizeToExportTable | CopyHerdSizeToExportTable.sql | OssExport | Slice 12 | Populates exp* staging table |
| CopyRelationToExportTable | CopyRelationToExportTable.sql | OssExport | Slice 12 | Populates exp* staging table |
| DeleteCase | DeleteCase.sql | CaseManagement | Slice 8 | |
| DeleteCaseFeed | DeleteCaseFeed.sql | CaseManagement | Slice 8 | |
| DeleteCaseRelation | DeleteCaseRelation.sql | AnimalRelations | Slice 9 | |
| DeleteClinicalVisit | DeleteClinicalVisit.sql | CaseManagement | Slice 8 | |
| DeleteFarmRelation | DeleteFarmRelation.sql | FarmManagement | Slice 5 | |
| DeleteHerdSize | DeleteHerdSize.sql | FarmManagement | Slice 5 | |
| DeleteluADNSRegion | DeleteluADNSRegion.sql | ReferenceData | Slice 2 | |
| DeleteluAHO | DeleteluAHO.sql | ReferenceData | Slice 2 | |
| DeleteluAHRO | DeleteluAHRO.sql | ReferenceData | Slice 2 | |
| DeleteluAnimalOrigin | DeleteluAnimalOrigin.sql | ReferenceData | Slice 2 | |
| DeleteluAnimalStatus | DeleteluAnimalStatus.sql | ReferenceData | Slice 2 | |
| DeleteluBirthDateSource | DeleteluBirthDateSource.sql | ReferenceData | Slice 2 | |
| DeleteluBreed | DeleteluBreed.sql | ReferenceData | Slice 2 | |
| DeleteluBSECounty | DeleteluBSECounty.sql | ReferenceData | Slice 2 | |
| DeleteluBSEForm | DeleteluBSEForm.sql | ReferenceData | Slice 2 | |
| DeleteluCaseFate | DeleteluCaseFate.sql | ReferenceData | Slice 2 | |
| DeleteluCaseType | DeleteluCaseType.sql | ReferenceData | Slice 2 | |
| DeleteluDocumentType | DeleteluDocumentType.sql | ReferenceData | Slice 2 | |
| DeleteluFeedRisk | DeleteluFeedRisk.sql | ReferenceData | Slice 2 | |
| DeleteluHerdType | DeleteluHerdType.sql | ReferenceData | Slice 2 | |
| DeleteluHorizontalRisk | DeleteluHorizontalRisk.sql | ReferenceData | Slice 2 | |
| DeleteluMaternalRisk | DeleteluMaternalRisk.sql | ReferenceData | Slice 2 | |
| DeleteluOwnerType | DeleteluOwnerType.sql | ReferenceData | Slice 2 | |
| DeleteluPedigreeType | DeleteluPedigreeType.sql | ReferenceData | Slice 2 | |
| DeleteluRationType | DeleteluRationType.sql | ReferenceData | Slice 2 | |
| DeleteluRegionalLab | DeleteluRegionalLab.sql | ReferenceData | Slice 2 | |
| DeleteluRelationFate | DeleteluRelationFate.sql | ReferenceData | Slice 2 | |
| DeleteluRelationType | DeleteluRelationType.sql | ReferenceData | Slice 2 | |
| DeleteluReportedLocation | DeleteluReportedLocation.sql | ReferenceData | Slice 2 | |
| DeleteluSex | DeleteluSex.sql | ReferenceData | Slice 2 | |
| DeleteluSupplier | DeleteluSupplier.sql | ReferenceData | Slice 2 | |
| DeleteluSurvey | DeleteluSurvey.sql | ReferenceData | Slice 2 | |
| DeleteluTestResult | DeleteluTestResult.sql | ReferenceData | Slice 2 | |
| DeleteluTestType | DeleteluTestType.sql | ReferenceData | Slice 2 | |
| DeleteluTSETestingSite | DeleteluTSETestingSite.sql | ReferenceData | Slice 2 | |
| DeleteluValuationAge | DeleteluValuationAge.sql | ReferenceData | Slice 2 | |
| DeleteOtherOwner | DeleteOtherOwner.sql | CaseManagement | Slice 8 | |
| DeleteTest | DeleteTest.sql | CaseManagement | Slice 8 | |
| EditCase | EditCase.sql | CaseManagement | Slice 8 | Core case update — must be wrapped in outer transaction (Risk R01) |
| EditCaseADNS | EditCaseADNS.sql | AdnsExport | Slice 11 | Updates ADNS-specific case fields post-export |
| EditCaseBAB | EditCaseBAB.sql | CaseManagement | Slice 8 | |
| EditCaseClinical | EditCaseClinical.sql | CaseManagement | Slice 8 | |
| EditCaseFeed | EditCaseFeed.sql | CaseManagement | Slice 8 | |
| EditCaseFinalResult | EditCaseFinalResult.sql | CaseManagement | Slice 8 | |
| EditCaseRelation | EditCaseRelation.sql | AnimalRelations | Slice 9 | |
| EditCaseWork | EditCaseWork.sql | CaseWork | Slice 10 | |
| EditCaseWorkEntry | EditCaseWorkEntry.sql | CaseWork | Slice 10 | |
| EditClinicalVisit | EditClinicalVisit.sql | CaseManagement | Slice 8 | |
| EditFarm | EditFarm.sql | FarmManagement | Slice 5 | |
| EditFarmRelation | EditFarmRelation.sql | FarmManagement | Slice 5 | |
| EditHerdSize | EditHerdSize.sql | FarmManagement | Slice 5 | |
| EditLastADNSReference | EditLastADNSReference.sql | AdnsExport | Slice 11 | Increments the sequential ADNS reference number (Risk R02) |
| EditluADNSRegion | EditluADNSRegion.sql | ReferenceData | Slice 2 | |
| EditluAHO | EditluAHO.sql | ReferenceData | Slice 2 | |
| EditluAHRO | EditluAHRO.sql | ReferenceData | Slice 2 | |
| EditluAnimalOrigin | EditluAnimalOrigin.sql | ReferenceData | Slice 2 | |
| EditluAnimalStatus | EditluAnimalStatus.sql | ReferenceData | Slice 2 | |
| EditluBirthDateSource | EditluBirthDateSource.sql | ReferenceData | Slice 2 | |
| EditluBreed | EditluBreed.sql | ReferenceData | Slice 2 | |
| EditluBSECounty | EditluBSECounty.sql | ReferenceData | Slice 2 | |
| EditluBSEForm | EditluBSEForm.sql | ReferenceData | Slice 2 | |
| EditluCaseFate | EditluCaseFate.sql | ReferenceData | Slice 2 | |
| EditluCaseType | EditluCaseType.sql | ReferenceData | Slice 2 | |
| EditluDocumentType | EditluDocumentType.sql | ReferenceData | Slice 2 | |
| EditluFeedRisk | EditluFeedRisk.sql | ReferenceData | Slice 2 | |
| EditluHerdType | EditluHerdType.sql | ReferenceData | Slice 2 | |
| EditluHorizontalRisk | EditluHorizontalRisk.sql | ReferenceData | Slice 2 | |
| EditluMaternalRisk | EditluMaternalRisk.sql | ReferenceData | Slice 2 | |
| EditluOwnerType | EditluOwnerType.sql | ReferenceData | Slice 2 | |
| EditluPedigreeType | EditluPedigreeType.sql | ReferenceData | Slice 2 | |
| EditluRationType | EditluRationType.sql | ReferenceData | Slice 2 | |
| EditluRegionalLab | EditluRegionalLab.sql | ReferenceData | Slice 2 | |
| EditluRelationFate | EditluRelationFate.sql | ReferenceData | Slice 2 | |
| EditluRelationType | EditluRelationType.sql | ReferenceData | Slice 2 | |
| EditluReportedLocation | EditluReportedLocation.sql | ReferenceData | Slice 2 | |
| EditluSex | EditluSex.sql | ReferenceData | Slice 2 | |
| EditluSupplier | EditluSupplier.sql | ReferenceData | Slice 2 | |
| EditluSurvey | EditluSurvey.sql | ReferenceData | Slice 2 | |
| EditluTestResult | EditluTestResult.sql | ReferenceData | Slice 2 | |
| EditluTestType | EditluTestType.sql | ReferenceData | Slice 2 | |
| EditluTSETestingSite | EditluTSETestingSite.sql | ReferenceData | Slice 2 | ⚠ SQL71558 case-sensitivity warnings — pre-existing in legacy |
| EditluValuationAge | EditluValuationAge.sql | ReferenceData | Slice 2 | |
| EditOtherOwner | EditOtherOwner.sql | CaseManagement | Slice 8 | |
| EditTest | EditTest.sql | CaseManagement | Slice 8 | |
| EditUser | EditUser.sql | UserManagement | Slice 3 | |
| GetADNSCasesForGB | GetADNSCasesForGB.sql | AdnsExport | Slice 11 | GB ADNS export query (Risk R02) |
| GetAuditLogByCase | GetAuditLogByCase.sql | AuditLog | Slice 4 | |
| GetAuditLogByDate | GetAuditLogByDate.sql | AuditLog | Slice 4 | |
| GetAuditLogByFarm | GetAuditLogByFarm.sql | AuditLog | Slice 4 | |
| GetAuditLogByUser | GetAuditLogByUser.sql | AuditLog | Slice 4 | |
| GetAuditLogCaseMoves | GetAuditLogCaseMoves.sql | AuditLog | Slice 4 | |
| GetAuditLogCPHHChanges | GetAuditLogCPHHChanges.sql | AuditLog | Slice 4 | |
| GetAuditLogNewFarms | GetAuditLogNewFarms.sql | AuditLog | Slice 4 | |
| GetAuditLogRBSEChanges | GetAuditLogRBSEChanges.sql | AuditLog | Slice 4 | |
| GetBABByRBSE | GetBABByRBSE.sql | CaseManagement | Slice 8 | |
| GetBatchIDForBatch | GetBatchIDForBatch.sql | Batch | Slice 7 | |
| GetBatchNumberByRBSE | GetBatchNumberByRBSE.sql | Batch | Slice 7 | |
| GetBSESSCheckByDate | GetBSESSCheckByDate.sql | BsessIntegration | Slice 13 | |
| GetBSESSCheckByRBSE | GetBSESSCheckByRBSE.sql | BsessIntegration | Slice 13 | |
| GetCaseByBatchID | GetCaseByBatchID.sql | Shared | — | ⚠ Cross-domain: used by CaseManagement, Batch, and FarmManagement; kept in Shared |
| GetCaseByRBSE | GetCaseByRBSE.sql | CaseManagement | Slice 8 | |
| GetCaseDetailsByRBSE | GetCaseDetailsByRBSE.sql | CaseManagement | Slice 8 | |
| GetCaseFarmByBatchID | GetCaseFarmByBatchID.sql | CaseManagement | Slice 8 | |
| GetCaseWorkByRBSE | GetCaseWorkByRBSE.sql | CaseWork | Slice 10 | |
| GetCaseWorkEntryByRBSE | GetCaseWorkEntryByRBSE.sql | CaseWork | Slice 10 | |
| GetClinicalByBatchID | GetClinicalByBatchID.sql | CaseManagement | Slice 8 | |
| GetClinicalByRBSE | GetClinicalByRBSE.sql | CaseManagement | Slice 8 | |
| GetClinicalVisitByRBSE | GetClinicalVisitByRBSE.sql | CaseManagement | Slice 8 | |
| GetClosedCaseReportData | GetClosedCaseReportData.sql | CaseManagement | Slice 8 | Reporting SP — confirm if used at runtime or reporting-only |
| GetCPHHBatchForRBSE | GetCPHHBatchForRBSE.sql | CaseManagement | Slice 8 | |
| GetCPHHOtherOwnerForRBSE | GetCPHHOtherOwnerForRBSE.sql | CaseManagement | Slice 8 | |
| GetCPHHRBSEForBatch | GetCPHHRBSEForBatch.sql | Batch | Slice 7 | |
| GetCPHHRBSEForBatchID | GetCPHHRBSEForBatchID.sql | Batch | Slice 7 | |
| GetDamDetailsByRBSE | GetDamDetailsByRBSE.sql | AnimalRelations | Slice 9 | |
| GetDamSireDetailsByBatchID | GetDamSireDetailsByBatchID.sql | AnimalRelations | Slice 9 | |
| GetDamSireDetailsMatches | GetDamSireDetailsMatches.sql | AnimalRelations | Slice 9 | |
| GetEditableLookupProcs | GetEditableLookupProcs.sql | ReferenceData | Slice 2 | |
| GetEditableLookups | GetEditableLookups.sql | ReferenceData | Slice 2 | |
| GetFarmByCPHH | GetFarmByCPHH.sql | FarmManagement | Slice 5 | |
| GetFarmDetailsByCPHH | GetFarmDetailsByCPHH.sql | FarmManagement | Slice 5 | |
| GetFarmsByCPH | GetFarmsByCPH.sql | FarmManagement | Slice 5 | |
| GetFeedByRBSE | GetFeedByRBSE.sql | CaseManagement | Slice 8 | |
| GetFeedsByBatchID | GetFeedsByBatchID.sql | CaseManagement | Slice 8 | |
| GetFinalResultByRBSE | GetFinalResultByRBSE.sql | CaseManagement | Slice 8 | |
| GetGroupForUser | GetGroupForUser.sql | UserManagement | Slice 3 | OIDC → UserGroup claim resolution |
| GetHerdDetailByBatchID | GetHerdDetailByBatchID.sql | FarmManagement | Slice 5 | |
| GetHerdSizeByCPHH | GetHerdSizeByCPHH.sql | FarmManagement | Slice 5 | |
| GetLastADNSReferenceByArea | GetLastADNSReferenceByArea.sql | AdnsExport | Slice 11 | Reads current sequential reference before increment (Risk R02) |
| GetLatestBatchNumbers | GetLatestBatchNumbers.sql | Batch | Slice 7 | |
| GetLatestDBSEForYear | GetLatestDBSEForYear.sql | CaseManagement | Slice 8 | DBSE number sequencing |
| GetLatestRBSEForYear | GetLatestRBSEForYear.sql | CaseManagement | Slice 8 | RBSE number sequencing |
| GetluADNSRegion | GetluADNSRegion.sql | ReferenceData | Slice 2 | |
| GetluADNSRegionByAuthority | GetluADNSRegionByAuthority.sql | ReferenceData | Slice 2 | |
| GetluAHO | GetluAHO.sql | ReferenceData | Slice 2 | |
| GetluAHOCode | GetluAHOCode.sql | ReferenceData | Slice 2 | |
| GetluAHRO | GetluAHRO.sql | ReferenceData | Slice 2 | |
| GetluAHROCode | GetluAHROCode.sql | ReferenceData | Slice 2 | |
| GetluAnimalOrigin | GetluAnimalOrigin.sql | ReferenceData | Slice 2 | |
| GetluAnimalStatus | GetluAnimalStatus.sql | ReferenceData | Slice 2 | |
| GetluAuthorityByAuthorityCounty | GetluAuthorityByAuthorityCounty.sql | ReferenceData | Slice 2 | |
| GetluAuthorityCountyAll | GetluAuthorityCountyAll.sql | ReferenceData | Slice 2 | |
| GetluBirthDateSource | GetluBirthDateSource.sql | ReferenceData | Slice 2 | |
| GetluBreed | GetluBreed.sql | ReferenceData | Slice 2 | |
| GetluBSECounty | GetluBSECounty.sql | ReferenceData | Slice 2 | |
| GetluBSEForm | GetluBSEForm.sql | ReferenceData | Slice 2 | |
| GetluBSERegion | GetluBSERegion.sql | ReferenceData | Slice 2 | |
| GetluCaseFate | GetluCaseFate.sql | ReferenceData | Slice 2 | |
| GetluCaseType | GetluCaseType.sql | ReferenceData | Slice 2 | |
| GetluDocumentType | GetluDocumentType.sql | ReferenceData | Slice 2 | |
| GetluFeedRisk | GetluFeedRisk.sql | ReferenceData | Slice 2 | |
| GetluHerdType | GetluHerdType.sql | ReferenceData | Slice 2 | |
| GetluHorizontalRisk | GetluHorizontalRisk.sql | ReferenceData | Slice 2 | |
| GetluMaternalRisk | GetluMaternalRisk.sql | ReferenceData | Slice 2 | |
| GetluOwnerType | GetluOwnerType.sql | ReferenceData | Slice 2 | |
| GetluPedigreeType | GetluPedigreeType.sql | ReferenceData | Slice 2 | |
| GetluRationType | GetluRationType.sql | ReferenceData | Slice 2 | |
| GetluRegionalLab | GetluRegionalLab.sql | ReferenceData | Slice 2 | |
| GetluRelationFate | GetluRelationFate.sql | ReferenceData | Slice 2 | |
| GetluRelationType | GetluRelationType.sql | ReferenceData | Slice 2 | |
| GetluReportedLocation | GetluReportedLocation.sql | ReferenceData | Slice 2 | |
| GetluSex | GetluSex.sql | ReferenceData | Slice 2 | |
| GetluSupplier | GetluSupplier.sql | ReferenceData | Slice 2 | |
| GetluSurvey | GetluSurvey.sql | ReferenceData | Slice 2 | |
| GetluTestResult | GetluTestResult.sql | ReferenceData | Slice 2 | |
| GetluTestType | GetluTestType.sql | ReferenceData | Slice 2 | |
| GetluTSETestingSite | GetluTSETestingSite.sql | ReferenceData | Slice 2 | |
| GetluUserGroup | GetluUserGroup.sql | UserManagement | Slice 3 | ⚠ Migration Plan Slice 2 also claims this — see Flags section |
| GetluValuationAge | GetluValuationAge.sql | ReferenceData | Slice 2 | |
| GetMapReferenceByCountyParish | GetMapReferenceByCountyParish.sql | ReferenceData | Slice 2 | Geo lookup |
| GetMinuteDetails | GetMinuteDetails.sql | CaseWork | Slice 10 | ⚠ SQL71558 case-sensitivity warnings — pre-existing in legacy |
| GetNonGBCounty | GetNonGBCounty.sql | ReferenceData | Slice 2 | Geo lookup |
| GetNumberOfCasesByCPHH | GetNumberOfCasesByCPHH.sql | CaseManagement | Slice 8 | |
| GetNumberOfConfirmedCases | GetNumberOfConfirmedCases.sql | CaseManagement | Slice 8 | |
| GetOpenCaseReportData | GetOpenCaseReportData.sql | CaseManagement | Slice 8 | Reporting SP |
| GetOSSExportByRBSE | GetOSSExportByRBSE.sql | OssExport | Slice 12 | |
| GetOtherOwnerByRBSE | GetOtherOwnerByRBSE.sql | CaseManagement | Slice 8 | |
| GetParishByCountyParish | GetParishByCountyParish.sql | ReferenceData | Slice 2 | Geo lookup |
| GetPossibleSuppliers | GetPossibleSuppliers.sql | ReferenceData | Slice 2 | |
| GetPrefixCodeByXYReference | GetPrefixCodeByXYReference.sql | ReferenceData | Slice 2 | Geo lookup |
| GetPreviousOwnerByBatchID | GetPreviousOwnerByBatchID.sql | CaseManagement | Slice 8 | |
| GetRelatedFarm | GetRelatedFarm.sql | FarmManagement | Slice 5 | |
| GetRelationDetailsOfRelatedCase | GetRelationDetailsOfRelatedCase.sql | AnimalRelations | Slice 9 | |
| GetRelationsByBatchID | GetRelationsByBatchID.sql | AnimalRelations | Slice 9 | |
| GetRelationsByRBSE | GetRelationsByRBSE.sql | AnimalRelations | Slice 9 | |
| GetRelationsDetailsByRBSE | GetRelationsDetailsByRBSE.sql | AnimalRelations | Slice 9 | |
| GetSearchCase | GetSearchCase.sql | Search | Slice 6 | |
| GetSearchCaseByCPHH | GetSearchCaseByCPHH.sql | Search | Slice 6 | |
| GetSearchCaseByEartagHerdmark | GetSearchCaseByEartagHerdmark.sql | Search | Slice 6 | |
| GetSearchFarm | GetSearchFarm.sql | Search | Slice 6 | |
| GetSearchOutstandingBSE1s | GetSearchOutstandingBSE1s.sql | Search | Slice 6 | |
| GetSearchOutstandingFates | GetSearchOutstandingFates.sql | Search | Slice 6 | |
| GetSearchOutstandingResults | GetSearchOutstandingResults.sql | Search | Slice 6 | |
| GetSearchRelatedAnimals | GetSearchRelatedAnimals.sql | Search | Slice 6 | |
| GetSireDetailsByRBSE | GetSireDetailsByRBSE.sql | AnimalRelations | Slice 9 | |
| GetSupplierByName | GetSupplierByName.sql | ReferenceData | Slice 2 | |
| GetTestByRBSE | GetTestByRBSE.sql | CaseManagement | Slice 8 | |
| GetUserByNTLogin | GetUserByNTLogin.sql | UserManagement | Slice 3 | NTLogin → UPN fallback during transition period |
| GetUsers | GetUsers.sql | UserManagement | Slice 3 | |
| GetVetnetDetailsByCPHH | GetVetnetDetailsByCPHH.sql | FarmManagement | Slice 5 | Reads VetNet herdmark data; confirm if still used |
| GetXYReferenceByPrefixCode | GetXYReferenceByPrefixCode.sql | ReferenceData | Slice 2 | Geo lookup |
| MoveCase | MoveCase.sql | CaseManagement | Slice 8 | |
| SetMinuteSentDate | SetMinuteSentDate.sql | CaseWork | Slice 10 | |
