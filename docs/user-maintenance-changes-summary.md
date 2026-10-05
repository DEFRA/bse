# User Maintenance Page — Change Summary

Page: `/Admin/Users` (User Maintenance)

Files touched:

- [src/BSE.Host/Pages/Admin/Users.cshtml](src/BSE.Host/Pages/Admin/Users.cshtml)
- [src/BSE.Host/Pages/Admin/Users.cshtml.cs](src/BSE.Host/Pages/Admin/Users.cshtml.cs)
- [src/BSE.Host/wwwroot/css/bse.css](src/BSE.Host/wwwroot/css/bse.css)
- [src/BSE.Host/wwwroot/js/user-maintenance.js](src/BSE.Host/wwwroot/js/user-maintenance.js) (new)

---

## 1. Inline edit grid (replacing the separate Add/Edit page flow)

The page was reworked from a grid plus a separate "Add user" page into a legacy-style
inline-edit grid:

- Each row has a left-hand select control (`▶`) that **highlights** the row only — it does
  not start editing.
- **New** and **Edit** buttons sit below the grid and are **disabled until a row is selected**.
- **Edit** turns the selected row into an inline editable row (name, email, user group,
  IsActive).
- **New** appends a blank editable row at the **end** of the grid.
- While editing, the New/Edit buttons are replaced by **Save** and **Cancel**.

Code-behind (`UsersModel`) gained `SelectedUserId`, `IsAddingNew`, `EditUserId`
(all `[BindProperty(SupportsGet = true)]`), helpers `IsEditing(int)` / `IsEditingOrAdding`,
and handlers `OnPostStartNew`, `OnPostStartEdit`, `OnPostCancel`, plus `OnPostEditAsync`
which branches on `EditUserId == 0` to add vs update (deriving `NTLogin` on add).

## 2. Bug fixes applied during review

| # | Issue | Fix |
|---|-------|-----|
| 1 | IsActive checkboxes were invisible | GOV.UK checkbox components draw the visible box via a sibling `<label class="govuk-checkboxes__label">`. The markup had no label. Added paired `id`/`for` label with visually-hidden text for both editable and read-only states. |
| 2 | Clicking New/Edit did nothing | `SelectedUserId` only existed in the query string, so it was never posted. Added `<input type="hidden" asp-for="SelectedUserId" />` inside the form. |
| 3 | "Active" column header/alignment | Renamed to **IsActive** and left-aligned (removed `govuk-table__cell--numeric` and centred CSS) across the new-row, editing-row and display-row renderings. |
| 4 | New record row appeared at the top | Moved the add-new `<tr>` block to render **after** the `@foreach` loop so it appears at the end of the grid. |
| 5 | Select arrow disappeared on the row being edited | Removed the `!isEditing` condition that hid the link. |
| 6 | Select arrows disappeared on all rows while adding a new record | Removed the `!Model.IsAddingNew` condition as well — the arrow is now always rendered. |
| 7 | Column headers only underlined on hover | Added scoped CSS: `.bse-user-maintenance-table .bse-sortable-table__button { text-decoration: underline; }` |
| 8 | Validation errors never appeared | `ModelState.AddModelError` was called server-side but the view had no error markup. Added a GOV.UK error summary (matching the `UsersAdd` page pattern) plus field-level `govuk-input--error` / `govuk-select--error` classes and inline messages, with element `id`s matching the summary anchor links. |

## 3. Layout / styling

Scoped rules added under `.bse-user-maintenance-table` in `bse.css`:

- Fixed `table-layout` with explicit column widths (22% / 32% / 22% / 6rem) so the IsActive
  column is never pushed off-screen.
- Normalised widths for inline `<input>` and `<select>` controls.
- Row state classes: `.bse-row-editing` (blue highlight) and `.bse-row-selected` (grey highlight).

## 4. No full page reload for select / new / edit / cancel

Previously every interaction (selecting a row, clicking New, Edit or Cancel) was a server
round-trip that reloaded the whole page. These are now handled entirely in the browser by
`wwwroot/js/user-maintenance.js`, loaded via `@section Scripts`.

Supporting markup changes in `Users.cshtml`:

- Each display row carries `data-user-id`, `data-user-name`, `data-email`, `data-nt-login`,
  `data-upn`, `data-group-id` and `data-is-active`.
- Action buttons carry `data-action="select" | "start-new" | "start-edit" | "save" | "cancel"`.
- The user group list is emitted as a JSON data island
  (`<script type="application/json" id="bse-user-groups-data">`) so the client can build the
  `<select>` options.
- Both button groups (Save/Cancel and New/Edit) are now always rendered, with the `hidden`
  attribute toggled, so the script can switch between them without rebuilding the DOM.

Client-side behaviour:

- **Select** — highlights the row, enables New/Edit, syncs the hidden `SelectedUserId` field.
- **Edit** — swaps the selected row's cells for inputs populated from its `data-*` attributes,
  and injects hidden `EditUserId` / `EditNTLogin` / `EditUpn` fields.
