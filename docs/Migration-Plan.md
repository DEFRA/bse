# BSE System — Migration Plan

> **Classification:** Programme Governance — Confidential  
> **Date:** 2026-07-20  
> **Author:** Modernise-to-Modular-Monolith Agent  
> **Status:** Approved for Implementation Planning — Awaiting Human Sign-off  
> **Disclaimer:** This document contains proprietary and confidential information belonging to Crown Commercial Property. Unauthorised use, disclosure, or distribution is prohibited.

---

## 1. Overview

This plan defines the ordered sequence of implementation slices for the BSE system migration. Each slice is a single PR-sized unit of work that can be independently implemented, tested, deployed, and rolled back. No big-bang rewrite occurs — the legacy system remains fully operational until the final parallel-run cutover.

**Migration approach:** Incremental extraction into the new `.slnx` modular monolith running alongside (and sharing the database with) the legacy system during transition. The new application is brought up slice-by-slice. When the UI layer is complete and all slices are delivered, a final parallel-run cutover replaces the legacy IIS application.

**Dependency on prior decisions:**
- Data access: **Option A** — Retain stored procedures via Dapper (see [ADR-005](ADR/ADR-005.md))
- Authentication: **OIDC** replacing Windows Auth (see [ADR-006](ADR/ADR-006.md))
- Target architecture: [Target-Architecture.md](Target-Architecture.md)

---

## 2. Slice Summary Table

| # | Slice Name | Scope Summary | SPs Owned (count) | Phase |
|---|------------|--------------|-------------------|-------|
| 0 | Scaffolding | `.slnx` solution, CI, Docker image, health endpoints, Dapper wrapper, OIDC stub, Serilog | 0 | 1 |
| 1 | SP Source Control | Migrate all ~230 SP `.sql` files into `database/BSE.Database/`; verify CI dacpac build | 230 (no app changes) | 1 |
| 2 | Reference Data | All `lu*` lookup CRUD, geo lookups, editable lookup admin service | ~90 | 1 |
| 3 | User Management | User resolution, OIDC → database mapping, group claims, centralised `AuthorizationPolicy` | 6 | 1 |
| 4 | Audit Log | All `GetAuditLog*` stored procedures; read-only audit service | 8 | 2 |
| 5 | Farm Management | Farm CRUD, CPHH change, FarmRelation, HerdSize | 18 | 2 |
| 6 | Search | All case/farm/outstanding/related-animal search stored procedures | 8 | 2 |
| 7 | Batch | Batch number creation, linking, retrieval | 9 | 2 |
| 8 | Case Management | Full case lifecycle + all child tables; **transactional save fix**; RBSE change; SP `TRY/CATCH` refactor | ~45 | 2 |
| 9 | Animal Relations | CaseRelation read queries; dam/sire pick; relation details | 7 | 2 |
| 10 | CaseWork | Casework minutes; minute sent date; casework entries | 7 | 2 |
| 11 | ADNS Export | GB/NI/CI ADNS export; SMTP dispatch; sequential reference assignment | 5 | 3 |
| 12 | OSS Export | OSS staging table population; OSS batch report generation | 6 | 3 |
| 13 | BSESS Integration | BSESS cross-check reads; replace SSIS with .NET `IHostedService` ETL | 2 + ETL | 3 |
| 14 | Session State Migration | Redis `IDistributedCache`; typed C# wizard state; enable horizontal scaling | 0 (cross-cutting) | 4 |

**Note:** The UI migration (Razor Pages replacing ~90 ASPX pages) is a separate Phase 4 body of work delegated to the UI implementation agent. Slice 14 is the final backend prerequisite before UI migration begins.

---

## 3. Slice Detail

---

### Slice 0 — Scaffolding

**Purpose:** Establish the empty solution skeleton, CI pipeline, Docker image, and shared infrastructure before any domain logic is introduced. Validates the build chain end-to-end.

**Scope:**
- Create `BSE.slnx` with all 29 projects (empty project stubs — no domain code)
- `BSE.Host/Program.cs` with minimal ASP.NET Core pipeline (routing, health checks, auth middleware stubs)
- `BSE.Infrastructure` with `IDbConnectionFactory`, `IDapperRepository`, `SqlConnectionFactory` (reads `BSE_CONNECTION_STRING` env var)
- `BSE.SharedKernel` with `RbseNumber`, `CphhNumber`, `DbseNumber` value types; `FormatRbse()`, `FormatCphh()`, `FormatDbse()` utilities
- OIDC authentication stub: `AddAuthentication().AddOpenIdConnect(...)` configured from env vars; challenge/callback routes only — no domain claims yet
- `GET /health/live` → always 200; `GET /health/ready` → SQL Server and Redis pings
- Serilog structured logging to stdout JSON
- `Dockerfile` (multi-stage, non-root user, exposes 8080/8443)
- `docker-compose.yml` for local development (SQL Server + Redis + app)
- GitHub Actions CI workflow: build → unit test → container image build (no push yet)
- Empty `xunit` test projects scaffolded with a passing placeholder test each

**Owned stored procedures:** None.

**Entry criteria:**
- Phase 0 foundation complete (existing CI pipeline, branch protection, CODEOWNERS in place)
- `.slnx` solution format confirmed supported by chosen SDK version

**Acceptance criteria:**
- `dotnet build BSE.slnx` succeeds with zero errors and zero warnings
- `dotnet test` passes all placeholder tests (coverage not yet meaningful)
- `docker build` produces a valid container image
- Container starts, responds 200 to `GET /health/live`, responds 200 to `GET /health/ready` when SQL Server and Redis are reachable
- OIDC challenge redirects correctly to a stub identity provider URL
- CI workflow passes on every commit

**Rollback:** No legacy code or database has been modified. Rollback is abandonment of the new solution only.

---

