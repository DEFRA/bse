# BSE System — Risk and Governance

> **Classification:** Programme Governance — Confidential  
> **Date:** 2026-07-20  
> **Author:** Intelligent Migration Agent  
> **Status:** Draft — Requires Delivery Lead Review  
> **Disclaimer:** This document contains proprietary and confidential information belonging to Crown Commercial Property. Unauthorised use, disclosure, or distribution is prohibited.

---

## 1. Chaos Report Failure Pattern Mapping

The Standish Group Chaos Report identifies recurring failure patterns in technology delivery projects. The table below maps each pattern to the BSE migration context, assesses its likelihood and impact, and defines the primary control.

| # | Chaos Report Failure Pattern | BSE Context Evidence | Likelihood | Impact | Primary Control |
|---|------------------------------|----------------------|------------|--------|-----------------|
| CR-1 | Lack of executive support | APHA is a statutory agency; system underpins regulatory reporting obligation — implicit mandate exists, but explicit sponsorship not yet formalised | Medium | Critical | Dedicated executive sponsor role (see Team Model §3.1); sponsor signs every phase gate |
| CR-2 | Lack of user involvement | Domain knowledge of BSE surveillance rules is held by a small number of APHA staff; none are currently allocated to the programme | High | High | APHA SME allocated 0.2 FTE (see Team Model §3.8); ADNS and OSS outputs require SME acceptance before Phase 3 gate |
| CR-3 | Incomplete or changing requirements | No requirements documentation exists; all requirements are embedded in 230 stored procedures and ~90 ASPX pages | High | High | System discovery outputs (HLD, LLD, ADRs) are the requirements baseline; changes must be raised as ADRs and approved by Tech Lead before implementation |
| CR-4 | Lack of planning | No migration plan existed prior to this programme | — | — | Addressed by this document and Intelligent-Migration-Plan.md |
| CR-5 | Unrealistic expectations | WebForms-to-modern migration of a 20-year-old codebase routinely underestimated | High | High | 18-month phased roadmap with explicit scope per phase; no big-bang rewrite; each phase independently deliverable |
| CR-6 | Technology competency deficit | Team may have limited .NET 10, containers, or Dapper experience | Medium | Medium | GitHub Copilot augmentation per role; Tech Lead provides pattern guidance; brown-bag sessions on new patterns in Phase 0 |
| CR-7 | Scope creep | Pressure to "improve" features during migration | Medium | Medium | Scope freeze: migration slices replace existing behaviour; new features are out of scope until post-cutover |
| CR-8 | Poor project management | No programme office or dedicated PM currently assigned | Medium | High | Delivery Lead role explicitly created; weekly status reporting; fortnightly stakeholder updates |

---

## 2. Programme Risk Register

Risks are rated on a 1–5 scale for Likelihood (L) and Impact (I). Risk Score = L × I. Risks with score ≥ 12 are programme-level escalation triggers.

### 2.1 TOP RISK: Partial Case Save Corruption (R01)

| Field | Detail |
|-------|--------|
| **Risk ID** | R01 |
| **Description** | `clsCase.UpdateCaseDetails()` calls 8+ stored procedures across separate ADO.NET connections with no outer database transaction. A failure after `AddCase` but before a child SP (e.g., `AddCaseClinical`) completes leaves the `Case` table in a partially updated state. This is an existing defect in the legacy system. |
| **Evidence** | `legacy/BSELib/clsCase.vb` — `UpdateCaseDetails()` method; `legacy/DataAccessLib/clsDataAccess.vb` — per-call connection pattern (ADR-004); `legacy/BSEDatabase/dbo/Stored Procedures/AddCase.sql` |
| **Phase at which risk peaks** | Phase 2 (CaseManagement migration) — if the transactional boundary is not implemented correctly in the migrated code |
| **Likelihood** | 3 (possible — existing latent defect; not yet manifested in production as observed) |
| **Impact** | 5 (critical — BSE case data is a statutory record; partial updates are a regulatory integrity failure) |
| **Risk Score** | **15** |
| **Control** | Mandatory: wrap all `UpdateCaseDetails` stored procedure calls in a single `SqlTransaction` in migrated C# code. This is an explicit go/no-go criterion for Phase 2. Verified by injected-fault integration test. |
| **Owner** | Tech Lead |
| **Residual Risk** | 2 × 5 = 10 (after transactional boundary control is in place) |

