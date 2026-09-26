# Lithograph ERP

**Document:** 39_V1_Data_Seed_and_Initial_Configuration.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Area:** Initial Data / Production Configuration

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `05_Numbering_System.md`
- `07_Authentication.md`
- `09_Auth_Tables.md`
- `10_Users_Module.md`
- `11_Employees_Module.md`
- `12_Clients_Module.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `21_Deployment_and_Backup.md`
- `27_Authentication_Implementation_Plan.md`
- `28_Employees_Implementation_Plan.md`
- `29_Clients_Implementation_Plan.md`
- `30_Projects_Implementation_Plan.md`
- `31_Orders_Implementation_Plan.md`
- `32_Calculator_Foundation_Implementation_Plan.md`
- `37_V1_Integration_and_Release_Plan.md`
- `38_V1_Role_and_Permission_Matrix.md`

---

# 1. Purpose

This document defines what data should exist in a fresh Lithograph ERP installation and how the first production database should be configured.

It distinguishes between:

```text id="yxtj6a"
System Seed Data
```

```text id="co7w8u"
First-Run Bootstrap Data
```

```text id="cwla90"
Administrator Configuration
```

and:

```text id="nw0jlr"
Real Business Data
```

These categories must not be mixed.

---

# 2. Main Goal

A new Lithograph ERP installation must be able to move safely from:

```text id="5s4z6i"
Empty PostgreSQL database
```

to:

```text id="8nwq94"
Ready for real Lithograph Orders
```

without:

```text id="u2xg2f"
manual SQL editing

hardcoded business customers

fake Employees

fake Orders

default passwords

production test records
```

---

# 3. Data Categories

Use four categories.

## Category A — System Seed Data

Data required for the application to function.

Examples:

```text id="e9j2u7"
Permission catalog

Protected Director Role
```

## Category B — First-Run Bootstrap Data

Created interactively during first launch.

Example:

```text id="561pyg"
director User
```

## Category C — Administrator Configuration

Created by Director after first login.

Examples:

```text id="g6cnf5"
Normal Roles

Employees

Users

Order Types

Calculator Templates
```

## Category D — Business Data

Created through normal ERP use.

Examples:

```text id="vhmjtz"
Clients

Projects

Orders

Cost Items
```

---

# 4. Seed Data Principle

Seed only data that is:

```text id="r69f7n"
Required by the software itself
```

Do not seed business assumptions merely because they seem convenient.

---

# 5. Mandatory System Seed Data

The mandatory persistent seed/configuration data is:

```text id="xl13vr"
Permission catalog

Director Role
```

No other business records are mandatory before first launch.

---

# 6. Permission Catalog

All Version 1 permission codes from:

```text id="cr39my"
38_V1_Role_and_Permission_Matrix.md
```

must exist in:

```text id="6hyctd"
auth.permissions
```

---

# 7. Canonical Permission Catalog

Version 1:

```text id="ppzxx5"
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

# 8. Permission Catalog Ownership

Permission definitions originate from application code.

Do not manually maintain them through ad hoc SQL.

---

# 9. Permission Synchronization

On application startup or controlled initialization:

```text id="9cwyml"
Add missing application-defined Permissions
```

without destroying valid existing Role assignments.

---

# 10. Permission Synchronization Must Be Idempotent

Running synchronization repeatedly must not create duplicates.

---

# 11. Permission Code Unique Constraint

Database must enforce unique:

```text id="e5xuiv"
auth.permissions.code
```

---

# 12. Permission Metadata

Each Permission may include:

```text id="9d2z3k"
code

display_name

description

module/group
```

according to Authentication schema.

The stable identity remains:

```text id="frxjhj"
code
```

---

# 13. Director Role Seed

A protected Role named:

```text id="pv739t"
Director
```

must exist before first Director User is created.

---

# 14. Director Role Purpose

Director is the highest system Role.

It provides full ERP access.

---

# 15. Director Role Is System Data

Unlike Manager/Operator/Cost User:

```text id="b6va30"
Director
```

is not merely a suggested business Role.

It is required by first-run bootstrap and final-Director protection.

---

# 16. Director Role Protection

Mark Director through an implementation mechanism suitable for protected/system Roles.

Conceptually:

```text id="4sl5bi"
is_system = true
```

if such field exists in approved schema.

If not, protect it through established domain logic.

