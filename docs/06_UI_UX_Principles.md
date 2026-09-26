# Lithograph ERP

**Document:** 06_UI_UX_Principles.md  
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
- `04_Data_Dictionary.md`
- `05_Numbering_System.md`

---

# 1. Purpose

This document defines the User Interface and User Experience principles for Lithograph ERP.

Its purpose is to keep the application:

- Consistent
- Fast
- Easy to learn
- Efficient for daily work
- Predictable across modules

Lithograph ERP is a professional business application running in a browser.

It is not a marketing website.

---

# 2. Primary UX Goal

The interface should help employees complete work quickly with minimal confusion.

Priority order:

```text
Productivity
Clarity
Consistency
Speed
Visual appearance
```

Visual design should support the work rather than become the focus.

---

# 3. Desktop-First Design

Version 1 is designed primarily for desktop computers.

Primary use case:

```text
Windows desktop
+
Modern browser
```

The interface should feel closer to a professional desktop business tool than a mobile-first website.

Tablet compatibility is desirable.

Dedicated mobile UX is outside Version 1.

---

# 4. Consistency

The same action should behave the same way everywhere.

Examples:

- Save buttons should look and behave consistently.
- Lists should use the same filtering patterns.
- Record opening behavior should remain consistent.
- Confirmation dialogs should follow the same pattern.
- Error messages should use the same visual language.

Users should not need to relearn controls between modules.

---

# 5. Main Navigation

The application should use persistent primary navigation.

Initial major sections may include:

```text
Dashboard

Clients

Projects

Orders

Reports

Administration
```

The exact navigation may evolve as modules are implemented.

Primary navigation should remain simple.

---

# 6. Administration

System-management functionality should be grouped under Administration where appropriate.

Examples:

```text
Users

Roles

Permissions

Employees

Order Types

Calculator Templates
```

Do not overcrowd the main navigation with rarely used administrative features.

---

# 7. List Pages

Lists are a primary ERP interaction pattern.

Examples:

```text
Clients List

Projects List

Orders List

Employees List
```

Lists should support the features that are useful for their specific module.

Common capabilities may include:

- Sorting
- Filtering
- Searching
- Pagination
- Column resizing
- Column visibility
- Row selection

Do not automatically add every possible table feature to every list.

---

# 8. Record Opening

Default list behavior:

```text
Single click
=
Select row
```

```text
Double click
=
Open record
```

This should remain consistent where data tables use row selection.

Clearly visible actions may also be provided when appropriate.

---

# 9. Business ID Visibility

Important business records should visibly display their Business ID.

Examples:

```text
CL-000125

PRJ-2026-000042

ORD-2026-000351
```

Business IDs should appear prominently enough to support communication and search.

Internal UUID values should normally remain hidden.

---

# 10. Workspaces

Important business records should use the Workspace concept.

A Workspace keeps information related to one record together.

Examples:

```text
Project Workspace

Order Workspace
```

Users should not need to navigate through multiple disconnected pages to manage one record.

---

# 11. Order Workspace

The Order Workspace may contain areas such as:

```text
Order Header

General Information

Calculator

Checklist

Folder Links
```

These areas may be implemented using:

- Tabs
- Sections
- Panels
- Collapsible areas

Choose the simplest layout that keeps the Order understandable.

---

# 12. Project Workspace

The Project Workspace may contain:

```text
Project Information

Project Team

Orders
```

The user should be able to understand the Project and access its Orders from the same context.

---

# 13. Workspace Header

Important Workspaces should have a clear header.

Example:

```text
ORD-2026-000351
UV Printed Entrance Panels
```

The header may also show important status information.

Avoid placing excessive actions in the header.

---

# 14. Forms

Forms should remain simple and predictable.

Guidelines:

- Use clear labels.
- Group related fields.
- Avoid unnecessary fields.
- Avoid excessive scrolling when practical.
- Show validation close to the relevant field.
- Use appropriate control types.
- Do not hide required fields behind unclear interactions.

---

# 15. Required Fields

Required fields should be visually identifiable.

Validation should not rely only on color.

