# Lithograph ERP

**Document:** 31_Orders_Implementation_Plan.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Orders

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `05_Numbering_System.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `22_Logging_Audit_and_Operational_History.md`
- `23_Frontend_Architecture.md`
- `24_Backend_Architecture.md`
- `25_Development_Workflow_for_AI.md`
- `30_Projects_Implementation_Plan.md`

---

# 1. Purpose

This document defines the implementation sequence for the Orders module.

Order is the central operational unit of Lithograph ERP.

The business hierarchy is:

```text
Client
   ↓
Project
   ↓
Order
```

An Order represents a specific piece of work performed for a Project.

---

# 2. Main Goal

After this phase, Lithograph ERP must be able to:

```text
Create Order Types

Create Orders

Generate Order Business IDs

Edit Orders

Change Order status

Set priority

Maintain simple Checklist Items

Maintain Folder Links

Store Preview Image reference

Display Selling Price

Display Cost Price

Open Order Workspace

Display inherited Project/Client/Team context
```

Calculator functionality is connected in the next phase.

---

# 3. Module Boundary

Orders owns:

```text
orders.orders

orders.order_types

orders.checklist_items

orders.folder_links
```

Orders references:

```text
projects.projects

calculator.templates
```

The Calculator relationship may initially remain nullable until Calculator implementation is completed.

---

# 4. Order Does Not Own Client

Do not add:

```text
orders.orders.client_id
```

Client is derived through:

```text
Order
→ Project
→ Client
```

---

# 5. Order Does Not Own Team

Do not add:

```text
order_owner_id

order_assignee_id

orders.order_members
```

Order Team context is derived through:

```text
Order
→ Project
→ Project Team
```

---

# 6. Version 1 Order Fields

`orders.orders` contains:

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

# 7. Version 1 Order Type Fields

`orders.order_types` contains:

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

# 8. Version 1 Checklist Item Fields

`orders.checklist_items` contains only:

```text
id

order_id

text

is_completed

sort_order
```

---

# 9. Version 1 Folder Link Fields

`orders.folder_links` contains:

```text
id

order_id

name

path

sort_order
```

---

# 10. Recommended Implementation Sequence

```text
Task 1
Order domain types

Task 2
Order Type entity

Task 3
Order entity

Task 4
Checklist Item entity

Task 5
Folder Link entity

Task 6
Order numbering

Task 7
EF Core configuration

Task 8
Orders migration

Task 9
Order permissions

Task 10
Order Type backend

Task 11
Order list/detail queries

Task 12
Create Order

Task 13
Edit Order

Task 14
Order status changes

Task 15
Checklist backend

Task 16
Folder Links backend

Task 17
Preview Image reference support

Task 18
Frontend Order Types

Task 19
Frontend Orders List

Task 20
Frontend Order Workspace

Task 21
Checklist UI

Task 22
Folder Links UI

Task 23
Tests

Task 24
Calculator readiness review
```

---

# 11. Task 1 — Order Domain Types

Create stable domain values for:

```text
OrderStatus

OrderPriority
```

---

# 12. Order Statuses

Supported statuses:

```text
Draft

Active

On Hold

Completed

Cancelled
```

Do not create configurable Order Status tables in Version 1.

---

# 13. Order Status Machine Values

Recommended API values:

```text
draft

active

on_hold

completed

cancelled
```

Display labels:

```text
Draft

Active

On Hold

Completed

Cancelled
```

---

# 14. Order Priorities

Supported priorities:

```text
Low

Normal

High

Urgent
```

---

# 15. Priority Machine Values

Recommended:

```text
low

normal

high

urgent
```

---

# 16. Default Priority

New Order default:

```text
Normal
```

---

# 17. Default Status

New Order default:

```text
Draft
```

---

# 18. Task 2 — Order Type Entity

Create:

```text
OrderType
```

mapped to:

```text
orders.order_types
```

---

# 19. Order Type Purpose

Order Type categorizes what kind of work is being performed.

Examples:

```text
UV Printing

CO₂ Laser Cutting

Graphic Design

Installation

Outsourced Work
```

---

# 20. Order Type Is Configurable Data

Do not implement Order Type as:

```text
enum
```

It must be database-configurable.

---

# 21. Order Type Name

`name` is required.

Recommended maximum length:

```text
200
```

---

# 22. Order Type Name Uniqueness

Order Type names should be unique case-insensitively.

Example:

```text
UV Printing
```

and:

```text
uv printing
```

should not become separate active Order Types.

---

# 23. Order Type Description

Optional free text.

---

# 24. Order Type Active State

`is_active` defaults to:

```text
true
```

Inactive Order Types:

```text
Cannot normally be selected for new Orders
```

but remain valid for historical Orders.

---

# 25. Calculator Template Relationship

Order Type contains optional:

```text
calculator_template_id
```

referencing:

```text
calculator.templates.id
```

---

# 26. Calculator Relationship During Orders Phase

Before Calculator module is implemented:

```text
calculator_template_id = NULL
```

is completely valid.

Do not create fake Calculator Templates.

---

# 27. Order Type Without Calculator

A valid Order Type may permanently have no Calculator.

Example:

```text
Graphic Design
```

may use a manually established commercial process until a Calculator is later configured.

---

# 28. Calculator Template Assignment

Once Calculator module exists, Administration may assign:

```text
Order Type
→ Calculator Template
```

Do not duplicate Template definitions inside Orders.

---

# 29. Task 3 — Order Entity

Create:

```text
Order
```

mapped to:

```text
orders.orders
```

---

# 30. Order Primary Key

Use:

```text
uuid
```

for internal identity.

---

# 31. Order Business ID

Format:

```text
ORD-2026-000001
```

---

# 32. Order Numbering Rules

Order Business ID is:

```text
Backend generated

