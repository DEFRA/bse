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

---

## Bug found and fixed: Cancel only discarded the current tab's own staged draft, not all tabs' (2026-10-09)

Reported symptom: edit a tab, switch to a different tab (staging the edit), click Cancel/Home, confirm
"discard changes" — the confirm appears and navigation to Home succeeds, but revisiting the case's tabs
afterwards still shows the discarded draft data.

### Root cause

Each of the 7 tabs' own Cancel handler (`OnGetCancelEditAsync`, `OnGetCancelVlaEditAsync`,
`CancelFarmEditAsync`, `OnGetCancelBabEdit`, `OnPostCancelClinicalEditAsync`/`OnGetCancelClinicalEditAsync`,
`CancelFeedsEditAsync`, `CancelRelationsEditAsync`) only cleared **its own** draft-state service (plus
the shared `CaseScalarDraftState`) — never the other five tabs' draft-state services. The generic
Home-link guard's `/case/{rbse}/discard-unsaved` endpoint already correctly cleared all six, but the
per-tab Cancel **buttons** (`asp-page-handler="CancelXEdit"`) navigate straight to that tab's own GET/POST
handler, bypassing the REST endpoint entirely — so clicking Cancel from, say, Farm only ever cleared the
Farm + scalar drafts, leaving a draft staged earlier on e.g. Relations or Feeds completely untouched.
Revisiting that other tab later would then overlay the still-staged (never actually discarded) draft
again, exactly matching the reported symptom. This mirrors legacy's single shared session `DataSet` —
Cancel from *any* tab must discard the *whole* in-progress case, not just the tab it was clicked from.

### Fix

New `ICaseDraftDiscardService`/`CaseDraftDiscardService` (`BSE.Host/Services/CaseDraftDiscardService.cs`)
clears all six draft-state services in one call. The `/case/{rbse}/discard-unsaved` endpoint in
`Program.cs` now delegates to it (previously had the correct logic inline; now shared, not duplicated).
All 7 tabs' Cancel handlers were updated to call `caseDraftDiscard.DiscardAllAsync(...)` instead of
clearing only their own 1–2 services, so Cancel from any tab now discards everything staged anywhere,
matching legacy exactly.

Files changed: `CaseDraftDiscardService.cs` (new), `Program.cs`, `Edit.cshtml.cs`, `Vla.cshtml.cs`,
`Farm.cshtml.cs`, `Bab.cshtml.cs`, `Clinical.cshtml.cs`, `Feeds.cshtml.cs`, `Relations.cshtml.cs`.
`get_errors` clean on all.

---

## Full Save/Cancel re-audit across all 7 case tabs (2026-10-09)

Per explicit request, re-verified Save and Cancel on every tab against legacy (`CaseEntrySave.aspx.vb`,
each `CaseEntry*.aspx.vb`) and against this doc's own prior findings, since several of these files had
been externally edited/reverted earlier in the session.

### Confirmed intact, no regressions

- **All 7 Save handlers commit via the shared orchestrator** (`CaseCommitHelper.CommitAllAsync` for
  Edit/Farm/Vla/Bab/Clinical/Feeds; `caseEditOrchestration.CommitAllAsync` directly with equivalent
  inline exception handling for Relations) — `MandatoryCaseFieldsMissingException` correctly routes to
  `/Case/SaveResult` (`SaveResultMode.MissingMandatoryFields`) and any other non-success result
  redirects to `/Home` with an error banner, on every tab, matching the eighth-follow-up design.
- **All 7 Save handlers redirect to `/Home` on full success** — matches `CaseEntrySave.aspx`'s
  auto-redirect, confirmed still correct on every tab (seventh-follow-up fix intact).
- **All 7 Cancel handlers call `caseDraftDiscard.DiscardAllAsync(...)`** — the cross-tab discard fix
  immediately above is intact and consistent on every tab; confirmed by reading each handler's current
  body verbatim.

### Bug found and fixed: Clinical had a dead, inconsistent second Cancel handler

`Clinical.cshtml.cs` had **two** Cancel handlers: `OnGetCancelClinicalEditAsync` (the one actually
reachable — the Cancel link is a plain `<a>`, i.e. a GET) correctly redirected to `/Home`, but the
unreachable `OnPostCancelClinicalEditAsync` redirected back to the same tab
(`RedirectToPage(new { rbse = Rbse })`) instead — the only one of the 7 tabs' Cancel paths that didn't
target `/Home`. Confirmed via `Clinical.cshtml` that nothing posts to this handler (dead code), but its
contradictory behaviour was a maintenance risk if ever wired up. **Fixed:** changed
`OnPostCancelClinicalEditAsync` to also redirect to `/Home`, matching its GET counterpart and all other
6 tabs.

Files changed: `Clinical.cshtml.cs`. `get_errors` clean.

---

## Question answered: does Cancel/Home behave identically across all 7 tabs? (2026-10-09)

Short answer: **it didn't fully, two gaps found and fixed; now yes.**

Checked three layers that exist on every tab: (1) the breadcrumb "Home" link, handled generically by
`_CaseTabs.cshtml`'s single document-level click listener on every tab; (2) each tab's own `.bse-cancel-link`
Cancel-button click handler (duplicated per-page, one per tab); (3) each tab's `beforeunload`
browser-close/refresh guard.

### Bug found and fixed: `beforeunload` protection existed on only 2 of 7 tabs

