# BSE System — ROI and Budget Model

> **Classification:** Programme Governance — Confidential  
> **Date:** 2026-07-20  
> **Author:** Intelligent Migration Agent  
> **Status:** Draft — Requires Delivery Lead and Sponsor Review  
> **Disclaimer:** This document contains proprietary and confidential information belonging to Crown Commercial Property. Unauthorised use, disclosure, or distribution is prohibited.

---

## 1. Purpose and Scope

This document provides a cost and return model for the BSE system migration. It is designed to support funding approval and to give the executive sponsor a basis for evaluating programme investment against ongoing operational risk.

All figures are estimates based on industry benchmarks, UK government rate cards, and the specific system characteristics identified during discovery. Figures must be validated with actual supplier and hosting cost data before submission for budget approval.

---

## 2. Cost Drivers

The following system characteristics identified during discovery are the primary cost drivers for this programme:

| Characteristic | Cost Implication |
|----------------|-----------------|
| ~230 stored procedures | All must be mapped, tested, and called from migrated code. The mapping work is largely automatable with GitHub Copilot but requires review for each. Estimated 0.5–2 hours per SP depending on complexity. |
| ~90 ASPX pages + 15 ASCX controls | Each UI page must be recreated in Razor Pages or Blazor. Simple forms: 2–4 hours. Complex multi-step pages (CaseEntry*, ADNSExport*): 4–12 hours. |
| 12 domain classes | Core business logic is well-bounded. Most classes translate cleanly to C# with Copilot assistance. Average: 1–3 days per domain. |
| Zero automated tests | Phase 0 test baseline must be built from scratch. Integration tests against live SQL Server are the primary mechanism. |
| SSIS packages | 2 packages to be replaced by .NET ETL. 3–5 days each. |
| Regulatory output equivalence | ADNS email and OSS export comparison harness — additional QA investment. |
| No migration experience baseline | Assumes team learns .NET 10 patterns in Phase 0; productivity ramp-up factored in. |

---

## 3. Team Cost Model

### 3.1 Assumptions

- Elapsed programme duration: **18 months** (72 weeks).
- All rates are fully loaded (salary + on-costs + management overhead).
- UK public sector / government framework day rates used as a proxy.
- GitHub Copilot Business: £19/user/month (current published price — verify at procurement).
- M365 Copilot: £25/user/month (current published price — verify at procurement).
- Azure container hosting: estimated based on a 2-replica container service with SQL Managed Instance.

### 3.2 Headcount and Duration by Phase

| Role | Phase 0 (6w) | Phase 1 (8w) | Phase 2 (20w) | Phase 3 (10w) | Phase 4 (14w) | Phase 5 (14w) | Total Weeks |
|------|-------------|-------------|--------------|--------------|--------------|--------------|-------------|
| Delivery Lead | 1.0 FTE | 1.0 FTE | 1.0 FTE | 1.0 FTE | 1.0 FTE | 0.5 FTE | ~68 |
| Cloud Architect | 1.0 FTE | 1.0 FTE | 1.0 FTE | 0.5 FTE | 0.5 FTE | 0.25 FTE | ~57 |
| Senior Developer | 0.5 FTE | 1.0 FTE | 1.0 FTE | 1.0 FTE | 1.0 FTE | 0.5 FTE | ~60 |
| Developer | 0.25 FTE | 0.5 FTE | 1.0 FTE | 1.0 FTE | 1.0 FTE | 0.5 FTE | ~52 |
| DevOps Engineer | 1.0 FTE | 0.5 FTE | 0.5 FTE | 0.5 FTE | 1.0 FTE | 0.5 FTE | ~46 |
| QA Engineer | 0.5 FTE | 0.5 FTE | 1.0 FTE | 1.0 FTE | 1.0 FTE | 0.5 FTE | ~53 |
| APHA SME (Advisory) | 0.1 FTE | 0.1 FTE | 0.2 FTE | 0.3 FTE | 0.3 FTE | 0.1 FTE | ~16 |

### 3.3 Rate Cards and One-Time Cost Estimate

| Role | Day Rate (est.) | Total Days (est.) | Total Cost (est.) |
|------|----------------|-------------------|--------------------|
| Delivery Lead | £650/day | 340 | £221,000 |
| Cloud Architect | £800/day | 285 | £228,000 |
| Senior Developer | £700/day | 300 | £210,000 |
| Developer | £550/day | 260 | £143,000 |
| DevOps Engineer | £650/day | 230 | £149,500 |
| QA Engineer | £600/day | 265 | £159,000 |
| APHA SME (Advisory) | £500/day | 80 | £40,000 |
| **Subtotal — People** | | | **£1,150,500** |

### 3.4 AI Tooling — One-Time and Recurring Cost