Do not add schema fields only because this document uses conceptual language.

---

# 17. Director Role Permissions

Director must effectively receive:

```text id="z6jjzb"
all current application Permissions
```

---

# 18. Future Permissions

When a future Permission is registered:

```text id="0h8f11"
Director receives it automatically.
```

Normal Roles do not.

---

# 19. No Default Director Password

Never seed:

```text id="at5ed0"
admin/admin

director/director

123456

password
```

or any other known credential.

---

# 20. First-Run User Count

Immediately after a clean migration and before first launch:

```text id="pwcc7a"
auth.users = 0
```

is expected.

---

# 21. First-Run Detection

Application determines setup state based on approved Authentication setup rules.

Conceptually:

```text id="pxfgk0"
No Users
→ setup required
```

---

# 22. First-Run Setup Screen

The initial setup asks the administrator to define the Director password.

Username is fixed initially as:

```text id="ff5t9s"
director
```

---

# 23. First Director Creation

The setup creates:

```text id="b9b3t9"
User:
username = director
active = true
```

and assigns:

```text id="wb49ev"
Director Role
```

---

# 24. Bootstrap Audit

Because no authenticated User exists yet:

```text id="vup4vq"
created_by = NULL
```

is valid for bootstrap-created Director.

---

# 25. Bootstrap Concurrency

Two simultaneous first-run setup requests must not create two initial Director accounts.

Protect transactionally.

---

# 26. Bootstrap One-Time Rule

After first User exists:

```text id="uhk5lt"
setup endpoint must no longer permit bootstrap
```

---

# 27. Director Username After Setup

Director may later change the username through normal authorized User administration.

The security model must not depend on username remaining:

```text id="yy73gc"
director
```

---

# 28. Data Present After Bootstrap

After first-run setup, the database should contain approximately:

```text id="zlg22h"
Permissions

Director Role

Role → Permission assignments

One Director User

User → Director Role assignment

Initial Session after login, if applicable
```

No business data is required yet.

---

# 29. Data That Must Not Be Seeded Automatically

Do not automatically seed:

```text id="g6j967"
Employees

Clients

Projects

Orders

Checklist Items

Folder Links

Cost Items

Calculator field values

Production folders

Business financial values
```

---

# 30. Why Employees Must Not Be Seeded

Employee records represent real Lithograph people.

They should be created deliberately.

Do not assume every User is an Employee.

---

# 31. Why Clients Must Not Be Seeded

Client records are real business master data.

No sample customers belong in production.

---

# 32. Why Projects/Orders Must Not Be Seeded

Projects and Orders are operational records.

Fake records distort:

```text id="d6dw1q"
Reports

Business IDs

Dashboard

Financial totals
```

---

# 33. No Demo Data in Production

Demo data may exist in:

```text id="vyx40j"
development

test
```

environments.

It must never be silently inserted into production.

---

# 34. Environment-Specific Seed Rules

Use separate logic for:

```text id="bgm653"
System initialization
```

and:

```text id="8lo2xu"
Development demo/test data
```

---

# 35. Development Data

Development may include generated:

```text id="5lf0u7"
Clients

Projects

Orders

Calculator Templates
```

to accelerate testing.

These scripts must be clearly non-production.

---

# 36. Test Data

Automated tests create their own isolated data.

Do not rely on production seed records.

---

# 37. Normal Roles

After Director login, Director may create normal Roles.

Recommended starting options:

```text id="mz74lm"
Manager

Operator

Cost User
```

but they are not mandatory.

---

# 38. Normal Roles Should Not Be Mandatory Seed

Recommended production approach:

```text id="yrtx9f"
Seed only Director Role.

Create normal Roles through Administration.
```

---

# 39. Why Normal Roles Stay Configurable

Lithograph responsibilities may evolve as the company begins operating.

Hardcoding normal roles would create unnecessary migration/code changes.

---

# 40. Recommended Role Setup Sequence

After first Director login:

```text id="mb96lv"
1. Review Permission catalog

2. Create Manager Role if needed

3. Create Operator Role if needed

4. Create Cost User Role if needed

5. Assign Permissions using document 38
```

---

# 41. Do Not Create Users Before Roles Are Understood

Avoid rapidly creating many Users with excessive permissions.

Configure a small Role model first.

---

# 42. Initial Employee Setup

