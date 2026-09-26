# Lithograph ERP

**Document:** 33_Order_Calculator_Implementation_Plan.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Order Calculator

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
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
- `31_Orders_Implementation_Plan.md`
- `32_Calculator_Foundation_Implementation_Plan.md`

---

# 1. Purpose

This document defines how Calculator Templates become actual Calculators attached to Orders.

This phase implements:

```text
calculator.order_calculators
```

and connects:

```text
Order
→ Order Type
→ Calculator Template
→ Published Template Version
→ Order Calculator
```

The major objective is to preserve historical pricing behavior.

---

# 2. Main Goal

After this phase, Lithograph ERP must be able to:

```text
Open an Order Calculator

Automatically select the correct Published Template Version

Create the Order Calculator lazily

Render Template fields dynamically

Accept input values

Evaluate formulas

Persist field values

Return calculated results

Synchronize Order Selling Price

Detect concurrent edits

Reload saved Calculator values

Reset Calculator explicitly

Preserve historical Template Version
```

---

# 3. Core Historical Rule

Once an Order Calculator is created:

```text
Order Calculator
→ exact Template Version
```

That relationship must remain stable.

Publishing a newer Template Version must not silently change existing Orders.

---

# 4. Example

Assume:

```text
UV Printing Calculator

v1 Published
```

Order A opens Calculator.

Result:

```text
Order A
→ v1
```

Later:

```text
v2 Published
```

Then:

```text
Order A
→ still v1
```

A new Order B opens Calculator:

```text
Order B
→ v2
```

This behavior is mandatory.

---

# 5. One Calculator per Order

Version 1 supports:

```text
maximum one Order Calculator
per Order
```

Do not create multiple active Calculators for one Order.

---

# 6. Database Relationship

`calculator.order_calculators` contains:

```text
id

order_id

template_version_id

field_values

created_at

created_by

updated_at

updated_by

last_calculated_at
```

---

# 7. Field Values Storage

Store Calculator input/current values in:

```text
field_values jsonb
```

---

# 8. Why JSONB Is Appropriate

Calculator fields vary by Template.

Example Template A may contain:

```text
width_mm
height_mm
quantity
material
```

while Template B may contain:

```text
cut_length_m
material_thickness
machine_time
```

Therefore fixed relational columns are not appropriate for Calculator field values.

---

# 9. JSONB Does Not Store the Template

Do not duplicate the complete Template Definition inside:

```text
order_calculators.field_values
```

The Template Definition comes from:

```text
template_version_id
```

---

# 10. Recommended Implementation Sequence

```text
Task 1
Order Calculator entity

Task 2
EF Core configuration

Task 3
Database migration

Task 4
Calculator runtime value model

Task 5
Calculator creation resolver

Task 6
GET Order Calculator

Task 7
Runtime evaluation engine integration

Task 8
Save Calculator

Task 9
Selling Price synchronization

Task 10
Calculator permissions

Task 11
Optimistic concurrency

Task 12
Calculator reset

Task 13
Order Type change protection

Task 14
Frontend dynamic renderer

Task 15
Calculator editing UX

Task 16
Error handling

Task 17
Historical tests

Task 18
Integration/security review
```

---

# 11. Task 1 — Order Calculator Entity

Create:

```text
OrderCalculator
```

mapped to:

```text
calculator.order_calculators
```

---

# 12. Primary Key

Use:

```text
uuid
```

for:

```text
id
```

---

# 13. Order Relationship

Required:

```text
order_id
→ orders.orders.id
```

---

# 14. Unique Order Constraint

Enforce:

```text
UNIQUE(order_id)
```

This guarantees one Calculator per Order.

---

# 15. Template Version Relationship

Required:

```text
template_version_id
→ calculator.template_versions.id
```

---

# 16. Exact Version Reference

The relationship is to:

```text
Template Version
```

not:

```text
Template
```

because historical behavior depends on the exact published definition.

---

# 17. Field Values

Use:

```text
jsonb
```

for:

```text
field_values
```

---

# 18. Initial Field Values

A newly created Calculator may begin with:

```text
{}
```

plus defaults derived from the Template Definition during runtime.

Do not unnecessarily persist every default value before the User changes anything.

---

# 19. Audit Fields

Store:

```text
created_at

created_by

updated_at

updated_by
```

---

# 20. Last Calculation Timestamp

Store:

```text
last_calculated_at
```

when an authoritative backend calculation completes successfully.

---

# 21. No Version Copy

Do not copy:

```text
version_number

Template Definition
```

into Order Calculator.

The FK provides the historical relationship.

---

# 22. No Active Flag

Version 1 does not require:

```text
is_active
```

on Order Calculator because there is only one current Calculator per Order.

Reset modifies/replaces its runtime state explicitly.

---

# 23. Task 2 — EF Core Configuration

Configure:

```text
schema = calculator

table = order_calculators
```

