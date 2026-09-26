# Lithograph ERP

**Document:** 37_V1_Integration_and_Release_Plan.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Phase:** V1 Integration, Pilot, and Release

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `20_Testing_Strategy.md`
- `21_Deployment_and_Backup.md`
- `22_Logging_Audit_and_Operational_History.md`
- `23_Frontend_Architecture.md`
- `24_Backend_Architecture.md`
- `25_Development_Workflow_for_AI.md`
- `27_Authentication_Implementation_Plan.md`
- `28_Employees_Implementation_Plan.md`
- `29_Clients_Implementation_Plan.md`
- `30_Projects_Implementation_Plan.md`
- `31_Orders_Implementation_Plan.md`
- `32_Calculator_Foundation_Implementation_Plan.md`
- `33_Order_Calculator_Implementation_Plan.md`
- `34_Costs_Implementation_Plan.md`
- `35_Reports_Implementation_Plan.md`
- `36_Dashboard_Implementation_Plan.md`

---

# 1. Purpose

This document defines the final integration, validation, pilot, and release process for Lithograph ERP Version 1.

The goal is to prove that independently implemented modules work correctly together as one production system.

V1 is not ready merely because:

```text
Authentication works

Clients work

Projects work

Orders work
```

individually.

V1 is ready only when the complete Lithograph workflow works reliably end to end.

---

# 2. V1 Release Objective

Lithograph ERP V1 must support the real workflow:

```text
Director Setup
    ↓
Users / Roles / Permissions
    ↓
Employees
    ↓
Client
    ↓
Project
    ↓
Project Team
    ↓
Order
    ↓
Order Calculator
    ↓
Cost Items
    ↓
Checklist
    ↓
Order Completion
    ↓
Reports
    ↓
Dashboard
```

without manual database intervention.

---

# 3. Release Principle

Do not release based on:

```text
"It seems to work."
```

Release based on:

```text
Documented acceptance criteria

Automated tests

Manual workflow verification

Migration verification

Permission verification

Backup verification

Restore verification

Pilot usage
```

---

# 4. V1 Scope Confirmation

Before integration begins, confirm that the implemented V1 matches the approved module scope:

```text
Authentication

Employees

Clients

Projects

Orders

Calculator

Costs

Reports

Dashboard
```

---

# 5. V1 Non-Goals Confirmation

Do not delay release for features intentionally outside V1:

```text
Warehouse

Accounting

Full Finance

CRM

Purchasing

Production Scheduling

Equipment Maintenance

Mobile App

Supplier Portal

Customer Portal

Microservices

Windows Desktop Agent

Direct Explorer Integration

Advanced Notifications

AI Assistant
```

A missing non-goal is not a release blocker.

---

# 6. Integration Phase Sequence

Recommended order:

```text
Task 1
Documentation consistency review

Task 2
Clean database migration test

Task 3
Authentication integration

Task 4
Master-data integration

Task 5
Project workflow integration

Task 6
Order workflow integration

Task 7
Calculator historical integration

Task 8
Cost synchronization integration

Task 9
Report integration

Task 10
Dashboard integration

Task 11
Permission-role matrix testing

Task 12
Concurrency testing

Task 13
Error/recovery testing

Task 14
Backup/restore test

Task 15
Performance baseline

Task 16
Security review

Task 17
Pilot data setup

Task 18
Real-user pilot

Task 19
Bug-fix freeze

Task 20
Production release
```

---

# 7. Task 1 — Documentation Consistency Review

Before final integration, compare implementation against approved documentation.

Review:

```text
Database fields

Business IDs

Statuses

Priorities

Permission codes

API routes

Module ownership

Financial rules

Calculator versioning

Cost rules
```

---

# 8. Terminology Review

Code and UI should consistently use:

```text
Client

Project

Order

Order Type

Checklist Item

Folder Link

Business ID

Project Team
```

Avoid accidental reintroduction of:

```text
Task

Job

Natural Key
```

for established V1 concepts.

---

# 9. Database Schema Review

Compare the actual PostgreSQL schema to:

```text
17_Database_Schema_Overview.md
```

Expected core tables:

```text
auth.users
auth.roles
auth.permissions
auth.user_roles
auth.role_permissions
auth.sessions

employees.employees

clients.clients

projects.projects
projects.project_members

orders.order_types
orders.orders
orders.checklist_items
orders.folder_links

calculator.templates
calculator.template_versions
calculator.order_calculators
calculator.cost_items
```

---

# 10. Expected Core Table Count

The conceptual V1 design contains:

```text
18 core tables
```

If implementation has additional tables, each should have a documented justification.

---

# 11. No Unexpected Infrastructure Tables

Investigate unnecessary custom tables such as:

```text
generic_audit_log

generic_settings

generic_entity_metadata

event_store

message_queue
```

unless deliberately approved.

---

# 12. Task 2 — Clean Migration Test

Create a completely empty PostgreSQL database.

Apply all EF Core migrations from first to last.

---

# 13. Clean Migration Acceptance

The migration chain must:

```text
Apply successfully

Require no manual SQL patches

Create all required schemas

Create all required tables

Create all constraints

Create all indexes

Create Permission definitions/bootstrap support
```

---

