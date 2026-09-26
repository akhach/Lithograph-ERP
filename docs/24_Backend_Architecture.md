# Lithograph ERP

**Document:** 24_Backend_Architecture.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `01_Technology_Stack.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `07_Authentication.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `22_Logging_Audit_and_Operational_History.md`
- `23_Frontend_Architecture.md`

---

# 1. Purpose

This document defines the internal backend architecture for Lithograph ERP Version 1.

The backend is responsible for:

```text
Business rules

Authorization

Database access

Transactions

Validation

REST APIs

Calculation logic

Audit metadata
```

The goal is to keep backend code:

```text
Modular

Understandable

Testable

Simple

Safe to extend
```

---

# 2. Technology

Version 1 backend uses:

```text
C#

ASP.NET Core

Entity Framework Core

PostgreSQL
```

The backend exposes:

```text
REST APIs
```

to the React frontend.

---

# 3. Architectural Style

Version 1 uses:

```text
Modular Monolith
```

All modules run inside one application process.

Modules remain logically separated.

---

# 4. Major Backend Modules

```text
Authentication

Employees

Clients

Projects

Orders

Calculator

Reports
```

These are business modules.

Do not split them into microservices in Version 1.

---

# 5. Backend Organization Principle

Prefer organization by business module.

Example:

```text
Modules/
├── Authentication/
├── Employees/
├── Clients/
├── Projects/
├── Orders/
├── Calculator/
└── Reports/
```

rather than mixing all business code globally by technical type.

---

# 6. Recommended Solution Structure

A reasonable structure:

```text
backend/
├── LithographERP.sln
│
├── src/
│   ├── LithographERP.Api/
│   ├── LithographERP.Application/
│   ├── LithographERP.Domain/
│   └── LithographERP.Infrastructure/
│
└── tests/
    ├── LithographERP.UnitTests/
    └── LithographERP.IntegrationTests/
```

Exact project count may be simplified if this separation creates unnecessary friction.

The architecture must remain understandable.

---

# 7. Simplicity Rule

Do not create extra projects only to satisfy architecture theory.

If a simpler structure provides the same clarity and testability, use it.

---

# 8. Responsibilities Overview

Conceptually:

```text
API
↓
Application
↓
Domain / Business Rules
↓
Infrastructure / EF Core
↓
PostgreSQL
```

Not every operation requires a complex Domain abstraction.

---

# 9. API Layer

The API layer is responsible for:

```text
HTTP routing

Request DTO binding

Authentication context

Authorization attributes/policies

HTTP response mapping
```

It should not contain large amounts of business logic.

---

# 10. Application Layer

The Application layer coordinates business use cases.

Examples:

```text
Create Client

Create Project

Assign Project Owner

Create Order

Save Calculator

Publish Template Version

Add Cost Item
```

---

# 11. Domain Layer

The Domain layer contains core business concepts and rules where they are useful.

Examples:

```text
OrderStatus

ProjectStatus

Priority

ProjectRole

Formula logic

Business invariants
```

Do not force every trivial CRUD entity to become a complex domain aggregate.

---

# 12. Infrastructure Layer

Infrastructure contains:

```text
EF Core

PostgreSQL mappings

Database migrations

Session persistence

File storage implementation

External service adapters if later added
```

Business rules should not be buried in Infrastructure.

---

# 13. Module-Oriented Structure

Within modules, a pattern may look like:

```text
Orders/
├── Domain/
├── Application/
├── Infrastructure/
└── Api/
```

or equivalent.

The exact folders should follow actual complexity.

---

# 14. Feature-Oriented Alternative

For smaller modules, feature grouping may be simpler.

Example:

```text
Clients/
├── CreateClient/
├── UpdateClient/
├── GetClient/
├── ListClients/
└── ClientEntityConfiguration.cs
```

Both approaches are acceptable if consistent.

---

# 15. Avoid Global Technical Dumping Grounds

Do not create large global folders such as:

```text
Services/
Repositories/
Helpers/
Managers/
Utils/
```

containing unrelated module logic.

Keep code close to the business module it belongs to.

---

# 16. Controllers

Controllers should remain thin.

Typical responsibilities:

```text
Receive request

Authorize

Call application service/use case

Return response
```

---

