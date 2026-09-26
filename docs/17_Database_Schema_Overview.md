# Lithograph ERP

**Document:** 17_Database_Schema_Overview.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `05_Numbering_System.md`
- `08_Auth_Schema_Design.md`
- `09_Auth_Tables.md`
- `11_Employees_Module.md`
- `12_Clients_Module.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `16_Reports_Module.md`

---

# 1. Purpose

This document provides a consolidated overview of the Lithograph ERP Version 1 database.

It combines the database structures defined by individual module specifications into one reference.

Its purpose is to answer:

```text
Which schemas exist?

Which tables exist?

Which module owns each table?

How are tables related?

Which foreign keys cross module boundaries?

What is the overall dependency structure?
```

This document does not replace detailed module specifications.

---

# 2. Database Platform

Version 1 uses:

```text
PostgreSQL
```

The application uses:

```text
Entity Framework Core
```

for normal database access and schema migrations.

---

# 3. Database Strategy

Lithograph ERP Version 1 uses:

```text
One PostgreSQL Database
```

with multiple PostgreSQL schemas organized by module.

This supports the Modular Monolith architecture.

---

# 4. Version 1 Schemas

Approved PostgreSQL schemas:

```text
auth

employees

clients

projects

orders

calculator
```

Reports does not require its own database schema in Version 1.

---

# 5. Complete Version 1 Table List

```text
auth
├── users
├── roles
├── permissions
├── user_roles
├── role_permissions
└── sessions

employees
└── employees

clients
└── clients

projects
├── projects
└── project_members

orders
├── orders
├── order_types
├── checklist_items
└── folder_links

calculator
├── templates
├── template_versions
├── order_calculators
└── cost_items
```

Total Version 1 tables:

```text
18
```

---

# 6. Reports

Reports owns no source-of-truth tables.

Conceptually:

```text
Operational Database
        ↓
      Reports
```

Reports queries existing module data.

Do not create duplicated reporting tables without a demonstrated performance or historical requirement.

---

# 7. High-Level Business Relationship

The primary business hierarchy is:

```text
Client
   ↓
Project
   ↓
Order
```

Additional relationships:

```text
Employee
   ↓
Project Team
   ↓
Project
```

and:

```text
Order
   ↓
Order Type
   ↓
Calculator Template
```

---

# 8. High-Level Database Map

```text
auth.users
     │
     ├──────────────────────────────────┐
     │                                  │
     ▼                                  ▼
employees.employees                Audit Fields
     │                                  │
     │                                  ├── clients
     │                                  ├── projects
     │                                  ├── orders
     │                                  └── calculator
     │
     ▼
projects.project_members
     │
     ▼
projects.projects
     │
     ├──────────────► clients.clients
     │
     ▼
orders.orders
     │
     ├──────────────► orders.order_types
     │                    │
     │                    ▼
     │               calculator.templates
     │                    │
     │                    ▼
     │               template_versions
     │
     ├──► checklist_items
     │
     ├──► folder_links
     │
     ├──► order_calculators
     │
     └──► cost_items
```

---

# 9. Authentication Schema

Schema:

```text
auth
```

Purpose:

```text
Identity

Login

Sessions

Roles

Permissions
```

Tables:

```text
auth.users
auth.roles
auth.permissions
auth.user_roles
auth.role_permissions
auth.sessions
```

---

# 10. auth.users

Primary responsibility:

```text
ERP login accounts
```

Primary key:

```text
id uuid
```

Important fields:

```text
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

# 11. auth.roles

Primary responsibility:

```text
Reusable Authentication Roles
```

Examples:

```text
Director

Designer

Operator
```

Primary key:

```text
id uuid
```

---

# 12. auth.permissions

Primary responsibility:

```text
Application capabilities
```

Examples:

```text
orders.view

projects.create

calculator.edit_costs
```

Primary key:

```text
id uuid
```

Permission Code is unique.

---

# 13. auth.user_roles

Relationship:

```text
User ↔ Role
```

Composite primary key:

```text
(user_id, role_id)
```

No synthetic UUID is required.

---

# 14. auth.role_permissions

Relationship:

```text
Role ↔ Permission
```

Composite primary key:

```text
(role_id, permission_id)
```

No synthetic UUID is required.

---

# 15. auth.sessions

Relationship:

```text
User
  ↓
Sessions
```

Primary key:

```text
id uuid
```

Sessions are technical Authentication records.

They do not require soft delete.

---

# 16. Authentication Relationship Diagram

```text
auth.users
    │
    ├──< auth.user_roles >── auth.roles
    │                            │
    │                            └──< auth.role_permissions >── auth.permissions
    │
    └──< auth.sessions
```

---

# 17. Employees Schema

Schema:

```text
employees
```

Purpose:

```text
Operational Employee directory
```

Table:

```text
employees.employees
```

---

# 18. employees.employees

Primary key:

```text
id uuid
```

Fields:

```text
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

# 19. Employee–User Relationship

Optional relationship:

```text
employees.employees.user_id
    ↓
auth.users.id
```

Cardinality:

```text
Employee
0..1 User
```

and:

```text
User
0..1 Employee
```

`user_id` must be unique when present.

---

# 20. Employee Relationship Principle

Use Employee identity for:

```text
Business responsibility
```

Use User identity for:

```text
Authentication and audit
```

Example:

```text
Project Owner
→ Employee
```

while:

```text
Project created_by
→ User
```

---

# 21. Clients Schema

Schema:

```text
clients
```

Table:

```text
clients.clients
```

Purpose:

```text
Customer master data
```

---

# 22. clients.clients

Primary key:

```text
id uuid
```

Business ID:

```text
CL-000001
```

Fields:

```text
id
business_id
name
contact_person
phone
email
address
notes
is_active
created_at
created_by
updated_at
updated_by
```

---

# 23. Client Business ID

Unique:

```text
business_id
```

Format:

```text
CL-000001
```

Sequence does not reset annually.

---

# 24. Projects Schema

Schema:

```text
projects
```

Tables:

```text
projects.projects

projects.project_members
```

Purpose:

```text
Project organization

Project Team assignments
```

---

# 25. projects.projects

Primary key:

```text
id uuid
```

Business ID:

```text
PRJ-YYYY-000001
```

Fields:

```text
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

---

# 26. Project–Client Relationship

```text
projects.projects.client_id
    ↓
clients.clients.id
```

Cardinality:

```text
Client
1
↓
Many Projects
```

Every Project requires one Client.

---

# 27. projects.project_members

Represents:

```text
Project
+
Employee
+
Project Role
```

Fields:

```text
project_id
employee_id
project_role
assigned_at
assigned_by
```

---

# 28. project_members Primary Key

Composite:

```text
(
    project_id,
    employee_id,
    project_role
)
```

This allows the same Employee to hold more than one Project Role.

---

# 29. Project Roles

Version 1 Project Roles:

```text
Owner

Assignee

Participant

Observer
```

These are not Authentication Roles.

---

# 30. Project Member Relationships

```text
projects.project_members.project_id
    ↓
projects.projects.id
```

```text
projects.project_members.employee_id
    ↓
employees.employees.id
```

```text
projects.project_members.assigned_by
    ↓
auth.users.id
```

---

# 31. Project Owner Constraint

Maximum:

```text
One Owner per Project
```

A partial unique constraint/index should enforce this.

---

# 32. Project Assignee Constraint

Maximum:

```text
One Assignee per Project
```

A partial unique constraint/index should enforce this.

---

# 33. Orders Schema

Schema:

```text
orders
```

Tables:

```text
orders.orders
orders.order_types
orders.checklist_items
orders.folder_links
```

Purpose:

```text
Operational production/service jobs
```

---

# 34. orders.orders

Primary key:

```text
id uuid
```

Business ID:

```text
ORD-YYYY-000001
```

Fields:

```text
id
business_id
project_id
order_type_id
name
description
status
priority
selling_price
cost_price
deadline
preview_image_path
created_at
created_by
updated_at
updated_by
```

---

# 35. Order–Project Relationship

```text
orders.orders.project_id
    ↓
projects.projects.id
```

Every Order requires one Project.

---

# 36. Order Does Not Store Client

There is no:

```text
orders.orders.client_id
```

Client is resolved through:

```text
Order
   ↓
Project
   ↓
Client
```

---

# 37. Order Does Not Store Team

There is no:

```text
orders.order_members
```

Order Team is resolved through:

```text
Order
   ↓
Project
   ↓
Project Team
```

---

