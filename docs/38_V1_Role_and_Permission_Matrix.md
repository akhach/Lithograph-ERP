# Lithograph ERP

**Document:** 38_V1_Role_and_Permission_Matrix.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Area:** Authentication / Authorization

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `07_Authentication.md`
- `08_Auth_Schema_Design.md`
- `09_Auth_Tables.md`
- `10_Users_Module.md`
- `11_Employees_Module.md`
- `12_Clients_Module.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `16_Reports_Module.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `23_Frontend_Architecture.md`
- `24_Backend_Architecture.md`
- `27_Authentication_Implementation_Plan.md`
- `28_Employees_Implementation_Plan.md`
- `29_Clients_Implementation_Plan.md`
- `30_Projects_Implementation_Plan.md`
- `31_Orders_Implementation_Plan.md`
- `32_Calculator_Foundation_Implementation_Plan.md`
- `33_Order_Calculator_Implementation_Plan.md`
- `34_Costs_Implementation_Plan.md`
- `35_Reports_Implementation_Plan.md`
- `37_V1_Integration_and_Release_Plan.md`

---

# 1. Purpose

This document consolidates all Lithograph ERP Version 1 permissions into one authoritative access-control matrix.

It defines:

```text id="dn8jve"
Permission codes

Permission meaning

Protected operations

Financial visibility rules

Starter Role profiles

Role design principles

Permission testing requirements
```

This document does not replace module documentation.

It consolidates authorization decisions from all modules.

---

# 2. Core Authorization Model

Lithograph ERP uses:

```text id="obs3he"
User
   ↓
User Roles
   ↓
Roles
   ↓
Role Permissions
   ↓
Permissions
```

Version 1 does not use direct User-to-Permission assignments.

---

# 3. Authorization Source of Truth

Backend permission checks are authoritative.

Frontend permission checks are only for:

```text id="xvq61p"
Navigation visibility

Button visibility

Read-only states

Better user experience
```

The frontend must never be the security boundary.

---

# 4. User vs Employee

Permissions belong to:

```text id="jeubte"
User
```

through Authentication Roles.

Permissions do not belong to:

```text id="dhlgk7"
Employee
```

Employee represents a person in business operations.

---

# 5. Employee Position Is Not Security

Example:

```text id="nkjd78"
Employee.position = "Operator"
```

does not automatically grant:

```text id="z3h7du"
Operator Authentication Role
```

These concepts remain independent.

---

# 6. Project Roles Are Not Authentication Roles

Project roles:

```text id="wmyh07"
Owner

Assignee

Participant

Observer
```

describe Project responsibility.

They do not automatically grant system permissions.

---

# 7. Permission Code Convention

Permission codes follow:

```text id="4rqczx"
module.action
```

Examples:

```text id="j42msk"
users.view

projects.manage_team

orders.view_cost_price

calculator.publish_templates
```

---

# 8. Permission Code Stability

Once used in production, permission codes should be treated as stable identifiers.

Avoid renaming them casually because they may be referenced by:

```text id="a2d4sk"
Roles

Seed/synchronization logic

Tests

Frontend permission checks
```

---

# 9. Permission Catalog Ownership

The backend application owns the permission catalog.

Normal Users do not invent arbitrary permission codes through the UI.

---

# 10. Director Role

`Director` is a protected system Role.

Director should effectively receive all current permissions.

New permissions added by future modules should be synchronized to Director automatically.

---

# 11. Director Is Not a Username Check

Do not implement:

```text id="33b69e"
if username == "director"
```

for authorization.

Authorization must use Role/Permission data.

---

# 12. System Role Protection

Director Role may be protected from:

```text id="g8o2tw"
Deletion

Accidental disabling

Changes that violate final-Director invariant
```

while still remaining represented through the normal authorization model.

---

# 13. Authentication Permission Group

Version 1 Authentication permissions:

```text id="27l1h1"
users.view

users.create

users.edit

users.activate

users.reset_password

users.manage_roles

roles.view

roles.create

roles.edit

roles.manage_permissions
```

---

# 14. `users.view`

Allows:

```text id="dmkff3"
View User list

View User details

View Username

View Roles

View active/inactive state

View last-login information where exposed
```

Does not allow modification.

---

# 15. `users.create`

Allows:

```text id="1ifd8p"
Create local ERP User accounts
```

Does not automatically allow:

```text id="y9a0wx"
Role assignment

Password reset of existing Users

User deactivation
```

unless combined with corresponding permissions.

---

# 16. Creating User With Roles

If User creation UI allows Role selection during creation, backend must require both:

```text id="wwavqr"
users.create
+
users.manage_roles
```

for the Role-assignment part.

---

# 17. `users.edit`

Allows:

```text id="0bsjl8"
Edit normal User properties

Change Username
```

subject to uniqueness rules.

Does not allow:

```text id="2jc6k7"
Password reset

Role changes

Activation changes
```

---

# 18. `users.activate`

Allows:

```text id="jtx4sj"
Activate User

Deactivate User
```

subject to final active Director protection.

---

# 19. `users.reset_password`

Allows an authorized administrator to set a new password for another User.

It does not reveal the existing password.

---

# 20. `users.manage_roles`

Allows:

```text id="c0u2aj"
Assign Role to User

Remove Role from User
```

subject to final Director protection.

---

# 21. `roles.view`

Allows:

```text id="9ad1ol"
View Roles

View Role Permission assignments
```

where appropriate.

---

# 22. `roles.create`

Allows creating configurable Authentication Roles.

---

# 23. `roles.edit`

Allows editing normal Role metadata such as:

```text id="oyhjsg"
Name

Description
```

subject to protected system-role rules.

---

# 24. `roles.manage_permissions`

Allows:

