# Lithograph ERP

**Document:** 08_Auth_Schema_Design.md  
**Version:** 1.1  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Authentication  

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

---

# 1. Purpose

This document defines the database-level architecture of the Authentication module.

It describes:

- PostgreSQL schema ownership
- Authentication entities
- Table responsibilities
- Entity relationships
- Data ownership boundaries
- Important design rules

Detailed column definitions are documented separately in:

```text
09_Auth_Tables.md
```

---

# 2. Schema Ownership

Authentication owns PostgreSQL schema:

```text
auth
```

The Authentication module is the only module responsible for directly modifying tables inside this schema.

Other modules may use Authentication application services or authorization infrastructure as required.

---

# 3. Initial Authentication Tables

Version 1 uses the following Authentication tables:

```text
auth
├── users
├── roles
├── permissions
├── user_roles
├── role_permissions
└── sessions
```

No additional Authentication tables should be created unless there is a documented requirement.

---

# 4. Entity Overview

The Authentication model is based on five main concepts:

```text
User

Role

Permission

Session

Relationship Tables
```

Relationship tables connect:

```text
User ↔ Role

Role ↔ Permission
```

---

# 5. Authentication Relationship Model

Conceptually:

```text
User
  │
  │ many-to-many
  ▼
Role
  │
  │ many-to-many
  ▼
Permission
```

A User may have multiple Roles.

A Role may belong to multiple Users.

A Role may have multiple Permissions.

A Permission may belong to multiple Roles.

---

# 6. Session Relationship

A User may have multiple Sessions.

Conceptually:

```text
User
  │
  ├── Session
  ├── Session
  └── Session
```

This supports login from more than one browser or device context if allowed by the application.

---

# 7. Table: auth.users

Purpose:

```text
Stores ERP login accounts.
```

The User record represents Authentication identity.

It owns concepts such as:

- Username
- Password hash
- Account active state
- Login-related timestamps

It does not own employee business information.

---

# 8. User Data Boundary

The following information belongs in `auth.users`:

```text
Username
Password hash
Authentication status
Last login information
```

The following does not belong in `auth.users`:

```text
Full name
Job title
Department
Salary
Project role
Order responsibility
Employee phone
Employee business notes
```

Those belong to the Employees module where applicable.

---

# 9. User and Employee Link

The future Employees module may link an Employee to a User.

Conceptually:

```text
employees.employees.user_id
    ↓
auth.users.id
```

The relationship should be optional.

This means:

```text
Employee without User
=
Allowed
```

and:

```text
Employee with User
=
Allowed
```

Authentication does not need Employee information to function.

---

# 10. Table: auth.roles

Purpose:

```text
Stores reusable permission groups.
```

Examples:

```text
Director
Manager
Designer
Operator
```

Roles are Authentication concepts.

They must not be confused with Project Roles such as:

```text
Owner
Assignee
Participant
Observer
```

---

# 11. Role Purpose

A Role groups Permissions.

Conceptually:

```text
Designer Role
    │
    ├── clients.view
    ├── projects.view
    ├── orders.view
    └── orders.edit
```

Roles simplify permission management.

---

# 12. Director Role

The Director Role is a protected system-level Role in Version 1.

Its purpose is to provide full system access.

Important rules:

- Initial setup creates the Director Role if it does not exist.
- Initial Director User receives this Role.
- New application Permissions must be available to the Director.
- The final active Director User must remain protected against lockout.

The exact implementation of automatic full access may be handled by authorization logic rather than manually inserting every Permission row if that approach is simpler and safer.

---

# 13. Table: auth.permissions

Purpose:

```text
Stores application-defined permission codes.
```

Examples:

```text
users.view

users.create

users.edit

clients.view

orders.create
```

Permissions represent system capabilities.

---

# 14. Permission Ownership

Permissions are defined by application modules.

Examples:

```text
Authentication
→ users.view

Clients
→ clients.view

Projects
→ projects.create

Orders
→ orders.edit
```

Authentication stores and resolves the Permissions, but business modules define which Permissions they require.

---

# 15. Permission Code Format

Permission codes use:

```text
module.action
```

Examples:

```text
users.view

projects.edit

orders.create

orders.view_cost
```

Permission codes must be unique.

---

# 16. Table: auth.user_roles

Purpose:

```text
Connects Users to Roles.
```

This is a many-to-many junction table.

Conceptually:

```text
user_id
role_id
```

The combination of:

```text
user_id + role_id
```

is unique.

---

# 17. user_roles Primary Key

Because `auth.user_roles` is a pure relationship table, it should use a composite primary key:

```text
PRIMARY KEY (user_id, role_id)
```

No artificial UUID is required.

This follows the global database design rules.

---

# 18. Table: auth.role_permissions

Purpose:

```text
Connects Roles to Permissions.
```