Unique

Immutable

Year-based

6-digit sequence

Never reused

Gaps allowed
```

---

# 33. Order Numbering Scope

Numbering is global per year.

Do not number Orders separately per Project.

Correct:

```text
ORD-2026-000001
ORD-2026-000002
ORD-2026-000003
```

regardless of which Project contains them.

---

# 34. Do Not Use Project Subnumbers

Do not generate:

```text
PRJ-2026-000125-01
```

or:

```text
ORD-PRJ125-001
```

Version 1 uses globally unique yearly Order numbers.

---

# 35. Project Relationship

Every Order requires:

```text
project_id
```

referencing:

```text
projects.projects.id
```

---

# 36. Order Type Relationship

Every Order requires:

```text
order_type_id
```

referencing:

```text
orders.order_types.id
```

---

# 37. Order Name

`name` is required.

Recommended maximum length:

```text
250
```

---

# 38. Description

`description` is optional free text.

---

# 39. Selling Price

Store:

```text
selling_price
```

as:

```text
numeric(18,2)
```

or matching approved exact decimal type.

---

# 40. Initial Selling Price

Default:

```text
0
```

until Calculator or approved pricing workflow sets it.

---

# 41. Selling Price Authority

Once Calculator is implemented:

```text
Calculator
→ authoritative Selling Price
```

Do not let ordinary Order edit endpoint arbitrarily overwrite it.

---

# 42. Cost Price

Store:

```text
cost_price
```

as exact decimal.

---

# 43. Initial Cost Price

Default:

```text
0
```

---

# 44. Cost Price Authority

Once Cost Items are implemented:

```text
cost_price
=
SUM(calculator.cost_items.amount)
```

Do not allow ordinary Order editing to change Cost Price directly.

---

# 45. Profit

Do not create:

```text
profit
```

column.

Profit is derived:

```text
selling_price - cost_price
```

---

# 46. Deadline

Order has optional:

```text
deadline
```

as date-only value.

---

# 47. Preview Image Path

Order may store one optional:

```text
preview_image_path
```

or equivalent file reference.

Do not store image binary in PostgreSQL.

---

# 48. Task 4 — Checklist Item Entity

Create:

```text
ChecklistItem
```

mapped to:

```text
orders.checklist_items
```

---

# 49. Checklist Design

There is one implicit Checklist per Order.

Do not create:

```text
orders.checklists
```

header table.

---

# 50. Checklist Item Text

`text` is required.

Recommended maximum length:

```text
500
```

or reasonable text constraint.

---

# 51. Checklist Completion

Store:

```text
is_completed
```

boolean.

Default:

```text
false
```

---

# 52. Checklist Sort Order

Store:

```text
sort_order
```

integer.

This controls display order.

---

# 53. Checklist Simplicity

Do not add:

```text
employee_id

deadline

priority

notes

folder_path

filename

status enum
```

to Checklist Item.

---

# 54. Checklist Deletion

Physical deletion of a Checklist Item is allowed.

This is a lightweight operational sub-record.

---

# 55. Checklist Progress

Progress is calculated.

Example:

```text
3 completed
of
5 total
```

Do not store a Checklist percentage column.

---

# 56. Task 5 — Folder Link Entity

Create:

```text
FolderLink
```

mapped to:

```text
orders.folder_links
```

---

# 57. Folder Link Purpose

Folder Link stores a textual reference to an external production folder.

Example:

```text
\\server\orders\ORD-2026-000125\artwork
```

---

# 58. Folder Link Name

Optional label.

Examples:

```text
Artwork

Production

Customer Files
```

---

# 59. Folder Link Path

`path` is required.

Store as text/string.

---

# 60. Folder Link Sort Order

Store:

```text
sort_order
```

to control UI order.

---

# 61. No File Name Field

Do not add:

```text
filename
```

to Folder Links.

That earlier idea is not part of Version 1.

---

# 62. No Explorer Integration

Do not implement:

```text
Open in Windows Explorer
```

in Version 1.

Browser may provide:

```text
Copy Path
```

only.

---

# 63. No Filesystem Validation

Backend should not require that the folder physically exists.

The path is stored as business data.

---

# 64. Task 6 — Order Numbering

Extend the shared Business ID infrastructure.

Add:

```text
ORD-{YYYY}-{000001}
```

---

# 65. Year Source

Use backend controlled time.

---

# 66. Year Boundary

Example:

```text
2026-12-31
→ ORD-2026-...

