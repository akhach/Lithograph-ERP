# Lithograph ERP

**Document:** 10_Users_Module.md  
**Version:** 1.1  
**Status:** Approved  
**Project:** Lithograph ERP  
**Parent Module:** Authentication  
**Feature:** User Administration  

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `06_UI_UX_Principles.md`
- `07_Authentication.md`
- `08_Auth_Schema_Design.md`
- `09_Auth_Tables.md`

---

# 1. Purpose

This document defines the User Administration feature of the Authentication module.

User Administration allows authorized Users to manage ERP login accounts.

Its purpose is to provide simple internal account management for Lithograph.

---

# 2. Architectural Position

Users are not a separate major Lithograph ERP module.

They belong inside:

```text
Authentication
├── Users
├── Roles
├── Permissions
└── Sessions
```

This document describes the Users feature because it has its own UI and business rules.

---

# 3. User Definition

A User is:

```text
An ERP login account
```

A User contains authentication information such as:

- Username
- Password authentication
- Active status
- Roles
- Permissions through Roles
- Sessions

A User is not an Employee.

---

# 4. User vs Employee

The project must maintain this distinction:

```text
User
=
Can log into ERP
```

```text
Employee
=
Person working for Lithograph
```

An Employee may optionally have one User account.

An Employee may exist without login access.

Do not add Employee business information to User Administration.

---

# 5. User Administration Responsibilities

Authorized Users may perform actions such as:

```text
View Users

Create User

Edit Username

Activate User

Deactivate User

Reset Password

View Roles

Assign Roles

Remove Roles
```

User Administration does not manage:

- Employee name
- Employee position
- Department
- Salary
- Project assignments
- Project Roles
- Order responsibilities

---

# 6. Public Registration

Lithograph ERP does not support public registration.

There is no:

```text
Register Account
```

page for normal users.

User accounts are created manually by authorized administrators.

---

# 7. First User

The first User is created by Initial Setup.

When no Users exist:

```text
Initial Setup
     ↓
Enter Director Password
     ↓
Create director User
     ↓
Assign Director Role
```

Initial username:

```text
director
```

The Director may later change the username.

---

# 8. Users List

User Administration should provide a Users list.

Example:

| Username | Roles | Status | Last Login |
|---|---|---|---|
| director | Director | Active | 2026-09-26 |
| designer1 | Designer | Active | 2026-09-25 |
| operator1 | Operator | Active | 2026-09-26 |
| installer1 | Installer | Inactive | 2026-08-10 |

The exact date formatting follows global UI standards.

---

# 9. Users List Columns

Recommended Version 1 columns:

```text
Username

Roles

Status

Last Login
```

Do not add Employee fields to this table.

Employee information belongs in the Employees interface.

---

# 10. Users List Search

Version 1 should support searching by:

```text
Username
```

Optional simple filters:

```text
Role

Active Status
```

Do not build an advanced search system for User Administration.

---

# 11. Create User

Authorized administrators may create a User manually.

Required Version 1 information:

```text
Username

Initial Password

Confirm Password

Role
```

Optional:

```text
Active
```

The default may be:

```text
Active = true
```

---

# 12. Create User Interface

Example:

```text
New User

Username
[________________]

Password
[________________]

Confirm Password
[________________]

Roles
[ Designer ▼ ]

☑ Active

[Create] [Cancel]
```

The interface should remain simple.

---

# 13. Username Rules

Username:

- Is required
- Must be unique
- Is case-insensitive
- May be changed by an authorized User
- Must have leading and trailing spaces removed
- Must not be empty after trimming

Examples treated as the same username:

```text
aram
Aram
ARAM
```

---

# 14. Username Storage

Database storage uses:

```text
username
```

and:

```text
normalized_username
```

Example:

```text
username:
Aram

normalized_username:
ARAM
```

The backend is responsible for consistent normalization.

The frontend must not be the only place enforcing uniqueness.

---

# 15. Password Rules

When creating a User:

- Password is required
- Password confirmation must match
- Password must satisfy the approved security policy
- Plain-text password must never be persisted

