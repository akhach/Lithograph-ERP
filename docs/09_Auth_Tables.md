# Lithograph ERP

**Document:** 09_Auth_Tables.md  
**Version:** 1.1  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Authentication  

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `07_Authentication.md`
- `08_Auth_Schema_Design.md`

---

# 1. Purpose

This document defines the exact Version 1 database tables for the Authentication module.

PostgreSQL schema:

```text
auth
```

Initial tables:

```text
auth.users
auth.roles
auth.permissions
auth.user_roles
auth.role_permissions
auth.sessions
```

---

# 2. General Rules

Authentication tables follow the global database standards.

Database identifiers use:

```text
lowercase snake_case
```

Normal entity tables use:

```text
UUID primary keys
```

Pure many-to-many relationship tables use:

```text
composite primary keys
```

Authentication does not store Employee business information.

---

# 3. Table Overview

```text
auth
│
├── users
│
├── roles
│
├── permissions
│
├── user_roles
│
├── role_permissions
│
└── sessions
```

Relationships:

```text
users
  │
  ├── user_roles ─── roles
  │                    │
  │                    └── role_permissions ─── permissions
  │
  └── sessions
```

---

# 4. Table: auth.users

## Purpose

Stores ERP login accounts.

A User represents authentication identity only.

It does not represent the Employee business record.

---

# 5. auth.users Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `username` | `varchar(100)` | No | Display/login username |
| `normalized_username` | `varchar(100)` | No | Normalized username used for uniqueness and login lookup |
| `password_hash` | `text` | No | Secure password hash |
| `is_active` | `boolean` | No | Whether login is allowed |
| `last_login_at` | `timestamptz` | Yes | Last successful login |
| `created_at` | `timestamptz` | No | Record creation time |
| `created_by` | `uuid` | Yes | User that created the account |
| `updated_at` | `timestamptz` | Yes | Last update time |
| `updated_by` | `uuid` | Yes | User that last updated the account |

---

# 6. auth.users Primary Key

```text
PRIMARY KEY (id)
```

`id` is generated as a UUID.

---

# 7. Username Normalization

Usernames are case-insensitive.

Examples:

```text
aram
Aram
ARAM
```

must represent the same login identifier.

The application stores:

```text
username
```

for display and:

```text
normalized_username
```

for reliable lookup and uniqueness.

Example:

```text
username = Aram

normalized_username = ARAM
```

The exact normalization routine must be consistent in the backend.

---

# 8. auth.users Constraints

Required constraints:

```text
username NOT NULL

normalized_username NOT NULL

password_hash NOT NULL

is_active NOT NULL
```

Default:

```text
is_active = true
```

Unique:

```text
UNIQUE (normalized_username)
```

---

# 9. auth.users Rules

- Username is required.
- Username must be unique case-insensitively.
- Leading and trailing spaces must be removed before validation.
- Password hash must never contain a plain-text password.
- Inactive Users cannot authenticate.
- Password hash must never be returned through normal APIs.
- The final active Director User must be protected by application business logic.

---

# 10. auth.users Audit Relationships

`created_by` and `updated_by` may reference:

```text
auth.users.id
```

These fields are nullable because the first Director account is created before any authenticated User exists.

Delete behavior should not cascade.

Historical creator references should remain stable.

---

# 11. User Deletion

Version 1 does not require normal physical deletion of Users.

Normal account removal uses:

```text
is_active = false
```

Do not add an `is_deleted` column unless a later requirement demonstrates that it is necessary.

This keeps Version 1 simpler.

---

# 12. Table: auth.roles

## Purpose

Stores reusable permission groups.

Examples:

```text
Director
Manager
Designer
Operator
```

---

# 13. auth.roles Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `name` | `varchar(100)` | No | Human-readable Role name |
| `normalized_name` | `varchar(100)` | No | Normalized Role name |
| `description` | `text` | Yes | Role explanation |
| `is_system` | `boolean` | No | Indicates protected system Role |
| `created_at` | `timestamptz` | No | Creation time |
| `created_by` | `uuid` | Yes | Creating User |
| `updated_at` | `timestamptz` | Yes | Last update time |
| `updated_by` | `uuid` | Yes | Last modifying User |

---

# 14. auth.roles Primary Key

```text
PRIMARY KEY (id)
```

---

# 15. auth.roles Constraints

Required:

```text
name NOT NULL

normalized_name NOT NULL

is_system NOT NULL
```

Default:

```text
is_system = false
```

Unique:

```text
UNIQUE (normalized_name)
```

