# BSE System — Low-Level Design (LLD)

> **Status:** As-is documentation of the legacy codebase. No future-state speculation.  
> **Date:** 2026-07-20  
> **Source path:** `legacy/`

---

## 1. Project Layer Summary

```
legacy/BSESystem/       Web layer       — ASPX pages, ASCX controls
legacy/BSELib/          Domain layer    — 12 VB.NET classes
legacy/DataAccessLib/   Data access     — ADO.NET Shared wrapper classes
legacy/BSEDatabase/     Persistence     — SQL Server stored procedures & schema
```

All three code projects target **.NET Framework 4.0** and are written in **VB.NET**. There is no ORM; every database operation executes a named stored procedure.

---

## 2. Key Classes and Services by Domain

### 2.1 Data Access Infrastructure

#### `legacy/DataAccessLib/clsDataAccess.vb` — `libDataAccess.DataAccess`
Shared (static) class. Low-level ADO.NET. Opens a `SqlConnection` per call; no connection pooling management beyond what ADO.NET provides automatically.

| Method | Signature summary | Responsibility |
|--------|-------------------|----------------|
| `Initialise()` | Private Shared | Reads `DBConnectionString` from `ConfigurationSettings.AppSettings` into static field `m_strConnection` on first call (SyncLock protected) |
| `ExecuteNonQuery()` | Shared, two overloads | Executes write operations (INSERT/UPDATE/DELETE via stored proc). Second overload accepts an existing `SqlConnection` + `SqlTransaction` for client-managed transactions |
| `ExecuteQuery()` | Shared | Executes a stored proc returning known output parameters |
| `FillDataTable()` | Shared | Fills a `DataTable` via `SqlDataAdapter.Fill()` |
| `FillDataSet()` | Shared | Fills a multi-table `DataSet` |
| `UpdateDataSet()` / `UpdateDataTable()` | Shared | Uses `SqlDataAdapter.Update()` with insert/update/delete stored procs |
| `CreateConnection()` | Private | Creates `SqlConnection` from `m_strConnection` |
| `SetupCommand()` | Private | Builds `SqlCommand` from stored proc name / command type |

#### `legacy/DataAccessLib/clsTBCultureDA.vb` — `libDataAccess.TBCultureDA`
Shared (static) façade over `DataAccess`. Adds exception logging to Windows Event Log before re-throwing. All domain classes call `TBCultureDA` methods rather than `DataAccess` directly. This class inherits from nothing — it is the implicit base that BSELib classes reach via `Imports libDataAccess.libDataAccess.TBCultureDA`, which exposes static methods callable without a qualifier in VB.NET.

#### `legacy/DataAccessLib/clsInfoLog.vb` — `libDataAccess.InfoLog`
Shared logging class. `LogToEventViewer()` writes to the Windows Application event log under source `"BSESystem"`, company `"VLA"`.

#### `legacy/DataAccessLib/clsParameterList.vb` — `libDataAccess.ParameterList`
Typed collection of `clsParameter` objects. Used to build `SqlCommand.Parameters` collections for stored procedure calls. `QuickAddInputParam()` is the most-used helper method.

#### `legacy/DataAccessLib/clsUpdateParameterList.vb` — `libDataAccess.UpdateParameterList`
Extends parameter building to handle insert/update/delete stored procedure triplets used with `UpdateDataTable()`.

---

### 2.2 Case Domain

#### `legacy/BSELib/clsCase.vb` — `BSELib.clsCase`
Largest domain class. Handles the full lifecycle of a BSE case.

