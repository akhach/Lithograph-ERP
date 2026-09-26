# Lithograph ERP

**Document:** 25_Development_Workflow_for_AI.md  
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
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `23_Frontend_Architecture.md`
- `24_Backend_Architecture.md`

---

# 1. Purpose

This document defines the working procedure for AI coding assistants used on Lithograph ERP.

Examples include:

```text
Cursor

Claude

ChatGPT

Other code-generation assistants
```

The objective is to make AI behave like a disciplined development assistant rather than an uncontrolled code generator.

---

# 2. Core Principle

AI does not define the architecture.

The approved documentation defines the architecture.

AI implements it.

Conceptually:

```text
Documentation
     ↓
AI reads
     ↓
AI understands scope
     ↓
AI inspects existing code
     ↓
AI implements
     ↓
AI tests
     ↓
Human reviews
```

---

# 3. Source of Truth Priority

When instructions conflict, use this priority:

```text
1. Current explicit Human instruction

2. AI_RULES.md

3. Approved module/specification documents

4. Architecture documents

5. Existing established code patterns

6. AI assumptions
```

AI assumptions have the lowest authority.

---

# 4. Do Not Guess Architecture

AI must not independently replace or redesign:

```text
ASP.NET Core

PostgreSQL

React

TypeScript

Material UI

Entity Framework Core

Modular Monolith
```

without explicit approval.

---

# 5. Mandatory First Step

Before changing code, AI must read:

```text
AI_RULES.md
```

Then read the documents directly relevant to the requested task.

---

# 6. Relevant Documents

Example:

If implementing Client creation, read at minimum:

```text
AI_RULES.md

03_Database_Design.md

04_Data_Dictionary.md

12_Clients_Module.md

19_API_Design_Guidelines.md

24_Backend_Architecture.md
```

If frontend work is included, also read:

```text
23_Frontend_Architecture.md
```

---

# 7. Do Not Read Everything Every Time

AI does not need to reread every document for every small change.

Read:

```text
Global rules

Relevant architecture document

Relevant module document

Relevant existing code
```

Keep context focused.

---

# 8. Inspect Existing Code Before Editing

Before implementing a feature, inspect:

```text
Existing folder structure

Related entities

Related DTOs

Controllers/endpoints

Services/application logic

EF configurations

Existing migrations

Tests

Frontend patterns if applicable
```

---

# 9. Existing Code Has Context

A new feature must fit the existing application.

Do not assume the repository is empty.

Do not recreate infrastructure that already exists.

---

# 10. Reuse Existing Patterns

If the project already has a pattern for:

```text
Pagination

API errors

Permissions

Entity configuration

Forms

Tables

Loading states
```

reuse it.

Do not invent another competing pattern.

---

# 11. Task Scope

Every AI coding task should have a clear bounded scope.

Good:

```text
Implement Client creation backend and tests.
```

Bad:

```text
Improve the ERP.
```

---

# 12. One Main Objective per Task

Prefer one main objective.

Examples:

```text
Implement Employee CRUD.

Implement Project Team assignment.

Implement Order Checklist.

Implement Formula parser.

Implement Client List frontend.
```

This keeps changes reviewable.

---

# 13. Do Not Expand Scope

If asked to implement:

```text
Client creation
```

do not also implement:

```text
Projects

Orders

Reports

New authentication system

New UI theme
```

unless directly necessary.

---

# 14. Necessary Supporting Changes

Small supporting changes are allowed when required.

Example:

Implementing Client creation may require:

```text
Client entity

EF configuration

Create DTO

Application logic

Controller endpoint

Migration

Tests
```

These all belong to the same feature.

---

# 15. Report Scope Expansion

If an unexpected necessary change appears, AI should clearly state:

```text
This task also required X because Y depends on it.
```

Do not silently broaden the task.

---

# 16. No Speculative Features

AI must not implement features merely because they seem useful.

Examples to avoid:

```text
Departments

Supplier Portal

Warehouse

Notifications

Comments

Attachments

Machine Scheduling
```

unless approved in documentation.

---

# 17. No Future-Proofing by Complexity

Do not add unnecessary abstractions for hypothetical future needs.

Bad reasoning:

```text
Maybe Lithograph will have 100 modules later,
so I added a plugin framework now.
```

Use current Version 1 requirements.

