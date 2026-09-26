# Lithograph ERP

**Document:** 03_Database_Design.md  
**Version:** 1.1  
**Status:** Approved  
**Project:** Lithograph ERP  

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `01_Technology_Stack.md`
- `02_Architecture.md`

---

# 1. Purpose

This document defines the database design standards used throughout Lithograph ERP.

It does not define every business table.

Instead, it defines the rules that all database modules and future tables must follow.

The purpose is to ensure:

- Consistency
- Data integrity
- Maintainability
- Predictable naming
- Reliable reporting
- Easy AI-assisted development
- Future extensibility

---

# 2. Database Engine

Lithograph ERP uses:

```text id="ri3x9l"
PostgreSQL
```

Version 1 uses one PostgreSQL database.

Major modules may own separate PostgreSQL schemas.

Example:

```text id="39tq1j"
auth

employees

clients

projects

orders

calculator
```

---

# 3. Database Philosophy

The database should remain:

```text id="jtv7d3"
Simple

Relational

Explicit

Consistent

Reliable
```

The database should not attempt to solve hypothetical future problems.

Data structures should reflect current Lithograph business requirements.

---

# 4. Single Source of Truth

Every business fact should normally have one authoritative location.

Avoid storing the same information in multiple places.

Example:

Project Client relationship belongs to:

```text id="dj40q4"
projects.projects.client_id
```

The same Client ID should not also be copied into every Order unless there is a documented historical reason.

---

# 5. Calculated Data

Calculated data should generally not be stored when it can be reliably derived.

Example:

```text id="zj83eu"
profit = selling_price - cost_price
```

Therefore:

```text id="dwy53n"
profit
```

is not stored as a normal Order column.

---

# 6. Historical Snapshot Exception

Some calculated values must be stored because they represent a historical business result.

For Orders, these include:

```text id="w2c071"
selling_price

cost_price
```

These values may originally come from the Calculator, but the final values are persisted on the Order.

This ensures that historical Orders retain their original business values even if Calculator Templates later change.

---

# 7. Database Naming Standard

All PostgreSQL identifiers use:

```text id="bswkdx"
lowercase snake_case
```

This applies to:

- Schemas
- Tables
- Columns
- Indexes
- Constraints
- Sequences

Correct:

```text id="4ae4yk"
projects.project_members

orders.checklist_items

project_id

selling_price

created_at
```

Incorrect:

```text id="4ayqpd"
Projects.ProjectMembers

ProjectID

SellingPrice

CreatedAt
```

---

# 8. Schema Naming

Schema names should be:

- Lowercase
- Short
- Descriptive
- Based on module ownership

Examples:

```text id="x891l5"
auth

employees

clients

projects

orders

calculator
```

Avoid technical or vague schema names such as:

```text id="txw0yr"
data

core

misc

general
```

unless there is a clearly documented reason.

---

# 9. Table Naming

Tables use:

```text id="6fbn6v"
plural snake_case
```

Examples:

```text id="u7o4h1"
users

employees

clients

projects

orders

project_members

checklist_items

calculator_templates
```

Avoid prefixes such as:

```text id="lgqub7"
tbl_users

t_orders

db_clients
```

---

# 10. Column Naming

Columns use:

```text id="lgvqba"
lowercase snake_case
```

Examples:

```text id="qwkuct"
id

project_id

order_type_id

selling_price

cost_price

created_at
```

Avoid abbreviations unless they are universally understood and approved.

Prefer:

```text id="8ylsrg"
description
```

instead of:

```text id="vnymk4"
desc
```

---

# 11. Primary Keys

Normal business entities use UUID primary keys.

Example:

```text id="wwnj9a"
id UUID PRIMARY KEY
```

UUID values are:

- Internal system identifiers
- Stable
- Globally unique
- Not intended for normal user interaction

---

# 12. UUID Generation

UUIDs should be generated consistently by the application or PostgreSQL using an approved UUID generation mechanism.

Do not mix multiple UUID strategies without a reason.

The implementation choice should remain consistent throughout the application.

---

# 13. Junction Tables

Pure many-to-many relationship tables do not require an artificial UUID when the relationship itself is the identity.

Example:

```text id="bt2xoh"
auth.user_roles

user_id
role_id
```

Primary key:

```text id="bknpp9"
PRIMARY KEY (user_id, role_id)
```

Another example:

```text id="vp2cwk"
auth.role_permissions

role_id
permission_id
```

Primary key:

```text id="m4s6xb"
PRIMARY KEY (role_id, permission_id)
```

Do not add unnecessary UUID IDs to simple relationship tables.

---

# 14. Business IDs

Human-facing business entities may have a generated Business ID in addition to the UUID primary key.

Examples:

```text id="5xjyhv"
CL-000001

PRJ-2026-000001

ORD-2026-000001
```

Business IDs exist for:

- Searching
- Communication
- Reports
- Printed documents
- Human reference

---

# 15. Business ID Rules

A Business ID must be:

- Unique
- Generated automatically
- Human-readable
- Stable
- Non-editable by normal users
- Never reused

Sequence gaps are acceptable.

Deleted or cancelled records do not cause their Business IDs to be reused.

---

# 16. Business IDs Are Not Primary Keys

Business IDs must not replace internal UUID primary keys.

Example:

```text id="qtaj05"
id = 6ca6df32-...

business_id = ORD-2026-000125
```

The application uses `id` internally.

Users normally interact with `business_id`.

---

# 17. Foreign Keys

Foreign key columns use the pattern:

```text id="fxmwql"
<entity>_id
```

Examples:

```text id="3f2465"
client_id

project_id

employee_id

order_type_id
```

Avoid vague names such as:

```text id="27mnyp"
reference_id

link_id

parent
```

unless the relationship genuinely has a generic purpose.

---

# 18. Foreign Key Constraints

Foreign key constraints should be used whenever practical to protect relational integrity.

Example:

```text id="4xadhh"
orders.orders.project_id
    ↓
projects.projects.id
```

The database should prevent an Order from referencing a Project that does not exist.

---

# 19. Cross-Schema Foreign Keys

Cross-schema foreign keys are allowed in Version 1.

Example:

```text id="b41mni"
projects.projects.client_id
    ↓
clients.clients.id
```

Example:

```text id="35fdd3"
orders.orders.project_id
    ↓
projects.projects.id
```

This is acceptable because Version 1 uses one PostgreSQL database.

---

# 20. Module Ownership Still Applies

A foreign key does not give one module ownership of another module's data.

For example:

Orders may reference:

```text id="nmnnrw"
project_id
```

but the Orders module must not arbitrarily modify Project records.

Database relationships and business ownership are separate concepts.

---

# 21. Required Relationships

A required relationship should use a non-null foreign key.

Example:

Every Order must belong to a Project.

Therefore:

```text id="m5z719"
project_id NOT NULL
```

Optional relationships may use nullable foreign keys.

---

# 22. Delete Behavior

Delete behavior must be chosen deliberately.

Avoid uncontrolled cascade deletes across important business entities.

For example:

Deleting a Client must not automatically delete all Projects and Orders.

Important business records should normally be preserved.

Relationship tables may safely use cascade deletion in some cases.

Example:

Removing a User may remove related `user_roles` rows when appropriate.

---

# 23. Soft Delete

Soft delete is not mandatory for every table.

Use soft delete only where it provides meaningful business value.

A soft-deletable record may use:

```text id="xup88u"
is_deleted BOOLEAN
```

---

# 24. When to Use Soft Delete

Soft delete is appropriate when:

- Historical references must remain
- Records may need restoration
- Accidental deletion must be recoverable
- Reports may depend on historical existence

Likely examples:

- Clients
- Employees
- Projects
- Orders

---

# 25. When Not to Use Soft Delete

Soft delete is usually unnecessary for temporary or relationship records.

Examples:

```text id="jxz9wg"
auth.sessions

auth.user_roles

auth.role_permissions
```

These may normally be physically removed.

---

# 26. Active vs Deleted

`is_active` and `is_deleted` represent different concepts.

Example:

A User may be:

```text id="dkyxgw"
is_active = false

is_deleted = false
```

This means:

The account still exists but cannot log in.

Do not use `is_deleted` as a replacement for normal business status.

---

# 27. Audit Fields

Important business entities may use audit fields.

