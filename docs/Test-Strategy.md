# BSE System — Test Strategy

> **Classification:** Programme Governance — Confidential  
> **Date:** 2026-07-20  
> **Author:** Testing Agent  
> **Status:** Baseline — Ready for Implementation Agent  
> **Disclaimer:** This document contains proprietary and confidential information belonging to Crown Commercial Property. Unauthorised use, disclosure, or distribution is prohibited.

---

## 1. Purpose and Scope

This document defines the test strategy for the BSE modular monolith migration. Its primary goal is to establish a safety net that captures and protects the existing system's behaviour before any code migration begins. Every test defined here must pass against the migrated C# code to confirm behavioural parity.

The strategy covers:
- Which test types are used and when
- Which critical flows are protected and how
- How the legacy codebase can be characterised before the new solution exists
- Tooling decisions and test framework configuration
- CI integration approach
- Special requirements for the three highest-risk slices

**Out of scope:** UI (Razor Pages) testing. End-to-end browser tests are deferred to the UI implementation phase. This strategy targets API/service-level and unit-level tests only.

---

## 2. Test Pyramid

The BSE migration uses a modified test pyramid suited to a Dapper-based modular monolith migrated from VB.NET WebForms with no existing test suite.

```
                    ┌─────────────────────┐
                    │   Characterisation   │  ← captures legacy SP output contracts
                    │     Tests (E2E-ish)  │     as baselines; golden-file comparison
                    └──────────┬──────────┘
               ┌───────────────┴───────────────┐
               │    Integration Tests (SQL)     │  ← Testcontainers SQL Server
               │  Slice-level happy + sad paths │     real SP execution, real schema
               └──────────────┬────────────────┘
        ┌─────────────────────┴─────────────────────┐
        │              Unit Tests                    │  ← pure logic, no DB
        │  Value types, formatters, validators,      │     xUnit + Shouldly
        │  mandatory-field rules, eartag parsing     │
        └────────────────────────────────────────────┘
```

### Rationale

- **Unit tests** are viable immediately against the pure-logic that can be extracted from the legacy codebase (see Section 4 and `Test-Baseline-Notes.md`). They require no Slice 0 scaffolding.
- **Integration tests** require a real SQL Server instance with the BSE schema and stored procedures deployed. They become executable once Slice 1 (SP source control) and a Testcontainers-compatible CI environment are available.
- **Characterisation tests** record the actual output of legacy stored procedures against a reference dataset before migration begins. These golden-file outputs are the contract baseline that migrated code must reproduce.

---

## 3. Tooling Decisions

| Concern | Chosen Tool | Justification |
|---------|-------------|---------------|
| Test framework | **xUnit 2.x** (latest stable for .NET 10) | Industry standard for .NET; native async support; no static state between tests |
| Assertion library | **Shouldly** | Human-readable failure messages without ceremony; `ShouldBe`, `ShouldContain`, `ShouldThrow` cover all assertion patterns needed |
| Dapper mock / repository abstraction | **`IDapperRepository` interface mock via NSubstitute** | The `IDapperRepository` interface (defined in `BSE.Infrastructure`) is the seam. `NSubstitute` is preferred over Moq for conciseness with async stubs |
| Integration DB | **Testcontainers for .NET (`Testcontainers.MsSql`)** | Spins up a real SQL Server 2022 container per test session; schema applied via `SqlPackage` against `BSE.Database` project output; no shared state between runs |
| Golden-file comparison | **`Verify` library (`Verify.Xunit`)** | Captures serialised output as `.verified.txt` files committed to source; snapshot comparison on re-run |
| Coverage | **Coverlet** (`coverlet.collector`) | Integrates with `dotnet test --collect "XPlat Code Coverage"` |
| Coverage reporting | **ReportGenerator** | Converts Coverlet XML to HTML; uploaded as CI artifact |

### Test project layout (created in Slice 0)

