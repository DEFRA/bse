# Search Pages Parity Gap Report (Migrated Razor Pages)

## Scope

This document compares current migrated Razor Pages in `src/BSE.Host/Pages/Search` against backend-supported search capabilities (query models + stored procedures) in `src/BSE.Modules.Search` and `src/BSE.Database/StoredProcedures/Search`.

---

## 1) Farm Search (`/Search/Farms`)

| Area | Backend-supported | Migrated page status | Gap/impact |
|---|---|---|---|
| Filters | `CPHH`, `OwnerName`, `Address`, `County`, `Herdmark`, `NumericHerdmark`, `IsDealer`, `AHO`, `IncludeNonGBFarms` | Present in UI | Mostly aligned |
| Dealer semantics | `@IsDealer` supports tri-state behavior in SQL (`NULL` = any, `0`, `1`) | `FarmSearchQuery.IsDealer` is `bool` and VM maps `IsDealer ?? false` | `Dealer=Any` becomes `false` (not true tri-state) |
| Result columns | Includes `NumericHerdmark`, `MapReference`, `AHO`, `HerdType`, `CorrespondenceAddress`, counts | UI shows subset | Non-blocking parity gap in displayed columns |

**Evidence**
- UI: `src/BSE.Host/Pages/Search/Farms.cshtml`
- VM mapping: `src/BSE.Host/Models/ViewModels/FarmSearchViewModel.cs`
- Query: `src/BSE.Modules.Search/Models/FarmSearchQuery.cs`
- SP: `src/BSE.Database/StoredProcedures/Search/GetSearchFarm.sql`

---

## 2) Case Search (`/Search/Cases`)

| Area | Backend-supported | Migrated page status | Gap/impact |
|---|---|---|---|
| Core filters | `RBSE`, `Eartag`, `DBSE`, `Fate`, `FinalResult`, `Survey` | Present | Aligned for these fields |
| Additional filters | `Sex`, `Notes`, `PassiveActive`, `IsImportedCase` | Missing from UI | Cannot apply these legacy filters |
| Date range filters | Form A, Final Result, Birth Date ranges | UI has Form A only | Missing date-range filtering options |
| Include non-GB only search | `IncludeNonGBCases` | Checkbox present | `HasAnyFilter()` ignores this flag when used alone |
| Search trigger | Any populated filter should trigger search | `HasAnyFilter()` only checks subset | Some valid filters do not execute search |
| Result columns | SP returns extra fields (`FinalResultDate`, `DBSE`, `Notes`, `BabNotes`, `Origin`, `ValuationAge`, `IsBirthDateEst`) | UI shows reduced columns | Reduced result visibility |

**Evidence**
- UI: `src/BSE.Host/Pages/Search/Cases.cshtml`
- PageModel trigger logic: `src/BSE.Host/Pages/Search/Cases.cshtml.cs`
- VM: `src/BSE.Host/Models/ViewModels/CaseSearchViewModel.cs`
- Query: `src/BSE.Modules.Search/Models/CaseSearchQuery.cs`
- SP: `src/BSE.Database/StoredProcedures/Search/GetSearchCase.sql`

---

## 3) Outstanding Data Search (`/Search/Outstanding`)

| Area | Backend-supported | Migrated page status | Gap/impact |
|---|---|---|---|
| Filters | `EarliestFormADate`, `LatestFormADate`, `IncludeNonGBCases` | Present | Aligned |
| Search type routing | BSE1/Fates/Results | Present | Aligned |
| Results shape | `RBSE`, `CPHH`, `Eartag`, `FormADate`, `BirthDate`, `Fate`, `FinalResult` | Present | Aligned |

**Note**
- Current behavior requires at least one Form A date to run search (`OnGetAsync` check). If legacy allowed no-date execution, this is a behavioral difference.

**Evidence**
- UI: `src/BSE.Host/Pages/Search/Outstanding.cshtml`
- PageModel: `src/BSE.Host/Pages/Search/Outstanding.cshtml.cs`
- Query/Result: `src/BSE.Modules.Search/Models/OutstandingSearchQuery.cs`, `OutstandingCaseResult.cs`
- SPs: `GetSearchOutstandingBSE1s.sql`, `GetSearchOutstandingFates.sql`, `GetSearchOutstandingResults.sql`

---

## 4) Related Animals Search (`/Search/RelatedAnimals`)

| Area | Backend-supported | Migrated page status | Gap/impact |
|---|---|---|---|
| Filters | `RBSE`, `Name`, `Eartag`, `RelationRBSE`, `RelationType` | All fields present in UI | Input coverage aligned |
| Search trigger | Backend can filter by all 5 params | PageModel triggers only when `RBSE` or `Eartag` or `Name` is provided | Searches by only `RelationRBSE`/`RelationType` do not run |
| Result columns | `RBSE`, `CPHH`, `RelationType`, `RelSex`, `Eartag`, `RelBirthDate`, `RelFate`, `LeftDate`, `RelName`, `RelEartag`, `RelationRBSE` | UI shows subset (`RBSE`, `Eartag`, `RelName`, `RelationType`, `RelationRBSE`) | Reduced result visibility |

**Evidence**
- UI: `src/BSE.Host/Pages/Search/RelatedAnimals.cshtml`
- PageModel: `src/BSE.Host/Pages/Search/RelatedAnimals.cshtml.cs`
- Result model: `src/BSE.Modules.Search/Models/RelatedAnimalResult.cs`
- SP: `src/BSE.Database/StoredProcedures/Search/GetSearchRelatedAnimals.sql`

---

## Why these gaps exist in migrated code

Across search pages, the migrated implementation follows a simplified Razor UI pattern:

1. Backend contracts (query models + SPs) were retained.
2. UI/forms were reduced to a smaller subset of fields.
3. Some `HasAnyFilter()` checks were implemented for only partial field sets.
4. Result tables were simplified, so not all returned fields are shown.

This creates parity gaps where backend supports more than the migrated page exposes.