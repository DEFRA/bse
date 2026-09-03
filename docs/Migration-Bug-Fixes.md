# Migration Bug Fixes â€” BSE .NET 10 Migrated Application

> Branch: `feature/gds-agent-on-migrated`  
> Date: August 2026

---

## 1. Home Page (`Pages/Home.cshtml` + `Pages/Home.cshtml.cs`)

### Error
Old screen content (legacy panels) was visible alongside the new GDS panels because `replace_string_in_file` appended new content instead of replacing it. The `.cs` file had a duplicate `HomeModel` class definition causing 40+ CS0102/CS0111 compiler errors. The `.cshtml` file had 448 lines (250 new + 198 old appended).

### Fix
- Truncated `Home.cshtml` to 250 lines (removed lines 251â€“448) using `[IO.File]::ReadAllLines` + `WriteAllLines`.
- Truncated `Home.cshtml.cs` to 117 lines (removed lines 118â€“212) using the same approach.
- Root cause: `replace_string_in_file` matched only the class opening brace and appended rather than replacing the full body. Full-file overwrite via PowerShell is required for large structural rewrites.

---

## 2. Home Page â€” RBSE Go Button Navigation

### Error
In the legacy system, entering an RBSE number on the Home page and clicking **Go** navigated to `CaseEntryFarm.aspx`, which showed **case + farm details combined** on one page. In the migrated app, the RBSE form pointed to `/Case/Lookup` which redirected to `/Case/Details` â€” showing case data only, with farm as a separate link.

### Fix
- Updated `Pages/Case/Details.cshtml.cs` to inject `IFarmService` alongside `ICaseService`.
- `OnGetAsync` now loads the farm record from `Case.Cphh` in parallel with case data.
- Updated `Pages/Case/Details.cshtml` to display the farm details section inline below the case details (Owner, Address, County, Parish, Herdmarks, AHO, Herd type, Dealer, Non-GB), matching the legacy combined view.
- Added a `View full farm details` link to `/Farm/Details` for full farm management.
- CPHH row in the case summary changed from a hyperlink to plain text (farm section below is the entry point).

---

## 3. Case Search Page (`Pages/Search/Cases.cshtml` + `.cs`)

### Error 1 â€” Spurious mandatory field validation errors on page load
On page load (before the user submits anything), the error summary showed:
```
The Sex field is required.
The Dbse field is required.
The Fate field is required.
The Notes field is required.
...
```

### Cause
ASP.NET Core 6+ treats every non-nullable `string` property as implicitly `[Required]` when `<Nullable>enable</Nullable>` is set in the project. The legacy .NET Framework WebForms app had no model binding or `ModelState` concept â€” all search filter fields were optional `Request.QueryString` reads. The migration correctly enabled nullable context but did not suppress this new framework behaviour.

### Fix â€” `Program.cs`
Chained `.AddMvcOptions` to `AddRazorPages` to suppress the implicit required behaviour globally:
```csharp
builder.Services.AddRazorPages(options => { ... })
	.AddMvcOptions(o =>
		o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
```

### Fix â€” `Pages/Search/Cases.cshtml`
Added `&& Model.Filter.HasSearched` guard to the error summary block so it only renders after a search attempt, not on initial page load:
```razor
@if (!ViewContext.ModelState.IsValid && Model.Filter.HasSearched)
```

---

### Error 2 â€” `ModelState.IsValid` guard blocking the search
The page model contained:
```csharp
if (!ModelState.IsValid)
{
	// Validation failed â€” do not run search
	return;
}
```
Because of Error 1 above, `ModelState.IsValid` was always `false`, so the search never executed and always returned 0 results.

### Fix â€” `Pages/Search/Cases.cshtml.cs`
Removed the `if (!ModelState.IsValid) { return; }` block entirely. The existing `HasAnyFilter()` guard correctly controls when to run the search without needing model validation.

---

### Error 3 â€” Search returning 0 results despite correct RBSE in DB

**Symptoms:** After fixing Errors 1 and 2, searching for `000000001` (which exists in the DB) still returned 0 results. Diagnostic logging confirmed:
```
CaseSearch: HasAnyFilter=true, Rbse='000000001', ModelStateValid=true, Errors=
CaseSearch: SP returned 0 rows
```
Manual execution of `GetSearchCase` SP with the same parameters returned 1 row.

**Cause:** Dapper's anonymous object parameter inference sends C# `string` values as `nvarchar` (Unicode) by default. The SP parameters are declared as `varchar` (ANSI). SQL Server's implicit conversion from `nvarchar` to `varchar` in a `LIKE` comparison can silently produce no matches. Similarly, `DateTime?` null values passed via anonymous objects may not reliably map to SQL `datetime` parameters declared without defaults.

### Fix â€” `BSE.Modules.Search/Repositories/SearchRepository.cs`
Replaced the anonymous object in `SearchCasesAsync` with `DynamicParameters` and explicit `DbType` values matching the SP declarations:
```csharp
var p = new DynamicParameters();
p.Add("RBSE",        q.Rbse,    DbType.AnsiString, size: 9);
p.Add("Eartag",      q.Eartag,  DbType.AnsiString, size: 35);
p.Add("DBSE",        q.Dbse,    DbType.AnsiString, size: 7);
// ... all string params as DbType.AnsiString
p.Add("EarliestFormADate", q.EarliestFormADate, DbType.DateTime);
// ... all date params as DbType.DateTime (null passed correctly as DBNull)
```
Added `using System.Data;` and `using Dapper;` to the file.

---

## 4. Mandatory Field Validation â€” Root Cause Summary

| Problem | Root Cause | Fix | Time Spent |
|---|---|---|---|
| All non-nullable `string` search fields shown as required on page load | ASP.NET Core 6+ implicit required for non-nullable reference types with `<Nullable>enable</Nullable>` | `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true` in `Program.cs` | â€” |
| Error summary renders on initial page load with no user input | `@if (!ModelState.IsValid)` in view has no guard for first load | Added `&& Model.Filter.HasSearched` condition | â€” |
| Search never executes when any filter is entered | `if (!ModelState.IsValid) { return; }` in page model always true due to implicit required | Removed the guard; `HasAnyFilter()` is sufficient | â€” |
| SP returns 0 rows despite matching data in DB | Dapper anonymous object sends `string` as `nvarchar`; SP expects `varchar` | Use `DynamicParameters` with `DbType.AnsiString` for all `varchar` SP params | â€” |
| SP returns 0 rows even after `DynamicParameters` fix | `DateTime?` ViewModel properties on a GET form generate `__Invariant` hidden fields with wrong name prefix â€” ASP.NET Core cannot bind them for nested models, causing `DateTime.MinValue` to be passed instead of `null` | Changed date ViewModel properties to `string?`; parse in `ToQuery()` with `DateTime.TryParseExact(..., "yyyy-MM-dd", ...)` | â€” |
| SP returns 0 rows via `Microsoft.Data.SqlClient` RPC but 1 row via inline `EXEC` SQL text | `Microsoft.Data.SqlClient` sets `ARITHABORT OFF` by default; SQL Server reuses a cached plan compiled under `ARITHABORT ON` (e.g. from SSMS), producing wrong results via parameter sniffing | Added `SET ARITHABORT ON` in `DapperRepository.OpenConnection()` â€” fixes all SPs in one place | **2 hrs** |
| SP returns 0 rows via `Microsoft.Data.SqlClient` RPC but 1 row via inline `EXEC` SQL text | `Microsoft.Data.SqlClient` sets `ARITHABORT OFF` by default; SQL Server reuses a cached plan compiled under `ARITHABORT ON` (e.g. from SSMS), producing wrong results via parameter sniffing | Added `SET ARITHABORT ON` in `DapperRepository.OpenConnection()` â€” fixes all SPs in one place | **2 hrs** |

---