Recommended next step:

```text id="sqz4mr"
Create real Lithograph Employees
```

---

# 43. Employee Creation Fields

For each real Employee, enter:

```text id="8dnr3g"
Full Name

Position

Phone if needed

Email if needed
```

---

# 44. User Link Is Optional

Do not create a User account merely because an Employee exists.

Only create Users who need ERP login.

---

# 45. Recommended Initial Users

Start with:

```text id="pdkeht"
Director

A small number of real operational Users
```

rather than all possible staff immediately.

---

# 46. User Creation Sequence

For each ERP User:

```text id="9gtcjp"
Create User

Assign appropriate Role(s)

Give User initial password securely

Link to Employee if appropriate
```

---

# 47. Password Distribution

Passwords should be communicated privately.

Do not:

```text id="3bt4et"
store them in documentation

send them in broad group chats

commit them to Git
```

---

# 48. Password Change

If desired, a future forced-password-change mechanism may be added.

It is not required for V1 unless already implemented.

---

# 49. Initial Client Data Strategy

Do not import every historical customer immediately.

Recommended:

```text id="yfx687"
Add active/current Clients as work requires them.
```

---

# 50. Benefits

This keeps:

```text id="w2746h"
Client database clean

Business IDs meaningful

Pilot manageable
```

---

# 51. Historical Client Import

Historical Clients may be imported later if needed for reporting/history.

Treat as a separate migration project.

---

# 52. Client Business ID Start

First production Client should receive:

```text id="40i21i"
CL-000001
```

if no prior imported Clients exist.

---

# 53. Do Not Manually Choose First Number

Backend numbering mechanism controls Business IDs.

---

# 54. Initial Project Strategy

Create Projects only for:

```text id="vzleac"
real active/new work
```

during initial production rollout.

---

# 55. Initial Order Strategy

Likewise, use ERP for current/new Orders.

Avoid entering large historical Order archives before V1 workflow is proven.

---

# 56. Order Type Configuration

Order Types are configuration/master data.

Director or authorized administrator should create the initial list.

---

# 57. Recommended Initial Order Types

A reasonable starting set may include actual Lithograph production categories such as:

```text id="5cm3pg"
UV Printing

Digital Printing

CO₂ Laser Cutting

Graphic Design

Finishing

Installation

Outsourced Work
```

Only create types that correspond to real workflows.

---

# 58. Order Type Names Are Business Configuration

The above list is a recommendation, not mandatory system seed.

---

# 59. Avoid Too Many Types

Do not start with dozens of narrowly defined types.

Example of excessive early fragmentation:

```text id="ncuyod"
UV Acrylic 3 mm

UV Acrylic 4 mm

UV PVC 3 mm

UV PVC 5 mm
```

These may be better represented through Calculator fields rather than separate Order Types.

---

# 60. Order Type Design Rule

Create a separate Order Type when:

```text id="30g32t"
the work meaningfully differs operationally
or
requires a different Calculator/workflow
```

---

# 61. Order Type Deactivation

If an initial Type later proves unnecessary:

```text id="x1bt6s"
Deactivate it
```

rather than deleting historical usage.

---

# 62. Calculator Template Configuration

Do not seed business Calculator Templates automatically.

They encode Lithograph pricing rules and require deliberate validation.

---

# 63. Initial Calculator Strategy

Start with:

```text id="d05d8h"
one or two high-value real Calculator Templates
```

rather than attempting to model every Order Type.

---

# 64. Suggested First Calculator Candidates

Potential candidates:

```text id="y5dfpc"
UV Printing

Digital Printing
```

or whichever real processes have the clearest pricing formulas.

---

# 65. Calculator Setup Sequence

For each Calculator:

```text id="ckh3ec"
Create Template

Edit Draft v1

Add fields

Add formulas

Set sellingPriceFieldKey

Validate

Test with real examples

Publish v1

Assign to Order Type
```

---

# 66. Do Not Publish Untested Pricing

Before publication, compare Calculator results to known/manual calculations.

---

# 67. Calculator Test Cases

For each initial Template, prepare several real scenarios:

```text id="dghzmt"
Small job

Typical job

Large job

Boundary values

Discount/conditional scenario if applicable
```

---

# 68. Pricing Approval

Template publication should be performed by a User with:

```text id="huv5o3"
calculator.publish_templates
```

