# BSE Application — Full Parity Analysis Report

**Produced:** 2026-08-12  
**Legacy system:** `bsenet-v2-2025-10` (ASP.NET Web Forms, VB.NET, .NET Framework 4.x)  
**Migrated system:** `bse` (ASP.NET Core 10, C#, Razor Pages, GOV.UK Frontend v6.2.0)  
**Analysis scope:** All modules, screens, workflows, business rules, and CRUD operations

---

## Executive Summary

The migrated BSE application covers the core day-to-day workflows but has **significant functional gaps** in document generation, sub-entity editing (feeds, clinical visits, relations, test results), batch reporting, and several case-entry sub-workflows. Of the 87 legacy `.aspx` pages, approximately **38 functional screens** have been migrated (some consolidated), while **26 features/screens are missing or only partially implemented**. Fourteen legacy screens are housekeeping/infrastructure that do not require direct equivalents.

| Category | Count |
|---|---|
| Fully migrated | 38 screens / equivalents |
| Partially migrated | 7 screens |
| Not migrated — **action required** | 26 features/screens |
| Not required (popups, redirect helpers, error pages) | 16 screens |

---

## 1. Screen-by-Screen Parity Map

### 1.1 Core Navigation and Infrastructure

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `Home.aspx` | `/Home` | ✅ Complete | Role-guarded panels for VLA/DEFRA groups; batch entry panel present |
| `Redirect.aspx` | n/a | ✅ Not needed | Session-based redirect no longer required in stateless Razor Pages |
| `SessionError.aspx` | `/Error` | ✅ Equivalent | Standard error page covers session/app error scenarios |
| `AppError.aspx` | `/Error` | ✅ Equivalent | — |
| `Help.aspx` / `help.htm` | ❌ Missing | ❌ **Gap** | No help page exists in the migrated application |
| `CalendarPopup.aspx` | n/a — HTML `<input type="date">` | ✅ Replaced | Native browser date picker is GDS-compliant |
| `ExitConfirmationPopup.aspx` | n/a | ✅ Not needed | Stateless POST/Redirect/GET pattern makes unsaved-change tracking unnecessary |
| `BatchNumberDisplayPopup.aspx` | Inline on `/Home` | ✅ Equivalent | Batch panel on Home page shows batch numbers inline |

---

### 1.2 Case Management

| Legacy Screen | Migrated Equivalent | Status | Notes |
|---|---|---|---|
| `ShowCase.aspx` | `/Case/Details` | ✅ Complete | Combined case+farm view maintained; GB/non-GB display handled |
| `CaseEntryFarm.aspx` | `/Case/Farm` | ⚠️ Fixed | **3 parity gaps resolved (2026-08):** (1) Page heading said "Case:" — legacy `lblRBSEHeader` shows "RBSE Number:" — corrected. (2) Farm section heading said "Farm:" — legacy `lblCPHH` shows "CPHH:" — corrected. (3) Legacy `BatchNumberDisplay` control showed batch numbers linked to the case (VLA users) — absent in migrated page — added inline batch numbers card via `IBatchRepository.GetBatchNumbersByRbseAsync`. |
| `CaseEntryDEFRA.aspx` | `/Case/Edit` | ✅ Complete | Eartag, dates, fate, paperwork flags — all fields present in `CaseRecord` and `Edit.cshtml` |
| `CaseEntryVLA.aspx` | `/Case/Edit` | ⚠️ Partial | Core VLA fields (birth date, purchase date, onset date, slaughter date, months pregnant/post-calving) are in `Edit.cshtml`. **Missing:** *Other Owners (previous owners) sub-grid* — no UI to add/edit/delete previous owner records. |
| `CaseEntryBAB.aspx` | `/Case/Edit` | ⚠️ Partial | BAB flag exists in `CaseRecord.IsBAB`. **Missing:** *Traced CPHH, traced farm details (name, address, feed risk, animal origin notes)* — these BAB-specific traced-farm fields have no UI. |
| `CaseEntryClinical.aspx` | ❌ Missing | ❌ **Gap** | Clinical visit records (date, inspector, findings) are not displayed or editable in the migrated application. The `AnimalRelations` module and database are present but there is no UI page or sub-form. |
| `CaseEntryFeeds.aspx` | ❌ Missing | ❌ **Gap** | Feed records (year from/to, ration type, supplier, pre-purchase flag) have no UI. Service layer references exist but no page or sub-form is implemented. |
| `CaseEntryRelations.aspx` | ❌ Missing | ❌ **Gap** | Animal relations (dam/sire linkage, offspring relations grid) are fully absent from the UI. `IAnimalRelationsService` and all repository methods exist; zero front-end pages. `PickSireDam.aspx` helper also missing. |
| `CaseEntrySave.aspx` | `/Case/New` | ✅ Complete | New case creation with RBSE, CPHH, core fields; transactional SP |
| `NonGBCaseCreation.aspx` | ❌ Missing | ❌ **Gap** | Dedicated non-GB case creation workflow (non-GB eartag, fate, final result, slaughter date, non-GB CPHH lookup) is absent. `CaseRecord.IsNonGbCase` flag exists in the model but no creation path. |
| `DeleteCase.aspx` | `/Case/Delete` | ✅ Complete | Confirmation + cascading delete |
| `MoveCase.aspx` | `/Case/MoveCase` | ✅ Complete | Move to existing CPHH |
| `MoveCaseNewFarm.aspx` | ❌ Missing | ❌ **Gap** | Move case to a new (not-yet-existing) farm is absent. Legacy allowed creating a new farm inline during a case move. Migrated `/Case/MoveCase` only accepts an existing CPHH. |
| `RBSEChange.aspx` | `/Case/RbseChange` | ✅ Complete | Cascades rename across all 15 child tables |
| `FinalResultEntry.aspx` | `/Case/FinalResultEntry` | ✅ Complete (2026-10-09 audit) | RBSE lookup, test-results grid (paged/sortable), Final Result selection, retrospective test fields, Download/Print Memo — full field-by-field parity audit completed, see dedicated section below. |
| `FinalResultConfirmation.aspx` | `/MaintenanceConfirmation` | ✅ Complete | Shared generic confirmation page, populated via `TempData` from `FinalResultEntryModel.OnPostSaveAsync` — correct pattern per the existing role/permission audit of this same page (§ `CaseEntryDEFRA-vs-CaseEdit-E2E-Parity-Report.md`, "FinalResultConfirmation" false-positive note). |
| `CPHHChange.aspx` | `/Farm/CphhChange` | ✅ Complete (2026-10-09 audit) | Old/New CPHH lookup and change workflow — full field-by-field parity audit completed, see dedicated section below. 2 bugs found and fixed (Cancel button gating, CPHH display format). |

---

## 2. CPHH Change — Detailed Functional Parity Audit (2026-10-09)

Full end-to-end comparison of [CPHHChange.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CPHHChange.aspx.vb) /
[CPHHChange.aspx](../../bsenet-v2-2025-10/BSESystem/CPHHChange.aspx) against
[CphhChange.cshtml](../src/BSE.Host/Pages/Farm/CphhChange.cshtml) /
[CphhChange.cshtml.cs](../src/BSE.Host/Pages/Farm/CphhChange.cshtml.cs), covering fields, field
validation, field cascading, editable/non-editable logic, green/red star message displays, error
handling, and every UI action element.

### Role/permission gate — confirmed match

Legacy's `EnableControls()` only lets **DEFRA Maintenance** and **VLA Maintenance** reach the page; DEFRA
Viewer, DEFRA Data Entry, and VLA Data Entry all redirect to `Home.aspx`. Migrated's
`[Authorize(Policy = "DEFRAMaintenance")]` maps to exactly those two legacy groups (both carry the
`DEFRAMaintenance` claim — see `GroupClaimsTransformation.GetPoliciesForGroup`). **Match, no change
needed.**

### Bug found and fixed: Cancel was only reachable after a successful lookup

Legacy always renders **both** `btnOK` and `btnCancel` — only `btnOK` (and the New CPHH field) starts
disabled, pending a successful Look Up; `btnCancel` is enabled and clickable from the moment the page
loads, with `CausesValidation="False"` so it never gets blocked by any pending validation state.
Migrated's Cancel link was nested inside the `@if (Model.FarmFound)` block alongside the Change form —
meaning a user who hadn't looked up a farm yet (or whose lookup failed) had **no Cancel affordance at
all** on the page body (only the generic breadcrumb Home link). **Fixed:** moved the Cancel link out of
the `FarmFound` conditional so it is always rendered, matching legacy's always-available Cancel button;
OK and the New CPHH field remain hidden until a farm is found (an accepted hide-vs-disable mechanism
difference, consistent with the same pattern already established on other audited pages such as
Final Result Entry).

### Bug found and fixed: CPHH inputs did not redisplay in the slashed legacy format

Legacy's `CPHH.ascx` control always reconstructs the slashed `NN/NNN/NNNN/NN` display form on every
postback (`ConstructCPHH()`), for both the Old and New CPHH boxes. Migrated's `old-cphh`/`new-cphh`
inputs were bound directly to the raw `Model.OldCphh`/`Model.NewCphh` string, so a digits-only entry (or
a value redisplayed after a failed Change/Look Up) would **not** be reformatted — the same bug class
previously found and fixed on the Farm tab's new-case-creation CPHH field (see the "CPHH display format
parity on the new-case creation top bar" entry in `CaseEntryDEFRA-vs-CaseEdit-E2E-Parity-Report.md`).
**Fixed:** both inputs' `value` now go through `BseFormat.FormatCphh(...)`. Safe because every handler
that reads these fields already normalises via `CphhNormalizer.Normalize()` regardless of whether slashes
are present.

### Field validation and ordering — confirmed match

Legacy's `btnOK_Click` checks, in order: (1) New CPHH blank → "You must enter a new CPHH"; (2) Old ==
New → "The Old and New CPHHs are the same"; (3) `FarmInDatabase(new)` → "This Farm already exists in the
database."; (4) `ChangeCPHH` SP call. Migrated's `OnPostChangeAsync` reproduces the same three checks in
the same order (`NewCphhRequiredMessage` → `SameCphhMessage` → `NewCphhExistsMessage`), with one
additional check inserted ahead of them — a CPHH-format/length check (`InvalidCphhMessage`) — which in
legacy is instead enforced client-side by `revCPHH` (always-enabled `RegularExpressionValidator`) and
therefore never reaches the code-behind at all when malformed; reaching the equivalent check server-side
in migrated achieves the same net effect through a different (and, per this app's established
architecture, already-accepted) mechanism. **Match.**

### Error display mechanism — confirmed deliberate divergence, not changed

Legacy shows every validation/lookup failure on this page via each `CPHH.ascx` control's own red
`*`/`ToolTip` (`SetValidMark`) — the page's own static `lblError` label is dead code (`Visible="False"`,
zero code-behind references, confirmed by an exhaustive grep). Migrated shows these as full GOV.UK
`govuk-error-message` paragraphs instead of a star/tooltip. Consistent with the explicit preference
established earlier in this session (the equivalent red-star addition on Final Result Entry's RBSE field
was deliberately removed on request), **no star/tooltip was added here** — the GOV.UK paragraph style is
kept as the sole, intentional display mechanism for this page.

