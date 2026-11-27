# ITSD81 — Laboratory Activity 5: API Integration, Traceability, and Acceptance Testing

## 1. Implementation Baseline Record

| Field | Student Entry |
|---|---|
| **Lab 4 Submitted Commit** | `ea5e037b5143a41e976db57cb90c7977a4563a56` (Tagged `lab4-submitted`) |
| **Baseline Limitations** | Lab 4 was a desktop-only architecture with direct EF Core / PostgreSQL access from Avalonia UI ViewModels. No remote API, HTTP client, or distributed boundaries existed. |
| **Lab 5 Revision and Tag** | Commit `2e5653f` / current revision (Tagged `lab5-submitted`) |
| **Before / After Boundary** | **Before (Lab 4):** Avalonia Desktop ViewModels directly depended on `EquipmentBorrowingDbContext`, `EfUnitOfWork`, and EF repositories using direct Npgsql database connections.<br>**After (Lab 5):** Desktop app communicates purely through `EquipmentBorrowingApiClient` over asynchronous HTTP (`http://localhost:5000/api/v1`). All EF Core, DbContext, Unit of Work, and PostgreSQL connections reside exclusively behind the `EquipmentBorrowing.Api` boundary. |
| **Migration / Refactoring** | 1. Extracted API DTO contracts into `EquipmentBorrowing.Application.Models.ApiContracts`.<br>2. Built ASP.NET Core REST API (`EquipmentBorrowing.Api`) hosting API-01 through API-16.<br>3. Configured OpenAPI 3.0 and interactive Scalar documentation at `/scalar/v1`.<br>4. Replaced database DI in Desktop with `HttpClient` and `IEquipmentBorrowingApiClient`.<br>5. Removed `EquipmentBorrowing.Infrastructure` reference from Desktop project. |
| **Reproduction** | 1. Start PostgreSQL 18 on `localhost:5432` (database `equipment_borrowing`).<br>2. Run API: `dotnet run --project src/EquipmentBorrowing.Api` (listens on `http://localhost:5000`).<br>3. Run Desktop: `dotnet run --project src/EquipmentBorrowing.Desktop`.<br>4. Run Tests: `dotnet test` (30/30 passed). |

---

## 2. API Endpoint Contract Catalog