```text id="670bzi"
Assign Permission to Role

Remove Permission from Role
```

This is a high-impact administrative permission.

---

# 25. Permission Definition Management

Version 1 does not require:

```text id="y9j1re"
permissions.create

permissions.delete
```

because Permissions originate from application features.

---

# 26. Employees Permission Group

Version 1:

```text id="gvl0ah"
employees.view

employees.create

employees.edit

employees.activate

employees.link_user
```

---

# 27. `employees.view`

Allows:

```text id="smomk6"
View Employee list

View Employee details

Use Employees in permitted selectors
```

---

# 28. `employees.create`

Allows creation of Employee records.

Does not automatically grant ability to create User accounts.

---

# 29. `employees.edit`

Allows editing:

```text id="mzj82c"
Full Name

Position

Phone

Email
```

---

# 30. `employees.activate`

Allows:

```text id="c1rqp5"
Activate Employee

Deactivate Employee
```

This does not activate/deactivate linked User.

---

# 31. `employees.link_user`

Allows:

```text id="jmm722"
Link Employee to User

Unlink Employee from User
```

This does not grant:

```text id="hkzi5x"
User creation

Role management

Password management
```

---

# 32. Clients Permission Group

Version 1:

```text id="7277js"
clients.view

clients.create

clients.edit

clients.activate
```

---

# 33. `clients.view`

Allows:

```text id="dfzcwv"
View Client list

View Client details

Use Clients in permitted selectors
```

---

# 34. `clients.create`

Allows creating Clients.

Backend generates Client Business ID.

---

# 35. `clients.edit`

Allows editing normal Client fields.

Does not permit changing:

```text id="ezy4ye"
Client Business ID
```

---

# 36. `clients.activate`

Allows:

```text id="kl4747"
Activate Client

Deactivate Client
```

Historical records remain valid.

---

# 37. Projects Permission Group

Version 1:

```text id="2jnbqj"
projects.view

projects.create

projects.edit

projects.manage_team

projects.change_status
```

---

# 38. `projects.view`

Allows:

```text id="qtf21o"
View Project list

View Project Workspace

View Project Team

View normal Project details
```

Financial information remains governed separately by Order financial permissions.

---

# 39. `projects.create`

Allows creation of Projects.

Does not automatically permit Team management.

---

# 40. `projects.edit`

Allows editing Project general information:

```text id="roek2e"
Client where allowed

Name

Description

Start Date

Deadline
```

Does not change Project Team.

Does not change Project status if status uses dedicated permission.

---

# 41. `projects.manage_team`

Allows managing:

```text id="blzk7a"
Owner

Assignee

Participants

Observers
```

---

# 42. `projects.change_status`

Allows changing Project status among approved values.

Does not automatically change Order statuses.

---

# 43. Orders Permission Group

Version 1:

```text id="6kkrag"
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

# 44. `orders.view`

Allows:

```text id="5a1u3r"
View Orders list

View Order Workspace

View non-financial Order information

View inherited Project/Client context

View Project Team context
```

It does not automatically allow financial fields.

---

# 45. `orders.create`

Allows creation of new Orders under eligible Projects.

---

# 46. `orders.edit`

Allows editing normal Order information according to lifecycle rules.

Examples:

```text id="fsimvj"
Project while allowed

Order Type while allowed

Name

Description

Priority

Deadline
```

Does not authorize Calculator values or Cost Items.

---

# 47. `orders.change_status`

Allows changes among:

```text id="dtsmol"
Draft

Active

On Hold

Completed

Cancelled
```

according to module rules.

---

# 48. `orders.manage_checklist`

Allows:

```text id="72niyi"
Add Checklist Item

Edit Checklist Item

Complete/Uncomplete Item

Reorder Item

Delete Item
```

---

# 49. `orders.manage_folder_links`

Allows:

```text id="z0o8ym"
Add Folder Link

Edit Folder Link

Reorder Folder Link

Delete Folder Link
```

It does not grant filesystem access.

---

# 50. `orders.manage_types`

Allows Administration of:

```text id="puc3q3"
Order Types
```

including:

```text id="r6x3jf"
Create

Edit

Activate

Deactivate

Assign Calculator Template
```

once Calculator integration exists.

---

# 51. `orders.view_selling_price`

Allows viewing:

```text id="ae4a5o"
Order Selling Price

Selling totals

Selling-sensitive Calculator output
```

where applicable.

This is a financial permission.

---

# 52. `orders.view_cost_price`

Allows viewing:

```text id="ccxavb"
Order aggregate Cost Price

Cost totals in permitted reports
```

It does not automatically grant detailed Cost Item access.

---

# 53. Cost Price vs Cost Items

Important distinction:

```text id="c5bm6k"
orders.view_cost_price
=
view aggregate Order Cost Price
```

```text id="nsu2uj"
calculator.view_costs
=
view detailed Cost Items
```

---

# 54. Profit Visibility

There is no separate:

```text id="ounhdc"
orders.view_profit
```

permission in V1.

Profit may be shown only if User can see both:

```text id="0rjnmv"
orders.view_selling_price
+
orders.view_cost_price
```

---

# 55. Calculator Permission Group

Version 1:

```text id="uytvcb"
calculator.view

calculator.edit

calculator.manage_templates

calculator.publish_templates

calculator.view_costs

calculator.edit_costs
```

---

# 56. `calculator.view`

Allows viewing an Order Calculator's general permitted Calculator structure and values.

Financially protected fields remain subject to financial permissions.

---

# 57. `calculator.edit`

Allows editing permitted Order Calculator input values and saving calculations.

Does not allow:

```text id="a0m7m4"
Template editing

Template publication