# 17. Controller Non-Responsibilities

Avoid putting in Controllers:

```text
Complex EF Core queries

Pricing formulas

Transaction logic

Business ID generation

Permission calculations

Project Team rules
```

---

# 18. Application Services

Application Services may coordinate:

```text
Validation

Entity loading

Business rules

Transactions

Audit metadata

DTO result creation
```

They should represent meaningful use cases.

---

# 19. Avoid One Giant Service per Module

Do not create:

```text
OrderService
```

with hundreds of unrelated methods if the module becomes large.

Split by meaningful feature/use case when necessary.

---

# 20. Avoid One Class per Trivial Line of Logic

Do not over-fragment every action into excessive command/handler/interface layers.

Use the simplest structure that remains clear.

---

# 21. EF Core

Entity Framework Core is the default data access technology.

Use:

```text
DbContext

DbSet

LINQ

Entity Configuration

Migrations
```

directly where appropriate.

---

# 22. Repository Pattern

Do not automatically create:

```text
IGenericRepository<T>

GenericRepository<T>
```

around EF Core.

This usually adds boilerplate without useful abstraction.

---

# 23. When Repository May Be Useful

A repository may be introduced only when:

```text
A domain operation needs a meaningful persistence abstraction

Complex reusable data access is genuinely repeated

Testing or module boundary clearly benefits
```

Do not introduce repositories by default.

---

# 24. Unit of Work

Do not create a custom Unit of Work abstraction merely to wrap:

```text
DbContext.SaveChangesAsync()
```

EF Core DbContext already acts as a Unit of Work.

---

# 25. DbContext Strategy

Version 1 may use:

```text
One main DbContext
```

for the whole Modular Monolith.

This is acceptable because the system uses one PostgreSQL database.

---

# 26. Module Ownership with One DbContext

One DbContext does not mean modules lose ownership.

Each module should still own:

```text
Its entities

Its configurations

Its business rules

Its write operations
```

---

# 27. Entity Configuration

Use explicit EF Core configuration classes.

Example:

```text
ClientConfiguration

ProjectConfiguration

OrderConfiguration
```

They define:

```text
Schema

Table

Keys

Indexes

Foreign keys

Lengths

Precision

Delete behavior
```

---

# 28. Schema Mapping

Entities must map to approved PostgreSQL schemas:

```text
auth

employees

clients

projects

orders

calculator
```

Do not place all tables into `public`.

---

# 29. Migration Ownership

Migrations belong to the application database.

They may include changes across modules where cross-schema dependencies require it.

Do not create artificial complexity just to force one migration assembly per module.

---

# 30. Business IDs

Business ID generation belongs in backend/database logic.

Examples:

```text
CL-000001

PRJ-2026-000001

ORD-2026-000001
```

Frontend never generates them.

---

# 31. Business ID Service

A small dedicated numbering component/service is appropriate because:

```text
Clients

Projects

Orders
```

all use related numbering behavior.

It should remain narrow in scope.

---

# 32. Number Generation Rules

The numbering component must support:

```text
Atomic generation

Concurrency safety

Yearly reset where required

No reuse of gaps
```

Do not use:

```text
SELECT MAX(number) + 1
```

---

# 33. Transactions

Use database transactions for operations that modify several related records and must remain consistent.

Examples:

```text
Create Project + Team assignments

Save Calculator + Selling Price

Add Cost Item + Cost Price

Replace Project Owner
```

---

# 34. Transaction Simplicity

Do not open explicit transactions for every single-row update.

Use them only when business consistency requires it.

---

# 35. Cross-Module Transactions

Because Version 1 uses one database, transactions may safely span module tables when one business operation requires it.

Example:

```text
Calculator
updates calculator.order_calculators
+
Orders
updates orders.orders.selling_price
```

---

# 36. Cross-Module Write Rule

A module should not casually modify another module's tables directly.

Prefer:

```text
Application interface

Coordinated service

Explicit module method
```

for cross-module business operations.

---

# 37. Orders–Calculator Integration

Calculator owns:

```text
Template logic

Calculator values

Cost Items
```

Orders owns:

```text
Order

Selling Price

Cost Price
```

Calculator may request Orders to update financial values through an explicit interface/service.

---

# 38. Example Interface

