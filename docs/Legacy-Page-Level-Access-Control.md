# BSE Legacy – Page-Level Access Control

**Source:** Legacy .NET 4.0 ASP.NET WebForms codebase (`C:\Workspace\bsenet-v2-2025-10\BSESystem`)  
**Authority files:** `Home.aspx.vb` (menu visibility per group), individual `*.aspx.vb` page code-behinds (`EnableControls` sub), `VLAHeader.ascx.vb` (authentication entry point)

---

## 1. User Groups

Groups are stored in the `luUserGroup` database table and loaded at login via the `GetUserByNTLogin` stored procedure. The group name string is stored in `Session(SessionVars.SV_HeaderGroupName)` and checked on every page.

| Group Name | Description |
|---|---|
| DEFRA Viewer | Read-only access – can view cases and search |
| DEFRA Data Entry | Read + case data entry for DEFRA |
| DEFRA Maintenance | DEFRA data entry + maintenance operations (ADNS export, CPHH/RBSE changes, Move/Delete case, Final Result, Pick Lists) |
| VLA Data Entry | VLA-specific data entry (OSS export, Print Batch, Pick Lists, case entry tabs) |
| VLA Maintenance | Full access including User Maintenance, CaseWork, Non-GB case creation |
| Others (e.g., DEFRA AI Wales Scotland, DEFRA AHO User) | Unrecognised group names – most pages redirect to `SearchMenu.aspx` |

---

## 2. Authentication Entry Point

`VLAHeader.ascx.vb` (`GetUserDetails` method) runs on every page load:

1. Reads the Windows NT Login of the current user.
2. Calls `BSELib.clsUser.GetUserByNTLogin` (stored procedure `GetUserByNTLogin`).
3. If the user is **not found** in the database → redirects to `unauthorized.htm`.
4. Stores the user's name, group name, user ID, and email in `Session`.

Every page then checks `Session(SessionVars.SV_HeaderGroupName)` in its own `EnableControls` sub and redirects accordingly.

---

## 3. Access Control Notation

| Symbol | Meaning |
|---|---|
| ✅ | Access granted (page loads for this group) |
| ✅ RO | Access granted – read-only mode (controls disabled/hidden) |
| ✅ RW | Access granted – read/write mode |
| ✅ VM only RW | Only VLA Maintenance has edit/delete rights; others can only view |
| ❌ →Home | Redirected to `Home.aspx` (access denied) |
| ❌ →Search | Redirected to `SearchMenu.aspx` (access denied) |
| 🔓 | Public – no group or authentication check |
| — | No explicit group check; page is session-protected but open to all authenticated users |

> **"Others"** = any group name not matching the five known groups (e.g., DEFRA AI Wales Scotland, DEFRA AHO User). These are treated as unrecognised by individual pages.

---

## 4. Home Page Menu Visibility

`Home.aspx` is accessible to all five groups. However, the menu links visible on the home page differ by group. Menu items are hidden/disabled via the `EnableControls → DEFRAViewerEnable / VLAMaintenanceEnable` etc. methods.

| Home Page Menu Item | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance |
|---|---|---|---|---|---|
| RBSE Lookup Panel (Panel1) | ❌ | ❌ | ✅ | ✅ | ✅ |
| Batch Number Panel (Panel2) | ❌ | ❌ | ❌ | ✅ | ✅ |
| ADNS Export link | ❌ | ❌ | ✅ | ❌ | ✅ |
| OSS Export link | ❌ | ❌ | ❌ | ✅ | ✅ |
| Print Batch link | ❌ | ❌ | ❌ | ✅ | ✅ |
| Final Result Entry link | ❌ | ❌ | ✅ | ❌ | ✅ |
| CPHH Change link | ❌ | ❌ | ✅ | ❌ | ✅ |
| RBSE Change link | ❌ | ❌ | ✅ | ❌ | ✅ |
| Move Case link | ❌ | ❌ | ✅ | ❌ | ✅ |
| Delete Case link | ❌ | ❌ | ✅ | ❌ | ✅ |
| Pick List Maintenance link | ❌ | ❌ | ✅ | ✅ | ✅ |
| User Maintenance link | ❌ | ❌ | ❌ | ❌ | ✅ |
| CaseWork link | ❌ | ❌ | ❌ | ❌ | ✅ |

> DEFRA Viewer and DEFRA Data Entry see the Home page but with **no operational links** visible – they must navigate via the top header (Search, Audit Log, BSESS) which are always present in `VLAHeader.ascx`.

---

