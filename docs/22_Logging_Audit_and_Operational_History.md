# Lithograph ERP

**Document:** 22_Logging_Audit_and_Operational_History.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `02_Architecture.md`
- `03_Database_Design.md`
- `07_Authentication.md`
- `09_Auth_Tables.md`
- `11_Employees_Module.md`
- `12_Clients_Module.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `17_Database_Schema_Overview.md`
- `20_Testing_Strategy.md`
- `21_Deployment_and_Backup.md`

---

# 1. Purpose

This document defines how Lithograph ERP Version 1 handles:

```text
Technical Logging

Audit Metadata

Operational History

Security Events

Business Record History
```

The purpose is to clearly separate:

```text
System troubleshooting information
```

from:

```text
Business accountability information
```

---

# 2. Core Principle

Lithograph ERP has three different concepts:

```text
Technical Logs

Audit Metadata

Business History
```

They must not be treated as the same thing.

---

# 3. Technical Logs

Technical Logs answer questions such as:

```text
Did the application fail?

Which request caused an exception?

Did PostgreSQL become unavailable?

Did a migration fail?

Why did an API request return an error?
```

Technical Logs are operational infrastructure data.

---

# 4. Audit Metadata

Audit Metadata answers questions such as:

```text
Who created this Client?

When was this Project created?

Who last modified this Order?

Who assigned this Project Member?

Who published this Calculator Template Version?
```

Audit Metadata is stored directly with important business records.

---

# 5. Business History

Business History answers questions such as:

```text
What was the previous Order status?

Who changed the Selling Price last month?

What was the Project Owner before the current Owner?

What fields changed during each edit?
```

A complete general-purpose Business History system is outside Version 1.

---

# 6. Version 1 Decision

Version 1 uses:

```text
Technical Logs
+
Audit Metadata
```

but does not implement:

```text
Universal Field-by-Field Audit History
```

---

# 7. No Generic audit_log Table

Version 1 does not require a table such as:

```text
audit.audit_log
```

or:

```text
system.change_history
```

containing every change to every entity.

Do not add this infrastructure preemptively.

---

# 8. Why No Generic Audit Table

A universal audit system adds complexity around:

```text
Large data volume

Serialization

Sensitive information

Old/new value storage

Schema changes

Query complexity

Retention rules

Permissions
```

Lithograph Version 1 does not currently require that level of history.

---

# 9. Technical Logging Architecture

Technical logging uses standard ASP.NET Core logging.

Conceptually:

```text
Application
    ↓
ILogger
    ↓
Configured Logging Provider
    ↓
Log Storage
```

Do not create a custom logging framework.

---

# 10. Technical Log Storage

Production logs may be stored in:

```text
Rolling log files

Operating-system logging

Central log service
```

depending on deployment.

Version 1 does not require a centralized logging platform.

---

# 11. Technical Log Levels

Use standard levels:

```text
Trace

Debug

Information

Warning

Error

Critical
```

---

# 12. Production Log Level

Production should normally use:

```text
Information
```

or a similarly reasonable default.

Do not continuously enable very verbose:

```text
Trace

Debug
```

unless troubleshooting requires it.

---

# 13. Information Events

Useful Information-level events may include:

```text
Application started

Application stopped

Migration completed

Director bootstrap completed

User login succeeded

Administrative password reset completed
```

Do not log every trivial database operation.

---

# 14. Warning Events

Examples:

```text
Repeated failed login attempt

Unexpected but recoverable validation situation

Database operation retried

Expired configuration detected
```

Warnings should indicate something worth investigating without implying application failure.

---

# 15. Error Events

Examples:

```text
Unhandled API exception

Database command failure

Formula Engine unexpected failure

File-storage failure

Migration failure
```

---

# 16. Critical Events

Critical should be reserved for severe situations such as:

```text
Application cannot start

Database unavailable during startup

Required security configuration missing

Severe unrecoverable failure
```

---

# 17. Do Not Log Secrets

Technical Logs must never contain:

```text
Plaintext passwords

Password hashes

Raw Session tokens

Authentication cookies

Secret keys

Database passwords
```

---

# 18. Avoid Sensitive Payload Logging

Do not routinely log complete request bodies containing:

```text
Calculator Cost Items