```
tests/
├── BSE.Infrastructure.Tests/            # IDapperRepository integration tests; SqlConnectionFactory config
├── BSE.SharedKernel.Tests/              # RbseNumber, CphhNumber, FormatRbse, FormatCphh value type tests
├── BSE.Modules.ReferenceData.Tests/
├── BSE.Modules.UserManagement.Tests/    # GetGroupForUser; UPN mapping; fallback logic
├── BSE.Modules.AuditLog.Tests/
├── BSE.Modules.FarmManagement.Tests/
├── BSE.Modules.Search.Tests/
├── BSE.Modules.Batch.Tests/
├── BSE.Modules.CaseManagement.Tests/   # Mandatory-field validation; transactional rollback
├── BSE.Modules.AnimalRelations.Tests/
├── BSE.Modules.CaseWork.Tests/
├── BSE.Modules.AdnsExport.Tests/       # Reference assignment; byte-identical output comparison
├── BSE.Modules.OssExport.Tests/
└── BSE.Modules.BsessIntegration.Tests/
```

Each test project is scaffolded in Slice 0 with a single `[Fact] void Placeholder_ReturnsTrue() => true.ShouldBe(true)` to keep CI green before domain tests are added.

---

## 4. Characterisation Test Approach

### Purpose

A characterisation test records the **current behaviour** of the legacy system as a golden file, without asserting anything about correctness. Once the migrated C# module is implemented, the same input is replayed against the new code and the output is compared to the golden file. Divergence is a regression.

### Execution model

1. **Before Slice 1 (now):** Connect to the existing SQL Server database (`vm-aphadev-003`), execute each critical SP with documented inputs, serialise the result (JSON or CSV), and commit the files to `tests/characterisation/golden/`.
2. **During migration (per slice):** The corresponding integration test replays the same SP call via the migrated Dapper repository and uses `Verify` to compare against the golden file.
3. **Pass criterion:** `Verify` reports zero differences. Any intentional change (e.g., a column renamed) requires the golden file to be explicitly approved and updated.

### Golden file naming convention

```
tests/characterisation/golden/
  GetCaseDetailsByRBSE_{RBSE}.json
  GetSearchCase_ByEartag_{Eartag}.json
  GetSearchFarm_ByCPHH_{CPHH}.json
  GetADNSCasesForGB_{Year}.json
  GetAuditLogByCase_{RBSE}.json
  AddBatchNumber_Session_{TestId}.json
  GetGroupForUser_{NTLogin}.json
```

### Reference dataset

A minimum of 5 RBSE values from the production database shall be selected (one per case type: GB new, GB edit, GB positive final result, NI, NonGB) and recorded as golden files before Slice 2 work begins. Selection is performed by the APHA SME.

---

## 5. Critical Flows — Test Coverage Map

