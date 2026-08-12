# Migration Bug Fixes — BSE .NET 10 Migrated Application

> Branch: `feature/gds-agent-on-migrated`  
> Date: August 2026

---

## 1. Home Page (`Pages/Home.cshtml` + `Pages/Home.cshtml.cs`)

### Error
Old screen content (legacy panels) was visible alongside the new GDS panels because `replace_string_in_file` appended new content instead of replacing it. The `.cs` file had a duplicate `HomeModel` class definition causing 40+ CS0102/CS0111 compiler errors. The `.cshtml` file had 448 lines (250 new + 198 old appended).

### Fix
- Truncated `Home.cshtml` to 250 lines (removed lines 251–448) using `[IO.File]::ReadAllLines` + `WriteAllLines`.
- Truncated `Home.cshtml.cs` to 117 lines (removed lines 118–212) using the same approach.
- Root cause: `replace_string_in_file` matched only the class opening brace and appended rather than replacing the full body. Full-file overwrite via PowerShell is required for large structural rewrites.

---

## 2. Home Page — RBSE Go Button Navigation

### Error
In the legacy system, entering an RBSE number on the Home page and clicking **Go** navigated to `CaseEntryFarm.aspx`, which showed **case + farm details combined** on one page. In the migrated app, the RBSE form pointed to `/Case/Lookup` which redirected to `/Case/Details` — showing case data only, with farm as a separate link.

### Fix
- Updated `Pages/Case/Details.cshtml.cs` to inject `IFarmService` alongside `ICaseService`.
- `OnGetAsync` now loads the farm record from `Case.Cphh` in parallel with case data.
- Updated `Pages/Case/Details.cshtml` to display the farm details section inline below the case details (Owner, Address, County, Parish, Herdmarks, AHO, Herd type, Dealer, Non-GB), matching the legacy combined view.
- Added a `View full farm details` link to `/Farm/Details` for full farm management.
- CPHH row in the case summary changed from a hyperlink to plain text (farm section below is the entry point).

---

## 3. Case Search Page (`Pages/Search/Cases.cshtml` + `.cs`)

### Error 1 — Spurious mandatory field validation errors on page load
On page load (before the user submits anything), the error summary showed:
```
The Sex field is required.
The Dbse field is required.
The Fate field is required.
The Notes field is required.
...
```

### Cause
ASP.NET Core 6+ treats every non-nullable `string` property as implicitly `[Required]` when `<Nullable>enable</Nullable>` is set in the project. The legacy .NET Framework WebForms app had no model binding or `ModelState` concept — all search filter fields were optional `Request.QueryString` reads. The migration correctly enabled nullable context but did not suppress this new framework behaviour.

### Fix — `Program.cs`
Chained `.AddMvcOptions` to `AddRazorPages` to suppress the implicit required behaviour globally:
```csharp
builder.Services.AddRazorPages(options => { ... })
	.AddMvcOptions(o =>
		o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
```

### Fix — `Pages/Search/Cases.cshtml`
Added `&& Model.Filter.HasSearched` guard to the error summary block so it only renders after a search attempt, not on initial page load:
```razor
@if (!ViewContext.ModelState.IsValid && Model.Filter.HasSearched)
```

---

### Error 2 — `ModelState.IsValid` guard blocking the search
The page model contained:
```csharp
if (!ModelState.IsValid)
{
	// Validation failed — do not run search
	return;
}
```
Because of Error 1 above, `ModelState.IsValid` was always `false`, so the search never executed and always returned 0 results.

### Fix — `Pages/Search/Cases.cshtml.cs`
Removed the `if (!ModelState.IsValid) { return; }` block entirely. The existing `HasAnyFilter()` guard correctly controls when to run the search without needing model validation.

---

### Error 3 — Search returning 0 results despite correct RBSE in DB

**Symptoms:** After fixing Errors 1 and 2, searching for `000000001` (which exists in the DB) still returned 0 results. Diagnostic logging confirmed:
```
CaseSearch: HasAnyFilter=true, Rbse='000000001', ModelStateValid=true, Errors=
CaseSearch: SP returned 0 rows
```
Manual execution of `GetSearchCase` SP with the same parameters returned 1 row.

**Cause:** Dapper's anonymous object parameter inference sends C# `string` values as `nvarchar` (Unicode) by default. The SP parameters are declared as `varchar` (ANSI). SQL Server's implicit conversion from `nvarchar` to `varchar` in a `LIKE` comparison can silently produce no matches. Similarly, `DateTime?` null values passed via anonymous objects may not reliably map to SQL `datetime` parameters declared without defaults.

### Fix — `BSE.Modules.Search/Repositories/SearchRepository.cs`
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

## 4. Mandatory Field Validation — Root Cause Summary