# 38. Order Financial Values

Stored:

```text
selling_price

cost_price
```

Calculated:

```text
profit
```

Formula:

```text
profit
=
selling_price - cost_price
```

Profit is not stored.

---

# 39. orders.order_types

Primary key:

```text
id uuid
```

Fields:

```text
id
name
description
calculator_template_id
is_active
created_at
created_by
updated_at
updated_by
```

---

# 40. Order–Order Type Relationship

```text
orders.orders.order_type_id
    ↓
orders.order_types.id
```

One Order Type may be used by many Orders.

---

# 41. Order Type–Calculator Relationship

```text
orders.order_types.calculator_template_id
    ↓
calculator.templates.id
```

This relationship is optional.

An Order Type may temporarily exist without a Calculator Template.

---

# 42. orders.checklist_items

Primary key:

```text
id uuid
```

Fields:

```text
id
order_id
text
is_completed
sort_order
```

Relationship:

```text
orders.checklist_items.order_id
    ↓
orders.orders.id
```

---

# 43. Checklist Simplicity

Checklist Items do not contain:

```text
employee_id
deadline
priority
folder_path
file_name
notes
```

Version 1 Checklist remains intentionally simple.

---

# 44. orders.folder_links

Primary key:

```text
id uuid
```

Fields:

```text
id
order_id
name
path
sort_order
```

Relationship:

```text
orders.folder_links.order_id
    ↓
orders.orders.id
```

---

# 45. Folder Link Principle

Folder Links store references only.

The ERP does not:

```text
Create folders

Delete folders

Open Windows Explorer

Manage physical files
```

in Version 1.

---

# 46. Calculator Schema

Schema:

```text
calculator
```

Tables:

```text
calculator.templates

calculator.template_versions

calculator.order_calculators

calculator.cost_items
```

---

# 47. calculator.templates

Primary key:

```text
id uuid
```

Fields:

```text
id
name
description
is_active
created_at
created_by
updated_at
updated_by
```

Represents the stable identity of a Calculator Template.

---

# 48. calculator.template_versions

Primary key:

```text
id uuid
```

Fields:

```text
id
template_id
version_number
status
definition
created_at
created_by
published_at
published_by
```

`definition` uses:

```text
jsonb
```

---

# 49. Template–Version Relationship

```text
calculator.template_versions.template_id
    ↓
calculator.templates.id
```

One Template may contain many Versions.

---

# 50. Template Version Constraint

Unique:

```text
(
    template_id,
    version_number
)
```

---

# 51. Template Version Status

Allowed Version 1 values:

```text
Draft

Published

Retired
```

Published Versions are immutable.

---

# 52. calculator.order_calculators

Primary key:

```text
id uuid
```

Fields:

```text
id
order_id
template_version_id
field_values
last_calculated_at
created_at
created_by
updated_at
updated_by
```

`field_values` uses:

```text
jsonb
```

---

# 53. Order Calculator Relationship

```text
calculator.order_calculators.order_id
    ↓
orders.orders.id
```

Version 1 allows maximum:

```text
One active Calculator per Order
```

Therefore:

```text
UNIQUE (order_id)
```

---

# 54. Historical Template Relationship

```text
calculator.order_calculators.template_version_id
    ↓
calculator.template_versions.id
```

This relationship permanently identifies the Calculator Version used by an Order.

---

# 55. Historical Calculator Principle

Existing Order Calculators must not automatically move to newer Template Versions.

Example:

```text
Order
→ Template Version 3
```

remains:

```text
Template Version 3
```

even if Version 4 is later published.

---

# 56. calculator.cost_items

Primary key:

```text
id uuid
```

Fields:

```text
id
order_id
category
supplier
expense_date
description
amount
sort_order
created_at
created_by
updated_at
updated_by
```

---

# 57. Cost Item Relationship

```text
calculator.cost_items.order_id
    ↓
orders.orders.id
```

One Order may contain many Cost Items.

---

# 58. Cost Price Relationship

Order Cost Price is synchronized from:

```text
SUM(calculator.cost_items.amount)
```

into:

```text
orders.orders.cost_price
```

---

# 59. Why Cost Price Is Stored

Cost Price is stored because it is an important historical business value.

Cost Items remain the detailed source of Order expenses.

---

# 60. Selling Price Relationship

