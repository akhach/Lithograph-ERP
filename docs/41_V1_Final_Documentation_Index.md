# Lithograph ERP

**Document:** 41_V1_Final_Documentation_Index.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Area:** Documentation Governance / AI Development Index

---

# 1. Purpose

This document is the authoritative index for Lithograph ERP Version 1 documentation.

It defines:

```text
Which documents exist

What each document governs

Which documents are authoritative

How documentation conflicts are resolved

What AI coding agents must read

Which documents are considered frozen

Which documents may evolve during implementation
```

This file should be one of the first documents read by any developer or AI agent working on Lithograph ERP.

---

# 2. Documentation Philosophy

Lithograph ERP documentation is intentionally divided into layers.

The main layers are:

```text
Product Vision

Technology / Architecture

Database / Data Rules

Module Specifications

Implementation Plans

Testing / Deployment / Operations

AI Development Rules

Release Governance
```

No single file should attempt to contain the entire ERP specification.

---

# 3. Documentation Precedence

When two documents appear to conflict, use the following precedence:

```text
1. Current explicit human instruction

2. AI_RULES.md

3. Latest approved module or implementation specification

4. Core data / architecture documents

5. Older documentation

6. Existing code

7. AI/developer assumption
```

---

# 4. Human Instruction Is Highest Priority

If the project owner explicitly changes a requirement, that instruction takes precedence over existing documentation.

However:

```text
Documentation should then be updated
```

so future implementation does not depend on chat history.

---

# 5. AI_RULES.md

`AI_RULES.md` governs how AI agents must work.

It does not normally define detailed business requirements.

It defines rules such as:

```text
Read documentation before editing

Do not invent fields

Do not expand scope

Do not rewrite applied migrations

Do not introduce unnecessary architecture

Preserve approved domain boundaries

Build/test after changes
```

---

# 6. Latest Specific Document Wins

If a general architecture document says something broad and a later approved module implementation plan defines a more specific behavior:

```text
the more specific approved implementation plan wins
```

provided it does not contradict a later explicit human decision.

---

# 7. Existing Code Is Not Automatically the Specification

If code conflicts with approved documentation:

```text
do not assume code is correct
```

Determine whether:

```text
documentation changed after code

code contains a bug

documentation is outdated
```

Then reconcile deliberately.

---

# 8. AI Must Not Resolve Material Conflicts by Guessing

If two authoritative documents materially disagree:

```text
identify the conflict
```

and follow the highest-priority source.

If the correct rule remains genuinely unclear and implementation would be destructive or architectural:

```text
stop that specific change
```

rather than inventing a rule.

---

# 9. Documentation Structure

The V1 documentation set is:

```text
00_Project_Vision.md
01_Technology_Stack.md
02_Architecture.md
03_Database_Design.md
04_Data_Dictionary.md
05_Numbering_System.md
06_UI_UX_Principles.md
07_Authentication.md
08_Auth_Schema_Design.md
09_Auth_Tables.md
10_Users_Module.md
11_Employees_Module.md
12_Clients_Module.md
13_Projects_Module.md
14_Orders_Module.md
15_Calculator_Module.md
16_Reports_Module.md
17_Database_Schema_Overview.md
18_Implementation_Roadmap.md
19_API_Design_Guidelines.md
20_Testing_Strategy.md
21_Deployment_and_Backup.md
22_Logging_Audit_and_Operational_History.md
23_Frontend_Architecture.md
24_Backend_Architecture.md
25_Development_Workflow_for_AI.md
26_Initial_Project_Setup.md
27_Authentication_Implementation_Plan.md
28_Employees_Implementation_Plan.md
29_Clients_Implementation_Plan.md
30_Projects_Implementation_Plan.md
31_Orders_Implementation_Plan.md
32_Calculator_Foundation_Implementation_Plan.md
33_Order_Calculator_Implementation_Plan.md
34_Costs_Implementation_Plan.md
35_Reports_Implementation_Plan.md
36_Dashboard_Implementation_Plan.md
37_V1_Integration_and_Release_Plan.md
38_V1_Role_and_Permission_Matrix.md
39_V1_Data_Seed_and_Initial_Configuration.md
40_V1_Error_Codes_and_User_Messages.md
41_V1_Final_Documentation_Index.md
```

---

# 10. Root Documents

At repository root:

```text
README.md

AI_RULES.md
```

These should not be buried inside `/docs`.

---

# 11. README.md

Purpose:

```text
Project orientation

Development entry point

How to run the system

High-level architecture

Links to documentation

Build/test/deployment basics
```

README should remain concise.

It is not the full ERP specification.

---

# 12. AI_RULES.md

Purpose:

```text
Mandatory operating rules for AI coding agents
```

Every AI coding task must consider this file authoritative.

---

# 13. Category A — Product Vision

## `00_Project_Vision.md`

Defines:

```text
What Lithograph ERP is

Why it exists

Core business workflow

V1 scope

Major non-goals
```

Use this document to understand the product before making architectural decisions.

---

# 14. Category B — Core Technical Architecture

## `01_Technology_Stack.md`

Defines the locked V1 technology stack:

```text
C#

ASP.NET Core REST API

EF Core

PostgreSQL

React

TypeScript

Material UI
```

Do not replace these technologies during V1 without explicit approval.

