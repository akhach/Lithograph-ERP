# Lithograph ERP

**Document:** 35_Reports_Implementation_Plan.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Reports

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `13_Projects_Module.md`
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
- `34_Costs_Implementation_Plan.md`

---

# 1. Purpose

This document defines implementation of the Reports module.

Reports provide read-only operational analysis based on existing source data.

Reports must not become a second source of truth.

---

# 2. Main Goal

After this phase, Lithograph ERP must provide:

```text
Orders Report

Project Financial Summary

Client Financial Summary

Order Type Financial Summary

Cost Summary
```

with:

```text
Server-side filters

Server-side aggregation

Pagination

Financial permissions

Drill-down to source records

Correct totals over the full filtered dataset
```

---

# 3. Reports Are Read-Only

Reports may:

```text
Read

Filter

Aggregate

Sort

Paginate

Export later
```

Reports must not:

```text
Modify Orders

Modify Projects

Modify Clients

Modify Costs

Modify Calculator data
```

---

# 4. Reports Own No Source Tables

Version 1 Reports does not create a dedicated reporting schema.

There is no:

```text
reports.orders_summary

reports.project_totals

reports.client_totals
```

Source tables remain authoritative.

---

# 5. Source Data

Reports read from:

```text
clients.clients

projects.projects

projects.project_members

orders.orders

orders.order_types

calculator.cost_items
```

and other required read-only relationships.

---

# 6. Financial Source of Truth

Selling Price:

```text
orders.orders.selling_price
```

Cost Price:

```text
orders.orders.cost_price
```

Profit:

```text
selling_price - cost_price
```

Profit is derived.

---

# 7. Operational Profit Meaning

Report Profit means:

```text
Order Selling Price
-
Order Cost Price
```

This is operational Order contribution/profit.

It is not complete accounting net profit.

---

# 8. Not Included Automatically

Reports do not automatically deduct:

```text
Office rent

Founder salaries

General electricity

Taxes

Leasing payments

General marketing

Other company overhead
```

unless such costs are explicitly represented through future accounting functionality.

---

# 9. Recommended Implementation Sequence

```text
Task 1
Report permissions

Task 2
Shared report filter models

Task 3
Shared financial projection

Task 4
Orders Report

Task 5
Project Financial Summary

Task 6
Client Financial Summary

Task 7
Order Type Financial Summary

Task 8
Cost Summary

Task 9
Cancelled-order behavior

Task 10
Financial permission filtering

Task 11
Report frontend foundation

Task 12
Individual report screens

Task 13
Drill-down

Task 14
Dashboard query reuse

Task 15
Testing

Task 16
Performance review
```

---

# 10. Task 1 — Report Permissions

Introduce a base Report permission if useful:

```text
reports.view
```

This controls access to the Reports area.

---

# 11. Financial Permissions Remain Separate

`reports.view` must not bypass:

```text
orders.view_selling_price

orders.view_cost_price

calculator.view_costs
```

Reports must respect the same financial restrictions as normal modules.

---

# 12. Example

A User with:

```text
reports.view
orders.view_selling_price
```

but without:

```text
orders.view_cost_price
```

may see Selling totals but must not see:

```text
Cost totals

Profit
```

---

# 13. Detailed Cost Report

Detailed Cost Item data requires:

```text
calculator.view_costs
```

---

# 14. Report Navigation

Main navigation:

```text
Reports
```

should appear only when User has the required Report access.

---

# 15. Task 2 — Shared Report Filters

Use consistent backend filter models.

Common filters may include:

```text
Date range

Client

Project

Order Type

Order Status

Order Priority

Project Owner

Project Assignee
```

Only expose filters relevant to each report.

---

# 16. Date Filter Source Must Be Explicit

Every report using a date range must clearly define which date is filtered.

Possible sources:

```text
Order created date

Order deadline

Project start date

Cost expense date
```

Do not use an ambiguous generic `date`.

---

# 17. Initial Financial Reports Date Basis

Recommended default for Order-based financial reports:

```text
Order created_at
```

unless a report explicitly says otherwise.