This is a many-to-many junction table.

Conceptually:

```text
role_id
permission_id
```

The combination must be unique.

---

# 19. role_permissions Primary Key

Use:

```text
PRIMARY KEY (role_id, permission_id)
```

No artificial UUID is required.

---

# 20. Table: auth.sessions

Purpose:

```text
Stores active or recently valid Authentication Sessions.
```

A Session belongs to one User.

A User may have multiple Sessions.

---

# 21. Session Data

A Session may contain technical information such as:

```text
user_id

token_hash or equivalent safe session identifier

created_at

expires_at

last_activity_at

ip_address

user_agent

is_active
```

The exact implementation depends on the selected ASP.NET Core session/authentication strategy.

---

# 22. Session Security

The database must never store raw reusable authentication secrets when a safer hashed representation can be used.

Session implementation must use established ASP.NET Core security mechanisms.

Do not design custom cryptographic protocols.

---

# 23. Session Deletion

Sessions are temporary technical records.

They do not require soft delete.

Expired or invalid Sessions may be physically removed.

This is intentionally different from long-lived business records.

---

# 24. User Deactivation

`auth.users` requires a clear account-status concept.

Conceptually:

```text
is_active
```

Inactive Users cannot authenticate.

User deactivation should also invalidate or otherwise disable existing Sessions.

---

# 25. User Soft Delete

Users are long-lived system records with historical importance.

Version 1 should prefer:

```text
Deactivate User
```

over destructive deletion.

If `is_deleted` is used for Users, it must remain distinct from:

```text
is_active
```

Example:

```text
is_active = false
is_deleted = false
```

means:

```text
Account exists but login is disabled.
```

---

# 26. Roles and Soft Delete

Roles may require preservation if they are historically referenced or currently assigned.

Normal UI behavior should avoid destructive deletion of important Roles.

If deletion is supported, existing relationships must be handled safely.

Role deletion must never cause unexpected deletion of Users.

---

# 27. Permissions and Deletion

Application-defined Permissions should not normally be manually deleted through ordinary administration.

They represent system capabilities.

When a feature or Permission is removed from the application, migration and compatibility behavior should be handled deliberately.

---

# 28. Relationship Delete Behavior

For junction tables, relationship rows may normally be physically deleted.

Example:

Removing a Role from a User:

```text
DELETE auth.user_roles relationship
```

This does not delete:

```text
User
```

or:

```text
Role
```

---

# 29. Session Delete Behavior

Deleting or deactivating a User should invalidate or remove related Sessions according to the final persistence implementation.

Sessions must never remain usable for a User who no longer has login access.

---

# 30. First-Run Bootstrap

When no User exists, the system performs Initial Setup.

Conceptually:

```text
No Users
   ↓
Initial Setup
   ↓
Create Director Role
   ↓
Create Director User
   ↓
Assign Director Role
   ↓
Normal Login Enabled
```

This bootstrap must work without an existing authenticated `created_by` User.

---

# 31. Bootstrap Audit Fields

Authentication bootstrap records may require nullable or system-handled creator fields because no authenticated User exists yet.

Do not force impossible foreign-key requirements during first-run initialization.

---

# 32. Username Uniqueness

Usernames must be unique in a case-insensitive manner.

Conceptually:

```text
aram
Aram
ARAM
```

must be treated as the same username.

This rule must be enforced at the database/application level, not only in the frontend.

---

# 33. Role Name Uniqueness

Role names should be unique according to the selected case-sensitivity rules.

The system should prevent confusing duplicates such as:

```text
Designer
designer
DESIGNER
```

if they represent the same intended Role.

---

# 34. Permission Code Uniqueness

Permission codes must be unique.

Example:

```text
orders.create
```

may exist only once.

Permission Code is a system identifier and should be stable.

---

# 35. Audit Requirements

Important Authentication entities may contain useful audit information such as:

```text
created_at

updated_at
```

and, where valuable:

```text
created_by

updated_by
```

Do not mechanically add full audit fields to every junction or technical table.

---

# 36. Table Categories

Authentication contains three categories of tables.

## Long-Lived Entity Tables

```text
users
roles
permissions
```

These represent durable system concepts.

## Relationship Tables

```text
user_roles
role_permissions
```

These represent associations.

## Technical Tables

```text
sessions
```

These represent temporary authentication state.

Different categories do not need identical persistence rules.

---

# 37. Schema Isolation

Other modules must not directly insert, update or delete Authentication tables.

For example:

Incorrect:

```text
Employees module
    ↓
UPDATE auth.users
```

Preferred:

```text
Employees module
    ↓
Authentication application interface
    ↓
Authentication module
```

This keeps ownership clear.

---

# 38. Read Access

Other modules may need basic current User identity information.

Example:

```text
Current User ID
```

for audit fields.

