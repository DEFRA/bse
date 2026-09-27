# BSE HLD – TSES Connectivity Update (SHIR-based design)

## 1. Scope of update

This revision updates only the sections of the HLD required to reflect the revised connectivity model for TSES integration:

- architecture narrative
- data flow description
- network/security requirements
- infrastructure component description
- assumptions, dependencies, risks, and operational considerations
- diagram redraw guidance

The prior design described direct Azure Data Factory (ADF) managed-service connectivity to the TSES database via the hub connectivity path. This revision replaces that with a Defra-hosted Self-Hosted Integration Runtime (SHIR) model.

---

## 2. Revised architecture narrative

Azure Data Factory performs scheduled data preparation and synchronisation tasks, connecting securely to the Azure SQL database through the platform controls in the Azure environment. Connectivity to the external TSES database, hosted in IBM AWS infrastructure, is no longer routed directly from the ADF managed service via the hub connectivity path.

Instead, a dedicated Self-Hosted Integration Runtime (SHIR) is installed and configured on a new Azure Virtual Machine provisioned in the Defra Azure environment. Azure Data Factory dispatches pipeline execution to this SHIR node, and the SHIR initiates the outbound connection to the IBM-hosted TSES database. IBM will whitelist the outbound IP address of the Defra VM before the connection is permitted. This keeps the database traffic path under Defra operational control while requiring IBM to grant firewall approval at the TSES database boundary.

Expected connectivity flow:

Azure Data Factory → Self-Hosted Integration Runtime (Defra Azure VM) → IBM firewall whitelist → TSES database (AWS RDS)

---

## 3. Revised infrastructure component description

### 3.1 Defra Azure VM for SHIR

A new Azure Virtual Machine will be provisioned within the Defra Azure environment, inside the appropriate spoke network aligned to the BSE solution landing zone. This VM hosts the Microsoft Self-Hosted Integration Runtime (SHIR) and acts as the controlled outbound egress point for ADF-to-TSES database connectivity.

### 3.2 SHIR registration and operation

- Microsoft Integration Runtime (Self-Hosted mode) is installed on the Defra Azure VM.
- The SHIR is registered with the BSE Azure Data Factory instance using the ADF-managed registration key.
- ADF pipelines are configured to execute through the SHIR runtime instead of using the default ADF managed service connectivity path.
- The SHIR VM is the only permitted egress point to the TSES database from the BSE integration platform.
- No inbound IBM-to-Defra connection is required for normal operation; the connection is outbound from the Defra VM to IBM AWS.

---

## 4. Revised data flow description

1. Azure Data Factory triggers a scheduled pipeline for TSES data synchronization.
2. The pipeline activity is dispatched to the Self-Hosted Integration Runtime hosted on the Defra Azure VM.
3. The SHIR initiates an outbound connection to the IBM-hosted TSES database (AWS RDS), using the configured linked service and credentials.
4. The request reaches IBM’s AWS security boundary, where IBM applies the whitelist for the Defra VM outbound IP address.
5. The database connection is permitted only after IBM confirms the allow-list entry is in place.
6. Data is retrieved or written via the same path: TSES database → IBM firewall/security group → Defra VM (SHIR) → ADF → BSE database.
7. No direct peer-to-peer connection exists between ADF managed service and TSES; all communication must traverse the Defra-hosted VM.

---

## 5. Firewall and network security requirements

### 5.1 Defra-side requirements

- Provision a Defra Azure VM dedicated to the SHIR.
- Assign a stable outbound public IP or NAT Gateway solution so the egress IP does not change unexpectedly.
- Configure Azure NSG and/or Azure Firewall rules to allow outbound access only to the IBM TSES database endpoint and required port(s).
- Restrict outbound access to the IBM subnet ranges for the relevant environment.
- Deny all other outbound traffic by default.
- Store database credentials in Azure Key Vault and reference them from ADF rather than storing them directly on the VM.

### 5.2 IBM-side requirements

