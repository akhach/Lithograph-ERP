# Lithograph ERP

**Document:** 27_Authentication_Implementation_Plan.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Authentication

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `07_Authentication.md`
- `08_Auth_Schema_Design.md`
- `09_Auth_Tables.md`
- `10_Users_Module.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `22_Logging_Audit_and_Operational_History.md`
- `24_Backend_Architecture.md`
- `25_Development_Workflow_for_AI.md`
- `26_Initial_Project_Setup.md`

---

# 1. Purpose

This document defines the implementation sequence for the Lithograph ERP Authentication module.

The module includes:

```text id="qv3q9e"
Users

Roles

Permissions

User Role assignments

Role Permission assignments

Sessions

First-run Director setup

Login

Logout

Current User

Password change

Administrative password reset

User Administration

Authorization policies
```

Authentication must be completed and tested before normal business modules begin.

---

# 2. Core Authentication Principle

Authentication answers:

```text id="odrt5f"
Who is using the ERP?
```

Authorization answers:

```text id="j0q1o3"
What is that User allowed to do?
```

Employee management is separate.

Do not combine Authentication implementation with Employee implementation.

---

# 3. Version 1 Authentication Model

Version 1 uses:

```text id="446x1k"
Local Username

Password

Server-managed Session

Roles

Permissions
```

It does not use:

```text id="oqxtld"
Email login

Public registration

Google login

Microsoft login

OAuth

Social login
```

---

# 4. Initial Authentication Milestone

The first Authentication milestone is complete when:

```text id="4t96lu"
Fresh database starts

Initial Setup appears

Director password can be created

director User is created

Director logs in

Director can create another User

Created User can log in

Logout works

Permissions are enforced
```

---

# 5. Recommended Implementation Sequence

Implement Authentication in this order:

```text id="5i19lr"
Task 1
Authentication domain/constants

Task 2
Auth database entities

Task 3
EF Core configuration

Task 4
Initial auth migration

Task 5
Permission catalog

Task 6
Director Role bootstrap data

Task 7
Password hashing

Task 8
Session infrastructure

Task 9
First-run Director setup

Task 10
Login

Task 11
Current User

Task 12
Logout

Task 13
Authorization/permission policies

Task 14
User Administration

Task 15
Roles Administration

Task 16
Password operations

Task 17
Final Director protection

Task 18
Frontend Authentication

Task 19
Frontend User Administration

Task 20
Authentication integration tests

Task 21
Security review
```

---

# 6. Task 1 — Authentication Domain Constants

## Goal

Create stable Authentication concepts before database/API code.

---

# 7. Initial Concepts

Define stable concepts for:

```text id="wqovh6"
Director Role

Permission Codes

Session state if needed
```

Do not hardcode permissions randomly throughout Controllers.

---

# 8. Director Role Identity

Director is the highest system Role.

Human-readable Role name:

```text id="6qu9hq"
Director
```

Use a stable normalized representation internally.

---

# 9. Permission Codes

Permission format:

```text id="pr4u43"
module.action
```

Examples:

```text id="x3a9ux"
users.view

users.create

users.edit

users.activate

users.reset_password

users.manage_roles
```

---

# 10. Initial Authentication Permissions

At minimum implement:

```text id="bp2qxr"
users.view

users.create

users.edit

users.activate

users.reset_password

users.manage_roles

roles.view

roles.create

roles.edit

roles.manage_permissions
```

Additional module permissions are added as those modules are implemented.

---

# 11. Permission Registry

Maintain one explicit permission catalog in backend code.

Conceptually:

```text id="h1tl67"
Permissions.Users.View

Permissions.Users.Create

Permissions.Users.Edit
```

or equivalent.

Do not scatter raw strings everywhere.

---

# 12. Task 2 — Authentication Entities

Implement entities matching:

```text id="dqze9b"
auth.users

auth.roles

auth.permissions

auth.user_roles

auth.role_permissions

auth.sessions
```

No additional Authentication tables are required for Version 1.

---

# 13. User Entity

Implement fields exactly according to `09_Auth_Tables.md`.

Core fields include:

```text id="278m3n"
id

username

normalized_username

password_hash

is_active

last_login_at

created_at

created_by

updated_at

updated_by
```

---

# 14. Do Not Add User Business Fields

Do not add:

```text id="5stnmi"
full_name