- **New** — appends a blank editable row and focuses the name field.
- **Cancel** — removes the new row, or restores the original row HTML for an existing row.
- **Save** — deliberately *not* intercepted; it still performs a real form POST to
  `OnPostEditAsync`, since persisting a change requires a server round-trip.

The existing server-side handlers (`OnPostStartNew`, `OnPostStartEdit`, `OnPostCancel` and the
query-string-driven rendering in `OnGetAsync`) were left intact, so the page remains fully
functional as a **no-JavaScript fallback** — the script is progressive enhancement only.

### Checkbox model-binding note

The hand-built checkbox in the JavaScript edit template initially submitted the browser default
value `on`, which failed to bind to the `bool EditIsActive` property and produced a
`FormatException` (500) on save. The template now mirrors what the ASP.NET `asp-for` tag helper
emits: a checkbox with `value="true"` and a paired
`<input type="hidden" name="EditIsActive" value="false" />`.

The hidden input must sit **after** the label, not between the checkbox and the label — GOV.UK
draws the tick via the adjacent-sibling selector `input:checked + label::after`, so breaking that
pairing left the box permanently blank even though the value was toggling correctly.

## 5. Button rules and styling

All four buttons sit on a single line, and the enabled states are:

| State | New | Edit | Save | Cancel |
|-------|-----|------|------|--------|
| No record selected | enabled | disabled | disabled | disabled |
| Record selected | enabled | enabled | disabled | disabled |
| Editing / adding | disabled | disabled | enabled | enabled |

The rules are applied both server-side (the `disabled` attribute, so the no-JS fallback behaves
identically) and client-side in `user-maintenance.js`.

- New and Edit both use the primary green style when enabled.
- GOV.UK only fades disabled buttons to 50% opacity, which left a disabled Save still looking
  green. Disabled buttons in this group are now rendered flat grey with a `not-allowed` cursor.
- The Save tick and Cancel cross are inline SVGs rather than Unicode characters: the glyphs
  `U+2713`/`U+2717` resolve to different fallback fonts, so their stroke weights never matched.
  Both icons now share `stroke-width` and `stroke: currentColor`, so they also grey out correctly.

## 6. Clickable-area fixes

Two layout defects made controls impossible to click:

- The select-arrow column collapsed to 0px, because `width: 2.5rem` was set only on the `<td>`;
  with `table-layout: fixed` the header row defines the columns, so the arrow overflowed on top
  of the Name cell, which then intercepted the clicks. The width is now set on `th:first-child`
  as well.
- `.govuk-select` carries `min-width: 11.5em`, which overrode `width: 100%` and made the User
  Group dropdown overflow its cell and cover the IsActive checkbox. Scoped `min-width: 0` fixes it.
  GOV.UK's small-checkbox variant also pulls the 44px hit area 10px left, which is reset here so
  it stays inside its own cell.

## 7. Adding records at the end of the last page

A new record always belongs at the end of the last page:

- `OnPostStartNew` resolves the last page and redirects there in add mode.
- After a successful add, the save redirect lands on the page the new record falls on, so it
  follows the record when it spills onto a new page.
- Client-side, New only adds the blank row inline when you are already on the last page and it
  still has room; otherwise it defers to the server handler to navigate there first.

## 8. Pagination

The pagination rework started here but is applied consistently across the whole application, so
`_Pagination.cshtml` carries the new behaviour as its default. Both switches remain overridable
per-screen if a page needs the previous style back.

- `SlidingWindow` (default on) — the numeric links follow the current page (up to 3 pages ahead,
  the window filled backwards to 10 links) instead of jumping a block at a time. Page 10 of 20
  shows 4..13.
- `ArrowsOnly` (default on) — Previous/Next render as `‹` / `›` so all four controls are symbols
  (`«  ‹  ›  »`) rather than mixing symbols with text labels. Visually-hidden labels are retained.
- The count and the controls now share one line: `Page X of Y` is right-aligned, the arrows and
  page numbers left-aligned. CSS `order` is used rather than reordering the markup, so the count
  is still announced before the links.
- Tooltips were added to every control (page numbers and arrows).

Three screens had their own hand-rolled pagination and were converted to the shared partial, so
they pick all of this up and no longer drift: `AuditLog/ByCase`, `Case/Clinical` (clinical visits)
and `Case/Vla` (previous owners). Their now-unused `PageUrl` / `VisitsPageUrl` / `OwnersPageUrl`
helpers were removed — the partial derives URLs from the query string, preserving each page's
other state (`rbse`, sort columns, `returnTo`) automatically.

## 9. Verification

Verified against the running application (`http://localhost:5080/Admin/Users`):

- Initial load — only New enabled; Edit/Save/Cancel greyed out.
- Selecting a row — no navigation, row highlighted, New and Edit enabled.
- Edit — no navigation, inline inputs populated with the row's current values.
- Cancel — no navigation, original row restored.
- New — blank row at the end of the last page with the name field focused.
- Save — real POST, record persisted and visible after the redirect.
- IsActive — toggles both ways in Add and Edit rows, and the value persists through Save.
- Validation — an invalid or duplicate email shows the error, retains the entered values, keeps
  Save enabled, and saves successfully once corrected.
- Pagination — validated across 20 pages; all four arrows navigate correctly.