# 14. Migration Order

The final migration chain must correctly resolve cross-schema dependencies involving:

```text
Authentication

Employees

Clients

Projects

Orders

Calculator
```

---

# 15. No Development Database Dependency

A clean installation must not depend on:

```text
existing rows

manually-created schemas

old developer data

manual sequence corrections
```

---

# 16. Fresh Installation Test

After clean migration:

```text
auth.users = 0
```

and the application must correctly enter first-run Director setup.

---

# 17. Task 3 — Authentication End-to-End Test

Starting from empty database:

```text
Open ERP
    ↓
Setup required
    ↓
Create Director password
    ↓
director User created
    ↓
Login
    ↓
Dashboard loads
```

---

# 18. Director Bootstrap Verification

Verify:

```text
Username = director

Director Role assigned

User active

Password securely hashed

created_by = null

Only one bootstrap setup possible
```

---

# 19. Director Administration Workflow

Director must be able to:

```text
Create User

Create/edit Role

Assign Permissions

Assign Role to User

Reset User password

Deactivate User

Reactivate User
```

---

# 20. Final Director Protection

Explicitly test:

```text
Only one active Director
→ cannot deactivate
```

and:

```text
Only one active Director
→ cannot remove Director Role
```

---

# 21. Second Director Scenario

Create a second active Director.

Verify one Director can then:

```text
lose Director Role
```

or:

```text
be deactivated
```

without violating the invariant.

---

# 22. Session Test

Verify:

```text
Login

Authenticated navigation

Refresh browser

Session still valid

Logout

Protected routes unavailable
```

---

# 23. Password Reset Session Test

Administrative password reset should invalidate existing Sessions according to Authentication rules.

---

# 24. Task 4 — Employee/User Integration

Create:

```text
Employee A
without User
```

Confirm valid.

---

# 25. Link User Workflow

Create User A.

Link:

```text
Employee A
→ User A
```

Verify one-to-one constraint.

---

# 26. Duplicate Link Protection

Attempt:

```text
Employee B
→ User A
```

must fail.

---

# 27. Activation Independence

Verify:

```text
Deactivate Employee A
```

does not automatically deactivate:

```text
User A
```

---

# 28. Reverse Activation Independence

Deactivate User A.

Verify Employee A remains active.

---

# 29. Task 5 — Client Workflow Integration

Create first Client.

Expected Business ID:

```text
CL-000001
```

---

# 30. Client Business ID Stability

Edit:

```text
Name

Phone

Contact Person
```

Verify:

```text
CL-000001
```

does not change.

---

# 31. Client Deactivation

Deactivate Client.

Verify:

```text
Client remains readable

Historical data remains valid

Client excluded from normal new Project selection
```

---

# 32. Task 6 — Project Workflow Integration

Create active Client.

Create Project.

Expected Business ID pattern:

```text
PRJ-YYYY-000001
```

---

# 33. Project Team Workflow

Assign:

```text
Owner

Assignee

Participants

Observers
```

---

# 34. Multiple Project Roles

Verify one Employee can be:

```text
Owner
+
Participant
```

on the same Project.

---

# 35. Owner Constraint

Attempt second Owner through direct API/concurrent requests.

Database/application must prevent two Owners.

---

# 36. Assignee Constraint

Same for Assignee.

---

# 37. Inactive Historical Employee

Assign Employee to Project.

Deactivate Employee.

Verify Project still displays that Employee with inactive state.

---

# 38. Task 7 — Order Workflow Integration

Create Order Type:

```text
UV Printing
```

Create Order under Project.

Expected Business ID:

```text
ORD-YYYY-000001
```

---

# 39. Order Context Verification

Order must display:

```text
Project

Client derived through Project

Project Owner

Project Assignee

Participants

Observers
```

without duplicated Order-level team records.

---

# 40. Project Team Update Propagation

Change Project Owner.

Reload Order.

Order must display the new Project Owner automatically.

---

# 41. No Order Team Storage

Verify database does not contain an Order-specific team table.

---

# 42. Project Restrictions

Verify no new Order can normally be created in:

```text
Completed Project

Cancelled Project
```

---

# 43. Inactive Order Type Restriction

Deactivate an Order Type.

Verify:

```text
Existing Orders remain readable

New Order cannot normally select inactive Type
```

---

# 44. Order Status Workflow

Verify:

```text
Draft

Active

On Hold

Completed

Cancelled
```

can be handled according to approved rules.

---

# 45. Order Priority

Verify:

```text
Low

Normal

High

Urgent
```

---

# 46. Checklist Workflow

Inside Order:

```text
Add Item

Complete Item

Uncomplete Item

Edit Text

Reorder

Delete Item
```

---

# 47. Checklist Schema Guard

Ensure UI/API did not accidentally reintroduce:

```text
Assignee

Deadline

Priority

Filename

Folder path
```

for Checklist Items.

---

# 48. Folder Link Workflow

Add:

```text
Artwork
\\server\orders\...
```

Verify:

```text
Display

Copy Path

Edit

Delete
```

---

# 49. External Folder Safety

Deleting ERP Folder Link must not affect the external filesystem.

---

# 50. Task 8 — Calculator Foundation Integration

Create Calculator Template:

```text
UV Printing Calculator
```

---

# 51. Draft Version Workflow

Verify:

```text
Draft v1 created

Fields added

Formulas added

Validation runs
```

---

# 52. Invalid Formula Test

Create an invalid formula.

Publication must fail.

---

# 53. Circular Formula Test

Example:

```text
a = b + 1

b = a + 1
```

Publication must fail.

---

# 54. Valid Publication

Fix Template.

Publish:

```text
v1
```

Verify it becomes immutable.

---

# 55. Published Edit Protection

Attempt direct API modification of v1.

Must fail.

---

# 56. New Version Workflow

Create:

```text
v2 Draft
```

from v1.

Modify.

Publish.

Verify v1 remains unchanged.

---

# 57. Order Type Calculator Assignment

Assign:

```text
UV Printing
→ UV Printing Calculator
```

---

# 58. Task 9 — Order Calculator Integration

Create Order A using UV Printing.

Open Calculator.

Verify lazy creation.

---

# 59. Exact Version Binding

If current Published Version is:

```text
v1
```

Order A must store:

```text
template_version_id = v1
```

---

# 60. Calculator Input Workflow

Enter Calculator values.

Save.

Verify:

```text
field_values persisted

Calculated fields correct

Order Selling Price synchronized
```

---

# 61. Backend Authority Test

Manipulate frontend/API request to submit a fake Selling Price.

Backend must ignore/reject it and use Formula Engine result.

---

# 62. Historical Version Integration Test

Mandatory sequence:

```text
Publish v1

Create Order A
Open Calculator A
→ A uses v1

Publish v2

Reopen A
→ A still uses v1

Create Order B
Open Calculator B
→ B uses v2
```

---

# 63. Historical Calculation Reproducibility

Reload Order A after v2 exists.

Its Calculator must still evaluate with v1 rules.

---

# 64. No Silent Upgrade

No application startup, page load, migration, or save operation may automatically upgrade A from v1 to v2.

---

# 65. Calculator Concurrency Integration

User A and User B load same Order Calculator.

User A saves.

User B attempts stale save.

Expected:

```text
409 Conflict
```

User A's data must remain intact.

---

# 66. Calculator Reset Integration

Explicit reset should:

```text
Clear/rebind Calculator as defined

Reset Selling Price to 0
```

but must not affect Costs.

---

# 67. Order Type Change With Calculator

Normal Order edit must reject Order Type change once Calculator exists.

Explicit reset/change workflow must work.

---

# 68. Task 10 — Costs Integration

Add Cost Item:

```text
Material
100,000
```

Verify:

```text
Order.cost_price = 100,000
```

---

# 69. Add Second Cost

Add:

```text
Transport
25,000
```

Expected:

```text
cost_price = 125,000
```

---

# 70. Edit Cost

Change Material:

```text
100,000
→ 120,000
```

Expected:

```text
cost_price = 145,000
```

---

# 71. Delete Cost

Delete Transport.

Expected:

```text
cost_price = 120,000
```

---

# 72. Delete Final Cost

Expected:

```text
cost_price = 0
```

---

# 73. Cost Invariant

For every Order tested:

```text
orders.orders.cost_price
=
SUM(calculator.cost_items.amount)
```

---

# 74. Cost Concurrency

Run concurrent Cost additions/edits.

Verify final Cost Price equals actual row total.

---

# 75. Calculator Reset Cost Preservation

Add Costs.

Reset Calculator.

Verify:

```text
Cost Items remain

Cost Price remains correct

Selling Price resets according to Calculator rule
```

---

# 76. Completed Order Late Cost

Complete an Order.

Add an authorized late Cost correction.

Verify financial data updates correctly.

---

# 77. Cancelled Order Cost Rule

Cancel an Order.

Verify historical Costs remain visible.

Verify new Cost mutations are blocked if that is the approved V1 rule.

---

# 78. Task 11 — Profit Integration

Given:

```text
Selling Price = 500,000

Cost Price = 300,000
```

derived Profit must be:

```text
200,000
```

---

# 79. No Profit Storage

Verify database contains no separate stored Order Profit column.

---

# 80. Task 12 — Reports Integration

Create realistic test data across:

```text
Several Clients

Several Projects

Several Order Types

Several Orders

Different statuses

Different priorities

Different financial values
```

---

# 81. Orders Report

Verify:

```text
Search

Filters

Sorting

Pagination

Summary totals

Financial permissions
```

---

# 82. Pagination Invariant

Changing page must not change full-dataset summary totals.

---

# 83. Project Financial Summary

Verify Project totals equal the sum of included Orders.

---

# 84. Client Financial Summary

Verify aggregation through:

```text
Client
→ Projects
→ Orders
```

---

# 85. Order Type Summary

Verify Orders group by Order Type ID.

---

# 86. Cost Summary

Verify grouping by:

```text
Category

Supplier

Order

Project

Client

Order Type
```

---

# 87. Cancelled Order Financial Rule

Verify default financial summaries exclude Cancelled Orders.

---

# 88. Include Cancelled

Enable Include Cancelled.

Verify totals update accordingly.

---

# 89. Detailed Cancelled Inspection

Verify Users can still explicitly inspect cancelled work where report filters allow.

