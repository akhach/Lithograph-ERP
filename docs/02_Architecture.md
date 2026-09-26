# Lithograph ERP

**Document:** 02_Architecture.md  
**Version:** 1.1  
**Status:** Approved  
**Project:** Lithograph ERP  

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `01_Technology_Stack.md`

---

# 1. Purpose

This document defines the high-level software architecture of Lithograph ERP.

Its purpose is to establish:

- Major module boundaries
- Layer responsibilities
- Database ownership
- Communication rules
- Backend structure
- Frontend structure
- Rules for future expansion

The architecture should keep the ERP simple while allowing it to grow over time.

---

# 2. Architectural Style

Lithograph ERP uses a:

```text
Modular Monolith
```

This means:

- One application
- One backend
- One frontend
- One PostgreSQL database
- Multiple clearly separated business modules

The application is deployed as one system but internally organized as independent LEGO-like modules.

---

# 3. Why Modular Monolith

A Modular Monolith provides the advantages needed for Lithograph without unnecessary distributed-system complexity.

Benefits:

- Simple deployment
- Simple debugging
- Simple database transactions
- Easy local development
- Clear module boundaries
- Easier AI-assisted development
- Future ability to extract modules if ever required

Version 1 does not use microservices.

---

# 4. High-Level Architecture

```text
                 User Browser
                      │
                      ▼
             React + TypeScript
                  Frontend
                      │
                      ▼
              ASP.NET Core API
                      │
                      ▼
             Application Modules
                      │
                      ▼
              Entity Framework
                      │
                      ▼
                PostgreSQL
```

---

# 5. Version 1 Major Modules

Lithograph ERP Version 1 contains these major modules:

```text
Authentication

Employees

Clients

Projects

Orders

Calculator

Reports
```

These are the main architectural LEGO blocks.

---

# 6. Small Features Stay Inside Their Parent Module

Not every feature should become a separate module.

For example:

```text
Authentication
├── Users
├── Roles
├── Permissions
└── Sessions
```

```text
Orders
├── Order Types
├── Checklists
└── Folder Links
```

This rule is important.

The project should avoid creating unnecessary modules for small features.

---

# 7. Module Responsibilities

## 7.1 Authentication

Responsible for:

- Login
- Logout
- Users
- Roles
- Permissions
- Sessions
- Access control

Authentication must not contain employee business data.

---

## 7.2 Employees

Responsible for:

- Employee records
- Employee business information
- Optional link between Employee and User account

Employees are used by Projects and other business modules.

---

## 7.3 Clients

Responsible for:

- Client records
- Client contact information
- Client-related business data

Clients may own multiple Projects.

---

## 7.4 Projects

Responsible for:

- Project records
- Project status
- Project deadlines
- Project Team
- Project relationships to Clients

Project Team roles include:

```text
Owner
Assignee
Participant
Observer
```

These assignments apply to the Project's Orders in Version 1.

---

## 7.5 Orders

Responsible for:

- Orders
- Order Types
- Order status
- Selling Price
- Cost Price
- Checklists
- Folder Links
- Order relationship to Projects

Orders are a central operational module.

---

## 7.6 Calculator

Responsible for:

- Calculator Templates
- Template Designer
- Formula engine
- Calculator fields
- Calculator tables
- Calculator evaluation

Order Types may be assigned Calculator Templates.

---

## 7.7 Reports

Responsible for presenting and aggregating business data.

Examples:

- Orders by date
- Orders by Client
- Orders by Project
- Orders by Type
- Selling totals
- Cost totals
- Calculated profit

Reports should not become a second source of business data.

They should read existing structured data.

---

# 8. Core Business Relationships

The main business hierarchy is:

```text
Client
   │
   ▼
Project
   │
   ├── Project Team
   │
   └── Orders
          │
          ├── Order Type
          ├── Calculator
          ├── Checklist
          └── Folder Links
```

---

# 9. User and Employee Separation

Authentication Users and Employees are different architectural entities.

```text
User
=
Login Account
```

```text
Employee
=
Business Person
```

An Employee may optionally reference one User account.

A User must not become the main business employee record.

Authentication credentials must remain inside the Authentication module.

---

# 10. Application Layers

Each major backend module should follow the same simple layered structure.

```text
Module

├── Domain
├── Application
├── Infrastructure
└── API
```

The objective is separation of responsibilities, not excessive abstraction.

---

# 11. Domain Layer

The Domain layer contains business concepts and rules.

Examples:

- Project rules
- Order rules
- Calculator template rules
- Permission concepts

The Domain layer should not depend on:

- React
- HTTP
- PostgreSQL-specific implementation details
- UI components

---

# 12. Application Layer

The Application layer coordinates use cases.

Examples:

```text
Create Client

Create Project

Create Order

Assign Employee to Project

Add Checklist Item
```

Application services may coordinate multiple domain objects.

Business workflows belong here when they do not naturally belong to a single domain entity.

---

# 13. Infrastructure Layer

