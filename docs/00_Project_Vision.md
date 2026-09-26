# Lithograph ERP

**Document:** 00_Project_Vision.md  
**Version:** 1.1  
**Status:** Approved  
**Project:** Lithograph ERP  

---

# 1. Project Overview

Lithograph ERP is a modular Enterprise Resource Planning system developed specifically for Lithograph, a printing, advertising, design and manufacturing company.

The system is intended to manage Lithograph's real operational workflow rather than reproduce the functionality of a generic ERP.

Its initial focus is:

```text
Clients
    ↓
Projects
    ↓
Orders
    ↓
Production / Service Work
```

The system will be developed gradually and expanded together with Lithograph's real business requirements.

Lithograph ERP is intended to become the central operating platform of the company.

---

# 2. Primary Project Goals

The primary goals are:

- Organize company operational information in one system.
- Simplify employees' daily work.
- Reduce repetitive manual work.
- Improve production workflow visibility.
- Make Projects and Orders easier to manage.
- Track Selling Price and Cost Price for Orders.
- Provide configurable Order calculators.
- Improve consistency in production calculations.
- Reduce operational errors.
- Make business reporting possible from reliable structured data.
- Create a platform that can grow with Lithograph.

---

# 3. Project Philosophy

Lithograph ERP should remain simple.

The project must avoid becoming complex merely because advanced architecture or additional features are technically possible.

Every feature should answer:

> What real Lithograph business problem does this solve?

If there is no current business requirement, the feature should normally wait.

---

# 4. LEGO Architecture Philosophy

Lithograph ERP should behave like LEGO.

The system is divided into clearly separated modules.

Each major module should:

- Have one clear area of responsibility.
- Own its own business logic.
- Own its own database structures.
- Have minimal dependencies on other modules.
- Be understandable independently.
- Connect to other modules through clear interfaces.
- Be extendable later without redesigning the entire ERP.

Version 1 uses a Modular Monolith.

Microservices are intentionally not used.

---

# 5. Database First

Database structure is considered the foundation of the ERP.

Development of a significant module follows this order:

```text
Business Requirement
        ↓
Documentation
        ↓
Database Design
        ↓
Business Rules
        ↓
API
        ↓
User Interface
        ↓
Implementation
```

Database decisions should be made before large amounts of code depend on them.

---

# 6. Documentation Before Implementation

Important system behavior should be documented before implementation.

Documentation exists to prevent:

- Contradictory AI-generated code
- Repeated architectural changes
- Inconsistent terminology
- Unnecessary features
- Accidental complexity

Approved project documentation is the primary source of truth for intended behavior.

---

# 7. AI-Friendly Development

Lithograph ERP is designed to be developed with modern AI coding assistants.

Possible development tools include:

- Cursor
- Claude
- ChatGPT
- Other future coding assistants

Project documentation must therefore remain:

- Structured
- Consistent
- Explicit
- Easy for humans to read
- Easy for AI systems to interpret

AI assistants must follow `AI_RULES.md`.

---

# 8. Incremental Development

Development happens one module at a time.

Large features should be divided into smaller manageable tasks.

A working module should not be unnecessarily rewritten while another module is being developed.

The preferred approach is:

```text
Design
   ↓
Build
   ↓
Test
   ↓
Stabilize
   ↓
Move to Next Module
```

---

# 9. Initial Target Users

Version 1 is intended primarily for Lithograph employees.

Examples include:

- Director
- Project Manager
- Designer
- Machine Operator
- Production Employee
- Installer

Future versions may also support:

- Purchasing Staff
- Warehouse Staff
- Accountants
- Sales Staff
- Customers
- Suppliers

Customer and supplier access is not part of Version 1.

---

# 10. User and Employee Concept

Lithograph ERP distinguishes between:

## User

A User is an ERP login account.

It is responsible for authentication and permissions.

## Employee

An Employee represents a person working for Lithograph.

Employee information belongs to the Employees module.

An Employee may have an ERP User account.

An Employee may also exist without login access.

This separation must remain part of the architecture.

---

# 11. Version 1 Major Modules

Version 1 contains the following major modules:

```text
Authentication

Employees

Clients

Projects

Orders

Calculator

Reports
```

Smaller functionality should remain inside the module that owns it.

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

Avoid creating unnecessary modules for small features.

---

# 12. Clients

A Client represents a company or individual purchasing products or services from Lithograph.

A Client may have multiple Projects.

Client functionality should initially remain simple.

---

# 13. Projects

A Project groups related Orders.

Example:

```text
Client:
Samsung

Project:
Store Opening 2027

Orders:
- Exterior Sign
- Window Stickers
- UV Printed Panels
- Installation
```

A Project may contain one or many Orders.

---

# 14. Project Team

Employees may be assigned to a Project using project roles:

```text
Owner

Assignee

Participant

Observer
```

These assignments apply to the Project's Orders in Version 1.

Employees should not need to be repeatedly assigned to every Order.

Order-specific team overrides are outside the initial scope unless a real requirement appears later.

---

# 15. Orders

An Order represents one production or service job.

Examples include:

- UV Printing
- CO₂ Laser Cutting
- CNC Routing
- Design Work
- Installation
- Outsourced Work

Every Order belongs to one Project.

Orders are one of the central objects of Lithograph ERP.

