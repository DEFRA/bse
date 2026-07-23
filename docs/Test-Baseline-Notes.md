# BSE System — Test Baseline Notes

> **Classification:** Programme Governance — Confidential  
> **Date:** 2026-07-20  
> **Author:** Testing Agent  
> **Status:** Ready for Implementation Agent  
> **Disclaimer:** This document contains proprietary and confidential information belonging to Crown Commercial Property. Unauthorised use, disclosure, or distribution is prohibited.

---

## Purpose

This document inventories what can be extracted from the legacy VB.NET codebase as testable contracts **right now**, before any C# scaffolding exists. It feeds directly into the first test authoring tasks for the implementation agent.

Two sections:
1. **Pure-logic items** — methods with no database dependency that can be ported to C# unit tests immediately.
2. **SP contract inventory** — the 10 most critical stored procedures with parameter signatures and return shapes, forming the contract baseline for Dapper wrapper integration tests.

---

## 1. Pure-Logic Testable Items

> "Pure" means: no call to `TBCultureDA`, `DataAccess`, `FillDataTable`, `ExecuteNonQuery`, or any DB method; operates only on in-memory values or data structures passed in as arguments.

### 1.1 `legacy/BSESystem/Common.vb` — `BSESystem.Common` module

| # | Method | Signature | What to Assert |
|---|--------|-----------|----------------|
| 1 | `FormatRBSE` | `(sRBSE As String) → String` | Strips slashes then reinserts: `"9900001001"` → `"99/00/00100 1"` wait — pattern is `XX/XX/XXXXX`. Actual: `Left(2) + "/" + Mid(3,2) + "/" + Mid(5,5)`. Test: `"9900001001"` → `"99/00/00100 1"` is wrong; correct is `"99/00/00100"` (Left 2 = "99", Mid 3,2 = "00", Mid 5,5 = "00100"). Full RBSE is 9 chars: `"990000100"` → `"99/00/00100"`. Test both 9-char and pre-formatted inputs. Also: empty string passes through unchanged. |
| 2 | `FormatDBSE` | `(sDBSE As String) → String` | Strips slashes then reinserts: `Left(2) + "/" + Mid(3,5)`. Test: `"9900001"` → `"99/00001"`; empty string unchanged; already-formatted string normalised. |
| 3 | `FormatCPHH` | `(sCPHH As String) → String` | Len > 9: `Left(2) + "/" + Mid(3,3) + "/" + Mid(6,4) + "/" + Mid(10,remaining)`. Len ≤ 9: `Left(2) + "/" + Mid(3,3) + "/" + Mid(6,4)`. Test: `"11111111111"` (11 chars) → `"11/111/1111/1"`; `"111111111"` (9 chars) → `"11/111/111"`; empty unchanged. |
| 4 | `GetRowColumnData` | `(objValue As Object) → Object` | `DBNull.Value` → `Nothing` (null); any non-null value → same value returned unchanged. |

**Unit test file location (target):** `tests/BSE.SharedKernel.Tests/Formatting/RbseFormatterTests.cs`

---

### 1.2 `legacy/BSELib/clsCase.vb` — `BSELib.clsCase`

| # | Method | Signature | Pure? | What to Assert |
|---|--------|-----------|-------|----------------|
| 5 | `CheckMandatoryFields` | `(dsCase As DataSet, dsFarm As DataSet, objErrorList As ArrayList) → Boolean` | Yes — no DB calls; operates on DataSet passed in | Validates: (a) `CPHH` null → error "No farm has been specified"; (b) farm `OwnerName` null → error; (c) farm `Address1` null → error; (d) farm `Parish` null + not NonGB → error; (e) farm `County` null → error; (f) farm `AHO` null + not NonGB → error; (g) farm `ADNSRegionID` null + not NonGB → error; (h) eartag country + eartag + herdmark all null → error; (i) `FormADate` null + not NonGB → error; (j) `FormBDate` not null but `Fate` null → error. Test all combinations, valid DataSet returns `True`. |
| 6 | `GetCentreCoordinate` (private) | `(sYCoord1 As String, sYCoord2 As String) → String` | Yes — pure integer arithmetic and string formatting | Extracted logic: `centreY = coord1 + (coord2 - coord1) / 2` with rounding for odd ranges. Pads result to 4 digits. Test: `("0010", "0020")` → `"0015"`; `("0010", "0021")` → `"0016"` (odd range rounds up); `("0001", "0001")` → `"0001"`. This method becomes `CoordinateCalculator.GetCentreCoordinate()` in `BSE.Modules.ReferenceData`. |