## 5. Full Page-Level Access Matrix

### 5.1 Public / Error Pages

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `SessionError.aspx` | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 |
| `AppError.aspx` | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 |
| `unauthorized.htm` | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 |

### 5.2 Core Navigation Pages

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others | Notes |
|---|---|---|---|---|---|---|---|
| `Home.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ →Search | Menu items vary — see Section 4 |
| `Help.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | No group check |
| `SearchMenu.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | No group check |

### 5.3 Search Pages

No group check on any search page — all authenticated users can access.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `SearchCase.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `SearchFarm.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `SearchCaseByHerdmark.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `SearchCPHH.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `SearchOutstandingData.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `SearchRelatedAnimal.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `ShowCase.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

### 5.4 Audit Log Pages

`AuditLogMenu.aspx` checks groups explicitly (all 5 allowed). The individual report pages have no group check themselves and are accessed via the menu.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `AuditLogMenu.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ →Search |
| `AuditLogByDate.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `AuditLogByUser.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `AuditLogCaseMoves.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `AuditLogCPHHChanges.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `AuditLogNewFarms.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `AuditLogRBSEChanges.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `CaseAuditLogReport.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ →Search |
| `FarmAuditLogReport.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ →Search |

### 5.5 BSESS Integration Pages

All five known groups are permitted; unrecognised groups are redirected to `SearchMenu.aspx`.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `BSESSMenu.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ →Search |
| `BSESSCheckByDate.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ →Search |
| `BSESSCheckByRBSE.aspx` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ →Search |

### 5.6 ADNS Export Pages

DEFRA Maintenance and VLA Maintenance only. All other named groups are redirected to `Home.aspx`.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `ADNSExportMenu.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `ADNSExportCI.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `ADNSExportGB.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `ADNSExportNI.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |

### 5.7 OSS Export Pages

VLA Data Entry and VLA Maintenance only. All DEFRA groups are redirected to `Home.aspx`.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `OSSExportMenu.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ✅ | ❌ →Search |
| `OSSExportBSE1.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ✅ | ❌ →Search |
| `OSSExportBSE1b.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ✅ | ❌ →Search |
| `OSSExportBSE1Download.aspx` | — | — | — | — | — | — |

### 5.8 Print Batch

VLA Data Entry and VLA Maintenance only.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `PrintBatch.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ✅ | ❌ →Search |

### 5.9 Case Maintenance Operations

DEFRA Maintenance and VLA Maintenance only. All other groups are redirected to `Home.aspx`.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `MoveCase.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `MoveCaseNewFarm.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `DeleteCase.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `CPHHChange.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `RBSEChange.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `FinalResultEntry.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `FinalResultConfirmation.aspx` | ❌ →Home | ❌ →Home | ✅ | ❌ →Home | ✅ | ❌ →Search |
| `MaintenanceConfirmation.aspx` | — | — | — | — | — | — |

### 5.10 Pick List Maintenance

DEFRA Maintenance, VLA Data Entry, and VLA Maintenance can access all Pick List Maintenance pages. VLA Maintenance additionally has **edit and delete** rights; DEFRA Maintenance and VLA Data Entry are in **read-only** view mode.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `PickListMaintenance.aspx` | ❌ →Home | ❌ →Home | ✅ RO | ✅ RO | ✅ VM only RW | ❌ →Search |
| `PickListMaintenanceAHO.aspx` | ❌ →Home | ❌ →Home | ✅ RO | ✅ RO | ✅ VM only RW | ❌ →Search |
| `PicklistMaintenanceAHRO.aspx` | ❌ →Home | ❌ →Home | ✅ RO | ✅ RO | ✅ VM only RW | ❌ →Search |
| `PickListMaintenanceBreed.aspx` | ❌ →Home | ❌ →Home | ✅ RO | ✅ RO | ✅ VM only RW | ❌ →Search |
| `PickListMaintenanceBSECounty.aspx` | ❌ →Home | ❌ →Home | ✅ RO | ✅ RO | ✅ VM only RW | ❌ →Search |
| `PickListMaintenanceRelationFate.aspx` | ❌ →Home | ❌ →Home | ✅ RO | ✅ RO | ✅ VM only RW | ❌ →Search |
| `PickListMaintenanceSupplier.aspx` | ❌ →Home | ❌ →Home | ✅ RO | ✅ RO | ✅ VM only RW | ❌ →Search |
| `PickListMaintenanceTestType.aspx` | ❌ →Home | ❌ →Home | ✅ RO | ✅ RO | ✅ VM only RW | ❌ →Search |
| `PickListMaintenanceTSETestingSite.aspx` | ❌ →Home | ❌ →Home | ✅ RO | ✅ RO | ✅ VM only RW | ❌ →Search |

### 5.11 User Maintenance

VLA Maintenance only. All other groups — including DEFRA Maintenance — are redirected to `Home.aspx`.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `UserMaintenance.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ❌ →Search |