position

phone

employee_number

department
```

to `auth.users`.

Those belong to Employees.

---

# 15. Role Entity

Role fields should follow the Authentication schema document.

Role represents a reusable set of Permissions.

---

# 16. Permission Entity

Permission contains stable application permission definitions.

Permission Code must be unique.

---

# 17. UserRole Entity

Represents:

```text id="hynwy1"
User ↔ Role
```

Use composite key:

```text id="18r296"
(user_id, role_id)
```

---

# 18. RolePermission Entity

Represents:

```text id="z4csck"
Role ↔ Permission
```

Use composite key:

```text id="gexbgo"
(role_id, permission_id)
```

---

# 19. Session Entity

Session represents one authenticated application session.

It must support:

```text id="7gqyws"
Session creation

Expiration

Revocation

Logout

Password-reset invalidation
```

---

# 20. Session Secret Storage

Never store a raw authentication Session token if a safer hashed-token design is used.

The exact implementation should use established security patterns.

Do not invent custom cryptography.

---

# 21. Task 3 — EF Core Configuration

Explicitly configure:

```text id="m6ijk2"
auth schema

Table names

UUID keys

String lengths

Unique indexes

Foreign keys

Delete behavior

Composite keys
```

---

# 22. Username Unique Constraint

Enforce case-insensitive uniqueness through:

```text id="vwp5cb"
normalized_username
```

with a unique index.

---

# 23. Role Name Unique Constraint

Role normalized name must be unique.

---

# 24. Permission Code Constraint

Enforce:

```text id="7ygxiz"
UNIQUE(permission.code)
```

---

# 25. User Role Constraint

Composite PK automatically prevents duplicate:

```text id="ngclvh"
same User
+
same Role
```

assignment.

---

# 26. Role Permission Constraint

Composite PK prevents duplicate Permission assignment to the same Role.

---

# 27. Session Indexes

Useful indexes may include:

```text id="w7qcmr"
user_id

token_hash / session identifier

expires_at
```

depending on exact Session implementation.

---

# 28. Delete Behavior

Deleting/deactivating a User must not cascade-delete unrelated business data.

Authentication assignments/Sessions may use deliberate cleanup behavior.

---

# 29. Audit User Self-Reference

`auth.users.created_by` and `updated_by` may reference:

```text id="hj24z5"
auth.users.id
```

and must allow null for bootstrap.

---

# 30. Task 4 — Initial Auth Migration

Create the first meaningful business migration.

Recommended migration name:

```text id="5xgzcj"
AddAuthenticationModule
```

---

# 31. Migration Review

Verify migration creates only:

```text id="o9r5yk"
auth schema

auth.users

auth.roles

auth.permissions

auth.user_roles

auth.role_permissions

auth.sessions
```

plus required indexes/FKs.

---

# 32. Migration Must Not Create

Do not create:

```text id="b0qpsd"
employees

clients

projects

orders

calculator
```

during this migration.

---

# 33. Migration Test

Apply migration to a clean PostgreSQL test database.

Confirm schema matches documentation.

---

# 34. Task 5 — Permission Catalog Registration

Implement a mechanism that synchronizes application-defined Permissions into:

```text id="qajkuf"
auth.permissions
```

---

# 35. Permission Source of Truth

Permission codes originate from application code.

Users must not freely invent arbitrary Permission codes through UI in Version 1.

---

# 36. Permission Synchronization

On controlled startup/migration/bootstrap:

```text id="ldjtcd"
Defined Permissions
      ↓
Ensure rows exist
```

Do not delete historical/unknown Permission rows automatically without deliberate migration logic.

---

# 37. Permission Metadata

A Permission may include:

```text id="cct2zv"
code

name/description

module/group
```

according to the auth schema design.

Use enough metadata for Role Administration UI.

---

# 38. Task 6 — Director Role Initialization

Ensure the system-level:

```text id="hqj01m"
Director
```

Role exists.

---

# 39. Director Permissions

Director should effectively have all current Permissions.

Preferred implementation should ensure future Permissions also become available to Director automatically.

---

# 40. Director Future-Proofing

Avoid requiring manual migration such as:

```text id="74zkye"
Add new Permission
Then remember to manually add it to Director
```

every time a module grows.

---

# 41. Possible Implementation

One acceptable strategy:

```text id="u38d5m"
Permission synchronization
      ↓