Typical fields:

```text id="tzxl5r"
created_at

created_by

updated_at

updated_by
```

These fields provide useful history without building a full audit system.

---

# 28. Audit Fields Are Not Mandatory Everywhere

Do not mechanically add all audit fields to:

- Junction tables
- Temporary tables
- Session tables
- Pure technical tables

Use them where they provide real value.

---

# 29. Bootstrap Records

During first-run setup there is no authenticated user yet.

Therefore fields such as:

```text id="8j6l52"
created_by
```

must allow a safe bootstrap strategy when necessary.

Do not create impossible database requirements for first system initialization.

---

# 30. Timestamps

Store timestamps using PostgreSQL timezone-aware timestamp types.

Conceptually:

```text id="15tf50"
timestamp with time zone
```

The application should use UTC internally.

---

# 31. Time Zone Rule

Persist application timestamps in UTC.

Convert to local time only for presentation when necessary.

Do not store server-local timestamps that depend on where the application happens to be deployed.

---

# 32. Date-Only Values

Business fields that represent dates rather than moments in time should use a date-only type.

Examples:

```text id="4n1qlt"
deadline

start_date
```

when time-of-day is not relevant.

Do not use a full timestamp when only a calendar date is required.

---

# 33. Money

Financial values must use exact decimal types.

Examples:

```text id="in9vqi"
selling_price

cost_price

amount
```

Use PostgreSQL numeric/decimal-compatible types.

Do not use floating-point types such as:

```text id="2dgnmn"
real

double precision
```

for money.

---

# 34. Currency

Version 1 may use one primary operating currency where appropriate.

However, database design should avoid embedding currency into column names.

Good:

```text id="6swg8l"
selling_price
```

Avoid:

```text id="7cst65"
selling_price_amd
```

If multi-currency support becomes necessary, it should be explicitly designed rather than improvised.

---

# 35. Numeric Precision

Precision and scale for financial and calculator values must be chosen deliberately.

Do not assume every number requires the same precision.

Examples requiring different treatment may include:

- AMD money
- Millimeters
- Square meters
- Percentages
- Machine rates
- Material quantities

The relevant module specification should define precision where important.

---

# 36. Text Fields

Use appropriate limits for short identifiers and names.

Examples:

```text id="i432zt"
username

business_id

permission_code
```

should use bounded string lengths.

Long free-form fields may use PostgreSQL `text`.

Avoid arbitrary very large lengths without purpose.

---

# 37. Boolean Fields

Boolean fields should use positive, clear names.

Good:

```text id="53cfqz"
is_active

is_completed

is_deleted
```

Avoid ambiguous names such as:

```text id="j6wwxe"
status_flag

enabled_value
```

---

# 38. Status Fields

Business statuses should be explicit.

Examples:

```text id="1atjs8"
project_status

order_status
```

Status values should be defined in the relevant module specification.

Do not use unexplained numeric codes such as:

```text id="wpkz6a"
status = 3
```

without a strongly typed application representation.

---

# 39. Enum Strategy

Application enums may be used when values are:

- Small
- Stable
- Truly structural

Configurable business data should normally live in database tables rather than source-code enums.

Example:

Order Types are configurable data.

Therefore do not hardcode them as:

```text id="8cfuqd"
enum OrderType
```

with fixed production technologies.

---

# 40. Unique Constraints

Use database unique constraints for data that must be unique.

Examples:

```text id="qpzguh"
auth.users.username

auth.permissions.code

orders.orders.business_id
```

Do not rely only on frontend checks for uniqueness.

---

# 41. Case-Insensitive Uniqueness

Fields such as Username should behave consistently regardless of letter case.

Example:

```text id="1zu6zc"
aram

Aram

ARAM
```

must not become three different login accounts.

The implementation must enforce case-insensitive uniqueness.

The exact technical implementation may be selected during Auth implementation.

---

# 42. Indexes

Indexes should be created for:

- Primary keys
- Unique constraints
- Frequently searched fields
- Important foreign keys
- Common filtering fields where justified

Avoid unnecessary indexing.

Indexes increase:

- Storage
- Write cost
- Migration complexity

Add indexes because queries need them, not automatically everywhere.

