# Lithograph ERP

**Document:** 19_API_Design_Guidelines.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `01_Technology_Stack.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `04_Data_Dictionary.md`
- `06_UI_UX_Principles.md`
- `07_Authentication.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`

---

# 1. Purpose

This document defines REST API conventions for Lithograph ERP Version 1.

Its purpose is to keep APIs:

```text
Consistent

Predictable

Secure

Easy to use from React

Easy to maintain

Easy for AI coding assistants to extend correctly
```

Every module should follow the same general API conventions unless a documented business requirement requires otherwise.

---

# 2. API Style

Lithograph ERP Version 1 uses:

```text
REST
+
JSON
+
HTTPS
```

The backend is implemented with:

```text
ASP.NET Core Web API
```

The frontend consumes the API using:

```text
React + TypeScript
```

---

# 3. Base API Prefix

Version 1 API routes should use a common prefix.

Recommended:

```text
/api
```

Examples:

```text
/api/auth/login

/api/users

/api/employees

/api/clients

/api/projects

/api/orders
```

---

# 4. API Versioning

Version 1 does not require complex API versioning.

Do not automatically introduce:

```text
/api/v1/
```

unless external integrations or backward compatibility requirements appear.

The ERP frontend and backend are deployed together.

---

# 5. Resource Naming

Use plural nouns for resource collections.

Good:

```text
/api/clients

/api/projects

/api/orders

/api/employees
```

Avoid:

```text
/api/client

/api/getOrders

/api/projectManager
```

---

# 6. Route Naming

Use:

```text
lowercase
```

and:

```text
kebab-case
```

for multi-word route segments.

Examples:

```text
/api/order-types

/api/calculator-templates

/api/project-members
```

Avoid PascalCase in URLs.

---

# 7. HTTP Methods

Use HTTP methods according to normal REST semantics.

```text
GET
Read

POST
Create or execute explicit command

PUT
Full replacement when genuinely appropriate

PATCH
Partial update when used consistently

DELETE
Remove simple child/relationship records
```

Version 1 will primarily use:

```text
GET
POST
PATCH
DELETE
```

---

# 8. Resource Creation

Create a new resource with:

```text
POST
```

Example:

```text
POST /api/clients
```

---

# 9. Resource Retrieval

Retrieve a collection:

```text
GET /api/clients
```

Retrieve one entity:

```text
GET /api/clients/{id}
```

---

# 10. Resource Update

Use:

```text
PATCH /api/clients/{id}
```

for normal partial edits when appropriate.

Example editable fields:

```text
name
contact_person
phone
email
```

Do not require the client to resend immutable fields.

---

# 11. Delete

Use DELETE only when actual physical removal is valid.

Examples:

```text
DELETE /api/orders/{orderId}/checklist-items/{itemId}

DELETE /api/orders/{orderId}/folder-links/{linkId}
```

Do not expose DELETE for long-lived business records that use deactivation or lifecycle statuses.

---

# 12. Long-Lived Record Removal

Use explicit business actions instead of DELETE.

Example Client:

```text
POST /api/clients/{id}/deactivate
```

or another consistently implemented command-style endpoint.

Likewise:

```text
POST /api/employees/{id}/deactivate

POST /api/users/{id}/deactivate
```

---

# 13. Why Explicit Action Endpoints Are Allowed

Some business actions are not simple CRUD operations.

Examples:

```text
Deactivate User

Reset Password

Publish Template Version

Change Project Status

Assign Project Owner
```

Command-style endpoints are acceptable when they represent explicit business operations.

---

# 14. Action Route Style

Use clear verbs only for actions that are genuinely commands.

Examples:

```text
POST /api/users/{id}/reset-password

POST /api/users/{id}/deactivate

POST /api/calculator-templates/{id}/versions/{versionId}/publish
```

Avoid verbs for ordinary CRUD.

Bad:

```text
GET /api/getClients

