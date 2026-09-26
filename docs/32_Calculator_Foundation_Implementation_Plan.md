# Lithograph ERP

**Document:** 32_Calculator_Foundation_Implementation_Plan.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Calculator Foundation

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

---

# 1. Purpose

This document defines the first implementation phase of the Calculator module.

This phase builds:

```text id="8z84kg"
Calculator Templates

Template Versions

Template Definition format

Stable Field Keys

Formula Parser

Formula Evaluator

Dependency Graph

Formula Validation

Circular Reference Detection

Template Validation

Template Publication

Order Type → Calculator Template assignment
```

This phase does **not** yet build the final visual Template Designer or Order Calculator editing experience.

---

# 2. Main Goal

After this phase, the backend must be capable of:

```text id="wjnm9k"
Creating Calculator Templates

Creating Draft Template Versions

Editing Draft Definitions

Validating Templates

Validating Formulas

Detecting Formula dependencies

Rejecting circular references

Publishing immutable Template Versions

Creating later Draft Versions

Assigning Templates to Order Types
```

---

# 3. Why Foundation Comes First

The Calculator UI depends on a reliable backend model.

Do not build a sophisticated visual designer before the system can safely answer:

```text id="1zg89m"
Is this Template valid?

Are the field keys valid?

Are the formulas valid?

Which fields depend on which?

Is there a circular reference?

Can this version be published?
```

---

# 4. Calculator Module Boundary

Calculator owns:

```text id="ox410r"
calculator.templates

calculator.template_versions
```

Later Calculator phases also own:

```text id="cgz31h"
calculator.order_calculators

calculator.cost_items
```

Those latter tables are not the primary focus of this foundation phase.

---

# 5. Orders Integration

Orders owns:

```text id="n0q1l3"
orders.order_types
```

Order Type may reference:

```text id="4sn1ct"
calculator.templates.id
```

through:

```text id="n7ysno"
calculator_template_id
```

---

# 6. Template vs Template Version

These are separate concepts.

```text id="ep533z"
Template
=
long-lived Calculator identity
```

```text id="60pfga"
Template Version
=
specific immutable definition after publication
```

---

# 7. Example

```text id="w3dz3j"
Template:
UV Printing Calculator

Versions:
v1 Published
v2 Published
v3 Draft
```

The Template remains the same logical Calculator.

Each Version contains a different definition.

---

# 8. Historical Rule

Published Template Versions are immutable.

Existing Orders must remain linked to the exact version originally used.

This rule is fundamental.

---

# 9. Recommended Implementation Sequence

```text id="kysb7i"
Task 1
Calculator domain types

Task 2
Template entity

Task 3
Template Version entity

Task 4
Template Definition contract

Task 5
Field validation

Task 6
Formula tokenizer/parser

Task 7
Formula expression model

Task 8
Formula evaluator

Task 9
Approved function library

Task 10
Dependency graph

Task 11
Circular-reference detection

Task 12
Template-level validation

Task 13
EF Core configuration

Task 14
Calculator foundation migration

Task 15
Template permissions

Task 16
Template CRUD backend

Task 17
Template Version backend

Task 18
Validate endpoint

Task 19
Publish endpoint

Task 20
Order Type assignment

Task 21
Basic Administration frontend

Task 22
Tests

Task 23
Security/performance review
```

---

# 10. Task 1 — Calculator Domain Types

Define stable concepts for:

```text id="igjz2l"
TemplateVersionStatus

CalculatorElementType

FieldVisibilityScope
```

---

# 11. Template Version Status

Supported values:

```text id="6v32fj"
Draft

Published

Retired
```

Recommended machine values:

```text id="z1wrtq"
draft

published

retired
```

---

# 12. Draft

Draft Version:

```text id="6yqg22"
Editable

Validatable

Not used automatically for new Orders
```

---

# 13. Published

Published Version:

```text id="cyvnaq"
Immutable

Validated

Available for Order Calculators
```

---

# 14. Retired

Retired Version:

```text id="mbfwjt"
Immutable

Historical

Not selected for new Order Calculators
```

Existing Order Calculators may continue referencing it.

---

# 15. Element Types

Initial supported element types:

```text id="cpsbu4"
Label

Number Input

Text Input

Dropdown

Checkbox

Calculated Field

Table

Section
```

---

# 16. Element Machine Values

Recommended:

```text id="8yqy1d"
label

number_input

text_input

dropdown

checkbox

calculated_field

table

section
```

---

# 17. Visibility Scopes

Initial field visibility scopes:

```text id="tnjg06"
general

selling

cost
```

These help backend/frontend protect financial information.

---

# 18. Task 2 — Calculator Template Entity

Create:

```text id="ho81u9"
CalculatorTemplate
```

mapped to:

```text id="z14z01"
calculator.templates
```

---

# 19. Template Fields

Core fields:

```text id="bvjsrh"
id

name

description

is_active

created_at

created_by

updated_at

updated_by
```

---

# 20. Template Name

Required.

Recommended maximum length:

```text id="b6hq9e"
200
```

---

# 21. Template Name Uniqueness

Template names should be unique case-insensitively.

---

# 22. Template Active State

`is_active` controls whether the Template may be assigned to new Order Types.

Historical versions remain valid when Template is inactive.

---

# 23. No Template Definition on Template Row

Do not store the active calculation definition directly on:

```text id="w4q91d"
calculator.templates
```

Definitions belong to:

```text id="sk8fr4"
calculator.template_versions
```

---

# 24. Task 3 — Template Version Entity

Create:

```text id="mwy5q7"
CalculatorTemplateVersion
```

mapped to:

```text id="h45pbd"
calculator.template_versions
```

---

# 25. Template Version Fields

Core fields:

```text id="j10nwb"
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

---

# 26. Version Number

Version number is an integer:

```text id="d66w3c"
1
2
3
4
...
```

---

# 27. Version Uniqueness

Enforce:

```text id="f62yc5"
UNIQUE(template_id, version_number)
```

---

# 28. Version Number Allocation

New Version should use the next version number for that Template.

Allocation must be concurrency-safe.

Do not simply:

```text id="6gmn84"
SELECT MAX(version_number) + 1
```

without transactional protection.

---

# 29. One Draft Recommendation

Version 1 should normally allow:

```text id="bqyblg"
maximum one active Draft Version per Template
```

This keeps editing behavior simple.

---

# 30. One Draft Database Protection

Where practical, enforce using a partial unique index:

```text id="ifhka1"
UNIQUE(template_id)
WHERE status = 'draft'
```

---

# 31. Definition Storage

Template Version Definition is stored as:

```text id="0yfcg5"
jsonb
```

---

# 32. Why JSONB Is Appropriate

Template layout/calculation definition is genuinely flexible.

Examples include:

```text id="4o6d4l"
Different field counts

Different field types

Dropdown options

Formula definitions

Sections

Table structures
```

Using JSONB here is intentional.

---

# 33. Normal Relationships Stay Relational

Do not use JSONB for:

```text id="dwfgfj"
Template identity

Order relationships

Users

Costs

Order Types
```

Those remain relational.

---

# 34. Task 4 — Template Definition Contract

Define a strict internal JSON contract.

A Template Definition conceptually contains:

```text id="za8r6q"
schemaVersion

elements
```

---

# 35. Definition Example

Conceptual example:

```text id="k56a3h"
{
  "schemaVersion": 1,
  "elements": [
    {
      "type": "number_input",
      "key": "width_mm",
      "label": "Width",
      "visibility": "general"
    },
    {
      "type": "number_input",
      "key": "height_mm",
      "label": "Height",
      "visibility": "general"
    },
    {
      "type": "calculated_field",
      "key": "area_m2",
      "label": "Area",
      "formula": "(width_mm * height_mm) / 1000000",
      "visibility": "general"
    }
  ]
}
```

Exact DTO structure may evolve, but must remain versioned and validated.

---

# 36. Definition Schema Version

Include:

```text id="dc3in7"
schemaVersion
```

inside Definition.

Initial value:

```text id="rmmv8u"
1
```

---

# 37. Why Definition Schema Version Exists

Future Calculator definition structure may evolve.

A schema version allows controlled parsing/migration rather than guessing old JSON structure.

---

# 38. Stable Field Keys

Every input/calculated field that participates in formulas needs a stable:

```text id="xok7md"
key
```

---

# 39. Field Key Format

Use:

```text id="wpr7vm"
lowercase snake_case
```

Examples:

```text id="tf2zc9"
width_mm

height_mm

quantity

material_price

selling_price
```

---

# 40. Field Key Rules

Recommended validation:

```text id="44hn7y"
Starts with lowercase letter

Contains lowercase letters

May contain digits

May contain underscore

No spaces

No punctuation
```

Conceptually regex:

```text id="yfof97"
^[a-z][a-z0-9_]*$
```

---

# 41. Field Keys Must Be Unique

Within one Template Version:

```text id="hs1hmb"
key
```

must be unique.

---

# 42. Labels Are Not Formula Identity

Formula references:

```text id="2unlqb"
width_mm
```

not:

```text id="zkms2y"
Width (mm)
```

Labels may change safely.

---

# 43. Reserved Keys

Reserve system-owned keys where necessary.

Do not allow user fields to conflict with internal Calculator identifiers.

Keep reserved list small and documented.

---

# 44. Element IDs

Each Definition element should also have a stable internal element ID if useful for Designer editing.

This can be a UUID/string independent of formula key.

---

# 45. Keyless Elements

Elements such as:

```text id="uqvygi"
Label

Section
```

do not necessarily require formula field keys.

---

# 46. Number Input

Supports numeric user input.

Potential properties:

```text id="1r4xol"
key

label

defaultValue?

min?

max?

decimalPlaces?

visibility
```

Keep first version minimal.

---

# 47. Text Input

Supports text values.

Text fields do not participate in numeric formulas unless future functions explicitly support them.

---

# 48. Dropdown

Contains controlled options.

Each option should have:

```text id="zcx033"
value

label
```

Formula usage may use numeric option values only if explicitly defined.

---

# 49. Checkbox

Represents boolean:

```text id="5zkuwp"
true

false
```

---

# 50. Calculated Field

Contains:

```text id="gc43ev"
key

label

formula

visibility
```

It is not directly editable by normal Order users.

---

# 51. Section

Provides layout grouping.

It does not store a Calculator value.

---

# 52. Label

Provides instructional/display text only.

---

# 53. Table

Table is a supported structural element, but Version 1 implementation should keep table formulas controlled.

Do not attempt to reproduce all Excel table behavior.

---

# 54. Table Foundation Scope

Initial Table support may define:

```text id="xrv2wy"
columns

rows

cell field types

simple row calculations
```

Only implement functionality required by real Lithograph Calculator templates.

---

# 55. Avoid Overbuilding Table Logic

If no first Calculator requires advanced Table behavior, implement minimal schema support and defer advanced formulas.

---

# 56. Task 5 — Definition Validation

Create a backend Template Definition validator.

It must validate:

```text id="kqw3jf"
Definition parseability

Schema version

Supported element types

Required properties

