# Lithograph ERP

**Document:** 07_Authentication.md  
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

---

# 1. Purpose

The Authentication module controls access to Lithograph ERP.

It is responsible for:

- User accounts
- Login
- Logout
- Password authentication
- Sessions
- Roles
- Permissions
- Authorization support
- User administration

The Authentication module does not store employee business information.

---

# 2. Module Scope

The Authentication module contains these features:

```text
Authentication
├── Users
├── Roles
├── Permissions
└── Sessions
```

These features belong to one module.

They should not be separated into independent services or applications in Version 1.

---

# 3. User

A User is an ERP login account.

A User is responsible for:

- Username
- Password authentication
- Account status
- Roles
- Permissions
- Sessions

A User is not the same as an Employee.

---

# 4. User and Employee Separation

Authentication must remain separate from employee business information.

Conceptually:

```text
User
=
Login Account
```

```text
Employee
=
Person Working for Lithograph
```

An Employee may optionally be linked to one User account.

An Employee may exist without ERP login access.

Do not store Employee information such as:

- Position
- Department
- Salary
- Project responsibilities

inside the Authentication User record.

---

# 5. Login Method

Version 1 uses local authentication.

Login requires:

```text
Username
Password
```

Email is not required.

Version 1 does not use:

- Email login
- Social login
- Google login
- Microsoft login
- Active Directory
- LDAP
- Public registration

---

# 6. Login Screen

The normal Login screen contains:

```text
Username

Password

Login
```

Optional UI elements may include:

```text
Show Password
```

if implemented using standard secure UI controls.

Do not add unnecessary registration or password-recovery features.

---

# 7. Username

Username is the identifier used to log into Lithograph ERP.

Rules:

- Required
- Unique
- Case-insensitive
- Editable by authorized users
- Cannot contain leading or trailing spaces

Examples:

```text
director

aram

designer1

operator1
```

The following must be treated as the same username:

```text
aram
Aram
ARAM
```

---

# 8. Password

Every User account requires a password unless a future approved authentication method replaces it.

Passwords must:

- Never be stored in plain text
- Never be logged
- Never be returned by the API
- Never be displayed by administrators
- Be stored only using established secure password hashing

Do not implement custom password cryptography.

---

# 9. Password Creation

When an authorized administrator creates a User, they set an initial password.

Example:

```text
Username:
designer1

Initial Password:
********
```

The administrator may provide the username and password directly to the employee.

Email delivery is not required.

---

# 10. Password Change

A User may change their own password if the final permissions and UI rules allow it.

An authorized administrator may reset a User password.

Resetting a password means setting a new password.

The old password must never be recoverable or displayed.

---

# 11. First-Run Setup

Lithograph ERP has no preconfigured password.

When the application starts and no User exists, the application enters Initial Setup mode.

Initial Setup asks the operator to define the password for the first Director account.

The system then creates:

```text
Username:
director

Role:
Director
```

The account becomes the initial administrator of Lithograph ERP.

---

# 12. First Director Username

The initial username is:

```text
director
```

After logging in, the Director may change the username.

The system must not require the username to remain `director`.

The special authority belongs to the Director role and final active Director protection, not permanently to the literal username.

---

# 13. Initial Setup Security

Initial Setup must only be available when no User account exists.

Once the initial Director account is successfully created, the Initial Setup process must no longer be available through normal application use.

The setup password must be handled using the same password-security rules as normal User passwords.

---

# 14. Director Role

The Director role is the highest system role in Version 1.

The Director has access to all available application permissions.

When a new module introduces new permissions, the Director must receive access to them.

The Director should not require manual permission updates each time a new system permission is introduced.

---

# 15. Final Active Director Protection

The system must prevent administrators from leaving the ERP without an active Director.

The final active Director account must not be:

- Deactivated
- Deleted
- Stripped of the Director role

until another active User with the Director role exists.

This prevents accidental system lockout.

---

# 16. User Creation

There is no public registration.

Authorized administrators create Users manually.

Minimum User creation information:

```text
Username

Initial Password

Role
```

The account may also have:

```text
Active
```

status.

Employee linking will be handled through the Employees module and should not complicate basic authentication setup.

---

# 17. User Account Status

Version 1 uses:

```text
Active

Inactive
```

Active User:

```text
May authenticate
```

Inactive User:

```text
Cannot authenticate
```

An inactive User still exists in the system.

Historical references must remain valid.

---

# 18. Deactivation

Deactivating a User:

- Prevents new login
- Should invalidate or disable active authentication Sessions
- Does not remove historical business records
- Does not delete related Employee data
- Does not rewrite historical audit fields

Deactivation is preferred over destructive deletion for normal account management.

---

# 19. Roles

A Role is a reusable collection of Permissions.

Examples:

```text
Director

Manager

Designer

Operator
```

A User may have one or more Roles.

---

# 20. Multiple Roles

Version 1 supports multiple Roles per User.

Example:

```text
User:
employee1

Roles:
Designer
Operator
```

The User receives the combined allowed Permissions of all assigned Roles.

---

# 21. Permissions

A Permission represents one allowed application action.

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

clients.view

projects.create

orders.edit
```

Permissions should describe actions, not job titles.

---

# 22. Permission Ownership

Business modules define the Permissions required for their own features.

Examples:

Orders may define:

```text
orders.view
orders.create
orders.edit
```

Authentication provides the mechanism for:

- Storing Permissions
- Assigning Permissions to Roles
- Resolving User access

---

# 23. Direct User Permissions

Version 1 does not require direct Permission assignment to individual Users.

Normal structure:

```text
User
   ↓
Role
   ↓
Permission
```

Do not add:

```text
user_permissions
```

unless a future requirement explicitly needs it.

---

# 24. Role Administration

Authorized administrators may:

- View Roles
- Create Roles
- Edit Roles
- Assign Permissions
- Remove Permissions
- Assign Roles to Users
- Remove Roles from Users

The Director role may require additional protection because of its system-level significance.

---

# 25. Permission Administration

Permissions are primarily defined by application modules.

Administrators may assign existing Permissions to Roles.

Normal users should not manually create arbitrary Permission codes through the UI.

Permission definitions should remain controlled by the application.

---

# 26. Authentication Session

A Session represents one authenticated connection between a User and Lithograph ERP.

Conceptually:

```text
User logs in
      ↓
Session created
      ↓
Requests authenticated
      ↓
Logout / Expiration
      ↓
Session invalidated
```

---

# 27. Multiple Sessions

Version 1 may allow a User to have multiple active Sessions.

Examples:

- Office computer
- Laptop
- Another browser

The system does not need complex device management in Version 1.

---

# 28. Session Security

Session authentication must use established ASP.NET Core security mechanisms.

Do not implement custom token cryptography.

Raw authentication secrets should not be stored in the database.

If a session token or equivalent secret must be persisted, only a safe representation such as a secure hash should be stored.

---

# 29. Session Expiration

Sessions must expire according to a defined application policy.

The timeout should be configurable in application configuration rather than hardcoded throughout the code.

Detailed timeout values may be selected during implementation.

---

# 30. Session Activity

The system may track basic session information such as:

```text
created_at

expires_at

last_activity_at
```

Additional security metadata may include:

```text
ip_address

user_agent
```

These fields are technical information, not employee profile data.

---

# 31. Logout

When the User logs out:

```text
Current Session
    ↓
Invalidated
```

The invalidated Session must no longer authenticate requests.

Logout should not deactivate the User account itself.

---

# 32. User Deactivation and Sessions

When a User is deactivated, their active Sessions should no longer provide valid access.

The system must not allow a previously authenticated inactive User to continue normal ERP access indefinitely.

---

# 33. Authorization

Authentication answers:

```text
Who is the User?
```

Authorization answers:

```text
What is the User allowed to do?
```

These concepts must remain separate.

---

# 34. Backend Authorization

Backend authorization is authoritative.

Every protected action must enforce required Permissions on the server.

The frontend may hide unavailable actions for usability, but frontend visibility is not a security boundary.

---

# 35. Frontend Permission Behavior

If the User lacks a Permission, the frontend should normally avoid presenting unusable functionality.

Depending on the feature, an action may be:

```text
Hidden
```

or:

```text
Disabled
```

Backend authorization must still reject unauthorized requests.

---

# 36. Authentication API Responsibilities

The Authentication API may include operations conceptually equivalent to:

```text
Login

Logout

Get Current User

Change Password
```

Administrative User operations may include:

```text
List Users

Create User

Edit User

Activate User

Deactivate User

Reset Password

Assign Role

Remove Role
```

Exact routes will be defined during implementation.

---

# 37. Authentication DTO Rules

Authentication APIs must use dedicated request and response DTOs.

Never return database User entities directly if doing so could expose internal fields.

Responses must never include:

```text
password_hash

session_token_hash

