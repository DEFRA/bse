# BSE System — Intelligent Migration Plan

> **Classification:** Programme Governance — Confidential  
> **Date:** 2026-07-20  
> **Author:** Intelligent Migration Agent  
> **Status:** Draft — Requires Delivery Lead Review  
> **Disclaimer:** This document contains proprietary and confidential information belonging to Crown Commercial Property. Unauthorised use, disclosure, or distribution is prohibited.

---

## 1. Executive Summary

The BSE System is a mission-critical, single-tenanted government application that supports APHA's statutory obligation to track and report Bovine Spongiform Encephalopathy cases in Great Britain, Northern Ireland, and imported animals. It has been in continuous operation since approximately 2003. The current implementation is built on VB.NET, ASP.NET WebForms, and .NET Framework 4.0 — a technology stack that reached end-of-life in 2016 and is no longer supported by Microsoft.

System discovery (see `docs/HLD.md` and `docs/LLD.md`) has confirmed six material risks that cannot be managed by patching:

| Risk | Evidence |
|------|----------|
| Framework end-of-life | .NET Framework 4.0 — unsupported; no security patches |
| Partial save corruption | `clsCase.UpdateCaseDetails` calls 8+ stored procedures across separate connections with no outer transaction |
| Single-server constraint | InProc session state prohibits load balancing or rolling deployment |
| Fragile authorisation | Per-page string comparison replaces centralised access control |
| Zero automated tests | No unit, integration, or end-to-end test coverage exists |
| DataSet coupling | Integer-indexed table constants silently break if stored procedure result ordering changes |

This plan describes a phased, incremental migration from the legacy stack to a C# 14 / .NET 10 modular monolith deployed in containers. The migration follows a Strangler-Fig pattern: the legacy system remains fully operational at every phase gate, and each slice is independently deployable and reversible.

The programme is sized for a team of 5–7 people, AI-augmented with GitHub Copilot and Microsoft 365 Copilot, with an estimated elapsed duration of **18 months** across five sequential phases. Incremental delivery begins at Phase 3.

---

## 2. Migration Objectives

| # | Objective | Measurable Outcome |
|---|-----------|-------------------|
| O1 | Eliminate framework end-of-life risk | All code compiles and runs on .NET 10 LTS |
| O2 | Achieve container deployability | Application starts and serves traffic inside a Linux container |
| O3 | Establish automated safety net | ≥ 80% branch coverage on migrated modules before go-live |
| O4 | Remove InProc session coupling | Multi-step workflows survive app-pool recycles and scale horizontally |
| O5 | Centralise authorisation | Single policy engine; no per-page string comparisons |
| O6 | Achieve transactional integrity on save | Case save wrapped in a single database transaction; no partial-update states |
| O7 | Preserve all regulatory outputs | ADNS, OSS, and BSESS integrations produce byte-identical outputs for equal inputs |
| O8 | Maintain or improve audit completeness | All audit log entries produced by migrated code match legacy behaviour |

---

## 3. Phased Roadmap

The roadmap is structured into five phases. Each phase has a defined set of deliverables, dependencies, success criteria, and a human-approved go/no-go gate before the next phase begins.

---

### Phase 0 — Foundation (Weeks 1–6)

**Purpose:** Establish the programme infrastructure and safety net before any migration work begins. Corresponds to Chaos Report risk mitigation: executive buy-in, scope clarity, and baseline technical control.

**Deliverables:**

| ID | Deliverable | Owner |
|----|-------------|-------|
| P0-D1 | Project charter signed by executive sponsor | Delivery Lead |
| P0-D2 | GitHub repository with branch protection, PR templates, and CODEOWNERS | DevOps Engineer |
| P0-D3 | GitHub Actions CI pipeline (build + lint for legacy solution) | DevOps Engineer |
| P0-D4 | Containerised SQL Server development environment | DevOps Engineer |
| P0-D5 | Baseline integration test suite — smoke tests against legacy system exercising all 12 domain flows | QA Engineer |
| P0-D6 | Agreed Definition of Done (DoD) and PR review checklist | Delivery Lead + Tech Lead |
| P0-D7 | Architecture Decision Records updated with migration decisions | Cloud Architect |
| P0-D8 | Risk register baselined (`docs/Risk-and-Governance.md`) | Delivery Lead |

