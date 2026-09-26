# Lithograph ERP

**Document:** 12_Clients_Module.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Module:** Clients

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `05_Numbering_System.md`
- `06_UI_UX_Principles.md`
- `11_Employees_Module.md`

---

# 1. Purpose

The Clients module stores information about Lithograph customers.

A Client may be:

```text
A company
```

or:

```text
An individual customer
```

Clients are used primarily by the Projects module.

---

# 2. Main Responsibilities

The Clients module is responsible for:

```text
Client records

Client Business IDs

Client names

Basic contact information

Client active status
```

The Clients module does not manage:

```text
Sales pipeline

CRM activities

Marketing campaigns

Contracts

Invoices

Accounting

Customer portal
```

These are outside Version 1.

---

# 3. Client Definition

A Client is:

```text
A company or individual that purchases products or services from Lithograph.
```

Examples:

```text
Samsung Armenia

ABC Construction

Armenian Expo

Individual Customer
```

---

# 4. Module Ownership

The Clients module owns PostgreSQL schema:

```text
clients
```

Initial Version 1 table:

```text
clients.clients
```

No additional Client tables are required initially.

---

# 5. Database Structure

```text
clients
└── clients
```

The module should remain intentionally small.

---

# 6. Table: clients.clients

## Purpose

Stores Lithograph Client records.

Each row represents one Client.

---

# 7. Client Columns

| Column | PostgreSQL Type | Nullable | Description |
|---|---|---:|---|
| `id` | `uuid` | No | Primary key |
| `business_id` | `varchar(20)` | No | Human-readable Client ID |
| `name` | `varchar(250)` | No | Client name |
| `contact_person` | `varchar(200)` | Yes | Main contact person |
| `phone` | `varchar(50)` | Yes | Main contact phone |
| `email` | `varchar(200)` | Yes | Main contact email |
| `address` | `text` | Yes | Client address |
| `notes` | `text` | Yes | General Client notes |
| `is_active` | `boolean` | No | Whether Client is available for new business |
| `created_at` | `timestamptz` | No | Creation time |
| `created_by` | `uuid` | Yes | Creating User |
| `updated_at` | `timestamptz` | Yes | Last update time |
| `updated_by` | `uuid` | Yes | Last modifying User |

---

# 8. Primary Key

```text
PRIMARY KEY (id)
```

`id` is a UUID.

---

# 9. Client Business ID

Every Client receives a Business ID.

Format:

```text
CL-000001
```

Examples:

```text
CL-000001

CL-000002

CL-000125
```

Client numbering does not reset annually.

---

# 10. Business ID Generation

The backend generates the Client Business ID automatically.

The frontend must not generate or edit it.

Example creation flow:

```text
Create Client
    ↓
Backend generates Business ID
    ↓
CL-000126
    ↓
Client stored
```

---

# 11. Business ID Rules

Client Business ID:

- Is required
- Is unique
- Is generated automatically
- Cannot be edited by normal users
- Is never reused
- Does not change if Client information changes

---

# 12. Business ID Constraint

Database must enforce:

```text
UNIQUE (business_id)
```

Business ID should also be indexed for fast lookup.

---

# 13. Client Name

`name` is required.

Examples:

```text
Samsung Armenia

ABC Advertising

Aram Petrosyan
```

The same field is used for both:

- Companies
- Individual Clients

This avoids introducing unnecessary Client Type complexity in Version 1.

---

# 14. Client Type

Version 1 does not require a separate:

```text
client_type
```

field.

A Client may simply be entered by name.

If future workflows require distinguishing:

```text
Company

Individual
```

a Client Type may be added later.

---

# 15. Client Name Uniqueness

Client name is not required to be unique.

Different Clients may legitimately have similar or identical names.

Therefore:

```text
name
```

must not use a database unique constraint.

---

# 16. Contact Person

`contact_person` stores the main person Lithograph communicates with for this Client.

Example:

```text
Anna Martirosyan
```

It is optional.

Version 1 supports one main contact person directly on the Client.

---

# 17. Multiple Client Contacts

Version 1 does not require a separate:

```text
client_contacts
```

table.

If Lithograph later needs multiple named contacts per Client, this can be introduced as a separate feature.

Do not build it preemptively.

---

# 18. Phone

`phone` is optional.

Store phone numbers as text.

This allows:

```text
+374 91 123456

(010) 123456
```