POST /api/createOrder
```

---

# 15. Entity IDs in Routes

API routes normally use internal UUID identifiers.

Example:

```text
GET /api/orders/{id}
```

Business IDs remain searchable and visible to users.

Do not use Business ID as the internal primary API identifier everywhere unless there is a specific need.

---

# 16. Business ID Search

Support searching by Business ID through query parameters.

Example:

```text
GET /api/orders?search=ORD-2026-000425
```

or dedicated lookup only where useful.

Do not duplicate complete APIs based on Business ID.

---

# 17. DTO Principle

Do not expose EF Core entities directly from API controllers.

Use DTOs.

Examples:

```text
ClientListDto

ClientDetailDto

CreateClientRequest

UpdateClientRequest
```

DTOs protect:

- Security
- API stability
- Module boundaries
- Frontend contracts

---

# 18. Request DTOs

Request DTOs should contain only fields the client may control.

Example:

```text
CreateClientRequest

name
contact_person
phone
email
address
notes
is_active
```

Do not allow the client to submit:

```text
id

business_id

created_at

created_by
```

when those values are system-generated.

---

# 19. Response DTOs

Response DTOs may contain:

```text
id

business_id

name

status

related display data
```

depending on screen requirements.

Avoid returning entire object graphs unnecessarily.

---

# 20. List DTO vs Detail DTO

Prefer smaller DTOs for lists.

Example:

```text
OrderListDto
```

may contain:

```text
id
business_id
name
client_name
project_name
order_type_name
status
priority
deadline
```

while:

```text
OrderDetailDto
```

may contain richer data.

---

# 21. Do Not Overfetch

Do not return:

```text
Full Client
Full Project
Full Project Team
Full Checklist
Full Calculator
All Costs
```

for every Order row in a list.

Return only what the screen needs.

---

# 22. Related Entity Representation

For simple related entities, use compact nested DTOs.

Example:

```text
client:
{
    id,
    business_id,
    name
}
```

Avoid exposing raw foreign-key-only information when the UI clearly needs a display name.

---

# 23. Foreign Keys in Requests

Create/update requests normally send IDs.

Example:

```text
project_id

order_type_id
```

Backend validates that referenced records exist and are valid for the requested operation.

---

# 24. Authentication Endpoint

Initial login endpoint:

```text
POST /api/auth/login
```

Request:

```text
username

password
```

Response depends on the selected secure Session implementation.

Never return password-related information.

---

# 25. Current User Endpoint

Provide an endpoint conceptually equivalent to:

```text
GET /api/auth/me
```

It may return:

```text
id

username

roles

permissions

linked_employee
```

as required by the frontend.

---

# 26. Logout

Use:

```text
POST /api/auth/logout
```

Logout is a command because it changes Session state.

---

# 27. Authorization

Every protected endpoint must enforce authorization on the backend.

The frontend is not a security boundary.

Example:

```text
POST /api/orders
```

requires:

```text
orders.create
```

---

# 28. Permission Mapping

Endpoint permission requirements should be explicit and easy to locate.

Avoid hidden authorization behavior spread unpredictably throughout the codebase.

---

# 29. Director Access

Director full-access behavior applies to API authorization as defined by Authentication documentation.

Do not add endpoint-specific exceptions for Director unless required by the centralized authorization model.

---

# 30. Authentication Failure

Unauthenticated request:

```text
401 Unauthorized
```

---

# 31. Authorization Failure

Authenticated User without required permission:

```text
403 Forbidden
```

Do not return:

```text
404
```

merely to hide every authorization failure unless a specific security requirement warrants it.

---

# 32. Validation Failure

Invalid request data should normally return:

```text
400 Bad Request
```

with a structured validation response.

---

# 33. Resource Not Found

When a requested entity does not exist:

```text
404 Not Found
```

---

# 34. Conflict

Use:

```text
409 Conflict
```

for business/data conflicts where appropriate.

Examples:

```text
Duplicate username

Duplicate Business ID due to exceptional conflict

Second Project Owner

