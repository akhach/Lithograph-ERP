# Lithograph ERP

**Document:** 23_Frontend_Architecture.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `01_Technology_Stack.md`
- `02_Architecture.md`
- `04_Data_Dictionary.md`
- `06_UI_UX_Principles.md`
- `07_Authentication.md`
- `10_Users_Module.md`
- `11_Employees_Module.md`
- `12_Clients_Module.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `16_Reports_Module.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`

---

# 1. Purpose

This document defines the frontend architecture for Lithograph ERP Version 1.

The frontend is responsible for:

```text
User Interface

Navigation

Forms

Tables

Workspaces

Permission-aware presentation

API interaction

Client-side validation

Responsive desktop behavior
```

The frontend is not the authoritative source for business rules or security.

---

# 2. Technology

Version 1 frontend uses:

```text
React

TypeScript

Material UI
```

The application runs in a modern web browser.

---

# 3. Frontend Principle

The frontend should be organized primarily around business modules.

Prefer:

```text
modules/
├── auth/
├── employees/
├── clients/
├── projects/
├── orders/
├── calculator/
└── reports/
```

rather than placing all business components in large global folders.

---

# 4. Recommended Source Structure

A practical structure:

```text
src/
├── app/
├── api/
├── components/
├── hooks/
├── layouts/
├── modules/
├── routes/
├── theme/
├── types/
├── utilities/
└── main.tsx
```

This is a guideline.

Do not create folders that contain no useful code merely to follow architecture theory.

---

# 5. app Folder

The `app/` area contains application-level infrastructure.

Possible contents:

```text
App.tsx

providers

authentication state

application configuration

global error handling
```

It should not contain module-specific business forms.

---

# 6. modules Folder

Each business module keeps its own frontend code together.

Example:

```text
modules/
└── clients/
    ├── api/
    ├── components/
    ├── pages/
    ├── hooks/
    ├── types/
    └── utilities/
```

Only create subfolders that are actually useful.

---

# 7. Shared Components

Reusable application-wide components belong in:

```text
components/
```

Examples:

```text
PageHeader

ConfirmDialog

EmptyState

ErrorAlert

LoadingIndicator

PermissionGuard

BusinessIdText
```

Do not move a component into shared space until it is genuinely reused.

---

# 8. Layouts

Version 1 should have a standard authenticated application layout.

Conceptually:

```text
Application
│
├── Header
├── Navigation
└── Main Content
```

---

# 9. Main Navigation

Primary navigation:

```text
Dashboard

Clients

Projects

Orders

Reports

Administration
```

Administration may contain:

```text
Users

Roles

Employees

Order Types

Calculator Templates
```

---

# 10. Navigation Permissions

Navigation entries should be shown only when the User has the relevant Permission.

Example:

```text
users.view
```

controls normal visibility of:

```text
Administration → Users
```

However, hiding navigation is not security.

Backend authorization remains authoritative.

---

# 11. Routing

Use stable frontend routes.

Recommended examples:

```text
/login

/dashboard

/clients
/clients/:clientId

/projects
/projects/:projectId

/orders
/orders/:orderId

/reports

/admin/users
/admin/roles
/admin/employees
/admin/order-types
/admin/calculator-templates
/admin/calculator-templates/:templateId
```

---

# 12. Deep Links

Important records should have stable URLs.

Example:

```text
/orders/{id}
```

should directly open that Order Workspace.

This allows:

- Bookmarking
- Browser refresh
- Sharing internal links
- Returning to records from reports

---

# 13. Route IDs

Frontend routes should use UUID IDs internally.

Business IDs remain displayed in the UI.

Example URL:

```text
/orders/8f2...
```

UI Header:

```text
ORD-2026-000425
```

---

# 14. Login Route

Unauthenticated Users should normally see:

```text
/login
```

except for first-run Initial Setup when applicable.

---

# 15. Protected Routes

Authenticated routes must verify that a valid Session/current User exists.

If not authenticated:

```text
Redirect to Login
```

Do not render protected application data first and then hide it.

---

# 16. Permission-Protected Routes

Certain Administration pages require specific Permissions.

Example:

```text
/admin/users
```

requires:

```text
users.view
```

Frontend may display:

```text
Access denied
```

or redirect appropriately.

Backend still independently checks permissions.

---

# 17. Authentication State

Frontend needs application-level knowledge of:

```text
Is User authenticated?