2027-01-01
→ ORD-2027-000001
```

---

# 67. Concurrency

Simultaneous Order creation must not generate duplicate Business IDs.

---

# 68. Client, Project, Order Counters

Client sequence:

```text
CL-000001
```

is separate from Project sequence.

Project yearly sequence is separate from Order yearly sequence.

---

# 69. Task 7 — EF Core Configuration

Configure schema:

```text
orders
```

tables:

```text
orders

order_types

checklist_items

folder_links
```

---

# 70. Orders Table Constraints

Configure:

```text
PK(id)

UNIQUE(business_id)

FK(project_id)

FK(order_type_id)

required name

status

priority

decimal precision

deadline

preview path

audit fields
```

---

# 71. Order Type Constraints

Configure:

```text
PK(id)

case-insensitive unique name

calculator_template_id nullable

is_active default true
```

---

# 72. Checklist Item Constraints

Configure:

```text
PK(id)

FK(order_id)

required text

is_completed default false

sort_order
```

---

# 73. Folder Link Constraints

Configure:

```text
PK(id)

FK(order_id)

required path

sort_order
```

---

# 74. Child Delete Behavior

Deleting a Checklist Item or Folder Link affects only that row.

---

# 75. Order Deletion

Normal physical Order deletion is not part of Version 1.

Use:

```text
Cancelled
```

status instead.

---

# 76. Project Delete Behavior

Order must never disappear because Project is removed.

Normal Project physical deletion is not supported.

Use restrictive safe FK behavior.

---

# 77. Order Type Delete Behavior

Historical Orders must remain valid when Order Type is inactive.

Normal physical Order Type deletion is not part of normal workflow.

---

# 78. Task 8 — Orders Migration

Recommended migration name:

```text
AddOrdersModule
```

---

# 79. Migration Scope

Create:

```text
orders schema

orders.order_types

orders.orders

orders.checklist_items

orders.folder_links
```

and Order numbering infrastructure changes.

---

# 80. Calculator FK Migration Dependency

Because:

```text
orders.order_types.calculator_template_id
→ calculator.templates.id
```

but Calculator may not yet exist, use one of these controlled approaches:

```text
A.
Create nullable column now
and add FK after calculator.templates exists
```

or:

```text
B.
Delay calculator_template_id column/FK until Calculator base migration
```

Choose the approach that produces the cleanest migration dependency.

---

# 81. Recommended Cross-Module FK Approach

If Calculator tables do not yet exist:

```text
Create Order Type without the FK first
```

then add:

```text
calculator_template_id
```

or its FK in the Calculator migration.

Do not create fake Calculator tables merely to satisfy an early FK.

---

# 82. Documentation Consistency

If implementation delays the physical FK but the conceptual model remains unchanged, document the migration dependency clearly.

---

# 83. Task 9 — Order Permissions

Register:

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

---

# 84. Financial Permission Principle

Permission to:

```text
orders.view
```

does not automatically imply permission to view financial fields.

---

# 85. Selling Price Permission

Requires:

```text
orders.view_selling_price
```

---

# 86. Cost Price Permission

Requires:

```text
orders.view_cost_price
```

Later Calculator cost permissions also apply to detailed Cost Items.

---

# 87. Profit Visibility

Profit should normally be visible only when User can see both:

```text
Selling Price
+
Cost Price
```

---

# 88. Task 10 — Order Type Backend

Implement:

```text
GET /api/order-types

GET /api/order-types/{id}

POST /api/order-types

PATCH /api/order-types/{id}

POST /api/order-types/{id}/activate

POST /api/order-types/{id}/deactivate
```

---

# 89. Order Type Permissions

Management requires:

```text
orders.manage_types
```

Reading active Order Types for selectors may be available to Users allowed to create/edit Orders.

Use the established authorization model rather than duplicating permissions unnecessarily.

---

# 90. Create Order Type Request

Initial fields:

```text
name

description
```

Calculator Template assignment may be added later.

---

# 91. Duplicate Order Type Name

Case-insensitive duplicate should be rejected.

---

# 92. Inactive Order Type

Inactive Order Type:

```text
remains visible historically
```

but:

```text
not available for normal new Order creation
```

---

# 93. Task 11 — Orders List Backend

Implement:

```text
GET /api/orders
```

Requires:

```text
orders.view
```

---

# 94. Order List DTO

Recommended fields:

```text
id

businessId

name

project

client

orderType

status

priority

deadline

previewImage

sellingPrice?

costPrice?

profit?
```

Financial fields depend on permissions.

---

# 95. Project Summary

Return compact:

```text
id

businessId

name
```

---

# 96. Client Summary

Derived through Project.

Return:

```text
id

businessId

name
```

---

# 97. Order Type Summary

Return:

```text
id