## 4b. Case Search â€” `__Invariant` GET Form Binding Bug (Search Still 0 Rows After DynamicParameters Fix)

### Symptom
After applying the `DynamicParameters` fix (Error 3), search with only RBSE still returned 0 rows. Standalone Dapper tests against the DB returned 1 row correctly, ruling out a Dapper or SP issue.

### Root Cause
`<input type="date" asp-for="Filter.EarliestFormADate">` on a `DateTime?` property causes the Razor tag helper to emit a hidden `__Invariant` field:
```html
<input type="hidden" name="__Invariant" value="Filter.EarliestFormADate">
```
For **GET forms with nested model binding** (`Filter.*`), ASP.NET Core's `InvariantDateTimeModelBinder` expects the invariant marker to be named `Filter.__Invariant` (prefixed to match the bound object). The emitted top-level `__Invariant` name is not associated with `Filter`, so the binder cannot find it.

Result: the empty string `""` submitted for each date field fails to parse as `DateTime?`, and the property is left at `DateTime.MinValue` (`0001-01-01 00:00:00`) rather than `null`.

The SP's `BETWEEN` clause then evaluates:
```sql
ISNULL([Case].[FormADate], '1 Jan 1900') BETWEEN '0001-01-01' AND '0001-01-01'
-- and similarly for FinalResultDate and BirthDate
```
`DateTime.MinValue` pre-dates SQL Server's minimum datetime â€” every row fails the condition â†’ **0 rows returned**.

### Fix â€” `BSE.Host/Models/ViewModels/CaseSearchViewModel.cs`
Changed the six date filter properties from `DateTime?` to `string?`:
```csharp
public string? EarliestFormADate { get; set; }
public string? LatestFormADate { get; set; }
public string? EarliestFinalResultDate { get; set; }
public string? LatestFinalResultDate { get; set; }
public string? EarliestBirthDate { get; set; }
public string? LatestBirthDate { get; set; }
```
`<input type="date">` always submits in `yyyy-MM-dd` (ISO 8601, culture-invariant) regardless of browser locale. `ToQuery()` now parses them explicitly:
```csharp
private static DateTime? ParseDate(string? value) =>
    DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
        ? d : null;
```
Empty strings â†’ `null` â†’ SP uses its `ISNULL(â€¦, default)` fallback. Valid dates â†’ correctly typed `DateTime`.

Also updated `HasAnyFilter()` in `Cases.cshtml.cs` to use `!string.IsNullOrWhiteSpace(Filter.*Date)` instead of `.HasValue`.

---

## 4c. Case Search â€” `ARITHABORT OFF` / Microsoft.Data.SqlClient Plan Sniffing Bug (Search Still 0 Rows After All Previous Fixes)

**Time spent: 2 hours**

### Symptom
After fixing the `__Invariant` date binding bug, search still returned 0 rows. Raw diagnostic confirmed:
- Direct `SELECT COUNT(*)` via app connection â†’ **1 row** (DB correct, row exists)
- `EXEC GetSearchCase ...` as inline SQL text â†’ **1 row** (SP logic correct)
- `CommandType.StoredProcedure` via `System.Data.SqlClient` (PowerShell) â†’ **1 row**
- `CommandType.StoredProcedure` via `Microsoft.Data.SqlClient` (app) â†’ **0 rows**
- Dapper `QueryAsync<CaseSearchResult>` (which wraps the above) â†’ **0 rows**

### Root Cause
`Microsoft.Data.SqlClient` (the modern .NET SQL client library) sets **`ARITHABORT OFF`** on every connection by default. This differs from:
- The legacy `System.Data.SqlClient` (sets `ARITHABORT ON`)
- SSMS (sets `ARITHABORT ON`)

SQL Server caches query plans keyed on `SET` options. When `GetSearchCase` was previously executed under `ARITHABORT ON` (e.g. from SSMS), SQL Server cached that plan. When the app calls it via RPC with `ARITHABORT OFF`, SQL Server reuses the cached plan but executes it under different `SET` options â€” a classic **parameter sniffing / plan reuse mismatch** that silently returns wrong results (0 rows).

Calling via inline `EXEC` text bypasses the compiled SP plan cache, which is why it returned 1 row correctly.

### Fix â€” `BSE.Infrastructure/DapperRepository.cs`
Added a private `OpenConnection()` helper that issues `SET ARITHABORT ON` immediately after every connection is opened. All 6 query methods now use `OpenConnection()` instead of calling `Open()` directly:
```csharp
private IDbConnection OpenConnection()
{
    var connection = _connectionFactory.CreateConnection();
    connection.Open();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "SET ARITHABORT ON";
    cmd.ExecuteNonQuery();
    return connection;
}
```
This is a **single fix covering every SP in the entire application** â€” no changes to individual stored procedures required.

---

## 5. General Lessons Learned

| # | Lesson |
|---|---|
| 1 | `replace_string_in_file` on a class opening only appends â€” use full-file PowerShell overwrite for structural rewrites |
| 2 | `get_errors` can report stale Roslyn IntelliSense after a PowerShell file write â€” verify with `get_file` first |
| 3 | Hot reload does not apply `Program.cs` startup changes â€” always do a full Stop + Start after changing service registration |
| 4 | Dapper anonymous objects infer `string` â†’ `nvarchar`. Always use `DynamicParameters` with `DbType.AnsiString` for SP params declared as `varchar`/`char` |
| 5 | `DateTime?` SP params without SQL defaults must be passed as `DBNull` explicitly â€” `DynamicParameters` with `DbType.DateTime` handles `null` correctly |
| 6 | ASP.NET Core's implicit required validation is a breaking change from .NET Framework â€” suppress globally when migrating search/filter forms |
| 7 | `DateTime?` properties on a **GET form** with nested model binding (`Filter.*`) generate `__Invariant` hidden fields at the wrong level â€” use `string?` + manual `TryParseExact` in `ToQuery()` instead |
| 8 | `Microsoft.Data.SqlClient` sets `ARITHABORT OFF` by default unlike the old `System.Data.SqlClient` and SSMS â€” always issue `SET ARITHABORT ON` after opening a connection when targeting SQL Server via the new client library. Fix once in `DapperRepository.OpenConnection()`, not in every SP | **2 hrs** |
| 11 | `GroupClaimsTransformation` did not strip upstream `ClaimTypes.Role` / `bse:*` claims before emitting the DB-authoritative set, and guarded double-transformation on `bse:group` (a display string an Azure AD app registration could emit). Fix: (1) guard changed to `bse:groupId` (a claim exclusively owned by this transformation, never present in upstream tokens); (2) all existing `ClaimTypes.Role`, `bse:group`, and `bse:groupId` claims are removed from the cloned identity before the DB-authoritative set is added. | **0.5 hrs** |
| 10 | User Management list page showed enum names (`ReadOnly`, `Admin`) in the Group column instead of real `luUserGroup.Name` display strings (e.g. `"DEFRA Viewer"`). Root cause: `Users.cshtml` rendered `@u.UserGroup` (enum) instead of the display name. Fix: the page model already loads `UserGroups` from `ILookupDataService.GetUserGroupsAsync()` (real DB lookup via `GetluUserGroup` SP); the view now resolves the name via `Model.UserGroups.FirstOrDefault(g => g.Id == u.UserGroupId)?.Name` â€” authoritative DB values, no hardcoded mapping. | **0.5 hrs** |
| 12 | Multiple build failures in `BSE.Host`: (1) **CS0102** â€” duplicate `Bab` property in `BabModel` (`CaseBabRecord?` and `BabFormViewModel` both named `Bab`); removed the stale `CaseBabRecord?` property. (2) **CS0234** â€” `ClosedReport.cshtml` and `OpenReport.cshtml` referenced wrong namespace `BSE.Web.Pages.CaseWork.*`; corrected to `BSE.Host.Pages.CaseWork.*`. (3) **RZ1031** â€” free-floating `@(condition ? "selected" : "")` in `<option>` tag helper attribute areas in `Bab.cshtml`, `NewNonGb.cshtml`, `TestResults.cshtml`; converted to `selected="@(condition ? "selected" : null)"`. (4) **CS1061/CS8130** â€” invalid tuple-await pattern `var (a,b) = await (task1, task2)` in `Bab.cshtml.cs`, `Relations.cshtml.cs`, `Feeds.cshtml.cs`; replaced with `var (a,b) = (await task1, await task2)`. (5) **CS1739** â€” named argument `ADNSRegionId` in `NewNonGb.cshtml.cs` mismatched `AddFarmCommand` parameter `ADNSRegionID`; fixed casing. | **0.5 hrs** |
| 13 | **NU1903 â€” SSH.NET 2023.0.0 high-severity vulnerability** (GHSA-q939-rpr3-3284) in `BSE.Modules.ReferenceData.Tests`. SSH.NET was not directly referenced; it was a transitive dependency via `Testcontainers.MsSql 3.10.0` â†’ `Testcontainers` â†’ `SSH.NET 2023.0.0`. Fixed by upgrading `Testcontainers.MsSql` from `3.10.0` to `4.14.0`, which resolves the chain to `SSH.NET 2026.0.0` (patched). No source code changes required. | **0.25 hrs** |
| 14 | **CS1061 â€” `ILookupItem` does not contain a definition for `Code`** in `Pages/Farm/_FarmFormFields.cshtml` (lines 50, 158, 168). The `ILookupItem` interface in `BSE.SharedKernel` declared only `Id` and `Description`, but the concrete `LookupItem` record also exposes `Code`. The Farm form fields partial iterates over `IEnumerable<ILookupItem>` for County, PedigreeType, and AHO dropdowns and binds the option value to `.Code` (a string code, not an int ID). Fixed by adding `string Code { get; }` to the `ILookupItem` interface â€” the sole implementor `LookupItem` already satisfies the new member. | **0.25 hrs** |