Concurrent modification conflict
```

---

# 35. Successful Creation

Successful resource creation should normally return:

```text
201 Created
```

with the created resource or useful representation.

Where practical include a Location header.

---

# 36. Successful Update

Normal successful update may return:

```text
200 OK
```

with updated DTO.

Avoid returning empty responses when the frontend immediately needs the updated state.

---

# 37. Successful Delete

Deleting a simple sub-record may return:

```text
204 No Content
```

or a consistent `200` response if UI requires returned state.

Choose one convention and keep it consistent.

---

# 38. Error Response Format

Use one consistent structure.

Conceptual format:

```text
{
    "code": "CLIENT_INACTIVE",
    "message": "Cannot create a Project for an inactive Client.",
    "errors": null
}
```

Validation example:

```text
{
    "code": "VALIDATION_ERROR",
    "message": "One or more fields are invalid.",
    "errors": {
        "name": [
            "Name is required."
        ]
    }
}
```

---

# 39. Error Code

`code` is a stable application-readable identifier.

Examples:

```text
VALIDATION_ERROR

NOT_FOUND

FORBIDDEN

USERNAME_ALREADY_EXISTS

FINAL_DIRECTOR_REQUIRED

CLIENT_INACTIVE

ORDER_TYPE_INACTIVE
```

Do not require the frontend to parse English error messages.

---

# 40. Error Message

`message` is human-readable.

Example:

```text
This username is already in use.
```

It may be shown directly to the User when appropriate.

---

# 41. Technical Errors

Unexpected server errors should return a generic message.

Example:

```text
An unexpected error occurred.
```

Do not expose:

```text
Stack traces

SQL queries

Connection strings

Internal file paths

Exception class internals
```

to the frontend.

---

# 42. Logging

Detailed server exceptions belong in backend logs.

Logs should contain enough technical information for troubleshooting without storing secrets.

---

# 43. Validation Location

Validation may occur at several levels:

```text
Frontend
Backend DTO validation
Business logic
Database constraints
```

Backend remains authoritative.

---

# 44. Business Validation

Some rules are not simple field validation.

Examples:

```text
Client must be active

Employee must be active for new Project assignment

Only one Project Owner

Order Type must be active

Published Template Version is immutable
```

These rules belong in backend business logic.

---

# 45. Database Constraints

Important invariants should also be protected by database constraints when practical.

Examples:

```text
Unique username

Unique Business ID

One Calculator per Order

Unique Template Version number
```

Application validation provides better messages.

Database constraints provide final integrity protection.

---

# 46. Pagination

Large list endpoints should support server-side pagination.

Example:

```text
GET /api/orders?page=1&page_size=50
```

---

# 47. Default Page Size

Recommended default:

```text
50
```

Exact values may be adjusted based on UX.

---

# 48. Maximum Page Size

Set a reasonable maximum.

Example:

```text
200
```

Do not allow clients to request unlimited rows through normal list endpoints.

---

# 49. Pagination Response

Conceptual structure:

```text
{
    "items": [...],
    "page": 1,
    "page_size": 50,
    "total_items": 1384,
    "total_pages": 28
}
```

Use one shared pagination model across modules.

---

# 50. Filtering

Use query parameters for simple filters.

Example:

```text
GET /api/orders?status=active&priority=urgent
```

---

# 51. Date Filters

Example:

```text
GET /api/orders?deadline_from=2026-10-01&deadline_to=2026-10-31
```

Use explicit names.

Avoid ambiguous:

```text
from
to
```

without context when several dates are possible.

---

# 52. Relationship Filters

Examples:

```text
client_id

project_id

order_type_id

owner_employee_id

assignee_employee_id
```

Use UUID values internally.

---

# 53. Search

Use a general `search` parameter where practical.

Example:

```text
GET /api/orders?search=ORD-2026-000425
```

The backend may search approved fields such as:

```text
Business ID

Name

Project Name

Client Name
```

depending on the resource.

---

# 54. Search Scope

Do not make every generic search scan every database field.

Search only meaningful indexed or searchable business fields.

---

# 55. Sorting

List endpoints may support:

```text
sort_by

sort_direction
```

Example:

```text
GET /api/orders?sort_by=deadline&sort_direction=asc
```

---

# 56. Allowed Sort Fields

Backend must whitelist allowed sort fields.

Do not directly inject arbitrary query parameter strings into SQL or dynamic expressions without validation.

---

# 57. Sort Direction

Allowed:

```text
asc