name

isActive
```

as appropriate.

---

# 98. Team Context in List

Do not include full Project Team in every Order list row unless needed.

Owner/Assignee may be included for filtering/display where useful.

---

# 99. Order Search

Support:

```text
Order Business ID

Order Name

Project Business ID

Project Name

Client Business ID

Client Name
```

where practical.

---

# 100. Order Filters

Useful filters:

```text
projectId

clientId

orderTypeId

status

priority

ownerEmployeeId

assigneeEmployeeId

deadline range
```

---

# 101. Client Filter Implementation

Because Order has no `client_id`, filtering by Client follows:

```text
Order
→ Project
→ Client
```

Do not denormalize solely for filtering.

---

# 102. Team Filter Implementation

Owner/Assignee filters follow:

```text
Order
→ Project
→ Project Members
```

---

# 103. Pagination

Use standard server-side pagination.

---

# 104. Sorting

Useful sort fields:

```text
business_id

name

status

priority

deadline

created_at

selling_price

cost_price
```

Financial sorting must still respect permissions.

---

# 105. Task 12 — Get Order

Implement:

```text
GET /api/orders/{id}
```

Requires:

```text
orders.view
```

---

# 106. Order Detail DTO

Recommended sections/data:

```text
General

Project Context

Client Context

Inherited Project Team

Checklist

Folder Links

Financial Summary

Preview Image

Calculator availability metadata
```

Heavy child resources may also be fetched separately.

---

# 107. Inherited Team

Order detail may display:

```text
Owner

Assignee

Participants

Observers
```

derived from Project.

No Order Team records are created.

---

# 108. Task 13 — Create Order

Implement:

```text
POST /api/orders
```

Requires:

```text
orders.create
```

---

# 109. Minimum Create Request

Required:

```text
projectId

orderTypeId

name
```

Optional:

```text
description

deadline

priority
```

---

# 110. Create Validation — Project

Backend must verify Project exists.

---

# 111. Closed Project Rule

Normal Order creation is rejected when Project status is:

```text
Completed

Cancelled
```

---

# 112. Draft/Active/On Hold Projects

Order creation may be allowed for:

```text
Draft

Active

On Hold
```

unless later business experience requires tighter rules.

---

# 113. Create Validation — Order Type

Order Type must:

```text
exist
```

and:

```text
be active
```

for normal new Order creation.

---

# 114. Create Defaults

Backend sets:

```text
status = Draft

priority = Normal

sellingPrice = 0

costPrice = 0
```

unless approved values are explicitly supplied where permitted.

---

# 115. Create Business ID

Generate:

```text
ORD-YYYY-000001
```

transactionally/concurrency-safely.

---

# 116. Audit

Set:

```text
createdAt

createdBy
```

from backend.

---

# 117. Create Response

Return:

```text
201 Created
```

with Order detail or suitable response DTO.

---

# 118. Task 14 — Edit Order

Implement:

```text
PATCH /api/orders/{id}
```

Requires:

```text
orders.edit
```

---

# 119. Normal Editable Fields

Allow:

```text
projectId

orderTypeId

name

description

priority

deadline
```

subject to rules.

---

# 120. Financial Fields Not General Edit

Do not allow normal Order edit DTO to directly set:

```text
sellingPrice

costPrice
```

once Calculator/Costs are authoritative.

Even before Calculator integration, avoid establishing direct unrestricted financial editing unless separately approved.

---

# 121. Business ID Immutable

Never allow update of:

```text
businessId
```

---

# 122. Status Separate

Status changes use dedicated operation.

---

# 123. Project Change Rule

Recommended:

```text
Project may change only while Order status = Draft
```

---

# 124. Project Change Validation

New Project must:

```text
exist

not be Completed

not be Cancelled
```

---

# 125. Project Change and Client

Because Client is derived from Project, changing Project may also change the Order's effective Client.

UI should clearly show this.

---

# 126. Project Change After Calculator

Once Calculator exists, Project change does not inherently alter Calculator Template because Template is based on Order Type.

However, Project change remains restricted by Order lifecycle.

---

# 127. Order Type Change Rule

Order Type may change freely while:

```text
no Order Calculator exists
```

subject to active Order Type validation.

---

# 128. Order Type Change After Calculator

Once Calculator exists:

```text
Do not silently replace Calculator.
```

Require explicit Calculator reset/change workflow.

This is implemented during Calculator phase.

---

# 129. Order Type Historical Rule

Existing Orders may reference Order Types that later become inactive.

Editing unrelated Order fields must still work.

---

# 130. Update Audit

General successful Order edit updates:

```text
updatedAt

updatedBy
```

---

# 131. Task 15 — Order Status API

Implement explicit status change.

Conceptually:

```text
POST /api/orders/{id}/status
```

Requires:

```text
orders.change_status
```

---

# 132. Allowed Status Values

```text
Draft

Active

On Hold

Completed