---

# 18. Architecture Simplicity

Prefer:

```text
Clear code
```

over:

```text
Highly abstract code
```

when both solve the same problem.

---

# 19. Planning Before Coding

For non-trivial tasks, AI should first create a short implementation plan.

Example:

```text
1. Add Client entity/configuration.

2. Add Business ID generation call.

3. Add request/response DTOs.

4. Add create endpoint.

5. Add integration tests.

6. Run build/tests.
```

---

# 20. Plan Must Match Scope

The plan should not contain unrelated cleanup.

Bad:

```text
1. Refactor entire backend architecture.
2. Replace authentication.
3. Add Clients.
```

---

# 21. Small Tasks

For very small fixes, a formal plan may be unnecessary.

Example:

```text
Fix Client phone field label.
```

AI can inspect and edit directly.

---

# 22. Database-First Awareness

Before changing an entity, inspect:

```text
Database docs

Existing entity configuration

Current migrations
```

Do not change entity structure casually.

---

# 23. Schema Change Detection

A change is a schema change if it modifies:

```text
Table

Column

Type

Nullability

Foreign Key

Index

Unique constraint

Delete behavior
```

---

# 24. Schema Change Procedure

Before implementing a new schema change:

```text
1. Confirm it is supported by documentation.

2. Identify affected module document.

3. Update documentation if requirement changed.

4. Modify entity/configuration.

5. Create EF migration.

6. Review generated migration.

7. Add tests.
```

---

# 25. Do Not Invent Columns

AI must not casually add fields such as:

```text
notes

code

metadata

extra_data

sort_key

is_deleted
```

unless required.

---

# 26. Do Not Add is_deleted Automatically

Soft delete is not a universal Lithograph rule.

Use it only where documentation requires it.

---

# 27. Business IDs

AI must preserve approved Business ID formats:

```text
Client:
CL-000001

Project:
PRJ-2026-000001

Order:
ORD-2026-000001
```

Do not invent different numbering.

---

# 28. Business ID Generation

Never implement:

```text
SELECT MAX(number) + 1
```

Business ID generation must remain concurrency-safe.

---

# 29. Terminology

Use official terminology.

Correct:

```text
Order

Project

Checklist Item

Business ID

Project Team
```

Avoid accidental synonyms such as:

```text
Task

Job

Natural Key

Checklist Task
```

unless a future separate entity explicitly uses them.

---

# 30. User vs Employee

AI must always preserve:

```text
User
=
login/access identity
```

and:

```text
Employee
=
business person
```

Do not merge these models.

---

# 31. Authentication Rules

AI must preserve:

```text
No email login

No public registration

Local username/password

First User = director

Director creates Users
```

---

# 32. Director Protection

AI must not break:

```text
Final active Director cannot be deactivated.

Final active Director cannot lose Director Role.
```

Any User/Role changes must retain these invariants.

---

# 33. Permissions

Before implementing a protected feature, identify its Permission.

Examples:

```text
orders.create

projects.manage_team

calculator.edit_costs
```

Do not add a new Permission unless required.

---

# 34. Backend Security First

Frontend hiding is not authorization.

Every sensitive operation must be protected in backend.

---

# 35. DTO Safety

Never expose:

```text
password_hash

token_hash

secret keys

connection strings
```

through API DTOs.

---

# 36. Financial Security

AI must preserve field-level restrictions for:

```text
Selling Price

Cost Price

Profit

Cost Items
```

Reports must respect the same rules.

---

# 37. Project Team Rule

Version 1 Team belongs to Project.

Do not create:

```text
orders.order_members
```

or separate Order Team assignments.

---

# 38. Checklist Rule

Checklist Item contains only:

```text
id

order_id

text

is_completed

sort_order
```

Do not add:

```text
employee

deadline

priority

notes

folder

filename
```

---

# 39. Folder Link Rule

Folder Link stores:

```text
name

path
```

and ordering.

Do not add Windows Explorer launch logic in Version 1.

---

# 40. Calculator Historical Rule

Published Template Versions are immutable.

Existing Orders must remain on their historical Template Version.

This rule must never be weakened for convenience.

---

# 41. Calculator Reset Rule

Calculator reset must not silently delete real Cost Items.

Costs belong to Order.

---

# 42. Selling Price Rule