Financial values

Passwords

Authentication data
```

Log only what is necessary for troubleshooting.

---

# 19. User Identification in Logs

Where useful, technical Logs may include:

```text
User ID

Username

Request ID
```

for authenticated requests.

Avoid adding unnecessary personal information.

---

# 20. Request Correlation

Each API request should have a correlation/request identifier where practical.

Example:

```text
Request ID:
a1b2c3
```

Unexpected API errors may return this identifier to the frontend.

---

# 21. Error Example

User sees:

```text
An unexpected error occurred.

Reference ID: a1b2c3
```

Administrator can locate:

```text
a1b2c3
```

in server logs.

---

# 22. Audit User Identity

Audit fields reference:

```text
auth.users.id
```

not:

```text
employees.employees.id
```

because audit fields answer:

```text
Which authenticated account performed this action?
```

---

# 23. Business Responsibility Identity

Business responsibility fields use:

```text
employees.employees.id
```

Example:

```text
Project Owner
```

is an Employee.

Example:

```text
Project updated_by
```

is a User.

---

# 24. Standard Creation Audit Fields

Important business entities may contain:

```text
created_at

created_by
```

---

# 25. Standard Update Audit Fields

Important editable business entities may contain:

```text
updated_at

updated_by
```

---

# 26. Timestamp Type

Audit timestamps use:

```text
timestamptz
```

and UTC internally.

---

# 27. created_by Nullable Rule

`created_by` may be nullable where system/bootstrap operations require it.

Most important example:

```text
First Director User
```

because no authenticated User exists yet.

---

# 28. System-Created Records

System-created records may use:

```text
created_by = NULL
```

when there is genuinely no User responsible.

Do not invent fake system User IDs merely to satisfy audit columns.

---

# 29. Authentication Users Audit

`auth.users` contains:

```text
created_at

created_by

updated_at

updated_by

last_login_at
```

---

# 30. User Creation

When an administrator creates another User:

```text
created_by
=
Administrator User ID
```

---

# 31. First Director Creation

For initial bootstrap:

```text
created_by = NULL
```

because there is no existing authenticated User.

---

# 32. User Update

When Username or account metadata changes:

```text
updated_at

updated_by
```

are updated.

---

# 33. Password Hash Changes

Version 1 does not store historical password hashes.

When a password changes:

```text
password_hash
```

is replaced.

Do not maintain old hashes unless a future password-history policy explicitly requires them.

---

# 34. Login Audit

Version 1 stores:

```text
last_login_at
```

on User.

It does not require a permanent record of every successful login.

---

# 35. Login Failure History

Failed login attempts may appear in technical/security logs.

Version 1 does not require a dedicated permanent:

```text
login_attempts
```

table.

---

# 36. Employee Audit

`employees.employees` contains:

```text
created_at

created_by

updated_at

updated_by
```

---

# 37. Client Audit

`clients.clients` contains:

```text
created_at

created_by

updated_at

updated_by
```

---

# 38. Project Audit

`projects.projects` contains:

```text
created_at

created_by

updated_at

updated_by
```

---

# 39. Project Member Audit

`projects.project_members` contains:

```text
assigned_at

assigned_by
```

This records who created the current assignment.

---

# 40. Project Team History Limitation

Version 1 does not preserve previous Project Team assignments after they are removed.

Example:

```text
Owner:
Employee A
```

changes to:

```text
Owner:
Employee B
```

The database stores the current assignment.

It does not retain a complete historical timeline showing Employee A as former Owner.

---

# 41. Project Team Change Logging

Technical Logs may record significant Project Team changes if useful.

Example:

```text
Project Owner changed
Project ID: ...
Old Employee ID: ...
New Employee ID: ...
User ID: ...
```

This is troubleshooting/operational logging.

It is not a guaranteed permanent business audit history.

---

# 42. Order Audit

`orders.orders` contains:

```text
created_at

created_by

updated_at

