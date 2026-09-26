# Lithograph ERP

**Document:** 04_Data_Dictionary.md  
**Version:** 1.1  
**Status:** Approved  
**Project:** Lithograph ERP  

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`
- `01_Technology_Stack.md`
- `02_Architecture.md`
- `03_Database_Design.md`

---

# 1. Purpose

This document defines the official business terminology used throughout Lithograph ERP.

The same terminology must be used consistently in:

- Database tables
- API endpoints
- C# classes
- TypeScript types
- User interface labels
- Reports
- Documentation
- AI prompts
- Tests

This document exists to prevent different names from being used for the same business concept.

---

# 2. Terminology Rule

When a business concept is defined in this document, its official term should be used throughout the project.

Do not introduce synonyms without a real reason.

For example:

Use:

```text
Order
```

Do not randomly replace it with:

```text
Job
Task
Work Item
Production Job
Request
```

unless one of those becomes a separately defined business object in the future.

---

# 3. User

A **User** is an account that can log into Lithograph ERP.

A User belongs to the Authentication module.

A User contains information related to:

- Username
- Password authentication
- Roles
- Permissions
- Sessions
- Account status

A User is not the same as an Employee.

---

# 4. Employee

An **Employee** is a person who works for Lithograph.

An Employee belongs to the Employees module.

Employee information may include:

- Full name
- Position
- Contact information
- Employment status
- Other future employee-related business information

An Employee may optionally be linked to one User account.

An Employee may exist without ERP login access.

---

# 5. User–Employee Relationship

The relationship is:

```text
Employee
   │
   └── optional
        │
        ▼
       User
```

Meaning:

- Every Employee does not require a User account.
- Every User account used by an internal employee may be linked to an Employee.
- Authentication information stays in Authentication.
- Employee business information stays in Employees.

---

# 6. Role

A **Role** is a reusable collection of Permissions assigned to Users.

Examples:

```text
Director
Manager
Designer
Operator
```

Roles are used for system access control.

A User may have one or more Roles.

---

# 7. Permission

A **Permission** represents one allowed system action.

Examples:

```text
orders.view

orders.create

orders.edit

users.manage_roles
```

Permissions are assigned to Roles.

Users normally receive permissions through Roles.

---

# 8. Session

A **Session** represents one authenticated login connection between a User and Lithograph ERP.

A Session may correspond to:

- A browser
- A computer
- Another authenticated device/session context

Sessions belong to the Authentication module.

---

# 9. Client

A **Client** is a company or individual that purchases products or services from Lithograph.

A Client may have multiple Projects.

Examples:

```text
Samsung Armenia

ABC Construction

Individual Customer
```

---

# 10. Project

A **Project** is a collection of related Orders for a Client.

A Project groups work that belongs together from a business perspective.

Examples:

```text
Store Opening 2027

Office Rebranding

Exhibition Stand

Retail Campaign
```

A Project belongs to one Client.

A Project may contain one or many Orders.

---

# 11. Project Team

A **Project Team** is the set of Employees assigned to a Project.

Project Team assignments apply to all Orders belonging to that Project in Version 1.

The Project Team is not duplicated separately for every Order.

---

# 12. Project Role

A **Project Role** describes an Employee's responsibility inside a Project.

Version 1 uses:

```text
Owner

Assignee

Participant

Observer
```

These are business workflow roles.

They are not the same as Authentication Roles.

---

# 13. Owner

The **Owner** is the Employee responsible for overall ownership of a Project.

Version 1 allows Project-level ownership.

The Owner relationship is inherited by the Orders inside that Project.

---

# 14. Assignee

The **Assignee** is the Employee primarily responsible for carrying out or coordinating the Project work.

The exact UI behavior will be defined in the Projects module specification.

---

# 15. Participant

A **Participant** is an Employee actively involved in the Project but not acting as its Owner or primary Assignee.

A Project may have multiple Participants.

---

# 16. Observer

An **Observer** is an Employee who needs visibility into a Project without being responsible for executing it.

A Project may have multiple Observers.

---

# 17. Order

An **Order** is one production or service job inside a Project.

Examples:

```text
Print UV panels

Cut acrylic letters

Design artwork

Install signage

