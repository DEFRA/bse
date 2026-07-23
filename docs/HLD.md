# BSE System — High-Level Design (HLD)

> **Status:** As-is documentation of the legacy codebase. No future-state speculation.  
> **Date:** 2026-07-20  
> **Source path:** `legacy/`

---

## 1. System Purpose

The BSE System (Bovine Spongiform Encephalopathy System) is a case-management and surveillance application used by APHA (Animal and Plant Health Agency), formerly the Veterinary Laboratories Agency (VLA). It tracks confirmed and suspected BSE cases in Great Britain, Northern Ireland, and imported (non-GB) animals, manages the associated farms (premises), and produces regulatory exports to the EU's Animal Disease Notification System (ADNS) and the OIE (via OSS export).

---

## 2. Major Components

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        BSESystem (ASP.NET WebForms)                         │
│  legacy/BSESystem/   — ~90 ASPX pages + ~15 ASCX user controls             │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │ project reference
┌───────────────────────────────────▼─────────────────────────────────────────┐
│                           BSELib (Class Library)                            │
│  legacy/BSELib/   — 12 domain/service classes + EartagValidation module    │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │ project reference
┌───────────────────────────────────▼─────────────────────────────────────────┐
│                      DataAccessLib / libDataAccess (Class Library)          │
│  legacy/DataAccessLib/   — ADO.NET wrapper: DataAccess, TBCultureDA,        │
│                            ParameterList, InfoLog                           │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │ SQL (stored procedures only)
┌───────────────────────────────────▼─────────────────────────────────────────┐
│               SQL Server — database: BSE (server: vm-aphadev-003)           │
│  legacy/BSEDatabase/  — 60+ tables, ~230 stored procedures, views          │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│              BSEIntegrationServices (SSIS packages)                         │
│  legacy/BSEIntegrationServices/  — two SSIS packages:                       │
│    • BSESS Import.dtsx   (imports BSESS surveillance data into BSESSImport) │
│    • BSE Access Export.dtsx (exports case data to Access/downstream)        │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.1 BSESystem — Web Application

- **Type:** ASP.NET WebForms, VB.NET, .NET Framework 4.0  
- **Project file:** `legacy/BSESystem/BSESystem.vbproj`  
- **Assembly name:** `BSESystem`  
- **Root namespace:** `BSESystem`  
- **Entry point:** `legacy/BSESystem/Global.asax` / `Home.aspx` (IIS default document)  
- **Third-party packages:** `AjaxControlToolkit 17.1.1.0` (see `packages.config`); `System.Web.Extensions 1.0.61025.0`

### 2.2 BSELib — Business Logic Library

- **Type:** VB.NET Class Library, .NET Framework 4.0  
- **Project file:** `legacy/BSELib/BSELib.vbproj`  
- **Assembly name:** `BSELib`  
- **Root namespace:** `BSELib`  
- **OptionStrict:** On  
- Contains all domain logic classes: `clsCase`, `clsFarm`, `clsSearch`, `clsAuditLog`, `clsBatch`, `clsBSESS`, `clsUser`, `clsADNSReport`, `clsOSSExport`, `clsRelations`, `clsDataCheck`, `clsLog`, `LookupData`, `EartagValidation/`

### 2.3 DataAccessLib — Data Access Layer

- **Type:** VB.NET Class Library, .NET Framework 4.0  
- **Project file:** `legacy/DataAccessLib/libDataAccess.vbproj`  
- **Namespace:** `libDataAccess`  
- Contains generic ADO.NET infrastructure: `DataAccess` (static/Shared class), `TBCultureDA` (facade with error logging), `ParameterList`, `UpdateParameterList`, `InfoLog`, `clsParameter`

### 2.4 BSEDatabase — SQL Server Database Project

- **Project file:** `legacy/BSEDatabase/BSEDatabase.sqlproj`  
- **Contents:**
  - `dbo/Tables/` — 60+ table definitions  
  - `dbo/Stored Procedures/` — ~230 stored procedures (sole data access method)  
  - `dbo/Views/` — supporting views  
  - `dbo/Functions/` — scalar and table-valued functions  
  - `Legacy/` — legacy schema artefacts  
  - `Release Scripts/` — deployment scripts  
  - Two UAT SSIS packages (`.dtsx`) for testing

### 2.5 BSEIntegrationServices — SSIS Integration

- **Project file:** `legacy/BSEIntegrationServices/BSEIntegrationServices.dtproj`  
- Two DTSX packages:
  - `BSESS Import.dtsx` — imports BSE Surveillance Survey data into `BSESSImport` table
  - `BSE Access Export.dtsx` — exports BSE case data to a downstream system
- UAT variants exist under `legacy/BSEIntegrationServices/uat/`

---