---

# 24. Required Constraints

Configure:

```text
PK(id)

UNIQUE(order_id)

FK(order_id)

FK(template_version_id)

field_values JSONB

audit FKs
```

---

# 25. Order Delete Behavior

Normal Order physical deletion is not supported.

Do not use dangerous cascading behavior that could silently destroy historical Calculator data.

---

# 26. Template Version Delete Behavior

A Template Version referenced by an Order Calculator must not be physically deleted.

Use restrictive behavior.

---

# 27. Historical Referential Integrity

This relationship:

```text
Order Calculator
→ Template Version
```

must remain valid permanently for historical Orders.

---

# 28. Task 3 — Migration

Recommended migration name:

```text
AddOrderCalculators
```

---

# 29. Migration Scope

Create:

```text
calculator.order_calculators
```

only, plus required indexes/FKs.

Do not create Cost Items yet unless that later phase is intentionally combined.

---

# 30. Migration Review

Verify:

```text
UUID PK

order_id unique

template_version_id required

field_values JSONB

audit fields

last_calculated_at

safe FKs
```

---

# 31. Task 4 — Runtime Calculator Model

Create a typed runtime representation.

Conceptually:

```text
OrderCalculatorRuntime
```

containing:

```text
Template metadata

Template Version

Rendered fields

Stored values

Calculated values

Validation errors

Selling Price result
```

---

# 32. Stored vs Calculated Values

Keep distinction clear.

Stored values represent User-entered or persistable field state.

Calculated values may be recreated from:

```text
Template Definition
+
stored field values
```

---

# 33. Do Not Store Every Calculated Field

Version 1 should not automatically persist all calculated outputs in JSONB if they can be safely recomputed.

Persist:

```text
field_values
```

necessary for reproducing Calculator state.

Calculate derived values from the exact Template Version.

---

# 34. Selling Price Exception

The authoritative final Selling Price is also stored in:

```text
orders.orders.selling_price
```

because it is a historical business value used widely throughout ERP.

---

# 35. Backend Authority

The backend Formula Engine is authoritative.

Frontend calculations are for responsiveness only.

---

# 36. Task 5 — Calculator Creation Resolver

When an Order Calculator is requested, backend must determine whether one already exists.

---

# 37. Existing Calculator

If:

```text
order_calculators
```

already contains the Order:

```text
Return that Calculator
```

using its existing:

```text
template_version_id
```

Do not inspect whether a newer Version exists for replacement.

---

# 38. Missing Calculator

If no Calculator exists:

```text
Resolve Order
→ Order Type
→ Calculator Template
→ Current Published Template Version
```

---

# 39. No Template Assigned

If:

```text
Order Type.calculator_template_id = NULL
```

return a controlled state such as:

```text
CALCULATOR_NOT_CONFIGURED
```

Do not create an empty arbitrary Calculator.

---

# 40. Template Inactive

If the Order Type already references an inactive Template, historical/admin behavior must remain safe.

For creation of a new Order Calculator, use the documented Template eligibility rule.

Recommended:

```text
Do not create a new Calculator from an inactive Template.
```

Return a configuration error.

---

# 41. No Published Version

If Template exists but has no Published Version:

```text
CALCULATOR_NO_PUBLISHED_VERSION
```

No Order Calculator is created.

---

# 42. Current Published Version

Use the current Published Version defined by the Calculator Foundation.

Do not use:

```text
Draft
```

or:

```text
Retired
```

for new Calculator creation.

---

# 43. Lazy Creation

Order Calculator is created:

```text
when Calculator is first opened
```

rather than automatically for every new Order.

---

# 44. Why Lazy Creation

This prevents unnecessary Calculator rows for:

```text
Orders never priced through Calculator

Order Types without Calculator

Draft Orders still being organized
```

and freezes the Version only when Calculator actually becomes relevant.

---

# 45. Lazy Creation Transaction

Conceptually:

```text
Load Order

Check existing Calculator

Resolve Published Version

Create Calculator

Set audit fields

Save
```

This must be concurrency-safe.

---

# 46. Concurrent First Open

Two simultaneous first-open requests must not create two Order Calculators.

The database:

```text
UNIQUE(order_id)
```

provides final protection.

Application should handle the conflict gracefully and load the existing row.

---

# 47. Task 6 — GET Order Calculator

Implement:

```text
GET /api/orders/{orderId}/calculator
```

---

# 48. Permission

Requires:

```text
calculator.view
```

plus normal access to the Order where applicable.

---

# 49. GET Behavior

Conceptually:

```text
Authenticate

Authorize

Load Order

Find existing Calculator

If none:
    resolve Template
    resolve current Published Version
    lazily create Calculator

Load exact Template Version

Parse Definition

Load stored field values

Evaluate derived fields

Apply visibility permissions

Return runtime DTO
```

---

# 50. Runtime DTO

Conceptually contains:

```text
orderId

calculatorId

template:
    id
    name

templateVersion:
    id
    versionNumber

definition / render model

values

calculatedValues

sellingPrice

updatedAt
```

Only return data the User may view.

---

# 51. Template Definition Exposure

Frontend needs enough Template Definition information to render the Calculator.

Do not expose internal parser/AST structures.

Return a safe renderable Definition/DTO.

---

# 52. Published Version Identity

UI should be able to display:

```text
Template Name — v3
```

for troubleshooting and historical clarity.

---

# 53. Task 7 — Runtime Evaluation

Combine:

```text
Template Definition

Stored field values

Template defaults

User input values
```

into the Formula Engine.

---

# 54. Runtime Evaluation Sequence

Conceptually:

```text
Validate stored/input values
      ↓
Build runtime value dictionary
      ↓
Apply defaults
      ↓
Evaluate dependency graph
      ↓
Calculate derived fields
      ↓
Calculate designated Selling Price
```

---

# 55. Do Not Revalidate Published Structure Every Keystroke Excessively

Published Versions were validated before publication.

Runtime should still fail safely if corrupted data is encountered.

Do not assume database corruption is impossible.

---

# 56. Input Type Validation

Each field value must match its Template element type.

Examples:

```text
number_input
→ decimal-compatible value

checkbox
→ boolean

text_input
→ string

dropdown
→ approved option
```

---

# 57. Unknown Input Keys

A save request containing a field key not defined by the exact Template Version should be rejected or ignored according to one consistent safe rule.

Recommended:

```text
Reject unknown input keys.
```

This catches stale/manipulated frontend requests.

---

# 58. Calculated Field Input

Frontend must not directly set:

```text
calculated_field
```

values.

Backend calculates them.

---

# 59. Section and Label Inputs

These elements do not accept values.

---

# 60. Dropdown Validation

Submitted value must correspond to an allowed Template option.

---

# 61. Numeric Bounds

If Template defines:

```text
min

max
```

backend validates them.

---

# 62. Missing Required Value

If a required formula dependency is missing:

```text
Return controlled Calculator validation state
```

rather than storing an invalid final Selling Price.

---

# 63. Task 8 — Save Calculator

Implement:

```text
PATCH /api/orders/{orderId}/calculator
```

---

# 64. Edit Permission

Requires:

```text
calculator.edit
```

---

# 65. Save Request

Contains:

```text
fieldValues
```

and concurrency information such as:

```text
updatedAt
```

if using the approved optimistic concurrency strategy.

---

# 66. Never Accept Template Version from Normal Save

Normal User should not be able to submit:

```text
templateVersionId
```

and switch the historical version.

Backend uses the Order Calculator's existing relationship.

---

# 67. Never Accept Final Selling Price

Do not trust:

```text
sellingPrice
```

submitted by frontend as authoritative.

Backend calculates it.

---

# 68. Save Flow

```text
Authenticate

Authorize calculator.edit

Load Order Calculator

Verify concurrency state

Load exact Template Version

Validate incoming field values

Evaluate Calculator

Determine Selling Price

Begin transaction

Persist field_values

Update Calculator audit fields

Update last_calculated_at

Update Order selling_price

Update Order audit fields

Commit

Return authoritative runtime result
```

---

# 69. Atomic Save

These operations must be in one transaction:

```text
Save Calculator values

Update Order Selling Price
```

They must not diverge.

---

# 70. Save Failure

If Calculator evaluation fails:

```text
Do not update Selling Price
```

and do not partially save an authoritative invalid result.

---

# 71. Partial Draft Input

Calculator UX may need to persist incomplete inputs before a valid Selling Price exists.

Version 1 should distinguish:

```text
saving editable field state
```

from:

```text
successful authoritative calculation
```

---

# 72. Recommended Incomplete-State Behavior

Allow validly typed partial field values to be stored even when required inputs are still missing, but:

```text
Do not replace Order Selling Price with an invalid/partial calculation.
```

Once complete calculation succeeds, synchronize Selling Price.

This supports gradual data entry.

---

# 73. Initial Order Selling Price

Before a successful Calculator calculation:

```text
selling_price = 0
```

according to current Orders design.

---

# 74. Calculation Errors

Runtime DTO may return structured errors such as:

```text
MISSING_REQUIRED_VALUE

VALUE_OUT_OF_RANGE

CALCULATION_ERROR
```

with relevant field keys.

---

# 75. Task 9 — Selling Price Synchronization

The critical invariant is:

```text
After successful Calculator save:

orders.orders.selling_price
=
authoritative designated Selling Price result
```

---

# 76. Designated Output

Use:

```text
sellingPriceFieldKey
```

from the exact Template Definition.

Do not infer the final price from:

```text
label

position

last field
```

---

# 77. Decimal Handling

Order Selling Price uses exact decimal behavior.

Apply explicit rounding according to Template/formula logic.

