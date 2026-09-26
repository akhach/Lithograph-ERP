# Lithograph ERP

**Document:** 21_Deployment_and_Backup.md  
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

---

# 1. Purpose

This document defines the Version 1 deployment, configuration, backup, restore, logging, and production-safety strategy for Lithograph ERP.

The goal is:

```text
Simple

Reliable

Recoverable

Secure

Easy to maintain
```

Version 1 does not require enterprise-scale infrastructure.

---

# 2. Deployment Principle

Lithograph ERP is an internal business application.

Preferred architecture:

```text
Users
  ↓
Web Browser
  ↓
Lithograph ERP Server
  ├── ASP.NET Core Backend
  ├── React Frontend
  └── PostgreSQL Database
```

PostgreSQL may run on the same server initially or on a separate database server if operational needs justify it.

---

# 3. Version 1 Deployment Model

Preferred simple deployment:

```text
Internal Server / VM
│
├── Reverse Proxy / HTTPS
│
├── ASP.NET Core Application
│
├── React Static Files
│
└── PostgreSQL
```

This is sufficient for Version 1.

---

# 4. No Microservices

Do not deploy separate services for:

```text
Authentication

Employees

Clients

Projects

Orders

Calculator

Reports
```

They remain one Modular Monolith application.

---

# 5. No Kubernetes

Version 1 does not require:

```text
Kubernetes

Service Mesh

Container Orchestrator

Autoscaling Cluster
```

These would add unnecessary operational complexity.

---

# 6. Containers

Docker may be used if it simplifies deployment.

Possible structure:

```text
Application Container

PostgreSQL Container
```

However, Docker is not mandatory.

Do not adopt containers solely because they are fashionable.

---

# 7. Server Operating System

Use a stable supported server operating system.

Typical choices:

```text
Windows Server

Linux Server
```

The exact OS may be selected based on Lithograph's available infrastructure and administration comfort.

The application itself should not depend on desktop-only behavior.

---

# 8. Internal Network Access

Version 1 may initially be accessible only inside Lithograph's trusted network.

Example:

```text
https://erp.lithograph.local
```

or an internal DNS equivalent.

Exact DNS naming is an infrastructure decision.

---

# 9. Internet Exposure

Do not expose the ERP directly to the public internet unless there is a real business need and the deployment security has been reviewed.

Internal-only access is preferable for Version 1.

---

# 10. Remote Access

If remote access becomes necessary, prefer a controlled secure solution such as:

```text
VPN

Secure private network

Approved remote-access infrastructure
```

rather than simply opening the ERP server publicly.

---

# 11. HTTPS

Production must use:

```text
HTTPS
```

even on an internal network where practical.

Authentication credentials and Sessions must not travel over plain HTTP.

---

# 12. Reverse Proxy

A reverse proxy is recommended for production.

Possible responsibilities:

```text
HTTPS termination

Request forwarding

Static file delivery

Security headers

Request size limits
```

The exact product may be selected during deployment.

---

# 13. Frontend Deployment

React production build creates static files.

Conceptually:

```text
React Source
    ↓
Production Build
    ↓
Static Files
    ↓
Served by Application / Reverse Proxy
```

Do not run the React development server in production.

---

# 14. Backend Deployment

ASP.NET Core should run using the production runtime configuration.

Do not deploy using:

```text
dotnet watch
```

or other development-only processes.

---

# 15. Production Database

Production uses:

```text
PostgreSQL
```

with a dedicated Lithograph ERP database.

Do not use the development database in production.

---

# 16. Environment Separation

At minimum maintain:

```text
Development

Production
```

Prefer also:

```text
Test
```

for automated integration testing.

---

# 17. Configuration Sources

Production configuration may come from:

```text
Environment Variables

Protected configuration files

Secret storage appropriate to the host
```

Never commit production secrets to Git.

---

# 18. Secrets

Secrets include:

```text
Database password

Session/authentication secrets

Encryption/signing secrets if used

External service credentials if added later
```

These must not appear in:

```text
README

Markdown documentation

Source code

Git history

Frontend JavaScript bundle
```

---

# 19. Connection String

The PostgreSQL connection string is production configuration.

It should not be hardcoded.

Conceptually:

```text
ConnectionStrings__DefaultConnection
```

or equivalent ASP.NET Core configuration.

---

# 20. Least-Privilege Database Account

The application should connect to PostgreSQL using a dedicated application database account.

Do not run the ERP permanently using a PostgreSQL superuser account.

---

# 21. Database Permissions

The application database user should have only the permissions necessary for:

```text
Normal application reads/writes

Required schema operations during controlled migrations
```

If desired, migration credentials may later be separated from runtime credentials.

This is optional for Version 1.

---

# 22. First Production Deployment

Recommended sequence:

```text
Prepare server

Install runtime/dependencies

Create PostgreSQL database

Configure secrets

Deploy application

Apply EF Core migrations

Start application

Open Initial Setup

Create Director password

Login

Run smoke tests
```

---

# 23. Production Migration Principle

Database schema changes must be deliberate.

Use:

```text
Reviewed EF Core migrations
```

Do not let production schema drift from source-controlled migrations.

---

# 24. Migration Deployment

Recommended process:

```text
Backup database

Stop or protect application if necessary

Apply migration

Start/update application

Run smoke tests

Verify logs
```

---

# 25. Migration Execution

Migrations may be applied by:

```text
Deployment script

Administrative migration command

Controlled application startup process
```

The chosen method must be predictable.

Do not hide dangerous schema changes inside normal user requests.

---

# 26. Automatic Migration on Every Startup

Automatically applying migrations on normal application startup may be convenient during development.

For production, prefer a controlled migration step.

This reduces the risk of an unreviewed deployment altering production unexpectedly.

---

# 27. Migration Review

Before deployment, review:

```text
Tables created

Columns changed

Indexes changed

Foreign keys changed

Delete behavior

Potential data loss
```

AI-generated migrations must not be accepted blindly.

---

# 28. Destructive Migrations

Changes such as:

```text
Drop column

Drop table

Change data type

Remove relationship
```

require special review.

Confirm whether existing production data will be lost.

---

# 29. Backup Before Migration

Before significant production migrations:

```text
Create database backup
```

and verify the backup completed successfully.

Do not begin a high-risk migration without a recovery path.

---

# 30. Backup Principle

A backup is not complete merely because a backup file exists.

A usable backup must be:

```text
Created successfully

Stored safely

Retained appropriately

Restorable
```

---

# 31. What Must Be Backed Up

At minimum:

```text
PostgreSQL Database
```

Also back up any application-managed file storage introduced later, such as:

```text
Preview Images
```

if files are not stored inside the database.

---

# 32. Database Backup Tool

Use standard PostgreSQL backup tools.

Preferred logical backup:

```text
pg_dump
```

Restore:

```text
pg_restore
```

or corresponding PostgreSQL-supported mechanisms.

---

# 33. Backup Format

A PostgreSQL custom-format backup is useful because it supports flexible restore.

Conceptually:

```text
pg_dump -Fc
```

Exact commands belong in deployment scripts rather than being manually improvised each time.

---

# 34. Backup Frequency

For an actively used ERP, a reasonable starting point is:

```text
Daily automatic database backup
```

If Order activity becomes very high or data-loss tolerance becomes lower, backup frequency can increase.

---

# 35. Recommended Initial Backup Schedule

Simple Version 1 recommendation:

```text
Daily:
Database backup

Before significant deployment:
Additional manual/automatic backup
```

---

# 36. Retention

A practical starting retention policy may be:

```text
Daily backups:
Keep 14–30 days

Monthly backups:
Keep several months
```

Exact retention should be adjusted based on available storage and business needs.

---

# 37. Backup Storage

Do not keep the only backup on the same disk as the production database.

Preferred:

```text
Production Server
        ↓
Backup Copy
        ↓
Separate Storage
```

---

# 38. Separate Backup Location

Examples:

```text
NAS

Second server

Secure external storage

Encrypted cloud backup
```

The important requirement is failure independence from the main production disk/server.

---

# 39. Backup Access

Backup files may contain sensitive company information.

Restrict access.

Do not make backup folders broadly writable/readable across the network.

---

# 40. Backup Encryption

If backups are stored outside a physically trusted environment, encryption should be considered.

Any encryption key must itself be protected and recoverable.

---

# 41. Restore Testing

Restore testing is mandatory.

At least periodically:

```text
Take backup

Restore into test database

Start/inspect application against restored copy

Verify important data
```

---

# 42. Restore Test Frequency

A practical starting point:

```text
Quarterly
```

or after important backup/deployment changes.

More frequent testing may be used if operational risk increases.

---

# 43. Restore Verification

After restoring, verify at minimum:

```text
Users exist

Clients exist

Projects exist

Orders exist

Calculator data exists

Cost Items exist

Business IDs are intact
```

---

# 44. Restore Must Not Target Production Accidentally

Restore procedures must clearly distinguish:

```text
Test Restore

Production Restore
```