**Unit test file location (target):** `tests/BSE.Modules.CaseManagement.Tests/CaseMandatoryFieldValidatorTests.cs`, `tests/BSE.Modules.ReferenceData.Tests/CoordinateCalculatorTests.cs`

---

### 1.3 `legacy/BSELib/EartagValidation/Eartag.vb` — `BSELib.Eartag` (abstract base)

| # | Method | Signature | Pure? | What to Assert |
|---|--------|-----------|-------|----------------|
| 7 | `GetEartag` (Shared factory) | `(countryCode, herdComponent, animalComponent) → Eartag` | Yes — routing only | `countryCode="UK"` → `UKEartag` instance; `countryCode="FR"` → `ECEartag` instance; `countryCode="826012345"` (numeric > 2 chars) → `IsoEarTag` instance; unknown code → `NoCountryEartag` instance. |
| 8 | `GetHerdComponent` (Protected) | `(strEarTag, lngStartIndex, lngEndIndex) → String` | Yes — pure string manipulation | Strips `/`, `\`, `-`, `~`, spaces, trims. Test: `"UK/123456"` from index 1 length 8 → `"UK123456"`; input with all separators removed. |
| 9 | `GetAnimalComponent` (Protected) | `(strEarTag, lngIndex) → String` | Yes | Extracts from index to end, strips separators. Test: `"UK1234/56789"` from index 7 → `"56789"`. |
| 10 | `IsECCountryCode` (Shared helper) | `(countryCode) → Boolean` | Yes — static list check | `"FR"` → True; `"UK"` → False; `"AT"`, `"BE"`, `"DE"`, `"DK"`, `"EL"`, `"ES"`, `"FI"`, `"IE"`, `"IT"`, `"LU"`, `"NL"`, `"PT"`, `"SE"` → True; unknown code → False. |
| 11 | `IsNewVersionId` (Shared helper) | `(countryCode) → Boolean` | Yes | Length > 2 → True; length ≤ 2 → False. Test: `"UK"` → False; `"GB0"` → True; `"8260"` → True; `"826012345"` → True. |
| 12 | `mUKCountries` static array | Field | Yes | Contains `{"UK", "GB0", "GB1", "GB2", "8260", "8261", "8262"}`. Verify by asserting all expected values present; no unexpected values. |

**Unit test file location (target):** `tests/BSE.SharedKernel.Tests/EartagValidation/EartagFactoryTests.cs`

---

### 1.4 `legacy/BSELib/EartagValidation/UKEartag.vb` — `BSELib.UKEartag`

| # | Method | Signature | Pure? | What to Assert |
|---|--------|-----------|-------|----------------|
| 13 | `GetFormat` (override) | Implicit via constructor — sets `mFormat` | Yes | NI numeric: herdComponent starts with "9" + both numeric → `NINumericEartagFormat`. Isle of Man: starts "01" → `IsleOfManNumericEartagFormat`. Guernsey: starts "02" → `GuernseyNumericEartagFormat`. Jersey: starts "03" → `JerseyNumericEartagFormat`. Other numeric/numeric → `GBNumericEartagFormat`. Numeric herd + alpha animal → `NIAlphaNumericEartagFormat`. Alpha herd starts "MN" → `IsleOfManAlphaNumericEartagFormat`. "GY" → `GuernseyAlphaNumericEartagFormat`. "JY" → `JerseyAlphaNumericEartagFormat`. Other alpha/alpha → `GBAlphaNumericEartagFormat`. |

**Unit test file location (target):** `tests/BSE.SharedKernel.Tests/EartagValidation/UkEartagFormatRoutingTests.cs`

---

### 1.5 `legacy/BSELib/EartagValidation/IsoEarTag.vb` — `BSELib.IsoEarTag`

| # | Method | Signature | Pure? | What to Assert |
|---|--------|-----------|-------|----------------|
| 14 | `GetFormat` (override) | Implicit via constructor | Yes | CountryCode first char is digit → `IsoNumericCountryEartagFormat`. First char is alpha → `IsoAlphaNumericCountryEartagFormat`. |

**Unit test file location (target):** `tests/BSE.SharedKernel.Tests/EartagValidation/IsoEartagFormatRoutingTests.cs`

---

### 1.6 `legacy/BSELib/EartagValidation/EartagFormat/` — Format validation classes (23 files)

Each format class implements `IEartagFormat` with a `Validate(countryCode, herdComponent, animalComponent)` method that returns an error code string (empty = valid) and a `GetPresentationFormat()` method that returns the formatted eartag string. These are entirely pure — no DB calls.

The following format classes each require a minimum of one valid and one invalid input test:

| # | Class | Key Validation Rule to Test |
|---|-------|-----------------------------|
| 15 | `GBNumericEartagFormat` | Herd must be numeric, 6 digits; animal must be numeric, 5 digits |
| 16 | `GBAlphaNumericEartagFormat` | Herd must be 2 alpha chars + digits; animal must be digits |
| 17 | `NINumericEartagFormat` | Herd starts with "9"; specific NI format rules |
| 18 | `NIAlphaNumericEartagFormat` | NI alpha-numeric format |
| 19 | `IsleOfManNumericEartagFormat` | Starts "01" |
| 20 | `IsleOfManAlphaNumericEartagFormat` | Starts "MN" |
| 21 | `GuernseyNumericEartagFormat` | Starts "02" |
| 22 | `GuernseyAlphaNumericEartagFormat` | Starts "GY" |
| 23 | `JerseyNumericEartagFormat` | Starts "03" |
| 24 | `JerseyAlphaNumericEartagFormat` | Starts "JY" |
| 25 | `ECEartagFormat` | EC country code present; herd and animal components per EC rules |
| 26 | `IsoNumericCountryEartagFormat` | Numeric country prefix > 2 digits |
| 27 | `IsoAlphaNumericCountryEartagFormat` | Alpha country prefix > 2 chars |
| 28 | `NIEartagFormat` | Base NI rules |
| 29 | `UKEartagFormat` | UK base rules |
| 30 | `UKNonNINumericEartagFormat` | UK non-NI numeric |
| 31 | `UKNonNIAlphaNumericEartagFormat` | UK non-NI alpha |
| 32 | `IsoEarTagFormat` | ISO format base |
| 33 | `PreBarimoEartagFormat` | Pre-Barimo format (legacy EU scheme) |
| 34 | `FreeEartagFormat` | Unconstrained free-text eartag |
| 35 | `ECEartagFormat` (base) | EC validation base |
| 36 | `EartagFormatBase` | Abstract base methods: `ReformatHerdComponent`, `ReformatAnimalComponent` strip separators |
| 37 | `IEartagFormat` | Interface definition — verify correct members: `FormatId`, `FormatName`, `Validate`, `GetPresentationFormat`, `ReformatHerdComponent`, `ReformatAnimalComponent` |

**Unit test file location (target):** `tests/BSE.SharedKernel.Tests/EartagValidation/EartagFormatValidationTests.cs`

> **Implementation note for the implementation agent:** All `IEartagFormat` implementations should be migrated to C# as part of `BSE.SharedKernel` in Slice 0 or early Slice 2. They have no dependencies on legacy infrastructure — they are pure business rules. Port each class with its validate logic and write a parameterised `[Theory]` test covering at minimum: (a) valid input returns empty error code, (b) invalid input returns non-empty error code, (c) presentation format matches expected string.

---

### Summary Count

| Source | Pure-logic items |
|--------|-----------------|
| `Common.vb` | 4 |
| `clsCase.vb` | 2 |
| `Eartag.vb` (base) | 6 |
| `UKEartag.vb` | 1 |
| `IsoEarTag.vb` | 1 |
| `EartagFormat/` (23 files) | 23 |
| **Total** | **37** |

---

## 2. SP Contract Inventory — Top 10 Critical Stored Procedures

These are ordered by risk × usage. Each entry defines: SP name, input parameters, output shape, and what the Dapper wrapper integration test must assert.

---

### SP-01: `GetCaseDetailsByRBSE`

**Domain:** Case Management  
**Risk:** Highest — sole source of truth for the entire case edit workflow; result ordering mapped to integer constants  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/GetCaseDetailsByRBSE.sql`