Ensure Director Role
      ↓
Ensure Director has all registered Permissions
```

The exact implementation may vary.

---

# 42. Task 7 — Password Hashing

Use established ASP.NET Core password hashing.

Do not implement:

```text id="zn8um2"
SHA256(password)

MD5

Custom salt logic

Reversible password encryption
```

---

# 43. Password Hashing Service

Create a narrow Authentication service/component for:

```text id="uxf4se"
Hash password

Verify password
```

using established framework security.

---

# 44. Password Storage

Only:

```text id="87gx7s"
password_hash
```

is stored.

Never store:

```text id="3ywsml"
plaintext password

encrypted recoverable password
```

---

# 45. Password Validation

Version 1 should use a sensible password policy.

Do not overcomplicate it with enterprise-style rules unless required.

At minimum prevent extremely weak/empty passwords.

---

# 46. Password Confirmation

Password confirmation is a frontend/user-experience concern.

Backend should receive the intended new password once and validate it.

---

# 47. Task 8 — Session Infrastructure

Implement Session creation and validation.

---

# 48. Session Requirements

A Session should support:

```text id="qyba5r"
User association

Created time

Expiration

Revocation

Last activity if defined

IP/User Agent if defined by schema
```

---

# 49. Session Cookie

For browser ERP, secure cookie-based Session handling is appropriate.

Production cookie should use appropriate:

```text id="tud10r"
HttpOnly

Secure

SameSite
```

configuration.

---

# 50. JavaScript Access

Frontend should not need direct access to raw Session secret.

Prefer HttpOnly cookie behavior when using cookie-based Sessions.

---

# 51. Session Validation

For each authenticated request:

```text id="vzf7lx"
Read Session credential

Validate Session

Validate not expired

Validate not revoked

Load User

Validate User active
```

---

# 52. Inactive User

If:

```text id="qq2mn8"
user.is_active = false
```

existing Sessions must no longer authorize normal access.

---

# 53. Session Expiration

Session lifetime must be centralized configuration.

Do not scatter expiration duration throughout code.

---

# 54. Session Cleanup

Expired Session rows may remain temporarily and be cleaned later.

No complex cleanup infrastructure is required initially.

---

# 55. Task 9 — First-Run Director Setup

Implement first-run setup.

---

# 56. Setup Availability

Initial setup is available only when:

```text id="206up7"
auth.users contains zero rows
```

---

# 57. Setup Request

User provides:

```text id="ngitkl"
Director password
```

Username is fixed initially as:

```text id="v9lc13"
director
```

---

# 58. Setup Transaction

First-run setup should transactionally:

```text id="5enbvz"
Verify zero Users

Ensure Director Role exists

Create director User

Hash password

Assign Director Role

Commit
```

---

# 59. Bootstrap Concurrency

Two simultaneous first-setup requests must not create multiple bootstrap Users.

Protect through transaction/database constraints.

---

# 60. Bootstrap Audit

First Director:

```text id="flfqj4"
created_by = NULL
```

because no authenticated User exists.

---

# 61. Setup Response

After successful setup, the application may:

```text id="0pqyqd"
Require normal login
```

or:

```text id="uwmngb"
Create Session automatically
```

Choose one consistent implementation.

A normal login after setup is simpler to reason about.

---

# 62. Setup Endpoint

Conceptual endpoint:

```text id="4u5baf"
POST /api/auth/setup
```

---

# 63. Setup Status Endpoint

Frontend may need to know whether setup is required.

Conceptual:

```text id="7cjp84"
GET /api/auth/setup-status
```

Response:

```text id="6k9nqn"
requiresSetup: true/false
```

---

# 64. Setup Security

Once at least one User exists:

```text id="tddh5l"
/api/auth/setup
```

must reject further setup attempts.

---

# 65. Task 10 — Login

Implement:

```text id="6wp1fm"
POST /api/auth/login
```

---

# 66. Login Request

```text id="0el6yx"
username

password
```

---

# 67. Login Normalization

Normalize username before lookup.

Examples:

```text id="a5e0p1"
director

Director

DIRECTOR
```

must resolve consistently.

---

# 68. Login Validation

Login succeeds only if:

```text id="nmc4zc"
User exists