Who is the current User?

Which Roles do they have?

Which Permissions do they have?

Is an Employee linked?
```

This information may come from:

```text
GET /api/auth/me
```

---

# 18. Current User Model

Conceptual frontend type:

```text
CurrentUser

id
username
roles[]
permissions[]
linked_employee?
```

Do not include sensitive authentication information.

---

# 19. Permission Checking

Use one shared Permission-checking mechanism.

Conceptually:

```text
hasPermission("orders.create")
```

Do not implement slightly different Permission logic in every component.

---

# 20. Permission Helper

A shared helper/hook may expose:

```text
hasPermission

hasAnyPermission

hasAllPermissions
```

only if actually useful.

Keep the API simple.

---

# 21. Permission Guard Component

A shared UI helper may conceptually support:

```text
<PermissionGuard permission="orders.create">
    <CreateOrderButton />
</PermissionGuard>
```

Exact implementation may differ.

---

# 22. Permission Guard Is Presentation Only

`PermissionGuard` does not secure backend endpoints.

It only controls UI visibility.

Never treat it as authorization.

---

# 23. API Layer

Frontend should use a shared API layer.

Do not place raw API requests directly throughout random components.

---

# 24. Module API Files

Example:

```text
modules/clients/api/clientsApi.ts

modules/projects/api/projectsApi.ts

modules/orders/api/ordersApi.ts

modules/calculator/api/calculatorApi.ts
```

or equivalent.

---

# 25. Shared API Client

A common HTTP client should handle:

```text
Base URL

Credentials/session handling

JSON serialization

Standard error parsing

Request cancellation where useful
```

---

# 26. API Errors

Backend error responses should be converted into a consistent frontend error model.

Conceptually:

```text
ApiError

code
message
fieldErrors
```

Components should not parse arbitrary HTTP response formats independently.

---

# 27. 401 Handling

If backend returns:

```text
401 Unauthorized
```

frontend should recognize that authentication is no longer valid.

Typical behavior:

```text
Clear current authentication state

Redirect to Login
```

---

# 28. 403 Handling

For:

```text
403 Forbidden
```

do not treat User as logged out.

Display:

```text
You do not have permission to perform this action.
```

or equivalent.

---

# 29. 404 Handling

Record detail routes should have a clear not-found state.

Example:

```text
Order not found.
```

Do not leave the page indefinitely loading.

---

# 30. 409 Handling

Conflicts should have clear business messages.

Example:

```text
This Calculator was modified by another User.

Reload the latest data before saving.
```

---

# 31. Forms

Forms should use consistent patterns.

At minimum:

```text
Labels

Validation

Save

Cancel

Loading state

Backend error display
```

---

# 32. Explicit Save

Version 1 generally prefers:

```text
Explicit Save
```

for business forms.

Do not introduce uncontrolled auto-save across the application.

Calculator may use a specialized save model defined by its module.

---

# 33. Save Button

While saving:

```text
Disable repeated submission

Show progress
```

Example:

```text
Saving...
```

---

# 34. Cancel Behavior

Cancel should return to a predictable previous context or discard unsaved changes.

Do not silently save when User expects Cancel.

---

# 35. Unsaved Changes

For forms where losing work would be costly, frontend may warn before navigating away.

Do not add unsaved-change prompts to every trivial dialog.

---

# 36. Frontend Validation

Client-side validation improves usability.

Examples:

```text
Required Name

Password confirmation

Invalid obvious email format

Missing Order Type
```

Backend performs authoritative validation again.

---

# 37. Backend Validation Display

When backend returns field errors, map them to form fields where possible.

Example:

```text
name:
Name is required.
```

---

# 38. Dialog vs Page

Use dialogs for small operations.

Examples:

```text
Reset Password

Add Checklist Item

Simple Employee edit
```

Use pages/workspaces for records with meaningful context.

Examples:

```text
Project

Order

Calculator Template
```

---

# 39. List Screens

Major list screens:

```text
Clients

Projects

Orders

Employees

Users

Order Types

Calculator Templates
```

should use broadly consistent table behavior.

---

# 40. Standard Table Behavior

Useful common capabilities:

```text
Sorting

Filtering

Search

Pagination