Never test restoration directly over the live production database.

---

# 45. Disaster Recovery Goal

Version 1 does not require a complex enterprise disaster-recovery site.

The practical goal is:

```text
If the ERP server/database fails,
Lithograph can rebuild the application
and restore business data from backup.
```

---

# 46. Source Code Recovery

Source code must be stored in Git.

Production server should not be the only location containing application source.

---

# 47. Documentation Recovery

Markdown documentation is part of the Git repository.

This ensures architecture and business specifications are also recoverable.

---

# 48. Production File Storage

If Preview Images are implemented, choose one controlled storage directory.

Example concept:

```text
/data/lithograph-erp/preview-images/
```

The exact OS path is deployment-specific.

---

# 49. Do Not Store Files in Source Tree

Production files must not be written into:

```text
Git repository

Frontend source

Backend source
```

Use dedicated persistent storage.

---

# 50. File Path in Database

The database should store a logical reference/path rather than an accidental temporary deployment path where possible.

This improves portability during server migration.

---

# 51. File Backup

If application-managed Preview Images exist, their storage must be included in backup strategy.

Database backup alone would not preserve those files.

---

# 52. Folder Links

Order Folder Links point to existing local/network folders.

Lithograph ERP backup is not responsible for automatically backing up those external production folders.

They belong to the company's normal file-server backup strategy.

---

# 53. Logging

Production must use structured application logging.

Important events include:

```text
Application startup

Application shutdown

Unexpected errors

Authentication failures where appropriate

Database connection failures

Migration failures

Background cleanup failures if any
```

---

# 54. Logging Framework

Use standard ASP.NET Core logging abstractions.

A production logging provider may write to:

```text
Files

System logging

Centralized log storage
```

depending on deployment.

Do not build a custom logging framework.

---

# 55. Log Levels

Use normal levels:

```text
Trace

Debug

Information

Warning

Error

Critical
```

Production should not normally run extremely verbose Debug/Trace logging continuously.

---

# 56. Sensitive Logging

Never log:

```text
Passwords

Password hashes

Raw Session tokens

Connection-string passwords

Secret keys
```

Avoid logging complete sensitive request bodies.

---

# 57. Financial Logging

Do not unnecessarily log detailed Cost information or full Calculator payloads.

Logs are for operations and troubleshooting, not a duplicate business database.

---

# 58. Error Correlation

Unexpected API errors should have a correlation/request identifier where practical.

Example user-facing message may include:

```text
Reference ID: abc123
```

allowing administrators to match the failure to server logs.

---

# 59. Log Retention

Logs should be rotated.

Do not allow one log file to grow forever.

A simple rolling-file strategy is acceptable if file logging is used.

---

# 60. Health Monitoring

At minimum monitor:

```text
Application process running

Database reachable

Disk space

Backup success
```

Version 1 does not need a full observability platform.

---

# 61. Health Endpoint

The application may expose:

```text
/health
```

for internal monitoring.

It should not expose sensitive technical details publicly.

---

# 62. Disk Space Monitoring

Low disk space can break:

```text
PostgreSQL

Logs

Backups

Preview Image storage
```

Production operations should monitor available disk space.

---

# 63. Backup Failure Monitoring

Automatic backup failure should not remain unnoticed.

At minimum, the operator should have a simple way to confirm:

```text
Last successful backup
```

---

# 64. Application Availability

Version 1 does not require:

```text
24/7 active-active cluster

Automatic failover

Multi-region deployment
```

A single reliable server with good backups is acceptable.

---

# 65. Planned Downtime

Short planned downtime for upgrades is acceptable for Version 1.

Schedule deployments outside busy production periods where practical.

---

# 66. Deployment Process

Recommended process:

```text
1. Review changes

2. Run automated tests

3. Build frontend/backend

4. Create production database backup

5. Stop/prepare application if necessary

6. Apply migrations

7. Deploy new build

8. Start application

9. Run smoke tests

10. Review logs
```

---

# 67. Build Artifacts

Production deployment should use built artifacts.

Do not copy random developer workspace folders to production.

---

# 68. Version Identification

The application should expose its deployed version somewhere accessible to administrators.

Example:

```text
Version 1.0.12
```

or Git commit/build identifier.

This simplifies troubleshooting.

---

# 69. Deployment Record

Maintain a simple record of:

```text
Deployment date

Application version

Migration applied

Person performing deployment

Important notes
```

This does not require a special ERP module.

A simple deployment log/process is sufficient.

---

# 70. Rollback

Code rollback should be possible by deploying the previous known-good build.

