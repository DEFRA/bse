# Case (DEFRA) Tab — End-to-End Workflow Parity Report

**Produced:** 2026-10-07
**Legacy page:** [bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb) (+ [CaseEntrySave.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntrySave.aspx.vb))
**Migrated page:** [src/BSE.Host/Pages/Case/Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml) / [Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs)
**Scope:** the specific user journey below, traced through both code bases line-by-line.

## User behaviour under test

> User enters/edits data on the Case (DEFRA) page, adds a new test record, checks the edited data is
> correct, navigates to another tab, edits data on that tab, continues across all tabs, and finally
> clicks **Save**.

---

## Remediation status (2026-10-07)

Fix #1 ("re-introduce true cross-tab staging for scalar fields") from §4 has been implemented for
**Farm, Case (DEFRA), Case (APHA)/Vla, BAB, and Clinical** — every tab with its own scalar "form"
whose data can be silently lost by the cross-tab gap:

- New `BSE.Host.Services.CaseScalarDraftState` + `ICaseScalarDraftStateService` stage each tab's scalar
  field edits (built the same way the existing `EditCaseCommand`/`UpdateFarmCommand`/
  `EditCaseBabCommand`/`EditCaseClinicalCommand` always were) keyed by RBSE, surviving a tab switch
  instead of being held only in an unsubmitted HTML form. Case (DEFRA) and Case (APHA) share the same
  `Case` slot, since both edit different columns of the same underlying Case row and each already
  round-trips the other's columns via hidden fields — whichever tab staged last carries the complete row.
- New `ICaseEditOrchestrationService.CommitAllAsync(rbse, userId)` opens **one** DB transaction and
  commits whatever is staged for Case, Farm, Bab, and/or Clinical together — restoring legacy's
  "whichever tab's Save is pressed commits everything staged" model. If Bab/Clinical is staged but
  the Case row itself was never edited this round, the orchestrator passes the current Case row
  through unchanged in the same transaction (mirroring legacy always re-saving the shared DataSet row
  regardless of which tab triggered the save). `IFarmRepository`/`IFarmRelationRepository`/
  `IHerdSizeRepository` gained transaction-enlisted overloads to support this; `IBabRepository` and
  `IClinicalRepository` already had them.
- `Edit.cshtml.cs`, `Farm.cshtml.cs`, `Vla.cshtml.cs`, `Bab.cshtml.cs`, and `Clinical.cshtml.cs` now
  route their Save handlers through the orchestrator instead of committing independently. For Bab and
  Clinical, only the **edit** path (an existing row) stages — first-time creation still commits
  immediately, since there's no earlier row for another tab's save to discard.
- All five pages overlay the staged draft on `OnGetAsync` (`ApplyStagedCommand`), so revisiting a tab
  shows pending edits made on another tab instead of the last-committed DB values.
- A new `OnPostStageAndGotoAsync` handler on each of the five pages validates and stages (but does not
  commit) before navigating; `_CaseTabs.cshtml` now submits to it (via a `formaction`-bound button)
  instead of a plain link when leaving any of these five tabs, restoring §2.4 (invalid data blocks tab
  navigation) for the whole group.

**Intentionally not converted — Feeds and Relations:** both are pure row-editing grids (feed records,
animal relations/dam-sire) with no scalar "form" fields of their own at risk of being silently lost —
their existing per-row Add/Edit/Delete handlers already stage immediately via
`ICaseFeedsDraftStateService`/`ICaseRelationsDraftStateService` (confirmed correct in §2.5 of the
original review), so the cross-tab data-loss gap this fix addresses does not apply to them. Their tab
links remain plain anchors (client-side `sessionStorage` snapshot only), which is sufficient for their
risk profile.

---

## Save/Cancel end-to-end comparison — follow-up review (2026-10-07)

A focused re-review of **Save and Cancel specifically**, across both the functional implementation and
the navigation behaviour, now that the UI has been consolidated to one common Save/Cancel component
(§ remediation above) and the cross-tab orchestrator is live on Farm, Case (DEFRA), Case (APHA), BAB and
Clinical.

### Legacy Save/Cancel (recap)

- **Save** (any tab): `UpdateSessionWithCaseDetails()` stages the current tab's controls into the shared
  session `DataSet`, then redirects to `CaseEntrySave.aspx`, which runs `clsCase.UpdateCaseDetails` —
  **one** DB transaction covering the Case row, Farm row, and every child table — then always redirects
  to `Home.aspx` (except the Casework button, which redirects to `CaseWorkEntry.aspx`).
- **Cancel** (`CancelCaseEdit()`, any tab): stages the current tab first (same as Save), then checks
  `clsDataCheck.DataSetHasChanges()` on **both** the Case and Farm session DataSets. If either has
  changes, shows the in-page `ExitConfirmation` control; otherwise redirects straight to `Home.aspx`.
  Critically, **nothing is cleared** on Cancel because nothing was ever committed — simply not calling
  `UpdateCaseDetails` is sufficient to discard the in-memory session state once the session ends/is replaced.

### Migrated Save/Cancel (current state)

| Tab | Save handler | Commits via | Redirects to | Cancel handler | Clears |
|---|---|---|---|---|---|
| Farm | `SaveFarm` | `CaseEditOrchestrationService` (Case+Farm, 1 txn) | **same page** (Farm) | `CancelFarmEdit` | `farmDraftState` + `caseScalarDraftState` |
| Case (DEFRA) | *(default)* | `CaseEditOrchestrationService` | **same page** (Edit) | `CancelEdit` | `caseEditDraftState` + `caseScalarDraftState` |
| Case (APHA) | *(default)* | `CaseEditOrchestrationService` | **same page** (Vla) | `CancelVlaEdit` | `caseEditDraftState` + `caseScalarDraftState` |
| BAB | `SaveBab` | orchestrator (edit path) / direct insert (create path) | **same page** (Bab) | `CancelBabEdit` | `caseScalarDraftState` |
| Clinical | `SaveSigns` | orchestrator (edit path) / direct insert (create path) | **same page** (Clinical) | `CancelClinicalEdit` | `clinicalDraftState` + `caseScalarDraftState` |
| Feeds | `SaveFeeds` | `CaseEditOrchestrationService` (same txn as Case/Farm/Bab/Clinical) | **same page** (Feeds) | `CancelFeedsEdit` | `feedsDraftState` + `caseScalarDraftState` |
| Relations | `SaveRelations` | `CaseEditOrchestrationService` (same txn as everything else) | **same page** (Relations) | `CancelRelationsEdit` | `relationsDraftState` + `caseScalarDraftState` |

### 🔴 Bug found and fixed in this review: Cancel didn't clear the cross-tab scalar draft

Before this fix, **Farm, Case (DEFRA), Case (APHA), BAB, and Clinical's Cancel handlers cleared only
their own grid draft state** (`farmDraftState`, `caseEditDraftState`, `clinicalDraftState`) — none of
them cleared `CaseScalarDraftState`, the shared store the new cross-tab orchestrator reads from. Net
effect: if a user staged a scalar edit on Tab A (e.g. typed a new Fate on DEFRA, then navigated to
Farm without saving), then clicked **Cancel on Tab B**, the Cancel appeared to discard everything —
but Tab A's staged edit silently survived in the distributed cache. If the user (or another session for
the same case) later pressed **Save on any tab**, the orchestrator would pick up and **commit the
supposedly-cancelled edit**. This is worse than the original pre-fix gap: it doesn't just lose data, it
resurrects discarded data. **Fixed** by adding `await caseScalarDraftState.ClearAsync(rbse)` to all five
Cancel handlers ([Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs),
[Farm.cshtml.cs](../src/BSE.Host/Pages/Case/Farm.cshtml.cs),
[Vla.cshtml.cs](../src/BSE.Host/Pages/Case/Vla.cshtml.cs),
[Bab.cshtml.cs](../src/BSE.Host/Pages/Case/Bab.cshtml.cs),
[Clinical.cshtml.cs](../src/BSE.Host/Pages/Case/Clinical.cshtml.cs)).

### Remaining gaps vs. legacy (suggested fixes, not yet applied)

1. **Cancel's "are you sure?" prompt doesn't see cross-tab scalar edits.** The confirm dialog on each
   page's `.bse-cancel-link`/`.bse-cancel-button` reads a server-rendered flag (`Model.HasUnsavedChanges`
   / `HasUnsavedOwnerChanges`) that only reflects **that page's own grid draft**, not
   `CaseScalarDraftState.HasPendingChanges` from another tab. A user who staged a scalar edit elsewhere
   and clicks Cancel on a "clean" tab gets no warning at all before it's discarded — legacy's
   `DataSetHasChanges` check, by contrast, covers the whole shared session object. **Suggested fix:**
   point `.bse-cancel-link`/`.bse-cancel-button` click handlers at the same async
   `/case/{rbse}/unsaved-status` endpoint already used by the Home-link guard in `_CaseTabs.cshtml`
   (expose `hasUnsavedChangesAcrossTabs()`/`clearUnsavedChangesAcrossTabs()` on `window` so each page's
   own script can call them) instead of each page's local synchronous flag.
2. **Save always returns to the same tab; legacy always returns to `Home.aspx`.** This looks like a
   deliberate UX improvement (stay in context after saving) rather than an oversight, but it is a
   navigation-behaviour difference from legacy and should be confirmed as intentional with product
   owners rather than assumed.