Row selection where needed
```

Do not enable every capability on every table automatically.

---

# 41. Server-Side Data Operations

Large lists should use server-side:

```text
Pagination

Filtering

Sorting

Search
```

Do not load all Orders into browser memory and then filter locally.

---

# 42. Table Pagination

List screens should preserve:

```text
Page

Page Size

Current Filters

Current Sort
```

while the User remains on the screen.

---

# 43. URL Query State

Where useful, table state may be represented in URL query parameters.

Example:

```text
/orders?status=active&page=2
```

This makes refresh/back navigation more predictable.

It is recommended for important list filters but not mandatory everywhere.

---

# 44. Table Row Interaction

For desktop workflows:

```text
Single Click
→ Select row where selection is useful

Double Click
→ Open record
```

This convention may be used consistently in business tables.

---

# 45. Open Action

Tables should also provide an explicit open action where necessary for accessibility and discoverability.

Do not rely entirely on double-click.

---

# 46. Client List

Recommended columns:

```text
Business ID

Name

Contact Person

Phone

Status
```

---

# 47. Project List

Recommended columns:

```text
Business ID

Client

Project Name

Status

Deadline

Owner
```

---

# 48. Orders List

Recommended columns:

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

Optional financial columns depend on Permissions.

---

# 49. Financial Columns

Display:

```text
Selling Price

Cost Price

Profit
```

only where permitted.

Backend must also omit protected data.

---

# 50. Currency Formatting

Use one shared money-formatting utility.

Do not manually format currency differently in multiple pages.

Exact currency configuration can be centralized.

---

# 51. Date Formatting

Use one shared date formatting approach.

Database/API date values remain ISO.

Frontend renders human-readable values consistently.

---

# 52. Client Details

Client Details may contain:

```text
Header

General Information

Projects
```

---

# 53. Client Header

Example:

```text
CL-000125

Samsung Armenia
```

Business ID should be visually easy to find.

---

# 54. Client Project List

Client Details may show related Projects.

The Projects module remains the source of that data.

Do not duplicate Project state inside the Client frontend module.

---

# 55. Project Workspace

Recommended layout:

```text
Project Header

General Information

Project Team

Orders
```

This is a central business Workspace.

---

# 56. Project Header

Display:

```text
Project Business ID

Project Name

Client

Status

Deadline
```

Important identification should remain visible.

---

# 57. Project Team UI

Recommended conceptual layout:

```text
Owner
[ Employee ▼ ]

Assignee
[ Employee ▼ ]

Participants
[ Employee ] [ Employee ] [+ Add]

Observers
[ Employee ] [+ Add]
```

Only active Employees should normally appear in new selectors.

---

# 58. Project Team Display Names

Employee selector should display:

```text
Full Name — Position
```

where Position exists.

---

# 59. Project Team Permissions

If User lacks:

```text
projects.manage_team
```

Project Team should be visible if they may view the Project, but editing controls must be unavailable.

---

# 60. Orders Inside Project

The Project Workspace should show related Orders.

The most important action:

```text
Create Order
```

should automatically use the current Project.

---

# 61. Order Workspace

Recommended sections:

```text
Order Header

General

Calculator

Checklist

Folder Links
```

The exact implementation may use:

```text
Tabs

Sections

Panels
```

depending on UX quality.

---

# 62. Order Header

Display:

```text
Order Business ID

Order Name

Project

Client

Status

Priority
```

This context should remain easy to see.

---

# 63. General Order Section

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

# 64. Inherited Project Team

Order Workspace may show:

```text
Owner

Assignee

Participants

Observers
```

as read-only Project context.

Do not create Order-specific team editing in Version 1.

---

# 65. Project Team Navigation

Where useful, provide a link/action:

```text
Open Project
```

to change the team at Project level.

---

# 66. Checklist UI

Checklist should remain extremely simple.

Example:

```text
☑ Receive artwork
☑ Print test sample
☐ Customer approval
☐ Production
```

---

# 67. Add Checklist Item

Use a clear action:

```text
+ Add Item
```

The new row contains:

```text
Checkbox

Text
```

only.

---

# 68. Checklist Reordering

If reordering is implemented, use simple:

```text
Drag handle
```

or:

```text
Move Up / Move Down
```

Do not complicate Checklist behavior.

---

# 69. Checklist Progress

May show:

```text
3 / 5 completed
```

or a simple progress indicator.

Progress is derived.

---

# 70. Folder Links UI

Example:

```text
Artwork
\\server\orders\...\artwork