after review.

---

# 69. Do Not Seed Published Templates Through Migration

Business pricing logic should not normally be embedded inside EF migrations.

Use Administration/import tooling if initial Templates need preloading.

---

# 70. Why

Pricing formulas may change independently from application schema.

They are business configuration, not database structure.

---

# 71. Initial Cost Categories

Because Cost Category is free text:

```text id="0k2ml6"
no Cost Category seed table exists.
```

---

# 72. Recommended Wording Convention

Lithograph may agree internally on common terms such as:

```text id="13u3n7"
Material

Outsource

Transport

Installation

Finishing

Other
```

to improve Report grouping.

---

# 73. Do Not Enforce Category Vocabulary Yet

V1 still stores category text.

Do not build a hidden enum merely to standardize it.

---

# 74. Initial Supplier Data

There is no Supplier master.

Supplier is entered as text on Cost Items.

---

# 75. Supplier Naming Convention

Users should use consistent names where practical.

Example:

```text id="zqxe7u"
ABC Materials LLC
```

instead of alternating between:

```text id="xa1671"
ABC

ABC LLC

ABC Materials
```

---

# 76. Folder Links

Do not seed folder paths.

Folder Links belong to individual Orders.

---

# 77. Preview Images

Do not seed placeholder image records into production Orders.

An Order may simply have no Preview Image.

---

# 78. Checklist Items

Checklists are manually created per Order in V1.

Do not seed Checklist templates.

---

# 79. Numbering Initialization

Business ID generation requires its numbering infrastructure to be initialized safely.

---

# 80. Client Counter

Initial logical next Client number:

```text id="vpbq14"
1
```

---

# 81. Project Counter

Initial logical yearly Project number for current year:

```text id="p0t2m5"
1
```

when first Project is created.

---

# 82. Order Counter

Initial logical yearly Order number:

```text id="eym1dt"
1
```

when first Order is created.

---

# 83. Lazy Numbering Initialization

Preferred design:

```text id="5z8mqs"
Create/init numbering state when first ID for that series/year is requested
```

rather than manually pre-seeding every future year.

---

# 84. Future Year

Do not create counters for:

```text id="qyjv28"
2027
2028
2029
```

in advance unless numbering design naturally does so.

---

# 85. Sequence Gaps

Do not attempt to make initial numbering perfectly contiguous after failed pilot operations.

Gaps are valid.

---

# 86. Production Test Records and Numbering

If production smoke testing creates real-numbered Clients/Projects/Orders and then removes/cancels them:

```text id="mlqkb1"
their Business IDs should not be reused.
```

---

# 87. Better Production Smoke Test

Prefer harmless configuration/read tests when possible to avoid unnecessary production Business ID consumption.

But numbering gaps remain acceptable.

---

# 88. Initial Database State Checklist

Immediately after clean migration but before setup:

```text id="i09nc5"
[ ] Schemas exist

[ ] Tables exist

[ ] Permission catalog synchronized

[ ] Director Role exists

[ ] Director Role has all Permissions

[ ] No Users exist

[ ] No Employees exist

[ ] No Clients exist

[ ] No Projects exist

[ ] No Orders exist

[ ] No Calculator Templates exist

[ ] No Cost Items exist
```

---

# 89. After First-Run Setup Checklist

```text id="jgl65a"
[ ] director User exists

[ ] director is active

[ ] Director Role assigned

[ ] Password is hashed

[ ] Setup endpoint disabled by existence of Users

[ ] Director can log in

[ ] Director sees all Administration modules
```

---

# 90. Initial Configuration Checklist

Before entering real business Orders:

```text id="fgkmwe"
[ ] Normal Roles configured

[ ] Required Employees created

[ ] Required Users created

[ ] Users linked to Employees where appropriate

[ ] Roles assigned

[ ] Initial Order Types created

[ ] Initial Calculator Templates tested/published where needed
```

---

# 91. Business Data Readiness Checklist

Before first real Order:

```text id="dajr7l"
[ ] Client exists

[ ] Project exists

[ ] Project Team assigned

[ ] Appropriate Order Type active

[ ] Calculator Template published if required
```

---

# 92. Recommended First Real Data Sequence

Use:

```text id="riypb8"
1. Create real Client

2. Create real Project

3. Assign real Project Team

4. Create real Order

5. Add Checklist Items

6. Add Folder Links as required

7. Use Calculator

8. Add Costs as they occur

9. Complete Order

10. Verify Reports
```

