# Lithograph ERP

**Document:** 18_Implementation_Roadmap.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `01_Technology_Stack.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `05_Numbering_System.md`
- `06_UI_UX_Principles.md`
- `07_Authentication.md`
- `08_Auth_Schema_Design.md`
- `09_Auth_Tables.md`
- `10_Users_Module.md`
- `11_Employees_Module.md`
- `12_Clients_Module.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `16_Reports_Module.md`
- `17_Database_Schema_Overview.md`

---

# 1. Purpose

This document defines the recommended implementation sequence for Lithograph ERP Version 1.

Its purpose is to prevent AI-assisted development from:

- Building features in the wrong order
- Creating unnecessary architecture
- Implementing modules before dependencies exist
- Creating inconsistent database structures
- Writing large amounts of code before the foundation is stable
- Mixing unrelated modules in one development step

The system should be built incrementally.

---

# 2. Main Implementation Principle

Build Lithograph ERP like LEGO.

```text
Foundation
    ↓
One Module
    ↓
Test
    ↓
Next Module
    ↓
Test
    ↓
Integration
```

Do not attempt to generate the entire ERP in one prompt.

---

# 3. Development Strategy

For each implementation phase:

```text
Read Documentation
      ↓
Implement Small Scope
      ↓
Compile
      ↓
Run Tests
      ↓
Manually Verify
      ↓
Commit
      ↓
Continue
```

Each phase should leave the application in a working state.

---

# 4. Recommended Implementation Order

Version 1 should be implemented approximately in this order:

```text
Phase 0
Repository and Development Environment

Phase 1
Backend Foundation

Phase 2
Database Foundation

Phase 3
Authentication

Phase 4
Frontend Foundation

Phase 5
Employees

Phase 6
Clients

Phase 7
Projects

Phase 8
Orders

Phase 9
Calculator Foundation

Phase 10
Calculator Template Designer

Phase 11
Order Calculator

Phase 12
Costs

Phase 13
Reports

Phase 14
Dashboard and UX Integration

Phase 15
Security and Permission Review

Phase 16
Testing and Stabilization

Phase 17
Deployment Preparation
```

---

# 5. Phase 0 — Repository and Environment

## Goal

Create a clean development environment before business code is written.

---

# 6. Repository Structure

Initial structure:

```text
Lithograph-ERP/
├── README.md
├── AI_RULES.md
├── docs/
├── backend/
├── frontend/
└── tests/
```

All existing approved Markdown documentation should be placed inside:

```text
docs/
```

except:

```text
README.md
AI_RULES.md
```

which remain at repository root.

---

# 7. Git

Initialize Git immediately.

Create a clean initial commit containing:

```text
Documentation

Repository folders

.gitignore
```

Do not wait until large amounts of code already exist.

---

# 8. Git Ignore

At minimum ignore:

```text
bin/

obj/

node_modules/

dist/

.env

.env.*

IDE temporary files

local secrets

generated local database files if any
```

Never commit secrets.

---

# 9. Development Configuration

Use separate configuration for:

```text
Development

Production
```

Sensitive configuration should come from:

```text
Environment Variables

ASP.NET Core User Secrets

Deployment Secret Storage
```

not Git.

---

# 10. Phase 1 — Backend Foundation

## Goal

Create the ASP.NET Core backend skeleton.

---

# 11. Backend Technology

Use:

```text
C#

ASP.NET Core

REST API

Entity Framework Core

PostgreSQL
```

Do not introduce alternative backend frameworks.

---

# 12. Solution Structure

Keep the backend structure modular but simple.

A reasonable initial structure:

```text
backend/
├── LithographERP.sln
│
├── src/
│   ├── LithographERP.Api/
│   ├── LithographERP.Application/
│   ├── LithographERP.Domain/
│   └── LithographERP.Infrastructure/
│
└── tests/
    ├── LithographERP.UnitTests/
    └── LithographERP.IntegrationTests/
```