---

# 18. Cost Report Date Basis

Cost Summary date filtering should primarily use:

```text
calculator.cost_items.expense_date
```

where present.

If `expense_date` is null, behavior must be explicit.

---

# 19. Null Expense Date

Recommended:

```text
Date-range filter on expense_date
excludes Cost Items with null expense_date
```

unless an explicit:

```text
Include items without date
```

option is later added.

---

# 20. Filter DTOs

Use typed request/query models.

Avoid a completely generic dynamic filter language.

---

# 21. Search

Where useful, reports may support a general search across stable identifiers/names.

Example:

```text
Order Business ID

Project Business ID

Client Business ID

Names
```

---

# 22. Task 3 — Shared Financial Projection

Create reusable query logic for an Order financial projection.

Conceptually:

```text
OrderFinancialProjection

orderId
orderBusinessId
orderName

projectId
projectBusinessId
projectName

clientId
clientBusinessId
clientName

orderTypeId
orderTypeName

status
priority
deadline

sellingPrice
costPrice
profit
```

plus Owner/Assignee where required.

---

# 23. Do Not Load Full Entities

Reports should prefer direct query projections.

Avoid loading complete entity graphs and aggregating them in memory.

---

# 24. Server-Side Aggregation

Calculate financial totals in PostgreSQL through EF Core queries where practical.

Do not:

```text
load every Order into browser
then sum locally
```

---

# 25. Report Query Ownership

Report-specific queries belong in the Reports module.

They may read across module schemas.

This is an acceptable read-side cross-module dependency.

---

# 26. Cross-Module Writes

Reports must never use their cross-module access to write source entities.

---

# 27. Task 4 — Orders Report

Implement:

```text
GET /api/reports/orders
```

---

# 28. Orders Report Purpose

Provides detailed searchable/filterable Order financial and operational data.

---

# 29. Orders Report Filters

Recommended:

```text
fromDate

toDate

clientId

projectId

orderTypeId

status

priority

ownerEmployeeId

assigneeEmployeeId

search

includeCancelled
```

---

# 30. Orders Report Columns

Base columns:

```text
Order Business ID

Order Name

Client

Project

Order Type

Status

Priority

Deadline
```

Financial columns depend on permissions:

```text
Selling Price

Cost Price

Profit
```

---

# 31. Orders Report Pagination

Use standard:

```text
page

page_size
```

---

# 32. Orders Report Sorting

Allow a controlled whitelist such as:

```text
order_business_id

order_name

client_name

project_name

order_type

status

priority

deadline

created_at

selling_price

cost_price

profit
```

Protected financial sort fields must respect financial access.

---

# 33. Orders Report Summary

Response may include:

```text
totalOrders

sellingTotal?

costTotal?

profitTotal?
```

depending on permissions.

---

# 34. Full Dataset Totals

Summary totals must be calculated across:

```text
the entire filtered dataset
```

not only:

```text
the current page
```

This rule is mandatory.

---

# 35. Example

If filter matches:

```text
350 Orders
```

but page shows:

```text
50 Orders
```

summary totals must represent all:

```text
350
```

matching Orders.

---

# 36. Orders Report Response Structure

Conceptually:

```text
items

page

pageSize

totalItems

totalPages

summary
```

---

# 37. Task 5 — Project Financial Summary

Implement:

```text
GET /api/reports/projects
```

---

# 38. Project Report Purpose

Aggregate Order financial data by Project.

---

# 39. Project Report Row

Recommended:

```text
Project Business ID

Project Name

Client

Status

Owner

Assignee

Order Count

Selling Total

Cost Total

Profit
```

Financial columns are permission-dependent.

---

# 40. Project Order Count

Derived through:

```text
COUNT(orders)
```

Do not store it on Project.

---

# 41. Project Selling Total

Derived:

```text
SUM(order.selling_price)
```

over included Orders.

---

# 42. Project Cost Total

Derived:

```text
SUM(order.cost_price)
```

---

# 43. Project Profit

Derived:

```text
Selling Total - Cost Total
```

---

# 44. Project Report Filters

