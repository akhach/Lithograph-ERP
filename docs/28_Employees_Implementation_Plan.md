# Lithograph ERP

**Document:** 28_Employees_Implementation_Plan.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Employees

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `10_Users_Module.md`
- `11_Employees_Module.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `22_Logging_Audit_and_Operational_History.md`
- `23_Frontend_Architecture.md`
- `24_Backend_Architecture.md`
- `25_Development_Workflow_for_AI.md`
- `27_Authentication_Implementation_Plan.md`

---

# 1. Purpose

This document defines the implementation sequence for the Employees module.

The Employees module manages people who work for Lithograph.

It establishes the practical separation between:

```text id="k2x1sz"
User
=
ERP login/access account
```

and:

```text id="a5qtms"
Employee
=
person working for Lithograph
```

An Employee may exist without a User account.

---

# 2. Main Implementation Goal

After this phase, Lithograph ERP must be able to:

```text id="c17euk"
Create Employees

Edit Employees

Activate/Deactivate Employees

Optionally link an Employee to a User

Unlink Employee from User

Display Employee information

Use Employees later in Project Team assignments
```

---

# 3. Module Boundary

Employees owns:

```text id="fkjbfe"
employees.employees
```

Authentication continues to own:

```text id="prc1dk"
auth.users
```

Employees must not move User authentication fields into its own schema.

---

# 4. Version 1 Employee Fields

Implement exactly:

```text id="70m1sk"
id

user_id

full_name

position

phone

email

is_active

created_at

created_by

updated_at

updated_by
```

---

# 5. Fields Not Included

Do not add:

```text id="xal80r"
Employee Business ID

First Name

Middle Name

Last Name

Department

Salary

Hire Date

Birthday

Address

Photo

HR records

Payroll fields
```

unless future requirements explicitly add them.

---

# 6. Recommended Implementation Sequence

```text id="afj027"
Task 1
Employee entity

Task 2
EF Core configuration

Task 3
Employees migration

Task 4
Employee permissions

Task 5
Employee backend queries

Task 6
Create Employee

Task 7
Update Employee

Task 8
Activate/Deactivate

Task 9
Link/Unlink User

Task 10
Employee selector endpoint

Task 11
Frontend Employee Administration

Task 12
User-link UI

Task 13
Tests

Task 14
Integration/security review
```

---

# 7. Task 1 — Employee Entity

Create the Employee entity inside the Employees module.

C# concept:

```text id="5rmm6y"
Employee
```

Database table:

```text id="fp415d"
employees.employees
```

---

# 8. Employee Primary Key

Use:

```text id="p9rxoy"
uuid
```

mapped to the normal C# UUID type.

Do not introduce an Employee Business ID.

---

# 9. Full Name

`full_name` is required.

Maximum length:

```text id="dt65db"
200
```

Do not split it into several name fields in Version 1.

---

# 10. Position

`position` is optional free text.

Maximum length:

```text id="1fcmkn"
150
```

Examples:

```text id="m7sguc"
Designer

Operator

Production Manager
```

Do not create a Positions table.

---

# 11. Phone

`phone` is optional.

Maximum length:

```text id="jtmj9o"
50
```

Do not build advanced phone normalization in Version 1.

---

# 12. Email

`email` is optional.

Maximum length:

```text id="jg45w8"
200
```

Employee email is contact information.

It is not an Authentication login field.

---

# 13. Active State

`is_active` is required.

Default:

```text id="zm1mdf"
true
```

Inactive Employees remain historically valid.

---

# 14. User Link

`user_id` is nullable.

Relationship:

```text id="ndgzw7"
employees.employees.user_id
→ auth.users.id
```

---

# 15. One-to-One Rule

When `user_id` is present:

```text id="vya24r"
One User
can link to
maximum one Employee
```

Enforce with a unique constraint/index.

---

# 16. Employee Without Login

Valid:

```text id="beqg3w"
Employee
user_id = NULL
```

This is normal.

Many Employees may not need ERP access.

---

# 17. User Without Employee

Also valid:

```text id="h6gofa"
User
without Employee link
```

Authentication must not require every User to have an Employee.

---

# 18. User and Employee Active States

These states are independent.

Example:

```text id="h623rr"
Employee inactive
User active
```

is technically possible.

Likewise:

```text id="l9rq29"
Employee active
User inactive
```

is possible.

Do not automatically mirror the states.

---

# 19. Task 2 — EF Core Configuration

Configure:

```text id="te67r9"
schema = employees