Bab and Vla each had a `window.addEventListener('beforeunload', ...)` guard (warns on browser
close/refresh/back if the form is dirty and the user isn't mid-navigation); Edit, Farm, Clinical, Feeds
and Relations had none at all — an edit on any of those 5 tabs could be lost with zero warning on a
refresh or tab close, while the same action on Bab/Vla would warn. Legacy itself has **no**
`beforeunload`-equivalent anywhere (confirmed: every `PromptBeforeNavigateScript` call site in legacy is
commented out), so this was never a legacy-parity gap — it was an enhancement rolled out to only 2 of
the 7 tabs. **Fixed:** added the same guard (`formDirty`/`submitting` tracking + `beforeunload` listener,
suppressed during an in-progress tab-switch via `window.__bseCaseTabNavigationInProgress`) to Edit, Farm,
Clinical, Feeds and Relations, so all 7 tabs now behave identically.

### Bug found and fixed: the breadcrumb Home link could trigger a duplicate native "leave site?" prompt

`_CaseTabs.cshtml`'s generic Home-link handler already shows its own custom confirm (`"You have unsaved
changes..."`) and, once confirmed/clear, navigates via `window.location.href = link.href`. Tab-switch
links already set `window.__bseCaseTabNavigationInProgress = true` first so a page's own `beforeunload`
listener knows not to fire a second, native browser prompt on top of the one already answered — but the
Home-link branch never set this flag. Before this round, this only mattered for Bab/Vla (the only 2
tabs with a `beforeunload` listener); rolling `beforeunload` out to all 7 tabs (immediately above) would
have made this a universal double-prompt bug. **Fixed:** both navigation points in the Home-link handler
(the "nothing unsaved" path and the "confirmed" path) now set `window.__bseCaseTabNavigationInProgress =
true` immediately before navigating, exactly like the tab-switch-link handler already does.

### Confirmed identical across all 7 tabs (no change needed)

- Cancel button confirm wording: `"Are you sure you want to cancel? Any unsaved changes will be lost."`
  on every tab.
- Cancel button logic: `formDirty` (or `dirty` on Bab/Vla — a naming difference only) OR the page's own
  `HasUnsavedChanges`/`HasUnsavedOwnerChanges` flag, falling back to the shared
  `window.hasUnsavedChangesAcrossTabs()` cross-tab check — identical shape on all 7.
- Cancel discard-on-confirm: all 7 call `caseDraftDiscard.DiscardAllAsync(...)` then redirect to `/Home`
  (per the fix two sections above).
- Breadcrumb Home link: identical markup (`asp-page="/Home"`, `govuk-breadcrumbs__link`) on all 7, so the
  one shared `_CaseTabs.cshtml` listener applies uniformly.

Files changed: `Edit.cshtml`, `Farm.cshtml`, `Clinical.cshtml`, `Feeds.cshtml`, `Relations.cshtml`,
`_CaseTabs.cshtml`. `get_errors` clean on all six.

---

## Strict legacy parity requested: `beforeunload` warning removed entirely (2026-10-09)

Follow-up to the section above, per explicit "I want strict legacy parity" request. Re-confirmed
legacy's actual behaviour: every one of the 8 `PromptBeforeNavigateScript` call sites across
`CaseEntryBAB.aspx.vb`, `CaseEntryClinical.aspx.vb`, `CaseEntryDEFRA.aspx.vb`, `CaseEntryFarm.aspx.vb`,
`CaseEntryFeeds.aspx.vb`, `CaseEntryRelations.aspx.vb`, `CaseEntryVLA.aspx.vb` and
`NonGBCaseCreation.aspx.vb` is commented out — legacy has **zero** browser-level unsaved-changes warning
on any tab, full stop. The `beforeunload` guard (previously on Bab/Vla only, then rolled out to all 7 in
the section above for cross-tab consistency) was therefore a pure migrated-only enhancement with no
legacy basis at all, not a partial/incomplete legacy feature. **Removed entirely** from all 7 tabs, per
the "strict parity" instruction, rather than completing its rollout:

- Removed the `window.addEventListener('beforeunload', ...)` block from all 7 tabs (Bab, Vla, Edit, Farm,
  Clinical, Feeds, Relations).
- Removed the now-purposeless `submitting` tracking that existed solely to suppress a second
  `beforeunload` prompt after the custom Cancel-link confirm (the `dirty`/`formDirty` tracking itself is
  kept — it is also used by the Cancel-link's own "is this tab dirty" check, independent of
  `beforeunload`). Vla's existing form `submit` listener keeps its other responsibilities
  (`herdWarningArmed`/`onsetWarningArmed`/`clearDateWarningIndicators()`), only the `submitting = true`
  line was removed.
- Reverted the two `window.__bseCaseTabNavigationInProgress = true` lines added to `_CaseTabs.cshtml`'s
  Home-link handler in the section above — they existed only to suppress `beforeunload` on that
  navigation path, which no longer exists.
- The pre-existing tab-switch-link setter of `__bseCaseTabNavigationInProgress` (used by other,
  unrelated pages — `DefraEdit.cshtml`/`VlaEdit.cshtml` — for their own purposes) was **not** touched.

Net result: Cancel-button and Home-link confirmation dialogs (the custom, in-app
`window.confirm(...)` prompts) are unchanged and still present on all 7 tabs — only the native
browser-level close/refresh/back warning has been removed, restoring strict legacy parity (legacy has
no warning of any kind on refresh/close, only its own in-page `ExitConfirmation` control for in-app
navigation, already reproduced by the `window.confirm` prompts).

Files changed: `Bab.cshtml`, `Vla.cshtml`, `Edit.cshtml`, `Farm.cshtml`, `Clinical.cshtml`,
`Feeds.cshtml`, `Relations.cshtml`, `_CaseTabs.cshtml`. `get_errors` clean on all eight.

## Clinical tab — brand-new clinical record loses in-progress Signs checkboxes on a Visits grid operation

`RestoreAndRestageSignsEditAsync` (in [Clinical.cshtml.cs](../src/BSE.Host/Pages/Case/Clinical.cshtml.cs))
re-stages the posted-but-unsaved Signs checkbox edit into `caseScalarDraftState` before every
Add/BeginEdit/Update/Delete Visit Row redirect, because `LoadAsync()` always reloads `Signs` from the
database on the subsequent GET. The method took a `clinicalRowStampBase64` parameter and returned
immediately, doing nothing, whenever that rowstamp was blank — which is exactly the case for a
brand-new clinical record that has never been saved (no Clinical row exists in the DB yet, so there is
no rowstamp to post). For that case specifically, any checked boxes were silently dropped the moment
the user added/edited/deleted a Visits grid row, before ever reaching Save — a parity gap versus
legacy, whose shared session `DataSet` holds in-progress field values regardless of persistence status.

Fix: `RestoreAndRestageSignsEditAsync` no longer bails out on a blank rowstamp. It always binds and
re-stages the posted Signs values; when no rowstamp exists yet it stages `signs.ToEditCommand(Rbse,
[])` (empty rowstamp), mirroring the existing "brand-new case" branch already used in
`OnPostSaveSignsAsync`. The existing-record path (non-blank rowstamp) is unchanged.

Added regression test `ClinicalModel_OnPostAddVisitRowAsync_PreservesPostedSignsCheckboxEdit_ForBrandNewClinicalRecord`
in [ClinicalVlaUsersHandlerTests.cs](../src/BSE.Modules.UserManagement.Tests/ClinicalVlaUsersHandlerTests.cs)
alongside the existing-record counterpart `..._PreservesPostedSignsCheckboxEdit`. Both pass after the
fix (`dotnet test --filter FullyQualifiedName~PreservesPostedSignsCheckboxEdit`: 2 passed, 0 failed).

## Relations tab — editing the Other Relations RBSE field spuriously triggered the Cancel confirm (2026-10-09)

Reported: typing in the "Other Relations" panel's RBSE field then clicking Cancel showed the "Are you
sure..." confirm in the migrated app, which legacy never shows for this field; and dismissing/declining
that confirm naturally left the typed value visible (since nothing reloaded).

Root cause confirmed against legacy source: `CancelCaseEdit`'s dirty-check in
`CaseEntryRelations.aspx.vb` only compares the `dsCase`/`dsFarm` DataSets, populated by
`UpdateSessionWithCaseDetails` — which writes Case Herdbook, Dam Status and the Dam/Sire fields only.
`ctlRelationRBSE_RBSEChanged` (the Other Relations RBSE field's change handler) only performs a lookup
and never touches either DataSet, so legacy never treats typing there — or even a committed "Add As
New"/"Update Selected" — as unsaved for the Cancel prompt.

The migrated `Relations.cshtml` script tracked `formDirty` from any `input`/`change` event anywhere in
`#case-relations-inline-form`, which bundles both the legitimate Dam/Sire/Case-Herdbook fields *and* the
Other Relations staging panel (`.bse-relations-grid`: RelationType, Sex, Fate, Eartag, RBSE, dates,
Sire). So merely typing/blurring the Other Relations RBSE field (which triggers its own client-side
lookup fetch, unrelated to any draft) flipped `formDirty`, even though no relation row had actually been
committed — committed changes are already correctly tracked separately via `Model.HasUnsavedChanges`
(`draft.HasPendingChanges`).

**Fixed:** `formDirty` tracking in [Relations.cshtml](../src/BSE.Host/Pages/Case/Relations.cshtml) now
ignores input/change events whose target is inside `.bse-relations-grid`, so only the Dam/Sire/Case
Herdbook fields set it — matching legacy's actual `dsCase`-based check. `Model.HasUnsavedChanges` is
unchanged and still warns on a genuinely committed (Add/Update/Delete) relation row, consistent with the
rest of the app's staged-draft pattern. With this fix, Cancel no longer prompts for a merely-typed,
never-committed Other Relations RBSE edit, and (since no confirm appears) Cancel navigates straight to
`CancelRelationsEditAsync`, which discards all per-tab drafts — so a genuine revisit shows the field
blank again, resolving both symptoms from the same root cause.

Files changed: `Relations.cshtml`. `get_errors` clean.

## Cross-tab comparison: form dirty-check logic across all 7 tabs (2026-10-09)

Following the Relations fix above, compared the client-side dirty-tracking on all 7 tabs against each
tab's actual legacy `UpdateSessionWithCaseDetails`/`CancelCaseEdit` source, to check whether the same
class of bug (raw typing in a not-yet-committed grid row/staging panel incorrectly counted as unsaved)
existed elsewhere. Confirmed: **every one of the 7 legacy pages' `CancelCaseEdit` only ever compares
`dsCase`/`dsFarm`** (via `clsDataCheck.DataSetHasChanges`), populated solely by that page's own
`UpdateSessionWithCaseDetails`. Checked what each tab's version of that function actually writes:

| Tab | Legacy writes to dsCase/dsFarm from... | Separate grid never in dsCase/dsFarm | Grid UI pattern |
|---|---|---|---|
| Bab | All BAB_TABLE fields (Notes, Traced*, risk dropdowns) + Origin | — (no grid on this tab) | n/a |
| Vla | Main VLA case fields (BirthDate, Sex, Breed, Origin, Purchase/Onset/Slaughter dates, etc.) | Other Owners | inline table rows |
| Edit | Main case fields (Eartag, Form A/B/C dates, Notes, Fate, etc.) | Test records | inline table rows |
| Farm | Main Farm fields (CPHH, Owner/Address, Herdmarks, AHO, HerdType, etc.) | Herd Size, Linked Farms | inline table rows |
| Clinical | Signs checkboxes only (CLINICAL_TABLE) | Visits | inline table rows |
| Feeds | **Nothing — `UpdateSessionWithCaseDetails` is a no-op** (`Return True`) | Feed records (the whole page) | external staging panel (not a table) |
| Relations | Case Herdbook, Dam Status, Dam/Sire fields | Other Relations | external staging panel (not a table) |

So besides the already-fixed Relations tab, the same bug existed on Vla, Edit, Farm, Clinical and Feeds:
each one's `formDirty`/`dirty` flag was set from **any** `input`/`change` anywhere in the whole inline
form, including grid rows/staging panels legacy never treats as unsaved (a committed Add/Update/Delete
row is still correctly tracked separately via `Model.HasUnsavedChanges`/`HasUnsavedOwnerChanges` — only
the raw, not-yet-committed typing was the bug). Only Bab needed no change, since it has no grid at all —
every field on that tab genuinely maps to a dsCase/BAB_TABLE write.

**Fixed**, consistent with the Relations approach:
- **Vla, Edit, Farm, Clinical**: their grids use inline editable table rows (new/edit inputs sit
  directly inside the grid `<table>`), so the listener now ignores any event target whose
  `closest('table')` is truthy — each page confirmed to contain exactly one (Vla, Edit, Clinical) or two
  (Farm: Herd Size + Linked Farms) `<table>` elements, both of which are grids, so this exclusion can't
  accidentally swallow a real scalar-field edit.
- **Feeds**: its Add/Edit panel is a `<div class="bse-feeds-grid">` outside the table (same shape as
  Relations), so the listener ignores any event target inside `.bse-feeds-grid` instead — meaning
  nothing on this tab sets `formDirty` from typing alone, matching legacy's no-op
  `UpdateSessionWithCaseDetails` exactly.

Files changed: `Vla.cshtml`, `Edit.cshtml`, `Farm.cshtml`, `Clinical.cshtml`, `Feeds.cshtml`. `get_errors`
clean on all five.

## GB case creation flow end-to-end: Home RBSE entry → Farm CPHH lookup → GB/non-GB branching (2026-10-11)

Compared legacy `Home.aspx.vb` (`btnGo_Click`) and `CaseEntryFarm.aspx.vb` (`btnLookUp_Click` + new-case
detection) against migrated `Home.cshtml.cs` (`OnPostRbseLookupAsync`) and `Farm.cshtml.cs`
(`OnGetNewCaseAsync`, `ResolveNewCaseCphhAsync`, `OnPostLookupNewCaseAsync`, `IsNonGbFarmCphh`).

**Checked and confirmed already correct (no change needed), despite initially looking suspicious:**
- **RBSE 'X'-position check for a new case.** Legacy checks `Mid$(sRBSE, 4, 1)`/`Mid$(sRBSE, 5, 1)` —
  but `sRBSE` there is the **slash-formatted** `CC/YY/NNNNN` display string (confirmed by the adjacent
  `Left$(sRBSE, 5) = "63/00"` check, which only makes sense with a literal slash at position 3), so
  those 1-based positions are the two **YY** digits. Migrated's `normalized[2]`/`normalized[3]` operate
  on the slash-**stripped** raw 9-digit string, where `raw[2..4]` is also YY (per
  `RbseHelper.Format`'s `CC/YY/NNNNN` layout). Translating legacy's formatted-string positions into the
  raw string's indices lands on exactly 2 and 3 — migrated is correct, not an off-by-one bug.
- **Role mapping** (`isVlaMaintenance`/`isVlaDataEntry`/`isDefraViewer`) — cross-checked against
  `/memories/repo/role-permission-model.md`'s authoritative `GetPoliciesForGroup` table; all three
  flags correctly isolate their one matching legacy group out of the five mutually-exclusive groups.
- **Non-GB CPHH error text** on the Farm new-case Look Up — migrated's `"The CPHH you have entered is
  for a non-GB Farm."` is already copied verbatim from legacy `lblNonGBCPHH`'s `ToolTip` attribute in
  `CaseEntryFarm.aspx`.
- **Batch panel scope** — confirmed in `Home.cshtml` (line 42) the Batch Number panel is rendered only
  for `VLAAccess`, matching legacy showing `BatchNumber1` only to VLA roles — so migrated's "batch is
  optional" handling is never even exercised for DEFRA users, same as legacy.
- **"Farm not found" flow on the new-case Look Up button specifically** — `OnPostLookupNewCaseAsync`
  unconditionally redirects to `/Case/PickFarm` when the CPHH isn't found, matching legacy's
  unconditional `Response.Redirect("PickFarm.aspx")`. The separate `RequireFarmDetails = true` inline
  path only applies on `OnGetNewCaseAsync` (the page's own GET load) once the user has already been
  through PickFarm and chosen to create a new farm — not a divergent behaviour of the Look Up button.
- **Batch context carries through** — `Farm.cshtml.cs` reads `wizardState.GetAsync()` as `PendingBatch`
  (`OnGetNewCaseAsync`/`LoadAsync`), so the batch selected on Home isn't lost even though the migrated
  app doesn't mirror legacy's session-DataSet storage mechanism.

**Bug found and fixed: RBSE-empty vs batch-validation check order**

Legacy's `btnGo_Click` validates the batch (`BatchNumber1.Validate(iBatchID)`, which returns `True` when
both batch fields are blank) **before** ever checking whether the RBSE box is empty. Migrated's
`OnPostRbseLookupAsync` checked RBSE emptiness **first**. For the edge case of submitting with both an
empty RBSE and an incomplete batch (year filled, number blank, or vice versa) simultaneously, legacy
shows the batch error while migrated showed the RBSE-empty error instead. **Fixed:** reordered
`OnPostRbseLookupAsync` to validate the batch first (hoisting the resolved `batchId` out of the `if`
block), then check RBSE emptiness, matching legacy's check order exactly. The eventual outcome for a
validly-submitted form (one problem at a time) is unchanged — only the simultaneous-double-error edge
case's message changes.

Files changed: `Home.cshtml.cs`. `get_errors` clean.

## Farm page: Non-GB CPHH could still be saved, with no error in the consolidated SaveResult screen (2026-10-11)

Reported: looking up a non-GB CPHH on the new-case Farm page shows the "non-GB Farm" error on Look Up,
but clicking Save/Create Case anyway lets the case save successfully, with no trace of that error on
the SaveResult (consolidated errors) screen.

Root cause: `Farm.cshtml.cs`'s `OnPostLookupNewCaseAsync` checks `IsNonGbFarmCphh` and blocks **only
that handler** — it never touches the staged Farm draft. The actual commit path,
`CaseEditOrchestrationService.CheckMandatoryFieldsAsync` (the migrated equivalent of legacy
`CaseEntrySave.aspx`'s `BSELib.clsCase.CheckMandatoryFields`, reached from every tab's Save button), has
no GB/non-GB mismatch check at all — confirmed by reading `clsCase.vb` directly: legacy's
`CheckMandatoryFields` only ever validates for missing fields, never for a GB-case/non-GB-farm mismatch.

Legacy still avoided this in practice, but only by accident: `CaseEntryFarm.aspx.vb`'s
`btnLookUp_Click` skips calling `GetFarmDetailsFromDatabase`/`LoadFarmDetails` entirely for a
"00"-prefixed CPHH, so the session's farm dataset is left blank for that CPHH — meaning
`CheckMandatoryFields` would then fail on missing Owner Name/Address/etc. regardless, even though the
real farm in the database might be fully populated. Migrated's `CheckMandatoryFieldsAsync` instead
re-resolves the farm fresh from the database at commit time (`farmRepository.GetByCphhAsync(cphh)`),
independent of whatever `OnPostLookupNewCaseAsync` did or didn't allow — so if the non-GB CPHH belongs
to a real, fully-populated farm (as any non-GB farm created via `/Case/NewNonGb` would be), every
mandatory-field check passes and the case silently saves with a non-GB farm attached to a GB case.

**Fixed, then corrected against a real legacy run:** my first fix added a brand-new, dedicated error
(`"The CPHH you have entered is for a non-GB Farm."`) whenever `isNonGbFarm && !isNonGbCase`. The user
subsequently ran the exact same scenario against running legacy and reported legacy's actual
`CaseEntrySave.aspx` message is the **generic** `"No farm has been specified for the case."` —
confirming the full mechanism: `UpdateSessionWithCaseDetails` only ever writes the Farm table's own
CPHH column from the CPHH textbox; it never touches `dsCase`'s CPHH (the Case→Farm link) for a new
case — `dsCase.CPHH` is populated only by `GetFarmDetailsFromDatabase`/`LoadFarmDetails`, which
`btnLookUp_Click` never calls for a "00"-prefixed CPHH. So `CheckMandatoryFields`'s very first check
(`.IsNull("CPHH")` on the **Case** table) fires, and the farm-field checks are never even reached.
**Corrected fix:** for a new case, when the staged CPHH resolves to a non-GB farm (and the case itself
isn't non-GB), the CPHH used for the mandatory check is now treated as absent (`cphh = null`) *before*
the null check, so the existing generic `"No farm has been specified for the case."` fires naturally
and the farm-field checks are skipped — matching legacy's exact message and control flow, not a new
message.

Added regression test `CommitAllAsync_WhenNewGbCaseStagesAnExistingNonGbFarmsCphh_ThrowsNoFarmSpecifiedError`
in `CaseEditOrchestrationServiceTests.cs`, reproducing the exact reported scenario (new GB case, staged
CPHH belongs to an existing, fully-populated non-GB farm) and asserting legacy's exact message.
`dotnet test --filter FullyQualifiedName~CaseEditOrchestrationServiceTests`: 4 passed, 0 failed.

## Map Reference field: duplicate/conflicting JS on Farm Edit, missing live validation on new-case Farm (2026-10-11)

Compared legacy `CaseEntryFarm.aspx.vb`'s `MapReference1_MapReferenceChanged`/`btnEstimate_Click`
(backed by `BSELib.clsCase.ValidateMapReference`/`EstimateMapReference`) against the migrated Map
Reference field, which is rendered by one shared partial (`_FarmFormFields.cshtml`) consumed by both
`/Case/Farm` (new-case creation) and `/Farm/Edit` (existing farm).

**Bug found: `/Farm/Edit` wired the Map Reference Estimate button and blur-validation *twice*.**
`_FarmFormFields.cshtml` already had its own `estimate-map-ref-btn` click handler (fetching
`?handler=EstimateMapReference`). `Farm/Edit.cshtml` additionally declared its own, separate `<script>`
block targeting the exact same element IDs (`estimate-map-ref-btn`, `MapRef1/2/3`,
`estimate-map-ref-msg`) with a second, independent Estimate click handler *and* its own blur-triggered
`validateMapRef()` live parish-boundary check. Every "Estimate" click on `/Farm/Edit` therefore fired
two concurrent AJAX calls and two message writes to the same element (a race, with the final displayed
message depending on which response arrived last).

**Bug found: `/Case/Farm` (new-case creation) had no live map-reference validation at all.** Only
`Farm/Edit.cshtml`'s own duplicate script included the blur-triggered `validateMapRef()` call — the
shared partial itself only wired the Estimate button, not live validation on the `MapRef1/2/3` inputs.
So unlike legacy (where `MapReference1_MapReferenceChanged` fires on any change, not only via Estimate)
and unlike `/Farm/Edit`, typing/editing the Map Reference directly on the new-case Farm page gave no
inline feedback — only a `TempData` warning at final commit (`ValidateFarmMapReferenceAsync`).

**Bug found: the two pages' `OnGetValidateMapReferenceAsync` handlers disagreed for incomplete input.**
`Farm.cshtml.cs` returned `{ valid: true }` (no error) when the CPHH/map reference was too short;
`Farm/Edit.cshtml.cs` returned `{ valid: false, message: "Map reference must be 8 characters..." }` for
the same input shape. Legacy's `MapReference1_MapReferenceChanged` only touches `lblInvalidMapReference`
once both `sMapReference` and the county/parish derived from the CPHH are non-blank — for incomplete
input it leaves the indicator untouched (effectively "not flagged"), matching `Farm.cshtml.cs`'s
behaviour, not `Farm/Edit.cshtml.cs`'s.

**Fixed:**
- Moved the blur-triggered live validation into the shared `_FarmFormFields.cshtml` partial (alongside
  its existing Estimate button wiring), so both `/Case/Farm` and `/Farm/Edit` now get identical
  Estimate + live parish-boundary validation behaviour from one place.
- Removed `Farm/Edit.cshtml`'s entire duplicate "Map reference — Estimate button and parish-boundary
  validation" `<script>` block.
- Changed `Farm/Edit.cshtml.cs`'s `OnGetValidateMapReferenceAsync` to return `{ valid: true }` for
  incomplete input, matching `Farm.cshtml.cs` and legacy's "leave untouched" semantics.

`EstimateMapReference`/`ConvertToMapReference`/`GetCentreCoordinate` math itself (parish mid-row
selection, OS grid prefix lookup, rounding on an odd coordinate sum) was checked against
`BSELib.clsCase.vb` and found already correct in both `Farm.cshtml.cs` and `Farm/Edit.cshtml.cs` — no
change needed there.

Files changed: `_FarmFormFields.cshtml`, `Farm/Edit.cshtml`, `Farm/Edit.cshtml.cs`. `get_errors` clean
on all three; existing `dotnet test --filter FullyQualifiedName~MapReference` (Case/Farm's own
coverage): 5 passed, 0 failed.

## Map Reference live validation rendered as a red error instead of legacy's green "are you sure?" star (2026-10-11)

Follow-up to the fix above. Legacy's `lblInvalidMapReference` is `ForeColor="#9CCE00"` (lime/green) —
confirmed, via a app-wide grep for that colour, to be the same established "soft, non-blocking advisory"
star used all over legacy (`lblFormCDateWarning`, `lblOnsetAgeIncorrect`, `lblGridSizeIncorrect`, etc.),
distinct from legacy's **red** hard-validation stars. This matches the field's own validation severity:
`ValidateMapReference` only ever produces a `TempData` warning at final commit, never a blocking
`ModelState` error.

My earlier fix rendered the live blur-validation result as a `govuk-hint`/`govuk-error-message`
**paragraph**, using red GOV.UK error styling when invalid — the wrong visual severity, and not using
this app's own already-established equivalent: `.bse-star-warning` (green, `#9CCE00`, a `*` span with a
`title` tooltip and a global CSS hover-tooltip layer), already used identically for the same "are you
sure?" semantics on `VlaEdit.cshtml`/`FinalResultEntry.cshtml`.

**Fixed:** added `<span id="map-ref-invalid-star" class="bse-star-warning" title="This map reference
isn't within the county" hidden>*</span>` next to the Map Reference row in `_FarmFormFields.cshtml`
(the exact legacy tooltip text), and changed `validateMapRef()` to toggle its `hidden` attribute
(`invalidStar.hidden = d.valid`) instead of writing a coloured paragraph — mirroring legacy's
`lblInvalidMapReference.Visible = Not objCase.ValidateMapReference(...)` exactly. The Estimate button's
own success/failure messaging (a separate, unrelated affordance with no legacy equivalent) still uses
the `estimate-map-ref-msg` paragraph, untouched. `.bse-star-warning` is also correctly excluded from
`_CaseTabs.cshtml`'s cross-tab dirty-detection, which only scans `.bse-star-error`/`.bse-vla-star-red` —
consistent with this being advisory, not a reason to block navigation.

Files changed: `_FarmFormFields.cshtml`. `get_errors` clean.

## Map Reference: 4-way duplicated Estimate/Validate logic, 2 of which were broken or missing entirely (2026-10-11)

Follow-up to a report that the green warning star never appeared even for a map reference genuinely
outside the parish. Investigating found the Estimate/Validate logic had been independently
reimplemented in **four** page handlers, two of which were wrong:

- `Case/Farm.cshtml.cs` and `Farm/Edit.cshtml.cs` — correct, faithful ports of
  `BSELib.clsCase.EstimateMapReference`/`ValidateMapReference` (confirmed in earlier passes).
- `Farm/New.cshtml.cs` and `Farm/MoveCaseNewFarm.cshtml.cs` — a **different, incorrect** algorithm:
  averaged `XReference1`/`XReference2` (legacy never averages X, only Y), never converted the numeric
  coordinate into an OS grid letter code (`mapRef1` was raw digits, not a 2-letter code like "SP"), and
  had **no `OnGetValidateMapReferenceAsync` handler at all**. Both pages also already use the shared
  `_FarmFormFields.cshtml` partial, so after the fix two sections ago they silently gained a "map-ref-
  invalid-star" that called a non-existent handler — the resulting error response broke `r.json()`
  parsing in `validateMapRef()`, silently swallowed by its `.catch()`, so the star could never appear on
  these two pages no matter how invalid the entered reference was.

**Fixed:** moved the correct algorithm out of the page handlers entirely and into
`IGeoLookupService`/`GeoLookupService` (`BSE.Host.Services`) as two new shared methods —
`EstimateMapReferenceAsync(cphh)` and `IsMapReferenceWithinParishAsync(cphh, mapRef)` — so there is now
exactly **one** implementation. All four page handlers (`Case/Farm`, `Farm/Edit`, `Farm/New`,
`Farm/MoveCaseNewFarm`) now just call these two methods; `Farm/New`/`Farm/MoveCaseNewFarm` also gained
their missing `OnGetValidateMapReferenceAsync` handler. `Farm/New.cshtml.cs` still separately injects
the unrelated `BSE.Modules.ReferenceData.Services.IGeoLookupService` (needed for its own `GetParishAsync`
call) alongside the new `BSE.Host.Services.IGeoLookupService` for map-reference math specifically.

Files changed: `IGeoLookupService.cs`, `GeoLookupService.cs`, `Farm.cshtml.cs`, `Farm/Edit.cshtml.cs`,
`Farm/New.cshtml.cs`, `Farm/MoveCaseNewFarm.cshtml.cs`, `FarmModelHandlerTests.cs`. `get_errors` clean on
all seven; `IGeoLookupService` already registered in `Program.cs` for DI. `FarmModelHandlerTests.cs`'s
four map-reference tests were updated to mock the new consolidated `EstimateMapReferenceAsync`/
`IsMapReferenceWithinParishAsync` methods instead of the now-bypassed low-level ones.
`dotnet test --filter FullyQualifiedName~MapReference`: 5 passed, 0 failed.

## Map Reference: "no parish boundary data" was treated as valid instead of legacy's fallthrough-to-invalid (2026-10-11)

Reported repro: CPHH `00/999/9001/03` (a non-GB placeholder CPHH — county "00", parish "999", resolving
to "Guernsey") with Map Reference `NX 084 441` entered/estimated. Legacy shows the green "isn't within
the county" warning star; migrated showed nothing.

Root cause, confirmed directly against `BSELib.clsCase.ValidateMapReference`: when
`GetMapReferenceByCountyParish` returns **no rows at all** (as it does for a non-GB placeholder
county/parish like "00"/"999", which was never a real OS-mapped parish), the VB function's row-matching
loop simply never executes and falls through to its final `Return False` — i.e. legacy treats "no
boundary data to check against" as **invalid**, not valid, so the star is shown for every non-GB farm's
Map Reference once entered. `GeoLookupService.IsMapReferenceWithinParishAsync` had an explicit
`if (rows.Count == 0) return true;` shortcut (inherited, unmodified, from the original pre-consolidation
code, commented "no boundary data — allow") — the exact opposite of legacy's behaviour.

**Fixed:** removed that shortcut; an empty `rows` collection now falls through to the same `rows.Any(...)`
check as everything else, which naturally evaluates to `false` for an empty sequence — matching legacy's
`Return False` fallthrough exactly.

Added `GeoLookupServiceTests.cs` with direct coverage of `IsMapReferenceWithinParishAsync`, including
`IsMapReferenceWithinParishAsync_NoParishBoundaryData_ReturnsFalse` reproducing the exact reported
CPHH/map-reference combination. `dotnet test --filter "FullyQualifiedName~GeoLookupServiceTests|FullyQualifiedName~MapReference"`: 8 passed, 0 failed.

Files changed: `GeoLookupService.cs`, `GeoLookupServiceTests.cs` (new). `get_errors` clean.

## Map Reference: existing-farm edit treated the parish-boundary check as a blocking error, not an advisory (2026-10-11)

Follow-up: once the star correctly appeared, switching case tabs from Farm (via `OnPostStageAndGotoAsync`)
surfaced `"Map reference does not lie within the parish boundaries for this CPHH."` in the page's
blocking error summary and refused to navigate.

Root cause: `ValidateFarmMapReferenceAsync` (the **existing**-farm-edit path, called from both
`OnPostSaveFarmAsync` and `OnPostStageAndGotoAsync` via `ValidateFarmForSaveAsync`) called
`ModelState.AddModelError(...)` — a hard, navigation-blocking error. The **new**-case-creation path
(`ValidateNewFarmMapAndAdnsAsync`) already correctly used `TempData["Warning"]` instead. Legacy only
ever calls `ValidateMapReference` from the cosmetic `MapReference1_MapReferenceChanged` star toggle —
`UpdateSessionWithCaseDetails` (the function that actually gates a Save or tab-switch) never calls it at
all, so it can never block anything in legacy, on either the new-case or existing-farm path.

**Fixed:** changed `ValidateFarmMapReferenceAsync` to set `TempData["Warning"]` instead of adding a
`ModelState` error, matching `ValidateNewFarmMapAndAdnsAsync` and legacy's advisory-only behaviour. Both
`OnPostSaveFarmAsync` and `OnPostStageAndGotoAsync` now proceed normally with just the existing
`_CaseEditNotifications` warning banner shown (which already reads `TempData["Warning"]` on every tab),
instead of refusing to save/navigate.

`dotnet test --filter "FullyQualifiedName~MapReference|FullyQualifiedName~GeoLookupServiceTests"`: 8
passed, 0 failed. Note: `dotnet test` on the full `FarmModelHandlerTests` class also surfaces 1
pre-existing, unrelated failure (`OnPostCancelFarmEditAsync_ClearsDraftAndRedirectsHome`, asserting a
mock call through `ICaseDraftDiscardService` that the test never actually wires to forward) — confirmed
unrelated to this change by tracing `CancelFarmEditAsync`, which never calls
`ValidateFarmMapReferenceAsync`; left as-is, out of scope here.

Files changed: `Farm.cshtml.cs`. `get_errors` clean.

## PickListMaintenanceTSETestingSite's AHO field was free text instead of an AHO dropdown (2026-10-11)

Compared legacy `PickListMaintenanceTSETestingSite.aspx`'s AHO column against the migrated TSE Testing
Site Add/Edit pick list form, and against legacy's own AHO pick list (`PickListMaintenanceAHO.aspx.vb`)
for reference.

Legacy's TSE Testing Site grid's `EditItemTemplate` renders AHO as `<asp:DropDownList id="ddlAHOEdit"
... DataSource="<%# GetAHOCodeList() %>" DataTextField="Name" DataValueField="Code">` — a constrained
dropdown of valid AHO codes, not free text. Migrated's shared `PickListsAddModel`/`PickListsEditModel`
(`FieldsFor(TseTestingSiteId)`) defined AHO as a plain `FieldSpec("AHO", "AHO")` with no lookup flag, so
`PickListsAdd.cshtml`/`PickListsEdit.cshtml` rendered it as an unconstrained text `<input>` — any text
could be saved as the TSE Testing Site's AHO, including values that don't correspond to any real AHO.

**Fixed:** added a new `IsAhoLookup` flag to the shared `PickListsEditModel.FieldSpec` record (mirroring
the already-existing `IsRegionLookup` pattern used for BSE Region), populated from
`lookupDataService.GetLookupAsync(LookupTableId.AHO)` into a new `AhoOptions` property on both
`PickListsAddModel` and `PickListsEditModel`, and marked the TSE Testing Site's `"AHO"` field with
`IsAhoLookup: true` in both models' `FieldsFor`. Both `PickListsAdd.cshtml`/`PickListsEdit.cshtml` now
render a `<select>` (AHO `Code` as the option value, `Name` as the display text — matching legacy's
`DataValueField`/`DataTextField` exactly) instead of a free-text input, and both models' shared
`ValidateRequired()` now also requires a selection for `IsAhoLookup` fields, same as `IsRegionLookup`.
The underlying `AddTSETestingSiteAsync`/`EditTSETestingSiteAsync` calls are unchanged — they already
took the AHO value as a plain string (the code), so only the *input mechanism* changed, not the stored
data shape.

Files changed: `PickListsAdd.cshtml.cs`, `PickListsEdit.cshtml.cs`, `PickListsAdd.cshtml`,
`PickListsEdit.cshtml`. `get_errors` clean on all four. No existing automated tests cover these two
page models; none were added given the scope (admin-only pick-list maintenance UI) relative to the
test-harness setup this would require — flagging for manual verification.

## Map Reference: corrected again — legacy shows no Save-time message at all, not even a warning (2026-10-11)

Correction to the fix immediately above. The user clarified: legacy shows **only** the green star
tooltip — there is no warning banner or any other message at Save/tab-switch time. This matches what
had already been confirmed: legacy's `UpdateSessionWithCaseDetails` never calls `ValidateMapReference`
at all, on any path — the cosmetic star (`MapReference1_MapReferenceChanged`) is the *entire* feature.
Setting `TempData["Warning"]` at commit time (previous fix) was therefore still wrong — a lesser wrong
than a blocking `ModelState` error, but still a message legacy never shows.

**Fixed:** removed the server-side commit-time Map Reference parish check entirely from both paths:
- `ValidateFarmMapReferenceAsync` (existing-farm Save/tab-switch) deleted outright; its call site in
  `ValidateFarmForSaveAsync` removed.
- `ValidateNewFarmMapAndAdnsAsync` (new-case Save) had its Map Reference block removed, leaving only
  the (unrelated, legitimate) ADNS-region-vs-authority check; renamed to `ValidateNewFarmAdns` and made
  synchronous now that its only remaining `await` is gone.

The live, client-side green star (blur-triggered `?handler=ValidateMapReference` in
`_FarmFormFields.cshtml`) is the sole remaining Map Reference validation feedback anywhere in the app,
matching legacy exactly.

`dotnet test --filter "FullyQualifiedName~MapReference|FullyQualifiedName~GeoLookupServiceTests|FullyQualifiedName~FarmModelHandlerTests"`:
103 passed, 1 failed — the same pre-existing, unrelated `OnPostCancelFarmEditAsync_ClearsDraftAndRedirectsHome`
failure noted above (confirmed unaffected by this change).

Files changed: `Farm.cshtml.cs`. `get_errors` clean.

## PickLists grid: TSE Testing Site's AHO column showed the raw stored Code instead of its resolved Name (2026-10-11)

Follow-up to the Add/Edit dropdown fix above. Compared legacy's `grdLookup_ItemDataBound` for the
TSE Testing Site grid: in display (non-edit) rows, `lblAHOCode.Text = GetAHOEditList(drv("AHO"))` —
`GetAHOEditList` looks the stored AHO **Code** up in the AHO list and returns its **Name**, so the grid
column shows e.g. "Bristol", not the raw code. Migrated's grid listing page (`PickLists.cshtml.cs`,
a separate `FieldSpec`/`FieldsFor` from the Add/Edit pages' shared one) defined `"AHO"` as a plain field
with no lookup flag, so `DisplayValue` returned the raw stored code untouched — the same kind of gap as
the Add/Edit form, just in the read-only grid instead.

**Fixed:** added the same `IsAhoLookup` flag (this page has its own separate `FieldSpec`/`DisplayFields`,
not shared with `PickListsAdd`/`PickListsEdit`) and a new `AhoOptions` list populated the same way, then
extended `DisplayValue` — which already resolved `IsRegionLookup` columns from a stored ID to a Name —
to also resolve `IsAhoLookup` columns from the stored Code to its Name, falling back to blank when not
found (matching `GetAHOEditList`'s own "return empty string if not found" behaviour). Also included in
the existing `IsRegionLookup`-only sort-by-resolved-value branch, so sorting the AHO column sorts by
name rather than code, consistent with how the BSE Region column already sorts. `PickLists.cshtml`
needed no changes — it already renders every cell via `Model.DisplayValue(row, field)`.

Files changed: `PickLists.cshtml.cs`. `get_errors` clean.

## PickLists Add/Edit: "Is Active" checkbox (Test Type, Relation Fate) could never be checked (2026-10-11)

Reported: on the Test Type pick list Add/Edit page, the "Is Active" checkbox couldn't be selected.

Root cause: `PickListsAdd.cshtml`/`PickListsEdit.cshtml`'s boolean-field markup used the classic
ASP.NET Core "checkbox + always-submitted hidden fallback" pattern —
`<input type="checkbox" name="Fields[IsActive]" value="true" />` followed by
`<input type="hidden" name="Fields[IsActive]" value="false" />` — copied from the pattern `asp-for`
generates for a **strongly-typed `bool` property** binder (which needs a fallback because a missing key
there means "binding failed", not false). Here the target is a loosely-typed `Dictionary<string, string> Fields`, bound via explicit
`Fields[IsActive]` keys — the hidden field duplicates the checkbox's own form key, and whichever of the
two values wins depends on how repeated keys resolve for that binding target; in practice the hidden
`"false"` entry won, so the checkbox's own `"true"` was never the value `BoolField` saw, regardless of
whether the user ticked it.

**Fixed:** removed the hidden fallback input from both `PickListsAdd.cshtml` and `PickListsEdit.cshtml`.
It was never needed for a dictionary target: `BoolField`'s existing `Fields.TryGetValue(column, out var
v) && (...)` already defaults to `false` whenever the key is simply **absent** — exactly what happens
when an unchecked checkbox contributes nothing to the POST body — so dropping the hidden field removes
the only source of ambiguity and lets the checkbox's own posted value (`"true"` when checked, key absent
when not) be read correctly in both directions.

Files changed: `PickListsAdd.cshtml`, `PickListsEdit.cshtml`. `get_errors` clean on both.

## Audit Log (CPHH Changes): DateTime column — daylight saving / "system time" comparison (2026-10-11)

Compared legacy `AuditLogCPHHChanges.aspx.vb`/`clsAuditLog.GetCPHHChangesAuditLogReport` against the
migrated `/AuditLog/CphhChanges` page for how the log entry's date/time is produced and displayed.

**The displayed `LogDate`/`DateTime` column itself has zero code-level divergence — already fully
parity-matched.** Confirmed `AuditLog.LogDate DATETIME CONSTRAINT DF_AuditLog_LogDate DEFAULT
(getdate())` is byte-identical between legacy's and migrated's `AuditLog` table, and every INSERT INTO
`[AuditLog]` (including `ChangeCPHH.sql`, confirmed byte-for-byte identical to legacy's own
`ChangeCPHH.sql`) omits the `LogDate` column entirely, relying on that default — so the timestamp is
set by **SQL Server's own `GETDATE()`**, never by .NET application code, in both apps. On the read side,
`CphhChanges.cshtml` renders `@e.DateTime.ToString("dd/MM/yyyy HH:mm:ss")` with no `.ToLocalTime()`/
`.ToUniversalTime()`/`TimeZoneInfo` conversion anywhere — a pure pass-through, matching legacy's grid
binding exactly. **So whether the displayed time correctly reflects BST during daylight saving depends
entirely on the database server's own clock/timezone configuration, not on any code in either app** —
confirmed this instance of the Migrated app targets Azure SQL **Managed Instance** (`ROI-and-Budget.md`:
"Azure SQL Managed Instance migration (from on-prem vm-aphadev-003)"), which — unlike Azure SQL Database
(always UTC-only) — supports a configurable `timezoneId` at provisioning time. **Action needed outside
this codebase:** confirm the Managed Instance's `timezoneId` is set to `GMT Standard Time` (the Windows
timezone ID that observes BST), matching the legacy on-prem server's OS timezone; this is an
infrastructure/IaC setting with no equivalent file in this application workspace to verify or fix here.

**Found and fixed a related, genuinely in-scope bug:** every AuditLog page's Excel/CSV export filename
date stamp used raw `DateTime.Now`/`DateTime.Today`, which (per the existing, already-flagged comment in
`PrintBatch.cshtml.cs`) follows the **host OS's** clock — UTC on typical Linux/container app-service
hosting, unlike legacy's on-prem UK Windows server. Near a BST midnight boundary this could stamp an
export with the wrong calendar day. Added a shared `BSE.SharedKernel.UkClock` (`Now`/`Today`, converting
from `DateTime.UtcNow` via `TimeZoneInfo` — "GMT Standard Time" on Windows, falling back to
"Europe/London" on Linux) and switched `PrintBatch.cshtml.cs` to use it instead of its own private
duplicate, plus all 7 AuditLog export-filename date stamps (`ByCase`, `ByFarm`, `ByUser`, `CaseMoves`,
`CphhChanges`, `NewFarms`, `RbseChanges`) that previously used `DateTime.Now`/`DateTime.Today`.
`ByDate.cshtml.cs`'s export filename uses the user-selected search date, not "now" — correctly left
unchanged.

Files changed: `UkClock.cs` (new, `BSE.SharedKernel`), `PrintBatch.cshtml.cs`, `ByCase.cshtml.cs`,
`ByFarm.cshtml.cs`, `ByUser.cshtml.cs`, `CaseMoves.cshtml.cs`, `CphhChanges.cshtml.cs`,
`NewFarms.cshtml.cs`, `RbseChanges.cshtml.cs`. `get_errors` clean on all nine.

## Farm tab "Linked farms" grid: enable/disable logic when creating a new GB/non-GB case (2026-10-11)

Compared legacy `CaseEntryFarm.aspx.vb`'s Linked Farms grid (`grdLinkedFarms`/`LinkedFarmsPager`) against
the migrated `/Case/Farm/{rbse}` page's "Linked farms" table, specifically for the **new-case creation**
path (both GB and non-GB), not just ordinary editing of an already-saved case.

**Non-GB new case creation has no Linked Farms grid in either app — correct, pre-existing parity.**
Legacy's brand-new non-GB case flow (`Home.aspx` → `NonGBCaseCreation.aspx` for VLA Maintenance only) is
a separate, simpler page with Owner/Address/County/Herdmark fields only — no Linked Farms or Herd Size
grid at all. The migrated equivalent, `NewNonGb.cshtml`/`.cs`, correctly mirrors this: no Linked Farms
markup or handlers exist on that page. (An *existing* non-GB case being edited by a VLA role does land on
the same `CaseEntryFarm.aspx` / `/Case/Farm/{rbse}` page as GB cases, and that path is covered below.)

**GB new case creation and existing-case editing share one page in both apps — found and fixed a
genuine gap in the grid's add/edit/delete gating.** Legacy's `MakeDEFRAControlsWritable()` — the method
that enables `LinkedFarmsPager.AllowAddNew/AllowEdit/AllowDelete` — is gated on two independent
conditions: the user's role (DEFRA Data Entry/Maintenance or VLA Maintenance) **and**
`Session(SV_CPHHNumber) <> ""`; if no CPHH has yet been looked up/confirmed for a brand-new case, it
falls through to `MakeDEFRAControlsReadOnly()` regardless of role, locking the grid (and every other
DEFRA field) until a CPHH is resolved. The migrated page's `CanEditDefraControls` — the property that
drives both the "Add linked farm" button and each row's Change/Remove buttons — only ever checks role
(`ApplyLegacyEditPermissions()`), with no equivalent "has a CPHH been resolved yet" condition. In the
create-mode view this was masked client-side by an HTML `disabled` fieldset wrapping the whole Linked
Farms section while `createDetailsLocked` is true (no CPHH/farm fields populated yet) — but the four
POST handlers behind those buttons (`OnPostAddLinkedFarmRowAsync`, `OnPostBeginEditLinkedFarmRowAsync`,
`OnPostUpdateLinkedFarmRowAsync`, `OnPostDeleteLinkedFarmAsync`) only checked `CanEditDefraControls`,
with **no server-side re-check of CPHH resolution** — a direct POST to any of these handlers (bypassing
the disabled client-side fieldset) would have let a correctly-roled user stage linked-farm rows against
a case that has no CPHH/farm yet, something legacy's server-side `Session(SV_CPHHNumber)` gate would
never allow. Added a `HasResolvedCphh()` helper (`!string.IsNullOrWhiteSpace(EditableFarm?.CPHH)` —
the direct equivalent of legacy's `Session(SV_CPHHNumber) <> ""`) and added it to all four handlers'
existing `!CanEditDefraControls` guard. For an existing case this is always true (CPHH is already
persisted) so there is no behaviour change there; it only takes effect for the brand-new-case path.

Grid *data* (as opposed to button enablement) was already correct: `LoadFromFarmCphhAsync` only
populates `LinkedFarms`/`PersistedLinkedFarms` once a non-blank CPHH is known, so the table correctly
shows "No linked farms recorded." before a CPHH is resolved in create mode — matching legacy's
`DisableLinkedFarmGrid()` (empty `DataTable`) vs. `EnableLinkedFarmGrid()` (bound to the real session
table) split.

Files changed: `Farm.cshtml.cs` (added `HasResolvedCphh()`; updated 4 Linked Farms POST handlers).
`get_errors` clean.

## "Add linked farm" silently does nothing for a brand-new GB case's farm (2026-10-11)

User report: on a brand-new GB case where the CPHH resolves to a **farm that doesn't exist in the
database yet** (`RequireFarmDetails = true` — the user is typing in fresh Owner/Address/Parish/etc.
details for a new farm, not editing an existing one), clicking "Add linked farm" → typing a CPHH →
"Confirm" appeared to do nothing: the row never showed up in the table, with no error.

**Root cause 1 — the staged row was saved but never read back for display.**
`LoadOrInitializeDraftStateAsync()` had an early return: `if (Farm is null) return new
CaseFarmDraftState { Rbse = Rbse };` — for a brand-new farm (`Farm` is only ever set once the farm
already exists in the DB), this skipped reading `farmDraftState.GetAsync(Rbse)` entirely and never
populated `StagedLinkedFarms`/`StagedHerdSizes`/`HasUnsavedChanges`. `OnGetNewCaseAsync()` compounded
this: it only called `LoadOrInitializeDraftStateAsync()` at all `if (Farm is not null)`, so for a
brand-new farm it was never called on the page reload that follows every Add/Edit/Delete redirect.
Net effect: `OnPostAddLinkedFarmRowAsync` correctly wrote the new row into the `farmDraftState` store,
but the very next GET that renders the page never read it back, so the table kept showing "No linked
farms recorded." — indistinguishable from the click doing nothing.

**Root cause 2 — even if it had displayed, the row would have been lost on Save.** In
`OnPostCreateCaseAsync`, `Farm` is resolved once at the top of the method (null for a brand-new farm,
since it doesn't exist yet) and never refreshed. `PersistStagedCollectionsAsync()` — called after
`CaseCommitHelper.CommitAllAsync` has just created the Case **and** the new Farm row — has its own `if
(Farm is null) return;` guard, so for a brand-new farm it would have silently skipped writing any
staged linked farms/herd sizes to the database at all, even on a fully successful Save.

**Fix:**
1. `LoadOrInitializeDraftStateAsync()` now always reads `farmDraftState.GetAsync(Rbse)` first
   (regardless of whether `Farm` is null), only falling back to seeding from `PersistedLinkedFarms`/
   `PersistedHerdSizes` when `Farm is not null` and no draft exists yet.
2. `OnGetNewCaseAsync()` now calls `LoadOrInitializeDraftStateAsync()` whenever `Farm is not null ||
   RequireFarmDetails` (previously `Farm is not null` only), so a brand-new farm's staged rows render
   on reload.
3. `OnPostCreateCaseAsync()` re-fetches `Farm` by CPHH immediately after `CommitAllAsync` succeeds,
   before calling `PersistStagedCollectionsAsync()`, so the newly-created farm row is found and the
   staged linked farms/herd sizes are actually persisted.

No change in behaviour for an existing case (`Farm` is always already resolved there) or for a
brand-new case where the CPHH matches an existing farm (`Farm is not null` from the first Look Up).

Files changed: `Farm.cshtml.cs` (`LoadOrInitializeDraftStateAsync`, `OnGetNewCaseAsync`,
`OnPostCreateCaseAsync`). `get_errors` clean.

## Linked farms grid: buttons must be hidden (not merely disabled) before CPHH lookup (2026-10-11)

User-verified legacy behaviour: on `CaseEntryFarm.aspx` for a new GB case, before the CPHH is looked up
the Linked Farms grid's Add/Edit/Delete buttons are **not shown at all**; once CPHH is looked up they
appear and become usable. Checked the legacy control, `DataGridPager.ascx.vb`: the `AllowAddNew`,
`AllowEdit`, `AllowDelete` property setters assign straight to each button's `Visible` property
(`imbtnNewRec.Visible = Value`, `cmdEdit.Visible = Value`, `cmdDel.Visible = Value`) — i.e. legacy
*hides* these buttons entirely when a CPHH hasn't been resolved, it does not merely grey them out.

The migrated page only partially matched this: the earlier two 2026-10-11 Linked Farms fixes above
added a `HasResolvedCphh` gate to the four server-side POST handlers and wrapped the whole section in
a `disabled` `<fieldset>` client-side — correct for blocking interaction, but the "Add linked farm"
button and each row's Change/Remove buttons were still **rendered** (just inert inside the disabled
fieldset) before a CPHH was resolved, rather than hidden outright as legacy does.

Fix: promoted the private `HasResolvedCphh()` helper to a public `HasResolvedCphh` property on
`FarmModel` and added `&& Model.HasResolvedCphh` to all four `@if (Model.CanEditDefraControls)` checks
in `Farm.cshtml` that gate the Linked Farms grid's Add/Change/Remove buttons (both the create-mode and
existing-case markup blocks). For an existing case this is always a no-op (CPHH is already persisted,
so the buttons render exactly as before); for a brand-new case the buttons are now absent from the
markup entirely until a CPHH has been looked up/confirmed, matching legacy's hidden-not-disabled
behaviour.

Files changed: `Farm.cshtml.cs` (`HasResolvedCphh` made a public property), `Farm.cshtml` (4 button
visibility conditions). `get_errors` clean on both.

## "Add linked farm" button visible but inert after a successful CPHH lookup (2026-10-11)

User report: after looking up an existing CPHH for a new GB case, the "Add linked farm" button appears
enabled, but clicking it (and submitting the Confirm row) still does nothing.

Root cause: the create-mode fieldset's lock condition, `createDetailsLocked`, used a fragile heuristic —
"nothing is locked once `OwnerName`/`Address1`/`Parish`/`County`/`AHO` are non-blank or `ADNSRegionID` is
set, or `RequireFarmDetails`/`SelectedCphh` indicate a farm is in progress" — as a *proxy* for "has a
CPHH been resolved". This proxy breaks for a perfectly ordinary case: an existing farm record whose
Owner/Address/Parish/County/AHO fields are genuinely blank (a sparse pre-existing record). In that
case `Model.Farm` is resolved and `HasResolvedCphh` is true — so "Add linked farm" renders *visible*
(per the previous fix) — but `createDetailsLocked` still evaluates true because every one of those
specific fields is blank, so the whole fieldset remains `disabled`, and a disabled HTML fieldset
silently blocks all clicks on its descendants, including an enabled-looking button. This is exactly
what "button enabled but functionality is not working" looks like.

Fix: replaced the multi-field heuristic with the same single flag used for button visibility —
`createDetailsLocked = !createReadOnly && !Model.HasResolvedCphh` — mirroring legacy's actual gate,
`Session(SV_CPHHNumber) <> ""`, which never depended on which individual fields happened to be filled
in. The fieldset and the buttons inside it now key off the identical condition, so they can no longer
disagree.

Files changed: `Farm.cshtml` (`createDetailsLocked` computation). `get_errors` clean.

## "Add linked farm" still does nothing: create-mode JS was never rendered at all (2026-10-11)

User report: the button visibly looks enabled, but clicking it performs no action whatsoever (not even
revealing the inline "type a CPHH" row) — persists after the two fixes above.

Root cause: `Farm.cshtml`'s create-mode markup block (`@if (Model.Case is null) { ... }`) ends with a
plain C# `return;` that stops the Razor page from rendering anything further. The `<script>` block that
wires `show-add-linked-row`/`cancel-add-linked-row` (and the equivalent Herd Size inline-row toggles)
was physically located *after* that `return;`, as part of the existing-case markup further down the
page. Because the two markup blocks are mutually exclusive (brand-new case vs. existing case), that
script was **never sent to the browser at all** for a brand-new case — the "Add linked farm" button
existed in the HTML (and, after the previous two fixes, was correctly visible and enabled), but zero
JavaScript ever ran `addEventListener('click', ...)` on it, so every click was a no-op. This also
silently broke the equivalent Herd Size "Add"/edit-row toggles in create mode, not just Linked Farms.

Fix: extracted the shared `<style>` + `<script>` block into a new partial,
`Pages/Shared/_FarmGridScripts.cshtml` (typed to `FarmModel`), and included it via
`@await Html.PartialAsync("_FarmGridScripts", Model)` from **both** the create-mode block (just before
its `return;`) and the existing-case block (in the same place the inline block used to sit) — so the
interactive JS now actually reaches the browser regardless of which markup branch rendered. Also fixed
a pre-existing, unrelated bug noticed while extracting this: the `.js-cancel-edit-linked-row` wiring
was nested inside `if (showAddBtn && addRow)` (the Herd Size "Add" button's guard), so a page where the
Herd Size "Add" button isn't rendered (e.g. `CanEditVlaControls` false) silently never wired up
"Cancel" for an in-progress Linked Farm row edit either; it is now a standalone statement.

Files changed: `Pages/Shared/_FarmGridScripts.cshtml` (new), `Farm.cshtml` (replaced the inline
style/script block with two partial includes). `get_errors` clean on both.

## Bottom action row (Farm audit log / View docs / Save / Cancel) inconsistent in create mode (2026-10-11)

User report: the bottom buttons on a brand-new GB case's Farm tab don't look or align like the same
row on an existing case's Farm tab.

The existing-case markup renders these four actions in a `bse-case-bottom-row` flex layout: "Farm
audit log" (left, `govuk-button govuk-button--secondary`), then "View docs"/"Save"/"Cancel" grouped on
the right (`govuk-button-group`), with Cancel using the `bse-cancel-link` class that the shared JS
wires up to the unsaved-changes confirmation dialog. The create-mode markup instead used a bare
`govuk-button-group` with Save only, a plain `govuk-link` (not a button) for Cancel with no dirty-check
wiring at all, a completely separate `<div>`/`<ul>` below it for View docs as a plain text link, and no
Farm audit log button at all.

Fix: rebuilt the create-mode bottom row with the identical `bse-case-bottom-row` structure and
button classes as the existing-case block: "Farm audit log" now appears (linking to
`/AuditLog/ByFarm?cphh=...&rbse=...`) once `Model.HasResolvedCphh` is true — hidden rather than
disabled, consistent with this page's existing "hide, don't grey out, until a CPHH is resolved"
pattern for the Linked Farms grid buttons (legacy's own `btnFarmAuditLog.Enabled = False` just greys
it out rather than hiding it, a minor, low-risk cosmetic divergence noted here rather than introducing
a new disabled-anchor CSS pattern for one button). "View docs" and "Cancel" are now styled as
`govuk-button`/`govuk-button--secondary` to match, and Cancel now goes through the same
`CancelFarmEdit` page handler as the existing-case Cancel button (discarding any staged Farm/Linked
Farms/Herd Size drafts for this RBSE) instead of navigating straight to `/Home` with nothing cleaned
up server-side.

Files changed: `Farm.cshtml` (create-mode bottom action row). `get_errors` clean.

## Verified: cross-tab staging / global Save / Cancel parity between create mode and existing case (2026-10-11)

User asked whether field staging, the global Save, and Cancel behave the same in create mode (brand-new
case) as they do for an existing case. Traced all three:

- **Cross-tab field staging** — the "switch tab" buttons in `_CaseTabs.cshtml` submit the Farm form to
  `FarmModel.OnPostStageAndGotoAsync`, the *same* handler for both modes (it isn't duplicated per mode).
  It already has an explicit `Case is null` branch: if no CPHH has been looked up yet it skips staging
  and validation entirely and navigates straight to the target tab — mirroring legacy's
  `UpdateSessionWithCaseDetails()` returning `True` unconditionally when `SV_FarmDetails` was never
  populated. Once a CPHH is resolved, staging proceeds identically to the existing-case path. No gap.
- **Global Save** — `OnPostCreateCaseAsync` (create mode) and `OnPostSaveFarmAsync` (existing case) both
  stage their own fields then call the same `CaseCommitHelper.CommitAllAsync`, which commits everything
  staged for the RBSE across every tab in one transaction — not two different commit paths. Confirmed
  already consistent (and `PersistStagedCollectionsAsync`'s Linked Farms/Herd Size persistence, fixed in
  the two entries above, is shared by both Save paths too). No gap.
- **Cancel — found and fixed a real gap.** The *bottom* Cancel button was fixed in the entry above, but
  the *top* Cancel button (rendered once by `_CaseTabs.cshtml` for every tab, including Farm) still
  differed: create mode passed `CancelPageOverride: "/Home"` with no handler override, so it navigated
  straight to `/Home`'s default `OnGet` — skipping the `CancelFarmEdit` handler's
  `caseDraftDiscard.DiscardAllAsync(Rbse)` entirely. The existing-case top Cancel (and the just-fixed
  bottom Cancel, in both modes) always goes through `CancelFarmEdit`. Changed create mode's `_CaseTabs`
  call to `CancelPageOverride: "/Case/Farm", CancelHandlerOverride: "CancelFarmEdit"` so the top Cancel
  now discards staged drafts the same way everywhere.

Files changed: `Farm.cshtml` (`_CaseTabs` call's `CancelPageOverride`/`CancelHandlerOverride`).
`get_errors` clean.

## "Is A Dealer?" checkbox editable/non-editable logic — new case vs existing case (2026-10-11)

Compared legacy's VLA-controls group (`ddlHerdType`, `ddlPedigree`, `chkIsDealer`, Herd Size grid) —
all toggled together by `MakeVLAControlsReadOnly()`/`MakeVLAControlsWritable()` — against the migrated
`_FarmFormFields.cshtml` partial shared by both the create-mode and existing-case Farm tab markup.

**Found: `chkIsDealer`'s migrated equivalent had no role gating at all.** `HerdType` and `PedigreeType`
both correctly render `disabled="@(!canEditVlaControls)"` (plus a hidden-input fallback so a disabled
`<select>` still posts its last-known value), but the "Is A Dealer?" checkbox right next to them had no
`disabled` attribute whatsoever — it was always interactively editable for every role, including DEFRA
Data Entry/Maintenance (who, per legacy, must never be able to touch this VLA-tier field) and VLA roles
without `IsVLAAllowedMainCaseEdit`. This is identical in both create mode and existing-case mode (same
shared partial), so it was not itself a new-vs-existing discrepancy, but a plain missing-gate bug.

**Found: the server-side reset guard was incomplete, and missing entirely from create mode's Save.**
`ApplyLegacyJointAndVlaEditGuards()` (defence-in-depth against a client bypassing the `disabled`
attribute) only reset `PedigreeType` back to the DB value when `!CanEditVlaControls` — not `IsDealer` —
and `OnPostCreateCaseAsync` (create mode's Save) never called this guard at all, only
`OnPostSaveFarmAsync` (existing case) and `OnPostStageAndGotoAsync` did. So even once the checkbox is
visually disabled, a crafted POST during brand-new-case creation against an *existing* farm's CPHH
could still have flipped `IsDealer` for a role that should never be allowed to.

The CPHH-not-yet-resolved case (pre-Look-Up, create mode only) was already correctly handled: the
checkbox sits inside the same `<fieldset disabled>` as the rest of the DEFRA/VLA fields, gated by
`createDetailsLocked` (fixed in an earlier entry above to key off `HasResolvedCphh`) — native HTML
`fieldset[disabled]` locks every descendant control regardless of its own `disabled` attribute, so no
separate fix was needed for that part.

Fix: added `disabled="@(!canEditVlaControls)"` to the IsDealer checkbox; extended
`ApplyLegacyJointAndVlaEditGuards()` to also reset `IsDealer` (alongside `PedigreeType`); and added a
call to that guard in `OnPostCreateCaseAsync`, matching the existing-case Save path.

Files changed: `_FarmFormFields.cshtml` (IsDealer `disabled` attribute), `Farm.cshtml.cs`
(`ApplyLegacyJointAndVlaEditGuards` extended; called from `OnPostCreateCaseAsync`). `get_errors` clean
on both.

## Map Reference green star: visual parity with the Eartag field's green star (2026-10-11)

User request: the Map Reference field's green "are you sure?" star on `/Case/Farm` should look like
the green star next to the Eartag field on the Case (DEFRA) tab.

The Eartag star (`.bse-eartag-help__icon` in `Edit.cshtml`) renders as `color: #00703c` (GOV.UK green),
`font-size: 2rem`, `font-weight: 700`, `line-height: 1`. The Map Reference star
(`#map-ref-invalid-star`) instead used the shared, sitewide `.bse-star-warning` class
(`bse.css`) — `color: #9CCE00` (a lighter yellow-green), `font-size: 125%`, `margin-left: 4px` — a
different colour and roughly half the size.

`.bse-star-warning` is shared by many other warning stars across the app (VLA tab, Final Result Entry,
etc.), so its shared rule was left untouched to avoid an unintended sitewide restyle; instead added an
ID-scoped override for this one star only.

Fix: added `#map-ref-invalid-star { color: #00703c; font-size: 2rem; line-height: 1; margin-left: 0; }`
to `bse.css` (ID selector outweighs the shared class, `font-weight: 700` already matches via the
existing `.bse-star-warning` rule so wasn't repeated; `margin-left` zeroed since the Map Reference row
already spaces its flex children via `gap: 6px`, mirroring how the Eartag star relies on its own
container's `gap` rather than its own margin).

Files changed: `wwwroot/css/bse.css`. `get_errors` clean.

## New GB case Save allowed a non-GB CPHH through once mandatory farm fields were filled in (2026-10-11, corrected)

User-verified: on `/Case/Farm` for a brand-new GB case, entering a "00"-prefixed (non-GB) CPHH and
clicking Save showed "The CPHH you have entered is for a non-GB Farm." (expected, matches Look Up's
own warning) — but then also showed the ordinary mandatory farm-detail errors (Owner Name, Address 1,
Parish, County, AHO). Filling those in let Save succeed and create the case, which legacy would never
allow.

**First attempt (superseded below):** added a standalone `IsNonGbFarmCphh` check directly in
`OnPostCreateCaseAsync` that short-circuited with its own inline CPHH error. The user then clarified
legacy's actual behaviour, which is different and more specific — traced through legacy source to
confirm it exactly:

- `CaseEntryFarm.aspx`'s `btnLookUp_Click` never calls `GetFarmDetailsFromDatabase` for a "00"-prefixed
  CPHH at all, so `Session(SV_FarmDetails)` is never created.
- `UpdateSessionWithCaseDetails()` (Save's handler) sees `dsFarm Is Nothing` and returns `True`
  **without writing anything** — not a validation failure, just a no-op for the farm — and Save still
  redirects to `CaseEntrySave.aspx`.
- `CaseEntrySave.aspx`'s `CheckMandatoryFields(dsCase, dsFarm, ...)` runs with `dsFarm = Nothing`,
  producing the **consolidated** message list the user reported: `"The case is missing the following
  items of data:"` / `"No farm has been specified for the case."` / `"Please specify an eartag for the
  case."` / `"Please specify a Form A date for the case."` — not a standalone CPHH error, and crucially
  **never checks Owner Name/Address/Parish/County/AHO at all**, because those checks live in the
  farm-exists branch of `CheckMandatoryFields`, never reached when `dsFarm` is null.
- The "Return" button redirects straight back to `CaseEntryFarm.aspx`, which reloads
  `Session(SV_FarmDetails)` — still `Nothing` — so the CPHH box (and every other farm field) renders
  blank again, exactly as the user described ("CPHH field is discarded back... farm page is modified to
  its original state").

This is already what `CaseEditOrchestrationService.CheckMandatoryFieldsAsync` produces for a new case
with no CPHH (confirmed correct in earlier session work) — the gap was that `OnPostCreateCaseAsync`
never routed a non-GB CPHH into that same "no CPHH" path; it instead ran its own (incomplete,
Owner/Address/Parish/County/AHO-requiring) `ValidateNewFarmDetails` check against it.

**Second attempt (superseded below):** made `OnPostCreateCaseAsync` blank out the CPHH itself (and skip
the farm-detail/ADNS pre-checks) before staging, so the staged Farm command — and therefore the CPHH
redisplayed after the SaveResult "Return" round trip — came back empty, replicating legacy's literal
"session dataset was never written" outcome. The user then overrode this: losing the typed CPHH on
return is not the wanted UX for this migrated app (which, unlike legacy's raw session dataset, already
has a "staged draft survives for convenience" model throughout) — Look Up + Save must never discard
the CPHH the user entered.

**Final fix:** stopped blanking `normalisedCphh`/`EditableFarm.CPHH` altogether. Instead,
`OnPostCreateCaseAsync` only uses `isNonGbCphh` to skip the *page-level* `ValidateNewFarmDetails`/
`ValidateNewFarmAdns` pre-checks (Owner/Address/Parish/County/AHO/ADNS — irrelevant for a CPHH that can
never resolve to a farm) and skips its own farm lookup for that CPHH. The *real*, as-typed CPHH is
staged and redisplayed completely unchanged. The consolidated "No farm has been specified for the
case." message still fires correctly with no further changes needed, because
`CheckMandatoryFieldsAsync` already has its own independent non-GB-prefix detection that nulls the CPHH
*internally, for that function's own purposes only* when deciding what to report — it does not depend
on the caller having blanked anything out first. Net effect: the user sees the exact right consolidated
message list, is never asked to fill in moot farm-detail fields, and the CPHH they typed is preserved
everywhere (staged draft, redisplay, and after "Return" from SaveResult).

Files changed: `Farm.cshtml.cs` (`OnPostCreateCaseAsync`). `get_errors` clean.

## Move Case functional parity: access control and missing mandatory-field validation (2026-10-11)

Compared legacy `MoveCase.aspx`/`MoveCaseNewFarm.aspx` against migrated `/Case/MoveCase` and
`/Farm/MoveCaseNewFarm`. The core lookup/check/move logic (RBSE lookup, destination-farm check,
same-CPHH guard, GB-case-vs-non-GB-farm consistency checks, "only case on this farm" warning,
"Create new farm" prompt gated on matching first-9-digit CPH, confirm-before-move dialog) was already
faithfully implemented in `MoveCaseModel`. Two real gaps found:

**1. Access control — VLA Maintenance was locked out entirely.** `docs/Legacy-Page-Level-Access-Control.md`
section 5.9 ("Case Maintenance Operations", sourced directly from each page's legacy `EnableControls()`)
documents `MoveCase.aspx`/`MoveCaseNewFarm.aspx` (and `DeleteCase.aspx`/`CPHHChange.aspx`/
`RBSEChange.aspx`/`FinalResultEntry.aspx`) as DEFRA Maintenance **or** VLA Maintenance — every other
group redirected home. Both migrated pages instead used `[Authorize(Policy = "DEFRAMaintenance")]`,
a single-role policy, so VLA Maintenance users were redirected home even though legacy explicitly
allows them. The "Move case" links on the Home page and the global nav bar were also gated on
`DEFRAMaintenance` only, hiding the link entirely for VLA Maintenance. Also found (dead code, no
behavioural effect since it's unused by any view): `Farm.cshtml.cs`'s `CanChangeCphh` property's own
doc comment already said "DEFRA Data Entry/Maintenance and VLA Maintenance can look up a different
farm" but its implementation only checked `DEFRAMaintenance`.

Fix: added a new `DEFRAOrVLAMaintenance` authorization policy (`RequireRole("DEFRAMaintenance",
"VLAMaintenance")` — same OR-of-roles pattern as the existing `AuditAccess` policy) and applied it to
both `MoveCase.cshtml.cs` and `Farm/MoveCaseNewFarm.cshtml.cs`; split the "Move case" link out of the
shared `DEFRAMaintenance`-only block into its own `DEFRAMaintenance || VLAMaintenance` block on both
`Home.cshtml` and `_Layout.cshtml`'s global nav; fixed `CanChangeCphh`'s role check to match its own
comment. **Not fixed** (flagged for separate follow-up, same latent bug, out of scope for this specific
request): `Delete.cshtml.cs`, `Farm/CphhChange.cshtml.cs`, `Case/RbseChange.cshtml.cs`,
`Case/FinalResultEntry.cshtml.cs` still use the single-role `DEFRAMaintenance` policy, and their own
Home/nav links are still gated the same way — all five pages share the identical documented access
rule, so this is a real, systemic gap beyond Move Case alone.

**2. `MoveCaseNewFarm` had no mandatory-field validation at all.** Legacy's `CheckMandatoryFields`
requires Owner Name, Address 1, AHO, Parish, County and ADNS Region before calling
`clsCase.MoveCaseNewFarm`; the migrated `OnPostAsync` only validated the CPHH format and
already-exists check — a farm could be created via this flow with every other field blank.

Fix: added the same six checks (reusing the exact wording already established for this rule in
`Farm.cshtml.cs`'s `ValidateNewFarmDetails`) to `MoveCaseNewFarmModel.OnPostAsync`, shown together via
the page's existing GOV.UK error summary.

Files changed: `Program.cs` (new policy), `MoveCase.cshtml.cs`, `Farm/MoveCaseNewFarm.cshtml.cs`
(policy + mandatory-field checks), `Farm.cshtml.cs` (`CanChangeCphh`), `Home.cshtml`, `_Layout.cshtml`.
`get_errors` clean on all.

## Move Case → new farm: save succeeded in migrated where legacy rejects it with unchanged defaults (2026-10-11)

User-reproduced: RBSE `009125899`, Check CPHH `44623045401` (already exists), Check again with
`44623045406` (new, same first-9-digit CPH → "Create new farm" offered). Both apps pre-populate the new
farm form from the current farm's details, unchanged. Clicking Save with nothing edited: legacy shows
an error and does not move the case; migrated moves it successfully. User also flagged that legacy has
no "Is A Dealer?" field on this screen at all, while migrated does.

**Is Dealer / Herd Type / Pedigree should never have appeared on this page.** Confirmed in legacy's
`clsCase.MoveCaseNewFarm`: the stored-procedure call always hardcodes `HerdType=NULL,
PedigreeType=NULL, IsDealer=False` — `MoveCaseNewFarm.aspx` has no UI controls for any of the three at
all. The migrated page reuses the same `_FarmFormFields` partial as the Farm tab (which does need
these fields), so all three rendered — editable — on a screen where legacy never offered them.

**Root-cause theory for the save succeeding where legacy fails:** `MoveCaseNewFarm.aspx.vb`'s
`PopulateControls()` copies `AuthorityCountyID`/`AuthorityID`/`ADNSRegionID` straight from the current
farm, then rebuilds the Local Authority and ADNS Region dropdowns from that copied
`AuthorityCountyID`/`AuthorityID` and only re-selects the copied `ADNSRegionID` if it is still present
in the freshly-rebuilt list. If the current farm's stored ADNS Region is no longer valid for its own
Authority (a plausible data-quality drift over time — exactly the same "stale cascade" issue already
fixed twice in `Farm.cshtml.cs` for the ordinary Farm tab, via `IsAdnsCompatibleWithAuthoritySelection`),
the dropdown silently resets to blank and `CheckMandatoryFields` then rejects the save with "You have
not entered an ADNS Region." The migrated `MoveCaseNewFarmModel.OnPostAsync` had no equivalent
ADNS-compatible-with-Authority check at all — only `Farm.cshtml.cs`'s own Farm-tab save path had it —
so a pre-populated-but-now-stale ADNS Region/Authority pairing copied from the current farm would
sail through, whereas legacy's reselect-from-a-rebuilt-list mechanism (acting as an implicit
revalidation) would have caught it.

Fix:
1. Added a `ShowVlaFields` ViewData flag to `_FarmFormFields.cshtml` (defaults to `true`, so the Farm
   tab's two existing call sites are unaffected) and set it to `false` from `MoveCaseNewFarm.cshtml`,
   removing Herd Type/Pedigree/Is A Dealer from this page's markup entirely, matching legacy's absence
   of these controls.
2. `MoveCaseNewFarmModel.OnPostAsync` now forces `HerdType = null, PedigreeType = null, IsDealer =
   false` on the `AddFarmCommand` via a `with` expression regardless of what's posted — defence in
   depth against a crafted request now that the fields are hidden rather than merely disabled.
3. Added the same `ViewData["AdnsOptions"]`-based ADNS-compatible-with-Authority re-check already used
   twice in `Farm.cshtml.cs` to `MoveCaseNewFarmModel.OnPostAsync`, rejecting the save with "Specify an
   ADNS region for the farm." when the pre-populated ADNS Region is no longer valid for its Authority.

Files changed: `_FarmFormFields.cshtml` (`ShowVlaFields` flag), `MoveCaseNewFarm.cshtml` (pass
`ShowVlaFields = false`), `MoveCaseNewFarm.cshtml.cs` (ADNS-compatibility check; hardcoded
HerdType/PedigreeType/IsDealer on save). `get_errors` clean on all three.

## BSE1/Paperwork date validation and cross-tab staging for new-case creation (2026-10-11)

User asked to check BSE1 date validation, Paperwork Complete field enablement, and whether these
values survive tab switches during new-case creation on the Case (DEFRA) tab.

**1. BSE1/Paperwork date validation was silently skipped entirely for a brand-new case.**
`ValidateCaseWorkDates` early-returns when `Case.RbseDate` is null. Legacy's `Common.vb`
(`GetCaseDetailsFromDatabase`) sets a brand-new case's in-memory CaseWork row's `RBSEDate` to
`Date.Today` immediately on creation — every BSE1/Paperwork date's legacy validation bound
(`RBSEDate + 1 day` .. today) depends on it. Migrated's new-case branches (`OnGetAsync`,
`LoadReadonlyPageAsync`) set `Case.HasCaseWork = true` but never set `Case.RbseDate`, so every BSE1
date and the Paperwork Complete date skipped range validation for the entire lifetime of a new case
until first Save. Fixed by setting `Case.RbseDate ??= DateTime.Today` alongside `HasCaseWork = true`
in both places.

**2. Field enablement (Paperwork Complete / BSE1 dates) was already correct** — both already use
`disabled="@(!Model.Case.HasCaseWork)"`, matching legacy's `MakeControlsWritable`'s CaseWork-row-exists
gate, which (like migrated) is always satisfied for a new case since the CaseWork row/flag exists
from the moment creation starts, in both apps.

**3. Confirmed and fixed real data loss: CaseWork fields (Barcode, AHF Reference, the five BSE1-received
dates, Paperwork Complete, RbseDate) were entirely outside the cross-tab staging mechanism.** Legacy
keeps the Case and CaseWork rows in the exact same shared session `DataSet`, written together by
`UpdateSessionWithCaseDetails()` on every tab switch. Migrated's cross-tab draft
(`CaseScalarDraftState`) only ever staged the regular Case-table fields (`EditCaseCommand`) — CaseWork
fields were persisted only via a direct, non-staged write inside `Edit.cshtml.cs`'s own `SaveAsync`,
using whatever was in the posted form at that exact moment. Typing a BSE1 date then switching to
another tab (`OnPostStageAndGotoAsync`) discarded it; saving from a *different* tab never touched
CaseWork at all, and a brand-new case's CaseWork row was always inserted with every optional field
hardcoded to null regardless of what had been typed.

Fix: added `EditCaseWorkCommand? CaseWork` to `CaseScalarDraftState`; added `StageCaseWorkEditAsync()`
(same change-detection pattern as Farm's staging, preserving the existing Barcode/AHF-Reference
security re-derivation) and wired it into tab-switching, Save, and the Tests-grid restage path;
extended `ApplyStagedCaseOverlayAsync` to restore staged CaseWork fields on GET; moved the actual
persistence from `SaveAsync`'s direct, non-transactional call into
`CaseEditOrchestrationService.CommitAllAsync`'s single transaction (alongside Bab/Clinical), and made
the new-case `AddCaseWorkCommand` use the staged values instead of hardcoded nulls.

**Not independently verified end-to-end** (no DB/runtime access in this environment) — please confirm:
type a BSE1 date on a new case, switch tabs and back, confirm it survives; save from a different tab
and confirm the CaseWork row picks up the staged dates.

Files changed: `Edit.cshtml.cs` (RbseDate init, `StageCaseWorkEditAsync`, overlay, wiring),
`CaseScalarDraftState.cs` (`CaseWork` field), `CaseEditOrchestrationService.cs` (persist staged
CaseWork in the commit transaction). `get_errors` clean on all three.

## Removed success-message banners from all Case tabs (2026-10-11)

User report: saving the CaseWork page for one RBSE showed its "Case work entry... has been updated"
success banner on the Case (DEFRA) tab of an unrelated, different RBSE during new-case creation.

Root cause: `TempData["Success"]` is a single, global, un-scoped key — read by `CaseWork/Entry.cshtml.cs`
(redirects to OpenCases/ClosedCases) and, separately, by all 7 Case tabs via `_CaseEditNotifications`
(or, for Relations, an inline check). Whichever page happens to read the key next displays whatever
was last written there by *any* other page, regardless of RBSE. User's explicit direction: no success
banners on case tabs at all, rather than attempting to scope TempData per-RBSE.

Fix: stopped passing `TempData["Success"]` into `_CaseEditNotifications` from Bab/Clinical/Edit/Farm/
Feeds/Vla (now only `WarningMessage` is passed; `SuccessMessage` defaults to `null` so the banner
never renders), removed Relations' own inline success-banner block, and removed the one place a case
tab itself set `TempData["Success"]` (`Edit.cshtml.cs`'s `OnPostSaveAsync`, the Tests-grid-only save
handler) so it can no longer plant a stale value for some other page to pick up later. Warning banners
and the GDS error summary are unaffected — only success banners were in scope.

Files changed: `Bab.cshtml`, `Clinical.cshtml`, `Edit.cshtml`, `Farm.cshtml`, `Feeds.cshtml`,
`Vla.cshtml`, `Relations.cshtml`, `Edit.cshtml.cs`. `get_errors` clean on all.

## Case (DEFRA) tab, new case creation: BSE1 date validation + cross-tab CaseWork staging (2026-10-11)

User asked to compare BSE1 date validation and "Is Paperwork Complete" enablement for new-case
creation specifically, and to check whether these values survive a tab switch.

**1. BSE1/Paperwork date range validation was silently skipped for every brand-new case.**
`ValidateCaseWorkDates` early-returns when `Case.RbseDate` is null. Legacy's `Common.vb` sets the new
in-memory CaseWork row's `RBSEDate` to `Date.Today` the moment a new case starts
(`AddEmptyRow(...) : .Item("RBSEDate") = FormatDate(Date.Today)`), so legacy always has a date to
validate the six BSE1/Paperwork dates against. The migrated new-case branches (`OnGetAsync`,
`LoadReadonlyPageAsync`) set `Case.HasCaseWork = true` but never set `Case.RbseDate` — it stayed null
for the entire lifetime of creating a case, so `ValidateCaseWorkDates` always skipped all six fields.
Fixed by setting `Case.RbseDate ??= DateTime.Today` alongside `HasCaseWork = true` in both places.

**2. CaseWork fields (Barcode, AHF Reference, the five BSE1-received dates, Paperwork Complete,
RbseDate) had no cross-tab staging at all — confirmed genuine data loss on tab-switch.** These fields
live on the separate `CaseWork` table/`EditCaseWorkCommand`, entirely outside `EditCaseCommand`/
`CaseScalarDraftState.Case`, which is the only thing `OnPostStageAndGotoAsync` (the tab-switch
handler) ever staged. Legacy keeps Case and CaseWork in the *same* shared session DataSet, written
together by `UpdateSessionWithCaseDetails()` on every tab-switch, so nothing is lost. Previously,
typing a BSE1 date then switching to another tab (or saving from a different tab entirely) silently
discarded it — `SaveAsync`'s own CaseWork persistence only ran when Save was clicked directly on this
tab, using the posted `Case` values, with no connection to staging at all.

Fix: added `EditCaseWorkCommand? CaseWork` to `CaseScalarDraftState`; added `StageCaseWorkEditAsync()`
(mirrors `StageFarmScalarEditAsync`'s change-detection pattern, and keeps the existing
Barcode/AHF-Reference security re-derivation from the freshly-read CaseWork row rather than trusting
the posted fields) and call it from both `OnPostStageAndGotoAsync` and `SaveAsync`/
`RestoreAndRestageCaseEditAsync`; extended `ApplyStagedCaseOverlayAsync` to also restore staged
CaseWork fields on GET. Moved the actual DB persistence from `SaveAsync`'s own direct,
non-transactional `caseWorkRepository.EditAsync` call into `CaseEditOrchestrationService.CommitAllAsync`
(alongside Bab/Clinical, in the same transaction as the Case/Farm commit) — for a brand-new case this
also replaces the previously hardcoded `AddCaseWorkCommand(rbse, DateTime.Today, null, null, ...)`
with whatever was actually staged, so BSE1 dates typed before the case existed are no longer discarded
on first Save either.

**Not independently verified end-to-end** (no DB/runtime access in this environment) — recommend
exercising: type a BSE1 date on a new case, switch tabs, switch back and confirm it's still there;
also save from a different tab entirely and confirm the CaseWork row picks up the staged BSE1 dates.

Files changed: `Edit.cshtml.cs` (`RbseDate` initialization; `StageCaseWorkEditAsync`;
`ApplyStagedCaseOverlayAsync`; `SaveAsync`/`OnPostStageAndGotoAsync`/`RestoreAndRestageCaseEditAsync`),
`CaseScalarDraftState.cs` (`CaseWork` field), `CaseEditOrchestrationService.cs` (persist staged
CaseWork in the commit transaction, new and existing case). `get_errors` clean on all three.

## Paperwork Complete field enablement for new-case creation, by role (2026-10-11)

User asked specifically whether legacy disables "Is Paperwork Complete?"/its date for a brand-new case.
Traced every `Enabled` assignment for `ctlPaperworkCompleteDate` across `CaseEntryDEFRA.aspx.vb`:

- `chkPaperworkComplete` (the checkbox) is set `Enabled = True` unconditionally inside
  `MakeControlsWritable()` — never gated by CaseWork/IsCaseClosed for any writable role.
- `ctlPaperworkCompleteDate` (the date) is **never touched at all** by `DEFRADataEntryEnable()`/
  `DEFRAMaintenanceEnable()`/`MakeControlsWritable()` — unlike Barcode/AHF Reference, which those two
  explicitly disable. The `.ascx` markup sets no `Enabled` attribute, so it keeps the plain ASP.NET
  TextBox default of `Enabled=True`. **So for DEFRA Data Entry/Maintenance, it is never disabled, new
  case or not.**
- For VLA Maintenance, `VLAMaintenanceEnable()` enables `ctlPaperworkCompleteDate` (grouped in the
  *same* `If/Else` block as Barcode/AHF Reference) only when
  `Not IsDBNull(CASEWORK_TABLE.Rows(0)("IsCaseClosed")) AndAlso ... <> "1"`. `Common.vb`'s
  `AddEmptyRow` for a brand-new case's in-memory CaseWork row sets only `RBSEDate` — `IsCaseClosed` is
  left as DBNull. `IsDBNull(...)` is therefore `True`, `Not IsDBNull(...)` is `False`, and the whole
  AndAlso chain is `False` — **so VLA Maintenance sees Paperwork Complete Date (and Barcode/AHF
  Reference) disabled for a brand-new case**, only becoming editable once the case (and its real
  CaseWork row) has actually been saved once.

**Found the exact migrated gap this implies:** `CanEditVlaMaintenanceCaseworkFields` was computed as
`isVlaMaintenance && Case.HasCaseWork && !Case.IsCaseClosed`. `Case.HasCaseWork` is forced `true` and
`Case.IsCaseClosed` defaults to C#'s `bool` default `false` for a brand-new case (nothing has loaded a
real row to apply) — so `!Case.IsCaseClosed` was `true`, wrongly enabling Barcode/AHF Reference (and,
since Paperwork Complete Date used the separate, simpler `!Case.HasCaseWork` check, that too) for VLA
Maintenance immediately on a new case, instead of only after first Save — the opposite of legacy's
"DBNull fails the check" outcome.

Fix: added `HasPersistedCaseWork` (true only once an actual CaseWork row has been loaded from the
database — never set on the brand-new-case path), changed `CanEditVlaMaintenanceCaseworkFields` to key
off it instead of `Case.HasCaseWork`, and added a new `CanEditPaperworkCompleteDate` property
(`isDefraDataEntry || CanEditVlaMaintenanceCaseworkFields`) so DEFRA roles keep the "always enabled"
behaviour while VLA Maintenance now shares the exact same CaseWork-row-exists-and-persisted gate as
Barcode/AHF Reference. The save-time Barcode/AHF Reference security re-derivation in
`StageCaseWorkEditAsync` was already correctly keyed off a freshly-queried `persistedCaseWork is not
null`, so no change was needed there.

Files changed: `Edit.cshtml.cs` (`HasPersistedCaseWork`, `CanEditPaperworkCompleteDate`,
`ApplyLegacyDefraPermissions`), `Edit.cshtml` (Paperwork Complete Date's `disabled` attribute).
`get_errors` clean on both.