This is a guideline, not permission to create excessive architectural layers.

---

# 13. Modular Monolith Structure

Inside the application, business code should remain organized by module.

Example:

```text
Modules/
├── Authentication/
├── Employees/
├── Clients/
├── Projects/
├── Orders/
├── Calculator/
└── Reports/
```

Avoid organizing the entire system only by generic technical folders such as:

```text
Controllers/
Services/
Repositories/
Models/
```

with every module mixed together.

---

# 14. Feature-Oriented Organization

Prefer:

```text
Modules/
└── Clients/
    ├── Domain/
    ├── Application/
    ├── Infrastructure/
    └── Api/
```

or an equivalent simple module-oriented structure.

Exact folder naming may be adjusted during implementation.

The important principle is:

```text
Keep related business code together.
```

---

# 15. Repository Pattern

Do not automatically create a generic Repository layer over Entity Framework Core.

EF Core already provides database abstraction.

Introduce repositories only when a real domain need justifies them.

Avoid boilerplate such as:

```text
IGenericRepository<T>
GenericRepository<T>
```

without a demonstrated requirement.

---

# 16. CQRS

Do not introduce full CQRS infrastructure in Version 1.

Simple application services or feature handlers are sufficient.

Commands and queries may be separated conceptually without requiring a CQRS framework.

---

# 17. Mediation Frameworks

Do not add MediatR or similar frameworks automatically.

Use them only if their benefit becomes clear.

Version 1 should prefer direct understandable application code.

---

# 18. API Foundation

Set up:

```text
ASP.NET Core Web API

JSON serialization

Dependency Injection

Global error handling

Validation infrastructure

Authentication infrastructure placeholder

OpenAPI/Swagger for Development
```

---

# 19. API Error Format

Define one consistent API error response.

Conceptual example:

```text
code
message
validation_errors
```

Do not expose stack traces to normal API clients.

---

# 20. Health Endpoint

A simple health endpoint may be created:

```text
/health
```

It may verify that the application is running.

Database health may later be included if useful.

Do not build a complex monitoring system.

---

# 21. Phase 2 — Database Foundation

## Goal

Connect PostgreSQL and create the initial EF Core infrastructure.

---

# 22. PostgreSQL Setup

Create a development PostgreSQL database.

Connection string must be external configuration.

Do not hardcode credentials.

---

# 23. DbContext

Create the EF Core database context according to the chosen modular architecture.

Possible approaches:

```text
One application DbContext with module configurations
```

or:

```text
Module-specific DbContexts sharing one database
```

For Version 1, prefer the simpler implementation.

One DbContext is acceptable if module ownership remains clear.

---

# 24. EF Core Configurations

Entity mapping should be explicit.

Define:

```text
Schema

Table Name

Primary Keys

Foreign Keys

Lengths

Indexes

Unique Constraints

Delete Behavior

Numeric Precision
```

Do not rely blindly on EF Core conventions for important database rules.

---

# 25. Initial Schema Verification

Before creating all entities, verify that EF Core correctly creates PostgreSQL schemas such as:

```text
auth

employees

clients

projects

orders

calculator
```

---

# 26. Migration Strategy

Create migrations only after entity configuration is reviewed.

Do not repeatedly create and delete migrations while AI is guessing the model.

The approved Markdown documents define the intended schema.

---

# 27. Development Database Reset

During very early local development, database reset may be acceptable before shared migrations become important.

Once migration history matters:

```text
Stop rewriting applied migrations.
```

Create new migrations instead.

---

# 28. Phase 3 — Authentication

## Goal

Make the ERP securely accessible.

Authentication must be implemented before normal business modules.

---

# 29. Authentication Tables

Implement:

```text
auth.users

auth.roles

auth.permissions

auth.user_roles

auth.role_permissions

auth.sessions
```

according to:

```text
09_Auth_Tables.md
```

---

# 30. Password Security

Use established ASP.NET Core password hashing/security.

Do not implement custom password encryption or hashing.