---

# 90. Task 13 — Dashboard Integration

Verify Dashboard shows correct:

```text
Active

Urgent

Due Soon

Overdue

Recent Orders

Recent Projects
```

---

# 91. Due Soon Boundary

Use controlled test date and verify exact seven-day behavior.

---

# 92. Overdue Boundary

Verify today's deadline is not incorrectly treated as past due.

---

# 93. Closed Order Exclusion

Completed and Cancelled Orders must not appear as overdue/urgent operational work.

---

# 94. Dashboard Drill-Down

Verify:

```text
Dashboard Order
→ Order Workspace

Dashboard Project
→ Project Workspace
```

---

# 95. Task 14 — Permission Role Matrix

Create realistic test roles.

Example conceptual roles:

```text
Director

Operator

Supplier / Cost User

General Employee
```

Exact Role names remain configurable.

---

# 96. Director

Director should have full access.

Test every major section.

---

# 97. Operator-Type Role

A possible test Role may allow:

```text
View Clients

View Projects

View Orders

View Selling Price

Edit assigned operational data as configured
```

but not detailed Costs.

---

# 98. Cost/Supplier-Type Role

A possible test Role may allow:

```text
View Orders

View Cost Items

Edit Cost Items
```

with restricted Selling data.

---

# 99. Restricted Employee Role

Test a User that can:

```text
View Orders
```

but cannot:

```text
See Selling Price

See Cost Price

See Cost Items

Manage Users
```

---

# 100. Permission Matrix Requirement

For each Role, verify both:

```text
Visible UI
```

and:

```text
Direct API access
```

---

# 101. UI Is Not Enough

A hidden button does not prove authorization.

Every restricted endpoint must be tested directly.

---

# 102. Financial Permission Matrix

Test at minimum:

```text
No financial access

Selling only

Cost Price only

Detailed Costs only where appropriate

Selling + Cost

Full Director
```

---

# 103. Profit Visibility Matrix

Profit appears only when User has access to both:

```text
Selling

Cost
```

---

# 104. Reports Permission Matrix

Reports must not reveal financial information unavailable elsewhere.

---

# 105. Dashboard Permission Matrix

Dashboard must not expose hidden Order/Project/financial data.

---

# 106. Task 15 — Business ID Integration

Verify Client numbering:

```text
CL-000001
CL-000002
...
```

---

# 107. Project Yearly Numbering

Verify:

```text
PRJ-2026-000001
```

and year rollover behavior.

---

# 108. Order Yearly Numbering

Verify:

```text
ORD-2026-000001
```

and independent yearly sequence.

---

# 109. Sequence Independence

Client, Project, and Order numbering must not interfere with one another.

---

# 110. Concurrency Numbering

Run parallel Client, Project, and Order creates.

Verify:

```text
No duplicate Business IDs
```

---

# 111. Gap Acceptance

Failed operations may create numbering gaps.

The ERP must continue normally.

Do not attempt to repair/reuse the gap.

---

# 112. Task 16 — Cross-Module Lifecycle Tests

Test realistic lifecycle changes.

---

# 113. Client Deactivation With Projects

Create Client + Project.

Deactivate Client.

Verify:

```text
Project remains valid

Client remains visible historically

New Project cannot normally use inactive Client
```

---

# 114. Employee Deactivation With Project Team

Deactivate assigned Employee.

Verify assignment remains.

---

# 115. Order Type Deactivation With Orders

Existing Orders remain valid.

---

# 116. Template Deactivation With Historical Calculator

Existing Order Calculator remains functional with exact Version.

---

# 117. Project Completion With Open Orders

System should warn according to approved UI behavior.

It must not automatically complete Orders.

---

# 118. Project Cancellation

Must not automatically cancel Orders.

---

# 119. Order Cancellation

Must preserve:

```text
Calculator

Costs

Checklist

Folder Links
```

---

# 120. Task 17 — API Contract Review

Review all major APIs for consistency.

Verify:

```text
/api prefix

HTTP methods

Error codes

Pagination

Filtering

Sorting

DTO naming

401 vs 403

404

409
```

---

# 121. No EF Entities in API

Verify API is not directly serializing persistence entities.

---

# 122. Generated Field Protection

Ensure clients cannot submit:

```text
business_id

created_by

updated_by

password_hash

published_at

cost_price

authoritative selling_price
```

through normal mutation DTOs.

---

# 123. Parent-Child Validation

Re-test:

```text
Checklist Item wrong Order

Folder Link wrong Order

Cost Item wrong Order

Calculator wrong Order
```

Backend must reject cross-parent manipulation.

---

# 124. Task 18 — Error Handling Review

Simulate:

```text
Invalid input

Missing record

Duplicate username

Duplicate Order Type

Inactive Client

Inactive Employee assignment

No Calculator Template

No Published Calculator Version

Concurrency conflict
```

---

# 125. Error Response Shape

Errors must follow the approved common shape:

```text
code

message

errors
```

---

# 126. No Stack Traces

Production-style responses must not expose:

```text
Stack trace

SQL

Constraint names

Filesystem paths

Connection strings
```

---

# 127. Task 19 — Security Review

Perform explicit review of:

```text
Passwords

Sessions

Permissions

Financial field protection

Formula execution

File upload if present

Logging

Configuration secrets
```

---

# 128. Password Review

Verify:

```text
No plaintext passwords stored

No passwords logged

Established ASP.NET hashing used
```

---

# 129. Session Review

Verify secure production cookie settings can be enabled:

```text
HttpOnly

Secure

Appropriate SameSite
```

---

# 130. Session Revocation

Verify:

```text
Logout

User deactivation

Password reset
```

invalidate Sessions correctly.

---

# 131. Formula Security

Attempt expressions resembling:

```text
C# code

SQL

JavaScript

shell commands

filesystem access

network access
```

All must be invalid Calculator formulas.

---

# 132. Frontend Secret Review

Ensure React build contains no:

```text
Database credentials

Signing secrets

Production passwords
```

---

# 133. Logging Review

Search logs/code for accidental sensitive logging.

---

# 134. Task 20 — Backup Test

Before production, create a real PostgreSQL backup using the selected procedure.

---

# 135. Backup Must Include

All ERP database schemas:

```text
auth

employees

clients

projects

orders

calculator
```

---

# 136. Preview Storage

If Preview Images are implemented outside PostgreSQL, back them up separately according to Deployment documentation.

---

# 137. External Folder Links

The ERP only stores references to external production folders.

Those external folders require their own file-server backup.

ERP backup does not back up those folders automatically.

---

# 138. Task 21 — Restore Test

A backup is not considered valid until restored.

---

# 139. Restore Procedure

Use a clean PostgreSQL environment.

Restore the backup.

Start the application against the restored database.

---

# 140. Restore Acceptance

Verify:

```text
Director/User login works

Clients exist

Projects exist

Orders exist

Calculator Versions exist

Historical Order Calculators open

Cost Items exist

Reports totals match
```

---

# 141. Historical Calculator Restore

This is particularly important.

After restore:

```text
Order A
```

must still reference the same exact historical Template Version.

---

# 142. Restore Documentation

Record:

```text
Backup command/process

Restore command/process

Required configuration

Observed result
```

in operational documentation.

---

# 143. Release Blocker

If restore has never been successfully tested:

```text
V1 is not production-ready.
```

---

# 144. Task 22 — Performance Baseline

Performance does not need enterprise-scale optimization.

It does need to be reasonable for Lithograph's expected workload.

---

# 145. Test Dataset

Create a realistic larger dataset, for example:

```text
Hundreds of Clients

Thousands of Projects

Several thousand Orders

Several thousand Cost Items

Multiple Calculator Templates/Versions
```

Exact volume should approximate foreseeable business growth.

---

# 146. Main Performance Areas

Test:

```text
Login

Orders List

Project List

Order Workspace

Calculator Load

Calculator Save

Reports

Dashboard
```

---

# 147. No N+1

Inspect key list/report queries.

---

# 148. Report SQL Review

Review generated SQL/query plans for:

```text
Orders Report

Project Summary

Client Summary

Cost Summary
```

---

# 149. Index Review

Add indexes only when justified by measured queries.

Do not use release preparation as an excuse to add large numbers of speculative indexes.

---

# 150. No Premature Infrastructure

Do not introduce:

```text
Redis

Elasticsearch

Read Replica

Materialized Views

Message Broker
```

unless a measured release-blocking problem requires them.

---

# 151. Task 23 — Browser/UI Review

Test in the primary supported browser environment.

---

# 152. Core Navigation

Verify:

```text
Dashboard

Clients

Projects

Orders

Reports

Administration
```

navigation.

---

# 153. Deep Link Review

Directly open and refresh:

```text
/clients/{id}

/projects/{id}

/orders/{id}

/admin/calculator-templates/{id}
```

---

# 154. Browser Back/Forward

Normal navigation history must work.

---

# 155. Forms Review

Test:

```text
Save

Cancel

Validation

Server error

Duplicate click protection
```

---

# 156. Loading States

Every major API-driven page must show a useful loading state.

---

# 157. Empty States

Verify useful empty states for:

```text
Clients

Projects

Orders

Checklist

Folder Links

Costs

Reports

Dashboard sections
```

---

# 158. Keyboard Review

Basic:

```text
Tab

Shift+Tab

Enter

Escape
```

should work appropriately.

---

# 159. Double Click

Where the table convention uses double-click to open records, verify it works consistently.

Explicit Open action must still exist where necessary.

---

# 160. Tablet Review

Check major pages on reasonable tablet widths.

Desktop remains the primary target.

---

# 161. Task 24 — Pilot Role Setup

Before real pilot, configure a small realistic Role structure.

Do not overbuild roles.

Example:

```text
Director

Manager

Operator

Cost User
```

Only if these match actual Lithograph needs.

---

# 162. Pilot Users

Use a small number of real Users first.

Do not onboard everyone before workflow stability is proven.

---

# 163. Pilot Employees

Create real Employees needed for Project Team tests.

---

# 164. Pilot Data

Create a controlled subset of real:

```text
Clients

Projects

Orders
```

Do not initially migrate every historical record.

---

# 165. Pilot Order Types

Create only actual near-term Order Types.

Example:

```text
UV Printing

CO₂ Laser Cutting

Digital Printing

Graphic Design

Installation

Outsourced Work
```

depending on real workflow.

---

# 166. Pilot Calculator Templates

Start with one or two real frequently used Calculator Templates.

Do not attempt to model every possible Lithograph production type before go-live.

---

# 167. Pilot Workflow

Use V1 for several real Orders from beginning to completion.

For each:

```text
Create Client if needed

Create Project

Assign Team

Create Order

Calculate Selling Price

Add Costs

Track Checklist

Complete Order

Review Reports
```

---

# 168. Pilot Observation

Record actual friction such as:

```text
Too many clicks

Missing filter

Unclear label

Calculator input confusion

Permission problem

Slow report

Missing required field
```

---

# 169. Distinguish Bug From New Feature

Pilot feedback should be classified as:

```text
Bug

Usability issue

Missing V1 requirement

Future enhancement
```

Do not automatically expand V1 for every idea.

---

# 170. Release Blocking Bugs

Examples:

```text
Wrong financial calculations

Permission leakage

Lost Calculator values

Incorrect Business IDs

Cost Price drift

Data loss

Broken migration

Broken restore

Login/security failure
```

These block release.

---

# 171. Important But Non-Blocking UX Issues

Examples:

```text
Minor spacing issue

Non-critical label wording

Table column preference

Optional extra filter
```

may be deferred.

---

# 172. Task 25 — Bug-Fix Period

After pilot begins, prioritize stabilization.

Avoid major architecture changes unless required by a severe flaw.

---

# 173. Bug Fix Rule

For meaningful bugs:

```text
Reproduce

Add regression test where practical

Fix

Run related tests

Run integration path
```

---

# 174. Financial Bug Rule

Any bug affecting:

```text
Selling Price

Cost Price

Profit

Reports
```

requires strong regression testing.

---

# 175. Authentication Bug Rule

Any bug affecting:

```text
Login

Sessions

Permissions

Director protection
```

requires regression testing.

---

# 176. Calculator Historical Bug Rule

Any bug that can change old Orders because a Template changed is a release blocker.

---

# 177. Task 26 — Release Freeze

Before production release, establish a short release freeze.

During freeze:

```text
No new V1 features

Only release-blocking fixes

Documentation updated

Migration chain frozen/reviewed
```

---

# 178. Release Candidate

Create a release candidate version.

Example conceptual tag:

```text
v1.0.0-rc1
```

---

# 179. Release Candidate Verification

Run:

```text
Backend build

Frontend build

Unit tests

Integration tests

Critical E2E tests

Migration-from-empty test

Backup/restore test

Manual smoke test
```

---

# 180. Task 27 — Production Configuration

Prepare production environment according to:

```text
21_Deployment_and_Backup.md
```

---

# 181. Production Requirements

At minimum:

```text
Stable server

PostgreSQL

HTTPS

Production configuration

Secrets outside Git

Backup destination

Persistent storage

Logging

Disk space monitoring
```

---

# 182. Production Database Account

Use a dedicated application database account.

Do not use PostgreSQL superuser for normal ERP operation.

---

# 183. Production Secret Review

Verify real:

```text
Database password

Session/security secrets

Other credentials
```

are not stored in repository.

---

# 184. Production HTTPS

Authentication Session cookies must use appropriate production security settings.

---

# 185. Task 28 — Production Migration

Before deployment:

```text
Create database backup
```

if upgrading an existing environment.

For first production deployment, still validate the migration plan carefully.

---

# 186. Migration Execution

Apply reviewed migrations in controlled manner.

Do not rely on uncontrolled automatic production migration at every application startup unless explicitly approved.

---

# 187. Migration Failure

If migration fails:

```text
Stop deployment

Preserve logs

Do not continue with partially compatible application
```

---

# 188. Application Deployment

Deploy:

```text
Backend

Frontend static build

Configuration
```

using the approved production model.

---

# 189. Task 29 — Production Smoke Test

Immediately after deployment, verify:

```text
Application loads

HTTPS works

Director/User login works

Dashboard loads

Client list loads

Project list loads

Orders load

Calculator opens

Reports load
```

---

# 190. Production Write Smoke Test

Using a controlled test record where appropriate:

```text
Create/edit harmless test record
```

or perform another safe write verification.

Do not contaminate real data unnecessarily.

---

# 191. Production Backup Check

Verify scheduled backup mechanism is active after deployment.

---

# 192. Logging Check

Verify production logs are being written and do not expose secrets.

---

# 193. Health Check

Verify application/database health endpoint.

---

# 194. Task 30 — V1 Release Checklist

V1 is ready only when all applicable items are checked.