Selling Price is produced by Calculator logic and stored on Order.

Do not blindly trust a frontend-submitted final value.

---

# 43. Cost Price Rule

Cost Price must remain synchronized with:

```text
SUM(cost_items.amount)
```

Do not allow normal direct editing.

---

# 44. Profit Rule

Profit is derived:

```text
selling_price - cost_price
```

Do not create a stored `profit` column.

---

# 45. Reports Rule

Reports are read-only.

Do not create duplicated source-of-truth reporting tables in Version 1.

---

# 46. Backend Task Procedure

Recommended backend workflow:

```text
Read docs

Inspect code

Plan

Implement domain/application logic

Implement persistence

Implement API

Add tests

Run build

Run tests

Review diff
```

---

# 47. Frontend Task Procedure

Recommended frontend workflow:

```text
Read docs

Inspect existing UI/API patterns

Plan

Add/update API types

Implement API call

Implement page/component

Add permissions

Add loading/error/empty states

Add tests

Build

Review
```

---

# 48. Full-Stack Task Procedure

For a feature touching both sides:

```text
Backend contract first

Backend tests

Frontend API integration

Frontend UI

End-to-end verification
```

Avoid designing frontend around an API that does not exist.

---

# 49. API Contract First

Before frontend implementation, define:

```text
Request

Response

Errors

Permission

Route
```

according to `19_API_Design_Guidelines.md`.

---

# 50. Thin Controllers

AI must not place large business logic blocks inside Controllers.

Move meaningful logic into module application/business code.

---

# 51. EF Core

Use EF Core directly where appropriate.

Do not automatically generate:

```text
GenericRepository

GenericUnitOfWork
```

---

# 52. No Unapproved CQRS

Do not introduce:

```text
MediatR

CQRS framework

Event bus
```

unless explicitly approved.

---

# 53. No Unapproved Mapping Framework

Do not add AutoMapper automatically.

Manual mapping is acceptable.

---

# 54. Dependency Rule

Before adding a new NuGet/npm package, AI must ask:

```text
Can the existing stack reasonably do this?
```

If yes, do not add the dependency.

---

# 55. New Dependency Checklist

A new dependency should be:

```text
Necessary

Maintained

Well-known enough

Compatible with current stack

Not duplicating existing functionality
```

---

# 56. Dependency Documentation

If adding an important dependency, mention:

```text
What it does

Why it is needed
```

in the task summary.

---

# 57. Avoid Version Guessing

When selecting package versions during real implementation:

```text
Inspect current project

Use compatible supported versions

Do not arbitrarily downgrade or upgrade major framework versions
```

---

# 58. Existing Dependency Versions

Do not modify existing dependency versions unless the task requires it.

A Client form task should not trigger unrelated package upgrades.

---

# 59. Formatting

Use existing project formatting.

Do not reformat hundreds of unrelated files.

---

# 60. Minimal Diff Principle

Prefer the smallest coherent diff that solves the task.

This makes review easier and reduces accidental regressions.

---

# 61. No Drive-By Refactoring

Do not refactor unrelated code while implementing another feature unless:

```text
The existing code blocks implementation

The bug is directly encountered

The change is small and clearly beneficial
```

State such changes explicitly.

---

# 62. Renaming

Do not rename public classes/routes/fields casually.

Renames may affect:

```text
Database

API

Frontend

Tests

Documentation
```

---

# 63. Breaking Changes

Before making a breaking change, identify all affected areas.

Do not modify only one layer.

---

# 64. Existing Migrations

Never rewrite an already-applied shared migration merely to make the final schema look cleaner.

Create a new migration.

---

# 65. Migration Review

After generating migration, AI must inspect it.

Look for unexpected:

```text
DropTable

DropColumn

CascadeDelete

Type conversion

Data loss
```

---

# 66. Migration Naming

Use descriptive migration names.

Good:

```text
AddClientsModule

AddProjectTeam

AddOrderChecklist
```

Avoid:

```text
Migration1

UpdateDb
```

---

# 67. Build After Changes

Backend task:

```text
dotnet build
```

or project equivalent must succeed.

Frontend task:

```text
production/type-check build
```

must succeed.

---

# 68. Tests After Changes

Run at least tests relevant to the changed area.

Do not claim success without running them when tooling is available.

---