Cancelled
```

---

# 133. No Workflow Engine

Version 1 does not need a configurable status-transition engine.

---

# 134. Completed Order

When changing to:

```text
Completed
```

and Checklist contains incomplete Items:

```text
warn
```

but do not automatically block completion.

---

# 135. Warning Implementation

Frontend may request confirmation:

```text
This Order has incomplete Checklist Items.

Complete Order anyway?
```

Backend does not need to enforce Checklist completion as a hard invariant.

---

# 136. Cancelled Order

Cancelled is the normal replacement for deleting an Order.

---

# 137. Cancelled Order Data

Cancellation must not delete:

```text
Checklist

Folder Links

Future Calculator

Future Cost Items
```

Historical data remains.

---

# 138. Reopening

Version 1 may permit status changes from Completed/Cancelled back to another state if business permissions allow.

Do not create irreversible state mechanics unless explicitly required.

---

# 139. Task 16 — Checklist Backend

Implement:

```text
GET /api/orders/{orderId}/checklist-items

POST /api/orders/{orderId}/checklist-items

PATCH /api/orders/{orderId}/checklist-items/{itemId}

DELETE /api/orders/{orderId}/checklist-items/{itemId}
```

---

# 140. Checklist Permission

Requires:

```text
orders.manage_checklist
```

for mutations.

Viewing Checklist follows normal Order view permission.

---

# 141. Add Checklist Item Request

Contains:

```text
text
```

and optionally:

```text
sortOrder
```

if needed.

---

# 142. Default Sort Order

If frontend does not send an order, backend may assign next reasonable `sort_order`.

Do not use complex fractional ordering unless needed.

---

# 143. Edit Checklist Item

Allow:

```text
text

isCompleted
```

and sorting through dedicated reorder operation if implemented.

---

# 144. Checklist Reorder

Optional dedicated endpoint:

```text
POST /api/orders/{orderId}/checklist-items/reorder
```

should update ordering atomically.

---

# 145. Checklist Parent Validation

For:

```text
orderId
+
itemId
```

verify Item belongs to Order.

---

# 146. Checklist Item Delete

Physical row deletion is acceptable.

---

# 147. No Checklist Audit Fields

Do not add audit metadata during Version 1.

---

# 148. Task 17 — Folder Links Backend

Implement:

```text
GET /api/orders/{orderId}/folder-links

POST /api/orders/{orderId}/folder-links

PATCH /api/orders/{orderId}/folder-links/{linkId}

DELETE /api/orders/{orderId}/folder-links/{linkId}
```

---

# 149. Folder Link Permission

Mutations require:

```text
orders.manage_folder_links
```

Viewing follows Order view permission.

---

# 150. Add Folder Link Request

Contains:

```text
name?

path
```

---

# 151. Path Validation

Validate:

```text
not empty
```

and reasonable length.

Do not reject solely because backend machine cannot access that path.

---

# 152. Folder Link Parent Validation

Verify Link belongs to the route Order.

---

# 153. Reorder Folder Links

May use same simple ordering approach as Checklist Items.

---

# 154. Removing Folder Link

Deleting ERP row:

```text
must not delete external folder
```

---

# 155. Task 18 — Preview Image Reference

Initial implementation may support only storing:

```text
preview_image_path
```

without implementing upload yet.

---

# 156. Preview Image Upload

If file storage is ready, a dedicated endpoint may later support upload.

Do not block Orders module completion on sophisticated file handling.

---

# 157. Initial Safe Approach

Version 1 first implementation may allow:

```text
previewImagePath
```

to be set by controlled backend/storage workflow.

Do not expose arbitrary server filesystem operations.

---

# 158. Gallery Readiness

Orders API should provide preview image reference suitable for future Gallery view.

---

# 159. Gallery View

Gallery is an alternate Orders presentation.

It is not a separate module or table.

---

# 160. Gallery Card Data

May show:

```text
Preview

Order Business ID

Order Name

Client

Project

Order Type
```

---

# 161. Gallery Timing

Gallery polish may be implemented after core Orders table/workspace works.

It should not delay Calculator development.

---

# 162. Task 19 — Frontend Order Types Administration

Add:

```text
/admin/order-types
```

---

# 163. Order Type UI Permissions

Management requires:

```text
orders.manage_types
```

---

# 164. Order Type List

Recommended columns:

```text
Name

Description

Calculator Template

Status
```

During this phase, Calculator Template may display:

```text
None
```

for all records.

---

# 165. Order Type Create/Edit

Fields:

```text
Name

Description
```

Calculator Template field is added/enabled once Calculator templates exist.

---

# 166. Deactivate Confirmation

Example:

```text
Deactivate Order Type?

It will no longer be available for new Orders.
Existing Orders will remain unchanged.
```

---

# 167. Task 20 — Frontend Orders List

Add primary route:

```text
/orders
```

Requires:

```text
orders.view
```

---

# 168. Main Navigation

Add:

```text
Orders
```

to primary navigation.

---

# 169. Order List Columns

Recommended:

```text
Business ID

Order Name

Client

Project

Order Type

Status

Priority