security secrets
```

---

# 38. Authentication Database Schema

Authentication owns PostgreSQL schema:

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

Exact table definitions are specified in the Authentication schema/table documentation.

---

# 39. Username Search

User administration should support straightforward searching by Username.

The list may also support filtering by:

- Role
- Active status

Do not build an advanced search system for Version 1.

---

# 40. User Administration UI

User administration belongs under:

```text
Administration
```

A simple Users list may show:

| Username | Roles | Status |
|---|---|---|
| director | Director | Active |
| designer1 | Designer | Active |
| operator1 | Operator | Inactive |

Possible actions:

```text
Create User

Edit User

Reset Password

Manage Roles

Activate

Deactivate
```

---

# 41. Create User Form

The initial Create User form should remain simple.

Fields:

```text
Username

Password

Confirm Password

Roles

Active
```

Actions:

```text
Create

Cancel
```

Do not add unrelated employee business fields here.

---

# 42. Edit User Form

The Edit User form may allow:

```text
Username

Roles

Active
```

Password reset should preferably remain a separate explicit action.

This prevents accidental password changes while editing unrelated account information.

---

# 43. Reset Password

Reset Password is a security-sensitive administrative action.

The UI should clearly indicate which User is being modified.

The administrator enters:

```text
New Password

Confirm Password
```

The old password is never shown.

---

# 44. Validation

Authentication must validate important rules on the backend.

Examples:

- Username required
- Username unique
- Username case-insensitive uniqueness
- Password required when appropriate
- Password confirmation matches in UI workflow
- Assigned Role exists
- Final active Director protection

Frontend validation is for usability.

Backend validation is authoritative.

---

# 45. Password Policy

Version 1 should use a reasonable secure password policy without creating unnecessary complexity for an internal ERP.

The exact minimum password rules may be selected during implementation using established security guidance.

Do not create complicated custom password scoring systems unless required.

---

# 46. Failed Login

Invalid login attempts should return a generic authentication failure.

Example:

```text
Invalid username or password.
```

Do not reveal whether:

- Username exists
- Password alone was incorrect

This avoids unnecessary account information disclosure.

---

# 47. Login Rate Protection

The implementation should use appropriate framework-supported protection against repeated automated login attempts where practical.

Do not build a complex custom anti-abuse platform for an internal Version 1 ERP.

---

# 48. Sensitive Logging

Never log:

```text
Plaintext passwords

Password hashes

Raw session tokens

Authentication secrets
```

Authentication logs may safely include appropriate operational information such as:

- Successful login time
- Failed login event
- User ID where safe
- Session lifecycle events

subject to implementation needs.

---

# 49. Audit Scope

Version 1 does not require a complete authentication audit-history system.

Basic timestamps and session information are sufficient initially.

Advanced audit logging may be added later if needed.

---

# 50. User Deletion

Normal Version 1 administration should prefer:

```text
Deactivate User
```

rather than destructive physical deletion.

This protects:

- Historical references
- Audit information
- Business relationships

The detailed persistence strategy is defined by the database specification.

---

# 51. Employee Linking

User-to-Employee linking belongs primarily to the Employees module.

Authentication should support the relationship without becoming responsible for employee data.

Do not require every User administration operation to include Employee fields.

---

# 52. No Email Dependency

Authentication must work fully without email.

The following workflows must not depend on email:

- First-run setup
- User creation
- Login
- Password reset by administrator
- Role assignment

This is an intentional project requirement.

---

# 53. Version 1 Non-Goals

Version 1 Authentication does not include:

```text
Public Registration

Email Login

Email Verification

Forgot Password Email

Google Login

Microsoft Login

OAuth Providers

Active Directory

LDAP

Two-Factor Authentication

Biometric Authentication

Single Sign-On

Direct User Permissions

Advanced Device Management

Security Alert Center

Complex Login History UI
```

Do not implement these unless the project scope changes.

---

# 54. Future Extensions

Possible future Authentication extensions may include:

- Two-factor authentication
- External identity providers
- Password expiration
- Advanced login history
- Session/device management
- Temporary accounts
- Single Sign-On

These possibilities must not complicate Version 1.

---

# 55. Security Principle

Authentication is one area where simplicity must not mean unsafe implementation.

Use simple architecture together with proven framework security.

Prefer:

```text
Standard ASP.NET Core security
```

over:

```text
Custom authentication inventions
```

---

# 56. Final Authentication Principle

The Authentication module should answer three questions reliably:

```text
Who is this User?

Is this Session valid?

Does this User have permission?
```

Everything else should remain outside the module unless it directly supports these responsibilities.

---

**End of Document**