3. **Feeds' Cancel redirects back to the Feeds page itself, not `/Home`** — every other tab's Cancel
   (Farm, DEFRA, APHA, BAB, Clinical, Relations) redirects to `/Home`. This is an inconsistency within
   the migrated app itself, not just vs. legacy. **Suggested fix:** change
   `OnPostCancelFeedsEditAsync` to `return RedirectToPage("/Home");` for consistency with the other six tabs.
4. **Legacy's `ExitConfirmation` is a custom in-page GOV.UK-style panel; migrated uses a native
   `window.confirm()`.** Functionally equivalent (both block navigation pending user confirmation) but
   visually inconsistent with the rest of the GOV.UK Design System used everywhere else in the app —
   worth a design-system component swap if visual consistency matters, not a functional defect.

### Feeds and Relations brought into the common Save/Cancel + fix #3 applied (2026-10-07, second follow-up)

Per explicit follow-up request: Feeds' and Relations' own hand-rolled Save/Cancel markup has been
removed and both now use the same common `_CaseSaveCancel` component (top, via `_CaseTabs.cshtml`, and
bottom) as the other five tabs — `_CaseTabs.cshtml`'s per-tab Save-handler/Cancel-target mapping was
extended to cover `"Feeds"` → `SaveFeeds`/`CancelFeedsEdit` and `"Relations"` → `SaveRelations`/
`CancelRelationsEdit`, reusing their existing, unchanged handler names (no new behaviour invented).

- **Feeds is now fully in the cross-tab orchestrator.** `CaseEditOrchestrationService.CommitAllAsync`
  was extended to also diff-and-persist staged feed rows (`ICaseFeedsDraftStateService`) in the **same**
  transaction as Case/Farm/Bab/Clinical — matching legacy, where `UpdateFeedRecords` runs inside the same
  `UpdateCaseDetails` transaction regardless of which tab's Save triggered it. `FeedsModel.OnPostSaveFeedsAsync`
  no longer opens its own connection/transaction; it stages (already done by the per-row Add/Edit/Delete
  handlers) and delegates the commit to the orchestrator. Fix #3 above (Feeds' Cancel redirecting to
  itself instead of `/Home`) was applied at the same time, and its Cancel now also clears
  `CaseScalarDraftState` for consistency with the other five tabs.
- **Relations was *not* folded into the orchestrator.** Its Save handler (`OnPostSaveRelationsAsync`)
  performs an extended sequence specific to pedigree linking — dam/sire RBSE lookup/validation, herdbook
  propagation, row-stamp refresh — before persisting staged relation rows, all of which must run in a
  precise order that is specific to this one tab. Moving that logic into a generic cross-tab orchestrator
  without dedicated, carefully tested rework risked introducing regressions into a delicate workflow, so
  it was deliberately left as-is: Relations still commits via its own repositories in its own transaction,
  unchanged. Its Cancel handler now additionally clears `CaseScalarDraftState` (so it does not resurrect a
  scalar edit staged on another tab), but a Save on Farm/DEFRA/etc. still will **not** commit a staged-but-
  unsaved Relations row, and vice versa — this residual gap is called out explicitly rather than silently
  left out of the record.

### Relations folded into the cross-tab orchestrator (2026-10-07, third follow-up)

Per explicit follow-up request, the gap above has been closed: **Relations now commits through
`CaseEditOrchestrationService` too**, matching legacy's `UpdateCaseDetails`, which calls
`UpdateRelationRecords` and `UpdateDamSireRecords` in the **same** transaction as the Case row update,
regardless of which tab's Save triggered it.

- `CaseScalarDraftState` gained a `DamSire` slot (`AddEditDamSireCommand?`) — only Relations ever stages
  this, so last-write-wins with no merge logic needed (unlike `Case`, which DEFRA/APHA share).
- The orchestrator gained `IAnimalRelationsRepository`/`IPedigreeRepository` and now, within the same
  transaction as Case/Farm/Bab/Clinical/Feeds: calls `pedigreeRepository.AddEditDamSireAsync` if a
  herdbook/dam-sire edit is staged, and diffs-and-persists staged relation rows (moved verbatim from
  the former `RelationsModel.PersistStagedRelationsAsync`, now reading `ICaseRelationsDraftStateService`
  directly). A stale RowStamp on any relation row now maps to `EditCaseResult.ConcurrencyConflict`
  instead of a bespoke string message.
- **What stayed exactly as it was, unchanged:** all of Relations' pre-commit business logic —
  `ApplyPendingParentRemovals`, `InferHasDamSireFromStagedInputs`, `ValidateDamSireInputs`,
  `ResolveLinkedParentsFromRbseLookupAsync`, `RefreshMissingParentRowStampsAsync`,
  `ValidateParentRowStampsPresent`, `RefreshLinkedParentDetailsFromPedigreeAsync` — these only prepare
  and validate the `DamSire`/relation-row state and can still short-circuit the request with validation
  errors exactly as before. Only the final two steps changed: `UpdateCaseDamStatusAsync` (used to call
  `caseService.EditCaseAsync` immediately) is now `StageCaseDamStatusAsync`, which merges `DamStatus`
  into whatever `EditCaseCommand` is already staged for the Case row (or builds one fresh if nothing
  else is staged); `SaveHerdbookAsync` (used to open its own connection/transaction) is now
  `StageHerdbookAsync`, which only builds the `AddEditDamSireCommand` and stores it. The legacy-specific
  "returned code 1–4" dam/sire error-message translation is preserved, now wrapped around the single
  `caseEditOrchestration.CommitAllAsync` call instead of around the old direct repository call.
- Net effect: Relations is now a full participant in the "any tab's Save commits everything staged"
  model — a Feed row added on the Feeds tab, a Fate change staged on DEFRA, and a new relation row
  added on the Relations tab are now all committed together by a single Save click on **any** of the
  seven tabs, matching legacy's single-session, single-transaction behaviour exactly.

---

## Save button navigation on success/error — follow-up review (2026-10-07, fourth follow-up)

Focused re-review of **where the Save button takes the user, on both the success path and every error
path**, across all 7 tabs, now that Save on every tab commits through the shared orchestrator.

### Legacy Save navigation

* Every tab's Save button posts to its own page, which stages the tab's edits into the shared session
  `DataSet` and redirects to `CaseEntrySave.aspx` ([e.g. CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb)) — a dedicated, tab-agnostic "commit" page.