Cost Item editing
```

---

# 58. Calculator Selling Data

If a Calculator field is Selling-sensitive, visibility must also require:

```text id="h8o2sw"
orders.view_selling_price
```

according to the final Calculator DTO rules.

---

# 59. `calculator.manage_templates`

Allows:

```text id="qpqvmq"
Create Calculator Template

Edit Template metadata

Create Draft Versions

Edit Draft Definitions

Validate Drafts
```

---

# 60. `calculator.publish_templates`

Allows:

```text id="xg53w9"
Publish valid Draft Template Version
```

This is intentionally separate from Template editing.

---

# 61. `calculator.view_costs`

Allows viewing detailed:

```text id="7v2gk6"
Cost Items

Supplier text

Cost Category

Cost descriptions

Cost amounts

Cost-sensitive Calculator fields
```

where applicable.

---

# 62. `calculator.edit_costs`

Allows:

```text id="w3qc0r"
Create Cost Item

Edit Cost Item

Delete Cost Item

Reorder Cost Items
```

---

# 63. Cost Edit Role Design

Normal Role configuration should generally assign:

```text id="3mglfl"
calculator.view_costs
```

together with:

```text id="yesyk6"
calculator.edit_costs
```

but backend must still enforce both independently where appropriate.

---

# 64. Reports Permission Group

Version 1:

```text id="7jxoxa"
reports.view
```

---

# 65. `reports.view`

Allows access to the Reports area and non-protected report information.

It does not automatically reveal financial data.

---

# 66. Orders Report Financial Rules

Selling columns require:

```text id="njt0vl"
orders.view_selling_price
```

Cost columns require:

```text id="r11kyp"
orders.view_cost_price
```

Profit requires both.

---

# 67. Cost Report Rule

Detailed Cost Summary requires:

```text id="ihh44t"
reports.view
+
calculator.view_costs
```

---

# 68. Dashboard Permissions

Version 1 does not require a separate:

```text id="ydcbmp"
dashboard.view
```

permission.

Any authenticated User may open Dashboard.

Dashboard sections depend on their module permissions.

---

# 69. Dashboard Orders Sections

Require:

```text id="xhi5uy"
orders.view
```

---

# 70. Dashboard Project Sections

Require:

```text id="cn1xee"
projects.view
```

---

# 71. Dashboard Financial Data

If financial cards are added later, they must use the same financial permissions as Reports.

---

# 72. Permission Summary

Canonical V1 permission catalog:

```text id="4iwr91"
users.view
users.create
users.edit
users.activate
users.reset_password
users.manage_roles

roles.view
roles.create
roles.edit
roles.manage_permissions

employees.view
employees.create
employees.edit
employees.activate
employees.link_user

clients.view
clients.create
clients.edit
clients.activate

projects.view
projects.create
projects.edit
projects.manage_team
projects.change_status

orders.view
orders.create
orders.edit
orders.change_status
orders.manage_checklist
orders.manage_folder_links
orders.manage_types
orders.view_selling_price
orders.view_cost_price

calculator.view
calculator.edit
calculator.manage_templates
calculator.publish_templates
calculator.view_costs
calculator.edit_costs

reports.view
```

---

# 73. Permission Count

The current V1 catalog contains:

```text id="825bjt"
40 permissions
```

If implementation contains more or fewer, review the difference against current approved documentation.

---

# 74. Permission Dependency Principle

Avoid implementing complicated permission inheritance in code.

Example:

```text id="idjxdj"
orders.edit
```

should not silently mean:

```text id="j97e8d"
orders.view_selling_price
```

Role configuration should explicitly grant needed capabilities.

---

# 75. Reasonable UI Assumptions

The frontend may assume that certain useful Role combinations normally exist.

The backend must not assume this.

---

# 76. Starter Roles

V1 may start with practical Roles such as:

```text id="22ma16"
Director

Manager

Operator

Cost User
```

These are suggested configuration profiles.

Only Director is a protected system role.

---

# 77. Roles Are Configurable

Do not hardcode business logic such as:

```text id="0nw9mb"
if role == "Operator"
```

for normal module authorization.

Check Permissions.

---

# 78. Director Starter Profile

Director receives:

```text id="vf2hp1"
ALL CURRENT PERMISSIONS
```

and future application Permissions through synchronization.

---

# 79. Manager Role Purpose

A Manager is a practical optional starter Role for people coordinating Clients, Projects, Orders, and pricing.

It is not mandatory for the application to function.

---

# 80. Suggested Manager Permissions — Administration

Recommended:

```text id="j3h59f"
employees.view

clients.view
clients.create
clients.edit
clients.activate

projects.view
projects.create
projects.edit
projects.manage_team
projects.change_status

orders.view
orders.create
orders.edit
orders.change_status
orders.manage_checklist
orders.manage_folder_links
orders.view_selling_price

calculator.view
calculator.edit

reports.view
```

---

# 81. Manager Cost Visibility

Whether Manager should also receive:

```text id="n0cnwt"
orders.view_cost_price

calculator.view_costs

calculator.edit_costs
```

is a business configuration choice.

Do not assume it automatically.

---

# 82. Manager System Administration

Recommended initial Manager does not receive:

```text id="gdutky"
users.create

users.reset_password

roles.manage_permissions

calculator.publish_templates
```

unless that person is also responsible for system administration.

---

# 83. Operator Role Purpose

Operator is a practical production/operational Role.

Possible responsibilities:

```text id="xset7i"
View Projects

View Orders

Update Order operational information

Manage Checklists

View Folder Links

Use Calculator where permitted
```

---

# 84. Suggested Operator Permissions

Possible starting configuration:

```text id="l3awmr"
clients.view

projects.view

orders.view
orders.edit
orders.change_status
orders.manage_checklist
orders.manage_folder_links