### Slice 1 — Stored Procedure Source Control

**Purpose:** Bring all ~230 stored procedure `.sql` files under version control in the new solution structure before any domain migration begins. Establishes the single-source-of-truth for SP definitions.

**Scope:**
- Copy all `.sql` files from `legacy/BSEDatabase/dbo/Stored Procedures/` into `database/BSE.Database/dbo/Stored Procedures/`
- Copy tables, views, and functions from `legacy/BSEDatabase/dbo/` equivalents
- Configure `database/BSE.Database/BSE.Database.sqlproj` (new `.sqlproj` or converted from legacy)
- Add CI step: `SqlPackage.exe /Action:Script` against a test database — validates all SPs parse and are deployable
- Document the SP-to-domain mapping (see §4) as a code comment header in each SP file
- No application code changes in this slice
- No SP code changes — copy only (refactoring of `@@ERROR` → `TRY/CATCH` occurs in Slice 8)

**Owned stored procedures:** All ~230 (copy only — no changes).

**Entry criteria:**
- Slice 0 complete and merged
- Test SQL Server database available in CI environment

**Acceptance criteria:**
- All ~230 SP files present in `database/BSE.Database/dbo/Stored Procedures/`
- `SqlPackage.exe` script generation succeeds against CI test database with no errors
- No changes to SP content (diff between `legacy/BSEDatabase/` and `database/BSE.Database/` shows only path differences)
- SP-to-domain mapping headers added to all SP files

**Rollback:** No application code changes. Rollback is removal of the `database/BSE.Database/` project. Legacy SQL project is unaffected.

---

### Slice 2 — Reference Data Module

**Purpose:** Migrate all reference/lookup data access. This is the lowest-coupling domain with no dependencies on other migrated modules and is used by every other domain — establishing it early unblocks all subsequent slices.

**Scope:**
- `BSE.Modules.ReferenceData`: services and repositories for all `lu*` lookup tables
- Typed C# record types for each lookup entity (e.g., `LookupItem`, `LuBreed`, `LuADNSRegion`, etc.)
- `ILookupDataService` interface exposed for use by other modules
- `IGeoLookupService` for county/parish → OS map reference lookups
- `IEditableLookupAdminService` for CRUD operations on editable lookup tables
- `LookupTableId` enum in `BSE.SharedKernel` replacing `Common.vb` integer constants
- `BSE.Modules.ReferenceData.Tests` with unit and integration tests

**Owned stored procedures (~90):**
- *Reads:* `GetluADNSRegion`, `GetluADNSRegionByAuthority`, `GetluAHO`, `GetluAHOCode`, `GetluAHRO`, `GetluAHROCode`, `GetluAnimalOrigin`, `GetluAnimalStatus`, `GetluAuthorityByAuthorityCounty`, `GetluAuthorityCountyAll`, `GetluBirthDateSource`, `GetluBreed`, `GetluBSECounty`, `GetluBSEForm`, `GetluBSERegion`, `GetluCaseFate`, `GetluCaseType`, `GetluDocumentType`, `GetluFeedRisk`, `GetluHerdType`, `GetluHorizontalRisk`, `GetluMaternalRisk`, `GetluOwnerType`, `GetluPedigreeType`, `GetluRationType`, `GetluRegionalLab`, `GetluRelationFate`, `GetluRelationType`, `GetluReportedLocation`, `GetluSex`, `GetluSupplier`, `GetluSurvey`, `GetluTestResult`, `GetluTestType`, `GetluTSETestingSite`, `GetluUserGroup`, `GetluValuationAge`, `GetEditableLookups`, `GetEditableLookupProcs`, `GetSupplierByName`, `GetPossibleSuppliers`
- *Geo lookups:* `GetMapReferenceByCountyParish`, `GetPrefixCodeByXYReference`, `GetXYReferenceByPrefixCode`, `GetParishByCountyParish`, `GetNonGBCounty`
- *Admin writes (×3 per table — Add/Edit/Delete for ~29 lu tables):* `AddluADNSRegion`, `EditluADNSRegion`, `DeleteluADNSRegion` … and so on for all lu* tables

**Entry criteria:**
- Slice 1 complete; SP source in version control
- `BSE.Infrastructure` passing CI (Slice 0)

**Acceptance criteria:**
- All lookup queries return results identical to legacy system for the same stored procedure calls
- Geo lookups produce identical OS map references for a set of 10 reference county/parish inputs
- Admin CRUD operations (Add/Edit/Delete) round-trip correctly with no data loss
- ≥ 80% branch coverage on `BSE.Modules.ReferenceData`
- No `DataSet` or `DataTable` types in migrated code — all results are typed C# records

**Rollback:** Remove `BSE.Modules.ReferenceData` project from solution. No database changes. Legacy system continues to serve reference data.

---

### Slice 3 — User Management Module

**Purpose:** Establish identity resolution (OIDC → database User) and centralised authorisation policy. This slice must be complete before any page-level authorisation can be applied.

**Scope:**
- `BSE.Modules.UserManagement`: `IUserContext`, `IUserRepository`, `IGroupResolver`
- OIDC token claims (UPN) → `GetGroupForUser` SP → `UserGroup` enum → ASP.NET Core role claims
- `User.UPN` column addition: preparatory database migration script (nullable, no existing data impact)
- Identity fallback strategy: UPN match first, then `NTLogin` substring match during transition period
- One-off data migration script (`UpdateUserUpnFromNtLogin.sql`) to back-populate UPN from existing NTLogin records
- Five `AuthorizationPolicy` registrations in `BSE.Host/Program.cs` (see Target Architecture §6.3)
- Roslyn analyser rule: all Razor Pages must have `[Authorize(Policy = "...")]` attribute
- `BSE.Modules.UserManagement.Tests`