Calculator evaluates its designated Selling Price output.

The final result is synchronized into:

```text
orders.orders.selling_price
```

---

# 61. Complete Business Data Flow

```text
Client
   ↓
Project
   ↓
Order
   ↓
Order Type
   ↓
Calculator Template
   ↓
Template Version
   ↓
Order Calculator
```

and:

```text
Order
   ↓
Cost Items
   ↓
Cost Price
```

and:

```text
Order Calculator
   ↓
Selling Price
```

---

# 62. Complete Project Responsibility Flow

```text
Employee
   ↓
Project Member
   ↓
Project
   ↓
Orders
```

Orders inherit responsibility logically through Project.

No Team data is duplicated on Orders.

---

# 63. Authentication Audit Flow

Authenticated actions may record:

```text
auth.users.id
```

in fields such as:

```text
created_by

updated_by

assigned_by

published_by
```

These are audit relationships.

They do not represent business responsibility.

---

# 64. Cross-Schema Foreign Keys

Version 1 explicitly allows cross-schema foreign keys.

Important examples:

```text
employees.employees.user_id
→ auth.users.id
```

```text
projects.projects.client_id
→ clients.clients.id
```

```text
projects.project_members.employee_id
→ employees.employees.id
```

```text
orders.orders.project_id
→ projects.projects.id
```

```text
orders.order_types.calculator_template_id
→ calculator.templates.id
```

```text
calculator.order_calculators.order_id
→ orders.orders.id
```

---

# 65. Cross-Schema Foreign Keys Are Acceptable

Lithograph ERP uses:

```text
One PostgreSQL Database
```

Therefore database referential integrity may cross schema boundaries.

This is acceptable for the Version 1 Modular Monolith.

---

# 66. Database Relationship vs Module Dependency

A database foreign key does not mean all application modules may freely modify each other's data.

Example:

```text
orders.order_types
→ calculator.templates
```

does not mean Orders owns Calculator Templates.

Calculator still owns Template behavior.

---

# 67. Module Ownership Rule

Each module owns modification of its own tables.

Conceptually:

```text
Authentication
→ auth.*

Employees
→ employees.*

Clients
→ clients.*

Projects
→ projects.*

Orders
→ orders.*

Calculator
→ calculator.*
```

---

# 68. Application Boundaries

Modules should interact through clearly defined application interfaces/services where practical.

Avoid application code that reaches into another module and directly modifies unrelated entities.

---

# 69. Cross-Module Read Operations

Cross-module reads are normal in a Modular Monolith.

Example:

Order Workspace may require:

```text
Order

Project

Client

Project Team

Calculator
```

The application may compose these values efficiently while retaining clear module ownership.

---

# 70. Avoid Circular Application Dependencies

The database contains legitimate cross-schema relationships in both directions between Orders and Calculator:

```text
Order Type
→ Calculator Template
```

and:

```text
Order Calculator
→ Order
```

This does not justify uncontrolled circular code dependencies.

Application interfaces should keep responsibilities clear.

---

# 71. Orders–Calculator Boundary

Orders owns:

```text
Order

Order Type

Selling Price

Cost Price
```

Calculator owns:

```text
Templates

Template Versions

Order Calculator Values

Cost Items

Calculation Logic
```

Calculator may update the Order's final financial values through an approved Orders application interface or coordinated transaction.

---

# 72. Transaction Boundary

Operations that must remain consistent should use database transactions.

Important example:

```text
Update Cost Item
       ↓
Recalculate Cost Total
       ↓
Update Order Cost Price
       ↓
Commit
```

These changes should succeed or fail together.

---

# 73. Selling Price Transaction

Likewise:

```text
Save Calculator Values
       ↓
Calculate Selling Price
       ↓
Update Order Selling Price
       ↓
Commit
```

should be handled safely.

---

# 74. Delete Philosophy

Deletion behavior depends on record type.

Do not apply one global delete rule to every table.

---

# 75. Long-Lived Business Records

Normal UI should avoid destructive deletion of:

```text
Users

Employees

Clients

Projects

Orders

Order Types

Calculator Templates

Published Template Versions
```

Use:

```text
Deactivation

Status changes

Retirement
```

as appropriate.

---

# 76. Simple Child Records

Physical deletion is acceptable for simple sub-records such as:

```text
Checklist Items

Folder Links

Cost Items

Role assignments

Permission assignments

Sessions
```

subject to appropriate authorization.

---

# 77. Cascade Delete Principle

Do not configure broad cascade deletes that could remove important business history accidentally.

Cascade behavior must be deliberate.

---

# 78. Safe Parent–Child Cascades

If an Order is physically removed only through exceptional development/maintenance operations, its simple dependent records may reasonably be deleted:

```text
Checklist Items

Folder Links
```

Order Calculator and Cost Item handling should remain deliberate because they contain historical calculation/financial information.

---

# 79. Client Deletion

A Client referenced by Projects must not be physically deleted during normal operation.

Use:

```text
is_active = false
```

---

# 80. Employee Deletion

An Employee referenced by Project history must not be physically deleted during normal operation.

Use:

```text
is_active = false
```

---

# 81. Project Deletion

Projects are normally retained.

Use lifecycle status:

```text
Cancelled
```

instead of physical deletion.

---

# 82. Order Deletion

Orders are normally retained.

Use:

```text
Cancelled
```

instead of physical deletion.

---

# 83. Order Type Deletion

An Order Type referenced by Orders must remain.

Use:

```text
is_active = false
```

---

# 84. Template Deletion

A Calculator Template referenced by Order Types or historical Versions must remain.

Use:

```text
is_active = false
```

and Version retirement.

---

# 85. Published Template Version Deletion

Published Template Versions used by Orders must never be physically deleted.

They are required for historical calculation reproducibility.

---

# 86. UUID Strategy

Normal entity tables use:

```text
uuid
```

primary keys.

Examples:

```text
auth.users.id

employees.employees.id

clients.clients.id

projects.projects.id

orders.orders.id

calculator.templates.id
```

---

# 87. Composite Primary Keys

Pure junction tables use composite primary keys where appropriate.

Examples:

```text
auth.user_roles
(user_id, role_id)
```

```text
auth.role_permissions
(role_id, permission_id)
```

```text
projects.project_members
(project_id, employee_id, project_role)
```

---

# 88. Business IDs

Business IDs exist only where they provide real human value.

Version 1:

```text
Client
→ CL-000001

Project
→ PRJ-2026-000001

Order
→ ORD-2026-000001
```

---

# 89. Entities Without Business IDs

Version 1 does not assign Business IDs to:

```text
Users

Employees

Roles

Permissions

Order Types

Checklist Items

Folder Links

Calculator Templates

Template Versions

Cost Items
```

UUIDs and names/context are sufficient.

---

# 90. Business ID vs UUID

Example:

```text
Order UUID
=
8c6a...

Order Business ID
=
ORD-2026-000425
```

UUID:

```text
Internal database identity
```

Business ID:

```text
Human-facing identity
```

---

# 91. Timestamp Strategy

Timestamp fields use:

```text
timestamptz
```

Examples:

```text
created_at

updated_at

assigned_at

published_at

last_login_at
```

Application logic uses UTC internally.

---

# 92. Date-Only Strategy

Use PostgreSQL:

```text
date
```

where time-of-day has no business meaning.

Examples:

```text
projects.projects.start_date

projects.projects.deadline

orders.orders.deadline

calculator.cost_items.expense_date
```

---

# 93. Money Strategy

Money values use:

```text
numeric(18,2)
```

Initial examples:

```text
orders.orders.selling_price

orders.orders.cost_price

calculator.cost_items.amount
```

Do not use floating-point types.

---

# 94. Status Strategy

Small stable workflow statuses may be represented through strongly typed application values and constrained database strings.

Examples:

```text
Project Status

Order Status

Priority

Template Version Status

Project Role
```

---

# 95. Configurable Business Data

Changeable business categories belong in tables.

Important example:

```text
orders.order_types
```

Do not hardcode Order Types in source code.

---

# 96. Structural Values

Stable structural concepts do not need configuration tables in Version 1.

Examples:

```text
Project Roles

Order Status

Project Status

Priority

Template Version Status
```

---

# 97. JSONB Usage

Version 1 intentionally uses JSONB only where structure is genuinely flexible.

Approved uses:

```text
calculator.template_versions.definition

calculator.order_calculators.field_values
```

---

# 98. JSONB Non-Uses

Do not use JSONB to store normal relational structures such as:

```text
Project Team

Client relationships

Role assignments

Order relationships

Cost Items
```

---

# 99. Arrays of IDs

Do not store relationships as:

```text
employee_ids = [ ... ]
```

inside JSON or comma-separated strings.

Use relational tables.

---

# 100. Main Foreign Key Map

| From | To | Purpose |
|---|---|---|
| `auth.user_roles.user_id` | `auth.users.id` | User Role assignment |
| `auth.user_roles.role_id` | `auth.roles.id` | User Role assignment |
| `auth.role_permissions.role_id` | `auth.roles.id` | Role Permission assignment |
| `auth.role_permissions.permission_id` | `auth.permissions.id` | Role Permission assignment |
| `auth.sessions.user_id` | `auth.users.id` | Session owner |
| `employees.employees.user_id` | `auth.users.id` | Optional ERP login |
| `clients.clients.created_by` | `auth.users.id` | Audit |
| `clients.clients.updated_by` | `auth.users.id` | Audit |
| `projects.projects.client_id` | `clients.clients.id` | Project Client |
| `projects.project_members.project_id` | `projects.projects.id` | Team Project |
| `projects.project_members.employee_id` | `employees.employees.id` | Team Employee |
| `projects.project_members.assigned_by` | `auth.users.id` | Audit |
| `orders.orders.project_id` | `projects.projects.id` | Parent Project |
| `orders.orders.order_type_id` | `orders.order_types.id` | Work category |
| `orders.order_types.calculator_template_id` | `calculator.templates.id` | Calculator assignment |
| `orders.checklist_items.order_id` | `orders.orders.id` | Checklist parent |
| `orders.folder_links.order_id` | `orders.orders.id` | Folder Link parent |
| `calculator.template_versions.template_id` | `calculator.templates.id` | Template history |
| `calculator.order_calculators.order_id` | `orders.orders.id` | Order Calculator |
| `calculator.order_calculators.template_version_id` | `calculator.template_versions.id` | Historical Version |
| `calculator.cost_items.order_id` | `orders.orders.id` | Order expense |

Audit FKs such as `created_by`, `updated_by`, and `published_by` also reference `auth.users.id` where defined.

---

# 101. Main Cardinalities

```text
Client
1 ────────< many Projects
```

```text
Project
1 ────────< many Orders
```

```text
Project
1 ────────< many Project Member rows
```

```text
Employee
1 ────────< many Project Member rows
```

```text
Order Type
1 ────────< many Orders
```

```text
Calculator Template
1 ────────< many Template Versions
```

```text
Calculator Template
1 ────────< many Order Types
```

```text
Order
1 ──────── 0..1 Order Calculator
```

```text
Order
1 ────────< many Checklist Items
```

```text
Order
1 ────────< many Folder Links
```

```text
Order
1 ────────< many Cost Items
```

---

# 102. Consolidated Logical ER Diagram

```text
┌────────────────────┐
│    auth.users      │
└─────────┬──────────┘
          │ optional 1:1
          ▼
┌────────────────────┐
│employees.employees │
└─────────┬──────────┘
          │
          │
          ▼
┌──────────────────────────┐
│projects.project_members  │
└────────────┬─────────────┘
             │
             ▼
       ┌───────────────┐
       │projects       │
       │.projects      │
       └──────┬────────┘
              │
              │ client_id
              ├──────────────────────►┌────────────────┐
              │                       │clients.clients │
              │                       └────────────────┘
              │
              │ 1:M
              ▼
        ┌───────────────┐
        │orders.orders  │
        └─────┬───┬─────┘
              │   │
              │   └─────────────► orders.order_types
              │                         │
              │                         │ calculator_template_id
              │                         ▼
              │                  calculator.templates
              │                         │
              │                         ▼
              │                  template_versions
              │
              ├────────► checklist_items
              │
              ├────────► folder_links
              │
              ├────────► order_calculators
              │              │
              │              └──► template_versions
              │
              └────────► cost_items
```

---

# 103. Required Unique Constraints

At minimum:

```text
auth.users.normalized_username

auth.roles.normalized_name

auth.permissions.code

employees.employees.user_id
    when not null

clients.clients.business_id

projects.projects.business_id

orders.orders.business_id

order type normalized name

calculator template normalized name

(template_id, version_number)

calculator.order_calculators.order_id
```

