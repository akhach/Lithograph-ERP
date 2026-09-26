# Lithograph ERP

**Document:** 36_Dashboard_Implementation_Plan.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Dashboard

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `06_UI_UX_Principles.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `16_Reports_Module.md`
- `18_Implementation_Roadmap.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `23_Frontend_Architecture.md`
- `24_Backend_Architecture.md`
- `25_Development_Workflow_for_AI.md`
- `30_Projects_Implementation_Plan.md`
- `31_Orders_Implementation_Plan.md`
- `35_Reports_Implementation_Plan.md`

---

# 1. Purpose

This document defines implementation of the Lithograph ERP Dashboard.

The Dashboard is the main operational landing page after login.

Its purpose is to answer quickly:

```text
What needs attention now?

What work is active?

What is urgent?

What is approaching its deadline?

What was recently created or changed?
```

The Dashboard is not a second reporting system.

---

# 2. Main Goal

After this phase, the Dashboard should provide:

```text
Active Orders

Urgent Orders

Due Soon Orders

Overdue Orders

Recent Orders

Recent Projects
```

and, where useful and permission-safe:

```text
Operational summary counts
```

Financial summary cards are optional and should be added only when they provide clear business value.

---

# 3. Dashboard Principle

The Dashboard should favor:

```text
Operational awareness

Fast navigation

Clear priorities

Small useful summaries
```

over:

```text
Large analytics collections

Complex charts

Decorative metrics

Duplicated Reports
```

---

# 4. Dashboard Data Ownership

Dashboard owns no business tables.

It reads existing source data from:

```text
projects.projects

projects.project_members

orders.orders

orders.order_types

clients.clients
```

and may reuse read/query logic from Reports.

---

# 5. No Dashboard Schema

Do not create:

```text
dashboard.dashboard_items

dashboard.metrics

dashboard_counters
```

in Version 1.

---

# 6. No Stored Counters

Do not store values such as:

```text
active_orders_count

urgent_orders_count

overdue_orders_count
```

unless real performance measurements later justify caching.

Calculate from source data.

---

# 7. Recommended Initial Dashboard

Version 1 should contain:

```text
Top Summary

Active Orders

Urgent Orders

Due Soon / Overdue

Recent Orders

Recent Projects
```

Keep the first implementation compact.

---

# 8. Recommended Implementation Sequence

```text
Task 1
Dashboard permission/access rules

Task 2
Dashboard summary query

Task 3
Active Orders query

Task 4
Urgent Orders query

Task 5
Due Soon query

Task 6
Overdue query

Task 7
Recent Orders query

Task 8
Recent Projects query

Task 9
Dashboard API

Task 10
Frontend layout

Task 11
Drill-down/navigation

Task 12
Financial visibility review

Task 13
Tests

Task 14
Performance review
```

---

# 9. Dashboard Permission

A separate:

```text
dashboard.view
```

permission is optional.

Recommended Version 1 approach:

```text
Any authenticated User who can access the ERP
may open the Dashboard.
```

Individual sections remain permission-aware.

This avoids unnecessary permission complexity.

---

# 10. Section Permissions

Dashboard content must respect existing module permissions.

Examples:

```text
Orders data
→ orders.view

Projects data
→ projects.view
```

---

# 11. Financial Permissions

If financial data is ever displayed:

```text
Selling
→ orders.view_selling_price

Cost
→ orders.view_cost_price

Profit
→ both
```

Dashboard must follow the same rules as Reports.

---

# 12. No Permission Leakage

A User who cannot view Orders must not receive hidden Order data through the Dashboard API.

Likewise for Projects.

---

# 13. Task 1 — Dashboard Summary

A small summary area may contain counts such as:

```text
Active Orders

Urgent Orders

Due Soon

Overdue
```

---

# 14. Active Order Definition

Recommended:

```text
status = active
```

Do not count:

```text
draft

on_hold

completed

cancelled
```

as Active.

---

# 15. Urgent Order Definition

Recommended:

```text
priority = urgent

AND

status NOT IN (
    completed,
    cancelled
)
```

This includes urgent Orders that are:

```text
Draft

Active

On Hold
```

---

# 16. Due Soon Definition