- IBM must whitelist the Defra VM outbound IP address on the AWS security group or firewall protecting the TSES database.
- IBM must permit access from the Defra VM IP to the relevant TSES database port for the active environment.
- IBM must ensure the relevant environment subnet ranges are allowed for the database access path.
- IBM should confirm the legacy direct hub-based allow-list is removed or not relied upon once the SHIR model is validated.

### 5.3 IBM subnet ranges by environment

- Development: 10.218.82.0/24, 10.218.83.0/24, 10.218.84.0/24
- Pre-Production: 10.218.50.0/24, 10.218.51.0/24, 10.218.52.0/24
- Production: 10.218.18.0/24, 10.218.19.0/24, 10.218.20.0/24

These subnet ranges should be considered when configuring Defra outbound restrictions and IBM allow-listing.

---

## 6. Assumptions and dependencies

### Assumptions

- The Defra Azure VM hosting the SHIR will be assigned a stable outbound IP address.
- IBM will complete whitelisting before environment testing or go-live.
- The IBM subnet ranges supplied for each environment remain valid and current.
- The ADF linked service will use the SHIR runtime for TSES access.

### Dependencies

- Defra VM provisioning and hardening
- SHIR installation and registration with Azure Data Factory
- IBM security group/firewall change to whitelist the VM IP
- Defra network team and IBM network team coordination per environment
- Key Vault access configuration for credentials and linked service configuration

---

## 7. Risks and operational considerations

| Risk | Impact | Mitigation |
|---|---|---|
| Single SHIR VM = single point of failure | ADF-to-TSES sync fails if the VM is unavailable | Use a shared SHIR group with 2+ nodes where possible, and monitor node health |
| VM outbound IP changes | IBM whitelist becomes invalid and connectivity breaks | Use a reserved/static IP or NAT Gateway configuration |
| IBM whitelist delay | Testing or cutover is blocked | Raise dependency early and validate per environment |
| VM patching/OS maintenance | Security exposure or downtime risk | Include VM in the platform patch cycle and maintenance windows |
| Credential exposure | Security risk if credentials are embedded on the VM | Store credentials in Azure Key Vault and reference via Linked Service |

---

## 8. Security considerations

This design aligns with Defra network segregation principles by ensuring that ADF does not connect directly to the external IBM-hosted TSES database. Instead, all TSES database communication is routed through a Defra-owned VM and SHIR node that is under Defra operational control, with only the approved outbound IP being allowed by IBM.

This reduces the exposure footprint, improves network control, and makes the egress path auditable and traceable through Azure firewall and NSG logs.

---

## 9. Diagram update guidance

The architecture and network diagrams should be updated to show the revised traffic path:

- ADF
- Defra Azure VM running Self-Hosted Integration Runtime
- IBM firewall whitelist
- TSES database (AWS RDS)

The previous direct ADF → hub/Fortinet → TSES connectivity should be removed from the diagrams and replaced with the SHIR-mediated outbound path. The direct external connectivity route should no longer be shown as an active design pattern.

### Diagram changes required

- Update system architecture diagram
- Update security architecture diagram
- Update business architecture diagram
- Update network connectivity diagram and deployment context diagram if they show direct ADF-to-TSES access

---

## 10. Summary of changes made

This revision updates the original HLD as follows:

1. Replaced direct ADF-managed-service connectivity with Defra-hosted SHIR connectivity.
2. Added the new Defra Azure VM hosting the Self-Hosted Integration Runtime.
3. Updated the TSES integration flow to show IBM whitelisting of the VM outbound IP.
4. Added Defra-side and IBM-side firewall/security requirements.
5. Added environment-specific IBM subnet ranges and egress restriction guidance.
6. Updated assumptions, dependencies, risk, and operational considerations.
7. Confirmed alignment with Defra network segregation and security principles.

---

## 11. Change tracking notes

This is the text-based HLD update to reflect the approved design change. The original binary Office document remains the authoritative source for final sign-off; the updates above capture the exact architecture changes required to the HLD narrative and diagram logic.
