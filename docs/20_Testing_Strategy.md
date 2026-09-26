# Lithograph ERP

**Document:** 20_Testing_Strategy.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `01_Technology_Stack.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `07_Authentication.md`
- `11_Employees_Module.md`
- `12_Clients_Module.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `16_Reports_Module.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `19_API_Design_Guidelines.md`

---

# 1. Purpose

This document defines the testing strategy for Lithograph ERP Version 1.

Its purpose is to ensure that:

```text
Business rules remain correct

Database integrity is protected

Permissions cannot be bypassed

Calculator results remain reliable

Historical data remains stable

New changes do not silently break existing modules
```

Testing should be practical and focused on the parts of the ERP where failure would cause real business problems.

---

# 2. Testing Principle

Lithograph ERP should use several layers of testing.

Conceptually:

```text
Unit Tests
    ↓
Integration Tests
    ↓
API Tests
    ↓
Frontend Component Tests
    ↓
Critical End-to-End Tests
```

Do not rely on only one layer.

---

# 3. Testing Priorities

Priority order:

```text
1. Business rules

2. Security and permissions

3. Database constraints

4. Calculator correctness

5. Historical behavior

6. API behavior

7. User workflow

8. Visual polish
```

---

# 4. What Should Be Tested Most Heavily

The highest-risk areas are:

```text
Authentication

Authorization

Business ID generation

Project Team rules

Order lifecycle

Calculator formulas

Template versioning

Cost synchronization

Selling Price synchronization

Financial data visibility
```

These require strong automated coverage.

---

# 5. What Does Not Need Excessive Testing

Do not create large amounts of low-value tests for:

```text
Simple property getters

Framework internals

Material UI rendering details

Trivial DTO mapping with no logic

EF Core behavior already guaranteed by framework
```

Test Lithograph behavior, not the framework itself.

---

# 6. Backend Test Projects

Recommended structure:

```text
backend/tests/
├── LithographERP.UnitTests/
└── LithographERP.IntegrationTests/
```

Additional projects should be added only if the test suite genuinely becomes difficult to manage.

---

# 7. Unit Tests

Unit Tests should test isolated business logic that does not require the full database or HTTP server.

Examples:

```text
Formula evaluation

Project status rules

Order status rules

Business ID formatting logic

Username normalization

Permission resolution helpers

Calculator dependency detection
```

---

# 8. Unit Test Characteristics

Unit Tests should generally be:

```text
Fast

Deterministic

Independent

Easy to understand

Focused on one behavior
```

---

# 9. Unit Test Naming

Test names should explain behavior.

Good:

```text
CreateProject_RejectsInactiveClient

PublishTemplate_RejectsCircularFormula

DeactivateUser_RejectsFinalActiveDirector
```

Avoid vague names such as:

```text
Test1

ProjectTest

WorksCorrectly
```

---

# 10. Arrange–Act–Assert

A clear test structure is preferred:

```text
Arrange

Act

Assert
```

Exact code style may vary, but tests should remain easy to read.

---

# 11. Integration Tests

Integration Tests verify several real components together.

Typical integration test stack:

```text
ASP.NET Core Application

Entity Framework Core

PostgreSQL Test Database

Real Module Services

Real Database Constraints
```

Mock only external boundaries when necessary.

---

# 12. Why PostgreSQL Integration Tests Matter

Lithograph ERP depends on PostgreSQL-specific behavior such as:

```text
Schemas

UUIDs

Partial Unique Indexes

JSONB

timestamptz

inet

Transactions

Foreign Keys
```

Do not use an in-memory database as the primary substitute for PostgreSQL integration tests.

It may behave differently.

---

# 13. Test Database Strategy

Integration tests should run against an isolated PostgreSQL test database.

Possible approach:

```text
Create clean test database

Apply EF Core migrations

Run tests

Reset data between tests or test groups
```

The exact infrastructure may use containers if convenient.

---

# 14. Testcontainers

Using a PostgreSQL test container is acceptable and often useful.

It provides:

```text
Real PostgreSQL