Do not store phone numbers as numeric values.

---

# 19. Email

`email` is optional.

Client email is contact information.

It is unrelated to ERP Authentication.

---

# 20. Address

`address` is optional free-form text.

Version 1 does not require separate fields such as:

```text
country

city

street

postal_code
```

unless future reporting or delivery requirements make those necessary.

---

# 21. Notes

`notes` is an optional general-purpose field for useful Client information.

Examples:

```text
Preferred delivery time is after 15:00.

Requires purchase order before production.

Main contact prefers WhatsApp.
```

Notes should contain useful operational information.

Do not use Notes as a replacement for structured data that later becomes important.

---

# 22. Active Client

Active Client:

```text
is_active = true
```

means the Client may normally be selected for new Projects.

---

# 23. Inactive Client

Inactive Client:

```text
is_active = false
```

remains stored for historical purposes.

Inactive Clients should normally not appear in default new Project selectors.

---

# 24. Client Deactivation

Normal Client removal behavior is:

```text
Deactivate Client
```

rather than physical deletion.

This preserves:

- Historical Projects
- Historical Orders
- Reports
- Audit information

---

# 25. Physical Client Deletion

Version 1 does not expose permanent Client deletion as a normal business operation.

Use:

```text
Deactivate
```

instead.

---

# 26. Soft Delete

Version 1 does not require:

```text
is_deleted
```

for Clients.

`is_active` is sufficient for current business requirements.

If future requirements demand a separate deletion state, it may be introduced later.

---

# 27. Client Reactivation

An inactive Client may be reactivated.

Conceptually:

```text
is_active = true
```

After reactivation, the Client may again be selected for new Projects.

---

# 28. Projects Relationship

A Client may have multiple Projects.

Relationship:

```text
Client
   │
   ├── Project
   ├── Project
   └── Project
```

The Projects module will store:

```text
client_id
```

referencing:

```text
clients.clients.id
```

---

# 29. Client Does Not Store Project IDs

Do not store Project IDs inside the Client record.

Incorrect:

```text
project_ids = [...]
```

Correct:

```text
projects.projects.client_id
```

The relationship is represented relationally from Project to Client.

---

# 30. Historical Projects

Deactivating a Client must not affect existing Projects.

Example:

```text
Client:
Inactive

Project from 2026:
Still exists
```

Historical data must remain valid.

---

# 31. New Project Rule

Inactive Clients should normally not be selectable when creating a new Project.

The backend should enforce this rule.

Existing Projects remain unchanged.

---

# 32. Client Creation

Authorized Users may create Clients manually.

Required information:

```text
Name
```

Automatically generated:

```text
Business ID
```

Optional:

```text
Contact Person

Phone

Email

Address

Notes

Active
```

Default:

```text
Active = true
```

---

# 33. Create Client Interface

Example:

```text
New Client

Client ID
Generated automatically

Name
[____________________________]

Contact Person
[____________________________]

Phone
[____________________________]

Email
[____________________________]

Address
[____________________________]

Notes
[____________________________]

☑ Active

[Create] [Cancel]
```

Business ID should be displayed as read-only if shown before or after creation.

---

# 34. Client Edit Interface

Editable fields:

```text
Name

Contact Person

Phone

Email

Address

Notes

Active
```

Business ID is read-only.

---

# 35. Client List

The Clients module should provide a simple Client list.

Example:

| Client ID | Name | Contact Person | Phone | Status |
|---|---|---|---|---|
| CL-000001 | Samsung Armenia | Anna Martirosyan | +374... | Active |
| CL-000002 | ABC Construction | Armen Sargsyan | +374... | Active |
| CL-000003 | Old Customer | — | — | Inactive |

---

# 36. Client List Columns

Recommended Version 1 columns:

```text
Business ID

Name

Contact Person

Phone

Status
```

Optional:

```text
Email
```

depending on available screen width.

Do not overload the list.

---

# 37. Client Search

Search should support:

```text
Business ID

Name

Contact Person

Phone
```

Business ID lookup should be fast.

---

# 38. Client Filters

Useful Version 1 filter:

```text
Active Status
```

Do not build a complex CRM-style filter system.

---

# 39. Default Client List

Default view should normally show:

```text
Active Clients
```

The user may choose to include inactive Clients.

This keeps normal operational lists clean.

---

# 40. Client Workspace