---

# 15. `02_Architecture.md`

Defines high-level architecture.

Core V1 rule:

```text
Modular Monolith
```

Not:

```text
Microservices
```

---

# 16. `03_Database_Design.md`

Defines global database standards such as:

```text
PostgreSQL naming

UUID primary keys

Foreign-key practices

Soft-delete policy

Audit conventions

JSONB usage

Money/date types

Migration policy
```

Read before any schema change.

---

# 17. `04_Data_Dictionary.md`

Defines canonical business fields and meanings.

Use it to avoid creating inconsistent names or meanings across modules.

---

# 18. `05_Numbering_System.md`

Defines Business ID rules for:

```text
Clients

Projects

Orders
```

Canonical formats:

```text
CL-000001

PRJ-YYYY-000001

ORD-YYYY-000001
```

This document is authoritative for numbering semantics.

---

# 19. Category C — UI / API / Testing Standards

## `06_UI_UX_Principles.md`

Defines general UI behavior:

```text
Navigation

Workspaces

Tables

Forms

Save behavior

Loading states

Empty states

Permission-aware UI
```

---

# 20. `19_API_Design_Guidelines.md`

Defines REST API conventions:

```text
/api prefix

HTTP methods

Pagination

Filters

Sorting

DTO usage

Error behavior

Status codes

Authorization expectations
```

---

# 21. `20_Testing_Strategy.md`

Defines V1 testing approach:

```text
Unit tests

PostgreSQL integration tests

API tests

Frontend tests

High-value E2E tests

Concurrency tests

Migration tests

Controlled clock/time
```

---

# 22. Category D — Authentication

## `07_Authentication.md`

Defines authentication behavior:

```text
Local username/password

No email login

No public registration

First-run Director

Sessions

Director protection
```

---

# 23. `08_Auth_Schema_Design.md`

Defines authentication schema relationships.

---

# 24. `09_Auth_Tables.md`

Defines detailed Authentication database tables:

```text
auth.users

auth.roles

auth.permissions

auth.user_roles

auth.role_permissions

auth.sessions
```

---

# 25. `10_Users_Module.md`

Defines User administration requirements.

---

# 26. `27_Authentication_Implementation_Plan.md`

Defines the concrete implementation sequence for Authentication.

When coding Authentication, this is one of the most specific authoritative documents.

---

# 27. `38_V1_Role_and_Permission_Matrix.md`

Defines the canonical V1 Permission catalog and access-control semantics.

Read this whenever touching:

```text
Authorization

Role management

Financial visibility

Frontend permission guards

Reports security
```

---

# 28. `39_V1_Data_Seed_and_Initial_Configuration.md`

Defines:

```text
System seed data

Permission synchronization

Director Role initialization

First-run Director account

Production initialization

What business data must NOT be seeded
```

---

# 29. Category E — Employees

## `11_Employees_Module.md`

Defines Employee domain behavior.

Critical rule:

```text
User ≠ Employee
```

---

# 30. `28_Employees_Implementation_Plan.md`

Defines concrete Employee implementation.

Important rules include:

```text
Employee may exist without User

Employee/User optional one-to-one link

Activation independent

No Departments table

No payroll
```

---

# 31. Category F — Clients

## `12_Clients_Module.md`

Defines Client behavior.

---

# 32. `29_Clients_Implementation_Plan.md`

Defines Client implementation including:

```text
CL Business IDs

Activation

Search

Selectors

CRUD behavior

No hard delete
```

---

# 33. Category G — Projects

## `13_Projects_Module.md`

Defines Project domain and Project Team.

---

# 34. `30_Projects_Implementation_Plan.md`

Defines concrete Project implementation.

Critical rules:

```text
Project belongs to one Client

Project Team lives at Project level

Owner

Assignee

Participants

Observers

One Owner max

One Assignee max

Same Employee may hold multiple roles

Orders inherit Project Team logically
```

---

# 35. Category H — Orders

## `14_Orders_Module.md`

Defines the central Order domain.

---

# 36. `31_Orders_Implementation_Plan.md`

Defines concrete Order implementation.

Critical rules:

```text
Order belongs to Project

Client derived through Project

No Order team

Order Type configurable

Simple Checklist

Folder Links

Preview reference

Selling Price

Cost Price

Profit derived
```

---

# 37. Category I — Calculator

## `15_Calculator_Module.md`

Defines overall Calculator architecture and business behavior.

---

# 38. `32_Calculator_Foundation_Implementation_Plan.md`

Defines:

```text
Templates

Template Versions

JSON Definition

Safe formula engine

Dependency graph

Circular-reference detection

Publishing
```

---

# 39. `33_Order_Calculator_Implementation_Plan.md`

Defines per-Order Calculator behavior.

Critical rule:

```text
Order Calculator references exact immutable Template Version
```

No silent Template upgrades.

---

# 40. `34_Costs_Implementation_Plan.md`

Defines Cost Items and financial invariant:

```text
Order.cost_price
=
SUM(cost_items.amount)
```

This rule is mandatory.

---

# 41. Category J — Reports / Dashboard

## `16_Reports_Module.md`

Defines report scope.

Reports are:

```text
read-only
```

and own no source-of-truth business data.

---

# 42. `35_Reports_Implementation_Plan.md`

Defines:

```text
Orders Report

Project Financial Summary

Client Financial Summary

Order Type Summary

Cost Summary

Filtering

Aggregation

Financial permissions
```

---

# 43. `36_Dashboard_Implementation_Plan.md`

Defines operational Dashboard.

Dashboard answers:

```text
What is active?

What is urgent?

What is overdue?

What is due soon?
```

Dashboard is not a second Reports module.

---

# 44. Category K — Architecture Implementation Guidance

## `17_Database_Schema_Overview.md`

Provides the consolidated V1 schema.

Expected core V1 tables:

```text
18
```

across:

```text
auth

employees

clients

projects

orders

calculator
```

---

# 45. `18_Implementation_Roadmap.md`

Defines recommended module implementation sequence.

Use it for ordering major work.

---

# 46. `23_Frontend_Architecture.md`

Defines React frontend structure and patterns.

Read before creating:

```text
routes

modules

shared components

API clients

permission helpers
```

---

# 47. `24_Backend_Architecture.md`

Defines ASP.NET backend structure and patterns.

Critical rules include:

```text
Modular Monolith

Thin Controllers

Application use cases

EF Core directly

No GenericRepository

One main DbContext

Built-in DI

No premature CQRS/MediatR infrastructure
```

---

# 48. `25_Development_Workflow_for_AI.md`

Defines the expected AI coding process.

This document should be read for virtually every implementation task.

---

# 49. `26_Initial_Project_Setup.md`

Defines initial repository/backend/frontend creation and foundational configuration.

---

# 50. Category L — Operations

## `21_Deployment_and_Backup.md`

Defines:

```text
Production topology

Secrets

HTTPS

PostgreSQL deployment

Backup

Restore

Operational deployment
```

---

# 51. `22_Logging_Audit_and_Operational_History.md`

Defines:

```text
Technical logs

Audit metadata

What history is retained

What history is intentionally not retained
```

Important:

```text
No generic audit-log system in V1.
```

---

# 52. Category M — Release Governance

## `37_V1_Integration_and_Release_Plan.md`

Defines final:

```text
Integration tests

Pilot

Backup/restore verification

Security checks

Production smoke tests

V1 Definition of Done
```

---

# 53. `40_V1_Error_Codes_and_User_Messages.md`

Defines canonical:

```text
API error shape

HTTP mapping

Stable error codes

Frontend messaging behavior
```

Read whenever adding or changing API failure behavior.

---

# 54. `41_V1_Final_Documentation_Index.md`

This file defines:

```text
Documentation map

Precedence

Required reading sets

Freeze status

Documentation governance
```

---

# 55. Core Business Invariants

The following rules should be treated as V1 invariants.

---

# 56. User ≠ Employee

Never merge Authentication User and Employee into one entity.

```text
User
=
system access identity

Employee
=
business person
```

---

# 57. Client → Project → Order

Canonical hierarchy:

```text
Client
   ↓
Project
   ↓
Order
```

Order does not duplicate Client.

---

# 58. Project Team Only

Team lives at Project level.

Do not create:

```text
Order Owner

Order Assignee

Order Participants

Order Observers
```

in V1.

---

# 59. Project Team Roles

Canonical:

```text
Owner

Assignee

Participant

Observer
```

---

# 60. Order Checklist Is Simple

Checklist Items contain:

```text
text

is_completed

sort_order
```

Do not add Employee assignment, deadline, priority, filename, or folder path.

---

# 61. Folder Links Are Simple

Folder Link contains:

```text
name

path

sort_order
```

No Explorer integration in V1.

---

# 62. Calculator History Is Immutable

Published Template Versions cannot be edited.

Existing Order Calculators stay on their exact Version.

---

# 63. Selling Price Authority

After successful Calculator save:

```text
Order.selling_price
=
authoritative Calculator result
```

---

# 64. Cost Price Authority

After Cost mutations:

```text
Order.cost_price
=
SUM(Cost Items)
```

---

# 65. Profit Is Derived

Do not store:

```text
profit
```

Canonical:

```text
profit
=
selling_price - cost_price
```

---

# 66. Reports Are Read-Only

Reports never become source data.

---

# 67. Director Is Protected

At least one active User with Director Role must remain.

---

# 68. Backend Authorization Is Authoritative

Frontend permission hiding is not security.

---

# 69. Business IDs Are Backend-Generated

Users cannot manually assign:

```text
CL-...

PRJ-...

ORD-...
```

---

# 70. Business IDs Are Immutable

Once created:

```text
never change

never reuse
```

Gaps are acceptable.

---

# 71. Documents to Read for Every Coding Task

At minimum, AI should normally read:

```text
AI_RULES.md

41_V1_Final_Documentation_Index.md

25_Development_Workflow_for_AI.md
```

plus the relevant module documents.

---

# 72. Authentication Task Reading Set

Read:

```text
AI_RULES.md

07_Authentication.md

08_Auth_Schema_Design.md

09_Auth_Tables.md

10_Users_Module.md

19_API_Design_Guidelines.md

20_Testing_Strategy.md

24_Backend_Architecture.md

25_Development_Workflow_for_AI.md

27_Authentication_Implementation_Plan.md

38_V1_Role_and_Permission_Matrix.md

40_V1_Error_Codes_and_User_Messages.md
```

---