Isolated test environment

Repeatable setup
```

Do not introduce unnecessary container orchestration beyond what testing requires.

---

# 15. Database Reset

Tests must not depend on the order in which other tests executed.

Each test should start from a known state.

Possible strategies:

```text
Transaction rollback

Database cleanup

Schema recreation

Snapshot/reset tool
```

Choose the simplest reliable option.

---

# 16. Production Database Safety

Automated tests must never point to a production database.

Test configuration should make accidental production connection extremely difficult.

---

# 17. Migration Testing

Integration tests should verify that a fresh database can apply all migrations successfully.

Critical test:

```text
Empty PostgreSQL database
    ↓
Apply all migrations
    ↓
Database becomes valid V1 schema
```

---

# 18. Migration History

Once a migration has been committed and used in shared environments, tests should use that migration history.

Do not silently rewrite historical migrations to make tests pass.

---

# 19. Database Constraint Tests

Important constraints must be tested directly.

Examples:

```text
Duplicate normalized username rejected

Duplicate Client Business ID rejected

Duplicate Project Business ID rejected

Duplicate Order Business ID rejected

Second Project Owner rejected

Second Project Assignee rejected

One Calculator per Order

Duplicate Template version rejected
```

---

# 20. Foreign Key Tests

Test important relationship integrity.

Examples:

```text
Project cannot reference missing Client

Order cannot reference missing Project

Project Member cannot reference missing Employee

Calculator cannot reference missing Order

Cost Item cannot reference missing Order
```

---

# 21. Delete Behavior Tests

Test deliberate delete behavior.

Examples:

```text
Deleting Project Member does not delete Employee

Deleting User Role does not delete User

Deleting Checklist Item does not delete Order

Deactivating Client does not delete Project

Deactivating Employee does not delete Project history
```

---

# 22. Authentication Tests

Authentication tests should include:

```text
Initial Director setup

Successful login

Incorrect password

Unknown username

Inactive User login

Logout

Session invalidation

Password change

Administrator password reset

Password reset invalidates active Sessions
```

---

# 23. First-Run Setup Tests

Critical scenarios:

```text
No users exist
→ Initial Setup available

Initial Setup creates director

Director receives Director Role

Password is hashed

Setup no longer available after first User exists
```

---

# 24. Username Tests

Test:

```text
aram

Aram

ARAM
```

as equivalent usernames.

A second account with a case variation must be rejected.

---

# 25. Password Security Tests

Tests should verify behavior, not expose secrets.

Examples:

```text
Stored value is not plaintext

Correct password verifies

Incorrect password fails
```

Do not write tests that log password hashes.

---

# 26. Final Director Protection

Critical tests:

```text
Only one active Director exists
→ cannot deactivate them

Only one active Director exists
→ cannot remove Director Role

Second active Director exists
→ one Director may be deactivated

Second active Director exists
→ Director Role may be removed from one
```

---

# 27. Permission Tests

Authorization tests should verify:

```text
User with permission succeeds

User without permission receives 403

Unauthenticated User receives 401
```

for important endpoints.

---

# 28. Financial Permission Tests

Critical security tests:

User without Selling Price permission must not receive:

```text
selling_price

selling_total
```

User without Cost Price permission must not receive:

```text
cost_price

cost_items

cost_total
```

User without both must not receive Profit.

---

# 29. Frontend Hiding Is Not Enough

Tests must make direct API calls using unauthorized Users.

Do not rely only on checking whether a React button is hidden.

---

# 30. Employee Tests

Important Employee tests:

```text
Create Employee

Create Employee without User

Create Employee with User

Prevent one User linking to two Employees

Unlink User

Deactivate Employee

Reactivate Employee

Inactive Employee excluded from new assignment lists

Historical assignments remain
```

---

# 31. Employee–User Independence Tests

Test:

```text
Deactivate Employee
→ linked User remains unchanged

Deactivate User
→ linked Employee remains unchanged
```

This protects the deliberate separation between Employee and User.

---

# 32. Client Tests

Important Client tests:

```text
Create Client

