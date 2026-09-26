# Lithograph ERP — Agent Instructions

Before any non-trivial change:

1. Read `AI_RULES.md`.
2. Read `docs/41_V1_Final_Documentation_Index.md`.
3. Read only the module documents and implementation plan relevant to the current task.
4. Treat approved documentation as the specification. Existing code is not automatically correct.
5. Do not invent fields, modules, permissions, tables, abstractions, infrastructure, or business rules.
6. Do not implement a later phase while the current phase completion gate is incomplete.
7. Do not rewrite already-applied migrations. Create a new migration when necessary.
8. Keep the V1 architecture a modular monolith using ASP.NET Core, EF Core, PostgreSQL, React, TypeScript, and Material UI.
9. Do not introduce microservices, generic repositories, MediatR/CQRS infrastructure, Redis, event buses, or other architecture unless explicitly approved.
10. Backend authorization is authoritative.
11. Preserve all invariants defined in the documentation, especially Calculator version immutability and financial synchronization rules.
12. After implementation, build the affected projects and run relevant tests.
13. Fix failures caused by the current task before declaring it complete.
14. Never declare a phase complete without checking its documented Completion Gate.
15. Report:
    - files changed
    - migrations created
    - tests added/changed
    - commands executed
    - build/test results
    - anything not completed

Work on one bounded implementation phase at a time.