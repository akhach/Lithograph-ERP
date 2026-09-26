# Lithograph ERP

**Document:** 14_Orders_Module.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Orders

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
- `13_Projects_Module.md`

---

# 1. Purpose

The Orders module manages individual production and service jobs performed by Lithograph.

An Order belongs to one Project.

An Order represents one actual unit of work such as:

```text
UV Printing

CO₂ Laser Cutting

Graphic Design

Installation

Outsourced Work
```

Orders are the central operational objects of Lithograph ERP.

---

# 2. Core Hierarchy

```text
Client
   ↓
Project
   ↓
Order
```

A Project may contain one or many Orders.

Every Order belongs to exactly one Project.

---

# 3. Main Responsibilities

The Orders module is responsible for:

```text
Orders

Order Types

Order Status

Order Priority

Selling Price

Cost Price

Checklist

Folder Links

Preview Image Reference
```

The Orders module does not own detailed Calculator Template logic.

That belongs to the Calculator module.

---

# 4. Module Ownership

The Orders module owns PostgreSQL schema:

```text
orders
```

Version 1 tables:

```text
orders.orders

orders.order_types

orders.checklist_items

orders.folder_links
```

No Order Team table exists in Version 1.

---

# 5. Database Structure

```text
orders
│
├── orders
├── order_types
├── checklist_items
└── folder_links
```

Conceptual relationships:

```text
Project
   ↓
Order
   ├── Order Type
   ├── Checklist Items
   ├── Folder Links
   └── Calculator
```

---

# 6. Table: orders.orders

## Purpose

Stores individual Lithograph production or service Orders.

Each row represents one Order.

---

# 7. Order Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `business_id` | `varchar(30)` | No | Human-readable Order ID |
| `project_id` | `uuid` | No | Parent Project |
| `order_type_id` | `uuid` | No | Order Type |
| `name` | `varchar(250)` | No | Order name |
| `description` | `text` | Yes | Order description |
| `status` | `varchar(30)` | No | Current Order status |
| `priority` | `varchar(20)` | No | Order priority |
| `selling_price` | `numeric(18,2)` | No | Final Selling Price |
| `cost_price` | `numeric(18,2)` | No | Final Cost Price |
| `deadline` | `date` | Yes | Order deadline |
| `preview_image_path` | `text` | Yes | Optional preview image reference |
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

# 9. Order Business ID

Every Order receives a Business ID.

Format:

```text
ORD-YYYY-000001
```

Example:

```text
ORD-2026-000425
```

Numbering resets each calendar year.

---

# 10. Business ID Generation

The backend generates the Business ID automatically.

The frontend does not calculate or edit it.

Example:

```text
Create Order
    ↓
Backend generates Business ID
    ↓
ORD-2026-000426
    ↓
Order stored
```

---

# 11. Business ID Rules

Order Business ID:

- Is required
- Is unique
- Is automatically generated
- Is read-only
- Is never reused
- Never changes after creation

Changing Project, Order Type, status, price, or deadline does not change the Business ID.

---

# 12. Business ID Constraint

Database must enforce:

```text
UNIQUE (business_id)
```

Business ID must be indexed for fast search.

---

# 13. Project Relationship

Every Order belongs to exactly one Project.

Database field:

```text
project_id
```

references:

```text
projects.projects.id
```

---

# 14. Project Is Required

```text
project_id NOT NULL
```

An Order cannot exist outside a Project in Version 1.

---

# 15. Client Relationship

Orders do not store:

```text
client_id
```

Client is determined through:

```text
Order
   ↓
Project
   ↓
Client
```

This prevents unnecessary duplicated data.

---

# 16. Project Team

Orders do not store separate Employee assignments in Version 1.

The Order uses the Project Team logically through its Project.

Conceptually:

```text
Order
   ↓
Project
   ↓
Project Team
```

---

# 17. No Order Team Table

Do not create:

```text
orders.order_members
```

Version 1 does not contain separate:

```text
Order Owner

Order Assignee

Order Participants

Order Observers
```

Project Team applies to all Orders in the Project.

---

# 18. Order Name

`name` is required.

Examples:

```text
Entrance UV Panels

Main Acrylic Logo

Window Stickers

Graphic Design

Installation
```

Order names do not need to be unique.

Business ID provides unique identification.

---

# 19. Description

`description` is optional.

It may contain useful Order-level information.

Example:

```text
Print 24 panels on 5 mm PVC and cut to final dimensions.
```

