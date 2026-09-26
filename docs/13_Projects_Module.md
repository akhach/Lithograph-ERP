# Lithograph ERP

**Document:** 13_Projects_Module.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Projects

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `05_Numbering_System.md`
- `06_UI_UX_Principles.md`
- `11_Employees_Module.md`
- `12_Clients_Module.md`

---

# 1. Purpose

The Projects module groups related Lithograph Orders under one Client and manages the Employees responsible for that work.

A Project acts as the container between:

```text
Client
   ↓
Project
   ↓
Orders
```

A Project may contain one or many Orders.

---

# 2. Main Responsibilities

The Projects module is responsible for:

```text
Project records

Project Business IDs

Client relationship

Project status

Project dates

Project Team

Project-level Order list
```

The Projects module does not manage:

```text
Order calculation

Order Selling Price

Order Cost Price

Order Checklist

Order Folder Links
```

Those belong to the Orders and Calculator modules.

---

# 3. Project Definition

A Project is:

```text
A collection of related Orders for one Client.
```

Example:

```text
Client:
Samsung Armenia

Project:
New Store Opening

Orders:
- Exterior Sign
- UV Printed Panels
- Window Stickers
- Graphic Design
- Installation
```

---

# 4. Module Ownership

The Projects module owns PostgreSQL schema:

```text
projects
```

Version 1 tables:

```text
projects.projects

projects.project_members
```

---

# 5. Database Structure

```text
projects
│
├── projects
│
└── project_members
```

Relationship:

```text
Client
   │
   ▼
Project
   │
   ├── Project Members
   │
   └── Orders
```

---

# 6. Table: projects.projects

## Purpose

Stores Project master records.

Each row represents one Lithograph Project.

---

# 7. Project Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `business_id` | `varchar(30)` | No | Human-readable Project ID |
| `client_id` | `uuid` | No | Client reference |
| `name` | `varchar(250)` | No | Project name |
| `description` | `text` | Yes | Project description |
| `status` | `varchar(30)` | No | Current Project status |
| `start_date` | `date` | Yes | Project start date |
| `deadline` | `date` | Yes | Project deadline |
| `created_at` | `timestamptz` | No | Creation time |
| `created_by` | `uuid` | Yes | Creating User |
| `updated_at` | `timestamptz` | Yes | Last update time |
| `updated_by` | `uuid` | Yes | Last modifying User |

---

# 8. Primary Key

```text
PRIMARY KEY (id)
```

`id` is a UUID.

---

# 9. Project Business ID

Every Project receives a Business ID.

Format:

```text
PRJ-YYYY-000001
```

Example:

```text
PRJ-2026-000125
```

Project numbering resets each calendar year.

---

# 10. Business ID Generation

The backend generates the Project Business ID automatically.

The frontend does not generate or edit it.

Example:

```text
Create Project
      ↓
Backend generates number
      ↓
PRJ-2026-000126
      ↓
Project stored
```

---

# 11. Business ID Rules

Project Business ID:

- Is required
- Is unique
- Is automatically generated
- Is read-only for normal Users
- Is never reused
- Never changes after creation

Changing the Client, status, name, deadline, or Project Team does not change the Business ID.

---

# 12. Business ID Constraint

Database must enforce:

```text
UNIQUE (business_id)
```

Business ID must be indexed for fast lookup.

---

# 13. Client Relationship

Every Project belongs to exactly one Client.

Database field:

```text
client_id
```

References:

```text
clients.clients.id
```

Relationship:

```text
Client
   │
   ├── Project
   ├── Project
   └── Project
```

---

# 14. Client Is Required

A Project cannot exist without a Client in Version 1.

Therefore:

```text
client_id NOT NULL
```

---

# 15. Creating Project for Inactive Client

A new Project must not normally be created for an inactive Client.

The frontend should exclude inactive Clients from normal selection.

The backend must also validate this rule.

Existing Projects remain valid if their Client later becomes inactive.

---

# 16. Client Deactivation

Deactivating a Client does not:

- Delete Projects
- Cancel Projects
- Change Project status
- Delete Orders

Historical relationships remain intact.

---

# 17. Changing Project Client

Version 1 may allow an authorized User to change the Project Client if required.

However, this should be treated as a deliberate edit.

Changing Client must not change:

```text
business_id
```

or delete existing Orders.

---

# 18. Project Name

`name` is required.

Examples:

```text
New Store Opening

Exhibition 2027

Office Rebranding

Annual Retail Campaign
```