User active

Password correct
```

---

# 69. Login Failure

Use a generic failure message where appropriate.

Do not reveal unnecessarily whether:

```text id="y5z6ll"
Username exists
```

or password was wrong.

---

# 70. Successful Login

On success:

```text id="boecae"
Create Session

Set secure Session cookie

Update last_login_at

Return safe User context
```

---

# 71. Login Logging

Successful and failed login events may be logged according to `22_Logging_Audit_and_Operational_History.md`.

Never log the password.

---

# 72. Task 11 — Current User

Implement:

```text id="3aqsn5"
GET /api/auth/me
```

---

# 73. Current User Response

May include:

```text id="6kvg8e"
id

username

roles

permissions

linked_employee
```

Employee link will initially be null/unavailable until Employees module exists.

Do not add Employee implementation here.

---

# 74. Permission Resolution

`/api/auth/me` should return effective Permissions in a frontend-friendly form.

Example:

```text id="1sm72m"
permissions:
[
  "users.view",
  "users.create"
]
```

---

# 75. Task 12 — Logout

Implement:

```text id="28j4yp"
POST /api/auth/logout
```

---

# 76. Logout Behavior

Logout should:

```text id="26ertp"
Revoke/delete current Session

Clear Session cookie
```

After logout, the Session must no longer authorize requests.

---

# 77. Task 13 — Authorization Infrastructure

Implement centralized Permission-based authorization.

---

# 78. Authorization Requirement

Endpoints should declare required Permissions clearly.

Example:

```text id="tcl74x"
users.create
```

---

# 79. Authorization Policy

Use ASP.NET Core policy/handler infrastructure or similarly centralized approach.

Avoid repeated manual database permission checks inside every Controller.

---

# 80. Permission Resolution Path

Conceptually:

```text id="k3s5ij"
Session
   ↓
User
   ↓
User Roles
   ↓
Role Permissions
   ↓
Effective Permissions
```

---

# 81. Multiple Roles

A User may have multiple Roles.

Effective Permissions are the union of Permissions granted by those Roles.

---

# 82. No Direct User Permissions

Do not implement:

```text id="91b24c"
auth.user_permissions
```

in Version 1.

---

# 83. Authorization Response Codes

Unauthenticated:

```text id="z8khcv"
401
```

Authenticated without Permission:

```text id="0mn682"
403
```

---

# 84. Permission Caching

Do not introduce complicated distributed permission caches.

Simple per-request/session behavior is sufficient initially.

---

# 85. Permission Changes

If a Role/Permission assignment changes, it should take effect predictably.

Avoid long-lived stale Permission state.

---

# 86. Task 14 — User Administration Backend

Implement according to:

```text id="zkcmxl"
10_Users_Module.md
```

---

# 87. User List

Implement:

```text id="muc75v"
GET /api/users
```

Requires:

```text id="80u7fz"
users.view
```

---

# 88. Create User

Implement:

```text id="srtj14"
POST /api/users
```

Requires:

```text id="eb35m7"
users.create
```

---

# 89. Create User Fields

Request should contain only approved fields such as:

```text id="kab3cp"
username

password

role_ids
```

depending on finalized DTO design.

---

# 90. User Creation Behavior

Backend:

```text id="in5p3o"
Normalize username

Check uniqueness

Hash password

Create User

Assign selected Roles

Set audit fields
```

transactionally where appropriate.

---

# 91. User List DTO

Do not expose:

```text id="b2zlc9"
password_hash

Session tokens
```

---

# 92. Edit Username

Implement normal User edit endpoint.

Requires:

```text id="4e3u5g"
users.edit
```

Username normalization/uniqueness rules still apply.

---

# 93. Activate User

Implement explicit action:

```text id="gcacpj"
POST /api/users/{id}/activate
```

---

# 94. Deactivate User

Implement:

```text id="57ztu7"
POST /api/users/{id}/deactivate
```

Requires:

```text id="16gz2h"
users.activate
```

or the approved activation-management permission.

---

# 95. User Deactivation and Sessions

When User is deactivated:

```text id="vvxxr4"
Existing Sessions should be revoked/invalidated
```

so deactivation takes effect promptly.

---

# 96. User Physical Delete

Do not implement normal:

```text id="1c3g6a"
DELETE /api/users/{id}
```

Version 1 uses deactivation.

---

# 97. Task 15 — Roles Administration

Implement:

```text id="r9t0vw"
Role list