Conceptually:

```text
IOrderFinancialUpdater
```

may expose:

```text
UpdateSellingPriceAsync()

UpdateCostPriceAsync()
```

if this improves module separation.

Exact name is not prescribed.

---

# 39. Avoid Circular Service Dependencies

Do not create:

```text
OrdersService → CalculatorService → OrdersService
```

circular dependencies.

Extract a narrow coordination boundary instead.

---

# 40. Module Read Dependencies

Reading data from another module is normal.

Example:

Orders creation needs to validate:

```text
Project exists

Order Type exists
```

This can be done through:

```text
Read interfaces

Queries

DbContext projection
```

while keeping write ownership clear.

---

# 41. Cross-Module Queries

For read-heavy views/reports, it is acceptable to query across schemas efficiently.

Example:

```text
Orders List
joins
Projects
Clients
Order Types
```

Do not force every read through multiple service hops.

---

# 42. Read Model Philosophy

Version 1 may use query-specific projections.

Example:

```text
OrderListDto
```

may be created directly from an EF Core LINQ query.

This is preferable to loading full entities unnecessarily.

---

# 43. Write Model Philosophy

Write operations should validate domain/business rules before persistence.

Do not directly map incoming DTOs to entities and save without rules.

---

# 44. DTO Mapping

Mapping may be manual.

Manual mapping is acceptable and often clearer.

Do not add AutoMapper or another mapping library unless repetition demonstrates clear benefit.

---

# 45. Mass Assignment Protection

Explicitly map allowed request fields.

Do not automatically bind request objects onto persistence entities in a way that permits changing:

```text
business_id

created_by

password_hash

published_at

final calculated values
```

---

# 46. Validation Layers

Backend validation has several forms:

```text
DTO validation

Business validation

Database constraints
```

All may be needed.

---

# 47. DTO Validation

Examples:

```text
Name required

String length

Email basic format

Amount non-negative
```

---

# 48. Business Validation

Examples:

```text
Client must be active

Employee must be active

Only one Owner

Order Type must be active

Published Template immutable
```

---

# 49. Database Validation

Examples:

```text
Unique username

Unique Business ID

One Calculator per Order

Unique Template version
```

Database is the final integrity layer.

---

# 50. Validation Library

Use ASP.NET Core built-in validation or one maintained validation library if useful.

Do not introduce several competing validation systems.

---

# 51. Authorization

Backend authorization is authoritative.

Permissions follow:

```text
module.action
```

examples:

```text
orders.view

projects.manage_team

calculator.edit_costs
```

---

# 52. Authorization Implementation

Use ASP.NET Core authorization policies/handlers or an equivalent centralized mechanism.

Avoid manual permission string checks scattered through Controllers.

---

# 53. Permission Service

A centralized Permission resolution component may determine effective permissions from:

```text
User

Roles

Role Permissions
```

---

# 54. Director

Director full access must be handled centrally.

Do not add:

```text
if (username == "director")
```

throughout business code.

Privilege belongs to Director Role/system authorization behavior.

---

# 55. Authentication vs Employee

Authentication code works with:

```text
User
```

Business responsibility uses:

```text
Employee
```

Do not confuse them.

---

# 56. Audit Fields

Application services set:

```text
created_by

updated_by

assigned_by

published_by
```

from current authenticated User.

Frontend cannot control them.

---

# 57. Current User Context

Provide one shared backend abstraction for current authenticated User identity.

Conceptually:

```text
ICurrentUser
```

may expose:

```text
UserId

Username

IsAuthenticated
```

and possibly permission access.

---

# 58. Do Not Read HTTP Context Everywhere

Business/Application services should not directly depend on raw HTTP context throughout the system.

Use a small abstraction.

---

# 59. Time

Provide a controlled time abstraction where useful.

Important for:

```text
Business ID year

Session expiration

Audit timestamps

Tests
```

---

# 60. Clock Abstraction

Conceptually:

```text
IClock.UtcNow
```

or equivalent.

Do not scatter:

```text
DateTime.Now
```

through business logic.

---

# 61. UTC

Backend timestamps use UTC.

Use timezone-aware DateTime types/handling compatible with PostgreSQL `timestamptz`.

---

# 62. Date-Only Fields