| Problem | Root Cause | Fix | Time Spent |
|---|---|---|---|
| All non-nullable `string` search fields shown as required on page load | ASP.NET Core 6+ implicit required for non-nullable reference types with `<Nullable>enable</Nullable>` | `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true` in `Program.cs` | — |
| Error summary renders on initial page load with no user input | `@if (!ModelState.IsValid)` in view has no guard for first load | Added `&& Model.Filter.HasSearched` condition | — |
| Search never executes when any filter is entered | `if (!ModelState.IsValid) { return; }` in page model always true due to implicit required | Removed the guard; `HasAnyFilter()` is sufficient | — |
| SP returns 0 rows despite matching data in DB | Dapper anonymous object sends `string` as `nvarchar`; SP expects `varchar` | Use `DynamicParameters` with `DbType.AnsiString` for all `varchar` SP params | — |
| SP returns 0 rows even after `DynamicParameters` fix | `DateTime?` ViewModel properties on a GET form generate `__Invariant` hidden fields with wrong name prefix — ASP.NET Core cannot bind them for nested models, causing `DateTime.MinValue` to be passed instead of `null` | Changed date ViewModel properties to `string?`; parse in `ToQuery()` with `DateTime.TryParseExact(..., "yyyy-MM-dd", ...)` | — |
| SP returns 0 rows via `Microsoft.Data.SqlClient` RPC but 1 row via inline `EXEC` SQL text | `Microsoft.Data.SqlClient` sets `ARITHABORT OFF` by default; SQL Server reuses a cached plan compiled under `ARITHABORT ON` (e.g. from SSMS), producing wrong results via parameter sniffing | Added `SET ARITHABORT ON` in `DapperRepository.OpenConnection()` — fixes all SPs in one place | **2 hrs** |
| SP returns 0 rows via `Microsoft.Data.SqlClient` RPC but 1 row via inline `EXEC` SQL text | `Microsoft.Data.SqlClient` sets `ARITHABORT OFF` by default; SQL Server reuses a cached plan compiled under `ARITHABORT ON` (e.g. from SSMS), producing wrong results via parameter sniffing | Added `SET ARITHABORT ON` in `DapperRepository.OpenConnection()` — fixes all SPs in one place | **2 hrs** |

---

## 4b. Case Search — `__Invariant` GET Form Binding Bug (Search Still 0 Rows After DynamicParameters Fix)

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
`DateTime.MinValue` pre-dates SQL Server's minimum datetime — every row fails the condition → **0 rows returned**.

### Fix — `BSE.Host/Models/ViewModels/CaseSearchViewModel.cs`
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
Empty strings → `null` → SP uses its `ISNULL(…, default)` fallback. Valid dates → correctly typed `DateTime`.

Also updated `HasAnyFilter()` in `Cases.cshtml.cs` to use `!string.IsNullOrWhiteSpace(Filter.*Date)` instead of `.HasValue`.

---

## 4c. Case Search — `ARITHABORT OFF` / Microsoft.Data.SqlClient Plan Sniffing Bug (Search Still 0 Rows After All Previous Fixes)

**Time spent: 2 hours**

### Symptom
After fixing the `__Invariant` date binding bug, search still returned 0 rows. Raw diagnostic confirmed:
- Direct `SELECT COUNT(*)` via app connection → **1 row** (DB correct, row exists)
- `EXEC GetSearchCase ...` as inline SQL text → **1 row** (SP logic correct)
- `CommandType.StoredProcedure` via `System.Data.SqlClient` (PowerShell) → **1 row**
- `CommandType.StoredProcedure` via `Microsoft.Data.SqlClient` (app) → **0 rows**
- Dapper `QueryAsync<CaseSearchResult>` (which wraps the above) → **0 rows**

### Root Cause
`Microsoft.Data.SqlClient` (the modern .NET SQL client library) sets **`ARITHABORT OFF`** on every connection by default. This differs from:
- The legacy `System.Data.SqlClient` (sets `ARITHABORT ON`)
- SSMS (sets `ARITHABORT ON`)

SQL Server caches query plans keyed on `SET` options. When `GetSearchCase` was previously executed under `ARITHABORT ON` (e.g. from SSMS), SQL Server cached that plan. When the app calls it via RPC with `ARITHABORT OFF`, SQL Server reuses the cached plan but executes it under different `SET` options — a classic **parameter sniffing / plan reuse mismatch** that silently returns wrong results (0 rows).

Calling via inline `EXEC` text bypasses the compiled SP plan cache, which is why it returned 1 row correctly.

### Fix — `BSE.Infrastructure/DapperRepository.cs`
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
This is a **single fix covering every SP in the entire application** — no changes to individual stored procedures required.

---

## 5. General Lessons Learned

| # | Lesson |
|---|---|
| 1 | `replace_string_in_file` on a class opening only appends — use full-file PowerShell overwrite for structural rewrites |
| 2 | `get_errors` can report stale Roslyn IntelliSense after a PowerShell file write — verify with `get_file` first |
| 3 | Hot reload does not apply `Program.cs` startup changes — always do a full Stop + Start after changing service registration |
| 4 | Dapper anonymous objects infer `string` → `nvarchar`. Always use `DynamicParameters` with `DbType.AnsiString` for SP params declared as `varchar`/`char` |
| 5 | `DateTime?` SP params without SQL defaults must be passed as `DBNull` explicitly — `DynamicParameters` with `DbType.DateTime` handles `null` correctly |
| 6 | ASP.NET Core's implicit required validation is a breaking change from .NET Framework — suppress globally when migrating search/filter forms |
| 7 | `DateTime?` properties on a **GET form** with nested model binding (`Filter.*`) generate `__Invariant` hidden fields at the wrong level — use `string?` + manual `TryParseExact` in `ToQuery()` instead |
| 8 | `Microsoft.Data.SqlClient` sets `ARITHABORT OFF` by default unlike the old `System.Data.SqlClient` and SSMS — always issue `SET ARITHABORT ON` after opening a connection when targeting SQL Server via the new client library. Fix once in `DapperRepository.OpenConnection()`, not in every SP | **2 hrs** |