calculator.view
calculator.edit
```

---

# 85. Operator Selling Price

If Lithograph wants Operators to see Selling Price, also grant:

```text id="086j8u"
orders.view_selling_price
```

This is a configurable policy decision.

---

# 86. Operator Cost Access

Recommended default:

```text id="e6ebm9"
No detailed Cost access
```

unless job responsibilities require it.

---

# 87. Cost User Role Purpose

Cost User is a practical Role for a person who records/controls direct Order Costs.

---

# 88. Suggested Cost User Permissions

Possible initial configuration:

```text id="tyyl0a"
clients.view

projects.view

orders.view
orders.view_cost_price

calculator.view
calculator.view_costs
calculator.edit_costs

reports.view
```

---

# 89. Cost User Selling Access

Do not automatically grant:

```text id="qcu69c"
orders.view_selling_price
```

unless required.

This allows Cost management without commercial-price visibility.

---

# 90. Cost User Profit

Without Selling Price access, Cost User does not see Profit.

---

# 91. General Employee Role

A highly restricted Role may be useful.

Example:

```text id="8yvqjt"
projects.view

orders.view
```

with no financial access and no mutation rights.

---

# 92. No Default Role Explosion

Do not create:

```text id="wsrwnz"
15–20 predefined Roles
```

during V1 setup.

Start small.

Add Roles as real responsibilities emerge.

---

# 93. Suggested Starter Matrix

A possible initial matrix:

| Permission | Director | Manager | Operator | Cost User |
|---|---:|---:|---:|---:|
| users.view | ✓ |  |  |  |
| users.create | ✓ |  |  |  |
| users.edit | ✓ |  |  |  |
| users.activate | ✓ |  |  |  |
| users.reset_password | ✓ |  |  |  |
| users.manage_roles | ✓ |  |  |  |
| roles.view | ✓ |  |  |  |
| roles.create | ✓ |  |  |  |
| roles.edit | ✓ |  |  |  |
| roles.manage_permissions | ✓ |  |  |  |
| employees.view | ✓ | ✓ |  |  |
| employees.create | ✓ |  |  |  |
| employees.edit | ✓ |  |  |  |
| employees.activate | ✓ |  |  |  |
| employees.link_user | ✓ |  |  |  |
| clients.view | ✓ | ✓ | ✓ | ✓ |
| clients.create | ✓ | ✓ |  |  |
| clients.edit | ✓ | ✓ |  |  |
| clients.activate | ✓ | ✓ |  |  |
| projects.view | ✓ | ✓ | ✓ | ✓ |
| projects.create | ✓ | ✓ |  |  |
| projects.edit | ✓ | ✓ |  |  |
| projects.manage_team | ✓ | ✓ |  |  |
| projects.change_status | ✓ | ✓ |  |  |
| orders.view | ✓ | ✓ | ✓ | ✓ |
| orders.create | ✓ | ✓ |  |  |
| orders.edit | ✓ | ✓ | ✓ |  |
| orders.change_status | ✓ | ✓ | ✓ |  |
| orders.manage_checklist | ✓ | ✓ | ✓ |  |
| orders.manage_folder_links | ✓ | ✓ | ✓ |  |
| orders.manage_types | ✓ |  |  |  |
| orders.view_selling_price | ✓ | ✓ | optional |  |
| orders.view_cost_price | ✓ | optional |  | ✓ |
| calculator.view | ✓ | ✓ | ✓ | ✓ |
| calculator.edit | ✓ | ✓ | ✓ |  |
| calculator.manage_templates | ✓ |  |  |  |
| calculator.publish_templates | ✓ |  |  |  |
| calculator.view_costs | ✓ | optional |  | ✓ |
| calculator.edit_costs | ✓ | optional |  | ✓ |
| reports.view | ✓ | ✓ |  | ✓ |

---

# 94. Matrix Is a Starting Configuration

The starter matrix above is not hardcoded business logic.

Director may change normal Role assignments through Administration.

---

# 95. Optional Cells

Cells marked:

```text id="gsiato"
optional
```

represent decisions Lithograph may configure based on actual staff responsibility.

Do not make the backend infer them.

---

# 96. Suggested Role: Manager With Full Finance

If a Manager is responsible for profitability and Costs, grant additionally:

```text id="zuox1o"
orders.view_cost_price

calculator.view_costs

calculator.edit_costs
```

Then Manager may also see Profit because they already have Selling access.

---

# 97. Suggested Role: Manager Without Costs

If the Manager should know Selling Price but not supplier/direct Cost detail:

```text id="djh2m4"
orders.view_selling_price = yes

orders.view_cost_price = no

calculator.view_costs = no
```

Profit remains unavailable.

---

# 98. Suggested Role: Commercial Manager

A future configuration may grant:

```text id="dc51qu"
clients.*

projects.view/create/edit/manage_team/change_status

orders.view/create/edit/change_status

orders.view_selling_price

calculator.view/edit

reports.view
```

without detailed Cost access.

No new Permission type is required.

---

# 99. Suggested Role: Production Operator

Could receive:

```text id="ks1i9o"
projects.view

orders.view

orders.edit

orders.change_status

orders.manage_checklist

orders.manage_folder_links
```

without Calculator editing if production staff should not modify commercial calculation inputs.

---

# 100. Suggested Role: Designer

Could receive a restricted combination such as:

```text id="beq51c"
clients.view

projects.view

orders.view

orders.manage_checklist

orders.manage_folder_links
```

depending on Lithograph workflow.

Again, no code changes should be needed.

---

# 101. Finance Visibility Model

Financial data has three useful levels:

```text id="u8yawy"
Level 0
No financial visibility

Level 1
Selling Price only

Level 2
Aggregate Cost Price

