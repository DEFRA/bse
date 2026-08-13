# Search Pages Migration Fix Summary

## Purpose
This document records the search-page parity fixes applied in the migrated Razor Pages app and explains why these gaps were missed during migration.

## Scope of fixes
- Farm Search (`/Search/Farms`)
- Case Search (`/Search/Cases`)
- Related Animals Search (`/Search/RelatedAnimals`)

---

## 1) Farm Search fixes

### What was changed
1. **Dealer filter semantics fixed to support Any/Yes/No**
   - `src/BSE.Modules.Search/Models/FarmSearchQuery.cs`
	 - Changed `IsDealer` from `bool` to `bool?`.
   - `src/BSE.Host/Models/ViewModels/FarmSearchViewModel.cs`
	 - Changed mapping from `IsDealer ?? false` to `IsDealer`.

2. **Validation coverage updated**
   - `src/BSE.Modules.Search.Tests/FarmSearchServiceTests.cs`
	 - Added tests for:
	   - `Dealer = Any` (`null`)
	   - `Dealer = Yes` (`true`)
	   - `Dealer = No` (`false`)

### Why this was missed in migration
- The migrated UI introduced a tri-state dealer control, but the query contract was simplified to a non-nullable boolean.
- This caused implicit coercion of "Any" into `false`, changing behavior without a compile error.

### Additional UI parity correction (Farm Search page layout)
- Legacy farm search uses a paired two-column form layout with dropdown-backed county/AHO fields.
- Migrated page had a simplified single-column layout and treated county/AHO as free-text inputs.

**Implemented fix**
- `src/BSE.Host/Pages/Search/Farms.cshtml`
  - Updated heading/title to `Farm Search`.
  - Reworked the form into a two-column row layout matching legacy field grouping.
  - Updated labels to legacy wording (`CPH(H)`, `Owner Name (partial search)`, `Address (partial search)`, `Numeric Herdmark`, `Include Non-GB Farms?`).
  - Converted `County` and `AHO` to dropdowns and replaced `Clear search` with legacy-style `Search Menu` action.
- `src/BSE.Host/Pages/Search/Farms.cshtml.cs`
  - Injected `ILookupDataService` and populated county/AHO option lists for the page.
- `src/BSE.Modules.ReferenceData/Services/LookupDataService.cs`
  - Preserved lookup `Code` values for `BSECounty` and `AHO` so dropdown selection posts the expected backend filter codes.

---

## 2) Case Search fixes

### What was changed
1. **Missing filters restored in Razor page**
   - `src/BSE.Host/Pages/Search/Cases.cshtml`
   - Added fields:
	 - `Sex`
	 - `Notes`
	 - `Passive/Active`
	 - `Final Result` date range (From/To)
	 - `Birth Date` date range (From/To)
	 - `Imported cases only` checkbox

2. **ViewModel mapping completed**
   - `src/BSE.Host/Models/ViewModels/CaseSearchViewModel.cs`
   - Added properties and query mapping:
	 - `PassiveActive`
	 - `IsImportedCase`

3. **Search trigger logic expanded**
   - `src/BSE.Host/Pages/Search/Cases.cshtml.cs`
   - `HasAnyFilter()` now includes all supported filters/flags, including:
	 - `Sex`, `Notes`, all date ranges
	 - `PassiveActive`, `IsImportedCase`, `IncludeNonGb`

4. **Validation coverage updated**
   - `src/BSE.Modules.Search.Tests/CaseSearchServiceTests.cs`
   - Added tests for:
	 - `IsImportedCase` pass-through
	 - `Sex` + `Notes` pass-through

### Why this was missed in migration
- Backend search contract (`CaseSearchQuery` + stored procedure) remained feature-complete.
- Migrated page implementation was reduced to a subset of fields (UI simplification), and `HasAnyFilter()` was only partially updated.
- Some fields existed in model/backend but were not wired in the page, so no runtime exceptions occurred, only missing functionality.

### Additional UI parity correction (dropdown-backed filters)
- Legacy case-search behavior uses lookup-driven dropdowns for coded fields.
- Migrated page had those fields as free-text inputs, which caused UX and parity differences.

**Implemented fix**
- `src/BSE.Host/Pages/Search/Cases.cshtml`
  - Converted these fields from text inputs to dropdowns:
	- `Fate`
	- `Final result`
	- `Sex`
	- `Survey`