Production
\\server\orders\...\production
```

Version 1 displays/copies paths.

It does not directly launch Windows Explorer.

---

# 71. Copy Path

A convenient frontend action may be:

```text
Copy Path
```

because copying text is browser-safe.

This does not require desktop integration.

---

# 72. Calculator Workspace

Calculator is embedded into the Order Workspace.

It should not feel like a completely disconnected application.

---

# 73. Calculator Layout

Conceptually:

```text
Calculator Header

Input Sections

Calculated Values

Cost Items

Final Selling Price

Final Cost Price
```

subject to permissions.

---

# 74. Calculator Template Rendering

Frontend renders controls dynamically from:

```text
Template Version definition
```

Supported element types include:

```text
Label

Number Input

Text Input

Dropdown

Checkbox

Calculated Field

Section

Table
```

as defined by Calculator specification.

---

# 75. Template Rendering Component

Use a dedicated Calculator rendering layer.

Do not write a unique React form manually for every Order Type.

---

# 76. Field Component Mapping

Conceptually:

```text
number
→ NumberField

text
→ TextField

dropdown
→ SelectField

checkbox
→ CheckboxField

calculated
→ CalculatedValue
```

Keep this mapping controlled.

---

# 77. Calculator Field Keys

React form logic must use stable internal:

```text
field_key
```

rather than displayed labels.

Labels may change.

Keys are calculation identities.

---

# 78. Calculator Form State

Calculator state should be isolated to the Order Calculator feature.

Do not store every Calculator field globally across the entire application.

---

# 79. Calculator Recalculation

When input changes, dependent calculated fields may update quickly in the UI.

Backend result remains authoritative when saving.

---

# 80. Formula Logic in Frontend

Frontend may use compatible calculation logic for immediate UX.

Do not create business logic that exists only in React.

Final calculations must be validated by backend.

---

# 81. Formula Errors

Display formula/input errors near affected fields when practical.

Example:

```text
Selling Price cannot be calculated because Quantity is empty.
```

Avoid showing low-level parser exceptions.

---

# 82. Calculator Save State

UI should clearly communicate:

```text
Unsaved

Saving

Saved

Error
```

if Calculator uses controlled auto-save or manual save.

Choose one consistent model.

---

# 83. Calculator Version Display

Order Calculator should allow the User to see which Template Version is being used.

Example:

```text
UV Printing Calculator — v3
```

This may be secondary information.

---

# 84. Cost Table

Cost Items should use a simple editable table.

Columns:

```text
Category

Supplier

Date

Description

Amount
```

---

# 85. Cost Total

Display:

```text
Cost Total
```

clearly below the table.

This corresponds to Order Cost Price.

---

# 86. Cost Permissions

If User lacks:

```text
calculator.view_costs
```

do not render Cost Item data.

The API must also not return it.

---

# 87. Calculator Selling Permissions

Selling-related sections should respect:

```text
orders.view_selling_price
```

or the final approved Selling permission model.

---

# 88. Template Designer

Template Designer belongs under Administration.

Recommended conceptual structure:

```text
Template Header

Version Selector

Element Palette

Design Surface

Properties Panel

Preview/Test
```

---

# 89. Template Designer V1 Simplicity

Do not start with a complex free-form Excel-like drag canvas.

The initial designer may use ordered Sections and Fields.

This is easier to build reliably and can later become more visual.

---

# 90. Template Version Selector

Display:

```text
v1 Published

v2 Published

v3 Draft
```

clearly.

Published versions are read-only.

Draft versions are editable.

---

# 91. Publish Button

Only Users with:

```text
calculator.publish_templates
```

should see/enable publication controls.

---

# 92. Published Template UI

Published Template Version should show:

```text
Published

Read-only
```

and provide:

```text
Create New Version
```

rather than Edit.

---

# 93. Reports Frontend

Reports should use:

```text
Filters

Summary

Results Table
```

as the standard structure.

---

# 94. Report Filters

Examples:

```text
Date Range

Client

Project

Order Type

Status

Priority

Owner

Assignee
```

Only show filters relevant to that report.

---

# 95. Report Summary

Possible summary cards:

```text
Orders