---

# 31. Initial Setup

Implement first-run behavior:

```text
No User exists
      ↓
Initial Setup screen
      ↓
User chooses Director password
      ↓
Create username: director
      ↓
Assign Director Role
```

---

# 32. First Login

After setup:

```text
Initial Setup disabled
```

and normal Login becomes available.

Test this before continuing.

---

# 33. Authentication API

Implement basic endpoints first:

```text
Login

Logout

Get Current User

Change Own Password
```

Then implement administrative User operations.

---

# 34. Roles and Permissions

Implement:

```text
Role creation

Role editing

Permission assignment

User Role assignment
```

Director full-access behavior must be tested.

---

# 35. Director Protection

Test:

```text
Final active Director cannot be deactivated.

Final active Director cannot lose Director Role.
```

This must work before business modules rely on authorization.

---

# 36. User Administration

Implement the functionality defined in:

```text
10_Users_Module.md
```

including:

```text
Users List

Create User

Edit Username

Activate/Deactivate

Reset Password

Assign Roles
```

---

# 37. Authentication Completion Gate

Do not move to business modules until:

```text
Login works

Logout works

Sessions work

Permissions work

Director setup works

Director protection works

User administration works

Authentication tests pass
```

---

# 38. Phase 4 — Frontend Foundation

## Goal

Create the React application shell before implementing business screens.

---

# 39. Frontend Technology

Use:

```text
React

TypeScript

Material UI
```

---

# 40. Frontend Base Structure

A reasonable structure:

```text
frontend/src/
├── app/
├── modules/
├── components/
├── api/
├── hooks/
├── types/
└── utilities/
```

Business functionality should remain organized by module.

---

# 41. Application Shell

Implement:

```text
Login screen

Main navigation

Application header

Main content area

Error handling

Loading states

Authentication state
```

---

# 42. Routing

Set up stable routes.

Conceptual examples:

```text
/login

/dashboard

/clients

/projects

/projects/:id

/orders

/orders/:id

/admin/users

/admin/employees

/admin/order-types

/admin/calculator-templates

/reports
```

Exact route structure may be adjusted.

---

# 43. Permission-Aware Navigation

Navigation must respect permissions.

Example:

If User lacks:

```text
users.view
```

do not show:

```text
Administration → Users
```

Backend security remains authoritative.

---

# 44. Shared UI Components

Create shared components only when actually reusable.

Examples:

```text
Page Header

Confirm Dialog

Loading Indicator

Error Display

Business ID Display

Standard Data Table Wrapper
```

Do not create a huge design-system project before real screens exist.

---

# 45. Phase 5 — Employees

## Goal

Implement the first small business module.

---

# 46. Employee Backend

Implement:

```text
employees.employees
```

and endpoints for:

```text
List

Get

Create

Update

Activate

Deactivate

Link User

Unlink User
```

---

# 47. Employee Frontend

Implement:

```text
Employee List

Create Employee

Edit Employee

User Link selection
```

Keep the UI simple.

---

# 48. Employee Completion Gate

Verify:

```text
Employee without User works

Employee with User works

One User cannot link to two Employees

Employee deactivation works

Historical record remains

Permissions work
```

---

# 49. Phase 6 — Clients

## Goal

Implement the Client master database.

---

# 50. Client Backend

Implement:

```text
clients.clients
```

including Business ID generation:

```text
CL-000001
```

---

# 51. Client API

Implement:

```text
List Clients

Get Client

Create Client

Update Client

Activate

Deactivate
```

---

# 52. Client Frontend

Implement:

```text
Client List

Client Search

Create Client

Edit Client

Client Details
```

---

# 53. Client Business ID Testing

Test concurrent Client creation.

Business IDs must remain:

```text
Unique

Automatic

Immutable
```

Sequence gaps are acceptable.

---

# 54. Phase 7 — Projects

## Goal

Implement the Client → Project relationship and Project Team.

---

# 55. Project Tables

