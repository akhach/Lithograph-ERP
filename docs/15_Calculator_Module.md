# Lithograph ERP

**Document:** 15_Calculator_Module.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Calculator

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `06_UI_UX_Principles.md`
- `14_Orders_Module.md`

---

# 1. Purpose

The Calculator module provides configurable calculation tools for Lithograph Orders.

Its main purposes are:

```text
Calculate Order Selling Price

Record Order Costs

Calculate Order Cost Price

Provide reusable Calculator Templates

Allow authorized Users to design Calculator Templates
```

The Calculator is built directly into Lithograph ERP.

It does not depend on Microsoft Excel.

---

# 2. Main Concept

Each Order has an Order Type.

An Order Type may have an assigned Calculator Template.

Conceptually:

```text
Order
   ↓
Order Type
   ↓
Calculator Template
   ↓
Published Template Version
   ↓
Order Calculator
```

---

# 3. Main Responsibilities

The Calculator module is responsible for:

```text
Calculator Templates

Template Versions

Template Designer

Formula Engine

Order Calculator Values

Order Cost Items

Selling Price Calculation

Cost Price Calculation
```

---

# 4. Module Ownership

The Calculator module owns PostgreSQL schema:

```text
calculator
```

Version 1 tables:

```text
calculator.templates

calculator.template_versions

calculator.order_calculators

calculator.cost_items
```

---

# 5. Database Structure

```text
calculator
│
├── templates
│
├── template_versions
│
├── order_calculators
│
└── cost_items
```

Relationships:

```text
Calculator Template
       │
       └── Template Versions

Order Type
       │
       └── Calculator Template

Order
       │
       ├── Order Calculator
       │       └── Template Version
       │
       └── Cost Items
```

---

# 6. Why Template Versions Are Required

Calculator Templates may change over time.

Example:

```text
January:
UV Printing Calculator v1

March:
UV Printing Calculator v2

June:
UV Printing Calculator v3
```

An Order created using Version 1 must not silently change because Version 3 exists later.

Therefore published Calculator versions are immutable.

---

# 7. Historical Safety Principle

Every Order Calculator is permanently linked to the specific Template Version it was created with.

Example:

```text
ORD-2026-000125
    ↓
UV Printing Calculator
    ↓
Version 3
```

If Version 4 is later published:

```text
ORD-2026-000125
```

continues using:

```text
Version 3
```

unless an explicit migration/reset operation is performed.

---

# 8. Table: calculator.templates

## Purpose

Represents the logical Calculator Template.

Examples:

```text
UV Printing Calculator

CO₂ Laser Calculator

Graphic Design Calculator

Installation Calculator
```

The Template itself is a stable identity.

Its detailed content exists in Template Versions.

---

# 9. templates Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `name` | `varchar(200)` | No | Template name |
| `description` | `text` | Yes | Template description |
| `is_active` | `boolean` | No | Available for assignment/use |
| `created_at` | `timestamptz` | No | Creation time |
| `created_by` | `uuid` | Yes | Creating User |
| `updated_at` | `timestamptz` | Yes | Last metadata update |
| `updated_by` | `uuid` | Yes | Last modifying User |

---

# 10. Template Primary Key

```text
PRIMARY KEY (id)
```

---

# 11. Template Name

Template Name is required.

Examples:

```text
UV Printing

Laser Cutting

Graphic Design
```

Names should be unique enough to avoid confusion.

The system should prevent case-insensitive duplicate Template names.

---

# 12. Template Business ID

Calculator Templates do not require Business IDs.

Do not create:

```text
CALC-000001
```

UUID and Template Name are sufficient.

---

# 13. Active Template

```text
is_active = true
```

means the Template may normally be assigned to Order Types.

Inactive Templates remain stored for historical Orders.

---

# 14. Template Deactivation

Deactivating a Template must not affect existing Orders.

Historical Order Calculators continue using their existing Template Versions.

---

# 15. Order Type Assignment

Order Types reference:

```text
calculator.templates.id
```

through:

```text
orders.order_types.calculator_template_id
```

An Order Type may have:

```text
0 or 1 Calculator Template
```

in Version 1.

---

# 16. One Template, Multiple Order Types

The same Template may be assigned to multiple Order Types if useful.

Example:

```text
Order Type:
Small UV Printing

Order Type:
Large UV Printing

Both
    ↓
UV Printing Calculator
```

This is allowed.

---

# 17. Table: calculator.template_versions

## Purpose

Stores actual Template definitions.

Published versions are immutable historical calculation definitions.

---

# 18. template_versions Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `template_id` | `uuid` | No | Parent Template |
| `version_number` | `integer` | No | Sequential Template version |
| `status` | `varchar(20)` | No | Draft / Published / Retired |
| `definition` | `jsonb` | No | Calculator layout, fields and formulas |
| `created_at` | `timestamptz` | No | Version creation time |
| `created_by` | `uuid` | Yes | Creating User |
| `published_at` | `timestamptz` | Yes | Publication time |
| `published_by` | `uuid` | Yes | Publishing User |

---

# 19. Template Version Primary Key

```text
PRIMARY KEY (id)
```

---

# 20. Version Number

Version numbers start at:

```text
1
```

Example:

```text
Template:
UV Printing

Versions:
1
2
3
4
```

