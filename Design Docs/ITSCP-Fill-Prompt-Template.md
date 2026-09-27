# Reusable Prompt — Fill "IT Service Continuity Plan Template v2.0" for a New Project

Use this prompt in a new chat session when you need to complete the DR/Service Continuity plan for a
different service. Attach the two reference files first, then paste the prompt below (customise the
bracketed placeholders).

## Files to attach / have in the workspace
1. `IT Service Continuity Plan Template v2.0.docx` (the blank ITSCP template)
2. `Defra Group DDTS Technical Resilience Standards.docx` (tiering/RTO-RPO reference standard)
3. The project's own architecture documentation (HLD / HLSA / Target Architecture / design docs) —
   whatever describes hosting model, regions, redundancy, backup strategy, and integrations.

## Prompt to use

```
Based on the [PROJECT NAME] HLD/HLSA (and any other architecture docs in this workspace), fill in the
"IT Service Continuity Plan Template v2.0.docx" completely, clearly, and simply.

Use "Defra Group DDTS Technical Resilience Standards.docx" as the reference for:
- Determining the correct Service Tier (1a/1b/2/3/4) based on the service's actual redundancy pattern
  (region vs zonal redundancy, active-active vs active-passive, single datacentre, etc.)
- The corresponding RTO/RPO targets, availability %, and test cycle for that tier

IMPORTANT: the architecture's redundancy pattern is only a starting-point guide for the tier — the
service's actual assigned tier is a business decision (based on a Business Impact Assessment) and may
not match what the architecture pattern alone would suggest. Always ask the user to confirm the
service's assigned tier explicitly before filling in RTO/RPO/availability/test-cycle values, and treat
that confirmed tier as authoritative even if it differs from the architecture-inferred one (e.g. a
zone-redundant service can still be officially Tier 3, not Tier 2, if that's what the business has
assessed) — just note the mismatch rather than overriding the confirmed tier.

For every section of the template, fill in real values derived from the architecture docs:
- Cover page (service name, plan/template version, author, approver, revision details)
- 2.1 Continuity Overview (RTO/RPO, region/AZs, backup schedule & retention, DR ownership, outage
  instructions)
- 2.2 Architectural Diagram (reference the relevant diagram/figure from the HLD)
- 2.3 Single Points of Failure (identify real SPOFs from the architecture — single-node components,
  single-region deployment, external/third-party dependencies, shared platform resources — with
  mitigations)
- 2.4 Upstream/Downstream Services
- 2.5 Infrastructure Components (list every major component with criticality, HA status, region,
  failover process, contact)
- 3.1 Backup and Recovery Strategies (per data store / service)
- 3.2 Vendor Support and SLA
- Shared Responsibility model (only where a component is hosted on a CSP outside the primary cloud,
  e.g. an external AWS-hosted dependency)
- Technical Recovery Plan ownership/review cadence
- 4.2 User Acceptance Test Cases (concrete, service-specific test rows — not generic examples)
- 5. Disaster Recovery Comms Plan (a real incident-to-resolution flow for this service)

Where a fact genuinely isn't available from the docs (e.g. a named approver, a vendor contact, a
scheduled test date), leave a clearly marked placeholder like "[Service Owner to confirm]" rather than
guessing — do not invent names, dates, or contract details.

After filling it in:
1. Validate the resulting .docx opens correctly (well-formed OPC package, no corrupted XML).
2. Do a thorough gap check — scan every table for cells that are still blank or only partially filled,
   and list them explicitly.
3. Give me a summary of what was filled per section, and a clear list of remaining gaps/placeholders
   that need my input (names, contacts, dates, sign-offs).
4. Explicitly list what should be tested as part of DR testing for this specific service (concrete,
   numbered test scenarios — failover, restore, redeploy-from-IaC, end-to-end UAT, comms/escalation
   drill — tailored to this service's actual components, not generic boilerplate).
```

## Notes on how this was implemented (for reference)
- The template is a `.docx` with mostly two-column label/value table rows, and a few wider
  header-row + blank-data-row tables (Vendor Support, Infrastructure Components, Upstream/Downstream,
  Shared Responsibility, UAT Test Cases). Extra data rows were added by cloning the blank data row.
- Edits were made by unzipping the `.docx` (it's an OOXML zip package), editing `word/document.xml`
  directly via .NET `XmlDocument`, then repackaging the zip — no Word installation was required.
- After every edit pass, the XML was validated with `XmlReader` and the repackaged `.docx` was opened
  via `System.IO.Packaging.Package` to confirm it's a valid Office file before considering it done.
- A final audit pass programmatically scanned every table row for cells that were empty while sibling
  cells in the same row were filled — this catches missed columns (e.g. a wide table where only the
  first few columns were filled) that a manual read-through can miss.

## Worked example — BSE
- BSE's architecture (zone-redundant Azure SQL/Container Apps, single region) initially suggested Tier 2
  per the DDTS pattern table, but the Service Owner confirmed BSE is actually **Tier 3**. RTO/RPO,
  restoration testing frequency, and TRP review cadence were all updated to the Tier 3 values
  (RTO 8-48h, RPO 8-48h, test cycle every 3 years) once this was confirmed — a reminder to always get
  explicit tier confirmation rather than relying solely on the architecture pattern.