Generate Business ID

Business ID immutable

Deactivate Client

Reactivate Client

Inactive Client rejected for new Project

Historical Projects remain accessible
```

---

# 33. Project Tests

Important Project tests:

```text
Create Project

Generate yearly Business ID

Project requires Client

Inactive Client rejected

Set Owner

Replace Owner

Prevent two Owners

Set Assignee

Prevent two Assignees

Add Participant

Add Observer

Same Employee may be Owner and Assignee

Inactive Employee rejected for new assignment
```

---

# 34. Project Team Historical Tests

Scenario:

```text
Employee assigned as Owner

Employee later deactivated

Project still shows Employee as Owner
```

This must remain valid.

---

# 35. Project Status Tests

Test:

```text
Draft

Active

On Hold

Completed

Cancelled
```

Ensure invalid status values are rejected.

---

# 36. Order Tests

Important Order tests:

```text
Create Order

Generate Business ID

Order requires Project

Order requires Order Type

Inactive Order Type rejected

Cancelled Project rejected

Completed Project rejected

Status default = Draft

Priority default = Normal

Selling Price default = 0

Cost Price default = 0
```

---

# 37. Order Project Context Tests

Verify:

```text
Order Client
=
Order.Project.Client
```

There must be no duplicated Client relationship on Order.

---

# 38. Order Team Tests

Verify:

```text
Order Project Team
=
Order.Project.ProjectTeam
```

and:

```text
No orders.order_members rows exist
```

---

# 39. Order Type Tests

Test:

```text
Create Order Type

Case-insensitive duplicate Name rejected

Deactivate Order Type

Inactive Order Type unavailable for new Orders

Historical Orders still load
```

---

# 40. Checklist Tests

Test:

```text
Add Item

Edit Text

Complete Item

Uncomplete Item

Reorder Items

Delete Item

Empty Checklist valid
```

---

# 41. Checklist Simplicity Test

Ensure Checklist Item API/database does not require:

```text
Employee

Deadline

Priority

Folder Link
```

Version 1 must remain simple.

---

# 42. Checklist Progress Tests

Examples:

```text
0 of 0
→ appropriate empty state

2 of 4
→ 50%

4 of 4
→ 100%
```

If percentage is displayed, handle zero items safely.

---

# 43. Folder Link Tests

Test:

```text
Add Folder Link

Edit Folder Link

Reorder Folder Links

Delete Folder Link
```

Deleting a Folder Link must not interact with the actual filesystem.

---

# 44. Calculator Unit Tests

The Formula Engine requires extensive Unit Tests.

This is one of the highest-value test areas.

---

# 45. Arithmetic Tests

Test:

```text
1 + 2

5 - 3

4 * 2

10 / 2

Operator precedence

Parentheses

Decimals
```

---

# 46. Formula Field Reference Tests

Example:

```text
width_mm = 1000
height_mm = 500

area =
width_mm * height_mm
```

Verify correct result.

---

# 47. Function Tests

Each approved function should have focused tests.

Examples:

```text
ROUND

MIN

MAX

ABS

CEILING

FLOOR

IF
```

Do not add a Formula Function without tests.

---

# 48. Formula Error Tests

Test:

```text
Unknown Field

Invalid Syntax

Division by Zero

Circular Reference

Unsupported Function

Invalid argument count
```

Errors must be controlled and understandable.

---

# 49. Circular Dependency Tests

Examples:

```text
a = b + 1
b = a + 1
```

must be rejected.

Also test longer cycles:

```text
a → b → c → a
```

---

# 50. Formula Security Tests

Ensure Formula definitions cannot execute:

```text
SQL

JavaScript

C#

Shell Commands

File Access

Network Calls
```

The parser must reject unsupported syntax.

---

# 51. Decimal Precision Tests

Test financial calculations using decimal values where binary floating-point would normally cause issues.

Example:

```text
0.1 + 0.2
```

must behave according to decimal arithmetic expectations.

---

# 52. Template Tests

Important Template tests:

```text
Create Template

Create Version 1 Draft