**Dependencies:** Executive sponsor identified and committed; development environment accessible.

**Success Criteria:**

- CI pipeline executes on every PR and blocks merge on failure.
- Legacy integration smoke tests pass in CI for all 12 domain flows.
- All team members have GitHub Copilot licences and have completed onboarding.
- Risk register reviewed and signed off by sponsor.

**Go/No-Go Gate — P0:** Delivery Lead and executive sponsor confirm all P0 success criteria are met. Human sign-off required before Phase 1 begins.

---

### Phase 1 — Strangler-Fig Shell and Reference Data (Weeks 7–14)

**Purpose:** Establish the .NET 10 modular monolith solution shell and migrate the first low-risk domain (Reference Data) as proof of pattern. Validates build pipeline, container runtime, and database connectivity end-to-end without touching business-critical data flows.

**Deliverables:**

| ID | Deliverable | Owner |
|----|-------------|-------|
| P1-D1 | `.slnx` solution with skeleton module projects for all 12 domains | Cloud Architect + Tech Lead |
| P1-D2 | Shared infrastructure modules: database connection (Dapper + SqlConnection), structured logging (Serilog → Application Insights), distributed cache (Redis) for session replacement | Tech Lead |
| P1-D3 | `ReferenceData` module migrated: all `lu*` lookup stored procedures accessible via typed C# repository | Developer |
| P1-D4 | `UserManagement` module migrated: Windows authentication replaced with ASP.NET Core Windows auth; `clsUser.GetGroupForUser` replaced with claims-based role resolution; centralised `AuthorizationPolicy` registered | Cloud Architect |
| P1-D5 | Containerised deployment: `Dockerfile`, `docker-compose.yml` for dev; CI pipeline builds and pushes image | DevOps Engineer |
| P1-D6 | Unit tests for `ReferenceData` and `UserManagement` modules (≥ 80% branch coverage) | QA Engineer |

**Dependencies:** Phase 0 complete and gate passed; SQL Server accessible from container network.

**Success Criteria:**

- New .slnx solution builds in CI with no errors or warnings.
- `ReferenceData` module serves all lookup data; results are byte-identical to legacy for the same stored procedures.
- `UserManagement` module resolves Windows identity to application role; group names match legacy `luUserGroup` values.
- Centralised `AuthorizationPolicy` replaces per-page string comparison for all five known user groups.
- Container image starts, connects to SQL Server, and responds to a health-check endpoint.
- ≥ 80% branch coverage on migrated modules.

**Go/No-Go Gate — P1:** Tech Lead confirms pattern validity and test coverage. Security review of new auth module by a named reviewer. Human sign-off required before Phase 2 begins.

---

### Phase 2 — Core Domain Migration (Weeks 15–34)

**Purpose:** Migrate the high-value, high-risk business domains. These domains carry the greatest regulatory weight and have the most identified coupling risks. Migrated in dependency order to avoid circular references.

**Domain Sequence and Rationale:**

| Order | Domain | Rationale |
|-------|--------|-----------|
| 1 | AuditLog | No dependencies on other domains; write-only from business logic. Migrating early gives an immutable audit trail for all subsequent migration activity. |
| 2 | Farm Management | `Case` has FK to `Farm`; farm must exist before case creation. Isolated — `clsFarm` only calls 3 stored procedures. |
| 3 | Search | Read-only; calls `GetSearchCase` and `GetSearchFarm`. Safe migration with direct comparison to legacy outputs. |
| 4 | Batch/Sampling | Low complexity; required by Case Management (BatchID). |
| 5 | Case Management | Largest, highest risk. Migrated last within Phase 2, after all dependencies are stable. Includes transactional integrity fix: `UpdateCaseDetails` wrapped in `SqlTransaction`. |
| 6 | Animal Relations | Depends on Case; self-referential `CaseRelation` — migrated as part of Case or immediately after. |
| 7 | CaseWork | Embedded in `clsCase.vb`; migrated as a sub-module of Case Management. |