---

# 43. Foreign Key Indexes

Frequently used foreign keys should normally be indexed.

Examples:

```text id="2nemb8"
projects.projects.client_id

orders.orders.project_id

projects.project_members.employee_id
```

This improves common relational queries.

---

# 44. Searchable Business IDs

Business ID columns should use unique indexes.

Example:

```text id="y1dtuh"
orders.orders.business_id
```

Searching by Business ID should remain fast.

---

# 45. Composite Indexes

Composite indexes should be added only for known query patterns.

Do not create speculative multi-column indexes before actual use cases exist.

---

# 46. Constraints

Use database constraints for important data integrity rules.

Examples:

- Required values
- Unique values
- Valid foreign keys
- Non-negative values when appropriate

The database should help prevent impossible data states.

---

# 47. Validation Layers

Validation should exist at multiple levels.

Frontend:

```text id="n0szoy"
User-friendly validation
```

Backend:

```text id="pvz67n"
Business and security validation
```

Database:

```text id="ls7yby"
Integrity constraints
```

Do not rely on only one layer.

---

# 48. Transactions

A business operation that updates multiple related records should use a database transaction when partial completion would create invalid data.

Example:

Creating a Project and its initial required related data should either:

```text id="cokadh"
Complete entirely
```

or:

```text id="ohjqsu"
Fail entirely
```

when atomicity is required.

---

# 49. Transaction Simplicity

Do not make transactions unnecessarily large.

A transaction should represent one meaningful business operation.

Avoid holding transactions open while waiting for:

- User interaction
- External services
- Long-running processing

---

# 50. Entity Framework Core

Entity Framework Core is the default data-access technology.

Use it for:

- Entity mappings
- Relationships
- Queries
- Transactions
- Migrations

Avoid bypassing Entity Framework Core unless there is a clear and documented reason.

---

# 51. Entity Configuration

Database mappings should be explicit enough that schema behavior is predictable.

Important items include:

- Table name
- Schema name
- Key
- Required fields
- Maximum lengths
- Relationships
- Delete behavior
- Indexes

Do not depend on fragile implicit conventions for important business structures.

---

# 52. Database Migrations

All schema changes must be made through Entity Framework Core migrations.

Migration names should clearly describe the change.

Example:

```text id="07rtor"
CreateAuthenticationSchema

AddProjectMembers

AddOrderChecklistItems
```

Avoid names such as:

```text id="guitc7"
Update1

FixStuff

Changes
```

---

# 53. Applied Migrations

Once a migration has been applied to a shared or production database, do not rewrite it.

Create a new migration for further changes.

This preserves reliable database history.

---

# 54. Seed Data

Seed data may be used for required system-level records.

Examples:

- Director system role
- Core permissions
- Required configuration values

Do not fill the database with unnecessary sample business data in production.

---

# 55. Configurable Data

Business data that users may change should normally be stored in tables.

Examples:

```text id="ai4xy4"
Order Types

Calculator Templates
```

Do not hardcode configurable business values into source code.

---

# 56. Historical Data

Historical business data should remain stable.

Changing current configuration must not unexpectedly rewrite historical Orders.

Examples:

Changing:

```text id="ynb2np"
Calculator Template
```

must not silently recalculate old Order Selling Prices.

Historical behavior for Calculator Templates will be defined in the Calculator module.

---

# 57. Checklist Simplicity

Version 1 checklist items remain intentionally simple.

Conceptual fields:

```text id="7o3ocu"
id

order_id

text

is_completed

sort_order
```

Do not add undocumented fields such as:

- Employee assignment
- Deadline
- Priority
- Notes
- Folder path
- File name

---

# 58. Project Team Data

Project Team assignments belong to the Projects module.

Version 1 does not duplicate the Project Team into Orders.

Conceptually:

```text id="tma5sj"
projects.project_members
```

connects:

```text id="7szuvt"
project

employee

project_role
```

The detailed table design belongs to the Projects specification.

---

# 59. Order Team Table

Version 1 should not create:

```text id="4yhb4m"
orders.order_members
```

unless a future requirement introduces Order-specific employee assignments.

This prevents duplicated team information.

---

# 60. Reports