| API ID | Method & Route | Request / Query | Success Response | Failure / Rules | Use Cases | Tests | Implementation Reference |
|---|---|---|---|---|---|---|---|
| **API-01** | `GET /api/v1/equipment` | `available=true` (optional) | `200` Array of `EquipmentResponse` | `400` malformed boolean; BR-05–07, 10 | UC-01, UC-02 | T01, T07 | `EquipmentEndpoints.cs:17` (`MapEquipmentEndpoints`) |
| **API-02** | `GET /api/v1/students` | `search` (optional) | `200` Array of `StudentResponse` | `400` invalid query; BR-01–03 | UC-03, 06, 07, 13, 14 | T01, T02, T07 | `StudentEndpoints.cs:17` (`MapStudentEndpoints`) |
| **API-03** | `GET /api/v1/students/{id}` | None | `200` `StudentResponse` | `404` not found; BR-01–03 | UC-07, 13, 14 | T01, T02, T07 | `StudentEndpoints.cs:38` (`MapStudentEndpoints`) |
| **API-04** | `POST /api/v1/students` | `{studentNumber, fullName, contact}` | `201` `StudentResponse`, `Location: /api/v1/students/{id}` | `400` blank; `409` duplicate; BR-01–02, 16 | UC-12 | T02, T07 | `StudentEndpoints.cs:53` (`MapStudentEndpoints`) |
| **API-05** | `PUT /api/v1/students/{id}` | `{studentNumber, fullName, contact}` | `200` `StudentResponse` | `400` invalid, `404` missing, `409` duplicate; BR-01–03, 14 | UC-13 | T02, T07 | `StudentEndpoints.cs:81` (`MapStudentEndpoints`) |
| **API-06** | `PATCH /api/v1/students/{id}/eligibility` | `{isEligible: boolean}` | `200` `StudentResponse`; same value is no-op | `400` invalid, `404` missing; BR-04, 08 | UC-14 | T02, T07 | `StudentEndpoints.cs:117` (`MapStudentEndpoints`) |
| **API-07** | `GET /api/v1/equipment/{id}` | None | `200` `EquipmentResponse` | `404` missing; BR-05–07 | UC-08, 16 | T01, T03, T07 | `EquipmentEndpoints.cs:40` (`MapEquipmentEndpoints`) |
| **API-08** | `POST /api/v1/equipment` | `{equipmentCode, name, description}` | `201` `EquipmentResponse`, `Location: /api/v1/equipment/{id}` | `400` invalid, `409` duplicate; BR-05–06, 16 | UC-15 | T03, T07 | `EquipmentEndpoints.cs:55` (`MapEquipmentEndpoints`) |
| **API-09** | `PUT /api/v1/equipment/{id}` | `{equipmentCode, name, description}` | `200` `EquipmentResponse` | `400` invalid/protected field (`isAvailable`), `404` missing, `409` duplicate; BR-05–07, 14 | UC-16 | T03, T07 | `EquipmentEndpoints.cs:83` (`MapEquipmentEndpoints`) |
| **API-10** | `POST /api/v1/borrowings` | `{studentId, equipmentId, expectedReturnDate}` | `201` `BorrowingResponse`, `Location: /api/v1/borrowings/{id}` | `400` invalid date; `404` missing reference; `409` eligibility/limit/availability; BR-08–11, 14–16 | UC-03 | T04, T05, T07 | `BorrowingEndpoints.cs:72` (`MapBorrowingEndpoints`) |
| **API-11** | `POST /api/v1/checkout-requests` | `{studentId, equipmentIds:[...], expectedReturnDate}` | `201` `{borrowings:[...]}` | `400` duplicate/fewer than two; `404` missing; `409` limit/availability/eligibility; BR-08–12, 15–16 | UC-06 | T05, T07 | `BorrowingEndpoints.cs:126` (`MapBorrowingEndpoints`) |
| **API-12** | `POST /api/v1/borrowings/{id}/return` | `{}` empty object | `200` `BorrowingResponse` | `404` missing; `409` already returned; BR-10, 13–14 | UC-05 | T04, T07 | `BorrowingEndpoints.cs:193` (`MapBorrowingEndpoints`) |
| **API-13** | `GET /api/v1/borrowings/{id}` | None | `200` `BorrowingResponse` | `404` missing; BR-14 | UC-03, UC-05 | T01, T04, T07 | `BorrowingEndpoints.cs:17` (`MapBorrowingEndpoints`) |
| **API-14** | `GET /api/v1/borrowings` | `search`, `status`, `sort`, `direction` | `200` Array of matching ordered `BorrowingResponse` | `400` unsupported filter/sort values; BR-14, 17 | UC-04, UC-09 | T01, T06, T07 | `BorrowingEndpoints.cs:32` (`MapBorrowingEndpoints`) |
| **API-15** | `GET /api/v1/students/{id}/borrowings` | None | `200` Array of `BorrowingResponse` | `404` missing student; BR-14, 19 | UC-07 | T01, T06, T07 | `StudentEndpoints.cs:141` (`MapStudentEndpoints`) |
| **API-16** | `GET /api/v1/equipment/{id}/borrowings` | None | `200` Array of `BorrowingResponse` | `404` missing item; BR-14, 19 | UC-08 | T01, T06, T07 | `EquipmentEndpoints.cs:127` (`MapEquipmentEndpoints`) |

---

## 3. API-to-Use-Case Traceability Matrix

