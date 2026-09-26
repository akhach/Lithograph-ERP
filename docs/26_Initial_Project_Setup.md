# Lithograph ERP

**Document:** 26_Initial_Project_Setup.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `01_Technology_Stack.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `17_Database_Schema_Overview.md`
- `18_Implementation_Roadmap.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `21_Deployment_and_Backup.md`
- `23_Frontend_Architecture.md`
- `24_Backend_Architecture.md`
- `25_Development_Workflow_for_AI.md`

---

# 1. Purpose

This document defines the exact initial setup process for Lithograph ERP before implementation of business modules begins.

The objective is to establish a clean working foundation containing:

```text
Git repository

ASP.NET Core backend

React + TypeScript frontend

PostgreSQL connectivity

Entity Framework Core

Testing projects

Base configuration

Module folder structure

Health check

Development documentation
```

At the end of this setup, the system should run but contain almost no business functionality.

---

# 2. Day-Zero Principle

The initial setup should prove that the technology stack works together.

It should not attempt to implement:

```text
Authentication

Employees

Clients

Projects

Orders

Calculator

Reports
```

yet.

The foundation must be stable before business modules are added.

---

# 3. Target Repository Structure

After initial setup:

```text
Lithograph-ERP/
│
├── README.md
├── AI_RULES.md
├── .gitignore
│
├── docs/
│   ├── 00_Project_Vision.md
│   ├── 01_Technology_Stack.md
│   ├── ...
│   └── 26_Initial_Project_Setup.md
│
├── backend/
│   ├── LithographERP.sln
│   ├── src/
│   │   ├── LithographERP.Api/
│   │   ├── LithographERP.Application/
│   │   ├── LithographERP.Domain/
│   │   └── LithographERP.Infrastructure/
│   │
│   └── tests/
│       ├── LithographERP.UnitTests/
│       └── LithographERP.IntegrationTests/
│
└── frontend/
    └── ...
```

The exact generated frontend substructure may depend on the selected React build tool.

---

# 4. Pre-Setup Requirements

Development machine should have supported versions of:

```text
Git

.NET SDK

Node.js

npm

PostgreSQL
```

Optional but useful:

```text
Docker
```

for integration tests.

---

# 5. Version Rule

Use currently supported stable versions compatible with the chosen project stack.

Do not hardcode dependency versions in this document unless the repository later formally locks them.

Before installation, inspect the current project/runtime requirements.

---

# 6. Git Repository

Initialize Git at repository root.

Conceptually:

```text
Lithograph-ERP/
```

is the Git root.

Do not create separate Git repositories for frontend and backend in Version 1.

---

# 7. Root .gitignore

The root `.gitignore` should exclude at minimum:

```text
**/bin/
**/obj/

**/node_modules/
**/dist/

.env
.env.*

*.user
*.suo

IDE temporary files

local secrets

local logs where appropriate
```

Do not ignore source-controlled migrations.

---

# 8. Environment Example Files

If environment files are used, provide examples containing configuration keys only.

Example:

```text
.env.example
```

must not contain real secrets.

---

# 9. Documentation Placement

Place all approved numbered documents inside:

```text
docs/
```

Keep:

```text
README.md

AI_RULES.md
```

at repository root.

---

# 10. Initial Git Commit

Before code generation, create a clean documentation commit.

Conceptual commit:

```text
docs: add Lithograph ERP architecture specifications
```

This creates a stable baseline before implementation begins.

---

# 11. Backend Solution Creation

Create:

```text
backend/LithographERP.sln
```

The solution should contain the approved backend projects.

---

# 12. API Project

Create:

```text
LithographERP.Api
```

as the ASP.NET Core Web API host.

Responsibilities:

```text
Application startup

HTTP endpoints

Middleware

Authentication/authorization wiring

OpenAPI

Dependency injection composition
```

---

# 13. Application Project

Create:

```text
LithographERP.Application
```

Responsibilities:

```text
Use cases

Application services

DTOs where appropriate

Business-operation coordination

Application interfaces
```

---

# 14. Domain Project

Create:

```text
LithographERP.Domain
```

Responsibilities may include:

```text
Core domain types

Enums/value concepts

Business rules

Formula concepts
```

Do not force simple CRUD behavior into complicated domain abstractions.

---

# 15. Infrastructure Project

Create:

```text
LithographERP.Infrastructure
```

Responsibilities:

```text
Entity Framework Core

PostgreSQL

DbContext

Entity configurations

Migrations

Persistence implementations

File storage implementations
```

---

# 16. Project References

Conceptual dependency direction:

```text
Domain
   ↑
Application
   ↑
Api
```

Infrastructure supplies technical implementations.

A practical structure may be:

```text
Api
├── references Application
└── references Infrastructure

Application
└── references Domain

Infrastructure
├── references Application
└── references Domain
```

Avoid circular project references.

---

# 17. Dependency Principle

Domain must not depend on:

```text
ASP.NET Core

Entity Framework Core

React

PostgreSQL driver
```

Application should remain mostly independent from HTTP details.

---

# 18. Test Projects

Create:

```text
LithographERP.UnitTests

LithographERP.IntegrationTests
```

---

# 19. Unit Test Project

Unit Tests should reference the projects needed to test pure business logic.

Do not require PostgreSQL for ordinary Unit Tests.

---

# 20. Integration Test Project

Integration Tests may reference:

```text
API

Infrastructure

Application
```

as needed to test the real application/database pipeline.

---

# 21. Backend Module Folders

Inside appropriate backend projects, prepare module-oriented folders:

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

Do not populate them with speculative classes yet.

---

# 22. Avoid Empty Architecture Explosion

Do not generate dozens of placeholder files such as:

```text
AuthenticationService.cs

EmployeeRepository.cs

ProjectManager.cs
```

before functionality exists.

Folders are enough.

---

# 23. Entity Framework Core

Add the required EF Core packages for:

```text
Entity Framework Core

PostgreSQL provider

Migration tooling
```

Use the PostgreSQL provider appropriate for EF Core.

---

# 24. DbContext

Create the initial application DbContext.

Recommended conceptual name:

```text
LithographDbContext
```

It should initially contain no unnecessary business entities.

---

# 25. DbContext Location

The DbContext belongs in:

```text
Infrastructure
```

because it is persistence infrastructure.

---

# 26. DbContext Registration

Register the DbContext through ASP.NET Core dependency injection.

The connection string must come from configuration.

---

# 27. PostgreSQL Development Database

Create a dedicated development database.

Conceptual name:

```text
lithograph_erp_dev
```

The exact name is configurable.

Do not use the future production database.

---

# 28. Development Database User

Use a dedicated development PostgreSQL account where practical.

Avoid using the PostgreSQL superuser as the normal application account.

---

# 29. Connection String

Store development connection information outside source control.

Possible sources:

```text
ASP.NET Core User Secrets

Environment Variables

Local non-committed configuration
```

---

# 30. appsettings Files

Source-controlled configuration may include:

```text
appsettings.json

appsettings.Development.json
```

but these files must not contain real secrets.

---

# 31. Connection String Placeholder

Safe configuration may contain only a placeholder or configuration structure.

Real credentials remain external.

---

# 32. User Secrets

During local development, ASP.NET Core User Secrets are recommended for:

```text
Database password

Sensitive development configuration
```

---

# 33. PostgreSQL Schemas

Version 1 approved schemas are:

```text
auth

employees

clients

projects

orders

calculator
```

Initial setup may create them through the first migration or when their corresponding module tables are introduced.

---

# 34. Schema Strategy

Do not manually create production schemas outside EF migrations and then forget to represent them in code.

Database structure should remain reproducible.

---

# 35. Initial Migration

There are two acceptable approaches.

Option A:

```text
Create an initial infrastructure migration
```

containing only genuinely required base database setup.

Option B:

```text
Wait until Authentication entities exist
```

and create the first meaningful migration then.

---

# 36. Recommended Initial Migration Choice

Prefer:

```text
Wait until Authentication database entities are implemented.
```

There is little value in creating an empty migration solely to say migrations exist.

---

# 37. EF Migration Location

Keep migrations in a predictable Infrastructure location.

Example:

```text
Infrastructure/Persistence/Migrations/
```

Exact folder may follow current project conventions.

---

# 38. Database Connectivity Verification

During initial setup, verify that backend can open a PostgreSQL connection.

Do not require business tables.

---

# 39. Health Endpoint

Create:

```text
GET /health
```

or the chosen consistent health route.

Initial response should confirm application availability.

---

# 40. Database Health

The health check may include PostgreSQL connectivity.

Conceptually:

```text
Application:
Healthy

Database:
Healthy
```

