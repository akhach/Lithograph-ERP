# Lithograph ERP

**Document:** 40_V1_Error_Codes_and_User_Messages.md  
**Version:** 1.0  
**Status:** Approved  
**Project:** Lithograph ERP  
**Area:** API Errors / User Messages

**Related Documents:**
- `README.md`
- `AI_RULES.md`
- `07_Authentication.md`
- `11_Employees_Module.md`
- `12_Clients_Module.md`
- `13_Projects_Module.md`
- `14_Orders_Module.md`
- `15_Calculator_Module.md`
- `16_Reports_Module.md`
- `19_API_Design_Guidelines.md`
- `20_Testing_Strategy.md`
- `23_Frontend_Architecture.md`
- `24_Backend_Architecture.md`
- `25_Development_Workflow_for_AI.md`
- `27_Authentication_Implementation_Plan.md`
- `28_Employees_Implementation_Plan.md`
- `29_Clients_Implementation_Plan.md`
- `30_Projects_Implementation_Plan.md`
- `31_Orders_Implementation_Plan.md`
- `32_Calculator_Foundation_Implementation_Plan.md`
- `33_Order_Calculator_Implementation_Plan.md`
- `34_Costs_Implementation_Plan.md`
- `35_Reports_Implementation_Plan.md`
- `38_V1_Role_and_Permission_Matrix.md`

---

# 1. Purpose

This document defines the canonical Version 1 error model for Lithograph ERP.

Its purpose is to keep:

```text
Backend errors

Frontend messages

Integration tests

AI-generated code

Operational logs
```

consistent across all modules.

---

# 2. Main Principle

Backend should return:

```text
Stable machine-readable error code
+
Human-readable message
+
Optional field validation details
```

Frontend should not attempt to identify business errors by parsing arbitrary message text.

---

# 3. Standard Error Shape

Canonical response:

```json
{
  "code": "CLIENT_INACTIVE",
  "message": "The selected Client is inactive.",
  "errors": null
}
```

---

# 4. Validation Error Shape

Example:

```json
{
  "code": "VALIDATION_FAILED",
  "message": "One or more fields are invalid.",
  "errors": {
    "name": [
      "Name is required."
    ],
    "deadline": [
      "Deadline cannot be before Start Date."
    ]
  }
}
```

---

# 5. Field Naming

Validation error keys should use the globally configured API JSON naming convention.

Do not invent separate field names specifically for error responses.

---

# 6. Stable Error Codes

Error codes are part of the API contract.

Once frontend/tests depend on a code, avoid renaming it casually.

---

# 7. Error Code Convention

Use:

```text
UPPER_SNAKE_CASE
```

Examples:

```text
CLIENT_NOT_FOUND

PROJECT_COMPLETED

CALCULATOR_CONCURRENCY_CONFLICT
```

---

# 8. Do Not Encode HTTP Status in Error Code

Avoid codes such as:

```text
ERROR_400_CLIENT

ERROR_409_DUPLICATE
```

HTTP status is already available separately.

---

# 9. HTTP Status Mapping

General mapping:

```text
400
Invalid request / validation / business input

401
Not authenticated

403
Authenticated but not authorized

404
Requested resource not found

409
Conflict with current resource/database state

500
Unexpected server failure
```

---

# 10. 201 / 204 Are Not Error Responses

Do not return error-shaped payloads for normal successful operations.

---

# 11. 401 — Authentication Required

Canonical code:

```text
AUTHENTICATION_REQUIRED
```

Message:

```text
Authentication is required.
```

---

# 12. Invalid or Expired Session

Use:

```text
SESSION_INVALID
```

or:

```text
SESSION_EXPIRED
```

where the backend can distinguish meaningfully.

Frontend behavior generally remains:

```text
Clear current authentication state
→ redirect to Login
```

---

# 13. 403 — Permission Denied

Canonical code:

```text
PERMISSION_DENIED
```

Message:

```text
You do not have permission to perform this action.
```

---

# 14. Do Not Reveal Excessive Security Detail

Normal 403 response does not need to expose:

```text
Internal authorization policy names

Role internals

Database permission mappings
```

---

# 15. Optional Permission Metadata

For development/admin troubleshooting, backend logs may include the required Permission.

Do not necessarily expose it to all end users.

---

# 16. 404 — Generic Resource Not Found

Use domain-specific codes whenever practical.

Examples:

```text
CLIENT_NOT_FOUND

PROJECT_NOT_FOUND

ORDER_NOT_FOUND
```

---

# 17. Do Not Reveal Hidden Resources

Where authorization/security requires it, a resource inaccessible to the User may be treated as:

```text
403
```

or:

```text
404
```

according to the application's chosen security model.

Keep behavior consistent.

---

# 18. 409 — Conflict

Use for state conflicts such as:

```text
Duplicate unique resource

Concurrent edit

Final Director protection

Already-linked User

Immutable Template Version
```

---

# 19. Validation vs Conflict

Use:

```text
400
```

when request data is inherently invalid.

Use:

```text
409
```

when the request could be valid but conflicts with current persisted state.

---

# 20. Unexpected Server Error

Canonical production response:

```json
{
  "code": "INTERNAL_SERVER_ERROR",
  "message": "An unexpected error occurred.",
  "errors": null
}
```

---

# 21. No Internal Details in Production

Do not expose:

```text
Stack traces

SQL statements

Constraint names

Connection strings

Server filesystem paths

Class names
```

---

# 22. Correlation / Request ID

If infrastructure provides a request/correlation ID, it may be returned separately for support diagnostics.

This is optional for V1.

---

# 23. Validation Codes

Canonical general validation code:

```text
VALIDATION_FAILED
```

---

# 24. Required Field Message

Preferred form:

```text
Name is required.
```

Avoid:

```text
The provided field named Name cannot have a null or empty string because validation failed.
```

---

# 25. String Length Message

Preferred:

```text
Name must be 250 characters or fewer.
```

---

# 26. Invalid Date Range

Code:

```text
INVALID_DATE_RANGE
```

Message:

```text
The end date cannot be before the start date.
```

---

# 27. Invalid Enum / Status Value

Code:

```text
INVALID_STATUS
```

or module-specific where necessary.