Do not apply invisible arbitrary frontend rounding.

---

# 78. Negative Selling Price

Unless a legitimate future pricing requirement requires negative values:

```text
final Selling Price < 0
```

should normally be rejected.

---

# 79. Selling Price Audit

When Calculator changes Order Selling Price:

```text
orders.orders.updated_at

orders.orders.updated_by
```

must update.

---

# 80. Calculator Audit

Likewise update:

```text
order_calculators.updated_at

order_calculators.updated_by

last_calculated_at
```

on successful authoritative calculation.

---

# 81. Task 10 — Financial Visibility

Calculator fields may use:

```text
general

selling

cost
```

visibility scopes.

---

# 82. General Fields

Visible to Users with ordinary:

```text
calculator.view
```

subject to normal Order access.

---

# 83. Selling Fields

Selling-sensitive Calculator values require appropriate Selling Price access.

Use the approved permission relationship consistently.

---

# 84. Cost Fields

Cost-sensitive fields require:

```text
calculator.view_costs
```

once Cost permissions are active.

Before Cost Item implementation, the permission may still exist in preparation.

---

# 85. Backend Filtering

Protected Calculator data must not simply be hidden by React.

Backend must avoid returning unauthorized values.

---

# 86. Formula Dependency and Protected Fields

A User may be allowed to edit a general input that internally affects a protected calculated field.

Backend can use protected values internally without returning them.

---

# 87. Do Not Break Calculation Because of Visibility

Authorization controls:

```text
what the User can see
```

not:

```text
what the backend is allowed to calculate internally.
```

---

# 88. Task 11 — Optimistic Concurrency

Calculator editing is a high-risk concurrent-edit area.

Implement optimistic concurrency.

---

# 89. Initial Concurrency Mechanism

Use the existing:

```text
updated_at
```

value as the Version 1 concurrency comparison if this is reliable within the EF/PostgreSQL implementation.

Do not introduce an extra `row_version` column unless testing shows it is necessary and documentation is updated.

---

# 90. Save Request Concurrency

Frontend sends the Calculator's last known:

```text
updatedAt
```

---

# 91. Stale Save

If database `updated_at` no longer matches:

```text
409 Conflict
```

with stable code such as:

```text
CALCULATOR_CONCURRENCY_CONFLICT
```

---

# 92. Conflict Message

Example:

```text
This Calculator was changed by another User.

Reload the latest values before saving.
```

---

# 93. Do Not Auto-Merge Calculator Values

Version 1 should not attempt automatic field-level merge of simultaneous edits.

Require reload.

---

# 94. Frontend Conflict Handling

On conflict:

```text
Do not silently overwrite.

Show conflict message.

Offer Reload.
```

---

# 95. Task 12 — Calculator Reset

Calculator reset is an explicit high-impact operation.

---

# 96. Reset Use Cases

Reset may be needed when:

```text
Order Type changes after Calculator creation

Administrator intentionally wants fresh Calculator values

Template relationship needs explicit rebinding
```

---

# 97. Reset Endpoint

Conceptually:

```text
POST /api/orders/{orderId}/calculator/reset
```

---

# 98. Reset Permission

Requires at minimum:

```text
calculator.edit
```

and possibly stronger administrative permission if experience shows need.

Version 1 may use `calculator.edit`.

---

# 99. Reset Confirmation

Frontend should clearly warn:

```text
Reset Calculator?

Entered Calculator values will be cleared.
```

---

# 100. Reset Must Preserve Costs

When Cost Items exist:

```text
Calculator reset must not delete Cost Items.
```

This is a mandatory future-compatible rule.

---

# 101. Reset Field Values

Reset clears:

```text
field_values
```

to an empty/default state.

---

# 102. Reset Selling Price

Recommended current rule:

```text
orders.orders.selling_price = 0
```

after reset until a new valid Calculator result is saved.

---

# 103. Reset Cost Price

Do not change:

```text
cost_price
```

because Cost Price belongs to Cost Items.

---

# 104. Reset Version Binding

There are two distinct reset operations conceptually:

```text
A. Clear values but keep exact Template Version

B. Rebind to current published Template Version
```

Avoid making this ambiguous.

---

# 105. Version 1 Reset Behavior

For normal reset triggered by an Order Type/Template change:

```text
Remove/recreate or rebind Order Calculator
using the current Published Version
of the Order Type's assigned Template.
```

This must be explicit.

---

# 106. Simple Same-Version Reset

If User only wants to clear inputs without changing Template:

```text
keep template_version_id
clear field_values
selling_price = 0
```

This can be implemented as a separate behavior if needed.

---

# 107. Avoid Ambiguous Single Button

UI should distinguish actions if both exist:

```text
Clear Calculator Values
```

and:

```text
Reset Calculator to Current Template
```

Only implement both if real workflow needs both.

---

# 108. Minimum Required Reset