| Method | Stored Procedure(s) | Notes |
|--------|---------------------|-------|
| `CreateNonGBCase()` | `AddNonGBCase` | Creates a non-GB (imported animal) case in a single call |
| `DeleteCase()` | `DeleteCase` | Returns int error codes 1–6; deletes case and optionally associated farm |
| `MoveCase()` | `MoveCase` | Moves a case to an existing farm; SP uses explicit SQL transaction |
| `MoveCaseNewFarm()` | `MoveCaseNewFarm` | Moves case to a newly created farm; 10 error codes |
| `ChangeRBSE()` | `ChangeRBSE` | Renumbers a case; 15 error codes; cascades to 11 child tables |
| `GetCaseDetails()` | `GetCaseDetailsByRBSE` | Returns a multi-table `DataSet` (11 tables: Case, Clinical, BAB, OtherOwner, Feed, ClinicalVisit, DamDetails, SireDetails, Relation, Test, Casework). Table index constants are defined as `Public Const` on the class (e.g., `CASE_TABLE = 0`) |
| `CheckMandatoryFields()` | none | Pure in-process validation against in-memory `DataSet`; populates `objErrorList` |
| `UpdateCaseDetails()` | `AddCase` or `EditCase` + child SPs | Detects whether the case is new or existing via `DataRowState`; calls child SPs for Clinical, BAB, Feed, Relations, Tests, Casework |
| `FinalResultEntry()` | `EditCaseFinalResult` | Saves `FinalResult`, DBSE number, retrospective test data; uses `RowStamp` for optimistic concurrency |
| `GetBatchNumberByRBSE()` | `GetBatchNumberByRBSE` | Returns batch records linked to an RBSE number |
| `EstimateMapReference()` | `GetMapReferenceByCountyParish`, `GetPrefixCodeByXYReference` | Calculates Ordnance Survey map reference from county/parish |

Table index constants referenced in calling code (`SessionVars`, `Common.vb`, and page code-behind):

```vb
' legacy/BSELib/clsCase.vb
Public Const CASE_TABLE As Integer = 0
Public Const CLINICAL_TABLE As Integer = 1
Public Const BAB_TABLE As Integer = 2
Public Const OTHER_OWNER_TABLE As Integer = 3
Public Const FEED_TABLE As Integer = 4
Public Const CLINICAL_VISIT_TABLE As Integer = 5
Public Const DAM_DETAILS_TABLE As Integer = 6
Public Const SIRE_DETAILS_TABLE As Integer = 7
Public Const RELATION_TABLE As Integer = 8
Public Const TEST_TABLE As Integer = 9
Public Const CASEWORK_TABLE As Integer = 10
```

These integer constants are the sole identifier for `DataSet` tables; any change to SP result ordering would silently break all callers.

---

### 2.3 Farm Domain

#### `legacy/BSELib/clsFarm.vb` — `BSELib.clsFarm`

| Method | Stored Procedure(s) | Notes |
|--------|---------------------|-------|
| `FarmInDatabase()` | `GetFarmByCPHH` | Existence check; returns `True` if any row returned |
| `ChangeCPHH()` | `ChangeCPHH` | Re-identifies a farm; 8 error codes; cascades CPHH to Case, OtherOwner, HerdSize, FarmRelation |
| `GetNumberOfConfirmedCases()` | `GetNumberOfConfirmedCases` | Returns count of confirmed (positive) cases for a farm |

Farm entry Dataset table constants (analogous to `clsCase`):

```vb
Public Const FARM_TABLE As Integer = 0
Public Const RELATED_FARMS_TABLE As Integer = 1
Public Const HERDSIZE_TABLE As Integer = 2
```

---

### 2.4 Search Domain

#### `legacy/BSELib/clsSearch.vb` — `BSELib.clsSearch`
All methods are `Shared`.

| Method | Stored Procedure | Notes |
|--------|-----------------|-------|
| `GetFarmSearchResults()` | `GetSearchFarm` | 8 filter params including CPHH, OwnerName, Address, County, Herdmark, IsDealer, AHO, IncludeNonGBFarms. Timeout extended to 60 s. |
| `GetCaseSearchResults()` | `GetSearchCase` | 17 filter params including RBSE, Eartag, DBSE, Fate, FinalResult, date ranges, PassiveActive, IsImportedCase |

---

### 2.5 Audit Log Domain

#### `legacy/BSELib/clsAuditLog.vb` — `BSELib.clsAuditLog`
All methods are `Shared`. Returns data for display only — the `AuditLog` table is written exclusively by stored procedures at mutation points.