Version 1 may use a simple Client details page rather than a complex Workspace.

A useful structure:

```text
Client Header

General Information

Projects
```

This allows the user to see Projects associated with the Client without introducing CRM complexity.

---

# 41. Client Header

Example:

```text
CL-000125
Samsung Armenia
```

Business ID should be visible.

---

# 42. Client Projects Section

The Client details page may show Projects belonging to the Client.

Example:

| Project ID | Project | Status |
|---|---|---|
| PRJ-2026-000015 | Store Opening | Active |
| PRJ-2026-000041 | Exhibition | Completed |

Projects remain owned by the Projects module.

The Clients module does not duplicate Project data.

---

# 43. Create Project from Client

A future convenient action may be:

```text
Create Project
```

from the Client details page.

If implemented, the new Project automatically uses the current Client.

This is a UI convenience, not a change in data ownership.

---

# 44. Client Permissions

Initial Clients module Permissions:

```text
clients.view

clients.create

clients.edit

clients.activate
```

---

# 45. clients.view

Allows:

- View Client list
- Search Clients
- View Client details

---

# 46. clients.create

Allows:

```text
Create Client
```

---

# 47. clients.edit

Allows editing Client information.

It does not automatically imply permission to deactivate Clients.

---

# 48. clients.activate

Allows:

```text
Activate Client

Deactivate Client
```

---

# 49. No clients.delete Permission

Version 1 does not require:

```text
clients.delete
```

because normal Client removal uses deactivation.

This keeps permissions aligned with actual functionality.

---

# 50. API Responsibilities

The Clients API may conceptually provide:

```text
Get Clients

Get Client

Create Client

Update Client

Activate Client

Deactivate Client
```

Exact REST routes are defined during implementation.

---

# 51. Client Response DTO

A normal Client response may contain:

```text
id

business_id

name

contact_person

phone

email

address

notes

is_active

created_at

updated_at
```

Internal audit User details may be included only when useful.

---

# 52. Client List DTO

Client list endpoints should return only fields required by the list.

Example:

```text
id

business_id

name

contact_person

phone

email

is_active
```

Do not transfer unnecessary data.

---

# 53. Create Client Request

Conceptual request:

```text
name

contact_person

phone

email

address

notes

is_active
```

Do not accept:

```text
business_id
```

from normal Client creation requests.

The backend generates it.

---

# 54. Update Client Request

Editable fields:

```text
name

contact_person

phone

email

address

notes

is_active
```

Business ID is not editable.

---

# 55. Validation Rules

Backend must validate:

- Name is required
- Name is not empty after trimming
- Business ID is unique
- Email format is reasonable if provided
- Client exists before update
- Current User has required Permission

Do not create excessive validation for optional contact information.

---

# 56. Email Validation

Email validation should be practical rather than excessively strict.

If provided, reject obviously invalid values.

Do not attempt to fully validate whether an email address actually exists.

---

# 57. Phone Validation

Phone validation should remain permissive.

The system may trim unnecessary leading/trailing whitespace.

Do not reject legitimate international formats unnecessarily.

---

# 58. Business ID Generation

Client Business IDs follow:

```text
05_Numbering_System.md
```

Format:

```text
CL-000001
```

Client sequence does not reset annually.

Generation must be concurrency-safe.

---

# 59. Required Indexes

Primary key:

```text
PRIMARY KEY (id)
```

Business ID:

```text
UNIQUE INDEX (business_id)
```

Additional indexes should be added only when actual query behavior requires them.

---

# 60. Client Name Index

A normal index on:

```text
name
```

may be introduced if Client search performance requires it.

Do not prematurely introduce advanced full-text search.

---

# 61. Audit Relationships

`created_by` and `updated_by` reference:

```text
auth.users.id
```

These fields identify the authenticated User who performed the ERP action.

They do not reference Employees.

---

# 62. Delete Behavior from Authentication

If an audit User is ever physically removed through exceptional maintenance, Client records must not be deleted.

Audit relationships should never cascade-delete Clients.

---

# 63. Client Business Data Ownership

The Clients module owns:

```text
Client Name

Contact Person

Phone

Email

Address

Notes

Active Status
```

Projects owns Project-specific information.

Orders owns Order-specific information.

Do not copy Client information into those modules merely for convenience.

---

# 64. Client Name Changes

If a Client changes its name, Version 1 updates the current Client record.