Edit Draft

Validate Draft

Publish Draft

Published Version immutable

Create next Draft

Version number increments

Retire Version
```

---

# 53. Template Publication Tests

Publication must fail when:

```text
Formula invalid

Field key duplicate

Unknown field reference

Circular reference

Required output missing
```

---

# 54. Historical Template Tests

Critical scenario:

```text
Create Template v1

Publish v1

Create Order A

Order A uses v1

Create/publish v2

Open Order A

Order A still uses v1

Create Order B

Order B uses v2
```

This test must exist before Calculator is considered complete.

---

# 55. Template Assignment Tests

Changing an Order Type's Template must affect:

```text
Future Order Calculators
```

but must not affect:

```text
Existing Order Calculators
```

---

# 56. Order Calculator Tests

Test:

```text
Calculator created from correct Template

Calculator stores exact Template Version

Field values persist

Calculated values correct

Selling Price synchronized

Reload returns same values
```

---

# 57. One Calculator per Order Test

Attempt to create two Calculator records for the same Order.

Database must reject this.

---

# 58. Calculator Backend Authority Test

Send a manipulated frontend request attempting to directly force final Selling Price.

Backend must calculate authoritative value instead of trusting client result.

---

# 59. Calculator Reset Tests

If Reset Calculator is implemented, test:

```text
Field values removed

Calculator recreated correctly

Cost Items preserved

Cost Price preserved

Selling Price follows documented reset rule
```

---

# 60. Cost Item Tests

Important Cost tests:

```text
Add Cost Item

Edit Cost Item

Delete Cost Item

Reject negative Amount

Allow optional Supplier

Allow optional Expense Date

Correct Sort Order
```

---

# 61. Cost Price Synchronization Tests

Critical invariant:

```text
Order Cost Price
=
SUM(Cost Items)
```

Test after:

```text
Add

Edit

Delete
```

operations.

---

# 62. Cost Transaction Tests

Simulate failure during:

```text
Insert Cost Item
+
Update Cost Price
```

The transaction must not leave inconsistent data.

---

# 63. Selling Price Transaction Tests

Simulate failure during:

```text
Save Calculator
+
Update Selling Price
```

The transaction must preserve consistency.

---

# 64. Report Tests

Reports should be tested using known datasets.

Verify exact expected totals.

---

# 65. Project Financial Report Test

Given Orders:

```text
Order A
Selling = 100
Cost = 60

Order B
Selling = 200
Cost = 100
```

Expected:

```text
Selling Total = 300

Cost Total = 160

Profit = 140
```

---

# 66. Client Financial Report Test

Create:

```text
One Client

Two Projects

Multiple Orders
```

Verify all relevant Orders aggregate correctly.

---

# 67. Order Type Report Test

Create several Orders across multiple Order Types.

Verify grouping by Order Type.

---

# 68. Cancelled Orders Reporting Test

Test both:

```text
Exclude Cancelled
```

and:

```text
Include Cancelled
```

behavior.

---

# 69. Pagination Total Test

If a filtered report has:

```text
120 rows
```

and page size is:

```text
50
```

summary totals must use all 120 filtered rows, not just the visible 50.

---

# 70. API Contract Tests

Integration tests should verify:

```text
Expected status code

Expected response shape

Required fields

Sensitive fields absent
```

---

# 71. Sensitive Field Absence Tests

Explicitly verify normal API responses never contain:

```text
password_hash

token_hash

normalized_username when unnecessary

internal security secrets
```

---

# 72. Not Found Tests

Test:

```text
Unknown Client

Unknown Project

Unknown Order

Unknown Employee

Unknown Calculator Template
```

returns:

```text
404
```

where appropriate.

---

# 73. Validation Response Tests

Verify validation responses use the standard format defined in:

```text
19_API_Design_Guidelines.md
```

---

# 74. Conflict Response Tests

Examples:

```text
Duplicate username

Second Project Owner

Stale Calculator update
```

should return:

```text
409
```

where the API design specifies conflict behavior.

---

# 75. Pagination Tests

Verify:

```text
Default page size