# 73. Employee Task Reading Set

Read:

```text
11_Employees_Module.md

28_Employees_Implementation_Plan.md

38_V1_Role_and_Permission_Matrix.md
```

plus core architecture/API/testing rules.

---

# 74. Client Task Reading Set

Read:

```text
05_Numbering_System.md

12_Clients_Module.md

29_Clients_Implementation_Plan.md
```

plus core architecture/API/testing rules.

---

# 75. Project Task Reading Set

Read:

```text
05_Numbering_System.md

11_Employees_Module.md

12_Clients_Module.md

13_Projects_Module.md

30_Projects_Implementation_Plan.md
```

plus permission/API/testing standards.

---

# 76. Order Task Reading Set

Read:

```text
05_Numbering_System.md

13_Projects_Module.md

14_Orders_Module.md

30_Projects_Implementation_Plan.md

31_Orders_Implementation_Plan.md
```

plus API/testing/permissions.

---

# 77. Calculator Foundation Reading Set

Read:

```text
14_Orders_Module.md

15_Calculator_Module.md

20_Testing_Strategy.md

24_Backend_Architecture.md

32_Calculator_Foundation_Implementation_Plan.md

40_V1_Error_Codes_and_User_Messages.md
```

---

# 78. Order Calculator Reading Set

Read:

```text
14_Orders_Module.md

15_Calculator_Module.md

31_Orders_Implementation_Plan.md

32_Calculator_Foundation_Implementation_Plan.md

33_Order_Calculator_Implementation_Plan.md
```

---

# 79. Cost Task Reading Set

Read:

```text
14_Orders_Module.md

15_Calculator_Module.md

33_Order_Calculator_Implementation_Plan.md

34_Costs_Implementation_Plan.md
```

---

# 80. Reports Task Reading Set

Read:

```text
14_Orders_Module.md

16_Reports_Module.md

19_API_Design_Guidelines.md

34_Costs_Implementation_Plan.md

35_Reports_Implementation_Plan.md

38_V1_Role_and_Permission_Matrix.md
```

---

# 81. Dashboard Task Reading Set

Read:

```text
06_UI_UX_Principles.md

14_Orders_Module.md

16_Reports_Module.md

23_Frontend_Architecture.md

35_Reports_Implementation_Plan.md

36_Dashboard_Implementation_Plan.md
```

---

# 82. Database Migration Reading Set

Before any schema migration, read:

```text
03_Database_Design.md

04_Data_Dictionary.md

17_Database_Schema_Overview.md

24_Backend_Architecture.md

25_Development_Workflow_for_AI.md
```

plus the relevant module plan.

---

# 83. Frontend Task Reading Set

Read:

```text
06_UI_UX_Principles.md

19_API_Design_Guidelines.md

23_Frontend_Architecture.md

38_V1_Role_and_Permission_Matrix.md

40_V1_Error_Codes_and_User_Messages.md
```

plus the relevant module.

---

# 84. Deployment Task Reading Set

Read:

```text
21_Deployment_and_Backup.md

22_Logging_Audit_and_Operational_History.md

37_V1_Integration_and_Release_Plan.md

39_V1_Data_Seed_and_Initial_Configuration.md
```

---

# 85. Release Task Reading Set

Read:

```text
20_Testing_Strategy.md

21_Deployment_and_Backup.md

22_Logging_Audit_and_Operational_History.md

37_V1_Integration_and_Release_Plan.md

38_V1_Role_and_Permission_Matrix.md

39_V1_Data_Seed_and_Initial_Configuration.md

40_V1_Error_Codes_and_User_Messages.md
```

---

# 86. Error Handling Task Reading Set

Read:

```text
19_API_Design_Guidelines.md

24_Backend_Architecture.md

40_V1_Error_Codes_and_User_Messages.md
```

plus the module specification producing the error.

---

# 87. Security / Permission Task Reading Set

Read:

```text
07_Authentication.md

10_Users_Module.md

27_Authentication_Implementation_Plan.md

38_V1_Role_and_Permission_Matrix.md

40_V1_Error_Codes_and_User_Messages.md
```

---

# 88. AI Task Preparation Pattern

Before a nontrivial code task:

```text
1. Read AI_RULES.md

2. Read this Index

3. Identify relevant module documents

4. Read related architecture/API/testing docs

5. Inspect current implementation

6. Produce a narrow plan

7. Implement only requested scope

8. Build/test

9. Report exact results
```

---

# 89. Documentation Freeze Concept

Some documents should be considered:

```text
Frozen for V1
```

once coding begins.

Frozen does not mean impossible to change.

It means:

```text
changes require deliberate product decision
```

rather than opportunistic code-driven edits.

---

# 90. V1 Frozen Product Documents

Recommended frozen group:

```text
00_Project_Vision.md

01_Technology_Stack.md

02_Architecture.md

03_Database_Design.md

04_Data_Dictionary.md

05_Numbering_System.md
```

These define foundational V1 decisions.

---

# 91. V1 Frozen Module Specifications

Once implementation begins, treat:

```text
07_Authentication.md

10_Users_Module.md

11_Employees_Module.md

12_Clients_Module.md

13_Projects_Module.md

14_Orders_Module.md

15_Calculator_Module.md

16_Reports_Module.md
```

as approved product/module specifications.

---