---

### 2.2 TOP RISK: ADNS Regulatory Output Regression (R02)

| Field | Detail |
|-------|--------|
| **Risk ID** | R02 |
| **Description** | The ADNS export emails case notifications to the EU (`SANTE-ADNS@ec.europa.eu`). Any regression in the content, format, or sequential reference number assignment would constitute a regulatory reporting failure with potential legal consequences. |
| **Evidence** | `legacy/BSELib/clsADNSReport.vb` — email construction and reference assignment logic; `legacy/BSESystem/ADNSExportGB.aspx.vb`; EU recipient hardcoded in `Web.config` (`ADNSEmailToAddress`) |
| **Phase at which risk peaks** | Phase 3 (ADNS module migration) and Phase 4 (cutover) |
| **Likelihood** | 3 (possible — complex multi-step email-and-update workflow; reference number sequence logic must be precisely replicated) |
| **Impact** | 5 (critical — EU regulatory obligation; failure to notify is a statutory breach) |
| **Risk Score** | **15** |
| **Control** | Automated output comparison harness (Phase 3 deliverable P3-D4); APHA SME sign-off on ADNS output before Phase 3 gate. ADNS module not enabled in production until harness passes. Parallel-run period in Phase 4 to validate live ADNS submissions via new system before legacy decommission. |
| **Owner** | QA Engineer + APHA SME |
| **Residual Risk** | 1 × 5 = 5 (after comparison harness and SME sign-off) |

---

### 2.3 TOP RISK: Session State Loss During Migration (R03)

| Field | Detail |
|-------|--------|
| **Risk ID** | R03 |
| **Description** | The legacy system stores 12+ DataSet and domain objects in InProc session (see ADR-002). During the transition from InProc to Redis-backed distributed cache, if session key contracts change or Redis is unavailable, users lose unsaved case entry work mid-session. |
| **Evidence** | `legacy/BSESystem/SessionVars.vb` — 40+ session key string constants; `legacy/BSESystem/CaseEntrySave.aspx.vb` — reads `SV_CaseDetails`, `SV_FarmDetails` immediately before save; `Web.config` `<sessionState mode="InProc" ... timeout="20"/>` |
| **Phase at which risk peaks** | Phase 1 (Redis introduction) and Phase 4 (UI migration) |
| **Likelihood** | 3 (possible — Redis infrastructure dependency introduced; session key contract must be preserved exactly) |
| **Impact** | 4 (high — data loss for users mid-session; degraded trust; no mechanism for recovery in legacy either) |
| **Risk Score** | **12** |
| **Control** | Session key constants are preserved verbatim from `SessionVars.vb`; Redis connection is health-checked before each request; circuit breaker configured (fall back to single-instance in-memory if Redis unavailable, disabling scale-out). Redis availability is a Phase 1 CI requirement before gate approval. |
| **Owner** | Tech Lead + DevOps Engineer |
| **Residual Risk** | 2 × 4 = 8 |

---

### 2.4 HIGH RISK: DataSet Column-Index Silent Corruption (R04)