```text
[ ] Documentation matches implementation

[ ] Clean migrations succeed

[ ] First-run Director setup works

[ ] Login/logout/session behavior works

[ ] Final Director protection works

[ ] User/Role/Permission Administration works

[ ] Employees work

[ ] Employee/User link works

[ ] Clients work

[ ] Client numbering works

[ ] Projects work

[ ] Project numbering works

[ ] Project Team works

[ ] Orders work

[ ] Order numbering works

[ ] Order Types work

[ ] Checklist works

[ ] Folder Links work

[ ] Calculator Templates work

[ ] Template Versions are immutable

[ ] Formula validation works

[ ] Circular references rejected

[ ] Order Calculator works

[ ] Historical Template Version behavior works

[ ] Selling Price synchronization works

[ ] Calculator concurrency works

[ ] Costs work

[ ] Cost Price synchronization works

[ ] Cost concurrency works

[ ] Calculator reset preserves Costs

[ ] Reports work

[ ] Report totals ignore pagination

[ ] Cancelled-order report behavior works

[ ] Financial permissions work

[ ] Dashboard works

[ ] No unauthorized financial leakage

[ ] Backup succeeds

[ ] Restore succeeds

[ ] Production deployment process tested

[ ] Pilot completed

[ ] Release-blocking bugs fixed
```

---

# 195. Data Integrity Checklist

Verify:

```text
[ ] No duplicate User usernames

[ ] No duplicate Business IDs

[ ] Maximum one Project Owner

[ ] Maximum one Project Assignee

[ ] Maximum one Order Calculator per Order

[ ] Published Template Versions immutable

[ ] Order Calculator exact Version preserved

[ ] Cost Price equals Cost Item SUM

[ ] Profit is derived

[ ] No Order-level team duplication

[ ] No Client duplication on Order
```

---

# 196. Security Checklist

```text
[ ] Password hashes only

[ ] No plaintext passwords

[ ] Session security reviewed

[ ] Password reset invalidates sessions

[ ] User deactivation blocks access

[ ] Backend permissions enforced

[ ] Final Director protected

[ ] Formula engine cannot execute code

[ ] Financial fields permission-protected

[ ] Secrets absent from frontend/repository

[ ] Production errors hide internals
```

---

# 197. Historical Integrity Checklist

```text
[ ] Inactive Client remains visible historically

[ ] Inactive Employee remains visible in Project Team

[ ] Inactive Order Type remains visible historically

[ ] Retired Template Version remains usable historically

[ ] Existing Order Calculator does not auto-upgrade

[ ] Cancelled Order keeps Calculator/Costs/Checklist/Folder Links
```

---

# 198. Financial Integrity Checklist

```text
[ ] Selling Price comes from backend Calculator

[ ] Cost Price comes from Cost Items

[ ] No direct Cost Price editing

[ ] Profit is not stored

[ ] Cost mutation is transactional

[ ] Calculator save + Selling Price is transactional

[ ] Reset preserves Costs

[ ] Reports use Order stored financial values

[ ] Reports do not claim accounting net profit
```

---

# 199. UI Checklist

```text
[ ] Main navigation clear

[ ] Stable deep links

[ ] Loading states

[ ] Error states

[ ] Empty states

[ ] Permission-aware actions

[ ] Explicit Save where required

[ ] Business IDs visible

[ ] Statuses consistent

[ ] Priority consistent

[ ] Financial data hidden correctly

[ ] Project Workspace usable

[ ] Order Workspace usable

[ ] Reports usable
```

---

# 200. Backup Checklist

```text
[ ] Automatic database backup configured

[ ] Backup stored outside primary server/disk

[ ] Retention configured

[ ] Manual pre-deployment backup procedure documented

[ ] Restore successfully tested

[ ] Preview storage backup addressed if applicable

[ ] External production folders have separate backup ownership
```

---

# 201. Known Limitations Document

Before V1 go-live, maintain a short known-limitations section in README or release notes.

Examples:

```text
No accounting

No warehouse

No multi-currency

No full cost history after deletion

No Order status history

No Explorer direct-open

No mobile app
```

This prevents users from confusing intentional V1 scope with defects.

---

# 202. Do Not Hide Limitations

If V1 intentionally does not preserve deleted Cost Item history, document that honestly.

Do not imply accounting-grade audit behavior.

---

# 203. Initial Production Data Strategy

Prefer:

```text
Start with current active data
```

rather than trying to import every historical record immediately.

---

# 204. Historical Data Import

Historical import may be a separate controlled project after V1 stabilizes.

Do not make it a release blocker unless Lithograph actually requires it.

---

# 205. Data Import Rule

If later importing historical data:

```text
Use validated import tooling/scripts

Do not bypass constraints casually

Do not generate fake historical audit metadata without clear convention
```

---

# 206. First Production Users

Start with a small trusted group.

Expand after a short period of stable real usage.

---

# 207. Operational Feedback

During early real usage, prioritize feedback on:

```text
Incorrect data

Missing workflow step

Permission friction

Calculator correctness

Report correctness

Navigation speed
```

---

# 208. Monitoring After Release

Check regularly:

```text
Application availability

Database availability

Disk space

Backup completion

Unexpected errors

Slow report queries
```

---

# 209. No Complex Monitoring Platform Required

Simple reliable monitoring is enough initially.

Do not introduce a large observability stack merely because V1 is released.

---

# 210. Post-Release Bug Priority

Priority 1:

```text
Data loss

Security issue

Financial calculation error

Cannot log in

Cannot create Orders

Database corruption
```

---

# 211. Priority 2

Examples:

```text
Broken report filter

Incorrect dashboard count

Permission UI mismatch

Calculator validation issue without data corruption
```

---

# 212. Priority 3

Examples:

```text
Minor UI inconvenience

Cosmetic inconsistency

Optional enhancement
```

---

# 213. V1.1 Planning

Do not start major V1.1 feature implementation until V1 has enough real operational usage to identify actual needs.

---

# 214. Candidate V1.1 Features

Potential future improvements may include:

```text
Better Calculator Designer

Excel/CSV exports

More Dashboard views

Order comments

Attachments

Status history

Cost history

Automatic folder integration

Supplier master

Additional reports
```

Prioritize based on real usage.

---

# 215. Avoid Immediate Architecture Rewrite

After V1 release, do not replace the architecture simply because a new framework or pattern appears attractive.

Evaluate actual pain points first.

---

# 216. Microservices Rule

Do not split V1 into microservices unless future scale/organization provides a concrete need.

A working modular monolith is the intended architecture.

---

# 217. Database Rule

Do not split the single PostgreSQL database merely because modules are logically separate.

Cross-module transactional consistency is valuable for this ERP.

---

# 218. Calculator Evolution Rule

Preserve immutable historical Template Version behavior in all future Calculator changes.

This is one of the most important long-term invariants.

---

# 219. Financial Evolution Rule

Any future accounting integration must clearly distinguish:

```text
Operational Order Cost/Profit
```

from:

```text
Accounting financial statements
```

---

# 220. Task 31 — Final AI Release Audit

A suitable AI task:

```text
Read:
- AI_RULES.md
- all module implementation documents
- docs/20_Testing_Strategy.md
- docs/21_Deployment_and_Backup.md
- docs/37_V1_Integration_and_Release_Plan.md

Task:
Perform a V1 release-readiness audit of the current repository.

Inspect:
- database schema
- migrations
- backend modules
- API authorization
- frontend routes
- permissions
- Calculator versioning
- Cost synchronization
- Reports
- Dashboard
- tests

Do not:
- refactor unrelated architecture
- add new features
- change approved business rules
- add dependencies unless required to fix a confirmed release blocker

Produce:
1. Release blockers
2. High-priority issues
3. Non-blocking issues
4. Missing tests
5. Documentation inconsistencies
6. Recommended fix order

Then fix only the release blockers and explicitly approved high-priority issues.

Before finishing:
- build backend
- build frontend
- run all tests
- test migrations from empty PostgreSQL
- report exact verification results
```

---

# 221. End-to-End Automated Test Candidate

Create at least one high-value E2E scenario covering:

```text
Login as Director

Create Client

Create Project

Assign Team

Create Order

Open Calculator

Enter pricing inputs

Save Calculator

Add Cost Item

Add Checklist Item

Complete Checklist Item

Open Reports

Verify financial result
```

---

# 222. E2E Scope

Do not attempt to automate every possible UI path.

A few high-value workflows are more useful than hundreds of brittle browser tests.

---

# 223. Core Manual Acceptance Scenario

Before production release, manually complete this exact workflow:

```text
1. Log in as Director.

2. Create an Employee.

3. Create a normal User.

4. Link User to Employee.

5. Create a Client.

6. Create a Project.

7. Assign Owner and Assignee.

8. Create an Order Type.

9. Assign a Published Calculator Template.

10. Create an Order.

11. Open Order Calculator.

12. Enter values and save.

13. Verify Selling Price.

14. Add several Cost Items.

15. Verify Cost Price.

16. Verify Profit.

17. Add Checklist Items.

18. Complete Checklist.

19. Add Folder Links.

20. Complete Order.

21. Open Project Report.

22. Open Client Report.

23. Open Order Type Report.

24. Open Cost Report.

25. Verify Dashboard.

26. Log out.

27. Log in as restricted User.

28. Verify restricted financial access.
```

---

# 224. V1 Definition of Done

Lithograph ERP Version 1 is considered complete when:

```text
The documented core workflow works reliably.

Data integrity is protected.

Permissions are enforced by backend.

Calculator history is preserved.

Financial values are consistent.

Reports are correct.

Backups can be restored.

Real users can operate the system without database/manual developer intervention.
```

---

# 225. What "Ready" Does Not Mean

V1 does not need to be:

```text
Feature-complete forever

Perfectly optimized

Fully automated

Enterprise-scale

Visually elaborate
```

---

# 226. What "Ready" Does Mean

V1 must be:

```text
Reliable

Understandable

Recoverable

Secure enough for its internal use

Financially consistent

Usable for real Lithograph work
```

---

# 227. Release Decision

Release should be based on evidence from:

```text
Tests

Pilot

Backup/restore

Security review

Financial invariants

Real workflow
```

not the number of features implemented.

---

# 228. Final V1 Principle

Lithograph ERP V1 should now form one coherent operational chain:

```text
Authentication
      ↓
Employees
      ↓
Clients
      ↓
Projects
      ↓
Orders
      ↓
Calculator
      ↓
Costs
      ↓
Reports
      ↓
Dashboard
```

Every module has a clear responsibility.

Every important financial value has a defined source.

Every important access decision is enforced by the backend.

Every historical Calculator uses its exact immutable Template Version.

Every production deployment can be recovered from backup.

The central release rule is:

```text
Do not release because all modules exist.

Release when the whole business workflow works.
```

---

**End of Document**