Message:

```text
The selected status is not valid.
```

---

# 28. Invalid Priority

Code:

```text
INVALID_PRIORITY
```

Message:

```text
The selected priority is not valid.
```

---

# 29. Authentication Error Codes

Canonical V1 Authentication errors include:

```text
AUTHENTICATION_REQUIRED

INVALID_CREDENTIALS

USER_INACTIVE

SESSION_INVALID

SESSION_EXPIRED

SETUP_NOT_REQUIRED

SETUP_ALREADY_COMPLETED

USERNAME_ALREADY_EXISTS

USER_NOT_FOUND

FINAL_DIRECTOR_REQUIRED
```

---

# 30. Invalid Credentials

Code:

```text
INVALID_CREDENTIALS
```

Message:

```text
Invalid username or password.
```

Use one generic message.

Do not reveal whether Username exists.

---

# 31. Inactive User

Recommended login behavior may still use:

```text
INVALID_CREDENTIALS
```

to avoid unnecessary account-state disclosure.

Internally, backend/logs may know the User is inactive.

---

# 32. Administrative Inactive User Error

For administrative actions where account identity is already known, code may be:

```text
USER_INACTIVE
```

Message:

```text
This User is inactive.
```

---

# 33. Username Duplicate

Code:

```text
USERNAME_ALREADY_EXISTS
```

HTTP:

```text
409
```

Message:

```text
That username is already in use.
```

---

# 34. User Not Found

Code:

```text
USER_NOT_FOUND
```

HTTP:

```text
404
```

---

# 35. First-Run Setup Already Completed

Code:

```text
SETUP_ALREADY_COMPLETED
```

HTTP:

```text
409
```

Message:

```text
Initial setup has already been completed.
```

---

# 36. Setup Not Required

If frontend attempts Setup after system initialization:

```text
SETUP_NOT_REQUIRED
```

may be used if more semantically appropriate.

Do not use several overlapping setup codes without need.

---

# 37. Final Director Protection

Code:

```text
FINAL_DIRECTOR_REQUIRED
```

HTTP:

```text
409
```

Message:

```text
At least one active Director must remain.
```

---

# 38. Role Error Codes

Canonical:

```text
ROLE_NOT_FOUND

ROLE_NAME_ALREADY_EXISTS

SYSTEM_ROLE_PROTECTED

ROLE_ALREADY_ASSIGNED

ROLE_NOT_ASSIGNED

PERMISSION_NOT_FOUND
```

---

# 39. System Role Protected

Code:

```text
SYSTEM_ROLE_PROTECTED
```

Message:

```text
This system Role cannot be modified in that way.
```

---

# 40. Permission Not Found

Normally indicates invalid administration request.

Code:

```text
PERMISSION_NOT_FOUND
```

HTTP:

```text
404
```

---

# 41. Employees Error Codes

Canonical:

```text
EMPLOYEE_NOT_FOUND

USER_ALREADY_LINKED_TO_EMPLOYEE

EMPLOYEE_ALREADY_LINKED_TO_USER

EMPLOYEE_USER_LINK_NOT_FOUND

EMPLOYEE_INACTIVE
```

---

# 42. Employee Not Found

Code:

```text
EMPLOYEE_NOT_FOUND
```

HTTP:

```text
404
```

---

# 43. User Already Linked to Another Employee

Code:

```text
USER_ALREADY_LINKED_TO_EMPLOYEE
```

HTTP:

```text
409
```

Message:

```text
This User is already linked to another Employee.
```

---

# 44. Employee Already Linked

Code:

```text
EMPLOYEE_ALREADY_LINKED_TO_USER
```

HTTP:

```text
409
```

Message:

```text
This Employee is already linked to a User. Unlink the current User first.
```

---

# 45. Employee Inactive

When attempting a new Project Team assignment:

```text
EMPLOYEE_INACTIVE
```

HTTP:

```text
400
```

Message:

```text
Inactive Employees cannot be assigned to new Project roles.
```

---

# 46. Clients Error Codes

Canonical:

```text
CLIENT_NOT_FOUND

CLIENT_INACTIVE

CLIENT_BUSINESS_ID_CONFLICT
```

---

# 47. Client Not Found

Code:

```text
CLIENT_NOT_FOUND
```

HTTP:

```text
404
```

---

# 48. Client Inactive

Code:

```text
CLIENT_INACTIVE
```

HTTP:

```text
400
```

Message:

```text
The selected Client is inactive.
```

---

# 49. Client Business ID Conflict

This should be extremely rare because Business ID generation is backend-controlled.

Code:

```text
CLIENT_BUSINESS_ID_CONFLICT
```

HTTP:

```text
409
```

Usually this indicates a numbering/concurrency/configuration problem and should also be logged.

---

# 50. Projects Error Codes

Canonical:

```text
PROJECT_NOT_FOUND

PROJECT_COMPLETED

PROJECT_CANCELLED

PROJECT_CLOSED

PROJECT_OWNER_CONFLICT

PROJECT_ASSIGNEE_CONFLICT

PROJECT_MEMBER_ALREADY_EXISTS

PROJECT_MEMBER_NOT_FOUND

PROJECT_INVALID_STATUS
```

---

# 51. Project Not Found

Code:

```text
PROJECT_NOT_FOUND
```

HTTP:

```text
404
```

---

# 52. Project Completed

When an action specifically cannot be performed on Completed Project:

```text
PROJECT_COMPLETED
```

Message:

```text
This action is not allowed because the Project is completed.
```

---

# 53. Project Cancelled

Code:

```text
PROJECT_CANCELLED
```

Message:

```text
This action is not allowed because the Project is cancelled.
```

---

# 54. Generic Closed Project

Where Completed and Cancelled share the same rule, code may be:

```text
PROJECT_CLOSED
```

Message:

```text
This action is not allowed for a completed or cancelled Project.
```

Prefer one consistent pattern per endpoint.

---

# 55. Owner Conflict

Code:

```text
PROJECT_OWNER_CONFLICT
```

HTTP:

```text
409
```

Message:

```text
The Project already has an Owner.
```

For normal PUT replacement behavior, this error may not occur unless concurrency causes a conflict.

---

# 56. Assignee Conflict

