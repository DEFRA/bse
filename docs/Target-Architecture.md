# BSE System — Target Architecture

> **Classification:** Programme Governance — Confidential  
> **Date:** 2026-07-20  
> **Author:** Modernise-to-Modular-Monolith Agent  
> **Status:** Approved for Implementation Planning — Awaiting Human Sign-off  
> **Disclaimer:** This document contains proprietary and confidential information belonging to Crown Commercial Property. Unauthorised use, disclosure, or distribution is prohibited.

---

## 1. Guiding Constraints

| Constraint | Value |
|------------|-------|
| Language | C# 14 |
| Runtime | .NET 10 (LTS) |
| Solution format | `.slnx` |
| Architecture pattern | Modular monolith — each module is its own C# project, deployed as a single unit |
| Deployment | Single Linux container (Docker / OCI-compatible runtime) |
| Data access | Dapper thin wrapper over `Microsoft.Data.SqlClient`; all database calls via named stored procedures (Option A) |
| Authentication | OIDC via `Microsoft.AspNetCore.Authentication.OpenIdConnect` replacing Windows Integrated Authentication |
| Authorisation | ASP.NET Core `AuthorizationPolicy` replacing per-page string comparisons |
| Session state | `IDistributedCache` backed by Redis replacing ASP.NET InProc session |
| Logging | Structured logging (`Serilog`) replacing Windows Event Log |
| Behaviour | Must be preserved throughout migration |

---

## 2. Solution Structure

```
BSE.slnx
├── src/
│   ├── BSE.Host/                      # Entry point: Program.cs, DI composition root, middleware pipeline
│   ├── BSE.Infrastructure/            # Shared data access: IDbConnectionFactory, IDapperRepository, SqlConnectionFactory
│   ├── BSE.SharedKernel/              # Value types: RbseNumber, CphhNumber, DbseNumber; formatting utilities; shared constants
│   ├── BSE.Modules.ReferenceData/     # All lu* lookup CRUD; geo lookups; editable lookup administration
│   ├── BSE.Modules.UserManagement/    # User, luUserGroup; OIDC sub/UPN → database User mapping; group claims
│   ├── BSE.Modules.AuditLog/          # AuditLog reads (write-only path remains in stored procedures)
│   ├── BSE.Modules.FarmManagement/    # Farm, FarmRelation, HerdSize; CPHH change cascade
│   ├── BSE.Modules.Search/            # Case/Farm/CPHH/Eartag/Outstanding search (read-only)
│   ├── BSE.Modules.Batch/             # Batch, lnkBatchCase; batch number assignment and linking
│   ├── BSE.Modules.CaseManagement/    # Case core + all child tables: CaseClinical, CaseBAB, CaseFeed,
│   │                                  #   CaseTest, OtherOwner, Pedigree/DamSire; transactional save;
│   │                                  #   FinalResult; RBSE change; NonGB case creation
│   ├── BSE.Modules.AnimalRelations/   # CaseRelation read/query; PickSireDam; relation search
│   ├── BSE.Modules.CaseWork/          # CaseWork minutes; minute sent date; casework entry
│   ├── BSE.Modules.AdnsExport/        # ADNS regulatory export — GB, NI, CI variants; SMTP dispatch;
│   │                                  #   sequential ADNS reference number assignment
│   ├── BSE.Modules.OssExport/         # OSS export staging; exp* table population; OSS batch reports
│   └── BSE.Modules.BsessIntegration/  # BSESS cross-check reads; .NET-hosted ETL job replacing SSIS
├── tests/
│   ├── BSE.Infrastructure.Tests/
│   ├── BSE.Modules.ReferenceData.Tests/
│   ├── BSE.Modules.UserManagement.Tests/
│   ├── BSE.Modules.AuditLog.Tests/
│   ├── BSE.Modules.FarmManagement.Tests/
│   ├── BSE.Modules.Search.Tests/
│   ├── BSE.Modules.Batch.Tests/
│   ├── BSE.Modules.CaseManagement.Tests/
│   ├── BSE.Modules.AnimalRelations.Tests/
│   ├── BSE.Modules.CaseWork.Tests/
│   ├── BSE.Modules.AdnsExport.Tests/
│   ├── BSE.Modules.OssExport.Tests/
│   └── BSE.Modules.BsessIntegration.Tests/
└── database/
    └── BSE.Database/                  # SQL Server project: all ~230 stored procedures, tables, views, functions
                                       # (migrated from legacy/BSEDatabase/ in Slice 1)
```