---

# 16. Protected System Roles

Version 1 requires at least one protected system Role:

```text
Director
```

For the Director Role:

```text
is_system = true
```

System Roles may have restrictions on:

- Renaming
- Deletion
- Deactivation
- Permission removal

The exact UI restrictions should follow Authentication business rules.

---

# 17. Role Deletion

Version 1 should not physically delete a Role that is actively assigned to Users.

Normal behavior should prefer:

- Remove assignments first
- Preserve protected system Roles

A generic soft-delete system is not required for Roles in Version 1.

---

# 18. Table: auth.permissions

## Purpose

Stores application-defined Permissions.

Examples:

```text
users.view

users.create

clients.view

projects.edit

orders.create
```

Permissions are system capabilities, not employee job titles.

---

# 19. auth.permissions Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `code` | `varchar(150)` | No | Stable Permission identifier |
| `name` | `varchar(150)` | No | Human-readable Permission name |
| `description` | `text` | Yes | Explanation |
| `module` | `varchar(100)` | No | Owning application module |
| `created_at` | `timestamptz` | No | Creation time |

---

# 20. auth.permissions Primary Key

```text
PRIMARY KEY (id)
```

---

# 21. Permission Code Rules

Permission codes use:

```text
module.action
```

Examples:

```text
users.view

users.create

users.edit

users.manage_roles

orders.view

orders.create
```

Permission codes should use lowercase characters.

---

# 22. auth.permissions Constraints

Required:

```text
code NOT NULL

name NOT NULL

module NOT NULL
```

Unique:

```text
UNIQUE (code)
```

Permissions are application-defined.

Administrators should not normally create arbitrary Permission codes through the UI.

---

# 23. Permission Module Field

The `module` column provides simple grouping.

Examples:

```text
users

clients

projects

orders
```

Example record:

```text
code:
orders.create

name:
Create Orders

module:
orders
```

This simplifies permission-management UI grouping.

---

# 24. Permission Lifecycle

Permissions are seeded or synchronized from approved application definitions.

They should not normally be manually deleted from normal administration.

If application functionality is removed later, permission cleanup must be handled deliberately.

---

# 25. Table: auth.user_roles

## Purpose

Represents the many-to-many relationship between Users and Roles.

---

# 26. auth.user_roles Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `user_id` | `uuid` | No | User reference |
| `role_id` | `uuid` | No | Role reference |
| `assigned_at` | `timestamptz` | No | Assignment creation time |
| `assigned_by` | `uuid` | Yes | User that assigned the Role |

---

# 27. auth.user_roles Primary Key

Use composite primary key:

```text
PRIMARY KEY (user_id, role_id)
```

No artificial UUID is required.

---

# 28. auth.user_roles Foreign Keys

```text
user_id
→ auth.users.id
```

```text
role_id
→ auth.roles.id
```

```text
assigned_by
→ auth.users.id
```

---

# 29. auth.user_roles Delete Behavior

If a User is physically removed through exceptional administrative/database maintenance:

```text
user_roles
```

relationships may be deleted.

If a Role is removed, its relationship rows may also be removed.

Normal Version 1 User management uses deactivation rather than User deletion.

---

# 30. auth.user_roles Rules

- A User cannot have the same Role twice.
- A User may have multiple Roles.
- Removing a Role assignment does not delete the User.
- Removing a Role assignment does not delete the Role.
- Final active Director protection must be enforced before removing the Director Role from a User.

---

# 31. Table: auth.role_permissions

## Purpose

Represents the many-to-many relationship between Roles and Permissions.

---

# 32. auth.role_permissions Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `role_id` | `uuid` | No | Role reference |
| `permission_id` | `uuid` | No | Permission reference |
| `assigned_at` | `timestamptz` | No | Assignment creation time |
| `assigned_by` | `uuid` | Yes | User responsible for assignment |

---

# 33. auth.role_permissions Primary Key

```text
PRIMARY KEY (role_id, permission_id)
```

No artificial UUID is required.

---

# 34. auth.role_permissions Foreign Keys

```text
role_id
→ auth.roles.id
```

```text
permission_id
→ auth.permissions.id
```

```text
assigned_by
→ auth.users.id
```

---

# 35. auth.role_permissions Rules

- A Role cannot contain the same Permission twice.
- Removing a relationship does not delete the Role.
- Removing a relationship does not delete the Permission.
- Protected Director behavior must remain consistent with full-access requirements.

---

# 36. Director Full Access Strategy