Selling Total

Cost Total

Profit
```

Only show authorized financial values.

---

# 96. Report Drill-Down

Rows should allow opening:

```text
Client

Project

Order
```

through normal application routes.

---

# 97. Dashboard

Version 1 Dashboard should be simple.

Possible sections:

```text
Active Orders

Urgent Orders

Due Soon

Recent Projects

Recent Orders
```

---

# 98. Dashboard Permissions

Dashboard must not reveal financial values or records the User cannot otherwise access.

---

# 99. Global State

Avoid putting all application data into one global state container.

Use global state only for cross-application concerns such as:

```text
Authentication

Current User

Theme/configuration if needed
```

Business page data should remain near the relevant module/page.

---

# 100. State Management Library

Do not add a large state-management library automatically.

Start with normal React patterns and focused data-fetching/state tools.

Introduce more infrastructure only if real complexity requires it.

---

# 101. Server State vs UI State

Keep distinction clear:

```text
Server State
=
Data from API

UI State
=
Dialog open, selected tab, temporary form values
```

Do not duplicate server data unnecessarily in global UI state.

---

# 102. Data Fetching

Use a consistent data-fetching approach.

It should support:

```text
Loading

Error

Refetch

Caching where useful

Request cancellation
```

The exact maintained library may be selected during implementation.

---

# 103. Avoid Duplicate Fetch Logic

Do not implement separate custom loading/error patterns for every module if one shared pattern works.

---

# 104. Cache Safety

Do not let cached frontend data cause sensitive information to remain visible after:

```text
Logout

User change

Permission change
```

Clear relevant client cache/state on logout.

---

# 105. Mutation Refresh

After successful changes, refresh or update relevant server state.

Example:

```text
Deactivate Client
```

should update:

```text
Client Details

Client List
```

without requiring full browser reload.

---

# 106. Forms and Types

Frontend forms should use TypeScript types based on API request contracts.

Example:

```text
CreateProjectRequest
```

should not be the same as:

```text
ProjectDetailDto
```

because server-controlled fields differ.

---

# 107. Type Safety

Avoid widespread:

```text
any
```

in business code.

Use explicit TypeScript types.

---

# 108. Enum Types

Status values should use shared stable TypeScript unions/enums.

Example concept:

```text
type OrderStatus =
  | "draft"
  | "active"
  | "on_hold"
  | "completed"
  | "cancelled";
```

Match backend serialization exactly.

---

# 109. Avoid Duplicated Constants

Do not define Order statuses independently in many files.

Use one module-level source.

---

# 110. Business Logic Location

Frontend may handle UI logic such as:

```text
Show warning

Disable invalid option

Format data
```

but critical business rules remain enforced by backend.

---

# 111. Example

Frontend may disable inactive Clients in Project creation.

Backend must still reject:

```text
inactive client_id
```

if submitted manually.

---

# 112. Confirmation Dialogs

Use confirmations for high-impact actions.

Examples:

```text
Deactivate User

Deactivate Client

Cancel Project

Cancel Order

Publish Template Version

Delete Cost Item

Reset Calculator
```

---

# 113. Avoid Confirmation Fatigue

Do not add confirmation dialogs to harmless actions such as:

```text
Open record

Change filter

Search

Switch tab
```

---

# 114. Notifications

Use simple temporary feedback for successful actions.

Examples:

```text
Client created.

Changes saved.

Template Version published.
```

Do not create a full notification center in Version 1.

---

# 115. Error Feedback

Errors should be visible near the affected operation.

Do not silently fail.

---

# 116. Empty States

Every important empty list/section should have a useful message.

Examples:

```text
No Orders yet.

No Checklist Items yet.

No Folder Links yet.

No Cost Items yet.
```

Where permitted, include the next action.

---

# 117. Loading States

Avoid showing blank pages during data fetch.

Use:

```text
Progress indicator

Skeleton

Loading text
```

according to context.

---

# 118. Accessibility

Use standard HTML/Material UI controls.

Maintain:

```text
Keyboard navigation

Visible focus

Labels

Reasonable contrast

Button text/tooltips
```

---

# 119. Keyboard Navigation

Normal application interaction should support:

```text
Tab

Shift+Tab

Enter