Deadline
```

Optional financial columns:

```text
Selling Price

Cost Price

Profit
```

based on permissions.

---

# 170. Create Order Button

Visible with:

```text
orders.create
```

---

# 171. Create Order From Orders List

Form requires:

```text
Project

Order Type

Name
```

Optional:

```text
Description

Priority

Deadline
```

---

# 172. Create Order From Project Workspace

When launched from Project:

```text
Project
```

should already be selected.

This is the preferred workflow for many users.

---

# 173. Project Selector

Show only Projects eligible for new Orders.

Normally exclude:

```text
Completed

Cancelled
```

---

# 174. Order Type Selector

Show only active Order Types for new Orders.

---

# 175. Orders Filters UI

Useful initial filters:

```text
Search

Client

Project

Order Type

Status

Priority

Owner

Assignee
```

---

# 176. Financial Columns

If User lacks permission, protected columns should not merely be hidden after data loads.

The API must not return protected values.

---

# 177. Task 21 — Order Workspace

Route:

```text
/orders/:orderId
```

---

# 178. Workspace Structure

Recommended:

```text
Order Header

General

Calculator

Checklist

Folder Links
```

Calculator section may initially show:

```text
No Calculator configured.
```

until Calculator module is connected.

---

# 179. Order Header

Display prominently:

```text
Order Business ID

Order Name

Project

Client

Status

Priority
```

---

# 180. General Section

Contains:

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

subject to permissions.

---

# 181. Project Context

Display:

```text
Project Business ID

Project Name
```

with navigation to Project Workspace.

---

# 182. Client Context

Display:

```text
Client Business ID

Client Name
```

with navigation to Client.

Derived through Project.

---

# 183. Inherited Team Section

Display Project Team as read-only context:

```text
Owner

Assignee

Participants

Observers
```

---

# 184. Team Editing

Do not provide Order-level Team editing.

Provide:

```text
Open Project
```

if User needs to manage Team.

---

# 185. Financial Display

Show Selling Price only if:

```text
orders.view_selling_price
```

Show Cost Price only if:

```text
orders.view_cost_price
```

Show Profit only when both are available.

---

# 186. Calculator Section Before Calculator Module

Display an appropriate state:

```text
No Calculator available for this Order Type.
```

or:

```text
Calculator module not configured.
```

Do not fake spreadsheet functionality.

---

# 187. Task 22 — Checklist UI

Inside Order Workspace:

```text
Checklist
```

---

# 188. Checklist Example

```text
☑ Receive artwork

☑ Print test sample

☐ Customer approval

☐ Production
```

---

# 189. Add Item

Show:

```text
+ Add Item
```

with permission:

```text
orders.manage_checklist
```

---

# 190. Completion Interaction

Checkbox directly changes:

```text
isCompleted
```

---

# 191. Editing Text

Keep interaction simple.

Inline edit is acceptable.

---

# 192. Delete Item

Use a small delete action.

A heavy confirmation dialog is optional because Checklist Items are lightweight.

---

# 193. Checklist Progress

Display:

```text
3 / 5 completed
```

if useful.

---

# 194. Empty Checklist

Display:

```text
No Checklist Items yet.
```

---

# 195. Task 23 — Folder Links UI

Inside Order Workspace:

```text
Folder Links
```

---

# 196. Folder Link Display

Example:

```text
Artwork

\\server\orders\ORD-2026-000125\artwork

[Copy Path]
```

---

# 197. Add Folder Link

Fields:

```text
Name

Path
```

---

# 198. Copy Path

Browser-safe:

```text
Copy Path
```

is recommended.

---

# 199. No Open Explorer Button

Do not implement direct Windows Explorer launching in Version 1.

---

# 200. Empty Folder Links

Display:

```text
No Folder Links yet.
```

---

# 201. Task 24 — Unit Tests

Useful Unit Tests include:

```text
Order Business ID formatting

Year boundary logic

Status parsing

Priority parsing

Checklist progress calculation
```

---

# 202. Business ID Formatting Tests

Example:

```text
year = 2026
sequence = 1
→ ORD-2026-000001
```

---

# 203. Year Reset Test

Verify controlled clock behavior across year boundary.

---

# 204. Task 25 — Order Type Tests

Required:

```text
Create Order Type

Duplicate case-insensitive name rejected

Deactivate Order Type

Reactivate Order Type

Inactive Order Type remains retrievable
```

---

# 205. Task 26 — Order Creation Tests

Required:

```text
Create Order in active Project

Business ID generated

Status Draft

Priority Normal

Selling Price 0

Cost Price 0

