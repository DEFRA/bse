# BSE HLD/HLSA — TSES Connectivity Design Revision

> **Status:** Both source documents have been updated directly:
> - `BSE_High Level Design_v1.0.docx` — updated with real Word **Track Changes** (author: "Copilot (TSES Connectivity Revision)"). Open in Word with Review > Track Changes/All Markup shown to see insertions (underlined) and deletions (strikethrough). Original preserved as `BSE_High Level Design_v1.0.ORIGINAL_BACKUP.docx`.
> - `BSE HLSA Presentation_BSE v3.pptx` — updated directly (PowerPoint has no native text track-changes mechanism for slide content), with each changed passage suffixed `[REVISED]`/`[REVISED - TSES Connectivity]` for reviewer visibility. Original preserved as `BSE HLSA Presentation_BSE v3.ORIGINAL_BACKUP.pptx`.
> `Figures 1, 3, 5` in the HLD and the equivalent **embedded diagram image** in the HLSA (slide 18) are raster graphics that cannot be edited by automated tooling — see Section 9 for exact redraw instructions.
> **Date:** 2026 (revision)
> **Note on naming:** the HLD/HLSA source documents refer to the external database as "TSE" (`tses.data.prd1.prd.cerespfm.cloud`); this is the same system referred to as "TSES" in the design brief. Updated text uses "TSE (TSES)" throughout for clarity.
> **Change driver:** Replace direct ADF-to-TSES hub connectivity (via the Fortinet firewall shown in the diagrams) with a Self-Hosted Integration Runtime (SHIR) hosted on a Defra Azure VM.

---

## 1. Summary of Changes Applied

| # | HLD Section | Location | Change Type | Description |
|---|---|---|---|---|
| 1 | 2.3.1 System Architecture — Overview | Narrative paragraph | Tracked text update | Replaced "ADF integrates through express route... to synchronise data from TSE to BSE" with the SHIR-based narrative and IBM whitelisting statement |
| 2 | 2.3.1 Step 3/4 — Integration with ADF | Bullet list | Tracked text update | "Scheduled ingestion from external TSE SQL systems" and "ADF connects using Managed Identity and Private Endpoints" bullets revised to describe the SHIR path |
| 3 | 2.3.3 Data Architecture — Data flow: TSE to BSE | Narrative paragraph | Tracked text update | Full data-flow description rewritten: ADF dispatches to SHIR on Defra VM → SHIR connects outbound to TSE(TSES)/AWS RDS → IBM whitelist → data returned |
| 4 | 2.3.4 Business Architecture — Overview narrative | Narrative paragraph | Tracked text update | "ADF serves as the integration layer... ingesting surveillance data from the external TSE SQL Database" revised to describe SHIR intermediary |
| 5 | 3.2.1 Network Security — Table 22 (Firewall Security Rules), rule `Outbound-OnPrem-SQL` | Table row | Tracked text update | Rule renamed `Outbound-SHIR-to-TSES`; Source cell updated to "Defra VM (SHIR) / Data Sync"; Destination updated to "TSES DB / AWS RDS (IBM AWS — reached only after IBM whitelists the SHIR VM's outbound IP)"; Notes updated to reference SHIR and IBM subnet whitelisting |
| 6 | 3.2.1 Network Security — Table 24 (Egress Path) | Table row | Tracked text update | Source/Destination/Via cells updated: "ADF (via SHIR on Defra VM)" → "TSES DB (AWS RDS, IBM AWS)" via "Defra VM egress → IBM firewall whitelist" |
| 7 | 2.3.2 Azure Service Inventory — Table 11 (Data services), "Azure Data Factory" row | Table cell | Tracked text update | Description extended to note TSE(TSES) ingestion is now routed via SHIR on a Defra VM rather than direct ADF managed-service connectivity |
| 8 | 9.1 Risks — R003 | Table row | Tracked text update | Risk title and mitigation updated to cover SHIR node HA, reserved outbound IP, and IBM whitelist dependency |
| 9 | 9.4 Dependencies — D005 | Table row | Tracked text update | Dependency description and acceptance criteria updated to include Defra VM provisioning, SHIR registration, and IBM whitelisting of the VM's outbound IP per environment (Dev/PreProd/Prod subnet ranges) |
| 10 | Figure 1 — System Architecture diagram (HLD) | Embedded image | **Diagram redraw required** | Insert "Defra Azure VM — Self-Hosted Integration Runtime" box between the Fortinet firewall and "ADF — Time Scheduled Data Sync"; relabel the connection to TSE External SQL DB as SHIR-mediated |
| 11 | Figure 5 — Security Architecture diagram (HLD) | Embedded image | **Diagram redraw required** | Same as Figure 1 — this diagram duplicates the System Architecture layout, including the Fortinet/TSE connectivity path |
| 12 | Figure 3 — Business Architecture diagram (HLD) | Embedded image | **Diagram redraw required** | Insert the SHIR/Defra VM box on the arrow between "Integration service [Azure Data Factory]" and "TSE Database [SQL Server] (tses.data.prd1.prd.cerespfm.cloud)"; update callout 3 wording |
| 13 | Slide 18 — "Cloud Architecture with Hub-Spoke pattern" narrative (HLSA) | Text box, callout 4 | **Applied directly** | "Data Synchronisation via ADF" description rewritten to describe the SHIR/Defra VM path and IBM whitelisting instead of the direct Fortinet route; suffixed `[REVISED - TSES Connectivity]` |
| 14 | Slide 18 — embedded diagram image `image17.png` (HLSA) | Embedded image | **Diagram redraw required** | Same diagram as HLD Figures 1/5 (`TSE External SQL DB → Fortinet → ADF`); insert the Defra VM (SHIR) box as described for Figures 1/5 above |
| 15 | Slide 35 — Dependencies row **D-003** "Accessing TSE database" (HLSA) | Table row (title/description/mitigation) | **Applied directly** | Title, description and mitigation rewritten to describe SHIR/Defra VM provisioning, registration, and IBM whitelisting of the VM's outbound IP; suffixed `[REVISED]` |
| 16 | Slide 41 — "Firewalls & Security Rules" table (HLSA) | Table rows | **Applied directly** | Same edits as HLD Tables 22/24: `Outbound-OnPrem-SQL` → `Outbound-SHIR-to-TSES`; `ADF IR / Data Sync` → `Defra VM (SHIR) / Data Sync`; `On-prem SQL (via VPN/ER)` → `TSES DB / AWS RDS (IBM AWS...)`; egress-path `ADF IR`/`On-prem SQL`/`Hub (VPN/ER)` cells relabelled to the SHIR/IBM-whitelist path. The unrelated row 210 (`App Subnet, ADF IR` → Azure SQL access) was intentionally left unchanged. |