| Field | Detail |
|-------|--------|
| **Risk ID** | R04 |
| **Description** | Integer constants (`CASE_TABLE = 0`, `CLINICAL_TABLE = 1`, etc.) index `DataSet` tables returned by `GetCaseDetailsByRBSE`. Any reordering of result sets in stored procedures silently corrupts data without a compile error. |
| **Evidence** | `legacy/BSELib/clsCase.vb` — 11 public `Const` table index declarations; all `CaseEntry*.aspx.vb` pages; `legacy/BSESystem/Common.vb` |
| **Likelihood** | 2 (unlikely in legacy — ordering is frozen; high risk during migration if SP result sets are touched) |
| **Impact** | 5 (critical — silent data corruption; wrong fields saved to wrong columns) |
| **Risk Score** | **10** |
| **Control** | Migration replaces all DataSet/integer-index patterns with named, typed C# record types as a Phase 2 deliverable. No stored procedure result ordering change is permitted without a corresponding schema migration PR reviewed by Tech Lead. |
| **Owner** | Tech Lead |
| **Residual Risk** | 1 × 2 = 2 |

---

### 2.5 HIGH RISK: Authorisation Bypass via Missing `EnableControls` (R05)

| Field | Detail |
|-------|--------|
| **Risk ID** | R05 |
| **Description** | Authorisation in the legacy system is enforced by per-page `EnableControls()` calls that compare session group name strings. A newly added UI page with no `EnableControls()` call is accessible to all authenticated users. This risk is carried forward unless resolved at migration. |
| **Evidence** | `legacy/BSESystem/ADNSExportGB.aspx.vb` — representative `EnableControls()` implementation; ADR-003 |
| **Likelihood** | 3 (possible during Phase 4 UI migration — 90 pages must each be correctly annotated) |
| **Impact** | 4 (high — regulatory data accessible to unauthorised users) |
| **Risk Score** | **12** |
| **Control** | Phase 1 replaces string-comparison pattern with a centralised `AuthorizationPolicy` using `[Authorize(Policy = "...")]` attributes. New UI pages in Phase 4 default to `[Authorize]` at the controller/page level; unannotated pages fail CI lint check. Security review of auth module at Phase 1 gate. |
| **Owner** | Cloud Architect |
| **Residual Risk** | 1 × 4 = 4 |

---

### 2.6 MEDIUM RISK: SSIS Runtime Dependency (R06)

| Field | Detail |
|-------|--------|
| **Risk ID** | R06 |
| **Description** | BSESS Import uses an SSIS package (`BSESS Import.dtsx`) that requires the SSIS runtime. SSIS cannot run inside a Linux container. The SSIS package password is stored in `README.txt` (`pass`) — a security finding requiring immediate remediation. |
| **Evidence** | `legacy/BSEIntegrationServices/BSESS Import.dtsx`; `legacy/BSEIntegrationServices/README.txt`; `legacy/BSEDatabase/UAT BSESS Import.dtsx` |
| **Likelihood** | 4 (likely — SSIS is a blocking dependency for containerisation if not replaced) |
| **Impact** | 3 (medium — BSESS cross-check is important but not the primary regulatory output) |
| **Risk Score** | **12** |
| **Control** | Phase 3 replaces SSIS with a .NET-hosted ETL job. SSIS package password exposure to be remediated immediately (rotate credential; update secret store). |
| **Owner** | DevOps Engineer (secret rotation) + Senior Developer (ETL replacement) |
| **Residual Risk** | 2 × 3 = 6 |

---

### 2.7 MEDIUM RISK: Plaintext Credentials in Source Control (R07)

| Field | Detail |
|-------|--------|
| **Risk ID** | R07 |
| **Description** | `legacy/BSESystem/Web.config` contains a plaintext SQL Server password (`BSESystemUser`/`password`). `legacy/BSEIntegrationServices/README.txt` contains SSIS package password `pass`. Both are committed to the repository. |
| **Evidence** | `legacy/BSESystem/Web.config` `DBConnectionString` key; `legacy/BSEIntegrationServices/README.txt` |
| **Likelihood** | 5 (confirmed — already in repository) |
| **Impact** | 4 (high — credential exposure enables database access; regulatory data at risk) |
| **Risk Score** | **20** |
| **Control** | **Immediate action (pre-Phase 0):** Rotate SQL Server credential; rotate SSIS package password; add `.gitignore` entries or `git-secrets` pre-commit hook to block future credential commits. DevOps Engineer to replace `DBConnectionString` with Azure Key Vault reference or GitHub Secrets injection in all non-legacy build targets. |
| **Owner** | DevOps Engineer |
| **Residual Risk** | 1 × 4 = 4 (after rotation and secret-scanning hook) |