updated_by
```

---

# 43. Order Status History

Version 1 stores only the current:

```text
status
```

It does not store a complete Order Status timeline.

---

# 44. Order Priority History

Version 1 stores only the current:

```text
priority
```

Previous priorities are not retained as structured history.

---

# 45. Selling Price History

Version 1 stores the current/final historical business value:

```text
orders.orders.selling_price
```

It does not keep every previous Selling Price revision.

---

# 46. Cost Price History

Version 1 stores:

```text
orders.orders.cost_price
```

synchronized from current Cost Items.

It does not create a separate Cost Price revision table.

---

# 47. Checklist Audit

Checklist Items do not require:

```text
created_at

created_by

updated_at

updated_by
```

in Version 1.

They are lightweight operational sub-records.

---

# 48. Checklist Completion History

Version 1 does not record:

```text
Who checked this item?

When was it checked?

How many times was it reopened?
```

Only:

```text
is_completed
```

is required.

---

# 49. Folder Link Audit

Folder Links do not require full audit metadata in Version 1.

They are simple Order references.

---

# 50. Calculator Template Audit

`calculator.templates` contains:

```text
created_at

created_by

updated_at

updated_by
```

---

# 51. Template Version Audit

`calculator.template_versions` contains:

```text
created_at

created_by

published_at

published_by
```

This is important because Template publication changes future pricing behavior.

---

# 52. Template Publication History

Template Versions themselves provide durable history.

Example:

```text
Version 1
Published by User A

Version 2
Published by User B

Version 3
Published by User A
```

This is structured business history.

---

# 53. Published Template Immutability

Because Published Template Versions cannot be edited, their content acts as a historical snapshot.

This provides strong auditability without a generic audit table.

---

# 54. Draft Template History

Version 1 does not keep every change made while editing a Draft.

Only the current Draft definition is stored.

---

# 55. Order Calculator Audit

`calculator.order_calculators` contains:

```text
created_at

created_by

updated_at

updated_by

last_calculated_at
```

---

# 56. Calculator Value History

Version 1 does not store a revision after every Calculator edit.

Only current:

```text
field_values
```

are stored for that Order Calculator.

---

# 57. Calculator Historical Reproducibility

Historical stability comes from:

```text
Exact Template Version
+
Current stored field values
+
Cost Items
+
Stored final Selling Price
```

not from recording every keystroke.

---

# 58. Cost Item Audit

Cost Items contain financial information.

Therefore:

```text
calculator.cost_items
```

must contain:

```text
created_at

created_by

updated_at

updated_by
```

---

# 59. Why Cost Items Receive Audit Metadata

Cost Items affect:

```text
Order Cost Price

Project Profit

Client Profit

Reports
```

Therefore it is useful to know:

```text
Who created this Cost Item?

Who last modified it?
```

---

# 60. Cost Item Delete History

Version 1 allows authorized Cost Item deletion.

A deleted Cost Item is not retained in a separate history table.

This is an accepted Version 1 limitation.

---

# 61. Cost Deletion Confirmation

Because Cost deletion affects financial values, the UI should require deliberate deletion.

Example:

```text
Delete Cost Item?

This will update the Order Cost Price.
```

---

# 62. Financial History Limitation

Version 1 is operational ERP, not accounting software.

It does not provide an append-only immutable accounting ledger.

Therefore authorized edits may change current Cost Items.

---

# 63. Reports Audit

Reports do not create audit records when viewed.

Opening:

```text
Orders Report
```

does not create a business history row.

---

# 64. Report Export Audit

If report export is later implemented, Version 1 does not require recording every exported report.

This may be added if security requirements change.

---

# 65. Role Assignment Audit

`auth.user_roles` contains:

```text
assigned_at

assigned_by
```

This records who created the current Role assignment.

---

# 66. Removed Role Assignments

When a Role assignment is removed, the relationship row is removed.

Version 1 does not preserve all previous Role assignment history.

---

# 67. Permission Assignment Audit

`auth.role_permissions` contains:

```text
assigned_at

assigned_by
```

---

# 68. Permission Removal History

Removing a Role Permission deletes the relationship.

Version 1 does not retain an immutable history of every previous Role Permission assignment.

---

# 69. Security Event Logging

Certain security-related operations should produce technical/security log entries.

Examples:

```text
Login failed

Inactive User attempted login

Administrator reset password

User deactivated

User activated

Role assigned

Role removed