Use proper date-only representation where practical for:

```text
Project start date

Project deadline

Order deadline

Cost expense date
```

Avoid pretending midnight timestamp has business meaning.

---

# 63. Money

Use:

```text
decimal
```

in C# for:

```text
selling_price

cost_price

amount
```

Never use:

```text
float

double
```

for money.

---

# 64. Status Types

Stable statuses should use strongly typed application values.

Examples:

```text
ProjectStatus

OrderStatus

OrderPriority

ProjectRole

TemplateVersionStatus
```

---

# 65. Database Status Storage

Database may store stable string values.

Mapping between C# and PostgreSQL must be explicit and consistent.

---

# 66. Configurable Data

Order Types are entities.

Do not convert them into C# enums.

---

# 67. Formula Engine

Formula Engine belongs to Calculator module.

It must be isolated from arbitrary code execution.

---

# 68. Formula Engine Input

Engine receives only approved values such as:

```text
Field values

Cost total

Template formula definitions
```

It must not receive unrestricted database access.

---

# 69. Formula Parser

Use a controlled parser/evaluator.

Do not use:

```text
eval

Roslyn dynamic compilation

JavaScript execution

SQL
```

to calculate formulas.

---

# 70. Formula Dependency Graph

Calculator module should build/evaluate field dependencies.

It must detect:

```text
Unknown references

Circular references

Invalid formulas
```

before publication/calculation.

---

# 71. Calculator Backend Authority

Frontend may preview calculations.

Backend must independently calculate authoritative results before storing:

```text
Selling Price
```

---

# 72. Cost Price Authority

Cost Price is calculated by backend from:

```text
SUM(cost_items.amount)
```

Do not accept direct arbitrary Cost Price changes through standard Order edit APIs.

---

# 73. Reports Architecture

Reports are read-only query services.

Reports should not have write entities.

---

# 74. Reports Queries

Reports may use efficient cross-module EF Core projections.

Examples:

```text
Client → Projects → Orders

Order → Project → Team

Order → Cost Items
```

---

# 75. Reports Business Logic

Reports may calculate:

```text
Selling totals

Cost totals

Profit

Counts

Averages
```

but must not duplicate source-of-truth data.

---

# 76. Raw SQL for Reports

Start with LINQ.

Use raw SQL only if:

```text
Measured performance requires it

Query is difficult to express

SQL remains parameterized

Tests cover it
```

---

# 77. Database Views

Do not create database views automatically.

Introduce only if repeated reporting queries clearly benefit.

---

# 78. Caching

Version 1 does not require distributed cache.

Do not add:

```text
Redis
```

unless measured need appears.

---

# 79. In-Memory Cache

Small in-memory caching may be used only for safe stable data if genuinely useful.

Do not cache permission-sensitive or rapidly changing data casually.

---

# 80. Sessions

Authentication Sessions are managed by the Authentication module.

Session validation should be centralized.

Business modules should not inspect raw Session tokens.

---

# 81. Password Hashing

Use established ASP.NET Core security/password hashing mechanisms.

Never implement custom cryptography.

---

# 82. Password Reset

Administrative password reset should:

```text
Hash new password

Replace old hash

Invalidate existing Sessions

Update audit metadata
```

---

# 83. Error Handling

Use centralized exception/error handling middleware.

Controllers should not repeat large try/catch blocks for every endpoint.

---

# 84. Expected Business Errors

Expected errors should be represented explicitly.

Examples:

```text
ClientInactive

FinalDirectorRequired

DuplicateUsername

OrderTypeInactive

TemplateAlreadyPublished
```

---

# 85. Unexpected Errors

Unexpected exceptions should:

```text
Be logged

Return safe generic response

Include request/correlation ID
```

Do not expose stack traces.

---

# 86. Domain/Application Exception Strategy

Use a small, understandable exception/result model.

Do not create dozens of exception classes without value.

---

# 87. Result Pattern

A Result type may be used if it genuinely improves consistency.

It is optional.

Do not introduce a complex functional programming framework just for error handling.

---

# 88. Logging

Use structured ASP.NET Core logging.

Backend logs operational events and unexpected failures.

Refer to:

```text
22_Logging_Audit_and_Operational_History.md
```

---

# 89. Logging Sensitive Data