**Owned stored procedures (6):**
`GetGroupForUser`, `GetUserByNTLogin`, `GetUsers`, `AddUser`, `EditUser`, `GetluUserGroup`

**Entry criteria:**
- Slice 2 complete (UserManagement uses `GetluUserGroup` from the same SP set)
- `User.UPN` database migration script reviewed by Tech Lead
- *(Azure AD tenant provisioning is NOT a blocker — see mock approach below)*

**Mock OIDC approach (unblocks implementation until real Azure AD is available):**
- `IUserContext` and all authorisation abstractions are implemented against real logic and real SPs — no mocking of business logic
- Tests use ASP.NET Core's `WebApplicationFactory` with a `TestAuthHandler` that issues known claims (UPN, group memberships) in the same format Azure AD would produce — no real IdP call required
- OIDC configuration (`ClientId`, `TenantId`, `ClientSecret`) is environment-variable-only; a local stub OIDC config is used in development and CI
- When real Azure AD tenant is provisioned, only environment variables change — zero code changes required
- The UPN claim shape used in `TestAuthHandler` must match the exact Azure AD UPN format (documented in the PR) so the switch-over is verified

**Acceptance criteria:**
- `TestAuthHandler` issues UPN + group claims; `IUserContext` resolves the correct `UserGroup` — verified by integration tests with no real IdP
- All five `AuthorizationPolicy` definitions pass a security review
- `GetUsers` and `AddUser`/`EditUser` operations round-trip correctly
- Identity fallback (UPN → NTLogin) works for users not yet migrated
- ≥ 80% branch coverage on `BSE.Modules.UserManagement`
- Security review of auth module signed off by named reviewer before merge
- PR description documents the UPN claim format assumed and the steps to wire in real Azure AD when tenant is available

**Production gate (separate from merge gate):** Real Azure AD tenant provisioned, `ClientId`/`TenantId`/`ClientSecret` set in deployment environment, and at least one real user end-to-end test passes before production go-live.

**Rollback:** Remove `BSE.Modules.UserManagement` project; revert `AuthorizationPolicy` registrations. `User.UPN` column is nullable — existing data is unaffected. Legacy Windows Auth continues to serve the legacy system.

---

### Slice 4 — Audit Log Module

**Purpose:** Migrate audit log reads. The AuditLog table is write-only from stored procedures (no application-side writes), making this the lowest-risk domain migration. Delivering it early provides an immutable audit record of all subsequent migration activity.

**Scope:**
- `BSE.Modules.AuditLog`: `IAuditLogService` with 8 query methods
- Typed C# records for audit log entries
- `BSE.Modules.AuditLog.Tests`

**Owned stored procedures (8):**
`GetAuditLogByCase`, `GetAuditLogByDate`, `GetAuditLogByFarm`, `GetAuditLogByUser`, `GetAuditLogCaseMoves`, `GetAuditLogCPHHChanges`, `GetAuditLogNewFarms`, `GetAuditLogRBSEChanges`

**Entry criteria:**
- Slice 3 complete (UserManagement provides `IUserContext` used in audit log queries by user)
- Slice 1 complete (SP source in version control)

**Acceptance criteria:**
- Audit log queries return identical results to legacy for a set of 10 reference RBSE/CPHH/date inputs
- ≥ 80% branch coverage on `BSE.Modules.AuditLog`
- No write operations introduced in this module (write path remains in SPs)

**Rollback:** Remove `BSE.Modules.AuditLog` project. No database changes.

---

### Slice 5 — Farm Management Module

**Purpose:** Migrate farm data access. `Farm` is a prerequisite for `Case` (FK constraint: `Case.CPHH` references `Farm.CPHH`). Farm Management must be stable before Case Management begins.

**Scope:**
- `BSE.Modules.FarmManagement`: `IFarmRepository`, `IFarmRelationRepository`, `IHerdSizeRepository`
- Typed C# records: `FarmRecord`, `FarmDetailRecord` (with related farms and herd size), `FarmRelationRecord`, `HerdSizeRecord`
- `ChangeCPHH` cascade: 8 error code enum replaces integer return codes
- `GetNumberOfConfirmedCases` and `GetNumberOfCasesByCPHH` as named methods (not raw DataTable queries)
- `BSE.Modules.FarmManagement.Tests`

**Owned stored procedures (18):**
`AddFarm`, `EditFarm`, `GetFarmByCPHH`, `GetFarmDetailsByCPHH`, `GetFarmsByCPH`, `ChangeCPHH`, `GetNumberOfConfirmedCases`, `GetNumberOfCasesByCPHH`, `AddFarmRelation`, `EditFarmRelation`, `DeleteFarmRelation`, `GetRelatedFarm`, `AddHerdSize`, `EditHerdSize`, `DeleteHerdSize`, `GetHerdSizeByCPHH`, `GetHerdDetailByBatchID`, `GetVetnetDetailsByCPHH`

**Entry criteria:**
- Slice 4 complete
- `CphhNumber` value type available in `BSE.SharedKernel` (Slice 0)

**Acceptance criteria:**
- `FarmInDatabase()` equivalent (`GetFarmByCPHH`) returns correct existence result for known and unknown CPHHs
- `ChangeCPHH` returns the correct named error code for each of the 8 error scenarios (integration test with injected fault conditions)
- Farm detail retrieval returns a record identical to legacy for 5 reference CPHHs
- HerdSize and FarmRelation CRUD operations round-trip correctly
- ≥ 80% branch coverage on `BSE.Modules.FarmManagement`

**Rollback:** Remove `BSE.Modules.FarmManagement` project. No database changes.

---

### Slice 6 — Search Module

**Purpose:** Migrate all search functionality. Search is read-only — no state changes, no FK dependencies on migrated modules. This slice validates that complex multi-parameter SP queries translate correctly through the Dapper wrapper.