Code:

```text
PROJECT_ASSIGNEE_CONFLICT
```

HTTP:

```text
409
```

---

# 57. Duplicate Project Member

Code:

```text
PROJECT_MEMBER_ALREADY_EXISTS
```

HTTP:

```text
409
```

Message:

```text
This Employee already has that Project role.
```

---

# 58. Project Member Not Found

Code:

```text
PROJECT_MEMBER_NOT_FOUND
```

HTTP:

```text
404
```

---

# 59. Orders Error Codes

Canonical:

```text
ORDER_NOT_FOUND

ORDER_TYPE_NOT_FOUND

ORDER_TYPE_INACTIVE

ORDER_TYPE_NAME_ALREADY_EXISTS

ORDER_PROJECT_CHANGE_NOT_ALLOWED

ORDER_TYPE_CHANGE_REQUIRES_CALCULATOR_RESET

ORDER_CANCELLED

ORDER_COMPLETED

CHECKLIST_ITEM_NOT_FOUND

FOLDER_LINK_NOT_FOUND
```

---

# 60. Order Not Found

Code:

```text
ORDER_NOT_FOUND
```

HTTP:

```text
404
```

---

# 61. Order Type Not Found

Code:

```text
ORDER_TYPE_NOT_FOUND
```

HTTP:

```text
404
```

---

# 62. Inactive Order Type

Code:

```text
ORDER_TYPE_INACTIVE
```

HTTP:

```text
400
```

Message:

```text
Inactive Order Types cannot be selected for new Orders.
```

---

# 63. Duplicate Order Type Name

Code:

```text
ORDER_TYPE_NAME_ALREADY_EXISTS
```

HTTP:

```text
409
```

Message:

```text
An Order Type with that name already exists.
```

---

# 64. Project Change Not Allowed

Code:

```text
ORDER_PROJECT_CHANGE_NOT_ALLOWED
```

HTTP:

```text
409
```

Message:

```text
The Project can only be changed while the Order is in Draft status.
```

---

# 65. Order Type Requires Calculator Reset

Code:

```text
ORDER_TYPE_CHANGE_REQUIRES_CALCULATOR_RESET
```

HTTP:

```text
409
```

Message:

```text
Changing the Order Type requires an explicit Calculator reset.
```

---

# 66. Order Cancelled

Code:

```text
ORDER_CANCELLED
```

Use when mutation is prohibited because Order is cancelled.

---

# 67. Checklist Item Not Found

Code:

```text
CHECKLIST_ITEM_NOT_FOUND
```

HTTP:

```text
404
```

---

# 68. Folder Link Not Found

Code:

```text
FOLDER_LINK_NOT_FOUND
```

HTTP:

```text
404
```

---

# 69. Parent-Child Mismatch

For nested resources, avoid revealing unrelated records.

A Checklist Item belonging to another Order should generally behave as:

```text
CHECKLIST_ITEM_NOT_FOUND
```

for the current route.

Same principle for Folder Links and Cost Items.

---

# 70. Calculator Template Errors

Canonical:

```text
CALCULATOR_TEMPLATE_NOT_FOUND

CALCULATOR_TEMPLATE_INACTIVE

TEMPLATE_VERSION_NOT_FOUND

TEMPLATE_VERSION_IMMUTABLE

TEMPLATE_DRAFT_ALREADY_EXISTS

TEMPLATE_VALIDATION_FAILED

TEMPLATE_PUBLISH_FAILED

TEMPLATE_NO_PUBLISHED_VERSION

TEMPLATE_SCHEMA_VERSION_UNSUPPORTED
```

---

# 71. Calculator Template Not Found

Code:

```text
CALCULATOR_TEMPLATE_NOT_FOUND
```

HTTP:

```text
404
```

---

# 72. Template Inactive

Code:

```text
CALCULATOR_TEMPLATE_INACTIVE
```

HTTP:

```text
400
```

Message:

```text
The selected Calculator Template is inactive.
```

---

# 73. Template Version Immutable

Code:

```text
TEMPLATE_VERSION_IMMUTABLE
```

HTTP:

```text
409
```

Message:

```text
Published and retired Template Versions cannot be edited.
```

---

# 74. Draft Already Exists

Code:

```text
TEMPLATE_DRAFT_ALREADY_EXISTS
```

HTTP:

```text
409
```

Message:

```text
This Calculator Template already has a Draft Version.
```

---

# 75. Template Validation Failed

Code:

```text
TEMPLATE_VALIDATION_FAILED
```

HTTP:

```text
400
```

Response should contain structured Calculator validation details where useful.

---

# 76. Template Publish Failed

Do not use a generic publish error for known validation failures.

Use:

```text
TEMPLATE_VALIDATION_FAILED
```

for invalid definitions.

Reserve:

```text
TEMPLATE_PUBLISH_FAILED
```

for other controlled publish failures.

---

# 77. Unsupported Definition Schema

Code:

```text
TEMPLATE_SCHEMA_VERSION_UNSUPPORTED
```

HTTP:

```text
409
```

or:

```text
400
```

depending on context.

Message:

```text
This Calculator Template uses an unsupported definition version.
```

---

# 78. Formula Validation Codes

Structured Calculator validation may use granular codes such as:

```text
INVALID_FIELD_KEY

DUPLICATE_FIELD_KEY

UNKNOWN_FIELD_REFERENCE

CIRCULAR_REFERENCE

UNKNOWN_FUNCTION

INVALID_FORMULA_SYNTAX

INVALID_FORMULA_ARGUMENTS

SELLING_PRICE_FIELD_MISSING

SELLING_PRICE_FIELD_INVALID
```

---

# 79. Invalid Field Key

Code:

```text
INVALID_FIELD_KEY
```

Message example:

```text
Field key must use lowercase snake_case.
```

---

# 80. Duplicate Field Key

Code:

```text
DUPLICATE_FIELD_KEY
```

Message:

```text
Each Calculator field key must be unique.
```

---

# 81. Unknown Field Reference

Code:

```text
UNKNOWN_FIELD_REFERENCE
```

Message example:

```text
Formula references unknown field 'material_price'.
```

---

# 82. Circular Reference

Code:

```text
CIRCULAR_REFERENCE
```