Implement:

```text
projects.projects

projects.project_members
```

---

# 56. Project Business ID

Implement:

```text
PRJ-YYYY-000001
```

with yearly sequence behavior.

---

# 57. Project Team

Implement:

```text
Owner

Assignee

Participant

Observer
```

using Employees.

---

# 58. Project Constraints

Enforce:

```text
Maximum one Owner

Maximum one Assignee

Only active Employees for new assignments

No duplicate identical role assignment
```

---

# 59. Project Workspace

Implement:

```text
Header

General Information

Project Team

Orders placeholder
```

At this phase, the Orders section may initially show:

```text
No Orders yet.
```

until Orders is implemented.

---

# 60. Project Completion Gate

Verify:

```text
Client relationship works

Inactive Client rejected for new Project

Project Business IDs work

Project Team works

Owner uniqueness works

Assignee uniqueness works

Inactive Employee historical assignment remains visible
```

---

# 61. Phase 8 — Orders

## Goal

Implement the central production/service workflow.

---

# 62. Order Tables

Implement:

```text
orders.order_types

orders.orders

orders.checklist_items

orders.folder_links
```

---

# 63. Order Type First

Implement Order Type administration before normal Order creation.

At this point Calculator Template selection may initially remain:

```text
None
```

until Calculator is implemented.

---

# 64. Order Business ID

Implement:

```text
ORD-YYYY-000001
```

with yearly sequence.

---

# 65. Order Core Fields

Implement:

```text
Project

Order Type

Name

Description

Status

Priority

Deadline

Selling Price

Cost Price

Preview Image Reference
```

Selling and Cost initially remain zero until Calculator integration.

---

# 66. Order Workspace

Implement:

```text
Header

General

Calculator placeholder

Checklist

Folder Links
```

---

# 67. Checklist

Implement only:

```text
Add Item

Edit Text

Complete

Reorder

Remove
```

Do not add extra Checklist features.

---

# 68. Folder Links

Implement:

```text
Name

Path

Sort Order
```

Do not implement Windows Explorer integration.

---

# 69. Order Team Display

Display Project Team in Order Workspace by reading Project relationships.

Do not create Order Team records.

---

# 70. Order Completion Gate

Verify:

```text
Order creation works

Project relationship works

Client context resolves through Project

Project Team resolves through Project

Order Type works

Status works

Priority works

Checklist works

Folder Links work

No order_members table exists
```

---

# 71. Phase 9 — Calculator Foundation

## Goal

Build the backend calculation model before building the full Template Designer UI.

---

# 72. Calculator Core Tables

Implement:

```text
calculator.templates

calculator.template_versions

calculator.order_calculators
```

Cost Items may be added in Phase 12 if preferred.

---

# 73. Template Version Logic

Implement:

```text
Draft

Published

Retired
```

and:

```text
Published Versions are immutable.
```

---

# 74. Template Version Creation

Implement:

```text
Create Template
      ↓
Create Version 1 Draft
```

and:

```text
Published Version
      ↓
Create New Draft
```

---

# 75. Definition Validation

Before publishing, backend validation must check:

```text
Field keys

Formula syntax

References

Circular dependencies

Required output definition
```

---

# 76. Formula Engine

Implement the safe Formula Engine independently from UI.

Test thoroughly before Template Designer is complete.

---

# 77. Initial Formula Features

Start with:

```text
Numbers

Field References

+
-
*
/
Parentheses
```

Then add required functions incrementally.

Do not implement all ~30 functions immediately unless needed.

---

# 78. Formula Engine Tests

Test:

```text
Simple arithmetic

Operator precedence

Parentheses

Field references

Missing values

Division by zero

Invalid formula

Circular dependency

Decimal precision
```

---

# 79. Formula Function Expansion

Add approved functions such as:

```text
SUM

MIN

MAX

ROUND

CEILING

FLOOR

ABS

IF
```

one controlled group at a time.

---

# 80. Phase 10 — Template Designer

