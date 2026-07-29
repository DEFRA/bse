# BSE System — Intelligent Team Model

> **Classification:** Programme Governance — Confidential  
> **Date:** 2026-07-20  
> **Author:** Intelligent Migration Agent  
> **Status:** Draft — Requires Delivery Lead Review  
> **Disclaimer:** This document contains proprietary and confidential information belonging to Crown Commercial Property. Unauthorised use, disclosure, or distribution is prohibited.

---

## 1. Overview

The BSE migration team is a compact, cross-functional unit. Its design is informed by the Chaos Report finding that small, empowered teams with clear decision rights deliver at significantly higher success rates than large committees. Every role is assigned explicit responsibilities and decision authority to prevent scope creep, unclear ownership, and escalation bottlenecks — the three most common governance failure modes observed in legacy system migrations.

Headcount: **6 permanent roles + 1 advisory role**. All roles are AI-augmented.

---

## 2. Team Structure

```
Executive Sponsor (APHA / Crown Commercial)
│
├── Delivery Lead / Programme Manager
│   ├── Cloud Architect (Tech Lead)
│   │   ├── Senior Developer (C# / .NET)
│   │   ├── Developer (C# / .NET)
│   │   └── DevOps Engineer
│   └── QA Engineer
│
└── (Advisory) APHA Domain Expert / SME
```

---

## 3. Roles and Responsibilities

### 3.1 Executive Sponsor

| Attribute | Detail |
|-----------|--------|
| **FTE allocation** | 0.1 FTE (c. 4 hours/week) |
| **Reporting line** | Crown Commercial / APHA programme board |
| **Decision authority** | Approves go/no-go gates; authorises budget changes > 10%; resolves escalated blockers |
| **Key responsibilities** | Champions programme at senior level; removes organisational blockers; signs off each phase gate |
| **Escalation trigger** | Any blocker unresolved for > 5 business days; any budget variance > 10%; go/no-go gate disputes |

**AI Augmentation:**

| Tool | Use |
|------|-----|
| M365 Copilot | Drafts executive status summaries from Delivery Lead reports; summarises risk register for board updates |

---

### 3.2 Delivery Lead / Programme Manager

| Attribute | Detail |
|-----------|--------|
| **FTE allocation** | 1.0 FTE |
| **Decision authority** | Manages scope, schedule, and budget within phase; escalates to Sponsor when thresholds are breached |
| **Key responsibilities** | Owns programme plan; facilitates weekly team ceremonies; tracks risk register; manages stakeholder communication; chairs go/no-go gates; produces weekly status report |
| **Does NOT decide** | Technical architecture; module sequencing; test coverage thresholds |

**AI Augmentation:**

| Tool | Use | Expected Productivity Uplift |
|------|-----|------------------------------|
| M365 Copilot | Drafts weekly status reports, meeting minutes, risk register updates from bullet-point notes | 30–40% reduction in governance admin time |
| GitHub Copilot Chat | Queries codebase for progress evidence (e.g., "which modules have test coverage > 80%?") | Reduces status-gathering interruptions to developers |

---

### 3.3 Cloud Architect (Tech Lead)

| Attribute | Detail |
|-----------|--------|
| **FTE allocation** | 1.0 FTE |
| **Decision authority** | Final technical authority within programme scope; escalates to Sponsor only if technical decision requires additional budget |
| **Key responsibilities** | Owns target architecture; reviews and approves all PRs touching cross-cutting concerns (auth, session, data access patterns); produces and maintains ADRs; defines module boundaries; mentors developers on .NET 10 patterns |
| **Does NOT decide** | Programme budget; go/no-go gates (recommends, does not decide unilaterally) |

**AI Augmentation:**

| Tool | Use | Expected Productivity Uplift |
|------|-----|------------------------------|
| GitHub Copilot | Accelerates C# scaffolding for module shell structure, DI wiring, interface definitions | 25–35% faster module skeleton delivery |
| GitHub Copilot Chat | Queries legacy VB.NET codebase to extract method signatures, coupling points, and stored procedure call graphs without manual read-through | Reduces discovery time by 40–50% vs manual code reading |
| M365 Copilot | Drafts ADR documents from architect notes; formats decision rationale | 50% reduction in ADR authoring time |