| Parameter | Direction | Type | Notes |
|-----------|-----------|------|-------|
| `@RBSE` | IN | `CHAR(9)` | Raw RBSE without slashes |

**Return shape:** 11 result sets in fixed order:

| Table index | Content | Key C# type |
|-------------|---------|------------|
| 0 | Case row | `CaseRecord` |
| 1 | CaseClinical row (0 or 1) | `ClinicalRecord` |
| 2 | CaseBAB row (0 or 1) | `BabRecord` |
| 3 | OtherOwner rows | `IEnumerable<OtherOwnerRecord>` |
| 4 | CaseFeed rows | `IEnumerable<FeedRecord>` |
| 5 | ClinicalVisit rows | `IEnumerable<ClinicalVisitRecord>` |
| 6 | DamDetails row (0 or 1) | `DamSireRecord` |
| 7 | SireDetails row (0 or 1) | `DamSireRecord` |
| 8 | CaseRelation rows | `IEnumerable<CaseRelationRecord>` |
| 9 | CaseTest rows | `IEnumerable<CaseTestRecord>` |
| 10 | CaseWork row (0 or 1) | `CaseWorkRecord` |

**Integration test assertion:** For a reference RBSE, all 11 result sets are non-null and `CaseRecord.Rbse == input`. Field count in each result set matches golden file.