## Goal

Create the administrative interface for designing Calculator Templates.

---

# 81. First Template Designer Version

Do not begin with drag-and-drop spreadsheet complexity.

Start with a structured editor that can create:

```text
Sections

Fields

Labels

Field Keys

Field Types

Formulas

Dropdown Options
```

A more visual designer can evolve later.

---

# 82. Designer Element Types

Initial support:

```text
Label

Number Input

Text Input

Dropdown

Checkbox

Calculated Field

Section
```

Table support may follow after basic fields are stable.

---

# 83. Template Preview

Implement preview/test mode.

Allow sample inputs and show calculation results.

Do not create Orders during Template testing.

---

# 84. Publishing

Add explicit:

```text
Publish
```

action only after Draft validation works reliably.

---

# 85. Designer Completion Gate

Verify:

```text
Create Template

Edit Draft

Preview Draft

Formula validation

Publish

Published Version locked

Create new Draft from Published

New version numbering works
```

---

# 86. Phase 11 — Order Calculator

## Goal

Connect Calculator Templates to real Orders.

---

# 87. Order Type Template Assignment

Enable:

```text
Order Type
→ Calculator Template
```

in Order Type Administration.

---

# 88. Calculator Creation

When first opening Calculator:

```text
Order
    ↓
Order Type
    ↓
Template
    ↓
Latest Published Version
    ↓
Create Order Calculator
```

---

# 89. Historical Lock

After creation:

```text
Order Calculator
→ Exact Template Version
```

must remain fixed.

Never silently update it.

---

# 90. Calculator Inputs

Render Calculator UI from:

```text
Template Version definition
```

Store values in:

```text
calculator.order_calculators.field_values
```

---

# 91. Selling Price

After backend calculation:

```text
Selling Price Result
      ↓
orders.orders.selling_price
```

must update transactionally.

---

# 92. Calculator Completion Gate

Verify:

```text
Correct Template selected

Exact Version retained

Existing Order unaffected by new Template Version

Field values persist

Formula results correct

Selling Price synchronizes

Unauthorized financial data hidden
```

---

# 93. Phase 12 — Costs

## Goal

Implement the Order mini expense database.

---

# 94. Cost Table

Implement:

```text
calculator.cost_items
```

---

# 95. Cost UI

Inside Order Calculator:

```text
Category

Supplier

Date

Description

Amount
```

plus:

```text
Add Row

Edit

Remove
```

---

# 96. Cost Price

Whenever Costs change:

```text
SUM(cost_items.amount)
      ↓
orders.orders.cost_price
```

must update transactionally.

---

# 97. Cost Permissions

Enforce:

```text
calculator.view_costs

calculator.edit_costs
```

on backend and frontend.

---

# 98. Cost Completion Gate

Verify:

```text
Add Cost

Edit Cost

Delete Cost

Cost Total correct

Order Cost Price synchronized

Unauthorized User cannot view Cost data

Calculator reset does not delete Costs
```

---

# 99. Phase 13 — Reports

## Goal

Add useful reporting only after source modules are stable.

---

# 100. First Reports

Implement:

```text
Orders Report

Project Financial Summary

Client Financial Summary

Order Type Financial Summary

Cost Summary
```

---

# 101. Report Filtering

Support useful filters:

```text
Date Range

Client

Project

Order Type

Status

Priority

Owner

Assignee
```

Only where relevant.

---

# 102. Report Authorization

Ensure:

```text
Selling Price permission

Cost Price permission

Profit visibility
```

are respected by report APIs.

---

# 103. Report Completion Gate

Verify:

```text
Filtered totals correct

Pagination does not alter totals

Cancelled inclusion/exclusion works

Inactive historical records remain visible

Financial permissions cannot be bypassed
```

---

# 104. Phase 14 — Dashboard and UX Integration

## Goal

Improve application flow after core modules actually work.

---

# 105. Dashboard

Start with a small Dashboard.

