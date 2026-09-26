# Lithograph ERP

**Document:** 01_Technology_Stack.md  
**Version:** 1.1  
**Status:** Approved  
**Project:** Lithograph ERP  

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `00_Project_Vision.md`

---

# 1. Purpose

This document defines the approved technology stack for Lithograph ERP.

The technology stack should remain stable unless there is a strong technical or business reason to change it.

The objective is to use mature, well-supported technologies that are suitable for:

- Business applications
- Long-term maintenance
- AI-assisted development
- Relational business data
- Modular architecture
- Future expansion

The technology stack must remain simpler than the business problem it is solving.

---

# 2. Technology Selection Principles

Technologies used by Lithograph ERP should be:

- Mature
- Well documented
- Actively maintained
- Widely used
- Suitable for long-term projects
- Easy for developers to understand
- Well supported by AI coding tools
- Appropriate for business software
- Compatible with modular development

Avoid adopting technologies only because they are new or fashionable.

---

# 3. Overall Application Type

Lithograph ERP is a browser-based internal business application.

The initial architecture is:

```text
User Browser
     │
     ▼
React Frontend
     │
     ▼
ASP.NET Core REST API
     │
     ▼
PostgreSQL Database
```

The system is desktop-first but runs inside a standard web browser.

---

# 4. Frontend

## 4.1 Language

The frontend uses:

```text
TypeScript
```

TypeScript is required instead of plain JavaScript.

Reasons:

- Strong typing
- Better refactoring
- Better AI-generated code reliability
- Easier maintenance
- Better development tools

---

# 5. Frontend Framework

The frontend uses:

```text
React
```

React provides:

- Component-based UI development
- Large ecosystem
- Strong TypeScript support
- Good AI coding support
- Reusable interface components

React is responsible only for presentation and client-side interaction.

Important business rules must not exist only in the frontend.

---

# 6. UI Component Library

The approved UI framework is:

```text
Material UI
```

Material UI should be used consistently throughout the application.

It provides reusable components for:

- Forms
- Buttons
- Tables
- Dialogs
- Navigation
- Tabs
- Menus
- Inputs
- Layout

The project should avoid introducing another general-purpose UI framework unless explicitly approved.

---

# 7. Frontend Design Goal

Lithograph ERP is not a marketing website.

The interface should behave like a professional desktop business application running in a browser.

Priority order:

```text
Productivity
Clarity
Consistency
Speed
Visual appearance
```

Visual design should support work rather than become the focus of the application.

---

# 8. Backend Language

The backend uses:

```text
C#
```

C# was selected because it provides:

- Strong typing
- Mature development tools
- Excellent support for business applications
- Good performance
- Long-term ecosystem stability
- Strong integration with ASP.NET Core
- Strong support from AI coding assistants

---

# 9. Backend Framework

The backend uses:

```text
ASP.NET Core
```

ASP.NET Core is responsible for:

- REST API endpoints
- Authentication
- Authorization
- Business workflows
- Validation
- Database access coordination
- Application services
- Logging
- Error handling

Business logic should not be placed directly inside API controllers.

---

# 10. API Style

Frontend and backend communicate through:

```text
REST API
```

REST was selected because it is:

- Simple
- Well understood
- Easy to debug
- Well supported by React and ASP.NET Core
- Suitable for future integrations

Version 1 does not require:

- GraphQL
- gRPC for browser communication
- Event-driven APIs

These should not be introduced without a real requirement.

---

# 11. Database

The approved database engine is:

```text
PostgreSQL
```

PostgreSQL stores the persistent ERP data.

Reasons for selection:

- Strong relational database capabilities
- Reliability
- Transaction support
- Excellent indexing
- Good reporting capabilities
- Open-source license
- Strong Entity Framework Core support
- Long-term stability

---

# 12. Database Architecture

Version 1 uses:

```text
One PostgreSQL database
```

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

This provides logical separation without the complexity of multiple database servers.

---

# 13. Database Naming

PostgreSQL identifiers use:

```text
lowercase snake_case
```

Examples:

```text
auth.users

projects.project_members

orders.checklist_items

project_id

created_at

selling_price
```

Avoid PascalCase database identifiers.

Avoid quoted database identifiers unless absolutely necessary.

---

# 14. Object-Relational Mapping

The backend uses:

```text
Entity Framework Core
```

Entity Framework Core is responsible for:

- Entity mapping
- Database queries
- Relationships
- Transactions
- Database migrations

Raw SQL may be used only when there is a clear technical reason.

The default approach should remain Entity Framework Core.

---

# 15. Database Migrations

All database schema changes must be managed through:

```text
Entity Framework Core Migrations
```

Database structure must not be changed manually during normal development or deployment.

Migrations provide:

- Version control
- Reproducible database changes
- Development consistency
- Deployment history

---

# 16. Authentication Model

Version 1 uses local Lithograph ERP authentication.

Login requires:

```text
Username
Password
```

Version 1 does not require:

- Email login
- Email verification
- Google login
- Microsoft login
- OAuth providers
- Active Directory
- LDAP

There is no public registration.

Users are created internally by authorized ERP users.

---

# 17. Authentication Security

Authentication must use established ASP.NET Core security mechanisms.

Do not create custom cryptographic algorithms.

Use trusted framework functionality for:

- Password hashing
- Secure random token generation
- Authentication
- Authorization
- Cookie and session security

Passwords must never be:

- Stored in plain text
- Logged
- Returned by APIs
- Recoverable from the database

---

# 18. Authentication Session Strategy

Version 1 should use secure server-recognized authentication sessions suitable for the React + ASP.NET Core architecture.

Authentication credentials or session identifiers must be protected against client-side script access where practical.

The detailed session implementation belongs to the Authentication module specification.

Do not introduce unnecessarily complex token infrastructure unless required.

---

# 19. Authorization

Authorization is permission-based.

Users receive permissions through Roles.

General structure:

```text
User
  │
  ▼
Role
  │
  ▼
Permissions
```

Example permissions:

```text
orders.view

orders.create

orders.edit

users.manage_roles
```

Business modules define the permissions they require.

---

# 20. First-Run Setup

When Lithograph ERP starts for the first time and no user exists, the system starts initial setup.

The setup asks for the initial Director password.

The system creates:

```text
Username: director
Role: Director
```

The Director receives full system access.

The Director may later change the username and password.

No email account is required.

---

# 21. File and Folder Handling

Version 1 does not manage physical production files.

The ERP may store folder paths related to Orders.

Example:

```text
\\server\orders\2026\ORD-2026-000125
```

Version 1 does not include:

- Desktop helper application
- Windows Explorer launching
- Automatic folder creation
- File synchronization
- File server management

These may be considered later.

---

# 22. Calculator Technology

The Order Calculator is implemented as part of Lithograph ERP.

It must not require Microsoft Excel to run.

The calculator consists of:

- Calculator Templates
- Template Designer
- Input fields
- Calculated fields
- Tables
- Formula expressions
- Approximately 30 approved spreadsheet-like functions

The exact implementation and supported functions are defined by the Calculator module documentation.

---

# 23. Reporting

Reports should primarily use data stored in PostgreSQL.

Version 1 reporting remains simple.

Reports may calculate values such as:

```text
profit = selling_price - cost_price
```

A separate analytics platform or data warehouse is not required in Version 1.

---

# 24. Deployment Architecture

Version 1 should support simple deployment.

Typical deployment:

```text
Application Server
├── ASP.NET Core Backend
├── React Frontend
└── PostgreSQL
```

The exact physical arrangement may vary.

The application should not require distributed infrastructure.

---

# 25. Docker

Docker is allowed and recommended when it simplifies:

- Development setup
- Testing
- Deployment consistency
- PostgreSQL setup

However, Docker is not a business requirement.

The application architecture must not depend unnecessarily on Docker-specific behavior.

---

# 26. Operating System

The backend should remain compatible with supported environments appropriate for ASP.NET Core and PostgreSQL.

Typical choices include:

- Windows
- Linux

Application business logic must not unnecessarily depend on a specific server operating system.

---

