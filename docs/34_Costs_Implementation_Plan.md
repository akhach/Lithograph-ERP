# Lithograph ERP

**Document:** 34_Costs_Implementation_Plan.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Calculator / Costs

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `16_Reports_Module.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `22_Logging_Audit_and_Operational_History.md`
- `23_Frontend_Architecture.md`
- `24_Backend_Architecture.md`
- `25_Development_Workflow_for_AI.md`
- `31_Orders_Implementation_Plan.md`
- `33_Order_Calculator_Implementation_Plan.md`

---

# 1. Purpose

This document defines implementation of Order Cost Items.

Cost Items provide the authoritative source for:

```text
Order Cost Price
```

The core invariant is:

```text
orders.orders.cost_price
=
SUM(calculator.cost_items.amount)
```

This phase must be complete before financial Reports are implemented.

---

# 2. Main Goal

After this phase, Lithograph ERP must be able to:

```text
Add Cost Items to an Order

Edit Cost Items

Delete Cost Items

List Cost Items

Calculate Cost Total

Synchronize Order Cost Price

Protect Cost data with permissions

Display Costs inside Order Calculator Workspace

Preserve Costs during Calculator reset

Provide reliable financial data for Reports
```

---

# 3. Cost Ownership

Costs belong to the Calculator domain because they participate in Order pricing and financial calculation.

Database table:

```text
calculator.cost_items
```

---

# 4. Cost Item Is Relational Data

Do not store Cost Items inside:

```text
calculator.order_calculators.field_values
```

Cost Items are normal relational business records.

---

# 5. Version 1 Cost Item Fields

Implement:

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

# 6. Fields Not Included

Do not add:

```text
supplier_id

cost_category_id

currency_id

invoice_id

purchase_order_id

tax_amount

vat_amount

payment_status

quantity

unit_price

attachment_id
```

unless a later module explicitly requires them.

---

# 7. Cost Item Meaning

A Cost Item represents one expense attributed to an Order.

Examples:

```text
Material

Outsource

Transport

Installation

Finishing

Subcontractor

Other direct expense
```

---

# 8. Category

`category` is required free text.

Recommended maximum length:

```text
150
```

Examples:

```text
Material

Outsource

Transport

Installation
```

---

# 9. No Cost Category Table

Version 1 does not create:

```text
calculator.cost_categories
```

Category remains text.

A configurable category master may be added later if real usage requires it.

---

# 10. Supplier

`supplier` is optional free text.

Recommended maximum length:

```text
200
```

---

# 11. No Supplier Table

Version 1 does not create:

```text
suppliers.suppliers
```

or any Procurement module.

Supplier remains text.

---

# 12. Expense Date

`expense_date` is optional.

Use a date-only type.

---

# 13. Description

`description` is optional free text.

Use it for useful context such as:

```text
3 mm acrylic sheet

Installation transport

External CNC cutting
```

---

# 14. Amount

`amount` is required.

Use:

```text
numeric(18,2)
```

in PostgreSQL and:

```text
decimal
```

in C#.

---

# 15. Amount Rule

Normal Cost Item amount must be:

```text
>= 0
```

Negative Cost Items are not required in Version 1.

If refunds/credits become necessary later, design them explicitly.

---

# 16. Zero Amount

A zero amount may technically be accepted, but it normally has little business value.

Recommended validation:

```text
amount > 0
```

for newly created Cost Items.

---

# 17. Sort Order

`sort_order` controls display order inside the Order Costs table.

It has no financial meaning.

---

# 18. Cost Price

Order Cost Price is stored on:

```text
orders.orders.cost_price
```

for fast reporting and historical business use.

It is derived from Cost Items.

---

# 19. Cost Price Invariant

At all times after a successful Cost mutation:

```text
Order.cost_price
=
SUM(Order Cost Items)
```

If the Order has no Cost Items:

```text
cost_price = 0
```

---

# 20. Do Not Manually Edit Cost Price

Normal Order API must not allow:

```text
costPrice
```

to be directly set.

Cost Item operations are the only normal authority.

---

# 21. Profit

Order operational Profit remains:

```text
selling_price - cost_price
```

Profit is not stored.

---

# 22. Recommended Implementation Sequence

```text
Task 1
Cost Item entity