The mandatory operation for Version 1 is the explicit reset required when Order Type changes after a Calculator exists.

---

# 109. Reset Transaction

Conceptually:

```text
Validate new Order Type/Template

Resolve current Published Version

Begin transaction

Update Order Type if part of workflow

Rebind/recreate Calculator

Clear field values

Set Selling Price = 0

Update audits

Commit
```

---

# 110. Historical Meaning of Reset

Reset is a deliberate business action that changes which Template Version the Order uses.

It must never happen silently.

---

# 111. Task 13 — Order Type Change Protection

Before Calculator exists:

```text
Order Type may change normally
```

subject to Orders rules.

---

# 112. After Calculator Exists

Normal Order PATCH attempting to change:

```text
orderTypeId
```

must be rejected with a specific error such as:

```text
ORDER_TYPE_CHANGE_REQUIRES_CALCULATOR_RESET
```

---

# 113. Explicit Change Workflow

A dedicated operation should perform:

```text
Order Type change
+
Calculator reset/rebinding
```

transactionally.

---

# 114. Conceptual Endpoint

Possible:

```text
POST /api/orders/{orderId}/change-order-type
```

Request:

```text
orderTypeId

resetCalculator: true
```

or another explicit API design.

Keep the command unmistakable.

---

# 115. New Order Type Validation

Target Order Type must:

```text
exist

be active
```

---

# 116. Target Calculator Requirement

If the target Order Type has no Calculator:

```text
Order Type can change

existing Order Calculator is removed/reset deliberately

Selling Price resets to 0
```

Costs remain.

---

# 117. Target Template Without Published Version

If new Order Type is linked to a Template without a Published Version:

```text
do not silently bind an invalid Calculator.
```

The change workflow should either:

```text
reject
```

or allow type change with no Calculator only if explicitly designed.

Recommended:

```text
Reject if the target type is configured to use a Template that has no Published Version.
```

---

# 118. No Silent Version Upgrade

Opening an existing Calculator must never detect a newer Template and automatically upgrade it.

---

# 119. Upgrade Visibility

UI may optionally display:

```text
This Order uses Template v2.
Current Template version is v4.
```

later.

Do not add automatic migration.

---

# 120. Task 14 — Frontend Dynamic Renderer

Order Workspace Calculator section consumes the exact Template Version Definition.

---

# 121. Renderer Principle

Use one reusable rendering engine.

Do not manually build a separate React Calculator for every Order Type.

---

# 122. Element Mapping

Conceptually:

```text
label
→ text/display component

number_input
→ numeric input

text_input
→ text input

dropdown
→ select/autocomplete

checkbox
→ checkbox

calculated_field
→ read-only calculated value

section
→ layout section

table
→ controlled table renderer
```

---

# 123. Stable Keys

Frontend state uses:

```text
field key
```

not label.

---

# 124. Display Order

Render elements in the order defined by Template Definition.

---

# 125. Section Rendering

Sections organize visual groups.

Example:

```text
Dimensions

Materials

Finishing

Pricing
```

---

# 126. Calculated Fields

Display as read-only.

Frontend may calculate preview values immediately.

---

# 127. Backend Recalculation

After Save, replace frontend preview with backend authoritative results.

---

# 128. Frontend Formula Engine

If frontend requires immediate recalculation, it must implement the same controlled semantics as backend or use a compatible shared specification.

Do not allow frontend behavior to diverge from backend.

---

# 129. Simpler Alternative

The first implementation may recalculate through backend calls rather than duplicating the full engine in TypeScript if responsiveness remains acceptable.

Choose the simplest reliable approach.

---

# 130. No Formula Editing in Order Workspace

Normal Order Calculator Users edit:

```text
values
```

not:

```text
Template formulas
```

Template formulas belong to Administration.

---

# 131. Task 15 — Calculator Editing UX

Inside Order Workspace:

```text
Calculator
```

should show:

```text
Template name/version

Input fields

Calculated fields

Selling Price

Save state

Validation messages
```

---

# 132. Initial Load State

Handle:

```text
Loading Calculator...
```

---

# 133. No Calculator Configured State

If Order Type has no Template:

```text
No Calculator is configured for this Order Type.
```

---

# 134. No Published Version State

If Template has no Published Version:

```text
This Calculator Template has no published version.
```

This is an administrative configuration issue.

---

# 135. Saved State

Provide clear feedback:

```text
Saved
```

or equivalent.

---

# 136. Unsaved State

When User changes inputs:

```text
Unsaved changes
```

may be shown.

---

# 137. Saving State

Prevent duplicate Save actions.

---

# 138. Save Error

Display the relevant validation/calculation error without destroying current input state.

---

# 139. Field-Level Errors

Where possible, show errors next to corresponding field.

---

# 140. General Calculation Errors

Show a Calculator-level error area for problems that cannot be assigned to one field.

---

# 141. Template Version Display