The user should understand what is required before trying to save.

---

# 16. Save Behavior

For normal editable records, changes should not be silently committed without clear behavior.

Version 1 should prefer explicit actions such as:

```text
Save
```

or clearly defined immediate-save interactions for simple sub-elements.

The chosen behavior must remain consistent within the same feature.

---

# 17. Checklist Interaction

Checklist interaction should be extremely simple.

The Order Workspace contains:

```text
Add Checklist Item
```

Pressing it creates a new row.

Each row contains:

```text
Checkbox

Text
```

The system also maintains internal Sort Order.

The user should be able to quickly:

- Add a row
- Type text
- Mark it completed
- Reorder items if reordering is implemented
- Remove an item if permitted

Do not overload checklist rows with extra fields.

---

# 18. Calculator Interaction

The Calculator should feel spreadsheet-like where useful but remain simpler than Excel.

Important goals:

- Inputs are obvious.
- Calculated values are visually distinguishable.
- Formula results update predictably.
- Tables are easy to edit.
- Errors are understandable.

The Calculator UI must follow its dedicated module specification.

---

# 19. Template Designer

The Calculator Template Designer is an advanced administrative interface.

It should prioritize:

- Clear structure
- Predictable field placement
- Easy formula editing
- Easy template testing
- Easy assignment to Order Types

It does not need to imitate the entire Excel interface.

---

# 20. Tables

Business tables should prioritize readability.

Guidelines:

- Important columns first
- Avoid excessive column count
- Allow horizontal scrolling only when genuinely necessary
- Keep Business IDs visible where useful
- Align numeric values appropriately
- Use consistent date formatting
- Use consistent money formatting

---

# 21. Sorting

Sortable columns should make their current sort direction clear.

Default sorting should reflect the module's workflow.

For example, Orders may commonly be sorted by recent activity or creation date.

Exact defaults belong to module specifications.

---

# 22. Filtering

Filters should solve common work needs.

Examples may include:

```text
Status

Client

Order Type

Date range

Employee
```

Do not create large generic filter builders in Version 1.

Start with direct useful filters.

---

# 23. Search

Important modules should have straightforward search.

Search may support:

- Business ID
- Name
- Description
- Other module-specific fields

Business ID searches should be fast.

---

# 24. Empty States

When no records exist, the interface should explain what the user can do.

Example:

```text
No checklist items yet.

Add Checklist Item
```

Avoid displaying a confusing empty table without context.

---

# 25. Buttons

Buttons should describe actions clearly.

Good:

```text
Create Order

Save

Add Checklist Item

Reset Password
```

Avoid vague buttons such as:

```text
Do

Action

OK
```

when a clearer label is possible.

---

# 26. Primary Actions

Each screen should have a clear primary action when one exists.

Examples:

```text
Create Project

Save

Create User
```

Avoid having several visually competing primary buttons on one screen.

---

# 27. Destructive Actions

Destructive actions require care.

Examples:

```text
Delete

Deactivate

Remove
```

Where accidental use could cause significant loss or disruption, require confirmation.

Confirmation text should clearly describe what will happen.

---

# 28. Confirmation Dialogs

Do not use confirmation dialogs for routine harmless actions.

Use them for:

- Destructive operations
- Irreversible changes
- Security-sensitive operations

Dialogs should remain concise.

---

# 29. Dialogs

Dialogs are appropriate for focused interactions.

Examples:

- Confirm deletion
- Reset password
- Add a simple related record

Avoid using large dialogs as replacements for normal pages or Workspaces.

---

# 30. Notifications

Notifications should be brief and useful.

Examples:

```text
Order saved.

Project created.

Password reset.
```

Avoid unnecessary success notifications for every trivial interaction.

---

# 31. Error Messages

Error messages should explain:

- What went wrong
- What the user can do next when possible

Good:

```text
This username is already in use.
Choose another username.
```

Bad:

```text
Error 500
```

Technical details belong in server logs.

---

# 32. Validation

Frontend validation exists to help the user.

Backend validation remains authoritative.

UI validation should:

- Appear near the relevant field
- Use understandable language
- Avoid showing internal database terminology