table = employees
```

with explicit lengths, keys, indexes, and foreign keys.

---

# 20. User Link Constraint

Configure unique:

```text id="qi4az3"
user_id
```

when non-null.

PostgreSQL allows multiple null values under normal unique behavior.

---

# 21. User Link Delete Behavior

Recommended:

```text id="etotmy"
ON DELETE SET NULL
```

for exceptional physical User deletion.

Normal User workflow remains deactivation.

---

# 22. Audit Relationships

`created_by` and `updated_by` reference:

```text id="p3iq91"
auth.users.id
```

Use safe delete behavior.

Never cascade-delete Employee because an audit User disappears.

---

# 23. Employee Physical Delete

Do not implement normal Employee deletion.

Version 1 uses:

```text id="bsax20"
is_active
```

for lifecycle.

---

# 24. Task 3 — Employees Migration

Recommended migration name:

```text id="6y1yfa"
AddEmployeesModule
```

---

# 25. Migration Scope

Migration should create only:

```text id="4x71op"
employees schema

employees.employees
```

and required constraints/indexes.

---

# 26. Migration Review

Verify:

```text id="tccgwy"
UUID PK

user_id nullable

unique user_id

correct varchar lengths

is_active default

audit FKs

no Employee Business ID

no departments table
```

---

# 27. Task 4 — Employee Permissions

Register:

```text id="wweev4"
employees.view

employees.create

employees.edit

employees.activate

employees.link_user
```

---

# 28. Director Permissions

Director must automatically receive the new Employee permissions through the established Authentication permission synchronization mechanism.

Do not manually special-case Director in Employee code.

---

# 29. Task 5 — Employee List Backend

Implement:

```text id="f0dy40"
GET /api/employees
```

Requires:

```text id="63vd9j"
employees.view
```

---

# 30. Employee List DTO

Recommended fields:

```text id="unmz4d"
id

fullName

position

phone

email

isActive

linkedUser
```

---

# 31. Linked User Summary

If linked, return a compact User summary.

Example:

```text id="yd2jtn"
linkedUser:
{
    id,
    username,
    isActive
}
```

Do not return Roles/Permissions unless screen needs them.

---

# 32. Employee Search

Initial search should support:

```text id="26ikjd"
Full Name

Position

Phone

Email

Linked Username
```

where practical.

---

# 33. Employee Filters

Useful filters:

```text id="yz1j69"
Active

Inactive

All
```

Optionally:

```text id="4ymrzg"
Has User

No User
```

if useful for Administration.

---

# 34. Employee Pagination

Use standard server-side pagination.

Do not load all Employees merely because the initial company size is small.

---

# 35. Employee Sorting

Useful sort fields:

```text id="fm40fz"
full_name

position

is_active
```

Whitelist allowed fields.

---

# 36. Task 6 — Get Employee

Implement:

```text id="zhr8r4"
GET /api/employees/{id}
```

Requires:

```text id="eaybmx"
employees.view
```

---

# 37. Employee Detail DTO

May include:

```text id="kq51n6"
id

fullName

position

phone

email

isActive

linkedUser

createdAt

updatedAt
```

Audit User names may be added if useful.

---

# 38. Task 7 — Create Employee

Implement:

```text id="qaaaym"
POST /api/employees
```

Requires:

```text id="xgrsvb"
employees.create
```

---

# 39. Create Employee Request

Allowed fields:

```text id="7q3f88"
fullName

position

phone

email

userId
```

`isActive` may default to true rather than being client-controlled during normal creation.

---

# 40. Create Validation

Validate:

```text id="dq5906"
fullName required

fullName trimmed

string lengths

userId exists if supplied

userId not already linked
```

---

# 41. Linking During Creation

If `userId` is supplied, the current User must also have:

```text id="qc4021"
employees.link_user
```

Do not let `employees.create` alone imply User-link permission.

---

# 42. Create Audit

Set:

```text id="tb42wb"
created_at

created_by
```

from backend/current authenticated User.

---

# 43. Task 8 — Update Employee

Implement:

```text id="z0dgp6"
PATCH /api/employees/{id}
```

Requires:

```text id="dtadpc"
employees.edit
```

---

# 44. Editable General Fields

Normal Employee edit may change:

```text id="f4ajkx"
fullName

position

phone