---

## 6. CaseEntryFarm Parity Fixes â€” August 2026 (G29, G31, G32, G33)

**Scope:** Comprehensive comparison of all 7 legacy `CaseEntry*` tabs against the migrated equivalent pages, with fixes for newly identified gaps.

### G29 â€” Linked Farms missing from inline farm section on `/Case/Details`

**Gap:** The legacy Farm tab showed a Linked Farms sub-grid when viewing a case. `/Farm/Details` has the related-farms table via `IFarmService.GetRelatedFarmsAsync`. `/Case/Details` did not call this and had no linked-farms display â€” only a "Full farm details" link that takes the user away.

**Fix â€” `Pages/Case/Details.cshtml.cs`:**
- Added `IReadOnlyList<FarmRelationRecord> RelatedFarms` property.
- `OnGetAsync` now calls `farms.GetRelatedFarmsAsync(Case.Cphh)` when a CPHH is present.

**Fix â€” `Pages/Case/Details.cshtml`:**
- Added a GOV.UK table showing related farms (CPHH, status, Open link) after the inline farm section, conditional on `Model.RelatedFarms.Count > 0`.

**Time spent: 0.25 hrs**

---

### G31 â€” ADNS Region name not displayed on `/Farm/Details`

**Gap:** `FarmRecord.ADNSRegionID` is an integer FK. `ILookupDataService.GetADNSRegionsAsync()` already exists and returns `LuADNSRegion { Id, Name }` records. `Farm/Details` was not calling this and not displaying the ADNS region name â€” only the raw ID would have been visible (and it was not even shown).

**Fix â€” `Pages/Farm/Details.cshtml.cs`:**
- Added `ILookupDataService` injection.
- `OnGetAsync` now fetches ADNS regions (in parallel with herd sizes and related farms), resolves `Farm.ADNSRegionID` to `ADNSRegionName`.

**Fix â€” `Pages/Farm/Details.cshtml`:**
- Added `ADNS region` row in the herd details summary list, rendered when `ADNSRegionName` is not null.

**Time spent: 0.25 hrs**

---

### G32 â€” Herd Size grid absent from `/Farm/Details`

**Gap:** The legacy Farm tab displayed `grdHerdSize` (annual herd size: HerdYear, TotalSize, Lactation1â€“Lactation10Plus) and `lblConfirmedCases`. The migrated `IFarmService.GetHerdSizesAsync` and `GetConfirmedCaseCountAsync` both existed but were never called from any Razor Page.

**Fix â€” `Pages/Farm/Details.cshtml.cs`:**
- Added `IReadOnlyList<HerdSizeRecord> HerdSizes` and `int ConfirmedCaseCount` properties.
- `OnGetAsync` fetches herd sizes and confirmed case count in parallel with related farms and ADNS regions using `Task.WhenAll`.
- Herd sizes sorted descending by year.

**Fix â€” `Pages/Farm/Details.cshtml`:**
- Added a "Herd statistics" section showing confirmed case count.
- Added a `govuk-details` collapsible with a GOV.UK table showing: Year, Total, Lac 1, Lac 2, Lac 3, Lac 4, Lac 5+ (Lactation5â€“10Plus summed). Caption cites the CPHH for screen reader users.
- Section is hidden when both `HerdSizes.Count == 0` and `ConfirmedCaseCount == 0`.

**Time spent: 0.5 hrs**

---

### G33 â€” Audit Log shortcut absent from Case sections nav bar

**Gap:** The legacy `CaseEntryDEFRA.aspx` had a direct "Case Audit log" button. The migrated `/AuditLog/ByCase` page exists (created in G14) but there was no link from the case view. Without this, users had to navigate via the top-level audit log menu.

**Fix â€” `Pages/Case/Details.cshtml`:**
- Added `<li><a asp-page="/AuditLog/ByCase" asp-route-rbse="..." class="govuk-link govuk-link--no-visited-state">Audit log</a></li>` to the case sections `govuk-list--inline` nav bar.

**Time spent: 0.1 hrs**

---

### G27 â€” Case Herdbook field not editable (deferred â€” documented as open gap)

**Finding:** `CaseRecord.Herdbook` and `CaseRecord.PedigreeRowStamp` exist in the model. In the legacy app, `CaseHerdbook` was saved via the `AddEditDamSireDetails` SP called from `CaseEntryRelations.aspx`. The migrated `AddEditDamSireCommand` and `PedigreeRepository.AddEditDamSireAsync` do NOT pass `CaseHerdbook` â€” nor do they pass the required concurrency parameters `DamID`, `DamRBSE`, `DamRowStamp`, `SireID`, `SireRBSE`, `SireRowStamp`, `CaseRowStamp`.

**Impact:** The `Case/Relations.cshtml` `EditDamSire` form handler is likely broken at runtime (SQL Server will throw "parameter not supplied" for `@DamID`). The Herdbook field on the Case table can only be read, not written.

**Action required (deferred):** Add the 8 missing parameters to `AddEditDamSireCommand`; update `PedigreeRepository.AddEditDamSireAsync` to pass them; load `DamId`/`DamRowStamp`/`SireId`/`SireRowStamp`/`CasePedigreeRowStamp` from the loaded `Details.Dam`, `Details.Sire`, and `CaseRecord`; add `CaseHerdbook` text input and the 5 hidden concurrency fields to `Relations.cshtml`; update `DamSireViewModel` and `OnPostEditDamSireAsync`. Logged as G27 in `BSE-Parity-Analysis-Report.md`.

**Time spent: 0 hrs (research only)**

---

## 7. Case/Farm Tab â€” Full Farm Tab Parity Page (August 2026)

**Scope:** The legacy `CaseEntryFarm.aspx` was the landing page when navigating from the Home page via RBSE number. It showed the "Farm" tab with: confirmed case count, all farm fields (owner, address, parish, county, herdmarks, AHO, herd type, pedigree, dealer, ADNS region), correspondence address, linked farms grid (add/delete), and herd size grid (add/delete/edit). The migrated `/Case/Details` page showed only a read-only summary of a subset of these fields.