# 69. Existing Test Failures

If unrelated tests already fail before the change:

```text
Report them clearly.
```

Do not pretend all tests passed.

---

# 70. New Test Failures

If AI's changes cause a failure:

```text
Investigate

Fix cause

Do not disable test
```

---

# 71. Never Weaken Tests to Pass

Bad:

```text
Expected 403
→ change test to accept 200
```

without a documented business-rule change.

---

# 72. Bug Fix Workflow

For a bug:

```text
Reproduce

Identify cause

Add regression test when practical

Fix

Run related tests
```

---

# 73. Security Bug Priority

Security bugs affecting:

```text
Authentication

Permissions

Financial data
```

require regression tests whenever practical.

---

# 74. Calculator Bug Priority

Calculator bugs affecting:

```text
Price

Formula

Template version history

Cost synchronization
```

must receive regression tests.

---

# 75. Review Generated Code

AI must review its own generated code before finishing.

Look for:

```text
Dead code

Unused imports

Wrong nullability

Missing authorization

Missing transaction

Duplicated logic

Incorrect naming
```

---

# 76. Review Diff

Before finalizing, inspect changed files.

Ask:

```text
Did I change anything outside requested scope?

Did I accidentally remove functionality?

Did I modify formatting unrelated to task?
```

---

# 77. Compilation Is Not Enough

A build passing does not prove business correctness.

Also check:

```text
Business rules

Authorization

Database behavior

Tests
```

---

# 78. TODO Comments

Do not leave vague TODOs such as:

```text
TODO: fix later
```

unless there is a documented deferred requirement.

If TODO remains, make it precise.

---

# 79. Placeholder Code

Do not leave fake implementations that silently succeed.

Bad:

```text
return true;
```

with a comment saying permission logic comes later.

If functionality is incomplete, make that explicit.

---

# 80. No Fake Production Data

Do not seed fake Clients/Orders in normal production migrations.

Use development/test fixtures instead.

---

# 81. Error Handling

Use the established API error model.

Do not create unique response shapes per endpoint.

---

# 82. Error Messages

Business errors should be human-readable and stable enough for frontend use.

Do not expose internal exception text.

---

# 83. Logging

Use existing logging pattern.

Do not add:

```text
Console.WriteLine
```

throughout production code.

---

# 84. Sensitive Logging

Never log:

```text
Passwords

Hashes

Tokens

Secrets
```

---

# 85. API Authorization Review

For each new endpoint, explicitly check:

```text
Is authentication required?

Which Permission is required?

Could this leak financial data?
```

---

# 86. Database Authorization Assumption

Do not rely on frontend restrictions to protect data.

Assume users can manually send API requests.

---

# 87. Parent-Child Validation

For nested resources, verify ownership.

Example:

```text
orderId + checklistItemId
```

must actually belong together.

---

# 88. Async Code

Use async database/network operations.

Avoid `.Result` and `.Wait()`.

---

# 89. Cancellation Tokens

Pass cancellation tokens where existing patterns do so.

Do not add excessive ceremony around trivial pure calculations.

---

# 90. Query Efficiency

For lists, prefer projection.

Do not load entire graphs unnecessarily.

---

# 91. N+1 Check

Whenever implementing a list with related names, inspect query behavior.

Do not issue one query per row.

---

# 92. Search

Search must remain parameterized.

Do not construct SQL directly from user text.

---

# 93. Sort Parameters

Whitelist sort fields.

Do not allow arbitrary property/SQL injection through `sort_by`.

---

# 94. Formula Engine Safety

Formula text is untrusted configuration.

Never execute it as arbitrary C#/JavaScript/SQL.

---

# 95. File Upload Safety

If Preview Image upload is implemented:

```text
Validate type

Validate size

Generate safe filename

Store outside source tree
```

---

# 96. Folder Link Safety

Treat Folder Links as strings only.

Do not perform filesystem commands using them in Version 1.

---

# 97. Frontend Component Reuse

Before creating a new component, search for existing equivalent.

Examples:

```text
ConfirmDialog

DataTable

PageHeader

ErrorAlert
```

---

# 98. Frontend Business Components

Keep module-specific components inside their module.

Do not move everything into `components/`.

---

# 99. API Calls

Use shared API client.

Do not scatter independent `fetch()` implementations.

---