Outsource metal fabrication
```

Every Order belongs to exactly one Project.

An Order is one of the central operational objects in Lithograph ERP.

---

# 18. Order Type

An **Order Type** defines what kind of work an Order represents.

An Order Type may be based on:

- A production machine
- A production technology
- Design work
- Installation
- Outsourced work
- Another service category

Examples:

```text
UV Printing

CO₂ Laser Cutting

Graphic Design

Installation

Outsourced Work
```

Order Types are configurable database data.

They must not be hardcoded into source code.

---

# 19. Selling Price

**Selling Price** is the final price charged to the Client for an Order.

The Selling Price is normally calculated using the Order's Calculator.

The final Selling Price is stored on the Order.

It is treated as historical business data.

---

# 20. Cost Price

**Cost Price** is the final internal cost associated with an Order.

Cost information may be entered and calculated inside the Order Calculator.

The final Cost Price is stored on the Order.

It is treated as historical business data.

---

# 21. Profit

**Profit** is a calculated value.

Formula:

```text
Profit = Selling Price - Cost Price
```

Profit is not normally stored as a database column.

It is calculated when needed by:

- Reports
- Dashboards
- Analytics
- Management views

---

# 22. Calculator

A **Calculator** is the interactive calculation page used inside an Order.

It is created from a Calculator Template associated with the Order Type.

The Calculator may contain:

- Inputs
- Tables
- Calculated values
- Formulas
- Price calculations
- Cost calculations

The Calculator is part of Lithograph ERP and does not require Microsoft Excel.

---

# 23. Calculator Template

A **Calculator Template** defines the structure and calculation logic of an Order Calculator.

A Calculator Template may be assigned to one or more Order Types depending on the final module design.

A template may define:

- Input fields
- Labels
- Dropdowns
- Tables
- Formula fields
- Results
- Layout

---

# 24. Template Designer

The **Template Designer** is the ERP interface used by authorized users to create and edit Calculator Templates.

Its purpose is to provide spreadsheet-like calculator design without reproducing the entire Microsoft Excel application.

---

# 25. Calculator Field

A **Calculator Field** is one element inside a Calculator Template.

Examples may include:

```text
Width

Height

Quantity

Material

Machine Time

Selling Price Result
```

A field may be:

- User-entered
- Selected
- Calculated
- Display-only

The exact field types will be defined in the Calculator module specification.

---

# 26. Formula

A **Formula** is an expression evaluated by the Calculator engine.

Example:

```text
width * height * quantity
```

or:

```text
ROUND(material_cost * markup, 0)
```

The formula engine will support a limited approved set of approximately 30 functions.

---

# 27. Calculator Function

A **Calculator Function** is a supported named operation available inside formulas.

Examples may include:

```text
SUM()

ROUND()

MIN()

MAX()

IF()
```

The complete approved list will be defined in the Calculator module specification.

Do not assume full Excel compatibility.

---

# 28. Calculator Table

A **Calculator Table** is a table-like structure inside a Calculator Template or Calculator.

It may be used for:

- Cost rows
- Material values
- Quantity-based calculations
- Other structured calculator data

Its exact behavior will be defined by the Calculator specification.

---

# 29. Checklist

A **Checklist** is the collection of manually created Checklist Items belonging to an Order.

The Checklist is intentionally simple in Version 1.

There is no automatic checklist generation.

---

# 30. Checklist Item

A **Checklist Item** is one manually created row inside an Order Checklist.

Version 1 contains only:

```text
Text

Completed

Sort Order
```

The Text field is free-form.

The user can write anything relevant to the Order.

Version 1 Checklist Items do not contain:

- Employee assignment
- Deadline
- Priority
- Notes
- Folder path
- File name

---

# 31. Completed

**Completed** is the boolean state of a Checklist Item.

Conceptually:

```text
false = not completed