### SP-level failure codes — confirmed close-enough match, not changed

Legacy's `ChangeCPHH` wrapper reads the `ChangeCPHH` SP's `RETURN_VALUE` and throws on **any** non-zero
code (1 through 8 — farm-not-found/already-exists, and six different child-table update failures),
caught generically and surfaced as a single message: "Error updating the CPHH". Migrated's
`ChangeCphhResult` enum preserves all 9 distinct codes and maps code 1
(`OldCphhNotFoundOrNewCphhAlreadyExists`) to the more specific `NewCphhExistsMessage`, with codes 2–8
falling back to the same generic `UpdateFailedMessage` legacy would show for all of them. Slightly more
specific than legacy for code 1 (a rare race-condition path, since the app already pre-checks both old
and new CPHH before calling the SP), never less specific — an accepted, intentional improvement, not
reverted.

### Confirmed matching, no action needed

- **Farm summary fields** — Owner Name, Address 1–3, Postcode, Number of Confirmed Cases all load and
  clear together on lookup success/failure, matching `LoadFarmDetails()`/`EmptyFields`-equivalent reset
  behaviour (`Farm = null` on any failure inside the same try block).
- **OK button confirm dialog** — legacy's `btnOK.Attributes.Add("onClick", "javascript:return
  confirm('Click OK to confirm change');")` is reproduced exactly via the same `onclick="return
  confirm(...)"` text on migrated's OK button.