Project names do not need to be unique.

Different Clients may use the same Project name.

The same Client may also have similarly named Projects.

Business ID provides unique human identification.

---

# 19. Description

`description` is optional free-form text.

It should contain useful high-level Project information.

Example:

```text
Complete visual branding for the new Dalma Garden store.
```

Do not use Project Description as a replacement for structured Order information.

---

# 20. Project Status

Version 1 Project statuses are:

```text
Draft

Active

On Hold

Completed

Cancelled
```

These are stable workflow states.

---

# 21. Draft

```text
Draft
```

means the Project exists but active work has not formally started.

Draft Projects may still be edited and prepared.

---

# 22. Active

```text
Active
```

means the Project is currently being worked on.

---

# 23. On Hold

```text
On Hold
```

means work is temporarily paused.

The Project and its Orders remain stored normally.

---

# 24. Completed

```text
Completed
```

means the Project's work is considered finished.

Completed Projects remain available for:

- Historical viewing
- Reports
- Order history
- Profit analysis

---

# 25. Cancelled

```text
Cancelled
```

means the Project will not continue.

Cancelled Projects remain stored.

Cancellation is not deletion.

---

# 26. Status Storage

The database stores a clear status value.

Conceptually:

```text
status
```

The C# application should represent Project Status as a strongly typed structural value.

Do not create a configurable Project Status management system in Version 1.

---

# 27. Project Status Is Not is_active

Projects do not require:

```text
is_active
```

because Project lifecycle is already represented by:

```text
status
```

Do not duplicate lifecycle concepts unnecessarily.

---

# 28. Project Deletion

Version 1 does not require normal permanent Project deletion.

Use:

```text
Cancelled
```

when a Project should no longer continue.

This preserves:

- Orders
- Project Team history
- Reports
- Business references

---

# 29. Soft Delete

Version 1 does not require:

```text
is_deleted
```

for Projects.

Project status provides the required operational lifecycle.

---

# 30. Start Date

`start_date` is optional.

It represents the business start date of the Project.

This is a calendar date rather than a timestamp.

---

# 31. Deadline

`deadline` is optional.

It represents the expected Project completion date.

Example:

```text
2026-11-30
```

The database stores it as:

```text
date
```

because time-of-day is not required.

---

# 32. Deadline Validation

If both dates exist:

```text
deadline
```

should normally not be earlier than:

```text
start_date
```

Backend should validate this.

---

# 33. Project Team

A Project may have Employees assigned using Project Roles.

Version 1 Project Roles:

```text
Owner

Assignee

Participant

Observer
```

These are business responsibility roles.

They are not Authentication Roles.

---

# 34. Project Team Relationship

Conceptually:

```text
Project
   │
   ├── Owner
   ├── Assignee
   ├── Participants
   └── Observers
```

Assignments reference:

```text
employees.employees.id
```

not Authentication Users.

---

# 35. Table: projects.project_members

## Purpose

Stores Employee assignments and Project Roles.

Each row connects:

```text
Project

Employee

Project Role
```

---

# 36. project_members Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `project_id` | `uuid` | No | Project reference |
| `employee_id` | `uuid` | No | Employee reference |
| `project_role` | `varchar(30)` | No | Owner / Assignee / Participant / Observer |
| `assigned_at` | `timestamptz` | No | Assignment creation time |
| `assigned_by` | `uuid` | Yes | Authentication User that created assignment |

---

# 37. project_members Primary Key

Use composite primary key:

```text
PRIMARY KEY (
    project_id,
    employee_id,
    project_role
)
```

No artificial UUID is required.

---

# 38. Why Role Is Part of Primary Key

An Employee may have more than one Project Role if necessary.

Example:

```text
Employee A
    Owner
    Assignee
```

Therefore the same Employee may appear more than once for the same Project when the roles differ.

The same Employee cannot have the same role twice.

---

# 39. Project Role Values

Allowed Version 1 values:

```text
Owner

Assignee

Participant

Observer
```

Do not allow arbitrary free-text Project Roles.

---

# 40. Project Role Implementation

The C# application should use a strongly typed Project Role representation.

Database storage may use stable string values.

A database constraint should ensure only approved values are stored where practical.

Do not create a separate:

```text
project_roles
```

table in Version 1.

These four roles are structural workflow concepts rather than user-configurable business data.

---

# 41. Owner Cardinality

A Project may have:

```text
Maximum one Owner
```