Create Role

Edit Role

Assign Permissions
```

---

# 98. Role List

Requires:

```text id="smld9q"
roles.view
```

---

# 99. Create Role

Requires:

```text id="8qjc4z"
roles.create
```

---

# 100. Edit Role

Requires:

```text id="j19qj1"
roles.edit
```

---

# 101. Manage Role Permissions

Requires:

```text id="5fkwgo"
roles.manage_permissions
```

---

# 102. Director Role Protection

Director Role should not be casually renamed/deleted/disabled if that would break system invariants.

Version 1 should treat it as a protected system Role.

---

# 103. Permission Creation UI

Do not implement arbitrary Permission creation through normal Administration UI.

Permissions are defined by application features.

---

# 104. Role Assignment to User

Implement:

```text id="fdruox"
POST /api/users/{userId}/roles/{roleId}
```

and:

```text id="3gi35g"
DELETE /api/users/{userId}/roles/{roleId}
```

or equivalent.

---

# 105. Role Assignment Permission

Requires:

```text id="3w1y9u"
users.manage_roles
```

---

# 106. Task 16 — Password Operations

Implement two distinct flows:

```text id="76dfua"
User changes own password

Administrator resets another User's password
```

---

# 107. Change Own Password

Conceptual endpoint:

```text id="8e1nhv"
POST /api/auth/change-password
```

---

# 108. Change Password Request

Contains:

```text id="u4gadv"
current_password

new_password
```

---

# 109. Change Own Password Validation

Backend must verify current password before accepting change.

---

# 110. Change Password Session Behavior

Recommended:

```text id="xbmovu"
Invalidate other Sessions
```

and decide whether current Session remains active or requires login again.

Keep behavior consistent.

---

# 111. Administrative Password Reset

Conceptual endpoint:

```text id="q69ed3"
POST /api/users/{id}/reset-password
```

Requires:

```text id="et5iiq"
users.reset_password
```

---

# 112. Password Reset Request

Contains new password.

The old password is not required because this is an authorized administrative operation.

---

# 113. Reset Session Behavior

After administrator reset:

```text id="kfaowl"
Invalidate all existing Sessions for target User
```

---

# 114. Password Recovery

Version 1 does not include:

```text id="vi41lo"
Forgot password email

Password reset email

SMS recovery

Security questions
```

Director/admin reset is sufficient.

---

# 115. Task 17 — Final Director Protection

Implement the invariant centrally.

---

# 116. Protected Operations

Final Director protection must apply when:

```text id="7g9gei"
Deactivating User

Removing Director Role
```

---

# 117. Final Active Director Definition

An active Director is a User where:

```text id="88fef6"
User is active
+
User has Director Role
```

---

# 118. Deactivation Rule

If target User is the final active Director:

```text id="yd83vy"
Reject deactivation
```

---

# 119. Role Removal Rule

If removing Director Role would leave zero active Directors:

```text id="jk5vdg"
Reject removal
```

---

# 120. Atomic Protection

Final Director checks and changes must be protected transactionally against concurrency.

Do not:

```text id="r4sk2y"
Count Directors

Release transaction

Then deactivate
```

in a race-prone way.

---

# 121. Error Code

Use a stable business error such as:

```text id="h31x55"
FINAL_DIRECTOR_REQUIRED
```

---

# 122. Task 18 — Authentication Frontend Foundation

After backend Authentication is working, implement:

```text id="ng9vbo"
Setup screen

Login page

Authentication state

Protected routes

Logout

Current User loading
```

---

# 123. Initial Startup Flow

Frontend startup:

```text id="6kiwxo"
Check setup status
      ↓
If setup required
→ Initial Setup

Otherwise
→ Check current Session
      ↓
Authenticated
→ Application

Not authenticated
→ Login
```

---

# 124. Initial Setup Screen

Screen should ask only for required information.

Initial V1:

```text id="4gdrru"
Username:
director
(read-only)

Password

Confirm Password
```

---

# 125. Login Screen

Fields:

```text id="3qfl7v"
Username

Password
```

Do not add:

```text id="f2up61"
Email

Forgot password