Existing Projects continue referencing the same Client ID.

Version 1 does not maintain historical Client-name snapshots.

If legal document requirements later need historical name snapshots, that should be designed explicitly.

---

# 65. Contact Person Changes

Version 1 stores only the current main Contact Person.

Historical Contact Person tracking is not required.

---

# 66. Multiple Addresses

Version 1 stores one general Client address.

Do not create:

```text
billing_address

shipping_address

installation_addresses
```

until a real business requirement exists.

Project-specific installation locations may later belong to Projects rather than Client master data.

---

# 67. VAT / Tax Information

Version 1 does not require Client VAT or tax registration data.

Do not add tax/accounting fields until the Finance or invoicing requirements are designed.

---

# 68. Payment Terms

Payment terms are outside the initial Clients scope.

Do not add:

```text
credit_limit

payment_terms

account_balance
```

in Version 1.

---

# 69. CRM Data

Do not add:

```text
lead_status

sales_stage

last_contact

next_followup

marketing_source
```

to Clients.

Lithograph ERP Version 1 is not a CRM.

---

# 70. Files and Attachments

Version 1 does not require Client file attachments.

Production folder references belong primarily to Orders.

Do not create a generic Client document-management system.

---

# 71. Client Logo

Version 1 does not require Client logos or profile images.

Add only if later UI requirements demonstrate value.

---

# 72. Client Categories

Version 1 does not require categories such as:

```text
VIP

Corporate

Retail

Government
```

Do not introduce classification without a real reporting/workflow requirement.

---

# 73. Individual Customer Handling

An individual person may be entered directly as Client Name.

Example:

```text
Name:
Armen Petrosyan
```

`contact_person` may remain empty.

No separate Person entity is required.

---

# 74. Company Customer Handling

Example:

```text
Name:
Samsung Armenia

Contact Person:
Anna Martirosyan
```

The same Client structure supports this scenario.

---

# 75. Client Selection in Projects

When creating a Project, the Client selector should normally show:

```text
Business ID + Name
```

Example:

```text
CL-000125 — Samsung Armenia
```

This makes similar Client names easier to distinguish.

---

# 76. Inactive Client in Historical Views

Historical Projects should continue displaying their Client even if:

```text
is_active = false
```

Inactive status must not break historical relationships.

---

# 77. New Project Validation

Backend should reject creation of a new Project for an inactive Client unless a future documented rule explicitly allows it.

The UI should normally exclude inactive Clients from the selector.

---

# 78. Navigation

Clients should appear in the primary application navigation.

Example:

```text
Dashboard

Clients

Projects

Orders

Reports

Administration
```

Client management is normal daily business functionality, not administration.

---

# 79. Version 1 Non-Goals

The Clients module does not include:

```text
CRM

Sales Pipeline

Lead Management

Marketing

Client Portal

Multiple Contacts

Contact History

Multiple Addresses

Contracts

Invoices

Account Balance

Payment Terms

Credit Limits

Tax/VAT Records

Client Documents

Client Logos

Client Categories
```

These may be designed later when needed.

---

# 80. Future Extensions

Possible future additions may include:

- Multiple contacts
- Multiple addresses
- Tax information
- CRM data
- Contract information
- Customer portal access
- Client-specific pricing rules

Future possibilities must not complicate Version 1.

---

# 81. Testing Requirements

Important Client tests should include:

```text
Create Client

Generate Business ID

Business ID is unique

Business ID cannot be edited

Create Client with only Name

Create Client with optional contact fields

Deactivate Client

Inactive Client remains stored

Reactivate Client

Inactive Client excluded from new Project selection

Existing Projects remain linked after Client deactivation

Client deletion is not available as normal Version 1 action
```

---

# 82. Version 1 Schema Summary

```text
clients.clients

id
business_id
name
contact_person
phone
email
address
notes
is_active
created_at
created_by
updated_at
updated_by
```

One table is sufficient for Version 1.

---

# 83. Module Simplicity Rule

Before adding another Client table or field, ask:

```text
Does this information help Lithograph manage current Clients, Projects, or Orders?
```

If not, do not add it yet.

---

# 84. Final Client Principle

The Clients module should answer:

```text
Who is the customer?
```

and provide enough basic contact information to create and manage Projects.

It should remain a simple Client master database rather than evolve prematurely into CRM or accounting software.

---

**End of Document**