---

# 93. Avoid Historical Bulk Import During First Days

Initial production use should focus on proving:

```text id="6gnodj"
daily workflow
```

not historical completeness.

---

# 94. Pilot Configuration

Recommended pilot:

```text id="ssll7a"
Few Users

Few Roles

Few Order Types

One or two Calculator Templates

Several real Orders
```

---

# 95. Pilot Database Should Become Production Data

If the pilot uses the actual production database and real business records, do not wipe it merely because pilot is complete.

Treat those records as production.

---

# 96. Test Environment Must Remain Separate

Experimental formulas and fake records belong in:

```text id="kba1ud"
development/test
```

not the production pilot database.

---

# 97. Calculator Template Development Environment

For complex pricing logic, it may be safer to develop/test Draft definitions in development first, then recreate/import them deliberately in production.

---

# 98. Template Import

If a future Template import tool is built, it must:

```text id="no375p"
validate Definition

preserve schemaVersion

not silently publish
```

unless explicitly requested by authorized User.

---

# 99. No SQL Template Import

Do not make direct SQL inserts the normal way to configure production Calculators.

---

# 100. Initial Role Assignment Review

Before production launch, review every User:

```text id="7sqzq4"
Username

Linked Employee

Role(s)

Effective Permissions
```

---

# 101. Avoid Shared User Accounts

Do not create:

```text id="0fksx3"
operator

designer

production
```

as shared credentials for multiple people if individual accountability is needed.

Prefer one User per person who needs access.

---

# 102. Generic Accounts

Only use generic accounts if there is a deliberate operational reason and the audit limitations are understood.

Not recommended for normal use.

---

# 103. Usernames

Use simple stable usernames.

Examples:

```text id="v5t5k0"
aram

samvel

suren
```

or another consistent company convention.

---

# 104. Username Is Not Employee Name

A User may change username without changing Employee `full_name`.

---

# 105. Initial Data Quality

Before adding a Client/Employee/Order Type, avoid unnecessary duplicates.

---

# 106. Client Duplicate Check

Search before creating another Client with a similar name.

Client name is not unique, so human review matters.

---

# 107. Employee Duplicate Check

Employee names are not unique by database rule.

Avoid duplicate records unless they truly represent different people.

---

# 108. Order Type Duplicate Check

Order Type name should already be protected case-insensitively.

---

# 109. Calculator Template Duplicate Check

Likewise avoid multiple Templates representing the same pricing process unnecessarily.

---

# 110. Initial Reports

Reports require no special seed/configuration besides:

```text id="spxo1x"
permissions
+
real source data
```

---

# 111. Initial Dashboard

Dashboard requires no seed data.

With no business records it should show useful empty states.

---

# 112. Empty-System Experience

Immediately after first login, Dashboard may show:

```text id="c08di6"
0 Active Orders

0 Urgent Orders

0 Due Soon

0 Overdue
```

and empty Recent sections.

This is correct.

---

# 113. Do Not Seed Fake Dashboard Activity

Never add fake Orders just to make Dashboard visually populated.

---

# 114. Production Configuration Storage

Environment configuration such as:

```text id="c8pj84"
Database connection string

HTTPS settings

File storage path

Logging configuration
```

is not database seed data.

Keep it in approved secure application configuration.

---

# 115. Secrets

Never seed secrets into database migrations unless they are generated securely for a defined system purpose.

---

# 116. Connection Credentials

Do not put database credentials into:

```text id="o9uyh7"
seed classes

migration files

repository documentation
```

---

# 117. Configuration vs Business Data

Examples:

```text id="ohysxa"
Permission
→ system configuration

Order Type
→ business configuration

Client
→ business data

Order
→ operational transaction
```

Maintain this conceptual separation.

---

# 118. Migration Rule

EF migrations should primarily define:

```text id="b02l8l"
schema structure

constraints

required system-level seed/synchronization hooks
```

not real company data.

---

# 119. Business Configuration Changes

Changing:

```text id="9ftdu4"
Order Type

Calculator Template

Normal Role
```

should normally happen through ERP Administration rather than a schema migration.

---

# 120. System Permission Changes

New application Permissions normally arrive with application releases and synchronization/migration logic.

---

