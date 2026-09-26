# Lithograph ERP

**Project:** Lithograph ERP  
**Architecture:** Modular Monolith  
**Status:** Design and Documentation Phase  

---

# 1. Overview

Lithograph ERP is a modular business management and production ERP developed specifically for Lithograph.

The system is intended to become the central operating platform for Lithograph by managing:

- Clients
- Employees
- Projects
- Orders
- Production and service workflows
- Order calculations
- Costs
- Selling prices
- Checklists
- Folder references
- Business reporting

The objective is not to create a generic ERP.

The objective is to create a simple, reliable, maintainable and expandable ERP specifically suited to Lithograph's real workflow.

---

# 2. Current Project Stage

The project is currently in the:

**Architecture and Database Design phase**

Production code should not be generated until the corresponding specification is designed and approved.

The general workflow is:

```text
Business Requirement
        ↓
Documentation
        ↓
Database Design
        ↓
Business Rules
        ↓
API Design
        ↓
UI Design
        ↓
Implementation
        ↓
Testing
        ↓
Review
```

Documentation comes before implementation.

---

# 3. Core Development Philosophy

Lithograph ERP follows these principles:

1. Keep everything as simple as possible.
2. Solve real business problems only.
3. Design the database before implementing features.
4. Document important decisions before writing code.
5. Build one module at a time.
6. Keep modules loosely coupled.
7. Prefer configuration over hardcoded business logic.
8. Avoid premature complexity.
9. Avoid implementing hypothetical future requirements.
10. Build for long-term maintainability.
11. Keep the system understandable by both humans and AI coding assistants.
12. Expand the ERP gradually as Lithograph's requirements grow.

---

# 4. Technology Stack

## Frontend

- React
- TypeScript
- Material UI

## Backend

- C#
- ASP.NET Core
- Entity Framework Core

## Database

- PostgreSQL

## API

- REST

## Architecture

- Modular Monolith
- Single application
- Single PostgreSQL database
- PostgreSQL schemas separated by major module

## Documentation

- Markdown

## Version Control

- Git

---

# 5. High-Level Architecture

```text
                User Browser
                     │
                     ▼
             React + TypeScript
                  Frontend
                     │
                     ▼
              ASP.NET Core
                 REST API
                     │
                     ▼
               PostgreSQL
                 Database
```

Version 1 intentionally uses one application and one database.

Microservices are not used in Version 1.

---

# 6. Version 1 Modules

The major Version 1 modules are:

```text
Authentication
Employees
Clients
Projects
Orders
Calculator
Reports
```

Small features should remain inside their owning module instead of becoming unnecessary independent modules.

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

This keeps the application modular without creating unnecessary complexity.

---

# 7. Core Business Structure

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

A Client may have multiple Projects.

A Project may have multiple Orders.

An Order belongs to one Project.

---

# 8. User and Employee

A User and an Employee are different entities.

## User

A User is an ERP login account.

It contains information related to:

- Username
- Password authentication
- Roles
- Permissions
- Sessions

## Employee

An Employee represents a person working for Lithograph.

It contains business information related to the employee.

An Employee may optionally have a User account.

An Employee may exist without ERP login access.

Authentication information must not be stored in the Employee record.

Employee business information must not be stored in the authentication User record.

---

# 9. Project Team

Employees can be assigned to a Project using the following project roles:

```text
Owner
Assignee
Participant
Observer
```

The Project Team applies to all Orders belonging to that Project in Version 1.

Version 1 does not require separate employee assignments for every Order.

Order-specific team overrides may be introduced later only if required.

---

# 10. Orders

An Order represents one production or service job.

Examples:

```text
UV Printing
CO₂ Laser Cutting
CNC Routing
Graphic Design
Installation
Outsourced Work
```

Every Order contains, among other information:

```text
Order Type
Selling Price
Cost Price
```

The Order Type determines what kind of work is being performed.

An Order Type may represent:

- Work performed by a machine
- Work performed by a designer
- Installation
- Outsourced work
- Another production or service category

---

# 11. Selling Price and Cost Price

Selling Price and Cost Price are stored on the Order.

They represent the final values produced by the Order calculator.

These values are intentionally stored because they are historical business values.

Profit is not stored.

Profit is calculated when required:

```text
profit = selling_price - cost_price
```

Profit may later be used by reports and analytics.

---

# 12. Calculator System

The calculator is a core Version 1 feature.

Lithograph ERP does not depend on Microsoft Excel for Order calculations.

Each Order Type may have an assigned Calculator Template.

Example:

```text
Order Type:
UV Printing

        ↓

Calculator Template:
UV Printing Calculator
```

When an Order is created, the appropriate calculator page is loaded according to the Order Type.

---

# 13. Calculator Template Designer

Lithograph ERP will include a Template Designer.

Authorized users can create and modify Calculator Templates.

The Template Designer provides spreadsheet-like functionality.

Possible template elements include:

- Number inputs
- Text inputs
- Dropdowns
- Labels
- Calculated values
- Tables
- Formulas

The goal is not to reproduce Microsoft Excel.

The calculator engine will support approximately 30 approved functions that cover Lithograph's real calculation requirements.

The exact function list will be defined in the Calculator module specification.

---

# 14. Checklist

Checklist functionality is intentionally simple in Version 1.

A user presses:

```text
Add Checklist Item
```

A new editable row is created.

Each checklist item contains only:

```text
Text
Completed
Sort Order
```

The user may write anything in the Text field.

Checklist items are created manually.

Version 1 does not include:

- Automatic checklist generation
- Checklist templates
- Employee assignment
- Deadlines
- Folder links inside checklist rows
- File names
- Priority
- Notes