Task 2
EF Core configuration

Task 3
Migration

Task 4
Cost permissions

Task 5
Cost query backend

Task 6
Add Cost Item

Task 7
Edit Cost Item

Task 8
Delete Cost Item

Task 9
Cost Price synchronization service

Task 10
Cost reorder

Task 11
Order Calculator integration

Task 12
Frontend Costs UI

Task 13
Financial visibility

Task 14
Reset preservation

Task 15
Tests

Task 16
Financial invariant review
```

---

# 23. Task 1 — Cost Item Entity

Create:

```text
CostItem
```

mapped to:

```text
calculator.cost_items
```

---

# 24. Cost Item Primary Key

Use:

```text
uuid
```

for `id`.

---

# 25. Order Relationship

Required:

```text
order_id
→ orders.orders.id
```

Every Cost Item belongs to exactly one Order.

---

# 26. Audit Fields

Cost Items are financially relevant.

Store:

```text
created_at

created_by

updated_at

updated_by
```

---

# 27. Audit User

Audit User references:

```text
auth.users.id
```

not Employee.

---

# 28. No Soft Delete

Version 1 does not require:

```text
is_deleted
```

for Cost Items.

Physical row deletion is allowed.

Because deletion affects financial history, frontend must use confirmation.

---

# 29. Task 2 — EF Core Configuration

Configure:

```text
schema = calculator

table = cost_items
```

---

# 30. Required Configuration

Configure:

```text
PK(id)

FK(order_id)

category required

supplier optional

expense_date optional

description optional

amount numeric(18,2)

sort_order

audit FKs
```

---

# 31. Amount Database Protection

Where practical, add a database check constraint:

```text
amount >= 0
```

or the stricter chosen rule.

Application validation still provides clearer user-facing errors.

---

# 32. Order Delete Behavior

Normal Order deletion is not supported.

Do not use uncontrolled cascading behavior that could silently destroy Costs.

---

# 33. Audit Delete Behavior

Audit User deletion must not cascade-delete Cost Items.

Use safe nullable audit references if needed.

---

# 34. Task 3 — Migration

Recommended migration name:

```text
AddOrderCostItems
```

---

# 35. Migration Scope

Create only:

```text
calculator.cost_items
```

plus required indexes/constraints.

---

# 36. Useful Indexes

At minimum consider:

```text
order_id
```

because Cost Items are normally queried per Order.

Possible reporting indexes can be added later based on real query needs.

---

# 37. Migration Review

Verify:

```text
UUID PK

Order FK

Category required

Supplier optional

Expense Date optional

Amount precision

Sort Order

Audit fields

No Supplier table

No Cost Category table
```

---

# 38. Task 4 — Cost Permissions

Register:

```text
calculator.view_costs

calculator.edit_costs
```

---

# 39. View Costs Permission

Required to receive:

```text
Cost Items

Cost totals

Cost-sensitive Calculator fields
```

where applicable.

---

# 40. Edit Costs Permission

Required for:

```text
Create Cost Item

Edit Cost Item

Delete Cost Item