Field key format

Unique keys

Formula presence where required

Visibility scope validity

Dropdown configuration
```

---

# 57. Validation Result

Return structured errors.

Conceptually:

```text id="ai386f"
isValid

errors[]
```

Each error may include:

```text id="xzi6ye"
code

message

elementId

fieldKey
```

where applicable.

---

# 58. Do Not Throw for Normal Validation Errors

An invalid user-created formula/template is expected behavior.

Return validation results rather than treating every invalid Template as an unexpected server exception.

---

# 59. Task 6 — Formula Tokenizer and Parser

Build or adopt a safe controlled expression parser.

---

# 60. Formula Syntax

Initial syntax should support:

```text id="ij3btw"
Numbers

Field references

Parentheses

Approved operators

Approved functions
```

---

# 61. Approved Arithmetic Operators

Support:

```text id="wrn4lp"
+

-

*

/

%
```

---

# 62. Comparison Operators

Support controlled comparisons such as:

```text id="76as0x"
=

!=

>

>=

<

<=
```

Exact syntax should be documented consistently.

---

# 63. Boolean Logic

Boolean functions/operators may support:

```text id="dti7h9"
AND

OR

NOT
```

---

# 64. No Arbitrary Execution

Formula text must never be passed to:

```text id="1sfqjh"
JavaScript eval

C# compiler

Roslyn scripting

SQL execution

Python

Shell
```

---

# 65. Formula Parser Output

Parser should produce a controlled internal expression model / AST.

Conceptually:

```text id="dvf95x"
BinaryExpression

FunctionCall

FieldReference

NumberLiteral

BooleanLiteral
```

---

# 66. Parser Errors

Examples:

```text id="br9bm9"
Unexpected token

Missing closing parenthesis

Unknown function syntax
```

must produce controlled validation errors.

---

# 67. Task 7 — Formula Expression Model

Keep the AST internal to Calculator.

Do not expose implementation details through API unnecessarily.

---

# 68. Immutable Parsed Expressions

Parsed formulas may be treated as immutable expression structures during validation/evaluation.

---

# 69. Task 8 — Formula Evaluator

Build a deterministic evaluator for the approved AST.

---

# 70. Evaluator Input

Evaluator receives only:

```text id="gu4jo7"
Parsed expression

Known field values

Approved function registry
```

---

# 71. Evaluator Output

Returns:

```text id="3126h4"
Decimal

Boolean
```

or another explicitly supported Calculator value type.

---

# 72. Decimal Arithmetic

Numeric calculation must use decimal-safe arithmetic.

Do not use binary floating point for money/precision-sensitive calculations.

---

# 73. Division by Zero

Division by zero must produce a controlled calculation error.

Do not crash the application.

---

# 74. Null/Empty Inputs

Behavior for missing values must be explicit.

Recommended:

```text id="71dzkr"
Required numeric formula dependency missing
→ calculation error / incomplete result
```

Do not silently treat every missing value as zero unless the Template explicitly defines a default.

---

# 75. Task 9 — Approved Function Library

Start with a limited function set.

---

# 76. Approved Numeric Functions

Initial candidates:

```text id="y48soy"
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
```

---

# 77. Approved Logical Functions

```text id="y7w1f3"
IF

AND

OR

NOT
```

---

# 78. Function Limit Principle

Target approximately:

```text id="hy8w81"
30 functions maximum
```

unless real templates demonstrate a need for more.

---

# 79. No Spreadsheet Compatibility Goal

Lithograph Calculator is not required to support every Excel function.

Do not attempt formula compatibility with:

```text id="s9hthp"
VLOOKUP

XLOOKUP

Macros

VBA

External workbook references
```

unless future needs justify specific additions.

---

# 80. Function Registration

Use one controlled registry/catalog.

Each function should define:

```text id="e7ux5o"
Name

Allowed argument count/range

Evaluator
```

---

# 81. Function Names

Use case-insensitive formula function recognition if convenient.

Store/normalize consistently.

---

# 82. Unknown Function

Formula using unsupported function:

```text id="vj8dnw"
FOOBAR(...)
```

must be rejected during validation.

---

# 83. Function Tests

Every approved function requires tests covering:

```text id="fgpj8p"
Normal case

Boundary cases

Invalid arguments
```

where applicable.

---

# 84. Task 10 — Dependency Graph

Calculated Fields depend on referenced fields.

Example:

```text id="zj9dej"
area_m2
=
width_mm * height_mm
```

Dependencies:

```text id="o7uxyq"
area_m2
→ width_mm
→ height_mm
```

---

# 85. Dependency Extraction

The Formula parser should identify FieldReference nodes.

Use these to build dependency relationships.

---

# 86. Dependency Graph Purpose

The graph allows Calculator to determine:

```text id="6l9ql0"
Evaluation order

Unknown references

Circular references

Affected downstream fields
```

---

# 87. Unknown Field Reference

Example:

```text id="laz2el"
area_m2 = width_mm * unknown_height
```

must be rejected if:

```text id="m5xlc1"
unknown_height
```

does not exist.

---

# 88. Self Reference

Example:

```text id="yqx7yr"
total = total + 1
```

must be rejected.

---

# 89. Task 11 — Circular Reference Detection

Detect both direct and indirect cycles.

---

# 90. Direct Cycle

```text id="pyga3i"
a = a + 1
```

invalid.

---

# 91. Two-Field Cycle

```text id="1jkf39"
a = b + 1