Final Director protection blocked an action
```

---

# 70. Security Logs Are Not Business Tables

Security logging belongs in technical logs.

Do not create a separate business entity for every security event in Version 1.

---

# 71. Successful Login Logging

Successful logins may be logged at Information level.

Avoid excessive verbose logging if it becomes noisy.

The database already stores:

```text
last_login_at
```

---

# 72. Failed Login Logging

Failed login events may include:

```text
Username attempted

Timestamp

Request ID

IP address if available
```

Do not log attempted password values.

---

# 73. IP Addresses

Sessions may contain:

```text
ip_address
```

according to Authentication design.

Technical Logs may also record connection IP where normal ASP.NET logging permits.

Treat this as operational/security information.

---

# 74. User Agent

Sessions may contain:

```text
user_agent
```

for troubleshooting/security context.

Do not build a device-management system around it in Version 1.

---

# 75. Business ID Logging

Technical logs may include Business IDs when useful.

Example:

```text
Failed to recalculate Order ORD-2026-000425
```

Business IDs make operational troubleshooting easier than UUIDs alone.

---

# 76. UUID Logging

Logs may also include UUIDs for precise database identification.

Useful combination:

```text
OrderBusinessId = ORD-2026-000425

OrderId = <uuid>
```

---

# 77. Audit Metadata Visibility

Normal Users do not need to see all audit metadata on every screen.

UI may show it where useful.

Example:

```text
Created:
26 Sep 2026

Last Updated:
28 Sep 2026
```

---

# 78. Admin Detail Visibility

Administrative screens may optionally show:

```text
Created By

Created At

Updated By

Updated At
```

if useful for troubleshooting.

Do not clutter normal operational screens.

---

# 79. Created By Display

When displaying `created_by`, show a human-friendly value such as:

```text
Username
```

or linked Employee name where available.

Do not expose UUID as the primary UI label.

---

# 80. Deleted/Inactive User Audit References

If a User becomes inactive, audit references to that User remain valid.

Historical records must still show who created or modified them.

---

# 81. Username Change and Audit History

Audit fields reference User UUID.

Therefore changing:

```text
username
```

does not break historical audit relationships.

This is one reason UUID is the proper foreign key.

---

# 82. Physical User Deletion

Normal User deletion is not part of Version 1.

This protects audit references.

If exceptional maintenance physically removes a User, audit FK behavior must not cascade-delete business records.

---

# 83. Audit FK Delete Behavior

Audit fields such as:

```text
created_by

updated_by

assigned_by

published_by
```

must never cause parent business records to be deleted when a User is removed.

Use safe restrictive or nulling behavior according to schema design.

---

# 84. Recommended User Audit FK Behavior

Where exceptional User deletion is technically allowed, a practical behavior is:

```text
ON DELETE SET NULL
```

for non-critical audit references.

This preserves the business record.

Exact EF Core configuration must be deliberate.

---

# 85. Template Publication User

For Published Template Versions, preserving:

```text
published_by
```

is valuable.

If User deletion is exceptional, losing the User relation is preferable to losing the Template Version.

Never cascade-delete Published Versions.

---

# 86. Audit Data Modification

Normal Users must not directly edit audit fields.

The backend controls:

```text
created_at

created_by

updated_at

updated_by

assigned_at

assigned_by

published_at

published_by
```

---

# 87. Frontend Requests

Frontend create/update DTOs must not normally send:

```text
created_by

updated_by

published_by
```

Backend derives these from the authenticated User.

---

# 88. Timestamp Generation

Backend/database generates audit timestamps.

Do not trust client-provided timestamps for audit information.

---

# 89. Updated At Rule

When an important entity is successfully modified:

```text
updated_at = current UTC timestamp
```

and:

```text
updated_by = current User ID
```

---

# 90. Non-Modification Reads

Opening or viewing a record must not update:

```text
updated_at
```

Read operations do not count as modifications.

---

# 91. Child Operations and Parent updated_at

Do not automatically update every parent record merely because a child row changed unless that behavior is useful.

Example:

Adding a Checklist Item does not necessarily need to update:

```text
orders.orders.updated_at
```

Version 1 should avoid noisy audit timestamps.

---

# 92. Financial Child Changes

Changing Cost Items does update the Order's:

```text
cost_price
```

Therefore the Order itself changes.

In that case:

```text
updated_at