The combination must be unique:

```text
UNIQUE (
    template_id,
    version_number
)
```

---

# 21. Template Version Status

Version 1 statuses:

```text
Draft

Published

Retired
```

---

# 22. Draft Version

A Draft Version may be edited in the Template Designer.

Draft changes do not affect existing Orders.

---

# 23. Published Version

A Published Version is available for new Order Calculators.

Once published:

```text
definition
```

must become immutable.

---

# 24. Retired Version

A Retired Version:

- Cannot normally be used for new Orders
- Remains stored
- Continues supporting historical Orders already linked to it

Retiring is not deletion.

---

# 25. Published Versions Are Immutable

Do not edit a published Template Version in place.

Incorrect:

```text
Version 3
    ↓
Modify formula
    ↓
Still call it Version 3
```

Correct:

```text
Version 3
    ↓
Create Draft Version 4
    ↓
Edit Version 4
    ↓
Publish Version 4
```

---

# 26. Template Editing Workflow

Recommended workflow:

```text
Published Version 3
       ↓
Create New Draft
       ↓
Version 4 Draft
       ↓
Edit
       ↓
Test
       ↓
Publish
       ↓
Version 4 Published
```

Version 3 remains unchanged.

---

# 27. Current Published Version

For new Orders, the system uses the newest active Published Version of the Template.

Example:

```text
Published:
v1
v2
v3

Newest:
v3
```

New Order Calculators use:

```text
v3
```

---

# 28. Draft Does Not Affect New Orders

If:

```text
v4 = Draft
```

while:

```text
v3 = Published
```

new Orders continue using:

```text
v3
```

until Version 4 is published.

---

# 29. Why JSONB Is Used

Calculator Template structure is genuinely flexible.

Different Templates may contain different:

- Fields
- Tables
- Labels
- Formulas
- Layouts
- Dropdown options

For this reason:

```text
definition jsonb
```

is appropriate.

This is an intentional exception to the general preference for relational data.

---

# 30. JSONB Boundary

JSONB is used only for flexible Calculator configuration and Calculator field values.

Normal business relationships remain relational.

Do not use JSONB for:

```text
Users

Employees

Clients

Projects

Orders

Roles

Project Teams
```

---

# 31. Template Definition

Conceptually, `definition` contains:

```text
Template metadata

Layout

Elements

Field keys

Field types

Labels

Default values

Dropdown options

Formulas

Visibility scope

Selling Price output definition
```

The exact JSON schema must be version-controlled in application code.

---

# 32. Stable Field Keys

Every editable or calculated Template field must have a stable internal key.

Example:

```text
width_mm

height_mm

quantity

material_price

markup

selling_price
```

The displayed label may be:

```text
Width
```

while the internal key remains:

```text
width_mm
```

---

# 33. Field Key Rules

Within one Template Version:

- Field keys must be unique
- Keys should be lowercase
- Keys should use `snake_case`
- Keys should not contain spaces
- Formula references should use keys, not display labels

---

# 34. Example

```text
Display Label:
Width (mm)

Field Key:
width_mm
```

Formula:

```text
width_mm * height_mm
```

not:

```text
Width (mm) * Height (mm)
```

---

# 35. Template Elements

Version 1 may support these element types:

```text
Label

Number Input

Text Input

Dropdown

Checkbox

Calculated Field

Table

Section
```

Do not attempt to reproduce all Excel control types.

---

# 36. Label

A Label displays explanatory text.

It stores no Order value.

Example:

```text
Material Information
```

---

# 37. Number Input

Number Input stores a numeric value.

Examples:

```text
Width

Height

Quantity

Material Price
```

---

# 38. Text Input

Text Input stores simple text.

Example:

```text
Material Name
```

Use structured fields instead where a defined list is more appropriate.

---

# 39. Dropdown

Dropdown allows selection from predefined Template options.

Example:

```text
Material

3 mm PVC
5 mm PVC
10 mm PVC
```

Dropdown options are part of the Template Version.

---

# 40. Checkbox

Checkbox stores:

```text
true / false
```

Example:

```text
Double-sided printing
```

---

# 41. Calculated Field

Calculated Fields display results produced by formulas.

They are normally read-only to the User.

Examples:

```text
Area

Material Cost

Print Cost

Selling Price
```

---

# 42. Section

Section is a UI grouping element.

Example:

```text
Dimensions

Material

Production

Price
```

A Section does not represent a database entity.

---

# 43. Table Element

A Template may contain spreadsheet-like tables.

Example:

| Material | Qty | Unit Price | Total |
|---|---:|---:|---:|
| PVC | 2 | 5,000 | 10,000 |
| Film | 3 | 2,000 | 6,000 |

Tables may support:

- Defined columns
- User-added rows
- Formula columns
- Total calculations

---

# 44. Spreadsheet-Like Does Not Mean Excel Clone

The Template Designer should feel familiar to spreadsheet users.

However, Lithograph ERP does not attempt to implement:

```text
Full Excel compatibility

Excel files as runtime templates

Macros

VBA

Pivot Tables

External workbook references

Thousands of Excel functions
```

---

# 45. Formula Engine

The Calculator uses an internal safe Formula Engine.

The Formula Engine evaluates approved expressions.