# 121. Director Role Future Safety

If Permission catalog has:

```text id="z6xspt"
41 permissions
```

after a future release, Director must receive all 41.

Normal Roles remain unchanged until reviewed.

---

# 122. Permission Sync Failure

If Permission synchronization fails during deployment:

```text id="3uuxr0"
deployment should not silently continue
```

in a state where new endpoints exist but required Permission definitions are missing.

---

# 123. Idempotent Initialization Test

Run application initialization repeatedly.

Verify:

```text id="l4zu6j"
No duplicate Permissions

No duplicate Director Role

No duplicate Role-Permission rows
```

---

# 124. Clean Database Initialization Test

Automated integration test should verify:

```text id="t1d3x2"
Migrate empty DB

Initialize system data

Permissions correct

Director Role correct

Users = 0
```

---

# 125. First-Run Bootstrap Test

Automated test:

```text id="jj193b"
Initialize clean DB

POST setup

Director created

Director Role assigned

Setup cannot run second time
```

---

# 126. Director Permission Completeness Test

Compare application permission catalog with Director effective Permissions.

They must match.

---

# 127. Normal Role Preservation Test

Add custom Role.

Run Permission synchronization.

Verify custom Role and assignments remain unchanged.

---

# 128. New Permission Test

Simulate addition of application Permission.

After synchronization:

```text id="32dmqd"
Permission exists

Director has it

Normal Role does not automatically receive it
```

---

# 129. No Business Seed Test

Production initialization test should assert zero:

```text id="moyb3v"
Employees

Clients

Projects

Orders

Templates

Cost Items
```

before humans configure them.

---

# 130. Development Seed Separation Test

Production environment must not execute development/demo seed routine.

---

# 131. Startup Behavior

Normal application startup may safely synchronize small system configuration such as Permissions.

Do not perform large business data imports on startup.

---

# 132. No Startup Destructive Cleanup

Never use startup logic that:

```text id="a6pqta"
deletes unknown Roles

deletes inactive Order Types

resets counters

clears data
```

---

# 133. Initial Backup

After first production configuration and before entering significant live Orders, create a baseline database backup.

---

# 134. Why Baseline Backup

It provides a clean recovery point containing:

```text id="ga1tnu"
Users

Roles

Employees

Order Types

Initial Calculator Templates
```

before extensive transactional data accumulates.

---

# 135. Backup After Calculator Setup

Because Calculator Templates contain valuable pricing logic, ensure backups are active before relying on them.

---

# 136. Configuration Documentation

Maintain a short operational record of:

```text id="n9npfy"
Current normal Roles

Current initial Order Types

Active Calculator Templates
```

without duplicating every database value manually.

---

# 137. Do Not Store Passwords in Configuration Documentation

Never include User passwords.

---

# 138. Initial Role Recommendation

For Lithograph V1, a practical first setup may be:

```text id="t4a08n"
Director
Manager
Operator
Cost User
```

but create only the Roles actually required at go-live.

---

# 139. Initial Employee Recommendation

Create real founders/staff who need to appear in Project Team assignments.

Not every Employee needs ERP access.

---

# 140. Initial User Recommendation

Create only those who need to:

```text id="712svh"
view/update Orders

manage Projects

enter Costs

administer ERP
```

---

# 141. Initial Client Recommendation

Start with:

```text id="qkwn43"
active Clients with current work
```

then add others as needed.

---

# 142. Initial Project Recommendation

Enter:

```text id="n1n06u"
current Projects
```

that will contain new Orders.

---

# 143. Initial Order Recommendation

Use ERP for new/current operational Orders from a clear go-live date.

---

# 144. Go-Live Cutover Date

Lithograph should define a clear operational date:

```text id="81ft6a"
From this date onward,
new Orders are entered in Lithograph ERP.
```

This is an operational policy, not a database field.

---

# 145. Avoid Split Tracking

After go-live, avoid tracking the same new Order partly in ERP and partly in old spreadsheets unless needed temporarily for validation.

---

# 146. Temporary Parallel Validation

A short period of:

```text id="1lexs6"
ERP calculation
vs
existing manual calculation
```

is useful for Calculator verification.

---

# 147. Parallel Validation End

Once Calculator reliability is proven, avoid permanent duplicate manual workflows.

---

# 148. Historical Data Migration Decision