Custom page size

Maximum page size

Total item count

Total pages

Empty last page behavior
```

---

# 76. Filtering Tests

Test combinations such as:

```text
Status + Client

Status + Order Type

Date Range + Project

Priority + Assignee
```

---

# 77. Sorting Tests

Verify only approved sort fields are accepted.

Invalid sort fields must not allow arbitrary SQL/expression injection.

---

# 78. Search Tests

Search should find expected records by:

```text
Business ID

Name
```

and module-specific supported fields.

---

# 79. Frontend Tests

Frontend tests should focus on useful behavior.

Examples:

```text
Form validation

Permission-dependent actions

Loading states

Error display

Critical component interactions
```

Do not attempt to snapshot-test the entire UI.

---

# 80. Frontend Test Tools

Use tools appropriate for the React ecosystem.

The exact library may be selected during implementation.

Prefer mainstream, maintained tools.

Do not introduce several overlapping test frameworks.

---

# 81. Component Tests

Useful component scenarios:

```text
Login form validation

Client form required Name

Project Team selector

Checklist interaction

Calculator field rendering

Cost table editing
```

---

# 82. Permission UI Tests

Examples:

```text
User lacks users.view
→ Users navigation hidden

User lacks calculator.view_costs
→ Cost section hidden

User lacks orders.view_selling_price
→ Selling Price hidden
```

Backend authorization still requires separate tests.

---

# 83. Form Error Tests

When backend returns:

```text
VALIDATION_ERROR
```

frontend should display relevant messages near fields where appropriate.

---

# 84. Loading State Tests

Verify:

```text
Create button disabled while creating

Save button prevents duplicate submission

Loading indicator displayed
```

for important actions.

---

# 85. End-to-End Tests

End-to-End tests should cover only critical business workflows.

Do not attempt to automate every UI click in the ERP.

---

# 86. Critical E2E Scenario 1 — First Run

```text
Start fresh system

Initial Setup appears

Set Director password

Director account created

Login succeeds

Dashboard loads
```

---

# 87. Critical E2E Scenario 2 — Business Setup

```text
Director logs in

Creates Employee

Creates Client

Creates Project

Assigns Owner

Creates Order Type
```

---

# 88. Critical E2E Scenario 3 — Order Workflow

```text
Create Order

Open Order Workspace

Add Checklist Items

Add Folder Link

Change Priority

Change Status
```

---

# 89. Critical E2E Scenario 4 — Calculator Workflow

```text
Create Calculator Template

Publish Version

Assign Template to Order Type

Create Order

Open Calculator

Enter values

Selling Price calculated

Reload Order

Values remain
```

---

# 90. Critical E2E Scenario 5 — Cost Workflow

```text
Open Order

Add Cost Rows

Cost Price updates

Edit one Cost

Cost Price updates

Delete Cost

Cost Price updates
```

---

# 91. Critical E2E Scenario 6 — Reports

```text
Complete several Orders

Open Reports

Filter by Client

Verify Order rows

Verify Selling Total

Verify Cost Total

Verify Profit
```

---

# 92. Critical E2E Scenario 7 — Permission Security

```text
Login as restricted User

Open Orders

Selling Price visible if allowed

Cost Price hidden

Direct Cost API request rejected

Reports also hide Cost
```

---

# 93. E2E Test Count

Keep E2E suite small.

A small number of reliable critical workflows is better than hundreds of fragile UI tests.

---

# 94. Manual Testing

Automated testing does not eliminate manual testing.

Before important releases, manually inspect:

```text
Major Workspaces

Forms

Navigation

Permissions

Calculator UX

Reports

Realistic data
```

---

# 95. Pilot Testing

Real Lithograph users should test the application with actual workflows before full production adoption.

Pilot testing can reveal problems automated tests cannot:

```text
Confusing labels

Slow workflows

Missing fields

Unnecessary clicks

Poor Calculator layout
```

---

# 96. Bug Reproduction Rule

When a bug is discovered:

```text
1. Reproduce it.