Possible elements:

```text
Active Orders

Urgent Orders

Orders Due Soon

Recent Orders

Recent Projects
```

Financial metrics should appear only for authorized Users.

---

# 106. Do Not Overbuild Dashboard

Do not begin Version 1 with:

```text
Custom widgets

Drag-and-drop dashboard

User-defined dashboards

Advanced charts
```

A clear summary is sufficient.

---

# 107. Navigation Review

At this phase review:

```text
Dashboard

Clients

Projects

Orders

Reports

Administration
```

Ensure normal operators are not exposed to unnecessary Administration functionality.

---

# 108. Workspace Review

Verify consistency between:

```text
Client Details

Project Workspace

Order Workspace
```

Important information should be easy to find.

---

# 109. Search Review

Ensure Business ID search works well for:

```text
Client

Project

Order
```

---

# 110. Gallery View

After Preview Images work reliably, add Order Gallery view if still useful.

Do not block core V1 completion on Gallery polish.

---

# 111. Phase 15 — Security and Permission Review

## Goal

Review the whole system as one security boundary.

---

# 112. Authorization Audit

Review every endpoint.

Check:

```text
Does this endpoint require login?

Which Permission is required?

Can protected financial information leak through DTOs?

Can frontend routes bypass backend authorization?
```

---

# 113. Director Review

Verify Director access to all newly introduced module permissions.

New Permissions must not accidentally lock Director out.

---

# 114. Permission Matrix

Create a practical test matrix.

Example:

| Feature | Director | Designer | Operator |
|---|---|---|---|
| View Orders | Yes | Yes | Yes |
| Edit Calculator | Yes | Yes | Maybe |
| View Selling Price | Yes | Maybe | Yes/No by configuration |
| View Cost Price | Yes | No | No |
| Manage Users | Yes | No | No |

Actual Role assignments remain configurable.

---

# 115. Password and Session Review

Verify:

```text
No plaintext passwords

No password hashes in API

No raw session secrets in database/API

Inactive User loses access

Password reset invalidates Sessions

Logout invalidates Session
```

---

# 116. Phase 16 — Testing and Stabilization

## Goal

Treat the full V1 workflow as one system.

---

# 117. Core Integration Scenario

Test this complete workflow:

```text
1. Director logs in.

2. Director creates Employee.

3. Director creates Client.

4. Director creates Project.

5. Employees assigned to Project Team.

6. User creates Order.

7. Order inherits Project Team context.

8. User fills Calculator.

9. Selling Price calculated.

10. Cost Items entered.

11. Cost Price calculated.

12. Checklist completed.

13. Order marked Completed.

14. Project financial report shows correct totals.
```

---

# 118. Historical Calculator Scenario

Test:

```text
Create Order using Template v1

Publish Template v2

Open old Order

Old Order still uses v1

Create new Order

New Order uses v2
```

This is a critical V1 test.

---

# 119. Employee History Scenario

Test:

```text
Employee assigned to Project

Employee becomes inactive

Historical Project still shows Employee

Employee excluded from new assignments
```

---

# 120. Client History Scenario

Test:

```text
Client has Projects and Orders

Client becomes inactive

Historical records still work

Client cannot normally be selected for new Project
```

---

# 121. Order Type History Scenario

Test:

```text
Order uses Order Type

Order Type becomes inactive

Historical Order still works

Inactive Type unavailable for new Order
```

---

# 122. Financial Security Scenario

Test with a User who lacks Cost permission.

Verify they cannot obtain Cost data through:

```text
Order API

Calculator API

Reports API

Browser developer tools

Alternate routes
```

---

# 123. Error Handling Review

Verify useful errors for:

```text
Duplicate username

Invalid Business ID generation

Inactive Client

Inactive Employee

Inactive Order Type

Invalid formula

Circular formula

Unauthorized action

Database conflict
```

---

# 124. Concurrency Testing

At minimum test concurrent:

```text
Client creation

Project creation

Order creation
```