---

### SP-02: `AddCase`

**Domain:** Case Management  
**Risk:** Highest — creates new BSE case; AuditLog row must be written atomically  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/AddCase.sql`

| Parameter | Direction | Type | Notes |
|-----------|-----------|------|-------|
| `@RBSE` | IN | `CHAR(9)` | |
| `@CPHH` | IN | `CHAR(11)` | |
| `@EartagCountry` | IN | `NVARCHAR` | |
| `@EartagHerdmark` | IN | `NVARCHAR` | |
| `@Eartag` | IN | `NVARCHAR` | |
| *(~35 additional case fields)* | IN | various | See `UpdateCaseRecord` in `clsCase.vb` |
| `@UserID` | IN | `INT` | |
| `RETURN_VALUE` | OUT | `INT` | 0=success, 1=AuditLog fail, 2=Farm not found, 3=duplicate RBSE |

**Integration test assertion:** Return value = 0; `Case` table contains row with correct RBSE; `AuditLog` contains 1 new row for this RBSE.

---

### SP-03: `EditCase`

**Domain:** Case Management  
**Risk:** High — optimistic concurrency via `RowStamp`; partial failure risk  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/EditCase.sql`

| Parameter | Direction | Type | Notes |
|-----------|-----------|------|-------|
| `@RBSE` | IN | `CHAR(9)` | |
| *(case fields as per AddCase minus CPHH)* | IN | various | |
| `@RowStamp` | IN | `ROWVERSION` | Optimistic concurrency token |
| `@UserID` | IN | `INT` | |
| `RETURN_VALUE` | OUT | `INT` | 0=success, 1=case deleted, 2=AuditLog fail, 3=RowStamp conflict, 4=update fail, 5=batch-link read fail, 6=batch-link insert fail |

**Integration test assertion:** Correct `RowStamp` → return 0, field updated. Stale `RowStamp` → return 3, no update.

---

### SP-04: `GetSearchCase`