# 92. Implementation Plans

Documents:

```text
27–36
```

are implementation execution plans.

They may receive clarifications during coding if required, but should not casually change approved business rules.

---

# 93. Release Governance Documents

Treat:

```text
37_V1_Integration_and_Release_Plan.md

38_V1_Role_and_Permission_Matrix.md

39_V1_Data_Seed_and_Initial_Configuration.md

40_V1_Error_Codes_and_User_Messages.md
```

as authoritative cross-module references.

---

# 94. Architecture Documents May Receive Clarifications

Documents like:

```text
23_Frontend_Architecture.md

24_Backend_Architecture.md
```

may receive implementation clarifications.

They should not change the fundamental approved stack/architecture without explicit approval.

---

# 95. Documentation Change Process

When a requirement changes:

```text
1. Identify affected documents

2. Update the most authoritative specification

3. Update dependent implementation plan if necessary

4. Update tests

5. Update code

6. Ensure no contradictory old rule remains
```

---

# 96. Do Not Update Only the Code

If a meaningful business rule changes in code but documentation remains old:

```text
the project becomes ambiguous
```

This must be avoided.

---

# 97. Do Not Update Only a Late Implementation File

Example:

If Client Business ID format changes, update:

```text
05_Numbering_System.md

04_Data_Dictionary.md if relevant

29_Clients_Implementation_Plan.md
```

not only the implementation plan.

---

# 98. Documentation Scope Discipline

Do not add speculative future requirements into V1 docs merely because they might be useful.

Use explicit:

```text
Future

Possible later extension

Out of V1
```

sections.

---

# 99. V1 Out-of-Scope Summary

Major non-goals include:

```text
Microservices

Accounting

Warehouse

Purchasing

Customer Portal

Supplier Portal

Mobile Application

Offline mode

Real-time collaboration

Generic workflow engine

Generic report designer

Generic form builder

CRM

Payroll

Attendance

Full document management

Production scheduler
```

---

# 100. V1 Database Boundary

Core schemas:

```text
auth

employees

clients

projects

orders

calculator
```

Reports owns no schema.

Dashboard owns no schema.

---

# 101. Consolidated V1 Database Tables

Canonical:

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

orders.orders
orders.order_types
orders.checklist_items
orders.folder_links

calculator.templates
calculator.template_versions
calculator.order_calculators
calculator.cost_items
```

---

# 102. Table Count

Canonical V1:

```text
18 tables
```

If implementation introduces another persistent table, ask:

```text
Why does V1 need this?
```

and document the reason.

---

# 103. V1 Technology Lock

Canonical:

```text
Backend
C# / ASP.NET Core

ORM
EF Core

Database
PostgreSQL

Frontend
React + TypeScript

UI
Material UI
```

---

# 104. Architecture Lock

Canonical deployment/application architecture:

```text
Browser
    ↓
ASP.NET Core application
    ↓
PostgreSQL
```

with modular internal code organization.

---

# 105. No Microservices in V1

Do not split:

```text
Authentication

Clients

Projects

Orders

Calculator
```

into separately deployed services.

---

# 106. One Main Database

Use one PostgreSQL database with logical schemas.

---

# 107. One Main DbContext

Use one main EF Core DbContext unless a concrete approved reason requires otherwise.

---

# 108. No Generic Repository

Use EF Core directly.

Do not automatically add:

```text
GenericRepository<T>

UnitOfWork abstraction
```

over EF Core.

---

# 109. No Framework Fashion Changes

Do not introduce:

```text
MediatR

CQRS framework

Event bus

Redis

GraphQL

OData
```

without a concrete V1 need and approval.

---

# 110. Canonical Frontend Routes

Expected:

```text
/login

/dashboard

/clients

/clients/:clientId

/projects

/projects/:projectId

/orders

/orders/:orderId

/reports

/admin/users

/admin/roles

/admin/employees

/admin/order-types

/admin/calculator-templates
```

---

# 111. Route Identity

Routes use internal UUID identifiers.

Business IDs are display/search identifiers.

---

# 112. Canonical Business IDs

```text
Client
CL-000001

Project
PRJ-2026-000001

Order
ORD-2026-000001
```

---

# 113. Client Numbering

Global sequence.

Does not reset yearly.

---

# 114. Project Numbering

Global per year.

Resets yearly.

---

# 115. Order Numbering

Global per year.

Resets yearly.

---

# 116. Numbering Safety

Never use:

```text
MAX + 1
```

without proper atomic/concurrency-safe mechanism.

---

# 117. Canonical Project Statuses

```text
Draft

Active

On Hold

Completed

Cancelled
```

---

# 118. Canonical Order Statuses

```text
Draft

Active

On Hold

Completed

Cancelled
```

---

# 119. Canonical Order Priorities

```text
Low

Normal

High

Urgent
```

---

# 120. Canonical Project Roles

```text
Owner

Assignee

Participant

Observer
```

---

# 121. One Project Owner

Maximum:

```text
1
```

---

# 122. One Project Assignee

Maximum:

```text
1
```

---

# 123. Multiple Participant / Observer

Allowed.

Same Employee may hold several distinct Project roles.

---

# 124. Authentication Role vs Project Role

Never confuse:

```text
Director / Manager / Operator
```

with:

```text
Owner / Assignee / Participant / Observer
```

---

# 125. Canonical Calculator Version Statuses

```text
Draft