Sign up
```

---

# 126. Login Errors

Show a clear generic login failure.

Do not display raw backend exception messages.

---

# 127. Authentication State

Create centralized current User/authentication state.

It should expose:

```text id="ukvpw7"
user

permissions

isAuthenticated

isLoading
```

---

# 128. Logout UI

Provide a clear Logout action in application header/account menu.

---

# 129. Session Expiration Frontend

When API returns 401 due to expired/revoked Session:

```text id="a3qrkt"
Clear frontend auth state

Return to Login
```

---

# 130. Task 19 — User Administration Frontend

Implement Administration → Users.

---

# 131. User List Columns

Recommended:

```text id="vzb24r"
Username

Roles

Status

Last Login
```

---

# 132. User List Actions

Depending on Permission:

```text id="2h4dhp"
Create

Edit

Activate/Deactivate

Reset Password

Manage Roles
```

---

# 133. Create User Dialog/Page

Fields:

```text id="0kauv0"
Username

Password

Confirm Password

Roles
```

---

# 134. User Edit

Do not show password hash.

Password reset is a separate action.

---

# 135. Deactivation Confirmation

Example:

```text id="nvh4gp"
Deactivate User?

The User will no longer be able to access the ERP.
```

---

# 136. Password Reset UI

Use a focused dialog:

```text id="kptw53"
New Password

Confirm Password
```

---

# 137. Role Management UI

Display assigned Roles with add/remove controls.

Respect:

```text id="795d2o"
users.manage_roles
```

---

# 138. Final Director UI

Frontend should prevent obvious invalid actions where possible.

Example:

```text id="u1ococ"
Disable Deactivate
```

for known final Director.

Backend remains authoritative.

---

# 139. Task 20 — Authentication Integration Tests

Authentication is not complete until integration tests cover the real HTTP + PostgreSQL pipeline.

---

# 140. Required Setup Tests

```text id="bvv1bk"
Fresh database reports setup required

Setup creates director

Setup hashes password

Setup assigns Director Role

Second setup rejected
```

---

# 141. Required Login Tests

```text id="3qhpzz"
Correct password succeeds

Wrong password fails

Unknown User fails

Username case normalization works

Inactive User rejected
```

---

# 142. Required Session Tests

```text id="ab74gt"
Login creates Session

Session authorizes request

Logout invalidates Session

Expired Session rejected

Revoked Session rejected
```

---

# 143. Required Permission Tests

```text id="o7v5q7"
Authorized User succeeds

User without Permission gets 403

Unauthenticated request gets 401

Multiple Roles union Permissions correctly
```

---

# 144. Required User Admin Tests

```text id="46xbzx"
Create User

Duplicate normalized username rejected

Edit username

Deactivate User

Reactivate User

Password reset
```

---

# 145. Required Role Tests

```text id="vvcqeh"
Create Role

Assign Permission

Assign Role to User

Remove Role

Effective Permissions update
```

---

# 146. Required Director Tests

```text id="th0cq8"
One active Director
→ cannot deactivate

One active Director
→ cannot remove Director Role

Two active Directors
→ one can deactivate

Two active Directors
→ one can lose Director Role
```

---

# 147. Concurrency Director Test

Where practical, test two concurrent operations attempting to remove/deactivate Directors.

The database/application must not end with zero active Directors.

---

# 148. Password Reset Test

After reset:

```text id="9dy23g"
Old password rejected

New password works

Old Sessions rejected
```

---

# 149. Security DTO Test

Verify APIs never return:

```text id="pjw7sn"
password_hash

session token hash

raw session secret
```

---

# 150. Task 21 — Authentication Security Review

Before Authentication milestone is complete, review:

```text id="k91h4l"
Password storage

Session cookie

Session expiration

Logout behavior

Deactivation

Password reset

Permission enforcement

Director protection

Sensitive logging

DTO exposure

CORS/HTTPS assumptions
```

---

# 151. Authentication Commit Strategy

Recommended small commits:

```text id="gqi9e1"
feat(auth): add authentication schema

feat(auth): add permission catalog

feat(auth): add director bootstrap

feat(auth): add login and sessions

feat(auth): add permission authorization

feat(users): add user administration

feat(auth): add password management

test(auth): add authentication integration tests

feat(frontend): add login and authentication state

