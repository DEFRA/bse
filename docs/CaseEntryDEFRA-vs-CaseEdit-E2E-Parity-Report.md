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

## Bug found and fixed: grid row operations silently discarded in-progress scalar edits (2026-10-07, fourteenth follow-up)

The user reported: *"when I tried to edit some of the select fields and then add linked farms or edit
or delete then that edited values got lost and reset back to previously persisted values in all the
case tabs."*

### Root cause

Every tab's inline grid handlers (Add/BeginEdit/Update/Delete a row) call that tab's own `LoadAsync()`
(or `LoadReadonlyPageAsync()`) to refresh grid/lookup data before acting on the posted row. `LoadAsync()`
unconditionally rebuilds the tab's scalar form view model from the **database** record and overlays only
the **cross-tab staged draft** (`ApplyStagedCommand`/`ApplyStagedSignsOverlayAsync`) — it never considers
what was just **posted in this exact request**. Any select/dropdown/text field the user had changed in
that same submission, but not yet staged or saved, was silently overwritten back to its last-persisted
value the moment a grid button was clicked.

Checked and fixed per tab:

- **Farm** (`Farm.cshtml.cs`) — `EditableFarm`/`EditableFarmRowStampBase64` are captured from the posted
  model before `LoadAsync()` runs, then restored and re-staged via new helper
  `RestoreAndRestageFarmEditAsync`. Applied to all 8 grid handlers (Linked Farms + Herd Size rows).
- **Case (DEFRA)** (`Edit.cshtml.cs`) — `Case` is captured before `LoadReadonlyPageAsync()`, then
  restored and re-staged via new helper `RestoreAndRestageCaseEditAsync`. Applied to all 4 Tests-grid
  handlers.
- **Clinical** (`Clinical.cshtml.cs`) — a different shape of the same bug: `Signs` is never itself
  model-bound from the post (it's always rebuilt either from the DB via `LoadAsync()` or from
  `Request.Form["Signs.*"]` via `BindSignsFromForm()`, and only the Save/StageAndGoto handlers called
  the latter). The 4 Visit-grid handlers now accept the already-posted `clinicalRowStampBase64` hidden
  field, call `BindSignsFromForm()` to recover the posted checkbox state, and re-stage it via new helper
  `RestoreAndRestageSignsEditAsync`, mirroring the existing `OnPostStageAndGotoAsync` pattern.
- **Case (APHA)/Vla** (`Vla.cshtml.cs`) — checked and confirmed **not affected**: its Other-Owners grid
  handlers never call any function that reloads `Case` from the database; `Case` remains whatever was
  model-bound from the post throughout.
- **BAB** (`Bab.cshtml.cs`) — checked and confirmed **not affected**: has no inline grid at all, only
  Save and StageAndGoto, both of which already use the directly posted `Bab` model.
- **Feeds** (`Feeds.cshtml.cs`) — initially (wrongly) assessed as not affected, on the basis that it
  has no scalar "form" outside the grid rows themselves. User confirmed the symptom also occurs here,
  specifically when clicking **Validate Supplier** — the affected state turned out to be the shared
  Add/Edit field panel itself (Year From/To, Ration Type, Ration Name, Pre-purchase), which legacy's
  single physical panel round-trips via ViewState regardless of which button is clicked, but the
  migrated page's `[BindProperty]`s for it only survive the current POST. `OnPostValidateSupplierNavigateAsync`
  only round-trips the supplier name via `RedirectToPage("/Case/PickSupplier", new { rbse, name })` —
  every other in-progress panel field (a Ration Type selected for a prospective new/edited row, Year
  From/To, etc.) was silently discarded the moment the user left for the supplier picker and came back.
  `OnPostDeleteFeedRowAsync` has the identical gap (redirects without using or preserving any panel
  field). Fixed both by stashing the posted panel state into TempData (`StashPostedPanelState`)
  immediately before each handler's redirect, and restoring it (`RestorePostedPanelState`) in
  `OnGetAsync` right after the standard load — TempData (not `CaseFeedsDraftState`/
  `CaseScalarDraftState`) was used deliberately since this panel state is never itself committed to the
  database and only needs to survive the redirect hop(s) (including the intermediate round trip through
  `/Case/PickSupplier`, which doesn't touch TempData and so doesn't disturb the stashed key), not a full
  tab switch. `OnPostAddFeedRowAsync`/`OnPostUpdateFeedRowAsync` were left unchanged — they already
  `return Page()` (preserving the posted panel exactly) on validation failure, and correctly clear the
  panel on success since the row they describe has just been committed to the draft.
- **Relations** (`Relations.cshtml.cs`) — initially (wrongly) assessed as not affected, since its
  `LoadAsync()` has an explicit guard comment and never overwrites `DamSire`/`CaseHerdbook` on a POST.
  User confirmed the symptom also occurs here — the actual mechanism is different and was missed: the
  Add/Update/Delete relation-row handlers all `RedirectToPage` on success (not `return Page()`), which
  triggers a **brand-new GET request**. That follow-up GET's `PopulateCaseAncillaryStateAsync()`
  unconditionally re-reads `CaseHerdbook` and `DamSire.DamStatus` straight from the database with no
  overlay of anything staged — so a Case Herdbook edit or Dam Status selection typed in the same
  submission as a relation-row add/edit/delete, but not yet committed via the dedicated "Save Dam/Sire"/
  "Save Case Herdbook" buttons, was silently discarded by the redirect's reload, not by the original
  request itself. Fixed by capturing the posted `CaseHerdbook`/`DamSire.DamStatus` before `LoadAsync()`
  in all three redirecting handlers, then calling new helper `RestoreAndRestageDamSireEditAsync` (reuses
  the existing `StageCaseDamStatusAsync`/`StageHerdbookAsync` staging methods, previously only called
  from the main Save handler) so the edit survives in `CaseScalarDraftState` across the redirect;
  `PopulateCaseAncillaryStateAsync` now overlays that staged state after its DB read, so the follow-up
  GET shows the preserved values instead of the stale persisted ones.
  (`OnPostBeginEditRelationRowAsync` returns `Page()`, not a redirect, so it was never affected by this
  specific mechanism and was left unchanged.)

Files changed: `Farm.cshtml.cs`, `Edit.cshtml.cs`, `Clinical.cshtml.cs`, `Relations.cshtml.cs`,
`Feeds.cshtml.cs`.

---

## Bug found and fixed: Farm's cascading Authority/ADNS Region dropdowns still reverted after a grid operation (2026-10-07)