Never log:

```text
Passwords

Password hashes

Raw tokens

Database credentials

Complete sensitive financial payloads
```

---

# 90. Request Logging

Standard request logging is acceptable.

Avoid logging entire request/response bodies globally.

---

# 91. Correlation ID

Backend should propagate a request/correlation identifier for troubleshooting.

---

# 92. API Documentation

Expose OpenAPI/Swagger in development.

API contracts should match:

```text
19_API_Design_Guidelines.md
```

---

# 93. Endpoint Organization

Endpoints may be organized by module/controller.

Examples:

```text
ClientsController

ProjectsController

OrdersController

CalculatorTemplatesController
```

Keep routes business-oriented.

---

# 94. Minimal APIs

ASP.NET Core Minimal APIs are technically acceptable, but do not mix several styles randomly.

Choose one consistent approach.

For this ERP, normal Controllers may be easier to organize and understand.

---

# 95. Background Jobs

Version 1 does not require a general background job system.

Do not add:

```text
Hangfire

Quartz

Message queue workers
```

unless a real recurring/background operation appears.

---

# 96. Session Cleanup

Expired Session cleanup may be handled by:

```text
Login/session logic

Simple scheduled maintenance

Database cleanup
```

Do not introduce an entire job platform solely for this.

---

# 97. File Storage

If Preview Images are implemented, define a small file-storage abstraction.

Conceptually:

```text
IPreviewImageStorage
```

This keeps storage path handling out of Controllers.

---

# 98. File Storage Boundary

Orders owns the Preview Image reference.

Storage implementation handles:

```text
Save

Replace

Delete physical preview file if appropriate

Resolve URL/reference
```

Do not mix production-artwork folder management into this abstraction.

---

# 99. Folder Links

Folder Links are just text data.

Backend does not inspect or open the path.

No Windows filesystem integration belongs in Version 1 backend.

---

# 100. Database Query Rules

Use async EF Core operations.

Examples:

```text
ToListAsync

SingleOrDefaultAsync

SaveChangesAsync
```

Pass cancellation tokens where practical.

---

# 101. No Blocking I/O

Avoid:

```text
.Result

.Wait()
```

around asynchronous database/network operations.

---

# 102. Read Queries

For read-only DTO projections, use:

```text
AsNoTracking()
```

where appropriate.

---

# 103. N+1 Prevention

Review list/report queries for repeated database access.

Avoid:

```text
Load Orders
Then query Project for each Order
Then Client for each Project
```

Use joins/projections.

---

# 104. Entity Loading

For write operations, load only entities/relationships needed for validation and modification.

Do not load huge graphs by default.

---

# 105. Include

Use `Include` when it clearly fits.

Do not blindly add many Includes to every query.

---

# 106. Query Projection

Prefer direct projection for API lists.

Example:

```text
.Select(x => new OrderListDto { ... })
```

can be more efficient than loading full Order objects.

---

# 107. Search

Search logic should remain typed and parameterized.

Do not build dynamic SQL strings.

---

# 108. Sorting

Whitelist allowed sort fields.

Do not blindly execute arbitrary field names from request.

---

# 109. Pagination

Pagination belongs in query/application logic.

Use consistent models across modules.

---

# 110. API DTO Ownership

DTOs belong near the module/use case that exposes them.

Avoid one giant global DTO project containing unrelated records.

---

# 111. Internal Models vs API DTOs

Do not use API DTOs as Domain entities.

Do not use EF entities as API contracts.

Keep boundaries clear.

---

# 112. Internal Interfaces

Introduce interfaces when they represent a useful boundary.

Good examples:

```text
ICurrentUser

IClock

IOrderFinancialUpdater

IPreviewImageStorage
```

Potentially unnecessary examples:

```text
IClientService
IEmployeeService
IProjectService
```

if each has only one implementation and no boundary value.

---

# 113. Dependency Injection

Use ASP.NET Core built-in Dependency Injection.

Do not add another DI container unless a real need appears.

---

# 114. Service Lifetimes

Choose service lifetimes deliberately.

Typical:

```text
DbContext
→ Scoped

Application services
→ Scoped

Stateless helpers
→ Singleton/Transient depending on behavior
```

Do not make DbContext singleton.

---