**Project count:** 15 source projects, 13 test projects, 1 database project = **29 projects total**.

---

## 3. Module Catalogue

### 3.1 Module Dependency Graph

Module dependencies are acyclic. Lower modules must not reference higher modules.

```
BSE.SharedKernel                (no project dependencies)
BSE.Infrastructure              → SharedKernel
BSE.Modules.ReferenceData       → Infrastructure, SharedKernel
BSE.Modules.UserManagement      → Infrastructure, SharedKernel
BSE.Modules.AuditLog            → Infrastructure, SharedKernel
BSE.Modules.FarmManagement      → Infrastructure, SharedKernel, ReferenceData
BSE.Modules.Search              → Infrastructure, SharedKernel, ReferenceData
BSE.Modules.Batch               → Infrastructure, SharedKernel
BSE.Modules.CaseManagement      → Infrastructure, SharedKernel, ReferenceData, FarmManagement, Batch
BSE.Modules.AnimalRelations     → Infrastructure, SharedKernel
BSE.Modules.CaseWork            → Infrastructure, SharedKernel
BSE.Modules.AdnsExport          → Infrastructure, SharedKernel, ReferenceData, CaseManagement
BSE.Modules.OssExport           → Infrastructure, SharedKernel, CaseManagement, FarmManagement, Batch
BSE.Modules.BsessIntegration    → Infrastructure, SharedKernel
BSE.Host                        → all modules (composition root only)
```

**Rule:** No module may take a dependency on `BSE.Host`. Modules register their own services via an extension method (e.g., `AddReferenceDataModule(this IServiceCollection)`), called from `BSE.Host/Program.cs`.

---

### 3.2 Module Responsibilities

| Module | Owns | Responsibilities | Legacy Equivalent |
|--------|------|-----------------|-------------------|
| `BSE.Host` | — | Compose DI container; configure middleware pipeline (auth, session, health, logging); Razor Pages routing root | `BSESystem/Global.asax`, `Web.config` |
| `BSE.Infrastructure` | — | `IDbConnectionFactory`, `IDapperRepository`, `SqlConnectionFactory`; connection string resolution from env vars | `DataAccessLib/clsDataAccess.vb`, `TBCultureDA.vb` |
| `BSE.SharedKernel` | — | `RbseNumber` value type; `CphhNumber` value type; `DbseNumber` value type; `FormatRbse()`, `FormatCphh()`, `FormatDbse()`; `LookupTableId` enum; `UserGroup` enum | `BSESystem/Common.vb` (format methods), `SessionVars.vb` (constants) |
| `BSE.Modules.ReferenceData` | `lu*` tables | CRUD for all 20+ lookup tables; geo lookups (county/parish → OS map ref); read-only lookup data for all other modules; editable lookup administration UI service | `BSELib/LookupData.vb`; `BSESystem/Common.vb` (LOOKUP_* constants) |
| `BSE.Modules.UserManagement` | `User`, `luUserGroup` | Resolve OIDC identity (`sub`/UPN) to internal `User` record; resolve group membership; expose `IUserContext` for use by other modules | `BSELib/clsUser.vb` |
| `BSE.Modules.AuditLog` | `AuditLog` (reads) | Read audit log by case, date, farm, user, and by change type (moves, CPHH changes, new farms, RBSE changes). Write path remains in stored procedures. | `BSELib/clsAuditLog.vb` |
| `BSE.Modules.FarmManagement` | `Farm`, `FarmRelation`, `HerdSize`, `FarmHistorical` | Farm CRUD; CPHH identity change (cascade); related farm management; herd size by year; farm existence check; confirmed case count | `BSELib/clsFarm.vb` |
| `BSE.Modules.Search` | reads `Case`, `Farm` | Case search (17-param); farm search (8-param); CPHH search; eartag/herdmark search; outstanding BSE1/fate/result searches; related animal search | `BSELib/clsSearch.vb` |
| `BSE.Modules.Batch` | `Batch`, `lnkBatchCase` | Create batch numbers (standard and 1989-scheme); link batches to cases; resolve batch year+sequence to BatchID; retrieve latest batch numbers for dashboard | `BSELib/clsBatch.vb` |
| `BSE.Modules.CaseManagement` | `Case`, `CaseClinical`, `ClinicalVisit`, `CaseBAB`, `CaseFeed`, `CaseTest`, `OtherOwner`, `Pedigree`, `CaseHistorical` | Full case lifecycle: create, edit, delete, move, change RBSE, NonGB creation; clinical, BAB, feed, test, other owner, pedigree child records; final result entry; all within a single `SqlTransaction` on save | `BSELib/clsCase.vb` |
| `BSE.Modules.AnimalRelations` | `CaseRelation` (reads) | Query relations for a case; pick sire/dam; dam/sire match lookup; related case relation details. Write path (Add/Edit/Delete CaseRelation) is part of the CaseManagement transactional save. | `BSELib/clsRelations.vb` |
| `BSE.Modules.CaseWork` | `CaseWork` | Casework minute management; minute sent date update; casework entry retrieval | `clsCase.vb` (embedded casework methods) |
| `BSE.Modules.AdnsExport` | writes `Case.ADNSReferenceYear`, `Case.ADNSReferenceNumber`, `Case.EmailSentToADNSDate` | GB/NI/CI ADNS export; sequential ADNS reference number assignment; SMTP email dispatch; last reference lookup | `BSELib/clsADNSReport.vb` |
| `BSE.Modules.OssExport` | `expCase`, `expFarm`, `expRelation`, `expAge` | OSS export staging table population; OSS batch report generation; 1989-scheme batch creation | `BSELib/clsOSSExport.vb` |
| `BSE.Modules.BsessIntegration` | `BSESSImport` | BSESS cross-check reads (by date, by RBSE); .NET-hosted `IHostedService` ETL replacing SSIS `BSESS Import.dtsx` | `BSELib/clsBSESS.vb` |