Useful:

```text
Date range

Client

Project Status

Owner

Assignee

Order Type

Order Status
```

---

# 45. Project Financial Filter Semantics

When an Order-level filter is applied, Project financial totals should aggregate only Orders matching that filter.

Example:

```text
Order Type = UV Printing
```

then Project totals represent only UV Printing Orders.

---

# 46. Project Row Inclusion

A Project should normally appear only if it matches the Project-level filters and contains Orders matching required Order-level filters.

If a report specifically needs zero-Order Projects later, add an explicit option.

---

# 47. Task 6 — Client Financial Summary

Implement:

```text
GET /api/reports/clients
```

---

# 48. Client Report Purpose

Aggregate Orders through:

```text
Client
→ Projects
→ Orders
```

---

# 49. Client Report Row

Recommended:

```text
Client Business ID

Client Name

Project Count

Order Count

Selling Total

Cost Total

Profit
```

subject to permissions.

---

# 50. Project Count

Project Count should follow explicit semantics.

Recommended:

```text
Count distinct Projects containing included Orders
```

when Order filters are active.

---

# 51. Client Filters

Useful:

```text
Date range

Client

Project Status

Order Type

Order Status

Owner

Assignee
```

---

# 52. Inactive Clients

Inactive Clients remain reportable historically.

Do not hide them merely because:

```text
is_active = false
```

---

# 53. Task 7 — Order Type Financial Summary

Implement:

```text
GET /api/reports/order-types
```

---

# 54. Purpose

Show financial performance grouped by configurable Order Type.

---

# 55. Row Fields

Recommended:

```text
Order Type

Order Count

Selling Total

Cost Total

Profit
```

---

# 56. Inactive Order Types

Inactive Order Types remain included historically when matching Orders exist.

---

# 57. Filters

Useful:

```text
Date range

Client

Project

Order Type

Order Status

Priority
```

---

# 58. Order Type Name History Limitation

Because Orders reference the Order Type entity directly, renaming an Order Type changes the displayed name in historical reports.

Version 1 accepts this.

If historical names later matter, design explicit snapshot/versioning.

---

# 59. Task 8 — Cost Summary

Implement:

```text
GET /api/reports/costs
```

Requires:

```text
reports.view
+
calculator.view_costs
```

---

# 60. Cost Summary Purpose

Analyze detailed Cost Items.

---

# 61. Cost Report Modes

Version 1 may support one endpoint with grouping options or a few explicit groupings.

Prefer explicit understandable grouping.

Initial useful groupings:

```text
Category

Supplier

Order

Project

Client

Order Type
```

---

# 62. Avoid Generic BI Query Language

Do not create:

```text
group_by=anything
columns=anything
formula=anything
```

generic report designer behavior.

Use controlled supported report shapes.

---

# 63. Cost Category Summary

Row:

```text
Category

Cost Item Count

Total Amount
```

---

# 64. Supplier Summary

Row:

```text
Supplier

Cost Item Count

Total Amount
```

Supplier may be null.

UI may display:

```text
No Supplier
```

---

# 65. Cost by Order

Row:

```text
Order Business ID

Order Name

Project

Client

Total Cost
```

---

# 66. Cost Report Filters

Recommended:

```text
fromExpenseDate

toExpenseDate

clientId

projectId

orderId

orderTypeId

category

supplier

orderStatus
```

---

# 67. Category/Supplier Filtering

Because both are free text, use controlled exact/contains search behavior.

Do not pretend they are normalized master entities.

---

# 68. Task 9 — Cancelled Order Behavior

Financial Reports require a consistent Cancelled-order policy.

---

# 69. Default Behavior

Recommended default:

```text
Cancelled Orders excluded
```

from financial totals.

---

# 70. Why

A Cancelled Order may contain abandoned or partial pricing/cost data that should not normally count as active business performance.

---

# 71. User Control

Reports should expose a visible option such as:

```text
Include Cancelled Orders
```

---

# 72. No Hidden Behavior

The report UI should make cancellation handling understandable.

Do not silently exclude Cancelled Orders without indication.