Published

Retired
```

---

# 126. Published Version Rule

Published and Retired Template Versions are immutable.

---

# 127. Canonical Calculator Element Types

Initial:

```text
Label

Number Input

Text Input

Dropdown

Checkbox

Calculated Field

Table

Section
```

---

# 128. Formula Field Keys

Use:

```text
lowercase_snake_case
```

and stable keys.

---

# 129. Formula Security

Never execute formula strings using:

```text
eval

C# scripting

JavaScript

SQL

Python

Shell
```

Use controlled parser/evaluator.

---

# 130. Canonical Cost Fields

```text
category

supplier

expense_date

description

amount

sort_order
```

Supplier and Category remain text in V1.

---

# 131. No Supplier Master in V1

Do not create supplier module simply for Cost Items.

---

# 132. No Cost Category Master in V1

Do not create category table unless explicitly added later.

---

# 133. Canonical Financial Visibility

Selling Price:

```text
orders.view_selling_price
```

Aggregate Cost Price:

```text
orders.view_cost_price
```

Detailed Costs:

```text
calculator.view_costs
```

---

# 134. Profit Visibility

Requires both:

```text
orders.view_selling_price

orders.view_cost_price
```

---

# 135. Canonical Permission Source

See:

```text
38_V1_Role_and_Permission_Matrix.md
```

Do not create undocumented permission codes casually.

---

# 136. Canonical Error Source

See:

```text
40_V1_Error_Codes_and_User_Messages.md
```

Do not branch frontend behavior on arbitrary error message strings.

---

# 137. Canonical API Error Shape

```json
{
  "code": "ERROR_CODE",
  "message": "Human-readable message.",
  "errors": null
}
```

---

# 138. Canonical Pagination

Use standard API pagination defined by `19_API_Design_Guidelines.md`.

Conceptually:

```text
page

page_size

items

total_items

total_pages
```

Use the globally configured JSON naming convention consistently.

---

# 139. API Naming Consistency

Do not choose casing independently per module.

Use the application-wide configured JSON naming convention.

---

# 140. Database Naming

PostgreSQL:

```text
lowercase_snake_case
```

---

# 141. C# Naming

Normal C# naming conventions.

---

# 142. React / TypeScript Naming

Normal TypeScript conventions aligned with API DTOs.

---

# 143. Audit Identity Rule

Business responsibility:

```text
Employee
```

Technical audit identity:

```text
User
```

---

# 144. Audit Examples

Project Owner:

```text
Employee
```

Project assigned_by:

```text
User
```

---

# 145. No Generic Audit Log V1

Do not introduce one without explicit approval.

Use specific audit metadata where defined.

---

# 146. Historical Integrity Priority

When changing code, preserve:

```text
historical Clients

historical Employees

historical Order Types

historical Calculator Versions

historical Order Calculators
```

Deactivation does not erase history.

---

# 147. Migration Rule

Never rewrite an applied production/shared migration.

Create a new migration.

---

# 148. Destructive Migration Rule

Before destructive schema change:

```text
identify data impact

update docs

create backup

test migration
```

---

# 149. AI Scope Rule

AI should implement:

```text
the requested bounded objective
```

not redesign neighboring modules automatically.

---

# 150. AI Refactor Rule

Do not perform unrelated large refactors while implementing a small feature.

---

# 151. AI Dependency Rule

Do not add external NuGet/npm dependencies without a real need.

Prefer built-in framework functionality.

---

# 152. AI Testing Rule

After meaningful change:

```text
build

run relevant tests
```

and report failures honestly.

---

# 153. AI Migration Rule

When schema changes:

```text
Update domain model

Update EF config

Create migration

Review migration

Apply to clean/test PostgreSQL

Run tests
```

---

# 154. AI Formula Rule

Calculator changes require especially strong tests because they affect Selling Price.

---

# 155. AI Cost Rule

Cost changes require invariant tests:

```text
Order.cost_price
=
SUM(Cost Items)
```

---

# 156. AI Permission Rule

Every protected endpoint must be tested through direct API authorization.

Hidden UI buttons are not enough.

---

# 157. AI Release Rule

Do not declare V1 complete merely because builds pass.

Use:

```text
37_V1_Integration_and_Release_Plan.md
```

---

# 158. Documentation Maintenance Rule

When implementation discovers a missing clarification that materially affects future work:

```text
update documentation
```

rather than leaving knowledge only inside code comments/chat.

---

# 159. Comments Are Not Primary Specification

Code comments explain code.

They do not replace module documentation.

---

# 160. Database Is Not Documentation

Do not infer entire business rules merely from current schema.

Use docs.

---

# 161. Tests Are Executable Evidence

Tests should reinforce documented invariants.

If tests and docs disagree, investigate rather than automatically choosing tests.

---

# 162. README Is Not a Detailed Spec

Keep detailed business rules in `/docs`.

README links to them.

---

# 163. Recommended README Documentation Section

Conceptually:

```text
Documentation

Start here:
- docs/41_V1_Final_Documentation_Index.md

Architecture:
- docs/02_Architecture.md
- docs/17_Database_Schema_Overview.md

Development:
- AI_RULES.md
- docs/25_Development_Workflow_for_AI.md