Message:

```text
A circular Calculator dependency was detected.
```

Include involved field keys where practical.

---

# 83. Unknown Function

Code:

```text
UNKNOWN_FUNCTION
```

Message:

```text
The formula uses an unsupported function.
```

---

# 84. Invalid Formula Syntax

Code:

```text
INVALID_FORMULA_SYNTAX
```

Message should identify the affected field and useful syntax location where possible.

Do not expose parser internals unnecessarily.

---

# 85. Selling Price Field Missing

Code:

```text
SELLING_PRICE_FIELD_MISSING
```

Message:

```text
A Selling Price output field must be configured before publication.
```

---

# 86. Order Calculator Runtime Errors

Canonical:

```text
CALCULATOR_NOT_CONFIGURED

CALCULATOR_NO_PUBLISHED_VERSION

CALCULATOR_NOT_FOUND

CALCULATOR_INVALID_INPUT

CALCULATOR_CALCULATION_ERROR

CALCULATOR_CONCURRENCY_CONFLICT

CALCULATOR_RESET_REQUIRED

TEMPLATE_VERSION_NOT_AVAILABLE
```

---

# 87. Calculator Not Configured

Code:

```text
CALCULATOR_NOT_CONFIGURED
```

HTTP:

```text
409
```

or a controlled successful no-calculator state for GET according to endpoint design.

Recommended mutation/open error message:

```text
No Calculator is configured for this Order Type.
```

---

# 88. No Published Version

Code:

```text
CALCULATOR_NO_PUBLISHED_VERSION
```

HTTP:

```text
409
```

Message:

```text
The Calculator Template has no published version.
```

---

# 89. Invalid Calculator Input

Code:

```text
CALCULATOR_INVALID_INPUT
```

HTTP:

```text
400
```

Use field-level errors.

Examples:

```text
Expected a number.

Selected option is not valid.

Value is greater than the allowed maximum.
```

---

# 90. Calculation Error

Code:

```text
CALCULATOR_CALCULATION_ERROR
```

HTTP:

```text
400
```

or controlled incomplete result when appropriate.

Examples:

```text
Division by zero

Missing required value

Invalid mathematical domain
```

---

# 91. Calculator Concurrency Conflict

Code:

```text
CALCULATOR_CONCURRENCY_CONFLICT
```

HTTP:

```text
409
```

Message:

```text
This Calculator was changed by another User. Reload the latest values before saving.
```

---

# 92. Calculator Reset Required

Code:

```text
CALCULATOR_RESET_REQUIRED
```

may be used for operations that require an explicit reset.

Where Order Type change is the cause, prefer the more specific:

```text
ORDER_TYPE_CHANGE_REQUIRES_CALCULATOR_RESET
```

---

# 93. Template Version Not Available

Code:

```text
TEMPLATE_VERSION_NOT_AVAILABLE
```

Use when the exact required Version cannot be resolved safely.

This should normally indicate a serious configuration/data-integrity problem and should be logged.

---

# 94. Cost Error Codes

Canonical:

```text
COST_ITEM_NOT_FOUND

COST_AMOUNT_INVALID

COST_MUTATION_NOT_ALLOWED

COST_PARENT_MISMATCH
```

---

# 95. Cost Item Not Found

Code:

```text
COST_ITEM_NOT_FOUND
```

HTTP:

```text
404
```

For cross-parent access, prefer the same not-found response.

---

# 96. Invalid Cost Amount

Code:

```text
COST_AMOUNT_INVALID
```

HTTP:

```text
400
```

Message:

```text
Cost amount must be greater than zero.
```

if using the stricter creation rule.

---

# 97. Cost Mutation Not Allowed

Code:

```text
COST_MUTATION_NOT_ALLOWED
```

HTTP:

```text
409
```

Message should explain the relevant lifecycle restriction.

Example:

```text
Costs cannot be changed on a cancelled Order.
```

---

# 98. Cost Parent Mismatch

This code may be useful internally/tests, but public nested APIs should generally return:

```text
COST_ITEM_NOT_FOUND
```

to avoid leaking another Order's child record.

---

# 99. Reports Error Codes

Canonical:

```text
REPORT_INVALID_FILTER

REPORT_INVALID_SORT

REPORT_DATE_RANGE_INVALID

REPORT_FINANCIAL_ACCESS_DENIED
```

Most authorization failures may still simply use:

```text
PERMISSION_DENIED
```

---

# 100. Invalid Report Filter

Code:

```text
REPORT_INVALID_FILTER
```

HTTP:

```text
400
```

---

# 101. Invalid Report Sort

Code:

```text
REPORT_INVALID_SORT
```

HTTP:

```text
400
```

Message:

```text
The selected report sort field is not supported.
```

---

# 102. Protected Financial Sort

If User attempts to sort by hidden Cost/Selling fields, return:

```text
PERMISSION_DENIED
```

or:

```text
REPORT_INVALID_SORT
```

Recommended:

```text
PERMISSION_DENIED
```

because the field exists but User cannot access it.

---

# 103. Invalid Report Date Range

Code:

```text
REPORT_DATE_RANGE_INVALID
```

Message:

```text
The start date cannot be after the end date.
```

---

# 104. Business ID Errors

Business IDs are backend-generated.

Users normally should not encounter numbering errors.

Potential internal conflict codes:

```text
CLIENT_BUSINESS_ID_CONFLICT

PROJECT_BUSINESS_ID_CONFLICT

ORDER_BUSINESS_ID_CONFLICT
```

---

# 105. Numbering Conflicts Are Operationally Important

If these occur repeatedly, do not simply tell Users to retry indefinitely.

Log and investigate the Business ID generation mechanism.

---

# 106. Generic Conflict Codes

Avoid excessive use of one generic:

```text
CONFLICT
```

when frontend needs to distinguish specific recoverable cases.

---

# 107. Generic Not Found

Avoid only:

```text
NOT_FOUND
```

for all resources.

Domain-specific codes improve frontend behavior and tests.

---

# 108. Generic Validation Is Fine for Field Errors

For normal form validation:

```text
VALIDATION_FAILED
```

plus field errors is preferable to inventing an error code for every required field.

---