---

# 104. Junction Uniqueness

Guaranteed through composite primary keys:

```text
auth.user_roles
(user_id, role_id)
```

```text
auth.role_permissions
(role_id, permission_id)
```

```text
projects.project_members
(project_id, employee_id, project_role)
```

---

# 105. Special Project Team Constraints

Additionally enforce:

```text
Maximum one Owner per Project
```

and:

```text
Maximum one Assignee per Project
```

Partial unique indexes are appropriate.

---

# 106. Core Index Strategy

Index:

```text
Primary Keys

Unique Business IDs

Important Foreign Keys

Common search/filter fields
```

Do not create indexes mechanically on every column.

---

# 107. Important Foreign Key Indexes

Important examples:

```text
projects.projects.client_id

projects.project_members.employee_id

orders.orders.project_id

orders.orders.order_type_id

orders.checklist_items.order_id

orders.folder_links.order_id

calculator.template_versions.template_id

calculator.order_calculators.template_version_id

calculator.cost_items.order_id
```

---

# 108. Search Index Priorities

Common lookup/search fields include:

```text
clients.clients.business_id

projects.projects.business_id

orders.orders.business_id

auth.users.normalized_username
```

Additional name-search indexes may be added based on actual use.

---

# 109. Migration Ownership

All schema changes are performed through:

```text
Entity Framework Core Migrations
```

Do not manually modify production schema and then attempt to reconstruct migrations afterward.

---

# 110. Applied Migration History

Once a migration has been applied to shared/production environments:

```text
Do not rewrite its history.
```

Create a new migration for later changes.

---

# 111. Initial Migration Strategy

Implementation may use either:

```text
One initial migration for the approved V1 baseline
```

or a small ordered set of module migrations.

The approach should remain understandable and deterministic.

---

# 112. Recommended Migration Order

Because of foreign key dependencies, a sensible conceptual order is:

```text
1. Authentication

2. Employees

3. Clients

4. Calculator base tables
   - templates
   - template_versions

5. Projects

6. Orders
   - including order_types

7. Calculator Order tables
   - order_calculators
   - cost_items
```

Exact EF Core migration organization may vary.

---

# 113. Why Calculator Appears Twice in Dependency Order

Order Types reference:

```text
calculator.templates
```

while:

```text
calculator.order_calculators
```

references Orders.

Therefore migration ordering must account for both sides.

This is a database dependency issue, not a reason to split the system into microservices.

---

# 114. Alternative Migration Technique

Foreign keys that create ordering difficulty may be added in a later migration after both tables exist.

Example:

```text
Create Orders tables
Create Calculator tables
Add cross-module FK
```

Prefer a clear migration over forcing awkward table ownership.

---

# 115. Seed Data

Only system-required data should be seeded automatically.

Initial required system data includes:

```text
Director Role

Authentication Permissions
```

and first-run setup creates:

```text
Director User
```

---

# 116. Business Configuration Is Not Seed Data by Default

Do not hardcode Lithograph-specific Order Types or Calculator Templates into migrations unless intentionally approved.

Examples such as:

```text
UV Printing

Laser Cutting
```

are configurable business records.

---

# 117. Bootstrap

First application startup with no Users:

```text
No Users
    ↓
Initial Setup
    ↓
Create Director User
    ↓
Create/Assign Director Role
```

This must work without an existing `created_by` User.

---

# 118. Audit References

Audit fields may be nullable where bootstrap or system operations require it.

Do not create impossible initialization dependencies.

---

# 119. Reports Query Path

Example Client financial report:

```text
clients.clients
      ↓
projects.projects
      ↓
orders.orders
```

---

# 120. Project Profit Query Path

```text
projects.projects
      ↓
orders.orders
      ↓
SUM(selling_price)
SUM(cost_price)
```

Profit:

```text
SUM(selling_price) - SUM(cost_price)
```

---

# 121. Employee Responsibility Query Path

Orders by Project Owner:

```text
orders.orders
      ↓
projects.projects
      ↓
projects.project_members
      ↓
employees.employees
```

where:

```text
project_role = Owner
```

---

# 122. Order Calculator Query Path

```text
orders.orders
      ↓
calculator.order_calculators
      ↓
calculator.template_versions
      ↓
calculator.templates
```

---

# 123. Order Cost Query Path