### 5.12 Non-GB Case Creation

VLA Maintenance only.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `NonGBCaseCreation.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ❌ →Search |

### 5.13 CaseWork Pages

VLA Maintenance only. All other groups are redirected to `Home.aspx`.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `CaseWorkMenu.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ❌ →Home |
| `CaseWorkEntry.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ❌ →Home |
| `CaseWorkOpenReport.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ❌ →Home |
| `CaseWorkClosedReport.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ❌ →Home |
| `CaseWorkMinuteConfirmation.aspx` | — | — | — | — | — | — |
| `ActiveMemo.aspx` | — | — | — | — | — | — |
| `ActiveMemoFallenStock.aspx` | — | — | — | — | — | — |
| `AnnexA.aspx` | — | — | — | — | — | — |
| `AnnexB.aspx` | — | — | — | — | — | — |
| `AnnexC.aspx` | — | — | — | — | — | — |
| `AnnexD.aspx` | — | — | — | — | — | — |

> CaseWork confirmation and minute pages have no group check of their own; they are only reachable through `CaseWorkEntry.aspx` which itself is VLA Maintenance only.

### 5.14 Case Entry Pages

Case entry is the DEFRA / VLA Data Entry workflow for entering case data across multiple tabs. **VLA Maintenance** is redirected to `SearchMenu.aspx` on all case entry pages — their workflow goes through `CaseWorkEntry.aspx` instead. DEFRA Viewer has read-only access (controls are disabled).

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others |
|---|---|---|---|---|---|---|
| `CaseEntryFarm.aspx` | ✅ RO | ✅ RW | ✅ RW | ✅ RW | ❌ →Search | ❌ →Home |
| `CaseEntryDEFRA.aspx` | ✅ RO | ✅ RW | ✅ RW | ✅ RW | ❌ →Search | — |
| `CaseEntryBAB.aspx` | ✅ RO | ✅ RW | ✅ RW | ✅ RW | ❌ →Search | ❌ →Home |
| `CaseEntryVLA.aspx` | ✅ RO | ✅ RW | ✅ RW | ✅ RW | ❌ →Search | ❌ →Home |
| `CaseEntryClinical.aspx` | ✅ RO | ✅ RW | ✅ RW | ✅ RW | ❌ →Search | — |
| `CaseEntryFeeds.aspx` | ✅ RO | ✅ RW | ✅ RW | ✅ RW | ❌ →Search | — |
| `CaseEntryRelations.aspx` | ✅ RO | ✅ RW | ✅ RW | ✅ RW | ❌ →Search | ❌ →Home |
| `CaseEntrySave.aspx` | — | — | — | — | — | — |

### 5.15 Farm Creation

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others | Notes |
|---|---|---|---|---|---|---|---|
| `NewFarm.aspx` | ❌ →Home | ✅ | ✅ | ❌ →Home | ✅ | ❌ →Search | DEFRA DE + DM, and VLA Maintenance |

### 5.16 Workflow Helper Pages (Popups / Pickers)

These pages assist case entry navigation. Access is validated via the calling page's workflow.

| Page (.aspx) | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | Others | Notes |
|---|---|---|---|---|---|---|---|
| `PickFarm.aspx` | ❌ →Home | ✅ | ✅ | ❌ →Home | ❌ →Search | ❌ →Search | DEFRA case entry farm-picker; VLA use different flow |
| `PickSireDam.aspx` | ✅* | ✅ | ✅ | ✅* | ❌ →Search | ❌ →Search | *DEFRA Viewer & VLA DE redirects are commented out in code |
| `PickSupplier.aspx` | ❌ →Home | ❌ →Home | ❌ →Home | ✅ | ✅ | ❌ →Search | VLA feeds entry helper |
| `ExcelExport.aspx` | — | — | — | — | — | — | Session-based; no group check |
| `RelationsPopup.aspx` | — | — | — | — | — | — | Session-based popup |
| `CalendarPopup.aspx` | — | — | — | — | — | — | No group check |
| `BatchNumberDisplayPopup.aspx` | — | — | — | — | — | — | No group check |
| `ExitConfirmationPopup.aspx` | — | — | — | — | — | — | No group check |
| `Redirect.aspx` | — | — | — | — | — | — | Utility redirect; no group check |

### 5.17 Print / Report Pages

Report pages are session-protected (require an RBSE number in session) but have no group check of their own. They are opened from within case entry or CaseWork.

| Page (.aspx) | Access |
|---|---|
| `ReportCaseFarm.aspx` | Session-protected — all authenticated users |
| `ReportClinical.aspx` | Session-protected — all authenticated users |
| `ReportFeeds.aspx` | Session-protected — all authenticated users |
| `ReportOffspring.aspx` | Session-protected — all authenticated users |
| `ReportPedigree.aspx` | Session-protected — all authenticated users |
| `ResultMemo.aspx` | Session-protected — all authenticated users |

---

## 6. Summary by Group

| Group | Pages Accessible |
|---|---|
| **DEFRA Viewer** | Home (view only), Search (all pages), Audit Log (all pages), BSESS (all pages), ShowCase, CaseEntry (all tabs, read-only), Help |
| **DEFRA Data Entry** | All DEFRA Viewer pages **+** CaseEntry (all tabs, writable), NewFarm, PickFarm, PickSireDam |
| **DEFRA Maintenance** | All DEFRA Data Entry pages **+** ADNSExportMenu, ADNSExportCI/GB/NI, MoveCase, MoveCaseNewFarm, DeleteCase, CPHHChange, RBSEChange, FinalResultEntry/Confirmation, PickList Maintenance (all variants, read-only), Non-GB creation ❌ |
| **VLA Data Entry** | Home (view+OSS/Print/PickList links), Search (all), Audit Log (all), BSESS (all), ShowCase, CaseEntry (all tabs, writable), OSSExportMenu/BSE1/BSE1b, PrintBatch, PickList Maintenance (read-only), PickSupplier, Help |
| **VLA Maintenance** | **All pages** except: CaseEntry tabs (uses CaseWork instead), PickFarm (uses different workflow). Full access to: UserMaintenance, NonGBCaseCreation, CaseWork (Menu/Entry/Reports/Minutes), ADNSExport, OSSExport, PrintBatch, all Maintenance ops, PickList Maintenance (edit/delete), NewFarm |
| **Others (unrecognised group)** | Home redirects to SearchMenu. Most pages redirect to SearchMenu. Search pages, Help, AuditLog sub-pages (no explicit check) remain accessible |

---

## 7. Key Architectural Notes

1. **No declarative authorization** – There are no `[Authorize]` attributes, `web.config` `<authorization>` rules, or ASP.NET Membership. All access control is imperative code inside each page's `Page_Load` → `EnableControls()` method.

2. **Group check pattern** – Every protected page follows the pattern:
   ```vb
   Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)
   If sGroupName = "DEFRA Viewer" Then
	   Response.Redirect("Home.aspx")    ' or Do Nothing
   ElseIf sGroupName = "DEFRA Data Entry" Then
	   ...
   ElseIf sGroupName = "VLA Maintenance" Then
	   ' Do Nothing  (access granted)
   Else
	   Response.Redirect("SearchMenu.aspx")  ' unknown group
   End If
   ```

3. **Two workflows for case editing:**
   - **DEFRA / VLA Data Entry:** `Home.aspx` → `ShowCase.aspx` → `CaseEntryFarm.aspx` → (tabs) → `CaseEntrySave.aspx`
   - **VLA Maintenance:** `Home.aspx` → `CaseWorkMenu.aspx` → `CaseWorkEntry.aspx` → (minutes/annexes)

4. **Changing a user's access:** Update `luUserGroup` / `UserGroup` column in the `[User]` database table. There is no code-change required, but the group name string must exactly match the hard-coded values in each page's `EnableControls` sub.

5. **PickSireDam anomaly:** The DEFRA Viewer and VLA Data Entry redirect statements exist in code but are **commented out**, meaning those groups can currently access `PickSireDam.aspx`. This appears to be an intentional or overlooked code decision.

---

## 8. Source Code References

| Concern | File |
|---|---|
| Authentication entry point | `BSESystem\VLAHeader.ascx.vb` – `GetUserDetails()` |
| User/group database lookup | `BSELib\clsUser.vb` – `GetUserByNTLogin()` |
| Session variable names | `BSESystem\SessionVars.vb` |
| Home page menu visibility | `BSESystem\Home.aspx.vb` – `EnableControls()`, `DEFRAViewerEnable()`, … |
| Per-page group check | Each `*.aspx.vb` – `EnableControls()` sub |
