# BSE Application — Full Parity Analysis Report

**Produced:** 2026-08-12  
**Legacy system:** `bsenet-v2-2025-10` (ASP.NET Web Forms, VB.NET, .NET Framework 4.x)  
**Migrated system:** `bse` (ASP.NET Core 10, C#, Razor Pages, GOV.UK Frontend v6.2.0)  
**Analysis scope:** All modules, screens, workflows, business rules, and CRUD operations

---

## Executive Summary

The migrated BSE application covers the core day-to-day workflows but has **significant functional gaps** in document generation, sub-entity editing (feeds, clinical visits, relations, test results), batch reporting, and several case-entry sub-workflows. Of the 87 legacy `.aspx` pages, approximately **38 functional screens** have been migrated (some consolidated), while **26 features/screens are missing or only partially implemented**. Fourteen legacy screens are housekeeping/infrastructure that do not require direct equivalents.

| Category | Count |
|---|---|
| Fully migrated | 38 screens / equivalents |
| Partially migrated | 7 screens |
| Not migrated — **action required** | 26 features/screens |
| Not required (popups, redirect helpers, error pages) | 16 screens |

---

## 1. Screen-by-Screen Parity Map

### 1.1 Core Navigation and Infrastructure

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `Home.aspx` | `/Home` | ✅ Complete | Role-guarded panels for VLA/DEFRA groups; batch entry panel present |
| `Redirect.aspx` | n/a | ✅ Not needed | Session-based redirect no longer required in stateless Razor Pages |
| `SessionError.aspx` | `/Error` | ✅ Equivalent | Standard error page covers session/app error scenarios |
| `AppError.aspx` | `/Error` | ✅ Equivalent | — |
| `Help.aspx` / `help.htm` | ❌ Missing | ❌ **Gap** | No help page exists in the migrated application |
| `CalendarPopup.aspx` | n/a — HTML `<input type="date">` | ✅ Replaced | Native browser date picker is GDS-compliant |
| `ExitConfirmationPopup.aspx` | n/a | ✅ Not needed | Stateless POST/Redirect/GET pattern makes unsaved-change tracking unnecessary |
| `BatchNumberDisplayPopup.aspx` | Inline on `/Home` | ✅ Equivalent | Batch panel on Home page shows batch numbers inline |

---

### 1.2 Case Management

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `ShowCase.aspx` | `/Case/Details` | ✅ Complete | Combined case+farm view maintained; GB/non-GB display handled |
| `CaseEntryFarm.aspx` | `/Case/Farm` | ⚠️ Fixed | **3 parity gaps resolved (2026-08):** (1) Page heading said "Case:" — legacy `lblRBSEHeader` shows "RBSE Number:" — corrected. (2) Farm section heading said "Farm:" — legacy `lblCPHH` shows "CPHH:" — corrected. (3) Legacy `BatchNumberDisplay` control showed batch numbers linked to the case (VLA users) — absent in migrated page — added inline batch numbers card via `IBatchRepository.GetBatchNumbersByRbseAsync`. |
| `CaseEntryDEFRA.aspx` | `/Case/Edit` | ✅ Complete | Eartag, dates, fate, paperwork flags — all fields present in `CaseRecord` and `Edit.cshtml` |
| `CaseEntryVLA.aspx` | `/Case/Edit` | ⚠️ Partial | Core VLA fields (birth date, purchase date, onset date, slaughter date, months pregnant/post-calving) are in `Edit.cshtml`. **Missing:** *Other Owners (previous owners) sub-grid* — no UI to add/edit/delete previous owner records. |
| `CaseEntryBAB.aspx` | `/Case/Edit` | ⚠️ Partial | BAB flag exists in `CaseRecord.IsBAB`. **Missing:** *Traced CPHH, traced farm details (name, address, feed risk, animal origin notes)* — these BAB-specific traced-farm fields have no UI. |
| `CaseEntryClinical.aspx` | ❌ Missing | ❌ **Gap** | Clinical visit records (date, inspector, findings) are not displayed or editable in the migrated application. The `AnimalRelations` module and database are present but there is no UI page or sub-form. |
| `CaseEntryFeeds.aspx` | ❌ Missing | ❌ **Gap** | Feed records (year from/to, ration type, supplier, pre-purchase flag) have no UI. Service layer references exist but no page or sub-form is implemented. |
| `CaseEntryRelations.aspx` | ❌ Missing | ❌ **Gap** | Animal relations (dam/sire linkage, offspring relations grid) are fully absent from the UI. `IAnimalRelationsService` and all repository methods exist; zero front-end pages. `PickSireDam.aspx` helper also missing. |
| `CaseEntrySave.aspx` | `/Case/New` | ✅ Complete | New case creation with RBSE, CPHH, core fields; transactional SP |
| `NonGBCaseCreation.aspx` | ❌ Missing | ❌ **Gap** | Dedicated non-GB case creation workflow (non-GB eartag, fate, final result, slaughter date, non-GB CPHH lookup) is absent. `CaseRecord.IsNonGbCase` flag exists in the model but no creation path. |
| `DeleteCase.aspx` | `/Case/Delete` | ✅ Complete | Confirmation + cascading delete |
| `MoveCase.aspx` | `/Case/MoveCase` | ✅ Complete | Move to existing CPHH |
| `MoveCaseNewFarm.aspx` | ❌ Missing | ❌ **Gap** | Move case to a new (not-yet-existing) farm is absent. Legacy allowed creating a new farm inline during a case move. Migrated `/Case/MoveCase` only accepts an existing CPHH. |
| `RBSEChange.aspx` | `/Case/RbseChange` | ✅ Complete | Cascades rename across all 15 child tables |
| `FinalResultEntry.aspx` | ❌ Missing | ❌ **Gap** | Dedicated page for entering and editing test results (test type, test result, date, retrospective date) with a test-results grid is completely absent. `CaseRecord.FinalResult` and `FinalResultDate` fields exist but are not editable in the UI. `GetFinalResultAsync` service method exists unused. |
| `FinalResultConfirmation.aspx` | ❌ Missing | ❌ **Gap** | Confirmation step for final result entry — absent (follows from above). |

---

### 1.3 Farm Management

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `NewFarm.aspx` | `/Farm/New` | ✅ Complete | — |
| `CPHHChange.aspx` | `/Farm/CphhChange` | ✅ Complete | — |
| `PickFarm.aspx` | `/Farm/Lookup` | ✅ Equivalent | Lookup helper page replaces modal popup |

---

### 1.4 Search

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `SearchMenu.aspx` | `/Home` (navigation links) | ✅ Not needed | Navigation from Home page replaces a dedicated menu |
| `SearchCase.aspx` | `/Search/Cases` | ⚠️ Partial | All 11 filter fields present. **Missing:** pagination; column sorting; result count badge |
| `SearchCaseByHerdmark.aspx` | `/Search/CasesByHerdmark` | ⚠️ Partial | **Missing:** pagination |
| `SearchCPHH.aspx` | ❌ Missing | ❌ **Gap** | Dedicated CPHH search page (search by parish/county/herdmark) is absent. Legacy `SearchCPHH` allowed officers to look up farms by CPHH components separately from `SearchFarm`. |
| `SearchFarm.aspx` | `/Search/Farms` | ⚠️ Partial | All filter fields present. **Missing:** pagination |
| `SearchOutstandingData.aspx` | `/Search/Outstanding` | ⚠️ Partial | Results sections present. **Missing:** tabbed UI with count badges |
| `SearchRelatedAnimal.aspx` | `/Search/RelatedAnimals` | ⚠️ Partial | **Missing:** pagination |
| — | `/Search/CasesByHoldingHerdmark` | ✅ New | Added in migration — not in legacy |

---

### 1.5 Audit Log

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `AuditLogMenu.aspx` | Home / navigation | ✅ Not needed | Direct page links replace a separate menu |
| `AuditLogByDate.aspx` | `/AuditLog/ByDate` | ✅ Complete | GDS date input replaces calendar popup |
| `AuditLogByUser.aspx` | `/AuditLog/ByUser` | ⚠️ Partial | **Missing:** pagination |
| `AuditLogCaseMoves.aspx` | `/AuditLog/CaseMoves` | ✅ Complete | — |
| `AuditLogCPHHChanges.aspx` | `/AuditLog/CphhChanges` | ✅ Complete | — |
| `AuditLogNewFarms.aspx` | `/AuditLog/NewFarms` | ✅ Complete | — |
| `AuditLogRBSEChanges.aspx` | `/AuditLog/RbseChanges` | ✅ Complete | — |
| `CaseAuditLogReport.aspx` | ❌ Missing | ❌ **Gap** | Per-case audit log report (case-level history printout) not implemented |
| `FarmAuditLogReport.aspx` | ❌ Missing | ❌ **Gap** | Per-farm audit log report not implemented |

---

### 1.6 CaseWork

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `CaseWorkMenu.aspx` | `/CaseWork/Menu` | ✅ Complete | Summary view with links to minute letters |
| `CaseWorkEntry.aspx` | `/CaseWork/Entry` | ✅ Complete | All 25+ date/text fields present; save handler implemented |
| `CaseWorkMinuteConfirmation.aspx` | `/CaseWork/Minute` | ✅ Complete | Displays minute details; "Mark as sent today" POST handler |
| `CaseWorkOpenReport.aspx` | ❌ Missing | ❌ **Gap** | Open cases report (list of all open cases for case-work review) not implemented. VLA Maintenance only. |
| `CaseWorkClosedReport.aspx` | ❌ Missing | ❌ **Gap** | Closed cases report not implemented |
| `MaintenanceConfirmation.aspx` | ❌ Missing | ❌ **Gap** | Maintenance confirmation step (used with open/closed case reports) not implemented |

---

### 1.7 BSESS Integration

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `BSESSMenu.aspx` | Home / navigation | ✅ Not needed | BSESS check pages linked directly |
| `BSESSCheckByDate.aspx` | `/Bsess/CheckByDate` | ✅ Complete | — |
| `BSESSCheckByRBSE.aspx` | `/Bsess/CheckByRbse` | ✅ Complete | — |

---

### 1.8 ADNS Export

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `ADNSExportMenu.aspx` | `/AdnsExport/Menu` | ✅ Complete | — |
| `ADNSExportGB.aspx` | `/AdnsExport/Gb` | ✅ Complete | — |
| `ADNSExportCI.aspx` | `/AdnsExport/Ci` | ✅ Complete | — |
| `ADNSExportNI.aspx` | `/AdnsExport/Ni` | ✅ Complete | — |

---

### 1.9 OSS Export

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `OSSExportMenu.aspx` | `/OssExport/Menu` | ⚠️ Partial | Menu + cover sheet lookup + staging table population implemented. **Missing:** BSE1 form download (actual file export). |
| `OSSExportBSE1.aspx` | ❌ Missing | ❌ **Gap** | The BSE1 export form (batch number input → download BSE1 data file) is absent. Legacy generated a downloadable export file. |
| `OSSExportBSE1b.aspx` | ❌ Missing | ❌ **Gap** | Extended BSE1b export not implemented. |
| `OSSExportBSE1Download.aspx` | ❌ Missing | ❌ **Gap** | File download endpoint for OSS BSE1 export data not implemented. |

---

### 1.10 Report Generation and Document Output

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `ActiveMemo.aspx` | `/CaseWork/Minute` (view only) | ⚠️ Partial | Minute details are displayed in `/CaseWork/Minute`. **Missing:** Word document (`.doc`) download — legacy streamed a `.doc` file via `application/vnd.ms-word`. No document generation exists in the migrated app. |
| `ActiveMemoFallenStock.aspx` | `/CaseWork/Minute` (AMFS type) | ⚠️ Partial | AMFS link listed in `/CaseWork/Menu`. **Missing:** Word document download. |
| `AnnexA.aspx` | `/CaseWork/Minute` (AnnexA type) | ⚠️ Partial | View only. **Missing:** Word document download. |
| `AnnexB.aspx` | `/CaseWork/Minute` (AnnexB type) | ⚠️ Partial | View only. **Missing:** Word document download. |
| `AnnexC.aspx` | `/CaseWork/Minute` (AnnexC type) | ⚠️ Partial | View only. **Missing:** Word document download. |
| `AnnexD.aspx` | `/CaseWork/Minute` (AnnexD type) | ⚠️ Partial | View only. **Missing:** Word document download. |
| `ResultMemo.aspx` | ❌ Missing | ❌ **Gap** | Result memorandum document (generated from RBSE via query string) is entirely absent. |
| `PrintBatch.aspx` | ❌ Missing | ❌ **Gap** | Batch print menu (select batch number + report type from: Clinical, Farm/Case, Feeds, Offspring, Pedigree) is absent. |
| `ReportCaseFarm.aspx` | ❌ Missing | ❌ **Gap** | Farm and case Word document report for a batch is absent. |
| `ReportClinical.aspx` | ❌ Missing | ❌ **Gap** | Clinical report Word document for a batch is absent. |
| `ReportFeeds.aspx` | ❌ Missing | ❌ **Gap** | Feeds report Word document for a batch is absent. |
| `ReportOffspring.aspx` | ❌ Missing | ❌ **Gap** | Offspring report Word document for a batch is absent. |
| `ReportPedigree.aspx` | ❌ Missing | ❌ **Gap** | Pedigree report Word document for a batch is absent. |
| `ExcelExport.aspx` | ❌ Missing | ❌ **Gap** | Generic Excel export of data grid results (streamed as `.xls`) is absent. Legacy used this for search result exports. |

---

### 1.11 Pick List / Reference Data Maintenance

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `PickListMaintenance.aspx` | `/Admin/PickLists` + `/Admin/PickListEdit` | ✅ Complete | Consolidated single UI covers all editable lookup tables via dropdown; all table IDs accessible |
| `PickListMaintenanceAHO.aspx` | `/Admin/PickListEdit?tableId=…` | ✅ Consolidated | Specific AHO table accessible via generic edit page |
| `PicklistMaintenanceAHRO.aspx` | `/Admin/PickListEdit?tableId=…` | ✅ Consolidated | — |
| `PickListMaintenanceBreed.aspx` | `/Admin/PickListEdit?tableId=…` | ✅ Consolidated | — |
| `PickListMaintenanceBSECounty.aspx` | `/Admin/PickListEdit?tableId=…` | ✅ Consolidated | — |
| `PickListMaintenanceRelationFate.aspx` | `/Admin/PickListEdit?tableId=…` | ✅ Consolidated | — |
| `PickListMaintenanceSupplier.aspx` | `/Admin/PickListEdit?tableId=…` | ✅ Consolidated | — |
| `PickListMaintenanceTestType.aspx` | `/Admin/PickListEdit?tableId=…` | ✅ Consolidated | — |
| `PickListMaintenanceTSETestingSite.aspx` | `/Admin/PickListEdit?tableId=…` | ✅ Consolidated | — |

---

### 1.12 Helper / Picker Pages

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `PickFarm.aspx` | `/Farm/Lookup` | ✅ Equivalent | — |
| `PickSireDam.aspx` | ❌ Missing | ❌ **Gap** | No sire/dam picker UI. Required by the missing relations management screen. |
| `PickSupplier.aspx` | ❌ Missing | ❌ **Gap** | Supplier picker used in feed entry is absent (follows from missing Feeds UI). |
| `RelationsPopup.aspx` | ❌ Missing | ❌ **Gap** | Relations popup (view/edit related animals) is absent. |

---

### 1.13 User Management

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `UserMaintenance.aspx` | `/Admin/Users` | ⚠️ Partial | Add user, list users, inline edit per row implemented. **Known gaps:** UPN column always blank (SP not updated); `luUserGroup` display names use legacy values ("DEFRA Viewer") not role names ("Admin"). |

---

## 2. CRUD Operation Parity

### 2.1 Case Management CRUD

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| Create case (GB) | `CaseEntrySave.aspx` | `/Case/New` | ✅ |
| Create case (non-GB) | `NonGBCaseCreation.aspx` | ❌ No UI | ❌ **Gap** |
| Read case | `CaseEntryFarm/DEFRA/VLA/BAB.aspx` | `/Case/Details` | ✅ |
| Update case (core fields) | All `CaseEntry*.aspx` tabs | `/Case/Edit` | ✅ |
| Update case — clinical visits (add/edit/delete) | `CaseEntryClinical.aspx` | ❌ No UI | ❌ **Gap** |
| Update case — feed records (add/edit/delete) | `CaseEntryFeeds.aspx` | ❌ No UI | ❌ **Gap** |
| Update case — animal relations / dam / sire | `CaseEntryRelations.aspx` | ❌ No UI | ❌ **Gap** |
| Update case — other owners / previous owners | `CaseEntryVLA.aspx` sub-grid | ❌ No UI | ❌ **Gap** |
| Update case — test results | `FinalResultEntry.aspx` | ❌ No UI | ❌ **Gap** |
| Update case — BAB traced details | `CaseEntryBAB.aspx` | ❌ No UI for traced fields | ⚠️ Partial |
| Delete case | `DeleteCase.aspx` | `/Case/Delete` | ✅ |
| Move case (existing farm) | `MoveCase.aspx` | `/Case/MoveCase` | ✅ |
| Move case (new farm) | `MoveCaseNewFarm.aspx` | ❌ No UI | ❌ **Gap** |
| Change RBSE | `RBSEChange.aspx` | `/Case/RbseChange` | ✅ |

### 2.2 Farm Management CRUD

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| Create farm | `NewFarm.aspx` | `/Farm/New` | ✅ |
| Read farm | Combined on CaseEntry | `/Farm/Details` | ✅ |
| Update farm | Via `CaseEntryFarm` fields | `/Farm/Edit` | ✅ |
| Delete farm | No dedicated page (DB only) | No UI | n/a |
| Change CPHH | `CPHHChange.aspx` | `/Farm/CphhChange` | ✅ |

### 2.3 Animal Relations CRUD

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| View relations for case | `CaseEntryRelations.aspx` | ❌ No UI | ❌ **Gap** |
| Add relation | `CaseEntryRelations.aspx` | ❌ No UI | ❌ **Gap** |
| Edit relation | `CaseEntryRelations.aspx` | ❌ No UI | ❌ **Gap** |
| Delete relation | `CaseEntryRelations.aspx` | ❌ No UI | ❌ **Gap** |
| Set dam for case | `CaseEntryRelations.aspx` | ❌ No UI | ❌ **Gap** |
| Set sire for case | `CaseEntryRelations.aspx` | ❌ No UI | ❌ **Gap** |
| Remove dam / sire | `CaseEntryRelations.aspx` | ❌ No UI | ❌ **Gap** |

### 2.4 Feed Records CRUD

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| View feeds for case | `CaseEntryFeeds.aspx` | ❌ No UI | ❌ **Gap** |
| Add feed record | `CaseEntryFeeds.aspx` | ❌ No UI | ❌ **Gap** |
| Edit feed record | `CaseEntryFeeds.aspx` | ❌ No UI | ❌ **Gap** |
| Delete feed record | `CaseEntryFeeds.aspx` | ❌ No UI | ❌ **Gap** |

### 2.5 Clinical Visits CRUD

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| View clinical visits | `CaseEntryClinical.aspx` | ❌ No UI | ❌ **Gap** |
| Add clinical visit | `CaseEntryClinical.aspx` | ❌ No UI | ❌ **Gap** |
| Edit clinical visit | `CaseEntryClinical.aspx` | ❌ No UI | ❌ **Gap** |
| Delete clinical visit | `CaseEntryClinical.aspx` | ❌ No UI | ❌ **Gap** |

### 2.6 Test Results CRUD

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| View test results for case | `FinalResultEntry.aspx` | ❌ No dedicated UI | ❌ **Gap** |
| Add test result | `FinalResultEntry.aspx` | ❌ No UI | ❌ **Gap** |
| Edit test result | `FinalResultEntry.aspx` | ❌ No UI | ❌ **Gap** |
| Delete test result | `FinalResultEntry.aspx` | ❌ No UI | ❌ **Gap** |
| Set final result | `FinalResultEntry.aspx` | ❌ No UI | ❌ **Gap** |

### 2.7 CaseWork CRUD

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| View casework entry | `CaseWorkMenu.aspx` | `/CaseWork/Menu` | ✅ |
| Edit casework entry | `CaseWorkEntry.aspx` | `/CaseWork/Entry` | ✅ |
| View minute details | `CaseWorkMinuteConfirmation.aspx` | `/CaseWork/Minute` | ✅ |
| Mark minute as sent | `CaseWorkMinuteConfirmation.aspx` | `/CaseWork/Minute` POST | ✅ |
| View open cases report | `CaseWorkOpenReport.aspx` | ❌ No UI | ❌ **Gap** |
| View closed cases report | `CaseWorkClosedReport.aspx` | ❌ No UI | ❌ **Gap** |

### 2.8 Reference Data (Pick Lists) CRUD

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| List all editable lookups | `PickListMaintenance.aspx` | `/Admin/PickLists` | ✅ |
| Edit lookup item | `PickListMaintenance*.aspx` | `/Admin/PickListEdit` | ✅ |
| Add lookup item | `PickListMaintenance*.aspx` | `/Admin/PickListEdit` | ✅ |
| Delete lookup item | `PickListMaintenance*.aspx` | `/Admin/PickListEdit` | ✅ |

### 2.9 User Management CRUD

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| List users | `UserMaintenance.aspx` | `/Admin/Users` | ✅ |
| Add user | `UserMaintenance.aspx` | `/Admin/Users` | ✅ |
| Edit user (group, active) | `UserMaintenance.aspx` | `/Admin/Users` (inline) | ✅ |
| Edit user UPN | `UserMaintenance.aspx` | `/Admin/Users` | ⚠️ UPN not saved — SP gap |

### 2.10 Export / Report Generation

| Operation | Legacy | Migrated | Status |
|---|---|---|---|
| Export to Excel | `ExcelExport.aspx` | ❌ No equivalent | ❌ **Gap** |
| Generate Active Memo (Word) | `ActiveMemo.aspx` | ❌ View only | ❌ **Gap** |
| Generate Annex A–D (Word) | `AnnexA–D.aspx` | ❌ View only | ❌ **Gap** |
| Generate Result Memo (Word) | `ResultMemo.aspx` | ❌ No UI | ❌ **Gap** |
| Generate Fallen Stock Memo (Word) | `ActiveMemoFallenStock.aspx` | ❌ View only | ❌ **Gap** |
| Print batch reports (5 types) | `PrintBatch.aspx` + `Report*.aspx` | ❌ No UI | ❌ **Gap** |
| OSS BSE1 export file download | `OSSExportBSE1*.aspx` | ❌ No download | ❌ **Gap** |
| ADNS XML/file export | `ADNSExport*.aspx` | `/AdnsExport/*` | ✅ |

---

## 3. Known Issues from Existing Migration Status Report

The following issues were already identified in `BSE-Migration-Status-Report.md` and remain **outstanding**:

| # | Issue | Severity | Status |
|---|---|---|---|
| 1 | UPN column always blank in `/Admin/Users` — `GetUsers` SP does not SELECT `[UPN]` | Medium | ⚠️ Outstanding |
| 2 | `luUserGroup` legacy display names ("DEFRA Viewer") do not match application role names ("Admin") | Medium | ⚠️ Outstanding |
| 3 | No pagination on any search results page — unbounded result sets | Medium | ⚠️ Outstanding |
| 4 | No column sorting on search results | Low | ⚠️ Outstanding |
| 5 | Tests sub-grid on `/Case/Details` is read-only (no edit) | Medium | ⚠️ Outstanding |
| 6 | Feeds sub-grid not verified / not shown on case pages | High | ⚠️ Outstanding |
| 7 | Azure AD OIDC not configured — `DevelopmentAuthHandler` bypass active | High (Security) | ⚠️ Outstanding |
| 8 | `DevelopmentAuthHandler` has no environment guard — must not reach production | High (Security) | ⚠️ Outstanding |
| 9 | `EditUser` SP does not accept UPN — UPN changes cannot be saved | Medium | ⚠️ Outstanding |
| 10 | Redis / distributed cache not configured in dev | Medium | ⚠️ Outstanding |
| 11 | `BSE.Host` has zero page model unit tests | Medium | ⚠️ Outstanding |
| 12 | 128 SQL71558 warnings (case-sensitivity) in database project | Low | ⚠️ Outstanding |

---

## 4. Consolidated Gap List — Actions Required for Full Parity

### Priority 1 — Critical: Core Business Functionality

| # | Gap | Recommended Action | Effort |
|---|---|---|---|
| G01 | **Animal Relations UI** — No page to add/edit/delete dam, sire, and related animals | Create `/Case/Relations` Razor Page with: dam/sire search & assignment form; relations grid with add/edit/delete; picker for sire/dam (`/Animal/PickSireDam`). Wire to existing `IAnimalRelationsService`. | High |
| G02 | **Feed Records UI** — No page to add/edit/delete feed records per case | Create feed sub-form (or tab section on `/Case/Edit`) with grid; wire to feed repository/service. Implement supplier picker (`/Supplier/Lookup`). | High |
| G03 | **Clinical Visits UI** — No page to add/edit/delete clinical visits per case | Create clinical visit sub-form with grid (date, inspector, notes fields). Wire to clinical visit repository/service. | Medium |
| G04 | **Final Result / Test Results UI** — No page to enter or manage test results | Create `/Case/FinalResult` Razor Page with test type, result, date fields and test-results grid; wire to `GetFinalResultAsync` and add commands for add/edit/delete test results. | High |
| G05 | **Non-GB Case Creation** — No dedicated workflow | Create `/Case/NewNonGb` Razor Page mirroring `NonGBCaseCreation.aspx` fields (non-GB eartag, fate, final result, slaughter date, non-GB CPHH/owner lookup). Reuse `CreateCaseAsync` with `IsNonGbCase = true`. | Medium |
| G06 | **Move Case to New Farm** — Move only supports existing CPHH | Extend `/Case/MoveCase` with a "Create new farm" inline option or link to `/Farm/New` that returns the new CPHH back to the move workflow. | Medium |
| G07 | **BAB Traced-Farm fields** — BAB-specific traced details (traced CPHH, owner name/address, feed risk) have no UI | Add a "BAB details" section to `/Case/Edit` for fields: `IsBAB`, traced CPHH/name/address, animal origin notes. Confirm DB column mapping. | Medium |
| G08 | **Other Owners (Previous Owners) sub-grid** — VLA tab other-owners records not shown | Add previous-owner records display and CRUD sub-form to `/Case/Details` or `/Case/Edit`. Identify SP for previous-owner data. | Medium |
| G25 | ✅ **RESOLVED** — **Case sub-page navigation bar** — `/Case/Details` had no links to the case sub-sections. A `govuk-list govuk-list--inline` nav bar was added to `Case/Details.cshtml` linking to: Test results, Clinical, Feeds, Relations, BAB, Other owners, Case work. | — | — |
| G26 | ✅ **RESOLVED** — **Farm section — incomplete field set on `/Case/Details` and `/Farm/Details`** — District, Map reference, Correspondence address/postcode, Numeric Herdmark 1 & 2, Pedigree type, and two-column layout with sub-section headings were added. An "Edit farm" button was added to the inline farm section on `/Case/Details`. | — | — |
| G27 | **Case Herdbook field not editable** — `CaseRecord.Herdbook` and `CaseRecord.PedigreeRowStamp` exist but the field is absent from all edit surfaces. In the legacy app, `CaseHerdbook` was saved via the `AddEditDamSireDetails` SP (called from `CaseEntryRelations.aspx`). The migrated `AddEditDamSireCommand` and `PedigreeRepository.AddEditDamSireAsync` do not include `CaseHerdbook` or the required concurrency parameters (`DamID`, `DamRowStamp`, `SireID`, `SireRowStamp`, `CaseRowStamp`). | Add `CaseHerdbook` to `AddEditDamSireCommand`; pass `CaseHerdbook` + all missing concurrency params in `PedigreeRepository`; add `CaseHerdbook` text input to `Case/Relations.cshtml` edit form; load `CasePedigreeRowStamp` from `CaseRecord` in `RelationsModel`. | Medium |
| G29 | **Linked farms not shown on inline farm section of `/Case/Details`** — The legacy Farm tab showed a Linked Farms sub-grid when viewing a case. The migrated `/Farm/Details` page has the related-farms table but `/Case/Details` only shows a "Full farm details" link — the linked farms are invisible from the case view. | Load `GetRelatedFarmsAsync(cphh)` in `Case/Details.cshtml.cs`; render a linked-farms table after the farm section. ✅ **Applied this session.** | Low |
| G30 | **Authority County and Local Authority display names not resolved** — `FarmRecord.AuthorityCountyID` and `FarmRecord.AuthorityID` are integer FKs. No display name is shown anywhere in the migrated app. The legacy Farm tab showed these as cascading dropdowns (Authority County → Local Authority → ADNS Region). The `GetluAuthorityAll` SP does not exist in the migrated database; only `GetluAuthorityByAuthorityCounty` exists (county-filtered). | Create `GetluAuthorityAll` SP; add `GetAllAuthoritiesAsync` to `ILookupRepository` and `LookupDataService`; load in `Farm/Details.cshtml.cs` and `Case/Details.cshtml.cs`; display resolved names. | Low |
| G31 | **ADNS Region name not shown on `/Farm/Details` or `/Case/Details`** — `FarmRecord.ADNSRegionID` is stored as an integer FK. `ILookupDataService.GetADNSRegionsAsync()` already exists. Neither `Farm/Details` nor `Case/Details` loads or displays the resolved ADNS Region name. | Load `GetADNSRegionsAsync()` in `Farm/Details.cshtml.cs`; resolve and display name in farm herd details section. ✅ **Applied this session.** | Low |
| G32 | **Herd Size grid absent from `/Farm/Details`** — The legacy Farm tab displayed a `grdHerdSize` grid (annual herd size: HerdYear, TotalSize, Lactation1Size–Lactation10PlusSize) and a confirmed-case count. The migrated `IFarmService.GetHerdSizesAsync` and `GetConfirmedCaseCountAsync` both exist but are never called and no page renders this data. | Add herd-sizes and confirmed-case-count loading to `Farm/Details.cshtml.cs`; render GOV.UK summary list + table. ✅ **Applied this session.** | Low |
| G33 | **Audit Log shortcut absent from `/Case/Details`** — The legacy `CaseEntryDEFRA.aspx` had a direct "Case Audit log" button. The migrated `/AuditLog/ByCase` page exists but there is no contextual link from the case view. | Add an "Audit log" link to the case sections nav bar on `Case/Details.cshtml`. ✅ **Applied this session.** | Low |

### Priority 2 — High: Reporting and Document Export

| # | Gap | Recommended Action | Effort |
|---|---|---|---|
| G09 | **Minute document download (Word)** — Active Memo, Annex A–D, AMFS display data but do not export `.doc` | Implement HTML-to-PDF/DOCX generation using Playwright/Razor (per `pdf-html-migration` skill). Add download handler to `/CaseWork/Minute`. Legacy used `application/vnd.ms-word` streaming — recommend Razor → PDF as modern equivalent. | High |
| G10 | **Result Memo document** — Entirely absent | Create `/CaseWork/ResultMemo` Razor Page with Word/PDF download, matching `ResultMemo.aspx` content. | Medium |
| G11 | **Print Batch — batch reports (5 types)** — No UI for Clinical/Farm/Feeds/Offspring/Pedigree batch reports | Create `/Batch/PrintBatch` page with batch number input and report type selector. Create 5 report Razor views. Wire to `IBatchService.GetCaseDetailsByBatchIdAsync` and relevant sub-data services. | High |
| G12 | **OSS BSE1 export file** — `OssExport/Menu` covers staging population but not the BSE1 download | Add BSE1 batch number input and download handler to `/OssExport/Menu` (or a new `/OssExport/Bse1` page). Wire to existing `IOssExportService.GetExportDetailsByRbseAsync`. | Medium |
| G13 | **Excel export** — No data export capability | Add CSV/Excel export buttons to search result pages (`/Search/Cases`, `/Search/Farms`) using `CsvHelper` or `ClosedXML`. Minimal approach: CSV download of current search results. | Medium |
| G14 | **Case Audit Log report** (`CaseAuditLogReport.aspx`) — Per-case audit history report | Add case audit trail section to `/Case/Details` or create `/AuditLog/CaseReport/{rbse}` page wired to audit log service. | Low |
| G15 | **Farm Audit Log report** (`FarmAuditLogReport.aspx`) — Per-farm audit history report | Add farm audit trail section to `/Farm/Details` or create `/AuditLog/FarmReport/{cphh}` page. | Low |
| G16 | **CaseWork Open/Closed reports** — VLA Maintenance user reports | Create `/CaseWork/OpenReport` and `/CaseWork/ClosedReport` Razor Pages. Wire to `ICaseWorkService` with appropriate SPs. | Low |

### Priority 3 — Medium: Search and UX Parity

| # | Gap | Recommended Action | Effort |
|---|---|---|---|
| G17 | **Pagination** — All 6 search pages return unbounded results | Implement GOV.UK-style pagination (server-side) on `/Search/Cases`, `/Search/Farms`, `/Search/CasesByHerdmark`, `/Search/CasesByHoldingHerdmark`, `/Search/RelatedAnimals`, `/AuditLog/ByUser`. Use `OFFSET/FETCH` in SPs. | High |
| G18 | **CPHH Search page** — Dedicated CPHH lookup absent | Create `/Search/Cphh` Razor Page with CPHH component search (parish/county/herdmark) wired to a new SP or existing farm search. | Low |
| G19 | **Help page** — No help documentation | Create `/Help` Razor Page with appropriate guidance content (or link to external documentation). | Low |
| G20 | **Outstanding page tab/count badges** — Tabbed UI with case counts per category | Refactor `/Search/Outstanding` to use GOV.UK tabs component with count badges per outstanding data category. | Low |

### Priority 4 — Database / Infrastructure Fixes

| # | Gap | Recommended Action | Effort |
|---|---|---|---|
| G21 | **UPN in GetUsers SP** — UPN column not returned | Update `GetUsers` SP to `SELECT [UPN]`; update `EditUser` SP to accept and persist UPN; remove `null` placeholder in `UserRepository`. | Low |
| G22 | **luUserGroup display names** | Either update `luUserGroup` seed data to use role names (Admin, DataEntry, Supervisor, Viewer) or add a UI-level mapping dictionary. Requires product owner decision. | Low |
| G23 | **OIDC configuration** — `DevelopmentAuthHandler` bypass active | Configure Azure AD OIDC in `appsettings.json` (with placeholder keys); add environment guard ensuring `DevelopmentAuthHandler` is never registered in `Production` environment. | High (Security) |
| G24 | **Page model unit tests** — Zero coverage for `[Authorize]`, form POST handlers | Add xUnit page model tests for at minimum: `Case/New`, `Case/Edit`, `Farm/New`, `Admin/Users` — covering authorization enforcement and form submission paths. | Medium |

---

## 5. Business Rule Verification

### 5.1 Rules Confirmed in Migrated Application

| Rule | Legacy Location | Migrated Location | Status |
|---|---|---|---|
| RBSE format validation (9-digit) | `EartagValidation.vb` | `BSE.SharedKernel` | ✅ |
| Role-based access (VLA vs DEFRA vs Admin) | `VLAHeader.ascx` `GetUserDetails()` | `[Authorize(Policy="...")]` on page models | ✅ |
| Transactional case creation (all child records or none) | `CaseEntrySave` multi-SP | `CreateCaseAsync` in transaction | ✅ |
| Optimistic concurrency on case edit | `RowStamp` timestamp | `CaseRecord.RowStamp` + `EditCaseAsync` | ✅ |
| RBSE cascade rename (15 child tables) | `RBSEChange.aspx` | `ChangeRbseAsync` | ✅ |
| Eartag three-part format (Country/Herdmark/Number) | `ThreePartEartag.ascx` | Separate input fields in forms | ✅ |
| Partial/estimated date handling | `PartialDate.ascx` | `IsBirthDateEst`, `IsOnsetDateEst` flags | ✅ |
| CPHH format (Parish/County/Herdmark) | `CPHH.ascx` | `/Farm/CphhChange`, farm form fields | ✅ |
| Batch number auto-creation | `BatchNumber.ascx` | `IBatchService.GetOrCreateBatchNumberAsync` | ✅ |
| Arithabort SET ON for SQL plan stability | Not in legacy (implicit) | `DapperRepository.OpenConnection()` | ✅ Fixed |

### 5.2 Business Rules Not Verified or Potentially Absent

| Rule | Legacy Source | Migrated Status | Risk |
|---|---|---|---|
| Non-GB case rules (fate defaults to SL, final result defaults to NE) | `NonGBCaseCreation.aspx` | No UI path — cannot verify | High |
| Feed pre-purchase flag logic | `CaseEntryFeeds.aspx` | No UI | High |
| Relation type constraints (offspring cannot be its own sire) | `CaseEntryRelations.aspx` | No UI — service constraints unverified | Medium |
| Clinical visit date uniqueness per case | `CaseEntryClinical.aspx` | No UI | Medium |
| Maintenance confirmation workflow (open/closed report actions) | `MaintenanceConfirmation.aspx` | Not implemented | Low |
| BSESS import job scheduling / retry | `BSEIntegrationServices` SSIS | `BsessImportJobTests` exists | Medium |

---

## 6. Recommendations

### Resolved Since Last Report (this session — August 2026)

- **G25** ✅ Case sub-page navigation bar added to `/Case/Details`
- **G26** ✅ Farm section field completeness and two-column layout fixed on `/Case/Details` and `/Farm/Details`; "Edit farm" button added
- **G28** ✅ Number of Offspring (ChildCount) already present in `/Case/Relations` — confirmed not a gap
- **G29** ✅ Linked farms now shown on inline farm section of `/Case/Details`
- **G31** ✅ ADNS Region name now displayed on `/Farm/Details` (resolved via `ILookupDataService.GetADNSRegionsAsync`)
- **G32** ✅ Herd Size grid and confirmed case count now shown on `/Farm/Details`
- **G33** ✅ Audit Log contextual link added to Case sections nav bar

### Immediate Actions (before production release)

1. **Implement Azure AD OIDC** and add an explicit environment guard on `DevelopmentAuthHandler` to prevent accidental deployment of the bypass. This is a **security blocker**.
2. **Implement Animal Relations UI** (G01) — this is a core daily workflow for VLA staff; its absence makes the migrated application functionally incomplete for case investigation tracking.
3. **Implement Clinical Visits and Feed Records UIs** (G02, G03) — required to fully replace the legacy multi-tab case entry workflow.
4. **Implement Final Result / Test Results UI** (G04) — the `FinalResult` and `FinalResultDate` fields are visible read-only but cannot be maintained without this page.
5. **Fix Case Herdbook editable field** (G27) — add `CaseHerdbook` to `AddEditDamSireCommand`, fix missing concurrency params in `PedigreeRepository.AddEditDamSireAsync` (`DamID`, `DamRowStamp`, `SireID`, `SireRowStamp`, `CaseRowStamp`), and add text input to `Case/Relations.cshtml`. Note: the current dam/sire edit form likely throws a runtime error due to missing required SP parameters; this bug must be fixed before the Relations page can be used in production.

### Near-term Actions (sprint backlog)

5. Implement document generation for minute letters (G09) using the Playwright PDF skill — Active Memo and Annex A–D are operational documents with legal significance.
6. Implement OSS BSE1 export file download (G12) — required for downstream OSS system integration.
7. Add server-side pagination to all search pages (G17) — performance risk on production-scale data.
8. Fix UPN column in `GetUsers` SP (G21) and add environment guard for `DevelopmentAuthHandler` (G23).
9. Implement Non-GB case creation workflow (G05).

### Planned Actions (future sprints)

10. Implement batch report printing — Print Batch + 5 report types (G11).
11. Implement Result Memo and Fallen Stock Memo document downloads (G10).
12. Excel/CSV export on search pages (G13).
13. CaseWork Open/Closed reports (G16).
14. Move Case to New Farm (G06).
15. Add page model unit tests (G24) — minimum 80% coverage target per project guidelines.
16. Other Owners sub-grid (G08) and BAB traced fields (G07).

---

## 7. Summary Scorecard

| Module | Legacy Pages | Migrated Pages | % Complete | Notes |
|---|---|---|---|---|
| Home / Navigation | 3 | 1 | 100% | Consolidated |
| Case Management (core) | 9 | 7 | 78% | Missing non-GB, move to new farm |
| Case Management (sub-entities) | 5 tabs | 0 sub-forms | 0% | Feeds, Clinical, Relations, Tests, Other Owners |
| Farm Management | 3 | 5 | 100% | Extra lookup page added |
| Search | 7 | 6 | 85% | Missing CPHH search; no pagination |
| Audit Log | 7 | 7 | 86% | Missing per-case/farm report pages |
| CaseWork | 5 | 3 | 60% | Missing Open/Closed reports; doc download |
| BSESS | 3 | 2 | 100% | Menu not needed |
| ADNS Export | 4 | 4 | 100% | — |
| OSS Export | 4 | 1 | 25% | BSE1 download missing |
| Document Generation | 10 | 0 downloads | 0% | View only; no Word/.doc output |
| Pick List Maintenance | 9 | 2 | 100% | Consolidated (all picklists accessible) |
| User Management | 1 | 1 | 80% | UPN gap |
| **Overall** | **87** | **38 equiv.** | **~57%** | See priority list above |

---

*Report generated by GitHub Copilot — August 2026. Review with BSE product owner and VLA staff before finalising sprint plan.*