Reorder Cost Items
```

---

# 41. Edit Does Not Automatically Imply View

In practice Users with:

```text
calculator.edit_costs
```

should also receive:

```text
calculator.view_costs
```

through Role configuration.

Do not rely on this assumption for backend security.

Each endpoint must enforce its required permission.

---

# 42. Order Cost Price Permission

Order-level Cost Price remains protected by:

```text
orders.view_cost_price
```

according to Orders permissions.

---

# 43. Permission Relationship

Detailed Costs require:

```text
calculator.view_costs
```

Order Cost Price display requires:

```text
orders.view_cost_price
```

These are separate capabilities.

---

# 44. Example

A User may be allowed to see:

```text
Order Cost Price = 250,000 AMD
```

without seeing every individual Supplier/Cost Item.

That is a valid permission configuration.

---

# 45. Task 5 — Cost List Endpoint

Implement:

```text
GET /api/orders/{orderId}/cost-items
```

Requires:

```text
calculator.view_costs
```

and normal access to the Order.

---

# 46. Cost Item DTO

Recommended fields:

```text
id

category

supplier

expenseDate

description

amount

sortOrder

createdAt

updatedAt
```

Audit User summaries may be included where useful.

---

# 47. Cost Response Summary

The response may also include:

```text
totalCost
```

calculated from the full Order Cost Item set.

---

# 48. Total Authority

`totalCost` should agree with:

```text
orders.orders.cost_price
```

If not, this indicates an invariant violation.

---

# 49. Cost Ordering

Default:

```text
sort_order ASC
```

then a stable secondary order such as creation time or ID.

---

# 50. Task 6 — Add Cost Item

Implement:

```text
POST /api/orders/{orderId}/cost-items
```

Requires:

```text
calculator.edit_costs
```

---

# 51. Create Request

Fields:

```text
category

supplier?

expenseDate?

description?

amount
```

Optional:

```text
sortOrder
```

if needed.

---

# 52. Create Validation

Validate:

```text
Order exists

Category required

Amount valid

String lengths

Expense date valid
```

---

# 53. Default Sort Order

If no explicit position is supplied:

```text
append to end
```

using a simple next sort value.

---

# 54. Add Cost Transaction

Conceptual flow:

```text
Authenticate

Authorize

Load Order

Validate request

Begin transaction

Insert Cost Item

Calculate SUM(cost_items.amount)

Update Order.cost_price

Update Order audit

Commit
```

---

# 55. Atomicity

Cost Item creation and Cost Price update must be one transaction.

Never allow:

```text
Cost Item saved
but
Order Cost Price unchanged
```

---

# 56. Task 7 — Edit Cost Item

Implement:

```text
PATCH /api/orders/{orderId}/cost-items/{costItemId}
```

Requires:

```text
calculator.edit_costs
```

---

# 57. Editable Fields

Allow:

```text
category

supplier

expenseDate

description

amount
```

Sort order may use separate reorder operation.

---

# 58. Parent Validation

Verify:

```text
costItem.order_id == route orderId
```

Never update a Cost Item through the wrong Order route.

---

# 59. Edit Transaction

If `amount` changes:

```text
Update Cost Item

Recalculate Order Cost Price

Update Order audit
```

in the same transaction.

---

# 60. Non-Amount Edit

Even if only Supplier/Description changes:

```text
Cost Item audit
```

updates.

Order `cost_price` itself does not change.

Order audit does not have to update unless Order financial value changes.

---

# 61. Recommended Simplicity

It is acceptable to recalculate the Cost total after every Cost Item edit even if amount did not change.

However, avoid unnecessary Order audit changes if the total remains the same.

---

# 62. Task 8 — Delete Cost Item

Implement:

```text
DELETE /api/orders/{orderId}/cost-items/{costItemId}
```

Requires:

```text
calculator.edit_costs
```

---

# 63. Delete Confirmation

Frontend should clearly confirm:

```text
Delete this Cost Item?

Order Cost Price will be recalculated.
```

---

# 64. Delete Transaction

Conceptual:

```text
Validate parent

Begin transaction

Delete Cost Item

Recalculate SUM

Update Order Cost Price

Update Order audit