# 109. Frontend Error Handling Layers

Frontend should distinguish:

```text
Authentication errors

Authorization errors

Field validation errors

Business-state conflicts

Concurrency conflicts

Unexpected system errors
```

---

# 110. Authentication Frontend Handling

For:

```text
AUTHENTICATION_REQUIRED

SESSION_INVALID

SESSION_EXPIRED
```

frontend should:

```text
Clear authentication state

Redirect to Login

Preserve safe return URL where appropriate
```

---

# 111. 403 Frontend Handling

For:

```text
PERMISSION_DENIED
```

show:

```text
You do not have permission to perform this action.
```

Do not redirect to Login because the User is already authenticated.

---

# 112. Form Validation Handling

For:

```text
VALIDATION_FAILED
```

map:

```text
errors[field]
```

to the appropriate form input.

---

# 113. Business Conflict Handling

For codes such as:

```text
CLIENT_INACTIVE

EMPLOYEE_INACTIVE

ORDER_TYPE_INACTIVE
```

show the backend business message near the relevant action/form.

---

# 114. Concurrency Handling

For:

```text
CALCULATOR_CONCURRENCY_CONFLICT
```

do not simply show a generic toast.

Provide:

```text
Reload
```

or equivalent recovery path.

---

# 115. Destructive Action Errors

If deactivate/delete/reset fails, leave the current screen/data intact.

Do not optimistically remove the record unless backend success is confirmed.

---

# 116. Toast vs Inline Message

Use inline field messages for:

```text
Validation
```

Use page/section-level message for:

```text
Calculation errors

Report errors

Load errors
```

Use toast/snackbar where appropriate for:

```text
Successful save

Simple operation failure
```

Do not rely exclusively on disappearing toasts for important errors.

---

# 117. Error Message Tone

Messages should be:

```text
Clear

Neutral

Specific

Actionable where possible
```

---

# 118. Avoid Developer Language

Bad:

```text
FK constraint violation on FK_orders_project_id.
```

Good:

```text
The selected Project could not be found.
```

---

# 119. Avoid Blaming the User

Bad:

```text
You entered an invalid stupid date.
```

Good:

```text
Deadline cannot be before Start Date.
```

---

# 120. Avoid Vague Messages

Bad:

```text
Something went wrong.
```

for known validation/business cases.

Use it only as a fallback unexpected error.

---

# 121. Use Domain Terminology

Say:

```text
Order

Project

Client

Checklist Item

Calculator Template
```

consistently.

Do not randomly alternate with:

```text
Task

Job

Customer Record
```

---

# 122. Error Messages Are Not Business Logic

Frontend should not infer state by testing exact message text.

Use:

```text
code
```

for logic.

---

# 123. Localization Readiness

Version 1 may use English messages.

Stable error codes allow Armenian localization later without changing frontend logic.

---

# 124. Backend Message vs Frontend Message

Frontend may replace a generic backend message with a better UI-specific message when the code is known.

Example:

```text
FINAL_DIRECTOR_REQUIRED
```

could display:

```text
You cannot deactivate this User because at least one active Director must remain.
```

---

# 125. Do Not Contradict Backend Meaning

Frontend wording may improve clarity but must not change the underlying business rule.

---

# 126. Error Logging

Known user/business errors generally do not require Error-level logging.

Examples:

```text
Validation failure

Inactive Client

Duplicate username

Permission denied
```

---

# 127. Unexpected Failures

Unexpected exceptions should be logged with sufficient technical context.

---

# 128. Sensitive Logging

Do not log:

```text
Passwords

Password reset values

Session tokens

Full Calculator financial payloads unnecessarily

Database credentials
```

---

# 129. Permission Denied Logging

Routine 403 responses need not flood logs.

Security-significant unusual patterns may be logged selectively.

---

# 130. Concurrency Logging

Calculator concurrency conflicts are expected and normally not server errors.

Debug/Information logging may be sufficient.

---

# 131. Data Integrity Failures

Unexpected conditions such as:

```text
Order Calculator points to missing Template Version

Cost Price differs from Cost Item SUM due to internal bug

Duplicate supposedly-unique Business ID
```

should be logged as serious operational errors.

---

# 132. Database Constraint Translation

Infrastructure should translate known database conflicts into domain/API errors.

Example:

```text
unique auth.users.normalized_username
```

becomes:

```text
USERNAME_ALREADY_EXISTS
```

---

# 133. Do Not Expose Constraint Names

Never return:

```text
IX_users_normalized_username
```

to frontend as user-facing error.

---

# 134. Race Condition Translation

Application validation may say no duplicate exists, but DB constraint may still fail under concurrency.

Catch known DB exception and return the same canonical conflict code.

---

# 135. Example — User Link Race

Two requests link the same User to two Employees.

Database unique constraint wins.

Return:

```text
USER_ALREADY_LINKED_TO_EMPLOYEE
```

not generic 500.

---

# 136. Example — Project Owner Race

Concurrent Owner assignment violating partial unique index should translate to:

```text
PROJECT_OWNER_CONFLICT
```

---

# 137. Example — Order Calculator Race

Concurrent lazy creation violating unique `order_id` should normally recover by loading the existing Calculator.

It should not necessarily surface an error to the User.

---

# 138. Error Code Constants

Backend should centralize codes.

Conceptually:

```text
ErrorCodes.ClientInactive
ErrorCodes.OrderNotFound
ErrorCodes.CalculatorConcurrencyConflict
```

Avoid scattered string literals.

---

# 139. Frontend Error Code Constants

Frontend TypeScript may maintain a corresponding typed list where useful.

Do not duplicate arbitrary messages across screens.

---

# 140. Shared API Client

Shared API client should normalize:

```text
HTTP status

code

message

errors
```

into one predictable application error model.

---

# 141. Network Failure

A network failure may not contain a backend error response.

Frontend should distinguish:

```text
NETWORK_ERROR
```

as a client-side condition.

---

# 142. Network Error Message

Example:

```text
Could not connect to the server. Check your connection and try again.
```

---

# 143. Request Timeout

If client infrastructure supports timeout classification:

```text
REQUEST_TIMEOUT
```

may be a frontend-only code.

---

# 144. Server Unavailable