Do not expose credentials or detailed database internals.

---

# 41. Development OpenAPI

Enable:

```text
Swagger / OpenAPI
```

in Development.

This confirms the API host is functioning.

---

# 42. Swagger Production Rule

Do not assume Swagger remains publicly enabled in production.

Production exposure is a deployment decision.

---

# 43. Global Error Handling

Set up the baseline global error-handling mechanism.

It should be ready to return the standard API error structure later.

---

# 44. Initial Error Response Shape

The infrastructure should support:

```text
code

message

errors
```

according to API guidelines.

No business-specific error codes are needed yet.

---

# 45. Correlation ID

Prepare simple request correlation support if easy within the chosen logging/error approach.

Do not overbuild distributed tracing.

---

# 46. Logging

Use ASP.NET Core logging.

Initial setup should confirm:

```text
Application startup logging

Application shutdown logging

Unexpected error logging
```

works.

---

# 47. No Custom Logging System

Do not create:

```text
LogRepository

Log database tables

Custom logging framework
```

during initial setup.

---

# 48. CORS

During development, configure CORS to permit the local React development origin.

Do not use unrestricted production CORS configuration.

---

# 49. Frontend Creation

Create the React frontend using a modern supported React + TypeScript toolchain.

The project must use:

```text
React

TypeScript
```

---

# 50. Material UI

Install and configure:

```text
Material UI
```

as the primary component library.

Do not install a second competing UI component system.

---

# 51. Frontend Initial Structure

Prepare:

```text
src/
├── app/
├── api/
├── components/
├── hooks/
├── layouts/
├── modules/
├── routes/
├── theme/
├── types/
├── utilities/
└── main.tsx
```

Only create useful base files.

---

# 52. Frontend Module Folders

Prepare:

```text
modules/
├── auth/
├── employees/
├── clients/
├── projects/
├── orders/
├── calculator/
└── reports/
```

Do not create full screens yet.

---

# 53. Material UI Theme

Create one base theme.

Initial theme only needs:

```text
Typography

Basic spacing/component defaults

Application palette
```

Do not spend significant development time on visual branding yet.

---

# 54. App Shell

Create a minimal App shell capable of rendering:

```text
Application Title

Placeholder content
```

The full authenticated navigation can be added during Authentication/frontend foundation work.

---

# 55. Development Page

A simple development page may show:

```text
Lithograph ERP

Frontend is running.
```

This is temporary foundation UI.

---

# 56. Backend Connectivity Test

Frontend should be able to call:

```text
GET /health
```

and show a simple successful result during setup verification.

This proves:

```text
Browser
→ React
→ ASP.NET Core
→ PostgreSQL
```

connectivity.

---

# 57. API Base URL

Configure frontend API base URL externally.

Do not hardcode:

```text
http://localhost:xxxx
```

throughout source files.

---

# 58. Development Environment Variable

A frontend development configuration may define the API URL.

Remember:

```text
Frontend environment variables are not secrets.
```

Anything shipped to browser is visible.

---

# 59. Shared API Client

Create one minimal API client.

It should support:

```text
Base URL

JSON requests

Basic error handling
```

Authentication behavior will be added in the Authentication phase.

---

# 60. Do Not Add Complex Data Library Yet

If a server-state/data-fetching library has not yet been selected, initial setup does not require one.

Choose it only when first real API screens justify it.

---

# 61. Routing

Set up basic frontend routing infrastructure.

Initial routes may contain only:

```text
/
```

and perhaps a simple development/placeholder page.

Authentication routes come later.

---

# 62. Route Infrastructure

Routing should already support future stable paths such as:

```text
/clients

/projects

/orders
```

but do not create fake module pages merely to populate routes.

---

# 63. TypeScript

Configure TypeScript with reasonably strict settings.

Do not globally disable:

```text
strict
```

or weaken typing merely because generated code is easier that way.

---

# 64. Linting

Configure one maintained linting approach.

Keep it practical.

Do not spend excessive time configuring dozens of stylistic rules.

---

# 65. Formatting

Use one formatter consistently.

Formatting should be automated.

---

# 66. Frontend Build Verification

The frontend must successfully complete its production/type-check build.

The initial generated application must not contain unresolved TypeScript errors.

---

# 67. Backend Build Verification

Run backend build.

All projects must compile.

---

# 68. Unit Test Baseline

Add at least one small baseline test if useful to prove the test runner works.

