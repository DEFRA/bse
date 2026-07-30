# GDS compliance change summary

## Scope completed
The following Razor Pages areas were updated from Bootstrap-oriented markup to GOV.UK Design System component patterns:

- Shared layout and assets
  - `src/BSE.Host/Pages/Shared/_Layout.cshtml`
  - `src/BSE.Host/wwwroot/govuk/govuk-frontend.min.css`
  - `src/BSE.Host/wwwroot/govuk/govuk-frontend.min.js`
  - `src/BSE.Host/wwwroot/assets/images/favicon.svg`
  - `src/BSE.Host/wwwroot/assets/images/govuk-crest.svg`
  - `src/BSE.Host/wwwroot/assets/images/govuk-icon-mask.svg`

- Search pages
  - `src/BSE.Host/Pages/Search/Cases.cshtml`
  - `src/BSE.Host/Pages/Search/Farms.cshtml`
  - `src/BSE.Host/Pages/Search/Outstanding.cshtml`
  - `src/BSE.Host/Pages/Search/RelatedAnimals.cshtml`

- Case pages
  - `src/BSE.Host/Pages/Case/New.cshtml`
  - `src/BSE.Host/Pages/Case/Edit.cshtml`
  - `src/BSE.Host/Pages/Case/Details.cshtml`
  - `src/BSE.Host/Pages/Case/Lookup.cshtml`
  - `src/BSE.Host/Pages/Case/Delete.cshtml`
  - `src/BSE.Host/Pages/Case/MoveCase.cshtml`
  - `src/BSE.Host/Pages/Case/RbseChange.cshtml`

- Farm pages
  - `src/BSE.Host/Pages/Farm/New.cshtml`
  - `src/BSE.Host/Pages/Farm/Edit.cshtml`
  - `src/BSE.Host/Pages/Farm/Details.cshtml`
  - `src/BSE.Host/Pages/Farm/Lookup.cshtml`
  - `src/BSE.Host/Pages/Farm/CphhChange.cshtml`
  - `src/BSE.Host/Pages/Farm/_FarmFormFields.cshtml`

- Home and shared pages
  - `src/BSE.Host/Pages/Home.cshtml`
  - `src/BSE.Host/Pages/Error.cshtml`
  - `src/BSE.Host/Pages/Shared/_AuditDateRangeForm.cshtml`

- Audit log pages
  - `src/BSE.Host/Pages/AuditLog/ByDate.cshtml`
  - `src/BSE.Host/Pages/AuditLog/ByUser.cshtml`
  - `src/BSE.Host/Pages/AuditLog/CaseMoves.cshtml`
  - `src/BSE.Host/Pages/AuditLog/CphhChanges.cshtml`
  - `src/BSE.Host/Pages/AuditLog/NewFarms.cshtml`
  - `src/BSE.Host/Pages/AuditLog/RbseChanges.cshtml`

- BSESS pages
  - `src/BSE.Host/Pages/Bsess/CheckByDate.cshtml`
  - `src/BSE.Host/Pages/Bsess/CheckByRbse.cshtml`

- CaseWork pages
  - `src/BSE.Host/Pages/CaseWork/Menu.cshtml`
  - `src/BSE.Host/Pages/CaseWork/Entry.cshtml`
  - `src/BSE.Host/Pages/CaseWork/Minute.cshtml`

- ADNS Export pages
  - `src/BSE.Host/Pages/AdnsExport/Menu.cshtml`
  - `src/BSE.Host/Pages/AdnsExport/Gb.cshtml`
  - `src/BSE.Host/Pages/AdnsExport/Ci.cshtml`
  - `src/BSE.Host/Pages/AdnsExport/Ni.cshtml`

- OSS Export page
  - `src/BSE.Host/Pages/OssExport/Menu.cshtml`

- Admin pages
  - `src/BSE.Host/Pages/Admin/PickLists.cshtml`
  - `src/BSE.Host/Pages/Admin/Users.cshtml`
  - `src/BSE.Host/Pages/Admin/PickListEdit.cshtml`

## Main improvements applied
- GOV.UK page structure introduced (header/service navigation/footer/skip link/initAll wiring in layout).
- GOV.UK form components applied (`govuk-input`, `govuk-label`, `govuk-hint`, `govuk-select`, `govuk-checkboxes`, `govuk-button`).
- GOV.UK result presentation applied (`govuk-table`, `govuk-summary-list`, `govuk-notification-banner`, `govuk-warning-text`, `govuk-error-summary`).
- Sentence case labels/headings and clearer action wording applied in updated pages.
- Quick-search query parameter alignment fixed on Home page (`Filter.Rbse`, `Filter.Cphh`) to match Razor Page models.

## Validation
- Build verification command used:
  - `dotnet build src/BSE.Host/BSE.Host.csproj -p:BaseOutputPath=C:\Workspace\MigratedWorkSpace\bse\artifacts\tempbuild\`
- Status: **Build succeeded** after each batch of changes.

## Remaining work candidates
- Confirm no residual Bootstrap classes in generated/conditional HTML fragments and custom scripts.
- Manually review each updated page against GOV.UK content style and one-question-per-page guidance where applicable.

## Manual asset follow-up
GOV.UK font binaries are still required in `src/BSE.Host/wwwroot/assets/fonts` for full visual parity:
- `light-94a07e06a1-v2.woff2`
- `light-f591b13f7d-v2.woff`
- `bold-b542beb274-v2.woff2`
- `bold-affa96571d-v2.woff`
