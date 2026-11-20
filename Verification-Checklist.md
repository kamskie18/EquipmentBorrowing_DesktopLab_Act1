# Verification Checklist & Traceability Matrix
## Campus Equipment Borrowing System — Laboratory Activity 4

This checklist provides the verification records for all acceptance scenarios (**A01 through A19**), tracking setup, actions, expected behavior, observed outcomes, and automated test coverage.

---

### Acceptance Scenario Matrix

| ID | Scenario / Feature | Use Case | Setup & Action | Expected Result | Observed Result | Status |
|---|---|---|---|---|---|---|
| **A01** | Display Equipment Catalog | UC-01 | Open Equipment Catalog view with seeded database. | All items displayed with identifier, name, and availability status. | Correctly listed all equipment with current availability. | VERIFIED |
| **A02** | Filter Available Equipment | UC-02 | Toggle "Show Available Only" checkbox. | Only items with `IsAvailable = true` remain visible. | Filter immediately updates list; unavailable items hidden. | VERIFIED |
| **A03** | Single Item Checkout | UC-03 | Select eligible student, 1 available item, valid return date. | Borrowing record created; item marked unavailable. | Record persisted; item status updated to unavailable. | VERIFIED |
| **A04** | Active Borrowings List | UC-04 | Navigate to Active Borrowings view. | All loans where `ActualReturnDate` is null are shown. | Displays active loans with borrower, item, and due date. | VERIFIED |
| **A05** | Return Equipment | UC-05 | Select an active borrowing record and click Return. | Item marked available; actual return date set to current UTC. | Loan completed; equipment immediately available for checkout. | VERIFIED |
| **A06** | Multi-Item Atomic Checkout | UC-06 | Select student and 2+ available items; confirm checkout. | All items checked out in a single atomic database transaction. | Single commit creates all borrowing rows and updates statuses. | VERIFIED |
| **A07** | Transaction Rollback on Failure | UC-06 | Simulate failure seam during multi-item checkout. | Entire transaction rolls back; zero partial rows created. | Zero changes committed to PostgreSQL; database remains consistent. | VERIFIED |
| **A08** | Ineligible Student Checkout Block | BR-01 | Attempt checkout with ineligible student (`IsAllowedToBorrow = false`). | System rejects checkout with clear message; no records created. | Validation blocks checkout; explains borrower is ineligible. | VERIFIED |
| **A09** | Three-Item Loan Limit Enforcement | BR-09 | Attempt checkout that would exceed 3 active items for one student. | Rejection before commit; prevents student exceeding active loan limit. | Blocked with notification that active loan limit would be exceeded. | VERIFIED |
| **A10** | Unavailable Item Checkout Block | BR-10 | Attempt checkout for item already checked out. | Blocked by application and database partial unique index. | Rejection prevents double-booking; database index protects integrity. | VERIFIED |
| **A11** | Invalid Return Date Block | BR-11 | Set expected return date earlier than or equal to checkout date. | Rejection with explanation that return date must be strictly later. | Form validation blocks submission; provides date guidance. | VERIFIED |
| **A12** | Register Student | UC-12 | Submit new student number and name. | Student persisted in PostgreSQL with default `IsAllowedToBorrow = true`. | Student record saved; appears immediately in student lists. | VERIFIED |
| **A13** | Duplicate Student Number Block | BR-04 | Register a student with an already existing student number. | Rejection via unique constraint; existing student data preserved. | Unique constraint blocks duplicate; user informed of collision. | VERIFIED |
| **A14** | Edit Student Information | UC-13 | Edit student name and contact info; save. | Updates persisted; student number and borrowing history unchanged. | Student details updated; all loan associations preserved. | VERIFIED |
| **A15** | Toggle Student Eligibility | UC-14 | Change student's `IsAllowedToBorrow` flag. | Flag updated; active borrowings and history unaffected. | Eligibility updated; correctly gates subsequent checkout attempts. | VERIFIED |
| **A16** | Register Equipment | UC-15 | Submit unique identifier and equipment name. | Item persisted in PostgreSQL with default `IsAvailable = true`. | Equipment record created; ready for immediate checkout. | VERIFIED |
| **A17** | Duplicate Equipment Identifier Block | BR-08 | Register equipment with an existing identifier. | Rejection via unique constraint; catalog integrity preserved. | Unique constraint blocks duplicate; no partial record created. | VERIFIED |
| **A18** | Edit Equipment Information | UC-16 | Edit equipment name and description. | Updates saved; availability flag and loan history preserved. | Descriptive fields updated without altering operational availability. | VERIFIED |
| **A19** | Borrowing History Search & Filter | UC-07, UC-08, UC-09 | Search by student/equipment name, filter status, sort results. | Accurate matched results sorted as requested; responsive UI. | Multi-parameter search returns correct records; supports clear/reset. | VERIFIED |

---

### Automated Test Suite Summary
- **Test Project**: `tests/EquipmentBorrowing.Tests/AcceptanceTests.cs`
- **Total Tests**: 25
- **Passed**: 25
- **Failed**: 0
- **Execution Command**: `dotnet test`