updated_by
```

should also update because a financial Order field changed.

---

# 93. Calculator Save

Saving Calculator values and changing:

```text
selling_price
```

should update the Order's audit fields.

---

# 94. Project Team Changes

Changing Project Team does not necessarily update:

```text
projects.projects.updated_at
```

because the team relationship has its own:

```text
assigned_at

assigned_by
```

metadata.

This avoids mixing parent metadata with relationship history.

---

# 95. Project General Edit

Changing:

```text
name

description

status

deadline

client_id
```

updates:

```text
projects.projects.updated_at

projects.projects.updated_by
```

---

# 96. Order General Edit

Changing:

```text
name

description

status

priority

deadline

order_type_id
```

updates:

```text
orders.orders.updated_at

orders.orders.updated_by
```

---

# 97. Client General Edit

Changing Client data updates:

```text
clients.clients.updated_at

clients.clients.updated_by
```

---

# 98. Employee General Edit

Changing Employee data updates:

```text
employees.employees.updated_at

employees.employees.updated_by
```

---

# 99. Deactivation Is an Update

Actions such as:

```text
Deactivate User

Deactivate Employee

Deactivate Client

Deactivate Order Type

Deactivate Calculator Template
```

must update their normal audit metadata where available.

---

# 100. Status Change Is an Update

Changing:

```text
Project Status

Order Status
```

updates normal parent audit metadata.

Version 1 does not require separate status-change history rows.

---

# 101. Audit Query Examples

Version 1 can answer:

```text
Who created this Client?

Who last edited this Project?

Who created this Order?

Who last recalculated/edited this Calculator?

Who added this Cost Item?

Who last edited this Cost Item?

Who published this Template Version?
```

---

# 102. Questions V1 Cannot Reliably Answer

Version 1 does not guarantee answers to:

```text
What was every previous value of this Order?

Who changed status from Active to Completed?

Who removed this Cost Item?

Who was Project Owner six months ago?

What was this Employee's old position?

What was the old Client name?
```

These require full history/audit functionality.

---

# 103. Accepted Limitation

This limitation is intentional.

Lithograph ERP Version 1 prioritizes:

```text
Current operational state

Historical Calculator safety

Basic accountability

Simple architecture
```

over exhaustive field-by-field history.

---

# 104. Future Audit Module

If future business requirements require full history, introduce it deliberately.

Possible future capabilities:

```text
Entity Change History

Old Value

New Value

Changed By

Changed At

Security Event History

Financial Change History
```

---

# 105. Future Event History

For selected high-value events, future tables could record:

```text
Order Status Changes

Project Team Changes

Cost Item Deletions

Price Overrides

Permission Changes
```

without necessarily auditing every database field.

---

# 106. Selective History Preferred

If more history is required later, prefer targeted business history over blindly recording every field change.

Example:

```text
Order Status History
```

may be more useful than a universal generic database diff log.

---

# 107. Financial Audit Future

If Lithograph ERP later becomes responsible for formal accounting, Cost records may require:

```text
Immutable posting

Correction entries

Deletion prevention

Financial period locking
```

That is outside Version 1.

---

# 108. Security Audit Future

If security requirements grow, future features may include:

```text
Permanent Login History

Permission Change History

Session Revocation History

Administrator Action History
```

These are not currently required.

---

# 109. Technical Log Retention

Technical Logs should use a finite retention period.

Do not keep unlimited logs forever.

Exact retention depends on storage and operational needs.

---

# 110. Initial Log Retention Recommendation

A practical starting point:

```text
30 days
```

of normal application logs.

Longer retention may be used if inexpensive and useful.

---

# 111. Error Log Retention

Critical Error logs may be retained longer if useful for troubleshooting.

Do not create complex retention tiers in Version 1 unless needed.

---

# 112. Log Rotation

If file-based logging is used:

```text
Rotate logs
```

by date or size.

Do not allow a single log file to grow indefinitely.

---

# 113. Backup of Technical Logs

Technical Logs are not as critical as PostgreSQL business data.

Version 1 backups do not need to treat application logs as the primary recovery source.

Database backups remain the priority.

---

# 114. Audit Metadata Backup

Audit Metadata lives inside PostgreSQL.

Therefore it is automatically included in normal database backups.

---

# 115. Template History Backup

Published Calculator Template Versions are business-critical historical data.

They are included in PostgreSQL backups.

---

# 116. Cost Audit Backup

Cost Item audit fields are included in database backups.

---

# 117. Clock Reliability

Accurate audit timestamps depend on correct server time.

Production server time should be synchronized.

---

# 118. UTC Rule

Store:

```text
created_at