Do not use Description as a replacement for structured Calculator information.

---

# 20. Order Type

Every Order has exactly one Order Type.

Order Type describes what kind of production or service work is being performed.

Examples:

```text
UV Printing

CO₂ Laser Cutting

Flatbed Cutting

Graphic Design

Installation

Outsourced Work
```

---

# 21. Order Types Are Configurable

Order Types are database data.

They must not be hardcoded into C# or React.

Authorized Users can create and edit Order Types.

---

# 22. Table: orders.order_types

## Purpose

Stores configurable Order Types.

Each row describes one category of Lithograph work.

---

# 23. order_types Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `name` | `varchar(150)` | No | Order Type name |
| `description` | `text` | Yes | Explanation |
| `calculator_template_id` | `uuid` | Yes | Assigned Calculator Template |
| `is_active` | `boolean` | No | Available for new Orders |
| `created_at` | `timestamptz` | No | Creation time |
| `created_by` | `uuid` | Yes | Creating User |
| `updated_at` | `timestamptz` | Yes | Last update |
| `updated_by` | `uuid` | Yes | Last modifying User |

---

# 24. Order Type Primary Key

```text
PRIMARY KEY (id)
```

---

# 25. Order Type Name

Order Type name is required.

Examples:

```text
UV Printing

Laser Cutting

Design

Installation
```

---

# 26. Order Type Name Uniqueness

Order Type names should be unique case-insensitively.

The system should prevent confusing duplicates such as:

```text
UV Printing

uv printing

UV PRINTING
```

The exact normalization implementation may follow the same general strategy used elsewhere.

---

# 27. Order Type Business ID

Order Types do not require Business IDs.

Their UUID and Name are sufficient.

Do not create:

```text
OT-000001
```

unless a future requirement demonstrates value.

---

# 28. Order Type Active Status

Active Order Type:

```text
is_active = true
```

may be selected for new Orders.

Inactive Order Type:

```text
is_active = false
```

remains available for historical Orders.

---

# 29. Order Type Deactivation

Deactivating an Order Type must not affect existing Orders.

Historical Orders continue referencing the same Order Type.

---

# 30. Calculator Template Assignment

An Order Type may reference one Calculator Template.

Conceptually:

```text
Order Type
    ↓
Calculator Template
```

Database field:

```text
calculator_template_id
```

references the Calculator module.

The field is nullable because an Order Type may temporarily exist without a Calculator Template.

---

# 31. Calculator Ownership

Calculator Templates belong to the Calculator module.

The Orders module stores only the assignment reference from Order Type to Template.

The detailed Template structure is not stored in Orders tables.

---

# 32. Order Calculator Selection

When an Order is created:

```text
Order
   ↓
Order Type
   ↓
Assigned Calculator Template
```

The appropriate Calculator page is loaded.

Detailed historical Calculator behavior is defined by the Calculator module.

---

# 33. Order Type Is Not Limited to Machines

Order Types may represent work performed by:

```text
Machine

Designer

Installer

External Supplier
```

Examples:

```text
UV Printing
→ Machine-based

Graphic Design
→ Employee-based

Installation
→ Service-based

Outsourced Work
→ External
```

Do not require every Order Type to reference a Machine.

---

# 34. Machines Module

Version 1 does not have a dedicated Machines module.

Do not add:

```text
machine_id
```

to Order Type unless a later requirement introduces machine management.

The Order Type name is sufficient for Version 1.

---

# 35. Order Status

Version 1 Order statuses are:

```text
Draft

Active

On Hold

Completed

Cancelled
```

---

# 36. Draft Order

```text
Draft
```

means the Order is being prepared and has not yet entered active work.

---

# 37. Active Order

```text
Active
```

means the Order is currently being worked on.

---

# 38. On Hold Order

```text
On Hold
```

means work is temporarily paused.

---

# 39. Completed Order

```text
Completed
```

means the Order work is finished.

The Order remains fully available for historical reporting.

---

# 40. Cancelled Order

```text
Cancelled
```

means the Order will not continue.

Cancelled Orders are not deleted.

---

# 41. Order Deletion

Version 1 does not require normal permanent Order deletion.

Use:

```text
Cancelled
```

instead.

This preserves historical data.

---

# 42. No is_active on Orders

Orders do not require:

```text
is_active
```

because lifecycle is represented by:

```text
status
```

---

# 43. No is_deleted on Orders

Version 1 does not require:

```text
is_deleted
```

for Orders.

Status provides the normal lifecycle.

---

# 44. Order Priority

Version 1 priorities are:

```text
Low

Normal

High

Urgent
```

---

# 45. Default Priority

New Orders default to:

```text
Normal
```

---

# 46. Priority Purpose

Priority helps employees understand which Orders need attention first.

Priority does not automatically change:

- Status
- Deadline
- Project
- Project Team

---

# 47. Priority Storage

Priority is structural workflow data.

Use a strongly typed C# representation.

Database stores a stable value.

Do not create a configurable Priority management system in Version 1.

---

# 48. Selling Price

Every Order stores:

```text
selling_price
```

Selling Price is the final amount charged to the Client for this Order.

It is normally calculated using the Order Calculator.

---

# 49. Cost Price

Every Order stores:

```text
cost_price
```

Cost Price is the final internal cost of the Order.

It is calculated from the Order Calculator's cost data.

---

# 50. Price Defaults

When an Order is first created before calculation:

```text
selling_price = 0

cost_price = 0
```

is acceptable.

These values are later updated from the Calculator.

---

# 51. Money Type

Selling Price and Cost Price use exact decimal storage.

```text
numeric(18,2)
```

Do not use floating-point types for money.

---

# 52. Negative Prices

Version 1 should normally reject negative:

```text
selling_price

cost_price
```

unless a future credit/refund workflow explicitly requires them.

---

# 53. Profit

Profit is calculated:

```text
profit = selling_price - cost_price
```

Do not store:

```text
profit
```

on the Order.

---

# 54. Historical Price Values

Selling Price and Cost Price are stored historical values.

Changing a Calculator Template later must not silently change the stored final values of old Orders.

---

# 55. Cost Details

Detailed Order Cost data belongs to the Calculator module.

The Orders table stores only:

```text
cost_price
```

as the final calculated result.

Do not create separate Orders-module cost-line tables before the Calculator design is defined.

---

# 56. Deadline

`deadline` is optional.

It stores the expected Order completion date.

Use PostgreSQL:

```text
date
```

Time-of-day is not required in Version 1.

---

# 57. Project Deadline vs Order Deadline

Project and Order deadlines are independent.

Example:

```text
Project Deadline:
30 November

Order 1 Deadline:
15 November

Order 2 Deadline:
25 November
```

An Order may have its own deadline.

---

# 58. Order Deadline Validation

Version 1 may warn if an Order deadline is later than the Project deadline.

It should not automatically rewrite either date.

The final UI should prefer a warning over hidden changes.

---

# 59. Preview Image

An Order may have one optional Preview Image.

Purpose:

- Quickly identify a job visually
- Support future Gallery view
- Help employees recognize recent work

Database field:

```text
preview_image_path
```

---

# 60. Preview Image Storage

Do not store image binary data directly in:

```text
orders.orders
```

The database stores only a reference/path.

The exact file-storage implementation should remain simple and may be finalized during implementation.

---

# 61. Preview Image Is Optional

Orders do not require a Preview Image.

An Order without an image must work normally.

---

# 62. Gallery View

Version 1 may provide a simple Gallery view of Orders that have Preview Images.

Possible display:

```text
Preview Image

Order ID

Order Name

Client

Project

Order Type
```

Gallery is an alternative view of Orders, not a separate module.

---

# 63. Checklist

Every Order may have a manually created Checklist.

Checklist is intentionally simple.

There is no automatic generation.

---

# 64. Table: orders.checklist_items

## Purpose

Stores simple Checklist rows belonging to Orders.

---

# 65. checklist_items Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `order_id` | `uuid` | No | Parent Order |
| `text` | `text` | No | Free-form checklist text |
| `is_completed` | `boolean` | No | Completion state |
| `sort_order` | `integer` | No | Visible row order |

---

# 66. Checklist Primary Key

```text
PRIMARY KEY (id)
```

---

# 67. Checklist Relationship

```text
order_id
→ orders.orders.id
```

An Order may contain zero or many Checklist Items.

---

# 68. Checklist Creation

The Order Workspace contains:

```text
Add Checklist Item
```

Pressing it creates a new editable row.

---

# 69. Checklist Item Content

Each row contains only:

```text
Checkbox

Text
```

The system internally stores:

```text
sort_order
```

---

# 70. Checklist Text

Checklist text is free-form.

Examples:

```text
Receive final artwork

Print test sample

Customer approval

Cut panels

Pack for delivery
```