The implementation must ensure that the Director receives all current and future Permissions.

Two implementation approaches are acceptable:

### Option A

Explicitly maintain:

```text
role_permissions
```

for every Permission assigned to Director.

### Option B

Authorization logic recognizes the protected Director Role as full access.

The implementation should choose one strategy and use it consistently.

The selected strategy must be:

- Simple
- Testable
- Documented
- Automatically compatible with newly introduced Permissions

Do not combine both approaches inconsistently.

---

# 37. Table: auth.sessions

## Purpose

Stores server-recognized Authentication Session state when required by the selected authentication implementation.

A Session belongs to one User.

---

# 38. auth.sessions Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Session primary key |
| `user_id` | `uuid` | No | Authenticated User |
| `token_hash` | `varchar(255)` | No | Hash of reusable Session secret/token where applicable |
| `created_at` | `timestamptz` | No | Session creation time |
| `expires_at` | `timestamptz` | No | Expiration time |
| `last_activity_at` | `timestamptz` | Yes | Most recent activity |
| `ip_address` | `inet` | Yes | Client IP address |
| `user_agent` | `text` | Yes | Browser/device user-agent string |
| `is_active` | `boolean` | No | Session validity state |

---

# 39. auth.sessions Primary Key

```text
PRIMARY KEY (id)
```

---

# 40. auth.sessions Foreign Key

```text
user_id
→ auth.users.id
```

---

# 41. auth.sessions Defaults

```text
is_active = true
```

`created_at` should be set when the Session is created.

---

# 42. Session Token Security

If a reusable Session secret is persisted:

```text
token_hash
```

must contain only a secure hash or equivalent non-reusable representation.

Never store a raw reusable Session token in PostgreSQL.

Never expose `token_hash` through the frontend API.

---

# 43. Authentication Framework Compatibility

The final ASP.NET Core authentication implementation may determine that some Session fields or storage mechanisms differ from this conceptual structure.

Security best practices of the selected framework implementation take priority over forcing a custom Session design.

If the established framework implementation safely eliminates the need for a custom `auth.sessions` table, this document should be updated before removing the table.

Do not create weaker custom authentication merely to preserve a table design.

---

# 44. Session Expiration

A Session becomes invalid when:

```text
expires_at <= current time
```

or:

```text
is_active = false
```

or the associated User is no longer active.

---

# 45. Session Logout

Logout invalidates the current Session.

Conceptually:

```text
is_active = false
```

or the equivalent secure framework-supported invalidation mechanism.

---

# 46. User Deactivation

When:

```text
auth.users.is_active = false
```

all existing Sessions for that User must become unusable.

The application may:

- Mark them inactive
- Delete them
- Reject them during authorization

depending on the final implementation.

The security result must be the same:

```text
Inactive User
=
No ERP access
```

---

# 47. Session Cleanup

Expired or inactive Sessions may be physically removed.

Sessions do not require soft delete.

Automatic cleanup may be implemented later using a simple scheduled mechanism if needed.

Do not introduce heavy background infrastructure solely for Session cleanup.

---

# 48. Required Indexes: auth.users

Indexes:

```text
PRIMARY KEY (id)
```

```text
UNIQUE INDEX on normalized_username
```

Optional additional index only if actual queries require it:

```text
is_active
```

Do not create indexes without a query need.

---

# 49. Required Indexes: auth.roles

Indexes:

```text
PRIMARY KEY (id)
```

```text
UNIQUE INDEX on normalized_name
```

---

# 50. Required Indexes: auth.permissions

Indexes:

```text
PRIMARY KEY (id)
```

```text
UNIQUE INDEX on code
```

Optional grouping index if useful:

```text
module
```

---

# 51. Required Indexes: auth.user_roles

Composite primary key:

```text
(user_id, role_id)
```

Because the primary key begins with `user_id`, an additional index should be considered for Role-based lookup:

```text
role_id
```

---

# 52. Required Indexes: auth.role_permissions

Composite primary key:

```text
(role_id, permission_id)
```

An additional index may be useful for Permission-based lookup:

```text
permission_id
```

---

# 53. Required Indexes: auth.sessions

Indexes should support common Session operations.

Recommended:

```text
PRIMARY KEY (id)
```

```text
INDEX (user_id)
```

```text
UNIQUE INDEX (token_hash)
```

if `token_hash` is used for direct Session lookup.

An expiration-related index may be added if Session cleanup requires it:

```text
expires_at
```

---

# 54. Timestamps

All timestamp fields use:

```text
timestamptz
```

and application logic uses UTC internally.

Examples:

```text
created_at

updated_at

last_login_at

assigned_at

expires_at

last_activity_at
```

---

# 55. Audit Field Philosophy

Long-lived entity tables contain useful audit fields.

Relationship tables use simple assignment metadata where useful.

Technical Session records use their own lifecycle timestamps.

Do not force every table into the same audit structure.

---

# 56. Initial Bootstrap Records

On first system setup, the application must create at minimum:

```text
Director Role
```

and:

```text
Director User
```

and connect them through:

```text
auth.user_roles
```

---

# 57. Initial Director User

Conceptual initial record:

```text
username:
director

normalized_username:
DIRECTOR

is_active:
true
```

Password hash is generated from the password entered during first-run setup.

`created_by` is:

```text
NULL
```

because no authenticated User exists yet.

---

# 58. Initial Director Role

Conceptual initial Role:

```text
name:
Director

normalized_name:
DIRECTOR

is_system:
true
```

`created_by` may be:

```text
NULL
```

during bootstrap.

---

# 59. Initial Permissions

Permissions should be introduced by their owning modules.

Authentication-related initial examples may include:

```text
users.view

users.create

users.edit

users.activate

users.reset_password

users.manage_roles
```

Other modules introduce their own Permissions as those modules are implemented.

---

# 60. Permission Seeding

Permission definitions must be controlled by application code/migrations/startup synchronization.

Administrators assign Permissions to Roles.

Administrators do not normally invent new Permission codes.

---

# 61. User API Safe Fields

Normal User responses may expose:

```text
id

username

is_active

last_login_at

roles
```

They must never expose:

```text
password_hash

normalized_username
```

unless a legitimate internal API requires normalization data, which normal frontend APIs do not.

---

# 62. Role API Safe Fields

Normal Role responses may expose:

```text
id

name

description

is_system

permissions
```

Internal normalized fields normally do not need to be displayed.

---

# 63. Permission API Safe Fields

Permission administration may expose:

```text
id

code

name

description

module
```

These are not authentication secrets.

---

# 64. Session API Safe Fields

If Session administration is later exposed, safe metadata may include:

```text
id

created_at

expires_at

last_activity_at

ip_address

user_agent

is_active
```

Never expose:

```text
token_hash
```

---

# 65. No Employee Fields

Do not add these fields to `auth.users`:

```text
full_name

position

department

phone

salary

project_role
```

Those belong to the Employees or other business modules.

---

# 66. No Business IDs

Authentication tables do not require generated Business IDs.

Do not create:

```text
USR-000001

ROLE-000001

SES-000001
```

UUIDs and existing identifiers are sufficient.

---

# 67. No Direct User Permissions

Version 1 does not contain:

```text
auth.user_permissions
```

Permission resolution is:

```text
User
  ↓
Role
  ↓
Permission
```

---

# 68. No Authentication Role for Project Responsibilities

Do not add these automatically to `auth.roles`:

```text
Owner

Assignee

Participant

Observer
```

These are Project Roles.

They belong to Projects.

---

# 69. Database Relationship Summary

```text
auth.users
   │
   ├──< auth.user_roles >── auth.roles
   │                           │
   │                           └──< auth.role_permissions >── auth.permissions
   │
   └──< auth.sessions
```

---

# 70. Table Summary

## auth.users

```text
Login identity
```

## auth.roles

```text
Permission group
```

## auth.permissions

```text
System capability
```

## auth.user_roles

```text
User ↔ Role
```

## auth.role_permissions

```text
Role ↔ Permission
```

## auth.sessions

```text
Authentication Session state
```

---

# 71. Version 1 Non-Goals

Do not add tables for:

```text
user_permissions

password_history

password_recovery_tokens

email_verification

oauth_accounts

two_factor_methods

trusted_devices

login_alerts

api_keys
```

without a new approved requirement.

---

# 72. Implementation Rule

When implementing these tables with Entity Framework Core:

- Explicitly define schema names.
- Explicitly define table names.
- Explicitly define important lengths.
- Explicitly define indexes.
- Explicitly define relationships.
- Explicitly define delete behavior.
- Use migrations.
- Do not manually create production tables.

---

# 73. Final Authentication Table Principle

The Authentication database should remain small and understandable.

It must securely answer:

```text
Who can log in?

Which Roles do they have?

Which Permissions do those Roles provide?

Is their Session valid?
```

Anything unrelated to those questions probably belongs in another module.

---

**End of Document**