Example:

```text
width_mm * height_mm * quantity
```

---

# 46. Formula Safety

Never evaluate Calculator formulas using:

```text
JavaScript eval()

C# dynamic compilation

SQL execution

Shell commands

Python execution
```

Calculator formulas are data, not executable application code.

---

# 47. Formula Parser

The Formula Engine must use a controlled parser/evaluator.

It should support only:

- Approved operators
- Approved functions
- Approved field references

Anything outside the allowed grammar must be rejected.

---

# 48. Basic Operators

Version 1 should support:

```text
+
-
*
/
%
```

and normal parentheses:

```text
(
)
```

---

# 49. Comparisons

Formula expressions may support:

```text
=

!=

>

>=

<

<=
```

using whatever exact syntax the selected Formula Engine defines.

The syntax must remain consistent.

---

# 50. Approved Functions

Version 1 should support a limited function set.

Initial functions may include:

```text
SUM

AVERAGE

MIN

MAX

COUNT

ROUND

ROUNDUP

ROUNDDOWN

CEILING

FLOOR

ABS

SQRT

POWER

MOD

IF

AND

OR

NOT

MIN

MAX
```

Duplicate semantic functions should not be added unnecessarily.

The final approved list should remain below approximately:

```text
30 functions
```

unless a real requirement appears.

---

# 51. Function Philosophy

New functions are added only when real Calculator Templates require them.

Do not implement Excel functions merely because Excel has them.

---

# 52. Formula Examples

Example:

```text
area_m2 =
(width_mm / 1000) * (height_mm / 1000)
```

Example:

```text
material_total =
material_price * quantity
```

Example:

```text
selling_price =
ROUND(base_cost * markup_factor, 0)
```

Example:

```text
minimum_charge =
MAX(calculated_price, 5000)
```

---

# 53. IF Example

Conceptually:

```text
IF(quantity >= 100, price * 0.9, price)
```

Exact syntax is defined by the Formula Engine.

---

# 54. Formula Dependency Graph

Calculated Fields may depend on other Fields.

Example:

```text
width
   ↓
area
   ↓
material_cost
   ↓
selling_price
```

The Calculator Engine must determine a valid evaluation order.

---

# 55. Circular References

Circular formulas are not allowed.

Example:

```text
a = b + 1

b = a + 1
```

The Template Designer must detect this and reject publication.

---

# 56. Division by Zero

Formula errors such as division by zero must produce a controlled Calculator error.

Do not crash the application.

The affected field should clearly show that calculation failed.

---

# 57. Invalid Formula

Draft Templates may temporarily contain invalid formulas during editing.

A Template Version must not be published while formula validation errors remain.

---

# 58. Template Publication Validation

Before publication, validate at least:

```text
Unique field keys

Valid formulas

No circular references

Valid dropdown definitions

Valid field references

Valid Selling Price output

Valid layout definition
```

---

# 59. Template Designer

The Template Designer is an Administration feature.

It allows authorized Users to create and edit Calculator Templates.

---

# 60. Template Designer Main Areas

A practical UI may contain:

```text
Element Palette

Design Surface

Element Properties

Formula Editor

Preview/Test Area
```

The exact visual layout may evolve during implementation.

---

# 61. Element Palette

The palette may contain:

```text
Label

Number

Text

Dropdown

Checkbox

Calculated Field

Table

Section
```

Users can add elements to the Template.

---

# 62. Element Properties

Selected element properties may include:

```text
Label

Field Key

Default Value

Required

Formula

Visibility Scope

Dropdown Options
```

depending on element type.

---

# 63. Template Layout

Version 1 should support a simple grid or ordered layout.

Do not build a full page-layout engine.

The purpose is to organize fields clearly, not replace professional design software.

---

# 64. Template Preview

Before publishing, the designer should be able to test the Template with sample values.

The test should show:

- Inputs
- Calculated results
- Formula errors

Testing does not create a real Order.

---

# 65. Table: calculator.order_calculators

## Purpose

Stores the Calculator state for one Order.

An Order has maximum:

```text
One active Calculator instance
```

in Version 1.

---

# 66. order_calculators Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `order_id` | `uuid` | No | Order reference |
| `template_version_id` | `uuid` | No | Historical Template Version |
| `field_values` | `jsonb` | No | User-entered and stored Calculator values |
| `last_calculated_at` | `timestamptz` | Yes | Most recent calculation |
| `created_at` | `timestamptz` | No | Creation time |
| `created_by` | `uuid` | Yes | Creating User |
| `updated_at` | `timestamptz` | Yes | Last update |
| `updated_by` | `uuid` | Yes | Last modifying User |

---

# 67. Order Calculator Primary Key

```text
PRIMARY KEY (id)
```

---

# 68. One Calculator per Order

Database must enforce:

```text
UNIQUE (order_id)
```

Version 1 does not maintain several simultaneous Calculator instances for one Order.

---

# 69. Template Version Relationship

```text
template_version_id
→ calculator.template_versions.id
```

The relationship points to an exact published Template Version.

---

# 70. Order Relationship

```text
order_id
→ orders.orders.id
```

The Calculator belongs to one Order.

---

# 71. Order Calculator Creation

The Calculator may be created lazily when the User first opens it.

