# Lithograph ERP

**Document:** 16_Reports_Module.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Reports

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `05_Numbering_System.md`
- `06_UI_UX_Principles.md`
- `12_Clients_Module.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`

---

# 1. Purpose

The Reports module provides read-only summaries and analysis of Lithograph ERP operational data.

Reports use existing data from:

```text
Clients
Projects
Orders
Calculator
Employees
```

The Reports module does not become a separate source of business data.

---

# 2. Main Responsibilities

The Reports module is responsible for:

```text
Operational summaries

Financial summaries

Date-based reporting

Client reporting

Project reporting

Order reporting

Order Type reporting

Basic profitability analysis
```

---

# 3. Read-Only Principle

Reports read operational data.

They do not own or duplicate normal business records.

Conceptually:

```text
Operational Modules
        ↓
      Reports
```

not:

```text
Reports
   ↓
Operational Modules
```

---

# 4. Database Ownership

Version 1 does not require a dedicated Reports database schema.

Do not automatically create:

```text
reports.*
```

tables.

Reports should query existing module data.

---

# 5. No Reporting Tables by Default

Do not create duplicated tables such as:

```text
reports.orders_summary

reports.project_totals

reports.client_totals
```

in Version 1.

These values can be calculated from source data.

---

# 6. Source-of-Truth Rule

Examples:

Order Selling Price source:

```text
orders.orders.selling_price
```

Order Cost Price source:

```text
orders.orders.cost_price
```

Project Client source:

```text
projects.projects.client_id
```

Project Team source:

```text
projects.project_members
```

Reports must use these existing sources.

---

# 7. Profit

Profit is calculated.

Formula:

```text
Profit = Selling Price - Cost Price
```

Do not store Profit in reporting tables.

---

# 8. Project Selling Total

Calculated as:

```text
SUM(Order Selling Price)
```

for Orders belonging to the Project.

---

# 9. Project Cost Total

Calculated as:

```text
SUM(Order Cost Price)
```

for Orders belonging to the Project.

---

# 10. Project Profit

Calculated as:

```text
Project Profit
=
Project Selling Total
-
Project Cost Total
```

---

# 11. Client Selling Total

Calculated through:

```text
Client
   ↓
Projects
   ↓
Orders
```

Formula:

```text
SUM(Order Selling Price)
```

for Orders belonging to that Client's Projects.

---

# 12. Client Cost Total

Calculated as:

```text
SUM(Order Cost Price)
```

for Orders belonging to that Client's Projects.

---

# 13. Client Profit

Calculated as:

```text
Client Selling Total
-
Client Cost Total
```

---

# 14. Order Type Totals

Reports may aggregate by Order Type.

Example:

| Order Type | Orders | Selling | Cost | Profit |
|---|---:|---:|---:|---:|
| UV Printing | 48 | ... | ... | ... |
| Laser Cutting | 27 | ... | ... | ... |
| Graphic Design | 19 | ... | ... | ... |

---

# 15. Basic Version 1 Reports

Initial useful reports should include:

```text
Orders by Date

Orders by Client

Orders by Project

Orders by Order Type

Orders by Status

Orders by Priority

Project Financial Summary

Client Financial Summary

Order Type Financial Summary
```

---

# 16. Orders by Date

This report shows Orders within a selected date range.

Possible columns:

```text
Order ID

Created Date

Client

Project

Order Name

Order Type

Status

Selling Price

Cost Price

Profit
```

Financial columns depend on permissions.

---

# 17. Date Source

The report must clearly define which date is being filtered.

Possible date choices include:

```text
Created Date

Deadline
```

Do not ambiguously label a generic:

```text
Date
```

when several dates exist.

---

# 18. Default Date Filter

A report should use an explicit date range.

Examples:

```text
From

To
```

Do not load unlimited historical data unnecessarily.

---

# 19. Orders by Client

Allows filtering or grouping Orders by Client.

Possible grouping:

```text
Client
   ├── Project
   │     ├── Order
   │     └── Order
   └── Project
```