---

### 2.8 MEDIUM RISK: No Automated Tests — Regression Blind Spot (R08)

| Field | Detail |
|-------|--------|
| **Risk ID** | R08 |
| **Description** | The legacy system has zero automated tests. Any migration change that alters behaviour will not be detected until manual testing or production exposure. |
| **Evidence** | No test projects in `legacy/BSESystem.sln`; no test runner in legacy CI |
| **Likelihood** | 3 (possible — regression is a normal risk in migration; absence of tests amplifies it) |
| **Impact** | 4 (high — regulatory outputs or data integrity could silently regress) |
| **Risk Score** | **12** |
| **Control** | Phase 0 baseline integration test suite (P0-D5); ≥ 80% branch coverage enforced before each phase gate. Test coverage metric is a CI gate — PRs that reduce coverage below threshold are blocked. |
| **Owner** | QA Engineer |
| **Residual Risk** | 1 × 4 = 4 (once Phase 0 baseline is in place) |

---

### 2.9 LOWER RISK: `ChangeRBSE` Cascade Without TRY/CATCH (R09)

| Field | Detail |
|-------|--------|
| **Risk ID** | R09 |
| **Description** | `ChangeRBSE` stored procedure cascades an RBSE number change across 15 child tables using `@@ERROR` (pre-SQL 2005 pattern, no `TRY/CATCH`). An intermediate failure may leave partial updates without a clean rollback. |
| **Evidence** | `legacy/BSEDatabase/dbo/Stored Procedures/ChangeRBSE.sql`; LLD §6.5 |
| **Likelihood** | 2 (unlikely in normal operation; risk increases if SQL Server is under load or storage is constrained) |
| **Impact** | 5 (critical — RBSE is the primary case key; partial cascade corrupts cross-references) |
| **Risk Score** | **10** |
| **Control** | Phase 2 migration must wrap `ChangeRBSE` call in a client-side `SqlTransaction`. Additionally, the stored procedure should be refactored to use `BEGIN TRY / BEGIN CATCH / ROLLBACK TRANSACTION` as a Phase 2 engineering obligation. |
| **Owner** | Senior Developer |
| **Residual Risk** | 1 × 5 = 5 |

---

## 3. Control Mechanisms

### 3.1 Code Review

All code changes enter the main branch via pull request only. No direct pushes to `main` or `release/*` branches. Minimum reviewers: 1 peer + Tech Lead (for cross-cutting concerns). Copilot is not a reviewer — it is a drafting tool.

### 3.2 CI Gate

GitHub Actions pipeline must pass before a PR is mergeable:

| Check | Blocks Merge? |
|-------|--------------|
| Build (dotnet build) | Yes |
| Unit tests (dotnet test) | Yes |
| Branch coverage < 80% on changed modules | Yes |
| Secret scanning (git-secrets / GitHub Advanced Security) | Yes |
| OWASP dependency-check | Yes |
| Linting (per-page authorisation attribute check) | Yes |

### 3.3 Dependency Injection Enforcement

No `static` classes (except constants and `Enum`) are permitted in migrated code. All dependencies must be resolvable via the ASP.NET Core DI container. Verified by Roslyn analyser rule in CI.

### 3.4 Stored Procedure Call Governance

Stored procedure calls in migrated code use Dapper. No ad-hoc SQL strings. SP names must match the `BSEDatabase` project. Any new or changed SP requires a corresponding schema migration PR reviewed by Tech Lead.