---

# 73. Orders Report

The detailed Orders Report may still allow Users to explicitly filter:

```text
status = cancelled
```

to inspect cancelled work.

---

# 74. Financial Summary Default

For Project/Client/Order Type financial summaries:

```text
Cancelled Orders excluded by default
```

---

# 75. Cost Summary and Cancelled Orders

Default Cost Summary should follow the same report filter policy when aggregating operational business performance.

However, Cost Items from Cancelled Orders remain real stored Costs and should be visible when:

```text
Include Cancelled Orders = true
```

---

# 76. Task 10 — Financial Permission Matrix

Reports must calculate response shape based on the current User's permissions.

---

# 77. No Financial Permissions

User with:

```text
reports.view
```

but no financial permissions may still see operational report columns such as:

```text
Order

Client

Project

Type

Status

Priority
```

---

# 78. Selling Permission Only

With:

```text
orders.view_selling_price
```

return:

```text
Selling Price

Selling Totals
```

but not:

```text
Cost

Profit
```

---

# 79. Cost Permission Only

With:

```text
orders.view_cost_price
```

return:

```text
Cost Price

Cost Totals
```

but not:

```text
Selling Price

Profit
```

---

# 80. Profit Permission Logic

Profit requires access to both:

```text
Selling
+
Cost
```

Do not expose Profit if one side is protected.

---

# 81. Detailed Cost Permission

Detailed Cost Summary additionally requires:

```text
calculator.view_costs
```

---

# 82. Backend Projection Safety

Do not retrieve protected values into a DTO and merely hide them during JSON serialization if avoidable.

Prefer permission-aware projection/output shaping.

---

# 83. Filter Leakage

Do not allow unauthorized Users to infer protected values through clever sorting/filtering.

Example:

A User without Cost access should not be able to sort by Cost Price and infer relative amounts.

Therefore protected financial sort/filter options must also be unavailable.

---

# 84. Summary Leakage

Do not return hidden financial totals inside metadata or debug fields.

---

# 85. Task 11 — Report Frontend Foundation

Add route:

```text
/reports
```

---

# 86. Reports Landing Page

May provide links/cards for:

```text
Orders

Projects

Clients

Order Types

Costs
```

Only show reports the User may access.

---

# 87. Common Report Layout

Use:

```text
Page Header

Filters

Summary

Results Table
```

---

# 88. Filters Section

Keep filters visible and understandable.

Do not hide essential filters behind excessive menus.

---

# 89. Apply Filters

Version 1 may use explicit:

```text
Apply
```

button for reports.

This prevents repeated expensive queries while User changes several filters.

---

# 90. Reset Filters

Provide:

```text
Reset
```

to return to default report state.

---

# 91. URL Filter State

Important report filters should preferably synchronize to URL query parameters.

Benefits:

```text
Refresh

Bookmark

Back/Forward

Share internal report link
```

---

# 92. Summary Cards

Examples:

```text
Orders

Selling Total

Cost Total

Profit
```

Show only authorized cards.

---

# 93. Summary Card Accuracy

Summary cards must reflect the full filtered dataset.

---

# 94. Results Table

Use server-side:

```text
Pagination

Sorting

Filtering
```

---

# 95. Loading Behavior

Changing filters should show a proper loading state without losing the whole page unnecessarily.

---

# 96. Empty State

Example:

```text
No Orders match the selected filters.
```

---

# 97. Error State

Report query failures should display a clear retryable error.

---

# 98. Task 12 — Orders Report Frontend

Route example:

```text
/reports/orders
```

---

# 99. Orders Report Filters UI

Provide relevant selectors for:

```text
Date Range

Client

Project

Order Type

Status

Priority

Owner

Assignee

Include Cancelled
```

---

# 100. Orders Report Table

Use the same business terminology as Orders module.

Do not rename Order to Job/Task.

---

# 101. Financial Column Visibility

Columns must appear only when the API/User permissions permit them.

---

# 102. Task 13 — Project Report Frontend

Route:

```text
/reports/projects
```

---

# 103. Project Summary Drill-Down