true = completed
```

The UI may present this as a checkbox.

---

# 32. Sort Order

**Sort Order** determines the visible position of a Checklist Item relative to other Checklist Items in the same Order.

It is an internal ordering value.

Users normally interact with it through row ordering rather than editing the number directly.

---

# 33. Folder Link

A **Folder Link** is a stored reference to a local or network folder related to an Order.

Example:

```text
\\server\orders\2026\ORD-2026-000125
```

Version 1 stores only the path.

The ERP does not manage the physical files inside that folder.

---

# 34. Folder Path

A **Folder Path** is the actual text value stored by a Folder Link.

Examples may include:

```text
\\server\production\orders\ORD-2026-000125
```

or another valid environment-specific path.

Direct Windows Explorer launching is not part of Version 1.

---

# 35. Business ID

A **Business ID** is a human-readable system-generated identifier for a business record.

Examples:

```text
CL-000001

PRJ-2026-000001

ORD-2026-000001
```

Business IDs are intended for:

- Employees
- Search
- Reports
- Communication
- Printed documents

Business IDs are not database primary keys.

---

# 36. UUID

A **UUID** is the internal primary identifier used for normal business entities.

Example:

```text
6ca6df32-...
```

UUIDs are intended for system use.

Users normally should not need to see UUID values.

---

# 37. Primary Key

A **Primary Key** uniquely identifies a database row.

Normal business entities use UUID primary keys.

Simple junction tables may instead use a composite primary key.

---

# 38. Foreign Key

A **Foreign Key** represents a database relationship between two records.

Example:

```text
orders.orders.project_id
```

references:

```text
projects.projects.id
```

Foreign keys are used to protect relational data integrity.

---

# 39. Junction Table

A **Junction Table** represents a many-to-many relationship between two entities.

Examples:

```text
auth.user_roles

auth.role_permissions

projects.project_members
```

A pure junction table does not automatically require its own UUID primary key.

---

# 40. Module

A **Module** is a major functional LEGO block of Lithograph ERP.

Version 1 major modules are:

```text
Authentication

Employees

Clients

Projects

Orders

Calculator

Reports
```

A Module owns its business logic and database structures.

---

# 41. Feature

A **Feature** is smaller functionality that belongs inside a Module.

Examples:

```text
Authentication Module
    └── Sessions
```

```text
Orders Module
    └── Checklist
```

A Feature should not automatically become an independent Module.

---

# 42. Modular Monolith

A **Modular Monolith** is the architectural style used by Lithograph ERP Version 1.

It means:

- One application
- One deployment
- One database
- Clearly separated internal modules

It does not mean that every module is a separate service.

---

# 43. Schema

A **Schema** refers to a PostgreSQL database schema used to organize tables by module ownership.

Examples:

```text
auth

employees

clients

projects

orders

calculator
```

A database Schema is not the same as a Module, although major Modules may own corresponding Schemas.

---

# 44. Authentication Role vs Project Role

These terms must not be confused.

## Authentication Role

Controls system permissions.

Examples:

```text
Director

Designer

Operator
```

## Project Role

Describes an Employee's responsibility inside a Project.

Examples:

```text
Owner

Assignee

Participant

Observer
```

These concepts serve different purposes.

---

# 45. Active

**Active** generally means a record is currently usable.

Example:

An active User may log in.

An inactive User still exists but may not log in.

Active is not the same as Deleted.

---

# 46. Deleted

**Deleted** may represent a soft-deleted business record where historical persistence is required.

Deletion behavior is defined per entity.

Not every table uses soft delete.

---

# 47. Created At

**Created At** is the timestamp representing when a record was created.

Database field:

```text
created_at
```

Important timestamps are stored in UTC.

---

# 48. Created By

**Created By** identifies the User responsible for creating a record when audit information is required.

Database field:

```text
created_by
```

The field may require special handling during first-run system bootstrap.

---

# 49. Updated At

**Updated At** represents the most recent stored modification time of a record.

Database field:

```text
updated_at
```

---

# 50. Updated By

**Updated By** identifies the User responsible for the most recent update where audit information is required.

Database field:

```text
updated_by
```

---

# 51. Report

A **Report** is a read-oriented presentation or aggregation of existing ERP data.

A Report should normally not become a separate source of operational data.

Examples:

```text
Orders by Client

Orders by Type

Selling totals

Cost totals

Profit by period
```

---

# 52. Historical Value

A **Historical Value** is a business value that must remain as it was at the time a transaction or Order was finalized.

Examples include:

```text
Order Selling Price