---

## 4. Data Access Strategy — Option A: Retain Stored Procedures with Dapper

**Decision:** The modernised system retains all ~230 existing stored procedures as the exclusive data access mechanism. No ORM (Entity Framework Core) is introduced. Application code calls stored procedures via a Dapper thin wrapper. This preserves ADR-001 intent and avoids the risk and effort of rewriting business logic embedded in stored procedures.

See also: [ADR-005](ADR/ADR-005.md).

---

### 4.1 Infrastructure Abstractions

All data access flows through two shared interfaces defined in `BSE.Infrastructure`:

```
IDbConnectionFactory
  └── CreateConnection() → SqlConnection   // one connection per unit of work

IDapperRepository
  └── QueryAsync<T>(spName, param)         // replaces FillDataTable
  └── QuerySingleOrDefaultAsync<T>(...)    // single-row result
  └── QueryMultipleAsync(spName, param)    // replaces FillDataSet (multi-result SPs)
  └── ExecuteAsync(spName, param)          // replaces ExecuteNonQuery
  └── ExecuteWithOutputAsync(spName, param) // replaces ExecuteQuery (OUTPUT params)
  └── ExecuteInTransactionAsync(work)      // explicit transaction scope
```

All methods set `CommandType.StoredProcedure`. No inline SQL is permitted in application code.

**Connection string** is injected from environment variable `BSE_CONNECTION_STRING` (not `Web.config`). On application start, `SqlConnectionFactory` validates the connection string is non-null and throws `InvalidOperationException` if absent — fail-fast at startup.

---

### 4.2 Dapper Wrapper Patterns

#### Standard read — replaces `FillDataTable`

```csharp
// Usage in a module repository
var results = await _db.QueryAsync<CaseSearchResult>("GetSearchCase", new
{
    RBSE           = rbse,
    Eartag         = eartag,
    FinalResult    = finalResult,
    // ...17 params
});
```

#### Multi-result read — replaces `FillDataSet`

Used exclusively for `GetCaseDetailsByRBSE` which returns 11 result sets.

```csharp
await using var multi = await _db.QueryMultipleAsync("GetCaseDetailsByRBSE", new { RBSE = rbse });
var caseRecord      = await multi.ReadFirstOrDefaultAsync<CaseRecord>();
var clinical        = await multi.ReadAsync<ClinicalRecord>();
var bab             = await multi.ReadFirstOrDefaultAsync<BabRecord>();
var otherOwners     = await multi.ReadAsync<OtherOwnerRecord>();
// ... remaining 7 result sets in declared order
```