---

# 20. Orders by Project

Shows Orders belonging to one or more selected Projects.

Useful fields:

```text
Order ID

Order Name

Order Type

Status

Priority

Deadline

Selling Price

Cost Price

Profit
```

---

# 21. Orders by Order Type

Allows analysis of production/service categories.

Example questions:

```text
How many UV Printing Orders were completed this month?

What revenue came from Laser Cutting?

What Cost and Profit came from Installation?
```

---

# 22. Orders by Status

Reports may group by:

```text
Draft

Active

On Hold

Completed

Cancelled
```

This is useful for operational workload visibility.

---

# 23. Orders by Priority

Reports may group or filter by:

```text
Low

Normal

High

Urgent
```

This supports operational review.

---

# 24. Project Financial Summary

A Project Financial Summary may show:

```text
Project ID

Project Name

Client

Order Count

Selling Total

Cost Total

Profit
```

These totals are calculated from Orders.

---

# 25. Client Financial Summary

A Client Financial Summary may show:

```text
Client ID

Client Name

Project Count

Order Count

Selling Total

Cost Total

Profit
```

---

# 26. Order Type Financial Summary

Possible fields:

```text
Order Type

Order Count

Selling Total

Cost Total

Profit

Average Selling Price
```

Average values are calculated, not stored.

---

# 27. Completed vs Non-Completed Orders

Reports must be clear about which statuses are included.

Financial reports may allow status filters such as:

```text
Completed only

Exclude Cancelled

All Orders
```

Do not silently assume one interpretation.

---

# 28. Cancelled Orders

Cancelled Orders remain stored and may appear in operational reports.

Financial summary behavior should be explicit.

A user may choose to:

```text
Include Cancelled

Exclude Cancelled
```

depending on the report.

---

# 29. Default Financial Behavior

Recommended default:

```text
Exclude Cancelled Orders
```

from normal financial totals.

The UI should make this visible.

---

# 30. Draft Orders

Draft Orders may have:

```text
selling_price = 0
cost_price = 0
```

or incomplete calculations.

Reports should not imply that all Draft values are final.

---

# 31. Financial Status Context

Where useful, financial reports should show or filter by Order Status so users can distinguish:

```text
Draft estimates

Active work

Completed work

Cancelled work
```

---

# 32. Cost Item Reporting

Version 1 may include simple reporting from:

```text
calculator.cost_items
```

Useful summaries:

```text
Costs by Category

Costs by Supplier Text

Costs by Date
```

These are optional but supported by the data model.

---

# 33. Cost by Category

Example:

| Category | Amount |
|---|---:|
| Material | ... |
| Outsource | ... |
| Transport | ... |
| Installation | ... |

Because Category is free text in Version 1, inconsistent spelling may produce separate groups.

This limitation is acceptable initially.

---

# 34. Cost by Supplier

Supplier is also free text in Version 1.

Reports may group by:

```text
supplier
```

but should not assume a normalized Supplier master database exists.

---

# 35. Cost Date

Cost reports may filter using:

```text
expense_date
```

This is different from Order creation date.

The UI should label it clearly.

---

# 36. Project Team Reporting

Version 1 may support operational filtering by:

```text
Project Owner

Project Assignee

Participant

Observer
```

Employee assignments come from:

```text
projects.project_members
```

---

# 37. Orders by Owner

An Order does not store an Owner directly.

To report Orders by Owner:

```text
Order
   ↓
Project
   ↓
Project Owner
```

Do not create duplicated Owner fields on Orders for reporting convenience.

---

# 38. Orders by Assignee

Same principle:

```text
Order
   ↓
Project
   ↓
Project Assignee
```

---

# 39. Employee Historical Reporting

Inactive Employees must remain visible in historical reports.

Example:

```text
Owner:
Former Employee
```

should still display correctly for old Projects.

---

# 40. Client Historical Reporting

Inactive Clients remain available in historical reports.