Level 3
Detailed Cost Items
```

A User may receive different combinations.

---

# 102. Financial Level 0

No:

```text id="x8g1yy"
orders.view_selling_price

orders.view_cost_price

calculator.view_costs
```

User sees operational Order information only.

---

# 103. Financial Level 1 — Selling Only

Grant:

```text id="k927gq"
orders.view_selling_price
```

Allows commercial pricing visibility.

---

# 104. Financial Level 2 — Aggregate Cost

Grant:

```text id="iy36uh"
orders.view_cost_price
```

Allows aggregate Cost Price visibility.

This does not show supplier-level detail.

---

# 105. Financial Level 3 — Detailed Cost

Grant:

```text id="hpg6nv"
calculator.view_costs
```

for detailed Cost rows.

If editing required:

```text id="yzu1n0"
calculator.edit_costs
```

---

# 106. Profit Visibility Formula

Conceptually:

```text id="l6nrzw"
CanViewProfit
=
HasPermission("orders.view_selling_price")
AND
HasPermission("orders.view_cost_price")
```

No direct Profit Permission is needed in V1.

---

# 107. Detailed Cost Does Not Imply Selling

A Cost User may have:

```text id="nqd2um"
calculator.view_costs
orders.view_cost_price
```

without:

```text id="icsoah"
orders.view_selling_price
```

and therefore no Profit access.

---

# 108. Selling Does Not Imply Cost

Likewise commercial staff may see Selling without Costs.

---

# 109. Calculator Permission Combination

A User with:

```text id="auvf7r"
calculator.view
calculator.edit
```

may operate non-protected Calculator inputs.

They still cannot see Selling-sensitive fields without the Selling permission.

---

# 110. Template Administration Separation

A person who edits Template Drafts does not necessarily have publication authority.

Possible configuration:

```text id="lavgtx"
calculator.manage_templates = yes

calculator.publish_templates = no
```

---

# 111. Four-Eyes Template Control

If Lithograph later wants separation of duties:

```text id="tb1dns"
Person A edits Template

Person B publishes Template
```

the current permission model already supports it.

No schema change required.

---

# 112. Reports Without Finance

`reports.view` can still be useful without financial access.

Possible visible columns:

```text id="3uxkjf"
Order count

Status

Client

Project

Order Type

Priority

Deadlines
```

---

# 113. Reports Financial Permission Reuse

Do not create report-specific duplicates such as:

```text id="1ycyzk"
reports.view_selling

reports.view_cost

reports.view_profit
```

in V1.

Reuse source-data permissions.

---

# 114. Cost Report Exception

Detailed Cost Report requires:

```text id="1hvf1b"
calculator.view_costs
```

because it exposes Cost Item details.

---

# 115. Selector Permissions

Selectors should not necessarily require broad Administration permissions.

Example:

A User permitted to create a Project may need to select Employees.

Backend/UI should support only the minimum read information needed.

---

# 116. Employees Selector

A Project Team selector conceptually needs access to:

```text id="6kdtt8"
Employee ID

Full Name

Position

Active state
```

The implementation should avoid exposing unrelated Employee data unnecessarily.

---

# 117. Client Selector

Project creation requires active Client selection.

A User with:

```text id="02s2cl"
projects.create
```

will normally also need:

```text id="23nl2r"
clients.view
```

through Role configuration.

Do not silently grant it in code.

---

# 118. Project Selector

Order creation normally requires:

```text id="eeosr6"
projects.view
```

along with:

```text id="nlgwwj"
orders.create
```

through Role configuration.

---

# 119. Order Type Selector

Users creating/editing Orders need selector access to active Order Types.

This does not require granting full:

```text id="e3xnah"
orders.manage_types
```

Order Type selector reads should be available through the Order workflow as appropriate.

---

# 120. Permission Dependencies Are Configuration Guidance

Useful Role combinations should be documented but not encoded as hard dependencies unless security genuinely requires them.

---

# 121. High-Risk Permissions

The following deserve additional care:

```text id="s081mz"
roles.manage_permissions

users.manage_roles

users.reset_password

calculator.publish_templates

calculator.edit_costs

orders.view_cost_price

calculator.view_costs
```

---

# 122. Why `roles.manage_permissions` Is High Risk

A User with this capability can potentially expand what a Role can access.

This effectively changes authorization policy.

---

# 123. Why `users.manage_roles` Is High Risk

A User may grant powerful existing Roles to Users.

---

# 124. Why Password Reset Is High Risk

Resetting another User's password can provide access to that account.

Technical audit/logging should make such administrative actions diagnosable.

---

# 125. Why Template Publication Is High Risk

Published Templates affect pricing rules for future Order Calculators.

---

# 126. Why Cost Editing Is High Risk

Cost edits directly affect:

```text id="p6wge4"
Order Cost Price

Profit

Reports
```

---

# 127. No Role Hierarchy

Version 1 does not require Role inheritance such as:

```text id="j04ejn"
Director > Manager > Operator
```

Each Role simply contains Permissions.

---

# 128. No Deny Permissions

Version 1 uses positive grants.

Do not implement:

```text id="tdudm6"
explicit deny
```

semantics.

---

# 129. Permission Union

If User has multiple Roles:

```text id="gh2s7a"
Effective Permissions
=
union of all Role Permissions
```

---

# 130. Example

Role A:

```text id="7pnmnq"
orders.view
```

Role B:

```text id="z6llez"
orders.view_selling_price
```

User receives both capabilities.

---

# 131. No Permission Conflict Resolution

Because V1 has no deny permissions, Role combinations do not require conflict precedence.

---

# 132. User Deactivation

An inactive User has no normal ERP access regardless of assigned Roles.

---

# 133. Employee Deactivation

Employee inactivity does not revoke User Permissions.

These are separate lifecycles.

---

# 134. Final Director Invariant

The system must always preserve at least:

```text id="tpj6od"
one active User
with Director Role
```

after initial setup.

---

# 135. Director Permission Synchronization

When a new permission is introduced:

```text id="nr02pw"
Register Permission