Named C# record types replace all `DataSet.Tables[n]` integer-index access. The mapping is enforced by the declared read order in the multi-result call, and is documented as a code comment tied to the SP's `SELECT` statement order.

#### Stored procedure with OUTPUT parameters — replaces `ExecuteQuery`

Used for `AddBatchNumber` (returns `BatchID`, `BatchYear`, `BatchNumber` as OUTPUT params) and similar.

```csharp
var p = new DynamicParameters();
p.Add("BatchYear",   dbType: DbType.Int32,  direction: ParameterDirection.Output);
p.Add("BatchNumber", dbType: DbType.Int32,  direction: ParameterDirection.Output);
p.Add("BatchID",     dbType: DbType.Int32,  direction: ParameterDirection.Output);
await _db.ExecuteWithOutputAsync("AddBatchNumber", p);
var batchId = p.Get<int>("BatchID");
```

#### Explicit transaction scope — fixes the case save partial-update defect

This is a mandatory engineering obligation for `CaseManagement.UpdateCaseDetailsAsync()`. All child stored procedure calls (AddCase/EditCase + 7+ child SPs) execute within a single `SqlTransaction`. A failure at any point rolls back the entire set.

```csharp
await _db.ExecuteInTransactionAsync(async (conn, tx) =>
{
    await conn.ExecuteAsync("AddCase",             caseParams,     tx, commandType: CommandType.StoredProcedure);
    await conn.ExecuteAsync("AddCaseClinical",     clinicalParams, tx, commandType: CommandType.StoredProcedure);
    await conn.ExecuteAsync("AddCaseBAB",          babParams,      tx, commandType: CommandType.StoredProcedure);
    await conn.ExecuteAsync("AddCaseFeed",         feedParams,     tx, commandType: CommandType.StoredProcedure);
    await conn.ExecuteAsync("AddCaseRelation",     relationParams, tx, commandType: CommandType.StoredProcedure);
    await conn.ExecuteAsync("AddTest",             testParams,     tx, commandType: CommandType.StoredProcedure);
    await conn.ExecuteAsync("AddCaseWork",         caseworkParams, tx, commandType: CommandType.StoredProcedure);
    // BatchNumberLink call also included in same transaction if BatchID > 0
});
```

---

### 4.3 Stored Procedure Version Control

- All ~230 stored procedure `.sql` files are sourced from `legacy/BSEDatabase/dbo/Stored Procedures/` and brought into `database/BSE.Database/` (SQL Server project) in **Slice 1** of the migration plan.
- The SQL Server project format (`.sqlproj` / dacpac) is preserved; deployment uses `SqlPackage.exe` in CI.
- No stored procedure source file may be modified without a PR that includes both the `.sql` change and the corresponding application code change that consumes it.
- PR template for SP changes must include: affected domain module, result set ordering if changed, downstream impact assessment.
- The `@@ERROR`-based error handling in high-risk SPs (`ChangeRBSE`, `MoveCase`, `AddCase`) must be refactored to `BEGIN TRY / BEGIN CATCH / ROLLBACK TRANSACTION` as an explicit Phase 2 deliverable (see Migration Plan Slice 8).

---

### 4.4 Replacing the Legacy `TBCultureDA` / `DataAccess` Pattern

| Legacy pattern | Modernised equivalent |
|----------------|----------------------|
| `TBCultureDA.FillDataTable("SP", params)` | `IDapperRepository.QueryAsync<T>("SP", params)` |
| `TBCultureDA.FillDataSet("SP", params)` | `IDapperRepository.QueryMultipleAsync("SP", params)` |
| `TBCultureDA.ExecuteNonQuery("SP", params)` | `IDapperRepository.ExecuteAsync("SP", params)` |
| `TBCultureDA.ExecuteQuery("SP", params)` | `IDapperRepository.ExecuteWithOutputAsync("SP", dynParams)` |
| `InfoLog.LogToEventViewer(ex)` | `ILogger<T>.LogError(ex, ...)` → stdout JSON → Application Insights |
| `DataSet.Tables[n].Rows[0]["ColumnName"]` | Named C# record property (e.g., `caseRecord.Rbse`) |
| Static shared class (no DI) | Constructor-injected `IDapperRepository` |

---

## 5. Container Deployment Model

The modernised BSE system deploys as a **single Linux container** running the `BSE.Host` ASP.NET Core application. There is no microservice split at deployment time — the modular structure is an internal code boundary only.