updated_at

assigned_at

published_at

last_login_at
```

in UTC-aware timestamps.

Convert only for display.

---

# 119. Technical Logs and Time

Technical Logs should also use clear timestamping.

UTC is preferred for consistency.

---

# 120. Log Message Style

Good log message:

```text
Failed to publish Calculator Template Version.
```

with structured fields:

```text
TemplateId

VersionId

UserId

RequestId
```

Avoid constructing giant unstructured text messages.

---

# 121. Structured Logging

Prefer structured properties over embedding everything in one string.

Conceptually:

```text
Event:
TemplatePublishFailed

TemplateId:
...

VersionId:
...

UserId:
...
```

This improves searchability.

---

# 122. Business Data in Logs

Log identifiers rather than large business payloads.

Prefer:

```text
OrderId

OrderBusinessId
```

over dumping the complete Order JSON.

---

# 123. Calculator Logging

Normal Calculator saves should not log every field/value.

Log only unusual events such as:

```text
Formula validation failure

Unexpected calculation exception

Template publication failure
```

---

# 124. Formula Errors

Expected user formula validation errors are normal application behavior.

They do not necessarily need Error-level server logs.

Unexpected engine failures should be logged as Errors.

---

# 125. Validation Logging

Normal user validation failures such as:

```text
Name required

Inactive Client

Duplicate Username
```

do not require Error-level logs.

These are expected business outcomes.

---

# 126. Authorization Logging

Repeated or suspicious authorization failures may be logged.

Do not flood logs with every harmless `403` unless operationally useful.

---

# 127. Database Error Logging

Unexpected PostgreSQL failures should include enough context to diagnose:

```text
Operation

Entity type

Request ID
```

but not expose secrets or sensitive query payloads unnecessarily.

---

# 128. Business ID Conflict Logging

Unexpected Business ID uniqueness conflict may be logged as a Warning or Error because numbering generation should already be concurrency-safe.

This may indicate an implementation problem.

---

# 129. Operational Event Examples

Useful events may include:

```text
ApplicationStarted

DatabaseMigrationApplied

DirectorBootstrapCompleted

UserDeactivated

PasswordResetByAdministrator

TemplateVersionPublished

BackupFailed
```

The exact log-event naming convention may be refined during implementation.

---

# 130. Logging Does Not Replace Tests

Technical Logs help diagnose failures.

They do not verify correctness.

Business rules remain protected through:

```text
Automated Tests

Database Constraints

Authorization