Infrastructure contains technical implementations.

Examples:

- Entity Framework Core
- PostgreSQL access
- Authentication persistence
- Logging integrations
- External services

Infrastructure implements technical concerns required by the application.

---

# 14. API Layer

The API layer exposes application functionality to the frontend.

Responsibilities include:

- HTTP endpoints
- Request DTOs
- Response DTOs
- Authorization checks
- Request validation
- HTTP status responses

Controllers should remain thin.

Business logic must not be placed directly in controllers.

---

# 15. Backend Folder Structure

A simple initial backend structure may look like:

```text
backend/
│
├── Lithograph.Api/
│
├── Modules/
│   │
│   ├── Authentication/
│   ├── Employees/
│   ├── Clients/
│   ├── Projects/
│   ├── Orders/
│   ├── Calculator/
│   └── Reports/
│
└── Shared/
```

The exact solution and project layout may be refined during implementation.

The structure should not become more complex than necessary.

---

# 16. Shared Backend Code

A small Shared area may contain truly cross-cutting functionality.

Examples:

- Common result types
- Shared date/time abstraction
- Common error handling
- Common API utilities

Do not move business logic into Shared merely because multiple modules use similar concepts.

Shared must remain small.

---

# 17. Frontend Architecture

The frontend should also be organized by business module.

Example:

```text
frontend/
└── src/
    │
    ├── app/
    ├── shared/
    └── modules/
        ├── authentication/
        ├── employees/
        ├── clients/
        ├── projects/
        ├── orders/
        ├── calculator/
        └── reports/
```

---

# 18. Frontend Module Structure

A frontend business module may contain:

```text
orders/
├── pages/
├── components/
├── api/
├── types/
└── hooks/
```

Not every module needs every folder.

Create structure only when required.

---

# 19. Shared Frontend Components

Reusable generic components may live in:

```text
frontend/src/shared/
```

Examples:

- Standard page header
- Generic confirmation dialog
- Reusable data table
- Loading indicator
- Error display
- Form controls

Business-specific components should remain inside their module.

---

# 20. Database Architecture

Lithograph ERP uses one PostgreSQL database.

Major modules may own separate schemas.

Example:

```text
auth
employees
clients
projects
orders
calculator
```

Reports normally read from existing module data and do not require their own schema unless persistent report configuration is later needed.

---

# 21. Database Schema Ownership

Each module owns the tables inside its PostgreSQL schema.

Example:

```text
auth
├── users
├── roles
├── permissions
└── sessions
```

```text
projects
├── projects
└── project_members
```

```text
orders
├── orders
├── order_types
├── checklist_items
└── folder_links
```

---

# 22. Cross-Module Database Rule

A module must not directly modify another module's tables.

Incorrect:

```text
Orders Module
    ↓
direct UPDATE
    ↓
clients.clients
```

Correct:

```text
Orders Module
    ↓
Client Application Service / Interface
    ↓
Clients Module
```

This rule protects module boundaries.

---

# 23. Foreign Keys Across Modules

Because Version 1 uses one PostgreSQL database, foreign keys between module schemas are allowed when they represent stable business relationships.

Example:

```text
projects.projects.client_id
    ↓
clients.clients.id
```

```text
orders.orders.project_id
    ↓
projects.projects.id
```

Foreign keys provide data integrity.

However, business updates must still respect module ownership.

---

# 24. Module Communication

Modules communicate using clear application-level interfaces.

Examples:

```text
Projects → Clients
```

Projects may need to verify that a Client exists.

```text
Orders → Projects
```

Orders may need Project information.

```text
Projects → Employees
```

Projects may need employee information for Project Team assignments.

Do not build a complex messaging system for this in Version 1.

Direct in-process application interfaces are sufficient.

---

# 25. Transaction Boundaries

Because the ERP is a Modular Monolith with one database, normal database transactions may be used when one business operation affects multiple tables.

Avoid distributed transaction concepts.

Keep transaction boundaries aligned with real business actions.

---

# 26. Project Team Inheritance

Project Team assignments belong to the Projects module.

Version 1 uses:

```text
Project
├── Owner
├── Assignee
├── Participants
└── Observers
```

These employees are considered applicable to all Orders in that Project.

The Orders module must not duplicate these assignments in separate Order Team tables in Version 1.

This avoids unnecessary duplicated data.

---

# 27. Order Type Ownership

Order Types belong to the Orders module.

Order Types are configurable data.

They must not be hardcoded into application source code.

Examples:

```text
UV Printing
CO₂ Laser Cutting
Graphic Design
Installation
Outsourced Work
```

---

# 28. Calculator Relationship

The Calculator module owns Calculator Templates.

Order Types may reference an assigned Calculator Template.

Conceptually:

```text
Order Type
    │
    ▼
Calculator Template
```

When an Order uses an Order Type, the application loads the associated calculator.

The exact historical/versioning behavior will be defined in the Calculator specification.

---

# 29. Checklist Ownership

Checklist belongs inside the Orders module.