Release:
- docs/37_V1_Integration_and_Release_Plan.md
```

---

# 164. Recommended AI System Prompt Reference

When giving a coding agent repository instructions, include:

```text
Always read AI_RULES.md and
docs/41_V1_Final_Documentation_Index.md
before making nontrivial changes.
```

---

# 165. Suggested Task Prompt Pattern

```text
Read:
- AI_RULES.md
- docs/41_V1_Final_Documentation_Index.md
- relevant module docs
- relevant implementation plan

Task:
<one bounded objective>

Do not:
<explicit non-goals>

Before finishing:
- build
- run relevant tests
- report changed files
- report verification results
```

---

# 166. Documentation Status Labels

Recommended values:

```text
Draft

Approved

Superseded
```

Use consistently if document status metadata is maintained.

---

# 167. Approved

Means:

```text
Implementation may rely on this document.
```

---

# 168. Draft

Means:

```text
Do not treat unresolved design decisions as final.
```

---

# 169. Superseded

Means:

```text
A newer document replaces this one.
```

Add a pointer to the replacement.

---

# 170. Avoid Silent Supersession

Do not leave two conflicting approved files with no indication which one is current.

---

# 171. Versioning Documents

Minor wording improvements do not require complex semantic versioning.

Material requirement changes should update:

```text
Version

Status/date if maintained
```

and affected dependent docs.

---

# 172. Current V1 Documentation Baseline

Documents `00` through `41` together form the current V1 baseline.

---

# 173. V1 Coding Start Gate

Before full implementation begins, verify:

```text
[ ] 00–41 documentation available

[ ] AI_RULES.md available

[ ] Technology stack fixed

[ ] Database design fixed

[ ] Module boundaries fixed

[ ] Permission catalog defined

[ ] Error model defined

[ ] Implementation sequence defined

[ ] Release criteria defined
```

---

# 174. What May Still Change During Development

Expected clarifications may include:

```text
UI spacing/layout

minor DTO shapes

internal class names

query implementation details

test helper design

performance indexes after measurement
```

These do not normally require product redesign.

---

# 175. What Should Not Change Casually

Do not casually change:

```text
User vs Employee separation

Client → Project → Order hierarchy

Project-level Team

Business ID formats

Calculator version immutability

Cost Price source

Selling Price authority

Permission model

Modular Monolith architecture

Technology stack
```

---

# 176. Architecture Decision Exception

If implementation reveals a serious flaw requiring change to a foundational decision:

```text
document the issue

explain impact

update the relevant architecture/spec documents first

then change code
```

---

# 177. No Architecture by Accident

A new package or one-off workaround should not silently become the project's architecture.

---

# 178. Final Documentation Review Before Release

Before V1 release, review all docs for:

```text
obsolete names

old requirements

contradictory fields

outdated routes

incorrect permissions

incorrect schema references

features removed from V1
```

---

# 179. Final Code-to-Docs Audit

Compare:

```text
Database schema

Permission catalog

API routes

Statuses

Enums

Business IDs

Frontend routes

Error codes
```

against documentation.

---

# 180. Documentation Is Part of Definition of Done

A feature is not fully complete if:

```text
implementation materially changes an approved rule
```

but relevant documentation remains wrong.

---

# 181. Recommended Final Repository Layout

```text
Lithograph-ERP/
│
├── README.md
├── AI_RULES.md
│
├── docs/
│   ├── 00_Project_Vision.md
│   ├── 01_Technology_Stack.md
│   ├── 02_Architecture.md
│   ├── 03_Database_Design.md
│   ├── 04_Data_Dictionary.md
│   ├── 05_Numbering_System.md
│   ├── 06_UI_UX_Principles.md
│   ├── 07_Authentication.md
│   ├── 08_Auth_Schema_Design.md
│   ├── 09_Auth_Tables.md
│   ├── 10_Users_Module.md
│   ├── 11_Employees_Module.md
│   ├── 12_Clients_Module.md
│   ├── 13_Projects_Module.md
│   ├── 14_Orders_Module.md
│   ├── 15_Calculator_Module.md
│   ├── 16_Reports_Module.md
│   ├── 17_Database_Schema_Overview.md
│   ├── 18_Implementation_Roadmap.md
│   ├── 19_API_Design_Guidelines.md
│   ├── 20_Testing_Strategy.md
│   ├── 21_Deployment_and_Backup.md
│   ├── 22_Logging_Audit_and_Operational_History.md
│   ├── 23_Frontend_Architecture.md
│   ├── 24_Backend_Architecture.md
│   ├── 25_Development_Workflow_for_AI.md
│   ├── 26_Initial_Project_Setup.md
│   ├── 27_Authentication_Implementation_Plan.md
│   ├── 28_Employees_Implementation_Plan.md
│   ├── 29_Clients_Implementation_Plan.md
│   ├── 30_Projects_Implementation_Plan.md
│   ├── 31_Orders_Implementation_Plan.md
│   ├── 32_Calculator_Foundation_Implementation_Plan.md
│   ├── 33_Order_Calculator_Implementation_Plan.md
│   ├── 34_Costs_Implementation_Plan.md
│   ├── 35_Reports_Implementation_Plan.md
│   ├── 36_Dashboard_Implementation_Plan.md
│   ├── 37_V1_Integration_and_Release_Plan.md
│   ├── 38_V1_Role_and_Permission_Matrix.md
│   ├── 39_V1_Data_Seed_and_Initial_Configuration.md
│   ├── 40_V1_Error_Codes_and_User_Messages.md
│   └── 41_V1_Final_Documentation_Index.md
│
├── backend/
├── frontend/
└── tests/
```

---

# 182. Documentation Group Summary

## Product / Domain

```text
00
07
10–16
```

## Architecture / Standards

```text
01–06
17
19
20
23
24
```

## Operations

```text
21
22
```

## Development Process

```text
18
25
26
```

## Module Implementation Plans

```text
27–36
```

## Release / Governance

```text
37–41
```

---

# 183. AI First-Read Set

For a new AI agent joining the repository, recommended first read:

```text
README.md