**Scope:**
- `BSE.Modules.Search`: `ICaseSearchService`, `IFarmSearchService`, `IOutstandingDataSearchService`
- Typed result records: `CaseSearchResult`, `FarmSearchResult`, `OutstandingCaseResult`, `RelatedAnimalResult`
- 60-second command timeout preserved for `GetSearchCase` and `GetSearchFarm` (configured on `IDapperRepository` call site)
- `BSE.Modules.Search.Tests`

**Owned stored procedures (8):**
`GetSearchCase`, `GetSearchCaseByCPHH`, `GetSearchCaseByEartagHerdmark`, `GetSearchFarm`, `GetSearchOutstandingBSE1s`, `GetSearchOutstandingFates`, `GetSearchOutstandingResults`, `GetSearchRelatedAnimals`

**Entry criteria:**
- Slice 5 complete
- Reference data and farm data available for search result mapping

**Acceptance criteria:**
- Case search with all 17 parameters returns identical results to legacy for 10 reference queries
- Farm search with all 8 parameters returns identical results for 10 reference queries
- Extended timeout (60 s) is correctly applied to search commands
- ≥ 80% branch coverage on `BSE.Modules.Search`

**Rollback:** Remove `BSE.Modules.Search` project. No database changes.

---

### Slice 7 — Batch Module

**Purpose:** Migrate batch number management. Batch is required by Case Management (BatchID is a parameter to `AddCase` / `EditCase` and to `CreateBatchNumberLink`). This slice must be complete before Slice 8.

**Scope:**
- `BSE.Modules.Batch`: `IBatchRepository`
- Typed result record: `BatchRecord` (BatchID, BatchYear, BatchNumber)
- OUTPUT parameter pattern via `DynamicParameters` for `AddBatchNumber` (three OUTPUT params)
- `CreateBatchNumberLink` must be transactionally composable — the method accepts an external `IDbConnection` + `IDbTransaction` parameter so the Case Management slice can enlist it in the case-save transaction
- `BSE.Modules.Batch.Tests`

**Owned stored procedures (9):**
`AddBatchNumber`, `AddBatchNumber1989`, `AddBatchNumberLink`, `GetBatchIDForBatch`, `GetBatchNumberByRBSE`, `GetLatestBatchNumbers`, `GetCaseByBatchID`, `GetCPHHRBSEForBatch`, `GetCPHHRBSEForBatchID`

**Entry criteria:**
- Slice 6 complete
- `IDapperRepository.ExecuteWithOutputAsync` implemented and tested (Slice 0 stub, finalised here)

**Acceptance criteria:**
- `AddBatchNumber` returns correctly populated `BatchRecord` with BatchID, BatchYear, BatchNumber via OUTPUT params
- `CreateBatchNumberLink` is callable both standalone and within an external transaction
- `GetLatestBatchNumbers` returns identical results to legacy for a reference date range
- ≥ 80% branch coverage on `BSE.Modules.Batch`

**Rollback:** Remove `BSE.Modules.Batch` project. No database changes.

---

### Slice 8 — Case Management Module

**Purpose:** The highest-risk, highest-value domain migration. Migrates the complete case lifecycle including all 11 child tables, replaces integer-indexed DataSet constants with named record types, and implements the transactional boundary fix for `UpdateCaseDetailsAsync` (the critical defect identified in R01).

**Engineering obligations specific to this slice:**
1. **Transactional save (mandatory):** All stored procedure calls in `UpdateCaseDetailsAsync` are wrapped in a single `SqlTransaction`. No partial-update states are possible.
2. **Named record types:** All `DataSet.Tables[n]` integer-index constants (`CASE_TABLE = 0` through `CASEWORK_TABLE = 10`) are replaced by named C# records populated from `GetCaseDetailsByRBSE` via `QueryMultipleAsync`.
3. **SP error handling refactor:** `AddCase`, `EditCase`, `ChangeRBSE`, `MoveCase`, and `DeleteCase` stored procedures are refactored from `@@ERROR`-based to `BEGIN TRY / BEGIN CATCH / ROLLBACK TRANSACTION` pattern as part of this slice. These are the only SP changes permitted in this slice.
4. **Optimistic concurrency:** `RowStamp` (`ROWVERSION`) parameter for `EditCase` and `EditFarm` is preserved; concurrency violation mapped to a named exception (`ConcurrencyViolationException`) rather than returning a raw integer error code.
5. **Error codes:** All integer error codes returned by `DeleteCase` (1–6), `MoveCaseNewFarm` (10 codes), `ChangeRBSE` (15 codes), `ChangeCPHH` (8 codes) are replaced by named C# enums.

**Scope:**
- `BSE.Modules.CaseManagement`: `ICaseRepository`, `IClinicalRepository`, `IBabRepository`, `IFeedRepository`, `ITestRepository`, `IOtherOwnerRepository`, `IPedigreeRepository`
- `ICaseService` with `GetCaseDetailsAsync`, `UpdateCaseDetailsAsync`, `DeleteCaseAsync`, `MoveCaseAsync`, `ChangeRbseAsync`, `CreateNonGbCaseAsync`, `FinalResultEntryAsync`
- `CaseWizardState` typed record (replacing 11-table DataSet in session)
- `CaseDetailRecord` with named properties for all 11 result sets from `GetCaseDetailsByRBSE`
- `BSE.Modules.CaseManagement.Tests` including injected-fault transaction test