Reports should normally query operational data rather than store copies of it.

Example:

```text id="700nl2"
SUM(selling_price)

SUM(cost_price)

SUM(selling_price - cost_price)
```

may be calculated from Orders.

Persistent report configuration may be added later if needed.

---

# 61. Database Views

Database views may be introduced when they simplify well-defined reporting or read models.

Do not create large numbers of database views before a real reporting need exists.

---

# 62. Stored Procedures

Stored procedures are not the default business logic mechanism.

Application business logic belongs primarily in C#.

Use database procedures or functions only when they provide a clear technical advantage.

---

# 63. Triggers

Avoid database triggers for normal business workflows.

Triggers can create hidden behavior that is difficult for developers and AI coding tools to understand.

Use them only when the database itself genuinely needs to guarantee behavior that is inappropriate elsewhere.

---

# 64. JSON Data

PostgreSQL JSON/JSONB should not replace normal relational design.

Use relational tables for structured business entities.

JSON may be appropriate for:

- Flexible Calculator-specific configuration
- Non-relational metadata
- Structures whose shape genuinely varies

The Calculator module will explicitly define where JSON is allowed.

---

# 65. Arrays and Lists

Do not store multiple relational IDs in:

- Comma-separated strings
- JSON arrays
- Text fields

Use relationship tables.

Incorrect:

```text id="3olams"
participant_ids = "id1,id2,id3"
```

Correct:

```text id="tbkd39"
project_members
```

with one relationship per row.

---

# 66. Nullability

Columns should be nullable only when the absence of a value is meaningful.

Required business information should use `NOT NULL`.

Do not make everything nullable for convenience.

---

# 67. Default Values

Database defaults may be used for predictable technical values.

Examples:

```text id="81wt7k"
is_active = true

is_deleted = false
```

Avoid hiding important business decisions inside database defaults.

---

# 68. Order of Data Ownership

When deciding where a field belongs, ask:

> Which module owns the meaning of this information?

Examples:

```text id="euk20k"
Username
→ Authentication

Employee Position
→ Employees

Client Name
→ Clients

Project Deadline
→ Projects

Selling Price
→ Orders

Formula Expression
→ Calculator
```

Place data with the module that owns its meaning.

---

# 69. Avoid Convenience Duplication

Do not copy information into another table merely because it makes one screen easier to build.

Example:

Do not copy:

```text id="6uv3ly"
client_name
```

into `orders.orders` just to display the Client name.

Use the relationship through Project and Client.

Duplicate only when a documented historical snapshot or performance requirement justifies it.

---

# 70. Performance Philosophy

Correct relational design comes before premature optimization.

Initial focus:

```text id="7xe2hg"
Correctness

Simplicity

Indexes for known queries
```

Do not denormalize the database before a real performance problem exists.

---

# 71. Backup and Recovery

Database backup strategy is an operational requirement and must exist before production use.

At minimum, production deployment should support:

- Regular PostgreSQL backups
- Restore testing
- Backup retention

Exact operational procedures will be documented closer to deployment.

---

# 72. Development Data

Development and test environments must not depend on production data.

Sample or seeded development data should be clearly separated from real business information.

---

# 73. Secrets

Database credentials must not be stored in:

- Source code
- Markdown documentation
- Git commits
- Client-side frontend code

Use appropriate application configuration and secret-management mechanisms.

---

# 74. Future Modules

Future modules such as:

```text id="7dpv71"
warehouse

purchasing

finance
```

may add their own schemas.

Existing schemas should not be redesigned merely to anticipate these future modules.

---

# 75. Database Change Rule

Before adding or changing a table, ask:

1. Which module owns this data?
2. Is this data actually required?
3. Is it already stored somewhere else?
4. Is this a business fact or a calculated value?
5. Does it need historical persistence?
6. Should the relationship be represented relationally?
7. Can the design remain simpler?

---

# 76. Final Database Principle

The Lithograph ERP database should remain understandable enough that a developer can inspect its schemas and quickly understand the business structure.

Prefer explicit relational design over clever database techniques.

Prefer:

```text id="dfi6r2"
Simple

Consistent

Constrained

Documented
```

over unnecessary flexibility or abstraction.

---

**End of Document**