AI_RULES.md

docs/00_Project_Vision.md

docs/01_Technology_Stack.md

docs/02_Architecture.md

docs/17_Database_Schema_Overview.md

docs/18_Implementation_Roadmap.md

docs/25_Development_Workflow_for_AI.md

docs/38_V1_Role_and_Permission_Matrix.md

docs/41_V1_Final_Documentation_Index.md
```

Then read the specific module documents for the assigned task.

---

# 184. Human Developer First-Read Set

Recommended:

```text
README.md

00_Project_Vision.md

01_Technology_Stack.md

02_Architecture.md

17_Database_Schema_Overview.md

18_Implementation_Roadmap.md

41_V1_Final_Documentation_Index.md
```

---

# 185. Database Developer First-Read Set

```text
03_Database_Design.md

04_Data_Dictionary.md

05_Numbering_System.md

17_Database_Schema_Overview.md

24_Backend_Architecture.md
```

---

# 186. Frontend Developer First-Read Set

```text
06_UI_UX_Principles.md

19_API_Design_Guidelines.md

23_Frontend_Architecture.md

38_V1_Role_and_Permission_Matrix.md

40_V1_Error_Codes_and_User_Messages.md
```

---

# 187. QA First-Read Set

```text
20_Testing_Strategy.md

37_V1_Integration_and_Release_Plan.md

38_V1_Role_and_Permission_Matrix.md

40_V1_Error_Codes_and_User_Messages.md
```

plus module plans.

---

# 188. Operations First-Read Set

```text
21_Deployment_and_Backup.md

22_Logging_Audit_and_Operational_History.md

37_V1_Integration_and_Release_Plan.md

39_V1_Data_Seed_and_Initial_Configuration.md
```

---

# 189. Calculator Developer First-Read Set

```text
15_Calculator_Module.md

32_Calculator_Foundation_Implementation_Plan.md

33_Order_Calculator_Implementation_Plan.md

34_Costs_Implementation_Plan.md
```

plus backend/testing/API standards.

---

# 190. Highest-Risk Areas

Changes in the following areas require special care:

```text
Authentication

Permissions

Business ID generation

Calculator formulas

Calculator versioning

Selling Price synchronization

Cost Price synchronization

Financial Reports

Database migrations

Backup/restore
```

---

# 191. Highest-Risk Invariants

Before merging changes affecting these areas, confirm:

```text
Final Director remains protected

No duplicate Business IDs

Published Templates immutable

Existing Orders do not auto-upgrade Calculator Version

Selling Price remains backend-authoritative

Cost Price equals Cost Item SUM

Financial permissions do not leak data
```

---

# 192. AI Stop Conditions

AI should stop a particular implementation change and report the issue when it discovers:

```text
A destructive migration with unclear data impact

Two authoritative docs defining incompatible business rules

A request that would break Calculator history

A request that would expose protected financial data

A request that requires silently rewriting production migrations
```

Routine implementation uncertainty should be resolved through the documentation hierarchy rather than unnecessary questions.

---

# 193. AI Must Not Treat Future Sections as V1 Requirements

Documents contain possible future features.

Text under:

```text
Future

Possible later

Optional future
```

is not automatically part of V1.

---

# 194. V1 Success Criteria

V1 is successful when Lithograph can operate:

```text
Client
→ Project
→ Order
→ Calculator
→ Costs
→ Reports
```

with:

```text
clear responsibility

reliable financial numbers

controlled access

recoverable data
```

---

# 195. V1 Documentation Definition of Done

The documentation baseline is complete when it provides clear answers to:

```text
What are we building?

What technologies are used?

What data exists?

How do modules relate?

How does Authentication work?

How are Users and Employees different?

How do Clients, Projects and Orders work?

How are prices calculated?

How are Costs calculated?

How are Reports built?

Who can access what?

How do errors behave?

How is the system deployed?

How is data backed up?

How is V1 tested and released?
```

The current `00–41` documentation set is intended to provide those answers.

---

# 196. Final Documentation Principle

The documentation exists so that a developer or AI agent does not need to reinvent Lithograph ERP from assumptions.

The desired development flow is:

```text
Read
   ↓
Understand
   ↓
Implement
   ↓
Test
   ↓
Verify against documentation
```

not:

```text
Guess
   ↓
Code
   ↓
Invent architecture
   ↓
Fix contradictions later
```

The central rule is:

```text
Documentation defines intent.

Code implements intent.

Tests prove intent.
```

---

**End of Document**