The user may write anything.

---

# 71. Checklist Non-Goals

Do not add:

```text
Employee assignment

Deadline

Priority

Notes

Folder path

File name

Checklist Template
```

to Checklist Items in Version 1.

---

# 72. Checklist Completion

```text
is_completed = false
```

means unfinished.

```text
is_completed = true
```

means completed.

---

# 73. Checklist Sort Order

`sort_order` determines the row position inside the Order.

Example:

```text
10
20
30
40
```

or another simple ordering strategy.

The exact implementation may allow easy insertion/reordering.

---

# 74. Checklist Reordering

If drag-and-drop or move controls are implemented, they update:

```text
sort_order
```

No complex workflow engine is required.

---

# 75. Checklist Item Deletion

Checklist Items may be physically removed.

They are simple Order sub-records and do not require soft delete in Version 1.

---

# 76. Checklist Progress

The UI may calculate progress from Checklist Items.

Example:

```text
6 of 8 completed
```

or:

```text
75%
```

Progress is calculated.

Do not store:

```text
checklist_progress
```

on the Order.

---

# 77. Empty Checklist

An Order may have no Checklist Items.

This is valid.

The UI should display an empty state such as:

```text
No checklist items yet.

Add Checklist Item
```

---

# 78. Folder Links

An Order may have multiple Folder Links.

Folder Links are simple references to local or network folders.

---

# 79. Table: orders.folder_links

## Purpose

Stores Folder Paths related to an Order.

---

# 80. folder_links Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `order_id` | `uuid` | No | Parent Order |
| `name` | `varchar(150)` | Yes | Optional label |
| `path` | `text` | No | Local/network Folder Path |
| `sort_order` | `integer` | No | Display order |

---

# 81. Folder Link Primary Key

```text
PRIMARY KEY (id)
```

---

# 82. Folder Link Relationship

```text
order_id
→ orders.orders.id
```

One Order may have zero or many Folder Links.

---

# 83. Folder Link Examples

```text
Artwork
\\server\orders\ORD-2026-000425\artwork
```

```text
Production
\\server\orders\ORD-2026-000425\production
```

```text
Photos
\\server\orders\ORD-2026-000425\photos
```

---

# 84. Folder Link Name

`name` is an optional human-readable label.

Examples:

```text
Artwork

Production

Customer Files

Photos
```

---

# 85. Folder Path

`path` stores the actual path text.

Example:

```text
\\server\production\orders\ORD-2026-000425
```

Version 1 stores the path only.

---

# 86. No Windows Explorer Integration

Version 1 does not:

```text
Open Windows Explorer

Launch local applications

Use a desktop helper

Access network folders from backend code
```

These features may be designed later.

---

# 87. Folder Link Deletion

Folder Link records may be physically removed.

Removing the ERP reference does not delete the actual folder.

---

# 88. Folder Link Reordering

`sort_order` controls visible order.

Users may reorder Folder Links if the UI provides that feature.

---

# 89. Order Workspace

Order Workspace is one of the most important ERP interfaces.

Recommended structure:

```text
Order Header

General Information

Calculator

Checklist

Folder Links
```

---

# 90. Order Header

Example:

```text
ORD-2026-000425

Entrance UV Panels

Project:
PRJ-2026-000125 — New Store Opening

Client:
CL-000125 — Samsung Armenia

Status:
Active

Priority:
High
```

---

# 91. General Information

The General Information section contains:

```text
Order Type

Name

Description

Status

Priority

Deadline

Selling Price

Cost Price

Preview Image
```

Business ID is displayed read-only.

---

# 92. Project Team Display

The Order Workspace may display the inherited Project Team.

Example:

```text
Owner:
Aram

Assignee:
Samvel

Participants:
Designer 1
Operator 1

Observers:
Employee 4
```

This information is read from the Project.

It is not copied into the Order.

---

# 93. Order Team Editing

Version 1 does not allow editing Project Team from inside an individual Order as an Order-specific override.

If the team must change:

```text
Open Project
    ↓
Edit Project Team
```

The change then applies to all Orders in that Project.

---

# 94. Calculator Section

The Calculator section loads the Calculator associated with the Order Type.

Conceptually:

```text
Order
   ↓
Order Type
   ↓
Calculator Template
   ↓
Order Calculator Data
```

Detailed behavior belongs to the Calculator module.

---

# 95. Price Synchronization

When the Calculator produces final values:

```text
Calculator Selling Result
→ orders.orders.selling_price
```

```text
Calculator Cost Result
→ orders.orders.cost_price
```

The Calculator module is responsible for updating these values safely.

---

# 96. Manual Price Editing

Version 1 should prefer Calculator-generated Selling Price and Cost Price.

If manual override is later required, it must be explicitly designed with:

- Permission
- UI indication
- Historical behavior

Do not introduce silent manual overrides before this requirement is approved.

---

# 97. Create Order

Minimum required information:

```text
Project

Order Type

Name
```

Automatically generated:

```text
Business ID
```

Defaults:

```text
Status = Draft

Priority = Normal

Selling Price = 0

Cost Price = 0
```

Optional:

```text
Description

Deadline

Preview Image
```

---

# 98. Create Order from Project

Preferred workflow:

```text
Project Workspace
    ↓
Create Order
```

The current Project is already known.

The user then selects:

```text
Order Type

Name
```

and other optional data.

---

# 99. Standalone Create Order

If Orders can also be created from the main Orders list, the User must select the Project.

Project selector should show:

```text
PRJ-2026-000125 — New Store Opening
```

and may also show Client context.

---

# 100. Create Order Interface

Example:

```text
New Order

Order ID
Generated automatically

Project
[ PRJ-2026-000125 — New Store Opening ▼ ]

Order Type
[ UV Printing ▼ ]

Name
[________________________________]

Description
[________________________________]

Status
[ Draft ▼ ]

Priority
[ Normal ▼ ]

Deadline
[__________]

[Create] [Cancel]
```

Calculator is opened after the Order exists.

---

# 101. Why Calculator Opens After Creation

The Calculator needs a real Order identity so its data can belong to:

```text
order_id
```

Therefore the simple workflow is:

```text
Create Order
    ↓
Order receives ID
    ↓
Order Workspace opens
    ↓
Calculator becomes available
```

---

# 102. Order List

The Orders module should provide an Orders list.

Example:

| Order ID | Order | Client | Project | Type | Status | Priority | Deadline |
|---|---|---|---|---|---|---|---|
| ORD-2026-000425 | Entrance Panels | Samsung | Store Opening | UV Printing | Active | High | 15 Nov |
| ORD-2026-000426 | Acrylic Logo | Samsung | Store Opening | Laser Cutting | Draft | Normal | 20 Nov |

---

# 103. Order List Financial Columns

Depending on Permissions, the list may also show:

```text
Selling Price

Cost Price

Profit
```

Profit is calculated dynamically.

---

# 104. Financial Visibility

Not every employee should necessarily see financial information.

Detailed field-level visibility rules will be finalized with the Permission design for Orders.

The architecture must support hiding:

```text
selling_price

cost_price

profit
```

according to Permissions.

---

# 105. Initial Financial Permissions

Suggested Orders financial permissions:

```text
orders.view_selling_price

orders.view_cost_price
```

Profit visibility can be derived from access to both values or defined separately if later necessary.

Do not overcomplicate this until actual role requirements are finalized.

---

# 106. Order Search

Search should support:

```text
Business ID

Order Name

Project Name

Client Name
```

---

# 107. Order Filters

Useful Version 1 filters:

```text
Status

Priority

Order Type

Client

Project

Deadline

Project Owner

Project Assignee
```

Do not create a generic query-builder system.

---

# 108. Default Orders View

Normal default view should prioritize unfinished Orders.

For example:

```text
Draft

Active

On Hold
```

Completed and Cancelled Orders remain searchable.

---

# 109. Order Sorting

Useful sorting options may include:

```text
Deadline

Priority

Creation Date

Business ID
```

Exact default sort may be adjusted after real use.

---

# 110. Gallery View

Orders may support two views:

```text
List View

Gallery View
```

Gallery View emphasizes Preview Images.

It uses the same Order data.

No separate Gallery database exists.

---

# 111. Orders Permissions

Initial Orders module permissions may include:

```text
orders.view

orders.create

orders.edit

orders.change_status

orders.manage_checklist

orders.manage_folder_links

orders.manage_types

orders.view_selling_price

orders.view_cost_price
```

Calculator permissions are defined by the Calculator module.

---

# 112. orders.view

Allows:

- View Order list
- Open Order Workspace
- Search and filter Orders

Financial fields remain subject to financial visibility permissions.

---

# 113. orders.create

Allows:

```text
Create Order
```

---

# 114. orders.edit

Allows editing normal Order fields:

```text
Order Type

Name

Description

Priority

Deadline

Preview Image
```

Status may use a separate Permission.

Calculator-generated financial values are not ordinary edit fields.

---

# 115. orders.change_status

Allows changing Order lifecycle status.

---

# 116. orders.manage_checklist

Allows:

```text
Add Checklist Item

Edit Checklist Item

Complete Checklist Item

Reorder Checklist Items

Remove Checklist Item
```

---

# 117. orders.manage_folder_links

Allows:

```text
Add Folder Link

Edit Folder Link

Remove Folder Link

Reorder Folder Links
```

---

# 118. orders.manage_types

Administrative Permission allowing:

```text
Create Order Type

Edit Order Type

Activate Order Type

Deactivate Order Type

Assign Calculator Template
```

---

# 119. Order Type Administration

Order Type management should normally appear under:

```text
Administration
```

Example:

```text
Administration
├── Users
├── Roles
├── Employees
├── Order Types
└── Calculator Templates
```

---

# 120. Order Type List

Example:

| Order Type | Calculator Template | Status |
|---|---|---|
| UV Printing | UV Printing v1 | Active |
| Laser Cutting | Laser Calculator v2 | Active |
| Graphic Design | Design Calculator | Active |
| Old Process | — | Inactive |

---

# 121. Order Type Creation

Minimum:

```text
Name
```

Optional:

```text
Description

Calculator Template

Active
```

Default:

```text
Active = true
```

---

# 122. Order Type Editing

Editable:

```text
Name

Description

Calculator Template

Active
```

Deactivating an Order Type does not alter historical Orders.

---

# 123. Changing Calculator Template on Order Type

Changing the Calculator Template assigned to an Order Type must not silently change historical Orders.

The Calculator module will define how existing Orders retain their original calculation context.

This requirement is critical.

---

# 124. Order API Responsibilities

The Orders API may conceptually provide:

```text
Get Orders

Get Order

Create Order

Update Order

Change Order Status

Get Checklist

Add Checklist Item

Update Checklist Item

Complete Checklist Item

Delete Checklist Item

Reorder Checklist

Get Folder Links

Add Folder Link

Update Folder Link

Delete Folder Link

Get Order Types

Create Order Type

Update Order Type

Activate Order Type

Deactivate Order Type
```

Exact REST routes are finalized during implementation.

---

# 125. Order Response DTO

A detailed Order response may contain:

```text
id

business_id

project

client

order_type

name

description

status

priority

selling_price

cost_price

deadline

preview_image

project_team

checklist

folder_links

created_at

updated_at
```

Financial fields must respect authorization.

---

# 126. Order List DTO

A lighter list response may contain:

```text
id

business_id

name

client_name

project_name

order_type_name

status

priority

deadline

preview_image
```

Financial values may be added only when authorized.

---

# 127. Create Order Request

Conceptual request:

```text
project_id

order_type_id

name

description

priority

deadline
```

Business ID is generated by backend.

Status may default to Draft.

Prices default to zero.

---

# 128. Update Order Request

Conceptual normal editable fields:

```text
order_type_id

name

description

priority

deadline

preview_image
```

Status changes should preferably use a dedicated operation.

Calculator financial updates should come from the Calculator workflow.

---

# 129. Validation Rules

Backend must validate:

- Project exists
- Order Type exists
- Order Type is active for new Orders
- Name is required
- Name is not empty after trimming
- Status is valid
- Priority is valid
- Selling Price is not negative
- Cost Price is not negative
- Deadline is a valid date
- Current User has required Permission

---

# 130. Inactive Order Type

An inactive Order Type:

```text
Cannot normally be selected for a new Order
```

Existing Orders using it remain valid.

---

# 131. Project Status and New Orders

Version 1 should normally allow new Orders for:

```text
Draft

Active

On Hold
```

Projects.

Creating Orders inside:

```text
Completed

Cancelled
```

Projects should normally be blocked or explicitly confirmed depending on final workflow.

The simplest recommended rule is to block it.

---

# 132. Completed Project Rule

Recommended:

```text
Cannot create a new Order in a Completed Project.
```

The Project must first be reopened if new work is required.

If reopening is later needed, it can use Project status changes.

---

# 133. Cancelled Project Rule

Do not allow new Orders inside a Cancelled Project.

---

# 134. Completing Order with Incomplete Checklist

Version 1 should warn, but not necessarily block, when completing an Order with unfinished Checklist Items.