Deactivation must not remove their Projects or Orders from reports.

---

# 41. Inactive Order Types

Historical Orders using inactive Order Types remain reportable.

Order Type deactivation does not alter report history.

---

# 42. Reporting Permissions

Reports must respect underlying business permissions.

Reports must not become a way to bypass:

```text
Orders permissions

Calculator financial permissions

Client access rules
```

---

# 43. Basic Reports Permission

Initial permission:

```text
reports.view
```

allows access to general operational reports.

---

# 44. Financial Reporting Permissions

Financial visibility should reuse existing permissions where practical.

Examples:

```text
orders.view_selling_price

orders.view_cost_price

calculator.view_costs
```

Do not create duplicate financial security models unnecessarily.

---

# 45. Selling Price Visibility

If a User cannot view Selling Price elsewhere, Reports must not expose:

```text
Selling Price

Selling Totals
```

---

# 46. Cost Price Visibility

If a User cannot view Cost Price, Reports must not expose:

```text
Cost Price

Cost Totals

Cost-based breakdowns
```

---

# 47. Profit Visibility

Profit requires both:

```text
Selling Price

Cost Price
```

Therefore Profit should normally be visible only when the User can see both values.

---

# 48. Backend Authorization

Financial columns must be removed or rejected by the backend according to Permissions.

Frontend hiding alone is not sufficient.

---

# 49. Report Filters

Version 1 reports may use straightforward filters such as:

```text
Date Range

Client

Project

Order Type

Status

Priority

Project Owner

Project Assignee
```

---

# 50. Filter Simplicity

Do not build an advanced SQL-like report builder in Version 1.

Prefer predefined reports with useful filters.

---

# 51. Report Search

Reports may support Business ID search where relevant.

Examples:

```text
ORD-2026-000425

PRJ-2026-000125

CL-000125
```

---

# 52. Report Tables

Reports should reuse normal ERP table behavior where useful:

```text
Sorting

Filtering

Pagination

Column visibility
```

Do not build an unrelated reporting UI system.

---

# 53. Report Totals

When a report shows filtered rows, total calculations should normally reflect the currently filtered result set.

Example:

```text
Filtered Orders:
UV Printing
September 2026

Selling Total:
...
```

---

# 54. Pagination and Totals

Totals must represent the complete filtered dataset, not only the current visible page.

This calculation should be performed correctly by the backend.

---

# 55. Money Precision

Financial reports use exact decimal values.

Do not use floating-point calculations for:

```text
Selling

Cost

Profit
```

---

# 56. Report Export

Version 1 may support simple export later.

Possible formats:

```text
CSV

Excel-compatible spreadsheet
```

Export is useful but not required for the first implementation of Reports.

---

# 57. PDF Reports

Version 1 does not require PDF report generation unless a specific business document is needed.

Do not build a generic reporting document engine prematurely.

---

# 58. Dashboard vs Reports

Dashboard and Reports have different purposes.

Dashboard:

```text
Quick overview
```

Reports:

```text
Detailed filtered analysis
```

The Reports module should not become the entire Dashboard system.

---

# 59. Dashboard Metrics

The Dashboard may reuse report queries for simple metrics such as:

```text
Active Orders

Urgent Orders

Orders Due Soon

Monthly Selling Total
```

Exact Dashboard design can be handled separately.

---

# 60. No Data Duplication for Dashboard

Dashboard metrics should read the same operational data.

Do not store separate counters unless performance later requires it.

---

# 61. API Responsibilities

The Reports API may conceptually provide endpoints such as:

```text
Orders Report

Projects Summary

Clients Summary

Order Types Summary

Cost Summary
```

Exact REST routes are decided during implementation.

---

# 62. Report Request Model

A report request may contain:

```text
date_from

date_to

client_id

project_id

order_type_id

status

priority

owner_employee_id

assignee_employee_id
```

Only relevant filters should be used for each report.

---

# 63. Report Response Model

A report may return:

```text
rows

summary
```

Example:

```text
rows
→ individual Orders

summary
→ count
→ selling_total
→ cost_total
→ profit_total
```

subject to permissions.

---

# 64. Server-Side Reporting

Filtering, aggregation, and authorization should normally happen on the backend.

Do not download all ERP records to the browser and calculate reports there.

---

# 65. Database Queries

Reports should use efficient database queries through EF Core or carefully controlled SQL when necessary.

Start with EF Core.

Use raw SQL only if a real reporting query cannot reasonably be expressed or performs poorly.

---

# 66. No Stored Procedures by Default

Do not introduce stored procedures for basic Version 1 reporting.

Business/reporting queries should remain in application code unless a demonstrated need arises.

---

# 67. Database Views

Database views may be introduced if they clearly simplify repeated complex read queries.

They are not required by default.

Do not create views merely because this is a Reports module.

---

# 68. Materialized Views

Version 1 does not require PostgreSQL materialized views.

The expected ERP data volume does not justify this complexity initially.

---

# 69. Caching

Version 1 does not require dedicated reporting cache infrastructure.

Use normal database queries first.

Optimize only when real performance measurements show a problem.

---

# 70. Report Date Boundaries

Date filters must use clear inclusive/exclusive behavior.

For date-only filters, the user expectation should be:

```text
From Date included

To Date included
```

Implementation should make this behavior consistent.

---

# 71. Time Zone

Timestamp-based reporting uses the application's normal UTC storage rules.

When presenting dates/times to Users, convert according to configured application/user timezone behavior.

Date-only fields remain dates.

---

# 72. Order Created Date

Order creation reporting uses:

```text
orders.orders.created_at
```

---

# 73. Order Deadline

Deadline reports use:

```text
orders.orders.deadline
```

This is a date-only field.

---

# 74. Project Dates

Project reporting may use:

```text
start_date

deadline

created_at
```

The report must state which date is being used.

---

# 75. Cost Dates

Expense reporting uses:

```text
calculator.cost_items.expense_date
```

when available.

---

# 76. Missing Expense Date

A Cost Item without `expense_date` remains valid.

Date-based Cost reports may:

```text
Exclude undated items from date-filtered results
```

or display them separately when no date filter exists.

The behavior must be clear.

---

# 77. Null Values

Reports must handle optional data safely.

Examples:

```text
No Deadline

No Supplier

No Assignee

No Preview Image
```

Null values must not cause report failures.

---

# 78. Empty Results

If filters produce no results, display a clear empty state.

Example:

```text
No Orders match the selected filters.
```

Do not display misleading zero-data charts or broken tables.

---

# 79. Report Titles

Report pages should use clear business names.

Examples:

```text
Orders Report

Project Financial Summary

Client Financial Summary

Order Type Summary

Cost Summary
```

---

# 80. Orders Report Layout

Recommended structure:

```text
Report Title

Filters

Summary

Results Table
```

Example:

```text
Orders Report

September 2026
Client: All
Status: Completed

Orders: 84
Selling: ...
Cost: ...
Profit: ...

[Results Table]
```

---

# 81. Financial Summary Cards

Simple summary values may be shown above the result table:

```text
Order Count

Selling Total

Cost Total

Profit
```

Only show values the User is authorized to view.

---

# 82. Profit Margin

Version 1 does not require Profit Margin as a stored value.

If displayed, calculate it.

Possible formula:

```text
Profit Margin %
=
Profit / Selling Price × 100
```

Handle zero Selling Price safely.

---

# 83. Average Values

Reports may calculate values such as:

```text
Average Selling Price

Average Cost Price

Average Profit
```

when useful.

These remain derived values.

---

# 84. Charting

Basic charts may be added where they clearly improve understanding.

Examples:

```text
Selling by Month

Orders by Type

Orders by Status
```

Charts are optional presentation of report data.

Do not build a large analytics platform in Version 1.

---

# 85. Chart Source

Charts must use the same filtered dataset/aggregation logic as the related report.

Do not calculate contradictory totals separately.