---

### 3.4 Senior Developer (C# / .NET)

| Attribute | Detail |
|-----------|--------|
| **FTE allocation** | 1.0 FTE |
| **Decision authority** | Implementation decisions within an assigned module slice |
| **Key responsibilities** | Implements migration slices for complex domains (CaseManagement, ADNS, OSS); writes unit and integration tests; raises PRs; participates in code review; flags coupling risks to Tech Lead |
| **Expected profile** | C# 10+ experience; familiarity with Dapper, ASP.NET Core, and Docker; able to read VB.NET |

**AI Augmentation:**

| Tool | Use | Expected Productivity Uplift |
|------|-----|------------------------------|
| GitHub Copilot | Auto-completes C# translations of VB.NET methods; generates Dapper query wrappers for stored procedures; suggests unit test cases | 35–45% faster implementation per slice |
| GitHub Copilot Chat | Explains legacy VB.NET code behaviour; identifies all callers of a method across the codebase | Eliminates manual grep sessions; saves 1–2 hours per complex method translation |

---

### 3.5 Developer (C# / .NET)

| Attribute | Detail |
|-----------|--------|
| **FTE allocation** | 1.0 FTE |
| **Decision authority** | Implementation decisions within an assigned module slice |
| **Key responsibilities** | Implements migration slices for lower-complexity domains (Search, Batch, ReferenceData, AuditLog); writes unit tests; raises PRs; participates in code review |
| **Expected profile** | C# 8+ experience; able to follow established patterns; willingness to learn .NET 10 features |

**AI Augmentation:**

| Tool | Use | Expected Productivity Uplift |
|------|-----|------------------------------|
| GitHub Copilot | Generates boilerplate C# from VB.NET originals; suggests test cases; autocompletes DI registration | 30–40% faster implementation per slice |
| GitHub Copilot Chat | Explains unfamiliar patterns in legacy code; asks targeted questions about specific classes | Reduces dependency on Senior Developer explanation time |

---

### 3.6 DevOps Engineer

| Attribute | Detail |
|-----------|--------|
| **FTE allocation** | 1.0 FTE (100% Phase 0; 0.5 FTE from Phase 2 onward) |
| **Decision authority** | CI/CD pipeline design; container and infrastructure configuration |
| **Key responsibilities** | Builds and maintains GitHub Actions pipelines; maintains Dockerfile and `docker-compose.yml`; provisions container hosting environment; manages secrets (no plaintext credentials in source control — see Runbook security note); configures Application Insights; maintains branch protection rules |
| **Security obligation** | Ensure the plaintext SQL Server password in `legacy/BSESystem/Web.config` (`DBConnectionString`) is never committed in migrated code; replace with environment-variable injection from a secrets store (Azure Key Vault or GitHub Secrets) |

**AI Augmentation:**

| Tool | Use | Expected Productivity Uplift |
|------|-----|------------------------------|
| GitHub Copilot | Generates GitHub Actions YAML, Dockerfile steps, Kubernetes manifests from natural language descriptions | 40–50% faster pipeline authoring |
| GitHub Copilot Chat | Diagnoses CI failures; explains container networking issues | Reduces mean time to resolve pipeline failures |

---

### 3.7 QA Engineer

| Attribute | Detail |
|-----------|--------|
| **FTE allocation** | 1.0 FTE |
| **Decision authority** | Test strategy and acceptance of test coverage metrics |
| **Key responsibilities** | Authors and maintains integration test suite; builds output comparison harness (Phase 3); writes Playwright E2E tests (Phase 4); enforces DoD coverage thresholds in CI; participates in go/no-go gate sign-off |
| **Does NOT decide** | Whether a module is "done" without ≥ 80% branch coverage metric being met |

**AI Augmentation:**