Example:

```text
This Order has 2 incomplete Checklist Items.

Complete the Order anyway?
```

This keeps the checklist flexible rather than turning it into a rigid workflow engine.

---

# 135. Cancelling Order

Cancelling an Order does not delete:

```text
Calculator Data

Checklist

Folder Links
```

Historical data remains available.

---

# 136. Changing Order Type

Changing Order Type may change the applicable Calculator Template.

This can have major calculation consequences.

Therefore, once Calculator data exists, changing Order Type must be handled carefully.

The Calculator module will define the exact behavior.

Recommended principle:

```text
Do not silently destroy existing Calculator data.
```

---

# 137. Order Type Change Warning

If Calculator data exists and the User changes Order Type, the UI should require an explicit decision according to the Calculator specification.

Do not implement automatic destructive conversion.

---

# 138. Project Change

Changing the Project of an existing Order changes:

- Client context
- Project Team context
- Project reporting context

Therefore changing Project should be treated as an important operation.

---

# 139. Project Change Warning

If an Order already contains Calculator data or production history, the UI should warn before moving it to another Project.

Version 1 may allow Project change only while Order is Draft if that proves simpler.

The final rule can be refined during implementation.

---

# 140. Recommended Project Change Rule

For Version 1, use the simple rule:

```text
Project may be changed only while Order status = Draft.
```

This avoids confusing historical changes after work begins.

---

# 141. Recommended Order Type Change Rule

Likewise, recommended simple rule:

```text
Order Type may be freely changed only before Calculator data exists.
```

Once calculation data exists, use the Calculator module's explicit reset/change workflow.

---

# 142. Audit Fields

Order audit fields:

```text
created_at

created_by

updated_at

updated_by
```

reference Authentication Users.

Checklist and Folder Link tables do not require full audit history in Version 1.

---

# 143. Required Indexes: orders.orders

Primary key:

```text
PRIMARY KEY (id)
```

Business ID:

```text
UNIQUE INDEX (business_id)
```

Foreign keys:

```text
INDEX (project_id)

INDEX (order_type_id)
```

Useful filters may justify:

```text
INDEX (status)

INDEX (deadline)
```

after observing query patterns.

---

# 144. Required Indexes: order_types

Primary key:

```text
PRIMARY KEY (id)
```

Unique normalized Order Type Name as appropriate.

Optional:

```text
INDEX (is_active)
```

only if useful.

---

# 145. Required Indexes: checklist_items

Primary key:

```text
PRIMARY KEY (id)
```

Required:

```text
INDEX (order_id)
```

Useful ordering index may conceptually combine:

```text
(order_id, sort_order)
```

---

# 146. Required Indexes: folder_links

Primary key:

```text
PRIMARY KEY (id)
```

Required:

```text
INDEX (order_id)
```

Optional ordering index:

```text
(order_id, sort_order)
```

---

# 147. Checklist Delete Behavior

When an Order is exceptionally physically removed through development or maintenance, its Checklist Items may cascade-delete.

Normal application workflow does not physically delete Orders.

---

# 148. Folder Link Delete Behavior

Folder Link database records may cascade-delete if their parent Order is physically removed.

This affects only ERP references.

It must never delete actual filesystem folders.

---

# 149. Order Type Delete Behavior

Order Types referenced by Orders must not be physically deleted.

Deactivate them instead.

This preserves historical Orders.

---

# 150. Calculator Template Delete Behavior

A Calculator Template referenced by an Order Type or historical Calculator data must not be destructively deleted without the Calculator module handling it safely.

---

# 151. Reporting

Orders provide the main operational data for reports.

Examples:

```text
Orders by Date

Orders by Client

Orders by Project

Orders by Type

Orders by Status

Orders by Priority
```

---

# 152. Financial Reporting

Examples:

```text
Total Selling Price

Total Cost Price

Total Profit
```

Formula:

```text
Profit = Selling Price - Cost Price
```

---

# 153. Project Aggregation

Project financial values are calculated from Orders.

Example:

```text
Project Selling Total
=
SUM(Orders Selling Price)
```

---

# 154. Client Aggregation

Client totals may later be calculated through:

```text
Client
  ↓
Projects
  ↓
Orders
```

No Client financial totals need to be stored.

---

# 155. Order History

Version 1 does not require a full field-by-field Order history/audit timeline.

Normal timestamps are sufficient initially.

Advanced change history may be added later if useful.

---