### Gap
The migrated app had no equivalent to the Farm tab's full functionality. Users navigating from the Home page landed on a read-only `/Case/Details` summary with:
- No confirmed case count
- No linked farms management (add/delete)
- No herd size management (add/delete)
- No direct action buttons grouped for farm operations
- The ADNS Region, pedigree, map reference, and both correspondence address lines were missing from the inline farm section prior to G26

### Fix â€” New page: `/Case/Farm/{rbse}`

**Created `Pages/Case/Farm.cshtml.cs`:**
- Page model with `[Authorize]` â€” accessible to all authenticated users
- Loads: `CaseRecord` (for CPHH), `FarmRecord`, confirmed case count, linked farms list, herd size list, ADNS region name â€” all in parallel via `Task.WhenAll`
- POST handlers:
  - `OnPostAddLinkedFarmAsync` â€” calls `IFarmRelationRepository.AddAsync(cphh, relatedCphh)`
  - `OnPostDeleteLinkedFarmAsync(int id, string rowStampBase64)` â€” calls `IFarmRelationRepository.DeleteAsync`
  - `OnPostAddHerdSizeAsync` â€” builds `AddHerdSizeCommand` and calls `IHerdSizeRepository.AddAsync`
  - `OnPostDeleteHerdSizeAsync(int id, string rowStampBase64)` â€” calls `IHerdSizeRepository.DeleteAsync`
- All services (`IFarmRelationRepository`, `IHerdSizeRepository`) already registered in DI via `AddFarmManagementModule()`

**Created `Pages/Case/Farm.cshtml`:**
- GDS breadcrumb: Home > {RBSE} > Farm
- Case sections nav bar (same as all other case sub-pages, with "Farm" shown in bold as current)
- Confirmed cases inset text (from `IFarmService.GetConfirmedCaseCountAsync`)
- Action button group: "Edit farm details" (â†’ `/Farm/Edit`), "Full farm record" (â†’ `/Farm/Details`), "Farm audit log" (â†’ `/AuditLog/ByFarm`)
- Two-column `govuk-grid-row` farm summary: Address column (Owner, Address 1â€“3, Postcode, Parish, District, County, Map reference, Correspondence address) + Herd details column (Herdmark 1/2/3 with numeric herdmarks, Herd type, Pedigree, AHO, Is a dealer, Non-GB, ADNS region)
- Linked farms GOV.UK table with delete button per row (with browser confirm dialog) + collapsible add form (`govuk-details`)
- Herd size GOV.UK table (13 columns: Year, Total, Lac 1â€“10, Lac 10+) with delete button per row + collapsible add form with year, total, and all lactation size inputs

**Updated `Pages/Case/Details.cshtml`:**
- Added "Farm" as the first link in the case sections `govuk-list--inline` nav bar (â†’ `/Case/Farm`)
- Updated the inline farm section action buttons to include "Farm details" (â†’ `/Case/Farm`) as the primary secondary button alongside "Edit farm" and "Full farm record"

**Time spent: 1.5 hrs**

---

## Fix 14 â€” Case/Farm Page Parity with Legacy CaseEntryFarm.aspx

**Files changed:** `src/BSE.Host/Pages/Case/Farm.cshtml`, `src/BSE.Host/Pages/Case/Farm.cshtml.cs`  
**Time spent: 0.5 hrs**

Three parity gaps identified by comparing `CaseEntryFarm.aspx` (legacy) against the migrated `/Case/Farm` page:

### Gap 1 â€” Page heading label
- **Legacy (`CaseEntryFarm.aspx`):** `lblRBSEHeader.Text = RBSE_CAPTION & Session(SV_RBSENumber)` â€” renders as **"RBSE Number: 12/12/12345"**
- **Migrated (before fix):** `<h1>Case: @Model.Rbse</h1>`
- **Fix:** Changed h1 to `RBSE Number: @Model.Rbse` and updated `ViewData["Title"]` accordingly

### Gap 2 â€” Farm section heading
- **Legacy (`CaseEntryFarm.aspx`):** `<asp:label id="lblCPHH">CPHH</asp:label>` followed by the `CPHH.ascx` user control
- **Migrated (before fix):** `<h2>Farm: @Model.Farm.CPHH</h2>`
- **Fix:** Changed h2 label from "Farm:" to "CPHH:"

### Gap 3 â€” Batch number display
- **Legacy (`CaseEntryFarm.aspx`):** `BatchNumberDisplay1` user control (a collapsible popup showing batch numbers linked to the case; visible for VLA Data Entry and VLA Maintenance roles)
- **Migrated (before fix):** No batch number display on the Farm page
- **Fix:** Added `IBatchRepository` to `FarmModel`, loaded `GetBatchNumbersByRbseAsync(Rbse)` in `LoadAsync`, and rendered the results as a GDS summary card ("Batch numbers") when any exist

---

## 8. Farm Audit Log â€” GDS Compliance and Contextual Navigation (August 2026)

**Files changed:**
- `src/BSE.Host/Pages/AuditLog/ByFarm.cshtml`
- `src/BSE.Host/Pages/AuditLog/ByFarm.cshtml.cs`
- `src/BSE.Host/Pages/Case/Farm.cshtml`

**Time spent: 0.25 hrs**

### Gap
The legacy `FarmAuditLogReport.aspx` was reached from `CaseEntryFarm.aspx` via `btnFarmAuditLog`. The migrated `/AuditLog/ByFarm` page existed and retrieved data correctly, but had four GDS compliance and navigation issues:

1. **Breadcrumb rendered below the `<h1>`** â€” GOV.UK Design System requires breadcrumb to appear before the page heading.
2. **No contextual back link** â€” Legacy had a "Return" button. When arriving from `/Case/Farm`, there was no way to return to the case without using the browser back button.
3. **No contextual breadcrumb** â€” Breadcrumb always showed "Home > Farm audit log" regardless of how the page was reached. It should show "Home > Case {RBSE} (Farm) > Farm audit log" when arrived from a case.
4. **Page heading did not show CPHH when pre-populated** â€” When arriving from `/Case/Farm` with a CPHH query parameter, the h1 still read "Audit log by farm" rather than identifying the farm being viewed.
5. **Non-existent CSS class `govuk-!-overflow-x`** on the results table wrapper div.

### Fix â€” `ByFarm.cshtml.cs`
- Added `[BindProperty(SupportsGet = true)] public string? Rbse { get; set; }` â€” optional route parameter passed from `/Case/Farm`.

### Fix â€” `ByFarm.cshtml`
- Moved `<nav class="govuk-breadcrumbs">` above the `<h1>` (GDS standard).
- Breadcrumb conditionally includes a "Case {RBSE} (Farm)" crumb linking back to `/Case/Farm/{rbse}` when `Rbse` is provided.
- Added `<a class="govuk-back-link">Back to farm</a>` linking to `/Case/{rbse}` when `Rbse` is provided.
- Page `<h1>` now reads "Farm audit log â€” CPHH: {cphh}" when a CPHH search has been performed, matching the legacy "CPHH: 123/456/7890" label.
- Standalone CPHH search form is only shown when `Rbse` is not provided (i.e. direct access, not from a case context).
- Removed spurious `<div class="govuk-!-overflow-x">` wrapper (not a GDS utility class) from the results table.
- Entry count displays as "{n} entry/entries found." above the table.

### Fix â€” `Case/Farm.cshtml`
- Audit log link updated to pass `asp-route-rbse="@Model.Rbse"` alongside `asp-route-cphh` so the back navigation context is always available.

---

## 9. Case/Farm Page â€” Field Name and Value Parity with `CaseEntryFarm.aspx` (August 2026)