---

# 86. Drill-Down

Where useful, a report row may open the underlying record.

Examples:

```text
Click Order
→ Order Workspace

Click Project
→ Project Workspace

Click Client
→ Client details
```

This improves navigation without duplicating editing inside Reports.

---

# 87. Reports Are Not Editable

Do not edit Orders, Projects, Clients, or Costs directly inside report tables in Version 1.

Reports are read-only.

Use drill-down to open the source record.

---

# 88. Business ID Display

Important reports should show human-readable Business IDs.

Examples:

```text
CL-000125

PRJ-2026-000125

ORD-2026-000425
```

Do not expose UUIDs to normal Users.

---

# 89. Record Names

Where possible, show both Business ID and Name.

Example:

```text
PRJ-2026-000125 — New Store Opening
```

This improves identification.

---

# 90. Cancelled Financial Values

Cancelled Orders may retain Selling and Cost values historically.

Excluding them from a report must be a filter decision, not deletion of their values.

---

# 91. Cost vs Selling Timing

Version 1 reports use the current stored historical Selling Price and Cost Price on each Order.

They do not attempt to create accounting-recognition rules such as:

```text
Revenue recognition date

Accrual accounting

Cash payment date
```

Those belong to future Finance/Accounting functionality.

---

# 92. Reports Are Not Accounting

The Reports module provides operational business reporting.

It is not:

```text
General Ledger

Profit & Loss Accounting

Balance Sheet

Tax Accounting

VAT Reporting

Accounts Receivable

Accounts Payable
```

---

# 93. Operational Profit

When Reports display:

```text
Profit
```

it means:

```text
Order Selling Price
-
Order Cost Price
```

It does not mean full company accounting profit.

This distinction should remain clear.

---

# 94. Company Expenses

Version 1 Order Cost Price contains Order-level Costs.

Reports do not automatically subtract:

```text
Office Rent

General Salaries

Utilities

Taxes

Leasing

Company overhead
```

unless those values are intentionally entered into Orders.

Therefore Order Profit is operational Order Profit, not final company net profit.

---

# 95. Terminology

Use:

```text
Order Profit

Project Profit
```

where useful to avoid implying full accounting net profit.

---

# 96. Report Permissions

Initial Reports permission:

```text
reports.view
```

Future specialized permissions should only be added if real access requirements require them.

---

# 97. Navigation

Reports should appear in primary navigation.

Example:

```text
Dashboard

Clients

Projects

Orders

Reports

Administration
```

---

# 98. Reports Home

A simple Reports landing page may contain:

```text
Orders

Projects

Clients

Order Types

Costs
```

Do not create an overly complex analytics portal.

---

# 99. Saved Reports

Version 1 does not require Users to save custom report definitions.

Filters may reset between sessions initially.

Saved reports may be added later if repeated workflows justify them.

---

# 100. User-Defined Reports

Version 1 does not include a report designer.

Users cannot arbitrarily select database fields and construct SQL-like reports.

Use predefined reports.

---

# 101. Scheduling Reports

Version 1 does not include:

```text
Scheduled Report Emails

Automatic Daily Reports

Automatic PDF Delivery
```

These may be added later if needed.

---

# 102. Report Sharing

Version 1 does not include public share links.

ERP Reports require normal authenticated access.

---

# 103. Report Snapshots

Version 1 does not store report snapshots.

If a User reruns a report later, it reflects the current underlying historical records.

Formal accounting/report snapshots may be designed later if required.

---

# 104. Data Updates

If an Order's stored Selling Price changes before finalization, a report using that Order will reflect the updated stored value.

Reports do not maintain a separate previous copy.

---

# 105. Historical Stability

Once business values are considered final, their historical safety depends on the source modules.

Reports should not create their own historical versioning mechanism.

---

# 106. Calculator Template Changes

Changing a Calculator Template does not directly affect Reports.

Reports use:

```text
orders.orders.selling_price

orders.orders.cost_price
```