email
```

---

# 45. User Link Not Normal Edit

Do not silently allow `userId` to change through ordinary Employee PATCH unless the API explicitly decides to combine it.

Preferred:

```text id="ge6ftv"
General Employee update
```

and:

```text id="mazed9"
Link User / Unlink User
```

as separate operations.

---

# 46. Update Audit

Successful general edit updates:

```text id="rp8wy6"
updated_at

updated_by
```

---

# 47. Task 9 — Activate Employee

Implement:

```text id="j7k3bn"
POST /api/employees/{id}/activate
```

Requires:

```text id="d3lw8d"
employees.activate
```

---

# 48. Deactivate Employee

Implement:

```text id="wby79x"
POST /api/employees/{id}/deactivate
```

Requires:

```text id="6dt7sz"
employees.activate
```

---

# 49. Deactivation Behavior

Deactivation:

```text id="1x390v"
is_active = false
```

It must not:

```text id="o46vjs"
Delete Employee

Remove Project history

Automatically deactivate linked User
```

---

# 50. Linked Active User Warning

If deactivating an Employee linked to an active User, return enough information for frontend to warn before final confirmation where appropriate.

The business rule remains:

```text id="s5im2r"
Employee deactivation does not automatically deactivate User.
```

---

# 51. Simple Deactivation Flow

Recommended UI:

```text id="fsbd0k"
Deactivate Employee?

This Employee has an active ERP User account.

The User account will remain active.
```

Then allow authorized User to proceed.

---

# 52. Task 10 — Link User

Implement explicit endpoint.

Conceptually:

```text id="fwdrct"
POST /api/employees/{employeeId}/link-user
```

Requires:

```text id="vllvto"
employees.link_user
```

---

# 53. Link User Request

```text id="k07pwo"
userId
```

---

# 54. Link Validation

Backend must verify:

```text id="krmmpp"
Employee exists

User exists

User not already linked to another Employee

Employee not already linked to another User
```

---

# 55. Replacing Existing Link

Version 1 should not silently replace one User link with another.

If Employee already has a User:

```text id="f7erpy"
Unlink first
```

then create the new link.

This keeps behavior explicit.

---

# 56. Link Audit

Linking User changes the Employee record.

Update:

```text id="yh6749"
updated_at

updated_by
```

---

# 57. Unlink User

Implement:

```text id="yzy3kk"
DELETE /api/employees/{employeeId}/user-link
```

Requires:

```text id="gg17kc"
employees.link_user
```

---

# 58. Unlink Behavior

Set:

```text id="zz631l"
user_id = NULL
```

Do not:

```text id="fbrkog"
Delete User

Deactivate User

Change User roles
```

---

# 59. Authentication Independence

Employees module must not directly manage:

```text id="oae2ar"
Passwords

Roles

Permissions

Sessions
```

These remain Authentication responsibilities.

---

# 60. Task 11 — Employee Selector Backend

Projects will later need Employee selectors.

Prepare a simple endpoint or query behavior suitable for selection.

---

# 61. Active Employee Selector

Conceptual endpoint:

```text id="97xms0"
GET /api/employees?is_active=true
```

may be sufficient.

No special endpoint is required if the normal Employees API supports lightweight filtering/projection well.

---

# 62. Selector DTO

Use compact data:

```text id="8v22gh"
id

fullName

position
```

Avoid returning full contact/audit information to selectors.

---

# 63. Selector Display

Frontend should render:

```text id="dm4p57"
Full Name — Position
```

where Position exists.

---

# 64. Historical Employee Retrieval

Although new selectors show active Employees, direct retrieval of inactive Employees must remain possible for historical Projects.

---

# 65. Task 12 — Frontend Employees Route

Add:

```text id="56kp9h"
/admin/employees
```

under Administration.

Requires:

```text id="v8al13"
employees.view
```

---

# 66. Administration Navigation

Show:

```text id="1b05pm"
Administration → Employees
```

only when User has:

```text id="cj4ov6"
employees.view
```

---

# 67. Employee List UI

Recommended columns:

```text id="6ad1u0"
Full Name

Position

Phone

Email

Linked User

Status
```

---

# 68. Employee Status

Display:

```text id="zam1hg"
Active

Inactive
```

consistently with other master-data screens.

---

# 69. Employee Search UI

Provide simple search.

Do not build advanced HR filtering.

---

# 70. Create Employee UI

Fields:

```text id="ko5j2y"
Full Name *

Position

Phone

Email

