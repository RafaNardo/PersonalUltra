# PU-M0-001 — Baseline audit

Date: 2026-08-12

## Scope decision

This milestone retains the donor application's working technical foundation.
It does not rename the SVR Method solution, packages, namespaces, database,
or mobile identifiers; those changes are explicitly assigned to `PU-M0-002`.
It also does not create actor-specific APIs or the role-switching mobile
composition, which belong to `PU-M0-003` and `PU-M0-004`.

The mobile client no longer defaults to the donor's deployed Railway API. It
defaults to the local compose/API endpoint and remains configurable with
`EXPO_PUBLIC_API_URL`. This keeps a new checkout self-contained while
preserving the existing API client as donor code until the separate actor
clients are introduced.

## Safe reusable foundation

| Area | Retained baseline |
| --- | --- |
| Mobile runtime | Expo, Expo Router, TypeScript, React Native and the existing workspace/scripts |
| Mobile platform services | TanStack Query, Zustand, React Hook Form, Zod, SQLite workout cache/queue, feedback and telemetry seams |
| UI | Generic primitives, layout/error boundary, design-token mechanism and exercise media utility |
| Backend runtime | ASP.NET Core minimal API host, EF Core, Npgsql/PostgreSQL, migrations, Swagger/Scalar and development authentication seam |
| Local development | `compose.yaml`, Containerfile, development seed/reset pattern and environment configuration |
| Verification | xUnit integration-test project and TypeScript typecheck convention |

## Boundaries preserved for subsequent milestones

The current mobile code is an unpartitioned donor application. No new code is
placed in a cross-actor module and no role-based authorization is introduced.
`PU-M0-004` must establish `src/features/trainer`, `src/features/student`,
`src/shared`, independent navigation trees, and `trainer-client` /
`student-client`; imports between the actor features must remain prohibited.

The backend remains a single donor API only temporarily. `PU-M0-003` must
introduce `TrainerApi` and `StudentApi` over shared Domain, Application,
Infrastructure, DbContext and PostgreSQL, rather than duplicating services or
databases.

## Inherited product assumptions deliberately left for later work

- SVR visual names and product copy in the inherited screens/assets
  (`PU-M0-008`). Technical solution/package/namespace/database identifiers,
  compose names, app identifiers and persisted SQLite/auth-store keys were
  renamed in `PU-M0-002`.
- A single member-oriented API and mobile navigation tree (`PU-M0-003`,
  `PU-M0-004`).
- SVR visual assets and product copy (`PU-M0-008`).
- `Member` (`PU-M0-005`); methodology rules, standard-plan provisioning,
  recommended load, automatic progression, progress photos and Coach action
  writes were removed in `PU-M0-006`.
- The inherited Coach UI/API could perform mutation flows; Coach V1 is now
  read-only, with its product experience completed in M4.

## Resolved follow-up decisions

- The PostgreSQL data/volume may be reset for the new Personal Ultra
  infrastructure. `PU-M0-002` therefore uses new database, credential and
  compose-volume identifiers without deleting local Podman data.
- The inherited exercise assets are licensed for continued use. Their existing
  SVR file names and visual treatment remain product/branding work for
  `PU-M0-008`.