After V1 stabilizes, decide whether historical records are worth importing based on:

```text id="4wkm2p"
reporting value

effort

data quality

business need
```

---

# 149. Historical Business IDs

If historical data is later imported, define carefully whether imported entities receive normal ERP Business IDs or preserve external references in separate fields.

Do not improvise inside production imports.

---

# 150. Do Not Modify Existing Business ID Rules

Historical import must not break:

```text id="5dm0vb"
CL-000001

PRJ-YYYY-000001

ORD-YYYY-000001
```

numbering guarantees.

---

# 151. Production Initialization Procedure

Recommended sequence:

```text id="yo32kc"
1. Provision PostgreSQL

2. Configure production secrets

3. Apply EF migrations

4. Start application

5. Synchronize Permission catalog / Director Role

6. Confirm Users = 0

7. Open first-run setup

8. Create Director password

9. Log in as Director

10. Create normal Roles

11. Create Employees

12. Create required Users

13. Assign Roles

14. Link Users to Employees

15. Create Order Types

16. Create/test/publish Calculator Templates

17. Assign Templates to Order Types

18. Create baseline backup

19. Enter first real Client

20. Create first real Project

21. Create first real Order
```

---

# 152. First Real Order Acceptance

For the first production Order, manually verify:

```text id="hcbd7p"
Business ID

Client derivation

Project Team

Order Type

Calculator Version

Selling Price

Costs

Checklist

Folder Links

Reports

Dashboard
```

---

# 153. First Financial Verification

Compare the first few real Orders against manual known-good calculations.

Verify:

```text id="7f0b5c"
Selling Price

Cost Price

Profit
```

---

# 154. Stop Condition

If the first real Calculator result is wrong:

```text id="w30lv7"
Do not continue creating many live Orders with that Template.
```

Fix/test the Template first.

---

# 155. Template Correction

If v1 has already been published and used:

```text id="l5n21o"
do not edit v1.
```

Create:

```text id="lw454k"
v2 Draft
```

fix, validate, publish.

Historical Order remains on v1 unless explicitly reset/migrated.

---

# 156. Configuration Audit Before Go-Live

Review:

```text id="7ag5uv"
Director access

Normal Role permissions

User-role assignments

Employee links

Order Types

Calculator assignments

Published Template versions

Backup status
```

---

# 157. Initial Configuration Completion Gate

Production configuration is ready when:

```text id="rriqrm"
System Permissions exist

Director Role correct

Director User created securely

Normal Roles configured as needed

Employees exist

Required Users exist

Role assignments verified

Order Types configured

Required Calculator Templates published

Templates assigned correctly

Baseline backup completed
```

---

# 158. No Need for Clients Before Configuration Complete

Do not rush into production business records until:

```text id="7ysopn"
security

roles

Order Types

pricing configuration
```

are understood.

---

# 159. Production Database Ownership

Once real business records are entered, treat database as production permanently.

Do not:

```text id="o8xt84"
reset migrations

drop/recreate database

reseed numbers

wipe tables
```

for development convenience.

---

# 160. Developer Workflow After Go-Live

Develop/test schema changes against separate environments.

Production changes go through migrations.

---

# 161. No Migration Rewrites

Do not edit already-applied shared production migrations to make history look cleaner.

Create new migrations.

---

# 162. No Production Direct Data Fixes by Default

Use application/API or reviewed maintenance scripts.

Direct SQL fixes should be rare, documented, backed up, and reviewed.

---

# 163. Business ID Counter Repair

Never manually reduce counters to fill gaps.

---

# 164. Permission Data Repair

If Permission catalog becomes inconsistent:

```text id="422zxe"
use controlled synchronization/migration logic
```

rather than manually inventing codes.

---

# 165. Role Configuration Export

A future configuration export may be useful but is not V1 requirement.

---

# 166. Template Export

Likewise future Calculator Template export/import may simplify moving tested Templates between environments.

Not required for initial release.

---

# 167. Future Seed Candidates

Possible future true system configuration may include:

```text id="p7d1mv"
Application settings

Feature flags

Currency configuration
```

only if formally designed.

Do not pre-create generic settings infrastructure.

---

# 168. Single Currency

V1 assumes one company currency.

No currency master seed table is required.

---

# 169. Currency Display Configuration

Currency display may be an application configuration value if needed.