Commit
```

---

# 65. Last Cost Item

If the deleted row was the final Cost Item:

```text
Order.cost_price = 0
```

---

# 66. Deletion History Limitation

Version 1 does not maintain deleted Cost Item history.

This is an accepted limitation.

Technical logs are not a substitute for business audit history.

---

# 67. Future History

If Cost deletion history becomes important later, design an explicit financial history/audit feature.

Do not silently introduce generic audit logging now.

---

# 68. Task 9 — Cost Price Synchronization Component

Use one centralized backend mechanism for recalculating Order Cost Price.

Conceptually:

```text
IOrderCostUpdater
```

or equivalent.

---

# 69. Synchronization Logic

Authoritative calculation:

```text
SUM(amount)
WHERE order_id = target Order
```

---

# 70. No Client-Supplied Total

Do not accept:

```text
totalCost
```

from frontend.

---

# 71. No Incremental Arithmetic as Sole Authority

Avoid relying only on:

```text
old total + new item amount
```

because edits/deletes/concurrency can cause drift.

Preferred safe approach:

```text
recalculate SUM from current rows
```

inside transaction.

---

# 72. Why Recalculate SUM

Typical Cost Item counts per Order are small.

Correctness is more valuable than premature optimization.

---

# 73. Cost Price Synchronization Invariant

After any successful Cost Item mutation:

```text
SELECT SUM(cost_items.amount)
=
orders.orders.cost_price
```

---

# 74. Task 10 — Reorder Costs

If UI needs reordering, implement a simple endpoint.

Conceptually:

```text
POST /api/orders/{orderId}/cost-items/reorder
```

---

# 75. Reorder Permission

Requires:

```text
calculator.edit_costs
```

---

# 76. Reorder Behavior

Update only:

```text
sort_order
```

No Cost Price recalculation is technically necessary.

---

# 77. No Financial Meaning

Changing row order must not change totals.

---

# 78. Task 11 — Order Calculator Integration

Costs appear inside the Order Calculator workspace.

Recommended conceptual structure:

```text
Calculator

├── Pricing Inputs
├── Calculated Values
├── Selling Price
└── Costs
```

---

# 79. Costs Are Independent of Template Version

Cost Items belong to:

```text
Order
```

not:

```text
Template Version
```

Changing Calculator Version must not automatically delete Costs.

---

# 80. Template Without Cost Fields

An Order can still have Cost Items even if its Calculator Template contains no cost-related fields.

---

# 81. Order Without Calculator

An Order Type may have no Calculator Template.

The Order may still have Cost Items.

Costs must not depend on existence of:

```text
calculator.order_calculators
```

---

# 82. Critical Rule

Valid:

```text
Order
├── no Calculator
└── several Cost Items
```

Cost tracking is still allowed.

---

# 83. Task 12 — Frontend Costs UI

Inside Order Workspace Calculator section, add a Costs area or sub-tab.

Recommended:

```text
Calculator
├── Pricing
└── Costs
```

or an equivalent clear layout.

---

# 84. Cost Table Columns

Recommended:

```text
Category

Supplier

Date

Description

Amount
```

---

# 85. Amount Column

Use shared money formatting.

---

# 86. Cost Total

Display clearly below/above table:

```text
Total Cost: 250,000 AMD
```

subject to permission.

---

# 87. Add Cost Button

Visible only with:

```text
calculator.edit_costs
```

---

# 88. Add Cost Form

Fields:

```text
Category *

Supplier

Date

Description

Amount *
```

---

# 89. Supplier Input

Use simple text input.

Do not build Supplier autocomplete against a nonexistent Supplier module.

---

# 90. Category Input

Use simple text input.

A dropdown of common recently used values may be considered later, but no category master is required.

---

# 91. Cost Editing

Allow inline or dialog editing.

Use existing UI conventions.

---

# 92. Cost Delete

Use clear delete action with confirmation.

---

# 93. Empty State

If no Costs:

```text
No Cost Items yet.
```

Authorized users may see:

```text
+ Add Cost
```

---

# 94. Loading State

Do not render an empty table while Cost API is still loading.

---

# 95. Error State

Cost operation failures must be visible near the Costs section.

---

# 96. Task 13 — Financial Visibility

Backend must enforce Cost visibility.

---

# 97. Without calculator.view_costs

User must not receive:

```text
Cost Item rows