Authentication should expose this through shared application/security infrastructure rather than forcing modules to query `auth.users` directly for every operation.

---

# 39. Authorization Resolution

Authorization conceptually resolves:

```text
User
    ↓
User Roles
    ↓
Roles
    ↓
Role Permissions
    ↓
Permissions
```

The implementation should make this efficient without introducing unnecessary complexity.

---

# 40. Director Authorization Strategy

Because Director represents full access, there are two reasonable implementation patterns:

```text
A. Director Role explicitly receives all Permission relationships
```

or:

```text
B. Authorization treats Director as a protected full-access Role
```

The implementation should choose the simpler reliable approach.

Whichever strategy is used must satisfy:

- Director automatically gets access to new module Permissions.
- The behavior is clear and testable.
- No hidden security exception exists outside documented authorization logic.

---

# 41. Permission Seed Strategy

Module Permissions may be inserted through controlled application startup/seeding or migrations.

The system should ensure required Permission definitions exist.

Do not depend on administrators manually typing Permission codes.

---

# 42. Role Seed Strategy

Version 1 requires at least:

```text
Director
```

Additional default Roles may be seeded later if useful.

Do not create many speculative Roles before the real employee permission requirements are defined.

---

# 43. User Business ID

Users do not require Business IDs.

Username is the human-facing login identifier.

Authentication Users are not numbered like:

```text
USR-000001
```

unless a future requirement explicitly introduces this.

---

# 44. Role Business ID

Roles do not require Business IDs.

Role Name and internal UUID are sufficient.

---

# 45. Permission Business ID

Permissions do not require Business IDs.

Permission Code acts as their stable human-readable system identifier.

Example:

```text
orders.edit
```

---

# 46. Session Business ID

Sessions do not require Business IDs.

Sessions are technical records and should not be exposed as business documents.

---

# 47. Password Storage Boundary

The password hash belongs only in:

```text
auth.users
```

or the final framework-supported Authentication storage structure approved during implementation.

It must not appear in:

- Employee tables
- API response models
- Client-side state
- Logs
- Reports

---

# 48. User API Boundary

The User API must not expose Authentication persistence entities directly.

Responses should contain only required safe fields.

Example safe information:

```text
id

username

is_active

roles
```

Unsafe information:

```text
password_hash

session secrets
```

---

# 49. Session API Boundary

Normal application APIs should not expose internal Session secret representations.

Any future session-administration UI should receive safe metadata only.

Examples:

```text
created_at

last_activity_at

device/browser description
```

not raw authentication secrets.

---

# 50. Employee Link Direction

Recommended ownership:

```text
employees.employees.user_id
```

rather than placing:

```text
employee_id
```

inside `auth.users`.

Reason:

Authentication should work independently of Employees.

Employees may optionally use Authentication.

This keeps dependency direction cleaner.

---

# 51. Employee Link Uniqueness

If an Employee is linked to a User, Version 1 should normally enforce one-to-one behavior:

```text
One Employee
↔
Maximum one User
```

and:

```text
One User
↔
Maximum one Employee
```

The final constraint belongs to the Employees module design.

---

# 52. Project Roles Do Not Belong Here

Do not add Project business roles to Authentication tables.

Incorrect:

```text
auth.roles
Owner
Assignee
Participant
Observer
```

unless those names independently become Authentication Roles for another reason.

Project Roles belong to the Projects module.

---

# 53. Order Permissions vs Project Roles

Permissions such as:

```text
orders.edit
```

describe system access.

Project Roles such as:

```text
Owner
```

describe responsibility for a particular Project.

These concepts must remain separate.

---

# 54. Version 1 Auth Schema Summary

```text
auth

├── users
│   └── Login identities
│
├── roles
│   └── Permission groups
│
├── permissions
│   └── Application capabilities
│
├── user_roles
│   └── User ↔ Role relationship
│
├── role_permissions
│   └── Role ↔ Permission relationship
│
└── sessions
    └── Authentication session state
```

---

# 55. Version 1 Non-Goals

Do not add Authentication tables for:

```text
OAuth Providers

Email Verification

Password Recovery Tokens

Two-Factor Authentication

Device Trust

External Identity Providers

Direct User Permissions

LDAP

Active Directory
```

unless the Authentication scope changes.

---

# 56. Schema Change Rule

Before adding another table to `auth`, ask:

1. Is this genuinely Authentication data?
2. Can the current tables already represent it?
3. Is it required by Version 1?
4. Would it belong more naturally to Employees or another business module?
5. Does adding it make Authentication simpler or more complicated?

If there is no clear Version 1 requirement, do not add it.

---

# 57. Final Authentication Schema Principle

The Authentication schema should remain small.

Its responsibility is to support:

```text
Identity

Login

Sessions

Roles

Permissions
```

It should not become a general-purpose people or employee database.

---

**End of Document**