# 100. Frontend Permission Logic

Use central permission helper.

Do not hardcode role names such as:

```text
if user.role == "Operator"
```

when Permission checks are appropriate.

---

# 101. Roles Are Configurable

Frontend behavior should normally depend on Permissions, not assumed hardcoded Role names.

Director is the special highest system role as documented.

---

# 102. Loading States

Every API-driven page should handle:

```text
Loading

Error

Empty

Loaded
```

---

# 103. Forms

Every mutation form should handle:

```text
Validation

Submitting

Server error

Success
```

---

# 104. Duplicate Click Protection

Disable create/save action while request is executing where duplicate creation is possible.

---

# 105. Stable URLs

Record workspaces must work after browser refresh.

Do not depend on navigation state alone.

---

# 106. Do Not Store Server Data Only in Memory

A route such as:

```text
/orders/{id}
```

must refetch the Order when loaded directly.

---

# 107. Frontend Types

Avoid `any`.

Match backend API contracts.

---

# 108. Enum Consistency

Do not use different status spellings across:

```text
Database

Backend

JSON

Frontend
```

Mapping must be explicit.

---

# 109. CSS/UI Libraries

Do not add a second UI framework alongside Material UI.

---

# 110. Visual Scope

Do not spend large task scope on visual polish when business behavior remains incomplete.

Functionality first.

---

# 111. User Experience Review

After functionality works, check:

```text
Can User find action?

Are labels clear?

Are important IDs visible?

Are errors understandable?

Are there unnecessary clicks?
```

---

# 112. Documentation Change

If implementation reveals a specification error, update the relevant document.

Do not simply code around the incorrect spec.

---

# 113. Documentation Consistency

When changing a requirement, inspect references in other docs.

Example:

Changing Order status list may affect:

```text
Orders Module

Reports

Frontend

Tests
```

---

# 114. Documentation Does Not Need Code Detail

Do not fill business specification documents with unnecessary implementation trivia.

Keep architecture docs useful.

---

# 115. AI Task Input Template

Recommended prompt structure:

```text
Read:
- AI_RULES.md
- relevant docs

Task:
[one clear feature]

Requirements:
[exact requirements]

Do not:
[scope boundaries]

Acceptance Criteria:
[testable outcomes]

Before finishing:
- build
- run relevant tests
- review migration if created
- summarize changed files
```

---

# 116. Example — Backend Client Task

```text
Read:
- AI_RULES.md
- docs/03_Database_Design.md
- docs/12_Clients_Module.md
- docs/19_API_Design_Guidelines.md
- docs/24_Backend_Architecture.md

Task:
Implement Client creation backend.

Requirements:
- generate CL-000001 Business ID
- Business ID immutable
- Name required
- create audit metadata
- enforce clients.create permission

Do not:
- implement Projects
- modify Authentication architecture
- add generic repositories

Acceptance Criteria:
- endpoint returns 201
- duplicate Business IDs impossible
- integration tests pass
```

---

# 117. Example — Frontend Project Task

```text
Read:
- AI_RULES.md
- docs/13_Projects_Module.md
- docs/19_API_Design_Guidelines.md
- docs/23_Frontend_Architecture.md

Task:
Implement Project Team section.

Requirements:
- Owner
- Assignee
- Participants
- Observers
- use active Employees for new selection
- read-only if User lacks projects.manage_team

Do not:
- create Order-specific team roles
- add Departments
```

---

# 118. Example — Calculator Task

```text
Read:
- AI_RULES.md
- docs/15_Calculator_Module.md
- docs/20_Testing_Strategy.md
- docs/24_Backend_Architecture.md

Task:
Implement formula dependency graph and circular-reference detection.

Requirements:
- support field references
- detect direct cycles
- detect indirect cycles
- return understandable validation error

Do not:
- implement JavaScript eval
- add external spreadsheet runtime

Acceptance Criteria:
- unit tests cover A→A
- A→B→A
- A→B→C→A
- valid acyclic graph
```

---

# 119. Completion Summary

At the end of a task, AI should summarize:

```text
What changed

Files changed

Migration created, if any

Tests run

Build result

Any limitations or follow-up required
```

---

# 120. Avoid Long Self-Congratulatory Summaries

Completion summaries should be concise and factual.

Do not write marketing-style claims about code quality.