b = a + 1
```

invalid.

---

# 92. Multi-Field Cycle

```text id="7icph3"
a → b → c → a
```

invalid.

---

# 93. Valid Dependency Chain

```text id="l68wmf"
a → b → c → d
```

without cycle is valid.

---

# 94. Topological Evaluation Order

For valid graph, calculate fields in dependency order.

Example:

```text id="0r9prz"
width
height
↓
area
↓
material_cost
↓
selling_price
```

---

# 95. Error Reporting

Circular-reference error should identify useful involved fields.

Example:

```text id="rtacns"
Circular dependency detected:
material_cost → selling_price → material_cost
```

---

# 96. Task 12 — Template-Level Validation

A Draft must pass all required validation before publication.

---

# 97. Publication Validation Includes

```text id="52t57p"
Definition schema valid

Element types valid

Field keys valid

Field keys unique

Formula syntax valid

Functions approved

References valid

No circular dependencies

Required outputs valid
```

---

# 98. Required Selling Output

A Template intended to calculate Selling Price needs one designated Selling Price result.

The Definition should identify which Calculated Field is the authoritative selling output.

---

# 99. Selling Result Designation

Prefer explicit Definition metadata.

Conceptually:

```text id="v0c1at"
sellingPriceFieldKey:
"final_price"
```

Do not infer Selling Price by label text.

---

# 100. Selling Output Validation

If Template is intended to produce Selling Price:

```text id="qnpfso"
designated field must exist

must be numeric/calculated

must have valid formula
```

---

# 101. Cost Output

Order Cost Price comes from Cost Items.

Do not require a Calculator Template formula to be the authoritative Cost Price.

---

# 102. Cost Fields in Template

Template may still contain fields/calculations with:

```text id="wz1s3n"
visibility = cost
```

for internal pricing logic.

But final Order Cost Price remains Cost Item total in the later phase.

---

# 103. Publication Must Be All-or-Nothing

A Draft Version either:

```text id="uyqk5y"
passes validation and becomes Published
```

or:

```text id="16ok29"
remains Draft
```

Do not partially publish.

---

# 104. Task 13 — EF Core Configuration

Create schema:

```text id="o4qrdp"
calculator
```

and tables:

```text id="t77vsh"
templates

template_versions
```

---

# 105. Template Constraints

Configure:

```text id="ndun09"
PK(id)

unique normalized name

is_active

audit fields
```

---

# 106. Template Version Constraints

Configure:

```text id="0nv9o5"
PK(id)

FK(template_id)

UNIQUE(template_id, version_number)

status

definition jsonb

created audit

published audit
```

---

# 107. One Draft Partial Unique Index

Where practical:

```text id="bln5s1"
one Draft per Template
```

using a partial unique index.

---

# 108. JSONB Mapping

Use appropriate EF Core/PostgreSQL JSONB mapping.

Do not deserialize JSON through ad hoc string manipulation throughout the application.

---

# 109. Definition DTO vs Persistence

Prefer a typed Definition model in application code even if persisted as JSONB.

Conceptually:

```text id="086mpg"
CalculatorTemplateDefinition
```

serialized into JSONB.

---

# 110. JSON Schema Migration

`schemaVersion` belongs inside Definition.

Future Definition migration should be explicit.

---

# 111. Task 14 — Calculator Foundation Migration

Recommended migration name:

```text id="ei6dx0"
AddCalculatorTemplates
```

---

# 112. Migration Scope

Create:

```text id="x8ndyd"
calculator schema

calculator.templates

calculator.template_versions
```

and, where appropriate:

```text id="tzmw1y"
orders.order_types.calculator_template_id
```

plus FK.

---

# 113. Cross-Module FK

Add:

```text id="2k5628"
orders.order_types.calculator_template_id
→ calculator.templates.id
```

once both tables exist.

---

# 114. Order Type Template Nullability

`calculator_template_id` remains nullable.

---

# 115. Template Delete Behavior

Do not cascade-delete:

```text id="5rsbyc"
Order Types

Template Versions
```

because a Template is removed/deactivated.

Normal Template lifecycle is deactivation, not physical delete.

---

# 116. Version Delete Behavior

Published Versions must not be physically deleted through normal workflow.

Draft deletion may be allowed later if useful, but is not required for initial implementation.

---

# 117. Task 15 — Calculator Template Permissions

Register:

```text id="owalvd"
calculator.view

calculator.edit

calculator.manage_templates

calculator.publish_templates
```

Cost-specific permissions come in later phase:

```text id="mzeok3"
calculator.view_costs

calculator.edit_costs
```

---

# 118. Template Administration Permission

Creating/editing Templates requires:

```text id="uq6dgd"
calculator.manage_templates
```

---

# 119. Publishing Permission

Publishing additionally requires:

```text id="h1x6bj"
calculator.publish_templates
```

---

# 120. Viewing Templates

Administration Template viewing may use:

```text id="iuia5o"
calculator.manage_templates
```

or `calculator.view` depending on final permission grouping.

Keep it consistent.

---

# 121. Task 16 — Template Backend

Implement:

```text id="vc854s"
GET /api/calculator-templates

GET /api/calculator-templates/{id}

POST /api/calculator-templates

PATCH /api/calculator-templates/{id}
```

---

# 122. Template List DTO

Recommended:

```text id="e1hf21"
id

name

description

isActive

latestPublishedVersion

draftVersion
```

where useful.

---

# 123. Create Template

Request:

```text id="vepi2i"
name

description
```

---

# 124. Create Template Behavior

Creating Template may either:

```text id="82m80q"
create only Template identity
```

then Version separately,

or:

```text id="qpkiid"
create Template + initial Draft v1
```

in one transaction.

---

# 125. Recommended Initial Behavior

Prefer:

```text id="lc40zg"
Create Template
+
automatically create Draft Version 1
```

because every usable Template needs a version.

This simplifies administration.

---

# 126. Initial Draft Definition

Initial Draft may contain:

```text id="uf6g8s"
schemaVersion = 1