---

# 16. Order Types

Every Order has an Order Type.

Order Type identifies what kind of work is being performed.

An Order Type may represent:

- A specific machine or production technology
- Work performed by a designer
- Installation
- Outsourced work
- Another service or production category

Order Types must be configurable data.

They must not be hardcoded into the application.

---

# 17. Selling Price

Every Order stores a Selling Price.

The Selling Price is calculated using the Order's Calculator Template.

The final calculated value is stored on the Order as historical business data.

---

# 18. Cost Price

Every Order stores a Cost Price.

The Order Calculator may contain its own mini spreadsheet-like cost structure.

Users can enter cost information according to the calculator template.

The resulting final Cost Price is stored on the Order.

---

# 19. Profit

Profit is not stored as a database field.

It is calculated when required:

```text
Profit = Selling Price - Cost Price
```

Profit may later be used by:

- Reports
- Dashboards
- Analysis
- Management statistics

This prevents unnecessary duplicated calculated data.

---

# 20. Calculator

The Order Calculator is a core Version 1 feature.

Lithograph ERP will not depend on external Microsoft Excel files for Order calculations.

When an Order Type is selected, the system loads the Calculator Template assigned to that Order Type.

Example:

```text
Order Type:
CO₂ Laser Cutting

        ↓

Calculator Template:
CO₂ Laser Calculator
```

The calculator page is part of the Order workspace.

---

# 21. Calculator Template Designer

Authorized users can create Calculator Templates inside Lithograph ERP.

The Template Designer should provide spreadsheet-like behavior while remaining much simpler than Microsoft Excel.

Possible elements include:

- Input fields
- Number fields
- Text fields
- Dropdowns
- Labels
- Tables
- Calculated fields
- Formulas

Templates can then be assigned to Order Types.

---

# 22. Calculator Formula Engine

The formula engine should intentionally remain limited.

The objective is not to build an Excel clone.

The first version should support approximately 30 approved functions that cover Lithograph's real production calculation requirements.

Examples of useful categories include:

- Arithmetic
- Rounding
- Aggregation
- Logic
- Minimum and maximum values
- Percentage calculations

The exact function list must be defined in the Calculator module specification.

---

# 23. Checklist

Orders contain a simple manually created checklist.

The user presses:

```text
Add Checklist Item
```

and receives a new row.

Each row contains only:

```text
Text

Completed

Sort Order
```

The text field is free-form.

Users may write anything relevant to that Order.

Version 1 does not include automatic checklist generation.

---

# 24. Folder Links

Orders may store folder paths.

This allows employees to keep references to local or network folders associated with an Order.

Version 1 stores only the folder path.

Direct Windows Explorer integration is intentionally excluded from Version 1.

A future desktop helper may be considered later if the requirement remains useful.

---

# 25. Reporting

Version 1 should support basic reporting from structured ERP data.

Examples may later include:

- Orders by date
- Orders by Type
- Orders by Client
- Orders by Project
- Selling totals
- Cost totals
- Profit calculations

Reporting should use database data rather than create duplicate sources of truth.

---

# 26. Version 1 Non-Goals

The following are intentionally outside Version 1:

```text
Warehouse Management

Accounting

Full Financial Management

CRM

Purchasing

Production Scheduling

Equipment Maintenance

AI Assistant

Customer Portal

Supplier Portal

Mobile Application

Windows Desktop Agent

Direct Windows Explorer Integration

Microservices
```

These features should not be implemented merely because they might be useful someday.

They should be introduced only when there is a real requirement.

---

# 27. Long-Term Vision

Lithograph ERP may gradually expand into additional areas such as:

- Warehouse
- Purchasing
- Production Planning
- Finance
- Equipment Maintenance
- CRM
- Business Analytics
- AI Assistance
- Customer Services
- Supplier Services
- Desktop Integration

The architecture should support this expansion without requiring unnecessary complexity today.

---

# 28. Core Principles

Every design decision should follow these principles:

1. Keep it simple.
2. Build only what is currently useful.
3. Keep modules clearly separated.
4. Maintain one source of truth for data.
5. Avoid unnecessary duplication.
6. Design the database before implementation.
7. Prefer configuration over hardcoding.
8. Keep important business rules documented.
9. Avoid premature microservices.
10. Keep AI-generated code controlled by specifications.
11. Preserve historical business data where necessary.
12. Make future expansion possible without building future features today.
13. If a feature can safely wait, it should wait.

---

# 29. Success Criteria

Lithograph ERP will be considered successful if it:

- Makes daily work easier.
- Reduces repetitive manual work.
- Reduces production and organizational mistakes.
- Makes Project and Order information easy to find.
- Makes calculations more consistent.
- Provides reliable Selling Price and Cost data.
- Improves visibility of active work.
- Provides useful business reports.
- Remains easy to understand.
- Remains easy to maintain.
- Can be extended gradually.
- Does not become unnecessarily complicated.

---

# 30. Final Vision

Lithograph ERP should become the operating system of Lithograph.

It should not attempt to imitate every feature of large generic ERP products.

Its strength should come from being designed specifically around Lithograph's workflows.

The system should remain:

```text
Simple

Modular

Reliable

Configurable

Maintainable

Expandable
```

Every future feature should strengthen these characteristics.

---

**End of Document**