# 115. Domain Events

Version 1 does not require a domain-event infrastructure.

Simple direct coordination is sufficient.

---

# 116. Event Bus

Do not add:

```text
Internal event bus

RabbitMQ

Kafka

MassTransit
```

for ordinary module communication.

---

# 117. CQRS

Do not introduce full CQRS infrastructure.

Read and write logic may be separated naturally without framework ceremony.

---

# 118. MediatR

MediatR is not required.

Do not add it automatically.

If later adopted, document the reason first.

---

# 119. AutoMapper

AutoMapper is not required.

Do not add it automatically.

Manual projections/mapping are acceptable.

---

# 120. FluentValidation

A maintained validation library such as FluentValidation may be used if it clearly improves consistency.

It is optional.

Do not duplicate the same validations across multiple frameworks.

---

# 121. API Versioning Library

Do not add API versioning infrastructure in V1 unless external compatibility requirements appear.

---

# 122. Serialization

Use one consistent JSON configuration.

Important:

```text
Stable enum strings

Consistent property naming

Safe null behavior
```

---

# 123. JSON Naming

API should use a consistent naming strategy.

Recommended:

```text
snake_case
```

or:

```text
camelCase
```

Pick one and use it consistently.

If current frontend/backend conventions favor camelCase JSON, keep database snake_case independent.

---

# 124. Database Naming vs API Naming

PostgreSQL:

```text
snake_case
```

C#:

```text
PascalCase
```

JSON may use:

```text
camelCase
```

This separation is normal.

---

# 125. Entity Naming

C# entity class:

```text
Order
```

PostgreSQL table:

```text
orders.orders
```

Do not name C# entities with table prefixes.

---

# 126. EF Core Migrations

Migrations must reflect approved schema docs.

Review generated migrations before applying.

---

# 127. Migration Data Operations

If a migration must transform existing data:

```text
Write explicit migration logic

Test on realistic copy

Backup before production
```

Do not assume schema-only migration is always safe.

---

# 128. Database Seeding

Seed only system-required configuration.

Examples:

```text
Director Role

Permissions
```

Do not seed normal Lithograph Client/Project/Order data.

---

# 129. Permission Seeding

Permissions should be registered centrally by modules.

Examples:

Orders module defines:

```text
orders.view

orders.create

orders.edit
```

Authentication stores them.

---

# 130. New Permission Rule

When a new feature adds a Permission:

```text
Define code

Seed/register it

Update Director full-access behavior

Update tests

Update docs
```

---

# 131. Director Permission Future-Proofing

Future permissions must automatically become available to Director.

Do not require manually assigning every newly introduced Permission after each deployment unless that is the explicitly chosen documented model.

---

# 132. Module Configuration

Each module may expose registration methods.

Conceptually:

```text
services.AddAuthenticationModule()

services.AddEmployeesModule()

services.AddOrdersModule()
```

This can keep startup configuration organized.

---

# 133. Avoid Reflection Magic

Do not build a complex reflection-based automatic module loader unless necessary.

Explicit registration is easier to understand.

---

# 134. Startup

Application startup should clearly configure:

```text
Logging

Configuration

DbContext

Authentication

Authorization

Modules

Controllers

Swagger

Error handling
```

Avoid giant opaque extension chains that make startup behavior impossible to trace.

---

# 135. Health Checks

Add simple health checks for:

```text
Application

PostgreSQL
```

where useful.

---

# 136. Startup Database Failure

If required database connection cannot be established, application should fail clearly rather than pretending to operate normally.

---

# 137. Initial Director Bootstrap

Bootstrap logic belongs to Authentication module.

It must safely determine:

```text
Are there zero Users?
```

and allow first setup only in that case.

---

# 138. Bootstrap Concurrency

Bootstrap must avoid accidentally creating multiple first Directors if two setup requests occur simultaneously.

Use transaction/database protection.

---

# 139. Business ID Concurrency

Number generation must also remain safe under simultaneous requests.

Test concurrency.

---

# 140. Optimistic Concurrency

Use optimistic concurrency where overlapping edits are materially risky.

Primary candidate:

```text
Order Calculator
```

Other entities may initially use simpler last-write-wins unless real conflict risk justifies tokens.

---

# 141. Concurrency Token