Escape
```

where appropriate.

---

# 120. Browser Shortcut Safety

Do not override standard shortcuts such as:

```text
Ctrl+T

Ctrl+W

Ctrl+L

Ctrl+N
```

---

# 121. Custom Shortcuts

Version 1 does not require a large custom keyboard shortcut system.

Add shortcuts only when a clear repetitive workflow benefits.

---

# 122. Responsive Design

Primary target:

```text
Desktop
```

Secondary target:

```text
Tablet
```

Mobile optimization is not a Version 1 requirement.

---

# 123. Minimum Useful Width

Business tables and Calculator may require wide layouts.

Do not damage desktop usability merely to force all workflows into narrow mobile screens.

---

# 124. Material UI

Use Material UI components consistently.

Do not mix several unrelated component frameworks.

---

# 125. Theme

Create one centralized application theme.

Define:

```text
Typography

Spacing

Component defaults

Application colors
```

in one location.

Do not hardcode style decisions across dozens of components.

---

# 126. Dark Mode

Version 1 does not require Dark Mode.

Do not delay business features for theme variants.

---

# 127. CSS Strategy

Prefer Material UI styling/system consistently.

Avoid a mixture of:

```text
Inline styles

Global CSS

Several CSS frameworks

Random component-level style methods
```

without a clear reason.

---

# 128. Business ID Presentation

Business IDs should be visually easy to recognize.

Examples:

```text
CL-000125

PRJ-2026-000125

ORD-2026-000425
```

Use a shared presentation component if repeated enough.

---

# 129. UUID Presentation

Normal Users should rarely see raw UUIDs.

UUIDs are technical identifiers.

---

# 130. Status Presentation

Use consistent status components/badges.

Examples:

```text
Draft

Active

On Hold

Completed

Cancelled
```

The same status should look consistent across List and Workspace views.

---

# 131. Priority Presentation

Likewise:

```text
Low

Normal

High

Urgent
```

should have one consistent UI representation.

Do not create conflicting priority styles across screens.

---

# 132. Inactive Master Records

Inactive:

```text
Employees

Clients

Order Types

Templates
```

should be clearly identifiable when shown historically.

---

# 133. Active Selectors

New-assignment selectors should normally show only active records.

Examples:

```text
Employee selector

Client selector

Order Type selector
```

---

# 134. Historical Display

Existing records must still display linked inactive entities.

Example:

```text
Old Order
→ inactive Order Type
```

must show the historical relationship correctly.

---

# 135. Selector Search

Selectors with potentially many items should allow typing/search.

Examples:

```text
Client

Project

Employee

Order Type
```

---

# 136. Selector Display

Use enough context to distinguish records.

Client:

```text
CL-000125 — Samsung Armenia
```

Project:

```text
PRJ-2026-000125 — New Store Opening
```

Employee:

```text
Samvel Petrosyan — Operator
```

---

# 137. Create-and-Select

Version 1 does not require every selector to support inline creation.

Prefer explicit master-data workflows unless repeated use proves inline creation valuable.

---

# 138. Breadcrumbs

Workspaces may use simple breadcrumbs if navigation benefits.

Example:

```text
Projects
>
PRJ-2026-000125
>
ORD-2026-000425
```

Do not overcomplicate navigation.

---

# 139. Back Navigation

Prefer normal browser navigation compatibility.

Do not create custom navigation behavior that breaks the browser Back button.

---

# 140. Browser Refresh

Refreshing a detail page should restore the same record using its stable route.

Important application state should not exist only in memory.

---

# 141. Administration Layout

Administration can use a consistent sub-navigation:

```text
Users

Roles

Employees

Order Types

Calculator Templates
```

Only authorized entries appear.

---

# 142. User Administration

Users page should support:

```text
List

Create

Edit Username

Activate/Deactivate

Reset Password

Manage Roles
```

Do not include Employee HR data.

---

# 143. Employee Administration

Employees page supports:

```text
List

Create

Edit

Activate/Deactivate

Link User
```

---

# 144. Roles Administration

Roles interface may contain:

```text
Role Details

Permission Groups
```

Permissions should be grouped by module for readability.

---

# 145. Order Type Administration

Supports:

```text
Name

Description

Calculator Template

Active Status
```

---

# 146. Calculator Template Administration

Supports:

```text
Template List

Versions

Draft Editing

Preview