---

# 33. Status Display

Business statuses should be easy to identify.

Status may use:

- Text
- Icon
- Color

Color must not be the only indicator.

---

# 34. Color Usage

Colors should communicate meaning rather than decoration.

Typical meanings:

```text
Green
Success / Completed

Yellow or Orange
Warning / Attention

Red
Error / Destructive / Critical

Blue
Information / Normal system emphasis
```

Exact theme colors are controlled by Material UI theme configuration.

---

# 35. Icons

Icons may support text.

Do not use unfamiliar icon-only actions where the meaning is unclear.

Provide:

- Text labels
- Tooltips
- Accessible names

where appropriate.

---

# 36. Typography

Typography should prioritize readability.

Use a limited consistent hierarchy for:

- Page titles
- Workspace titles
- Section titles
- Field labels
- Table text
- Helper text

Avoid unnecessary decorative typography.

---

# 37. Spacing

Layouts should have enough spacing to remain readable but should not waste large amounts of screen space.

Because Lithograph ERP is a productivity tool, information density may be higher than on a marketing website.

---

# 38. Screen Real Estate

Desktop screens should use available width intelligently.

Avoid extremely narrow centered layouts for data-heavy Workspaces.

Lists and Workspaces may use wide layouts when the information benefits from it.

---

# 39. Responsive Design

Version 1 is desktop-first.

Responsive behavior should prevent the UI from breaking at smaller widths.

It is not necessary to redesign every complex workflow specifically for phones.

---

# 40. Keyboard Navigation

Normal browser and accessibility keyboard navigation should work.

Examples:

```text
Tab
Shift + Tab
Enter
Escape
```

where appropriate.

Keyboard access should not require the mouse for every operation.

---

# 41. Application Shortcuts

Custom keyboard shortcuts may be added later where they provide clear productivity benefits.

Do not override standard browser or operating-system shortcuts casually.

For example, avoid automatically taking over:

```text
Ctrl + N

Ctrl + T

Ctrl + W

Ctrl + L
```

because browsers already use them.

---

# 42. Safe Shortcuts

If custom shortcuts are introduced, they should:

- Be documented
- Be consistent
- Avoid browser conflicts
- Be optional where practical

The shortcut system should not be implemented until real workflows demonstrate a need.

---

# 43. Context Menus

Right-click context menus may be used when they improve productivity.

They must not be the only way to access important actions.

Every important action should remain discoverable through normal UI controls.

---

# 44. Drag and Drop

Drag-and-drop may be used where it provides obvious value.

Possible example:

```text
Reordering Checklist Items
```

Do not introduce drag-and-drop for actions where a normal control is clearer.

---

# 45. Loading States

When data is loading, the user should receive appropriate feedback.

Use:

- Loading indicator
- Skeleton
- Disabled action state

as appropriate.

Avoid leaving the UI appearing frozen.

---

# 46. Saving States

When a save action is running:

- Prevent accidental duplicate submissions.
- Show that the operation is processing.
- Clearly indicate failure if save fails.

---

# 47. Unsaved Changes

For significant forms where users may lose substantial work, warn before navigating away when unsaved changes exist.

Do not implement aggressive warnings for trivial inline edits unless they are necessary.

---

# 48. Money Display

Money values should be displayed consistently.

Examples:

```text
Selling Price

Cost Price
```

Use appropriate numeric separators and formatting.

The detailed currency strategy belongs to the relevant business specification.

---

# 49. Numeric Inputs

Numeric inputs must make units clear when applicable.

Examples:

```text
Width (mm)

Height (mm)

Quantity

Area (m²)
```

Do not rely on users remembering hidden units.

---

# 50. Dates

Dates should use one consistent user-facing format throughout the ERP.

The implementation should distinguish:

- Date-only business values
- Date/time events

Internal UTC storage rules are defined in the database specification.

---

# 51. Natural Language

UI labels should use business language defined in:

```text
04_Data_Dictionary.md
```

Example:

Use:

```text
Order
```

not randomly:

```text
Job
Task
Work Item
```