Recommended Version 1 definition:

```text
deadline IS NOT NULL

AND deadline >= today

AND deadline <= today + 7 days

AND status NOT IN (
    completed,
    cancelled
)
```

---

# 17. Seven-Day Window

Use:

```text
7 calendar days
```

initially.

Do not create user-configurable Dashboard thresholds in Version 1.

---

# 18. Overdue Definition

Recommended:

```text
deadline IS NOT NULL

AND deadline < today

AND status NOT IN (
    completed,
    cancelled
)
```

---

# 19. Date Source

Use backend-controlled current date.

Do not trust frontend date for defining overdue/due-soon state.

---

# 20. Time Zone

Use the application's approved business/local-date behavior consistently.

Stored date-only deadlines remain date-only.

Do not introduce timezone conversion complexity for deadline comparison.

---

# 21. Task 2 — Active Orders Query

Provide a small list of active Orders.

Recommended maximum initial display:

```text
10
```

---

# 22. Active Orders Fields

Display:

```text
Business ID

Order Name

Client

Project

Order Type

Priority

Deadline
```

---

# 23. Active Orders Sort

Recommended:

```text
Urgent first

Then nearest deadline

Then newest/recently updated
```

or a simpler deterministic equivalent.

Do not create complex scoring logic.

---

# 24. Active Orders Without Deadline

Orders without deadline should still appear if space permits.

They should sort after deadline-sensitive Orders.

---

# 25. Active Orders Navigation

Each row should open:

```text
/orders/{orderId}
```

---

# 26. View All

Provide:

```text
View All Orders
```

linking to:

```text
/orders?status=active
```

where appropriate.

---

# 27. Task 3 — Urgent Orders Query

Urgent Orders deserve a dedicated section only if there are urgent items.

---

# 28. Urgent Order Fields

Display:

```text
Business ID

Name

Client

Project

Status

Deadline
```

---

# 29. Urgent Sort

Recommended:

```text
Overdue first

Then nearest deadline

Then recently updated
```

---

# 30. No Duplicate Concern

An Order may appear in both:

```text
Active Orders
```

and:

```text
Urgent Orders
```

This is acceptable because sections answer different operational questions.

Do not build complex de-duplication logic merely for aesthetics.

---

# 31. Task 4 — Due Soon Query

Due Soon shows upcoming deadlines.

Recommended maximum:

```text
10
```

items.

---

# 32. Due Soon Sort

```text
deadline ASC
```

then stable secondary sort.

---

# 33. Due Soon Display

Recommended:

```text
Business ID

Order Name

Client

Deadline

Priority

Status
```

---

# 34. Deadline Emphasis

Deadline should be visually prominent.

Do not rely only on color.

Display the actual date.

---

# 35. Task 5 — Overdue Query

Overdue Orders should be visible separately from Due Soon if any exist.

---

# 36. Overdue Sort

Recommended:

```text
oldest missed deadline first
```

or:

```text
most recently overdue first
```

Choose one consistent approach.

Recommended:

```text
oldest deadline first
```

because it surfaces longest overdue work.

---

# 37. Overdue Status

Completed and Cancelled Orders are excluded.

On Hold Orders remain overdue if their deadline passed.

This accurately reflects the stored deadline.

---

# 38. On Hold Interpretation

Do not automatically extend/remove deadlines when Order enters:

```text
On Hold
```

That would require explicit business rules not currently defined.

---

# 39. Task 6 — Recent Orders

Recent Orders helps users return quickly to newly created/current work.

---

# 40. Recent Definition

Recommended:

```text
created_at DESC
```

for initial implementation.

---

# 41. Recently Updated Alternative

Later, a separate:

```text
Recently Updated
```

section may use:

```text
updated_at DESC
```

Do not mix the concepts invisibly.

---

# 42. Recent Orders Fields

Display:

```text
Business ID

Name

Client

Project

Status

Created
```

---

# 43. Recent Limit

Recommended:

```text
10
```

---

# 44. Cancelled Orders in Recent List

Recent Orders may include recently created Cancelled Orders.

However, if this creates noise, Version 1 may exclude Cancelled by default.

Recommended:

```text
exclude Cancelled
```

from Dashboard operational lists.