Database rollback is more complicated.

Do not assume every EF migration can safely be reversed after production data changes.

---

# 71. Forward-Fix Preference

For many production database issues, the safest approach is:

```text
Create corrected forward migration
```

rather than blindly executing down migrations.

Use backup restore only when necessary.

---

# 72. Backward-Compatible Changes

Where practical, deployments should prefer safe staged changes.

Example:

```text
Add new nullable column

Deploy code using it

Populate data if needed

Later tighten constraints
```

This can reduce migration risk.

---

# 73. Production Data Editing

Do not directly edit production database rows manually unless absolutely necessary.

Normal business corrections should happen through the ERP.

If emergency SQL correction is required:

```text
Backup first

Document change

Use transaction

Verify result
```

---

# 74. Database Administration Access

Limit direct PostgreSQL administrative access to trusted administrators.

Normal Users interact only through the ERP application.

---

# 75. Development Access to Production

Developers/AI tools should not automatically connect to production.

Development and production credentials must remain separate.

---

# 76. AI Tool Safety

Cursor/Claude/other AI coding assistants must never be given production secrets in prompts or committed files.

Do not paste:

```text
Production passwords

Connection strings with credentials

Session secrets
```

into AI chat when avoidable.

---

# 77. Environment Files

If `.env` or similar local files are used:

```text
.env
```

must be excluded from Git.

Provide an example file such as:

```text
.env.example
```

containing names only, not real secrets.

---

# 78. Production Settings

Important production settings may include:

```text
Database connection

Allowed frontend origin

Session expiration

Cookie security

File storage location

Logging location

Application URL
```

---

# 79. Cookie Security

If cookie-based Sessions are used, production configuration should use appropriate:

```text
Secure

HttpOnly

SameSite
```

settings according to the chosen architecture.

Do not expose authentication tokens to JavaScript unnecessarily.

---

# 80. Session Expiration

Production Session expiration should be configured centrally.

Avoid hardcoded Session lifetime values scattered across code.

---

# 81. Clock Accuracy

Production server clock should be synchronized correctly.

Incorrect time can affect:

```text
Sessions

Audit timestamps

Business ID year transitions

Logs
```

---

# 82. Database Time

Application and database timestamps use UTC internally.

Server configuration should support reliable UTC timekeeping.

---

# 83. Business Date Display

Frontend may display local business dates/times according to the application/user timezone.

This must not change stored UTC timestamps.

---

# 84. Initial Data Import

If Lithograph later imports existing Clients/Projects/Orders into ERP, treat that as a separate controlled migration/import task.

Do not mix data-import logic into normal deployment startup.

---

# 85. Production Seeding

Production deployment may seed only required system data such as:

```text
Permissions

Director Role
```

Normal business data should not be created through automatic migrations unless explicitly approved.

---

# 86. Director Bootstrap

If no User exists:

```text
Initial Setup
```

creates the first Director User.

Do not seed a default password.

---

# 87. No Default Credentials

Never ship production with:

```text
admin/admin

director/123456

default password
```

The initial password must be chosen during secure setup.

---

# 88. Backup Before Import

Before a large data import into an already-used ERP:

```text
Create backup
```

so the import can be recovered from if necessary.

---

# 89. Preview Image Size

If Preview Images are implemented, consider reasonable size limits.

The ERP Gallery does not need original multi-gigabyte production artwork.

Preview Images should remain optimized for screen use.

---

# 90. Production Artwork

Lithograph ERP is not intended to become the primary storage system for high-resolution production files in Version 1.

Those remain in normal network/file-server folders referenced by Folder Links.

---

# 91. Network File Server

Folder Links may point to:

```text
Windows network shares

NAS folders

Other approved internal storage
```

ERP availability should not depend on the backend reading those paths.

---

# 92. Database Server Separation

If usage grows, PostgreSQL may later move to a separate server.

Because application configuration uses a connection string, this should not require redesigning business modules.

---

# 93. Horizontal Scaling

Version 1 does not require several application servers.

If scaling is later required, Session/storage behavior must be reviewed.

Do not design distributed infrastructure prematurely.

---

# 94. Maintenance Window

Choose a simple maintenance process.

Example:

```text
Notify Users

Finish active work

Take backup

Deploy update

Run smoke tests

Resume use
```

---

# 95. User Notification

Version 1 does not need a complex maintenance-notification system.

Operational communication may initially happen outside ERP.

---

# 96. Database Vacuum and Maintenance

PostgreSQL normal automatic maintenance should be enabled.

Do not manually disable:

```text
autovacuum
```

