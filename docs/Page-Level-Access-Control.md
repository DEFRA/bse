# BSE – Page-Level Access Control

**Generated from:** migrated .NET 10 codebase (`BSE.Host`, `BSE.Modules.UserManagement`)  
**Authority files:** `Program.cs` (policy definitions), `GroupClaimsTransformation.cs` (group → role mapping), `UserGroup.cs` (enum)

---

## 1. User Groups

The following groups are defined in `luUserGroup` (database) and in `UserGroup.cs`:

| Group Name | Enum Value | Description |
|---|---|---|
| DEFRA Viewer | `ReadOnly (3)` | Read-only access across the application |
| DEFRA Data Entry | `DataEntry (2)` | Read + data entry access |
| DEFRA Maintenance | `DEFRAMaintenance (4)` | Read + data entry + DEFRA maintenance access |
| VLA Data Entry | `DataEntry (2)` | Read + data entry access (VLA staff) |
| VLA Maintenance | `Admin (1)` | Full access including administration |
| DEFRA AI Wales Scotland | `ReadOnly (3)` | Read-only access |
| DEFRA AHO User | `ReadOnly (3)` | Read-only access |

---

## 2. Group → Policy Mapping

Policies are emitted as `ClaimTypes.Role` claims by `GroupClaimsTransformation.GetPoliciesForGroup()` and enforced in `Program.cs` via `AddAuthorization`.

| Group Name | Policies (Roles) Granted |
|---|---|
| DEFRA Viewer | `ReadOnly` |
| DEFRA Data Entry | `DataEntry`, `ReadOnly` |
| DEFRA Maintenance | `ReadOnly`, `DEFRAMaintenance`, `DataEntry` |
| VLA Data Entry | `DataEntry`, `ReadOnly` |
| VLA Maintenance | `DataEntry`, `ReadOnly`, `Admin`, `DEFRAMaintenance` |
| DEFRA AI Wales Scotland | `ReadOnly` |
| DEFRA AHO User | `ReadOnly` |
| _(unrecognised / no group)_ | _(none – access denied)_ |

> **Note:** Policies are additive and cumulative. A group with `DataEntry` can access all pages requiring `DataEntry` and also all pages requiring only authentication.

---

## 3. Authorization Levels Used in the Application

| Level | How Applied | Who Can Access |
|---|---|---|
| **Anonymous** | `AllowAnonymousToPage(...)` in `Program.cs` | Anyone (no login required) |
| **Authenticated** | `AuthorizeFolder("/")` global convention or `[Authorize]` attribute | Any successfully logged-in user who belongs to a recognised group |
| **DataEntry** | `[Authorize(Policy = "DataEntry")]` | Groups: DEFRA Data Entry, DEFRA Maintenance, VLA Data Entry, VLA Maintenance |
| **Admin** | `[Authorize(Policy = "Admin")]` | Groups: VLA Maintenance only |

---

## 4. Page-Level Access Matrix

### Legend

| Symbol | Meaning |
|---|---|
| ✅ | Access granted |
| ❌ | Access denied |
| 🔓 | Public – no login required |

### 4.1 Public / Error Pages

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| Error | `/Error` | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 |
| Session Error | `/SessionError` | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 |

### 4.2 Home

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| Home | `/Home` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

### 4.3 Admin Pages  _(requires `Admin` policy)_

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| Users | `/Admin/Users` | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Pick Lists | `/Admin/PickLists` | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Pick List Edit | `/Admin/PickListEdit` | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |

### 4.4 ADNS Export Pages  _(Menu: Authenticated; Ci / Gb / Ni: `DataEntry`)_

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| ADNS Export Menu | `/AdnsExport/Menu` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ADNS Export – CI | `/AdnsExport/Ci` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| ADNS Export – GB | `/AdnsExport/Gb` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| ADNS Export – NI | `/AdnsExport/Ni` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |

### 4.5 Audit Log Pages  _(Authenticated)_

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| Audit – By Date | `/AuditLog/ByDate` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Audit – By User | `/AuditLog/ByUser` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Audit – Case Moves | `/AuditLog/CaseMoves` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Audit – CPHH Changes | `/AuditLog/CphhChanges` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Audit – New Farms | `/AuditLog/NewFarms` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Audit – RBSE Changes | `/AuditLog/RbseChanges` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

### 4.6 BSESS Integration Pages  _(Authenticated)_

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| Check By Date | `/Bsess/CheckByDate` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Check By RBSE | `/Bsess/CheckByRbse` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