| Lab 4 Use Case | Applicable Rules | Lab 5 Endpoint | Test | Implementation Reference | Evidence |
|---|---|---|---|---|---|
| **UC-01** (Browse Catalog) | BR-05–07, 10, 18–19 | API-01 | T01, T07 | `EquipmentEndpoints.cs:17`, `EquipmentBorrowingApiClient.cs:81` | `ApiClientTests.cs:41` (`T01_GetEquipmentAndStudents_ReturnsParsedList`) |
| **UC-02** (Filter Available) | BR-10, 18–19 | API-01 | T01, T07 | `EquipmentEndpoints.cs:17`, `EquipmentViewModel.cs:93` | `ApiClientTests.cs:41`, HTTP GET `available=true` verified |
| **UC-03** (Single Borrow) | BR-08–11, 14–16, 18–19 | API-10, API-13 | T04, T07 | `BorrowingEndpoints.cs:72`, `CheckoutViewModel.cs:116` | `ApiClientTests.cs:113` (`T04_BorrowAndReturn_HandlesValidationAndRepeatReturn`) |
| **UC-04** (Active Loans) | BR-14, 18–19 | API-14 | T01, T06, T07 | `BorrowingEndpoints.cs:32`, `BorrowingsViewModel.cs:44` | Verified GET `/api/v1/borrowings?status=Active` |
| **UC-05** (Return Item) | BR-10, 13–16, 18–19 | API-12, API-13 | T04, T07 | `BorrowingEndpoints.cs:193`, `BorrowingsViewModel.cs:91` | `ApiClientTests.cs:113` (Verified 200 return & 409 repeat reject) |
| **UC-06** (Multi Checkout) | BR-08–12, 14–16, 18–19 | API-11 | T05, T07 | `BorrowingEndpoints.cs:126`, `MultiItemBorrowService.cs:39` | `AcceptanceTests_A06_MultiItemCheckout.cs:37` (Atomic rollback) |
| **UC-07** (Student History) | BR-14, 18–19 | API-02, 03, 15 | T01, T06, T07 | `StudentEndpoints.cs:141`, `HistoryViewModel.cs:177` | Verified GET `/api/v1/students/{id}/borrowings` |
| **UC-08** (Equipment History) | BR-14, 18–19 | API-01, 07, 16 | T01, T06, T07 | `EquipmentEndpoints.cs:127`, `HistoryViewModel.cs:224` | Verified GET `/api/v1/equipment/{id}/borrowings` |
| **UC-09** (Search/Filter/Sort) | BR-17–19 | API-14 | T06, T07 | `BorrowingEndpoints.cs:32`, `HistoryViewModel.cs:121` | Verified GET `/api/v1/borrowings?search=...&sort=...` |
| **UC-12** (Register Student) | BR-01–02, 16, 18–19 | API-04 | T02, T07 | `StudentEndpoints.cs:53`, `StudentsViewModel.cs:102` | `ApiClientTests.cs:67` (`T02_StudentRegistration_HandlesSuccessAndConflict`) |
| **UC-13** (Edit Student) | BR-01–03, 14, 16, 18–19 | API-02, 03, 05 | T02, T07 | `StudentEndpoints.cs:81`, `StudentsViewModel.cs:132` | Verified PUT `/api/v1/students/{id}` updates and 409 duplicate check |
| **UC-14** (Student Eligibility) | BR-04, 08, 13–14, 18–19 | API-02, 03, 06 | T02, T07 | `StudentEndpoints.cs:117`, `StudentsViewModel.cs:175` | `ApiClientTests.cs:67` (PATCH eligibility verified) |
| **UC-15** (Register Item) | BR-05–06, 10, 16, 18–19 | API-08 | T03, T07 | `EquipmentEndpoints.cs:55`, `EquipmentViewModel.cs:116` | Verified POST `/api/v1/equipment` returns 201 + Location header |
| **UC-16** (Edit Item) | BR-05–07, 10, 14, 16, 18–19 | API-01, 07, 09 | T03, T07 | `EquipmentEndpoints.cs:83`, `EquipmentViewModel.cs:146` | `ApiClientTests.cs:98` (`T03_EquipmentUpdate_ProtectedFieldRejection`) |

---

## 4. Acceptance Test Execution Record (T01–T08)