- **New CPHH enable/disable** — legacy enables `ctlNewCPHH`/`btnOK` only once Look Up succeeds; migrated
  achieves the same end state by not rendering that section until `Model.FarmFound` (hide vs. disable,
  same functional outcome, consistent with the pattern already accepted elsewhere).
- **Location fields not touched by CPHH change** — legacy's confirmation message explicitly warns that
  County/AHO/ADNS Region are not updated by this operation; migrated's `MaintenanceConfirmationModel`
  message text reproduces this warning verbatim in substance.

Files changed: `CphhChange.cshtml`. `get_errors` clean.

---

## Bug found and fixed: Search/Cases date-range filters were silently ignored (2026-10-09)

Reported symptom: entering only a start date (e.g. Form A Date "from") and clicking Search returned
every case, regardless of the date entered.

### Root cause

Legacy's `GetSearchCase` SP filters `ISNULL([Case].[FormADate], '1900') BETWEEN
ISNULL(@EarliestFormADate, '1900') AND ISNULL(@LatestFormADate, GETDATE())` — confirmed byte-for-byte
identical in the migrated [GetSearchCase.sql](../src/BSE.Database/StoredProcedures/Search/GetSearchCase.sql),
so a start-date-only search correctly means "on or after the start date, up to today" in both apps, and
the SP itself was not the problem. The bug was in
[CaseSearchViewModel.ToQuery()](../src/BSE.Host/Models/ViewModels/CaseSearchViewModel.cs): its private
`ParseDate` helper only accepted the strict ISO `yyyy-MM-dd` format
(`DateTime.TryParseExact(value, "yyyy-MM-dd", ...)`), but the six date fields on this page (Form A Date,
Final Result Date, Birth Date — each "from"/"to") are rendered as `moj-datepicker` inputs submitting
`dd/MM/yyyy` (confirmed in [Cases.cshtml](../src/BSE.Host/Pages/Search/Cases.cshtml) — `placeholder="dd/mm/yyyy"`,
`data-module="moj-date-picker"`). Every date typed therefore failed `ParseDate` silently and was passed
to the SP as `null` — meaning the date filter was **always** a no-op regardless of what the user
entered, while any other populated field, or no filter at all if the date was the only one filled in,
still triggered `HasAnyFilter()` and ran an effectively unfiltered search. The page's own `ValidateDates()`
check uses the separate, correct `SearchDateField.TryParse` helper (which already accepts both
`dd/MM/yyyy` and ISO), so the date was accepted as valid input and no error was ever shown — the bug was
invisible until the results came back unfiltered.

### Fix

`CaseSearchViewModel.ParseDate` now delegates to the same `SearchDateField.TryParse` already used by
`ValidateDates()`, instead of its own narrower, inconsistent ISO-only parser. Removed the now-unused
`System.Globalization` import. All six date-range filters (Form A Date, Final Result Date, Birth Date)
go through this one method, so all three are fixed together, not just Form A Date.

Files changed: `CaseSearchViewModel.cs`. `get_errors` clean.

---

## Search/Cases date fields — full display/conversion/validation/persistence audit (2026-10-09)

Follow-up to the parsing-bug fix above: a complete comparison of the six date-range fields (Form A
Date, Final Result Date, Birth Date — each "from"/"to") against legacy's `CalendarDate.ascx[.vb]` +
`IsDateRangeValid` (`Common.vb`) + `clsSearch.GetCaseSearchResults`.

### Bug found and fixed: range-order error messages were missing the field name

Legacy's `IsDateRangeValid(ctlDateFrom, ctlDateTo, sName)` produces
`"Must be earlier than the specified latest " & sName` / `"Must be later than the specified earliest "
& sName`, called once per field with `sName` = "Form A Date", "Final Result Date", "Birth Date". Migrated's
`CaseSearchViewModel.CheckOrder` used one generic pair of messages ("Must be earlier than the latest
date" / "Must be later than the earliest date") for all three fields, with no field name — so a user
with an invalid Birth Date range couldn't tell from the message alone which of the three ranges was
wrong. **Fixed:** `CheckOrder` now takes the field name and reproduces legacy's exact wording per field.

### Confirmed matching, no action needed

- **Display/round-trip of typed text** — legacy's `CalendarDate.ascx` never reformats the on-screen
  textbox after a postback (the zero-padding in `FormattedDate` only applies to the transient value
  read via the `DateField` property getter when building the search parameters, not to what's
  redisplayed in the box); migrated's `asp-for` binding likewise redisplays exactly what was
  typed/posted, with no server-side reformatting. Genuinely equivalent behaviour, not just an
  accepted difference.
- **Leading-zero normalisation while typing** — legacy has no live client-side reformatting at all
  (a manual textbox); migrated's `moj-date-picker` (`data-leading-zeros="true"`) live-pads single-digit
  day/month segments in the browser — confirmed the real MOJ Frontend JS (not dead markup) via
  `wwwroot/moj/moj-frontend.min.js`'s `leadingZeros()` method. A UI enhancement beyond legacy, not a
  functional divergence (the ultimate stored/queried value is unaffected either way).
- **Accepted input formats** — `SearchDateField.TryParse` accepts `dd/MM/yyyy`, `d/M/yyyy` and
  single-leading-zero variants, plus ISO `yyyy-MM-dd`; legacy's VB `IsDate`/`CDate` is more permissive
  (accepts almost any .NET-recognisable date string) but the only format ever actually exposed to the
  user is the same `dd/mm/yyyy` free-text box in both apps, so this wider legacy permissiveness is not
  reachable through the real UI in either system. Not changed.
- **"Please enter a valid date" / `SearchDateField.InvalidDateMessage`** — intentionally kept as the
  existing shared, generic GDS-style message (`"Enter a valid date"`), not changed to match legacy's
  per-control red-star tooltip wording ("Invalid Date") — this message is shared by several other search
  pages (Farms, Outstanding, CasesByHoldingHerdmark), so narrowing it to Cases-specific, legacy-exact
  wording would require a wider, separate change outside this page's scope.
- **"Show all errors at once" vs. legacy's short-circuit** — legacy's `btnSearch_Click` checks each of
  the three date ranges in sequence and `Exit Sub`s on the first failure, so only one range's error is
  ever shown per submission; migrated's `ValidateDates()` evaluates all six fields and all three ranges
  unconditionally, surfacing every problem in one round trip. A deliberate, already-established GDS/UX
  improvement pattern used consistently elsewhere in this app (e.g. Outstanding's search) — not reverted.
- **SQL parameter semantics** — legacy passes the date as a `dbtDate`-typed string parameter (via
  `FormatEmptyString`, DBNull when blank); migrated passes a typed `DbType.DateTime` (DBNull when null).
  Both ultimately bind to the same `datetime` SP parameters with identical `BETWEEN
  ISNULL(@Earliest,'1900') AND ISNULL(@Latest, GETDATE())` semantics (confirmed byte-identical SP SQL)
  — no behavioural difference.
- **No-criteria / "Include Non-GB Cases alone" rule** — already reviewed and documented as a deliberate,
  business-confirmed divergence from legacy in the existing `HasAnyFilter()` comment; not re-litigated
  here as it is unrelated to date-field handling specifically.

Files changed: `CaseSearchViewModel.cs`. `get_errors` clean.

---

## Bug found and fixed: Cancel didn't warn after a CPHH lookup on a brand-new case (2026-10-09)

Reported symptom: on `/Case/Farm` in new-case-creation mode, looking up an existing CPHH (populating
Owner Name/Address/etc. from the found farm) then clicking Cancel showed no "unsaved changes" prompt.

### Root cause

Both the generic `_CaseTabs.cshtml` Home-link guard and `Farm.cshtml`'s own `.bse-cancel-link` handler
ultimately rely on `isCurrentFormDirty()`, which detects changes by diffing the current form against a
snapshot taken on `DOMContentLoaded`. A successful CPHH lookup (`OnPostLookupNewCaseAsync`) returns
`Page()` directly with the found farm's data already populated — there is no earlier, blanker render the
browser ever displays, so the very first snapshot the JS captures already includes the looked-up data.
Nothing then differs from that baseline, so the dirty-check always reports "no changes" even though
legacy's equivalent (populating the shared session farm dataset via `CheckLookupCPHH`) is exactly the
kind of change that makes legacy's `DataSetHasChanges()` return true for the rest of that session.

### Fix

`isCurrentFormDirty()` (`_CaseTabs.cshtml`) now also treats the presence of a
`[data-force-dirty="true"]` marker element inside the current case form as dirty, in addition to the
existing snapshot diff — a small, reusable hook for exactly this "the initial render itself is already
a change" case. `Farm.cshtml`'s create-mode form now renders that hidden marker whenever
`!RequireFarmDetails` and the farm fields are populated (i.e. a lookup found a real farm). Both the
generic Home-link guard and Farm's own Cancel-link handler call through to this same function, so both
are fixed by the one change.

Files changed: `_CaseTabs.cshtml`, `Farm.cshtml`. `get_errors` clean on both.