Show secondary information such as:

```text
UV Printing Calculator · v3
```

This helps troubleshooting.

---

# 142. Financial Visibility UI

Hide Selling/Cost-scoped fields when User lacks corresponding permissions.

Again, backend must also filter them.

---

# 143. Selling Price Display

The final Selling Price should be clearly visible to authorized Users.

---

# 144. Selling Price Formatting

Use shared money formatting.

Do not embed currency formatting inside the Formula Engine.

---

# 145. Auto-Save Decision

Version 1 should start with:

```text
Explicit Save
```

for Order Calculator.

This reduces concurrency and debugging complexity.

---

# 146. Why Explicit Save

Calculator values affect:

```text
Selling Price

Reports

Project totals
```

A deliberate Save action is safer initially.

---

# 147. Future Auto-Save

Auto-save may be considered later if real usage shows it improves workflow.

---

# 148. Task 16 — Error Codes

Useful stable errors include:

```text
CALCULATOR_NOT_CONFIGURED

CALCULATOR_NO_PUBLISHED_VERSION

CALCULATOR_INVALID_INPUT

CALCULATOR_CALCULATION_ERROR

CALCULATOR_CONCURRENCY_CONFLICT

ORDER_TYPE_CHANGE_REQUIRES_CALCULATOR_RESET

TEMPLATE_VERSION_NOT_AVAILABLE
```

---

# 149. Do Not Expose Parser Internals

User-facing errors should be understandable.

Do not expose:

```text
AST node failures

internal class names

stack traces
```

---

# 150. Task 17 — Persistence Tests

Required:

```text
Create Order Calculator

One per Order

Exact Template Version stored

Field Values JSONB round-trip

Audit fields correct
```

---

# 151. Lazy Creation Test

Scenario:

```text
Order exists

No Calculator row

GET Calculator

→ Calculator row created
```

---

# 152. Reopen Test

Second GET:

```text
does not create second row
```

---

# 153. Concurrent Lazy Creation Test

Two simultaneous GET/open operations:

```text
one Calculator row only
```

---

# 154. No Template Test

Order Type has no Template:

```text
GET Calculator
→ controlled not-configured result

No Calculator row created
```

---

# 155. No Published Version Test

Template only has Draft:

```text
GET Calculator
→ controlled configuration error

No Order Calculator created
```

---

# 156. Historical Version Test — Mandatory

Scenario:

```text
Publish Template v1

Create Order A

Open Calculator A
→ binds v1

Publish v2

Reopen Calculator A
→ still v1

Create Order B

Open Calculator B
→ binds v2
```

This test is mandatory.

---

# 157. Retired Version Test

After v1 becomes Retired:

```text
Order A using v1
```

must continue to calculate successfully.

---

# 158. Save Input Test

Submit valid values.

Verify:

```text
field_values persisted

calculated values correct

last_calculated_at updated
```

---

# 159. Selling Price Synchronization Test

After valid calculation:

```text
Order.selling_price
=
Calculator final Selling result
```

---

# 160. Transaction Failure Test

Simulate failure between:

```text
Calculator save

Order Selling Price update
```

Result:

```text
neither should commit inconsistently
```

---

# 161. Frontend Manipulation Test

Submit fake:

```text
sellingPrice = 1
```

with Calculator inputs producing:

```text
5000
```

Backend must store:

```text
5000
```

not `1`.

Ideally normal request does not even include `sellingPrice`.

---

# 162. Calculated Field Manipulation Test

Submit value for a calculated-only field.

Backend must ignore/reject manipulation according to the approved safe rule.

Recommended:

```text
reject
```

---

# 163. Unknown Field Test

Submit:

```text
"secret_discount": 0.01
```

when Template has no such key.

Reject.

---

# 164. Wrong Type Test

Submit text into numeric field.

Reject with field-level validation.

---

# 165. Dropdown Test

Submit value not present in allowed options.

Reject.

---

# 166. Partial Input Test

Save valid partial values.

Verify:

```text
field state stored
```

while:

```text
Selling Price remains previous valid value or 0 according to state
```

For a Calculator that has never completed successfully, it remains:

```text
0
```

---

# 167. Existing Valid Price with Later Incomplete Edit

To avoid ambiguity, recommended behavior is:

```text
If User saves an incomplete Calculator state after a previously valid calculation,
Selling Price resets to 0.
```

This prevents stale Selling Price from appearing valid when the current Calculator inputs no longer produce a valid result.

---

# 168. Incomplete State Rule

Therefore after any persisted Calculator state that cannot produce a valid authoritative Selling Price:

```text
orders.orders.selling_price = 0
```

This keeps current Order state consistent.

---

# 169. Validation Transaction Test

Save invalid/incomplete inputs according to the allowed partial-state behavior and verify Order price follows the rule above.

---

# 170. Concurrency Test

User A loads Calculator.