Example:

```text
Basic test project executes successfully.
```

Do not retain meaningless tests indefinitely.

---

# 69. Integration Test Baseline

Create infrastructure allowing Integration Tests to eventually start the ASP.NET Core application and PostgreSQL test database.

A full business integration test is not required yet.

---

# 70. PostgreSQL Test Strategy

Prefer real PostgreSQL for Integration Tests.

Do not configure EF Core's in-memory provider as the primary integration database.

---

# 71. Testcontainers Preparation

If Docker is available and Testcontainers is selected, initial setup may verify:

```text
Start PostgreSQL container

Connect

Stop container
```

Keep the implementation simple.

---

# 72. Testcontainers Is Optional

Do not block the entire project if Testcontainers is inconvenient on the development machine.

A dedicated isolated test PostgreSQL database is also valid.

---

# 73. Configuration Validation

On backend startup, important required configuration should fail clearly when absent.

Example:

```text
Database connection configuration missing.
```

Better to fail clearly than continue in a half-configured state.

---

# 74. Development Error Details

Development may show detailed error information.

Production configuration must not expose internal exception details.

---

# 75. HTTPS Development

Use normal ASP.NET Core HTTPS development configuration where practical.

Do not weaken production security because local HTTPS setup is inconvenient.

---

# 76. Secrets Verification

Before first code commit, search for accidental:

```text
Passwords

Connection strings with passwords

API secrets

Tokens
```

---

# 77. Backend Package Review

After initial package installation, remove packages that are not actually needed.

Do not carry template-generated dependencies blindly.

---

# 78. Frontend Package Review

Likewise remove unused starter packages.

The dependency list should remain intentional.

---

# 79. Template Cleanup

Generated sample code should be removed.

Examples:

```text
WeatherForecast

Counter demo

Sample React logo content
```

Do not let starter-template examples remain mixed with production architecture.

---

# 80. No Business Entities Yet

At the end of initial setup, do not create placeholder entities such as:

```text
Client

Project

Order
```

until their implementation phase begins.

Documentation already defines them.

Code should be added when the module is implemented and tested.

---

# 81. No Authentication Stub

Do not create fake authentication such as:

```text
Always authenticated

Hardcoded admin user

Temporary password bypass
```

Authentication is the next real module and should be implemented correctly.

---

# 82. No Hardcoded Director

Initial setup should not create a hardcoded Director account.

The proper first-run bootstrap belongs to Authentication implementation.

---

# 83. No Business Seed Data

Do not seed:

```text
Clients

Employees

Projects

Orders

Order Types
```

during foundation setup.

---

# 84. Permission Infrastructure

Do not prematurely implement permission tables during day-zero setup.

That belongs to Authentication.

---

# 85. Base Backend Startup Flow

At this phase:

```text
Start backend
    ↓
Load configuration
    ↓
Configure logging
    ↓
Configure DbContext
    ↓
Connect to PostgreSQL
    ↓
Map /health
    ↓
Map Swagger in Development
    ↓
Run
```

---

# 86. Base Frontend Startup Flow

```text
Start frontend
    ↓
Load frontend configuration
    ↓
Render React application
    ↓
Call backend /health
    ↓
Show connection result
```

This is enough to validate the stack.

---

# 87. Development Run Documentation

README should contain basic development startup instructions.

At minimum:

```text
How to start PostgreSQL

How to configure backend connection

How to start backend

How to start frontend

How to run tests
```

Do not rely on developer memory.

---

# 88. README Scope

README should remain a practical entry point.

Detailed architecture stays in:

```text
docs/
```

---

# 89. Local Development Commands

Document actual commands only after project scaffolding determines them.

Do not write fictional commands that have not been verified.

---

# 90. Ports

Development ports may use framework defaults or explicit configuration.

Do not treat local port numbers as business architecture.

---

# 91. API Proxy Option

Frontend development may use:

```text
Direct API base URL
```

or a development proxy.

Choose one simple approach and keep it consistent.

---

# 92. Production Serving Model

Initial setup should not force a final production bundling method prematurely.

Likely options:

```text
React static files served by reverse proxy

or

React static files served by ASP.NET Core
```

The production choice can be finalized during deployment implementation.

---

# 93. Backend Namespace Convention

Use consistent namespaces based on:

```text
LithographERP
```

and module/project structure.

Do not introduce inconsistent abbreviations.