Flow:

```text
Open Order Calculator
       ↓
Check Order Type
       ↓
Find assigned Template
       ↓
Find newest Published Version
       ↓
Create order_calculators record
       ↓
Lock Order to that Template Version
```

---

# 72. Existing Calculator Opening

If an Order already has an Order Calculator:

```text
Use existing template_version_id
```

Do not switch automatically to the newest Template Version.

---

# 73. Field Values

`field_values` stores values keyed by stable Template field keys.

Conceptually:

```text
{
    "width_mm": 1200,
    "height_mm": 800,
    "quantity": 10,
    "material": "5mm_pvc"
}
```

---

# 74. Calculated Values

Calculated fields do not necessarily need to be permanently stored if they can safely be recalculated from:

```text
Template Version
+
Field Values
+
Cost Items
```

The application may calculate them when loading/saving.

Do not duplicate every calculated field unless a real historical or performance requirement exists.

---

# 75. Historical Determinism

Because a Calculator retains:

```text
Template Version

Field Values

Cost Items
```

the historical calculation can be reproduced.

Final Selling Price and Cost Price are still stored on the Order as historical business values.

---

# 76. Selling Price Output

Each published Template must define one calculated output as:

```text
Selling Price Output
```

Example field key:

```text
selling_price
```

The exact key does not need to be identical across Templates because the Template metadata identifies the designated output.

---

# 77. Selling Price Synchronization

After successful Calculator calculation:

```text
Calculator Selling Price
       ↓
orders.orders.selling_price
```

The update should occur safely in the same business operation where practical.

---

# 78. Selling Price Historical Snapshot

`orders.orders.selling_price` is the final stored historical value.

Even if the Calculator can reproduce it, it remains stored because it is important transactional business data.

---

# 79. Costs

The Calculator module also contains the Order Costs submodule.

Each Order may have multiple Cost Items.

Examples:

```text
Material purchase

Outsourcing

Transport

Installation expense

Packaging
```

---

# 80. Table: calculator.cost_items

## Purpose

Stores individual expenses related to an Order.

This provides the per-Order mini expense database required by Lithograph.

---

# 81. cost_items Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `order_id` | `uuid` | No | Order reference |
| `category` | `varchar(150)` | No | Expense category |
| `supplier` | `varchar(200)` | Yes | Supplier name |
| `expense_date` | `date` | Yes | Expense date |
| `description` | `text` | Yes | Expense description |
| `amount` | `numeric(18,2)` | No | Expense amount |
| `sort_order` | `integer` | No | Display order |
| `created_at` | `timestamptz` | No | Creation time |
| `created_by` | `uuid` | Yes | Creating User |
| `updated_at` | `timestamptz` | Yes | Last update |
| `updated_by` | `uuid` | Yes | Last modifying User |

---

# 82. Cost Item Primary Key

```text
PRIMARY KEY (id)
```

---

# 83. Cost Item Relationship

```text
order_id
→ orders.orders.id
```

One Order may contain:

```text
Zero or many Cost Items
```

---

# 84. Why Cost Items Reference Order

Cost Items reference the Order directly rather than the Template Version.

This is intentional.

Actual Order expenses should survive:

- Template changes
- Calculator redesign
- Calculator resets

They are business records, not Template configuration.

---

# 85. Cost Category

Version 1 stores:

```text
category
```

as simple text.

Examples:

```text
Material

Outsource

Transport

Installation

Packaging
```

---

# 86. Cost Category Table

Version 1 does not require a separate Cost Categories table.

If inconsistent category names become a reporting problem, configurable Cost Categories can be introduced later.

Do not add that complexity prematurely.

---

# 87. Supplier

Version 1 stores:

```text
supplier
```

as optional text.

Example:

```text
ABC Plastics
```

A dedicated Suppliers module is outside Version 1.

---

# 88. Supplier Relationship

Do not create:

```text
supplier_id
```

until a Supplier/Purchasing module exists.

The current text field keeps Costs independent from future Purchasing architecture.

---

# 89. Expense Date

`expense_date` is optional.

Use:

```text
date
```

rather than timestamp because time-of-day is not required.

---

# 90. Cost Amount

`amount` uses:

```text
numeric(18,2)
```

Do not use floating point.

Negative Cost Items are not allowed in normal Version 1 operation.

---

# 91. Cost Price Calculation

Order Cost Price is:

```text
SUM(cost_items.amount)
```

for that Order.

Example:

```text
Material       25,000
Transport       5,000
Outsource      40,000
---------------------
Cost Price     70,000
```

---

# 92. Cost Price Synchronization

Whenever Cost Items change:

```text
SUM(Cost Items)
       ↓
orders.orders.cost_price
```

The update should occur transactionally where practical.

---

# 93. No Manual Cost Price Editing

Version 1 should not normally allow direct editing of:

```text
orders.orders.cost_price
```

The value comes from Cost Items.

This avoids disagreement between:

```text
Cost Rows
```

and:

```text
Stored Cost Price
```

---

# 94. Empty Costs

If an Order has no Cost Items:

```text
cost_price = 0
```

---

# 95. Cost Mini Database UI

The Order Calculator may contain a Costs area resembling a simple spreadsheet.

Example:

| Category | Supplier | Date | Description | Amount |
|---|---|---|---|---:|
| Material | ABC Plastics | 2026-09-20 | PVC 5 mm | 25,000 |
| Transport | — | 2026-09-21 | Delivery | 5,000 |
| Outsource | MetalCo | 2026-09-22 | Metal frame | 40,000 |

Total:

```text
70,000
```

---

# 96. Cost Row Operations

Users with permission may:

```text
Add Cost Item

Edit Cost Item

Remove Cost Item

Reorder Cost Items
```

---

# 97. Cost Deletion

Cost Items may be physically removed in Version 1.

Because they affect financial values, deletion must require proper authorization.

A full financial audit ledger is outside Version 1.

---

# 98. Cost Audit Fields

Unlike simple Checklist Items, Cost Items contain financial information.

Therefore they include:

```text
created_at

created_by

updated_at

updated_by
```

---

# 99. Calculator and Cost Relationship

The Selling Price Calculator may use the Order's Cost Total if needed.

Conceptually the Formula Engine may expose a special system value:

```text
cost_total
```

representing:

```text
SUM(Order Cost Items)
```

---

# 100. Example Selling Formula

A Template may conceptually calculate:

```text
selling_price =
ROUND(cost_total * markup_factor, 0)
```

This allows actual cost data to participate in price calculation where useful.

---

# 101. System Values

The Formula Engine may expose approved read-only system values such as:

```text
cost_total
```

Additional system values must be added only when required.

Do not expose arbitrary database data to formulas.

---

# 102. Formula Database Access

Calculator formulas must not execute database queries directly.

The application prepares approved values and passes them into the Formula Engine.

Formula examples must never include:

```text
SELECT ...

SQL()

DATABASE()
```

---

# 103. Financial Visibility

Calculator financial information must respect Permissions.

At minimum distinguish:

```text
General Calculator Data

Selling Price Data

Cost Data
```

---

# 104. Visibility Scope

Template elements may define a simple visibility scope:

```text
general

selling

cost
```

This is stored in Template configuration.

---

# 105. General Scope

```text
general
```

is ordinary production/calculation information.

Examples:

```text
Width

Height

Quantity

Material
```

---

# 106. Selling Scope

```text
selling
```

is information related to Client pricing.

Visibility requires the appropriate Selling Price permission.

---

# 107. Cost Scope

```text
cost
```

is internal cost information.

Visibility requires appropriate Cost permissions.

---

# 108. Backend Security

Visibility is not only a frontend concern.

The backend must avoid returning protected Cost/Selling data to Users who lack permission.

Hiding a field in React alone is insufficient.

---

# 109. Cost Permission

Suggested permissions:

```text
calculator.view_costs

calculator.edit_costs
```

---

# 110. Calculator Permissions

Initial Calculator permissions may include:

```text
calculator.view

calculator.edit

calculator.view_costs

calculator.edit_costs

calculator.manage_templates

calculator.publish_templates
```

---

# 111. calculator.view

Allows viewing the Order Calculator fields permitted by the User's financial permissions.

---

# 112. calculator.edit

Allows editing normal Calculator input values.

It does not automatically allow:

```text
Edit Costs

Design Templates
```

---

# 113. calculator.view_costs

Allows viewing:

```text
Cost Items

Cost Price

Cost-scoped Calculator Fields
```

---

# 114. calculator.edit_costs

Allows:

```text
Add Cost Item

Edit Cost Item

Remove Cost Item
```

The User also implicitly needs appropriate viewing access.

---

# 115. calculator.manage_templates

Allows:

```text
Create Template

Edit Draft Version

Create New Draft Version

Deactivate Template
```

---

# 116. calculator.publish_templates

Allows publishing a validated Draft Version.

Publication is separated because it changes the Calculator used by future Orders.

---

# 117. Template Administration

Calculator Templates should normally appear under:

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

# 118. Templates List

Example:

| Template | Latest Published | Draft | Status |
|---|---:|---:|---|
| UV Printing | v3 | v4 | Active |
| Laser Cutting | v2 | — | Active |
| Graphic Design | v1 | — | Active |

---

# 119. Create Template

Creating a Template should also create an initial Draft Version:

```text
Template created
      ↓
Version 1 Draft
```

The User then opens the Template Designer.

---

# 120. Publish Template

Publication flow:

```text
Validate Draft
      ↓
No errors
      ↓
Publish
      ↓
Draft becomes immutable Published Version
```

If validation fails:

```text
Publication blocked
```

with clear errors.

---

# 121. Editing Published Template

The UI should not allow direct editing of Published content.

Instead:

```text
Create New Version
```

which copies the latest Published definition into a new Draft.

---

# 122. Template Version Deletion

A Draft Version that has never been used may be deleted if desired.

A Published Version referenced by an Order Calculator must never be physically deleted.

---

# 123. Template Version Retention

Historical Published Versions should remain indefinitely while referenced by Orders.

This protects reproducibility.

---

# 124. Order Calculator Upgrade

Version 1 should not automatically upgrade existing Order Calculators to newer Template Versions.

Automatic upgrade is explicitly prohibited.

---

# 125. Manual Calculator Upgrade

If a real need appears to migrate an Order to a newer Template Version, the action must be explicit.