If needed, add:

```text
row_version

xmin

updated_at
```

or another supported concurrency mechanism.

Choose deliberately.

Do not add tokens to every table automatically.

---

# 142. Calculator Concurrency

Calculator must not silently overwrite another User's recent changes.

If stale update detected:

```text
Return conflict
```

and require reload.

---

# 143. Audit Update Rules

When business entity changes:

```text
updated_at = UTC now

updated_by = current User
```

according to audit documentation.

---

# 144. Read Operations

Reads must not mutate:

```text
updated_at
```

---

# 145. Child Updates

Do not update parent audit metadata for every child change unless parent business data truly changes.

Exception:

Cost change updates Order Cost Price, so Order is updated.

---

# 146. Testing Boundaries

Business logic should be structured so critical rules can be unit-tested without HTTP.

Example:

```text
Formula Engine

Number formatter

Status transitions

Director protection
```

---

# 147. Integration Testing

Database-dependent business operations should have integration tests.

Examples:

```text
Create Client Business ID

Assign Project Owner

Save Cost + Cost Price

Publish Template
```

---

# 148. API Integration Tests

Critical endpoints should be tested through real ASP.NET Core request pipeline.

This validates:

```text
Routing

Authentication

Authorization

Validation

Persistence
```

together.

---

# 149. Test Data Isolation

Integration tests must not depend on production or developer data.

Use isolated PostgreSQL test database/container.

---

# 150. Performance

Version 1 should favor correct and clear code first.

Optimize measured bottlenecks later.

---

# 151. Expected Performance Areas

Pay attention to:

```text
Orders list

Project list

Reports

Calculator loading

Business ID search
```

because they may contain joins/aggregations.

---

# 152. Avoid Premature Optimization

Do not introduce:

```text
Compiled query infrastructure everywhere

Distributed cache

Read replicas

Search engine

Data warehouse
```

without evidence.

---

# 153. Security Principle

Assume browser requests can be manipulated.

Backend must validate:

```text
Identity

Permission

IDs

Business rules

Calculated values
```

for every sensitive operation.

---

# 154. Direct ID Manipulation

A User may manually change:

```text
orderId

projectId

employeeId
```

in requests.

Backend must still validate access/business rules.

---

# 155. Nested Resource Security

When updating:

```text
/orders/{orderId}/cost-items/{costItemId}
```

verify the Cost Item actually belongs to that Order.

---

# 156. Sensitive DTOs

Never serialize:

```text
password_hash

token_hash

secret configuration
```

---

# 157. File Upload Security

If Preview Image upload is implemented:

```text
Validate size

Validate allowed file type

Generate safe storage name

Do not trust original filename
```

---

# 158. Path Security

Folder Link path is stored as text.

Backend must not use it to perform arbitrary filesystem operations.

This prevents path traversal concerns in Version 1 folder-link feature.

---

# 159. Reports Security

Reports must apply the same financial permissions as normal modules.

Do not let Reports bypass Cost/Selling restrictions.

---

# 160. API Error Safety

Database exception details must not reach frontend.

Map known unique/constraint violations to useful business errors where appropriate.

---

# 161. Constraint Mapping

Examples:

```text
Unique username
→ USERNAME_ALREADY_EXISTS

Second Owner
→ PROJECT_OWNER_ALREADY_EXISTS
```

Do not expose constraint names to User.

---

# 162. Domain Terminology

Use official terms from:

```text
04_Data_Dictionary.md
```

Examples:

```text
Order

Project Team

Checklist Item

Business ID
```

Avoid introducing synonyms casually.

---

# 163. Task vs Order

Do not use:

```text
Task
```

for the core Order entity.

The official term is:

```text
Order
```

---

# 164. Natural Key

Do not use:

```text
Natural Key
```

for Client/Project/Order human numbers.

Use:

```text
Business ID
```

---

# 165. Backend Non-Goals

Version 1 backend does not require:

```text
Microservices

CQRS Framework

Event Sourcing

Message Broker

Generic Repository

Generic CRUD Engine

GraphQL

OData

Distributed Cache

Service Mesh

Dynamic Plugin System

Workflow Engine
```

---

# 166. AI Coding Rule

AI must read:

```text
AI_RULES.md

24_Backend_Architecture.md

Relevant module document

Database design

API guidelines
```

before implementing backend features.

---

# 167. AI Must Inspect Existing Patterns

Before creating a new pattern, AI must inspect existing:

```text
Entities

Configurations

Services

Controllers

DTOs

Tests
```

and reuse established conventions.

---

# 168. AI Must Avoid Unrequested Refactoring

A task to implement:

```text
Create Client
```

must not also rewrite:

```text
Authentication

DbContext architecture

API error model

Frontend
```

unless necessary.

---

# 169. AI Database Change Rule

If implementation appears to require a schema change not already documented:

```text
Stop

Identify reason

Update documentation

Then create migration
```

Do not silently invent fields.

---

# 170. AI Dependency Rule

AI must not add new NuGet packages unless:

```text
Existing stack cannot reasonably solve the problem

Package is maintained

Benefit is clear
```

and the change is documented/reviewed.

---

# 171. AI Completion Rule

A backend task is complete only when:

```text
Code builds

Relevant tests pass

Database changes have migration

Authorization works

Error behavior works

Docs still match implementation
```

---

# 172. Example Backend Use Case — Create Client

Flow:

```text
Controller
    ↓
CreateClient Application Logic
    ↓
Validate Permission
    ↓
Validate Name
    ↓
Generate Business ID
    ↓
Create Client Entity
    ↓
Set Audit Fields
    ↓
SaveChanges
    ↓
Return Client DTO
```

---

# 173. Example — Assign Project Owner

```text
Request
    ↓
Authorize projects.manage_team
    ↓
Load Project
    ↓
Load Employee
    ↓
Validate Employee Active
    ↓
Transaction
    ↓
Remove Existing Owner if changing
    ↓
Add New Owner Assignment
    ↓
Save
```

---

# 174. Example — Add Cost Item

```text
Request
    ↓
Authorize calculator.edit_costs
    ↓
Load Order
    ↓
Validate Amount
    ↓
Transaction
    ↓
Insert Cost Item
    ↓
Calculate SUM(cost_items.amount)
    ↓
Update Order Cost Price
    ↓
Update Order Audit
    ↓
Commit
```

---

# 175. Example — Save Order Calculator

```text
Request
    ↓
Authorize calculator.edit
    ↓
Load Order Calculator
    ↓
Load Exact Template Version
    ↓
Validate Field Values
    ↓
Evaluate Formula Graph
    ↓
Calculate Selling Price
    ↓
Transaction
    ↓
Save field_values
    ↓
Update Order Selling Price
    ↓
Update Audit
    ↓
Commit
```

---

# 176. Example — Publish Template Version

```text
Request
    ↓
Authorize calculator.publish_templates
    ↓
Load Draft
    ↓
Validate Definition
    ↓
Validate Formula Dependencies
    ↓
Reject Invalid Draft
    ↓
Set Published
    ↓
Set published_at
    ↓
Set published_by
    ↓
Save
```

---

# 177. Example — Orders Report

```text
Request Filters
    ↓
Authorize reports.view
    ↓
Build EF Core Query
    ↓
Apply Financial Permissions
    ↓
Apply Filters
    ↓
Calculate Summary
    ↓
Apply Pagination
    ↓
Project DTOs
    ↓
Return
```

---

# 178. Module Boundary Summary

```text
Authentication
owns login/access

Employees
owns people

Clients
owns customers

Projects
owns project grouping/team

Orders
owns operational work

Calculator
owns calculation/cost logic

Reports
owns read-only analysis
```

---

# 179. Core Dependency Flow

Conceptually:

```text
Authentication
      ↑
      │ audit/access
      │
Employees
      ↓
Projects
      ↓
Orders
      ↔
Calculator
      ↓
Reports reads all
```

Clients connect to Projects:

```text
Clients
  ↓
Projects
```

---

# 180. Final Backend Principle

Lithograph ERP backend should make business rules easy to find.

A developer should be able to answer:

```text
Where is this business rule?

Which module owns this data?

Who is allowed to change it?

Which transaction protects it?

Which tests verify it?
```

without tracing through unnecessary framework layers.

The core rule is:

```text
Use architecture to clarify the business.

Do not use architecture to hide it.
```

---

**End of Document**