desc
```

Invalid values should be rejected or safely defaulted consistently.

---

# 58. Default Sorting

Each module may define sensible default sorting.

Examples:

Clients:

```text
name
```

Projects:

```text
created_at desc
```

Orders:

```text
created_at desc
```

Exact defaults may be adjusted after use.

---

# 59. Combined List Query

Example:

```text
GET /api/orders
    ?search=panel
    &status=active
    &order_type_id={uuid}
    &sort_by=deadline
    &sort_direction=asc
    &page=1
    &page_size=50
```

---

# 60. Filtering Implementation

Start with direct typed filter parameters.

Do not create a generic expression language or OData-style dynamic query system in Version 1.

---

# 61. Client API

Conceptual routes:

```text
GET    /api/clients
GET    /api/clients/{id}
POST   /api/clients
PATCH  /api/clients/{id}

POST   /api/clients/{id}/activate
POST   /api/clients/{id}/deactivate
```

---

# 62. Employee API

Conceptual routes:

```text
GET    /api/employees
GET    /api/employees/{id}
POST   /api/employees
PATCH  /api/employees/{id}

POST   /api/employees/{id}/activate
POST   /api/employees/{id}/deactivate

POST   /api/employees/{id}/link-user
DELETE /api/employees/{id}/user-link
```

---

# 63. User Administration API

Conceptual routes:

```text
GET    /api/users
GET    /api/users/{id}
POST   /api/users
PATCH  /api/users/{id}

POST   /api/users/{id}/activate
POST   /api/users/{id}/deactivate

POST   /api/users/{id}/reset-password
```

---

# 64. User Role API

Possible routes:

```text
POST   /api/users/{userId}/roles/{roleId}

DELETE /api/users/{userId}/roles/{roleId}
```

This clearly represents a relationship.

---

# 65. Role API

Conceptual routes:

```text
GET    /api/roles
GET    /api/roles/{id}
POST   /api/roles
PATCH  /api/roles/{id}
```

Role permission relationships:

```text
POST   /api/roles/{roleId}/permissions/{permissionId}

DELETE /api/roles/{roleId}/permissions/{permissionId}
```

---

# 66. Permission API

Permissions are application-defined.

Normal API should primarily provide:

```text
GET /api/permissions
```

for Role administration.

Do not expose normal arbitrary Permission creation unless explicitly required.

---

# 67. Project API

Conceptual routes:

```text
GET    /api/projects
GET    /api/projects/{id}
POST   /api/projects
PATCH  /api/projects/{id}

POST   /api/projects/{id}/status
```

---

# 68. Project Team API

Because Project Team roles have business rules, explicit endpoints are acceptable.

Examples:

```text
PUT /api/projects/{id}/owner

PUT /api/projects/{id}/assignee

POST /api/projects/{id}/participants/{employeeId}

DELETE /api/projects/{id}/participants/{employeeId}

POST /api/projects/{id}/observers/{employeeId}

DELETE /api/projects/{id}/observers/{employeeId}
```

---

# 69. Why Owner Uses PUT

Owner is conceptually a single assignable relationship.

Example:

```text
PUT /api/projects/{id}/owner
```

request:

```text
employee_id
```

sets/replaces the current Owner.

---

# 70. Clearing Owner

If Project Owner is optional in Draft state, a dedicated operation may clear it.

Possible:

```text
DELETE /api/projects/{id}/owner
```

Use consistently.

---

# 71. Order API

Conceptual routes:

```text
GET    /api/orders
GET    /api/orders/{id}
POST   /api/orders
PATCH  /api/orders/{id}

POST   /api/orders/{id}/status
```

---

# 72. Order Checklist API

Nested resource:

```text
GET    /api/orders/{orderId}/checklist-items

POST   /api/orders/{orderId}/checklist-items

PATCH  /api/orders/{orderId}/checklist-items/{itemId}