Potential workflow:

```text
Upgrade Calculator Version
       ↓
Warning
       ↓
Review compatibility
       ↓
Explicit confirmation
```

This does not need to be implemented initially.

---

# 126. Order Type Change with Existing Calculator

If the Order Type changes after Calculator data exists:

```text
Do not silently replace Calculator.
```

The UI should block or require explicit Calculator reset.

---

# 127. Version 1 Recommended Rule

Use the simple rule:

```text
Order Type may change freely only before an Order Calculator exists.
```

After a Calculator exists, changing Order Type requires an explicit:

```text
Reset Calculator
```

workflow.

---

# 128. Reset Calculator

Resetting Calculator data is destructive.

If implemented, the UI must clearly warn:

```text
Existing Calculator inputs will be removed.

Order Cost Items will be preserved.
```

This distinction is important.

---

# 129. Cost Preservation During Reset

Calculator reset must not delete:

```text
calculator.cost_items
```

because Cost Items belong directly to the Order and represent actual expenses.

---

# 130. Selling Price After Reset

If Calculator data is reset, the application should deliberately decide what happens to:

```text
selling_price
```

Recommended Version 1 behavior:

```text
Set Selling Price to 0
```

until the new Calculator is completed.

The User must confirm this destructive operation.

---

# 131. Cost Price After Reset

Because Cost Items are preserved:

```text
cost_price
```

remains:

```text
SUM(cost_items.amount)
```

---

# 132. Autosave

Calculator input editing may benefit from auto-save.

However, Version 1 should not introduce unreliable hidden saving behavior.

A clear model may use:

```text
Save Calculator
```

or controlled auto-save with visible state.

Whichever approach is selected must remain consistent.

---

# 133. Recalculation

When an input changes:

```text
Dependent calculated fields
```

should update immediately in the frontend when practical.

The backend must still validate and recalculate authoritative results when saving.

---

# 134. Backend Is Authoritative

The frontend may calculate for responsiveness.

However:

```text
Final Selling Price
Final Cost Price
```

must be validated/calculated by backend logic before persistence.

Never trust a client-supplied final price without server validation.

---

# 135. Shared Formula Engine Logic

Avoid having unrelated formula implementations in frontend and backend.

Where practical, define one authoritative formula specification and ensure both sides behave consistently.

Backend result is authoritative.

---

# 136. Number Precision

Formula calculations involving money must avoid normal binary floating-point inaccuracies.

Use decimal-safe arithmetic.

C# should use appropriate decimal handling for financial values.

---

# 137. Rounding

Rounding must be explicit.

Do not rely on accidental language/runtime rounding behavior.

Templates should use approved functions such as:

```text
ROUND
CEILING
FLOOR
```

where necessary.

---

# 138. Units

Calculator fields should make units explicit.

Examples:

```text
Width (mm)

Height (mm)

Area (m²)

Quantity (pcs)
```

Do not hide unit assumptions.

---

# 139. Unit Conversion

Version 1 formulas may perform normal arithmetic conversion.

Example:

```text
area_m2 =
(width_mm / 1000) *
(height_mm / 1000)
```

A complex unit-conversion subsystem is not required.

---

# 140. Required Fields

Template input fields may be marked:

```text
required
```

The backend must prevent final calculation/save when required values are missing where necessary.

---

# 141. Defaults

Templates may define default values.

Example:

```text
quantity = 1
```

Defaults become initial Order Calculator values.

Changing a Template default later does not change existing Order Calculator values.

---

# 142. Dropdown Option Stability

Published dropdown options belong to the Template Version.

Existing Orders retain the options and selected values from their historical Template Version.

---

# 143. Formula Error Display

Users should receive understandable errors.

Good:

```text
Selling Price cannot be calculated because Width is empty.
```

Better than:

```text
Expression evaluation exception.
```

Technical details belong in logs.

---

# 144. Template Designer Error Display

Publication errors should identify:

```text
Field

Formula

Problem
```

Example:

```text
Field: selling_price

Unknown field reference: material_costx
```

---

# 145. Order Calculator Workspace

Inside the Order Workspace:

```text
General

Calculator

Checklist

Folder Links
```

The Calculator tab/section contains:

```text
Template Calculator

Cost Items

Selling Price

Cost Price
```

subject to Permissions.

---

# 146. Calculator Header

Useful information:

```text
Calculator:
UV Printing

Template Version:
3
```

Version information may be secondary but should be available for troubleshooting/history.

---

# 147. Cost Items and Formula Area

The UI should not force Users to navigate to a completely separate Costs module.

Costs belong in the Order Calculator context.

---

# 148. Calculator Without Template

If an Order Type has no assigned Template:

```text
No Calculator Template is assigned to this Order Type.
```

The Order can still exist in Draft configuration scenarios.

Calculator calculation is unavailable.

---

# 149. Order Type Without Published Version

If the assigned Template has no Published Version:

```text
Calculator is not available for new Orders.
```

The administrative UI should clearly show this configuration problem.

---

# 150. Changing Template Assignment

Changing an Order Type's assigned Template affects:

```text
Future Order Calculators
```

It does not change existing Order Calculators.

---

# 151. Template Assignment Example

Existing:

```text
ORD-001
→ UV Template v2
```