| # | Flow | Test Type | Module | SP(s) Exercised | Pass Criterion |
|---|------|-----------|--------|----------------|----------------|
| F1 | Case search by RBSE and eartag | Integration | `BSE.Modules.Search` | `GetSearchCase` | Returns same rows as golden file for same inputs |
| F2 | Farm search by CPHH, name, county | Integration | `BSE.Modules.Search` | `GetSearchFarm` | Returns same rows as golden file |
| F3 | Load full case details (11-table DataSet → typed records) | Integration + Characterisation | `BSE.Modules.CaseManagement` | `GetCaseDetailsByRBSE` | All 11 result sets map to named C# records without data loss |
| F4 | Case mandatory-field validation (new case) | Unit | `BSE.Modules.CaseManagement` | none | `CheckMandatoryFields`-equivalent rejects missing CPHH, Eartag, FormADate; accepts valid input |
| F5 | Case save — new case (AddCase) | Integration | `BSE.Modules.CaseManagement` | `AddCase` | Row appears in `Case` table; `AuditLog` row written; no partial state on failure |
| F6 | Case save — edit with optimistic concurrency (EditCase) | Integration | `BSE.Modules.CaseManagement` | `EditCase` | RowStamp mismatch returns error code 3; committed save updates `Case` table |
| F7 | Case transactional rollback on mid-save fault | Integration (injected fault) | `BSE.Modules.CaseManagement` | `AddCase` + child SPs | See §7.1 — Slice 8 special requirement |
| F8 | Batch number creation and case link | Integration | `BSE.Modules.Batch` | `AddBatchNumber`, `AddBatchNumberLink` | Output params `BatchID`, `BatchYear`, `BatchNumber` populated; link row present |
| F9 | Farm existence check (CPHH) | Integration | `BSE.Modules.FarmManagement` | `GetFarmByCPHH` | Returns `true` for known CPHH; `false` for unknown |
| F10 | Confirmed case count for farm | Integration | `BSE.Modules.FarmManagement` | `GetNumberOfConfirmedCases` | Count matches golden file for reference CPHH |
| F11 | ADNS export — GB reference assignment | Unit + Integration | `BSE.Modules.AdnsExport` | `GetADNSCasesForGB`, `GetLastADNSReferenceByArea` | Sequential reference numbers are monotonically increasing; no duplicates |
| F12 | ADNS export — email body content | Characterisation | `BSE.Modules.AdnsExport` | `GetADNSCasesForGB` | See §7.2 — Slice 11 byte-identical comparison |
| F13 | ADNS case update (EditCaseADNS) | Integration | `BSE.Modules.AdnsExport` | `EditCaseADNS` | `EmailSentToADNSDate`, `ADNSReferenceYear`, `ADNSReferenceNumber` written correctly |
| F14 | Audit log read by case | Integration | `BSE.Modules.AuditLog` | `GetAuditLogByCase` | Result matches golden file for reference RBSE |
| F15 | User group resolution (NT login) | Integration + Unit | `BSE.Modules.UserManagement` | `GetGroupForUser` | Returns correct `UserGroup` enum value; see §7.3 |
| F16 | OIDC UPN → database User mapping | Unit | `BSE.Modules.UserManagement` | none | UPN match logic; NTLogin fallback logic; see §7.3 |
| F17 | Eartag format parsing — UK GB numeric | Unit | `BSE.SharedKernel` | none | `Eartag.GetEartag("UK", herd, animal)` returns correct `FormatId` and `PresentationValue` |
| F18 | Eartag format parsing — NI alphanumeric | Unit | `BSE.SharedKernel` | none | NI eartag parsed to `NIAlphaNumericEartagFormat`; validation returns empty error code |
| F19 | Eartag format parsing — EC country code | Unit | `BSE.SharedKernel` | none | EC eartag parsed to `ECEartag`; non-EC non-UK falls to `NoCountryEartag` |
| F20 | `FormatRbse` / `FormatCphh` / `FormatDbse` | Unit | `BSE.SharedKernel` | none | `"9900001001"` → `"99/00/00100 1"`; `"11111111111"` → `"11/111/1111/1"` |
| F21 | Health endpoint — live and ready | Integration | `BSE.Host` | none | `GET /health/live` → 200; `GET /health/ready` → 200 with SQL Server reachable |

---

## 6. Known Gaps

The following items cannot be tested until the listed dependency is resolved.

| Gap | Reason | Unblocked By |
|-----|--------|--------------|
| All integration tests against real SQL Server | Testcontainers requires the `BSE.Database` sqlproj to produce a deployable dacpac; no `.slnx` exists yet | Slice 0 scaffolding + Slice 1 SP source control |
| Full case save integration test (F5, F6, F7) | `CaseManagement` module not scaffolded | Slice 8 |
| ADNS byte-identical output comparison (F12) | `AdnsExport` module not scaffolded; golden files not yet extracted | Slice 11; reference dataset from APHA SME |
| OIDC UPN mapping (F16) | Azure AD tenant and test user not yet provisioned | Slice 3 + IdP registration |
| Session state continuity tests | Redis not yet introduced; `IDistributedCache` not configured | Slice 14 |
| OSS export characterisation | `OssExport` module not scaffolded; SP staging behaviour not captured | Slice 12 |
| BSESS ETL (`IHostedService`) | SSIS replacement not implemented | Slice 13 |
| `CheckMandatoryFields` integration with farm `IsNonGBFarm` flag | DataSet-dependent; pure unit test requires DataSet construction; meaningful only with module scaffold | Slice 8 |
| Coverage enforcement (≥ 80% branch) | No C# projects to measure | Slice 0 |
| UI tests (Razor Pages) | Out of scope for this strategy; handled by UI implementation agent | After Slice 14 |

---

## 7. Special Test Requirements for High-Risk Slices

### 7.1 Slice 8 — Case Management: Transactional Rollback (Risk R01)

**Risk:** `UpdateCaseDetails` calls 8+ stored procedures. If any fails mid-sequence, partial data could be written to the database. This is the highest-risk slice in the programme.

**Requirement:** An injected-fault integration test must verify that a failure after `AddCase` but before `AddCaseClinical` (or any other child SP) causes a full rollback of the `Case` table insert.