User B loads Calculator.

User A saves.

User B saves stale version.

Expected:

```text
409 Conflict
```

---

# 171. Concurrency Preservation Test

User B conflict must not overwrite User A's values.

---

# 172. Reset Test

After Calculator contains values:

```text
Reset

field_values empty/default

selling_price = 0
```

---

# 173. Reset Version Test

If reset/rebind targets newer Published Version:

```text
template_version_id
```

changes only through explicit reset workflow.

---

# 174. Cost Preservation Reset Test

Once Costs are implemented:

```text
Reset Calculator
→ Cost Items unchanged
→ Cost Price unchanged
```

This test must be added in the Costs phase.

---

# 175. Order Type Change Test Before Calculator

Draft Order without Calculator:

```text
Order Type change succeeds
```

---

# 176. Order Type Change Test After Calculator

Normal PATCH:

```text
→ rejected
```

with explicit reset-required error.

---

# 177. Explicit Order Type Change Test

Using reset workflow:

```text
Order Type changes

new Template Version resolved

Calculator values cleared

Selling Price = 0
```

---

# 178. New Type Without Template Test

Explicit type-change workflow to Order Type without Calculator:

```text
Order Type changes

existing Calculator removed/reset as designed

Selling Price = 0
```

No fake Calculator created.

---

# 179. Permission Tests

Test:

```text
calculator.view

calculator.edit
```

independently.

---

# 180. Financial Visibility Tests

User without Selling permission:

```text
protected Selling fields/results absent
```

even when Calculator internally calculates them.

---

# 181. Cost Visibility Tests

When Cost-scoped fields exist:

```text
User without calculator.view_costs
```

must not receive protected values.

---

# 182. API Security Tests

Direct manipulated requests must not bypass:

```text
Formula rules

Template Version binding

Financial visibility

Order relationship
```

---

# 183. Parent Validation Test

For:

```text
/api/orders/{orderId}/calculator
```

backend must operate only on Calculator belonging to that Order.

---

# 184. Migration Test

Apply full migration chain through:

```text
Order Calculator
```

on clean PostgreSQL.

Verify all cross-schema relationships.

---

# 185. Query Efficiency

Loading Calculator should use a small predictable number of queries.

Do not cause one query per Template element.

---

# 186. JSONB Size

Typical `field_values` should remain small.

Do not store:

```text
images

files

full audit history
```

inside it.

---

# 187. Calculator Logging

Do not log every field value on every save.

Useful logging includes:

```text
Unexpected calculation failures

Concurrency conflicts if diagnostically useful

Template corruption/errors
```

---

# 188. Sensitive Calculator Logging

Cost/selling values should not be dumped into technical logs unnecessarily.

---

# 189. Order Calculator Commits

Recommended:

```text
feat(calculator): add order calculator schema

feat(calculator): add lazy calculator creation

feat(calculator): add runtime evaluation

feat(calculator): add order calculator persistence

feat(calculator): synchronize selling price

feat(calculator): add optimistic concurrency

feat(calculator): add calculator reset workflow

test(calculator): add historical order calculator tests

feat(frontend): add dynamic order calculator renderer
```

---

# 190. First AI Coding Task

Recommended:

```text
Read:
- AI_RULES.md
- docs/14_Orders_Module.md
- docs/15_Calculator_Module.md
- docs/17_Database_Schema_Overview.md
- docs/20_Testing_Strategy.md
- docs/24_Backend_Architecture.md
- docs/25_Development_Workflow_for_AI.md
- docs/32_Calculator_Foundation_Implementation_Plan.md
- docs/33_Order_Calculator_Implementation_Plan.md

Task:
Implement only Order Calculator persistence.

Create:
- OrderCalculator entity
- EF Core configuration
- calculator.order_calculators
- unique order_id
- exact template_version_id FK
- field_values JSONB
- audit fields
- last_calculated_at
- migration
- persistence integration tests

Do not implement:
- Calculator API
- frontend renderer
- Cost Items
- reset workflow
- Selling Price synchronization yet

Before finishing:
- build
- apply migration
- test one-Calculator-per-Order
- test JSONB round trip
- review migration
```

---

# 191. Second AI Coding Task

```text
Implement Order Calculator lazy creation resolver.

Requirements:
- use Order Type's assigned Template
- use current Published Version
- exact version stored
- existing Calculator never auto-upgraded
- concurrency-safe first creation
- clear errors for no Template/no Published Version
```

---

# 192. Third AI Coding Task

```text
Implement GET /api/orders/{orderId}/calculator.

Requirements:
- calculator.view
- load exact Template Version
- merge stored field values with Definition
- evaluate calculated fields
- return safe runtime DTO
- apply financial visibility
```

---

# 193. Fourth AI Coding Task

```text
Implement Order Calculator save.

Requirements:
- calculator.edit
- validate field types
- reject unknown keys
- reject submitted calculated-field values
- backend formula evaluation
- persist field_values
- synchronize Order Selling Price
- atomic transaction
- audit fields
- tests
```