2. Add a failing automated test when practical.

3. Fix the bug.

4. Verify the test passes.

5. Run related regression tests.
```

---

# 97. Regression Tests

Every serious bug should ideally result in a test preventing recurrence.

This is especially important for:

```text
Permissions

Financial calculations

Business IDs

Template history

Data loss
```

---

# 98. Test Data

Use clearly fake development/test data.

Examples:

```text
Test Client A

Test Project A

Test Employee A
```

Do not copy real confidential Client data into automated test fixtures.

---

# 99. Test Builders and Fixtures

As tests grow, simple helper builders may be created.

Example:

```text
CreateActiveClient()

CreateEmployee()

CreateProject()

CreateOrder()
```

Do not create a huge generic test framework before repetition justifies it.

---

# 100. Deterministic Tests

Tests must not depend unnecessarily on:

```text
Current local time

Random execution order

External internet

Production files

Real user accounts
```

---

# 101. Time Testing

Where behavior depends on date/year, inject or abstract current time when necessary.

This is especially important for:

```text
Yearly Business ID numbering

Session expiration

Template publication timestamps
```

Do not make tests depend directly on the actual current year.

---

# 102. Business ID Year Tests

Test boundary:

```text
December 31, 2026
→ PRJ-2026-...

January 1, 2027
→ PRJ-2027-000001
```

using controlled test time.

---

# 103. Business ID Concurrency Tests

Simulate multiple concurrent creates.

Verify:

```text
No duplicates

All successful records unique

Sequence gaps acceptable
```

---

# 104. Session Expiration Tests

Use controlled time to verify:

```text
Valid before expires_at

Invalid after expires_at
```

without waiting in real time.

---

# 105. External Dependencies

Version 1 has very few external dependencies.

Tests must not require:

```text
Microsoft Excel

External email server

External authentication provider

Network file server
```

---

# 106. File Path Tests

Folder Link tests should treat paths as strings.

They should not require the actual Windows/network folder to exist.

---

# 107. Preview Image Tests

If Preview Image upload is implemented, test:

```text
Allowed type

Rejected type

Maximum size

Missing file

Updated image reference
```

Do not depend on real production storage.

---

# 108. Performance Tests

Heavy load testing is not required for early Version 1.

However, basic performance checks should cover realistic datasets.

---

# 109. Realistic Dataset

Useful test scale:

```text
Hundreds of Clients

Thousands of Projects

Thousands to tens of thousands of Orders

Many Cost Items
```

This is enough to expose obviously inefficient queries.

---

# 110. Query Count Review

For important list screens, inspect for N+1 problems.

Examples:

```text
Orders list

Projects list

Reports
```

One row should not trigger many extra queries.

---

# 111. Search Performance

Business ID lookup should remain fast.

Examples:

```text
ORD-2026-000425

PRJ-2026-000125

CL-000125
```

---

# 112. Formula Performance

Calculator formulas should evaluate quickly for normal Templates.

No distributed computation or background engine is required.

---

# 113. CI

When practical, use continuous integration to run:

```text
Backend build

Backend tests

Frontend build

Frontend tests
```

on commits/pull requests.

---

# 114. CI Database

Integration tests in CI should use an isolated PostgreSQL instance/container.

Do not share a persistent database across unrelated CI runs.

---

# 115. Test Failure Rule

A failing test must not be ignored simply because AI-generated code appears correct.

Investigate the failure.

Update a test only when the intended business behavior has genuinely changed.

---

# 116. AI Testing Rule

When Cursor/Claude implements a task, the prompt should require:

```text
Add tests

Run relevant tests

Report failures

Do not disable existing tests

Do not weaken assertions just to pass
```

---

# 117. Do Not Delete Tests Casually

AI must not remove an existing test because new code fails it unless:

```text
Business behavior intentionally changed

Documentation was updated

Replacement coverage exists
```

---

# 118. Test Coverage Percentage

Version 1 does not require chasing a specific global coverage percentage.

A high coverage number does not guarantee correct ERP behavior.

Prioritize important business paths.

---

# 119. Coverage Goal

Coverage should be strong around:

```text
Business rules