Validation
```

---

# 131. Logging Does Not Replace Audit Metadata

A log file is not a reliable permanent answer to:

```text
Who created this Order?
```

That belongs in:

```text
created_by
```

on the business record.

---

# 132. Audit Metadata Does Not Replace Logs

`updated_by` cannot explain why an exception occurred.

That belongs in Technical Logs.

---

# 133. Separation Summary

```text
Technical Log
=
What happened inside the software?
```

```text
Audit Metadata
=
Who created or last changed this business record?
```

```text
Business History
=
How did this business record change over time?
```

Version 1 fully implements the first two and only selected parts of the third.

---

# 134. V1 Audit Matrix

| Entity / Relationship | Created | Updated | Specific Audit |
|---|---|---|---|
| User | `created_at`, `created_by` | `updated_at`, `updated_by` | `last_login_at` |
| Role | `created_at`, `created_by` | `updated_at`, `updated_by` | — |
| Permission | `created_at` | — | — |
| User Role | `assigned_at`, `assigned_by` | — | — |
| Role Permission | `assigned_at`, `assigned_by` | — | — |
| Session | `created_at` | `last_activity_at` | expiration/status |
| Employee | `created_at`, `created_by` | `updated_at`, `updated_by` | — |
| Client | `created_at`, `created_by` | `updated_at`, `updated_by` | — |
| Project | `created_at`, `created_by` | `updated_at`, `updated_by` | — |
| Project Member | `assigned_at`, `assigned_by` | — | — |
| Order | `created_at`, `created_by` | `updated_at`, `updated_by` | — |
| Order Type | `created_at`, `created_by` | `updated_at`, `updated_by` | — |
| Checklist Item | — | — | — |
| Folder Link | — | — | — |
| Calculator Template | `created_at`, `created_by` | `updated_at`, `updated_by` | — |
| Template Version | `created_at`, `created_by` | immutable after publish | `published_at`, `published_by` |
| Order Calculator | `created_at`, `created_by` | `updated_at`, `updated_by` | `last_calculated_at` |
| Cost Item | `created_at`, `created_by` | `updated_at`, `updated_by` | — |

---

# 135. V1 History Matrix

| Area | Full History in V1? |
|---|---|
| User field changes | No |
| Login history | No |
| Role assignment history | No |
| Employee changes | No |
| Client changes | No |
| Project field changes | No |
| Project Team history | No |
| Order status history | No |
| Order priority history | No |
| Selling Price revisions | No |
| Cost Price revisions | No |
| Checklist completion history | No |
| Template Published Versions | **Yes** |
| Calculator Draft edit history | No |
| Cost Item edit/delete history | No |

---

# 136. Why Template Versions Are Different

Calculator Template Versions require permanent history because pricing logic must remain reproducible.

Therefore:

```text
Published Version History
```

is a core Version 1 requirement.

Most other business entities only need current state plus basic audit metadata.

---

# 137. Testing Requirements

Tests should verify:

```text
created_by set correctly

updated_by set correctly

created_at generated correctly

updated_at changes on edit

Read operation does not alter updated_at

First Director allows created_by = NULL

Project Member assigned_by stored

Role assignment assigned_by stored

Template published_by stored

Published Template Version immutable

Cost Item audit fields updated

Sensitive values absent from logs where testable
```

---

# 138. Security Testing

Where practical, verify application logging does not accidentally serialize:

```text
Password

Password Hash

Raw Session Token
```

---

# 139. Audit Permission

Version 1 does not require a separate:

```text
audit.view
```

permission.

Audit metadata shown inside normal record details follows access to that record.

If detailed audit/history becomes a future module, dedicated permissions may be introduced.

---

# 140. Technical Log Access

Normal ERP Users do not access server Technical Logs through the ERP UI.

Logs are for administrators/developers.

Do not build a Log Viewer screen in Version 1.

---

# 141. Audit Dashboard

Version 1 does not require an Audit Dashboard.

Do not build:

```text
Recent administrator actions

Security event timeline

Global changes feed
```

until a real need exists.

---

# 142. Activity Feed

Version 1 does not include a general Activity Feed.

A future activity/history feature may use targeted business events if needed.

---

# 143. Version 1 Non-Goals

Version 1 does not include:

```text
Universal Audit Log

Full Entity Version History

Field-by-Field Change History

Immutable Financial Ledger

Project Team Timeline

Order Status Timeline

User Login History Database

Administrator Activity Dashboard

Log Viewer UI

SIEM Integration

Event Sourcing
```

---

# 144. Future Extensions

Possible future additions include:

- Order Status History
- Project Team History
- Financial Change History
- Security Event History
- Administrator Action History
- Entity revision timelines
- Immutable Cost adjustment records
- Audit report exports

These should be added based on real operational or legal requirements.

---

# 145. Simplicity Rule

Before adding new history storage, ask:

```text
What business question must Lithograph be able to answer later?
```

If there is no clear question, do not store extra history merely because it is technically possible.

---

# 146. Final Logging Principle

Technical Logs should help answer:

```text
Why did the software behave this way?
```

Audit Metadata should help answer:

```text
Who created or last changed this record?
```

Historical Template Versions should answer:

```text
Which pricing logic was used for this Order?
```

Version 1 deliberately stops before implementing a universal business-history system.

The core rule is:

```text
Log enough to operate the system.

Audit enough to understand responsibility.

Preserve history only where the business truly needs it.
```

---

**End of Document**