---

# 94. Frontend Naming Convention

React components:

```text
PascalCase
```

Functions/variables:

```text
camelCase
```

Types:

```text
PascalCase
```

Follow normal TypeScript conventions.

---

# 95. Database Naming Convention

Database remains:

```text
lowercase snake_case
```

as already approved.

---

# 96. JSON Naming Convention

Select one API JSON naming convention and configure it globally.

Recommended:

```text
camelCase
```

because it integrates naturally with React/TypeScript.

Database remains snake_case independently.

---

# 97. Enum JSON Convention

If string enums are used, serialize stable lowercase values such as:

```text
draft

active

on_hold

completed

cancelled
```

Use the same convention consistently.

---

# 98. Culture

Backend calculations must not depend on developer-machine locale for decimal parsing/serialization.

JSON and database behavior must remain predictable.

---

# 99. Encoding

Use UTF-8 across:

```text
Source files

JSON

Documentation
```

This supports Armenian, English, and other text safely.

---

# 100. Armenian Text Support

PostgreSQL and frontend must support Unicode text naturally.

No special Armenian-specific database encoding system is required.

---

# 101. Source File Line Endings

Use repository tooling/editor settings to keep line endings consistent where practical.

Do not allow AI tools to create huge meaningless diffs from line-ending changes.

---

# 102. Editor Configuration

An `.editorconfig` is recommended.

It may define basic:

```text
Indentation

Encoding

Line endings

C# conventions
```

Keep it practical.

---

# 103. Root Tooling Files

Possible root files:

```text
.editorconfig

.gitignore
```

Do not introduce monorepo build systems unless required.

---

# 104. No Nx/Turborepo Requirement

Version 1 does not need:

```text
Nx

Turborepo

Bazel
```

to manage one backend and one frontend.

---

# 105. No Docker Compose Requirement

Docker Compose may be useful, especially for PostgreSQL development.

It is optional.

Do not make it mandatory unless the development team prefers it.

---

# 106. Optional Development Docker Compose

A simple setup could contain:

```text
PostgreSQL
```

only.

Backend/frontend may run directly from development tools.

This can simplify debugging.

---

# 107. Docker Secrets

If Docker Compose is used locally, do not commit real production secrets.

Development credentials should remain clearly non-production.

---

# 108. Health Check Test

Create a basic Integration Test verifying:

```text
GET /health
→ successful response
```

where practical.

This provides a useful baseline test for the application host.

---

# 109. Database Connectivity Test

Health Integration Test may verify PostgreSQL is accessible.

This proves test database infrastructure works.

---

# 110. CI Preparation

Initial repository should be structured so CI can later run:

```text
dotnet build

dotnet test

frontend install

frontend build

frontend tests
```

A complete CI pipeline is optional during initial setup.

---

# 111. Package Lock

Commit frontend package lock file.

This ensures repeatable dependency installation.

---

# 112. .NET Dependency Locking

Use normal .NET project/package management.

Additional lock-file infrastructure is optional unless team policy requires it.

---

# 113. Warning Policy

Compiler warnings should be reviewed.

Do not globally suppress broad classes of warnings just to obtain a clean build.

---

# 114. Nullable Reference Types

Enable modern C# nullable reference type checking.

Do not disable it globally to simplify AI-generated code.

---

# 115. Treat Warnings as Errors

This may be enabled selectively later.

It is not mandatory if it creates unnecessary friction during early setup.

Important warnings should still be addressed.

---

# 116. Frontend Strict Mode

React development Strict Mode may remain enabled if compatible with chosen tooling.

Code should not depend on unsafe side effects.

---

# 117. API Controller Base Convention

If Controllers are chosen, establish a simple route convention:

```text
/api/[controller]
```

or explicit equivalent.

Do not create complex controller inheritance hierarchies.

---

# 118. API Response Convention

Do not wrap every successful response inside unnecessary generic structures such as:

```text
{
  success: true,
  data: ...
}
```

unless the project explicitly chooses that convention.

Use normal HTTP semantics and DTOs.

---

# 119. Error Response Convention

Errors do use the approved common structure:

```text
code
message
errors
```

---

# 120. Date/Time Foundation

Introduce a simple clock abstraction when business functionality first requires it.

It does not need to be implemented during day-zero unless used by baseline infrastructure.

---

# 121. Current User Abstraction

Likewise:

```text
ICurrentUser
```