## 3. Domain Map

| Domain | Owns Data | Owns Business Rules | Web Surface Area |
|--------|-----------|---------------------|------------------|
| **Case Management** | `Case`, `CaseClinical`, `CaseBAB`, `CaseFeed`, `CaseRelation`, `CaseTest`, `ClinicalVisit`, `OtherOwner`, `Pedigree`, `CaseHistorical` | `BSELib/clsCase.vb` | `CaseEntry*.aspx`, `ShowCase.aspx`, `DeleteCase.aspx`, `MoveCase*.aspx`, `FinalResult*.aspx`, `NonGBCaseCreation.aspx` |
| **Farm Management** | `Farm`, `FarmRelation`, `HerdSize`, `FarmHistorical` | `BSELib/clsFarm.vb` | `CaseEntryFarm.aspx`, `NewFarm.aspx`, `CPHHChange.aspx`, `SearchFarm.aspx` |
| **Batch / Sampling** | `Batch`, `lnkBatchCase` | `BSELib/clsBatch.vb` | `BatchNumber*.aspx`, `PrintBatch.aspx` |
| **Search** | reads `Case`, `Farm` | `BSELib/clsSearch.vb` | `SearchCase.aspx`, `SearchFarm.aspx`, `SearchCPHH.aspx`, `SearchCaseByHerdmark.aspx`, `SearchOutstandingData.aspx`, `SearchRelatedAnimal.aspx` |
| **Audit Logging** | `AuditLog` | `BSELib/clsAuditLog.vb` | `AuditLog*.aspx`, `CaseAuditLogReport.aspx`, `FarmAuditLogReport.aspx` |
| **Animal Relations** | `CaseRelation` | `BSELib/clsRelations.vb` | `CaseEntryRelations.aspx`, `RelationsPopup.aspx`, `PickSireDam.aspx` |
| **ADNS Export** | writes `Case.ADNSReferenceYear/Number`, `Case.EmailSentToADNSDate` | `BSELib/clsADNSReport.vb` | `ADNSExportGB.aspx`, `ADNSExportCI.aspx`, `ADNSExportNI.aspx`, `ADNSExportMenu.aspx` |
| **OSS Export** | reads `Case`, `Farm`; writes `expCase`, `expFarm`, `expRelation` | `BSELib/clsOSSExport.vb` | `OSSExport*.aspx` |
| **BSESS Cross-check** | `BSESSImport` (populated by SSIS) | `BSELib/clsBSESS.vb` | `BSESSCheck*.aspx`, `BSESSMenu.aspx` |
| **Reference Data** | `lu*` lookup tables | `BSELib/LookupData.vb` | `PickList*.aspx`, `PickFarm.aspx`, `PickSupplier.aspx` |
| **User Management** | `User`, `luUserGroup` | `BSELib/clsUser.vb` | `UserMaintenance.aspx` |
| **CaseWork** | `CaseWork` | embedded in `clsCase.vb` | `CaseWork*.aspx` |

---

## 4. Data Stores

### 4.1 SQL Server Database (`BSE` on `vm-aphadev-003`)

Primary and only data store. All access is via stored procedures — no ad-hoc SQL or ORM.

**Core tables:**

| Table | Primary Key | Description |
|-------|-------------|-------------|
| `Case` | `RBSE CHAR(9)` | Primary BSE case record. RBSE = Regional BSE reference number |
| `Farm` | `CPHH CHAR(11)` | Premises (County Parish Holding Herdmark) |
| `AuditLog` | `ID INT IDENTITY` | Immutable change log — all mutations recorded here |
| `User` | `ID INT IDENTITY` | Application users, keyed by NT login |
| `Batch` | `ID INT IDENTITY` | Sampling batch records |
| `lnkBatchCase` | composite | Links batches to RBSE cases |
| `CaseClinical` | FK `RBSE` | Clinical observations |
| `CaseBAB` | FK `RBSE` | Born After the Ban data |
| `CaseFeed` | FK `RBSE` | Feed history |
| `CaseRelation` | FK `RBSE` | Related animals (dam, sire, offspring) |
| `CaseTest` | FK `RBSE` | Laboratory test records |
| `CaseWork` | FK `RBSE` | Casework minute/action records |
| `OtherOwner` | FK `RBSE`/`CPHH` | Previous/other owners |
| `Pedigree` | FK `RBSE` | Pedigree details |
| `HerdSize` | FK `CPHH` | Annual herd size records |
| `FarmRelation` | FK `CPHH` | Farm-to-farm relationships |
| `BSESSImport` | — | Staging table populated by SSIS from BSESS system |
| `expCase`, `expFarm`, `expRelation`, `expAge` | — | Export staging tables |
| `CaseHistorical`, `FarmHistorical` | — | History tables for RBSE/CPHH changes |
| `luXxx` (~20 tables) | `Code` or `ID` | Reference/lookup data |