Reports remain available for complete history.

---

# 45. Task 7 — Recent Projects

Display recently created Projects.

---

# 46. Recent Project Fields

Recommended:

```text
Business ID

Project Name

Client

Status

Owner
```

---

# 47. Recent Project Sort

```text
created_at DESC
```

---

# 48. Recent Project Limit

Recommended:

```text
5–10
```

Use a compact layout.

---

# 49. Project Navigation

Clicking row opens:

```text
/projects/{projectId}
```

---

# 50. Task 8 — Dashboard API

Prefer a dedicated endpoint:

```text
GET /api/dashboard
```

returning the small set of required Dashboard data.

---

# 51. Why One Dashboard Endpoint

The Dashboard loads several small related summaries.

One endpoint can reduce:

```text
multiple round trips

duplicated frontend loading logic
```

while keeping backend query responsibilities clear.

---

# 52. Dashboard Response

Conceptually:

```text
summary:
    activeOrders
    urgentOrders
    dueSoonOrders
    overdueOrders

activeOrders: []

urgentOrders: []

dueSoonOrders: []

overdueOrders: []

recentOrders: []

recentProjects: []
```

Sections may be omitted/null when User lacks permission.

---

# 53. Permission-Aware Response

If User lacks:

```text
orders.view
```

return no Order sections.

If User lacks:

```text
projects.view
```

return no Project section.

Do not return data and depend on frontend to hide it.

---

# 54. Partial Dashboard

Dashboard remains useful even if User has access to only part of the ERP.

Example:

```text
Operator
→ Orders only
```

may see operational Order sections without Project administration.

---

# 55. Dashboard DTOs

Use compact DTOs.

Do not return full:

```text
OrderDetailDto

ProjectDetailDto
```

for Dashboard cards.

---

# 56. Dashboard Order DTO

Recommended:

```text
id

businessId

name

clientName

projectName

orderTypeName

status

priority

deadline
```

---

# 57. Dashboard Project DTO

Recommended:

```text
id

businessId

name

clientName

status

ownerName
```

---

# 58. No Checklist Data

Do not load Checklist Items into Dashboard.

---

# 59. No Calculator Values

Do not load Calculator field data into Dashboard.

---

# 60. No Cost Items

Do not load Cost Items into Dashboard.

---

# 61. Summary Count Query

Counts should be calculated server-side.

Do not load Order rows just to count them in React.

---

# 62. Query Reuse

Dashboard may reuse shared query/filter helpers from Reports where appropriate.

Do not copy/paste entire report queries.

---

# 63. Avoid Coupling to Report DTOs

Reusing query logic is good.

Forcing Dashboard to use oversized Report DTOs is not.

Use Dashboard-specific projections.

---

# 64. Task 9 — Frontend Route

Dashboard route:

```text
/dashboard
```

---

# 65. Root Route

Authenticated root:

```text
/
```

may redirect to:

```text
/dashboard
```

---

# 66. Login Redirect

After successful login, default destination:

```text
/dashboard
```

unless a preserved safe return URL exists.

---

# 67. Application Navigation

Dashboard should be the first primary navigation item.

---

# 68. Dashboard Layout

Recommended desktop layout:

```text
Summary Cards

Operational Attention
├── Overdue
├── Urgent
└── Due Soon

Current Work
└── Active Orders

Recent Activity
├── Recent Orders
└── Recent Projects
```

Exact visual arrangement may adapt to screen width.

---

# 69. Avoid Excessive Cards

Do not create one card for every available number.

Four useful metrics are better than twenty decorative ones.

---

# 70. Summary Card Example

```text
Active Orders
24
```

Clicking may navigate to:

```text
/orders?status=active
```

---

# 71. Urgent Card

May navigate to:

```text
/orders?priority=urgent
```

with closed statuses excluded if supported by list query state.

---

# 72. Due Soon Card

May navigate to Orders list with equivalent deadline filters.

If exact Dashboard filter cannot be represented cleanly in current URL/API, navigation may simply open Orders page.

Do not create fragile query syntax purely for card links.

---

# 73. Overdue Card

Same principle.

---

# 74. Operational Attention Emphasis

Overdue and Urgent sections should be more visually noticeable than Recent sections.

