# Lithograph ERP

**Document:** 11_Employees_Module.md
**Version:** 1.0
**Status:** Approved
**Project:** Lithograph ERP
**Module:** Employees

**Related Documents:**

- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `06_UI_UX_Principles.md`
- `07_Authentication.md`
- `10_Users_Module.md`

---

# 1. Purpose

The Employees module stores information about people working for Lithograph.

An Employee represents a business person.

An Employee is different from an Authentication User.

The Employees module exists so other business modules can reference real people without depending directly on login accounts.

---

# 2. Main Responsibilities

The Employees module is responsible for:

```text
Employee records

Employee names

Employee positions

Employee contact information

Employee active status

Optional link to Authentication User
```

The Employees module does not manage:

```text
Passwords

Roles

Permissions

Sessions

Authentication
```

Those belong to the Authentication module.

---

# 3. Employee Definition

An Employee is:

```text
A person working for Lithograph
```

Examples:

```text
Director

Designer

Machine Operator

Installer

Project Manager
```

The Employee entity represents the person, not their ERP access.

---

# 4. Employee vs User

The distinction must remain clear.

```text
Employee
=
Business Person
```

```text
User
=
ERP Login Account
```

An Employee may have:

```text
No User Account
```

or:

```text
One User Account
```

---

# 5. Why Separate Employee and User

An Employee may exist without ERP access.

Example:

```text
Installer
```

may work for Lithograph but not need to log into the ERP.

Likewise, Authentication must remain independent from employee business information.

This allows future account types that may not represent Employees.

---

# 6. Module Ownership

The Employees module owns PostgreSQL schema:

```text
employees
```

Initial table:

```text
employees.employees
```

Version 1 does not require additional Employee tables.

---

# 7. Database Structure

Initial table:

```text
employees
└── employees
```

The module should remain intentionally small.

---

# 8. Table: employees.employees

## Purpose

Stores Lithograph Employee records.

Each row represents one person working for Lithograph.

---

# 9. Employee Columns

| Column       | PostgreSQL Type | Nullable | Description                                   |
| ------------ | --------------- | -------: | --------------------------------------------- |
| `id`         | `uuid`          |       No | Primary key                                   |
| `user_id`    | `uuid`          |      Yes | Optional linked Authentication User           |
| `full_name`  | `varchar(200)`  |       No | Employee full name                            |
| `position`   | `varchar(150)`  |      Yes | Employee job position                         |
| `phone`      | `varchar(50)`   |      Yes | Contact phone                                 |
| `email`      | `varchar(200)`  |      Yes | Contact email                                 |
| `is_active`  | `boolean`       |       No | Whether Employee currently works/is available |
| `created_at` | `timestamptz`   |       No | Creation time                                 |
| `created_by` | `uuid`          |      Yes | Creating User                                 |
| `updated_at` | `timestamptz`   |      Yes | Last update time                              |
| `updated_by` | `uuid`          |      Yes | Last modifying User                           |

---

# 10. Primary Key

```text
PRIMARY KEY (id)
```

`id` is a UUID.

---

# 11. Employee Business ID

Version 1 does not require Employee Business IDs.

Do not create identifiers such as:

```text
EMP-000001
```

unless a future business requirement demonstrates a need for them.

Employees are normally identified in the UI by:

```text
Full Name
```

---

# 12. Full Name

`full_name` is required.

Examples:

```text
Aram Khachatryan

Samvel Petrosyan
```

Version 1 intentionally uses one Full Name field rather than creating separate:

```text
first_name

middle_name

last_name
```

unless future requirements make that necessary.

This keeps the model simple.

---

# 13. Position

`position` is a simple free-text field.

Examples:

```text
Director

Designer

UV Printer Operator

Installer
```

Version 1 does not require a separate Positions table.

Do not create a Position management module unless a real requirement appears.

---

# 14. Department

Version 1 does not contain Departments.

Do not add:

```text
department_id
```

or a Departments table.

Lithograph is currently small enough that this would create unnecessary structure.

---

# 15. Phone

Employee phone is optional.

The system should store it as text rather than a numeric value.

This allows:

- `+`
- spaces
- parentheses
- country codes

No complex phone-number validation is required in Version 1.

---

# 16. Email

Employee email is optional.

Employee email is contact information only.

It is not used for Authentication in Version 1.

A User can log in without an email address.

---

# 17. Active Employee

An active Employee:

```text
is_active = true
```

means the Employee currently works for Lithograph or should be available for normal business assignment.

---

# 18. Inactive Employee

An inactive Employee:

```text
is_active = false
```

remains stored for historical purposes.

Inactive Employees should normally not appear in new assignment selectors unless the user explicitly requests inactive records.

---

# 19. Employee Deactivation

When an Employee leaves Lithograph, normal behavior should be:

```text
Deactivate Employee
```

rather than deleting the record.

This preserves historical relationships such as:

- Previous Projects
- Previous Project Teams
- Audit history
- Future reports

---

# 20. Physical Employee Deletion

Version 1 does not require normal physical deletion of Employees.

The UI should provide:

```text
Deactivate
```

instead of:

```text
Delete permanently
```

for ordinary use.

---

# 21. Soft Delete

Version 1 does not require a separate:

```text
is_deleted
```

field for Employees.

`is_active` is sufficient for normal business use.

If future legal or operational requirements demand true soft deletion, it may be added later.

---

# 22. User Link

An Employee may optionally link to one Authentication User.

Database field:

```text
user_id
```

References:

```text
auth.users.id
```

---

# 23. User Link Relationship

Version 1 relationship:

```text
Employee
   │
   └── 0 or 1 User
```

and:

```text
User
   │
   └── 0 or 1 Employee
```

This is an optional one-to-one relationship.

---

# 24. User Link Constraint

`user_id` must be unique when it is not null.

Conceptually:

```text
UNIQUE (user_id)
```

This prevents one User account from being linked to multiple Employees.

---

# 25. User Link Foreign Key

```text
employees.employees.user_id
    ↓
auth.users.id
```

The link is optional.

The Employees module depends on Authentication only for this optional relationship.

Authentication itself does not depend on Employees.

---

# 26. User Deletion Behavior

Because normal Version 1 User management uses deactivation rather than deletion, the relationship should normally remain stable.

If a User is ever physically removed through exceptional maintenance, the Employee record must not be deleted.

Recommended relationship behavior:

```text
ON DELETE SET NULL
```

or equivalent Entity Framework behavior.

This preserves the Employee.

---

# 27. Employee Deactivation Does Not Automatically Deactivate User

Employee and User status are separate concepts.

Therefore:

```text
Employee is_active = false
```

does not automatically change:

```text
User is_active
```

This avoids hidden cross-module behavior.

However, the UI should warn when an inactive Employee still has an active linked User account.

---

# 28. User Deactivation Does Not Automatically Deactivate Employee

Likewise:

```text
User is_active = false
```

does not automatically mean:

```text
Employee is_active = false
```

A person may continue working for Lithograph while temporarily having no ERP access.

---

# 29. Inactive Employee with Active User Warning

When an Employee is being deactivated and has an active linked User, the UI should warn:

```text
This Employee has an active ERP User account.

Deactivating the Employee does not disable ERP login access.
```

If the current administrator has permission, a convenient separate action may be offered:

```text
Deactivate User Account
```

but this should remain an explicit action.

---

# 30. Employee Creation

Authorized Users may create an Employee manually.

Minimum required information:

```text
Full Name
```

Optional information:

```text
Position

Phone

Email

Linked User

Active
```

Default:

```text
Active = true
```

---

# 31. Create Employee Interface

Example:

```text
New Employee

Full Name
[____________________________]

Position
[____________________________]

Phone
[____________________________]

Email
[____________________________]

User Account
[ None ▼ ]

☑ Active

[Create] [Cancel]
```

The interface should remain simple.

---

# 32. User Account Selector

The User Account selector should show available Authentication Users that are not already linked to another Employee.

Example:

```text
None

director

designer1

operator1
```

Already-linked Users should not normally appear as selectable options.

---

# 33. Employee Without User

It must be possible to create:

```text
Employee
```

without selecting a User account.

Example:

```text
Employee:
Installation Worker

User:
None
```

This is a normal supported scenario.

---

# 34. User Without Employee

Authentication may also contain Users that are not linked to an Employee.

This should remain technically valid.

The system should not require every User to have an Employee record.

---

# 35. Link Existing User

An authorized administrator may link an existing User to an Employee.

The system must validate:

- Employee exists
- User exists
- User is not already linked to another Employee

---

# 36. Unlink User

An authorized administrator may remove the User–Employee relationship.

This removes only:

```text
Employee.user_id
```

It does not delete:

```text
Employee
```

or:

```text
User
```

---

# 37. Create User for Employee

A future convenience action may allow:

```text
Create User Account
```

directly from an Employee.

However, Version 1 does not require this combined workflow initially.

The simple approach is:

1. Create User in Administration.
2. Link User to Employee.

A combined action may be added later if repeated daily use demonstrates value.

---

# 38. Employee Permissions

Initial Employees module Permissions:

```text
employees.view

employees.create

employees.edit

employees.activate

employees.link_user
```

---

# 39. employees.view

Allows viewing:

- Employee list
- Employee details

Sensitive future Employee information, if introduced, may require separate Permissions later.

Version 1 Employee data is basic operational information.

---

# 40. employees.create

Allows creation of new Employees.

---