to ensure Business ID generation remains unique.

---

# 125. Calculator Concurrency

Test two edits to the same Calculator.

Avoid silent overwrite.

A simple optimistic concurrency mechanism is sufficient.

---

# 126. Database Constraint Testing

Do not rely only on application validation.

Test database constraints for:

```text
Unique usernames

Unique Business IDs

One Owner

One Assignee

One Calculator per Order

Unique Template version number
```

---

# 127. Performance Testing

Test realistic datasets before optimization.

Examples:

```text
Thousands of Orders

Hundreds of Projects

Reports with date filters

Order search

Business ID lookup
```

PostgreSQL should handle this easily with correct indexes.

---

# 128. Do Not Optimize Prematurely

Do not add:

```text
Redis

Elasticsearch

Message Broker

Materialized Views

Separate Reporting Database
```

without measured need.

---

# 129. Phase 17 — Deployment Preparation

## Goal

Prepare V1 for real Lithograph internal use.

---

# 130. Production Configuration

Create production configuration for:

```text
Database connection

Authentication secrets

Allowed frontend origin

Logging

Session settings

File storage paths if required
```

Secrets remain external to source control.

---

# 131. Database Backup

Before production use, define a PostgreSQL backup process.

At minimum decide:

```text
How backup is created

Where backup is stored

How often backup occurs

How restoration is tested
```

A backup that has never been tested for restoration is not sufficient.

---

# 132. Migration Deployment

Production database changes use:

```text
Reviewed EF Core migrations
```

Do not let the application randomly alter production schema without a controlled deployment process.

---

# 133. Initial Production Setup

Production first run:

```text
Deploy database/application
      ↓
Open ERP
      ↓
Initial Director Setup
      ↓
Create Director password
      ↓
Login
```

---

# 134. Initial Business Configuration

After Director login:

```text
Create Employees

Create Users

Create Roles

Configure Permissions

Create Order Types

Create Calculator Templates
```

Then normal business data entry can begin.

---

# 135. Initial Real-World Pilot

Before full adoption, test with a small number of real Projects and Orders.

Verify:

```text
Workflow feels natural

Required fields are sufficient

Calculator matches real pricing

Cost entry works

Permissions are correct

Reports are useful
```

---

# 136. Pilot Rule

During pilot:

```text
Fix real workflow problems.
```

Do not use the pilot as an excuse to add every imaginable feature.

---

# 137. V1 Completion Definition

Lithograph ERP Version 1 is complete when the company can reliably:

```text
Manage Users

Manage Employees

Manage Clients

Create Projects

Assign Project Teams

Create Orders

Classify Orders by Type

Calculate Selling Prices

Record Order Costs

Calculate Cost Prices

Use Checklists

Store Folder Links

Track Order status and priority

View operational and financial Reports
```

---

# 138. V1 Does Not Require

V1 completion does not depend on:

```text
CRM

Warehouse

Accounting

Purchasing

Production Scheduling

Machine Maintenance

Mobile App

AI Assistant

Customer Portal

Supplier Portal

Windows Explorer Integration

Microservices
```

Do not delay V1 for these features.

---

# 139. Recommended Commit Strategy

Create small meaningful Git commits.

Examples:

```text
feat(auth): add initial director setup

feat(employees): add employee CRUD

feat(clients): add client business IDs

feat(projects): add project team assignments

feat(orders): add checklist

feat(calculator): add formula engine

feat(reports): add project financial summary
```

Avoid giant commits containing several unrelated modules.

---

# 140. AI Coding Task Size

Prompts to Cursor/Claude should describe one bounded task.

Good:

```text
Implement the employees.employees EF Core entity and configuration according to docs/11_Employees_Module.md.
```

Bad:

```text
Build the entire ERP.
```

---

# 141. AI Task Rule

Each AI coding task should specify:

```text
What document to read

What exact feature to implement

Which files may be changed

What must not be changed

Acceptance criteria

Required tests
```