Do not mislabel infrastructure outage as:

```text
INVALID_CREDENTIALS
```

or another business error.

---

# 145. Load Error State

When page data fails to load:

```text
Show error state

Provide Retry

Keep navigation usable
```

---

# 146. Save Failure State

If Save fails:

```text
Keep User input

Show error

Do not pretend data was saved
```

---

# 147. Success Messages

Keep successful operation messages short.

Examples:

```text
Client created.

Project updated.

Cost Item deleted.

Calculator saved.
```

---

# 148. Avoid Success Spam

Do not show a toast for every checkbox click if it becomes distracting.

Use judgment.

---

# 149. Confirmation Messages

Destructive/high-impact operations should have explicit confirmation where useful.

Examples:

```text
Deactivate Client?

Delete Cost Item?

Publish Template Version?

Reset Calculator?
```

---

# 150. Confirmation Is Not Authorization

Even after frontend confirmation, backend still enforces permission/business rules.

---

# 151. Authentication Message Catalog

Recommended frontend wording:

```text
INVALID_CREDENTIALS
→ Invalid username or password.

SESSION_EXPIRED
→ Your session has expired. Please sign in again.

PERMISSION_DENIED
→ You do not have permission to perform this action.

FINAL_DIRECTOR_REQUIRED
→ At least one active Director must remain.
```

---

# 152. Employees Message Catalog

```text
USER_ALREADY_LINKED_TO_EMPLOYEE
→ This User is already linked to another Employee.

EMPLOYEE_ALREADY_LINKED_TO_USER
→ This Employee is already linked to a User.

EMPLOYEE_INACTIVE
→ Inactive Employees cannot be assigned to new Project roles.
```

---

# 153. Clients Message Catalog

```text
CLIENT_INACTIVE
→ The selected Client is inactive.

CLIENT_NOT_FOUND
→ The Client could not be found.
```

---

# 154. Projects Message Catalog

```text
PROJECT_CLOSED
→ This action is not allowed for a completed or cancelled Project.

PROJECT_MEMBER_ALREADY_EXISTS
→ This Employee already has that Project role.

PROJECT_OWNER_CONFLICT
→ The Project already has an Owner.

PROJECT_ASSIGNEE_CONFLICT
→ The Project already has an Assignee.
```

---

# 155. Orders Message Catalog

```text
ORDER_TYPE_INACTIVE
→ Inactive Order Types cannot be selected for new Orders.

ORDER_PROJECT_CHANGE_NOT_ALLOWED
→ The Project can only be changed while the Order is in Draft status.

ORDER_TYPE_CHANGE_REQUIRES_CALCULATOR_RESET
→ Changing the Order Type requires an explicit Calculator reset.

ORDER_CANCELLED
→ This action is not allowed because the Order is cancelled.
```

---

# 156. Calculator Message Catalog

```text
CALCULATOR_NOT_CONFIGURED
→ No Calculator is configured for this Order Type.

CALCULATOR_NO_PUBLISHED_VERSION
→ The Calculator Template has no published version.

CALCULATOR_CONCURRENCY_CONFLICT
→ This Calculator was changed by another User. Reload the latest values before saving.

TEMPLATE_VERSION_IMMUTABLE
→ Published and retired Template Versions cannot be edited.

TEMPLATE_DRAFT_ALREADY_EXISTS
→ This Calculator Template already has a Draft Version.
```

---

# 157. Costs Message Catalog

```text
COST_AMOUNT_INVALID
→ Cost amount must be greater than zero.

COST_ITEM_NOT_FOUND
→ The Cost Item could not be found.

COST_MUTATION_NOT_ALLOWED
→ Costs cannot be changed in the current Order state.
```

---

# 158. Reports Message Catalog

```text
REPORT_DATE_RANGE_INVALID
→ The start date cannot be after the end date.

REPORT_INVALID_SORT
→ The selected report sort field is not supported.

PERMISSION_DENIED
→ You do not have permission to access this report data.
```

---

# 159. Error Page Requirements

Frontend should have a general error state/page for:

```text
Unexpected route/page load failure

403 access denied

404 application route/resource where appropriate
```

---

# 160. 404 Page

Application route not found may show:

```text
Page not found.
```

with link to:

```text
Dashboard
```

---

# 161. Resource Not Found

If an Order URL references a missing Order:

```text
Order not found.
```

Prefer module-specific screen state instead of generic browser-like 404 page where practical.

---

# 162. 403 Page

For route-level access denial:

```text
Access denied.

You do not have permission to view this page.
```

---

# 163. 500 Page

For unexpected page failure:

```text
Something went wrong while loading this page.

Try again.
```

Do not display internal exception details.

---

# 164. Validation UX

When backend returns multiple field errors:

```text
Show all relevant errors
```

rather than only the first one.

---

# 165. Scroll/Focus

For long forms, frontend should focus/scroll toward the first invalid field where practical.

---

# 166. API Client Parsing

If backend response does not match expected error shape because of proxy/server failure, frontend should safely fall back to a generic message.

---

# 167. Error Contract Test

Integration tests should verify representative endpoints return:

```text
correct HTTP status

correct code

correct message shape

correct validation error structure
```

---

# 168. Authentication Contract Tests

Test:

```text
Wrong password
→ INVALID_CREDENTIALS

No session
→ AUTHENTICATION_REQUIRED

Insufficient permission
→ PERMISSION_DENIED
```

---

# 169. Conflict Contract Tests

Test:

```text
Duplicate username
→ USERNAME_ALREADY_EXISTS

Final Director deactivation
→ FINAL_DIRECTOR_REQUIRED

Duplicate Employee User link
→ USER_ALREADY_LINKED_TO_EMPLOYEE
```

---

# 170. Project Contract Tests

Test inactive Employee assignment:

```text
EMPLOYEE_INACTIVE
```

and closed Project Order creation:

```text
PROJECT_CLOSED
```

or chosen specific equivalent.

---

# 171. Order Contract Tests

Test:

```text
Inactive Order Type
→ ORDER_TYPE_INACTIVE

Order Type change after Calculator
→ ORDER_TYPE_CHANGE_REQUIRES_CALCULATOR_RESET
```

---

# 172. Calculator Contract Tests