The backend uses established ASP.NET Core security mechanisms.

---

# 16. Password Visibility

Administrators must never be able to view an existing password.

The system stores only the password hash.

There is no:

```text
Show Existing Password
```

function.

---

# 17. Reset Password

Authorized administrators may reset a User password.

Reset means:

```text
Replace existing password with a new password
```

The reset form contains:

```text
New Password

Confirm Password
```

The old password is not shown.

---

# 18. Reset Password Interface

Example:

```text
Reset Password

User:
designer1

New Password
[________________]

Confirm Password
[________________]

[Reset Password] [Cancel]
```

The interface should clearly show which User is being modified.

---

# 19. User Self Password Change

A logged-in User may be allowed to change their own password.

The exact UI may contain:

```text
Current Password

New Password

Confirm Password
```

The backend must verify the current password before changing it.

---

# 20. Edit User

Authorized administrators may edit basic account information.

Version 1 Edit User may contain:

```text
Username

Roles

Active Status
```

Password changes should remain a separate action.

This reduces accidental password modification.

---

# 21. Active User

An Active User:

```text
May log into Lithograph ERP
```

Database concept:

```text
is_active = true
```

---

# 22. Inactive User

An Inactive User:

```text
Cannot log into Lithograph ERP
```

Database concept:

```text
is_active = false
```

The User record remains in the database.

---

# 23. Deactivate User

Deactivation is the normal way to remove login access.

When a User is deactivated:

- New login is blocked
- Existing Sessions become unusable
- Historical references remain
- User Role relationships may remain
- Employee record is not deleted
- Projects and Orders are not modified

---

# 24. Activate User

An authorized administrator may reactivate an inactive User.

Reactivation:

```text
is_active = true
```

The User can then authenticate again using their current password unless it has been reset or otherwise invalidated.

---

# 25. Physical User Deletion

Normal Version 1 User Administration does not require physical User deletion.

Use:

```text
Deactivate
```

instead.

Physical database deletion should not be exposed as a normal User Administration action.

---

# 26. Roles

A User may have one or more Authentication Roles.

Example:

```text
User:
employee1

Roles:
Designer
Operator
```

Effective access is determined by the Permissions available through assigned Roles.

---

# 27. Assign Role

Authorized Users may assign an existing Role to a User.

Conceptually:

```text
User
  +
Role
  ↓
auth.user_roles
```

The same Role cannot be assigned to the same User more than once.

---

# 28. Remove Role

Authorized Users may remove a Role assignment.

Removing the relationship does not delete:

```text
User
```

or:

```text
Role
```

It removes only the assignment.

---

# 29. Director Role

The Director Role is protected.

The system must prevent removal of the Director Role from the final active Director User.

Example:

If two active Director Users exist:

```text
Director A
Director B
```

the Director Role may potentially be removed from one of them.

If only one active Director remains, removal must be blocked.

---

# 30. Director Deactivation

The final active Director User cannot be deactivated.

The UI should explain why.

Example:

```text
This User is the final active Director.

Assign the Director Role to another active User before deactivating this account.
```

---

# 31. Director Username

The username:

```text
director
```

is not permanently special.

After Initial Setup, the first Director may change it.

Protection is based on:

```text
Director Role
```

not on the literal username.

---

# 32. User Permissions

User Administration defines Permissions such as:

```text
users.view

users.create

users.edit

users.activate

users.reset_password

users.manage_roles
```

The exact names must remain consistent throughout:

- Backend authorization
- Permission seeding
- UI permission checks
- Tests
- Documentation

---

# 33. users.view

Allows the User to:

- Open User Administration
- View Users
- View basic account status
- View assigned Roles

It must not expose:

- Password hashes
- Session secrets

---

# 34. users.create

Allows:

```text
Create User
```

The action must still follow validation and security rules.

---

# 35. users.edit

Allows editing normal User account information such as:

```text
Username
```

It does not automatically imply:

```text
Reset Password
```

or:

```text
Manage Roles
```

because these have separate Permissions.

---

# 36. users.activate

Allows:

```text
Activate User

Deactivate User
```