Administrator changes Order Type to:

```text
UV Template B
```

New Order:

```text
ORD-002
→ UV Template B latest published version
```

Existing:

```text
ORD-001
```

remains on:

```text
UV Template v2
```

---

# 152. Calculator API Responsibilities

Conceptual operations:

```text
Get Templates

Create Template

Update Template Metadata

Create Draft Version

Get Template Version

Update Draft Definition

Validate Draft

Publish Version

Retire Version

Get Order Calculator

Create Order Calculator

Update Calculator Values

Calculate Order

Get Cost Items

Add Cost Item

Update Cost Item

Remove Cost Item

Reorder Cost Items
```

Exact REST routes are defined during implementation.

---

# 153. Template Response DTO

May contain:

```text
id

name

description

is_active

latest_published_version

draft_version
```

---

# 154. Template Version DTO

May contain:

```text
id

template_id

version_number

status

definition

created_at

published_at
```

Only Users with Template permissions should receive full editable Template definitions where appropriate.

---

# 155. Order Calculator DTO

May contain:

```text
id

order_id

template

template_version

visible_definition

field_values

calculated_values

selling_price

cost_price

last_calculated_at
```

Data must be filtered by authorization.

---

# 156. Cost Item DTO

May contain:

```text
id

category

supplier

expense_date

description

amount

sort_order
```

Only authorized Users should receive Cost Item data.

---

# 157. Validation Rules

Backend must validate:

```text
Template exists

Template Version exists

Published Version is immutable

Field keys are unique

Formula syntax is valid

Field references exist

No circular formulas

Required fields are valid

Dropdown values are valid

Order exists

Order Type matches Calculator context

Cost amount is non-negative

User has required Permissions
```

---

# 158. Required Indexes: templates

```text
PRIMARY KEY (id)
```

Template normalized-name uniqueness should be enforced according to implementation.

---

# 159. Required Indexes: template_versions

```text
PRIMARY KEY (id)
```

```text
UNIQUE (
    template_id,
    version_number
)
```

Index:

```text
template_id
```

---

# 160. Required Indexes: order_calculators

```text
PRIMARY KEY (id)
```

```text
UNIQUE (order_id)
```

Index:

```text
template_version_id
```

---

# 161. Required Indexes: cost_items

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

# 162. Delete Behavior

Templates referenced by Order Types or historical Calculator data must not be destructively deleted.

Use:

```text
is_active = false
```

and Version retirement.

---

# 163. Template Version Delete Behavior

Published Versions referenced by Orders must be preserved.

Draft Versions with no historical use may be deleted.

---

# 164. Order Calculator Delete Behavior

Normal business workflow does not delete Order Calculators.

Explicit Calculator reset may replace/remove Calculator data only through controlled business logic.

---

# 165. Cost Item Delete Behavior

Cost Items may be deleted by authorized Users.

Deleting one immediately changes the Order's calculated Cost Price.

The UI should make this effect clear.

---

# 166. Reporting

Cost Items may support future reports such as:

```text
Costs by Category

Costs by Supplier Name

Costs by Date

Costs by Order
```

Do not create separate reporting storage.

Reports query operational data.

---

# 167. Calculator Performance

Templates are small.

Version 1 expects approximately:

```text
Maximum ~30 supported Formula Functions
```

and modest numbers of fields.

Do not introduce distributed calculation infrastructure.

---

# 168. No External Excel Dependency

Version 1 must run without:

```text
Microsoft Excel

Office installation

Excel COM automation

LibreOffice runtime

External spreadsheet server
```

The Calculator Engine is internal to Lithograph ERP.

---

# 169. Excel Import

Version 1 does not require importing Excel formulas or workbooks.

Existing Excel calculators may be manually recreated using Template Designer.

---

# 170. Excel Export

Version 1 does not require exporting Calculator Templates back to Excel.

Reporting export can be designed separately if needed.

---

# 171. Macros

Calculator Templates do not support:

```text
VBA

JavaScript

Python

C#

Shell scripts
```

Formula logic uses only the approved Formula Engine.

---

# 172. External API Calls

Calculator formulas may not call arbitrary external APIs in Version 1.

This protects:

- Security
- Predictability
- Historical reproducibility

---

# 173. Dynamic Database Lookups

Version 1 Formula Engine should not contain arbitrary database lookup functions.

If a future Calculator needs structured price lists, that should be designed as a specific feature rather than allowing free database queries.

---

# 174. Template Sharing

Templates are internal Lithograph configuration.

Version 1 does not require:

```text
Template Marketplace

Template Sharing

Template Import Packages
```

---

# 175. Template Permissions

Production Users should not automatically receive Template Designer access.

Template modification can change future prices.

It should be limited to authorized administrative Users.

---

# 176. Template Publication Importance

Publishing a Template Version is a significant configuration action because all future applicable Orders may use it.

The UI should clearly show:

```text
Publish Version 4?
```

and identify the affected Template.

---

# 177. Publication Confirmation

A simple confirmation is appropriate.

Example:

```text
Publish UV Printing Calculator Version 4?

New Orders will use this version.
Existing Orders will remain unchanged.
```

---

# 178. Cost Data and Template Visibility

Cost Items are not embedded directly inside the Template JSON.