```text
orders.orders
      ↓
calculator.cost_items
```

Cost total:

```text
SUM(amount)
```

should match:

```text
orders.orders.cost_price
```

---

# 124. Data Consistency Invariant

For every Order:

```text
orders.orders.cost_price
=
SUM(calculator.cost_items.amount)
```

subject to transactional synchronization.

If no Cost Items exist:

```text
cost_price = 0
```

---

# 125. Selling Price Invariant

After successful Calculator save/calculation:

```text
orders.orders.selling_price
```

must equal the authoritative Selling Price result produced by that Order Calculator.

---

# 126. Project Team Invariant

Orders must not contain duplicated Project Team assignments.

Project responsibility always resolves through:

```text
Order.project_id
→ Project
→ Project Members
```

in Version 1.

---

# 127. Business ID Invariant

Business IDs:

```text
Are generated by backend

Are unique

Are immutable

Are never reused
```

---

# 128. Published Template Invariant

A Published Calculator Template Version must be immutable.

Existing Order Calculators remain linked to their exact historical Version.

---

# 129. Active-State Invariant

Inactive master records remain historically valid.

Examples:

```text
Inactive Client
→ Existing Projects remain valid

Inactive Employee
→ Existing Project assignments remain valid

Inactive Order Type
→ Existing Orders remain valid

Inactive Calculator Template
→ Existing Calculators remain valid
```

---

# 130. Database Non-Goals

Version 1 does not require:

```text
Separate database per module

Microservice databases

Event Store

Data Warehouse

Reporting Database

Redis

Elasticsearch

MongoDB

Graph Database

Distributed Transactions

Event Sourcing

CQRS Database Infrastructure
```

PostgreSQL is sufficient.

---

# 131. Version 1 Schema Summary

```text
Lithograph ERP Database

auth
├── users
├── roles
├── permissions
├── user_roles
├── role_permissions
└── sessions

employees
└── employees

clients
└── clients

projects
├── projects
└── project_members

orders
├── orders
├── order_types
├── checklist_items
└── folder_links

calculator
├── templates
├── template_versions
├── order_calculators
└── cost_items
```

---

# 132. Table Count Summary

| Schema | Tables |
|---|---:|
| `auth` | 6 |
| `employees` | 1 |
| `clients` | 1 |
| `projects` | 2 |
| `orders` | 4 |
| `calculator` | 4 |
| **Total** | **18** |

---

# 133. Core Business Tables

The most important operational tables are:

```text
clients.clients

projects.projects

projects.project_members

orders.orders

orders.order_types

calculator.order_calculators

calculator.cost_items
```

---

# 134. Supporting Tables

Supporting tables include:

```text
Authentication tables

Checklist Items

Folder Links

Calculator Templates

Template Versions
```

---

# 135. Database Design Completion Check

Before implementation begins, verify:

```text
All entities have clear ownership

All required relationships are defined

All primary keys are defined

All important foreign keys are defined

Business IDs are defined

Status models are defined

Delete behavior is understood

Historical Calculator behavior is defined

User vs Employee is separated

Project Team inheritance is defined

Profit remains calculated

Reports remain read-only
```

---

# 136. Implementation Principle

Do not begin by generating controllers and UI pages independently.

Implementation should proceed from the approved model:

```text
Documentation
     ↓
Database Entities
     ↓
EF Core Configuration
     ↓
Migrations
     ↓
Backend Business Logic
     ↓
REST API
     ↓
Frontend
     ↓
Tests
```

---

# 137. Schema Change Principle

Once implementation begins, if the database design needs to change:

```text
1. Identify the business reason.

2. Update the relevant module document.

3. Update this schema overview when necessary.

4. Create a new migration.

5. Update backend logic.

6. Update frontend/API contracts if affected.

7. Update tests.
```

Do not silently let code and documentation diverge.

---

# 138. Final Database Principle

Lithograph ERP Version 1 should have a database that is:

```text
Simple

Relational

Understandable

Historically safe

Modular

Extensible without premature complexity
```

The database should model Lithograph's real workflow:

```text
Client
   ↓
Project
   ↓
Order
```

supported by:

```text
Employees
Authentication
Calculator
Reports
```

The database is the structural foundation of the ERP.

Keep that foundation clear before adding more features.

---

**End of Document**