### 5.1 Container Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     BSE Container (Linux, .NET 10)                          │
│                                                                             │
│   BSE.Host (ASP.NET Core)                                                   │
│   ├── BSE.Modules.ReferenceData                                             │
│   ├── BSE.Modules.UserManagement                                            │
│   ├── BSE.Modules.AuditLog                                                  │
│   ├── BSE.Modules.FarmManagement                                            │
│   ├── BSE.Modules.Search                                                    │
│   ├── BSE.Modules.Batch                                                     │
│   ├── BSE.Modules.CaseManagement                                            │
│   ├── BSE.Modules.AnimalRelations                                           │
│   ├── BSE.Modules.CaseWork                                                  │
│   ├── BSE.Modules.AdnsExport                                                │
│   ├── BSE.Modules.OssExport                                                 │
│   └── BSE.Modules.BsessIntegration (includes .NET ETL hosted service)      │
│                                                                             │
│   Listens: 8080 (HTTP), 8443 (HTTPS)                                        │
│   Health:  GET /health/live, GET /health/ready                              │
└─────────────────────────┬───────────────────────────────────────────────────┘
                          │ TCP 1433
         ┌────────────────▼────────────────┐
         │  SQL Server (existing, external)  │
         │  Database: BSE                    │
         └───────────────────────────────────┘
                          │
         ┌────────────────▼────────────────┐
         │  Redis (distributed session)      │
         │  Standalone or Azure Cache        │
         └───────────────────────────────────┘
                          │
         ┌────────────────▼────────────────┐
         │  SMTP relay (ADNS export)         │
         │  Configured via env var           │
         └───────────────────────────────────┘
```

### 5.2 Environment Variables

All configuration previously in `Web.config` moves to environment variables injected at container start:

| Variable | Legacy equivalent | Description |
|----------|------------------|-------------|
| `BSE_CONNECTION_STRING` | `DBConnectionString` in `appSettings` | SQL Server connection string (no plaintext password in source — use secret injection) |
| `OIDC__Authority` | N/A (new) | OIDC provider authority URL (e.g., Azure AD tenant) |
| `OIDC__ClientId` | N/A (new) | OIDC client ID |
| `OIDC__ClientSecret` | N/A (new) | OIDC client secret (injected via secret store) |
| `Redis__ConnectionString` | N/A (new) | Redis connection string for distributed session |
| `SMTP__Host` | `SMTPHost` | SMTP relay hostname |
| `SMTP__Port` | `SMTPPort` | SMTP port (default: 25) |
| `ADNS__EmailToAddress` | `ADNSEmailToAddress` | EU ADNS recipient (`SANTE-ADNS@ec.europa.eu`) |
| `ADNS__EmailFromAddress` | `ADNSEmailFromAddress` | APHA sender address |
| `SharePoint__SiteUrl` | `SPOLSite` | SharePoint document library URL |
| `BSE_ENVIRONMENT` | N/A | `Development` / `Staging` / `Production` |

**Secret management:** `BSE_CONNECTION_STRING`, `OIDC__ClientSecret`, and `Redis__ConnectionString` must never be set in source control. They are injected via container secret injection (Kubernetes Secrets, Azure Key Vault, or equivalent) at runtime.

### 5.3 Dockerfile Outline

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080 8443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BSE.slnx .
COPY src/ src/
RUN dotnet restore
RUN dotnet publish src/BSE.Host/BSE.Host.csproj -c Release -o /app/publish

FROM runtime AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BSE.Host.dll"]
```

Non-root user (`USER app`) must be set before `ENTRYPOINT` to satisfy container security policy.

### 5.4 Scaling Constraints

- The single-container deployment is horizontally scalable once the session state migration (Slice 14) is complete.
- Redis-backed `IDistributedCache` replaces InProc session, removing the single-server constraint documented in ADR-002.
- Before Slice 14 is delivered, the container must be deployed as a single replica only.

---

## 6. Authentication Strategy — OIDC Replacing Windows Integrated Authentication

See also: [ADR-006](ADR/ADR-006.md).

### 6.1 Replacement Approach