---

# 194. Fifth AI Coding Task

```text
Add optimistic Calculator concurrency.

Use existing updated_at strategy unless implementation requires a documented schema change.

Stale saves must return 409 and never overwrite newer data.
```

---

# 195. Sixth AI Coding Task

```text
Implement explicit Calculator reset and Order Type change protection.

Requirements:
- no silent reset
- no silent Template upgrade
- values cleared
- Selling Price reset to 0
- future Cost Items must remain untouched
```

---

# 196. Seventh AI Coding Task

```text
Implement dynamic Calculator renderer in Order Workspace.

Use the exact Template Version Definition.

Support:
- Label
- Number Input
- Text Input
- Dropdown
- Checkbox
- Calculated Field
- Section

Add Table only to the level already supported by the Template Definition engine.

Use explicit Save.
```

---

# 197. Eighth AI Coding Task

```text
Add Calculator error, save-state, and concurrency-conflict UX.

Do not implement auto-save.
```

---

# 198. Completion Gate

Do not begin Cost Items implementation until:

```text
Order Calculator table works

One Calculator per Order enforced

Lazy creation works

Published Version resolution works

Exact historical version preserved

GET Calculator works

Dynamic rendering works

Input validation works

Formula evaluation works

Field values persist

Selling Price synchronizes

Transactions protect consistency

Concurrency conflicts work

Reset is explicit

Order Type changes cannot silently destroy Calculator

Financial visibility works

Historical version tests pass
```

---

# 199. Mandatory Historical Test Before Completion

This exact behavior must be proven:

```text
Template v1 published
        ↓
Order A opens Calculator
        ↓
Order A locked to v1
        ↓
Template v2 published
        ↓
Order A remains on v1
        ↓
Order B opens Calculator
        ↓
Order B uses v2
```

If this test fails, Order Calculator is not complete.

---

# 200. No Cost Item Logic Yet

Do not add Cost rows into:

```text
field_values
```

Costs have their own relational table.

---

# 201. No Supplier Data in Calculator JSON

Future Cost Item Supplier remains relational row text data.

---

# 202. No Cost Price Formula Authority

Calculator formulas must not replace:

```text
Order Cost Price
```

as the authoritative Cost total.

That comes from Cost Items.

---

# 203. No Template Editing From Order

Order Calculator cannot modify:

```text
Template Definition

Formula

Template Version
```

---

# 204. No Silent Repair

If an old Published Template Version is invalid because of database corruption:

```text
fail safely
```

Do not silently substitute the latest Version.

---

# 205. No Auto-Migration

Do not automatically migrate old `field_values` to a new Template Version.

Any future migration must be an explicit operation.

---

# 206. No Shared Mutable Runtime State

Each Order Calculator stores its own field values.

Changing Order A must not affect Order B even when both use the same Template Version.

---

# 207. Order Calculator vs Template

Keep distinction clear:

```text
Template Version
=
Calculation structure
```

```text
Order Calculator
=
One Order's values using that structure
```

---

# 208. Stored Selling Price Meaning

The stored Order Selling Price represents the current authoritative result of that Order's Calculator state.

It allows Reports to work without recalculating every historical Template on every query.

---

# 209. Historical Price Stability

Because the Calculator remains tied to an immutable Template Version:

```text
future Template edits
```

cannot silently alter historical Order pricing.

---

# 210. Future Manual Price Override

Version 1 does not introduce a manual Selling Price override mechanism.

If later required, it must be explicitly designed with:

```text
permission

reason

audit behavior

Calculator relationship
```

Do not add it casually.

---

# 211. Future Version Migration

A future feature may support:

```text
Migrate Order Calculator from v2 to v4
```

but it must be explicit and audited.

It is not Version 1 behavior.

---

# 212. Future Calculator Comparison

Administration may later compare:

```text
Order's historical Version

Current Template Version
```

This is optional.

---

# 213. Future Auto-Save

May be added after real usage shows a need.

It must preserve:

```text
concurrency protection

transaction safety

clear save state
```

---

# 214. Future Calculation Preview

Frontend may later calculate every change instantly if a shared/compatible TypeScript evaluator proves worthwhile.

Backend remains final authority.

---

# 215. Simplicity Rule

Before adding Order Calculator behavior, ask:

```text
Does this help calculate and preserve
the current Order's price reliably?
```

If not, defer it.

---

# 216. Final Order Calculator Principle

The Order Calculator must answer:

```text
Which exact pricing rules apply to this Order?

What values did the User enter?

What values were calculated?

What is the authoritative Selling Price?

Can this historical Order still be reproduced after Templates change?
```

The central rule is:

```text
Template Versions define the rules.

Order Calculators preserve the values.

Orders preserve the business price.
```

---

**End of Document**