# 156. Order Comments

Version 1 does not include comments or chat inside Orders.

Do not introduce collaboration infrastructure prematurely.

---

# 157. Order Attachments

Version 1 does not include generic file attachments.

Use Folder Links for external/local file references.

Preview Image is the only dedicated visual file reference.

---

# 158. Filename Field

Version 1 does not require a separate filename field next to Folder Links.

Folder Links represent folders only.

If a future production workflow needs explicit file-name references, it may be added later.

---

# 159. Automatic Folder Creation

Version 1 does not automatically create physical folders.

Folder creation remains outside ERP behavior.

---

# 160. Notifications

Version 1 does not require automatic Order notifications.

Do not add email, push, or messaging infrastructure.

---

# 161. Order Templates

Version 1 does not include Order Templates.

Calculator Templates are separate and are associated with Order Types.

---

# 162. Workflow Engine

Order Status is intentionally simple.

Do not create a generic workflow engine for:

```text
Draft
Active
On Hold
Completed
Cancelled
```

Straightforward business logic is sufficient.

---

# 163. Production Scheduling

Version 1 does not schedule machine time or employee time.

This may become a future Production Planning module.

---

# 164. Machines

Version 1 does not track individual machine assets, maintenance, availability, or scheduling.

Order Type is sufficient for current workflow classification.

---

# 165. Outsourced Orders

Outsourced work uses a normal Order Type.

Example:

```text
Order Type:
Outsourced Work
```

It remains part of the same Order structure.

A Supplier or Purchasing workflow may be added later.

---

# 166. Graphic Design Orders

Design work also uses a normal Order Type.

Example:

```text
Order Type:
Graphic Design
```

This confirms that an Order does not require physical machine production.

---

# 167. Installation Orders

Installation is represented as an Order Type.

Example:

```text
Order Type:
Installation
```

No separate Installation module is required in Version 1.

---

# 168. Version 1 Non-Goals

Orders Version 1 does not include:

```text
Order-Specific Teams

Machine Management

Production Scheduling

Employee Scheduling

Notifications

Order Chat

Order Comments

Generic Attachments

Automatic Folder Creation

Windows Explorer Integration

Supplier Purchasing

Invoice Generation

Order Templates

Generic Workflow Engine

Full Audit Timeline
```

---

# 169. Future Extensions

Possible future additions may include:

- Order-specific team overrides
- File attachments
- Automatic folder creation
- Windows desktop integration
- Production scheduling
- Machine tracking
- Notifications
- Order history
- Supplier relationships
- Installation planning

These possibilities must not complicate Version 1.

---

# 170. Testing Requirements

Important Orders tests should include:

```text
Create Order

Generate Order Business ID

Business ID is unique

Business ID cannot be edited

Order requires Project

Order requires Order Type

Cannot create Order with inactive Order Type

Cannot create Order in Cancelled Project

Cannot create Order in Completed Project

Default Status is Draft

Default Priority is Normal

Default Selling Price is zero

Default Cost Price is zero

Reject negative Selling Price

Reject negative Cost Price

Order uses Project Client context

Order uses Project Team logically

No Order Team rows created

Add Checklist Item

Edit Checklist Item

Complete Checklist Item

Delete Checklist Item

Reorder Checklist

Checklist progress calculates correctly

Add Folder Link

Edit Folder Link

Delete Folder Link

Folder Link deletion does not touch filesystem

Deactivate Order Type

Historical Orders retain inactive Order Type

Assign Calculator Template to Order Type

Changing Template does not silently alter historical Order prices

Complete Order with unfinished Checklist produces warning behavior
```

---

# 171. Version 1 Schema Summary

```text
orders.orders

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

```text
orders.order_types

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

```text
orders.checklist_items

id
order_id
text
is_completed
sort_order
```

```text
orders.folder_links

id
order_id
name
path
sort_order
```

Four tables are sufficient for the Orders module in Version 1.

---

# 172. Module Simplicity Rule

Before adding another Order table or field, ask:

```text
Is this required to manage Lithograph's current production and service Orders?
```

If not, do not add it yet.

---

# 173. Final Orders Principle

The Orders module should answer:

```text
What work must be done?
```

```text
For which Project?
```

```text
What type of work is it?
```

```text
What is its Selling Price?
```

```text
What is its Cost Price?
```

```text
What steps remain?
```

and:

```text
Where are its related folders?
```

Everything else should be added only when a real operational need appears.

---

**End of Document**