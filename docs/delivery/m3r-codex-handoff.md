# Codex handoff — M3R Training Refactor

Use this handoff after reading `AGENTS.md`, `docs/design/trainer-training-refactor.md`, `docs/architecture/domain.md` and the M3R section of `docs/delivery/backlog.md`.

## Why this refactor exists

The first M3 implementation is functionally connected but product-wise too shallow. The Trainer currently creates a template through free text and the UI starts from a single exercise. The intended Personal Ultra experience is a fast, visual prescription flow backed by a reusable exercise catalog, while the Student workout experience should preserve the successful SVR-derived UX.

Do not patch the existing one-exercise form incrementally if that keeps the wrong interaction model. Refactor the Trainer prescription surface toward the documented flow while preserving working backend/session/offline behavior where possible.

## Execution strategy

Do **not** implement all M3R tasks in one Codex run.

Recommended order:

```text
PU-M3R-001  catalog domain
PU-M3R-002  seed + existing exercise assets mapping
PU-M3R-003  template prescription model
PU-M3R-004  Student/session snapshots
PU-M3R-005  catalog read API
PU-M3R-006  student-centric Trainer navigation/shell
PU-M3R-007  catalog selector UI
PU-M3R-008  multi-exercise workout editor
PU-M3R-009  template reuse/apply snapshot
PU-M3R-010  Student compatibility/media adaptation
PU-M3R-011  execution/history/offline regression pass
PU-M3R-012  complete commercial demo seed
```

Each task is a gate. Implement and validate only the requested task before continuing.

## Critical preservation rules

- Keep `TrainerApi` and `StudentApi` actor boundaries.
- Keep one PostgreSQL/EF Core domain/infrastructure foundation.
- Do not introduce a catalog admin UI in V1.
- Catalog content is seeded in V1.
- Reuse existing exercise assets from the donor/SVR codebase when they still exist in the repository or can be mapped cleanly.
- Do not redesign the Student workout UX unless required by the new catalog-backed data.
- Do not add recommended load.
- Do not add video workflows.
- Do not add AI workout generation.
- Do not add speculative versioning infrastructure.
- Templates are optional accelerators, not the center of the Student prescription UX.
- Student workouts are editable snapshots; later template changes must not mutate existing Student workouts.
- Historical sessions must remain reconstructable after catalog/template changes.

## Standard task prompt

Use the following pattern, replacing the task ID:

```text
Read AGENTS.md, docs/design/trainer-training-refactor.md, docs/architecture/domain.md, docs/delivery/backlog.md and this handoff before making changes.

Implement PU-M3R-XXX only.

This is a corrective refactor of the first M3 implementation. Preserve working behavior outside the current task and do not advance future M3R tasks.

The approved product direction is:
- Trainer prescription is student-centric.
- Exercises come from a reusable system catalog seeded in V1.
- Trainer can eventually build a workout with multiple catalog exercises.
- Exercise prescription data is separate from catalog data.
- Student workout and session history use stable snapshots where required.
- Existing Student workout UX should be preserved.
- Personal Ultra dark design system is authoritative.

Do not implement:
- catalog admin;
- uploads;
- video workflow;
- AI workout generation;
- recommended load;
- future M3R tasks.

Before changing architecture outside the documented boundaries, report the ambiguity instead of inventing a new direction.

Validation:
- run relevant .NET build/tests;
- run mobile typecheck when mobile code changes;
- preserve API actor boundaries;
- report migrations/data implications;
- report any existing M3 code intentionally retained or replaced.

At the end report:
- files changed;
- behavior delivered;
- tests/commands run;
- remaining limitations belonging to later M3R tasks;
- any discovered regression risk.

Do not continue to the next M3R task.
```

## Product acceptance target

When all M3R gates are finished, the demo should prove this story without fake UI data:

Trainer opens Ana → sees Upper A / Lower A / Upper B / Lower B → opens Upper A → sees several exercises with images → searches the seeded catalog → selects an exercise → configures sets, rep range, rest and notes → inserts/reorders it → saves → Student sees the updated workout in the established Student flow → executes and records actual weight/repetitions → Trainer sees the resulting history.