Test:

```text
No Template
→ CALCULATOR_NOT_CONFIGURED

No Published Version
→ CALCULATOR_NO_PUBLISHED_VERSION

Stale save
→ CALCULATOR_CONCURRENCY_CONFLICT
```

---

# 173. Cost Contract Tests

Test:

```text
Negative/invalid amount
→ COST_AMOUNT_INVALID

Wrong parent
→ COST_ITEM_NOT_FOUND
```

---

# 174. Report Contract Tests

Test invalid date range:

```text
REPORT_DATE_RANGE_INVALID
```

and protected financial sorting:

```text
PERMISSION_DENIED
```

---

# 175. Frontend Component Tests

Shared error handling components should test:

```text
Field errors

Page errors

403

409 concurrency

Retry behavior
```

---

# 176. Error Code Documentation Rule

When adding a new business error:

```text
Define code

Define HTTP status

Define meaning

Define user-facing default message

Add tests
```

---

# 177. Do Not Invent Codes Ad Hoc

AI-generated code must not create new codes such as:

```text
BAD_ORDER_17

ERROR_EMP

SOMETHING_WRONG
```

without updating this catalog or relevant module specification.

---

# 178. Module-Specific Codes Are Preferred

Prefer:

```text
CLIENT_INACTIVE
```

over:

```text
RESOURCE_INACTIVE
```

when frontend behavior/domain meaning differs.

---

# 179. Avoid Excessive Hyper-Specific Codes

Do not create:

```text
CLIENT_NAME_REQUIRED

CLIENT_PHONE_TOO_LONG

CLIENT_EMAIL_TOO_LONG
```

for ordinary field validation.

Use:

```text
VALIDATION_FAILED
```

with field errors.

---

# 180. Business Errors vs Validation Errors

Example:

```text
name empty
→ VALIDATION_FAILED
```

Example:

```text
selected Client inactive
→ CLIENT_INACTIVE
```

This distinction should remain clear.

---

# 181. Concurrency Errors

Use 409 and dedicated codes where User action can recover through reload/retry.

---

# 182. Retry Safety

Frontend should not automatically retry mutation conflicts blindly.

Example:

```text
CALCULATOR_CONCURRENCY_CONFLICT
```

requires user review/reload.

---

# 183. Automatic Retry

Automatic retry may be acceptable for safe idempotent GET failures in infrastructure code, but not required for V1.

---

# 184. Duplicate Submit

Backend should remain safe if User double-clicks Create.

Database constraints/business IDs protect integrity where needed.

Frontend should also disable duplicate submission while request is pending.

---

# 185. Business ID Errors and User Message

If Business ID generation unexpectedly fails:

```text
The record could not be created because its Business ID could not be generated.
```

This should be rare and logged.

---

# 186. Formula Validation Display

Template Designer should associate validation errors with:

```text
fieldKey

elementId
```

where available.

---

# 187. Formula Error Example

```text
Field: final_price

Unknown field reference:
material_costt
```

This is more useful than only:

```text
Template invalid.
```

---

# 188. Publication Error UX

If Publish fails because of validation:

```text
Keep Draft unchanged

Show all validation errors

Do not switch status
```

---

# 189. Calculator Runtime Error UX

If calculation cannot complete:

```text
Keep current input state

Show error

Do not present stale calculated values as newly valid
```

---

# 190. Cost Error UX

If deleting Cost Item fails:

```text
Keep row visible

Show error

Do not locally reduce Total Cost
```

until backend confirms success.

---

# 191. Report Error UX

If filtered report fails:

```text
Keep selected filters

Show Retry

Do not reset filters unnecessarily
```

---

# 192. Dashboard Error UX

Dashboard error should not automatically log out the User unless error is Authentication-related.

---

# 193. Audit and User Messages

Do not show internal audit IDs in normal messages.

Example bad:

```text
updated_by 3a4f... caused conflict
```

Possible future improved message:

```text
This Calculator was changed by another User.
```

---

# 194. Future User Name in Conflict

If safely available later, conflict UI may show:

```text
Last updated by Samvel at 14:32.
```

Not required for V1.

---

# 195. Error Code Versioning

V1 error codes should remain backward-compatible as frontend/backend evolve together.

Remove codes only deliberately.

---

# 196. Deprecated Error Codes

If a code is replaced later:

```text
keep compatibility where necessary
```

or update all consumers atomically.

---

# 197. Error Catalog Ownership

This file is the central error-code reference.

Module docs may define additional context, but conflicting codes should be reconciled here.

---

# 198. AI Coding Rule

Before adding a new API error in code, AI should:

```text
Search this document

Reuse an existing code if semantically correct

Add a new code only when genuinely distinct

Update tests/documentation
```

---

# 199. Recommended Backend Error Types

A simple application error abstraction may include:

```text
Code

Message

HttpStatus

FieldErrors
```

Do not build a large exception hierarchy unless it provides real value.

---

# 200. Known Business Exceptions

Expected business errors should be translated predictably by centralized middleware or result handling.

---

# 201. Unexpected Exceptions

Unexpected exceptions should fall through centralized exception handling to:

```text
INTERNAL_SERVER_ERROR
```

and be logged.

---

# 202. Validation Implementation

ASP.NET validation errors should be normalized into the same API contract rather than returning several incompatible framework-native formats.

---

# 203. API Consistency Requirement

All endpoints should avoid returning a mix of:

```text
plain string

ProblemDetails

custom JSON

HTML error page
```

for normal application errors.

Use one consistent application contract.

---

# 204. Infrastructure Errors

Reverse proxy/database outages may not always produce the normal application contract.

Frontend must handle this gracefully.

---

# 205. Production Logging Correlation

Where available, attach request identifier to unexpected server logs so support can correlate a User-reported failure.

---

# 206. Security Principle

Error messages must provide enough information to fix normal business mistakes without exposing sensitive internals.

---

# 207. Usability Principle

A good error should answer:

```text
What happened?

Why could this action not be completed?

What should the User do next?
```

where practical.

---

# 208. Example — Good Concurrency Error

```text
This Calculator was changed by another User.

Reload the latest values before saving.
```

---

# 209. Example — Good Inactive Master Error