Audit set
```

---

# 206. Closed Project Tests

Attempt creation under:

```text
Completed Project
```

→ rejected.

Attempt creation under:

```text
Cancelled Project
```

→ rejected.

---

# 207. Inactive Order Type Test

Attempt create with inactive Order Type:

```text
→ rejected
```

---

# 208. Missing Relationships

Unknown:

```text
projectId
```

or:

```text
orderTypeId
```

must be rejected.

---

# 209. Order Business ID Concurrency Test

Concurrent Order creation:

```text
No duplicates
```

---

# 210. Project Change Tests

Draft Order:

```text
Project change allowed
```

Non-Draft Order:

```text
Project change rejected
```

according to approved rule.

---

# 211. Client Derivation Test

Given:

```text
Order → Project A → Client X
```

Order API must show:

```text
Client X
```

without storing Client on Order.

---

# 212. Team Derivation Test

Given Project Team:

```text
Owner A

Assignee B
```

Order detail must reflect them without Order Team storage.

---

# 213. Checklist Tests

Required:

```text
Add Item

Edit text

Complete

Uncomplete

Reorder

Delete

Empty Checklist valid
```

---

# 214. Checklist Simplicity Schema Test

Verify database table has no:

```text
employee_id

deadline

priority

filename
```

---

# 215. Folder Link Tests

Required:

```text
Add Link

Edit Link

Reorder

Delete Link
```

---

# 216. Folder Link Filesystem Test

Deleting Folder Link must not invoke filesystem delete behavior.

---

# 217. Status Tests

All supported statuses should serialize/validate correctly.

Unknown values rejected.

---

# 218. Priority Tests

All supported priorities should serialize/validate correctly.

Unknown values rejected.

---

# 219. Financial Permission Tests

User with only:

```text
orders.view
```

must not automatically receive:

```text
sellingPrice

costPrice

profit
```

---

# 220. Selling-Only Permission Test

User with:

```text
orders.view_selling_price
```

may see Selling Price but not Cost Price.

Profit should remain hidden.

---

# 221. Cost-Only Permission Test

User with:

```text
orders.view_cost_price
```

may see Cost Price but not Selling Price.

Profit remains hidden.

---

# 222. Both Financial Permissions

User with both may receive:

```text
sellingPrice

costPrice

profit
```

where the API exposes Profit.

---

# 223. Permission Tests

Test:

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

---

# 224. Unauthenticated Tests

Protected endpoints:

```text
401
```

when no valid Session.

---

# 225. Parent-Child Security Tests

Attempt:

```text
/orders/A/checklist-items/item-from-B
```

must be rejected.

Same for Folder Links.

---

# 226. Pagination Tests

Orders list should follow shared pagination conventions.

---

# 227. Filter Tests

Test combinations:

```text
Client + Status

Project + Priority

Order Type + Status

Owner + Deadline
```

---

# 228. Search Tests

Verify Business ID search such as:

```text
ORD-2026-000425
```

works efficiently.

---

# 229. Query Efficiency Review

Orders List is a high-value query.

Review for N+1 access across:

```text
Project

Client

Order Type

Owner

Assignee
```

---

# 230. Migration Test

Apply:

```text
Authentication

Employees

Clients

Projects

Orders
```

migrations to clean PostgreSQL.

Verify cross-schema FKs.

---

# 231. Orders Implementation Commits

Recommended:

```text
feat(orders): add order types and order schema

feat(numbering): add yearly order numbering

feat(orders): add order CRUD

feat(orders): add status workflow

feat(orders): add checklist items

feat(orders): add folder links

test(orders): add order integration tests

feat(frontend): add order type administration

feat(frontend): add orders list

feat(frontend): add order workspace
```

---

# 232. First AI Coding Task

Recommended:

```text
Read:
- AI_RULES.md
- docs/03_Database_Design.md
- docs/05_Numbering_System.md
- docs/14_Orders_Module.md
- docs/17_Database_Schema_Overview.md
- docs/20_Testing_Strategy.md
- docs/24_Backend_Architecture.md
- docs/25_Development_Workflow_for_AI.md
- docs/31_Orders_Implementation_Plan.md

Task:
Implement only the Orders database model and Order numbering.

Create:
- OrderStatus
- OrderPriority
- OrderType entity
- Order entity
- ChecklistItem entity
- FolderLink entity
- EF Core configurations
- orders schema
- ORD-YYYY-000001 numbering
- migration
- database and numbering tests

Do not implement:
- Order API
- frontend
- Calculator tables
- Cost Items
- Order Team
- Client duplication
- Checklist assignment
- Explorer integration

Important:
If calculator.templates does not yet exist, do not create fake Calculator tables.
Delay the physical calculator_template_id FK if necessary and document the migration dependency.

Before finishing:
- build
- apply migrations to clean PostgreSQL
- test yearly numbering
- test concurrent Order creation numbering
- review migration
```

---

# 233. Second AI Coding Task

```text
Implement Order Type backend:
- list
- detail
- create
- edit
- activate
- deactivate

Use orders.manage_types.
Do not implement Calculator Template assignment yet unless Calculator base tables already exist.
```

---

# 234. Third AI Coding Task

```text
Implement Order list, detail, create, and edit backend.

Requirements:
- Project required
- Order Type required
- Project-derived Client
- Project-derived Team
- Completed/Cancelled Project cannot receive new Orders
- inactive Order Type rejected for new Order
- Draft/Normal defaults
- financial permissions respected