**Key Engineering Obligations per Domain:**

- Replace all `DataSet`/`DataTable` column-index integer constants with named, typed C# record types.
- Replace all session state with Redis-backed distributed cache keyed by user session ID.
- Implement a single outer `SqlTransaction` wrapping `UpdateCaseDetails` and all child stored procedure calls.
- All domain modules expose interfaces; data access is injected (no static classes).
- Stored procedures are called via Dapper — no ORM, preserving ADR-001 intent.

**Deliverables:**

| ID | Deliverable |
|----|-------------|
| P2-D1 | AuditLog module with read API and audit-write interceptor |
| P2-D2 | FarmManagement module (all `clsFarm` methods + 3 SPs) |
| P2-D3 | Search module (read-only; all `clsSearch` methods) |
| P2-D4 | Batch module (`clsBatch` all methods) |
| P2-D5 | CaseManagement module (all `clsCase` methods; transactional save; typed records replacing DataSet constants) |
| P2-D6 | AnimalRelations module (`clsRelations`) |
| P2-D7 | Integration tests for each module; comparison tests validating outputs equal legacy for shared data set |
| P2-D8 | Updated Runbook covering migrated modules |

**Dependencies:** Phase 1 complete and gate passed; UserManagement auth in place.

**Success Criteria:**

- Each domain module has ≥ 80% branch coverage.
- Case save with 8+ child records completes atomically; partial failure rolls back cleanly (verified by injected fault test).
- Search results for 20 reference queries are identical between legacy and migrated system.
- RBSE change (`ChangeRBSE`) cascades correctly across all 15 child tables.
- No session state in migrated modules (all state in Redis or database).
- Security review passed for CaseManagement module (input validation, injection risk on all SP parameters).

**Go/No-Go Gate — P2:** All integration tests pass in CI. Tech Lead + QA sign off on coverage and output equivalence. Security review complete. Human sponsor sign-off required before Phase 3 begins.

---

### Phase 3 — Regulatory Integration Migration (Weeks 35–44)

**Purpose:** Migrate the three regulatory output domains — ADNS Export, OSS Export, and BSESS Cross-check. These domains have external consumers (EU ADNS, OIE/OSS, BSESS system) and produce outputs that must be byte-identical to the legacy system for compliance.

**Deliverables:**

| ID | Deliverable |
|----|-------------|
| P3-D1 | `ADNSExport` module: GB, NI, and CI variants; SMTP email via modern `SmtpClient`; sequential ADNS reference number assignment |
| P3-D2 | `OSSExport` module: staging table population via `Copy*` stored procedures; batch OSS report generation |
| P3-D3 | `BsessIntegration` module: cross-check read methods; SSIS package replaced with .NET-hosted ETL job using `Microsoft.Data.SqlClient` bulk copy |
| P3-D4 | Output comparison harness: automated comparison of ADNS email bodies and OSS export files between legacy and migrated systems against shared test data |
| P3-D5 | SMTP configuration externalised to environment variables (no hardcoded relay host) |
| P3-D6 | Unit and integration tests for all three modules |

**Dependencies:** Phase 2 complete and gate passed; test data set covering all ADNS/OSS/BSESS scenarios agreed with APHA stakeholders.

**Success Criteria:**

- ADNS email body output for 10 reference cases is identical between legacy and migrated system.
- OSS export staging tables contain identical rows for the same input data set.
- BSESS cross-check returns identical results to legacy for shared data.
- SSIS dependency eliminated; ETL job runs in container without SSIS runtime.
- Regulatory stakeholder (APHA) has reviewed and accepted ADNS output format.

**Go/No-Go Gate — P3:** APHA regulatory stakeholder sign-off on ADNS output format. QA output comparison harness passes. SSIS replacement validated. Human sponsor sign-off required before Phase 4 begins.

---

### Phase 4 — UI Migration and Cutover (Weeks 45–58)

**Purpose:** Replace the ASP.NET WebForms UI with a modern ASP.NET Core Razor Pages (or Blazor Server) UI layer. Run legacy and new systems in parallel on shared database until confidence is established, then cut over.

**Key decisions required before Phase 4 starts:**