**Files changed:**
- `src/BSE.Host/Pages/Case/Farm.cshtml`
- `src/BSE.Host/Pages/Case/Farm.cshtml.cs`

**Time spent: 0.5 hrs**

### Root cause â€” codes shown instead of descriptions
The legacy `CaseEntryFarm.aspx` used dropdown lists (`ddlCounty`, `ddlAHO`, `ddlHerdType`, `ddlPedigree`) bound with `DataValueField = "Code"` and `DataTextField = "Description/Name"`. The DB stores the codes; the UI displayed the descriptions. The migrated page was rendering the raw code values from `FarmRecord` directly.

### Specific issues fixed

| Field | Issue | Fix |
|---|---|---|
| County | Showed raw code (e.g. `"BA"`) instead of description (e.g. `"Bath and North East Somerset"`) | Resolved via `GetLookupAsync(LookupTableId.BSECounty)` matching on `Code` |
| AHO | Showed raw code (e.g. `"W"`) instead of name (e.g. `"WCVS"`) | Resolved via `GetLookupAsync(LookupTableId.AHO)` matching on `Code` |
| Herd type | Showed raw code (e.g. `"D"`) instead of description (e.g. `"Dairy"`) | Resolved via `GetHerdTypesAsync()` matching on `Code` |
| Pedigree | Showed raw code (e.g. `"P"`) instead of description (e.g. `"Pedigree Herd"`) | Resolved via `GetLookupAsync(LookupTableId.PedigreeType)` matching on `Code` |
| Authority county | Field entirely missing | Resolved via `GetLookupAsync(LookupTableId.AuthorityCounty)` matching on `Id == Farm.AuthorityCountyID` |
| Owner name | Label read "Owner" | Corrected to "Owner name" |
| Numeric herdmark 1/2 | Shown inline in parentheses inside Herdmark 1/2 row | Separated into dedicated "Numeric herdmark 1" and "Numeric herdmark 2" rows |
| Is a dealer | Missing `?` â€” read "Is a dealer" | Corrected to "Is a dealer?" |

### Changes â€” `Farm.cshtml.cs`
- Added `using BSE.SharedKernel;`
- Added 5 new display-name properties: `CountyName`, `AHOName`, `HerdTypeName`, `PedigreeTypeName`, `AuthorityCountyName`
- `LoadFromCase()` expanded to fetch 5 additional lookup tasks in parallel (`BSECounty`, `AHO`, `HerdType`, `PedigreeType`, `AuthorityCounty`) and resolve display names via code/ID matching

### Changes â€” `Farm.cshtml`
- All 4 lookup-code fields now render `Model.XxxName ?? Model.Farm.XxxCode` (fallback to raw code if lookup fails)
- "Numeric herdmark 1" and "Numeric herdmark 2" are now separate `<dl>` rows
- "Authority county" row added (conditionally hidden when `AuthorityCountyName` is null)
- Label corrections: "Owner name", "Is a dealer?"

### Known deferred gap
**Local Authority** â€” `FarmRecord.AuthorityID` is an integer FK to `luAuthority`. No `GetluAuthorityAll` SP exists yet in the migrated database (G30 open item). The field remains absent from the display; when G30 is resolved, add `LookupTableId.Authority` and render the name.

---

## [Bug Fix] Farm/Edit â€” "Could not find stored procedure GetluADNSRegionAll"

**Hours spent: 0.5**

### Error
Accessing `/Farm/Edit` threw a runtime SQL exception: `Could not find stored procedure 'GetluADNSRegionAll'`. The stored procedure file (`BSE.Database/StoredProcedures/ReferenceData/GetluADNSRegionAll.sql`) exists in the database project and is included via the `**\*.sql` wildcard glob, but the SP had never been deployed to the running dev/test database instance.

### Root cause
The integration test seed SQL in `ReferenceDataIntegrationTestBase.cs` created the `luADNSRegion` table and seeded it with data, but never created the `GetluADNSRegionAll` stored procedure. When Testcontainers span up a fresh SQL Server for tests, or when the dev database was provisioned without a full DACPAC publish, the SP was absent.

### Fix
- Added `GetluADNSRegionAll` SP creation to the `SeedSql` constant in `ReferenceDataIntegrationTestBase.cs`, consistent with the pattern used for `GetluBreed` and `GetMapReferenceByCountyParish`.
- **Action required for dev database**: Run the following against the local SQL Server to deploy the missing SP:
  ```sql
  IF OBJECT_ID('GetluADNSRegionAll', 'P') IS NOT NULL DROP PROCEDURE GetluADNSRegionAll;
  EXEC('
  CREATE PROCEDURE GetluADNSRegionAll AS
  SELECT [ID], [Name], [AuthorityID]
  FROM [luADNSRegion]
  ORDER BY [Name]
  ');
  ```
  Alternatively, republish `BSE.Database.sqlproj` against your local instance via `sqlpackage` or SSDT.

---

## 10. Case(DEFRA) Tab â€” Full Parity with `CaseEntryDEFRA.aspx` (August 2026)

**Time spent: 3.0 hrs**

**Files changed:**
- `src/BSE.Host/Models/ViewModels/CaseEditViewModel.cs`
- `src/BSE.Host/Pages/Case/Edit.cshtml`
- `src/BSE.Host/Pages/Case/Edit.cshtml.cs`
- `src/BSE.Modules.CaseWork/Repositories/ICaseWorkRepository.cs`
- `src/BSE.Modules.CaseWork/Repositories/CaseWorkRepository.cs`

### Issues found and fixed

| Issue | Root cause | Fix |
|---|---|---|
| `Fate`, `Survey`, `ReportedLocation`, `BirthDateSource`, `ValuationAge`, `CaseType` shown as plain `<input type="text">` | Initial migration didn't wire lookup service to this page | Replaced all 6 with `<select>` dropdowns via `ILookupDataService.GetLookupAsync()`; value=`Code` matching legacy `DataValueField` |
| `FinalResultDate`, `FinalResult`, `DBSE` missing entirely | Not mapped from `CaseRecord` to ViewModel | Added 3 read-only properties to `CaseEditViewModel`, populated in `FromRecord()`; rendered as `<dl>` read-only summary |
| Casework date fields missing (`PurchaserBse1ReceivedDate`, `BreederBse1ReceivedDate`, `Vendor1Bse1ReceivedDate`, `HomebredBse1ReceivedDate`, `SummarySheetReceivedDate`, `PaperworkCompleteDate`) | `CaseWork` table not loaded on GET | Added `ICaseWorkRepository.GetByRbseAsync` call in `OnGetAsync`; added 6 date fields to ViewModel; rendered inline with each BSE-1 checkbox |
| `Barcode` and `AHFReference` missing | CaseWork fields not surfaced | Added both to ViewModel via `ApplyCaseWork()`; rendered conditionally when `HasCaseWork` |
| Save redirected to `/Case/Details` instead of `/Home` | Copy-paste from Details page model | Fixed `OnPostAsync` redirect to `RedirectToPage("/Home")` |
| Cancel link pointed to `/Case/Details` | Same copy-paste origin | Changed to `asp-page="/Home"` |
| Standalone `ICaseWorkRepository.EditAsync` missing | Only existed as overload with connection/transaction params | Added standalone overload to interface + implementation using base `ExecuteAsync` |
| `HasCaseWork` not round-tripped through form | Hidden field absent | Added `<input type="hidden" asp-for="Case.HasCaseWork" />` |
| Non-DEFRA fields (Sex, Breed, Origin, SlaughterDate, OnsetDate, DamStatus, PurchaseDate, PurchaseAgeInMonths, PurchasedCounty, HerdEntryDate, MonthsPregnant, MonthsPostCalving, OnsetAgeInMonths, AlternateDiagnosis) shown as visible editable inputs | These fields belong to other tabs (VLA, BAB) and are not in CaseEntryDEFRA.aspx | Converted to `<input type="hidden">` to round-trip without data loss; removed from visible form |
| Section order didn't match legacy | Initial migration re-ordered fields | Reordered to match legacy: Eartag â†’ Dates/Fate â†’ Results (read-only) + CaseType + LabComment â†’ Date of Birth â†’ Notes â†’ BSE-1 checkboxes â†’ Casework |
| Case Audit Log button missing | Not included in initial migration | Added as secondary GOV.UK button linking to `/AuditLog/ByCase?rbse=` |
| Unsaved-changes JS prompt missing | Not included in initial migration | Added `beforeunload` + Cancel confirm dialog (same pattern as Farm/Edit) |
| BSE-1 received date inputs shown when no casework record exists | Always rendered unconditionally | Now only rendered when `Model.Case.HasCaseWork` is true (matching legacy `ctlXxx.Enabled` based on CaseWork table row count) |