**Owned stored procedures (~45):**
- *Core case:* `AddCase`, `EditCase`, `EditCaseFinalResult`, `DeleteCase`, `MoveCase`, `AddNonGBCase`, `ChangeRBSE`, `GetCaseByRBSE`, `GetCaseDetailsByRBSE`, `GetCaseFarmByBatchID`, `GetFinalResultByRBSE`, `GetClosedCaseReportData`, `GetOpenCaseReportData`, `GetLatestDBSEForYear`, `GetLatestRBSEForYear`, `GetCPHHBatchForRBSE`
- *Clinical:* `AddCaseClinical`, `EditCaseClinical`, `GetClinicalByRBSE`, `GetClinicalByBatchID`, `AddClinicalVisit`, `EditClinicalVisit`, `DeleteClinicalVisit`, `GetClinicalVisitByRBSE`
- *BAB:* `AddCaseBAB`, `EditCaseBAB`, `GetBABByRBSE`
- *Feed:* `AddCaseFeed`, `EditCaseFeed`, `DeleteCaseFeed`, `GetFeedByRBSE`, `GetFeedsByBatchID`
- *Test:* `AddTest`, `EditTest`, `DeleteTest`, `GetTestByRBSE`
- *Other owner:* `AddOtherOwner`, `DeleteOtherOwner`, `EditOtherOwner`, `GetOtherOwnerByRBSE`, `GetCPHHOtherOwnerForRBSE`, `GetPreviousOwnerByBatchID`
- *Pedigree/Dam-Sire:* `AddEditDamSireDetails`, `GetDamDetailsByRBSE`, `GetSireDetailsByRBSE`, `GetDamSireDetailsByBatchID`, `GetDamSireDetailsMatches`

**Entry criteria:**
- Slices 5, 6, 7 complete and merged
- SP error-handling refactor (`TRY/CATCH`) reviewed and approved by Tech Lead before SP changes are committed
- Injected-fault transaction test designed and reviewed before implementation begins

**Acceptance criteria:**
- `GetCaseDetailsByRBSE` result mapped correctly to `CaseDetailRecord` for 10 reference RBSE numbers (output identical to legacy)
- `UpdateCaseDetailsAsync` with a simulated failure after `AddCase` (before child SPs) rolls back the entire transaction — no `Case` row remains in database
- `ChangeRBSE` cascades correctly across all 15 child tables (verified by integration test)
- Optimistic concurrency violation on `EditCase` returns `ConcurrencyViolationException` (not raw integer)
- All 6 `DeleteCase` error codes map to named enum values
- All 10 `MoveCaseNewFarm` error codes map to named enum values
- All 15 `ChangeRBSE` error codes map to named enum values
- SP `TRY/CATCH` refactor: `AddCase`, `EditCase`, `ChangeRBSE`, `MoveCase`, `DeleteCase` have no `@@ERROR` checks — all use `BEGIN TRY / BEGIN CATCH`
- ≥ 80% branch coverage on `BSE.Modules.CaseManagement`
- Security review of all SP parameters (injection risk assessment) passed

**Rollback:** Remove `BSE.Modules.CaseManagement` project. SP `TRY/CATCH` refactors are backwards-compatible (behaviour preserved) — no rollback needed for SP changes. If a SP change must be reverted, the database is the rollback target (deploy previous dacpac version).

---

### Slice 9 — Animal Relations Module

**Purpose:** Migrate relation read/query services. The write path (Add/Edit/Delete CaseRelation) is owned by the CaseManagement transactional save and is therefore not duplicated here. This slice focuses on relation query, dam/sire pick, and relation display.

**Scope:**
- `BSE.Modules.AnimalRelations`: `IAnimalRelationsService`
- Read queries for relations by RBSE, dam/sire detail lookup, relation match search
- `RelationRecord`, `DamSireDetailRecord`, `RelatedCaseRecord` typed records
- `BSE.Modules.AnimalRelations.Tests`

**Owned stored procedures (7):**
`AddCaseRelation`, `EditCaseRelation`, `DeleteCaseRelation`, `GetRelationsByRBSE`, `GetRelationsDetailsByRBSE`, `GetRelationDetailsOfRelatedCase`, `GetRelationsByBatchID`

*Note:* `AddCaseRelation`, `EditCaseRelation`, `DeleteCaseRelation` are also called within the CaseManagement transactional save (Slice 8). This module owns the SP definitions; CaseManagement calls them directly via `IDapperRepository` within its transaction scope.

**Entry criteria:**
- Slice 8 complete

**Acceptance criteria:**
- `GetRelationsByRBSE` returns identical result to legacy for 5 reference cases with known relations
- Dam/sire match results (`GetDamSireDetailsMatches`) match legacy for 5 reference inputs
- ≥ 80% branch coverage on `BSE.Modules.AnimalRelations`

**Rollback:** Remove `BSE.Modules.AnimalRelations` project. No database changes.

---

### Slice 10 — CaseWork Module

**Purpose:** Migrate casework minutes management. CaseWork records are written as part of the CaseManagement transactional save. This slice adds the specialised casework display and minute management services.

**Scope:**
- `BSE.Modules.CaseWork`: `ICaseWorkService`
- Minute retrieval and display; minute sent date update; casework entry management
- `CaseWorkRecord`, `MinuteRecord` typed records
- `BSE.Modules.CaseWork.Tests`

**Owned stored procedures (7):**
`AddCaseWork`, `EditCaseWork`, `EditCaseWorkEntry`, `GetCaseWorkByRBSE`, `GetCaseWorkEntryByRBSE`, `GetMinuteDetails`, `SetMinuteSentDate`

*Note:* `AddCaseWork` and `EditCaseWork` are called within the CaseManagement transactional save (Slice 8). This module owns the SP definitions; CaseManagement calls them within its transaction scope.

**Entry criteria:**
- Slice 9 complete

**Acceptance criteria:**
- `GetCaseWorkByRBSE` returns identical results to legacy for 5 reference RBSE numbers
- `SetMinuteSentDate` update verified by round-trip query
- ≥ 80% branch coverage on `BSE.Modules.CaseWork`

**Rollback:** Remove `BSE.Modules.CaseWork` project. No database changes.