Do not implement Calculator logic.
```

---

# 235. Fourth AI Coding Task

```text
Implement Order status-change behavior and tests.

Keep workflow simple.
Warn at UI level for incomplete Checklist when completing.
Do not create status-history table.
```

---

# 236. Fifth AI Coding Task

```text
Implement simple Checklist Items backend.

Fields only:
- id
- order_id
- text
- is_completed
- sort_order

Do not add assignment, deadline, priority, notes, folder path, or filename.
```

---

# 237. Sixth AI Coding Task

```text
Implement Folder Links backend.

Fields:
- id
- order_id
- name
- path
- sort_order

Do not access the filesystem.
Do not implement Explorer integration.
```

---

# 238. Seventh AI Coding Task

```text
Implement Order Types Administration frontend using existing Administration patterns.
```

---

# 239. Eighth AI Coding Task

```text
Implement Orders List frontend with:
- search
- filters
- pagination
- financial permission-aware columns
- create Order
```

---

# 240. Ninth AI Coding Task

```text
Implement Order Workspace:
- Header
- General
- inherited Project/Client context
- inherited Project Team
- Checklist
- Folder Links
- Calculator placeholder
```

---

# 241. Orders Completion Gate

Do not begin Calculator implementation until:

```text
Order Type schema works

Order schema works

Yearly Order numbering works

Concurrency tested

Order CRUD works

Project restrictions work

Order Type restrictions work

Status works

Priority works

Client context derives through Project

Project Team derives through Project

Checklist works

Folder Links work

Financial field permissions work

Order Types Administration works

Orders List works

Order Workspace works

Integration tests pass
```

---

# 242. Calculator Readiness

Before Calculator begins, every Order must have stable:

```text
id

businessId

projectId

orderTypeId

sellingPrice

costPrice
```

and Order Type must be ready to receive:

```text
calculatorTemplateId
```

relationship.

---

# 243. No Manual Calculator Data Yet

Do not create:

```text
calculator JSON

formula values

cost item arrays
```

inside Orders tables.

Calculator owns those structures.

---

# 244. No Cost Items Yet

Do not add:

```text
orders.cost_items
```

Costs belong to:

```text
calculator.cost_items
```

in the next module.

---

# 245. No Supplier Table

Supplier in Cost Items will remain simple text in Version 1.

Do not create supplier infrastructure during Orders.

---

# 246. No Stored Profit

Do not add a Profit field during Orders implementation.

---

# 247. No Order-Level Employees

Do not create:

```text
owner_employee_id

assignee_employee_id

participant IDs

observer IDs
```

inside Orders.

---

# 248. No Checklist Templates

Checklist is manually created per Order.

Do not auto-generate Checklist based on Order Type.

---

# 249. No Checklist Workflow Engine

Checklist Items are simple checkbox rows.

Do not create dependencies/status machines between Items.

---

# 250. No Folder Automation

Do not implement:

```text
automatic network folder creation

folder deletion

file synchronization

folder scanning
```

---

# 251. Preview Image Scope

One Order:

```text
maximum one current Preview Image reference
```

No image gallery per Order is required.

---

# 252. Historical Order Type Rule

When Order Type is deactivated:

```text
Existing Orders remain fully readable.
```

---

# 253. Historical Project Rule

Order remains related to its Project even if Project later becomes Completed or Cancelled.

---

# 254. Project Team Change Behavior

If Project Owner changes:

```text
Order immediately shows new Project Owner
```

because Team is inherited dynamically.

There is no copied historical Order Team.

---

# 255. Client Change Behavior

If Project Client is changed while allowed:

```text
Orders inside Project immediately derive the new Client.
```

This is why changing Client on a Project containing Orders should later require a warning.

---

# 256. Order Financial Meaning

`selling_price` represents the stored commercial price of that Order.

`cost_price` represents the stored operational Cost Item total.

These values support:

```text
Order Reports

Project Financial Summary

Client Financial Summary
```

---

# 257. Order Profit Meaning

Derived Order Profit is:

```text
selling_price - cost_price
```

This is operational Order profit.

It is not Lithograph's full accounting net profit.

---

# 258. Future Order Extensions

Possible future additions:

```text
Comments

Attachments

Status history

Production scheduling

Machine assignment

Customer approval workflow

Automatic folder creation

Order-specific team override
```

None belong to Version 1.

---

# 259. Order Simplicity Rule

Before adding an Order feature, ask:

```text
Is this necessary to describe,
price,
cost,
track,
or complete
the current production work?
```

If not, defer it.

---

# 260. Final Orders Principle

The Orders module should answer:

```text
What exactly are we producing or doing?

For which Project and Client?

What type of work is it?

What is its status and priority?

What remains to be completed?

Where are its production folders?

What is its selling price?

What is its cost?
```

while Team responsibility remains inherited from Project.

The central rule is:

```text
Project provides context.

Order represents the work.

Calculator determines the numbers.
```

---

**End of Document**