Supplier names

Cost descriptions

Individual amounts
```

---

# 98. Order Cost Price

A User without:

```text
calculator.view_costs
```

could still theoretically see aggregate `cost_price` if they have:

```text
orders.view_cost_price
```

This separation is intentional.

---

# 99. Calculator Cost Fields

Template fields marked:

```text
visibility = cost
```

require cost visibility according to Calculator rules.

---

# 100. Reports

Later Reports must apply the same permissions.

Reports must not become a bypass to detailed Cost data.

---

# 101. Frontend Security

Do not fetch Cost Items and merely hide the table.

Unauthorized Cost data must not be returned by backend.

---

# 102. Task 14 — Calculator Reset Preservation

Calculator reset must preserve:

```text
calculator.cost_items
```

---

# 103. Reset Scenario

Before reset:

```text
Order
├── Calculator v1
│   └── field values
├── Cost Item A
└── Cost Item B
```

After Calculator reset:

```text
Order
├── Calculator reset/rebound
├── Cost Item A
└── Cost Item B
```

---

# 104. Cost Price After Reset

Cost Price remains:

```text
SUM(existing Cost Items)
```

---

# 105. Selling Price After Reset

Per Calculator rules:

```text
selling_price = 0
```

until a new valid Calculator result is saved.

---

# 106. Cost Price Must Not Reset

Do not do:

```text
cost_price = 0
```

during Calculator reset unless Cost Items genuinely total zero.

---

# 107. Order Type Change

Changing Order Type through explicit Calculator reset workflow also preserves Cost Items.

---

# 108. Why Preserve Costs

Costs represent actual expenses on the Order.

They are not merely Calculator inputs.

---

# 109. Task 15 — Concurrency

Cost mutations can occur concurrently.

The database transaction must preserve correct Cost total.

---

# 110. Concurrent Add Example

Two Users add Cost Items simultaneously.

After both successful commits:

```text
Order.cost_price
=
amount A + amount B + previous Costs
```

---

# 111. Avoid Lost Total Update

Do not calculate total outside the protected transaction and then overwrite another transaction's newer total.

---

# 112. Transaction Isolation / Locking

Implementation should use a safe PostgreSQL/EF strategy such as:

```text
transaction
+
Order row locking / serialization where needed
+
fresh SUM
```

The exact mechanism should be tested under concurrency.

---

# 113. Simplicity Preference

Because Cost operations are small and relatively infrequent, prefer correctness over highly optimized lock-free behavior.

---

# 114. Task 16 — Cost Creation Tests

Required:

```text
Add first Cost Item

Order Cost Price becomes item amount

Audit fields set

Cost row persisted
```

---

# 115. Multiple Cost Items Test

Example:

```text
100,000

50,000

25,000
```

must produce:

```text
cost_price = 175,000
```

---

# 116. Cost Edit Test

Change:

```text
50,000
→
70,000
```

new Cost Price must update correctly.

---

# 117. Cost Delete Test

Delete one Cost Item.

Order Cost Price decreases correctly.

---

# 118. Delete Last Item Test

Deleting final Cost Item results in:

```text
cost_price = 0
```

---

# 119. Non-Financial Edit Test

Change Supplier or Description only.

Cost Price remains unchanged.

---

# 120. Cost Amount Validation Tests

Reject:

```text
negative amount

invalid numeric value
```

according to request validation.

---

# 121. Decimal Precision Test

Test values such as:

```text
10.10