---

# 142. Example AI Task

```text
Read AI_RULES.md, README.md, docs/03_Database_Design.md,
docs/09_Auth_Tables.md and docs/10_Users_Module.md.

Implement only User creation.

Requirements:
- username is required
- username uniqueness is case-insensitive
- password is securely hashed
- Role assignment is supported
- final Director rules must not be affected

Do not implement Employees or any other module.

Add unit/integration tests.

Build and run tests before finishing.
```

---

# 143. AI Must Inspect Existing Code

Before every implementation task, AI should:

```text
Read relevant docs

Inspect existing code

Inspect current migrations

Inspect tests

Reuse existing patterns
```

Do not regenerate working infrastructure unnecessarily.

---

# 144. AI Must Not Rewrite Architecture

AI must not decide to replace:

```text
ASP.NET Core

PostgreSQL

React

Material UI

Modular Monolith
```

with another stack.

Architecture changes require explicit approval.

---

# 145. Definition of Done for Each Task

A task is not complete merely because code was generated.

Definition of Done:

```text
Code compiles

Relevant tests pass

Migration reviewed if database changed

API behavior verified

Security checked

Documentation still matches implementation

No unrelated changes introduced
```

---

# 146. Documentation Update Rule

If implementation reveals a genuine design change:

```text
Do not silently modify code only.
```

Update the relevant Markdown document first or alongside the implementation.

Documentation remains the architectural source of truth.

---

# 147. Bug Fix Rule

For a normal implementation bug:

```text
Fix the code.
```

Do not change documentation simply to make an accidental bug appear correct.

---

# 148. Schema Change Rule

For a proposed schema change:

```text
Explain business reason

Check existing docs

Update docs

Create migration

Update tests
```

Do not let AI casually add database columns.

---

# 149. Avoid Premature Abstractions

Do not create generic systems for hypothetical future needs.

Examples to avoid:

```text
Universal Entity Framework

Dynamic Everything

Generic Workflow Engine

Generic Form Builder outside Calculator

Generic Repository

Generic Plugin System
```

Build what Lithograph actually needs.

---

# 150. Reuse After Repetition

A useful rule:

```text
First implementation:
Make it clear.

Second similar implementation:
Observe similarity.

Third repetition:
Consider abstraction.
```

Do not abstract before patterns actually exist.

---

# 151. Recommended First Coding Milestone

The first milestone should be:

```text
Backend starts

PostgreSQL connects

Auth tables exist

Initial Director setup works

Director can log in

Director can create another User
```

This proves the basic platform.

---

# 152. Recommended Second Milestone

```text
Employees

Clients

Projects
```

working together.

At this point Lithograph can represent:

```text
Who works here?

Who is the customer?

What Project are we doing?

Who is responsible?
```

---

# 153. Recommended Third Milestone

```text
Orders

Checklist

Folder Links

Order Types
```

At this point the core production workflow exists.

---

# 154. Recommended Fourth Milestone

```text
Calculator Templates

Formula Engine

Order Calculator

Selling Price

Cost Items

Cost Price
```

This delivers Lithograph's most custom functionality.

---

# 155. Recommended Fifth Milestone

```text
Reports

Dashboard

Permission review

Integration tests

Deployment
```

This completes V1.

---

# 156. Roadmap Summary

```text
Documentation
     ↓
Repository
     ↓
Backend Foundation
     ↓
Database
     ↓
Authentication
     ↓
Frontend Shell
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
     ↓
Security Review
     ↓
Integration Testing
     ↓
Deployment
```

---

# 157. Final Implementation Principle

The objective is not to write the maximum amount of code.

The objective is to create a reliable ERP that Lithograph can actually use.

Development should therefore follow:

```text
Understand
    ↓
Build
    ↓
Test
    ↓
Use
    ↓
Improve
```

not:

```text
Generate Everything
    ↓
Hope It Works
```

One stable module at a time is the preferred development strategy.

---

**End of Document**