### Label corrections
- "Fate" â†’ "Fate (Form B reason)" (legacy: "Fate (Form B Reason)")
- "Birth date source" â†’ "Date of birth source" (legacy: "Date Of Birth Source")
- "Valuation age" retained (legacy match)
- "AHF reference" (legacy: "AHF Reference")
- Checkbox labels expanded: "Is purchaser BSE-1 received?", "Is breeder BSE-1 received?", etc.

---

## 11. GDS Compliance â€” Case Tab Pages (August 2026)

**Time spent: 0.5 hrs**

**Files changed:**
- `src/BSE.Host/Pages/Case/Vla.cshtml`
- `src/BSE.Host/Pages/Case/Clinical.cshtml`
- `src/BSE.Host/Pages/Case/Feeds.cshtml`
- `src/BSE.Host/Pages/Case/Relations.cshtml`
- `src/BSE.Host/Pages/Case/OtherOwners.cshtml`

### Issues found and fixed

| Issue | Pages affected | Fix |
|---|---|---|
| `<h1>` rendered before `<nav class="govuk-breadcrumbs">` â€” GOV.UK Design System requires breadcrumb above page heading | `Vla.cshtml`, `Clinical.cshtml`, `Feeds.cshtml`, `Relations.cshtml`, `OtherOwners.cshtml` | Swapped order: breadcrumb nav moved above `<h1>` on all 5 pages |
| No inline field-level error messages â€” GDS requires `govuk-form-group--error`, `govuk-error-message`, and `govuk-input--error`/`govuk-select--error` per field when ModelState contains errors | `Vla.cshtml` (all 9 field rows) | Added `ViewData.ModelState[key]?.Errors.Count > 0` guards on all form groups; added `<p class="govuk-error-message">` with `<span class="govuk-visually-hidden">Error:</span>` above each input/select; added `govuk-input--error`/`govuk-select--error` class conditionally |
| Checkbox rows (`IsBirthDateEst`, `IsOnsetDateEst`) missing `govuk-form-group` wrapper â€” error state class has nowhere to attach | `Vla.cshtml` rows 3 and 7 | Wrapped each `govuk-checkboxes` div in `<div class="govuk-form-group">` |
| External "View docs" link missing `(opens in new tab)` indicator â€” GDS accessibility guidance requires visually-hidden text on `target="_blank"` links | `Vla.cshtml` | Added `<span class="govuk-visually-hidden"> (opens in new tab)</span>` inside the anchor text |


---

## [Bug Fix] RZ1031 `@Sel()` in `<option>` attribute area â€” `Vla.cshtml`, `Edit.cshtml`

**Hours spent: 0.5**

### Error
Multiple `RZ1031: The tag helper 'option' must not have C# in the element's attribute declaration area` errors in `Vla.cshtml` (5 dropdowns: Sex, BirthDateSource, Breed, Origin, PurchasedCounty) and `Edit.cshtml` (6 dropdowns: Fate, CaseType, ReportedLocation, BirthDateSource, Survey, ValuationAge).

### Root cause
The `@Sel(val, opt)` local helper function was being called inline inside an `<option>` tag helper attribute position (e.g. `<option value="@o.Code" @Sel(...)>`). The Razor tag helper engine (RZ1031) does not permit free-form C# expressions in an element's attribute declaration area â€” only named attributes with `=` bindings are allowed.

### Fix
Replaced all occurrences of `@Sel(val, opt)` with the `selected="@(o.Code == val ? "selected" : null)"` pattern, which binds `selected` as a named attribute. Removed the now-unused `Sel` local function from the `@{ }` block in both files to eliminate the accompanying CS8321 unused-function warning.


---

## 12. View/Edit Pattern and Batch Numbers — Case Tab Pages (August 2026)

**Time spent: 3 hrs**

**Files changed:**
- `src/BSE.Host/Pages/Case/Edit.cshtml.cs`
- `src/BSE.Host/Pages/Case/Edit.cshtml`
- `src/BSE.Host/Pages/Case/Vla.cshtml.cs`
- `src/BSE.Host/Pages/Case/Vla.cshtml`
- `src/BSE.Host/Pages/Case/Bab.cshtml.cs`
- `src/BSE.Host/Pages/Case/Bab.cshtml`
- `src/BSE.Host/Pages/Case/Clinical.cshtml.cs`
- `src/BSE.Host/Pages/Case/Clinical.cshtml`
- `src/BSE.Host/Pages/Case/Feeds.cshtml.cs`
- `src/BSE.Host/Pages/Case/Feeds.cshtml`
- `src/BSE.Host/Pages/Case/Relations.cshtml.cs`
- `src/BSE.Host/Pages/Case/Relations.cshtml`

### Changes made

| Change | Pages affected | Detail |
|---|---|---|
| View/edit toggle pattern | `Edit.cshtml`, `Vla.cshtml`, `Bab.cshtml`, `Clinical.cshtml` | Default GET shows read-only `govuk-summary-list` with "Edit details" secondary button. `?edit=true` query param switches to the existing edit form. `[BindProperty(SupportsGet = true)] public bool Edit { get; set; }` added to page models. POST handlers redirect back to view mode (no `?edit=true`). Cancel link in edit form links back without `?edit=true`. |
| Batch Numbers section added | All 6 tabs | `IBatchRepository` injected into each page model. `IReadOnlyList<BatchNumberEntry> BatchNumbers` property loaded in `OnGetAsync`/`LoadAsync` in parallel with other data. `govuk-summary-card` with batch numbers rendered above the main content on all tabs when records exist. Pattern matches Farm tab. |
| Breadcrumb order fix | `Edit.cshtml`, `Bab.cshtml` | `<h1>` was rendered before `<nav class="govuk-breadcrumbs">`. Fixed to match GDS requirement: breadcrumb above page heading. |
| Cancel link fix | `Vla.cshtml` | Cancel previously linked to `/Home`. Now links back to the same page without `?edit=true` (returns to read-only view). |
| Cancel link fix | `Bab.cshtml` | Cancel previously linked to `/Case/Details`. Now links back to the same page without `?edit=true`. |
| Parallel data loading | All 6 `.cshtml.cs` files | `LoadAsync` methods rewritten to use `Task.WhenAll` for batch + other data, reducing page load latency. |
| Clinical signs read-only view | `Clinical.cshtml` | In view mode, only the checked signs are rendered as a `govuk-list govuk-list--bullet`. Visits table and add-visit details are shown in both view and edit modes. |

---

## [Bug Fix] RZ2008 bare `asp-page` attribute on `<a>` tag helpers

**Hours spent: 0.5**

### Error
`RZ2008: Attribute 'asp-page' on tag helper element 'a' requires a value.` in 4 files: `Edit.cshtml` (×2), `Bab.cshtml` (×2), `Clinical.cshtml` (×2), `Vla.cshtml` (×2).

### Root cause
The anchor tag helpers used `asp-page` without a value (e.g. `<a asp-page asp-route-rbse="...">`). In Razor Pages the anchor tag helper requires `asp-page` to be a non-empty string. The intent was to link back to the **current page** (toggling off `?edit=true`), which is expressed as `asp-page=""`.