elements = []
```

and no selling output until configured.

It will fail publication until valid.

---

# 127. Template Edit

Template metadata edit may change:

```text id="eqcmhy"
name

description
```

but must not modify already Published Version definitions.

---

# 128. Template Activation

Implement:

```text id="u2c57d"
activate

deactivate
```

if consistent with existing master-data pattern.

---

# 129. Template Deactivation

Deactivation:

```text id="9jbaho"
prevents new assignments
```

but does not break:

```text id="kc8fh9"
Order Types already linked

Historical Order Calculators
```

Existing configuration remains visible.

---

# 130. Task 17 — Template Version Backend

Implement:

```text id="5zyvas"
GET /api/calculator-templates/{templateId}/versions

GET /api/calculator-templates/{templateId}/versions/{versionId}
```

---

# 131. Create New Version

Implement:

```text id="rjdck8"
POST /api/calculator-templates/{templateId}/versions
```

---

# 132. New Version Source

Recommended behavior:

```text id="r6168j"
If Published Version exists:
Copy latest Published Definition
into new Draft Version
```

This provides a practical starting point.

---

# 133. First Version

If Template has no previous version:

```text id="ud1q14"
create Draft v1
```

with empty/minimal Definition.

---

# 134. New Draft Rule

Do not create a second Draft if one already exists.

Return existing Draft or conflict according to API design.

---

# 135. Edit Draft

Implement:

```text id="7m5w46"
PATCH /api/calculator-templates/{templateId}/versions/{versionId}
```

for Draft only.

---

# 136. Published Edit Rejection

Attempt to edit Published Version:

```text id="i8dps8"
→ reject
```

with stable error code such as:

```text id="7gevb6"
TEMPLATE_VERSION_IMMUTABLE
```

---

# 137. Retired Edit Rejection

Retired Version is also immutable.

---

# 138. Draft Definition Replacement

For initial implementation, saving the Draft may replace the whole typed Definition JSON.

This is simpler than implementing dozens of micro-endpoints for individual fields.

---

# 139. Optimistic Concurrency

Draft editing may use:

```text id="yhczep"
updatedAt
```

or another concurrency token to prevent accidental overwrites.

If two administrators edit the same Draft, stale saves should be detected where practical.

---

# 140. Task 18 — Validation Endpoint

Implement:

```text id="e2tcx5"
POST /api/calculator-templates/{templateId}/versions/{versionId}/validate
```

---

# 141. Validation Endpoint Behavior

It must:

```text id="dqptc5"
Load Version

Parse Definition

Validate elements

Parse formulas

Validate functions

Validate references

Build dependency graph

Detect cycles

Validate selling output

Return structured result
```

---

# 142. Validation Does Not Publish

Validation endpoint does not change Version status.

---

# 143. Validation Response Example

Conceptually:

```text id="ak65db"
{
  "isValid": false,
  "errors": [
    {
      "code": "UNKNOWN_FIELD_REFERENCE",
      "fieldKey": "material_cost",
      "message": "Formula references unknown field 'material_price'."
    }
  ]
}
```

---

# 144. Task 19 — Publish Endpoint

Implement:

```text id="952mb7"
POST /api/calculator-templates/{templateId}/versions/{versionId}/publish
```

---

# 145. Publish Requirements

Require:

```text id="dkz3ke"
calculator.publish_templates
```

---

# 146. Publish Flow

```text id="d9i7fq"
Load Draft

Verify it belongs to Template

Run full validation

Reject if invalid

Begin transaction

Set status = Published

Set published_at

Set published_by

Commit
```

---

# 147. Published Immutability

After transaction:

```text id="f41zwp"
Definition cannot change
```

---

# 148. Existing Published Versions

Publishing a new Version does not modify older Published Versions.

---

# 149. Retiring Previous Version

Publishing v2 does not have to automatically mark v1 Retired.

Two approaches are possible:

```text id="fjyc9r"
A. Keep all Published

B. Mark previous Published as Retired
```

---

# 150. Recommended Version 1 Behavior

Use:

```text id="61ki90"
latest newly published version = Published

previous Published versions = Retired
```

while keeping all immutable and historically usable.

This makes selection of the current version clear.

---

# 151. Historical References

Retiring an older Version must never invalidate:

```text id="teu9rj"
existing Order Calculators
```

---

# 152. Latest Published Version

For new Order Calculators, choose:

```text id="g6pyh8"
highest/current Published version
```

not a Retired version.

---

# 153. Task 20 — Order Type Template Assignment

Enable:

```text id="vfrh0w"
Order Type
→ Calculator Template
```

---

# 154. Assignment Endpoint

May be handled through Order Type edit or explicit endpoint.

Conceptually:

```text id="lutb3m"
PATCH /api/order-types/{id}
```

with:

```text id="fnvsup"
calculatorTemplateId
```

after Calculator exists.

---

# 155. Assignment Validation

Template must:

```text id="z6wdi1"
exist

be active
```

for new assignment.

---

# 156. Published Version Requirement

An Order Type may be assigned to a Template with no Published Version during administrative setup.

However, Order Calculator creation later must fail gracefully until a Published Version exists.

---

# 157. Shared Template

Multiple Order Types may use the same Template.

Valid:

```text id="2abmsc"
UV Printing
→ Template A