### 3.5 Secret Management

No credentials in source control. DevOps Engineer maintains a secret store (Azure Key Vault or GitHub Secrets). CI injects secrets at runtime. `git-secrets` pre-commit hook blocks accidental credential commits.

### 3.6 Scope Freeze

The migration scope is defined by the current system behaviour as documented in HLD, LLD, and ADRs. New features, UI redesigns beyond parity, or performance optimisations are not in scope for migration phases. All out-of-scope requests are logged, deferred to post-cutover backlog, and communicated to the requester.

---

## 4. Human-in-the-Loop Decision Points

The following decisions require explicit human sign-off and must not be automated or delegated to an AI tool:

| Decision Point | Required Approver(s) | Phase |
|----------------|----------------------|-------|
| Phase 0 go/no-go | Delivery Lead + Executive Sponsor | Phase 0 end |
| Phase 1 go/no-go | Delivery Lead + Tech Lead + named Security Reviewer | Phase 1 end |
| Phase 2 go/no-go | Delivery Lead + Tech Lead + QA | Phase 2 end |
| Phase 3 go/no-go | Delivery Lead + QA + APHA SME | Phase 3 end |
| Production cutover (Phase 4) | Delivery Lead + Executive Sponsor + APHA SME | Phase 4 gate |
| Legacy decommission (Phase 5) | Delivery Lead + Executive Sponsor | Phase 5 |
| Any budget change > 10% | Executive Sponsor | Any phase |
| Any scope change | Tech Lead (technical) + Delivery Lead (schedule/budget) | Any phase |
| Credential rotation (immediate — R07) | DevOps Engineer + Delivery Lead | Pre-Phase 0 |

---

## 5. Rollback Strategies

### 5.1 During Migration Phases (0–3)

Legacy system remains in production throughout Phases 0–3. Rollback is not required — the new system is not yet serving production traffic.

### 5.2 During Phase 4 Parallel Run

Both legacy and new systems serve production traffic, reading and writing to the same SQL Server database. Rollback procedure:

1. Remove new system from load balancer routing.
2. Redirect all traffic to legacy system.
3. Verify legacy system processes requests correctly.
4. Capture incident report with root cause before next cutover attempt.

**Prerequisite:** Load balancer routing must be configurable within 15 minutes without a deployment.

### 5.3 Post-Cutover (Legacy Not Yet Decommissioned)

Legacy system remains in a warm-standby state for 4 weeks after cutover. Rollback procedure:

1. Stop new system containers.
2. Update load balancer to route to legacy system.
3. Verify legacy system processes requests correctly (smoke tests).
4. Notify executive sponsor and APHA SME.
5. Schedule root cause analysis before re-attempting cutover.

**Prerequisite:** Legacy IIS configuration is not dismantled until Phase 5 gate is approved.

### 5.4 Database Rollback

No destructive schema changes are permitted during migration. Stored procedures are additive (new SPs added; existing SPs not modified in ways that break legacy callers). Any SP modification that changes the result set contract requires a version suffix (`_v2`) and a corresponding update to the legacy compatibility layer before the legacy system is decommissioned.

**Database backups:** Full database backup taken immediately before each phase gate and immediately before production cutover. Backup verified restorable before cutover window opens.

---

## 6. Audit and Telemetry Expectations

| Requirement | Mechanism |
|-------------|-----------|
| Application errors | Serilog → Application Insights (structured logging) |
| Database audit trail | `AuditLog` table (unchanged — existing SP-based writes preserved) |
| Deployment audit | GitHub Actions deployment log; GitHub release tags |
| Access log | IIS/ASP.NET Core request logging; Application Insights page views |
| Secret access log | Azure Key Vault audit log (if Key Vault used) |
| Phase gate decisions | Documented in GitHub PR description; linked from `docs/` |

---

*Next document: [ROI-and-Budget.md](ROI-and-Budget.md)*