---

### Slice 11 — ADNS Export Module

**Purpose:** Migrate the highest-regulatory-risk domain. ADNS export emails EU notifications. Any regression in content, format, or reference number sequencing is a statutory breach (Risk R02). APHA regulatory stakeholder sign-off is required before this slice is merged to `main`.

**Engineering obligations specific to this slice:**
1. **Output comparison harness:** Automated test that generates ADNS email body text via the new module and compares it byte-for-byte against the legacy output for the same input data set (10 reference cases).
2. **Sequential reference numbers:** `ADNSReferenceYear` + `ADNSReferenceNumber` assignment logic must be transactionally safe — the sequence must not produce gaps or duplicates under concurrent access.
3. **SMTP configuration:** All SMTP configuration from environment variables (no hardcoded relay host); SMTP calls wrapped in a retry policy (Polly) with exponential backoff.
4. **Stateless service:** The `clsADNSReport` stateful instance pattern is replaced by a stateless `IAdnsExportService`; intermediate state is held in distributed cache keyed by user session (not in a long-lived object).

**Scope:**
- `BSE.Modules.AdnsExport`: `IAdnsExportService` with GB, NI, CI variants
- SMTP dispatch via `ISmtpClient` (injectable, mockable)
- `EditCaseADNS` and `EditLastADNSReference` wrapped in a single transaction
- Output comparison harness (automated test asset, not production code)
- `BSE.Modules.AdnsExport.Tests`

**Owned stored procedures (5):**
`GetADNSCasesForGB`, `GetLastADNSReferenceByArea`, `EditLastADNSReference`, `EditCaseADNS`
*(NI variant uses same SP set with area parameter; CI variant uses equivalent pattern)*

**Entry criteria:**
- Slices 8–10 complete
- *(APHA SME allocation and golden file are NOT blockers for merge — see stub approach below)*

**Stub golden file approach (unblocks implementation until APHA SME validation is available):**
- A placeholder file `tests/characterisation/adns-golden.txt` is committed with content: `PLACEHOLDER — APHA SME validation required before production go-live`
- The output comparison harness is fully implemented and wired into CI
- The golden file comparison test is decorated `[Fact(Skip = "Golden file pending APHA SME validation — see Risk R02")]` so CI stays green
- The stub and the `Skip` attribute are tracked as an open item in this slice's PR description and in the Risk register entry for R02
- When the APHA SME provides validated output, the golden file is replaced, the `Skip` attribute is removed, and CI must be green against the real file before production go-live is approved

**Acceptance criteria (merge gate):**
- Output comparison harness code is implemented, reviewed, and merged — test infrastructure is production-ready
- Placeholder golden file committed at `tests/characterisation/adns-golden.txt`
- Sequential reference number assignment produces no gaps or duplicates under a 10-concurrent-request load test
- `EditCaseADNS` and `EditLastADNSReference` are called within a single transaction; a simulated failure leaves neither record updated
- SMTP configuration is 100% from environment variables — no hardcoded values
- ≥ 80% branch coverage on `BSE.Modules.AdnsExport`
- PR description explicitly records the open golden file item and the production gate requirement

**Production gate (separate from merge gate — BLOCKING):** APHA SME selects reference cases, validates output, golden file committed with real content, `Skip` attribute removed, CI green against real golden file. This gate cannot be waived.

**Rollback:** Remove `BSE.Modules.AdnsExport` project. No database changes. Legacy ADNS export remains operative.

---

### Slice 12 — OSS Export Module

**Purpose:** Migrate the OSS export staging pipeline. OSS export populates `exp*` staging tables and generates batch export reports. The staging tables are written by application-called SPs; this slice ensures they are called correctly and in the right order.

**Scope:**
- `BSE.Modules.OssExport`: `IOssExportService`
- Staging table population via `Copy*` stored procedures
- OSS export detail retrieval and batch report generation
- `OssExportRecord` typed record
- `BSE.Modules.OssExport.Tests`

**Owned stored procedures (6):**
`GetOSSExportByRBSE`, `CopyCaseToExportTable`, `CopyFarmToExportTable`, `CopyHerdSizeToExportTable`, `CopyRelationToExportTable`, `AddBatchNumber1989`

*Note:* `GetCaseByBatchID` is shared with Batch (Slice 7) — the Batch module's `IBatchRepository` is used here via DI rather than duplicating the SP call.

**Entry criteria:**
- Slice 12 depends on Slice 11 (both are Phase 3 regulatory integrations; sequential delivery lowers risk)
- APHA SME consulted on OSS export format requirements

**Acceptance criteria:**
- OSS staging tables (`expCase`, `expFarm`, `expRelation`) contain identical rows after export as legacy for a set of 10 reference RBSE cases
- `Copy*` SP calls are transactionally consistent (all or none for a given export run)
- ≥ 80% branch coverage on `BSE.Modules.OssExport`

**Rollback:** Remove `BSE.Modules.OssExport` project. `exp*` staging tables are unaffected (no schema change). Legacy OSS export remains operative.

---

### Slice 13 — BSESS Integration Module

**Purpose:** Migrate BSESS cross-check reads and replace the SSIS `BSESS Import.dtsx` package with a .NET-hosted ETL service that can run inside a Linux container.

**Engineering obligations specific to this slice:**
- SSIS runtime dependency is eliminated (SSIS cannot run in a Linux container — Risk R06)
- SSIS package password (`pass`) is rotated as an immediate security action before this slice begins (Risk R07 remediation)
- ETL job implemented as `IHostedService` using `Microsoft.Data.SqlClient.SqlBulkCopy` to import `BSESSImport` staging data from the source system