| Method | Stored Procedure |
|--------|-----------------|
| `GetCaseAuditLogReport()` | `GetAuditLogByCase` |
| `GetDailyAuditLogReport()` | `GetAuditLogByDate` |
| `GetFarmAuditLogReport()` | `GetAuditLogByFarm` |
| `GetUserAuditLogReport()` | `GetAuditLogByUser` |

Additional audit-focused stored procedures (called from ASPX code-behind directly, not via `clsAuditLog`): `GetAuditLogCaseMoves`, `GetAuditLogCPHHChanges`, `GetAuditLogNewFarms`, `GetAuditLogRBSEChanges`.

---

### 2.6 Batch Domain

#### `legacy/BSELib/clsBatch.vb` — `BSELib.clsBatch`

| Method | Stored Procedure | Notes |
|--------|-----------------|-------|
| `CreateBatchNumber()` | `AddBatchNumber` | Returns `BatchID`, `BatchYear`, `BatchNumber` as output params |
| `GetBatchIDForBatch()` | `GetBatchIDForBatch` | Resolves year + sequence to internal `BatchID` |
| `CreateBatchNumberLink()` | `AddBatchNumberLink` | Links a batch to an RBSE case and document type; supports both auto-connection and caller-supplied connection/transaction |
| `GetLatestBatchNumbers()` | `GetLatestBatchNumbers` | Returns recent batches for Home page display |

---

### 2.7 ADNS Export Domain

#### `legacy/BSELib/clsADNSReport.vb` — `BSELib.clsADNSReport`
Stateful instance class; one instance represents one export run.

- **Constructor** (GB): Calls `GetADNSCasesForGB` stored procedure. Builds `DataTable` of cases not yet notified to ADNS. Assigns sequential ADNS references (`ADNSReferenceYear` + `ADNSReferenceNumber`).
- `GetLastADNSReference()` (Shared): Calls `GetLastADNSReferenceByArea`; returns a `Hashtable` of last used reference.
- `SendEmail()`: Constructs and sends an SMTP email to `SANTE-ADNS@ec.europa.eu` via `System.Net.Mail` (or legacy `System.Web.Mail`). Updates `Case.EmailSentToADNSDate` and `Case.ADNSReferenceYear/Number` via `EditCaseADNS` stored procedure.
- Northern Ireland variant uses `CreateADNSReportTable()` (Shared) to manually build the notification table.

ASPX entry points: `ADNSExportGB.aspx.vb`, `ADNSExportNI.aspx.vb`, `ADNSExportCI.aspx.vb`.

---

### 2.8 OSS Export Domain

#### `legacy/BSELib/clsOSSExport.vb` — `BSELib.clsOSSExport`

| Method | Stored Procedure | Notes |
|--------|-----------------|-------|
| `CreateBSE1bBatch()` | `AddBatchNumber1989` | Creates a 1989-scheme OSS export batch |
| `GetOSSExportDetailsByRBSE()` | `GetOSSExportByRBSE` | Returns export fields for a single RBSE case |
| `GetCaseByBatchID()` | `GetCaseByBatchID` | Returns all cases in a batch |

Staging stored procedures (called from page code-behind): `CopyCaseToExportTable`, `CopyFarmToExportTable`, `CopyRelationToExportTable`, `CopyHerdSizeToExportTable` — populate `exp*` staging tables.

---

### 2.9 BSESS Integration Domain

#### `legacy/BSELib/clsBSESS.vb` — `BSELib.clsBSESS`
Cross-checks BSE case data against the BSESS (BSE Surveillance Survey System), whose data is loaded into `BSESSImport` by the SSIS package.

| Method | Stored Procedure |
|--------|-----------------|
| `GetBSESSCheckByDate()` | `GetBSESSCheckByDate` |
| `GetBSESSCheckByRBSE()` | `GetBSESSCheckByRBSE` — 11 output parameters |

---

### 2.10 User Management Domain

#### `legacy/BSELib/clsUser.vb` — `BSELib.clsUser`