Linked User
```

Linked User may be optional.

---

# 71. Create Employee User-Link Permission

If current User lacks:

```text id="t35f69"
employees.link_user
```

do not show the User selector during creation.

Employee may still be created without login.

---

# 72. Employee Edit UI

Allow general editing based on:

```text id="ohak3s"
employees.edit
```

---

# 73. Activation Controls

Show Activate/Deactivate only with:

```text id="74nj3h"
employees.activate
```

---

# 74. User Link UI

If User has:

```text id="i7wnq9"
employees.link_user
```

provide:

```text id="k36c0y"
Link User

Unlink User
```

actions.

---

# 75. Available User Selector

When linking, show only Users not already linked to another Employee.

Backend must still validate this.

---

# 76. User Selector Display

Recommended:

```text id="ahjq4p"
username
```

and optionally status.

Example:

```text id="h7tqu8"
operator1 — Active
```

---

# 77. Linking Inactive User

Version 1 may allow linking an inactive User because Employee/User state is independent.

However, UI should show clearly that the User is inactive.

Do not silently activate the User.

---

# 78. Employee Details

A dedicated Employee detail page is optional in Version 1.

A list + edit dialog/page may be sufficient because the entity is simple.

Do not build an elaborate Employee Workspace.

---

# 79. No HR Dashboard

Do not implement:

```text id="1s27kf"
Attendance

Vacation

Payroll

Performance reviews

HR reports
```

---

# 80. Task 13 — Unit Tests

Pure Employee logic is limited.

Unit tests may cover helpers/validation if meaningful.

Do not manufacture low-value tests solely for coverage percentage.

---

# 81. Task 14 — Integration Tests

Integration tests are more important for Employees.

---

# 82. Required Employee Creation Tests

```text id="1e4pm4"
Create Employee without User

Create Employee with User

Full Name required

Audit fields set
```

---

# 83. User Link Uniqueness Tests

```text id="8yh1u6"
User linked to Employee A

Attempt link same User to Employee B

→ rejected
```

Database constraint and application validation should both protect this.

---

# 84. Employee Multiple-User Test

```text id="t3xjkg"
Employee linked to User A

Attempt link User B without unlinking

→ rejected
```

---

# 85. Unlink Test

```text id="s2n90w"
Employee linked to User

Unlink

Employee remains

User remains

user_id becomes null
```

---

# 86. Deactivation Test

```text id="u88u99"
Deactivate Employee

Employee remains in database

is_active = false
```

---

# 87. User Independence Test

```text id="ykgv4n"
Employee linked to active User

Deactivate Employee

User remains active
```

---

# 88. Reverse Independence Test

```text id="pw4fjd"
Employee active

Linked User deactivated

Employee remains active
```

Authentication module behavior must not automatically alter Employee.

---

# 89. Historical Employee Test

Later, when Projects exist, integration coverage should confirm inactive Employees remain visible in existing Project Team relationships.

That cross-module test belongs mainly to Projects implementation.

---

# 90. Permission Tests

Required:

```text id="d0b5kt"
employees.view

employees.create

employees.edit

employees.activate

employees.link_user
```

For each sensitive operation, test:

```text id="or7p98"
Authorized → succeeds

Unauthorized → 403
```

---

# 91. Unauthenticated Tests

Employee API requires authenticated access.

Unauthenticated requests return:

```text id="mw08wx"
401
```

---

# 92. DTO Security Test

Employee API must not expose:

```text id="kao9nx"
password_hash

Session information

Permission internals
```

from linked User.

---

# 93. Database Constraint Test

Directly verify unique:

```text id="mkqxne"
employees.employees.user_id
```

constraint where applicable.

---

# 94. Migration Test

Apply Authentication + Employees migrations to a clean PostgreSQL database.

Verify both schemas work together.

---

# 95. API Tests

Test:

```text id="domuov"
GET list

GET detail

POST create

PATCH edit

Activate

Deactivate

Link User

Unlink User
```

---

# 96. Search Tests

Verify search does not return unrelated Employees and supports expected fields.

---

# 97. Active Filter Test

```text id="h7frpi"
is_active=true
```

returns only active Employees.

This becomes important for Project selectors.

---

# 98. Audit Tests

Verify:

```text id="2s5lti"
created_by

created_at

updated_by