# 41. employees.edit

Allows editing:

```text
Full Name

Position

Phone

Email
```

Normal User-account linking may require:

```text
employees.link_user
```

separately.

---

# 42. employees.activate

Allows:

```text
Activate Employee

Deactivate Employee
```

---

# 43. employees.link_user

Allows:

```text
Link User

Unlink User
```

This Permission controls cross-module account linking.

---

# 44. Employee List

The Employees module should provide a simple Employee list.

Example:

| Employee            | Position  | User     | Status |
| ------------------- | --------- | -------- | ------ |
| Aram Khachatryan    | Director  | director | Active |
| Samvel Petrosyan    | Operator  | samvel   | Active |
| Installation Worker | Installer | —        | Active |

---

# 45. Employee List Columns

Recommended Version 1 columns:

```text
Full Name

Position

Linked User

Status
```

Optional:

```text
Phone
```

if useful in daily operation.

Do not overload the list with unnecessary fields.

---

# 46. Employee Search

Search should support:

```text
Full Name

Position
```

Optional filtering:

```text
Active Status

Has User Account
```

Advanced search is not required.

---

# 47. Active Employee Filtering

Normal Employee selectors used by business modules should show:

```text
Active Employees
```

by default.

This applies especially to:

```text
Project Team selection
```

Inactive Employees remain available for historical display.

---

# 48. Employee Edit Interface

Employee editing may use a simple page or dialog.

Fields:

```text
Full Name

Position

Phone

Email

User Account

Active
```

Keep the interface straightforward.

---

# 49. Employee Workspace

Version 1 does not require a complex Employee Workspace.

A simple:

```text
Employee List
    ↓
Create / Edit
```

pattern is sufficient.

If future HR functionality appears, a richer Workspace may be introduced later.

---

# 50. Project Usage

The Projects module will reference:

```text
employees.employees.id
```

for Project Team assignments.

Example:

```text
Project
    │
    ├── Owner → Employee
    ├── Assignee → Employee
    ├── Participant → Employee
    └── Observer → Employee
```

Project assignments reference Employees, not Authentication Users.

---

# 51. Why Projects Reference Employee

A Project responsibility belongs to a person working for Lithograph.

It should not depend on whether that person currently has login access.

Therefore:

Correct:

```text
project_members.employee_id
```

Incorrect:

```text
project_members.user_id
```

for the normal business relationship.

---

# 52. Checklist Usage

Version 1 Checklist Items do not assign Employees.

Do not create an Employee relationship from Checklist Items.

This may be added later only if required.

---

# 53. Orders Usage

Version 1 Orders do not contain separate Employee team assignments.

Orders inherit the logical Project Team.

Do not create:

```text
orders.order_members
```

from the Employees module.

---

# 54. Created By and Updated By

Employee audit fields reference Authentication Users.

Example:

```text
created_by
    ↓
auth.users.id
```

This is correct because audit fields represent which authenticated account performed an action.

Business responsibility fields should reference Employees instead.

---

# 55. Important Distinction

Use:

```text
User
```

when asking:

```text
Who performed this action in the ERP?
```

Use:

```text
Employee
```

when asking:

```text
Which person is responsible for this business work?
```

This rule should guide future database relationships.

---

# 56. API Responsibilities

The Employees API may conceptually provide:

```text
Get Employees

Get Employee

Create Employee

Update Employee

Activate Employee

Deactivate Employee

Link User

Unlink User
```

Exact REST routes will be defined during implementation.

---

# 57. Employee Response DTO

A normal response may contain:

```text
id

full_name

position

phone

email

is_active

linked_user
```

Linked User information should contain only safe Authentication data such as:

```text
id

username

is_active
```

Never expose Authentication secrets.

---

# 58. Create Employee Request

Conceptual fields:

```text
full_name

position

phone

email

user_id

is_active
```

Only:

```text
full_name
```

is required initially.

---

# 59. Update Employee Request

Conceptual editable information:

```text
full_name

position

phone

email

is_active
```

User linking may use a dedicated operation or be included if permission handling remains clear.

---

# 60. Validation Rules

Backend must validate:

- Full Name is required
- Full Name is not empty after trimming
- Linked User exists if provided
- Linked User is not already linked to another Employee
- Employee exists before update
- Current User has the required Permission

---

# 61. Duplicate Employees

Version 1 does not enforce Full Name uniqueness.

Two people may legitimately have the same name.

Therefore:

```text
full_name
```

must not have a unique constraint.

---

# 62. Email Uniqueness

Version 1 does not require Employee email uniqueness.

Email is optional contact information rather than Authentication identity.

Do not create unnecessary uniqueness constraints.

---

# 63. Phone Uniqueness

Phone numbers are not unique identifiers.

Do not enforce Phone uniqueness.

---