DELETE /api/orders/{orderId}/checklist-items/{itemId}
```

---

# 73. Checklist Completion

Completion may be updated through normal PATCH:

```text
{
    "is_completed": true
}
```

A dedicated endpoint is unnecessary unless UI/business logic later benefits from it.

---

# 74. Checklist Reordering

For reorder operation, use a dedicated endpoint if multiple rows must change atomically.

Example:

```text
POST /api/orders/{orderId}/checklist-items/reorder
```

Request concept:

```text
[
    {
        "id": "...",
        "sort_order": 10
    },
    {
        "id": "...",
        "sort_order": 20
    }
]
```

---

# 75. Folder Links API

Nested resource:

```text
GET    /api/orders/{orderId}/folder-links

POST   /api/orders/{orderId}/folder-links

PATCH  /api/orders/{orderId}/folder-links/{linkId}

DELETE /api/orders/{orderId}/folder-links/{linkId}
```

---

# 76. Order Types API

Conceptual routes:

```text
GET    /api/order-types
GET    /api/order-types/{id}
POST   /api/order-types
PATCH  /api/order-types/{id}

POST   /api/order-types/{id}/activate
POST   /api/order-types/{id}/deactivate
```

---

# 77. Calculator Template API

Conceptual routes:

```text
GET   /api/calculator-templates

GET   /api/calculator-templates/{id}

POST  /api/calculator-templates

PATCH /api/calculator-templates/{id}
```

---

# 78. Calculator Template Versions API

Conceptual routes:

```text
GET  /api/calculator-templates/{templateId}/versions

GET  /api/calculator-templates/{templateId}/versions/{versionId}

POST /api/calculator-templates/{templateId}/versions
```

Creating a new version normally creates a Draft.

---

# 79. Template Draft Update

A Draft Version may be updated:

```text
PATCH /api/calculator-templates/{templateId}/versions/{versionId}
```

Published Versions must reject modification.

---

# 80. Template Validation

Explicit action:

```text
POST /api/calculator-templates/{templateId}/versions/{versionId}/validate
```

This may return:

```text
is_valid

errors
```

---

# 81. Template Publication

Explicit action:

```text
POST /api/calculator-templates/{templateId}/versions/{versionId}/publish
```

Publication must:

```text
Validate

Reject invalid Draft

Set Published state

Set published_at

Set published_by
```

---

# 82. Order Calculator API

Conceptual routes:

```text
GET /api/orders/{orderId}/calculator
```

The backend may lazily create the Calculator if it does not yet exist and a valid Template is available.

---

# 83. Calculator Save

Possible:

```text
PATCH /api/orders/{orderId}/calculator
```

Request contains editable field values.

Backend:

```text
Validates

Calculates

Stores field values

Updates Selling Price

Returns calculated results
```

---

# 84. Calculator Calculation Preview

If needed, a non-persisting calculation endpoint may be:

```text
POST /api/orders/{orderId}/calculator/calculate
```

This can calculate values without saving.

Only implement if Template/Calculator UX needs it.

---

# 85. Cost API

Nested under Order:

```text
GET    /api/orders/{orderId}/cost-items

POST   /api/orders/{orderId}/cost-items

PATCH  /api/orders/{orderId}/cost-items/{costItemId}

DELETE /api/orders/{orderId}/cost-items/{costItemId}
```

---

# 86. Cost Update Behavior

Every Cost mutation must safely recalculate:

```text
Order Cost Price
```

The API should return updated Cost totals where useful.

---

# 87. Reports API

Reports are read-only.

Examples:

```text
GET /api/reports/orders

GET /api/reports/projects

GET /api/reports/clients

GET /api/reports/order-types

GET /api/reports/costs
```

Filters use query parameters.

---

# 88. Report Summary Response

Conceptual response:

```text
{
    "items": [...],
    "summary": {
        "count": 125,
        "selling_total": 1000000,
        "cost_total": 600000,
        "profit_total": 400000
    }
}
```

Financial fields must respect permissions.

---

# 89. Financial DTO Filtering

If User lacks Cost permission, the API must not serialize:

```text
cost_price

cost_total

profit
```

merely with null values if that still reveals sensitive structure unnecessarily.

Prefer DTOs or projection logic that respects authorization.

---

# 90. Do Not Trust Frontend Fields

The frontend must never be able to force values that the backend owns.

Examples:

```text
business_id

created_by

updated_by

password_hash

profit

template_version publication state