Final active Director protection still applies.

---

# 37. users.reset_password

Allows an administrator to set a new password for another User.

This Permission does not allow viewing an existing password.

---

# 38. users.manage_roles

Allows:

```text
Assign Role

Remove Role
```

Protected system rules still apply.

---

# 39. User Creation Permission

Creating a User may require:

```text
users.create
```

If the creation UI also assigns Roles, the system should determine whether:

```text
users.manage_roles
```

is additionally required.

For Version 1, the simplest approved implementation may require both when Role assignment occurs.

The rule must be applied consistently.

---

# 40. User API

The User Administration backend may conceptually expose operations such as:

```text
Get Users

Get User

Create User

Update User

Activate User

Deactivate User

Reset Password

Assign Role

Remove Role
```

Exact route names should follow normal REST conventions and implementation standards.

---

# 41. User Response DTO

A normal User Administration response may contain:

```text
id

username

is_active

last_login_at

roles
```

It must not contain:

```text
password_hash

session token hash
```

---

# 42. Create User Request

Conceptual request data:

```text
username

password

role_ids

is_active
```

The frontend may use `confirm_password`, but confirmation does not need to be stored or passed beyond the layer where it is useful.

---

# 43. Update User Request

Conceptual editable information:

```text
username

is_active
```

Role changes may use dedicated role-assignment operations.

Password reset should use a dedicated operation.

This keeps security-sensitive actions explicit.

---

# 44. Server Validation

The backend must validate:

- Username exists
- Username is valid after trimming
- Username is unique case-insensitively
- Referenced Roles exist
- User exists before update
- Final Director rules
- Required Permissions
- Password policy where applicable

Frontend validation alone is insufficient.

---

# 45. Duplicate Username Error

Example user-facing error:

```text
This username is already in use.
```

Do not expose database constraint names or SQL messages.

---

# 46. Last Login

The Users list may display:

```text
last_login_at
```

This field updates after successful authentication.

It is informational.

It should not be used as the primary source for detailed login history.

Version 1 does not require a complex login-history feature.

---

# 47. User Administration Workspace

User Administration does not need a large Workspace like Orders.

A simple pattern is sufficient:

```text
Users List
    ↓
Create / Edit dialogs or simple pages
```

Do not over-engineer User management.

---

# 48. Role Administration

Role Administration remains part of Authentication.

A Role management interface may allow authorized Users to:

- View Roles
- Create normal Roles
- Edit Role name and description
- Assign Permissions
- Remove Permissions

Director system Role protections apply.

---

# 49. Permission Administration

Permissions should normally be presented as application-defined capabilities.

A useful UI may group them by module.

Example:

```text
Orders
☑ View Orders
☑ Create Orders
☑ Edit Orders

Projects
☑ View Projects
☐ Create Projects
```

Users should not type arbitrary Permission codes in normal administration.

---

# 50. User–Employee Linking

Creating a User does not require creating an Employee at the same time.

Likewise, creating an Employee does not require a User.

The Employees module may later provide actions such as:

```text
Link User Account
```

or:

```text
Create User for Employee
```

if that proves useful.

Do not complicate Version 1 User Creation before the Employee workflow is designed.

---

# 51. Employee Name in User Lists

The Authentication User table itself does not own a Full Name.

After Employee linking exists, UI may optionally display an Employee name by reading from the Employees module.

Example:

| Username | Employee | Roles | Status |
|---|---|---|---|
| aram | Aram Khachatryan | Director | Active |

This is a presentation join.

Do not copy Employee name into `auth.users`.

---

# 52. Authentication Role vs Project Role

User Administration deals only with Authentication Roles.

Examples:

```text
Director
Designer
Operator
```

It does not assign Project Roles:

```text
Owner
Assignee
Participant
Observer
```

Project Roles belong to Project Team management.

---

# 53. Error Handling

User Administration errors should use clear business language.

Examples:

```text
This username is already in use.
```

```text
This User is already inactive.
```

```text
You cannot deactivate the final active Director.
```

Do not display:

- SQL errors
- Stack traces
- Database constraint names