| Method | Stored Procedure |
|--------|-----------------|
| `GetGroupForUser()` | `GetGroupForUser` — returns `UserGroup` (int) and `Name` (string) for a given NT login |
| `GetUsers()` | `GetUsers` |
| `SaveUsers()` | `AddUser` (insert), `EditUser` (update), `CannotDeleteUser` (delete — always returns error) |
| `GetUserGroups()` | `GetluUserGroup` |

---

### 2.11 Reference Data

#### `legacy/BSELib/LookupData.vb` — `BSELib.LookupData`
Resolves numeric `TableID` constants (defined in `legacy/BSESystem/Common.vb`) to stored procedure names for reference data reads.

Lookup constants defined in `Common.vb` (1–29): `LOOKUP_ANIMAL_ORIGIN=1`, `LOOKUP_FEED_RISK=2`, `LOOKUP_CASE_FATE=6`, `LOOKUP_TEST_TYPE=7`, `LOOKUP_TEST_RESULT=8`, `LOOKUP_SEX=12`, `LOOKUP_BREED=13`, etc.

---

### 2.12 Web Infrastructure

#### `legacy/BSESystem/SessionVars.vb` — `SessionVars` (Module/Class)
Central catalogue of all ASP.NET `HttpSessionState` key strings. Multi-step case entry stores entire `DataSet` objects in session:

| Session Key Constant | Value stored |
|---------------------|-------------|
| `SV_CaseDetails` | `DataSet` (11 tables) populated from `GetCaseDetailsByRBSE` |
| `SV_FarmDetails` | `DataSet` (3 tables: Farm, RelatedFarms, HerdSize) |
| `SV_BatchID`, `SV_BatchNumber` | Integer BatchID, formatted string |
| `SV_RBSENumber`, `SV_CPHHNumber` | Current case/farm identifiers |
| `SV_HeaderUserID`, `SV_HeaderGroupName`, `SV_HeaderUserName` | User identity |
| `SV_ADNSObject` | `clsADNSReport` instance (entire object stored in session) |
| `SV_OSSExportDataBSE1` | OSS export data |

Session data is cleared on successful save (`Common.RemoveCaseFromSession(Session)`) or on navigation to `Home.aspx`.

#### `legacy/BSESystem/Common.vb` — `Common` (Module)
Shared utility functions available to all pages via VB.NET module-level accessibility:
- `FormatRBSE()`, `FormatDBSE()`, `FormatCPHH()` — format raw keys with slash separators
- `GetCaseDetailsFromDatabase()` — calls `clsCase.GetCaseDetails()` and stores in session
- `GetBatchNumbersFromDatabase()` — calls `clsCase.GetBatchNumberByRBSE()` and stores in session
- Lookup constant definitions (`LOOKUP_*`)

#### `legacy/BSESystem/Global.asax.vb` — `Global` (HttpApplication)
Application lifecycle handler. `Application_Error` calls `clsAppError.LogMessage()` and `clsAppError.DisplayError()`. All other handlers (`Application_Start`, `Session_Start`, etc.) have empty bodies.

#### `legacy/BSESystem/VLAHeader.ascx` — `VLAHeader` (UserControl)
Rendered on every page. Displays page title, batch number, logged-in user and group. Contains navigation links (Home, Help). `GetUserDetails()` resolves the Windows identity to an internal user record and populates session variables.

---

## 3. End-to-End Execution Flows

### Flow 1 — Case Search

**User action:** User navigates to `SearchCase.aspx`, enters search criteria, clicks "Search".

```
SearchCase.aspx (browser)
  └─ POST to SearchCase.aspx
       └─ SearchCase.Page_Load (legacy/BSESystem/SearchCase.aspx.vb)
            └─ btnSearch_Click()
                 └─ validates at least one criterion is supplied
                      └─ BSELib.clsSearch.GetCaseSearchResults()
                           (legacy/BSELib/clsSearch.vb)
                           └─ builds ParameterList (17 params)
                                └─ TBCultureDA.FillDataTable("GetSearchCase", ...)
                                     (legacy/DataAccessLib/clsTBCultureDA.vb)
                                     └─ DataAccess.FillDataTable()
                                          (legacy/DataAccessLib/clsDataAccess.vb)
                                          └─ SqlDataAdapter.Fill(dtData)
                                               executing SP: dbo.GetSearchCase
                                               (legacy/BSEDatabase/dbo/Stored Procedures/GetSearchCase.sql)
                           └─ returns DataTable
                      └─ binds DataTable to grdResults (DataGrid)
                      └─ makes grdResults and ResultsPager visible
```