Ensure Director Role has Permission
```

as part of controlled synchronization.

---

# 136. Normal Roles and New Permissions

Do not automatically grant new permissions to every existing normal Role.

New capabilities should be reviewed.

---

# 137. Example

If future permission is added:

```text id="a5cmsj"
orders.attachments.manage
```

Director receives it automatically.

Manager/Operator do not until intentionally configured.

---

# 138. Permission Administration UI

Administration → Roles should allow:

```text id="ufk3eq"
View Role

Create Role

Edit Role

Assign/Remove Permissions
```

subject to authorization.

---

# 139. Permission Grouping UI

For usability, group Permissions by module:

```text id="kgg2ll"
Users

Roles

Employees

Clients

Projects

Orders

Calculator

Reports
```

---

# 140. Permission Labels

UI may display friendly text:

```text id="gsy6tp"
View Orders

Create Orders

View Selling Price
```

while storing stable codes.

---

# 141. Permission Description

Each Permission should have a short explanation.

Example:

```text id="dzni8s"
orders.view_cost_price

"View aggregate Cost Price on Orders and authorized financial reports."
```

---

# 142. Do Not Expose Internal Implementation Details

Permission descriptions should explain business capability, not C# methods/controllers.

---

# 143. Role Edit UX

Role detail may show checkboxes grouped by module.

---

# 144. Save Role Permission Changes

Role-permission updates should be transactional.

---

# 145. Current User Permission Update

Changes to Role assignments/permissions should take effect predictably.

Avoid requiring server restart.

---

# 146. Session Permission Freshness

Do not embed an unchangeable full Permission list into a long-lived Session in a way that causes stale authorization for hours/days.

Backend should resolve or refresh Permissions sufficiently for administrative changes to take effect.

---

# 147. Frontend Refresh After Permission Change

The affected User may need to reload current User state or sign in again depending on implementation.

Backend remains immediately authoritative.

---

# 148. Permission-Protected API Principle

Each mutation endpoint should declare/check its required permission clearly.

Do not hide permission checks deep inside unrelated helper code only.

---

# 149. Defense in Depth

Critical operations may also enforce domain rules after permission succeeds.

Example:

```text id="bjsnhm"
User has users.activate
```

but still cannot deactivate final active Director.

Permission does not override business invariants.

---

# 150. Example — Project Team

A User may have:

```text id="9u27ne"
projects.manage_team
```

but cannot assign an inactive Employee.

---

# 151. Example — Orders

A User may have:

```text id="mxbmce"
orders.create
```

but cannot create Order in a Completed Project.

---

# 152. Example — Calculator

A User may have:

```text id="4ej7m1"
calculator.publish_templates
```

but cannot publish an invalid Template.

---

# 153. Example — Costs

A User may have:

```text id="gfy6dj"
calculator.edit_costs
```

but cannot add invalid negative Cost according to V1 rules.

---

# 154. Permission Test Strategy

Every permission-protected mutation should have tests for:

```text id="7reeda"
Authenticated + authorized

Authenticated + unauthorized

Unauthenticated
```

---

# 155. Expected Responses

Authorized:

```text id="4hz674"
operation-specific success
```

Unauthorized:

```text id="o38i3o"
403
```

Unauthenticated:

```text id="bt8vq8"
401
```

---

# 156. Financial DTO Tests

Tests must verify unauthorized fields are absent.

Do not test only that the frontend hides columns.

---

# 157. Selling Price DTO Test

Without:

```text id="ybw9j5"
orders.view_selling_price
```

the relevant DTO must not expose Selling Price.

---

# 158. Cost Price DTO Test

Without:

```text id="zz12gn"
orders.view_cost_price
```

aggregate Cost Price must not be exposed.

---

# 159. Profit DTO Test

If either Selling or Cost permission is absent:

```text id="hrh1oh"
profit
```

must be absent.

---

# 160. Cost Item DTO Test

Without:

```text id="jghgvr"
calculator.view_costs
```

Cost Item details must not be exposed.

---

# 161. Report Security Tests

Repeat financial DTO protection in Reports.

Reports are a common place for accidental leakage.

---

# 162. Dashboard Security Tests

If Dashboard later contains financial fields, apply the same tests.

---

# 163. Sorting Leakage Tests

Unauthorized Users must not sort by protected financial values.

---

# 164. Filtering Leakage Tests

Unauthorized Users must not use protected filters to infer financial information.

---

# 165. Export Security

If CSV/Excel export is added later, exports must use the same permission filtering.

---

# 166. Role Matrix Integration Test

During V1 release, create representative Users for each starter Role.

Test real UI and direct API behavior.

---

# 167. Director Test User

Verify access to all current features.

---

# 168. Manager Test User

Verify intended business workflow without system-admin privileges.

---

# 169. Operator Test User

Verify operational workflow and absence of restricted finance/admin access.

---

# 170. Cost User Test User

Verify Cost workflow and absence of unauthorized Selling/admin access.

---

# 171. Multiple Role Test User

Create User with two Roles.

Verify effective Permission union.

---

# 172. No Role User

A User with no Role may authenticate but should have almost no protected application capability.

This is valid as a controlled state.

---

# 173. Navigation With No Permissions

Such a User may see:

```text id="npm4dn"
Dashboard shell