Therefore historical report totals remain stable when Template versions change.

---

# 107. Cost Item Changes

Editing Cost Items updates:

```text
Order Cost Price
```

and therefore affects future reports.

This is correct because Cost Items are source business data.

---

# 108. Report Data Consistency

When Cost Items and `orders.orders.cost_price` are updated, they should remain synchronized through Calculator business logic.

Reports should not calculate one Cost Price from Cost Items while another stored value disagrees.

The source modules must maintain consistency.

---

# 109. Project Team Changes

Operational reports by current Project Owner/Assignee will reflect the Project Team currently assigned.

Version 1 does not preserve Project Team assignment history over time.

---

# 110. Historical Team Reporting Limitation

Because Project Team history is not versioned, Version 1 cannot reliably answer questions such as:

```text
Who was the Project Owner three months ago?
```

It can answer:

```text
Who is currently stored as the Project Owner?
```

This limitation is accepted for Version 1.

---

# 111. Client Name Changes

Reports show the current Client Name referenced by the Project.

Version 1 does not preserve historical Client-name snapshots.

---

# 112. Order Type Name Changes

Reports show the current stored Order Type Name.

The Order Type identity remains stable through UUID.

Historical name snapshotting is not required in Version 1.

---

# 113. Performance Principle

Start with direct indexed relational queries.

Only optimize after measuring real report performance.

Do not prematurely introduce:

```text
Data warehouse

OLAP cube

Elasticsearch

Separate analytics database

ETL pipeline
```

---

# 114. Expected Scale

Lithograph ERP Version 1 is an internal small-business system.

PostgreSQL can handle the expected reporting workload directly.

Keep architecture simple.

---

# 115. Testing Requirements

Important Reports tests should include:

```text
Orders by Date filter works

Orders by Client works

Orders by Project works

Orders by Order Type works

Orders by Status works

Orders by Priority works

Project Selling Total is correct

Project Cost Total is correct

Project Profit is correct

Client totals are correct

Order Type totals are correct

Cancelled Orders excluded when selected

Cancelled Orders included when selected

Business IDs display correctly

Inactive Clients remain historically reportable

Inactive Employees remain historically visible

Inactive Order Types remain historically reportable

Selling Price hidden without permission

Cost Price hidden without permission

Profit hidden without both permissions

Filtered totals include all filtered rows, not only current page

Cost Item category totals are correct

Report APIs reject unauthorized access
```

---

# 116. Version 1 Report Summary

Initial Reports:

```text
Orders Report

Project Financial Summary

Client Financial Summary

Order Type Financial Summary

Cost Summary
```

These are enough to provide useful operational reporting without building a general analytics platform.

---

# 117. Version 1 Non-Goals

Reports Version 1 does not include:

```text
Accounting Statements

General Ledger

Balance Sheet

VAT Reports

Cash Flow Accounting

Data Warehouse

OLAP

Custom Report Designer

Saved User Reports

Scheduled Report Delivery

Public Report Links

Separate Analytics Database

Historical Report Snapshots

Advanced BI Platform
```

---

# 118. Future Extensions

Possible future additions include:

- Saved filters
- Export to Excel
- PDF reports
- Charts
- Dashboard integration
- Scheduled reports
- More advanced profitability analysis
- Employee performance reporting
- Production capacity reports
- Accounting reports after Finance modules exist

These should be added only when real requirements appear.

---

# 119. Module Simplicity Rule

Before adding a new report or reporting table, ask:

```text
Can this information be calculated directly and reliably from existing ERP data?
```

If yes, use the existing data.

Do not create another source of truth.

---

# 120. Final Reports Principle

The Reports module should answer:

```text
What work did Lithograph do?
```

```text
For which Clients?
```

```text
Through which Projects and Order Types?
```

```text
How much was sold?
```

```text
How much did the Orders cost?
```

and:

```text
What was the calculated operational Profit?
```

The central rule is:

```text
Operational modules own the data.

Reports explain the data.
```

---

**End of Document**