Do not turn it into multi-currency accounting.

---

# 170. Armenia-Specific Accounting Data

Do not seed:

```text id="2m4p0y"
VAT rates

tax codes

chart of accounts
```

because accounting functionality is outside current ERP V1.

---

# 171. No Demo Admin User

Do not seed an additional:

```text id="23rpqg"
admin
```

account.

The first Director is sufficient.

---

# 172. No Hidden Support Account

Do not create developer/vendor backdoor Users.

---

# 173. Recovery Access

Administrative recovery should use documented system/DB recovery procedures, not hidden credentials.

---

# 174. First-Run UX

If setup is required, normal Login should guide to Setup rather than display confusing authentication failure.

---

# 175. Setup Status

Frontend may use:

```text id="hz7yss"
GET /api/auth/setup-status
```

or approved equivalent.

---

# 176. Setup Status Security

It may reveal only whether setup is required.

Do not expose sensitive database/system details.

---

# 177. Empty Administration UX

Immediately after first login:

```text id="53m7ez"
Employees list empty

Clients empty

Projects empty

Orders empty
```

should be presented intentionally.

---

# 178. Helpful Empty Actions

Examples:

```text id="zmh4fo"
No Employees yet.
[Create Employee]

No Clients yet.
[Create Client]
```

subject to permissions.

---

# 179. No Setup Wizard Required

A large multi-step business setup wizard is not required in V1.

The documented configuration order plus clear Administration screens is sufficient.

---

# 180. Future Setup Checklist UI

If onboarding proves confusing, a future Dashboard setup checklist may be added.

Do not build it preemptively.

---

# 181. Initial Configuration Test Environment

Before production, rehearse the entire initialization sequence in a clean staging/test environment.

---

# 182. Rehearsal Acceptance

A developer/admin should be able to follow this document without:

```text id="oy17wd"
editing SQL

changing source code

inventing undocumented steps
```

---

# 183. Time-to-First-Order Principle

The setup should remain simple enough that once the server is deployed, Lithograph can reach the first real Order through ordinary Administration.

---

# 184. Seed Audit AI Task

Recommended:

```text id="ht3jxi"
Read:
- AI_RULES.md
- docs/27_Authentication_Implementation_Plan.md
- docs/38_V1_Role_and_Permission_Matrix.md
- docs/39_V1_Data_Seed_and_Initial_Configuration.md

Task:
Audit all application initialization and seed behavior.

Verify:
- only approved system data is seeded
- Permission catalog is idempotently synchronized
- Director Role exists once
- Director gets every current Permission
- no default Director password exists
- no normal business Users are seeded
- no Employees/Clients/Projects/Orders are seeded in production
- development/demo seed logic cannot run accidentally in production
- first-run setup creates the only initial User
- setup is concurrency-safe
- repeated startup does not duplicate system data

Do not:
- seed Manager/Operator/Cost User automatically
- seed business data
- seed demo records
- create hidden admin/support accounts
- change Business ID counters manually
```

---

# 185. Initial Production Checklist

```text id="c87d9h"
[ ] Production database provisioned

[ ] Production secrets configured

[ ] Migrations applied

[ ] Permission catalog synchronized

[ ] Director Role created

[ ] Director has all Permissions

[ ] No default password exists

[ ] First Director setup completed

[ ] Normal Roles configured as needed

[ ] Employees created

[ ] Required Users created

[ ] User Roles verified

[ ] User/Employee links verified

[ ] Order Types created

[ ] Initial Calculator Templates validated

[ ] Calculator Templates published

[ ] Order Type → Template links verified

[ ] Baseline backup completed

[ ] First real Client entered

[ ] First real Project entered

[ ] First real Order verified end to end
```

---

# 186. Final Data Initialization Principle

A fresh Lithograph ERP should begin with:

```text id="g9i56p"
System knowledge
```

not:

```text id="15tdau"
invented business data.
```

The application should know:

```text id="b13d22"
which Permissions exist

what the Director Role is

how to bootstrap the first Director
```

Lithograph should decide:

```text id="8zgrxq"
who the Employees are

which Users need access

which Roles they receive

which Order Types exist

which pricing Templates are used

which Clients/Projects/Orders are real
```

The central rule is:

```text id="vkh8p6"
Seed the software.

Configure the business.

Enter real operations through the ERP.
```

---

**End of Document**