# BSE System — Runbook

> **Status:** As-is documentation of the legacy codebase. No future-state speculation.  
> **Date:** 2026-07-20  
> **Source path:** `legacy/`

---

## 1. Prerequisites

| Dependency | Version / Notes |
|------------|----------------|
| .NET Framework | 4.0 (target in all `.vbproj` files) |
| Visual Studio | 2015 or later (MSBuild ToolsVersion 14.0 used in project files) |
| SQL Server | SQL Server (version not pinned in project; BSE database hosted on `vm-aphadev-003`) |
| SSDT (SQL Server Data Tools) | Required to open and deploy `BSEDatabase.sqlproj` |
| SQL Server Integration Services | Required to open/run `.dtsx` packages in `BSEIntegrationServices/` |
| IIS | Required for deployment; IIS Express is explicitly disabled (`<UseIISExpress>false</UseIISExpress>`) |
| Windows Authentication | Application requires a Windows domain account; anonymous access is denied |

---

## 2. Solution Open and Build

### 2.1 Open the Solution

Open `legacy/BSESystem.sln` in Visual Studio 2015 or later.

Projects loaded by the solution:
- `BSESystem/BSESystem.vbproj` — ASP.NET WebForms web application
- `BSELib/BSELib.vbproj` — Business logic class library
- `DataAccessLib/libDataAccess.vbproj` — Data access class library
- `BSEDatabase/BSEDatabase.sqlproj` — SQL Server database project

The `BSEIntegrationServices` SSIS project is opened separately in SQL Server Data Tools (SSDT) or Visual Studio with the SSIS extension.

### 2.2 Restore NuGet Packages

```
Right-click solution in Solution Explorer → Restore NuGet Packages
```

Or from the Package Manager Console:

```powershell
Update-Package -reinstall
```

The only NuGet package is `AjaxControlToolkit 17.1.1.0` (declared in `BSESystem/packages.config`).

### 2.3 Build Order

Build must occur in dependency order (Visual Studio handles this automatically via project references):

1. `libDataAccess` (DataAccessLib)
2. `BSELib`
3. `BSESystem`

MSBuild command-line build from `legacy/`:

```cmd
msbuild BSESystem.sln /p:Configuration=Debug /p:Platform="Any CPU"
```

or for Release:

```cmd
msbuild BSESystem.sln /p:Configuration=Release /p:Platform="Any CPU"
```

Build output for `BSESystem` goes to `BSESystem/bin/`. The web application is deployed in-place (IIS points to the `BSESystem/` folder, not a published output directory).

---

## 3. Database Setup

### 3.1 Deploy the Database Schema

The `BSEDatabase.sqlproj` is a SQL Server Data Tools (SSDT) project. To publish:

1. Open `legacy/BSEDatabase/BSEDatabase.sqlproj` in Visual Studio with SSDT.
2. Right-click project → **Publish**.
3. Configure target server and database name (`BSE`).
4. Review the deployment preview (dacpac diff).
5. Click **Publish**.

Alternatively, generate a `.dacpac` and deploy with `SqlPackage.exe`:

```cmd
SqlPackage.exe /Action:Publish /SourceFile:BSEDatabase.dacpac /TargetServerName:vm-aphadev-003 /TargetDatabaseName:BSE
```

**Release scripts** are in `legacy/BSEDatabase/Release Scripts/`. These are likely manual migration scripts applied outside of SSDT deployment; review before applying to any environment.

### 3.2 Database Connection

The connection string is in `legacy/BSESystem/Web.config`:

```xml
<add key="DBConnectionString"
     value="Data Source=vm-aphadev-003;Initial Catalog=BSE;User Id=BSESystemUser;Password=password;"/>
```

> **Security note:** This connection string contains a plaintext SQL Server login and password. Before deploying to any environment, replace the password with the actual credential. Do not commit real credentials to source control.

To point at a local SQL Server instance for development, update `Data Source` to `(localdb)\MSSQLLocalDB` or your local instance name and ensure the `BSE` database is deployed there.

---

## 4. IIS Configuration

The application must be hosted under IIS (not IIS Express).