- UI technology choice: Razor Pages (lower risk, similar paradigm) vs Blazor Server (richer UX, higher complexity).
- Parallel-run period duration (recommendation: 4 weeks minimum).
- Cutover date and rollback window agreed with APHA.

**Deliverables:**

| ID | Deliverable |
|----|-------------|
| P4-D1 | UI module with all 90 ASPX pages migrated to Razor Pages/Blazor |
| P4-D2 | AjaxControlToolkit dependency eliminated; replaced with modern JS components or Blazor components |
| P4-D3 | Accessibility review against WCAG 2.1 AA (government requirement) |
| P4-D4 | End-to-end Playwright test suite covering all 12 domain workflows |
| P4-D5 | Production container image; Kubernetes or container-hosting manifests |
| P4-D6 | Parallel-run monitoring dashboard (Application Insights) comparing legacy vs new error rates, response times |
| P4-D7 | Updated Runbook for container deployment |
| P4-D8 | Legacy system decommission plan |

**Dependencies:** Phase 3 complete and gate passed; hosting environment (container platform) provisioned; APHA sign-off on UI technology choice.

**Success Criteria:**

- All 12 domain workflows complete successfully in E2E tests.
- Error rate in new system ≤ error rate in legacy during parallel-run period.
- ADNS regulatory submission completed successfully via new system (live test).
- No open P1/P2 defects at cutover gate.
- Legacy system is not decommissioned until parallel-run success criteria are met.

**Go/No-Go Gate — P4 (Cutover):** Delivery Lead, APHA stakeholder, and executive sponsor all sign off. Rollback plan tested and ready. Cutover window agreed. Human sign-off required.

---

### Phase 5 — Hardening and Closure (Weeks 59–72)

**Purpose:** Stabilise the production system, decommission legacy, complete documentation, and hand over to support.

**Deliverables:**

| ID | Deliverable |
|----|-------------|
| P5-D1 | Legacy system decommissioned (IIS site removed, `.NET Framework` build artefacts archived) |
| P5-D2 | SSIS packages archived (superseded by Phase 3 ETL) |
| P5-D3 | Final Runbook updated for container-hosted production system |
| P5-D4 | Post-implementation review report |
| P5-D5 | Support handover package (runbooks, architecture diagrams, contact lists) |
| P5-D6 | Application Performance Monitoring (APM) baselines established |

**Success Criteria:**

- Legacy system decommissioned with no service impact.
- Support team able to operate the new system without migration team involvement.
- All documentation up to date in repository.
- Post-implementation review completed and lessons learned recorded.

---

## 4. Roadmap Summary

| Phase | Name | Elapsed Weeks | Key Go/No-Go Gate |
|-------|------|---------------|-------------------|
| 0 | Foundation | 1–6 | CI pipeline live; smoke tests pass; risk register signed |
| 1 | Shell + Reference/User | 7–14 | Auth module security reviewed; pattern validated; container runs |
| 2 | Core Domain Migration | 15–34 | Transactional integrity verified; coverage ≥ 80%; output equivalence |
| 3 | Regulatory Integrations | 35–44 | APHA sign-off on ADNS output; SSIS eliminated |
| 4 | UI Migration + Cutover | 45–58 | Parallel-run passed; APHA + sponsor sign-off; rollback tested |
| 5 | Hardening + Closure | 59–72 | Legacy decommissioned; support handed over |

---

## 5. Explicit Success Criteria — Programme Level

| Criterion | Measurement |
|-----------|-------------|
| Zero data loss | No partial-save corruptions in production post-migration (monitored via AuditLog completeness check) |
| Regulatory continuity | ADNS email sent on same schedule with identical content |
| No regression | E2E test suite green for all 12 domain workflows |
| Horizontal scalability | Application runs correctly with two container replicas (session parity verified) |
| Security posture | No per-page string-comparison authorisation in new codebase; OWASP Top 10 review passed |
| Test coverage | ≥ 80% branch coverage across all migrated modules at cutover |
| Support readiness | Support team completes acceptance exercise without migration team involvement |

---

*Next document: [Intelligent-Team-Model.md](Intelligent-Team-Model.md)*