### Test ID: T01
- **Date / Environment**: 2026-11-24 / Local Development (.NET 10, Npgsql, PostgreSQL 18)
- **Objective**: Verify API operation without Avalonia and correct reads (API-01–03, 07, 13–16).
- **Expected Result**: 200 arrays/objects match seed; available excludes unavailable items; empty history returns `[]`; missing returns 404; unsupported method returns 405. OpenAPI JSON accessible at `/openapi/v1.json`.
- **Observed Result**: Independent HTTP requests succeed without Avalonia running. OpenAPI and Scalar accessible. 404 on unknown ID.
- **Pass / Fail**: **PASS**
- **Evidence Reference**: `ApiClientTests.cs:41` (`T01_GetEquipmentAndStudents_ReturnsParsedList`), Scalar UI at `http://localhost:5000/scalar/v1`.

### Test ID: T02
- **Date / Environment**: 2026-11-24 / Local Development
- **Objective**: Student create/edit/eligibility, uniqueness and history (API-04–06; UC-12–14; BR-01–04).
- **Expected Result**: POST returns 201 + Location header; duplicate returns 409; blank returns 400; PATCH eligibility updates state without altering borrowing history.
- **Observed Result**: Student registration and eligibility toggle persist correctly; duplicate student numbers rejected with 409 Conflict.
- **Pass / Fail**: **PASS**
- **Evidence Reference**: `ApiClientTests.cs:67` (`T02_StudentRegistration_HandlesSuccessAndConflict`).

### Test ID: T03
- **Date / Environment**: 2026-11-24 / Local Development
- **Objective**: Equipment create/edit, uniqueness and protected state (API-07–09; UC-15–16; BR-05–07).
- **Expected Result**: POST returns 201 + Location; duplicate code returns 409; attempting to update `isAvailable` directly via PUT returns 400 Bad Request.
- **Observed Result**: Protected field `isAvailable` in PUT request rejected with 400 Bad Request; valid updates persist.
- **Pass / Fail**: **PASS**
- **Evidence Reference**: `ApiClientTests.cs:98` (`T03_EquipmentUpdate_ProtectedFieldRejection`).

### Test ID: T04
- **Date / Environment**: 2026-11-24 / Local Development
- **Objective**: Single borrowing, return, validation and persistence (API-10, 12–13; UC-03/05; BR-08–11/13).
- **Expected Result**: POST borrowing creates active record, marks equipment unavailable; return endpoint marks available and sets returned date; repeat return rejected with 409.
- **Observed Result**: Borrow and return flow succeeds; repeat return returns 409 with `"This equipment borrowing has already been returned."`
- **Pass / Fail**: **PASS**
- **Evidence Reference**: `ApiClientTests.cs:113` (`T04_BorrowAndReturn_HandlesValidationAndRepeatReturn`).

### Test ID: T05
- **Date / Environment**: 2026-11-24 / Local Development
- **Objective**: Multi-item success, limit boundary, invalid requests and atomic rollback (API-10–11; UC-03/06; BR-08–12/15–16).
- **Expected Result**: 2+ items checked out atomically; exceeding 3-item limit rejected with 409; on simulated write failure, complete transaction rolls back with zero partial writes.
- **Observed Result**: Database transaction maintains all-or-nothing atomicity; injected failure triggers clean rollback.
- **Pass / Fail**: **PASS**
- **Evidence Reference**: `AcceptanceTests_A06_MultiItemCheckout.cs:37` (`A06_MultiItemBorrow_AtomicRollback_OnFailure`).

### Test ID: T06
- **Date / Environment**: 2026-11-24 / Local Development
- **Objective**: Histories, search, filters, sorting and empty results (API-14–16; UC-04/07–09; BR-14/17).
- **Expected Result**: GET with query parameters performs case-insensitive partial match, filters by status, sorts with stable ID tie-breaker.
- **Observed Result**: Search, filter, and sorting query parameters execute cleanly against database; empty results return `[]`.
- **Pass / Fail**: **PASS**
- **Evidence Reference**: `AcceptanceTests_A07_A09_HistoriesAndSearch.cs:25` (`A09_GlobalSearch_Filter_Sort_Combinations`).