### 4.1 Minimum IIS Configuration

1. Create an IIS Application pointing to the `legacy/BSESystem/` directory.
2. Set the Application Pool to **.NET Framework v4.0**, **Integrated Pipeline**.
3. Enable **Windows Authentication** on the IIS site/application.
4. Disable **Anonymous Authentication**.
5. The default document is `Home.aspx` (set in `Web.config` → `<defaultDocument>`).

### 4.2 Authentication Note

The application uses Windows Integrated Authentication (NTLM/Kerberos). The requesting user must have a matching `NTLogin` record in the `dbo.[User]` table. The `GetGroupForUser` stored procedure resolves the Windows identity to an internal user group. If the user is not found in the `User` table, they will receive an error or empty group name.

---

## 5. SSIS Package Execution

### 5.1 BSESS Import

Package: `legacy/BSEIntegrationServices/BSESS Import.dtsx`  
Purpose: Imports BSE Surveillance Survey data from an external source into the `BSESSImport` table.

```
dtexec /f "legacy\BSEIntegrationServices\BSESS Import.dtsx" /Decrypt <password>
```

> **Note:** The SSIS package password is required for decryption (`README.txt` contains `package password = pass` — verify this is the correct password for the environment before executing).

### 5.2 BSE Access Export

Package: `legacy/BSEIntegrationServices/BSE Access Export.dtsx`  
Purpose: Exports case data from the BSE database to a downstream system.

```
dtexec /f "legacy\BSEIntegrationServices\BSE Access Export.dtsx" /Decrypt <password>
```

UAT variants are in `legacy/BSEIntegrationServices/uat/` and `legacy/BSEDatabase/UAT BSE Access Export.dtsx`, `legacy/BSEDatabase/UAT BSESS Import.dtsx`.

---

## 6. Running and Testing

### 6.1 No Automated Tests

**No unit tests, integration tests, or end-to-end tests were found in the repository.** There are no test projects in `BSESystem.sln`, no `NUnit`, `MSTest`, or `xUnit` packages, and no CI pipeline configuration.

All verification is manual:
- Run the application under IIS.
- Log in with a Windows domain account present in the `dbo.[User]` table.
- Navigate to `Home.aspx`.

### 6.2 Manual Smoke Test Sequence

1. Navigate to `Home.aspx` — verify page loads and user name/group appears in header.
2. Enter a known RBSE number → click Go → verify case entry pages load with correct data.
3. Navigate to `SearchCase.aspx` → enter a known RBSE → click Search → verify result appears.
4. Navigate to `SearchFarm.aspx` → enter a known CPHH → verify farm data loads.
5. Navigate to `AuditLogMenu.aspx` → view a daily audit log → verify data appears.

---

## 7. Configuration Reference

All configuration is in `legacy/BSESystem/Web.config`.

| Key | Default value | Purpose |
|-----|---------------|---------|
| `DBSQLServerNative` | `1` | Use `SqlClient` (1) vs OleDB (0) — always `1` |
| `DBConnectionString` | `Data Source=vm-aphadev-003;Initial Catalog=BSE;...` | SQL Server connection string (contains plaintext password) |
| `ADNSEmailFromAddress` | `bse.adns@apha.gov.uk` | From address for ADNS export emails |
| `ADNSEmailToAddress` | `SANTE-ADNS@ec.europa.eu` | EU recipient for ADNS notifications |
| `SMTPHost` | `appssmtprelay.demeter.zeus.gsi.gov.uk` | SMTP relay host |
| `SMTPPort` | `25` | SMTP relay port |
| `SystemVersion` | `LIVE Version` | Displayed in page header |
| `SPOLSite` | `https://defra.sharepoint.com/teams/Team3516/BSE/...` | SharePoint document library URL |
| `IsoEarTagCommonPartExpression` | `[0,1,2,_, ]$` | Regex for ISO ear tag common part |
| `IsoEarTagAlphaNumericCountryPartExpression` | `^[A-Z][A-Z]` | Regex for ISO ear tag country (alpha) |
| `IsoEarTagNumericCountryPartExpression` | `^[0-9][0-9][0-9]` | Regex for ISO ear tag country (numeric) |
| `IsoEarTagAnimalPartExpression` | `^[0-9][0-9][0-9][0-9][0-9]$` | Regex for ISO ear tag animal number |
| `IsoEarTagHerdmarkPartExpression` | `^[0-9][0-9][0-9][0-9][0-9][0-9]$` | Regex for ISO ear tag herdmark |