calculated final prices
```

Backend derives or validates these.

---

# 91. Server-Generated Fields

Typical server-generated fields include:

```text
id

business_id

created_at

created_by

updated_at

updated_by
```

Do not accept these from ordinary create/update requests.

---

# 92. Business ID Generation

Business ID generation occurs inside backend/domain/database logic.

Never rely on frontend numbering.

---

# 93. Idempotency

Normal Version 1 API does not require general idempotency-key infrastructure.

Where duplicate submission could be harmful, the UI should prevent duplicate clicks and backend constraints should protect integrity.

---

# 94. Duplicate Create Requests

Business ID gaps resulting from retried or failed transactions are acceptable.

Do not try to reuse skipped numbers.

---

# 95. Concurrency

Important editable resources may require optimistic concurrency.

Most important candidate:

```text
Order Calculator
```

because simultaneous editing could overwrite values.

---

# 96. Concurrency Response

When a stale update is detected, return:

```text
409 Conflict
```

with a clear message.

Example:

```text
This Calculator was modified by another User. Reload the latest data before saving.
```

---

# 97. ETags

Version 1 does not require HTTP ETag infrastructure unless chosen as the simplest concurrency implementation.

A database/application concurrency token is sufficient.

---

# 98. Transactions

Use transactions for operations involving multiple dependent updates.

Examples:

```text
Create Project + Project Team

Update Cost Item + Cost Price

Save Calculator + Selling Price

Change Owner relationship
```

---

# 99. Transaction Boundary Rule

Do not create one giant transaction across unrelated UI operations.

Transactions should protect one coherent business operation.

---

# 100. Nested Resource Validation

When route contains both parent and child IDs:

```text
/api/orders/{orderId}/checklist-items/{itemId}
```

backend must verify:

```text
itemId belongs to orderId
```

Do not update a child merely because its ID exists.

---

# 101. Request Cancellation

ASP.NET Core endpoints should accept request cancellation where practical.

Pass cancellation tokens into EF Core async operations.

---

# 102. Async API

Database/API I/O should use normal asynchronous ASP.NET Core patterns.

Avoid blocking calls around database/network operations.

---

# 103. Database Query Projection

For list endpoints, project directly to DTOs where practical.

Do not load huge entity graphs and then discard most fields.

---

# 104. N+1 Queries

Avoid N+1 query patterns.

Example:

Do not load 100 Orders and issue one separate database query per Order for Client Name.

Use appropriate joins/projections.

---

# 105. Include Usage

EF Core `Include` is useful but should not be applied blindly.

Prefer projections for read-heavy list endpoints.

---

# 106. Read-Only Queries

Use no-tracking queries for read-only operations where appropriate.

This reduces unnecessary EF Core tracking overhead.

---

# 107. Raw SQL

Start with EF Core LINQ.

Use raw SQL only when:

```text
Query is genuinely difficult

Performance measurement demonstrates a need

The SQL remains parameterized and reviewed
```

Never concatenate user input into SQL.

---

# 108. SQL Injection

All database access must use parameterized EF Core/query mechanisms.

Never generate SQL by string concatenation from:

```text
Search terms

Sort parameters

Filters

Formula values
```

---

# 109. Mass Assignment

Do not bind request DTOs directly into entities in a way that allows unexpected fields to be modified.

Map explicitly.

---

# 110. API Security Logging

Never log:

```text
Passwords

Password hashes

Raw Session tokens

Authentication cookies

Sensitive Calculator Cost data unnecessarily
```

---

# 111. Sensitive Response Caching

Authentication and sensitive financial responses should not be accidentally cached by shared/public caches.

Version 1 does not need a complex caching layer.

---

# 112. CORS

Production should permit only approved frontend origins.

Do not leave unrestricted CORS in production.

---

# 113. HTTPS

Production must use HTTPS.

Authentication credentials and Sessions must not travel over unencrypted HTTP.

---

# 114. Request Size

Set reasonable request limits.

Calculator Template JSON may be larger than ordinary DTOs but should still remain bounded.

Do not allow unlimited payload sizes.

---

# 115. File Uploads

Version 1 may eventually need Preview Image upload.

If implemented, file upload should use a dedicated endpoint rather than embedding large Base64 image strings inside ordinary Order JSON.

---

# 116. Preview Image Endpoint

Possible design:

```text
POST /api/orders/{id}/preview-image
```

The backend stores the file according to the approved storage strategy and updates the Order's image reference.

Exact implementation may be deferred until file storage is defined.

---

# 117. File Validation

If image upload is implemented, validate:

```text
File size