### Test ID: T07
- **Date / Environment**: 2026-11-24 / Local Development
- **Objective**: Avalonia HTTP client, asynchronous refresh and API independence (API-01–16; architecture).
- **Expected Result**: Desktop application operates exclusively as an HTTP client without direct database connections or DbContext references.
- **Observed Result**: Desktop app loads data, registers entities, and performs borrowings via `EquipmentBorrowingApiClient`; `EquipmentBorrowing.Desktop.csproj` contains zero references to `Infrastructure` or EF Core.
- **Pass / Fail**: **PASS**
- **Evidence Reference**: `EquipmentBorrowing.Desktop.csproj:28`, `App.axaml.cs:46`.

### Test ID: T08
- **Date / Environment**: 2026-11-24 / Local Development
- **Objective**: API unreachable vs server/database errors, recovery and restart (API-01–16; BR-18/19).
- **Expected Result**: Client distinguishes unreachable API (no response / connection failure) from 500 server error, displaying safe user-friendly messages without exposing stack traces.
- **Observed Result**: `ApiException.IsConnectionFailure` correctly identifies connection drops; `ApiException.IsServerError` correctly identifies 500 errors.
- **Pass / Fail**: **PASS**
- **Evidence Reference**: `ApiClientTests.cs:134` (`T08_DistinguishConnectionFailureFromServerError`).

---

## 5. Failures, Diagnosis, Correction, and Retest Log

| # | Related Test | Failure Observed | Diagnosis | Correction Applied | Retest Result |
|---|---|---|---|---|---|
| **F-01** | **T01** (OpenAPI / API Host) | `NU1107: Version conflict detected for Microsoft.OpenApi` during `dotnet restore` when configuring OpenAPI docs. | `Swashbuckle.AspNetCore` pulled an incompatible `Microsoft.OpenApi` version against .NET 10's built-in `Microsoft.AspNetCore.OpenApi`. | Removed `Swashbuckle.AspNetCore`; configured native `AddOpenApi()` / `MapOpenApi()` paired with `Scalar.AspNetCore` (`/scalar/v1`) and exported `openapi.json`. | **PASS** — API builds cleanly; `/openapi/v1.json` and `/scalar/v1` return HTTP 200 OK. |
| **F-02** | **T07** (Desktop HTTP Migration) | Build failed with `CS0234` (missing `Models` namespace) and `CS1061` (`HistoryViewModel` missing `LoadCommand`) after removing `EquipmentBorrowing.Infrastructure` from Desktop. | Desktop ViewModels still referenced old namespaces and `HistoryViewModel` lacked an async `[RelayCommand]` `LoadAsync()` for HTTP reload. | Extracted shared DTOs into `EquipmentBorrowing.Application.Models`, updated ViewModels to use `IEquipmentBorrowingApiClient`, and added `LoadAsync()` to `HistoryViewModel.cs`. | **PASS** — `dotnet build` and `dotnet test` pass (**30/30**), with zero EF Core/Infrastructure references in Desktop. |
| **F-03** | **T08** (API Unreachable Behavior) | Launching `EquipmentBorrowing.Desktop` alone showed empty Equipment/Student lists and `"Connection Error: API server is unreachable. Please verify http://localhost:5000 is running."` | Desktop client is strictly decoupled from PostgreSQL; `EquipmentBorrowing.Api` was not running on `http://localhost:5000`. | Confirmed expected T08 failure behavior (safe error message, no crash), then started `EquipmentBorrowing.Api` (`dotnet run --project src/EquipmentBorrowing.Api`) and refreshed. | **PASS** — Desktop connected to `http://localhost:5000`, loaded all Equipment and Student records from PostgreSQL. |
| **F-04** | **T07** (UI Labeling) | Desktop window title still displayed `"Lab Activity 4 (PostgreSQL)"`. | Window title in `MainWindow.axaml` was not updated after migrating from direct PostgreSQL to REST API. | Updated `MainWindow.axaml` title to `"Campus Equipment Borrowing System - Lab Activity 5 (REST API Client)"`. | **PASS** — Rebuilt and verified `dotnet test` (**30/30 Passed**). |