### Fix
Changed all 8 occurrences from `asp-page` (bare) to `asp-page=""` across `Edit.cshtml`, `Bab.cshtml`, `Clinical.cshtml`, and `Vla.cshtml`.

---

## 13. Legacy Parity Review — All Case Tab Pages (August 2026)

**Time spent: 2.5 hrs**

**Files changed:**
- `src/BSE.Host/Pages/Case/Edit.cshtml`
- `src/BSE.Host/Pages/Case/Bab.cshtml.cs`
- `src/BSE.Host/Pages/Case/Bab.cshtml`
- `src/BSE.Host/Pages/Case/Clinical.cshtml.cs`
- `src/BSE.Host/Pages/Case/Clinical.cshtml`
- `src/BSE.Host/Pages/Case/Feeds.cshtml.cs`
- `src/BSE.Host/Pages/Case/Feeds.cshtml`
- `src/BSE.Host/Pages/Case/Relations.cshtml.cs`
- `src/BSE.Host/Pages/Case/Relations.cshtml`

### Changes made

| Issue | Pages affected | Fix |
|---|---|---|
| Casework entry button/link present in migrated page | `Edit.cshtml` | Removed `Casework` secondary button from edit form button group and removed `Casework entry` link from the read-only Casework section. Legacy `btnCaseWork` redirected to `CaseEntrySave.aspx?redirect=CaseWorkEntry.aspx` — this feature is not required in the migrated application per user instruction. |
| View Docs link missing from BAB, Clinical, Feeds, Relations tabs | `Bab.cshtml`, `Clinical.cshtml`, `Feeds.cshtml`, `Relations.cshtml` | Added `IConfiguration` + `SpolSiteUrl` property to each page model. Added View Docs link (with `govuk-visually-hidden` new-tab indicator) to read-only view button groups, edit form button groups, and bottom button groups in each view. |
| Clinical sign label mismatch: "Milk yield drop" vs legacy "Milk Yield Loss" | `Clinical.cshtml` | Changed label to "Milk yield loss" in both edit form checkbox label and read-only bullet list (via `checkedSigns.Add`). |
| Clinical sign label mismatch: "Reluctant at doorways" vs legacy "Reluctant to enter doorways" | `Clinical.cshtml` | Changed label to "Reluctant to enter doorways" in both edit form checkbox label and read-only bullet list (via `checkedSigns.Add`). |

### Navigation parity verified

| Feature | Legacy | Migrated | Status |
|---|---|---|---|
| Save | `CaseEntrySave.aspx` | POST handler → redirect to view mode | ✓ Equivalent |
| Cancel (DEFRA/VLA/BAB) | `Home.aspx` | View mode (same page, no `?edit=true`) | ✓ Improved UX |
| Case Audit Log (DEFRA, VLA) | `CaseAuditLogReport.aspx` | `/AuditLog/ByCase?rbse=` | ✓ Equivalent |
| View Docs (all tabs) | `GetSpolSiteButtonCode()` rendered link | `SpolSiteUrl` config link | ✓ Equivalent |
| Casework button (DEFRA only) | `CaseEntrySave.aspx?redirect=CaseWorkEntry.aspx` | Removed | ✓ Removed per requirement |
| Back to case (BAB, Clinical, Feeds, Relations) | `Home.aspx` Cancel | View mode / `/Case/Details` link | ✓ Equivalent |

## 14. Case (DEFRA) Page — Runtime Bug Fixes (August 2026)

**Hours spent: 2**

### Issues fixed

#### 14.1 — Save button redirected to Home instead of returning to tab view mode

**Affected files:**
- src/BSE.Host/Pages/Case/Edit.cshtml.cs
- src/BSE.Host/Pages/Case/Vla.cshtml.cs

**Root cause:** OnPostAsync() ended with 
eturn RedirectToPage("/Home") after a successful save.

**Fix:** Changed to 
eturn RedirectToPage(new { rbse = Rbse }) so the user is returned to the same tab in view mode with the success banner visible.

---

#### 14.2 — "Add / edit test records" link used @Model.Case.Rbse instead of @Model.Rbse

**Affected file:** src/BSE.Host/Pages/Case/Edit.cshtml

**Root cause:** Two occurrences of the link used sp-route-rbse="@Model.Case.Rbse". Model.Case is a CaseEditViewModel bound from the POST body during form submission — it can have an empty Rbse if the POST body is incomplete or on initial page load before the GET runs. Using @Model.Rbse (the route-bound property) is always safe and populated.

**Fix:** Both sp-route-rbse="@Model.Case.Rbse" references changed to sp-route-rbse="@Model.Rbse".

---

#### 14.3 — Audit log missing return link, pagination, and CSV export

**Affected files:**
- src/BSE.Host/Pages/AuditLog/ByCase.cshtml
- src/BSE.Host/Pages/AuditLog/ByCase.cshtml.cs

**Root cause:** The migrated ByCase.cshtml page was a minimal stub — it had no return link, showed all entries in a flat unpagianted table, and had no export.

**Legacy behaviour:**
- Return button read Request.QueryString["page"] and navigated back to {page}.aspx
- Pagination via DataGridPager control with session storage (20 rows per page)
- Excel export via ExcelExport.aspx (data stored in session)

**Fixes applied:**
1. **Return link**: Added Back to case {Rbse} link pointing to /Case/Edit?rbse= when Model.Rbse is set. Breadcrumb also updated to include the case RBSE as a breadcrumb item.
2. **Pagination**: Server-side slicing — 20 rows per page. Added PageNumber GET parameter, TotalCount/TotalPages properties, and GDS govuk-pagination component in the view.
3. **Export to CSV**: Added OnGetExportAsync() handler that re-queries all entries and streams them as a UTF-8 CSV download (AuditLog_{RBSE}_{date}.csv). Export button shown above the results table.

## Fix: Dapper FinalResultRecord materialization error on TestResults page

**File:** src/BSE.Modules.CaseManagement/Models/ReportRecords.cs`n**Error:** InvalidOperationException — Dapper could not materialise FinalResultRecord because the positional record constructor (3 params: Rbse, FinalResult, FinalResultDate) did not match the 40+ columns returned by the GetFinalResultByRBSE stored procedure.
**Fix:** Converted FinalResultRecord from a positional primary-constructor record to a property-based record with { get; init; } properties. Dapper now maps by column name instead of constructor signature, ignoring unreferenced SP columns.
**Hours spent:** 0.25

## 15. Case (DEFRA) — TestResults Page Navigation and UX Fixes (August 2026)

**Hours spent: 1**

### Issues fixed

#### 15.1 — "Add / edit test records" link navigated to wrong page

**Root cause:** The existing TestResults.cshtml page had:
- Breadcrumb @Model.Rbse link pointing to /Case/Details instead of /Case/Edit
- "Back to case" link at the bottom pointing to /Case/Details instead of /Case/Edit

When the user finished adding or reviewing test records and clicked "Back to case", they landed on the generic case details view rather than the DEFRA tab they came from.

**Fix:** Both the breadcrumb RBSE link and the "Back to case" link changed from sp-page="/Case/Details" to sp-page="/Case/Edit". The back link text also updated to "Back to case (DEFRA)" to make the destination explicit.

---

#### 15.2 — "Add test record" form hidden in collapsed details element

**Root cause:** The add-test form was wrapped in a <details>/<summary> collapsible, requiring an extra click to expand before the user could see the form.

**Legacy behaviour:** The test records grid on CaseEntryDEFRA.aspx had Add/Edit/Delete available inline — users could always see and interact with the test row inputs without expanding anything.

**Fix:** Removed the <details> wrapper. The "Add a test record" section is now rendered as a visible <h2> heading with the form directly beneath it, matching the always-visible inline approach from the legacy page.

---

#### 15.3 — GDS breadcrumb/h1 order

**Fix:** Moved <h1> to appear after <nav class="govuk-breadcrumbs">, following GDS page layout conventions.

### TestResults page functionality summary

| Action | How it works | Returns to |
|---|---|---|
| Add test record | Select Test Type + Test Result, click "Add test record" | Same TestResults page (success banner shown) |
| Delete test record | Click "Delete" on any row, confirm prompt | Same TestResults page (success banner shown) |
| Save final result | Set Final Result + Date + DBSE, click "Save final result" | Same TestResults page (success banner shown) |
| Done / back | Click "Back to case (DEFRA)" link | /Case/Edit?rbse= (DEFRA tab, view mode) |

## Fix: Remove legacy Final Result section from Case/TestResults page

**Files:** src/BSE.Host/Pages/Case/TestResults.cshtml, src/BSE.Host/Pages/Case/TestResults.cshtml.cs`n**Change:** Removed the 'Final result' form block (SaveFinalResult POST handler, FinalResultCode/FinalResultDate/Dbse bind properties, CurrentFinalResult and Case properties, caseService/caseRepository/currentUser constructor dependencies, and corresponding LoadAsync calls). The page now contains only the 'Individual test records' table and 'Add a test record' form.
**Hours spent:** 0.25

