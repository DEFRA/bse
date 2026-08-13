# Search Pages Parity Fix Priority Plan


## Scope

This plan prioritizes parity fixes for migrated Razor Pages under `src/BSE.Host/Pages/Search` by impact and risk.

---

## Priority Summary

| Priority | Page | Reason |
|---|---|---|
| High | `Cases` | Missing multiple backend-supported filters and date ranges; search trigger logic is incomplete. |
| High | `RelatedAnimals` | Search cannot run for valid filter-only combinations (`RelationRBSE` / `RelationType` only). |
| High | `Farms` | `Dealer=Any` currently maps to `false`, not true tri-state behavior. |
| Medium | `Cases` | Result table omits several backend-returned fields users may rely on. |
| Medium | `RelatedAnimals` | Result table omits several backend-returned columns. |
| Low | `Outstanding` | Mostly aligned; confirm whether date-required execution is intended legacy behavior. |

---

## 1) High Priority Fixes

### 1.1 Cases page (`/Search/Cases`)

**Files**
- `src/BSE.Host/Pages/Search/Cases.cshtml`
- `src/BSE.Host/Pages/Search/Cases.cshtml.cs`
- `src/BSE.Host/Models/ViewModels/CaseSearchViewModel.cs`

**Fixes**
1. Add missing filters to UI:
   - `Sex`, `Notes`
   - Final Result date range (from/to)
   - Birth date range (from/to)
   - `PassiveActive`
   - `IsImportedCase`
2. Update `HasAnyFilter()` to include all supported filters (including `IncludeNonGb`).
3. Keep `ToQuery()` mapping aligned with all query properties.
4. Optionally add validation/messages for date range consistency.

**Acceptance checks**
- Any single filter can trigger a search.
- `PassiveActive` and `IsImportedCase` are passed and honored.
- Final/Birth date filters affect result set as expected.

---

### 1.2 Related Animals page (`/Search/RelatedAnimals`)

**Files**
- `src/BSE.Host/Pages/Search/RelatedAnimals.cshtml.cs`

**Fixes**
1. Update search trigger condition so any non-empty filter starts search:
   - `RBSE`, `Name`, `Eartag`, `RelationRBSE`, or `RelationType`.

**Acceptance checks**
- Search runs when only `RelationRBSE` is set.
- Search runs when only `RelationType` is set.

---

### 1.3 Farms page (`/Search/Farms`)

**Files**
- `src/BSE.Modules.Search/Models/FarmSearchQuery.cs`
- `src/BSE.Host/Models/ViewModels/FarmSearchViewModel.cs`
- `src/BSE.Modules.Search/Repositories/SearchRepository.cs` (verify unchanged mapping behavior)

**Fixes**
1. Restore true tri-state dealer filter:
   - Query model should allow nullable dealer flag (`bool?`) or equivalent.
   - VM should pass null for “Any”, not coerce to `false`.
2. Ensure SQL parameter handling preserves “Any/Yes/No” semantics.

**Acceptance checks**
- `Dealer=Any` returns both dealer and non-dealer farms.
- `Dealer=Yes` and `Dealer=No` filter correctly.

---

## 2) Medium Priority Fixes

### 2.1 Cases results table enrichment

**File**
- `src/BSE.Host/Pages/Search/Cases.cshtml`

**Candidate columns to add**
- `FinalResultDate`, `DBSE`, `Notes`, `BabNotes`, `Origin`, `ValuationAge`, `IsBirthDateEst`

**Acceptance checks**
- Extra returned fields are visible and correctly formatted.

---

### 2.2 Related Animals results table enrichment

**File**
- `src/BSE.Host/Pages/Search/RelatedAnimals.cshtml`

**Candidate columns to add**
- `CPHH`, `RelSex`, `RelBirthDate`, `RelFate`, `LeftDate`, `RelEartag`

**Acceptance checks**
- Output reflects full backend result shape.

---

## 3) Low Priority Validation

### 3.1 Outstanding search behavior confirmation

**Files**
- `src/BSE.Host/Pages/Search/Outstanding.cshtml.cs`

**Check**
- Confirm with business/legacy behavior whether date-less search should be blocked.
- If parity requires, allow search without dates.

---

## Suggested Execution Order

1. Fix `Farms` dealer tri-state semantics.
2. Fix `RelatedAnimals` trigger logic.
3. Implement full `Cases` filter parity + trigger updates.
4. Add optional result-column enrichments (`Cases`, `RelatedAnimals`).
5. Validate `Outstanding` behavior with product owner.

---

## Test Strategy (minimum)

1. Add/update unit tests in search page model tests for `HasAnyFilter()` / trigger behavior.
2. Add repository/service tests for dealer tri-state behavior.
3. Add lightweight integration tests for:
   - `Cases` full filter binding
   - `RelatedAnimals` relation-only filters
   - `Farms` dealer Any/Yes/No behavior

---

## Risks

- UI expansion may require layout updates for usability.
- Legacy field labels/value mappings (`PassiveActive`, relation types) may need exact wording/value compatibility.
- Nullable filter semantics must remain consistent through model binding to SQL parameters.