---

# 54. Confirmation Behavior

High-impact actions should require confirmation where useful.

Example:

```text
Deactivate User?
```

Message:

```text
This User will no longer be able to log into Lithograph ERP.
```

Routine actions should not use unnecessary confirmations.

---

# 55. Reset Password Confirmation

Resetting another User's password is security-sensitive.

The UI should require an explicit action.

After success:

```text
Password reset successfully.
```

Do not display the password afterward unless the administrator just entered it and the UI specifically needs to support copying during that immediate workflow.

The system itself must not be able to retrieve it later.

---

# 56. Session Handling After Password Reset

The security behavior after an administrator resets a User password must be deliberate.

Recommended Version 1 behavior:

```text
Invalidate existing Sessions
```

for that User.

This reduces the chance that previously authenticated Sessions remain active after credential reset.

---

# 57. Session Handling After Username Change

Changing Username does not necessarily require Session invalidation from a security perspective.

The final implementation may keep existing Sessions active unless a simpler consistent policy chooses otherwise.

Do not introduce unnecessary complexity.

---

# 58. Audit Information

Important account changes may preserve:

```text
created_at
created_by
updated_at
updated_by
```

according to the Authentication database specification.

Version 1 does not require a complete field-by-field change history.

---

# 59. User Business ID

Users do not receive Business IDs.

Do not create:

```text
USR-000001
```

Username is the human-facing User identifier.

UUID remains the internal database primary key.

---

# 60. Default Roles

Only the Director Role is required by the core system.

Other Roles such as:

```text
Designer

Operator

Manager
```

should be created based on real permission requirements.

Do not prebuild a large hierarchy of speculative Roles.

---

# 61. New Module Permissions

When a new business module is implemented, it may introduce new Permissions.

Example:

```text
orders.view

orders.create

orders.edit
```

Those Permissions become available for assignment to Roles.

The Director must automatically retain full access.

---

# 62. Navigation

User Administration should normally appear under:

```text
Administration
```

Example:

```text
Administration
├── Users
├── Roles
└── Employees
```

The final navigation may be refined later.

---

# 63. UI Access

If the current User lacks:

```text
users.view
```

the Users administration screen should not be normally accessible from navigation.

Backend authorization must still prevent direct unauthorized access.

---

# 64. Loading and Save States

Create, update, reset-password and activation operations should provide clear processing feedback.

Prevent accidental duplicate submissions.

Example:

```text
Creating...
```

while the request is running.

---

# 65. Version 1 Non-Goals

User Administration Version 1 does not include:

```text
Public Registration

Email Invitation

User Avatar

Rich User Profile

Email Verification

Forgot Password Email

Social Login

Two-Factor Authentication

API Key Management

Per-User Direct Permissions

Complex Login History

Device Management

Temporary Access Scheduling

Employee HR Management
```

---

# 66. Future Extensions

Possible future improvements may include:

- User self-service profile
- Advanced Session management
- Password expiration
- Temporary Users
- Two-factor authentication
- External identity providers

These are not current implementation requirements.

---

# 67. Testing Requirements

Important User Administration tests should include:

```text
Create User

Reject duplicate username

Case-insensitive username uniqueness

Deactivate User

Inactive User cannot log in

Reactivate User

Assign Role

Prevent duplicate Role assignment

Remove Role

Reset Password

Old password no longer works

Final active Director cannot be deactivated

Final active Director cannot lose Director Role
```

Exact automated test implementation will be determined during development.

---

# 68. Simplicity Rule

User Administration should remain small.

Its responsibility is:

```text
Manage ERP login accounts
```

It must not become:

```text
Employee Management

HR Management

Project Management

Security Operations Platform
```

Those responsibilities belong elsewhere or are outside Version 1.

---

# 69. Final User Administration Principle

A Director should be able to:

```text
Create a User

Give them a Username and Password

Assign the correct Role

Activate or deactivate access
```

without requiring:

- Email
- External identity providers
- Complex setup
- Technical database knowledge

That simplicity is the main Version 1 goal.

---

**End of Document**