Use hierarchy, not distracting animation.

---

# 75. No Alarm Animation

Do not use:

```text
flashing

blinking

continuous pulsing
```

for overdue work.

Business software should remain calm and readable.

---

# 76. Priority Styling

Use the central Order priority component.

Do not invent separate Dashboard priority colors/styles.

---

# 77. Status Styling

Use the same status component used elsewhere.

---

# 78. Dates

Use shared date formatting.

---

# 79. Client/Project Names

Display enough context to identify the work without overloading each row.

---

# 80. Empty Dashboard Sections

If there are no Overdue Orders:

```text
No overdue Orders.
```

This is useful positive information.

---

# 81. Empty Urgent Section

```text
No urgent Orders.
```

---

# 82. Loading State

Dashboard should show skeleton/loading states rather than an empty layout during load.

---

# 83. Error Handling

If Dashboard query fails:

```text
Dashboard could not be loaded.
```

Provide Retry.

Do not redirect the User unnecessarily.

---

# 84. Partial Failure Strategy

Version 1 dedicated endpoint will generally succeed/fail as one request.

Do not build complicated independent widget failure recovery unless real need appears.

---

# 85. Refresh Behavior

A manual:

```text
Refresh
```

action is optional.

Normal browser refresh and page revisit are sufficient initially.

---

# 86. No Real-Time Requirement

Dashboard does not require:

```text
WebSockets

SignalR

live streaming
```

in Version 1.

---

# 87. Freshness

Dashboard data is refreshed when:

```text
Page loads

User revisits Dashboard

User manually refreshes if provided
```

This is sufficient.

---

# 88. No Polling by Default

Do not poll the Dashboard every few seconds.

It is unnecessary for Lithograph's initial workflow.

---

# 89. Task 10 — Optional Financial Summary

Financial cards are optional.

Do not add them merely because data exists.

---

# 90. Possible Financial Cards

If later approved:

```text
Selling Total

Cost Total

Profit
```

for a defined period.

---

# 91. Period Must Be Explicit

Never display an unlabeled:

```text
Profit
```

number on Dashboard.

It must clearly state the period, for example:

```text
This Month
```

---

# 92. Financial Data Source

Reuse Reports aggregation logic.

Do not calculate separate competing financial totals.

---

# 93. Financial Cancelled Rule

Use the same default:

```text
Cancelled Orders excluded
```

as Reports.

---

# 94. Financial Permission Rules

Selling card:

```text
orders.view_selling_price
```

Cost card:

```text
orders.view_cost_price
```

Profit card:

```text
both
```

---

# 95. Initial Recommendation

For first Dashboard implementation:

```text
Do not include financial cards yet.
```

Start with operational workflow.

Financial analysis already exists in Reports.

---

# 96. Task 11 — Drill-Down

Every actionable Dashboard item should lead to the source Workspace.

---

# 97. Order Drill-Down

```text
Dashboard Order
→ /orders/{id}
```

---

# 98. Project Drill-Down

```text
Dashboard Project
→ /projects/{id}
```

---

# 99. Summary Drill-Down

Summary cards may navigate to filtered list screens where appropriate.

---

# 100. No Inline Editing

Dashboard does not edit:

```text
Order status

Priority

Deadline

Project Team
```

in Version 1.

Open the source Workspace.

---

# 101. Why No Inline Editing

This keeps Dashboard:

```text
simple

read-focused

safe

consistent
```

and avoids duplicating module forms.

---

# 102. Task 12 — Optional User-Relevant Dashboard

Version 1 Dashboard is company/permission-based.

Do not initially personalize sections as:

```text
My Orders
```

because Order responsibilities are inherited from Project and Users/Employees are separate.

---

# 103. Future My Work View

Later, if useful:

```text
My Projects

Orders where my Employee is Owner/Assignee/Participant
```

may be added.

This requires resolving:

```text
Current User
→ linked Employee
```

---

# 104. No Linked Employee Assumption

A User may have no Employee link.

Therefore Dashboard must not depend on linked Employee for basic operation.

---

# 105. Task 13 — Backend Tests

Required Dashboard tests:

```text
Summary counts

Active definition

Urgent definition

Due Soon definition

Overdue definition

Recent Orders ordering

Recent Projects ordering

Permission filtering
```

---

# 106. Active Count Test

Given:

```text
Draft: 2
Active: 5
On Hold: 3
Completed: 4
Cancelled: 1
```

Active count must be:

```text
5
```

---

# 107. Urgent Count Test

Count only:

```text
priority = urgent
```

and non-closed statuses.

---

# 108. Completed Urgent Test

An Order that is:

```text
priority = urgent
status = completed
```

must not appear in Urgent Dashboard section.

---

# 109. Cancelled Urgent Test

Cancelled urgent Order excluded.

---

# 110. Due Soon Boundary Test

If today is controlled as:

```text
2026-09-26
```

then with 7-day window:

```text
2026-09-26
through
2026-10-03
```

are included if using inclusive boundaries.

Test boundaries explicitly.

---

# 111. Overdue Boundary Test

Deadline:

```text
today - 1 day
```

→ overdue.

Deadline:

```text
today
```

→ not overdue.

---

# 112. Completed Deadline Test

Completed Order with past deadline must not appear overdue.

---

# 113. Cancelled Deadline Test

Cancelled Order with past deadline must not appear overdue.

---

# 114. On Hold Overdue Test

On Hold Order with past deadline:

```text
appears overdue
```

under current rule.

---

# 115. Null Deadline Test

Order without deadline appears in neither:

```text
Due Soon

Overdue
```

---

# 116. Recent Orders Test

Verify result order uses:

```text
created_at DESC
```

and respects item limit.

---

# 117. Recent Projects Test

Same.

---

# 118. Permission Test — Orders

User without:

```text
orders.view
```

must not receive Dashboard Order sections.

---

# 119. Permission Test — Projects

User without:

```text
projects.view
```

must not receive Recent Projects section.

---

# 120. Authentication Test

Unauthenticated Dashboard request:

```text
401
```

---

# 121. Financial Leakage Test

If financial cards are not implemented, Dashboard DTO must not accidentally contain:

```text
sellingPrice

costPrice

profit
```

unless required by a visible section and authorized.

---

# 122. Query Efficiency Test

Dashboard should not issue one query for every row.

Review generated SQL/query count.

---

# 123. Reasonable Query Count

A handful of focused queries is acceptable.

Do not combine everything into one unreadable SQL monster merely to reduce query count to one.

---

# 124. Performance Target Philosophy

Dashboard should feel fast with the expected Lithograph dataset.

Do not introduce caching before measuring a real problem.

---

# 125. Index Review

Dashboard commonly filters:

```text
orders.status

orders.priority

orders.deadline

orders.created_at

projects.created_at
```

Review indexes together with Reports performance.

Do not duplicate equivalent indexes unnecessarily.

---

# 126. Task 14 — Frontend Tests

Test:

```text
Loading state

Error state

Empty sections

Summary cards

Permission-hidden sections

Order navigation

Project navigation
```

---

# 127. Responsive Test

Dashboard should remain usable on:

```text
Desktop

Tablet
```

Mobile optimization is not required.

---

# 128. Browser Refresh Test

Directly opening:

```text
/dashboard
```

and refreshing must work.

---

# 129. Dashboard Commit Strategy

Recommended:

```text
feat(dashboard): add dashboard queries

feat(dashboard): add operational summary

feat(frontend): add dashboard layout

test(dashboard): add deadline and permission tests
```

---

# 130. First AI Coding Task

Recommended:

```text
Read:
- AI_RULES.md
- docs/06_UI_UX_Principles.md
- docs/14_Orders_Module.md
- docs/16_Reports_Module.md
- docs/19_API_Design_Guidelines.md
- docs/20_Testing_Strategy.md
- docs/23_Frontend_Architecture.md
- docs/24_Backend_Architecture.md
- docs/25_Development_Workflow_for_AI.md
- docs/35_Reports_Implementation_Plan.md
- docs/36_Dashboard_Implementation_Plan.md

Task:
Implement only the Dashboard backend.

Create:
- GET /api/dashboard
- Active Order count/list
- Urgent Order count/list
- Due Soon count/list
- Overdue count/list
- Recent Orders
- Recent Projects
- permission-aware response
- compact DTOs
- integration tests

Rules:
- no dashboard tables
- no stored counters
- no financial cards
- no WebSockets
- no background jobs
- no polling infrastructure
- no inline editing
- reuse existing query patterns where appropriate

Before finishing:
- build
- run tests
- inspect query behavior
- verify permission filtering
```