Version 1 should enforce this rule.

A Project may temporarily have no Owner during Draft setup if that simplifies creation.

Before active work begins, having an Owner is recommended.

---

# 42. Assignee Cardinality

A Project may have:

```text
Maximum one Assignee
```

Version 1 should enforce this rule.

The Owner and Assignee may be:

```text
Different Employees
```

or:

```text
The same Employee
```

because an Employee may have multiple Project Roles.

---

# 43. Participants

A Project may have:

```text
Zero or many Participants
```

Participants are actively involved in Project work.

---

# 44. Observers

A Project may have:

```text
Zero or many Observers
```

Observers have Project visibility without primary responsibility.

---

# 45. One Owner Constraint

The database/application must prevent two simultaneous Owner assignments for the same Project.

Conceptually:

```text
Project A

Owner:
Employee 1
```

cannot also have:

```text
Owner:
Employee 2
```

at the same time.

---

# 46. One Assignee Constraint

Likewise, only one Assignee may exist per Project.

The implementation may enforce this through:

- Database partial unique index
- Application validation
- Preferably both where practical

---

# 47. Employee May Hold Multiple Roles

The following is valid:

```text
Employee:
Aram

Project Roles:
Owner
Assignee
```

The following is also valid:

```text
Employee:
Designer 1

Project Roles:
Participant
Observer
```

although unnecessary redundant assignments should generally be avoided in the UI.

---

# 48. Active Employee Requirement

New Project Team assignments may use only:

```text
Active Employees
```

The backend must validate Employee active status when creating a new assignment.

---

# 49. Inactive Historical Employee

If an Employee later becomes inactive, existing Project Team assignments remain.

Example:

```text
Project completed in 2026

Owner:
Employee A

Employee A becomes inactive in 2027
```

The historical Project still shows Employee A as Owner.

---

# 50. Removing Project Member

Removing a Project Team assignment deletes only the corresponding:

```text
project_members
```

relationship.

It does not delete:

- Employee
- Project
- User

---

# 51. Changing Owner

Changing Owner should conceptually:

```text
Remove existing Owner assignment
        ↓
Add new Owner assignment
```

This should happen as one safe business operation.

The Project must never accidentally end up with two Owners.

---

# 52. Changing Assignee

Changing Assignee follows the same principle.

The operation should maintain the maximum-one-Assignee rule.

---

# 53. Project Team Inheritance

Version 1 uses Project-level team inheritance.

The Project Team applies logically to all Orders belonging to the Project.

Conceptually:

```text
Project Team
   │
   ├── Order 1
   ├── Order 2
   └── Order 3
```

---

# 54. Inheritance Does Not Copy Data

Project Team inheritance is logical.

Do not copy Project Team rows into every Order.

Incorrect:

```text
Project Team
    ↓ copy
Order Team
    ↓ copy
Order Team
```

Correct:

```text
Order
   ↓
Project
   ↓
Project Team
```

Orders read the Project Team through the Project relationship.

---

# 55. Team Changes Affect Project Orders

Because Version 1 does not duplicate Project Team data, changing the Project Team changes the team considered applicable to all Orders in that Project.

Example:

```text
Project Owner changes
```

then Orders inside that Project use the new Project Owner.

This is intentional Version 1 behavior.

---

# 56. Order-Specific Team

Version 1 does not have:

```text
orders.order_members
```

or separate:

```text
Order Owner

Order Assignee

Order Participants

Order Observers
```

If Lithograph later needs exceptions for individual Orders, Order-specific team overrides may be designed then.

---

# 57. Project Team UI

A Project Workspace should include a Project Team section.

Example:

```text
Project Team

Owner
[ Aram ▼ ]

Assignee
[ Samvel ▼ ]

Participants
[ Designer 1 × ] [ Operator 1 × ] [+ Add]

Observers
[ Employee 4 × ] [+ Add]
```

The exact visual implementation may use Material UI controls.

---

# 58. Employee Selector

Employee selectors should normally show:

```text
Active Employees
```

Useful display:

```text
Full Name — Position
```

Example:

```text
Samvel Petrosyan — Machine Operator
```

---

# 59. Project Creation

Minimum information required to create a Project:

```text
Client

Name
```

Automatically generated:

```text
Business ID
```

Defaults may include:

```text
Status = Draft
```

Optional:

```text
Description

Start Date

Deadline

Project Team
```

---

# 60. Create Project Interface

Example:

```text
New Project

Project ID
Generated automatically

Client
[ CL-000125 — Samsung Armenia ▼ ]

Name
[________________________________]

Description
[________________________________]

Status
[ Draft ▼ ]

Start Date
[__________]

Deadline
[__________]

Project Team

Owner
[ None ▼ ]

Assignee
[ None ▼ ]

Participants
[ + Add ]

Observers
[ + Add ]

[Create] [Cancel]
```

---

# 61. Project Team During Creation

Project Team may be assigned:

```text
During Project creation
```

or:

```text
Immediately afterward
```

Implementation should choose the simpler reliable workflow.

Do not make Project creation unnecessarily difficult.

---

# 62. Project Workspace

The Project Workspace is the primary interface for one Project.

Recommended structure:

```text
Project Header

General Information

Project Team

Orders
```

---

# 63. Project Header

Example:

```text
PRJ-2026-000125

Samsung Armenia
New Store Opening

Status: Active
Deadline: 30 Nov 2026
```

Important information should be visible without opening additional dialogs.

---

# 64. General Information Section

Contains:

```text
Client

Project Name

Description

Status

Start Date

Deadline
```

Business ID is displayed but read-only.

---

# 65. Orders Section

The Project Workspace should show Orders belonging to the Project.

Example:

| Order ID | Order | Type | Status | Selling Price | Cost Price |
|---|---|---|---|---:|---:|
| ORD-2026-000351 | Entrance Panels | UV Printing | Active | ... | ... |
| ORD-2026-000352 | Acrylic Letters | Laser Cutting | Draft | ... | ... |

Exact financial visibility depends on Permissions.

---

# 66. Create Order from Project

The Project Workspace should provide:

```text
Create Order
```

This action automatically sets:

```text
project_id
```

to the current Project.

The user should not need to reselect the Project.

---

# 67. Project Order Aggregation

Project-level financial summaries may be calculated from its Orders.

Examples:

```text
Project Selling Total
=
SUM(Order Selling Prices)
```

```text
Project Cost Total
=
SUM(Order Cost Prices)
```

```text
Project Profit
=
Project Selling Total - Project Cost Total
```

These values should normally be calculated, not stored on the Project.

---

# 68. No Stored Project Profit

Do not create:

```text
projects.projects.profit
```

in Version 1.

Profit is derived from Orders when required.

---

# 69. No Stored Project Selling Total

Do not create:

```text
selling_total
```

on Projects.

Calculate from Orders.

---

# 70. No Stored Project Cost Total

Do not create:

```text
cost_total
```

on Projects.

Calculate from Orders.

---

# 71. Project List

The Projects module should provide a Project list.

Example:

| Project ID | Client | Project | Status | Deadline | Owner |
|---|---|---|---|---|---|
| PRJ-2026-000125 | Samsung Armenia | Store Opening | Active | 30 Nov | Aram |
| PRJ-2026-000126 | ABC | Exhibition | Draft | 15 Dec | Samvel |

---

# 72. Project List Columns

Recommended Version 1 columns:

```text
Business ID

Client

Project Name

Status

Deadline

Owner
```

Optional:

```text
Assignee
```

depending on screen space and workflow usefulness.

---

# 73. Project Search

Search should support:

```text
Business ID

Project Name

Client Name
```

---

# 74. Project Filters

Useful Version 1 filters:

```text
Status

Client

Owner

Assignee

Deadline
```

Do not create an advanced generic query builder.

---

# 75. Default Project List

The normal default view may prioritize unfinished work.

For example:

```text
Draft

Active

On Hold
```

Completed and Cancelled Projects remain searchable and filterable.

Exact default filter behavior may be adjusted during use.

---

# 76. Project Permissions

Initial Projects module Permissions:

```text
projects.view

projects.create

projects.edit

projects.manage_team

projects.change_status
```

---

# 77. projects.view

Allows:

- View Projects
- Search Projects
- Open Project Workspace
- View Project Team
- View related Orders subject to Orders permissions

---

# 78. projects.create

Allows:

```text
Create Project
```

---

# 79. projects.edit

Allows editing:

```text
Client

Name

Description

Start Date

Deadline
```

Changing status may use a separate Permission.

---

# 80. projects.manage_team

Allows:

```text
Set Owner

Set Assignee

Add Participant

Remove Participant

Add Observer

Remove Observer
```

---

# 81. projects.change_status

Allows changes between Project statuses.

This separates Project lifecycle control from general text editing.