Allowed content types

Actual file content where practical
```

Do not trust filename extension alone.

---

# 118. Folder Paths

Folder Links are plain text business data.

API does not access the filesystem path.

Example request:

```text
{
    "name": "Artwork",
    "path": "\\\\server\\orders\\ORD-2026-000425\\artwork"
}
```

Backend stores the path.

It does not open or verify the network folder in Version 1.

---

# 119. Date Format

API dates use ISO 8601.

Date-only example:

```text
2026-11-30
```

Timestamp example:

```text
2026-09-26T18:03:00Z
```

---

# 120. UTC Timestamps

API timestamp values should use UTC where practical.

Frontend handles user-facing local display.

---

# 121. Money Representation

JSON numeric values should represent decimal money amounts.

Example:

```text
125000.00
```

Backend uses decimal-safe financial types.

---

# 122. Null Values

Use null only where the business field is genuinely optional.

Examples:

```text
deadline: null

phone: null

calculator_template_id: null
```

Avoid empty strings as substitutes for every null field.

---

# 123. Empty Collections

For collections, return:

```text
[]
```

rather than:

```text
null
```

when there are zero items.

---

# 124. Boolean Naming

Use positive names.

Good:

```text
is_active

is_completed
```

Avoid confusing double-negative names.

---

# 125. Status Values

API status values should use one consistent representation.

Recommended JSON form:

```text
"active"
"completed"
"on_hold"
```

or another explicitly defined format.

Do not mix:

```text
Active
active
ACTIVE
```

across endpoints.

---

# 126. Enum Serialization

If enums are serialized as strings, configure this consistently.

Frontend TypeScript types should use matching stable values.

---

# 127. Project Roles

API Project Role values should also use one stable representation.

Conceptually:

```text
owner

assignee

participant

observer
```

Internal database/application mapping must be consistent.

---

# 128. API Documentation

Development environment should expose OpenAPI/Swagger.

Swagger helps:

- Developers
- Cursor/Claude
- Testing
- Frontend integration

---

# 129. Swagger Security

Production Swagger exposure should be deliberate.

Do not expose development tooling publicly by accident.

---

# 130. API Contract Stability

When frontend depends on an API contract, avoid casually renaming fields.

If a rename is necessary:

```text
Update backend

Update frontend

Update tests

Update documentation
```

in the same change.

---

# 131. TypeScript API Types

Frontend TypeScript types should match backend contracts.

Avoid maintaining several manually inconsistent versions of the same DTO.

If code generation later proves useful, it may be introduced.

It is not required initially.

---

# 132. API Client Layer

Frontend should use a shared API client layer.

Do not scatter raw `fetch()` logic throughout every React component.

Conceptually:

```text
api/
├── auth.ts
├── clients.ts
├── projects.ts
├── orders.ts
└── calculator.ts
```

or equivalent module structure.

---

# 133. Frontend Error Handling

The shared API layer should convert backend error responses into predictable frontend errors.

Components should not parse arbitrary server responses independently.

---

# 134. Loading State

Each mutating API action should expose a loading state to the UI.

Prevent accidental duplicate actions such as:

```text
Create Order
Create Order
Create Order
```

from repeated clicks.

---

# 135. Cancellation of Search Requests

For fast-changing searches or filters, the frontend may cancel obsolete requests.

Do not allow stale search responses to overwrite newer results.

---

# 136. API Testing

Important API tests should cover:

```text
Authentication

Authorization

Validation

Successful CRUD

Not Found

Conflict

Pagination

Filtering

Sorting

Cross-module business rules

Financial field protection
```

---

# 137. Integration Tests

Use integration tests for critical endpoint/database flows.

Examples:

```text
Create Client
→ Business ID generated