20.25
```

Result must be exact:

```text
30.35
```

Do not allow binary floating-point drift.

---

# 122. Large Value Test

Test amounts within:

```text
numeric(18,2)
```

limits.

---

# 123. Parent Validation Test

Given Cost Item belonging to Order A:

```text
PATCH /orders/B/cost-items/{itemA}
```

must be rejected.

---

# 124. Permission Tests

Test:

```text
calculator.view_costs

calculator.edit_costs
```

independently.

---

# 125. Unauthorized View Test

User without `calculator.view_costs`:

```text
GET Cost Items
→ 403
```

or protected omission according to endpoint design.

Dedicated Cost endpoint should normally return 403.

---

# 126. Unauthorized Edit Test

User with view but without edit:

```text
can GET
cannot POST/PATCH/DELETE
```

---

# 127. Aggregate Permission Test

A User with:

```text
orders.view_cost_price
```

but without:

```text
calculator.view_costs
```

may see:

```text
Order Cost Price
```

but not Cost Item details.

---

# 128. Audit Tests

Verify Cost Item:

```text
created_at

created_by

updated_at

updated_by
```

behavior.

---

# 129. Order Audit Test

When Cost Price changes, verify:

```text
Order.updated_at

Order.updated_by
```

updates.

---

# 130. Non-Amount Edit Audit Test

If Supplier changes but Cost total does not:

```text
Cost Item audit updates
```

Order audit need not update.

---

# 131. Read Audit Test

Reading Cost Items must not mutate audit timestamps.

---

# 132. Transaction Rollback Test

Simulate failure after Cost Item mutation but before Order Cost Price update.

Expected:

```text
entire transaction rolls back
```

---

# 133. Concurrent Add Test

Two simultaneous additions must produce:

```text
two Cost Items

correct total
```

---

# 134. Concurrent Edit/Delete Test

Test one User editing while another deletes where practical.

System must either:

```text
serialize safely
```

or return a controlled conflict/not-found response.

Cost Price must remain correct.

---

# 135. Invariant Verification Test

After a series of random-ish create/edit/delete operations:

```text
Order.cost_price
==
SUM(cost_items.amount)
```

This is one of the most important Cost tests.

---

# 136. Calculator Reset Preservation Test

Scenario:

```text
Add Cost A

Add Cost B

Save Calculator

Reset Calculator

Verify:
Cost A still exists
Cost B still exists
Cost Price unchanged
Selling Price reset according to Calculator rule
```

Mandatory.

---

# 137. Order Type Change Preservation Test

Change Order Type through explicit reset workflow.

Verify Cost Items remain unchanged.

---

# 138. No Calculator Test

Order with no Order Calculator:

```text
Add Cost Item
```

must succeed.

---

# 139. Cancelled Order Cost History

Existing Costs remain visible on a Cancelled Order for authorized Users.

Do not automatically delete Costs.

Whether new Cost Items may be added to Cancelled Orders can remain restricted by application workflow.

---

# 140. Recommended Cancelled Order Rule

For Version 1:

```text
Do not allow new Cost mutations on Cancelled Orders
```

unless a genuine correction workflow later requires it.

Historical Cost data remains visible.

---

# 141. Completed Order Cost Rule

A Completed Order may still require correction of Costs in real operations.

Recommended Version 1 behavior:

```text
Allow authorized Cost edits on Completed Orders
```

because late invoices/actual costs may arrive after production completion.

---

# 142. Why Completed Costs Remain Editable

Examples:

```text
late subcontractor invoice

transport charge

installation expense
```

may arrive after the Order is operationally completed.

---

# 143. Financial Reports Impact

Any later Cost correction immediately changes:

```text
Order Cost Price

Order Profit

Project Profit

Client Profit

Cost Reports
```

because Reports use current source data.

---

# 144. No Financial Snapshots

Version 1 does not freeze financial Reports at completion.

Reports show current stored financial state.

---

# 145. Future Accounting Boundary

Cost Items represent:

```text
direct Order operational expenses
```

They are not a complete accounting ledger.

---

# 146. Costs Not Included Automatically

Lithograph overhead such as:

```text
Office rent