UV Board Printing
→ Template A
```

---

# 158. Removing Template Assignment

Set:

```text id="h2ciq9"
calculatorTemplateId = null
```

through explicit administrative edit.

This affects future Order Calculator creation.

It must not delete existing Order Calculators later.

---

# 159. Task 21 — Basic Administration Frontend

Extend:

```text id="ytia8q"
/admin/calculator-templates
```

---

# 160. Template List

Recommended columns:

```text id="6rn9n0"
Name

Latest Published Version

Draft Version

Status
```

---

# 161. Template Create UI

Simple fields:

```text id="j4uuyr"
Name

Description
```

Creation automatically creates Draft v1 if using recommended behavior.

---

# 162. Template Detail Route

Example:

```text id="z8og4f"
/admin/calculator-templates/:templateId
```

---

# 163. Version List UI

Display:

```text id="1adggp"
v1 Retired

v2 Published

v3 Draft
```

with timestamps where useful.

---

# 164. Draft Editing UI in Foundation Phase

Do not build the final drag/drop Designer yet.

Use a structured development/admin editor sufficient to prove the Definition model.

Possible UI:

```text id="c7osot"
Ordered element list

Add Element

Edit properties

Formula text field

Validate

Publish
```

---

# 165. Structured Editor Goal

The UI must allow development/testing of real templates without requiring a spreadsheet-like canvas.

---

# 166. Element List

Conceptually:

```text id="rv0z2x"
Section: Dimensions

Number Input: Width
Number Input: Height
Calculated Field: Area

Section: Pricing

Number Input: Material Price
Calculated Field: Final Price
```

---

# 167. Element Properties Panel

For selected element, edit relevant properties:

```text id="6vzu6l"
Type

Key

Label

Visibility

Formula

Options
```

depending on element type.

---

# 168. Formula Editor

Initial formula editor may be a standard text field with validation feedback.

Do not build a complex code editor unless needed.

---

# 169. Validate Button

Provide:

```text id="jcfif4"
Validate
```

which calls backend validation.

---

# 170. Validation Feedback

Show errors associated with fields/elements clearly.

Example:

```text id="lmu15x"
material_cost

Unknown field:
material_price
```

---

# 171. Publish Button

Visible only with:

```text id="yeiv5z"
calculator.publish_templates
```

---

# 172. Publish Confirmation

Publishing is high-impact because Version becomes immutable and affects future Orders.

Use confirmation:

```text id="ghovz2"
Publish Template Version v3?

Published versions cannot be edited.
New Orders will use this version.
```

---

# 173. Published Version UI

Published Version must render read-only.

---

# 174. Create New Version

From Published Version provide:

```text id="kml58h"
Create New Version
```

which copies current Definition into a new Draft.

---

# 175. Order Type Administration Update

Add Calculator Template selector to:

```text id="x568id"
/admin/order-types
```

---

# 176. Template Selector

Show active Templates.

Example:

```text id="y5bkqy"
UV Printing Calculator
```

Option:

```text id="72o495"
None
```

---

# 177. Task 22 — Formula Unit Tests

Formula engine requires extensive Unit Tests.

---

# 178. Arithmetic Tests

Test:

```text id="d1su50"
1 + 2

5 - 3

4 * 2

10 / 2

10 % 3
```

---

# 179. Precedence Tests

Verify:

```text id="4cm45j"
2 + 3 * 4
=
14
```

and:

```text id="lplxet"
(2 + 3) * 4
=
20
```

---

# 180. Decimal Tests

Verify decimal calculations such as:

```text id="7vnccy"
0.1 + 0.2
```

behave correctly under decimal arithmetic.

---

# 181. Field Reference Tests

Example:

```text id="4pim6y"
width_mm = 1000

height_mm = 500

formula:
(width_mm * height_mm) / 1000000

result:
0.5
```

---

# 182. Unknown Field Test

Formula referencing missing key must fail validation.

---

# 183. Function Tests

Each approved function requires dedicated tests.

---

# 184. IF Tests

Examples:

```text id="msjpc3"
IF(quantity > 100, 0.9, 1)
```

test true and false branches.

---

# 185. Rounding Tests

Test:

```text id="kgya71"
ROUND

ROUNDUP

ROUNDDOWN

CEILING

FLOOR
```

including decimal boundaries.

---

# 186. Division by Zero Test

Must return controlled error.

---

# 187. Invalid Syntax Tests

Examples:

```text id="1o5drn"
1 +