feat(frontend): add user administration
```

Exact grouping may vary.

---

# 152. Do Not Build Everything in One Commit

Authentication is security-critical.

Small reviewed commits are preferable.

---

# 153. Suggested First AI Coding Task

The first Authentication coding task should be database-only.

Prompt:

```text id="6d7l9u"
Read:
- AI_RULES.md
- docs/07_Authentication.md
- docs/08_Auth_Schema_Design.md
- docs/09_Auth_Tables.md
- docs/17_Database_Schema_Overview.md
- docs/20_Testing_Strategy.md
- docs/24_Backend_Architecture.md
- docs/25_Development_Workflow_for_AI.md
- docs/27_Authentication_Implementation_Plan.md

Task:
Implement only the Authentication database model.

Create:
- auth.users
- auth.roles
- auth.permissions
- auth.user_roles
- auth.role_permissions
- auth.sessions
- EF Core entity configurations
- first Authentication migration
- database constraint tests

Do not implement:
- login
- setup endpoint
- password hashing UI
- frontend
- Employees
- any other business module
- generic repository
- CQRS/MediatR

Requirements:
- use auth schema
- UUID PKs for normal entities
- composite PKs for junction tables
- username case-insensitive uniqueness
- created_by nullable for bootstrap
- safe delete behavior
- no plaintext password fields

Before finishing:
- run build
- apply migration to clean PostgreSQL test database
- run relevant tests
- review generated migration
- summarize changed files
```

---

# 154. Suggested Second AI Coding Task

After auth schema is stable:

```text id="j8uvac"
Implement Permission catalog and Director Role initialization only.
```

Do not combine with Login yet.

---

# 155. Suggested Third AI Coding Task

Then:

```text id="dsxlwt"
Implement password hashing and Session persistence infrastructure.
```

---

# 156. Suggested Fourth AI Coding Task

Then:

```text id="j3ll2i"
Implement first-run Director setup.
```

---

# 157. Suggested Fifth AI Coding Task

Then:

```text id="2y20wf"
Implement Login, Current User, and Logout.
```

---

# 158. Suggested Sixth AI Coding Task

Then:

```text id="kldqw2"
Implement Permission-based authorization policies.
```

---

# 159. Suggested Seventh AI Coding Task

Then:

```text id="hn2q2i"
Implement User Administration backend.
```

---

# 160. Suggested Eighth AI Coding Task

Then:

```text id="8hz4p2"
Implement Roles and Permission Administration backend.
```

---

# 161. Suggested Ninth AI Coding Task

Then:

```text id="y193ea"
Implement Authentication frontend:
setup, login, auth state, protected routes, logout.
```

---

# 162. Suggested Tenth AI Coding Task

Then:

```text id="k5z1an"
Implement User/Role Administration frontend.
```

---

# 163. Authentication Completion Gate

Do not begin Employees until all of these work:

```text id="md6hcl"
Initial Setup

Director login

Logout

Session expiration/revocation

Current User

User creation

Username normalization

User activation/deactivation

Roles

Permissions

Password change

Password reset

Final Director protection

Frontend authentication

User Administration UI

Integration tests
```

---

# 164. Authentication Non-Goals

This phase does not include:

```text id="dctqlr"
Employees

Email login

Public registration

Password recovery email

OAuth

SSO

MFA

Biometric login

API keys

External identity providers

Customer accounts

Supplier accounts
```

---

# 165. Future Authentication Extensions

Possible future features:

```text id="lb9e87"
MFA

External identity provider

More detailed login history

Trusted device management

Password expiration policy

Account lockout policy
```

Only add if Lithograph's security requirements justify them.

---

# 166. Simplicity Rule

Before adding Authentication complexity, ask:

```text id="k6y9t5"
Does this materially improve security for Lithograph's current internal ERP?
```

If not, defer it.

---

# 167. Final Authentication Principle

Authentication must establish a trusted answer to:

```text id="loah7y"
Who is this User?
```

Authorization must establish a trusted answer to:

```text id="fw44nj"
What may this User do?
```

The frontend may improve usability, but the backend must enforce both.

The central rule is:

```text id="u0tabu"
No business module should need to invent its own authentication or permission logic.
```

Once Authentication is complete, every later module should simply declare and use its Permissions.

---

**End of Document**