## 16. Case BAB Tab — Legacy Parity: Label and Layout Fixes (August 2026)

**Hours spent: 1**

### Scope

Label and layout corrections only. No data model, repository, or SQL changes were made — the CaseBAB table fields and the BabFormViewModel are unchanged.

> Note: Origin is a Case table field saved by the DEFRA tab (Edit.cshtml.cs) via the full EditCase stored procedure. It is not a BAB concern and was not added to this tab.

### Changes — Bab.cshtml (view only)

| Area | Before | After |
|---|---|---|
| Natal CPHH label | "Natal CPHH" | "Traced CPHH" (matches legacy label) |
| Traced name label | "Owner name" | "Traced Name" (matches legacy label) |
| Address display | Combined single string | Three address lines displayed separately with <br /> |
| Traced address label | "Address" | "Traced Address" |
| Risk labels | Lower-case "risk" | Title-case "Risk" (Feed Risk, Horizontal Risk, Maternal Risk) |
| Address section heading | "Traced origin farm" | "Traced origin farm" (kept) |
| Address inputs | Three separate <div class="govuk-form-group"> blocks | Single labelled group with three stacked inputs |
| Audit log link | Missing from edit button group | Added "Case audit log" secondary button in edit form button group |
| Breadcrumb | Linked to /Case/Details | Corrected to /Case/Bab (current page) |
| View docs link | Present in view mode only | Present in both view mode and edit button group |

### Not changed

- Bab.cshtml.cs — no changes (save redirect was already correct from session 14)
- CaseBabRecord.cs — unchanged
- BabFormViewModel — unchanged
- IBabRepository / BabRepository — unchanged
- No SQL stored procedures added or modified

## 17. Case BAB Tab — Origin Dropdown Added, Audit Log Removed (August 2026)

**Hours spent: 0.5**

### Changes

#### 17.1 — Origin dropdown added

Origin is stored on the Case table (char(1), FK to luAnimalOrigin). The legacy BAB page (CaseEntryBAB.aspx) included an Origin dropdown that was missing in the migrated page.

**New SP:** UpdateCaseOrigin.sql — updates only the [Origin] column on [Case] for a given RBSE within the caller's transaction. A targeted SP avoids the full EditCase overhead (RowStamp, 40 parameters).

**Repository changes:**
- ICaseRepository — added UpdateOriginAsync(string rbse, string? origin, IDbConnection, IDbTransaction)
- CaseRepository — implemented via ExecuteAsync("UpdateCaseOrigin", ...)

**Page model (Bab.cshtml.cs):**
- Added ICaseRepository caseRepository to constructor
- Added IEnumerable<LookupItem> AnimalOrigins property (loaded via lookups.GetAnimalOriginsAsync())
- Added [BindProperty] string? Origin — bound from form, pre-populated from caseRecord.Origin in LoadAsync
- OnPostSaveBabAsync now calls caseRepository.UpdateOriginAsync(Rbse, Origin, conn, tx) inside the same transaction as the BAB save

**View (Bab.cshtml):**
- Read-only: Origin shown in summary list between Notes and Traced CPHH
- Edit form: Origin select rendered between Notes and Traced CPHH (matches legacy field order)

#### 17.2 — Case audit log link removed from BAB tab

Audit log was added in a previous session by mistake. It is not present on the legacy BAB page. Removed from both view-mode button group and edit-mode button group.

#### 17.3 — Save and Cancel verified

- **Save**: POSTs to OnPostSaveBabAsync, saves CaseBAB fields + Origin in one transaction, then RedirectToPage(new { rbse = Rbse }) returns to view mode with success banner.
- **Cancel**: navigates to same page without ?edit=true, returning to read-only view.

## 18. Create New Batch — Home Page Must Stay on Home, Not Redirect to /Case/New (`Pages/Home.cshtml`, `Pages/Home.cshtml.cs`)

**Hours spent: 1**

### Error

`OnPostCreateBatchAsync` called `GetOrCreateBatchNumberAsync()` and then executed `RedirectToPage("/Case/New", ...)`, taking the user directly to the new case form. This contradicts the legacy `Home.aspx` behaviour where clicking "Create New" called `clsBatch.CreateBatchNumber()`, stored the result in `Session[SV_BatchID]` / `Session[SV_BatchNumber]`, and **stayed on the Home page** with the batch year/number populated in the form fields. The user was expected to see the assigned batch number and then navigate to case entry by clicking "Go".

### Fix

**`Home.cshtml.cs`:**
- Added `bool BatchJustAssigned` property (read-only, driven by the handler).
- Rewrote `OnPostCreateBatchAsync` to: call `GetOrCreateBatchNumberAsync()`, assign `BatchYear`, `BatchNumber`, and `BatchJustAssigned = true`, call `OnGetAsync()` to reload `LatestBatches` and RBSE data, then return `Page()` — remaining on the Home page.

**`Home.cshtml`:**
- Added a `govuk-inset-text` confirmation block inside the batch panel, rendered only when `Model.BatchJustAssigned` is true. It displays the assigned batch number (`YYYY/N`) and instructs the user to click "Go" to begin entering cases for that batch.

### Legacy behaviour matched

| Step | Legacy | Migrated (after fix) |
|---|---|---|
| 1 | Click "Create New" → `AddBatchNumber` SP called | Click "Create New" → `AddBatchNumber` SP called via `GetOrCreateBatchNumberAsync()` |
| 2 | Session stored; page stays on `Home.aspx`; batch shown in VLAHeader | `Page()` returned; `BatchYear`/`BatchNumber` pre-populated in form; inset confirmation shown |
| 3 | User clicks through to case entry via navigation | User clicks "Go" (LookupBatch handler) → redirect to `/Case/New?batchYear=...&batchNumber=...` |

### Not changed

- `BatchService` / `BatchRepository` / `AddBatchNumber.sql` — unchanged
- `OnPostLookupBatchAsync` ("Go" button) — unchanged
- `/Case/New` and `New.cshtml.cs` — unchanged

#### 17.2 — Case audit log link removed from BAB tab

Audit log was added in a previous session by mistake. It is not present on the legacy BAB page. Removed from both view-mode button group and edit-mode button group.

#### 17.3 — Save and Cancel verified

- **Save**: POSTs to OnPostSaveBabAsync, saves CaseBAB fields + Origin in one transaction, then RedirectToPage(new { rbse = Rbse }) returns to view mode with success banner.
- **Cancel**: sp-page="" asp-route-rbse="@Model.Rbse" — navigates to same page without ?edit=true, returning to read-only view.