belongs to Authentication implementation.

Do not create a fake current User during initial setup.

---

# 122. Formula Engine

Do not add Formula Engine libraries during initial setup.

Calculator implementation comes later.

---

# 123. File Storage

Do not implement Preview Image storage during foundation setup.

Only prepare architecture when Orders/Preview functionality begins.

---

# 124. Reports

Do not add reporting libraries during initial setup.

Version 1 reports use normal backend queries and frontend tables initially.

---

# 125. Initial Setup Acceptance Criteria

The initial project setup is complete when:

```text
Repository structure exists

Documentation committed

Backend solution builds

Frontend builds

ASP.NET Core application runs

React application runs

PostgreSQL connection works

/health responds successfully

Frontend can reach /health

Swagger works in Development

Unit test project runs

Integration test infrastructure works

No secrets are committed

No business modules are implemented prematurely
```

---

# 126. Initial Setup Manual Verification

Manually verify:

```text
1. Start PostgreSQL.

2. Start backend.

3. Open Swagger.

4. Call /health.

5. Start frontend.

6. Open frontend in browser.

7. Confirm backend health connection.

8. Run backend build.

9. Run backend tests.

10. Run frontend build.
```

---

# 127. Initial Commit After Scaffolding

After acceptance criteria pass, create a clean commit.

Recommended style:

```text
chore: initialize Lithograph ERP project
```

This commit represents the technical foundation.

---

# 128. First Milestone State

After the initial setup commit, the application should conceptually be:

```text
Lithograph ERP

Backend:
Running

Frontend:
Running

PostgreSQL:
Connected

Business Modules:
Not yet implemented
```

This is the correct state.

---

# 129. Next Implementation Phase

Only after this foundation is stable should development proceed to:

```text
Authentication
```

including:

```text
Users

Roles

Permissions

Sessions

Director bootstrap
```

---

# 130. AI Prompt for Initial Setup

A suitable Cursor/Claude prompt may be:

```text
Read:
- AI_RULES.md
- docs/01_Technology_Stack.md
- docs/02_Architecture.md
- docs/03_Database_Design.md
- docs/18_Implementation_Roadmap.md
- docs/20_Testing_Strategy.md
- docs/23_Frontend_Architecture.md
- docs/24_Backend_Architecture.md
- docs/25_Development_Workflow_for_AI.md
- docs/26_Initial_Project_Setup.md

Task:
Create only the initial Lithograph ERP project foundation.

Create:
- .NET solution and approved backend projects
- React + TypeScript frontend
- Material UI setup
- PostgreSQL EF Core connection
- base DbContext
- Swagger in Development
- /health endpoint
- unit and integration test projects
- base frontend folder structure
- simple frontend health check
- root .gitignore and .editorconfig if missing

Do not implement:
- Authentication
- Users
- Employees
- Clients
- Projects
- Orders
- Calculator
- Reports
- business entities
- business migrations
- fake admin login
- generic repositories
- CQRS/MediatR
- extra frameworks

Requirements:
- use existing approved repository structure
- do not commit secrets
- remove template/demo code
- use modular folder structure
- keep the implementation minimal

Before finishing:
- run backend build
- run backend tests
- run frontend build/type check
- verify /health
- verify PostgreSQL connectivity
- review changed files
- report any failures honestly
```

---

# 131. Initial Setup Non-Goals

This phase does not include:

```text
Authentication

Authorization

Business database tables

Business ID generation

User administration

Employees

Clients

Projects

Orders

Calculator

Reports

Dashboard

Production deployment

Advanced CI/CD

Microservices

Generic Repository

CQRS

Message Broker
```

---

# 132. Avoid Premature Success Claims

A running blank application is not:

```text
ERP Version 1 complete
```

It is only:

```text
ERP technical foundation ready.
```

This distinction should remain clear.

---

# 133. Foundation Simplicity Rule

Before adding infrastructure during setup, ask:

```text
Is this required for the first real module?
```

If not, defer it.

---

# 134. Final Initial-Setup Principle

The purpose of Day Zero is not to predict every technical need.

It is to establish a clean platform on which the approved modules can be implemented safely.

The desired result is:

```text
Small foundation

Clean build

Working database connection

Working frontend/backend communication

Working tests

No business complexity yet
```

The central rule is:

```text
Build the foundation once.

Then build the ERP one verified module at a time.
```

---

**End of Document**