SP `GetSearchCase` performs LIKE-based filtering across `Case`, `CaseBAB`, and six lookup tables. Returns formatted RBSE and CPHH columns.

---

### Flow 2 — Case Entry and Save (New or Edit)

**User action:** User enters an RBSE number on `Home.aspx` and navigates through 6–8 entry pages, then saves.

**Step 2a — Load case from Home page:**

```
Home.aspx (browser) — user enters RBSE, clicks Go
  └─ Home.btnGo_Click()
       (legacy/BSESystem/Home.aspx.vb)
       └─ Session[SV_RBSENumber] = sRBSE
            └─ Common.GetCaseDetailsFromDatabase(sRBSE, Session)
                 (legacy/BSESystem/Common.vb)
                 └─ clsCase.GetCaseDetails(sRBSE, dsData)
                      (legacy/BSELib/clsCase.vb)
                      └─ TBCultureDA.FillDataSet("GetCaseDetailsByRBSE", ...)
                           executing SP: dbo.GetCaseDetailsByRBSE
                           returns multi-result DataSet (11 tables)
                      └─ sets primary keys on tables 4,5,3,8,9
                 └─ Session[SV_CaseDetails] = dsData (DataSet, 11 tables)
                 └─ Session[SV_CPHHNumber] = dsData.Tables(0).Rows(0)["CPHH"]
       └─ Response.Redirect("CaseEntryFarm.aspx")
```

**Step 2b — User edits across pages:**  
Pages `CaseEntryFarm.aspx`, `CaseEntryClinical.aspx`, `CaseEntryBAB.aspx`, `CaseEntryFeeds.aspx`, `CaseEntryRelations.aspx`, `CaseEntryVLA.aspx`, `CaseEntryDEFRA.aspx` each read `DataSet` from Session, modify rows in-place, and write modified `DataSet` back to Session on navigation. No database writes occur during this phase.

**Step 2c — Final save:**

```
CaseEntrySave.aspx (browser) — POST on page load (IsPostBack=False path)
  └─ CaseEntrySave.PerformSave()
       (legacy/BSESystem/CaseEntrySave.aspx.vb)
       └─ dsCase  = Session[SV_CaseDetails]   (DataSet)
            dsFarm  = Session[SV_FarmDetails]   (DataSet)
            iBatchID = Session[SV_BatchID]
       └─ clsCase.CheckMandatoryFields(dsCase, dsFarm, objErrorList)
            (legacy/BSELib/clsCase.vb)
            — validates CPHH, OwnerName, Address1, Parish, County, AHO, ADNSRegionID
            — validates Eartag, FormADate, Fate
       └─ (if valid) clsCase.UpdateCaseDetails(UserID, BatchID, dsCase, dsFarm, objErrorList)
            (legacy/BSELib/clsCase.vb line 874)
            └─ detects DataRowState (Added vs Modified) on dsCase.Tables[CASE_TABLE]
            └─ if new: TBCultureDA.ExecuteNonQuery("AddCase", ...)
               if edit: TBCultureDA.ExecuteNonQuery("EditCase", ...)
               — uses RowStamp for optimistic concurrency on EditCase
            └─ child table updates: AddCaseClinical/EditCaseClinical,
               AddCaseBAB/EditCaseBAB, AddCaseFeed/EditCaseFeed/DeleteCaseFeed,
               AddClinicalVisit/EditClinicalVisit/DeleteClinicalVisit,
               AddCaseRelation/EditCaseRelation/DeleteCaseRelation,
               AddTest/EditTest/DeleteTest, AddCaseWork/EditCaseWork
            └─ if BatchID > 0: clsBatch.CreateBatchNumberLink(BatchID, RBSE, Document)
       └─ on success: Common.RemoveCaseFromSession(Session)
            Response.Redirect("home.aspx")
```