Create Project
→ Client validation

Assign Owner
→ second Owner rejected

Create Order
→ Project and Order Type validated

Add Cost
→ Order Cost Price updated
```

---

# 138. Authorization Tests

Every protected action should have at least tests covering:

```text
Authorized User succeeds

Unauthorized User gets 403
```

for important features.

---

# 139. Authentication Tests

Test:

```text
Correct login

Wrong password

Inactive User

Logout

Expired Session

Reset Password

Old Sessions invalidated
```

---

# 140. Validation Tests

Test both:

```text
Application validation
```

and important:

```text
Database constraints
```

where appropriate.

---

# 141. No API for Database Internals

Do not expose generic endpoints such as:

```text
/api/database/query

/api/table/{name}

/api/sql
```

The frontend should interact only through business APIs.

---

# 142. No Generic CRUD Controller

Do not create one generic controller capable of editing arbitrary database entities.

Each module should expose explicit business endpoints.

---

# 143. No GraphQL in V1

Version 1 uses REST.

Do not add GraphQL unless a future requirement clearly justifies it.

---

# 144. No OData in V1

Do not add OData or arbitrary query languages.

Typed filters and predefined reporting APIs are sufficient.

---

# 145. No Public API

Version 1 API is internal to Lithograph ERP.

It is not designed as a public developer platform.

Do not create:

```text
Public API Keys

External Developer Accounts

Webhook Platform
```

unless future integration requirements arise.

---

# 146. No Message Broker Requirement

REST API operations are synchronous application interactions.

Do not introduce:

```text
RabbitMQ

Kafka

Azure Service Bus
```

for ordinary Version 1 API operations.

---

# 147. Business Operation Example — Create Order

Conceptual flow:

```text
POST /api/orders

Authenticate User

Authorize orders.create

Validate Project

Validate Project Status

Validate Order Type

Validate Order Type active

Generate Business ID

Create Order

Commit Transaction

Return Order DTO
```

---

# 148. Business Operation Example — Add Cost

```text
POST /api/orders/{orderId}/cost-items

Authenticate

Authorize calculator.edit_costs

Validate Order

Validate amount

Begin Transaction

Insert Cost Item

Calculate Cost Total

Update Order Cost Price

Commit

Return Cost Item + Updated Cost Total
```

---

# 149. Business Operation Example — Save Calculator

```text
PATCH /api/orders/{orderId}/calculator

Authenticate

Authorize calculator.edit

Load Order Calculator

Load exact Template Version

Validate input values

Evaluate formulas

Calculate Selling Price

Begin Transaction

Save field_values

Update Order Selling Price

Commit

Return Calculator Result
```

---

# 150. Business Operation Example — Publish Template

```text
POST /api/calculator-templates/{templateId}/versions/{versionId}/publish

Authenticate

Authorize calculator.publish_templates

Load Draft Version

Validate Definition

Validate Formulas

Check Circular References

Set Published

Set published_at

Set published_by

Commit
```

---

# 151. API Simplicity Rule

Before adding an endpoint, ask:

```text
Is this a real business operation or data retrieval need?
```

If not, do not add it.

---

# 152. API Consistency Rule

Before inventing a new pattern, inspect existing endpoints.

If the same type of action already has an established convention, reuse it.

---

# 153. API Change Rule

If an API change alters documented business behavior:

```text
Update documentation

Update DTOs

Update backend

Update frontend

Update tests
```

Do not let API behavior silently drift from specifications.

---

# 154. Version 1 Non-Goals

API Version 1 does not require:

```text
GraphQL

OData

Public API

API Keys

Webhook Platform

Complex API Versioning

Generic CRUD API

Generic Database Query API

Message Broker

Distributed Transactions

External Developer SDK
```

---

# 155. Final API Principle

Lithograph ERP APIs should make business actions obvious.

A developer should be able to understand:

```text
What resource is being accessed?

What business action is occurring?

Which Permission is required?

What data is accepted?

What data is returned?

What errors are possible?
```

without needing to reverse-engineer hidden conventions.

The main rule is:

```text
Explicit business APIs
over
generic technical APIs.
```

---

**End of Document**