- `src/BSE.Host/Pages/Search/Cases.cshtml.cs`
  - Injected `ILookupDataService` and loaded option lists from reference data.
  - Populated options for the above fields on `OnGetAsync`.
- `src/BSE.Modules.ReferenceData/Models/LookupItem.cs`
  - Updated lookup DTO shape to support both two-column and three-column lookup SP projections.
  - Added constructor support for `(ID, Code, Description)` materialization used by Dapper.

**Reason this difference happened in migration**
- Lookup binding logic to reference-data services was not carried over to the migrated search page.
- Without that binding, text boxes were used as a simplified placeholder, which compiles but does not match legacy interaction design.
- After binding was reintroduced, a latent contract mismatch surfaced: several lookup SPs return `ID, Code, Description` while `LookupItem` originally modeled only `Id, Description`.

### Additional label/control parity correction
- `src/BSE.Host/Pages/Search/Cases.cshtml`
  - Updated labels/headings to align more closely with legacy UI wording:
	- `Case Search`
	- `Eartag (starting with...)`
	- `Final Result`
	- `Passive/Active`
	- `Form A Date Between`
	- `Final Result Date Between`
	- `Birth Date Between`
	- `Include Non-GB Cases?`
	- `Imported Cases`
  - Changed `Notes` from single-line input to **textarea** with label `Notes (partial search)`.
  - Added legacy note text under notes field:
	- `Note: % character can be used as wildcard character in text fields`
  - Updated dropdown default option text from `Any` to blank entry for legacy parity.

---

## 3) Related Animals Search fixes

### What was changed
1. **Trigger logic corrected for relation-only searches**
   - `src/BSE.Host/Pages/Search/RelatedAnimals.cshtml.cs`
   - Search now runs when any of these is provided:
	 - `Rbse`, `Name`, `Eartag`, `RelationRbse`, `RelationType`

### Why this was missed in migration
- The original trigger check only considered three inputs (`Rbse`, `Eartag`, `Name`).
- Relation filters existed on the page and backend API call, but were omitted from trigger logic.
- This produced a silent behavior gap: valid user input that never executed a search.

---

## Technical root causes across pages
1. **Partial UI parity implementation**
   - Migrated pages were implemented with reduced forms while backend contracts stayed broad.

2. **Contract mismatch drift**
   - UI control semantics (tri-state) did not match non-nullable query model fields.

3. **Incomplete "has filter" predicates**
   - PageModel guards were not updated when additional filters existed or were reintroduced.

4. **Limited migration-time parity checks**
   - Compilation succeeded because types were valid; behavior parity issues required targeted functional checks.

---

## Validation performed
- Solution build: **successful**
- Search module test project: **27 passed, 0 failed**
  - Includes newly added/updated farm and case filter delegation tests.

---

## Recommended follow-up
- Add parity-focused page-model tests for `HasAnyFilter()`/trigger rules.
- Keep query-model nullability aligned with UI control semantics.
- Add migration checklist item: "backend-supported filters vs rendered filters" for each search page.

---

## 4) Result-table parity pass (additional fix)

### What was changed
1. **Case Search result columns expanded**
   - `src/BSE.Host/Pages/Search/Cases.cshtml`
   - Added display columns already returned by backend:
	 - `IsBirthDateEst`
	 - `FinalResultDate`
	 - `Dbse`
	 - `Notes`
	 - `BabNotes`
	 - `Origin`
	 - `ValuationAge`

2. **Related Animals result columns expanded**
   - `src/BSE.Host/Pages/Search/RelatedAnimals.cshtml`
   - Added display columns already returned by backend:
	 - `Cphh`
	 - `RelationType`
	 - `RelSex`
	 - `RelBirthDate`
	 - `RelFate`
	 - `LeftDate`
	 - `RelEartag`

### Why this was missed in migration
- The migrated pages initially focused on core action flows and rendered only a subset of result data.
- Backend result models and stored procedures remained richer, but those fields were not surfaced in the Razor tables.
- This created visibility gaps rather than runtime failures, so the issue was not caught by compile-time checks.

### Validation update
- Solution build remains **successful** after this result-table parity pass.

---

## 5) Herdmark/holding search split parity

### Legacy parity requirement
- Legacy UI exposes two separate pages:
  - `List Of Cases With A Given Herdmark`
  - `List Of Cases For A Given Holding/Herdmark`