Clicking Project should navigate to:

```text
/projects/{projectId}
```

---

# 104. Project Report Financial Values

Show:

```text
Selling

Cost

Profit
```

according to permissions.

---

# 105. Task 14 — Client Report Frontend

Route:

```text
/reports/clients
```

---

# 106. Client Drill-Down

Click Client:

```text
/clients/{clientId}
```

---

# 107. Task 15 — Order Type Report Frontend

Route:

```text
/reports/order-types
```

No separate complex Order Type Workspace is required.

A row may offer:

```text
View Orders
```

with Orders Report filters applied.

---

# 108. Task 16 — Cost Report Frontend

Route:

```text
/reports/costs
```

Requires Cost visibility.

---

# 109. Cost Report Views

A simple selector may choose:

```text
By Category

By Supplier

By Order

By Project

By Client

By Order Type
```

This is not a generic report designer.

---

# 110. Task 17 — Drill-Down

Reports should link naturally back to source records.

Examples:

```text
Order
→ /orders/{id}

Project
→ /projects/{id}

Client
→ /clients/{id}
```

---

# 111. Filtered Drill-Down

A summary row may also open another report.

Example:

```text
Client summary
→ Orders Report filtered to that Client
```

---

# 112. No Duplicate Detail UI

Do not recreate full Order/Project/Client editing inside Reports.

Reports navigate to existing workspaces.

---

# 113. Task 18 — Dashboard Query Reuse

Dashboard is separate from Reports but may reuse report/query logic.

---

# 114. Dashboard Candidates

Initial Dashboard may show:

```text
Active Orders

Urgent Orders

Due Soon

Recent Orders

Recent Projects
```

Financial summary cards may be added only if useful and permission-safe.

---

# 115. Dashboard Is Not a Report Database

Do not create stored counters merely to make the Dashboard easy.

Use normal queries first.

---

# 116. Dashboard Financial Permissions

Dashboard must follow the exact same financial visibility rules.

---

# 117. Dashboard Cancelled Orders

Operational Dashboard should normally exclude Cancelled Orders.

---

# 118. Due Soon Definition

If implemented, define explicitly.

Example:

```text
deadline >= today
AND
deadline <= today + 7 days
AND
status NOT IN (completed, cancelled)
```

Do not leave "Due Soon" ambiguous.

---

# 119. Overdue Orders

May later show:

```text
deadline < today
AND
status NOT IN (completed, cancelled)
```

if useful.

---

# 120. Task 19 — Export Preparation

Version 1 may later support:

```text
CSV

Excel-compatible export
```

---

# 121. Export Must Use Full Filtered Dataset

If export is implemented:

```text
Export
```

should export all matching records, not just the current page.

---

# 122. Export Permissions

Export must respect exactly the same financial field permissions.

---

# 123. No Export Bypass

A hidden Cost column in UI must also be absent from exported data for unauthorized Users.

---

# 124. PDF Reports

PDF report generation is not required for initial Version 1.

---

# 125. Scheduled Reports

Do not implement:

```text
Daily email report

Scheduled PDF

Automatic Excel delivery
```

in Version 1.

---

# 126. Saved Reports

Do not implement customizable saved filter/report definitions initially.

URL query state is sufficient.

---

# 127. Task 20 — Orders Report Tests

Required:

```text
Filtering

Sorting

Pagination

Search

Cancelled handling

Financial permissions

Full-dataset summary totals
```

---

# 128. Pagination Summary Test

Create more Orders than one page.

Verify:

```text
summary totals
```

remain identical when requesting:

```text
page 1
page 2
page 3
```

for the same filters.

---

# 129. Selling Total Test

Verify:

```text
SUM(selling_price)
```

over all included Orders.

---

# 130. Cost Total Test

Verify:

```text
SUM(cost_price)
```

---

# 131. Profit Total Test

Verify:

```text
SUM(selling_price - cost_price)
```

or equivalent:

```text
SUM(selling_price) - SUM(cost_price)
```

with exact decimals.

---

# 132. Cancelled Default Test

Given:

```text
Active Order A

Completed Order B

Cancelled Order C
```

default financial totals include:

```text
A + B
```

but exclude:

```text
C
```

---

# 133. Include Cancelled Test

With:

```text
includeCancelled = true
```

Order C is included.

---

# 134. Explicit Cancelled Filter

Orders Report with:

```text
status = cancelled
```

must return Cancelled Orders.

---

# 135. Date Filter Test

Verify exact boundary behavior.

Example:

```text
fromDate inclusive

toDate inclusive
```

if that is the chosen API convention.

Document and test it.

---

# 136. Project Report Tests

Verify aggregation across multiple Orders within the same Project.

---

# 137. Project Count/Order Count Tests

Ensure counts reflect filtered source data correctly.

---

# 138. Project Filter Test

Example:

```text
Order Type = UV Printing
```

Project totals include only matching UV Orders.

---

# 139. Client Report Tests

Create:

```text
Client A
  Project 1
    Orders
  Project 2
    Orders
```

Verify:

```text
Project Count

Order Count

Selling

Cost

Profit
```

---

# 140. Client Distinct Project Count Test

Multiple matching Orders in one Project count as:

```text
1 Project
```

not multiple.

---

# 141. Order Type Report Tests

Verify grouping uses:

```text
Order Type ID
```

not merely display text.

---

# 142. Inactive Order Type Test

Historical Orders with inactive Order Type remain in report results.

---

# 143. Cost Report Tests

Verify grouping by:

```text
Category

Supplier

Order

Project

Client

Order Type
```

---

# 144. Cost Category Exact Grouping Test

```text
Material
```

and:

```text
Materials
```

remain separate groups.

This documents the Version 1 limitation.

---

# 145. Null Supplier Test

Cost Items without Supplier should group predictably.

---

# 146. Expense Date Test

Verify date filtering uses:

```text
expense_date
```

rather than Cost Item creation timestamp.

---

# 147. Detailed Cost Permission Test

User without:

```text
calculator.view_costs
```

cannot access Cost Summary endpoint.

---

# 148. Selling-Only Report Test

User with only Selling permission:

```text
Selling values present

Cost absent

Profit absent
```

---

# 149. Cost-Only Report Test

User with only Cost Price permission:

```text
Cost values present

Selling absent

Profit absent
```

---

# 150. Both Permissions Test

User with both:

```text
Selling

Cost

Profit
```

available.

---

# 151. No Financial Permissions Test

Operational report still works without financial data if User otherwise has Report access.

---

# 152. Protected Sort Test

User without Cost access attempts:

```text
sort_by=cost_price
```

Backend rejects or ignores according to established API error behavior.

Recommended:

```text
reject with validation error
```

---

# 153. Protected Filter Test

Likewise, unauthorized financial filters must not be accepted.

---

# 154. Query Efficiency Tests

Report endpoints must not cause N+1 queries.

---

# 155. Large Dataset Test

Test with a realistically larger development dataset.

Example:

```text
Thousands of Orders

Thousands of Cost Items
```

Verify acceptable query behavior.

---

# 156. Database Execution Review

Inspect generated SQL for important report queries.

Look for:

```text
unnecessary client-side evaluation

multiple repeated subqueries

cartesian explosions

unbounded result loading
```

---

# 157. Index Review

Do not add indexes blindly.

Review query plans for common report filters.

Likely useful existing/possible indexes may involve:

```text
orders.project_id

orders.order_type_id

orders.status

orders.deadline

orders.created_at

projects.client_id

project_members role/employee

cost_items.order_id

cost_items.expense_date
```

Add only where justified.

---

# 158. No Materialized Views Initially

Do not introduce:

```text
Materialized Views
```

unless measured performance requires them.

---

# 159. No Data Warehouse

Version 1 does not need:

```text
ETL

OLAP cube

Separate analytics database

Star schema
```

---

# 160. No Reporting Cache Initially

Do not add Redis or summary caches before measuring actual report performance.

---

# 161. Report Consistency

A report query should read one logically consistent snapshot of the database where practical.

Normal PostgreSQL transaction consistency is sufficient for Version 1.