Session timeout: 20 minutes (`<sessionState timeout="20"/>`).

---

## 8. Known Technical Debt and Limitations

| Issue | Location | Severity |
|-------|----------|----------|
| Plaintext SQL Server password in Web.config committed to source control | `legacy/BSESystem/Web.config` → `DBConnectionString` | **Critical** |
| `debug="true"` set in `<compilation>` — performance and information disclosure risk in production | `legacy/BSESystem/Web.config` | High |
| `OptionStrict Off` in BSESystem.vbproj — allows implicit type coercions and late binding; masks type errors at compile time | `legacy/BSESystem/BSESystem.vbproj` | High |
| Deprecated `ConfigurationSettings.AppSettings` API (replaced by `ConfigurationManager.AppSettings` in .NET 2.0) | `legacy/DataAccessLib/clsDataAccess.vb` → `Initialise()` | Medium |
| Multi-SP `UpdateCaseDetails()` executes across separate database connections — no single transaction wrapping the full case save | `legacy/BSELib/clsCase.vb` → `UpdateCaseDetails()` | High |
| Stored procedures use `@@ERROR` (pre-SQL-2005 pattern) instead of `TRY/CATCH/THROW` — partial failure behaviour is unpredictable | `legacy/BSEDatabase/dbo/Stored Procedures/AddCase.sql`, `MoveCase.sql`, `ChangeRBSE.sql`, etc. | High |
| InProc session — single-server only; app pool recycle discards all unsaved user data | `legacy/BSESystem/Web.config` → `sessionState mode="InProc"` | High |
| Access control enforced by string comparison of group name on each page individually — no centralised authorisation | Every `*.aspx.vb` with `EnableControls()` | Medium |
| DataSet table index constants (integers 0–10) used to address `GetCaseDetailsByRBSE` result tables — no type safety | `legacy/BSELib/clsCase.vb`, all `CaseEntry*.aspx.vb` | High |
| `clsADNSReport` full object serialised into session during ADNS export flow | `legacy/BSESystem/ADNSExportGB.aspx.vb` | Medium |
| No automated tests of any kind | Entire codebase | High |
| AjaxControlToolkit 17.1.1.0 (2017) targeting `net20` in `packages.config`, while the app targets .NET 4.0 | `legacy/BSESystem/packages.config` | Low |
| `System.Web.Extensions` version 1.0.61025.0 — a very early ASP.NET AJAX release | `legacy/BSESystem/Web.config`, `BSESystem.vbproj` | Low |
| Source Control Annotation `SAK` (SourceSafe/TFS placeholder) still present in all `.vbproj` files — indicates incomplete SCM migration | `SccProvider`, `SccProjectName` in all `.vbproj` files | Low |
| `Global.asax.vb` `Application_Error` calls `Server.GetLastError().InnerException.ToString()` without null checking — will throw NullReferenceException if InnerException is null | `legacy/BSESystem/Global.asax.vb` → `Application_Error()` | Medium |

---

## 9. Modern Solution — SP Source Control (Slice 1)

> **Added:** Slice 1 — Stored Procedure Source Control  
> **Status:** Slice 1 complete. SP files are under version control but no application code has changed.

### 9.1 What Was Added

All 257 stored procedure `.sql` files from `legacy/BSEDatabase/dbo/Stored Procedures/` have been copied verbatim into `src/BSE.Database/` and organised into domain-aligned subfolders. Supporting schema objects (tables, views, functions) are also included so the SQL project builds cleanly.

The file content is **binary-identical** to the legacy source. No SP logic has been modified — that occurs in later slices (see `src/BSE.Database/SP-Inventory.md` for per-SP ownership).