The `AddCase` stored procedure (`legacy/BSEDatabase/dbo/Stored Procedures/AddCase.sql`) verifies RBSE uniqueness, inserts an `AuditLog` record, and inserts the `Case` row. It is designed to be called within a transaction, though `clsCase.UpdateCaseDetails` does not currently wrap all child SP calls in a single database transaction — each SP call opens its own connection.

---

### Flow 3 — ADNS Export (GB Cases)

**User action:** Authorised user (DEFRA Maintenance or VLA Maintenance) opens `ADNSExportGB.aspx`, enters an ADNS reference, clicks "Generate Report", then clicks "Send Email".

```
ADNSExportGB.aspx (browser)
  └─ Page_Load → EnableControls() checks Session[SV_HeaderGroupName]
       redirects non-authorised groups to Home.aspx
  └─ Page_Load → clsADNSReport.GetLastADNSReference("GB")
       (legacy/BSELib/clsADNSReport.vb)
       └─ TBCultureDA.FillDataSet("GetLastADNSReferenceByArea", ...)
            executing SP: dbo.GetLastADNSReferenceByArea
       → pre-populates ADNS year/number fields

  └─ btnGenerateReport_Click()
       (legacy/BSESystem/ADNSExportGB.aspx.vb)
       └─ new BSELib.clsADNSReport(emailRef, year, number)
            (legacy/BSELib/clsADNSReport.vb constructor, GB branch)
            └─ TBCultureDA.FillDataSet("GetADNSCasesForGB", ...)
                 executing SP: dbo.GetADNSCasesForGB
                 — returns cases where EmailSentToADNSDate IS NULL and FinalResult='Pos'
            └─ assigns sequential ADNSReferenceYear/Number to each case
            └─ builds email body (Subject, Body text)
            └─ identifies MissingCases (cases without ADNS region)
       └─ Session[SV_ADNSObject] = objADNS
       └─ binds SummaryCases DataTable to grdADNSSummary

  └─ btnSendEmail_Click()
       └─ objADNS = Session[SV_ADNSObject]
            └─ clsADNSReport.SendEmail(True, userEmail)
                 └─ System.Net.Mail.SmtpClient(SMTPHost, SMTPPort).Send(...)
                      To: SANTE-ADNS@ec.europa.eu
                 └─ for each case in report:
                      TBCultureDA.ExecuteNonQuery("EditCaseADNS", ...)
                           executing SP: dbo.EditCaseADNS
                           — sets Case.EmailSentToADNSDate, ADNSReferenceYear, ADNSReferenceNumber
       └─ Response.Redirect("MaintenanceConfirmation.aspx?title=ADNS Export Complete...")
```

---

## 4. Database Schema Key Relationships

```
Farm (CPHH)
  └─ Case (RBSE → CPHH FK)
       ├─ CaseClinical (RBSE FK)
       ├─ CaseBAB (RBSE FK)
       ├─ CaseFeed (RBSE FK)
       ├─ CaseRelation (RBSE FK, RelatedRBSE FK self-ref)
       ├─ CaseTest (RBSE FK)
       ├─ CaseWork (RBSE FK)
       ├─ OtherOwner (RBSE FK, CPHH FK)
       ├─ Pedigree (RBSE FK)
       ├─ lnkBatchCase (RBSE FK, BatchID FK)
       └─ CaseHistorical (RBSE FK)

User (ID)
  └─ AuditLog (UserID FK, RBSE/CPHH recorded as strings)

Batch (ID)
  └─ lnkBatchCase (BatchID FK)

luXxx tables → FK references from Case, Farm, and child tables
```

---

## 5. Stored Procedure Count by Category

| Category | Count (approx.) |
|----------|----------------|
| `Add*` — insert operations | 40 |
| `Edit*` — update operations | 40 |
| `Delete*` — delete operations | 30 |
| `Get*` — read operations | 90 |
| `Copy*` — export staging | 4 |
| `Change*`, `Move*` — complex identity changes | 4 |
| Total | ~230 |