---

# 162. No Stored Historical Snapshots

Reports show the current state of source records.

Example:

If an authorized User corrects a Cost Item after Order completion:

```text
Historical report totals change
```

accordingly.

This is intended Version 1 behavior.

---

# 163. No Accounting Close

Version 1 has no:

```text
Month close

Fiscal close

Locked financial period
```

---

# 164. Report Naming

Use clear names:

```text
Orders

Projects

Clients

Order Types

Costs
```

Avoid vague names such as:

```text
Analytics 1

Financial Dashboard 2
```

---

# 165. Report UI Density

Reports are business tools.

Prefer:

```text
Readable tables

Useful filters

Clear totals

Minimal decorative elements
```

---

# 166. Charts

Charts are optional.

Do not delay useful reporting to build charts.

---

# 167. If Charts Are Added

Charts must use the same backend-authoritative aggregated data as tables.

Do not separately calculate conflicting totals in browser.

---

# 168. Report API Error Handling

Use standard error shape:

```text
code

message

errors
```

---

# 169. Invalid Filter

Example:

```text
invalid date range
```

should return a validation error.

---

# 170. Unknown Entity Filter

Example:

```text
clientId
```

that does not exist may produce either:

```text
empty result
```

or:

```text
validation/not-found error
```

Choose consistently.

For report filters, empty result is generally simpler.

---

# 171. Date Range Validation

If:

```text
fromDate > toDate
```

reject with validation error.

---

# 172. Report Logging

Do not log entire report datasets.

Technical logs may include:

```text
endpoint

duration

unexpected failure
```

---

# 173. Slow Query Logging

If useful, log unusually slow report queries in a controlled manner without dumping sensitive data.

---

# 174. Report Commits

Recommended:

```text
feat(reports): add report permissions and filters

feat(reports): add orders report

feat(reports): add project financial summary

feat(reports): add client financial summary

feat(reports): add order type financial summary

feat(reports): add cost summary

feat(frontend): add reports workspace

test(reports): add aggregation and permission tests

perf(reports): optimize measured report queries
```

---

# 175. First AI Coding Task

Recommended:

```text
Read:
- AI_RULES.md
- docs/16_Reports_Module.md
- docs/17_Database_Schema_Overview.md
- docs/19_API_Design_Guidelines.md
- docs/20_Testing_Strategy.md
- docs/24_Backend_Architecture.md
- docs/25_Development_Workflow_for_AI.md
- docs/34_Costs_Implementation_Plan.md
- docs/35_Reports_Implementation_Plan.md

Task:
Implement only the Orders Report backend foundation.

Create:
- reports.view permission if not already present
- typed Orders Report filters
- server-side query projection
- pagination
- sorting whitelist
- search
- default exclusion of Cancelled Orders
- permission-aware Selling/Cost/Profit fields
- full filtered-dataset summary totals
- integration tests

Do not implement:
- Project Report
- Client Report
- Cost Report
- frontend
- report tables
- materialized views
- generic report designer

Before finishing:
- build
- run integration tests
- inspect generated SQL
- verify summary totals are independent of pagination
```

---

# 176. Second AI Coding Task

```text
Implement Project Financial Summary.

Requirements:
- aggregate Orders by Project
- Order Count
- Selling Total
- Cost Total
- Profit
- Client
- Owner
- Assignee
- filters
- financial permissions
- default Cancelled exclusion
- tests
```

---

# 177. Third AI Coding Task

```text
Implement Client Financial Summary.

Requirements:
- aggregate Client → Projects → Orders
- distinct Project Count
- Order Count
- Selling
- Cost
- Profit
- filters
- financial permissions
- tests
```

---

# 178. Fourth AI Coding Task

```text
Implement Order Type Financial Summary.

Requirements:
- group by Order Type ID
- Order Count
- Selling
- Cost
- Profit
- include inactive historical Order Types
- filters
- tests
```

---

# 179. Fifth AI Coding Task