| Concern | Legacy approach | Target approach |
|---------|----------------|-----------------|
| Authentication protocol | Windows NTLM/Kerberos at IIS | OIDC (OAuth 2.0 + OpenID Connect) via `Microsoft.AspNetCore.Authentication.OpenIdConnect` |
| Identity provider | Windows Active Directory (implicit) | Azure Active Directory (Entra ID) — same underlying directory |
| User identifier | `WindowsIdentity.Name` (DOMAIN\username) → `NTLogin` in `User` table | OIDC `upn` claim (UserPrincipalName) mapped to `User.NTLogin` column |
| Group resolution | `clsUser.GetGroupForUser(ntLogin)` → `luUserGroup` | `IUserContext.ResolveGroupAsync(upn)` → same `GetGroupForUser` SP |
| Role enforcement | Per-page `EnableControls()` string comparison | `[Authorize(Policy = "...")]` on every Razor Page; default-deny if attribute absent |

### 6.2 User Identity Continuity

The existing `User.NTLogin` column (VARCHAR 25) stores Windows NT login names in `DOMAIN\username` format. The OIDC token will carry the `preferred_username` or `upn` claim, which will be in UPN format (`user@domain.gov.uk`).

**Transition strategy:**
1. The `User` table is supplemented with a new `UPN` column (VARCHAR 100, nullable) in a preparatory database migration.
2. `UserManagement` module resolves identity first by UPN, then falls back to NTLogin for existing records not yet updated.
3. During Slice 3 (UserManagement migration), a one-off data migration script is provided to populate `UPN` from existing `NTLogin` records by mapping through AD.
4. Once all records have UPN populated, the NTLogin fallback is removed (Phase 4 clean-up).

### 6.3 Authorisation Policies

Five named policies replace the legacy per-page group-name string comparison:

| Policy name | Allowed groups | Legacy string literals |
|-------------|---------------|----------------------|
| `BseViewer` | DEFRA Viewer, VLA Data Entry, VLA Maintenance, DEFRA Data Entry, DEFRA Maintenance | N/A — all authenticated users previously had implicit read access |
| `BseDataEntry` | DEFRA Data Entry, VLA Data Entry, VLA Maintenance, DEFRA Maintenance | Various `EnableControls()` checks |
| `BseMaintenance` | DEFRA Maintenance, VLA Maintenance | `"DEFRA Maintenance"`, `"VLA Maintenance"` |
| `BseVlaMaintenance` | VLA Maintenance | `"VLA Maintenance"` |
| `BseDefraMaintenance` | DEFRA Maintenance | `"DEFRA Maintenance"` |

Policies are registered in `BSE.Host/Program.cs`. Every Razor Page has an explicit `[Authorize(Policy = "...")]` attribute. A Roslyn analyser rule enforces that no Razor Page is missing an `[Authorize]` attribute — this runs in CI and blocks merge.

---

## 7. State Management Strategy — Replacing InProc Session

### 7.1 Current State Problem

The legacy system stores entire `DataSet` objects (11-table case dataset, 3-table farm dataset, full `clsADNSReport` instance) in ASP.NET InProc session (ADR-002). This creates a hard single-server constraint.

### 7.2 Target State

| Session concern | Legacy pattern | Target pattern |
|----------------|----------------|----------------|
| Transport | `HttpSessionState` (InProc) | `IDistributedCache` (Redis-backed) |
| Case wizard state | 11-table `DataSet` in session | Typed `CaseWizardState` record serialised as JSON in distributed cache, keyed by `{userId}:case:{rbse}` |
| Farm wizard state | 3-table `DataSet` in session | Typed `FarmWizardState` record in distributed cache |
| User identity | `SV_HeaderUserID`, `SV_HeaderGroupName` in session | ASP.NET Core `HttpContext.User` claims (no session needed) |
| ADNS export object | `clsADNSReport` instance in session | Stateless ADNS service; intermediate state held server-side keyed by user session ID |
| Batch number display | `SV_BatchID`, `SV_BatchNumber` in session | Razor Page model populated from database per-request |

### 7.3 Fallback Behaviour

If Redis is unavailable:
- Circuit breaker activates; application falls back to `MemoryCache`-backed `IDistributedCache`.
- When memory-backed, horizontal scaling is disabled (single replica only); this is logged as a warning.
- The `/health/ready` endpoint reflects Redis unavailability as a degraded (not failed) status.

### 7.4 Session Key Contract Migration

Session key string constants from `SessionVars.vb` are not carried forward verbatim. They are replaced by strongly-typed cache key methods on a `ICacheKeyProvider` service:

```
ICacheKeyProvider
  └── CaseWizardKey(userId, rbse)    → "{userId}:case-wizard:{rbse}"
  └── FarmWizardKey(userId, cphh)    → "{userId}:farm-wizard:{cphh}"
  └── AdnsExportKey(userId)          → "{userId}:adns-export"
```

Compile-time type safety replaces the 40+ plain-string session key constants.

---

## 8. Observability

### 8.1 Health Endpoints

Registered via `Microsoft.Extensions.Diagnostics.HealthChecks`:

| Endpoint | Purpose | Checks |
|----------|---------|--------|
| `GET /health/live` | Liveness — container is running | Always returns 200 |
| `GET /health/ready` | Readiness — can serve traffic | SQL Server ping; Redis ping |

Health check results are exposed as JSON and are the basis for container orchestrator restart/drain decisions.

### 8.2 Structured Logging

- **Provider:** Serilog, writing to stdout as newline-delimited JSON.
- **Replaces:** `InfoLog.LogToEventViewer()` (Windows Event Log writes removed entirely).
- **Minimum fields per log event:** `Timestamp`, `Level`, `MessageTemplate`, `Properties` (including `RequestId`, `UserId`, `Rbse` where applicable), `Exception` (if error).
- **Sink targets:** stdout (always); Application Insights or OTLP exporter (when `BSE_ENVIRONMENT=Production`).
- **Sensitive data:** `RBSE` and `CPHH` are logged as identifiers; no personal data (owner name, address) is written to logs.

### 8.3 Request Correlation

Each HTTP request receives a `X-Correlation-ID` header (generated if absent). All log events and downstream SP calls within a request carry the correlation ID, enabling end-to-end tracing across the application and SQL Server's extended events.

---

## 9. Cross-Cutting Concerns

### 9.1 Authorisation (centralised)

Described in §6.3. Key additional rules:
- All five user groups resolve to named ASP.NET Core roles populated as claims by `UserManagement.IUserContext`.
- Role claims are populated at login from `GetGroupForUser` SP; refreshed on each session start (not per-request).
- No page may perform a runtime group-name string comparison. Any such code detected by code review is a blocking defect.

### 9.2 Input Validation

- RBSE format: validated against `[0-9][0-9][0123456789X][0123456789X][0-9]{5}` regex at the `RbseNumber` value type constructor; invalid values throw `ArgumentException` before reaching the data layer.
- CPHH format: validated against `[0-9]{11}` regex at the `CphhNumber` value type constructor.
- All SP parameters are passed as typed C# values (never string-concatenated). Dapper's parameterised query mechanism prevents SQL injection at the data layer.

### 9.3 Exception Handling

- A global `IMiddleware` exception handler (`BseExceptionHandlerMiddleware`) catches unhandled exceptions, logs them via `ILogger`, and returns a structured error response.
- No stack traces are returned to the browser in production (`BSE_ENVIRONMENT=Production`).
- The legacy `Application_Error` / `clsAppError.DisplayError()` pattern is removed.

### 9.4 Dependency Injection Rules

- No `static` classes except `const`-only types and `enum`.
- All dependencies injected via constructor; no service-locator pattern.
- `IDapperRepository` is registered as `Scoped` (one instance per HTTP request).
- `IDbConnectionFactory` is registered as `Singleton`.

---

## 10. Deployment Diagram (Textual)

```
Internet / APHA network
         │
         ▼
  ┌─────────────┐
  │  Reverse    │  (nginx or Azure Application Gateway)
  │  Proxy /    │  TLS termination; /health routing
  │  Load Balancer│
  └──────┬──────┘
         │ HTTP/2 (8080)
         ▼
  ┌──────────────────────────────────────────┐
  │         BSE Container (Linux .NET 10)    │
  │         Replicas: 1 (pre-Slice 14)       │
  │                   N (post-Slice 14)      │
  └──────┬───────────────────────────────────┘
         │                   │                     │
         ▼                   ▼                     ▼
  SQL Server             Redis                SMTP Relay
  (BSE database)    (distributed cache)   (ADNS email)
  TCP 1433          TCP 6379              TCP 25
```

---

*Next documents: [Migration-Plan.md](Migration-Plan.md) | [ADR-005](ADR/ADR-005.md) | [ADR-006](ADR/ADR-006.md)*