Consistency in language is part of UX.

---

# 52. Technical Terms

Avoid exposing technical database or API language to normal users.

For example, users should see:

```text
Order ID
```

rather than:

```text
business_id
```

and should normally never see:

```text
UUID
Foreign Key
Entity
DTO
```

---

# 53. Permissions and UI

If a User lacks permission for an action, the interface should not encourage them to use it.

Depending on the feature, actions may be:

- Hidden
- Disabled

Backend authorization must still enforce security.

UI hiding alone is never security.

---

# 54. Sensitive Information

Sensitive values such as password hashes, session tokens and security secrets must never appear in the normal UI.

Password inputs should use standard secure password controls.

---

# 55. Director Experience

The Director needs access to Administration functionality such as:

- User management
- Role management
- Employee management
- Order Type configuration
- Calculator Templates

These administrative interfaces should remain separated from normal daily production workflows where practical.

---

# 56. Employee-Focused Simplicity

Operators and production staff should not be forced to navigate administrative functionality unrelated to their work.

Permissions and navigation should help keep each employee's interface relevant.

---

# 57. Performance Perception

The UI should feel responsive.

Prefer:

- Quick list loading
- Local visual feedback
- Efficient API calls
- Minimal unnecessary page reloads

Do not add complex frontend optimization before it is needed.

---

# 58. Browser Refresh

Important record URLs should remain meaningful enough that refreshing the browser does not unexpectedly lose the user's current record context.

Where practical, Workspaces should have stable routes.

Example concept:

```text
/orders/<id>
```

The user-facing route design will be finalized during implementation.

---

# 59. Deep Linking

Important records should eventually be directly addressable by URL.

This makes it possible to:

- Bookmark records
- Share internal links
- Return to a specific Order

Do not expose sensitive information through URL parameters.

---

# 60. Accessibility

The application should follow reasonable accessibility practices.

Important goals include:

- Keyboard navigation
- Sufficient contrast
- Proper labels
- Accessible dialog behavior
- Semantic controls

Accessibility should be supported through standard React and Material UI practices rather than treated as a separate later rewrite.

---

# 61. User Preferences

Version 1 should avoid building a large personalization system.

Future versions may introduce:

- Saved table layouts
- Theme preferences
- Dashboard customization

Only introduce these when there is a demonstrated need.

---

# 62. Dark Mode

Dark mode is not required for Version 1.

The UI architecture should not deliberately prevent future theme support.

---

# 63. Undo

Do not build a generic global undo system in Version 1.

Where accidental changes are important, use:

- Confirmation
- Soft delete
- Explicit Cancel
- Restore workflows

as appropriate.

---

# 64. Auto-Save

Do not make the entire ERP auto-save by default.

Auto-save may be appropriate for specific simple interactions if clearly designed.

Examples could include:

- Checklist completion state

The detailed behavior should be defined per feature.

---

# 65. UI Complexity Rule

Before adding a new interaction pattern, ask:

1. Does this make daily work faster?
2. Is the behavior obvious?
3. Can the same result be achieved more simply?
4. Will the pattern be reusable?
5. Does it conflict with existing UI behavior?

Prefer the simplest usable design.

---

# 66. Module UI Consistency

Modules should not invent their own visual systems.

All modules should use:

- Shared theme
- Shared spacing approach
- Shared form behavior
- Shared table behavior
- Shared notifications
- Shared dialog patterns

while keeping business-specific interfaces inside their own modules.

---

# 67. Version 1 UI Non-Goals

Version 1 does not require:

```text
Mobile Application

Complex Dashboard Customization

Full User-Defined Themes

Global Drag-and-Drop Interface

Global Undo System

Advanced Keyboard Shortcut System

Desktop Window Management

Windows Explorer Integration
```

These may be evaluated later.

---

# 68. Final UI Principle

Lithograph ERP should feel like a focused professional tool.

The best interface is not the one with the most controls.

The best interface is the one that allows employees to understand what they are looking at and complete their work quickly.

Every UI decision should support:

```text
Clarity

Speed

Consistency

Low Cognitive Load
```

---

**End of Document**