---

## 6. Coupling Hotspots

### 6.1 DataSet Table Index Constants — HIGH RISK

`clsCase.CASE_TABLE`, `CLINICAL_TABLE`, etc. (integer constants 0–10) are used by every case entry ASPX page to index into the `DataSet` returned by `GetCaseDetailsByRBSE`. The mapping from integer to table is not enforced by any type-safe mechanism. Any change to the stored procedure's result-set ordering would silently corrupt all callers.

**Affected files:** `legacy/BSELib/clsCase.vb`, all `CaseEntry*.aspx.vb` pages, `Common.vb`.

### 6.2 ASP.NET InProc Session as State Transport — HIGH RISK

All multi-page case entry data flows through `HttpSessionState`. Twelve or more `DataSet`/`DataTable`/`DataView` objects plus the entire `clsADNSReport` object are stored in session (see `SessionVars.vb`). A session expiry (20-minute timeout) or app pool recycle mid-flow discards all unsaved data with no recovery mechanism.

### 6.3 Hardcoded String-based User Group Authorisation — MEDIUM RISK

Access control is implemented by comparing `Session[SV_HeaderGroupName]` to string literals (`"DEFRA Viewer"`, `"VLA Maintenance"`, etc.) inside each page's `EnableControls()` method. There is no centralised authorisation check or attribute-based access control. A group name change in the database requires updating every affected page's code.

**Affected files:** Every ASPX code-behind that restricts access; `ADNSExportGB.aspx.vb` is a representative example.

### 6.4 Static Shared Connection String in `DataAccess` — MEDIUM RISK

`DataAccess.m_strConnection` is a `Private Shared` field on the static class. Once set, it is never refreshed. The connection string is read from `ConfigurationSettings.AppSettings` (deprecated API). There is no mechanism to update the connection at runtime without restarting the application.

### 6.5 `ChangeRBSE` Stored Procedure — HIGH RISK

`ChangeRBSE` cascades an RBSE number change across **15 child tables** within a single stored procedure (`legacy/BSEDatabase/dbo/Stored Procedures/ChangeRBSE.sql`). Error handling uses the pre-SQL-2005 `@@ERROR` pattern (no `TRY/CATCH`). Any intermediate failure leaves the database in a partially updated state if the stored procedure does not roll back cleanly.

### 6.6 `OptionStrict Off` in BSESystem.vbproj — MEDIUM RISK

`BSESystem.vbproj` sets `<OptionStrict>Off</OptionStrict>`, allowing implicit type coercions and late binding in all ASPX code-behind files. This hides type mismatches that would otherwise be compile errors.

### 6.7 No Separation Between Presentation and Data Loading in ASPX Pages — MEDIUM RISK

Many ASPX pages call BSELib methods directly in event handlers (e.g., `Page_Load`, `btnSearch_Click`) with no intermediate controller or service layer pattern. Business rule enforcement (e.g., field validation in `CheckMandatoryFields`) is mixed between page code-behind and BSELib classes, with no consistent layering.

### 6.8 Lookup Table ID Magic Numbers — LOW-MEDIUM RISK

The integer IDs (`1`–`29`) defined as constants in `Common.vb` map to stored procedure names via a `Select Case` inside `LookupData.GetSelectProc()`. Adding a new lookup table requires modifying `LookupData.vb`, `Common.vb`, and the database.

---

## 7. EarTag Validation

**Module:** `legacy/BSELib/EartagValidation/` and `legacy/BSESystem/EartagValidation.vb`

Eartag format is validated using regular expressions stored in `Web.config`:
- `IsoEarTagCommonPartExpression`
- `IsoEarTagAlphaNumericCountryPartExpression`
- `IsoEarTagNumericCountryPartExpression`
- `IsoEarTagAnimalPartExpression`
- `IsoEarTagHerdmarkPartExpression`

The three-part eartag (country + herdmark + animal number) is handled by the `ThreePartEartag.ascx` and `Eartag.ascx` user controls. The `RBSE.ascx` control handles the Regional BSE reference number input.