**Test design:**

```csharp
// Test: CaseManagement_Save_RollsBack_WhenChildSpFails
// Arrange: insert a known Farm into the test container; set up a test Case DataSet
// Act: call ICaseRepository.SaveCaseAsync() with a valid CaseRecord but a
//      deliberately bad ClinicalRecord that will cause AddCaseClinical to fail
//      (e.g., a RBSE that does not exist in the Case table at the point AddCaseClinical fires)
// Assert:
//   - the method returns a failure result (not throws)
//   - a direct SQL query to the test DB confirms NO row was inserted into Case
//   - a direct SQL query confirms NO row was inserted into CaseClinical
//   - a direct SQL query confirms NO row was written to AuditLog for this RBSE
```

The test framework for this is Testcontainers with a transaction that is explicitly NOT committed. The injected fault can be a mock that throws on the second SP call while the real `ExecuteInTransactionAsync` wrapper must roll back.

**Go/no-go criterion:** This test must exist, be named `CaseManagement_Save_RollsBack_WhenChildSpFails`, and pass before Slice 8 is approved for merge.

---

### 7.2 Slice 11 — ADNS Export: Byte-Identical Output Comparison (Risk R02)

**Risk:** The ADNS email body is a statutory regulatory document sent to the EU. Any regression in content, format, or reference number sequence constitutes a compliance breach.

**Requirements:**

1. **Golden-file extraction (before Slice 11 begins):** Execute `GetADNSCasesForGB` against the production (or UAT) database with a known date range. Serialise the full email body (subject, all case lines, reference numbers) to `tests/characterisation/golden/ADNSEmail_GB_{Year}.verified.txt`.

2. **Reference number sequence test:** Given a set of N cases returned by `GetADNSCasesForGB`, the reference numbers assigned by the migrated code must be identical to those assigned by `clsADNSReport`'s constructor logic: sequential integers starting at `lastReference + 1`, per-year, per-area.

   ```csharp
   // Test: AdnsExport_AssignsSequentialReferences_StartingAfterLastUsed
   // Given: GetLastADNSReferenceByArea returns year=2026, number=42 for "GB"
   //        GetADNSCasesForGB returns 3 cases
   // Assert: cases receive references 43, 44, 45 in row order
   ```

3. **Email body comparison:** The migrated `IAdnsEmailBuilder.BuildEmailBody(cases, emailRef)` must produce output byte-identical to the golden file when given the same input data.

   ```csharp
   // Test: AdnsExport_EmailBody_MatchesGoldenFile
   // Uses Verify library: await Verify(emailBody).UseDirectory("golden");
   ```

4. **SMTP is NOT called in tests.** The `ISmtpDispatcher` dependency is substituted with `NSubstitute.Substitute.For<ISmtpDispatcher>()`. The test asserts that `DispatchAsync(message)` was called once with the correct `To` address (`SANTE-ADNS@ec.europa.eu`) and that `EditCaseADNS` was called for each case.

**Go/no-go criterion:** The golden file must exist in the repository and the byte-identical comparison test must pass before Slice 11 is approved for merge. APHA SME sign-off on golden file content is required.

---

### 7.3 Slice 3 — User Management / OIDC: UPN Mapping (Risk: Auth continuity)

**Risk:** Moving from Windows NTLM authentication to OIDC means user records mapped by NT login must be re-mapped to UPN without breaking existing user sessions or access control.

**Requirements:**

1. **UPN exact-match test:**

   ```csharp
   // Test: UserManagement_GetGroupForUser_ByUpn_ReturnsCorrectGroup
   // Given: a User row with UPN = "john.smith@defra.gov.uk"
   //        an OIDC token with claim upn = "john.smith@defra.gov.uk"
   // Assert: GetGroupForUser returns UserGroup.VlaDataEntry (or appropriate group)
   ```

2. **NTLogin fallback test:**

   ```csharp
   // Test: UserManagement_GetGroupForUser_FallsBackToNtLogin_WhenUpnNull
   // Given: a User row with UPN = NULL and NTLogin = "DEFRA\\jsmith"
   //        OIDC token with upn = "jsmith@defra.gov.uk" (no UPN match)
   // Assert: GetGroupForUser resolves user via NTLogin substring match
   ```