---

# 131. Second AI Coding Task

```text
Implement Dashboard frontend.

Create:
- /dashboard
- summary cards
- Overdue section
- Urgent section
- Due Soon section
- Active Orders section
- Recent Orders
- Recent Projects
- loading/error/empty states
- drill-down links

Reuse existing:
- Order status components
- Priority components
- date formatting
- PageHeader/layout
```

---

# 132. Third AI Coding Task

```text
Add Dashboard navigation behavior.

Requirements:
- authenticated root redirects to /dashboard
- login success goes to Dashboard unless a safe return URL exists
- card/list drill-down works
- browser Back/Forward remains normal
```

---

# 133. Dashboard Completion Gate

Dashboard is complete when:

```text
Active Orders count/list correct

Urgent Orders correct

Due Soon definition correct

Overdue definition correct

Recent Orders correct

Recent Projects correct

Permissions enforced on backend

No unauthorized data returned

Navigation works

Loading state works

Error state works

Empty states work

No dashboard persistence tables exist

No duplicated business logic exists

Tests pass
```

---

# 134. Dashboard Must Not Become Reports

Do not gradually add every financial/report table to Dashboard.

If the User wants detailed analysis:

```text
Dashboard
→ Reports
```

---

# 135. Dashboard Must Not Become Orders

Do not add full Order editing forms.

If the User needs to modify an Order:

```text
Dashboard
→ Order Workspace
```

---

# 136. Dashboard Must Not Become Projects

Do not add Project Team management directly.

Use Project Workspace.

---

# 137. Dashboard Must Not Become Notification System

Version 1 does not require:

```text
Notifications

Unread badges

Push alerts

Email alerts

Desktop alerts
```

---

# 138. Dashboard Must Not Become Scheduler

Do not create:

```text
Calendar

Gantt chart

Production scheduler
```

inside Dashboard.

These are separate possible future features.

---

# 139. No Employee Monitoring

Do not use Dashboard to create:

```text
employee productivity score

employee ranking

time tracking
```

without explicit future requirements.

---

# 140. No AI Insights

Version 1 Dashboard does not need:

```text
AI recommendations

automatic risk scoring

predictive deadlines
```

---

# 141. No Arbitrary Widgets

Do not create user-configurable widget placement in Version 1.

Use one curated layout.

---

# 142. No Drag-and-Drop Dashboard

Dashboard does not need a widget designer.

---

# 143. No Persistent Personal Layout

Do not create User dashboard-layout tables.

---

# 144. No Chart Library Requirement

Do not add a charting dependency until a real chart is approved.

---

# 145. Future Dashboard Extensions

Possible future additions:

```text
My Work

Financial period cards

Overdue Projects

Recently Updated Orders

Production KPIs

Charts

Calendar view

Machine workload
```

Only add based on real operating needs.

---

# 146. Dashboard Simplicity Rule

Before adding a Dashboard element, ask:

```text
Does this help the User decide
what to work on next?
```

If not, it probably belongs in Reports or another module.

---

# 147. Operational Hierarchy

Dashboard should reinforce the existing ERP structure:

```text
Client
   ↓
Project
   ↓
Order
```

not introduce a new parallel work model.

---

# 148. Dashboard Data Flow

Conceptually:

```text
Source Tables
     ↓
Dashboard Read Queries
     ↓
Compact Dashboard DTO
     ↓
React Dashboard
     ↓
Drill-down
     ↓
Existing Workspace
```

---

# 149. Final Dashboard Principle

The Dashboard should answer:

```text
What is active?

What is urgent?

What is overdue?

What is due soon?

What was recently created?

Where should I click next?
```

It should do this without duplicating:

```text
Reports

Order Workspace

Project Workspace
```

The central rule is:

```text
Dashboard shows attention.

Workspaces perform work.

Reports analyze results.
```

---

**End of Document**