All docx text edits above (#1–#9) have been applied directly to `BSE_High Level Design_v1.0.docx` as real Word **Track Changes** (Review ribbon → Track Changes → All Markup) so they can be accepted/rejected individually.
All pptx text edits above (#13, #15, #16) have been applied directly to `BSE HLSA Presentation_BSE v3.pptx` — PowerPoint has no native inline track-changes mechanism for slide text, so each changed passage is suffixed `[REVISED]` for reviewer visibility; the pre-edit file is preserved as `BSE HLSA Presentation_BSE v3.ORIGINAL_BACKUP.pptx` for comparison.
Items #10–#12 and #14 require manual redraws in Visio/PowerPoint since they are embedded raster images, not editable text/shapes accessible to automated tooling.

---

## 2. Architecture Narrative — Data Synchronisation

**[MODIFIED]**

~~Azure Data Factory performs scheduled data preparation and synchronization tasks, connecting securely to the Azure SQL database through Private Endpoints, while connectivity to the external on-premises database is routed securely via the hub through the Fortinet appliance.~~

Azure Data Factory performs scheduled data preparation and synchronisation tasks, connecting securely to the Azure SQL database through Private Endpoints. Connectivity to the external TSES database (hosted on AWS RDS within IBM's AWS infrastructure) is **no longer routed directly from the ADF managed service via hub connectivity**. Instead, a dedicated **Self-Hosted Integration Runtime (SHIR)**, installed on a Defra-provisioned Azure Virtual Machine within the BSE spoke network, acts as the intermediary. ADF triggers pipeline activities that execute through the SHIR, which initiates the outbound connection to the TSES database. IBM whitelists the Defra VM's outbound IP address at their AWS security group/firewall layer to permit this connection.

---

## 3. New Component — Self-Hosted Integration Runtime (SHIR)

**[NEW SECTION]**

**Purpose:** Enables Azure Data Factory to reach the TSES database (AWS RDS, hosted in IBM's AWS environment) without direct ADF-managed-service-to-external-network connectivity, aligning with Defra's network segregation principles.

**Component details:**
- **Host:** Dedicated Azure Virtual Machine provisioned within the Defra Azure environment (BSE spoke VNet)
- **Software:** Microsoft Integration Runtime (Self-Hosted mode), registered against the BSE Azure Data Factory instance
- **Registration:** SHIR node registered via authentication key generated in ADF; node status monitored via ADF Studio "Integration Runtimes" blade
- **Outbound connectivity:** VM has a defined, static/reserved outbound IP (via NAT Gateway or fixed Public IP/Azure Firewall SNAT range) that IBM registers in their AWS security group allow-list
- **Inbound connectivity:** No inbound access required from IBM/AWS side — connection is Defra-initiated (outbound only), reducing external attack surface
- **High availability:** Recommend minimum 2 SHIR nodes in a shared IR group for resilience; single-VM deployment is a single point of failure (see Risks)
- **Patching/maintenance:** VM OS and SHIR software patching is a Defra-managed operational responsibility (not covered by Azure PaaS SLA)

---

## 4. Data Flow Description

**[MODIFIED]** Revised flow (replaces prior ADF → TSE Database direct flow):

1. Azure Data Factory triggers a scheduled pipeline for data synchronisation with the TSES database.
2. The pipeline activity is dispatched to the **Self-Hosted Integration Runtime** running on the Defra Azure VM (rather than executing via the ADF Azure Integration Runtime/managed service).
3. The SHIR, from within the Defra Azure environment, initiates an **outbound** connection to the TSES database (AWS RDS) using the connection string/credentials configured in the Linked Service.
4. The outbound request reaches IBM's AWS environment; **IBM's firewall/security group allows the connection only because the Defra VM's IP has been explicitly whitelisted.**
5. Data is retrieved/pushed and returned via the same path: TSES → IBM Firewall → SHIR (Defra VM) → ADF → BSE Database.
6. No direct network path exists between the ADF managed service (or the CCoE hub) and the TSES database — **all traffic must traverse the Defra VM.**

**Expected Connectivity Flow:**
```
Azure Data Factory → Self-Hosted Integration Runtime (Defra Azure VM) → IBM Firewall Whitelist → TSES Database (AWS RDS)
```

---

## 5. Firewall & Network Security Requirements

**[NEW SECTION]**

| Requirement | Detail |
|---|---|
| Outbound whitelisting | IBM must whitelist the Defra Azure VM's outbound public/NAT IP address on their AWS security group governing TSES RDS access |
| Target IBM subnet ranges (per environment) | **Development:** `10.218.82.0/24`, `10.218.83.0/24`, `10.218.84.0/24` — **Pre-Production:** `10.218.50.0/24`, `10.218.51.0/24`, `10.218.52.0/24` — **Production:** `10.218.18.0/24`, `10.218.19.0/24`, `10.218.20.0/24` |
| Rationale for subnet ranges | RDS IP is dynamically assigned within these subnets in each IBM AWS account/environment; all three AZ ranges per environment must be permitted on the BSE Azure Firewall/NSG **outbound** rule to TSES, since the RDS instance IP may fall in any of them |
| NSG on SHIR VM subnet | Restrict outbound to only the required TSES database port and only to the listed IBM CIDR ranges — deny all other outbound by default |
| Inbound to SHIR VM | No inbound rule required from IBM; SHIR VM should not be internet-facing for inbound traffic |
| Credential/certificate storage | TSES database credentials used by the Linked Service should be stored in Azure Key Vault, referenced by ADF — not embedded in the VM/SHIR configuration |

### 5.1 Firewall Changes — Defra Side vs. IBM Side

**[NEW SECTION]**

| # | Change | Owner | Detail |
|---|---|---|---|
| 1 | Provision Defra Azure VM with a **static/reserved outbound IP** (via NAT Gateway, dedicated Public IP, or Azure Firewall SNAT range) | **Defra** | Ensures the whitelisted IP does not change; document the IP as a managed/change-controlled asset |
| 2 | Create **outbound NSG/Azure Firewall rule** on the SHIR VM's subnet permitting egress only to the IBM TSES subnet CIDRs (per environment) on the required database port | **Defra** | Deny-all-else outbound rule; scoped per environment (Dev/Pre-Prod/Prod) |
| 3 | Remove/decommission the **existing direct hub-to-TSES firewall rule** used by the ADF managed service (previous hub connectivity approach) | **Defra** | Ensures ADF managed service can no longer reach TSES directly, per the "no direct connectivity" requirement |
| 4 | Register the Defra VM's outbound IP with ADF's Self-Hosted Integration Runtime configuration (linked service update) | **Defra** | Configuration change, not a firewall change, but required for the new path to function |
| 5 | Add an **inbound allow rule on IBM's AWS security group** (attached to the TSES RDS instance / VPC) permitting the Defra VM's whitelisted outbound IP, on the RDS listener port | **IBM** | This is the core "IBM whitelisting" action — must be applied per environment (Dev/Pre-Prod/Prod), since RDS IP is dynamic within each environment's AZ subnets |
| 6 | Confirm IBM's security group does **not** retain the previous rule permitting the ADF hub's IP range (legacy hub connectivity) | **IBM** | Cleanup step — old direct-hub rule should be removed once cutover to SHIR is validated, to fully close the previous path |
| 7 | Provide confirmation/change record once whitelisting is applied in each environment, including effective date and rule reference | **IBM** | Required for Defra's change control record and go-live sign-off per environment |

**Sequencing note:** Item 5 (IBM inbound whitelist) must be completed **before** SHIR-based connectivity testing begins in each environment; Item 3/6 (removal of old hub rules) should only happen **after** the new path is validated end-to-end, to avoid an outage gap.

---

## 6. Assumptions & Dependencies

**[NEW ASSUMPTIONS]**
- The Defra Azure VM hosting the SHIR will be assigned a static/reserved outbound IP address that does not change without prior notice to IBM.
- IBM will complete their firewall whitelisting change within an agreed lead time ahead of each environment's go-live (Dev/Pre-Prod/Prod).
- The three AZ subnet ranges per environment provided by IBM are current and will be kept up to date if IBM's AWS network changes.

**[NEW DEPENDENCIES]**
- IBM Cloud/AWS network team to action outbound IP whitelisting request per environment before cutover.
- Defra Azure VM must be provisioned, hardened, and SHIR installed/registered against ADF **before** the corresponding environment's data synchronisation testing begins.
- Coordination required between Defra network team (Azure NSG/Firewall config) and IBM network team (AWS security group config) for each environment.

---

## 7. Risks & Operational Considerations

**[NEW SECTION]**

| Risk | Impact | Mitigation |
|---|---|---|
| Single VM / single SHIR node = single point of failure | ADF-to-TSES sync fails entirely if VM is down | Deploy 2+ SHIR nodes in a shared IR group; monitor node health via ADF alerts |
| VM patching/OS maintenance is now a Defra operational responsibility | Unpatched VM = security exposure; missed patching = downtime risk | Include VM in existing patch management cadence; schedule maintenance windows outside sync windows |
| Defra VM outbound IP changes (e.g. VM rebuild, NAT reconfiguration) | Breaks connectivity until IBM re-whitelists | Use a stable NAT Gateway/reserved IP, not VM's default dynamic public IP; document IP in change control |
| IBM whitelist change delays | Blocks go-live/testing in an environment | Raise whitelisting requests with adequate lead time per environment; track as a formal cross-team dependency |
| Credential/connection string management on SHIR host | Credential exposure risk if stored locally | Use Key Vault-backed Linked Service credentials; do not store TSES credentials directly on the VM |

---

## 8. Security Considerations

**[NEW NOTE]** This design **improves** alignment with Defra's network segregation principles compared to the direct-hub-connectivity model: the ADF managed service (multi-tenant, outside Defra's direct network control) no longer has a direct path to the TSES database. All traffic is funneled through a single, Defra-owned, hardened VM with an auditable, whitelisted egress path — providing a clearer network segregation boundary and a single point for monitoring/logging outbound TSES traffic (via NSG flow logs / Azure Firewall logs).

---

## 9. Diagram Update Instructions

The HLD contains **three embedded diagram images** that show the ADF ↔ TSES connectivity and must be redrawn (they are raster PNGs embedded in the docx — not editable by automated tooling):

### Figure 1 — "System Architecture" (Section 2.3.1) and Figure 5 — "Security Architecture" (Section 3.1)
These two figures use the **same layout** (Figure 5 duplicates Figure 1). Both currently show:
`TSE External SQL DB (tses.data.prd1.prd.cerespfm.cloud) → Fortinet → "Data Pull (Scheduled)" → ADF - Time Scheduled Data Sync (attached with BSE DB Private IP)`, labelled step **4** "Additional Data Preparation & Sync", with a dashed "Secured via Fortinet" annotation.

**Required changes (apply identically to both figures):**
- Insert a new box **"Defra Azure VM — Self-Hosted Integration Runtime (SHIR)"** on the connection line between the **Fortinet** icon and the **"ADF - Time Scheduled Data Sync"** box.
- Redraw the arrow direction/order as: `ADF (step 4) → Defra Azure VM (SHIR) → Fortinet/IBM whitelist → TSE External SQL DB`.
- Replace the "Secured via Fortinet" / "Data Pull (Scheduled)" caption with: *"SHIR — outbound-only, IBM-whitelisted egress to TSE (TSES) subnet ranges."*
- Add the new VM box to the diagram legend/component list if the diagram has one.

### Figure 3 — "Business Architecture" (Section 2.3.4)
Currently shows: `Integration service [Azure Data Factory] → (callout 3) → TSE Database [SQL Server] (tses.data.prd1.prd.cerespfm.cloud)`, with callout 3 reading *"Defra preferred Cloud integration service (Azure Data Factory) fetches data from TSE Database and transform the surveillance data."*

**Required changes:**
- Insert a new component box **"Defra Azure VM — Self-Hosted Integration Runtime"** on the arrow between the Azure Data Factory box and the TSE Database box.
- Update callout **3** text to: *"Azure Data Factory dispatches the pipeline to a Self-Hosted Integration Runtime on a Defra Azure VM, which connects outbound to the TSE (TSES) Database; IBM whitelists the VM's outbound IP to permit the connection."*

### HLSA PowerPoint (`BSE HLSA Presentation_BSE v3.pptx`) — Slide 18, `image17.png`
Confirmed: slide 18 ("High Level To-Be: Cloud Architecture with Hub-Spoke pattern") embeds the **identical diagram** to HLD Figures 1/5 (`TSE External SQL DB (tses.data.prd1.prd.ceresptm.cloud) → Fortinet → "Data Pull (Scheduled)" → ADF - Time Scheduled Data Sync`, step **4** "Additional Data Preparation & Sync"). Apply the same required changes listed above for Figures 1/5 to this image. The slide's supporting narrative text (callout 4) has already been updated directly; only the embedded diagram graphic itself still needs a manual redraw.

---

## 10. Change-Tracking Convention — Status

- **HLD (`BSE_High Level Design_v1.0.docx`):** All text-level changes listed in Section 1 (rows #1–#9) have been applied directly using native Word **Track Changes** (`w:ins`/`w:del`, author "Copilot (TSES Connectivity Revision)"). Open in Word with *Review → Track Changes → Display for Review: All Markup* to see insertions (underlined) and deletions (strikethrough) inline, and use *Accept/Reject* per change. The pre-edit version is preserved as `BSE_High Level Design_v1.0.ORIGINAL_BACKUP.docx`.
- **HLSA (`BSE HLSA Presentation_BSE v3.pptx`):** All text-level changes listed in Section 1 (rows #13, #15, #16) have been applied directly. PowerPoint has no native inline track-changes mechanism for slide body text (unlike Word), so each changed passage is suffixed `[REVISED]` / `[REVISED - TSES Connectivity]` for reviewer visibility instead. The pre-edit version is preserved as `BSE HLSA Presentation_BSE v3.ORIGINAL_BACKUP.pptx` for side-by-side comparison.
- **Diagrams (HLD Figures 1, 3, 5 and HLSA slide 18's `image17.png`):** Not modified — these are embedded raster images outside the reach of automated text tooling. Use the redraw instructions in Section 9 above; keep the current diagrams as an "As-Is" appendix and add the revised versions as "To-Be (Revised)", consistent with the phase-gate documentation pattern in `docs/Risk-and-Governance.md`.