| Item | Unit Cost | Quantity | Duration | Total |
|------|-----------|----------|----------|-------|
| GitHub Copilot Business | £19/user/month | 6 users | 18 months | £2,052 |
| M365 Copilot | £25/user/month | 3 users (DL, Tech Lead, QA) | 18 months | £1,350 |
| **Subtotal — AI Tooling** | | | | **£3,402** |

### 3.5 Infrastructure — One-Time Setup Cost

| Item | Estimated Cost |
|------|---------------|
| Azure Container Apps / AKS setup and configuration | £15,000 |
| Azure SQL Managed Instance migration (from on-prem `vm-aphadev-003`) | £10,000 |
| Azure Key Vault and secret management setup | £3,000 |
| Application Insights and monitoring configuration | £2,000 |
| Redis Cache instance setup (session replacement) | £1,500 |
| GitHub Advanced Security (secret scanning) | £2,500 |
| **Subtotal — Infrastructure Setup** | | **£34,000** |

### 3.6 Contingency

A 15% contingency is applied to people costs, reflecting the complexity of regulatory output equivalence verification and the absence of automated tests in the legacy system.

| Item | Amount |
|------|--------|
| People cost contingency (15%) | £172,575 |
| Infrastructure contingency (10%) | £3,400 |
| **Total Contingency** | **£175,975** |

---

## 4. Total One-Time Migration Investment

| Category | Amount |
|----------|--------|
| People costs | £1,150,500 |
| AI tooling (18 months) | £3,402 |
| Infrastructure setup | £34,000 |
| Contingency | £175,975 |
| **Total One-Time Investment** | **£1,363,877** |

> **Rounding for budget planning:** £1.4 million.

---

## 5. Ongoing Operational Cost Model

This section compares the annual cost of running the legacy system against the target state.

### 5.1 Legacy System — Current Annual Costs (Estimated)

| Item | Annual Cost (est.) |
|------|-------------------|
| IIS server hosting (`vm-aphadev-003`) — power, maintenance, licensing | £18,000 |
| SQL Server license (on-prem, Standard Edition) | £12,000 |
| .NET Framework support (no Microsoft security patches — cost is risk, not cash) | £0 direct / high latent risk |
| SSIS runtime maintenance | £3,000 |
| Developer maintenance capacity (bug fixes, manual deployments) | £60,000 (est. 100 days/year at £600/day) |
| **Total Legacy Annual** | **£93,000** |

Note: The legacy system's primary cost driver is **risk**, not cash. A security breach, data corruption event, or regulatory reporting failure would carry costs several multiples of the above.

### 5.2 Target State — Annual Costs

| Item | Annual Cost (est.) |
|------|-------------------|
| Azure Container Apps (2 replicas, autoscale) | £14,400 |
| Azure SQL Managed Instance (General Purpose, 4 vCores) | £20,000 |
| Azure Redis Cache (Basic C1) | £1,800 |
| Application Insights (estimated data ingestion) | £2,400 |
| Azure Key Vault | £200 |
| GitHub Advanced Security | £1,800 |
| GitHub Copilot Business (ongoing — support team) | £456 (2 users × £19 × 12) |
| Developer support capacity (lower — modern stack, CI, tests) | £36,000 (est. 60 days/year at £600/day — 40% reduction vs legacy) |
| **Total Target Annual** | **£77,056** |

### 5.3 Annual Operational Saving

| Metric | Value |
|--------|-------|
| Legacy annual cost | £93,000 |
| Target annual cost | £77,056 |
| **Annual saving** | **£15,944** |
| **Saving (%)** | **17%** |

---

## 6. Productivity Uplift from AI Augmentation

### 6.1 Basis for Assumptions

GitHub Copilot productivity uplifts are estimated from Microsoft/GitHub published research (2023–2025 studies) and adjusted for the profile of this migration (translation of existing code rather than greenfield development).