The Template may decide where the Cost table appears visually, but the actual financial rows remain relational:

```text
calculator.cost_items
```

---

# 179. Why Costs Are Relational

Cost Items need reliable:

```text
Dates

Amounts

Supplier text

Categories

Reporting

Audit fields
```

Therefore relational storage is better than storing all expenses inside one JSON document.

---

# 180. Why Calculator Fields Use JSONB

Calculator field structure varies substantially between Order Types.

Example:

UV Printing may need:

```text
Width
Height
Material
Ink Coverage
Quantity
```

Laser Cutting may need:

```text
Material
Thickness
Cut Length
Machine Time
Quantity
```

Creating database columns for every possible field would be rigid and wasteful.

---

# 181. Concurrency

When two Users edit the same Order Calculator simultaneously, the system should avoid silently overwriting changes.

Version 1 may use a simple optimistic concurrency strategy.

Exact implementation may use:

```text
updated_at
```

or a dedicated concurrency token if required.

Do not build complex collaborative spreadsheet editing.

---

# 182. Collaborative Editing

Real-time multi-user collaborative Calculator editing is outside Version 1.

No Google-Sheets-style simultaneous cursor collaboration is required.

---

# 183. Audit History

Version 1 does not require complete Calculator value history after every keystroke.

Current values plus audit timestamps are sufficient initially.

A detailed calculation revision history may be added later if required.

---

# 184. Template History

Template history is already preserved through immutable Published Versions.

This is more important than recording every Draft edit.

---

# 185. Cost History

Version 1 stores current Cost Item records and basic audit information.

It does not require an append-only accounting ledger.

A future accounting system may introduce stricter rules.

---

# 186. Currency

Version 1 assumes one business currency context.

Do not add multi-currency Calculator complexity until Lithograph requires it.

Money fields still use exact decimal types.

---

# 187. Taxes

VAT and other tax calculation are not automatically part of the Calculator module unless explicitly configured in a Template.

A future Accounting/Invoicing module may define tax behavior separately.

---

# 188. Discounts

Discount logic may be implemented through Template fields and formulas when required.

No separate Discount Engine is needed in Version 1.

---

# 189. Markup

Markup may be represented as a normal Calculator input or formula value.

Example:

```text
markup_factor = 1.3
```

No separate markup subsystem is required.

---

# 190. Minimum Price

Templates may implement minimum pricing using formulas.

Example:

```text
MAX(calculated_price, minimum_price)
```

---

# 191. Quantity Pricing

Quantity-dependent pricing may use:

```text
IF
```

and normal formulas.

If more complex lookup-table pricing becomes necessary later, it can be added deliberately.

---

# 192. Version 1 Non-Goals

The Calculator module does not include:

```text
Full Excel Compatibility

Microsoft Excel Runtime

VBA

Macros

Arbitrary Code Execution

Arbitrary SQL

External API Formula Calls

Real-Time Collaborative Editing

Complex Price List Engine

Multi-Currency Engine

Accounting Ledger

Template Marketplace

Automatic Existing-Order Template Upgrades

Unlimited Formula Functions
```

---

# 193. Future Extensions

Possible future additions may include:

- Configurable Cost Categories
- Supplier integration
- Material price lists
- Lookup tables
- Calculator revision history
- More formula functions
- Template import/export
- Manual Calculator version migration
- More advanced tables

These should be introduced only when real workflows require them.

---

# 194. Testing Requirements

Important tests include:

```text
Create Calculator Template

Create Version 1 Draft

Reject duplicate field keys

Validate formula

Reject unknown field reference

Detect circular reference

Publish valid Template Version

Reject invalid publication

Published Version cannot be edited

Create new Draft from Published Version

New Orders use latest Published Version

Existing Orders retain historical Version

Changing Template assignment does not change existing Orders

Create Order Calculator

One Calculator per Order

Save field values

Calculate Selling Price

Synchronize Selling Price to Order

Create Cost Item

Edit Cost Item

Delete Cost Item

Cost Price equals Cost Item total

Synchronize Cost Price to Order

Cost Items survive Calculator reset

Reject negative Cost amount

Hide Cost data without permission

Hide Selling fields without permission

Backend recalculates authoritative result

Formula division-by-zero handled safely

Draft Template can be tested

Order Type change does not silently destroy Calculator data
```

---

# 195. Version 1 Schema Summary

```text
calculator.templates

id
name
description
is_active
created_at
created_by
updated_at
updated_by
```

```text
calculator.template_versions

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

```text
calculator.order_calculators

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

```text
calculator.cost_items

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

Four tables are sufficient for Calculator Version 1.

---

# 196. Module Simplicity Rule

Before adding Calculator functionality, ask:

```text
Does Lithograph currently need this to calculate prices or record real Order costs?
```

If not, do not implement it yet.

The Calculator should remain powerful enough for Lithograph without becoming a general spreadsheet application.

---

# 197. Final Calculator Principle

The Calculator module should answer:

```text
How is this Order's Selling Price calculated?
```

```text
What actual Costs belong to this Order?
```

```text
What is the final Cost Price?
```

and:

```text
Which exact Calculator Version was used?
```

The central design rule is:

```text
Templates may evolve.

Historical Orders must not.
```

---

**End of Document**