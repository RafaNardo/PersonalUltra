# API regression gate

## Decision

The demo uses the existing
`tests/PersonalUltra.Api.IntegrationTests` project as its API regression gate.
A separate k6 or test project would duplicate bootstrapping, authentication and
database assertions without improving the current product-demo feedback loop.

The suite hosts the real `TrainerApi` and `StudentApi` endpoint graphs through
`WebApplicationFactory`, with an isolated EF Core database for each fixture. It
does not mock endpoint handlers. Provider boundaries such as S3 signing use
non-secret test configuration and do not call paid/external services.

## Local command

From the repository root:

```powershell
npm run test:api:regression
```

The command builds and runs the entire API integration project. The suite is
small enough that a filtered “smoke” subset would save negligible time while
increasing the chance of a missed regression.

## GitHub Actions

`.github/workflows/api-regression.yml` contains the same gate for GitHub Actions.
It is temporarily restricted to manual dispatch because the repository account
reported an Actions billing lock on 2026-08-24, before the job could start.
Automatic `push` and `pull_request` triggers should be restored when that
external account issue is resolved; keeping them enabled meanwhile would mark
every push as failed without executing a test.

The workflow restores, builds with warnings treated as errors and runs every API
integration test. It needs no application secrets and never mutates Railway.

## Current coverage

The gate covers:

- Trainer and Student authentication/ownership boundaries;
- invites, Student creation, onboarding and anamnesis;
- Trainer dashboard, Student detail and Trainer messages;
- exercise catalog, media references, presets and Student prescriptions;
- workout ordering, immutable session snapshots and soft deletion;
- detailed set synchronization, idempotency and free exercise order;
- repetition and duration tracking;
- explicit exercise/workout completion without synthetic performances;
- Trainer-visible workout history;
- prescription settings and catalog/demo seed idempotency;
- nutrition ownership/authentication, empty state, Trainer-to-Student roundtrip,
  ordered meals/items with units, attribution, full replacement and validation
  failures that preserve the previous plan.

Weight endpoints currently have no dedicated integration tests and remain an
explicit gap. The nutrition review added four dedicated scenarios, bringing the
gate to 100 tests.

## What this gate does not prove

This is not a mobile UI test, PostgreSQL migration rehearsal, Railway smoke test,
load test or visual review. Mobile changes still require TypeScript typecheck and
Expo export, while UX remains a device-review responsibility until a future E2E
mobile gate is intentionally added.