---

# 82. No projects.delete Permission

Version 1 does not require:

```text
projects.delete
```

Projects use lifecycle statuses rather than normal deletion.

---

# 83. API Responsibilities

The Projects API may conceptually provide:

```text
Get Projects

Get Project

Create Project

Update Project

Change Project Status

Get Project Team

Set Owner

Set Assignee

Add Participant

Remove Participant

Add Observer

Remove Observer
```

Exact REST routes are determined during implementation.

---

# 84. Project Response DTO

A Project response may contain:

```text
id

business_id

client

name

description

status

start_date

deadline

team

created_at

updated_at
```

Related Client data should contain only what is required by the UI.

---

# 85. Project List DTO

Project lists should use a lighter representation.

Example:

```text
id

business_id

client_name

name

status

deadline

owner_name

assignee_name
```

Do not return full Orders or full Employee records for every row.

---

# 86. Project Team DTO

Conceptually:

```text
owner

assignee

participants[]

observers[]
```

Each Employee representation may contain:

```text
id

full_name

position

is_active
```

---

# 87. Create Project Request

Conceptual fields:

```text
client_id

name

description

status

start_date

deadline

owner_employee_id

assignee_employee_id

participant_employee_ids

observer_employee_ids
```

Business ID is generated by the backend.

---

# 88. Update Project Request

Conceptual normal Project fields:

```text
client_id

name

description

start_date

deadline
```

Status and team management may use separate operations.

This keeps important workflows explicit.

---

# 89. Validation Rules

Backend must validate:

- Client exists
- Client is active for new Project creation
- Name is required
- Name is not empty after trimming
- Status is valid
- Deadline is not earlier than Start Date when both exist
- Team Employees exist
- New Team Employees are active
- Maximum one Owner
- Maximum one Assignee
- Duplicate identical member-role assignment is rejected
- Current User has required Permissions

---

# 90. Project Name Uniqueness

Project names do not need to be unique.

Do not enforce:

```text
UNIQUE(name)
```

Business ID provides unique Project identification.

---

# 91. Required Indexes

Primary key:

```text
PRIMARY KEY (id)
```

Business ID:

```text
UNIQUE INDEX (business_id)
```

Foreign key:

```text
INDEX (client_id)
```

Useful Project list/filter indexes may include:

```text
status

deadline
```

only when query behavior justifies them.

---

# 92. project_members Indexes

Composite primary key:

```text
(project_id, employee_id, project_role)
```

Additional index:

```text
INDEX (employee_id)
```

may support finding Projects for an Employee.

---

# 93. Unique Owner Enforcement

The implementation should enforce one Owner per Project.

A PostgreSQL partial unique index is an acceptable approach conceptually equivalent to:

```text
UNIQUE project_id
WHERE project_role = 'Owner'
```

The exact migration syntax is determined during implementation.

---

# 94. Unique Assignee Enforcement

Likewise:

```text
UNIQUE project_id
WHERE project_role = 'Assignee'
```

may be used.

Application validation should also provide clear user-facing errors.

---

# 95. Audit Fields

Project audit fields:

```text
created_at

created_by

updated_at

updated_by
```

reference Authentication Users where applicable.

Project member assignments use:

```text
assigned_at

assigned_by
```

---

# 96. User vs Employee in Projects

Use Authentication User for:

```text
Who changed the Project?
```

Use Employee for:

```text
Who owns the Project?
```

Example:

```text
updated_by
→ auth.users.id
```

while:

```text
project_members.employee_id
→ employees.employees.id
```

---

# 97. Client Snapshot

Version 1 does not copy Client name or contact information into the Project.

Project stores:

```text
client_id
```

and reads current Client information through the Clients module.

Historical legal snapshots may be designed later if required.

---

# 98. Project Contact Person

Version 1 does not require a Project-specific Contact Person field.

The Client's main Contact Person is available from the Client record.

If Projects commonly require different contacts, this may be added later based on real usage.

---

# 99. Project Address

Version 1 does not require a Project Address field.

If installation/location information becomes important, it can be added based on an actual Project workflow requirement.

Do not add it preemptively.

---

# 100. Project Files

Version 1 does not manage Project-level physical files.

Folder Links are currently defined at Order level.

If Lithograph later needs Project-level folder references, they can be added separately.

---

# 101. Project Checklist

Projects do not contain Checklists in Version 1.

Checklists belong to Orders.

Do not duplicate checklist functionality at Project level.