### 4.7 Case Management Pages  _(Details / Lookup: Authenticated; others: `DataEntry`)_

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| Case Details | `/Case/Details` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Case Lookup | `/Case/Lookup` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| New Case | `/Case/New` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Edit Case | `/Case/Edit` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Delete Case | `/Case/Delete` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Move Case | `/Case/MoveCase` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| RBSE Change | `/Case/RbseChange` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |

### 4.8 Case Work Pages  _(Menu / Minute: Authenticated; Entry: `DataEntry`)_

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| CaseWork Menu | `/CaseWork/Menu` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| CaseWork Minute | `/CaseWork/Minute` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| CaseWork Entry | `/CaseWork/Entry` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |

### 4.9 Farm Management Pages  _(Details / Lookup: Authenticated; others: `DataEntry`)_

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| Farm Details | `/Farm/Details` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Farm Lookup | `/Farm/Lookup` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| New Farm | `/Farm/New` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Edit Farm | `/Farm/Edit` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| CPHH Change | `/Farm/CphhChange` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |

### 4.10 OSS Export Pages  _(Authenticated)_

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| OSS Export Menu | `/OssExport/Menu` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

### 4.11 Search Pages  _(Authenticated)_

| Page | URL Path | DEFRA Viewer | DEFRA Data Entry | DEFRA Maintenance | VLA Data Entry | VLA Maintenance | DEFRA AI Wales Scotland | DEFRA AHO User |
|---|---|---|---|---|---|---|---|---|
| Search Cases | `/Search/Cases` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Search Cases by Herdmark | `/Search/CasesByHerdmark` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Search Cases by Holding Herdmark | `/Search/CasesByHoldingHerdmark` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Search Farms | `/Search/Farms` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Outstanding Data | `/Search/Outstanding` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Related Animals | `/Search/RelatedAnimals` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## 5. Summary by Group

| Group | Pages Accessible |
|---|---|
| **DEFRA Viewer** | Home, all Search pages, all Audit Log pages, all BSESS pages, Case Details, Case Lookup, Farm Details, Farm Lookup, CaseWork Minute, ADNS Export Menu, OSS Export Menu, Error, Session Error |
| **DEFRA AI Wales Scotland** | _(same as DEFRA Viewer)_ |
| **DEFRA AHO User** | _(same as DEFRA Viewer)_ |
| **DEFRA Data Entry** | All pages accessible to DEFRA Viewer **+** ADNS Export (Ci, Gb, Ni), Case (New, Edit, Delete, MoveCase, RbseChange), CaseWork (Menu, Entry), Farm (New, Edit, CphhChange) |
| **VLA Data Entry** | _(same as DEFRA Data Entry)_ |
| **DEFRA Maintenance** | _(same as DEFRA Data Entry — DEFRAMaintenance policy is not yet assigned to any page)_ |
| **VLA Maintenance** | All pages accessible to DEFRA Data Entry **+** Admin pages (Users, PickLists, PickListEdit) |

---

## 6. How Access Control Works (Technical Summary)

1. **Global Folder Convention** – `options.Conventions.AuthorizeFolder("/")` in `Program.cs` requires authentication for every page by default.
2. **Anonymous exceptions** – `Error` and `SessionError` pages are explicitly whitelisted via `AllowAnonymousToPage`.
3. **Policy attributes** – Individual page models override the default with `[Authorize(Policy = "PolicyName")]` (e.g., `"DataEntry"`, `"Admin"`).
4. **Policy definitions** – Four named policies are registered in `AddAuthorization`: `Authenticated`, `ReadOnly`, `DataEntry`, `Admin`, `DEFRAMaintenance`.
5. **Role emission** – On each authenticated request, `GroupClaimsTransformation` looks up the user's `UserGroup` from the database and emits the corresponding set of `ClaimTypes.Role` claims, which ASP.NET Core uses to evaluate the policy.
6. **Changing access** – To change a user's access level, update `[User].UserGroup` in the SQL Server database. No code changes are required.

---

## 7. Source Code References

| Concern | File |
|---|---|
| Group enum definition | `src/BSE.SharedKernel/UserGroup.cs` |
| Group → policy mapping | `src/BSE.Modules.UserManagement/Identity/GroupClaimsTransformation.cs` |
| Policy definitions | `src/BSE.Host/Program.cs` |
| Page-level `[Authorize]` attributes | `src/BSE.Host/Pages/**/*.cshtml.cs` |