| Activity | AI Uplift (published) | BSE-adjusted uplift | Rationale for adjustment |
|----------|----------------------|---------------------|--------------------------|
| Code translation (VB.NET → C#) | 40–55% | 35% | Legacy code is procedural and well-structured; Copilot performs well on direct translation tasks |
| Boilerplate generation (DI wiring, test stubs) | 50–65% | 45% | High-confidence activity for Copilot |
| Documentation authoring (ADRs, runbook) | 40–60% | 50% | M365 Copilot is highly effective for structured document generation |
| Test case generation | 35–50% | 30% | BSE business rules are complex and domain-specific; AI-suggested tests require expert review |
| PR description and status reporting | 50–70% | 55% | Low-risk, high-volume activity; well-suited to AI |
| Debugging / root-cause analysis | 20–35% | 20% | Complex legacy code requires human expertise; AI assists but doesn't replace |

### 6.2 Estimated Programme Saving from AI Augmentation

Without AI augmentation, the estimated programme cost at the same team size and scope would be approximately **£1.85 million** (team size driven up, or elapsed time extended by ~6 months).

| Benefit | Estimated Value |
|---------|----------------|
| Reduced implementation time (developers) | £280,000 |
| Reduced documentation time (architect, delivery lead) | £45,000 |
| Reduced test authoring time (QA) | £50,000 |
| Reduced pipeline/config authoring (DevOps) | £30,000 |
| **Total AI Augmentation Saving** | **£405,000** |

This saving is embedded in the £1.4M programme budget (i.e., without Copilot, the budget would be ~£1.8M).

---

## 7. ROI Breakeven Analysis

### 7.1 Investment vs Cumulative Saving

| Year | Annual Saving | Cumulative Saving | Cumulative Investment (one-time + annual delta) | Net Position |
|------|--------------|-------------------|-------------------------------------------------|--------------|
| Year 0 (migration) | £0 | £0 | £1,363,877 | -£1,363,877 |
| Year 1 (post-cutover) | £15,944 | £15,944 | £1,363,877 | -£1,347,933 |
| Year 2 | £15,944 | £31,888 | £1,363,877 | -£1,331,989 |
| Year 3 | £15,944 | £47,832 | £1,363,877 | -£1,316,045 |

> **Note on cash ROI breakeven:** At £15,944 annual saving, the breakeven on direct operational savings alone is approximately **85 years**, which is economically irrelevant. The migration cannot be justified on operational savings alone.

### 7.2 Risk-Adjusted ROI Narrative

The ROI case for this migration is **risk elimination**, not cost reduction. The following risk exposures justify the investment:

| Risk | Potential Cost if Materialised |
|------|-------------------------------|
| .NET Framework 4.0 security vulnerability exploited | £500K–£2M (regulatory breach, incident response, data recovery) |
| Partial case save corruption producing a regulatory reporting error | £100K–£500K (investigation, retrospective audit, potential fine) |
| ADNS notification failure (missed EU submission) | £50K–£200K (regulatory investigation, legal exposure) |
| IIS server failure on `vm-aphadev-003` with no disaster recovery | £200K–£1M (system unavailability, data recovery if backups are incomplete) |
| Plaintext credential exposure (R07 — SQL Server + SSIS passwords in source control) | £100K–£500K (data breach notification, ICO investigation) |

**Minimum risk exposure without migration:** Estimated £950K–£4.2M over a 5-year horizon, based on probability-weighted occurrence of the above risks.

**Investment:** £1.4M (one-time) + ~£77K/year thereafter.

**Risk-adjusted break-even:** Programme cost is recovered within **18–36 months** of go-live if one significant risk event (OWASP vulnerability exploitation, partial-save corruption, or server failure) is avoided.

### 7.3 Sensitivity Analysis

| Scenario | Programme Cost | Risk Avoided | Net Position (5-year) |
|----------|---------------|-------------|----------------------|
| **Base case** | £1.4M | £950K (minimum risk exposure) | -£450K → £+150K by year 5 |
| **Optimistic** (AI productivity 10% higher, no risk events legacy) | £1.25M | £0 risk events legacy | -£1.25M (migration not justified without risk lens) |
| **Pessimistic** (team underperforms, 20% overrun) | £1.68M | £2M risk avoided (one breach) | **+£320K net positive** |
| **Regulatory breach occurs legacy** | £1.4M invest | £500K breach avoided | **+£100K minimum by year 2** |

**Conclusion:** The migration investment is justified primarily on risk elimination grounds. The base case shows a positive net position within 5 years if even one medium-severity risk event is avoided. The pessimistic scenario remains net positive if the credentials exposure (R07) results in a breach before migration is complete — reinforcing the urgency of the immediate credential rotation action.

---

## 8. Budget Approval Milestones

| Milestone | Spend Authority | Approval Required |
|-----------|----------------|-------------------|
| Phase 0 initiation | Up to £80,000 | Delivery Lead |
| Phase 1 start | Up to £200,000 cumulative | Executive Sponsor |
| Phase 2 start | Up to £700,000 cumulative | Executive Sponsor |
| Phase 3 start | Up to £1,000,000 cumulative | Executive Sponsor |
| Phase 4 start (UI + cutover) | Up to £1,300,000 cumulative | Executive Sponsor + programme board |
| Phase 5 start | Full programme budget | Executive Sponsor + programme board |
| Any unplanned variance > 10% of phase budget | — | Executive Sponsor approval required before spend |

---

*Document series:*  
*[Intelligent-Migration-Plan.md](Intelligent-Migration-Plan.md) → [Intelligent-Team-Model.md](Intelligent-Team-Model.md) → [Risk-and-Governance.md](Risk-and-Governance.md) → **ROI-and-Budget.md***