**Scope:**
- `BSE.Modules.BsessIntegration`: `IBsessCheckService`, `IBsessEtlService`
- Cross-check read methods: by date, by RBSE
- `BsessCheckRecord` typed record (11 output properties from `GetBSESSCheckByRBSE`)
- `BsessImportJob : IHostedService` replacing `BSESS Import.dtsx`
- Import schedule and retry policy configurable from environment variables
- `BSE.Modules.BsessIntegration.Tests`

**Owned stored procedures (2) + ETL:**
`GetBSESSCheckByDate`, `GetBSESSCheckByRBSE`

**Entry criteria:**
- Slices 11 and 12 complete (Phase 3 complete)
- SSIS package password (`pass`) rotated **before** this slice begins
- BSESS source system access credentials available for .NET ETL service

**Acceptance criteria:**
- `GetBSESSCheckByRBSE` returns identical 11-field results to legacy for 5 reference cases
- `BsessImportJob` successfully imports test data from a BSESS source file/connection into `BSESSImport` staging table
- SSIS runtime dependency: no `.dtsx` execution in any CI job or container startup
- Import job is idempotent: running it twice with the same data does not duplicate rows
- ≥ 80% branch coverage on `BSE.Modules.BsessIntegration`
- Phase 3 gate: APHA sign-off on ADNS output format (from Slice 11) confirmed before this slice merges

**Rollback:** Remove `BSE.Modules.BsessIntegration` project. SSIS packages in `legacy/BSEIntegrationServices/` remain available for rollback use. Database staging table `BSESSImport` is unaffected.

---

### Slice 14 — Session State Migration

**Purpose:** Replace all InProc ASP.NET session state with Redis-backed `IDistributedCache`. This is the final backend prerequisite before the UI migration begins. After this slice, the application can run with multiple container replicas.

**Engineering obligations specific to this slice:**
1. **No InProc session** anywhere in new codebase after this slice
2. **Typed wizard state:** `CaseWizardState` and `FarmWizardState` C# records serialised as JSON in Redis (replacing 11-table and 3-table DataSets)
3. **Cache expiry:** Redis keys expire after 60 minutes (configurable); equivalent to legacy 20-minute session with additional buffer
4. **Circuit breaker:** If Redis is unavailable, fall back to `MemoryCache`-backed distributed cache; log degraded state to Serilog; set `/health/ready` to `Degraded`
5. **User identity:** No user identity in cache — served from OIDC claims on every request (Slice 3)

**Scope:**
- `ICacheKeyProvider` registered in `BSE.Infrastructure`
- `CaseWizardState` record and `FarmWizardState` record as serialisation targets
- `IDistributedCache` registration with Redis primary / MemoryCache fallback
- `Redis__ConnectionString` environment variable validation at startup
- All existing module services updated to use typed cache state instead of DataSet session patterns
- Circuit breaker implementation using Polly or `Microsoft.Extensions.Caching.StackExchangeRedis` with custom retry policy
- `BSE.Infrastructure.Tests` updated for cache key provider coverage

**Owned stored procedures:** None (cross-cutting infrastructure change).

**Entry criteria:**
- Slices 0–13 complete and merged
- Redis infrastructure provisioned in target hosting environment
- Load test plan drafted (two-replica scale test)

**Acceptance criteria:**
- Two container replicas share session state correctly — a user's case wizard state loaded in replica A is accessible after replica B handles the next request
- Cache expiry works: a session that goes idle for > 60 minutes returns the user to the start of the wizard on next access (no crash, no corrupt state)
- Circuit breaker: application continues to serve traffic (single-replica mode) when Redis is made unavailable
- `/health/ready` returns `Degraded` (not unhealthy) when Redis is unavailable
- No `HttpContext.Session` references remain in any non-legacy source file
- ≥ 80% branch coverage on all updated modules

**Rollback:** Re-register `IDistributedCache` as `MemoryCache`-backed only (single-replica constraint re-applies). No database changes.

---

## 4. SP-to-Domain Mapping Reference