### 9.2 New Project Layout

```
src/
└── BSE.Database/
    ├── BSE.Database.sqlproj          ← SDK-style SQL project (Microsoft.Build.Sql 2.2.0)
    ├── SP-Inventory.md               ← Full SP inventory with domain mapping and slice ownership
    ├── StoredProcedures/
    │   ├── ReferenceData/            (136 SPs — all lu* + editable lookups + geo lookups)
    │   ├── CaseManagement/           (44 SPs — full case lifecycle)
    │   ├── AnimalRelations/          (12 SPs — CaseRelation, dam/sire)
    │   ├── FarmManagement/           (16 SPs — Farm, HerdSize, CPHH change)
    │   ├── AuditLog/                 (8 SPs)
    │   ├── Search/                   (8 SPs)
    │   ├── CaseWork/                 (7 SPs — minutes, CaseWork entries)
    │   ├── Batch/                    (7 SPs)
    │   ├── OssExport/                (6 SPs)
    │   ├── UserManagement/           (6 SPs)
    │   ├── AdnsExport/               (4 SPs)
    │   ├── BsessIntegration/         (2 SPs)
    │   ├── Shared/                   (1 SP — GetCaseByBatchID, cross-domain)
    │   └── Legacy/                   (0 SPs — reserved for any SPs that cannot be mapped)
    ├── Tables/                       (68 table definitions — verbatim from legacy)
    ├── Views/                        (2 views — verbatim from legacy)
    └── Functions/                    (2 scalar functions — verbatim from legacy)
```

### 9.3 Build Commands

**Build the SQL project (produces a dacpac and validates SQL syntax):**

```powershell
dotnet build src/BSE.Database/BSE.Database.sqlproj
```

Expected output: 0 errors. 126 pre-existing `SQL71558` case-sensitivity warnings from the legacy SP content — these are runtime-safe under `Latin1_General_CI_AS` collation and must not be fixed until the owning domain slice is implemented.

**Build the full solution (C# projects + SQL project registration check):**

```powershell
dotnet build src/BSE.slnx
```

Expected output: 0 errors, 0 warnings. Note: `dotnet build src/BSE.slnx` reports `BSE.Database is not selected for building in solution configuration "Debug|Any CPU"` — this is expected. The SQL project is registered in the solution but does not participate in the default `dotnet build` configuration because `.slnx` does not yet expose a build-configuration slot for SSDT-style projects. Build the SQL project directly (command above) to validate SP syntax.

**Deploy SPs to a local SQL Server instance:**

Prerequisites:
- SQL Server 2019+ (or SQL Server LocalDB) with an empty `BSE` database
- `SqlPackage` CLI installed: `dotnet tool install -g Microsoft.SqlPackage`

```powershell
# 1. Build the dacpac
dotnet build src/BSE.Database/BSE.Database.sqlproj -c Release -o dacpac-out

# 2. Deploy to local SQL Server (review diff before applying)
SqlPackage /Action:Publish `
    /SourceFile:dacpac-out/BSE.Database.dacpac `
    /TargetServerName:"(localdb)\MSSQLLocalDB" `
    /TargetDatabaseName:BSE

# Or generate a preview script without deploying:
SqlPackage /Action:Script `
    /SourceFile:dacpac-out/BSE.Database.dacpac `
    /TargetServerName:"(localdb)\MSSQLLocalDB" `
    /TargetDatabaseName:BSE `
    /OutputPath:preview.sql
```

### 9.4 Constraints

- **Do NOT modify SP content** — files must remain verbatim copies of the legacy source until the owning slice is implemented (see `SP-Inventory.md` for ownership).
- **Do NOT delete `legacy/BSEDatabase/`** — it remains the operational source until final cutover.
- The `@@ERROR` → `TRY/CATCH` refactor is deferred to **Slice 8** (CaseManagement).

### 9.5 Rollback

Delete `src/BSE.Database/`. Remove the `<Project Path="BSE.Database/BSE.Database.sqlproj" />` line from `src/BSE.slnx`. No code changes to revert. `legacy/BSEDatabase/` is completely untouched.