---

# 121. Example Completion Summary

```text
Implemented Client creation.

Changed:
- Client entity/configuration
- CreateClient request/response
- Client creation service
- ClientsController
- EF migration
- Integration tests

Verification:
- dotnet build passed
- 8 Client tests passed

No unrelated modules were changed.
```

---

# 122. Report Partial Completion Clearly

If part of a task could not be completed:

```text
Completed X and Y.

Z remains incomplete because ...
```

Do not imply completion.

---

# 123. Do Not Hide Errors

If build/test fails, state it.

Do not report:

```text
Done
```

when verification failed.

---

# 124. Human Review

AI-generated database/security/calculator changes should be reviewed carefully before production.

The most critical areas are:

```text
Migrations

Authentication

Permissions

Financial calculation

Template versioning
```

---

# 125. Git Workflow

Use small logical commits.

Do not combine unrelated tasks into one commit when avoidable.

---

# 126. Commit Messages

Recommended format:

```text
feat(clients): add client creation

fix(auth): protect final director

feat(orders): add checklist items

test(calculator): cover circular formulas
```

Exact conventional-commit usage is recommended, not mandatory.

---

# 127. Commit Before Major Refactor

Before a risky refactor, ensure current working state is committed.

This makes rollback easier.

---

# 128. Do Not Commit Broken Builds

Normal feature commits should compile and have relevant tests passing.

---

# 129. Do Not Commit Secrets

Before commit, inspect for:

```text
.env

connection strings

passwords

tokens

local secrets
```

---

# 130. Generated Files

Do not commit generated artifacts that are excluded by project conventions.

Examples:

```text
bin

obj

node_modules

dist
```

unless deployment strategy explicitly requires otherwise.

---

# 131. Branching

A complex Git branching model is not required.

A simple feature-branch workflow is enough if branches are used.

---

# 132. Feature Completion

Merge only after:

```text
Review

Build

Tests

Migration review

Documentation consistency
```

---

# 133. AI Refactoring Tasks

Refactoring should have a specific purpose.

Examples:

```text
Remove duplicated permission checking.

Extract shared pagination model.

Split oversized OrderWorkspace component.
```

Do not ask AI simply:

```text
Refactor everything.
```

---

# 134. Refactoring Behavior Preservation

Unless explicitly changing requirements, refactoring must preserve behavior.

Tests should remain unchanged where possible.

---

# 135. Performance Optimization Tasks

Before optimizing:

```text
Measure

Identify bottleneck

Optimize specific area

Measure again
```

Do not optimize based on guesses.

---

# 136. Security Review Tasks

When reviewing security, examine:

```text
Authentication

Authorization

DTO exposure

Nested resource ownership

Financial fields

File uploads

Logging
```

---

# 137. Database Review Tasks

When reviewing schema, compare:

```text
EF entities

EF configurations

Migrations

PostgreSQL result

17_Database_Schema_Overview.md
```

---

# 138. Documentation Review Tasks

Periodically verify:

```text
Code terminology

API routes

Database schema

Permission codes

Statuses
```

still match docs.

---

# 139. AI Must Not Modify Documentation to Hide Bugs

If code incorrectly uses:

```text
Task
```

instead of:

```text
Order
```

do not update documentation to call it Task.

Fix the code.

---

# 140. Stable Architecture Rule

Once a pattern is working, keep it stable unless a real problem appears.

Constant AI-driven architecture changes create more risk than value.

---

# 141. No Technology Churn

Do not repeatedly replace:

```text
State libraries

Validation libraries

Database patterns

Routing patterns
```

without a concrete reason.

---

# 142. Version 1 Focus

The goal is:

```text
Usable Lithograph ERP Version 1
```

not:

```text
Perfect theoretical architecture
```

---

# 143. Stop Conditions

AI should stop a coding task and report rather than continue blindly when it discovers:

```text
Requirement conflicts with approved docs

Required migration would destroy existing data unexpectedly

Critical dependency is missing

Existing code architecture is materially different from docs

Security requirement cannot be satisfied safely with current design
```

---

# 144. Minor Ambiguity

For small implementation details not specified, AI may choose the simplest reasonable option consistent with existing patterns.

Example:

```text
Exact local variable name

Private helper method name

Minor layout spacing
```

---

# 145. Major Ambiguity