**Domain:** Search  
**Risk:** High — primary case lookup; 17 filter parameters  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/GetSearchCase.sql`

| Parameter | Direction | Type |
|-----------|-----------|------|
| `@RBSE` | IN | `NVARCHAR` (nullable) |
| `@Eartag` | IN | `NVARCHAR` (nullable) |
| `@DBSE` | IN | `NVARCHAR` (nullable) |
| `@Fate` | IN | `NVARCHAR` (nullable) |
| `@FinalResult` | IN | `NVARCHAR` (nullable) |
| `@FormADateFrom` | IN | `DATETIME` (nullable) |
| `@FormADateTo` | IN | `DATETIME` (nullable) |
| `@FormBDateFrom` | IN | `DATETIME` (nullable) |
| `@FormBDateTo` | IN | `DATETIME` (nullable) |
| `@PassiveActive` | IN | `NVARCHAR` (nullable) |
| `@IsImportedCase` | IN | `BIT` (nullable) |
| `@CPHH` | IN | `NVARCHAR` (nullable) |
| `@OwnerName` | IN | `NVARCHAR` (nullable) |
| `@Herdmark` | IN | `NVARCHAR` (nullable) |
| `@County` | IN | `NVARCHAR` (nullable) |
| `@BSE1DateFrom` | IN | `DATETIME` (nullable) |
| `@BSE1DateTo` | IN | `DATETIME` (nullable) |

**Return shape:** DataTable rows with formatted `RBSE`, `CPHH`, case summary fields.  
**Integration test assertion:** RBSE-only search returns exactly 1 row for a known reference RBSE; all-null search returns all cases (or configured row limit).

---

### SP-05: `GetSearchFarm`

**Domain:** Search  
**Risk:** Medium — primary farm lookup; 8 filter parameters  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/GetSearchFarm.sql`

| Parameter | Direction | Type |
|-----------|-----------|------|
| `@CPHH` | IN | `NVARCHAR` (nullable) |
| `@OwnerName` | IN | `NVARCHAR` (nullable) |
| `@Address` | IN | `NVARCHAR` (nullable) |
| `@County` | IN | `NVARCHAR` (nullable) |
| `@Herdmark` | IN | `NVARCHAR` (nullable) |
| `@IsDealer` | IN | `BIT` (nullable) |
| `@AHO` | IN | `NVARCHAR` (nullable) |
| `@IncludeNonGBFarms` | IN | `BIT` |

**Return shape:** Farm rows including `CPHH`, `OwnerName`, `Address1`, `County`.  
**Integration test assertion:** CPHH search returns exactly 1 row for known CPHH; combined filter returns subset.

---

### SP-06: `AddBatchNumber`

**Domain:** Batch  
**Risk:** High — batch number sequence integrity; output parameters  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/AddBatchNumber.sql`

| Parameter | Direction | Type |
|-----------|-----------|------|
| `@BatchYear` | OUT | `INT` |
| `@BatchNumber` | OUT | `INT` |
| `@BatchID` | OUT | `INT` |

**Return shape:** All three output parameters populated.  
**Integration test assertion:** `BatchID` > 0; `BatchYear` = current year (or configured year); `BatchNumber` increments by 1 on successive calls within same year.

---

### SP-07: `GetADNSCasesForGB`

**Domain:** ADNS Export  
**Risk:** Critical — regulatory output; used to build email to EU  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/GetADNSCasesForGB.sql`

| Parameter | Direction | Type | Notes |
|-----------|-----------|------|-------|
| *(no input parameters observed in clsADNSReport constructor)* | — | — | Returns all GB cases where `EmailSentToADNSDate IS NULL` and `FinalResult='Pos'` |

**Return shape:** Case rows including `RBSE`, `CPHH`, `FinalResultDate`, `ADNSRegionID`, farm address fields.  
**Integration test assertion:** Returns only positive, not-yet-notified GB cases; no non-GB or already-notified cases included. Golden file comparison (see Test-Strategy §7.2).

---

### SP-08: `EditCaseADNS`

**Domain:** ADNS Export  
**Risk:** Critical — marks case as notified; must be atomic with email send  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/EditCaseADNS.sql`

| Parameter | Direction | Type |
|-----------|-----------|------|
| `@RBSE` | IN | `CHAR(9)` |
| `@ADNSReferenceYear` | IN | `SMALLINT` |
| `@ADNSReferenceNumber` | IN | `INT` |
| `@EmailSentToADNSDate` | IN | `DATETIME` |

**Return shape:** No result set; RETURN_VALUE or rows affected.  
**Integration test assertion:** After call, `Case.EmailSentToADNSDate`, `ADNSReferenceYear`, `ADNSReferenceNumber` match inputs; case no longer returned by `GetADNSCasesForGB`.

---

### SP-09: `GetGroupForUser`

**Domain:** User Management  
**Risk:** High — sole authorisation mechanism; OIDC transition depends on correct mapping  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/GetGroupForUser.sql`