```text
Implement Cost Summary.

Support controlled grouping:
- Category
- Supplier
- Order
- Project
- Client
- Order Type

Require calculator.view_costs.
Use expense_date for cost date filters.
Do not create generic dynamic BI/reporting infrastructure.
```

---

# 180. Sixth AI Coding Task

```text
Implement Reports frontend foundation.

Create:
- /reports
- report navigation
- common filter panel
- summary cards
- server-side table pattern
- loading/error/empty states
- URL query state where useful
```

---

# 181. Seventh AI Coding Task

```text
Implement Orders, Projects, Clients, Order Types, and Costs report pages.

Reuse the common reporting patterns.

Do not duplicate source-record edit forms.
Use drill-down links to existing workspaces.
```

---

# 182. Eighth AI Coding Task

```text
Add Dashboard queries using the existing reporting/query foundation.

Start only with:
- Active Orders
- Urgent Orders
- Due Soon
- Recent Orders
- Recent Projects

Do not create stored dashboard counters.
```

---

# 183. Reports Completion Gate

Reports phase is complete only when:

```text
Orders Report works

Project Financial Summary works

Client Financial Summary works

Order Type Financial Summary works

Cost Summary works

Filters are server-side

Sorting is controlled

Pagination works

Summary totals use full filtered dataset

Cancelled Orders are handled explicitly

Financial permissions work

Detailed Cost permissions work

Profit is hidden when either financial side is unavailable

Drill-down works

No source data is duplicated

No report tables exist

Integration tests pass

Measured query performance is acceptable
```

---

# 184. Mandatory Full-Dataset Test

For every paginated financial report:

```text
Summary totals
must not change
when page changes.
```

This is mandatory.

---

# 185. Mandatory Financial Permission Test

A report must never expose:

```text
Selling

Cost

Profit

Detailed Cost Items
```

without the corresponding permissions.

This must be tested at API level.

---

# 186. Mandatory Cancelled-Order Test

Default financial summaries:

```text
exclude Cancelled Orders
```

unless the User explicitly includes them.

---

# 187. Mandatory Profit Test

Profit must always derive from:

```text
Selling - Cost
```

and must not come from a stored Profit field.

---

# 188. No Report Business IDs

Reports themselves do not need Business IDs.

They are views over source data.

---

# 189. No Report Editing

Do not add inline financial editing inside report tables.

Editing belongs in the source Workspace.

---

# 190. No Generic Report Builder

Version 1 does not include:

```text
Drag-and-drop report designer

Custom SQL report builder

Arbitrary formulas

User-defined grouping engine

User-defined joins
```

---

# 191. No Scheduled Delivery

Version 1 does not include:

```text
Email schedules

Telegram report delivery

Automatic PDFs

Recurring Excel generation
```

---

# 192. No Public Report Links

All Reports remain inside authenticated ERP.

---

# 193. No Customer Reports Portal

Customer-facing reporting is outside Version 1.

---

# 194. No Accounting Statements

Do not create:

```text
Balance Sheet

Income Statement

Cash Flow Statement

VAT Return
```

from operational Order data.

These require an accounting system.

---

# 195. No Misleading Net Profit Label

Use wording such as:

```text
Profit
```

within the clearly operational Order context, or:

```text
Order Profit
```

where ambiguity exists.

Do not label these totals:

```text
Company Net Profit
```

---

# 196. Future Report Extensions

Possible later additions:

```text
Charts

Saved filters

Excel export

PDF export

Period comparison

Trend analysis

Production KPIs

Employee workload

Machine utilization

Accounting integration
```

Only add after Version 1 workflows are stable.

---

# 197. Reports Simplicity Rule

Before adding a report, ask:

```text
What business decision will this report help Lithograph make?
```

If the answer is unclear, do not build it yet.

---

# 198. Final Reports Principle

The Reports module should answer:

```text
What work was done?

For which Clients?

Through which Projects?

Which Order Types generated the work?

How much was sold?

How much did direct Costs total?

What operational Profit remains?

Where did direct Costs come from?
```

without becoming a second accounting system.

The central rule is:

```text
Reports read source data.

Reports do not become source data.
```

---

**End of Document**