SUM(

width_mm **
```

must be rejected.

---

# 188. Unsupported Function Test

```text id="g63ij3"
VLOOKUP(...)
```

must be rejected unless later explicitly supported.

---

# 189. Security Formula Tests

Ensure parser rejects attempts resembling:

```text id="w9tdf6"
System.IO.File.Delete(...)

SELECT * FROM users

fetch(...)

process.start(...)
```

These must remain meaningless/invalid Calculator syntax.

---

# 190. Dependency Tests

Test:

```text id="awzkne"
Input → Calculated
```

and multi-level dependencies.

---

# 191. Direct Circular Test

```text id="c0zs1h"
a = a + 1
```

invalid.

---

# 192. Two-Field Circular Test

```text id="qnhtp8"
a = b + 1

b = a + 1
```

invalid.

---

# 193. Multi-Level Circular Test

```text id="ks18od"
a = b

b = c

c = d

d = a
```

invalid.

---

# 194. Valid Graph Test

```text id="jj49wm"
a
↓
b
↓
c
↓
d
```

valid.

---

# 195. Evaluation Order Test

Ensure Calculated Fields evaluate in dependency order, not Definition display order.

---

# 196. Template Definition Tests

Verify:

```text id="apgw84"
Duplicate field key rejected

Invalid key rejected

Unknown element type rejected

Missing required property rejected

Invalid visibility rejected
```

---

# 197. Selling Output Tests

Verify publication rejects:

```text id="tzhg7d"
Missing selling output

Unknown selling output key

Non-numeric selling output
```

---

# 198. Template Version Tests

Required:

```text id="wyjc85"
Create Template

Draft v1 created

Edit Draft

Publish v1

Published immutable

Create Draft v2

v2 number correct
```

---

# 199. One Draft Test

Attempt to create second Draft:

```text id="90aymp"
→ rejected or existing Draft returned
```

according to chosen API behavior.

---

# 200. Publication Audit Test

Verify:

```text id="1atrg7"
published_at

published_by
```

are correctly stored.

---

# 201. Publication Permission Test

User without:

```text id="wyc8ev"
calculator.publish_templates
```

cannot publish even if allowed to edit Templates.

---

# 202. Historical Version Test

After publishing v2:

```text id="5utzo6"
v1 Definition unchanged
```

---

# 203. Version Copy Test

Creating new Draft from latest Published should copy Definition exactly before edits.

---

# 204. Order Type Assignment Tests

Verify:

```text id="9mo1ij"
Assign active Template

Remove assignment

Share Template between two Order Types

Inactive Template rejected for new assignment
```

---

# 205. Existing Assignment Inactive Template Test

If Template later becomes inactive:

```text id="6j400c"
existing Order Type relation remains visible
```

Do not silently clear the FK.

---

# 206. Migration Test

Apply complete migration chain:

```text id="24spth"
Authentication

Employees

Clients

Projects

Orders

Calculator Templates
```

to clean PostgreSQL.

---

# 207. JSONB Round-Trip Test

Store typed Template Definition in JSONB.

Reload it.

Verify:

```text id="jf9m3i"
same schema version

same elements

same keys

same formulas
```

---

# 208. Unknown Future Definition Version

If backend encounters unsupported:

```text id="6s9yt0"
schemaVersion
```

it must fail safely.

Do not guess how to interpret unknown schema.

---

# 209. API Validation Tests

Test:

```text id="8319wv"
Template create

Template edit

Version create

Draft edit

Validate

Publish

Published edit rejection
```

---

# 210. Authorization Tests

Test:

```text id="b9igh6"
calculator.manage_templates

calculator.publish_templates
```

independently.

---

# 211. Performance Expectations

Calculator Definition sizes are small.

Typical Template is expected to contain:

```text id="rsdhc4"
tens of elements
```

not millions.

No specialized distributed calculation architecture is needed.

---

# 212. Formula Compilation/Caching

Do not implement sophisticated persistent formula compilation caches initially.

Parse/evaluate efficiently and measure later.

---

# 213. Template Validation Performance

Publication-time full validation may do more work because publishing is infrequent.

Correctness is more important than micro-optimizing publication.

---

# 214. Calculator Foundation Commits

Recommended:

```text id="9ntm55"
feat(calculator): add template schema

feat(calculator): add template definition model

feat(calculator): add formula parser

feat(calculator): add formula evaluator

feat(calculator): add dependency validation

feat(calculator): add template version publishing

feat(orders): add calculator template assignment

test(calculator): add formula and version tests

feat(frontend): add calculator template administration foundation
```

---

# 215. First AI Coding Task

Recommended:

```text id="fw4moe"
Read:
- AI_RULES.md
- docs/03_Database_Design.md
- docs/15_Calculator_Module.md
- docs/17_Database_Schema_Overview.md
- docs/20_Testing_Strategy.md
- docs/24_Backend_Architecture.md
- docs/25_Development_Workflow_for_AI.md
- docs/32_Calculator_Foundation_Implementation_Plan.md

Task:
Implement only Calculator Template and Template Version persistence.

Create:
- CalculatorTemplate entity
- CalculatorTemplateVersion entity
- TemplateVersionStatus
- typed CalculatorTemplateDefinition foundation
- calculator schema
- templates table
- template_versions table
- JSONB mapping
- one-Draft-per-Template protection
- version uniqueness
- migration
- persistence integration tests

Do not implement:
- formula parser
- Template Designer
- Order Calculator
- Cost Items
- Calculator frontend
- arbitrary JSON editing endpoint

Before finishing:
- build
- apply migrations to clean PostgreSQL
- test JSONB round trip
- test version constraints
- review migration
```

---

# 216. Second AI Coding Task

```text id="r4e8bl"
Implement Template Definition validation:
- schemaVersion
- supported element types
- stable field keys
- unique keys
- required properties
- visibility scopes

Do not implement formula evaluation yet.
```

---

# 217. Third AI Coding Task

```text id="4d6whb"
Implement safe formula tokenizer/parser and AST.

Support only documented syntax.
Do not use eval, Roslyn scripting, JavaScript, SQL, Python, or shell execution.
Add parser tests.
```

---

# 218. Fourth AI Coding Task

```text id="nh7lhv"
Implement formula evaluator and approved function registry.

Use decimal arithmetic.
Add tests for every supported function.
```

---

# 219. Fifth AI Coding Task

```text id="3xcvz8"
Implement field dependency extraction, topological evaluation ordering, unknown-reference validation, and circular-reference detection.

Add direct and indirect cycle tests.
```

---

# 220. Sixth AI Coding Task

```text id="uuyeak"
Implement complete Template validation and sellingPriceFieldKey validation.

Return structured validation errors.
```

---

# 221. Seventh AI Coding Task

```text id="r628p3"
Implement Calculator Template and Template Version API:
- create template
- list/detail
- create draft version
- edit draft
- validate

Do not implement publish until validation tests are stable.
```

---

# 222. Eighth AI Coding Task

```text id="t6krcm"
Implement Template publication.

Requirements:
- full validation
- calculator.publish_templates
- immutable after publish
- published audit metadata
- previous published version retired
- transaction
- tests
```

---

# 223. Ninth AI Coding Task

```text id="x0vxwj"
Add Order Type → Calculator Template assignment.

Requirements:
- nullable
- active Template required for new assignment
- no effect on historical Orders
- no automatic Calculator creation yet
```

---

# 224. Tenth AI Coding Task

```text id="pemmrj"
Implement basic structured Calculator Template Administration UI.

Support:
- Template list
- create Template
- version list
- structured Draft editing
- formula field
- validation
- publish
- create new version

Do not build advanced drag-and-drop spreadsheet Designer yet.
```

---

# 225. Calculator Foundation Completion Gate

Do not begin Order Calculator implementation until:

```text id="tdqw7c"
Template schema works

Template Versions work

JSONB Definition works

Field keys validated

Formula parser works

Formula evaluator works

Approved functions tested

Unknown references rejected

Circular references rejected

Dependency order works

Selling output validation works

Draft editing works

Published versions immutable

New versions work

Order Type assignment works

Permissions work

Basic Template Administration works

Integration tests pass
```

---

# 226. Historical Safety Gate

Before proceeding, explicitly verify:

```text id="p5ysrp"
Publish v1

Create v2 Draft

Modify v2

Publish v2

Reload v1

v1 is unchanged
```

This test is mandatory.

---

# 227. No Order Calculator Yet

This phase must not create per-Order Calculator values unless needed for a small integration spike.

The next phase owns:

```text id="2gb1df"
calculator.order_calculators
```

---

# 228. No Cost Items Yet

Do not create:

```text id="zjjcxo"
calculator.cost_items
```

during Foundation unless implementation sequencing deliberately includes the empty schema later.

Costs have their own phase.

---

# 229. No Manual Selling Price Override

Do not add an override system during Calculator Foundation.

---

# 230. No External Excel Runtime

Do not require:

```text id="ow4vno"
Microsoft Excel

LibreOffice

Excel files at runtime
```

for Calculator execution.

---

# 231. No Excel Formula Compatibility Layer

Do not make the Formula Engine accept arbitrary Excel syntax.

Lithograph uses its own controlled formula subset.

---

# 232. No Macros

Do not support:

```text id="feicb9"
VBA

JavaScript

Scripts
```

inside Templates.

---

# 233. No Database Queries from Formulas

Formula must not query:

```text id="xjvwys"
Clients

Projects

Orders

Users

PostgreSQL
```

directly.

The Calculator runtime supplies approved values only.

---

# 234. No Network Calls from Formulas

Formula cannot call:

```text id="un4hne"
HTTP APIs

web services

network files
```

---

# 235. No File Access from Formulas

Formula cannot read/write files.

---

# 236. Definition Flexibility Rule

Use JSONB only for the flexible Template Definition.

Do not respond to flexibility by storing every Calculator concept inside one unvalidated JSON document.

---

# 237. Typed Definition Rule

Application code must parse Definition into typed structures before use.

Do not pass arbitrary raw JSON directly into formula execution.

---

# 238. Versioning Rule

Any structural change to a Published calculation requires:

```text id="w5xz3x"
New Draft Version
```

not:

```text id="5tipr6"
Edit Published JSON directly
```

---

# 239. Label Change Rule

Even a label change on Published Version should not mutate historical Version.

If historical consistency matters, create a new Version.

---

# 240. Field Key Change Rule

Changing a field key is effectively a schema change to the Calculator Definition.

Do it only in a new Draft Version.

---

# 241. Formula Change Rule

Changing formula always requires a new Draft Version after publication.

---

# 242. Order Type Assignment vs Version

Order Type references:

```text id="0fv0m6"
Template
```

not:

```text id="macgqn"
Template Version
```

because future Orders should use the latest Published Version.

---

# 243. Order Calculator Version

Later, each actual Order Calculator references:

```text id="eckl6d"
exact Template Version
```

This is how historical pricing is preserved.

---

# 244. Future Template Designer

The later visual Designer may improve:

```text id="90lxqj"
Drag-and-drop

Field palette

Layout editing

Formula assistance

Preview
```

but it must write the same validated Template Definition contract.

---

# 245. Designer Must Not Create a Second Format

Do not allow the visual Designer to invent a different runtime schema.

There should be:

```text id="5qmfcm"
one Template Definition format
```

used by:

```text id="go81ek"
Designer

Validation

Formula Engine

Order Calculator
```

---

# 246. Future Formula Enhancements

Possible future additions:

```text id="hv7slm"
More functions

Reusable constants

Better formula autocomplete

Table aggregate helpers

Template test datasets
```

Only add based on real Calculator needs.

---

# 247. Calculator Foundation Simplicity Rule

Before adding spreadsheet functionality, ask:

```text id="v99vn7"
Do our real Lithograph pricing templates need this?
```

If not, defer it.

---

# 248. Final Calculator Foundation Principle

The Calculator Foundation must answer:

```text id="y29zm1"
What fields exist?

Which fields are user inputs?

Which fields are calculated?

What formulas calculate them?

Are those formulas safe?

In which order are they evaluated?

Is the Template valid?

Which published version defines historical pricing?
```

The central rule is:

```text id="4ehszb"
Build a small safe calculation engine first.

Build the visual spreadsheet experience on top of it.
```

---

**End of Document**