Security

Calculator

Financial synchronization

Historical behavior
```

Less critical boilerplate may have lower coverage.

---

# 120. Release Test Checklist

Before a production release, verify:

```text
Build succeeds

Unit tests pass

Integration tests pass

Critical E2E tests pass

Migrations apply successfully

Authentication works

Permissions reviewed

Financial calculations reviewed

Backup procedure available
```

---

# 121. Smoke Test After Deployment

Immediately after deployment, verify:

```text
Application opens

Login works

Database reachable

Client list loads

Project list loads

Order list loads

Calculator opens

Reports load
```

---

# 122. Database Backup Before Major Migration

Before significant production schema migrations:

```text
Create backup

Verify backup exists

Apply migration

Run smoke test
```

---

# 123. Rollback Thinking

Before high-risk deployments, understand:

```text
How code can be rolled back

Whether database migration is backward compatible

How backup can be restored
```

Do not improvise recovery only after failure occurs.

---

# 124. Testing Documentation

Tests themselves are the primary technical documentation of executable behavior.

Important business rules should also remain documented in the Markdown specifications.

The two should agree.

---

# 125. Example Test Pyramid

Conceptually:

```text
            ┌─────────────┐
            │   E2E Tests │
            │    Few      │
            └──────┬──────┘
                   │
          ┌────────▼────────┐
          │ Integration/API │
          │     Many        │
          └────────┬────────┘
                   │
             ┌─────▼─────┐
             │ Unit Tests │
             │   Many     │
             └───────────┘
```

The exact number is less important than useful coverage.

---

# 126. Module Completion Rule

A module is not complete until:

```text
Business logic implemented

Database constraints implemented

Permissions implemented

Relevant tests pass

Integration with existing modules works
```

UI alone does not make a module complete.

---

# 127. Authentication Completion Tests

Before Authentication is considered complete:

```text
First-run Director

Login

Logout

Password Change

Reset Password

Role assignment

Permissions

Final Director protection

Session invalidation
```

must pass.

---

# 128. Projects Completion Tests

Before Projects is complete:

```text
Business ID

Client validation

Project Team

Owner uniqueness

Assignee uniqueness

Inactive Employee behavior
```

must pass.

---

# 129. Orders Completion Tests

Before Orders is complete:

```text
Business ID

Project relationship

Order Type validation

Statuses

Priorities

Checklist

Folder Links

Project Team inheritance
```

must pass.

---

# 130. Calculator Completion Tests

Before Calculator is complete:

```text
Formula engine

Template validation

Version publishing

Historical version lock

Selling Price calculation

Cost synchronization

Permission security
```

must pass.

---

# 131. Reports Completion Tests

Before Reports is complete:

```text
Totals

Filters

Pagination

Cancelled behavior

Financial permissions
```

must pass.

---

# 132. Version 1 Testing Non-Goals

Version 1 does not require:

```text
Massive Selenium suite

Full visual regression platform

Chaos engineering

Distributed load testing

Penetration-testing infrastructure

Mutation testing

100% code coverage target

Production traffic replay
```

These may be considered later if scale or risk changes.

---

# 133. Security Testing

Although Version 1 does not require a full enterprise security testing platform, manually and automatically verify common risks such as:

```text
Broken authorization

SQL injection

Mass assignment

Sensitive data exposure

Session misuse

Invalid file upload
```

where applicable.

---

# 134. OWASP Awareness

Implementation should follow current mainstream web security practices.

Security tests should focus on actual Lithograph attack surfaces rather than blindly implementing a large compliance checklist.

---

# 135. Final Testing Principle

Testing exists to protect Lithograph's business behavior.

The most important questions are:

```text
Can unauthorized Users see or change protected data?

Can financial values become inconsistent?

Can historical Orders change unexpectedly?

Can duplicate identifiers appear?

Can one broken change damage another module?
```

The test suite should provide confidence that the answers remain:

```text
No
```

as the ERP evolves.

---

**End of Document**