3. **Unknown user test:**

   ```csharp
   // Test: UserManagement_GetGroupForUser_ReturnsNone_WhenUserNotFound
   // Given: no User row matches UPN or NTLogin
   // Assert: method returns UserGroup.None (or equivalent) without throwing
   ```

4. **Authorization policy registration test:** All five `AuthorizationPolicy` registrations (`DefraViewer`, `DefraDataEntry`, `DefraMaintenance`, `VlaDataEntry`, `VlaMaintenance`) must be verifiable by an in-process test that builds the `IServiceCollection` and asserts all policies are registered.

**Go/no-go criterion:** All three UPN mapping tests and the policy registration test must pass before Slice 3 is approved for merge. Security review by named reviewer required.

---

## 8. Integration Test Base Class Pattern

All integration tests that require SQL Server use a shared `SqlServerFixture` based on Testcontainers:

```csharp
// tests/BSE.Infrastructure.Tests/Fixtures/SqlServerFixture.cs
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Deploy BSE.Database dacpac via SqlPackage CLI
        await DeployDacpacAsync(ConnectionString);
        // Seed reference data from lu* tables (static data only)
        await SeedReferenceDataAsync(ConnectionString);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

Each module's test project references this fixture via `IClassFixture<SqlServerFixture>`. Test data is inserted and cleaned up per test using `BEGIN TRANSACTION` / `ROLLBACK` or explicit `DELETE` statements against test-only RBSE values (prefix `TS` reserved for test data).

---

## 9. Dapper Repository Mocking Pattern

For unit tests that need to verify service logic without a database, inject `IDapperRepository` via NSubstitute:

```csharp
// Example: unit test for ADNS reference assignment logic
var mockRepo = Substitute.For<IDapperRepository>();
mockRepo.QueryAsync<CaseForAdns>("GetADNSCasesForGB", Arg.Any<object>())
        .Returns(new[] { new CaseForAdns { Rbse = "9900001001" } });

var service = new AdnsExportService(mockRepo, /* other deps */);
var result = await service.BuildExportAsync("GB");

result.Cases.Single().AdnsReferenceNumber.ShouldBe(43); // given last ref was 42
```

---

## 10. How to Run Tests Locally

> **Note:** These commands apply once Slice 0 scaffolding is complete. Before Slice 0, only the pure unit tests (targeting `BSE.SharedKernel.Tests`) can be run.

```bash
# Unit tests only (no Docker required)
dotnet test src/BSE.slnx --filter "Category=Unit"

# All tests including integration (requires Docker)
dotnet test src/BSE.slnx

# Integration tests for a specific module
dotnet test tests/BSE.Modules.CaseManagement.Tests

# Run with coverage
dotnet test src/BSE.slnx --collect "XPlat Code Coverage" --results-directory ./coverage

# Generate HTML coverage report
reportgenerator -reports:coverage/**/*.xml -targetdir:coverage/html -reporttypes:Html
```

### Environment variables required for integration tests

| Variable | Value |
|----------|-------|
| `BSE_CONNECTION_STRING` | Set automatically by Testcontainers fixture; not required for local Docker tests |
| `SKIP_INTEGRATION` | Set to `true` to run unit tests only in offline environments |

---

## 11. CI Integration

The GitHub Actions CI workflow is defined in `.github/workflows/ci.yml`. It:

1. Detects whether the Slice 0 solution file exists (`src/BSE.slnx` or `src/BSE.sln`).
2. If the solution file is absent, the build and test steps are skipped with an informational notice (pre-Slice 0 state).
3. When the solution exists: restore → build (zero warnings enforced via `-warnaserror`) → test (with coverage collection).
4. Runs on all pushes and pull requests targeting `main` and `feature/*` branches.

See `.github/workflows/ci.yml` for the full workflow definition.

---

## 12. Coverage Targets

| Phase | Target | Notes |
|-------|--------|-------|
| Slice 0 | Placeholder tests pass; no coverage gate | No domain code yet |
| Slices 1–7 | ≥ 70% branch coverage per module | Enforced via Coverlet threshold in CI |
| Slices 8–14 | ≥ 80% branch coverage per module | Migration plan acceptance criterion |
| High-risk slices (3, 8, 11) | 100% coverage of the specific methods called out in §7 | Hard gate — merge blocked if not met |