A checklist item in Version 1 contains only:

```text
text
is_completed
sort_order
```

Checklist is not an independent architectural module.

---

# 30. Folder Link Ownership

Folder Links belong inside the Orders module.

They store references related to an Order.

Version 1 stores paths only.

Folder Links are not a separate service or module.

---

# 31. Reporting Architecture

Reports should query existing application data.

The Reports module must not duplicate operational records.

For example:

```text
Selling Price
Cost Price
```

are stored in Orders.

Profit is calculated:

```text
profit = selling_price - cost_price
```

Reports may aggregate these values without storing separate copies.

---

# 32. Record Workspace Concept

Important records should behave as workspaces.

For example, an Order workspace may contain:

```text
Order Header

General Information

Calculator

Checklist

Folder Links
```

This information belongs to one Order context.

Users should not need to navigate through many unrelated pages to manage one Order.

---

# 33. Dependency Direction

Modules should depend only on what they need.

Avoid circular dependencies.

A typical dependency flow may look like:

```text
Authentication

Employees

Clients
   │
   ▼
Projects
   │
   ▼
Orders
   │
   ▼
Calculator
```

This is conceptual, not a strict compile-time dependency diagram.

The goal is to keep relationships understandable.

---

# 34. Avoid Generic Abstraction Too Early

Do not build generic engines for concepts that are currently simple.

Examples:

Do not create a universal:

```text
Entity Assignment Engine
```

just to assign employees to Projects.

Do not create a universal:

```text
Workflow Engine
```

just to manage Order status.

Do not create a universal:

```text
Metadata Engine
```

for simple configurable fields.

Build simple direct solutions first.

---

# 35. Microservice Readiness

The architecture should not prevent future module extraction.

However, Version 1 must not add microservice infrastructure simply to be "ready."

Future separation should happen only if there is a real need such as:

- Independent scaling
- Independent deployment
- External integration
- Heavy background processing
- Clear organizational ownership

Possible future candidates might include:

- AI processing
- Document processing
- External integration services

The core ERP should remain a Modular Monolith unless requirements change.

---

# 36. Background Processing

Version 1 should avoid adding background job infrastructure unless a feature actually requires it.

If later required, background processing should be introduced for a specific use case rather than as a general architectural assumption.

---

# 37. Caching

Do not add distributed caching in Version 1 without demonstrated need.

Prefer straightforward database access and normal application-level optimization first.

---

# 38. Event Architecture

Version 1 does not require an event bus.

Internal application events may be used only when they simplify code.

Do not introduce infrastructure such as:

- Kafka
- RabbitMQ
- Service Bus

without a real business requirement.

---

# 39. Error Handling

Error handling should be consistent across modules.

Technical failures should be logged.

User-facing responses should remain understandable and safe.

Modules should not invent incompatible error response formats.

---

# 40. Validation

Validation exists at appropriate layers.

Frontend:

```text
Usability validation
```

Backend:

```text
Authoritative business and input validation
```

Database:

```text
Data integrity constraints
```

Do not rely only on frontend validation.

---

# 41. Security Boundaries

Authentication determines who the user is.

Authorization determines what the user may do.

Each module is responsible for enforcing its own required permissions.

Example:

```text
orders.create
```

belongs to Orders functionality.

Authentication provides the permission system used to enforce it.

---

# 42. Testing Architecture

Tests should follow module boundaries.

Example:

```text
tests/
├── Authentication/
├── Employees/
├── Clients/
├── Projects/
├── Orders/
└── Calculator/
```

The exact structure may be adjusted during implementation.

Testing should focus on useful business behavior.

---

# 43. Documentation Rule

Every major module should eventually have one main specification document containing:

- Purpose
- Requirements
- Database design
- Business rules
- API requirements
- UI behavior
- Validation
- Permissions
- Future extensions

Avoid creating dozens of tiny Markdown files unless a module becomes large enough to require them.

---

# 44. Architecture Change Rule

Architecture changes must be deliberate.

Before introducing a new:

- Module
- Service
- Database
- Framework
- Infrastructure dependency
- Cross-module pattern

the change should answer:

1. What current problem does it solve?
2. Why is the existing architecture insufficient?
3. What complexity does it introduce?
4. Can the requirement be solved more simply?

---

# 45. Version 1 Architecture Summary

```text
Lithograph ERP

React + TypeScript + Material UI
              │
              ▼
        ASP.NET Core
              │
              ▼
     Modular Monolith
              │
              ├── Authentication
              ├── Employees
              ├── Clients
              ├── Projects
              ├── Orders
              ├── Calculator
              └── Reports
              │
              ▼
         PostgreSQL
```

One application.

One database.

Clear modules.

Simple communication.

No unnecessary distributed architecture.

---

# 46. Final Architecture Principle

Lithograph ERP should be modular enough to grow but simple enough for one developer, assisted by AI, to understand and maintain.

When architectural purity conflicts with simplicity without delivering meaningful business value, prefer simplicity.

---

**End of Document**