| Tool | Use | Expected Productivity Uplift |
|------|-----|------------------------------|
| GitHub Copilot | Generates xUnit test stubs from method signatures; suggests edge cases for validation logic | 35–45% faster test authoring |
| GitHub Copilot Chat | Asks "what are the boundary conditions for `CheckMandatoryFields`?" against legacy code | Reduces requirements-extraction time from domain code |

---

### 3.8 APHA Domain Expert / SME (Advisory)

| Attribute | Detail |
|-----------|--------|
| **FTE allocation** | 0.2 FTE (c. 8 hours/week) |
| **Decision authority** | Final authority on regulatory output acceptability (ADNS format, OSS export correctness) |
| **Key responsibilities** | Reviews ADNS and OSS output comparison results (Phase 3); participates in UAT (Phase 4); accepts or rejects cutover readiness at Phase 4 gate |
| **Does NOT decide** | Technical implementation; test coverage standards |

**AI Augmentation:**

| Tool | Use |
|------|-----|
| M365 Copilot | Summarises technical test results into plain-English acceptance reports for SME review |

---

## 4. AI Augmentation Boundaries

GitHub Copilot and M365 Copilot are productivity accelerators. The following constraints apply to all AI tool use on this programme:

| Constraint | Rationale |
|-----------|-----------|
| Copilot output must be reviewed by a human developer before merge | AI generates; humans decide and are accountable |
| Copilot must not generate credentials, connection strings, or secrets | Security obligation — see Runbook note on `DBConnectionString` |
| Copilot suggestions referencing legacy patterns (static classes, InProc session) must be rejected | Migration objective is to eliminate these patterns |
| Copilot-generated test cases must be validated for correctness against known legacy outputs | AI-generated tests that assert wrong behaviour are worse than no tests |
| M365 Copilot summaries for external stakeholders must be reviewed by Delivery Lead | Accuracy and sensitivity — government communications |

---

## 5. Accountability Matrix (RACI)

| Decision | Sponsor | Delivery Lead | Tech Lead | Sr Dev | Dev | DevOps | QA | SME |
|----------|---------|---------------|-----------|--------|-----|--------|----|-----|
| Go/no-go gate approval | **A** | **R** | C | — | — | — | C | C |
| Architecture decisions | I | I | **A/R** | C | — | C | — | — |
| Module implementation | — | I | A | **R** | **R** | — | C | — |
| Test coverage gate | — | I | C | C | C | — | **A/R** | — |
| ADNS/OSS output acceptance | I | I | C | — | — | — | C | **A/R** |
| Pipeline configuration | — | I | C | — | — | **A/R** | — | — |
| Budget change > 10% | **A** | **R** | C | — | — | — | — | — |
| Cutover approval | **A** | **R** | C | — | — | C | C | C |

*A = Accountable, R = Responsible, C = Consulted, I = Informed*

---

## 6. Escalation Path

```
Developer / DevOps / QA
  │  (unresolved blocker > 1 business day)
  ▼
Tech Lead
  │  (unresolved blocker > 3 business days; or budget/scope impact)
  ▼
Delivery Lead
  │  (unresolved blocker > 5 business days; or budget variance > 10%)
  ▼
Executive Sponsor
```

All escalations must be documented in the risk register with the date raised, resolution action, and date resolved.

---

## 7. Team Ceremonies

| Ceremony | Cadence | Attendees | Purpose |
|----------|---------|-----------|---------|
| Sprint Planning | Fortnightly | All team | Commit slice deliverables for next sprint |
| Daily Stand-up | Daily | Dev team (Tech Lead, Devs, QA, DevOps) | Blockers, progress, pair-up |
| PR Review | Async (within 24h of raise) | Tech Lead + one peer | Code quality, pattern compliance |
| Phase Gate Review | End of each phase | All team + Sponsor + SME | Go/no-go decision |
| Risk Review | Monthly | Delivery Lead + Tech Lead | Risk register update |
| Stakeholder Update | Fortnightly | Delivery Lead + Sponsor | Status report delivery |

---

*Next document: [Risk-and-Governance.md](Risk-and-Governance.md)*