Do not invent major business rules.

Examples:

```text
New Order statuses

New financial fields

New User roles

New Project hierarchy

New Calculator behavior
```

These require documented decisions.

---

# 146. Human Decision Boundary

AI should support the developer by:

```text
Explaining options

Identifying tradeoffs

Implementing approved choice
```

not by silently making major business decisions.

---

# 147. AI Review Checklist — Before Coding

```text
[ ] Read AI_RULES.md

[ ] Read relevant docs

[ ] Inspect existing code

[ ] Understand task boundaries

[ ] Identify affected modules

[ ] Identify required Permission

[ ] Identify whether DB schema changes

[ ] Identify tests needed
```

---

# 148. AI Review Checklist — Before Finishing

```text
[ ] Code builds

[ ] Relevant tests pass

[ ] Migration reviewed

[ ] No secrets added

[ ] Authorization checked

[ ] Financial data exposure checked

[ ] No unrelated changes

[ ] Documentation still matches

[ ] Changed files reviewed
```

---

# 149. Database Task Checklist

```text
[ ] Correct schema

[ ] Correct table name

[ ] Correct UUID/composite PK

[ ] Correct nullability

[ ] Correct varchar lengths

[ ] Correct numeric precision

[ ] Correct foreign keys

[ ] Correct delete behavior

[ ] Correct indexes

[ ] Correct unique constraints

[ ] Migration reviewed
```

---

# 150. API Task Checklist

```text
[ ] Correct route

[ ] Correct HTTP method

[ ] Correct Permission

[ ] Safe request DTO

[ ] Safe response DTO

[ ] Validation

[ ] Expected errors

[ ] Pagination/filtering if required

[ ] API tests
```

---

# 151. Frontend Task Checklist

```text
[ ] Correct route

[ ] Correct Permission visibility

[ ] API integration uses shared client

[ ] Loading state

[ ] Error state

[ ] Empty state

[ ] Form validation

[ ] Duplicate submit prevented

[ ] TypeScript types correct

[ ] Relevant tests
```

---

# 152. Calculator Task Checklist

```text
[ ] Exact Template Version preserved

[ ] Published Version immutable

[ ] Formula safe

[ ] Decimal arithmetic

[ ] Circular dependency checked

[ ] Backend authoritative

[ ] Selling Price synchronized

[ ] Cost Items preserved

[ ] Financial permissions enforced

[ ] Historical behavior tested
```

---

# 153. Authentication Task Checklist

```text
[ ] No plaintext password

[ ] Secure hash

[ ] Session safety

[ ] Correct 401/403 behavior

[ ] Final Director protection

[ ] Username normalization

[ ] Sensitive DTO fields excluded

[ ] Security tests
```

---

# 154. Reports Task Checklist

```text
[ ] Read-only

[ ] Uses source data

[ ] Correct filters

[ ] Correct totals

[ ] Financial Permissions

[ ] Pagination totals over full filtered set

[ ] No duplicate report data tables
```

---

# 155. Production Readiness Boundary

AI must never assume local success means production deployment is safe.

Production deployment requires:

```text
Backup

Migration review

Production configuration

Smoke tests
```

according to `21_Deployment_and_Backup.md`.

---

# 156. Production Secrets

AI code should reference configuration keys, not actual production values.

---

# 157. Production Database

AI-assisted development should not run destructive experiments against production data.

---

# 158. Backup Before High-Risk Change

Before production schema/data operations:

```text
Backup first.
```

---

# 159. Development Database

Destructive reset is acceptable only in early/local development where migration/data history is intentionally disposable.

---

# 160. Version 1 Non-Goals for AI Workflow

Do not introduce:

```text
Autonomous self-modifying architecture

Automatic package replacement

Automatic production deployment

Automatic destructive migrations

Automatic database cleanup

Automatic undocumented refactoring
```

---

# 161. Final Development Principle

AI should behave as:

```text
A careful implementation partner
```

not:

```text
An autonomous architect with unlimited scope
```

The desired cycle is:

```text
Read
 ↓
Understand
 ↓
Inspect
 ↓
Plan
 ↓
Implement
 ↓
Test
 ↓
Review
 ↓
Report
```

The core rule is:

```text
Small verified changes
are better than
large impressive changes.
```

---

**End of Document**