Founder salaries

General electricity

Leasing

Taxes

Company-wide marketing
```

does not automatically appear in Order Cost Price.

---

# 147. Order Profit Meaning

Therefore:

```text
selling_price - cost_price
```

is:

```text
Order operational gross contribution
```

not full company accounting net profit.

---

# 148. Reports Must State This

Reports documentation/UI should avoid labeling this as company net profit.

---

# 149. Cost API Error Codes

Useful errors include:

```text
COST_ITEM_NOT_FOUND

COST_AMOUNT_INVALID

COST_MUTATION_NOT_ALLOWED

ORDER_CANCELLED

COST_PARENT_MISMATCH
```

Use the project's normal error conventions.

---

# 150. Do Not Expose Database Errors

Constraint names and SQL messages must not reach frontend.

---

# 151. Cost Query Performance

Typical Order will have a small number of Costs.

Simple indexed query by:

```text
order_id
```

is sufficient.

---

# 152. Report Query Preparation

Although per-Order Cost Item queries are simple, Reports later aggregate by:

```text
Order

Project

Client

Order Type

Category

Supplier

Date
```

Do not prematurely index every column.

Add indexes based on report query measurements.

---

# 153. Category Reporting

Because Category is text, Reports may group exact text values.

This means:

```text
Material
```

and:

```text
Materials
```

are separate groups.

This is an accepted Version 1 limitation.

---

# 154. Supplier Reporting

Likewise:

```text
ABC LLC
```

and:

```text
ABC
```

are separate Supplier text groups.

A Supplier master can solve this later if required.

---

# 155. UI Guidance

Users should be encouraged to use consistent Category/Supplier wording, but the system should not overcomplicate data entry.

---

# 156. Cost Item Commits

Recommended:

```text
feat(calculator): add cost item schema

feat(calculator): add cost item CRUD

feat(calculator): synchronize order cost price

feat(calculator): add cost permissions

feat(frontend): add order costs table

test(calculator): add cost synchronization tests

test(calculator): add cost concurrency tests
```

---

# 157. First AI Coding Task

Recommended:

```text
Read:
- AI_RULES.md
- docs/14_Orders_Module.md
- docs/15_Calculator_Module.md
- docs/17_Database_Schema_Overview.md
- docs/20_Testing_Strategy.md
- docs/22_Logging_Audit_and_Operational_History.md
- docs/24_Backend_Architecture.md
- docs/25_Development_Workflow_for_AI.md
- docs/34_Costs_Implementation_Plan.md

Task:
Implement only Cost Item persistence.

Create:
- CostItem entity
- EF Core configuration
- calculator.cost_items
- Order FK
- decimal amount
- category
- supplier
- expense_date
- description
- sort_order
- audit fields
- migration
- persistence integration tests

Do not implement:
- Cost API
- frontend
- Supplier table
- Cost Category table
- Reports
- direct Order Cost Price editing

Before finishing:
- build
- apply migration
- run persistence tests
- review migration
```

---

# 158. Second AI Coding Task

```text
Implement Cost Item query and create backend.

Requirements:
- calculator.view_costs
- calculator.edit_costs
- parent Order validation
- transaction
- recalculate SUM(cost_items.amount)
- synchronize Order.cost_price
- update Order audit
- tests
```

---

# 159. Third AI Coding Task

```text
Implement Cost Item edit and delete.

Requirements:
- parent-child validation
- transaction
- full Cost total recalculation
- correct zero total after final delete
- audit behavior
- confirmation-ready API errors
```

---

# 160. Fourth AI Coding Task

```text
Implement Cost concurrency protection and invariant tests.

Test simultaneous additions and verify Order.cost_price always equals SUM(cost_items.amount).
```

---

# 161. Fifth AI Coding Task

```text
Implement Costs UI inside Order Calculator Workspace.