**Key constraints observed:**
- `Case.RBSE` format: `[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]`
- `Case.CPHH` foreign key to `Farm.CPHH`
- `Farm.CPHH` format: `[0-9]{11}`
- `Case` has 20+ CHECK constraints enforcing domain rules at database level
- `AuditLog` has FK to `User.ID` and CHECK constraints on RBSE/CPHH formats
- Optimistic concurrency enforced via `ROWVERSION` columns (`RowStamp`) on `Case` and `Farm`

### 4.2 Windows Event Log

Application exceptions are written to the Windows Event Log via `InfoLog.LogToEventViewer()` (`legacy/DataAccessLib/clsInfoLog.vb`). No database-based logging of application errors exists.

---

## 5. External Dependencies

| Dependency | Type | Configuration Key / Location |
|------------|------|-------------------------------|
| SQL Server `vm-aphadev-003`, database `BSE` | Relational database | `Web.config` → `DBConnectionString` |
| Windows Active Directory (NTLM) | Authentication | `Web.config` → `<authentication mode="Windows">` |
| SMTP relay `appssmtprelay.demeter.zeus.gsi.gov.uk:25` | Email (ADNS export) | `Web.config` → `SMTPHost`, `SMTPPort` |
| EU ADNS recipient `SANTE-ADNS@ec.europa.eu` | Regulatory notification | `Web.config` → `ADNSEmailToAddress` |
| APHA sender `bse.adns@apha.gov.uk` | Email identity | `Web.config` → `ADNSEmailFromAddress` |
| BSESS system (external) | Cross-check source | Imported via SSIS (`BSEIntegrationServices/BSESS Import.dtsx`) |
| SharePoint (SPOL) `defra.sharepoint.com/teams/Team3516/BSE/` | Document store (referenced) | `Web.config` → `SPOLSite` |

---

## 6. Authentication and Authorisation

- **Authentication:** Windows Integrated (NTLM/Kerberos). Anonymous access is denied at IIS level (`<deny users="?"/>` in `Web.config`).
- **Authorisation:** NT login is resolved to an internal `User` record and `luUserGroup` at login via `GetGroupForUser` stored procedure. Group names are stored in ASP.NET Session (`SV_HeaderGroupName`).
- **Observed user groups** (from `ADNSExportGB.aspx.vb`): `DEFRA Viewer`, `DEFRA Data Entry`, `DEFRA Maintenance`, `VLA Data Entry`, `VLA Maintenance`. Access control is enforced by page-level `Response.Redirect` checks on group name string comparison (not role-based claims).
- The `VLAHeader` user control (`legacy/BSESystem/VLAHeader.ascx.vb`) is included on every page and calls `GetUserDetails()` to populate session-based user identity.

---

## 7. Runtime Assumptions

- Deployed to **IIS** (not IIS Express). `BSESystem.vbproj` sets `<UseIISExpress>false</UseIISExpress>`.
- **Session state:** `InProc`, 20-minute timeout, cookie-based. Requires single-server deployment — no distributed session store.
- **Database connection string** is read from `Web.config` `appSettings` key `DBConnectionString`. The connection string contains a SQL Server login with plaintext password.
- `DBSQLServerNative=1` instructs the `DataAccess` class to use `SqlClient` (rather than OleDB).
- The `DataAccess.Initialise()` method uses `ConfigurationSettings.AppSettings` (a deprecated API since .NET 2.0) with a `SyncLock` to cache the connection string as a static field on first call.
- **Compilation:** `debug="true"` in `Web.config`; this is the committed setting and applies to the production-deployed configuration as found.
- **Character encoding:** UTF-8 request and response (`globalization` element in `Web.config`).
- **AJAX:** Microsoft ASP.NET AJAX Extension 1.0 (version 1.0.61025.0) with ScriptModule. Also AjaxControlToolkit.
- The application has no start-up initialisation in `Global.asax.Application_Start` — the handler body is empty.

---

## 8. Solution Structure Summary

```
legacy/BSESystem.sln
├── BSESystem/          (ASP.NET WebForms web app, .NET 4.0)
│   └── references → BSELib, DataAccessLib (project references)
├── BSELib/             (Business logic library, .NET 4.0)
│   └── references → DataAccessLib (project reference)
├── DataAccessLib/      (ADO.NET wrapper library, .NET 4.0)
│   └── references → System.Data, System.Configuration
├── BSEDatabase/        (SQL Server database project)
└── BSEIntegrationServices/ (SSIS packages — separate tooling)
```

`legacy/SharedAssemblyInfo.vb` provides shared assembly version metadata across projects.