Publish
```

---

# 147. Component Size

Avoid components that grow into thousands of lines.

Split meaningful UI responsibilities.

Example Order Workspace may have:

```text
OrderHeader

OrderGeneralSection

OrderCalculatorSection

OrderChecklistSection

OrderFolderLinksSection
```

---

# 148. Avoid Micro-Components

Do not split every label/button into its own file.

Component boundaries should follow meaningful behavior or reuse.

---

# 149. Module Boundaries

Frontend module A should not freely import internal implementation details from module B.

Prefer:

```text
Public types/components/hooks
```

or application-level composition.

Keep dependencies understandable.

---

# 150. Cross-Module Example

Order Workspace needs Project and Client context.

It may consume an Order Detail DTO that already contains compact:

```text
project

client
```

rather than directly reaching into multiple unrelated internal API implementations for trivial display data.

---

# 151. Avoid Frontend Joins

Do not make the browser manually join large independent datasets when backend can efficiently return the required projection.

Example:

Orders List should not:

```text
Load all Orders

Load all Projects

Load all Clients

Join in browser
```

Backend should return display projection.

---

# 152. Lazy Loading Sections

Heavy Workspace sections may load only when needed.

Example:

```text
Calculator
```

could load when its tab is opened.

Do this when it meaningfully improves performance.

---

# 153. Avoid Premature Lazy Complexity

Do not split every tiny section into asynchronous chunks before performance problems exist.

---

# 154. Preview Images

Gallery and Order Header may display optimized Preview Images.

Do not load full-resolution production artwork.

---

# 155. Broken Preview Image

If image cannot load:

```text
Show placeholder
```

rather than breaking layout.

---

# 156. No File Manager

Frontend does not become a production file manager in Version 1.

Folder Links remain references.

---

# 157. Copy-to-Clipboard

Useful safe browser actions may include:

```text
Copy Business ID

Copy Folder Path
```

where practical.

---

# 158. Reports Export

If future export is implemented, it should be initiated from Reports UI.

Do not mix export buttons into every table before needed.

---

# 159. Frontend Logging

Frontend may log unexpected development errors during development.

Production should avoid excessive console logging.

Never log:

```text
Passwords

Session secrets

Sensitive financial payloads unnecessarily
```

---

# 160. Error Boundary

An application-level React Error Boundary may handle unexpected rendering failures.

It should show a safe fallback rather than a blank page.

---

# 161. Development Error Information

Detailed frontend errors may be visible during development.

Production should show simpler user-facing messages.

---

# 162. Testing

Frontend tests should follow:

```text
20_Testing_Strategy.md
```

Focus on:

```text
Forms

Permissions

Critical interactions

Calculator rendering

Error/loading states
```

---

# 163. Testable Components

Keep business components structured so behavior can be tested without running the entire application.

Avoid tightly coupling every component to global browser state.

---

# 164. Mocking API

Component tests may mock the API boundary.

End-to-End tests should use the real backend.

---

# 165. Frontend Build

Production frontend build must:

```text
Compile TypeScript

Fail on important type errors

Produce optimized static assets
```

Do not ignore TypeScript errors for production builds.

---

# 166. Linting and Formatting

Use one consistent formatter/linter configuration.

Do not spend excessive development time debating minor formatting rules.

Automate them.

---

# 167. TypeScript Strictness

Use reasonably strict TypeScript settings.

Avoid disabling type safety globally merely to make AI-generated code compile.

Fix incorrect types.

---

# 168. Dependency Rule

Before adding an npm dependency, ask:

```text
Does this solve a real recurring problem better than the existing stack?
```

Avoid adding libraries for trivial utilities.

---

# 169. Dependency Security

Use maintained dependencies.

Remove unused packages.

Avoid abandoned UI/formula libraries for critical features.

---

# 170. Package Lock

Commit the appropriate package lock file.

Production and development should install deterministic dependency versions.

---

# 171. Frontend Environment Configuration

Frontend may receive non-secret configuration such as:

```text
API base URL
```

Secrets must never be embedded in the React build.

Anything shipped to browser should be considered visible to Users.

---

# 172. Backend Base URL

Use environment/configuration rather than hardcoding developer machine addresses.

Do not scatter:

```text
http://localhost:5000
```

through module code.

---

# 173. No Business Secrets in Frontend

Never put:

```text
Database credentials