### Evidence in current codebase
- Search navigation currently exposes only:
  - `/Search/Cases`
  - `/Search/Farms`
  - `/Search/Outstanding`
  - `/Search/RelatedAnimals`
  (see `src/BSE.Host/Pages/Home.cshtml` and `src/BSE.Host/Pages/Shared/_Layout.cshtml`)
- Backend support for herdmark-based case retrieval **does exist**:
  - Service contracts: `GetCasesByCphhAsync(...)`, `GetCasesByEartagHerdmarkAsync(...)`
	(`src/BSE.Modules.Search/Services/ICaseSearchService.cs`)
  - Repository wiring: `SearchRepository` methods call stored procedures
	(`src/BSE.Modules.Search/Repositories/SearchRepository.cs`)
  - Stored procedures:
	- `GetSearchCaseByCPHH` with `@Herdmark`, `@NumericHerdmark`, `@IncludeNonGBCases`
	- `GetSearchCaseByEartagHerdmark` with herdmark input
	(`src/BSE.Database/StoredProcedures/Search/*.sql`)
- Current usage appears test-only for these methods (no host page invocation).

### Why this was missed in migration
- Migration prioritized the four core search pages and did not create a dedicated host UI for the legacy herdmark-to-case listing workflow.
- Since backend contracts/SPs were retained, compilation and service tests passed, masking the missing user-facing page.
- This is a **functional parity gap in the migrated UI layer**, not a backend capability gap.

### Implemented fix
1. **Dedicated herdmark-only page**
   - `src/BSE.Host/Pages/Search/CasesByHerdmark.cshtml`
   - `src/BSE.Host/Pages/Search/CasesByHerdmark.cshtml.cs`
   - Input/trigger: `Herdmark` (+ `Include Non-GB Cases?`)
   - Query call: `GetCasesByCphhAsync("", herdmark, "", includeNonGb)`

2. **Dedicated holding/herdmark page**
   - `src/BSE.Host/Pages/Search/CasesByHoldingHerdmark.cshtml`
   - `src/BSE.Host/Pages/Search/CasesByHoldingHerdmark.cshtml.cs`
   - Inputs/trigger: `CPH(H)`, `Herdmark`, `Numeric Herdmark` (+ `Include Non-GB Cases?`)
   - Query call: `GetCasesByCphhAsync(cphh, herdmark, numericHerdmark, includeNonGb)`

3. **Navigation updates**
   - Added separate links for both workflows in:
	 - `src/BSE.Host/Pages/Home.cshtml`
	 - `src/BSE.Host/Pages/Shared/_Layout.cshtml`

### Severity
- **Medium-High parity issue**: legacy business workflow is unavailable in migrated UI, although backend support already exists.

### Scope clarification
- `GetCasesByEartagHerdmarkAsync(...)` remains backend-supported but is **not** exposed as a user-facing page.
- This remains intentional for strict legacy UI parity (no separate legacy search-menu entry for eartag-herdmark).

---

## 6) Outstanding and Related Animals legacy-layout parity pass

### Outstanding Data Search (`/Search/Outstanding`)
- `src/BSE.Host/Pages/Search/Outstanding.cshtml`
  - Updated title/heading to `Outstanding Data Search`.
  - Replaced search-type dropdown with inline radios matching legacy options/order (`Outstanding Results`, `Fate`, `BSE1s`).
  - Updated date section label to `Form A Date Between` and aligned the two-date row with legacy-style positioning.
  - Updated checkbox wording to `Include Non-GB Cases?`.
  - Replaced clear-search action with `Search Menu` navigation link.

### Related Animal Search (`/Search/RelatedAnimals`)
- `src/BSE.Host/Pages/Search/RelatedAnimals.cshtml`
  - Updated title/heading to `Related Animal Search`.
  - Reworked form into legacy-style two-column layout and updated labels:
	- `RBSE Of Case`
	- `Eartag of Related Animal (starting with...)`
	- `RBSE of Related Animal`
	- `Name of Related Animal (starting with...)`
	- `Relation Type`
  - Replaced free-text `Relation Type` with dropdown-backed selection.
  - Replaced clear-search action with `Search Menu` navigation link.
- `src/BSE.Host/Pages/Search/RelatedAnimals.cshtml.cs`
  - Injected `ILookupDataService`.
  - Loaded relation-type options from `LookupTableId.RelationType` for dropdown binding.

### Validation update
- Solution build remains **successful**.
- Search module tests remain **27 passed, 0 failed**.