* `CaseEntrySave.aspx.vb` first runs `clsCase.CheckMandatoryFields(dsCase, dsFarm)`
  ([clsCase.vb](../../bsenet-v2-2025-10/BSELib/clsCase.vb#L819-L841)) across the **whole** staged case —
  both Case-level fields (eartag, Form A Date unless non-GB, Fate if Form B Date is set) and Farm-level
  fields (CPHH, Owner Name, Address 1, Parish unless non-GB, County, AHO unless non-GB, ADNS Region
  unless non-GB) — regardless of which tab's data actually changed this round, because the whole case +
  farm pair lives in one shared `DataSet`.
  * **Missing-fields path:** shows a summary of what's missing with a **"Return"** button that always
    navigates back to `CaseEntryFarm.aspx` (the Farm tab) — never back to the tab the user was actually
    on. Nothing is committed.
  * **All-present path:** calls `clsCase.UpdateCaseDetails(dsCase, dsFarm, ...)`
    ([clsCase.vb](../../bsenet-v2-2025-10/BSELib/clsCase.vb#L843-L874)) to commit everything in one
    transaction.
    * On full success with no partial errors: clears the session case state and auto-redirects to
      `Home.aspx` (or to the `redirect` query-string target — e.g. `CaseWorkEntry.aspx` — if the user
      arrived via the Casework link).
    * On partial success or an outright failure (e.g. a stale/locked row): shows an error message with a
      default **"OK"** button that navigates to `Home.aspx` regardless of which tab's data failed —
      the session is still cleared, so unsaved cross-tab edits are lost either way.
* **Net legacy behaviour:** Save always leaves the tab the user was on — success goes Home (or to the
  Casework redirect target), missing-mandatory-fields goes to Farm, any other failure goes Home. There is
  no "stay on the current tab" outcome anywhere in legacy's Save flow.

### Migrated Save navigation (before this review's fix)

* Every tab's Save handler stages its own edit, calls `caseEditOrchestration.CommitAllAsync(rbse, userId)`,
  and on `EditCaseResult.Success` returns `RedirectToPage(new { rbse = Rbse })` — **the same tab**, not
  Home (already flagged as Executive Summary item/§"Remaining gaps" item 2 — treated as an intentional,
  product-confirmed UX improvement, not a bug to fix here).
* On `EditCaseResult.ConcurrencyConflict` (or another non-success result), each tab sets an inline error
  and `return Page()` — stays on the same tab, which is arguably **better** than legacy's forced
  navigation to Home on failure (the user doesn't lose their place), so this is not flagged as a gap either.
* **Confirmed gap (🔴 Critical, now fixed — see below):** no tab performed the legacy
  `CheckMandatoryFields` equivalent. Each tab validated only **its own** fields
  (`ValidateLegacyParityRules` on DEFRA, Farm's own required-field checks, etc.) before staging/committing.
  Because `CommitAllAsync` only ever writes whatever is staged and otherwise passes through unrelated
  columns unchanged, a case could be left (or could already be) missing mandatory Farm fields (e.g. Owner
  Name cleared via direct DB edit, or a Farm row created before mandatory fields were enforced) and the
  DEFRA/BAB/Clinical/Feeds/Relations tabs' Save would commit successfully regardless — something legacy
  would have unconditionally blocked on every single Save, from every tab.

### 🔴 Fix applied in this review: cross-tab mandatory-fields check reproduced in the orchestrator

* Added `CaseEditOrchestrationService.CheckMandatoryFieldsAsync`
  ([CaseEditOrchestrationService.cs](../src/BSE.Host/Services/CaseEditOrchestrationService.cs)), which
  runs **before any transaction is opened** whenever there is anything staged to commit. It resolves the
  "effective" Case and Farm values the same way the rest of the orchestrator already does for Bab/Clinical
  passthrough — staged-this-round value if present, else the currently persisted value — then reproduces
  legacy's exact `CheckMandatoryFields` checks (Case: eartag, Form A Date unless non-GB, Fate if Form B
  Date set; Farm: CPHH/farm-exists, Owner Name, Address 1, Parish unless non-GB, County, AHO unless
  non-GB, ADNS Region unless non-GB — using `FarmRecord.IsNonGBFarm`, a stored flag, rather than
  re-deriving it from the CPHH prefix). Legacy's `.IsNull("FormBDate") And Not (.IsNull("FormBDate"))`
  check is logically self-contradictory (unreachable dead code) and is deliberately **not** reproduced,
  consistent with the original migration report's recommendation.
* If any check fails, a new `MandatoryCaseFieldsMissingException`
  ([MandatoryCaseFieldsMissingException.cs](../src/BSE.Host/Services/MandatoryCaseFieldsMissingException.cs))
  is thrown with the list of human-readable messages — thrown strictly before the connection/transaction
  is opened, so this is fully safe: no partial writes are possible.
* All 7 Save handlers now catch this exception around their `CommitAllAsync` call and surface the
  messages inline, **staying on the tab the user was on**, via each page's existing generic error
  rendering:
  * `Edit.cshtml.cs`, `Farm.cshtml.cs`, `Bab.cshtml.cs`, `Clinical.cshtml.cs`, `Feeds.cshtml.cs` —
    `ModelState.AddModelError(string.Empty, message)` + `return Page()`, rendered by the existing
    `govuk-error-summary` block already present on each of those five pages.
  * `Vla.cshtml.cs` — has no generic `ModelState`-driven error summary (only the dedicated
    `ConcurrencyError` warning banner), so the messages are joined into `ConcurrencyError` instead, to
    ensure they are actually visible rather than silently swallowed.
  * `Relations.cshtml.cs` — follows its existing error-handling convention for this handler (a
    `TempData[RelationsWarningKey]` warning + `RedirectToPage(new { rbse = Rbse })`), consistent with
    how it already surfaces the dam/sire SP-error-code messages a few lines below.
* **Deliberate divergence from legacy, documented as intentional:** legacy's missing-fields path always
  navigates to the Farm tab regardless of which tab's Save triggered it; the migrated fix keeps the user
  on **whichever tab they were on**, consistent with the rest of the migrated app's "stay in context"
  Save behaviour (see the "Remaining gaps" item 2 above) and because the error messages already indicate
  which fields (Case or Farm) are missing, so forcing a tab switch adds no information the user doesn't
  already have inline.
* `Farm.cshtml.cs`'s Save handler was also found, in the course of this fix, to not inspect
  `CommitAllAsync`'s return value at all — a concurrency conflict or any other non-success result from
  the orchestrator was silently ignored and the handler proceeded to persist the staged grids and
  redirect with a "Farm updated successfully" message regardless. This has been fixed to check for
  `EditCaseResult.ConcurrencyConflict` and any other non-`Success` result the same way the other six
  tabs already did, before persisting collections or clearing the draft.
* Files changed: `CaseEditOrchestrationService.cs` (new check + call site),
  `MandatoryCaseFieldsMissingException.cs` (new), `Edit.cshtml.cs`, `Farm.cshtml.cs`, `Vla.cshtml.cs`,
  `Bab.cshtml.cs`, `Clinical.cshtml.cs`, `Feeds.cshtml.cs`, `Relations.cshtml.cs`. `get_errors` clean on
  all of them.

---

## Field-level validation / red-star / green-star / enable-disable / cascade review (2026-10-07, fifth follow-up)

Deep, function-by-function comparison of [CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb)
against [Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml) / [Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs),
covering exactly five areas: field-level validation rules, the red "mandatory" asterisk, the green
"non-blocking warning" asterisks, field enable/disable logic, and field cascading logic.

### Legend: what a "star" means in legacy

Legacy has no ASP.NET `RequiredFieldValidator`/`CustomValidator` controls on this page at all — every
check is hand-rolled in code-behind. Two CSS classes drive the only two "*" indicators on the page
([vla-ie.css](../../bsenet-v2-2025-10/BSESystem/Style/vla-ie.css#L347-L351)):

* `.ValidatorText` → `color: Red; font-size: 300%` — used by exactly one label, `lblrfvFate`, a **red**
  asterisk next to Fate meaning "this will be required".
* `.validatortext` (same class, case-insensitive) with an inline `ForeColor="#9CCE00"` override → a
  **green/yellow** asterisk, used by `lblFormCDateWarning` and `lblHerdEntryDateWarning` — non-blocking,
  "please double-check this" warnings, never blocking Save.

### 🔴 Confirmed gap, fixed: Date of Birth vs Purchase Date check was missing

Legacy's `DateOfBirthValid` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L821-L854))
runs **three** upper-bound checks against Date of Birth: before Form A Date (or `Now()` if Form A isn't
set), before **Purchase Date** (if the case has one — set via the BAB tab), and before Onset Date (if
set). `ValidateLegacyParityRules` in `Edit.cshtml.cs` only reproduced two of the three — the Purchase
Date check was missing entirely, even though `CaseEditViewModel.PurchaseDate` was already round-tripped
through the page as a hidden field. **Fixed:** added the missing
`if (Case.PurchaseDate.HasValue && birthDate > Case.PurchaseDate.Value.Date)` check, same message
("Date of Birth must be before the Purchase Date") and same non-short-circuiting style already used by
the other two checks on this page.

### 🔴 Confirmed gap, fixed: no live visual hint that Fate is about to become mandatory

Legacy shows the red `lblrfvFate` asterisk next to Fate whenever Form B Date has a value and Fate is
still unselected — reproduced in `MakeControlsWritable()`
([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L562-L569)) and
re-evaluated on every full postback thanks to Form B Date's `AutoPostBack="True"`. This is a **live
pre-save hint**, distinct from and in addition to the blocking "Select a fate" error that only appears
after Save is clicked. The migrated page only had the blocking Save-time error — no equivalent hint
existed before Save was attempted. **Fixed:** added a `showFateRequiredStar` flag (`hasFormB &&
string.IsNullOrWhiteSpace(Model.Case.Fate)`) computed the same way the page already computes
`showFormCDateWarning`/`showHerdEntryDateWarning`, rendering a red `*` (GOV.UK error red, `#d4351c`)
next to the Fate label with the same tooltip text as legacy ("You must enter a Form B Reason"). Like the
rest of this page's enable/disable state, it is recalculated on every full page render, not live via
JS — consistent with the already-documented, larger "no client-side reactive cascade" gap (Executive
Summary item 4 / §2.3), which is unaffected by this change.

### 🟡 Noted, not auto-fixed: `lblHerdEntryDateWarning` is dead code in real legacy — migrated reproduces the *intended* logic, not the *actual* runtime behaviour

`CheckHerdEntryDate()` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L1051-L1074))
computes the green `lblHerdEntryDateWarning` star, but its only caller,
`ctlDateOfBirth_DateChanged` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L263-L267)),
has **no `Handles ctlDateOfBirth.DateChanged` clause** (confirmed: no `OnDateChanged` wiring in the
`.aspx` markup either, and this is the method's only reference in the file). Despite
`ctlDateOfBirth.AutoPostBack = True`, the `DateChanged` event has no registered handler, so
`CheckHerdEntryDate()` is **never actually invoked** — `lblHerdEntryDateWarning` starts
`Visible="False"` and nothing in the live application ever sets it `True`. The same orphaned handler
also contains `CheckBirthDateSource()`, which would otherwise clear `ddlBirthDateSource` (with a user
message) whenever Date of Birth is cleared — that logic is equally dead in production.

The migrated page computes `showHerdEntryDateWarning` directly from `Model.Case.HerdEntryDate`/
`BirthDate` on every render ([Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml#L44-L49)) — faithfully
reproducing what `CheckHerdEntryDate()` *would* do if it were ever called, which means **migrated users
will see this warning and real legacy users never did and never will.** This is a behavioural
difference from legacy's actual production behaviour, even though it matches legacy's written logic.
**Not changed in this review** — recommend keeping the migrated (working) warning rather than
reintroducing the dead-code bug, since it's a genuine, harmless, non-blocking quality-of-life
improvement, but flagging it explicitly here so it's a deliberate, signed-off decision rather than an
unnoticed behavioural drift.

### 🟡 Noted, not auto-fixed: BirthDateSource clearing on Save is stricter in migrated than real legacy behaviour

Legacy's `UpdateSessionWithCaseDetails` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L716-L814))
clears `IsBirthDateEst` to `DBNull` when Date of Birth is empty, but — because the only code that would
also clear `BirthDateSource` is the same dead `CheckBirthDateSource()` handler described above — a
stale `BirthDateSource` value is left in the database in real legacy if DOB is cleared without the
(unreachable) clearing logic running. `ApplyLegacyPreSaveNormalizations` in `Edit.cshtml.cs` clears
**both** `BirthDateSource` and `IsBirthDateEst` when `BirthDate` is empty. This is more correct than
legacy's actual (buggy) behaviour, not less, so **not changed** — recorded here as a deliberate decision
to keep the cleaner migrated behaviour rather than reproduce a dead-code artifact.

### ✅ Confirmed matching, no action needed

* Eartag mandatory-and-format validation (country/herdmark/animal-number checksum rules) — reproduced
  via `EartagValidator.Validate`, including a live AJAX hint (`OnGetValidateEartag`) that exceeds
  legacy's own per-postback `ctlEartag.Validate()` reactivity (legacy needs a full round trip; migrated
  resolves it in-browser via `fetch` with a 250 ms debounce).
* Form A/B/C date range and ordering rules, Form A Resubmitted Date range, BSE1 Received Date past-date
  check, and all six CaseWork receipt-date range checks (`PurchaserBSE1ReceivedDateValid` through
  `PaperworkCompleteDateValid`) — all reproduced with equivalent bounds and messages.
  `ValidateOptionalRange` cleanly consolidates what legacy repeats six times almost verbatim.
  Non-reproduction of the `CASEWORK_TABLE.Rows.Count <> 0` gate around these six checks is correct —
  migrated gates the same way via `Case.HasCaseWork`.
  "You must enter a Form A Date first"/"You must enter a Form B Date first" ordering guards for Form A
  Resubmitted/Form B/Form C match legacy's "should never happen but guard anyway" `Return False` paths.
* `showFormCDateWarning` matches `FormCDateValid()`'s warning condition exactly (Form C set, Form B set,
  values differ) and — unlike `lblHerdEntryDateWarning` above — legacy's equivalent trigger
  (`ctlFormCDate_DateChanged`) **is** correctly wired with `Handles ctlFormCDate.DateChanged`, so this
  one is real, live legacy behaviour, correctly reproduced (and, per the existing JS in this page,
  reproduced *more* reactively than legacy via `updateFormCWarningVisibility`).
* Field enable/disable driven by sibling-field state — Form A Resubmitted/Form B enabled only once Form A
  has a value, Fate/Form C enabled only once Form B has a value, `BirthDateSource`/`IsBirthDateEst`
  enabled only once Date of Birth has a value, and all six CaseWork receipt date fields enabled only
  when `HasCaseWork`/the CaseWork row exists — all match `MakeControlsWritable()`
  ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L533-L639)) exactly,
  just computed once per render (`hasFormA`/`hasFormB`/`hasBirthDate`/`Case.HasCaseWork`) instead of
  reactively — the known, already-tracked client-side reactivity gap (Executive Summary item 4 / §2.3),
  not a new finding.
* Form A Date non-GB read-only behaviour — reproduced exactly
  (`disabled="@(Model.IsNonGbCase ? "disabled" : null)"` vs legacy's `chkNonGBCase.Checked` branch in
  `MakeControlsWritable()`).

### Already-tracked items this review re-confirms rather than duplicates

* Role/permission 5-group matrix (incl. `IsCaseClosed` gating of Barcode/AHF Reference/Paperwork
  Complete for VLA Maintenance) vs migrated's 2-way `DataEntry`/`VLAAccess` check on Barcode/AHF
  Reference — confirmed still simplified exactly as described in Executive Summary item 3 / §2.2.
  Fixing this properly means redesigning the migrated role/claims model, which is a separate,
  larger piece of work outside the scope of this field-validation-focused review — intentionally not
  attempted here.
* Full client-side reactive cascades (Form A clear blanking Form B/C/Resubmitted/Fate; received-date
  auto-tick of the six receipt checkboxes) — confirmed still absent, as described in Executive Summary
  item 4 / §2.3. The two fixes applied in this review (Fate's red star, Date of Birth/Purchase Date
  check) do not depend on or change that gap.

Files changed: `Edit.cshtml.cs` (Purchase Date check), `Edit.cshtml` (Fate required star + its CSS).
`get_errors` clean on both.

---

## BSE1 received-dates — enable/disable, cascade and display parity (2026-10-07, sixth follow-up)

Focused review of the six "Is X Received?" checkbox + date pairs (Purchaser, Breeder, Vendor 1,
Homebred, Summary Sheet, Paperwork Complete) specifically, following up on item 4 of the Executive
Summary / §2.3's generic mention of the missing auto-tick cascade.

### ✅ Confirmed matching: the checkbox/date table split

Legacy's `UpdateSessionWithCaseDetails` writes the six `IsXReceived` checkbox flags into the **Case**
table row unconditionally ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L764-L776)),
but the six paired *date* fields only into the **CaseWork** table row, gated behind
`dsData.Tables(CASEWORK_TABLE).Rows.Count <> 0` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L780-L807)).
Confirmed matched exactly: `EditModel.OnPostAsync` always includes `IsPurchaserBse1Received` through
`IsPaperworkComplete` in the `EditCaseCommand` sent on every Save
([CaseEditViewModel.cs](../src/BSE.Host/Models/ViewModels/CaseEditViewModel.cs#L185-L190)), while the six
received-date fields are only written via `EditCaseWorkCommand`, gated by `if (Case.HasCaseWork)`
([Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs)). The display layer matches this split too:
none of the six checkboxes carry a `disabled` attribute (always editable, matching legacy's unconditional
`chkXReceived.Enabled = True` in `MakeControlsWritable()`), while every one of the six date inputs is
`disabled="@(!Model.Case.HasCaseWork)"` (matching the date fields' `ctlXReceivedDate.Enabled` gate on
`CASEWORK_TABLE.Rows.Count <> 0`). This means a case with no CaseWork row can still have "Is X Received?"
ticked and saved with **no** corresponding date in either app — a legacy quirk, faithfully reproduced
rather than silently "fixed".

### 🔴 Confirmed gap, fixed: missing auto-tick cascade

Legacy auto-ticks each checkbox the moment its paired date gets a value, via six live, correctly-wired
`DateChanged` handlers ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L376-L409))
— confirmed, unlike the DOB handler from the previous review, these six **do** carry a proper `Handles`
clause each, so they are real, live behaviour (triggered by each `CalendarDate`'s `AutoPostBack="True"`).
It is one-way only — no code ever un-ticks a checkbox when its date is cleared. The migrated page had
**no** equivalent: typing a received date and clicking Save left its checkbox exactly as the user left
it, with no automatic ticking either live or at Save time. **Fixed** with two complementary changes:

* **Client-side (live, matches legacy's live AutoPostBack behaviour):** a new `wireReceivedDateCascade()`
  in `Edit.cshtml`'s existing inline script ticks each checkbox the instant its paired date input gets a
  non-empty value (`input`/`change` listeners), using the same event-wiring style already established by
  `updateFormCWarningVisibility`. Never un-ticks, matching legacy exactly.
* **Server-side (safety net, authoritative at Save):** `ApplyLegacyPreSaveNormalizations` now also sets
  each `IsXReceived = true` whenever its paired date `HasValue`, immediately after the existing Slaughter
  Date default-from-Form-B-Date normalisation. This guarantees the same end state as legacy even if the
  page is submitted without JavaScript, or via a direct POST, closing the gap completely rather than only
  cosmetically.

### 🟡 Noted, not auto-fixed: Paperwork Complete Date's enable/disable is a legacy bug, and migrated is already more correct

`MakeControlsWritable()` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L605-L629))
explicitly sets `.Enabled` for the other five received-date controls inside the
`CASEWORK_TABLE.Rows.Count <> 0` / `Else` branches, but **never touches `ctlPaperworkCompleteDate.Enabled`
anywhere in this method.** The only places that do are `MakeControlsReadOnly()` (always `False`) and
`VLAMaintenanceEnable()`'s `IsCaseClosed` gate. Since `CalendarDate.Enabled` proxies a plain `TextBox`
that defaults to `Enabled="True"` and nothing in the `.aspx` markup overrides that default
([CaseEntryDEFRA.aspx](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx#L109)), the practical effect
is: for **DEFRA Data Entry** and **DEFRA Maintenance** roles (the only two that call
`MakeControlsWritable()` without otherwise touching this field), Paperwork Complete Date stays enabled
regardless of whether a CaseWork row exists — inconsistent with its five siblings, and inconsistent with
its own Save-time validation, which (like the other five) only ever runs inside the
`CASEWORK_TABLE.Rows.Count <> 0` block, so a date typed with no CaseWork row is silently discarded on
Save without ever being validated or saved. The migrated page applies the exact same
`disabled="@(!Model.Case.HasCaseWork)"` condition to Paperwork Complete Date as its five siblings —
consistent, and arguably correct, but not bug-for-bug identical to legacy's DEFRA-role behaviour.
**Not changed** — recorded as a deliberate decision to keep the consistent (and more correct) migrated
behaviour rather than reproduce an inconsistent legacy bug that only affects two specific roles.

### Already-tracked items this review re-confirms rather than duplicates

* The role matrix gap (Executive Summary item 3 / §2.2) is the reason the Paperwork Complete Date
  oddity above exists at all in legacy — not re-litigated here beyond the note above.

Files changed: `Edit.cshtml.cs` (received-date → checkbox normalisation), `Edit.cshtml` (JS cascade
wiring). `get_errors` clean on both.

---

## Save navigation brought into full legacy parity (2026-10-07, seventh follow-up)

Per explicit follow-up request, the two "deliberate divergence, not fixed" navigation decisions from
the fourth follow-up (above) have been reversed: Save now reproduces legacy's actual navigation targets
on every outcome, across all 7 tabs.

### What changed

* **Success → Home**, not the originating tab. `CaseEntrySave.aspx` clears the session case state and
  auto-redirects to `Home.aspx` on a fully successful save; every tab's Save handler now does the
  equivalent: `return RedirectToPage("/Home");` instead of `RedirectToPage(new { rbse = Rbse })`. No
  success banner is shown on arrival at Home — per explicit follow-up request, the success message that
  was initially added alongside this redirect (`TempData["SuccessMessage"] = "..."`) was removed; the
  silent redirect itself is the only user-visible confirmation, same as legacy's auto-redirect leaving
  no trace of a "success" banner on `Home.aspx`.
* **Missing mandatory fields → Farm tab**, not the originating tab. Every tab's
  `catch (MandatoryCaseFieldsMissingException ex)` block now does
  `TempData["ErrorMessage"] = string.Join(" ", ex.Errors); return RedirectToPage("/Case/Farm", new { rbse = Rbse });`
  (Farm's own handler redirects to itself) — matching legacy's "Return" button on the missing-fields
  summary, which always targets `CaseEntryFarm.aspx` regardless of which tab's Save triggered the check.
* **Any other commit failure (concurrency conflict, `RbseNotFound`, `AuditLogError`, `PostUpdateError`,
  etc.) → Home**, not the originating tab. Matches legacy's "error message with a default OK button that
  navigates to Home.aspx" behaviour for any `UpdateCaseDetails` failure.
* Messages are now carried via the **global** `TempData["ErrorMessage"]` key (rendered by the error
  summary already present in
  [_Layout.cshtml](../src/BSE.Host/Pages/Shared/_Layout.cshtml#L116-L137) on every page), rather than
  each tab's own locally-read `TempData["Warning"]` key or inline `ModelState` errors — necessary
  because the user is now redirected to a **different** page than the one that set the message, so a
  page-local banner would never render. Each tab's own local `Success`/`Warning` banners are unchanged
  and still used by their other (non-Save) handlers.

### Bug found and fixed in passing: 3 tabs never checked `CommitAllAsync`'s result at all

While converting the `MandatoryCaseFieldsMissingException` handling, found that `Bab.cshtml.cs`,
`Clinical.cshtml.cs` and `Feeds.cshtml.cs` called `await caseEditOrchestration.CommitAllAsync(...)`
and **discarded the returned `EditCaseResult` entirely** — a concurrency conflict or any other failure
was silently ignored and the handler proceeded straight to "Save" success messaging regardless (the
same class of bug fixed for `Farm.cshtml.cs` in the fourth follow-up, which had been missed for these
three at the time). All three now capture the result and apply the same
ConcurrencyConflict-and-other-failure handling as the other four tabs.

### Explicitly out of scope (unchanged, already tracked elsewhere)

* Legacy's success redirect sometimes targets `CaseWorkEntry.aspx` instead of `Home.aspx`, via the
  `?redirect=CaseWorkEntry.aspx` query string set by the Casework link's own `btnCaseWork_Click` handler.
  The migrated Casework link is still a plain navigation anchor that does not save first (Executive
  Summary item 2 / §2.1) — reproducing the `redirect=` target behaviour depends on fixing that link
  first, so Save unconditionally targets `/Home` for now. Tracked under the existing §2.1 item, not
  duplicated here.

Files changed: `Edit.cshtml.cs`, `Farm.cshtml.cs`, `Vla.cshtml.cs`, `Bab.cshtml.cs`, `Clinical.cshtml.cs`,
`Feeds.cshtml.cs`, `Relations.cshtml.cs`. `get_errors` clean on all seven.

---

## Missing-mandatory-fields screen now mirrors legacy's CaseEntrySave results page (2026-10-07, eighth follow-up)

Per explicit follow-up request, the missing-mandatory-fields path no longer shows a single joined
string in the global error banner — it now reproduces legacy's dedicated results screen.

### Legacy behaviour being reproduced

`CaseEntrySave.aspx.vb`'s `PerformSave()` ([CaseEntrySave.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntrySave.aspx.vb))
never redirects immediately when `CheckMandatoryFields` fails. Instead it renders its own page in
place: an intro line ("The case is missing the following items of data:"), every item from
`CheckMandatoryFields`' error list as its own paragraph, and a single button whose **text** is changed
to "Return" (from its default "OK"). Clicking it navigates to `CaseEntryFarm.aspx` — always Farm,
regardless of which tab's Save triggered the check. (The same page/pattern is reused, with different
intro text and an "OK"-labelled button targeting `Home.aspx`, for the "saved with some errors" and
"save failed outright" cases — out of scope for this follow-up, see below.)

### What changed

* New page [SaveResult.cshtml](../src/BSE.Host/Pages/Case/SaveResult.cshtml) /
  [SaveResult.cshtml.cs](../src/BSE.Host/Pages/Case/SaveResult.cshtml.cs) — a direct analogue of
  `CaseEntrySave.aspx`. Renders an intro heading, every message in its own list item (via the standard
  `govuk-error-summary` component rather than legacy's raw `<p>` tags), and a single button whose text
  and target page are both driven by a `SaveResultMode` enum (`MissingMandatoryFields` wired up now;
  `PartialSuccess`/`Failure` modes defined for future reuse but not yet called from anywhere — see
  "Explicitly out of scope" below).
* `SaveResultModel.Stage(TempData, mode, messages)` is a small static helper that JSON-serialises the
  message list into TempData (plain strings only round-trip natively through ASP.NET Core's default
  TempData provider; a `List<string>` does not, hence the explicit serialise/deserialise instead of
  storing the list directly).
* All 7 tabs' `catch (MandatoryCaseFieldsMissingException ex)` blocks now do
  `SaveResultModel.Stage(TempData, SaveResultMode.MissingMandatoryFields, ex.Errors);
  return RedirectToPage("/Case/SaveResult", new { rbse = Rbse });` instead of setting the single
  global `TempData["ErrorMessage"]` string and redirecting straight to `/Case/Farm`. The intermediate
  page itself has the "Return" button that performs the Farm-tab navigation, exactly matching legacy's
  two-step "show the list → user clicks Return → go to Farm" flow instead of a one-step redirect.

### Explicitly out of scope (noted, not implemented this round)

* The "saved with some errors" and "save failed outright" legacy branches (`PerformSave`'s other two
  `ctlDIV.InnerHtml = ...` cases) also render through the same `CaseEntrySave.aspx` page/pattern in
  legacy, with an "OK" button targeting `Home.aspx`. The migrated `ConcurrencyConflict`/other-failure
  paths (seventh follow-up, above) still use the single global `TempData["ErrorMessage"]` banner on
  Home rather than routing through `SaveResult.cshtml`. `SaveResultMode.PartialSuccess`/`Failure` are
  defined in anticipation of this, but no handler constructs them yet — this request was specifically
  about the missing-mandatory-fields screen, so the other two were left as they were rather than
  expanding scope unasked.

Files changed: `SaveResult.cshtml` (new), `SaveResult.cshtml.cs` (new), `Edit.cshtml.cs`,
`Farm.cshtml.cs`, `Vla.cshtml.cs`, `Bab.cshtml.cs`, `Clinical.cshtml.cs`, `Feeds.cshtml.cs`,
`Relations.cshtml.cs`. `get_errors` clean on all nine.

---

## Bug found and fixed: Farm's own Save blocked on fields legacy never validates there (2026-10-07, ninth follow-up)

Reported symptom: saving with ADNS Region blank still showed the error inline on the Farm tab instead
of reaching the new `SaveResult` screen.

### Root cause

`Farm.cshtml.cs`'s `ValidateRequiredFarmFields` (called from `ValidateFarmForSaveAsync`, used by both
Save and tab-switch-staging) re-validated Owner Name, Address 1, Parish, County, AHO and ADNS Region as
required **on the Farm tab itself**, returning `Page()` before the orchestrator — and therefore before
`CheckMandatoryFieldsAsync`/`MandatoryCaseFieldsMissingException`/the `SaveResult` redirect — was ever
reached. Checked legacy's actual `CaseEntryFarm.aspx.vb` `UpdateSessionWithCaseDetails` (the function
`btnSave_Click` calls before redirecting to `CaseEntrySave.aspx`): it writes `OwnerName`, `Address1`,
`Parish`, `County`, `AHO` and `ADNSRegionID` **completely unconditionally**, with no null/empty checks
at all — the only `Return False` guards in that function are for the HerdSize/LinkedFarms grids still
being mid-edit, unrelated to mandatory-data checks. These six fields are **only ever** enforced by the
cross-tab `CheckMandatoryFields` on `CaseEntrySave.aspx` — exactly what
`CaseEditOrchestrationService.CheckMandatoryFieldsAsync` already reproduces faithfully. The six checks
in `ValidateRequiredFarmFields` were an un-sourced, incorrect addition that pre-dates this review.

### Fix

Removed the Owner Name/Address 1/Parish/County/AHO/ADNS Region checks from `ValidateRequiredFarmFields`.
Kept the CPHH-required and numeric-herdmark-format checks (format/identity checks, not part of
`CheckMandatoryFields`, so not duplicates) and the separate ADNS-vs-authority **consistency** check in
`ValidateFarmForSaveAsync` (a cross-field compatibility check, not a required-ness check — untouched).
Now a blank ADNS Region (or any of the other five) on the Farm tab passes Farm's own validation, reaches
`CommitAllAsync`, trips `CheckMandatoryFieldsAsync`, and correctly redirects to `/Case/SaveResult` with
the consolidated list and "Return" button — from Farm's own Save **and** from every other tab's Save,
since all 7 tabs share the same orchestrator check.

Note: `Farm.cshtml.cs`'s separate `ValidateNewFarmDetails` (used only by the brand-new-case-creation
flow, a different page/journey entirely) was **not** touched — this review was scoped to the
edit-existing-case Save flow the symptom was reported against.

Files changed: `Farm.cshtml.cs` only. `get_errors` clean.

---

## Bug found and fixed: Farm form's "non-GB" determination disagreed with the backend's (2026-10-07, tenth follow-up)

Reported symptom: `SaveResult` showed only one of the expected missing-field messages (Owner Name and
ADNS Region both left blank, only one appeared).

### Root cause

[_FarmFormFields.cshtml](../src/BSE.Host/Pages/Farm/_FarmFormFields.cshtml) — the shared partial that
renders the Farm tab's actual input fields — computed its own `isNonGbFarm` flag by re-deriving it from
the CPHH prefix (`CphhNormalizer.Normalize(farm.CPHH).StartsWith("00")`), then used that flag to
`disabled="..."` (not `readonly`) the Parish, AHO, Authority County, Local Authority and **ADNS Region**
fields whenever it evaluated true. `CaseEditOrchestrationService.CheckMandatoryFieldsAsync`, by
contrast, uses `FarmRecord.IsNonGBFarm` — the authoritative, stored column (confirmed in the fourth
follow-up as the deliberate, correct source of truth, specifically **not** re-derived from the CPHH
prefix). When these two determinations disagree for a given farm, the UI can `disabled`-lock a field
the backend still considers mandatory. A `disabled` HTML field is never included in the form POST at
all (unlike `readonly`, which still submits its current value) — so the field's current value is lost
from that round's staged command entirely, and whether it then shows up as "missing" or silently falls
back to its last-persisted value depends on what's already in the database for that one field, making
the set of fields that actually surface on `SaveResult` inconsistent and dependent on this mismatch
rather than on what the user actually left blank.

### Fix

* Added `IsNonGBFarm` to [FarmEditViewModel](../src/BSE.Host/Models/ViewModels/FarmEditViewModel.cs),
  populated from `FarmRecord.IsNonGBFarm` in `FromRecord` — the same stored column the orchestrator
  already uses.
* `_FarmFormFields.cshtml` now reads `farm.IsNonGBFarm` directly instead of re-deriving it from the
  CPHH prefix, so the UI's field-locking and the backend's mandatory-fields check are guaranteed to
  agree for every farm, every time.
* Confirmed both callers that edit an **existing** farm (`Pages/Case/Farm.cshtml` and
  `Pages/Farm/Edit.cshtml`) populate their view model via `FromRecord`, so both pick up the fix
  automatically. The brand-new-farm-creation callers (`Pages/Farm/New.cshtml`,
  `Pages/Farm/MoveCaseNewFarm.cshtml`) construct a fresh, blank `FarmEditViewModel` with no prior
  record — `IsNonGBFarm` defaults to `false` there, which is the safe default (fields stay enabled;
  the real flag is derived by the SP on insert and only matters for edits thereafter).

Files changed: `FarmEditViewModel.cs`, `_FarmFormFields.cshtml`. `get_errors` clean on both.

---

## Mandatory-fields accumulation logic proven correct by regression test (2026-10-07, eleventh follow-up)

User reported the symptom persisted after the tenth follow-up's fix. Since this is the second report of
"only one error" and static review alone hadn't settled it, added a direct, executable regression test
rather than continuing to reason about it statically.

### What was verified

New test [CaseEditOrchestrationServiceTests.cs](../src/BSE.Modules.UserManagement.Tests/CaseEditOrchestrationServiceTests.cs)
constructs the **real** `CaseEditOrchestrationService` (not a substitute) with every dependency mocked,
stages a Farm update with **both** `OwnerName` and `ADNSRegionID` null (`IsNonGBFarm = false`, Address1/
Parish/County/AHO all present so only those two are missing), and asserts `CommitAllAsync` throws
`MandatoryCaseFieldsMissingException` whose `Errors` contains **both** messages. Ran via
`dotnet test --filter CaseEditOrchestrationServiceTests` — **passed**, proving `CheckMandatoryFieldsAsync`
correctly accumulates multiple simultaneous missing-field errors into one exception; this rules out the
orchestrator/exception/SaveResult pipeline itself as the source of the "only one error" symptom.

### Where this leaves the investigation

With the core accumulation logic now proven correct by an executable test (not just code review), the
remaining explanation is upstream of the orchestrator: either the specific scenario reproduced after the
tenth follow-up's fix genuinely only has one field missing at the point of Save (e.g. the other field is
being posted with a non-blank value the user didn't expect), or there is a narrower staging-path issue
specific to how `Farm.cshtml.cs` builds the `UpdateFarmCommand` for that exact repro that hasn't been
isolated yet. Follow-up needed: the exact message(s) now shown on `SaveResult`, to confirm whether this
is a continuing defect or already-correct behaviour the user wants double-checked.

Files changed: `CaseEditOrchestrationServiceTests.cs` (new). `dotnet test` passed (1/1).

---

## Diagnostic logging added to pin down the remaining case (2026-10-07, twelfth follow-up)

Follow-up confirmed via direct questions: Owner Name **is** genuinely blank on the test farm, yet only
"Please specify an ADNS Region for the farm." appears on `SaveResult` — not the Owner Name message too.
This is a genuine inconsistency the regression test (eleventh follow-up) proves is **not** caused by
`CheckMandatoryFieldsAsync`'s accumulation logic itself (it correctly returns both messages when both
inputs it receives are blank). Checked every code path between the browser form post and that check —
`StageFarmScalarEditAsync` fully replaces `draft.Farm` each round (no partial-merge risk),
`ApplyLegacyJointAndVlaEditGuards` only restores Herdmark/PedigreeType fields (not Owner Name), the
distributed-cache draft store does a full JSON serialise/deserialise (no merge semantics) — found no
further defect by static review alone.

Added structured logging to `CheckMandatoryFieldsAsync` (now takes an `ILogger<CaseEditOrchestrationService>`)
recording, for every Save: whether `stagedFarm` was null, `isNonGbFarm`, and the exact resolved
`ownerName`/`address1`/`parish`/`county`/`aho`/`adnsRegionId` values the checks evaluated, plus the
final error count/list for the whole check. This is purely additive (no behaviour change — confirmed by
re-running the eleventh follow-up's regression test, still passing) and gives a concrete, inspectable
answer next time this is reproduced, instead of further static guessing.

**Action needed from the user:** restart the running app (a new constructor parameter was added, so
hot-reload may not pick it up) and reproduce the Owner Name + ADNS Region scenario once more, then check
the application console/log output for a line starting `CheckMandatoryFieldsAsync farm check for
<rbse>: stagedFarmIsNull=... ownerName=... adnsRegionId=...` — that line will show definitively whether
`ownerName` was actually blank at the point the check ran, or had been populated from somewhere.

Files changed: `CaseEditOrchestrationService.cs` (added `ILogger` + 2 log statements),
`CaseEditOrchestrationServiceTests.cs` (updated for the new constructor parameter). `dotnet test`
re-run, still passing.

---

## 🔴 Root cause found and fixed: effective-value resolution silently masked a cleared mandatory field (2026-10-07, thirteenth follow-up)

The user's restarted app surfaced the real underlying defect directly as a fatal SQL error:

```
Microsoft.Data.SqlClient.SqlException: Cannot insert the value NULL into column 'OwnerName',
table 'bse_new.dbo.Farm'; column does not allow nulls. UPDATE fails.
```

### Root cause

`CheckMandatoryFieldsAsync`'s "effective value" resolution used `stagedFarm?.OwnerName ?? farm.OwnerName`
(and the equivalent for `Address1`/`Parish`/`County`/`AHO`, and separately for the Case-level
`stagedCase?.EartagCountry ?? currentCase.EartagCountry` and siblings). This `?.`/`??` combination
collapses two **different** situations into the same fallback behaviour:

1. "The whole Farm command wasn't staged this round at all" (`stagedFarm` is `null`) — falling back to
   the current DB value is correct here.
2. "The whole Farm command **was** staged this round, and this particular property on it is `null`
   because the user left that field genuinely blank on the form" — falling back to the DB value here is
   **wrong**: it silently resurrects the old value and makes it impossible for the mandatory check to
   ever see the field as missing.

Because `Farm.cshtml.cs`'s `StageFarmScalarEditAsync` always stages the **complete** current form state
(`draft.Farm = command`, a full replace, not a partial diff — confirmed in the twelfth follow-up), a
`null` on an individual property of that staged command unambiguously means "submitted blank this
round", never "not touched". Owner Name's column is `NOT NULL` in the database, so once the check
incorrectly treated a cleared Owner Name as "still has its old value", `CommitAllAsync` proceeded past
the mandatory check (no exception, no `SaveResult` redirect) straight into the real `UPDATE Farm` call
with the genuinely-null staged value — which the database then rejected outright.

This also explains the "only one error" symptom precisely: `ADNSRegionID` already used the correct
object-level pattern (`stagedFarm is not null ? stagedFarm.ADNSRegionID : farm.ADNSRegionID` — written
correctly from the start in the fourth follow-up), so a cleared ADNS Region was detected correctly,
while Owner Name (and Address 1/Parish/County/AHO, and every Case-level field) used the flawed `??`
pattern and were never detected as missing once previously saved with a value.

### Fix

Changed every effective-value resolution in `CheckMandatoryFieldsAsync` — both the Farm-level fields
(`OwnerName`, `Address1`, `Parish`, `County`, `AHO`) and the Case-level fields (`EartagCountry`,
`EartagHerdmark`, `Eartag`, `FormADate`, `FormBDate`, `Fate`) — to the same object-level pattern already
used correctly for `ADNSRegionID`: `stagedX is not null ? stagedX.Field : current.Field`. Now, whenever
the whole staged command exists, its value is used as-is (even if `null`), and the database fallback
only applies when nothing was staged for that tab at all this round.

Added a second regression test,
`CommitAllAsync_WhenStagedFarmClearsAFieldThatStillHasAnOldDbValue_StillFlagsItMissing`, which stages
Owner Name as `null` while the database still holds a non-null `"Previously Saved Owner"` — this is the
exact scenario that previously reached the database and caused the `SqlException` above, and that the
first regression test (eleventh follow-up) did not catch, because both its staged **and** DB values were
`null` for Owner Name, so the bug didn't affect that result either way.

Files changed: `CaseEditOrchestrationService.cs` (effective-value resolution fix, Case + Farm fields),
`CaseEditOrchestrationServiceTests.cs` (new regression test added).

---

## Executive summary

| # | Finding | Severity |
|---|---|---|
| 1 | Legacy stages **all 7 tabs** in one shared server-side session object and commits them in **one atomic DB transaction** whenever Save is pressed anywhere. The migrated app has **no equivalent cross-tab staging for scalar fields** — each tab is an independent page with its own immediate DB commit. Following the exact journey described (edit tab → switch tab → edit → … → Save once) **silently loses every tab's edits except the one actually carrying the Save click.** | 🔴 Critical |
| 2 | The **"Casework" link** on the migrated DEFRA tab is a plain navigation `<a>` with no save step — it does **not** stage or commit in-progress edits first. Legacy's equivalent button performs a full commit-save before navigating. This is the exact regression the original migration risk register for this page warned against. | 🔴 Critical |
| 3 | Role/permission matrix collapsed from legacy's 5 groups (+ a 3-way CaseWork gate) to a 2-way `DataEntry` / `VLAAccess` check. VLA Data Entry (should be fully read-only) is not distinguished from VLA Maintenance, and the CaseWork-field gate drops the `IsCaseClosed` condition entirely. | 🟠 High |
| 4 | Date-cascade behaviour (clearing Form A Date auto-blanks Form B/C/Resubmitted/Fate; newly entering Form A Date live-enables Form B Date) is **not reproduced client-side** — enable/disable state is computed once at page render and does not react to in-browser edits. Received-date auto-tick of the paperwork checkboxes is also absent. | 🟠 High |
| 5 | Tab-switch and Cancel no longer run the DEFRA tab's own validation chain before navigating away (legacy blocks navigation on an invalid Form A/B/C/DOB chain; migrated tab links are plain `<a>` tags with no server round-trip). | 🟡 Medium |
| 6 | Mandatory Fate-when-Form-B-set rule, the two non-blocking warnings (Form C ≠ Form B, DOB vs Herd Entry Date), and the core date-range validations **are faithfully reproduced** on Save. Tests grid add/edit/delete staging-until-Save and the single RBSE-scoped "View docs" link are also correct. | ✅ Matches |

---

## 1. Architecture comparison — the root cause

### Legacy: one session-held case, one commit

* `Page_Load` loads `Session(SV_CaseDetails)` (a `DataSet` containing the `Case` and `CaseWork` tables) and `Session(SV_TestTable)` once per case, shared across **all seven tabs** (Farm/DEFRA/BAB/VLA/Clinical/Feeds/Relations) for the lifetime of the session — see [CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L367).
* Every tab-switch handler follows the identical pattern: `If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntry<Tab>.aspx")` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L353-L373)) — this **only writes into the in-memory session `DataSet`**, never the database.
* Pressing **Save on any tab** redirects to `CaseEntrySave.aspx`, whose `PerformSave()` takes the **same shared** `dsCase`/`dsFarm` DataSets and calls `objCase.UpdateCaseDetails(userId, batchId, dsCase, dsFarm, errorList)` **once**, in one DB transaction — see [CaseEntrySave.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntrySave.aspx.vb#L36-L70).
* Net effect: a user can visit Farm → DEFRA → BAB → … → Relations, editing freely, and the **single** Save click at the end commits **everything** they changed across every tab, atomically.

### Migrated: one page per tab, one independent commit per tab

* Each tab is its own Razor Page with its own `OnGetAsync` (fresh DB read) and `OnPostAsync` (its own DB write). `Edit.cshtml.cs` calls `caseService.EditCaseAsync(command, userId)` where `command` only carries `Case` (DEFRA fields) with `Clinical: null, Bab: null, DamSire: null` — [Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs#L266-L271).
* `CaseService.EditCaseAsync` opens **its own transaction**, scoped to that single POST, and commits immediately — [CaseService.cs](../src/BSE.Modules.CaseManagement/Services/CaseService.cs#L144-L174).
* `Farm.cshtml.cs`'s `OnPostSaveFarmAsync` does the same independently via `farmService.UpdateAsync(...)` ([Farm.cshtml.cs](../src/BSE.Host/Pages/Case/Farm.cshtml.cs#L331-L338)), as do `Vla.cshtml.cs`, `VlaEdit.cshtml.cs`, `Relations.cshtml.cs` — each constructs and submits its **own** `EditCaseDetailsCommand`/update call.
* Tab links in [_CaseTabs.cshtml](../src/BSE.Host/Pages/Shared/_CaseTabs.cshtml) (`a.bse-case-tabs__link`) are **plain anchors** — a click only runs client-side `persistCurrentTabDraft()` (writes the current form's field values into `sessionStorage`, browser-local only) before the browser navigates by GET; **no POST, no server-side staging, no DB write occurs**.
* There is a *partial* server-side staging mechanism (`ICaseEditDraftStateService` and its Farm/Clinical/Feeds/Relations siblings — see [Program.cs](../src/BSE.Host/Program.cs#L483-L519)), but it **only covers grid/sub-record rows** (Tests, Linked Farms, Herd Sizes, Relations, Clinical Visits, Feeds rows) — the main scalar fields of each tab (eartag, dates, Fate, notes, CPHH, address, etc.) are **never staged**; they exist only as unsubmitted HTML form state until that specific page's own Save button is clicked.

### Consequence for the exact journey described

| Step in the scenario | Legacy result | Migrated result |
|---|---|---|
| Edit DEFRA tab fields | Staged in shared session `DataSet` | Held only in the DEFRA page's unsubmitted HTML form |
| Add a new test record | Staged in `SV_TestTable` | Staged server-side via `ICaseEditDraftStateService` (good — survives tab revisit) |
| "Check the data is correct" (re-read the page) | Reads back the same session-staged values | Reads back the **freshly-reloaded DB values** plus the staged test rows — scalar edits not yet submitted are **not shown** unless the browser's `sessionStorage` draft happens to restore them on this exact tab |
| Navigate to another tab | `UpdateSessionWithCaseDetails()` runs the full validation chain, then stages before redirecting | Plain anchor click; `persistCurrentTabDraft()` only snapshots to `sessionStorage`, no validation, no server write |
| Edit data on the new tab | Builds on the same shared session `DataSet` | Starts from a **fresh DB read** for that tab — has no knowledge of the still-unsaved DEFRA tab edits |
| "…continues with all tabs and finally Save" | The **one** final Save commits **every** tab's staged changes together | The Save click only commits the **one page it was clicked on**. Every other tab visited and edited along the way is **silently discarded** — no error, no confirmation, no indication to the user beyond a same-page-only "unsaved changes" banner that does not survive navigating to a different tab's page |

This is the single most significant functional parity gap for this workflow. It is not a cosmetic difference — it means **users who follow the exact described working pattern (common for a multi-tab wizard) will lose data** without any error being raised, because they only pressed Save once, at the end, as the legacy UI trained them to do.

---

## 2. Other confirmed mismatches and gaps

### 2.1 Casework link no longer forces a save first (🔴 Critical)

* Legacy: `btnCaseWork_Click` → `If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntrySave.aspx?redirect=CaseWorkEntry.aspx")` — i.e. it **stages then fully commits** the case before navigating to `CaseWorkEntry.aspx` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L411-L414)). This was explicitly flagged in the prior migration analysis as "the one cross-page link on this tab that differs from the audit-log pattern — committing vs. not committing before leaving the page is correctness-critical here."
* Migrated: the "Casework" item is `<a asp-page="/CaseWork/Entry" asp-route-rbse="@Model.Case.Rbse" class="govuk-link">Casework</a>` — a bare GET link ([Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml#L601-L604)). Clicking it performs **no save at all** (not even a stage), so any in-progress edits on the DEFRA tab (including newly-added test rows sitting in the per-tab draft) are abandoned with zero warning.
* **Fix:** make the Casework link a form submit (`type="submit" formaction="?handler=SaveAndGotoCasework"`) that runs the same validation + `EditCaseAsync` + `PersistStagedTestsAsync` path as the main Save handler, then redirects to `/CaseWork/Entry`, mirroring the legacy "stage/validate → commit → redirect" sequence exactly.

### 2.2 Role/permission matrix simplified (🟠 High)

* Legacy has 5 groups with distinct behaviour (`DEFRAViewerEnable`, `DEFRADataEntryEnable`, `DEFRAMaintenanceEnable`, `VLADataEntryEnable`, `VLAMaintenanceEnable`), plus a 3-way gate for Barcode/AHF Reference/Paperwork-complete date: VLA Maintenance role **and** a CaseWork row exists **and** `IsCaseClosed <> 1` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L336-L386)).
* Migrated: `ApplyLegacyDefraPermissions()` sets only `CanEditDefraNotes = User.IsInRole("DataEntry")` ([Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs#L519-L522)); the view further derives `isDefraDataEntry = DataEntry && !VLAAccess` to make Barcode/AHF Reference `readonly` ([Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml#L23)), and the six CaseWork date inputs are gated purely on `!Model.Case.HasCaseWork` ([Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml#L550-L585)) — there is **no check for `IsCaseClosed`**, and no distinction between "VLA Data Entry" (should be fully read-only, Save is a no-op) and "VLA Maintenance" (should be the only group that can edit the CaseWork-tracking fields).
* **Fix:** reintroduce the 5-way role check (or equivalent policy-based authorization) and thread `IsCaseClosed` from the `CaseWork` record into the gate for Barcode/AHF Reference/Paperwork-complete date.

### 2.3 Date auto-clear cascade is not reactive in the browser (🟠 High)

* Legacy's `AutoPostBack="True"` date controls re-run server logic on every change: clearing Form A Date blanks Form B/C/Resubmitted Date and Fate and disables them immediately ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L274-L300)); entering a Form A Date for the first time live-enables Form B Date.
* Migrated: `disabled="@(!hasFormA)"` / `disabled="@(!hasFormB)"` ([Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml#L231), [L237](../src/BSE.Host/Pages/Case/Edit.cshtml#L237), [L260](../src/BSE.Host/Pages/Case/Edit.cshtml#L260)) are computed **once at render time** from the value loaded at `OnGetAsync`/`OnPostAsync`. No JS listener is attached to `Case_FormADate` (the only wired `change`/`input` listeners are for the Form C warning, on `formBDateInput`/`formCDateInput` — see [Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml#L1105-L1108)). A user clearing Form A Date in the browser will **not** see Form B/C/Fate auto-blank or auto-disable; they only find out via a validation error after clicking Save.
* Related: legacy auto-ticks (never auto-unticks) `chkPurchaserBSE1Received` etc. when its paired received-date is set ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L376-L399)). Migrated renders the six checkboxes and six date inputs as fully independent controls with no cascade script at all ([Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml#L547-L585)).
* **Fix:** add client-side `change` handlers mirroring the four cascades (Form A clear → blank B/C/Resubmitted/Fate; Form B clear → blank C/Fate; Form A set → enable B; Form B set → enable C/Fate; each received-date set → tick its checkbox), in addition to keeping the existing server-side validation as the authoritative backstop.

### 2.4 Validation chain no longer blocks tab navigation / Cancel (🟡 Medium)

* Legacy explicitly re-runs `UpdateSessionWithCaseDetails()` (the full Form A/B/C/DOB/CaseWork date-validation chain) on **every** navigation action — Save, Cancel, and all 6 tab-switch handlers — so an invalid date chain blocks leaving the tab, not just blocks Save ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L339-L373)).
* Migrated tab links are plain anchors with no POST; `ValidateLegacyParityRules()` only runs inside `OnPostAsync` ([Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs#L247-L380)), i.e. only when this tab's own Save is clicked. A user can freely navigate to another tab (or Home, for the fields covered here) while the DEFRA tab is in an invalid state (e.g. Form B Date set with no Fate).
* This is a direct corollary of the architecture gap in §1 — fixing cross-tab staging properly (so that "navigate" implies "persist/validate current tab first") would also fix this.

### 2.5 Items confirmed to match legacy behaviour (no action needed)

* Mandatory-Fate-when-Form-B-Date-set rule — reproduced ([Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs#L256-L257)) vs legacy ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L763)).
* Form A/B/Resubmitted/CaseWork-date range validations and the DOB floor/Form-A/Onset checks — all reproduced in `ValidateLegacyParityRules` with equivalent bounds.
* The two non-blocking warnings (Form C ≠ Form B; DOB outside 18 months of Herd Entry Date) are computed and rendered, not treated as blocking errors — [Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml#L39-L48).
* Tests grid add/edit/delete is staged server-side via `ICaseEditDraftStateService` and only persisted together with the DEFRA scalar fields when **this page's** Save is clicked ([Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs#L256-L273) + `PersistStagedTestsAsync`) — correct parity with legacy's "stage grid rows, commit at Save" pattern, scoped correctly to this one tab.
* Single RBSE-scoped "View docs" link, no CPHH-scoped duplicate — correct per the legacy spec for this tab.
* Non-GB case: Form A Date forced read-only/unchanged — reproduced ([Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs#L222-L224), [Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml#L210)).
* Optimistic concurrency (RowStamp) replaces legacy's implicit single-session-owner assumption with an explicit conflict check — a genuine **improvement**, but see §3 for an interaction risk with the cross-tab gap.

---

## 3. Interaction risk: concurrency check masks, but does not fix, the cross-tab data-loss gap

`Edit.cshtml.cs` reloads the full `CaseRecord` at the top of `OnGetAsync`/`OnPostAsync` and sends the **entire** `CaseEditViewModel` back as one UPDATE guarded by `RowStamp` ([Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs#L260-L271)). Because every tab page does the same independently, if a user edits DEFRA fields, switches to the Farm tab, edits and saves Farm, then returns to DEFRA and finally saves DEFRA: the DEFRA Save's own RowStamp was captured **before** the Farm save, so it will not itself detect a stale write — but the Farm tab's commit happened on a case row snapshot taken **before** the user's original DEFRA edits were persisted, and the final DEFRA save was built from a view model loaded **before** the Farm edit. Each individual commit succeeds (no concurrency error surfaces), yet the end state is whichever tab's independent commit happened to touch which columns last — there is no single source of truth the way legacy's shared session `DataSet` guarantees. This is a second-order consequence of §1, not a separate bug, but worth calling out because the RowStamp mechanism could otherwise be mistaken for "this is already handled."

---

## 4. Recommended fix plan (priority order)

1. **Re-introduce true cross-tab staging for scalar fields**, not just grid rows. Extend the existing `ICaseEditDraftStateService` family (or a new unified `ICaseEditSessionStateService`) to hold the in-progress scalar field values for **all seven tabs** keyed by RBSE, populated on each tab's `OnGetAsync`/edit, and only flushed to the database by a single consolidated "Save" action — functionally restoring legacy's "one session, one commit" model. This directly resolves §1 and, as a side effect, §2.4 (navigation can require/trigger validation of the staged state).
2. **Fix the Casework link** to submit-and-save (or submit-and-stage, once #1 lands) before redirecting to `/CaseWork/Entry`, exactly mirroring `CaseEntrySave.aspx?redirect=CaseWorkEntry.aspx`.
3. **Restore the 5-way role matrix** and the 3-way CaseWork gate (role **and** CaseWork row exists **and** not closed) for Barcode/AHF Reference/Paperwork-complete date.
4. **Add the client-side date cascade** (auto-blank/auto-disable on clear, auto-enable on entry, auto-tick receipt checkboxes) so the live editing experience matches legacy, backed by the already-correct server-side validation.
5. Once #1 is in place, re-enable "block navigation on invalid state" for tab switches/Cancel to fully restore §2.4.

## 5. Open items carried over for follow-up (not newly introduced by this review)

* Full legacy `CaseEntrySave.aspx` / `clsCase.UpdateCaseDetails` DB-commit semantics for the Test table were never fully traced in the original legacy analysis — worth confirming before treating the migrated "stage tests, commit with DEFRA tab" pattern as 100% equivalent in every edge case (e.g. what legacy does if a user adds a test row then saves from the *Farm* tab instead of DEFRA).
* `BSELib.clsDataCheck.DataSetHasChanges()` exact change-detection semantics (legacy) vs. the migrated `HasPendingChanges` flags — not bit-for-bit compared here; recommend a dedicated test once fix #1 is implemented.