# 64. Required Indexes

Primary key:

```text
PRIMARY KEY (id)
```

User link:

```text
UNIQUE INDEX on user_id
WHERE user_id IS NOT NULL
```

or equivalent unique nullable behavior through PostgreSQL/Entity Framework.

An index may be added for:

```text
is_active
```

only if actual Employee-selection queries justify it.

---

# 65. Name Search Index

Do not prematurely introduce specialized full-text search infrastructure.

Normal database querying is sufficient initially.

If Employee count grows enough to justify optimization, indexes can be added based on actual query behavior.

---

# 66. Delete Behavior

Important historical business records may reference Employees.

Therefore ordinary Employee deletion should not be available.

Deactivation preserves historical references.

---

# 67. Historical Project Assignments

If an Employee becomes inactive, existing Project Team history must remain valid.

Example:

```text
Project completed in 2026
Owner: Employee A
```

must not lose Employee A merely because that person later left Lithograph.

---

# 68. New Assignment Rule

Inactive Employees should normally not be assignable to new Project Team roles.

The backend should enforce this rule when creating new assignments.

Historical existing assignments remain valid.

---

# 69. Employee Reactivation

An inactive Employee may be reactivated.

Example:

```text
is_active = true
```

After reactivation, the Employee may again appear in normal assignment selectors.

---

# 70. Position History

Version 1 does not track Employee position history.

If:

```text
Designer
```

changes to:

```text
Project Manager
```

the current `position` value is updated.

Historical position tracking may be added later if it becomes useful.

---

# 71. Employment Dates

Version 1 does not require:

```text
hire_date

termination_date
```

unless Lithograph decides these are operationally useful.

Do not add HR-oriented fields preemptively.

---

# 72. Salary

Salary information is outside Version 1 Employees scope.

Do not add:

```text
salary
```

to the Employees table.

Future Finance or HR functionality should define salary handling separately if required.

---

# 73. Personal HR Information

Version 1 does not store:

- Birth date
- Passport details
- Home address
- Family information
- Tax information
- Medical information

The Employees module is operational, not a full HR system.

---

# 74. Employee Notes

Version 1 does not require a generic Employee Notes field.

Add it later only if there is a clear operational use case.

This avoids accumulating unstructured information without purpose.

---

# 75. Department Non-Goal

Do not introduce:

```text
Departments

Teams

Organizational hierarchy
```

in Version 1.

Project Teams provide the current operational grouping required by Lithograph.

---

# 76. Employee Avatar

Version 1 does not require profile photos or avatars.

These may be added later if they provide useful identification in Project Team interfaces.

---

# 77. User Account Status Display

When an Employee has a linked User, the UI may show:

```text
User: director
Status: Active
```

When no User exists:

```text
User: None
```

This is informational.

---

# 78. Employee Active vs User Active

These two statuses must be visibly distinct when necessary.

Example:

```text
Employee:
Inactive

User:
Active
```

This is allowed but should trigger an administrative warning.

Do not silently merge the two concepts.

---

# 79. Navigation

Employees should normally appear under:

```text
Administration
```

in Version 1.

Example:

```text
Administration
├── Users
├── Roles
├── Employees
├── Order Types
└── Calculator Templates
```

If Employee management becomes a major daily workflow later, navigation may be reconsidered.

---

# 80. Version 1 Non-Goals

The Employees module does not include:

```text
HR Management

Payroll

Salary

Attendance

Timesheets

Vacation Management

Department Management

Position History

Employment Contracts

Performance Reviews

Employee Documents

Employee Avatars

Employee Business IDs

Complex Organizational Hierarchy
```

These should not be introduced without real requirements.

---

# 81. Future Extensions

Possible future additions may include:

- Employee photos
- Departments
- Employment dates
- Skill information
- Machine qualifications
- Work schedules
- HR information

Future possibilities must not complicate Version 1.

---

# 82. Testing Requirements

Important Employee tests should include:

```text
Create Employee

Create Employee without User

Link existing User

Prevent one User from linking to two Employees

Unlink User

Deactivate Employee

Inactive Employee remains stored

Reactivate Employee

Inactive Employee excluded from new Project assignments

Employee deactivation does not automatically deactivate User

User deactivation does not automatically deactivate Employee
```

---

# 83. Module Simplicity Rule

Before adding another Employee field or table, ask:

```text
Does Lithograph need this information for current daily operations?
```

If the answer is no, do not add it yet.

---

# 84. Version 1 Employee Schema Summary

```text
employees.employees

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

One table is enough for Version 1.

---

# 85. Final Employee Principle

The Employees module exists to answer:

```text
Who works for Lithograph?
```

and:

```text
Which Employee is responsible for business work?
```

It should remain a simple operational people directory, not become an HR system.

---

**End of Document**