Signing keys

Internal secrets
```

in frontend configuration.

---

# 174. Initial Frontend Implementation Order

Recommended:

```text
1. App Shell

2. Login

3. Authentication State

4. Navigation

5. Users Administration

6. Employees

7. Clients

8. Projects

9. Orders

10. Calculator

11. Reports

12. Dashboard refinement
```

This follows backend implementation dependencies.

---

# 175. Module Completion Rule

A frontend module is not complete until:

```text
Loading state works

Error state works

Empty state works

Permissions work

Forms validate

API integration works

Navigation works

Relevant tests pass
```

---

# 176. Frontend Non-Goals

Version 1 does not require:

```text
Mobile App

Offline Mode

Real-Time Collaborative Editing

Global Drag-and-Drop System

Dark Mode

Full Keyboard Shortcut System

Public Customer UI

Supplier Portal

Complex Notification Center

Generic Form Builder outside Calculator

Generic Dashboard Builder

Desktop Native Shell
```

---

# 177. AI Coding Rules for Frontend

Before modifying frontend code, AI must:

```text
Read AI_RULES.md

Read relevant module document

Inspect existing components

Inspect existing API client patterns

Reuse established structure

Avoid unrelated refactoring
```

---

# 178. AI Must Not Rebuild Working UI

If a shared component/pattern already exists, extend or reuse it.

Do not regenerate:

```text
Navigation

API Client

Table system

Authentication state
```

for each new module.

---

# 179. AI Task Scope

Good frontend task:

```text
Implement Clients List page using the existing table and API patterns.

Read:
- AI_RULES.md
- 12_Clients_Module.md
- 19_API_Design_Guidelines.md
- 23_Frontend_Architecture.md

Do not modify Projects or Orders.
```

---

# 180. Avoid Giant Generated Pages

Do not ask AI to implement:

```text
All frontend pages for the entire ERP
```

in one operation.

Build and test one feature at a time.

---

# 181. Frontend Consistency Checklist

Before completing a page, verify:

```text
Correct route

Correct permission

Loading state

Error state

Empty state

Consistent table/form patterns

Backend errors handled

No sensitive fields exposed

Browser refresh works

Relevant navigation works
```

---

# 182. Workspace Consistency

Client, Project, and Order screens should feel like parts of one ERP.

Use consistent:

```text
Headers

Spacing

Actions

Status presentation

Tables

Save behavior
```

---

# 183. Productivity Principle

This is internal business software.

Prefer:

```text
Fewer clicks

Clear tables

Visible important information

Fast navigation
```

over decorative effects.

---

# 184. Visual Design Principle

The frontend should feel:

```text
Professional

Clean

Dense enough for real work

Readable

Predictable
```

It should not look like a marketing website.

---

# 185. Animation

Use subtle UI transitions only where useful.

Do not add distracting animation to business workflows.

---

# 186. Main Frontend Data Flow

Conceptually:

```text
User Action
    ↓
React Component
    ↓
Module API Layer
    ↓
Backend REST API
    ↓
Response DTO
    ↓
Frontend State
    ↓
Updated UI
```

---

# 187. Security Data Flow

```text
Current User
    ↓
Permissions
    ↓
UI Visibility
```

but authoritative security remains:

```text
API Request
    ↓
Backend Authorization
```

---

# 188. Order Workspace Data Flow

```text
Order Detail
     │
     ├── Project Context
     ├── Client Context
     ├── Project Team
     ├── General Data
     ├── Calculator
     ├── Checklist
     └── Folder Links
```

Do not duplicate these data relationships in frontend storage.

---

# 189. Calculator Data Flow

```text
Template Version
       ↓
Dynamic Renderer
       ↓
User Inputs
       ↓
Frontend Calculation Preview
       ↓
Backend Save / Validation
       ↓
Authoritative Result
       ↓
Selling Price
```

---

# 190. Final Frontend Principle

The Lithograph ERP frontend should make the underlying business structure obvious:

```text
Client
   ↓
Project
   ↓
Order
```

with supporting:

```text
Employees

Authentication

Calculator

Reports
```

The frontend should not invent a second architecture separate from the backend.

The core rule is:

```text
Business modules define the structure.

The frontend makes that structure fast and clear to use.
```

---

**End of Document**