# 27. Client Operating Environment

Version 1 primarily targets desktop computers using modern web browsers.

Primary environment:

```text
Windows desktop
+
Modern browser
```

The web application itself should remain browser-based.

---

# 28. Mobile and Tablet

Version 1 is optimized for desktop use.

Tablet compatibility is desirable where it comes naturally from responsive design.

Dedicated mobile support is not required.

A mobile application is outside Version 1.

---

# 29. Source Control

The project uses:

```text
Git
```

Git tracks:

- Source code
- Documentation
- Database migrations
- Configuration templates
- Tests

Secrets and production credentials must never be committed to Git.

---

# 30. Repository Structure

The initial repository structure should remain simple.

```text
Lithograph-ERP/
│
├── README.md
├── AI_RULES.md
│
├── docs/
│
├── backend/
│
├── frontend/
│
└── tests/
```

Additional folders should be created only when required.

---

# 31. Development Environment

Recommended development tools include:

- Cursor
- Visual Studio
- Visual Studio Code
- JetBrains Rider

AI-assisted development may use tools such as:

- Claude
- ChatGPT
- Cursor AI

No specific AI provider is part of the runtime architecture.

---

# 32. Package Management

Backend dependencies should use the standard .NET package ecosystem.

Frontend dependencies should use the standard Node.js package ecosystem.

The project should avoid unnecessary dependencies.

Before adding a package, verify that the required functionality is not already available through:

- .NET
- ASP.NET Core
- React
- Material UI
- Existing project dependencies

---

# 33. Logging

ASP.NET Core's standard logging architecture should be used.

Logs should help diagnose:

- Application errors
- Authentication problems
- Integration failures
- Unexpected system behavior

Logs must never contain:

- Passwords
- Password hashes
- Session tokens
- Sensitive credentials

Advanced centralized logging infrastructure is not required in Version 1.

---

# 34. Error Handling

Backend errors must be handled centrally and consistently.

Users should receive understandable error messages.

The frontend must not receive unnecessary internal technical details.

Do not expose:

- Stack traces
- SQL queries
- Database credentials
- Internal configuration
- Authentication secrets

---

# 35. Testing

The project should support automated testing.

Important areas include:

- Business rules
- Authentication
- Authorization
- Database behavior
- Calculator formulas
- Important workflows

The exact test structure will evolve with implementation.

Testing should provide value rather than unnecessary complexity.

---

# 36. Security

Security is part of normal application development, not a separate future feature.

Version 1 must include appropriate protection for:

- Authentication
- Authorization
- Passwords
- Sessions
- Database access
- API input
- Error responses
- Application secrets

Security should use established framework capabilities wherever possible.

---

# 37. Version 1 Architecture Restrictions

Version 1 does not require:

```text
Microservices

Kubernetes

Message Brokers

Event Sourcing

CQRS Infrastructure

Distributed Caches

Distributed Transactions

Service Mesh

Separate Database Per Module

GraphQL

Desktop Agent

Mobile Application
```

These technologies must not be introduced without a demonstrated requirement.

---

# 38. Future Expansion

The selected technology stack must allow future additions such as:

- Warehouse
- Purchasing
- Finance
- Production planning
- Customer portal
- Supplier portal
- AI integrations
- Desktop integration
- External APIs

Future capability does not justify implementing these features today.

---

# 39. Technology Decision Rule

When deciding whether to introduce a new technology, ask:

1. Does the current requirement need it?
2. Can the approved stack already solve the problem?
3. Does it simplify the system or make it harder to maintain?
4. Will another developer understand why it exists?
5. Does it introduce a new operational dependency?

If the existing stack can solve the requirement simply, prefer the existing stack.

---

# 40. Approved Stack Summary

```text
Frontend
React
TypeScript
Material UI

Backend
C#
ASP.NET Core

ORM
Entity Framework Core

Database
PostgreSQL

API
REST

Architecture
Modular Monolith

Authentication
Local Username + Password

Version Control
Git

Documentation
Markdown
```

This technology stack is the approved foundation of Lithograph ERP Version 1.

---

**End of Document**