| Parameter | Direction | Type | Notes |
|-----------|-----------|------|-------|
| `@NTUserID` | IN | `NVARCHAR` | NT login string e.g. `"DEFRA\jsmith"` |
| `@UserGroup` | OUT | `INT` | Group code (maps to `UserGroup` enum) |
| `@Name` | OUT | `NVARCHAR` | Display name |

**Return shape:** Two output parameters.  
**Integration test assertion:** Known NTLogin → expected group code; unknown NTLogin → output params null or 0 and method returns false.

---

### SP-10: `EditCaseFinalResult`

**Domain:** Case Management  
**Risk:** High — statutory final result; assigns DBSE number; uses optimistic concurrency  
**Location:** `legacy/BSEDatabase/dbo/Stored Procedures/EditCaseFinalResult.sql`

| Parameter | Direction | Type | Notes |
|-----------|-----------|------|-------|
| `@RBSE` | IN | `CHAR(9)` | |
| `@FinalResult` | IN | `NVARCHAR` | |
| `@FinalResultDate` | IN | `DATETIME` | |
| `@RetrospectiveTestType` | IN | `NVARCHAR` | nullable |
| `@RetrospectiveResult` | IN | `NVARCHAR` | nullable |
| `@RetrospectiveResultDate` | IN | `DATETIME` | nullable |
| `@RetrospectiveComment` | IN | `NVARCHAR` | nullable |
| `@RowStamp` | IN | `ROWVERSION` | Optimistic concurrency |
| `@UserID` | IN | `INT` | |
| `@AlternateDiagnosis` | IN | `NVARCHAR` | nullable |
| `@LabComment` | IN | `NVARCHAR` | nullable |
| `@DBSE` | OUT | `NVARCHAR(7)` | Assigned DBSE number |

**Return shape:** `@DBSE` output parameter populated.  
**Integration test assertion:** `@DBSE` is non-empty after call; `Case.FinalResult`, `FinalResultDate`, `DBSE` match inputs; `AuditLog` row written.

---

## 3. Additional SPs Worth Early Characterisation

The following SPs are not in the top 10 by risk but should have golden files captured before their slice begins:

| SP Name | Slice | Reason |
|---------|-------|--------|
| `GetLastADNSReferenceByArea` | Slice 11 | Required before reference number sequence can be verified |
| `GetAuditLogByCase` | Slice 4 | Immutable record; golden file comparison is straightforward |
| `GetFarmByCPHH` | Slice 5 | Existence check; simple to capture |
| `ChangeCPHH` | Slice 5 | Cascade to 4 tables; error codes 1–8; high cascade risk |
| `ChangeRBSE` | Slice 8 | Cascade to 11 child tables; error codes 1–15; critical |
| `GetBSESSCheckByRBSE` | Slice 13 | 11 output parameters; complex shape |
| `GetlatestBatchNumbers` | Slice 7 | Dashboard data; quick to capture |

---

## 4. Items NOT Testable Until Slice 0 (C# scaffolding exists)

| Item | Reason |
|------|--------|
| `clsCase.UpdateCaseDetails()` | Tightly coupled to ADO.NET `SqlConnection`/`SqlTransaction`; no seam for injection without refactoring (which is the point of migration) |
| `clsFarm.UpdateFarmDetails()` | Same as above |
| `clsADNSReport` constructor | Calls SP in constructor body; no way to substitute without modifying legacy code |
| `clsBatch.CreateBatchNumber()` | SP call with OUTPUT params; testable only via Dapper integration test once scaffold exists |
| `clsUser.GetGroupForUser()` | SP call via `TBCultureDA`; integration-only |
| `clsSearch` all methods | All are thin SP wrappers; integration-only |
| `clsAuditLog` all methods | Read-only SP wrappers; integration-only |
| `LookupData.GetLookupData()` | Dynamic SP name routing; integration-only |