Order Cost Price
```

Historical values may be stored even when they originated from calculations.

---

# 53. Configuration

**Configuration** is data that controls system behavior without requiring source-code changes.

Examples:

```text
Order Types

Calculator Templates
```

Whenever practical, changeable business behavior should be configuration rather than hardcoded logic.

---

# 54. Hardcoded

**Hardcoded** means a business value is written directly into application source code instead of being configurable data.

Example to avoid:

```text
if orderType == "UV Printing"
```

when Order Types are intended to be configurable.

Hardcoding may still be appropriate for true system constants or structural rules.

---

# 55. Workspace

A **Workspace** is a UI concept where information related to one important business record is managed within one context.

Examples:

```text
Project Workspace

Order Workspace
```

An Order Workspace may contain:

```text
General Information

Calculator

Checklist

Folder Links
```

A Workspace is not a database entity.

---

# 56. Order Workspace

The **Order Workspace** is the primary interface for managing one Order.

It should provide convenient access to Order-related information without unnecessary navigation between disconnected screens.

---

# 57. Project Workspace

The **Project Workspace** is the primary interface for managing one Project.

It may include:

- Project information
- Project Team
- Related Orders

Its exact UI structure will be defined by the Projects module specification.

---

# 58. Inheritance

Within the current Project Team design, **Inheritance** means that the Project Team is considered applicable to Orders belonging to that Project.

Version 1 does not copy Project Team assignments into each Order.

This is logical inheritance, not duplicated database data.

---

# 59. Order-Specific Team Override

An **Order-Specific Team Override** would allow an Order to have employee assignments different from its Project Team.

This functionality is not part of Version 1.

It should not be implemented unless later approved.

---

# 60. Outsourced Work

**Outsourced Work** is an Order Type or service category where the work is performed outside Lithograph rather than by internal production equipment or employees.

It remains an Order inside the normal Project/Order structure.

A dedicated purchasing or supplier workflow is outside Version 1.

---

# 61. Designer Work

**Designer Work** is an Order Type representing work performed by a designer rather than by a production machine.

This demonstrates that Order Types are not limited to machines.

---

# 62. Production Machine

A **Production Machine** is physical equipment used to perform production work.

Examples may include:

```text
UV Printer

CO₂ Laser

Flatbed Cutter
```

A dedicated Machines module is not currently part of Version 1.

Order Types may describe machine-based work without requiring a separate machine-management system.

---

# 63. Template Version

A **Template Version** is a future Calculator concept used to preserve historical calculator behavior when templates change.

The exact versioning model has not yet been approved.

Do not implement it until the Calculator module defines it.

---

# 64. Current Scope

**Current Scope** refers to functionality explicitly approved for the current development version.

Future ideas mentioned in documentation do not automatically belong to Current Scope.

---

# 65. Future Extension

A **Future Extension** is functionality that may be useful later but is intentionally outside the current implementation scope.

Examples:

```text
Warehouse

Finance

CRM

AI Assistant

Windows Desktop Agent
```

Future Extensions must not be implemented merely because they appear in planning documentation.

---

# 66. Official Version 1 Vocabulary Summary

Use these terms consistently:

```text
User
Employee
Role
Permission
Session

Client

Project
Project Team
Project Role
Owner
Assignee
Participant
Observer

Order
Order Type
Selling Price
Cost Price
Profit

Calculator
Calculator Template
Template Designer
Calculator Field
Calculator Table
Formula
Calculator Function

Checklist
Checklist Item
Completed
Sort Order

Folder Link
Folder Path

Business ID
UUID

Module
Feature
Schema
Workspace
Report
Configuration
Historical Value
```

---

# 67. Terminology Change Rule

If a new important business term is introduced:

1. Define it in this document.
2. Explain how it differs from existing concepts.
3. Use the approved term consistently afterward.
4. Update affected documentation if necessary.

Do not introduce multiple names for the same concept.

---

# 68. Final Terminology Principle

Clear language is part of the architecture.

If developers, employees and AI coding assistants use the same words for the same concepts, the system becomes easier to:

- Design
- Build
- Test
- Maintain
- Explain
- Expand

This document is the authoritative vocabulary of Lithograph ERP.

---

**End of Document**