without expert reason.

---

# 97. PostgreSQL Updates

Keep PostgreSQL on a supported release.

Major database upgrades should be planned and tested separately from ordinary application releases.

---

# 98. .NET Runtime Updates

Keep the selected supported .NET runtime patched.

Do not automatically jump to a new major framework version during routine deployment without testing.

---

# 99. Frontend Dependency Updates

React/npm dependency updates should be reviewed and tested.

Do not run automatic major upgrades directly in production code without checking compatibility.

---

# 100. Security Updates

Critical OS/runtime/database security updates should be applied through normal maintenance.

A stable ERP still requires supported underlying software.

---

# 101. Restore Scenario — Server Failure

Conceptual recovery:

```text
Prepare replacement server

Install runtime/PostgreSQL

Deploy known-good ERP build

Restore PostgreSQL backup

Restore Preview Images if applicable

Configure secrets

Start application

Run smoke tests
```

---

# 102. Restore Scenario — Database Corruption

Conceptual recovery:

```text
Stop application writes

Identify last good backup

Prepare replacement/test database

Restore

Verify data

Switch application to restored database

Run smoke tests
```

Actual production recovery must be performed carefully.

---

# 103. Restore Scenario — Bad Migration

Possible responses:

```text
Forward-fix migration
```

or:

```text
Restore pre-deployment database backup
+
Deploy previous application build
```

Choice depends on whether new production data has already been created after deployment.

---

# 104. Recovery Point Objective

Version 1 does not need a formal enterprise SLA.

However, daily backups imply potential loss of up to approximately one business day of data in a worst-case restore scenario.

If this becomes unacceptable, increase backup frequency or introduce PostgreSQL continuous recovery mechanisms later.

---

# 105. Recovery Time Objective

Version 1 does not define a strict recovery-time guarantee.

The objective is a recovery process that is documented and practiced rather than improvised.

---

# 106. Future Database Recovery

If business dependence increases, future improvements may include:

```text
More frequent backups

WAL archiving

Point-in-time recovery

Database replication
```

These are not initial V1 requirements.

---

# 107. Future High Availability

Possible future infrastructure may include:

```text
Separate database server

Standby database

Multiple application instances

Automated failover
```

Only implement these after real availability requirements justify the complexity.

---

# 108. Production Checklist Before Go-Live

Verify:

```text
Production server prepared

HTTPS working

Production database created

Secrets configured

Migrations applied

Director setup works

Backup automation configured

Backup stored separately

Restore procedure tested

Logs working

Disk space sufficient

Smoke tests pass
```

---

# 109. Daily Operational Checklist

Automated/manual monitoring should make it easy to confirm:

```text
Application is running

Database is healthy

Backup succeeded

Disk space is sufficient
```

---

# 110. Weekly Operational Review

A simple weekly review may check:

```text
Unexpected errors

Backup history

Disk growth

Application performance

Failed login anomalies if relevant
```

Do not create excessive operations bureaucracy.

---

# 111. Monthly Review

Periodically review:

```text
Backup retention

Log retention

Software updates

Storage capacity
```

---

# 112. Deployment Non-Goals

Version 1 does not require:

```text
Kubernetes

Multi-region deployment

Active-active database

Microservices

Service mesh

Distributed tracing platform

Complex CI/CD platform

Automatic zero-downtime migration framework

24/7 operations center
```

---

# 113. Backup Non-Goals

Version 1 does not initially require:

```text
Continuous replication

Point-in-time recovery

Multi-cloud backups

Immutable enterprise backup appliances
```

These may become appropriate later as ERP dependence grows.

---

# 114. Security Non-Goals

Version 1 deployment does not require exposing ERP publicly.

Avoid adding internet-facing complexity unless there is a clear need.

---

# 115. Deployment Simplicity Rule

Before adding infrastructure, ask:

```text
What real failure or operational problem does this solve?
```

If the answer is hypothetical, keep the deployment simpler.

---

# 116. Backup Simplicity Rule

A simple tested backup process is better than a sophisticated backup system nobody knows how to restore.

Priority:

```text
Backup exists

Backup is separate

Backup is recent

Restore is tested
```

---

# 117. Final Deployment Principle

Lithograph ERP Version 1 should be deployable and recoverable without requiring a dedicated DevOps team.

The preferred model is:

```text
One application

One PostgreSQL database

Controlled configuration

HTTPS

Reviewed migrations

Automatic backups

Tested restore procedure
```

The most important operational rule is:

```text
A system is not safely deployed
until its data can be restored.
```

---

**End of Document**