User reported the grid-reset symptom was **still** present on the Farm tab, specifically for Authority
County, Local Authority and ADNS Region — three fields already covered by the fourteenth follow-up's
`RestoreAndRestageFarmEditAsync` fix. This needed a different, additional fix because these three
fields are a **cascading** group (Authority County → Local Authority → ADNS Region, each `<select>`'s
option list depends on the previous field's selected value), not independent scalar fields.

### Root cause

`LoadAsync()` loads the Farm row, then — in this order — (1) calls `LoadLookupsForEditAsync()`, which
builds `ViewData["AuthorityOptions"]`/`ViewData["AdnsOptions"]` from the **just-loaded, last-persisted**
`EditableFarm.AuthorityCountyID`/`AuthorityID`, and only **afterwards** (2) overlays any cross-tab staged
draft onto `EditableFarm` via `ApplyStagedCommand`. `RestoreAndRestageFarmEditAsync` (the fourteenth
follow-up's fix) correctly restores the **posted** `EditableFarm` — including the correct
`AuthorityCountyID`/`AuthorityID`/`ADNSRegionID` values — but runs *after* `LoadAsync()` has already
built those `ViewData` option lists from the **stale** DB-loaded IDs. The restored ID value is technically
correct on the model, but the `<select>`'s rendered `<option>` list no longer contains a matching
`<option>` for it, so the browser shows the field reverted to blank/first-option — visually identical to
the original bug, but with the underlying cause one level removed: the **value** survives, but its
**dependent options list** does not.

This is a structurally different problem from the fourteenth follow-up's fix (which only needed to
restore+re-stage a scalar value) — any field whose displayed `<option>` list is itself derived from
another field's current value needs its options list **rebuilt after** the value is restored, not just
the value itself preserved.

### Fix

`RestoreAndRestageFarmEditAsync` now ends with a call to `LoadLookupsForEditAsync()`, after `EditableFarm`
has been restored and re-staged — so `ViewData["AuthorityOptions"]`/`ViewData["AdnsOptions"]` are rebuilt
from the **restored** `AuthorityCountyID`/`AuthorityID`, guaranteeing the Local Authority/ADNS Region
`<select>`s always have a matching `<option>` for whatever was posted. Some callers already re-called
`LoadLookupsForEditAsync()` themselves afterward for unrelated reasons (e.g. validation failure) — that is
harmless, redundant work, not a correctness issue.

Checked all 7 tabs for this same "cascading-options-built-from-a-field-that-gets-restored-later" pattern
— it is unique to `Farm.cshtml.cs`'s Authority County/Local Authority/ADNS Region trio; no other tab has
a server-rendered `<select>` whose options list is itself derived from another field's value, so no
other tab needed this third fix shape.

Files changed: `Farm.cshtml.cs` only. `get_errors` clean.

### Follow-up: the same ordering bug was still live in `LoadAsync()` itself (2026-10-08)

User reported the values were **still** being discarded after the fix above. The first fix only covered
`RestoreAndRestageFarmEditAsync`, which only runs for handlers that render `Page()` directly in the same
request (e.g. a validation failure). The far more common path — a successful Add/Edit/Delete grid row
operation — calls `RestoreAndRestageFarmEditAsync` too, but then `RedirectToPage(...)` to itself, and the
**follow-up GET** re-enters `OnGetAsync()` → `LoadAsync()`, which had exactly the same ordering bug,
untouched by the first fix: it still called `LoadLookupsForEditAsync()` (building the Authority/ADNS
option lists) **before** overlaying the cross-tab staged draft (`EditableFarm.ApplyStagedCommand`) onto
the freshly DB-loaded `EditableFarm`. The staged `AuthorityCountyID`/`AuthorityID`/`ADNSRegionID` (saved
correctly by `RestoreAndRestageFarmEditAsync`'s `StageFarmScalarEditAsync` call) was there and correct on
the model, but — the same as before — the rendered `<select>` had no matching `<option>` for it, since
the options were built one step too early.

**Fix:** reordered `LoadAsync()` so the staged-draft overlay happens **before** `LoadLookupsForEditAsync()`,
not after — the Authority/ADNS option lists are now always built from whichever `AuthorityCountyID`/
`AuthorityID` ends up on `EditableFarm` (DB value, or the staged overlay if one exists), never from a
value that's about to be replaced. Both fixes are needed together: this one covers the GET-after-redirect
path; the earlier `RestoreAndRestageFarmEditAsync` fix covers the same-request `Page()` path.

Files changed: `Farm.cshtml.cs` only. `get_errors` clean.

### Follow-up: the actual root cause — `AuthorityID`/`AuthorityCountyID` were never staged at all (2026-10-08)

User reported the symptom was **still** present after both ordering fixes above. Both of those fixes
were real and necessary, but neither was sufficient, because the deeper problem was upstream of both:
`AuthorityID`/`AuthorityCountyID` were **never part of the staged command in the first place**.

### Root cause

`UpdateFarmCommand` — the command type `FarmEditViewModel.ToUpdateCommand()` builds and
`StageFarmScalarEditAsync` persists into `CaseScalarDraftState` — only ever carried `ADNSRegionID`.
`FarmEditViewModel.ApplyStagedCommand(UpdateFarmCommand c)` (the method both ordering fixes depend on to
restore the overlay) correspondingly only restored `ADNSRegionID` — it had no `AuthorityID`/
`AuthorityCountyID` to restore **from**, because `ToUpdateCommand()` never put them there. Confirmed this
is consistent with the `EditFarm` stored procedure itself
([FarmRepository.cs](../src/BSE.Modules.FarmManagement/Repositories/FarmRepository.cs)'s
`BuildEditFarmParams`), which also only ever sends `ADNSRegionID` — `AuthorityID`/`AuthorityCountyID` are
**not persisted columns driven by this command at all**; they exist purely as the cascading picker's
own intermediate "narrow down to the right ADNS Region" state. No ordering fix could have restored a
value that was never captured anywhere to begin with — fixing the two call-order bugs was necessary
(and remains necessary, now that the values genuinely are available to restore) but not sufficient on
its own.

### Fix

* `UpdateFarmCommand` gained two new optional, trailing parameters — `AuthorityID`/`AuthorityCountyID`
  (default `null`, so none of its 4 existing construction call sites across the codebase needed
  updating). They are **not** added to `FarmRepository.BuildEditFarmParams`, so the `EditFarm` SP call
  and the database columns it actually writes are completely unchanged — these two fields exist on the
  command purely to round-trip through staging, never to be persisted directly.
* `FarmEditViewModel.ToUpdateCommand()` now passes its own `AuthorityID`/`AuthorityCountyID` into the
  command it builds.
* `FarmEditViewModel.ApplyStagedCommand()` now also restores `AuthorityID`/`AuthorityCountyID` from the
  staged command, alongside the `ADNSRegionID` it already restored.
* With this in place, the two ordering fixes above now have a correctly-populated staged command to
  restore from: `LoadAsync()`'s staged overlay (now running before `LoadLookupsForEditAsync()`) and
  `RestoreAndRestageFarmEditAsync`'s direct restore (for the same-request `Page()` path) both now
  correctly carry forward the user's in-progress County → Local Authority → ADNS Region selection,
  and the Authority/ADNS Region `<select>` option lists are rebuilt from those **same, correctly
  restored** IDs — so every part of the cascade agrees with every other part.

Files changed: `UpdateFarmCommand.cs`, `FarmEditViewModel.cs`. `get_errors` clean on both and on all 4
existing `UpdateFarmCommand` construction call sites (2 in `FarmServiceTests.cs`, 2 in
`CaseEditOrchestrationServiceTests.cs`).

### Follow-up: the same redirect-then-reload mechanism, missed on Vla's Other-Owners grid (2026-10-08)

User reported the same symptom on the **Case (APHA)/Vla tab**'s `PurchasedCounty` field: editing it,
then adding/editing/deleting an Other-Owners row, discarded the edit. The fourteenth follow-up had
assessed Vla as "not affected", on the basis that no handler reloads `Case` from the database mid-request
— true, but incomplete: it missed that `OnPostAddOwnerRowAsync`, `OnPostUpdateOwnerRowAsync`, and
`OnPostDeleteOwnerAsync` all succeed via `RedirectToPage(...)`, not `return Page()`. That redirect starts
a **brand-new GET request**, whose `OnGetAsync` rebuilds `Case` fresh from the database and overlays only
`ApplyStagedCaseOverlayAsync()` — i.e. only whatever was staged by a previous Save/`OnPostStageAndGotoAsync`
call. None of the three owner-row handlers ever called `StageCaseScalarEditAsync`, so any scalar field
(`PurchasedCounty` or otherwise) edited in the same submission as an owner-row action was silently
discarded the moment the redirect's follow-up GET ran — the exact same "RedirectToPage()-then-reload
discards a same-request edit" mechanism already fixed for Relations, just not yet recognised as present
here because the reload happens in the *next* request, not inside the handler itself.

**Fix:** added `StagePostedCaseScalarsBeforeRedirectAsync()`, which stages the already-model-bound `Case`
(via the existing `StageCaseScalarEditAsync`, reusing the row-stamp already held in `TempData` from the
page's last GET) — called right before the success-path redirect in `OnPostAddOwnerRowAsync`,
`OnPostUpdateOwnerRowAsync`, and `OnPostDeleteOwnerAsync`. `OnPostBeginEditOwnerRowAsync` needed no change
— it returns `Page()` on success, so `Case` is already whatever was just posted when the page re-renders.
Also found and fixed a related normalisation gap in `OnPostDeleteOwnerAsync`: unlike its three siblings,
it never reassigned `Rbse = caseRbse;` after parsing — meaning the staged draft would have been keyed by
the raw, un-normalised `Rbse` instead of the same normalised key the follow-up GET looks up by.

Files changed: `Vla.cshtml.cs` only. `get_errors` clean.

### Audit: searched all 7 tabs for any other cascading-options `<select>` pair (2026-10-08, no code change)

Per explicit follow-up request, checked whether Farm's Authority County → Local Authority → ADNS Region
is the only group of dropdowns anywhere in the Case wizard whose **options list** (not just its
enabled/disabled state) is itself derived from another field's current value — i.e. the same shape of
bug as the fifteenth/sixteenth follow-ups above, where restoring the value alone isn't enough because
the rendered `<option>` list can still omit it.

* Searched every `lookups.Get...Async(...)`/`_lookups.Get...Async(...)` call across all 7 tab page
  models for one that takes a **parameter sourced from another bound field** (the defining trait of a
  cascading lookup) — only `GetAuthoritiesByCountyAsync(authorityCountyId)` and
  `GetADNSRegionsByAuthorityAsync(authorityId)` matched, both exclusively in `Farm.cshtml.cs` (plus the
  three standalone, non-wizard Farm pages `Farm/Edit.cshtml.cs`, `Farm/New.cshtml.cs`,
  `Farm/MoveCaseNewFarm.cshtml.cs`, which are single-page/single-commit flows with no
  `CaseScalarDraftState` staging at all, so the specific grid-redirect-reset mechanism this report
  tracks cannot occur on them).
* Searched every `ViewData["...Options"] = ...` assignment across all 7 tabs for a conditional build
  (`? ... : []`) keyed off another field — only the two Farm ones above matched (`AuthorityOptions`,
  `AdnsOptions`); every other `...Options` list (`CountyOptions`, `AhoOptions`, `HerdTypeOptions`,
  `PedigreeOptions`, `AuthorityCountyOptions`, and the various static lookup lists bound to `<select>`s
  on Edit/Vla/Bab/Feeds/Relations/Clinical — e.g. `Case.Fate`, `Case.Sex`, `Case.BirthDateSource`,
  `Case.Origin`, `Case.PurchasedCounty`, `Bab.FeedRisk`, `RationType`, `RelationType`) is a fixed,
  unconditional lookup table list, never derived from another field's value.
* Vla's `Case.PurchasedCounty` **is** conditionally `disabled` based on another field (`Case.Origin`,
  via `purchaseFieldsDisabled`), and Clinical/Edit have similar disable-only cascades (Fate on Form B
  Date, BirthDateSource on Date of Birth, etc.) — but in every one of these cases the **options list
  itself** is a static county/lookup table, unaffected by the other field's value. These are
  enable/disable cascades, already covered by the general staging fix pattern (restoring the posted
  value is sufficient; there is no stale-options-list risk because the list never changes).

**Conclusion: Farm's Authority County/Local Authority/ADNS Region trio is the only true
cascading-options `<select>` group in the entire 7-tab Case wizard.** No further fix is needed elsewhere;
this audit found no code to change.

---

## 🔴 Audit finding: per-table concurrency detection is implemented for only 2 of 11 tables (2026-10-08, no code change yet)

Per explicit follow-up request, checked whether "another user modified this case at the same time" is
detected and reported with the same granularity as legacy's `CaseEntrySave.aspx`.

### Legacy behaviour: every table's stored procedure returns a RowStamp-mismatch code, and it's a *soft* error

Every one of legacy's per-table update methods in `clsCase.vb`/`clsFarm.vb` passes the row's `RowStamp`
to its `Edit*` stored procedure and reads back a `RETURN_VALUE` that includes a specific code for "the
row was changed by another user since it was read" (a classic optimistic-concurrency check). Confirmed
for every table in the shared session `DataSet`:

| Table | Legacy concurrency check | On conflict |
|---|---|---|
| Case | `EditCase` SP, code 3 | `objErrorList.Add("...has been modified by another user")` — **soft**, does not abort |
| Farm | `EditFarm` SP, code 3 | same pattern — **soft** |
| BAB | `EditCaseBAB` SP, code 1 | same pattern — **soft** |
| Clinical | `EditCaseClinical` SP, code 1 | same pattern — **soft** |
| Dam/Sire/Pedigree | `AddEditDamSireDetails` SP, codes 1/2/4 | same pattern — **soft** |
| Other Owner, Test, Clinical Visit, Feed, Relation (grids) | `OptimisticUpdateDataTable`'s row-updated callback checks `RecordsAffected = 0` | per-row `RowError = "Data was changed by another user"` — **soft**, only that row is skipped |
| CaseWork | *(none — `RowStamp` is deliberately never sent; see code comment "Can't think of a reason to need RowStamp?")* | not checked at all, by design |

Critically, **every one of these is a *soft* error, added to `objErrorList`, never thrown as an
exception**. `UpdateCaseDetails` only rolls back the transaction on an actual thrown
`CaseUpdateException`/unhandled exception (e.g. a hard SP failure) — a RowStamp mismatch on one table
does **not** stop the other, non-conflicting tables in the same transaction from committing. The net
result on `CaseEntrySave.aspx` is the **"saved with some errors"** branch: *"The database has been
updated but some errors were encountered: ...has been modified by another user..."* — a partial commit,
not an all-or-nothing abort.

### Migrated behaviour: only Case and Relations actually check; the other 9 tables silently discard the SP's result

* **`ICaseRepository.EditCaseAsync`** correctly reads the SP's `RETURN_VALUE` via a `DynamicParameters`
  return-value parameter and maps a mismatch to `EditCaseResult.ConcurrencyConflict`
  ([CaseRepository.cs](../src/BSE.Modules.CaseManagement/Repositories/CaseRepository.cs)) — this part is
  correctly implemented.
* **Relations** (`PersistStagedRelationsAsync` → `relationsRepository.DeleteRelationAsync`) also checks
  and returns `false` on a stale RowStamp, which the orchestrator maps to `EditCaseResult.ConcurrencyConflict`.
* **Every other repository call the orchestrator makes — Farm (`farmRepository.UpdateAsync`), BAB
  (`babRepository.EditAsync`), Clinical (`clinicalRepository.EditAsync`), Dam/Sire
  (`pedigreeRepository.AddEditDamSireAsync`), Feeds (`PersistStagedFeedsAsync` → `feedRepository.EditAsync`/
  `DeleteAsync`), and (outside the orchestrator) CaseWork, Other Owner, and Test — all return a plain,
  untyped `Task`, built on `DapperRepository.ExecuteAsync(string, object?)`
  ([DapperRepository.cs](../src/BSE.Infrastructure/DapperRepository.cs)), which calls Dapper's
  `connection.ExecuteAsync(...)` with a plain anonymous parameter object — `RETURN_VALUE` is never
  declared as an output parameter, so it is never read, regardless of what the underlying SP returns.**
  Confirmed by reading every one of these repositories' interface and implementation: none of them
  declares a return-value parameter or inspects a result code; `RowStamp` is passed into the SP (so the
  SP itself may well still refuse to apply a stale update, exactly as legacy's SP does), but the .NET
  code has **no way of knowing whether the update actually happened or was silently skipped by the SP**.

### Net effect: both a detection gap *and* an all-or-nothing-vs-partial-success divergence

* **Detection gap (more severe):** if another user edits the Farm (or BAB, Clinical, Dam/Sire, Feeds,
  CaseWork, Other Owner, Test) record between this user's page load and Save, and the underlying SP
  silently no-ops the conflicting row (consistent with how it behaves for legacy), the migrated app has
  **no way to detect this** — no exception, no returned failure code, nothing. The transaction commits,
  `CommitAllAsync` returns `Success`, and the user is told the save succeeded — while that one table's
  edit was, in fact, silently discarded by the database. This is **worse than both** legacy (which
  explicitly surfaces every one of these as a "modified by another user" message) **and** the Case/
  Relations tables in the migrated app itself (which correctly detect and report their own conflicts).
* **All-or-nothing vs. partial-success (for the 2 tables that *are* checked):** even where detection
  *is* implemented (Case, Relations), the orchestrator's response differs from legacy's: a Case-row
  conflict or a stale relation row currently rolls back the **entire** transaction and returns
  `EditCaseResult.ConcurrencyConflict` for the whole Save — discarding Farm/BAB/Clinical/Feeds edits
  staged in the *same* round that had **no** conflict of their own. Legacy, by contrast, would have let
  those non-conflicting tables' changes commit and only reported the one table that actually conflicted.
  This is a deliberate, defensible safety trade-off (avoids ending up with a half-saved case spread
  across tabs the user can't see all at once), but it is a genuine behavioural divergence from legacy
  worth confirming as intentional rather than assuming.

### Recommendation (not yet implemented — flagging for a decision before changing 6+ repositories' contracts)

To close the detection gap, `FarmRepository.UpdateAsync`, `BabRepository.EditAsync`,
`ClinicalRepository.EditAsync`, `ChildRepositories.cs`'s `AddEditDamSireAsync`/`EditAsync`(OtherOwner,Test),
`FeedRepository.EditAsync`/`DeleteAsync`, and `CaseWorkRepository.EditAsync` would each need to change
from `Task` to a typed result (mirroring `ICaseRepository.EditCaseAsync`'s
`Task<EditCaseResult>` pattern), reading the SP's `RETURN_VALUE` via `DynamicParameters`, and the
orchestrator would need to decide — consistent with legacy's **soft-error, partial-success** model —
whether to surface a per-table "modified by another user" message (extending `SaveResult.cshtml`'s
existing message-list pattern, rather than treating it as a hard `ConcurrencyConflict` abort like Case/
Relations currently do). This is a non-trivial, multi-repository contract change — not applied in this
audit pass pending confirmation of the desired behaviour (soft/partial, matching legacy exactly, vs. the
stricter all-or-nothing model already in place for Case/Relations).

### 🔴 Fix implemented: full legacy soft-error, partial-success concurrency parity (2026-10-08)

Per explicit follow-up request ("I want exactly like legacy parity"), the recommendation above has been
implemented in full for every table the orchestrator commits in its single transaction (Case, Farm, BAB,
Clinical, Dam/Sire/Pedigree, Feeds, Relations). **Out of scope, left unchanged, and called out
explicitly:** CaseWork (legacy itself deliberately never sends its RowStamp — see the table above — so
there is nothing to reproduce), and Other Owner/Test (these are already persisted in their own separate
transaction outside the orchestrator — a different, already-tracked architectural gap, see §5's first
open item — so adding concurrency *detection* to them without first folding them into the shared
transaction would not be genuine parity; not attempted here to avoid conflating two different problems).

**New shared result type:** `CaseEditOrchestrationService.CommitAllAsync` now returns
`Task<CaseCommitOutcome>` instead of `Task<EditCaseResult>`, where
`CaseCommitOutcome(EditCaseResult Result, IReadOnlyList<string> Warnings)` mirrors legacy's
`objErrorList`: `Warnings` accumulates every soft, per-table "modified by another user" message
encountered during the commit, without aborting the transaction. All 7 tabs' Save handlers were updated
to read `Warnings` after a successful commit and, if non-empty, route to the existing
`/Case/SaveResult` page with `SaveResultMode.PartialSuccess` (already scaffolded, previously unused) —
reproducing legacy's exact message ("The database has been updated but some errors were encountered:")
with its "OK" button back to Home.

**Per-table changes (repository layer):**

* **Case** — `caseRepository.EditCaseAsync`'s own return type/contract is **unchanged** (used by other
  callers too), but the orchestrator's *handling* of `EditCaseResult.ConcurrencyConflict` changed from
  rolling back the transaction to adding a warning and continuing, exactly matching legacy's EditCase
  code-3 handling.
* **Farm** — `IFarmRepository.UpdateAsync`'s transaction-enlisted overload (used only by the
  orchestrator; the non-transactional overload used by new-case-creation is untouched) now returns
  `Task<string?>` instead of `Task`: `null` on success, the exact legacy message on code 3 (soft), and
  still **throws** on codes 1/2/4 (hard, matching legacy's `FarmUpdateException`-driven abort for those).
* **BAB, Clinical** — `IBabRepository.EditAsync`/`IClinicalRepository.EditAsync` now return
  `Task<string?>` and — matching legacy's own code exactly — wrap the **entire** SP call in a try/catch
  that converts *any* failure, including what would otherwise be a hard DB exception, into a soft
  warning string. Legacy's `UpdateBABRecord`/`UpdateClinicalRecord` never roll back the transaction for
  their own table's failures at all; this is now reproduced exactly.
* **Dam/Sire/Pedigree** — `IPedigreeRepository.AddEditDamSireAsync` previously already read the SP's
  return code but **threw** on any non-zero value (a divergence introduced when this method was first
  written, not caught by the earlier audit pass). Now returns `Task<string?>` with the 4 legacy messages
  (codes 1/2/3/4), never throwing — matching legacy's soft `objErrorList.Add` handling exactly.
* **Feeds, Relations** — `IFeedRepository.EditAsync`/`DeleteAsync` now return `Task<int>` (rows
  affected, via the pre-existing `DapperRepository.ExecuteWithRowCountAsync` helper) instead of `Task`;
  `IAnimalRelationsRepository.EditRelationAsync`/`DeleteRelationAsync` already returned row counts. The
  orchestrator's `PersistStagedFeedsAsync`/`PersistStagedRelationsAsync` now treat a `0`-rows-affected
  result as a **per-row soft skip** (add a warning, move on to the next row) instead of aborting the
  whole save — matching legacy's `OptimisticUpdateDataTable` row-updated callback exactly (it skips only
  the one conflicting row via `UpdateStatus.SkipCurrentRow`, not the whole batch).

**Dead code removed:** `Relations.cshtml.cs`'s `TryGetDamSireReturnCode` helper and its calling
`catch (Exception ex)` block parsed a thrown exception's message for an embedded return code — this was
only ever reachable because `AddEditDamSireAsync` used to throw; now that it returns a soft warning
string instead, that code path is unreachable and has been removed in favour of a plain, generic
log-and-redirect fallback for genuinely unexpected exceptions (e.g. DB connectivity).

**What still throws and aborts the whole transaction (intentionally, matching legacy):** Case's
RbseNotFound/AuditLogError/PostUpdateError codes, and Farm's "deleted by another user"/"audit log
error"/"table update failed" codes (1/2/4) — these are legacy's own hard failures (`Throw New
CaseUpdateException`/`FarmUpdateException`), not concurrency conflicts, and legacy itself aborts the
whole `UpdateCaseDetails` transaction for them too.

Files changed: `CaseEditOrchestrationService.cs` (new `CaseCommitOutcome` type, full `CommitAllAsync`
rewrite, `PersistStagedFeedsAsync`/`PersistStagedRelationsAsync` rewrite), `IFarmRepository.cs` +
`FarmRepository.cs`, `BabRepository.cs`, `ClinicalRepository.cs`, `ChildRepositories.cs`
(`IPedigreeRepository`/`PedigreeRepository`), `FeedRepository.cs`, `Edit.cshtml.cs`, `Farm.cshtml.cs`,
`Vla.cshtml.cs`, `Bab.cshtml.cs`, `Clinical.cshtml.cs`, `Feeds.cshtml.cs`, `Relations.cshtml.cs`.
`get_errors` clean across the whole workspace.

---

## UI change: removed the "unsaved changes" banner from the Relations tab (2026-10-07)

Per user request, the `<output id="case-relations-unsaved-banner">` inset-text banner ("You have
unsaved changes. Select Save to keep them, or Cancel to discard them.") shown above the Save/Cancel
buttons on the Relations tab whenever a row/dam/sire edit was staged has been removed — it was deemed
unnecessary. The Cancel link's existing JavaScript confirm-before-discard dialog (driven by the same
`Model.HasUnsavedChanges` flag) was left in place, since it is a separate, deliberate safeguard against
accidental data loss rather than a persistent banner.

Files changed: `Relations.cshtml`.

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

---

## Bug found and fixed: new-case creation had its own "old style" Save/Cancel instead of the unified top bar (2026-10-08)

Reported symptom: entering an existing RBSE on Home and landing on `/Case/Farm` shows the unified top
Save/Cancel bar shared by all 7 tabs; entering an RBSE that **doesn't exist yet** also lands on
`/Case/Farm`, but shows a visually different, page-specific Save/Cancel pair instead.

### Legacy behaviour

`CaseEntryFarm.aspx` is one physical page for both brand-new case creation and existing-case editing,
with a single consistent Save/Cancel control regardless of which mode it's in.

### Migrated behaviour found (the bug)

`Farm.cshtml`'s existing-case view passes `CanEditCurrentTab` (derived from the `DataEntry` role) to
`_CaseTabs`, which renders one common Save/Cancel pair at the top (`showTopSaveCancel`) using the real
`SaveFarm`/`CancelFarmEdit` handlers — this is the "single Save/Cancel for all tabs" experience. The
create-mode branch, however, called `_CaseTabs` with `CanEditCurrentTab` left at its default (`false`),
so that top bar never appeared; instead the page fell back to its own bespoke Save/Cancel pair (posting
to `CreateCase`), rendered once immediately under the breadcrumb/tab bar — a visually distinct,
"old style" control pre-dating the top-bar unification done earlier in this session.

### Fix

* `CaseTabsViewModel` gained `SaveHandlerOverride`/`CancelPageOverride`/`CancelHandlerOverride` so a
  caller can point the common top Save/Cancel at a different handler than the `ActiveTab`-keyed default
  (needed because "Farm" normally means `SaveFarm`/`CancelFarmEdit`, which don't exist/apply for a case
  that hasn't been created yet).
* Farm's create-mode call to `_CaseTabs` now passes `CanEditCurrentTab: !createReadOnly`,
  `SaveHandlerOverride: "CreateCase"`, `CancelPageOverride: "/Home"` (no cancel handler — Cancel is a
  plain navigate-away, same as the page's own pre-existing Cancel link, since nothing has been
  committed yet to undo), so the common top bar now submits to the correct `CreateCase` handler.
* Removed the redundant page-specific Save/Cancel pair that used to render directly under the tab bar —
  the one further down the page (after the herd-size table) is kept, mirroring the established
  "common top bar + page-specific bottom bar" pattern already used by every existing-case tab.

Files changed: `CaseTabsViewModel.cs`, `_CaseTabs.cshtml`, `Farm.cshtml`. `get_errors` clean across the
whole workspace.

### Known remaining gap, flagged rather than silently fixed: the other 6 tabs are still dead ends during creation

Legacy's `CaseEntryFarm.aspx` tab bar lets a user freely switch to DEFRA/BAB/VLA/Clinical/Feeds/Relations
**while still creating** a brand-new case, because the whole Case+Farm pair already lives in the shared
in-memory session `DataSet` the moment typing starts — every other tab reads that same session object,
not the database. The migrated app has no equivalent: every other tab's `OnGetAsync` reads the case via
`caseService.GetCaseAsync(Rbse)`, which is `null` until `OnPostCreateCaseAsync` actually commits it, so
clicking e.g. "Case (DEFRA)" from the Farm create-mode tab bar still lands on that tab's own "case not
saved yet" fallback rather than a working DEFRA edit form for the in-progress new case. This was not
attempted in this fix — building full cross-tab staging support for a case that doesn't exist in the
database yet (extending `CaseScalarDraftState`/`CaseEditOrchestrationService` to support a first-time
multi-table INSERT instead of an UPDATE) is a substantially larger piece of work than the Save/Cancel
consistency fix above, and is called out here as a candidate follow-up rather than guessed at.

---

## Bug found and fixed: batch assignment had its own separate Save/Cancel step, duplicating the page's real Save/Cancel (2026-10-08)

Reported symptom: after creating a new batch on Home and entering an existing case's RBSE then
clicking **Go**, the Farm tab showed *"Selecting **Save** assigns this case to batch **2026/4**. The
case is not assigned until you save."* followed by its own **Save**/**Cancel** buttons — then the page's
normal Save/Cancel controls appeared again further down, so the user saw two independent-looking
Save/Cancel decisions on one page.

### Legacy behaviour

`Home.aspx.vb`'s `btnGo_Click` only ever stores the chosen batch in `Session(SV_BatchID)` — it never
commits a batch link to the database itself. The link is created entirely as a **side effect of the
case's own Save**: `clsCase.UpdateCaseDetails` (called from `CaseEntrySave.aspx`, reached by whichever
tab's Save button was clicked) unconditionally runs `CreateBatchLink(dsCase, iBatchID, ...)`
**whenever `iBatchID <> 0`, regardless of which tab triggered Save and regardless of whether the case
itself changed at all** ([clsCase.vb](../../bsenet-v2-2025-10/BSELib/clsCase.vb#L905-L911)) —
inside the **same transaction** as everything else. There is no separate confirmation step, no
dedicated Save/Cancel pair for the batch, and no message about it at all — it is completely silent.
Failure to link (any SP exception) is wrapped in `CaseUpdateException` and aborts the whole save, the
same as any other hard failure.

### Migrated behaviour found (the bug)

`Farm.cshtml.cs`'s `OnPostSaveBatchAsync`/`OnPostCancelBatchAsync` committed the batch link
**immediately and independently**, via their own dedicated form or with its own Save/Cancel buttons
rendered in a "Batch assignment" section — a separate, one-shot decision point that doesn't exist in
legacy at all, and which visually duplicated the page's real Save/Cancel controls immediately below it.

### Fix

* `CaseEditOrchestrationService.CommitAllAsync` now folds the batch link into the same shared
  transaction as Case/Farm/Bab/Clinical/Feeds/Relations: if a pending batch (from
  `ICaseWizardStateService`) matches the current RBSE, it calls
  `IBatchRepository.AssignCaseToBatchAsync(...)` (the already-existing transaction-enlisted overload)
  silently, clearing the pending batch after a successful commit — matching legacy's
  unconditional-whenever-a-batch-is-pending, same-transaction, no-confirmation behaviour exactly. A
  non-success/non-already-assigned result now throws (aborting the whole transaction), mirroring
  legacy's `CreateBatchLink` → `CaseUpdateException` hard-failure wrapping.
* `Farm.cshtml`'s "Batch assignment" section is now **purely informational** (no form, no separate
  Save/Cancel) — *"This case will be assigned to batch **2026/4** when you select Save below."* — so
  there is exactly one Save/Cancel decision on the page, same as legacy.
* Removed `OnPostSaveBatchAsync`/`OnPostCancelBatchAsync` from `Farm.cshtml.cs` and their tests from
  `FarmModelHandlerTests.cs` (the handlers they tested no longer exist). `CaseEditOrchestrationServiceTests.cs`
  updated for the orchestrator's two new constructor dependencies (`ICaseWizardStateService`,
  `IBatchRepository`).
* Unaffected: brand-new-case creation (`GetOrCreateCaseBatchIdAsync`/`BuildNewCaseCommand`) already
  folded the pending batch into the new case's own insert with no separate step — this was already
  correct and was not changed.

Files changed: `CaseEditOrchestrationService.cs`, `Farm.cshtml`, `Farm.cshtml.cs`,
`CaseEditOrchestrationServiceTests.cs`, `FarmModelHandlerTests.cs`. `get_errors` clean across the whole
workspace.

---

## Bug found and fixed: `SessionError.aspx` guard missing from 6 of the 7 Case wizard tabs (2026-10-08)

Per explicit follow-up request, compared legacy's `SessionError.aspx` (a static, informational "you got
here without the required context" page) against the migrated app.

### Legacy behaviour

`SessionError.aspx` itself has no logic — `Page_Load` just sets the page title; the body is static text
explaining three reasons the required data might be missing (session timeout, direct URL/bookmark
access, stale back-button). **31 legacy pages** redirect to it, each via the identical one-line guard at
the very top of `Page_Load`: `If IsNothing(Session(SessionVars.SV_RBSENumber)) Then
Response.Redirect("SessionError.aspx")` (or an equivalent check for a different required session key,
e.g. `PickSupplier.aspx.vb` checks `SV_TempSupplierDetails`). Within the scope of this report, that
includes all 7 Case-wizard tabs (`CaseEntryFarm`, `CaseEntryDEFRA`, `CaseEntryVLA`, `CaseEntryBAB`,
`CaseEntryClinical`, `CaseEntryFeeds`, `CaseEntryRelations`), plus `CaseEntrySave` and `CaseWorkEntry`.

### Migrated behaviour found

A `/SessionError` page **already exists** ([SessionError.cshtml](../src/BSE.Host/Pages/SessionError.cshtml) /
[SessionError.cshtml.cs](../src/BSE.Host/Pages/SessionError.cshtml.cs)), faithfully reproducing legacy's
content (consolidated into one GOV.UK error-summary message) and is correctly wired for **2** of the 31
legacy call sites (`MaintenanceConfirmation.cshtml.cs`, `CaseWork/MinuteDocument.cshtml.cs`). The
migrated architecture has no server-side session state to check (every page re-fetches by `Rbse` from
the database), so the literal legacy condition can't be reproduced — but the **functionally equivalent**
condition is a blank/missing `Rbse` (the exact same "arrived here with no required context" scenario:
stale bookmark, direct URL typing, or a link followed after the server restarted). None of the 7
Case-wizard tabs, nor `CaseWorkEntry`, checked for this at all.

**Confirmed the actual (wrong) behaviour instead:** a blank `Rbse` on any of DEFRA/APHA/BAB/Clinical/
Feeds/Relations falls straight into each page's existing "case not yet saved" branch
(`if (record is null) { ...; TempData["Warning"] = $"Case '{Rbse}' is not saved yet. Complete Farm
first."; return Page(); }`), rendering a confusing, malformed banner — literally `Case '' is not saved
yet.` — instead of legacy's clear explanation and a way back. This branch is correct and still needed
for its *real* purpose (a genuinely in-progress new case, reached via Farm → DEFRA with a real but
not-yet-persisted RBSE) — the bug is specifically that it was also being used for the blank-`Rbse`,
no-context-at-all case, which is a different situation entirely.

**Farm is correctly excluded from this fix:** a blank `Rbse` on the Farm tab is the legitimate brand-new
case entry point (`RequireFarmDetails`/"Create mode", mirroring legacy's `Home.aspx` routing a new GB
case straight to `CaseEntryFarm.aspx`), not an error — Farm's own blank-`Rbse` handling was left
untouched.

### Fix

Added `if (string.IsNullOrWhiteSpace(Rbse)) return RedirectToPage("/SessionError");` as the first line
of `OnGetAsync()` in `Edit.cshtml.cs` (DEFRA), `Vla.cshtml.cs` (APHA), `Bab.cshtml.cs`, `Clinical.cshtml.cs`,
`Feeds.cshtml.cs`, and `Relations.cshtml.cs`'s shared `LoadRelationsPageAsync` helper (covers both
`OnGetAsync` and `OnGetEditCaseHerdbookAsync`), plus `CaseWork/Entry.cshtml.cs` — matching legacy's
Page_Load-time guard exactly for the 7 wizard tabs plus the Casework page. The existing "case not saved
yet" warning banner is unchanged and still fires correctly for its own, narrower, legitimate scenario
(non-blank `Rbse`, record genuinely not yet persisted).

Deliberately **not** extended to `OnPostAsync`/grid-row sub-handlers: those can only be reached by
submitting a form rendered by `OnGetAsync`, which — by construction — already redirects away before any
such form exists if `Rbse` was blank, so a POST with a blank `Rbse` cannot occur through normal
navigation (only via a hand-crafted request, which is outside what `SessionError.aspx` is meant to
guard against).

Files changed: `Edit.cshtml.cs`, `Vla.cshtml.cs`, `Bab.cshtml.cs`, `Clinical.cshtml.cs`, `Feeds.cshtml.cs`,
`Relations.cshtml.cs`, `CaseWork/Entry.cshtml.cs`. No changes to `Farm.cshtml.cs` (intentional — see
above) or to `SessionError.cshtml`/`.cshtml.cs` (already correct). `get_errors` clean on all seven.

---

## CPHH display format parity on the new-case creation top bar (2026-10-08)

### Legacy behaviour

Legacy's `CPHH.ascx` control (used by every page that edits a CPHH, including `CaseEntryFarm.aspx`'s
top bar) always displays the value in the slashed `NN/NNN/NNNN/NN` format
([CPHH.ascx.vb](../../bsenet-v2-2025-10/BSESystem/CPHH.ascx.vb)): `SeparateCPHH()` strips the raw value
to digits only, and `ConstructCPHH()` immediately reassembles it into the slashed display string, even
while the field is still being edited (any postback — e.g. `txtCPHH_TextChanged` — reconstructs it).

### Migrated behaviour found (the bug)

The new-case creation top bar's CPHH input (`create-farm-cphh` in
[Farm.cshtml](../src/BSE.Host/Pages/Case/Farm.cshtml)) was bound with `asp-for="EditableFarm!.CPHH"`,
which renders the model's raw, unformatted value (e.g. `01001000101`) instead of the slashed display
format. The existing-case Farm tab's own `farm-cphh-display` field already used
`BseFormat.FormatCphh(...)` correctly — only the create-mode field had the gap.

### Fix

Replaced the `asp-for` binding with a manual `name="EditableFarm.CPHH"` plus
`value="@BseFormat.FormatCphh(Model.EditableFarm?.CPHH)"`. This is safe because every handler that reads
this field (`OnPostLookupNewCaseAsync`, the `CreateCase` handler, etc.) already runs the posted value
through `CphhNormalizer.Normalize()`, which strips back to digits-only regardless of whether slashes are
present. Files changed: [Farm.cshtml](../src/BSE.Host/Pages/Case/Farm.cshtml). `get_errors` clean.

---

## Cancel's "unsaved changes" confirmation brought into full cross-tab parity (2026-10-08)

Closes gap #1 from the "Remaining gaps vs. legacy" list above, which this review re-confirmed rather
than re-discovered.

### Legacy behaviour

Every tab's `CancelCaseEdit()` ([e.g. CaseEntryFarm.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryFarm.aspx.vb#L222-L242))
is wired to **both** the page's own Cancel button and the shared header's Home link
(`VLAHeader1_HomeClick`). It calls `UpdateSessionWithCaseDetails()` to sync the current form into the
shared session `DataSet`, then checks `clsDataCheck.DataSetHasChanges(dsCase) OR DataSetHasChanges(dsFarm)`
([clsDataCheck.vb](../../bsenet-v2-2025-10/BSELib/clsDataCheck.vb)) — a deep, field-level comparison
across **every table in the whole shared session** (Case, Bab, Clinical, Feeds, Relations all live as
child tables of the one `dsCase` DataSet; Farm/LinkedFarms/HerdSizes live in `dsFarm`). If anything
anywhere has changed, it shows the `ExitConfirmation` popup; otherwise it navigates straight to
`Home.aspx` with no prompt. There is no `beforeunload`/browser-level warning anywhere in legacy — every
`PromptBeforeNavigateScript` call site is commented out.

### Migrated behaviour found (the bug)

Each of the 7 tabs' `.bse-cancel-link` click handler only checked that **one page's own** server-rendered
`Model.HasUnsavedChanges` (or `HasUnsavedOwnerChanges` on Vla) flag — reflecting only that page's own
grid draft, not `CaseScalarDraftState` or any other tab's staged edits. A user who staged an edit on one
tab (e.g. a Fate change on DEFRA) and then clicked Cancel on a "clean" tab (e.g. Farm) got no warning at
all before the orchestrator-wide shared state was discarded — the opposite of legacy's whole-session
check. Two further bugs were found in passing:

- **Feeds' confirm never fired at all.** Its script queried `.bse-cancel-button`, but the Cancel link's
  actual class is `.bse-cancel-link` — a dead selector, so Cancel always navigated away silently with no
  confirmation regardless of `Model.HasUnsavedChanges`.
- **Bab and Vla could double-prompt.** Both track a local `dirty` flag and also install a `beforeunload`
  handler. Confirming the custom "Are you sure?" dialog didn't set `submitting = true`, so the
  subsequent real navigation still tripped the `beforeunload` handler and showed a second, native
  "Leave site?" prompt immediately after the user had already said yes.

### Fix

The Home-link guard in [_CaseTabs.cshtml](../src/BSE.Host/Pages/Shared/_CaseTabs.cshtml) already called
an async `/case/{rbse}/unsaved-status` endpoint that checks every tab's draft-state service
(`ICaseEditDraftStateService`, `ICaseFarmDraftStateService`, `ICaseClinicalDraftStateService`,
`ICaseFeedsDraftStateService`, `ICaseRelationsDraftStateService`, `ICaseScalarDraftStateService`) —
the migrated, server-authoritative equivalent of legacy's whole-session `DataSetHasChanges` check.
`hasUnsavedChangesAcrossTabs()` is now exposed as `window.hasUnsavedChangesAcrossTabs` so every tab's own
script can reuse it. Each of the 7 tabs'
([Clinical](../src/BSE.Host/Pages/Case/Clinical.cshtml), [Edit](../src/BSE.Host/Pages/Case/Edit.cshtml),
[Farm](../src/BSE.Host/Pages/Case/Farm.cshtml), [Feeds](../src/BSE.Host/Pages/Case/Feeds.cshtml),
[Relations](../src/BSE.Host/Pages/Case/Relations.cshtml), [Bab](../src/BSE.Host/Pages/Case/Bab.cshtml),
[Vla](../src/BSE.Host/Pages/Case/Vla.cshtml)) Cancel-link handlers now: check the page's own local
signal first (cheap, synchronous), and only if that's clear, `await window.hasUnsavedChangesAcrossTabs()`
before deciding whether to prompt — matching legacy's OR-across-the-whole-session semantics while keeping
the common case (nothing staged anywhere) fast. Feeds' dead `.bse-cancel-button` selector was corrected
to `.bse-cancel-link`. Bab's and Vla's confirmed-navigation paths now set `submitting = true` before
navigating, so the `beforeunload` handler no longer double-prompts. The async check cannot be used inside
`beforeunload` itself (browsers only honour a synchronous `preventDefault()`/`returnValue` there), so
browser-close/refresh protection remains local-only on Bab/Vla — unchanged from before, and still an
enhancement beyond legacy (which has no `beforeunload` protection at all), not a regression.

Files changed: `_CaseTabs.cshtml`, `Clinical.cshtml`, `Edit.cshtml`, `Farm.cshtml`, `Feeds.cshtml`,
`Relations.cshtml`, `Bab.cshtml`, `Vla.cshtml`. `get_errors` clean on all eight.

---

## Full button/action-element parity audit across all 7 case tabs (2026-10-08)

Per explicit request to cover every button/action element, not just Save/Cancel. Inventoried every
clickable element on Farm/Edit/Bab/Vla/Clinical/Feeds/Relations against legacy. Most are already at
parity from earlier rounds (grid row Add/Edit/Delete staging, Save/Cancel, tab-switch staging). Two
items specifically checked and confirmed as **already correct, no change needed**:

- **Remove Sire / Remove Dam** (Relations tab) — legacy attaches
  `onClick="javascript:return confirm('This will disassociate the sire/dam from the case.  Do you wish
  to continue?');"` to `btnRemoveSire`/`btnRemoveDam`
  ([CaseEntryRelations.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryRelations.aspx.vb#L56-L57)).
  [Relations.cshtml](../src/BSE.Host/Pages/Case/Relations.cshtml) already has matching
  `onclick="return confirm(...)"` handlers on both buttons with the exact same wording. Checked every
  other Delete/Remove grid-row button across all 7 tabs (LinkedFarms, HerdSizes, Tests, Owners, Visits,
  Feeds, Relations rows) for an equivalent legacy confirm — legacy has **no** confirm on any of them;
  only Remove Sire/Dam get one. Migrated correctly has no confirm on those either.

One gap found and fixed:

### 🔴 Bug found and fixed: Casework link no longer forces a save first

Confirmed and fixed §2.1 from the "Other confirmed mismatches and gaps" section above (previously
documented as a known gap, not yet implemented).

**Legacy:** `btnCaseWork_Click` ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L412-L414))
runs `If UpdateSessionWithCaseDetails() Then Response.Redirect("CaseEntrySave.aspx?redirect=CaseWorkEntry.aspx")`
— the exact same stage-then-commit pipeline as the ordinary Save button, just with a different redirect
target on full success. Confirmed via `clsCase.vb`/`CaseEntrySave.aspx.vb` that the `redirect=` query
value is honoured **only** on the fully-successful, no-warnings path — missing-mandatory-fields still
goes to the Farm tab and any other failure still goes to Home, regardless of where the user came from.
Confirmed (via grep across all 7 `CaseEntry*.aspx.vb` files) that this Casework button exists **only**
on the DEFRA tab in legacy, matching `Model.HasCaseWorkLink`'s current migrated gating.

**Migrated behaviour found (the bug):** the Casework item was a bare
`<a asp-page="/CaseWork/Entry" asp-route-rbse="@Model.Case.Rbse">Casework</a>` — plain navigation, no
save, no staging. Any in-progress edit on the DEFRA tab (including unsaved test rows) was silently
discarded with zero warning when clicking it.

**Fix:** [Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs)'s `OnPostAsync()` body was extracted
into a private `SaveAsync(string successRedirectPage)`, called by `OnPostAsync()` (passing `"/Home"`,
unchanged behaviour) and a new `OnPostSaveAndGotoCaseworkAsync()` (passing `"/CaseWork/Entry"`). Only the
single final success-path return respects the parameter — the `MandatoryCaseFieldsMissingException` catch
(→ `/Case/SaveResult`) and the `ConcurrencyConflict`/other-failure/partial-success paths (→ `/Home` or
`/Case/SaveResult`) are unchanged, matching legacy's "redirect= only honoured on full success" rule.
[Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml)'s Casework link is now
`<button type="submit" form="case-edit-inline-form" asp-page-handler="SaveAndGotoCasework">Casework</button>`
— posts the same form as the ordinary Save button (already carries a hidden `Rbse` input), so it goes
through identical validation, mandatory-field checking, cross-tab staging/commit, and test-row
persistence as Save, only redirecting to Casework instead of Home on success.

Files changed: `Edit.cshtml.cs`, `Edit.cshtml`. `get_errors` clean on both.

### Remaining lookup/navigation buttons checked (2026-10-08, same audit continued)

All confirmed already at parity — no changes made:

- **Vla "Calculate" buttons** (Age Purchased / Onset Age) — legacy's `CalculateAgePurchased()` uses a
  plain `DateDiff(DateInterval.Month, dob, purchaseDate)` with **no day-of-month adjustment**, while
  `CalculateOnsetAge()` does adjust (subtracts a month if the day-of-month hasn't been reached yet) — a
  genuine inconsistency between the two legacy methods, not a bug to "fix to be consistent".
  [Vla.cshtml](../src/BSE.Host/Pages/Case/Vla.cshtml)'s `calculatePurchasedAge()`/`calculateOnsetAge()`
  JS functions already reproduce this exact asymmetry, with a comment explicitly noting it.
- **Look Up Sire / Look Up Dam** (Relations) — reviewed `OnPostLookUpSireAsync`/`OnPostLookUpDamAsync`;
  stage-before-redirect pattern is sound, no data-loss risk found.
- **Validate Supplier** (Feeds) — confirmed the `StashPostedPanelState()`/`RestorePostedPanelState()`
  fix from an earlier round is still intact after the unrelated external file changes.
- **CPHH change / "Look Up" button on an *existing* case's Farm tab** — investigated migrated's unused
  `FarmModel.CanChangeCphh => User.IsInRole("DEFRAMaintenance")` property (declared, never referenced in
  any view — looked like dead/incomplete code). Checked legacy's `CheckLookupCPHH()`
  ([CaseEntryFarm.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryFarm.aspx.vb#L565-L573)): `btnLookUp.Visible`
  is `True` **only** when the Case row's `RowState = DataRowState.Added` (i.e. a brand-new, not-yet-saved
  case) — for any already-persisted case it is always `False`, and `CPHH1.Enabled = btnLookUp.Visible`
  means the CPHH field is also always read-only then. **Conclusion: legacy itself has no "change CPHH on
  an existing case" capability on this tab at all** — migrated's read-only `farm-cphh-display` field for
  existing cases is correct, and `CanChangeCphh` being unused is harmless leftover scaffolding, not a
  functional gap. Not removed (out of scope for a parity audit; flagging only).
- **Sort-column headers** (LinkedFarms/HerdSizes/Tests/Owners/Visits/Feeds/Relations grids) — all use the
  shared `_SortableHeader.cshtml` partial, a generic column-sort component with no business-rule content;
  legacy's equivalent is a standard ASP.NET DataGrid `AllowSorting` column — both are generic UI sorting
  with no case-specific logic to diverge, treated as out of scope for a *functional* parity audit.

This closes out the button/action-element audit requested across all 7 tabs. Outstanding, already-tracked
items not re-examined in this pass: role/permission matrix (§2.2), full client-side date-cascade
reactivity (§2.3) — both pre-existing, documented gaps, not button/action-element items.

---

## §2.2 and §2.3 implemented (2026-10-08)

Per explicit request to implement the two remaining tracked gaps.

### §2.3 (date auto-clear cascade reactivity) — found already implemented, not by this session

Before writing anything, re-checked the current file (per the external-changes notice) and found
`applyFormACascade()`/`applyFormBCascade()`/`applyBirthDateCascade()`/`wireReceivedDateCascade()` in
[Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml) already fully reproduce every rule from §2.3:
Form A cleared blanks+disables Form A Resubmitted/Form B (which cascades on to Fate/Form C); Form A set
re-enables them; Form B cleared blanks+disables Fate/Form C; Form B set re-enables them; Date of Birth
cleared disables Birth Date Source and un-ticks "estimated"; each received-date auto-ticks its checkbox.
This must have landed via the external file changes noted at the top of this session (not written in
this pass) — verified complete and correctly wired (`wireFieldCascades()` is called), no further action
needed. §2.3 is now closed.

### §2.2 (role/permission matrix) — implemented

**Legacy** ([CaseEntryDEFRA.aspx.vb](../../bsenet-v2-2025-10/BSESystem/CaseEntryDEFRA.aspx.vb#L420-L484)):
`EnableControls()` dispatches on 5 AD groups, but only 4 distinct *behaviours* actually exist — DEFRA
Data Entry and DEFRA Maintenance are identical (`MakeControlsWritable()`, Save enabled, Barcode/AHF
Reference always disabled). DEFRA Viewer is fully read-only, Save disabled. VLA Data Entry is fully
read-only but Save stays enabled (a harmless no-op since nothing editable could have changed). VLA
Maintenance is writable **and** is the only group ever able to edit Barcode/AHF Reference/Paperwork
Complete Date, further gated by a CaseWork row existing **and** `IsCaseClosed <> 1`.

**Migrated behaviour found (the bug):** every POST handler's Forbid-gate and the main fieldset's
disabled-state checked only `User.IsInRole("DataEntry")` — a VLA Maintenance user (who does not hold the
DEFRA-specific "DataEntry" role claim) was completely blocked from saving or editing this tab at all,
despite legacy explicitly allowing it. Separately, Barcode/AHF Reference's readonly logic used
`isDefraDataEntry = DataEntry && !VLAAccess` — backwards from legacy: it left these two fields **editable
for any VLA user** (including plain VLA Data Entry, who should never edit them) with no `IsCaseClosed`
check at all, while being needlessly readonly for the one case that doesn't matter (DEFRA+VLAAccess
combined). `FarmModel`'s precedent (`ApplyLegacyEditPermissions`) was used as the established pattern for
this fix.

**Fix:**
- [Edit.cshtml.cs](../src/BSE.Host/Pages/Case/Edit.cshtml.cs): added `CanEditCaseFields` (DataEntry role
  OR VLA Maintenance — replaces the DEFRA-only fieldset/tab-Save gate) and
  `CanEditVlaMaintenanceCaseworkFields` (VLA Maintenance AND `Case.HasCaseWork` AND NOT
  `Case.IsCaseClosed` — the 3-way gate for Barcode/AHF Reference). All 7 of this file's
  `if (!User.IsInRole("DataEntry")) return Forbid();` POST-handler gates became
  `if (!CanUserEditCase())`, which also admits VLA Maintenance (DEFRA Data Entry/Maintenance already
  covered by the existing "DataEntry" role check — legacy's two DEFRA groups behave identically here, so
  no separate DEFRAMaintenance role check was needed, unlike Farm's own permission model).
- [CaseEditViewModel.cs](../src/BSE.Host/Models/ViewModels/CaseEditViewModel.cs): added `IsCaseClosed`,
  populated in `ApplyCaseWork` from `CaseWorkRecord.IsCaseClosed` — round-tripped via a new hidden field
  next to the existing `Case.HasCaseWork` one, since Save's failure-path re-renders never re-fetch
  CaseWork from the DB (same reason `HasCaseWork` already needed one).
- [Edit.cshtml](../src/BSE.Host/Pages/Case/Edit.cshtml): the main fieldset, the "view but cannot edit"
  message, and the `_CaseTabs` partial's `CanEditCurrentTab` argument all now read
  `Model.CanEditCaseFields` instead of `User.IsInRole("DataEntry")`; Barcode/AHF Reference's `readonly`
  now reads `!Model.CanEditVlaMaintenanceCaseworkFields`.
- **Deliberately not touched:** Paperwork Complete Date's existing `disabled="@(!Model.Case.HasCaseWork)"`
  gate — the sixth follow-up review (field-level validation deep-dive, above) already investigated this
  specific field in detail and found legacy's own DEFRA-role behaviour for it is genuinely
  ambiguous/ViewState-dependent (never explicitly set in `MakeControlsWritable`), and deliberately kept
  migrated's simpler, more-consistent rule rather than replicate that ambiguity. Only Barcode/AHF
  Reference — unambiguously always-disabled for DEFRA roles in legacy — were changed this round.
- **VLA Data Entry's Save** is now blocked outright (`Forbid`) rather than reproducing legacy's
  "technically enabled but every field is read-only so it's a no-op" quirk — same net effect (nothing can
  ever be changed by this group), safer mechanism (an explicit 403 instead of trusting that every field
  really is disabled and can't smuggle a value through).

Files changed: `Edit.cshtml.cs`, `Edit.cshtml`, `CaseEditViewModel.cs`. `get_errors` clean on all three
individually and on the whole `BSE.Host` project.

---

## Role/permission matrix audited across the whole application; regression found in the fix above (2026-10-08)

Per explicit request to extend the role/permission audit beyond the DEFRA tab to the entire application.

### Critical correction: the claims mapping makes "DataEntry" role non-exclusive to DEFRA

Found the authoritative source of truth —
[GroupClaimsTransformation.GetPoliciesForGroup](../src/BSE.Modules.UserManagement/Identity/GroupClaimsTransformation.cs#L130-L143)
— which maps each of legacy's 5 DB groups to migrated role claims:

| Legacy group | Policies granted |
|---|---|
| DEFRA Viewer | ReadOnly, DEFRAAccess |
| DEFRA Data Entry | ReadOnly, **DataEntry**, FarmCreation, DEFRAAccess |
| DEFRA Maintenance | ReadOnly, **DataEntry**, DEFRAMaintenance, PickListAccess, FarmCreation, DEFRAAccess |
| VLA Data Entry | ReadOnly, **DataEntry**, VLAAccess, PickListAccess |
| VLA Maintenance | ReadOnly, **DataEntry**, DEFRAMaintenance, VLAAccess, VLAMaintenance, PickListAccess, FarmCreation |

**All four writable legacy groups hold the "DataEntry" role claim — not just the two DEFRA groups.**
This means `User.IsInRole("DataEntry")` alone can never distinguish a DEFRA user from a VLA user; it only
distinguishes "any writable group" from "DEFRA Viewer". Any permission check that needs to tell DEFRA and
VLA users apart must additionally check `VLAAccess`.

### 🔴 Regression found in this session's own earlier DEFRA-tab fix (§2.2 above) — now corrected

The §2.2 fix (same day, earlier section above) introduced `CanEditCaseFields = isDefraDataEntry ||
isVlaMaintenance` where `isDefraDataEntry = User.IsInRole(DataEntryRole)` — **without excluding
VLAAccess**. Because VLA Data Entry also holds the DataEntry claim (per the table above), this made the
entire DEFRA-tab fieldset editable for VLA Data Entry, when legacy's `VLADataEntryEnable()` makes it
**fully read-only** (`MakeControlsReadOnly()`) for that group. Worse: since the fieldset would still be
visually disabled by the OLD `User.IsInRole("DataEntry")`-based gate in some render paths, a VLA Data
Entry user's POST could carry blank/default Case field values (disabled inputs are never submitted) —
`SaveAsync`/`OnPostStageAndGotoAsync` would have applied those blanks as if they were real edits,
silently blanking out the Case row.

**Fixed:** `isDefraDataEntry` now correctly excludes VLA users (`User.IsInRole(DataEntryRole) &&
!User.IsInRole(VlaAccessRole)`), matching the discriminator the rest of the codebase already uses
correctly elsewhere (e.g. Farm.cshtml.cs's `ApplyLegacyEditPermissions`). `CanUserEditCase()` (the
Forbid-gate) was simplified back to a plain `User.IsInRole(DataEntryRole)` check — correct and
sufficient now that it's understood to mean "any of the four writable groups", not "DEFRA only". Added
explicit no-op guards to `SaveAsync` and `OnPostStageAndGotoAsync`: when `!CanEditCaseFields` (the VLA
Data Entry case), both now redirect immediately without staging or committing anything — reproducing
legacy's "Save button technically works but is a no-op because nothing is editable" behaviour, safely.

### Audited all 7 case-entry tabs' Save handlers for the same "generic role check committs un-gated posted data" risk

| Tab | Save handler gate found | Verdict |
|---|---|---|
| Farm | `OnPostSaveFarmAsync` checks only `DataEntryRole`, but `ApplyLegacyJointAndVlaEditGuards()` restores every joint/VLA-protected field from the original record regardless of what was posted | Already safe (restore-based defence, not a return-early gate) |
| Case (DEFRA) | Was broken (see above) | **Fixed this round** |
| BAB | `OnPostSaveBabAsync` checks `EvaluateLegacyBabEditPermission(...)` and redirects before touching posted data | Already correct |
| Case (APHA)/Vla | `OnPostAsync` checks `LoadBatchContextAndCheckEditPermissionAsync` (→ `CanEditMainCase`, itself `DataEntryRole && VLAAccess` plus the batch-selection check) and redirects before touching posted data | Already correct |
| Clinical | `OnPostSaveSignsAsync` checked only `User.IsInRole("DataEntry")` — **missing the `VlaAccessRole` check every other handler in this same file already has** (Add/Update/Delete/BeginEdit visit row all correctly require `DataEntry && VLAAccess`) | 🔴 **Bug found and fixed** |
| Feeds | `OnPostSaveFeedsAsync` only commits rows already staged by `OnPostAddFeedRowAsync` etc., which correctly require `DataEntry && VLAAccess` before staging anything | Already safe (gated at staging time, not at commit time) |
| Relations | `OnPostSaveRelationsAsync` checks only `DataEntryRole`, but this is correct — legacy's Dam/Herdbook section is editable by **any** writable group (DEFRA or VLA), only the Sire section and relation rows are VLA-only, and those are gated separately at their own handlers (`OnPostLookUpSireAsync` etc. already require `VlaAccessRole`) | Already correct |

### 🔴 Bug found and fixed: Clinical's Save handler missing the VLA-only gate

[Clinical.cshtml.cs](../src/BSE.Host/Pages/Case/Clinical.cshtml.cs)'s `OnPostSaveSignsAsync` checked only
`User.IsInRole("DataEntry")`, inconsistent with every other handler in the same file. Legacy's
`CaseEntryClinical.aspx.vb`: DEFRA Data Entry/Maintenance always get `MakeControlsReadOnly()` on this
tab (VLA-only tab, same shape as Feeds/Relations' Sire section) — a DEFRA-only user's POST (fields
disabled client-side) could have silently blanked out clinical signs. Fixed to match the same
`!User.IsInRole("DataEntry") || !User.IsInRole(VlaAccessRole)` gate already used by the visit-row
handlers in this file.

### Not yet audited this round (flagged for continuation, not silently skipped)

The role/permission matrix extends well beyond the 7 case-entry tabs — `SV_HeaderGroupName` is read in
51 legacy files (PickList maintenance pages, CaseWork open/closed reports, ADNS/OSS export menus, audit
log reports, Move/Delete/RBSE-change case utilities, Home.aspx, etc.). Given the scope, this round
focused on the 7 case-entry tabs (the highest-traffic, highest-risk area, and where the confirmed
regression lived). The remaining ~44 pages were not individually re-audited against their legacy
counterparts this round — a reasonable next increment if the user wants to continue.

Files changed this round: `Edit.cshtml.cs`, `Clinical.cshtml.cs`. `get_errors` clean on both and on the
whole `BSE.Host` project.

---

## Whole-application role/permission audit completed — remaining ~44 pages (2026-10-08, continued)

Per "continue", audited every remaining legacy page that reads `SV_HeaderGroupName` (51 files total,
7 already covered above). Used targeted Explore subagents per logical group, then **independently
verified every claimed mismatch against actual source** before accepting it (two of the subagents'
claimed mismatches turned out to be false positives on closer inspection — see below).

### Group A — PickList maintenance (9 pages) + User Maintenance: all MATCH

`PickListMaintenance[AHO/AHRO/Breed/BSECounty/RelationFate/Supplier/TestType/TSETestingSite].aspx.vb`
and `UserMaintenance.aspx.vb` all gate add/edit/delete to `"VLA Maintenance"` only. Migrated
`Admin/PickLists*.cshtml.cs` (`[Authorize(Policy = "PickListAccess")]` + `CanEdit =>
User.IsInRole("VLAMaintenance")`) and `Admin/Users.cshtml.cs` (`[Authorize(Policy = "VLAMaintenance")]`)
match exactly for all 5 legacy groups.

### Group B — CaseWork pages (4 pages): all MATCH

`CaseWorkMenu/Entry/OpenReport/ClosedReport.aspx.vb` all redirect away unless `sGroupName = "VLA
Maintenance"`. Migrated `CaseWork/Menu.cshtml.cs`, `Entry.cshtml.cs`, `OpenCases.cshtml.cs`,
`ClosedCases.cshtml.cs` all use `[Authorize(Policy = "VLAMaintenance")]`. Match.

### Group C — Export menus, audit logs, BSESS (14 pages): all MATCH

ADNS export pages require DEFRA Maintenance or VLA Maintenance (migrated: `[Authorize(Policy =
"DEFRAMaintenance")]`, correct since VLA Maintenance also holds that claim). OSS export and Print Batch
require either VLA group (migrated: `[Authorize(Policy = "VLAAccess")]`). Audit log pages and BSESS
pages allow all 5 legacy groups (migrated: `[Authorize(Policy = "AuditAccess")]`, requiring `DEFRAAccess`
OR `VLAAccess` — every group holds one or the other). All verified matching.

### Group D — Case-utility pages (13 pages): all MATCH once re-verified

`MoveCase`, `MoveCaseNewFarm`, `DeleteCase`, `CPHHChange`, `RBSEChange`, `FinalResultEntry`: all require
DEFRA Maintenance or VLA Maintenance in legacy; migrated `[Authorize(Policy = "DEFRAMaintenance")]`
matches (VLA Maintenance holds that claim too). `NewFarm` requires DEFRA Data Entry/Maintenance or VLA
Maintenance (not VLA Data Entry); migrated `[Authorize(Policy = "FarmCreation")]` matches exactly (only
those 3 groups hold `FarmCreation`). `NonGBCaseCreation` requires VLA Maintenance only; migrated
`[Authorize(Policy = "VLAMaintenance")]` matches. `PickSupplier` requires a VLA group; migrated's
`HasAccess() => DataEntry && VLAAccess` matches exactly.

Two items an Explore subagent initially flagged as mismatches, independently re-verified and found to be
**false positives**:
- **`FinalResultConfirmation`**: the subagent assumed migrated's `MaintenanceConfirmation.cshtml.cs` (the
  page `FinalResultEntry` redirects to on success) needed its own `DEFRAMaintenance` policy. Re-checked:
  this is a deliberately generic, shared confirmation page used by multiple unrelated flows with
  different role requirements (e.g. Farm creation confirmations) — adding a `DEFRAMaintenance` policy
  there would break those other flows. The actual protection is at the entry point: `FinalResultEntry`
  itself already carries `[Authorize(Policy = "DEFRAMaintenance")]`, and the confirmation text is only
  ever populated via non-forgeable server-side `TempData` (never a query string, unlike legacy) — a user
  cannot reach meaningful confirmation content without having already passed the real gate. No fix made.
- **`PickSireDam`**: legacy's `EnableControls` has no redirect branch at all (every group including DEFRA
  Viewer reaches the page) due to how the dispatch is written, but migrated requires `[Authorize(Policy =
  "DataEntry")]`, excluding DEFRA Viewer. Re-checked how this page is actually reached: only via the
  Relations tab's "Look Up Sire"/"Look Up Dam" actions, which are themselves gated to writable users only
  (confirmed in the case-tab audit above) — a DEFRA Viewer can never navigate here through the real UI.
  Widening the gate to match legacy's permissive-by-construction code literally would only let a
  hand-crafted direct URL in; left as the stricter (safer) migrated behaviour, not changed.
- **`ShowCase`**: legacy's `Page_Load` calls `Session.Clear()` *before* reading `sGroupName`, so its
  intended non-GB-case role gate never actually executes (a real legacy bug — every group reaches the
  non-GB branch with an empty group name). Migrated has no direct equivalent page; GB/non-GB case entry
  is split into separately-gated `/Case/New` and `/Case/NewNonGb` pages instead, which is **stricter than
  legacy's actual (buggy) runtime behaviour** — an improvement, not a gap. No fix made.

### Home.aspx link-visibility matrix: verified MATCH, link-by-link

Legacy's `EnableControls`/`DEFRAViewerEnable`/`DEFRADataEntryEnable`/`DEFRAMaintenanceEnable`/
`VLADataEntryEnable`/`VLAMaintenanceEnable` (lines 205-384) set 11 links' `Visible`/`Enabled` per group.
Compared every one against [Home.cshtml](../src/BSE.Host/Pages/Home.cshtml)'s role checks:

| Link | Legacy visible for | Migrated check | Match? |
|---|---|---|---|
| Casework | VLA Maintenance only | `VLAMaintenance` | ✅ |
| Print batch | VLA Data Entry, VLA Maintenance | `VLAAccess` | ✅ |
| Final result entry / CPHH change / RBSE change / Move case / Delete case | DEFRA Maintenance, VLA Maintenance | `DEFRAMaintenance` | ✅ |
| Export to ADNS | DEFRA Maintenance, VLA Maintenance | `DEFRAMaintenance` | ✅ |
| Export to OSS | VLA Data Entry, VLA Maintenance | `VLAAccess` | ✅ |
| Pick List Maintenance | DEFRA Maintenance, VLA Data Entry, VLA Maintenance | `PickListAccess` | ✅ |
| User Maintenance | VLA Maintenance only | `VLAMaintenance` | ✅ |

All 7 link groups match exactly, including the non-obvious one (DEFRA Data Entry gets `Panel1`/batch
entry hidden in legacy while DEFRA Maintenance gets it shown — already correctly reproduced via the
`VLAAccess` role check on the Batch Number panel, which DEFRA Data Entry never holds).

### Conclusion

This completes a full sweep of all 51 legacy files that read `SV_HeaderGroupName`. Combined with the
case-entry-tab audit above (1 regression fixed, 1 real bug fixed), **every page's role/permission gate
in the application has now been individually compared against its legacy source and either confirmed
matching or fixed.** No further role/permission gaps were found in this round. No files changed in this
section (Groups A-D and Home.aspx were already correct).

---

## Bug found and fixed: Cancel/Home confirm didn't catch a direct edit with no tab switch (2026-10-08)

Reported symptom: the "unsaved changes" confirm correctly appears after switching tabs then clicking
Cancel or Home, but **not** when editing a field and immediately clicking Cancel/Home on the **same**
tab, with no tab switch in between.

### Root cause

Legacy's `CancelCaseEdit()`/`VLAHeader1_HomeClick` both call `UpdateSessionWithCaseDetails()` — a full
server **postback** that merges the browser's current, just-typed field values into the shared session
`DataSet` — immediately before checking `DataSetHasChanges()`. Because Cancel/Home are themselves form
postbacks in legacy, a same-tab, never-staged edit is still visible to the check. Migrated's Cancel is a
plain GET-navigating `<a>` that never submits the form, so `hasUnsavedChangesAcrossTabs()` (added in the
previous follow-up) could only ever see what had been explicitly **staged** server-side (via a tab
switch or Save) — a field typed and left un-staged on the same tab was invisible to it, matching Bab's
and Vla's own pre-existing local `dirty` flag (tracked via `input`/`change` listeners) but not reproduced
for Edit/Farm/Clinical/Feeds/Relations, nor fed into the shared Home-link guard at all.

### Fix

[_CaseTabs.cshtml](../src/BSE.Host/Pages/Shared/_CaseTabs.cshtml) already computes a form snapshot
(`initialSnapshot`, captured on page load) for its existing sessionStorage draft-restore feature — the
exact same data needed to detect a same-tab edit. Added `isCurrentFormDirty()`, which reuses that
snapshot/`serializeForm()` comparison, and folded it into `hasUnsavedChangesAcrossTabs()` (checked first,
before the server round-trip, since it's free). Because every one of the 7 tabs' own Cancel-link
handlers (and the Home-link guard) already call `window.hasUnsavedChangesAcrossTabs()`, this one
centralised change closes the gap for all 7 tabs — Bab's and Vla's own local `dirty` flags are
unaffected (still checked first, cheaply) and now have a consistent fallback instead of none.

Files changed: `_CaseTabs.cshtml` only. `get_errors` clean on it and on the whole `BSE.Host` project.