```text
The selected Client is inactive.

Choose an active Client or reactivate this Client first.
```

Frontend may add the second sentence if the User has the relevant workflow available.

---

# 210. Example — Good Template Error

```text
The Calculator Template has no published version.

Publish a valid Template Version before using this Calculator.
```

---

# 211. Example — Bad Security Error

Do not return:

```text
Authorization handler ProjectManagerPolicy failed because claim perm=projects.edit is absent from session token XYZ...
```

---

# 212. Example — Bad Database Error

Do not return:

```text
23505 unique_violation auth_users_normalized_username_key
```

---

# 213. Example — Bad Formula Error

Do not return:

```text
NullReferenceException at FormulaEvaluator.VisitBinaryNode line 247
```

---

# 214. Release Checklist

Before V1 release:

```text
[ ] All major endpoints use standard error shape

[ ] Authentication errors consistent

[ ] Permission errors return 403

[ ] Field validation returns structured errors

[ ] Business conflicts use stable codes

[ ] Calculator errors use stable codes

[ ] Concurrency returns 409

[ ] Database conflicts translated

[ ] Production responses hide internals

[ ] Frontend handles 401/403/404/409

[ ] Frontend preserves form data after failed saves

[ ] Report errors preserve filters

[ ] Error contract tests pass
```

---

# 215. Canonical Core Error Catalog

At minimum, V1 should recognize:

```text
AUTHENTICATION_REQUIRED
INVALID_CREDENTIALS
USER_INACTIVE
SESSION_INVALID
SESSION_EXPIRED
PERMISSION_DENIED

VALIDATION_FAILED
INVALID_DATE_RANGE
INVALID_STATUS
INVALID_PRIORITY

USER_NOT_FOUND
USERNAME_ALREADY_EXISTS
FINAL_DIRECTOR_REQUIRED

ROLE_NOT_FOUND
ROLE_NAME_ALREADY_EXISTS
SYSTEM_ROLE_PROTECTED
PERMISSION_NOT_FOUND

EMPLOYEE_NOT_FOUND
EMPLOYEE_INACTIVE
USER_ALREADY_LINKED_TO_EMPLOYEE
EMPLOYEE_ALREADY_LINKED_TO_USER

CLIENT_NOT_FOUND
CLIENT_INACTIVE

PROJECT_NOT_FOUND
PROJECT_COMPLETED
PROJECT_CANCELLED
PROJECT_CLOSED
PROJECT_OWNER_CONFLICT
PROJECT_ASSIGNEE_CONFLICT
PROJECT_MEMBER_ALREADY_EXISTS
PROJECT_MEMBER_NOT_FOUND

ORDER_NOT_FOUND
ORDER_TYPE_NOT_FOUND
ORDER_TYPE_INACTIVE
ORDER_TYPE_NAME_ALREADY_EXISTS
ORDER_PROJECT_CHANGE_NOT_ALLOWED
ORDER_TYPE_CHANGE_REQUIRES_CALCULATOR_RESET
ORDER_CANCELLED
CHECKLIST_ITEM_NOT_FOUND
FOLDER_LINK_NOT_FOUND

CALCULATOR_TEMPLATE_NOT_FOUND
CALCULATOR_TEMPLATE_INACTIVE
TEMPLATE_VERSION_NOT_FOUND
TEMPLATE_VERSION_IMMUTABLE
TEMPLATE_DRAFT_ALREADY_EXISTS
TEMPLATE_VALIDATION_FAILED
TEMPLATE_SCHEMA_VERSION_UNSUPPORTED

INVALID_FIELD_KEY
DUPLICATE_FIELD_KEY
UNKNOWN_FIELD_REFERENCE
CIRCULAR_REFERENCE
UNKNOWN_FUNCTION
INVALID_FORMULA_SYNTAX
INVALID_FORMULA_ARGUMENTS
SELLING_PRICE_FIELD_MISSING
SELLING_PRICE_FIELD_INVALID

CALCULATOR_NOT_CONFIGURED
CALCULATOR_NO_PUBLISHED_VERSION
CALCULATOR_INVALID_INPUT
CALCULATOR_CALCULATION_ERROR
CALCULATOR_CONCURRENCY_CONFLICT
TEMPLATE_VERSION_NOT_AVAILABLE

COST_ITEM_NOT_FOUND
COST_AMOUNT_INVALID
COST_MUTATION_NOT_ALLOWED

REPORT_INVALID_FILTER
REPORT_INVALID_SORT
REPORT_DATE_RANGE_INVALID

PROJECT_BUSINESS_ID_CONFLICT
ORDER_BUSINESS_ID_CONFLICT
CLIENT_BUSINESS_ID_CONFLICT

INTERNAL_SERVER_ERROR
```

---

# 216. Error Catalog Is Not a Requirement to Trigger Every Code

Do not manufacture application paths solely so every listed code is used.

Some exist to normalize exceptional conflicts if they occur.

---

# 217. Recommended AI Audit Task

```text
Read:
- AI_RULES.md
- docs/19_API_Design_Guidelines.md
- docs/20_Testing_Strategy.md
- docs/40_V1_Error_Codes_and_User_Messages.md
- all module implementation plans

Task:
Audit the repository's API error handling.

Verify:
- one consistent error response shape
- stable error codes
- correct HTTP status codes
- validation fields are structured
- expected business conflicts do not become 500 errors
- known PostgreSQL constraint failures are translated
- permission failures use 403
- authentication failures use 401
- concurrency conflicts use 409
- production errors hide internals
- frontend does not branch on message text

Produce:
1. inconsistent responses
2. missing translations
3. incorrect status codes
4. duplicate/overlapping error codes
5. frontend handling gaps
6. missing tests

Do not:
- invent a large exception hierarchy
- expose SQL/stack traces
- change business rules
- replace stable error codes without updating all consumers
```

---

# 218. Final Error Handling Principle

Lithograph ERP errors should allow the system to communicate clearly with both:

```text
Humans
```

and:

```text
Software
```

Humans receive:

```text
clear messages
```

Software receives:

```text
stable codes
```

The central rule is:

```text
Do not make the frontend guess what happened.

Return a stable code, a useful message, and structured validation details.
```

---

**End of Document**