Columns:
- Category
- Supplier
- Date
- Description
- Amount

Add:
- Create
- Edit
- Delete
- Total Cost
- loading/error/empty states

Respect calculator.view_costs and calculator.edit_costs.
```

---

# 162. Sixth AI Coding Task

```text
Integrate Calculator reset and Order Type change workflow with Costs.

Requirement:
Cost Items and Order Cost Price must remain unchanged when Calculator values/version are reset.
```

---

# 163. Costs Completion Gate

Do not begin Reports until:

```text
Cost Item schema works

Create works

Edit works

Delete works

Order Cost Price synchronizes

Last Cost deletion produces zero

Decimal arithmetic is exact

Concurrent Cost mutations preserve total

Cost permissions work

Detailed Costs are protected

Aggregate Cost Price permission remains separate

Costs UI works

Calculator reset preserves Costs

Order Type change preserves Costs

Order.cost_price = SUM(cost_items.amount) invariant passes
```

---

# 164. Mandatory Financial Invariant Test

Before Reports development begins, this must be proven repeatedly:

```text
For every Order:

orders.orders.cost_price
=
SUM(calculator.cost_items.amount)
```

with:

```text
0
```

when there are no Cost Items.

---

# 165. Mandatory Selling Invariant

Together with the Order Calculator phase, the system must now satisfy:

```text
Order.selling_price
=
authoritative current Calculator result
```

when Calculator state is valid.

---

# 166. Financial Foundation

At the end of this phase every Order has reliable:

```text
Selling Price

Cost Price

Derived Profit
```

---

# 167. Report Readiness

Reports can now derive:

```text
Order Profit

Project Selling Total

Project Cost Total

Project Profit

Client Selling Total

Client Cost Total

Client Profit

Order Type totals

Cost Category totals

Supplier text totals
```

without storing duplicate summary values.

---

# 168. No Project Financial Storage

Do not add:

```text
projects.selling_total

projects.cost_total

projects.profit
```

---

# 169. No Client Financial Storage

Do not add financial totals to Clients.

---

# 170. No Report Snapshot Tables

Reports should query source data.

---

# 171. No Cost Formula Replacement

Even if Calculator Template computes an estimated internal cost field:

```text
that is not Order.cost_price
```

unless a future explicit feature changes the business model.

---

# 172. Estimated vs Actual Cost

Future versions may distinguish:

```text
Estimated Cost

Actual Cost
```

Version 1 has one operational Cost Price based on Cost Items.

---

# 173. No Multi-Currency

All Cost Items use the company's single currency context.

Do not add currency conversion.

---

# 174. No Automatic VAT

Do not automatically add/remove VAT from Cost Items unless a later accounting requirement defines it.

Amounts are stored according to Lithograph's chosen operational data-entry convention.

---

# 175. No Purchasing Module

Adding a Cost Item does not:

```text
create Purchase Order

pay Supplier

update Inventory

generate invoice
```

---

# 176. No Supplier Portal

Supplier text is only descriptive data.

---

# 177. No Attachment Workflow

Cost Items do not require receipt/invoice file attachments in Version 1.

---

# 178. Future Cost Extensions

Possible future additions:

```text
Cost Category master

Supplier master

Invoice reference

Attachments

Estimated vs Actual Costs

Purchase Orders

Approval workflow

Tax/VAT treatment
```

Only add if real business workflow requires them.

---

# 179. Cost Simplicity Rule

Before adding Cost complexity, ask:

```text
Do we need this information
to understand the direct cost
of this Order today?
```

If not, defer it.

---

# 180. Final Costs Principle

The Costs feature should answer:

```text
What direct expenses belong to this Order?

Who supplied them?

When did they occur?

How much did they cost?

What is the Order's total Cost Price?
```

The central rule is:

```text
Cost Items are the source.

Order Cost Price is the synchronized total.

Reports read the result.
```

---

**End of Document**