These may be introduced later if required.

---

# 15. Folder Links

Orders may contain references to local or network folders.

Example:

```text
\\server\orders\2026\ORD-2026-000125
```

Version 1 stores the folder path only.

Version 1 does not include:

- Opening Windows Explorer directly from the browser
- Local desktop helper applications
- File synchronization
- File management
- Automatic folder creation

Windows desktop integration may be designed in a future version.

---

# 16. Database

Lithograph ERP uses one PostgreSQL database.

Major modules may own separate PostgreSQL schemas.

Example:

```text
auth
employees
clients
projects
orders
calculator
```

Database identifiers use lowercase `snake_case`.

Examples:

```text
auth.users

projects.projects

projects.project_members

orders.orders

orders.checklist_items

selling_price

created_at

project_id
```

---

# 17. Database IDs

Normal business entities use UUID primary keys internally.

Example:

```text
id UUID
```

UUID values are intended for the system and normally should not be displayed to users.

Human-facing business entities may additionally have a Business ID.

Examples:

```text
CL-000001

PRJ-2026-000001

ORD-2026-000001
```

Business IDs are intended for employees, reports, communication and search.

---

# 18. Documentation Structure

Project documentation is stored in:

```text
/docs
```

Root files:

```text
README.md

AI_RULES.md
```

Core documentation:

```text
docs/
├── 00_Project_Vision.md
├── 01_Technology_Stack.md
├── 02_Architecture.md
├── 03_Database_Design.md
├── 04_Data_Dictionary.md
├── 05_Numbering_System.md
├── 06_UI_UX_Principles.md
└── ...
```

Module specifications will be added incrementally.

---

# 19. AI-Assisted Development

Lithograph ERP is intended to be developed with AI coding assistants such as Cursor and Claude.

Before modifying the project, an AI assistant must read:

```text
AI_RULES.md
```

and the documentation relevant to the task.

Documentation is authoritative.

An AI assistant must not invent:

- New modules
- New features
- New frameworks
- New database structures
- New infrastructure
- New business rules

without explicit approval.

---

# 20. Version 1 Non-Goals

Version 1 intentionally excludes:

```text
Warehouse Management

Accounting

Full Financial Management

CRM

Purchasing

Production Scheduling

AI Assistant

Customer Portal

Supplier Portal

Mobile Application

Windows Desktop Agent

Direct Windows Explorer Integration

Microservices
```

These may be introduced in future versions only when a real business requirement exists.

---

# 21. Development Objective

Lithograph ERP should behave like a collection of LEGO blocks.

Each major module should:

- Have a clear responsibility
- Be understandable independently
- Have clear database ownership
- Avoid unnecessary dependencies
- Connect cleanly to other modules
- Be replaceable or extendable later

The system should begin small and become more capable over time without requiring complete redesign.

---

# 22. Final Project Principle

When several solutions are possible, prefer the simplest solution that:

- Solves the current requirement
- Fits the documented architecture
- Is easy to understand
- Is easy to maintain
- Can be extended later if necessary

Do not build today what may only be needed tomorrow.

---

# 23. Development Setup

## Prerequisites

- Git
- .NET 10 SDK
- Node.js 24 LTS (includes npm)
- PostgreSQL 18 (Windows service), with `C:\Program Files\PostgreSQL\18\bin` on `PATH` for `psql`

Trust the ASP.NET Core HTTPS development certificate once:

```powershell
dotnet dev-certs https --trust
```

## 1. Start PostgreSQL and create the development databases

PostgreSQL runs as the `postgresql-x64-18` Windows service. Create a dedicated non-superuser account and two databases (development and integration tests) once, as the `postgres` superuser:

```powershell
psql -U postgres
```

```sql
CREATE ROLE lithograph_dev LOGIN PASSWORD '<choose-a-dev-password>';
CREATE DATABASE lithograph_erp_dev  OWNER lithograph_dev ENCODING 'UTF8' TEMPLATE template0;
CREATE DATABASE lithograph_erp_test OWNER lithograph_dev ENCODING 'UTF8' TEMPLATE template0;
```

## 2. Configure the backend connection

Connection strings are never committed. Store them in ASP.NET Core User Secrets:

```powershell
cd backend
dotnet user-secrets set "ConnectionStrings:LithographDb" "Host=localhost;Port=5432;Database=lithograph_erp_dev;Username=lithograph_dev;Password=<password>" --project src/LithographERP.Api
dotnet user-secrets set "ConnectionStrings:LithographTestDb" "Host=localhost;Port=5432;Database=lithograph_erp_test;Username=lithograph_dev;Password=<password>" --project tests/LithographERP.IntegrationTests
```

The API refuses to start when `ConnectionStrings:LithographDb` is missing.

## 3. Start the backend

```powershell
cd backend
dotnet run --project src/LithographERP.Api --launch-profile https
```

- Health: `https://localhost:7205/health`
- Swagger (Development only): `https://localhost:7205/swagger`

## 4. Start the frontend

```powershell
cd frontend
npm ci
copy .env.example .env.local
npm run dev
```

Open `http://localhost:5173`. The page shows the backend and database health result.

## 5. Build, lint and test

```powershell
cd backend
dotnet build LithographERP.sln
dotnet test LithographERP.sln
```

```powershell
cd frontend
npm run lint
npm run format:check
npm run build
```

## 6. EF Core tooling

```powershell
cd backend
dotnet tool restore
dotnet ef migrations list -p src/LithographERP.Infrastructure -s src/LithographERP.Api
```

---

**End of Document**