---

# 102. Project Calculator

Projects do not have Calculator Templates.

Calculators belong to Orders through Order Types.

Project totals aggregate completed Order values.

---

# 103. Project Priority

Version 1 does not require Project Priority.

Priority belongs more naturally to Orders if required there.

Do not add Project Priority unless daily use demonstrates a need.

---

# 104. Project Comments

Version 1 does not include Project comments or messaging.

Do not create a collaboration subsystem before it is needed.

---

# 105. Project Attachments

Version 1 does not include generic Project attachments.

Avoid creating document-management infrastructure prematurely.

---

# 106. Orders After Project Completion

Completing a Project does not automatically rewrite Order statuses in Version 1.

Project and Order statuses are separate.

The application may warn if unfinished Orders exist when completing a Project.

Do not silently change multiple Order statuses unless later explicitly specified.

---

# 107. Completing Project with Open Orders

Recommended UI behavior:

If a User attempts to mark a Project:

```text
Completed
```

while non-completed Orders remain, display a warning.

Example:

```text
This Project still contains unfinished Orders.

Complete the Project anyway?
```

The final status rules may be refined when the Orders status model is defined.

---

# 108. Cancelling Project

Cancelling a Project does not automatically delete or cancel its Orders.

The system may warn the User if active Orders exist.

Avoid hidden cascaded business changes.

---

# 109. Client Change with Existing Orders

Changing a Project Client also changes the logical Client associated with all Orders through that Project.

The UI should therefore clearly warn when changing Client on a Project that already contains Orders.

Example:

```text
This Project contains Orders.

Changing the Client will also change the Client context of those Orders.
```

The Orders themselves remain unchanged because they reference the Project, not the Client directly.

---

# 110. Project Team Change with Existing Orders

Changing Project Team affects the logical team available for all existing Orders in that Project.

This is intentional.

No Order rows are modified.

---

# 111. Project Business ID Display

Project Business ID should be visible in:

```text
Project List

Project Workspace

Order Project reference

Reports

Search results
```

---

# 112. Order Project Display

When an Order shows its Project, useful display may be:

```text
PRJ-2026-000125 — New Store Opening
```

This improves identification.

---

# 113. Client Project Display

When selecting or showing the Client, useful display may be:

```text
CL-000125 — Samsung Armenia
```

---

# 114. Version 1 Non-Goals

Projects Version 1 does not include:

```text
Project Documents

Project Chat

Project Comments

Project Checklist

Project Calculator

Project Billing

Project Invoices

Project Contracts

Project Address Management

Project Contact Management

Project Priority

Project Templates

Order-Specific Team Overrides

Complex Workflow Engine

Gantt Charts

Resource Scheduling
```

These should not be introduced without real requirements.

---

# 115. Future Extensions

Possible future additions may include:

- Project-level folder links
- Installation locations
- Project-specific Client contacts
- Project templates
- Activity history
- Comments
- Scheduling
- More advanced reporting

These future possibilities must not complicate Version 1.

---

# 116. Testing Requirements

Important tests include:

```text
Create Project

Generate yearly Business ID

Project Business ID is unique

Project Business ID cannot be edited

Project requires Client

Cannot create Project for inactive Client

Create Project with minimum fields

Set Owner

Prevent second Owner

Set Assignee

Prevent second Assignee

Same Employee can be Owner and Assignee

Add Participant

Add Observer

Prevent duplicate identical member-role assignment

Inactive Employee cannot receive new assignment

Existing inactive Employee remains visible historically

Change Owner

Remove Participant

Project Team is available to Orders through Project

No Order Team rows are created

Complete Project

Cancel Project

Existing Orders survive Project status changes
```

---

# 117. Version 1 Schema Summary

```text
projects.projects

id
business_id
client_id
name
description
status
start_date
deadline
created_at
created_by
updated_at
updated_by
```

```text
projects.project_members

project_id
employee_id
project_role
assigned_at
assigned_by
```

Two tables are sufficient for Version 1.

---

# 118. Module Simplicity Rule

Before adding another Project field or table, ask:

```text
Is this information required to organize Projects and their Orders today?
```

If not, do not add it yet.

---

# 119. Final Project Principle

The Projects module should answer:

```text
Which Client is this work for?
```

```text
What Orders belong together?
```

and:

```text
Which Employees are responsible for the Project?
```

It should remain the simple organizational bridge between Clients, Employees, and Orders.

---

**End of Document**