Account/logout
```

but no protected module navigation.

---

# 174. Avoid Automatic Default Powerful Role

Creating a User should not automatically assign a powerful Role unless explicitly selected.

---

# 175. Bootstrap Exception

The initial `director` User receives Director Role as part of bootstrap.

---

# 176. Starter Role Creation

The system may optionally seed only:

```text id="lhkmo9"
Director
```

as mandatory.

Other starter Roles can be created/configured after setup.

---

# 177. Recommended V1 Approach

Seed:

```text id="6i68rb"
Director
```

and Permissions required by the application.

Then let Director create:

```text id="or6hdi"
Manager

Operator

Cost User
```

based on this document.

---

# 178. Why Not Seed All Roles

Lithograph may adjust responsibilities as the company starts using the ERP.

Keeping normal Roles configurable avoids unnecessary migration/code changes.

---

# 179. Role Deletion

If Role deletion is implemented later, it must not:

```text id="zpcbfk"
delete Users

delete Permissions
```

Only assignments/Role record should be affected according to safe rules.

---

# 180. V1 Role Deletion Recommendation

Physical Role deletion is not required.

A simpler approach is sufficient if roles remain manageable without deletion.

Do not add lifecycle complexity unless needed.

---

# 181. Permission Removal Impact

Removing a Permission from a Role affects all Users holding that Role.

UI should make this understandable.

---

# 182. Role Assignment Audit

`auth.user_roles` should retain:

```text id="cpqyem"
assigned_at

assigned_by
```

as already designed.

---

# 183. Role Permission Audit

`auth.role_permissions` should retain:

```text id="1n8mkf"
assigned_at

assigned_by
```

---

# 184. No Full Authorization History

Version 1 does not provide a complete historical timeline of every permission change.

Current audit metadata is sufficient.

---

# 185. Future Authorization Extensions

Possible later features:

```text id="avxfne"
More granular report permissions

Per-project access restrictions

Temporary Roles

Permission-change history

MFA for administrators
```

They are not V1 requirements.

---

# 186. No Row-Level Access in V1

Version 1 permissions are capability-based, not per-record ACLs.

Example:

```text id="074e37"
projects.view
```

allows viewing permitted Projects generally.

There is no initial rule such as:

```text id="2nl5ir"
Only Projects where Employee is Participant
```

---

# 187. Why No Row-Level Security Yet

Lithograph is initially a small internal team.

Row-level ACL complexity is not justified by current V1 requirements.

---

# 188. Future "My Work"

A future view may filter Projects/Orders based on linked Employee without changing core permission model.

---

# 189. PostgreSQL Row-Level Security

Do not implement PostgreSQL RLS for Version 1.

Application-layer authorization is the approved model.

---

# 190. No Permission Checks in Database Triggers

Do not put User authorization logic in PostgreSQL triggers/procedures.

---

# 191. Permission Enforcement Location

Authorization belongs primarily in:

```text id="pd91f0"
ASP.NET Core authorization policies/handlers

Application use-case boundaries
```

---

# 192. No Repeated Raw String Checks

Use centralized permission constants/catalog.

Avoid:

```text id="sc9l7v"
"orders.view"
```

being manually typed in dozens of unrelated places if a shared constant is available.

---

# 193. Frontend Permission Utility

Frontend should use centralized helpers such as:

```text id="pwwi0l"
hasPermission()

hasAnyPermission()

hasAllPermissions()
```

---

# 194. Permission Guard

A reusable component may handle conditional UI rendering.

Example concept:

```text id="yi8a32"
PermissionGuard
```

---

# 195. Route Guards

Frontend route guards improve UX.

Backend endpoints still enforce permissions independently.

---

# 196. Administration Navigation Matrix

Recommended:

```text id="xpyw7p"
Users
→ users.view

Roles
→ roles.view

Employees
→ employees.view

Order Types
→ orders.manage_types

Calculator Templates
→ calculator.manage_templates
```

---

# 197. Main Navigation Matrix

Recommended:

```text id="hp2a0k"
Dashboard
→ authenticated

Clients
→ clients.view

Projects
→ projects.view

Orders
→ orders.view

Reports
→ reports.view
```

---

# 198. Hidden vs Disabled Actions

For actions the User fundamentally cannot perform, hiding is usually preferable.

For context-sensitive business restrictions despite having permission, disable/show explanation where useful.

---

# 199. Example

User lacks:

```text id="vly7d4"
projects.manage_team
```

→ hide Team edit controls.

User has permission but selected Employee is inactive:

```text id="9fr9mz"
show/return validation explaining inactive Employee cannot be newly assigned.
```

---

# 200. Permission Error UX

A direct 403 should show a clear message such as:

```text id="27s19k"
You do not have permission to perform this action.
```

Do not show internal policy names unless useful for administrators.

---

# 201. Director UX

Director should see all administrative areas.

Do not build a second special Director-only UI if the normal permission-aware UI already supports full access.

---

# 202. Default Permission Principle

When uncertain whether a new sensitive feature should be visible:

```text id="ba9imk"
default to no access
```

until a Permission is explicitly granted.

---

# 203. New Module Rule

When adding a future module:

```text id="5bm73t"
Define its permissions first

Add to catalog

Synchronize Director

Add authorization tests

Then expose UI
```

---

# 204. New Financial Field Rule

If future feature exposes sensitive financial data, explicitly map which existing/new Permission protects it.

Do not assume `orders.view`.

---

# 205. Permission Naming Rule

Prefer verbs representing capabilities:

```text id="oae1ur"
view

create

edit

activate

manage_team

change_status

manage_templates
```

Avoid vague codes such as:

```text id="z4brxe"
orders.full

orders.admin2
```

---

# 206. Granularity Rule

Do not create a separate Permission for every button.

Permissions should represent meaningful business capabilities.

---

# 207. Example of Excessive Granularity

Do not create all of:

```text id="h4rvwn"
orders.checklist.add