updated_at
```

are correctly maintained.

---

# 99. Read Audit Test

Loading an Employee must not change:

```text id="63vxgo"
updated_at
```

---

# 100. Employee Implementation Commits

Recommended commits:

```text id="gpyc2v"
feat(employees): add employee schema

feat(employees): add employee CRUD

feat(employees): add activation workflow

feat(employees): add optional user linking

test(employees): add employee integration tests

feat(frontend): add employee administration
```

---

# 101. First AI Coding Task

Recommended:

```text id="1bjr4d"
Read:
- AI_RULES.md
- docs/03_Database_Design.md
- docs/11_Employees_Module.md
- docs/17_Database_Schema_Overview.md
- docs/20_Testing_Strategy.md
- docs/24_Backend_Architecture.md
- docs/25_Development_Workflow_for_AI.md
- docs/28_Employees_Implementation_Plan.md

Task:
Implement only the Employees database model.

Create:
- Employee entity
- EF Core configuration
- employees schema/table migration
- User FK
- unique optional user_id
- audit FKs
- database integration tests

Do not implement:
- Employee API
- frontend
- Projects
- HR functionality
- Departments
- Employee Business ID

Before finishing:
- build
- apply migration to clean PostgreSQL test database
- run tests
- review migration
```

---

# 102. Second AI Coding Task

```text id="cffvw1"
Implement Employee list, detail, create, and edit backend.

Use existing API, authorization, validation, and error patterns.

Do not implement User linking yet.
```

---

# 103. Third AI Coding Task

```text id="tdf04w"
Implement Employee activate/deactivate behavior and tests.
```

---

# 104. Fourth AI Coding Task

```text id="ptwjw4"
Implement Employee ↔ User link/unlink behavior and tests.
```

---

# 105. Fifth AI Coding Task

```text id="h38c5b"
Implement Administration → Employees frontend using existing table/form patterns.
```

---

# 106. Employees Completion Gate

Do not begin Clients until:

```text id="02gyf3"
Employee schema works

Employee list works

Employee creation works

Employee editing works

Activate/Deactivate works

Optional User link works

User uniqueness works

Employee/User active states remain independent

Permissions work

Frontend works

Integration tests pass
```

---

# 107. Employee Selector Readiness

Before Projects implementation, confirm frontend/backend can retrieve active Employees efficiently for:

```text id="hl6kjw"
Owner

Assignee

Participant

Observer
```

selectors.

---

# 108. No Project Logic Yet

Employees phase must not create:

```text id="nj618d"
Project Team

Project Owner

Project Assignee
```

Those belong to Projects.

---

# 109. No Checklist Assignment

Do not connect Employees to Order Checklist Items.

Version 1 Checklist has no employee assignment.

---

# 110. No Order Team

Do not create direct Employee ↔ Order assignment during this phase.

Version 1 responsibility comes through Project Team.

---

# 111. No Employee Financial Access Fields

Do not store permissions such as:

```text id="tgfqho"
can_view_cost

can_view_price
```

on Employee.

Financial access belongs to Authentication Roles/Permissions on User.

---

# 112. No Employee Role Field for Security

`position` is business information.

It must not be used as Authentication authorization.

Example:

```text id="c8i6dw"
position = Operator
```

does not automatically grant an Operator Role.

---

# 113. User Roles vs Employee Position

Keep separate:

```text id="zufrj7"
Employee Position
=
Business/job description
```

```text id="6qdu1c"
User Role
=
ERP access grouping
```

---

# 114. Example

```text id="sambk9"
Employee:
Samvel
Position:
Production Manager

Linked User:
samvel

User Roles:
Operator
Project Manager
```

These are separate concepts.

---

# 115. Future Employee Extensions

Possible future additions may include:

```text id="al0r7c"
Departments

Skills

Shift scheduling

Salary

HR data

Profile photo
```

They are not Version 1 requirements.

---

# 116. Employee Simplicity Rule

Before adding an Employee field, ask:

```text id="yhy0te"
Is this required for Projects or current ERP operations?
```

If not, defer it.

---

# 117. Final Employees Principle

The Employees module should answer:

```text id="zclwbf"
Who works for Lithograph?
```

```text id="31vbmp"
What is their basic work/contact information?
```

and:

```text id="5n1f60"
Do they have an ERP User account?
```

It should not answer:

```text id="qaoovm"
What permissions do they have?
```

through Employee data.

That remains an Authentication responsibility.

The central rule is:

```text id="1nxw9x"
User controls access.

Employee represents the person.
```

---

**End of Document**