| Domain Module | Stored Procedure Set |
|---------------|---------------------|
| ReferenceData | All `GetluXxx`, `AddluXxx`, `EditluXxx`, `DeleteluXxx` (~87 SPs), `GetEditableLookups`, `GetEditableLookupProcs`, `GetMapReferenceByCountyParish`, `GetPrefixCodeByXYReference`, `GetXYReferenceByPrefixCode`, `GetParishByCountyParish`, `GetNonGBCounty`, `GetSupplierByName`, `GetPossibleSuppliers` |
| UserManagement | `GetGroupForUser`, `GetUserByNTLogin`, `GetUsers`, `AddUser`, `EditUser`, `GetluUserGroup` |
| AuditLog | `GetAuditLogByCase`, `GetAuditLogByDate`, `GetAuditLogByFarm`, `GetAuditLogByUser`, `GetAuditLogCaseMoves`, `GetAuditLogCPHHChanges`, `GetAuditLogNewFarms`, `GetAuditLogRBSEChanges` |
| FarmManagement | `AddFarm`, `EditFarm`, `GetFarmByCPHH`, `GetFarmDetailsByCPHH`, `GetFarmsByCPH`, `ChangeCPHH`, `GetNumberOfConfirmedCases`, `GetNumberOfCasesByCPHH`, `AddFarmRelation`, `EditFarmRelation`, `DeleteFarmRelation`, `GetRelatedFarm`, `AddHerdSize`, `EditHerdSize`, `DeleteHerdSize`, `GetHerdSizeByCPHH`, `GetHerdDetailByBatchID`, `GetVetnetDetailsByCPHH` |
| Search | `GetSearchCase`, `GetSearchCaseByCPHH`, `GetSearchCaseByEartagHerdmark`, `GetSearchFarm`, `GetSearchOutstandingBSE1s`, `GetSearchOutstandingFates`, `GetSearchOutstandingResults`, `GetSearchRelatedAnimals` |
| Batch | `AddBatchNumber`, `AddBatchNumber1989`, `AddBatchNumberLink`, `GetBatchIDForBatch`, `GetBatchNumberByRBSE`, `GetLatestBatchNumbers`, `GetCaseByBatchID`, `GetCPHHRBSEForBatch`, `GetCPHHRBSEForBatchID` |
| CaseManagement | `AddCase`, `EditCase`, `EditCaseFinalResult`, `DeleteCase`, `MoveCase`, `AddNonGBCase`, `ChangeRBSE`, `GetCaseByRBSE`, `GetCaseDetailsByRBSE`, `GetCaseFarmByBatchID`, `GetFinalResultByRBSE`, `GetClosedCaseReportData`, `GetOpenCaseReportData`, `GetLatestDBSEForYear`, `GetLatestRBSEForYear`, `GetCPHHBatchForRBSE`, `AddCaseClinical`, `EditCaseClinical`, `GetClinicalByRBSE`, `GetClinicalByBatchID`, `AddClinicalVisit`, `EditClinicalVisit`, `DeleteClinicalVisit`, `GetClinicalVisitByRBSE`, `AddCaseBAB`, `EditCaseBAB`, `GetBABByRBSE`, `AddCaseFeed`, `EditCaseFeed`, `DeleteCaseFeed`, `GetFeedByRBSE`, `GetFeedsByBatchID`, `AddTest`, `EditTest`, `DeleteTest`, `GetTestByRBSE`, `AddOtherOwner`, `DeleteOtherOwner`, `EditOtherOwner`, `GetOtherOwnerByRBSE`, `GetCPHHOtherOwnerForRBSE`, `GetPreviousOwnerByBatchID`, `AddEditDamSireDetails`, `GetDamDetailsByRBSE`, `GetSireDetailsByRBSE`, `GetDamSireDetailsByBatchID`, `GetDamSireDetailsMatches` |
| AnimalRelations | `AddCaseRelation`, `EditCaseRelation`, `DeleteCaseRelation`, `GetRelationsByRBSE`, `GetRelationsDetailsByRBSE`, `GetRelationDetailsOfRelatedCase`, `GetRelationsByBatchID` |
| CaseWork | `AddCaseWork`, `EditCaseWork`, `EditCaseWorkEntry`, `GetCaseWorkByRBSE`, `GetCaseWorkEntryByRBSE`, `GetMinuteDetails`, `SetMinuteSentDate` |
| AdnsExport | `GetADNSCasesForGB`, `GetLastADNSReferenceByArea`, `EditLastADNSReference`, `EditCaseADNS` |
| OssExport | `GetOSSExportByRBSE`, `CopyCaseToExportTable`, `CopyFarmToExportTable`, `CopyHerdSizeToExportTable`, `CopyRelationToExportTable`, `AddBatchNumber1989` (shared with Batch) |
| BsessIntegration | `GetBSESSCheckByDate`, `GetBSESSCheckByRBSE` |

---

## 5. Risk Summary Per Slice

| Slice | Key Risk | Control |
|-------|----------|---------|
| 0 | CI pipeline not catching regressions early | Placeholder tests in every project; fail-fast on zero coverage |
| 1 | SP copy introduces subtle differences | Automated diff check between legacy and new SQL file contents |
| 2 | Lookup data drift from legacy | Comparison integration tests against shared SQL Server |
| 3 | OIDC user identity mismatch with DB records | UPN fallback to NTLogin; data migration script reviewed before execution |
| 4 | AuditLog query result divergence | Output comparison tests for 10 reference inputs |
| 5 | `ChangeCPHH` partial cascade (8 error codes) | Named enum + injected-fault integration test for each error code |
| 6 | Search timeout causing CI failures | 60-second timeout configured; CI uses representative test dataset |
| 7 | `AddBatchNumber` OUTPUT params mapped incorrectly | Explicit OUTPUT parameter direction verification test |
| 8 | Transactional save fails silently | **Mandatory injected-fault test**: failure after `AddCase` must roll back entirely |
| 8 | SP `TRY/CATCH` refactor changes behaviour | Behaviour-preservation tests before and after refactor |
| 9 | Relation write/read boundary confusion | Module boundary documented; write path in CaseManagement only |
| 10 | CaseWork write path duplicated | Only read-side in CaseWork module; write path remains in CaseManagement |
| 11 | ADNS email content regression | Byte-identical comparison harness; APHA sign-off blocking gate |
| 12 | OSS staging table corruption | Transaction wraps all `Copy*` SP calls; staging table row counts verified |
| 13 | BSESS ETL job causes data duplication | Idempotency test: double-run produces no duplicate rows |
| 14 | Redis unavailability causes user data loss | Circuit breaker with MemoryCache fallback; `/health/ready` degrades not fails |

---

## 6. Phase Gates

Human approval is required before each phase transition:

| Gate | Phase transition | Required sign-offs |
|------|-----------------|-------------------|
| P1 Gate | After Slice 3 | Tech Lead (pattern validation), Security Reviewer (auth module), DevOps (container runs) |
| P2 Gate | After Slice 10 | Tech Lead + QA (coverage ≥ 80%, transactional integrity verified), Security Reviewer |
| P3 Gate | After Slice 13 | APHA regulatory stakeholder (ADNS output), QA (output comparison harness pass), DevOps (SSIS eliminated) |
| P4 Gate (start) | Before UI agent begins | Slice 14 merged; Redis operational; two-replica test passed |
| P4 Gate (cutover) | Legacy decommission | Delivery Lead + APHA stakeholder + Executive Sponsor; parallel-run success criteria met |

---

*Preceding document: [Target-Architecture.md](Target-Architecture.md)  
Next documents: [ADR-005](ADR/ADR-005.md) | [ADR-006](ADR/ADR-006.md)*