orders.checklist.edit

orders.checklist.complete

orders.checklist.reorder

orders.checklist.delete
```

V1 uses:

```text id="9z75sd"
orders.manage_checklist
```

---

# 208. Example of Necessary Separation

Separate:

```text id="s1h4qs"
calculator.manage_templates
```

from:

```text id="jg5oxm"
calculator.publish_templates
```

because publication is a materially higher-impact action.

---

# 209. Another Necessary Separation

Separate:

```text id="yd7jvr"
orders.view_cost_price
```

from:

```text id="r7tqxf"
calculator.view_costs
```

because aggregate Cost and detailed supplier expenses are meaningfully different information.

---

# 210. Role Configuration Checklist

When creating a Role, review:

```text id="7txend"
What data must they see?

What can they create?

What can they modify?

What can they activate/deactivate?

Can they see Selling Price?

Can they see aggregate Costs?

Can they see detailed Costs?

Can they change security settings?

Can they publish Calculator Templates?
```

---

# 211. Least Privilege Principle

Give each normal Role only the Permissions required for its responsibilities.

Do not grant Director-like access for convenience.

---

# 212. Practicality Principle

Least privilege should not make normal work unusably complicated.

Role design should reflect actual workflows.

---

# 213. Avoid Per-User Custom Permission Workarounds

Because V1 has no direct User Permissions, if one User needs a different capability set:

```text id="haz83n"
create or adjust an appropriate Role
```

rather than modifying schema.

---

# 214. Role Naming

Use clear business names.

Examples:

```text id="zvz117"
Manager

Operator

Cost User

Designer
```

Avoid ambiguous:

```text id="lwlf7k"
Role 1

Level B

Power User 2
```

---

# 215. Role Description

Store/use descriptions if the Role schema supports it.

Example:

```text id="ba0h6x"
Operator:
Can manage operational Order progress but cannot access Costs or system administration.
```

---

# 216. Permission Synchronization Safety

Permission synchronization must:

```text id="58bfnf"
Add newly defined Permissions

Preserve existing valid Role assignments

Ensure Director receives all current Permissions
```

---

# 217. Do Not Auto-Remove Unknown Permissions Casually

If application code no longer contains a permission code, do not automatically delete its database row during ordinary startup without deliberate migration/review.

---

# 218. Removed Feature Permissions

If a Permission becomes obsolete, handle through an explicit migration/cleanup decision.

---

# 219. Role and Permission Backup

Roles and Permission assignments are normal database data and are included in PostgreSQL backup.

---

# 220. Restore Verification

After database restore, verify:

```text id="f5ttg2"
Roles exist

Role Permission assignments exist

User Role assignments exist

Director still has full effective access
```

---

# 221. Release Checklist

Before V1 release:

```text id="u1phzi"
[ ] Permission catalog matches this document

[ ] Director has all Permissions

[ ] Final Director invariant works

[ ] Starter Roles tested

[ ] User with multiple Roles gets union

[ ] User with no Role has no unintended access

[ ] Financial fields protected

[ ] Cost Item details protected

[ ] Reports protected

[ ] Dashboard sections protected

[ ] Direct API 403 tests pass

[ ] Frontend hides unauthorized actions

[ ] Permission changes take effect predictably
```

---

# 222. Financial Security Checklist

```text id="zhh7of"
[ ] Selling Price protected by orders.view_selling_price

[ ] Cost Price protected by orders.view_cost_price

[ ] Profit requires both

[ ] Detailed Costs protected by calculator.view_costs

[ ] Cost mutations protected by calculator.edit_costs

[ ] Reports do not bypass source permissions

[ ] Exports later must not bypass permissions
```

---

# 223. Administration Security Checklist

```text id="k0sp6r"
[ ] User creation protected

[ ] User activation protected

[ ] Password reset protected

[ ] User Role assignment protected

[ ] Role Permission assignment protected

[ ] Director Role protected

[ ] Final active Director protected
```

---

# 224. Calculator Security Checklist

```text id="q1h23t"
[ ] Calculator values require calculator.view

[ ] Calculator edits require calculator.edit

[ ] Template editing requires calculator.manage_templates

[ ] Publishing requires calculator.publish_templates

[ ] Cost values require calculator.view_costs

[ ] Cost editing requires calculator.edit_costs
```

---

# 225. Recommended AI Audit Task

```text id="mcxu1m"
Read:
- AI_RULES.md
- docs/07_Authentication.md
- docs/10_Users_Module.md
- all module implementation plans
- docs/38_V1_Role_and_Permission_Matrix.md

Task:
Audit the repository's complete V1 permission implementation.

Verify:
- every documented Permission exists
- no undocumented permission codes are used without justification
- backend endpoints use correct Permissions
- financial fields are protected
- Reports reuse source financial permissions
- Dashboard does not leak restricted data
- Director receives all current Permissions
- final active Director protection works
- frontend navigation/actions use centralized permission helpers

Do not:
- change Role business configuration automatically
- add direct User permissions
- add deny permissions
- add row-level ACLs
- hardcode normal Role names into business logic

Produce:
1. missing permissions
2. unused permissions
3. incorrect endpoint mappings
4. financial leakage risks
5. frontend/backend mismatches
6. required tests
```

---

# 226. Final Permission Principle

Lithograph ERP authorization should answer:

```text id="g4k5n0"
What may this User see?

What may this User create?

What may this User change?

What financial information may this User access?

What administrative powers does this User have?
```

without coupling access to:

```text id="zla90o"
Employee position

Project role

Username

Hardcoded business Role names
```

The central rule is:

```text id="vy4h13"
Roles group Permissions.

Permissions protect capabilities.

Backend enforces the decision.
```

---

**End of Document**